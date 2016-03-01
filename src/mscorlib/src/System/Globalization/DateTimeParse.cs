using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using System.Threading;

namespace System
{
    internal static class DateTimeParse
    {
        internal const Int32 MaxDateTimeNumberDigits = 8;
        internal delegate bool MatchNumberDelegate(ref __DTString str, int digitLen, out int result);
        internal static MatchNumberDelegate m_hebrewNumberParser = new MatchNumberDelegate(DateTimeParse.MatchHebrewDigits);
        internal static DateTime ParseExact(String s, String format, DateTimeFormatInfo dtfi, DateTimeStyles style)
        {
            DateTimeResult result = new DateTimeResult();
            result.Init();
            if (TryParseExact(s, format, dtfi, style, ref result))
            {
                return result.parsedDate;
            }
            else
            {
                throw GetDateTimeParseException(ref result);
            }
        }

        internal static DateTime ParseExact(String s, String format, DateTimeFormatInfo dtfi, DateTimeStyles style, out TimeSpan offset)
        {
            DateTimeResult result = new DateTimeResult();
            offset = TimeSpan.Zero;
            result.Init();
            result.flags |= ParseFlags.CaptureOffset;
            if (TryParseExact(s, format, dtfi, style, ref result))
            {
                offset = result.timeZoneOffset;
                return result.parsedDate;
            }
            else
            {
                throw GetDateTimeParseException(ref result);
            }
        }

        internal static bool TryParseExact(String s, String format, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result)
        {
            result = DateTime.MinValue;
            DateTimeResult resultData = new DateTimeResult();
            resultData.Init();
            if (TryParseExact(s, format, dtfi, style, ref resultData))
            {
                result = resultData.parsedDate;
                return true;
            }

            return false;
        }

        internal static bool TryParseExact(String s, String format, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result, out TimeSpan offset)
        {
            result = DateTime.MinValue;
            offset = TimeSpan.Zero;
            DateTimeResult resultData = new DateTimeResult();
            resultData.Init();
            resultData.flags |= ParseFlags.CaptureOffset;
            if (TryParseExact(s, format, dtfi, style, ref resultData))
            {
                result = resultData.parsedDate;
                offset = resultData.timeZoneOffset;
                return true;
            }

            return false;
        }

        internal static bool TryParseExact(String s, String format, DateTimeFormatInfo dtfi, DateTimeStyles style, ref DateTimeResult result)
        {
            if (s == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "s");
                return false;
            }

            if (format == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "format");
                return false;
            }

            if (s.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (format.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
                return false;
            }

            Contract.Assert(dtfi != null, "dtfi == null");
            return DoStrictParse(s, format, style, dtfi, ref result);
        }

        internal static DateTime ParseExactMultiple(String s, String[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style)
        {
            DateTimeResult result = new DateTimeResult();
            result.Init();
            if (TryParseExactMultiple(s, formats, dtfi, style, ref result))
            {
                return result.parsedDate;
            }
            else
            {
                throw GetDateTimeParseException(ref result);
            }
        }

        internal static DateTime ParseExactMultiple(String s, String[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out TimeSpan offset)
        {
            DateTimeResult result = new DateTimeResult();
            offset = TimeSpan.Zero;
            result.Init();
            result.flags |= ParseFlags.CaptureOffset;
            if (TryParseExactMultiple(s, formats, dtfi, style, ref result))
            {
                offset = result.timeZoneOffset;
                return result.parsedDate;
            }
            else
            {
                throw GetDateTimeParseException(ref result);
            }
        }

        internal static bool TryParseExactMultiple(String s, String[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result, out TimeSpan offset)
        {
            result = DateTime.MinValue;
            offset = TimeSpan.Zero;
            DateTimeResult resultData = new DateTimeResult();
            resultData.Init();
            resultData.flags |= ParseFlags.CaptureOffset;
            if (TryParseExactMultiple(s, formats, dtfi, style, ref resultData))
            {
                result = resultData.parsedDate;
                offset = resultData.timeZoneOffset;
                return true;
            }

            return false;
        }

        internal static bool TryParseExactMultiple(String s, String[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, out DateTime result)
        {
            result = DateTime.MinValue;
            DateTimeResult resultData = new DateTimeResult();
            resultData.Init();
            if (TryParseExactMultiple(s, formats, dtfi, style, ref resultData))
            {
                result = resultData.parsedDate;
                return true;
            }

            return false;
        }

        internal static bool TryParseExactMultiple(String s, String[] formats, DateTimeFormatInfo dtfi, DateTimeStyles style, ref DateTimeResult result)
        {
            if (s == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "s");
                return false;
            }

            if (formats == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "formats");
                return false;
            }

            if (s.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (formats.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
                return false;
            }

            Contract.Assert(dtfi != null, "dtfi == null");
            for (int i = 0; i < formats.Length; i++)
            {
                if (formats[i] == null || formats[i].Length == 0)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
                    return false;
                }

                DateTimeResult innerResult = new DateTimeResult();
                innerResult.Init();
                innerResult.flags = result.flags;
                if (TryParseExact(s, formats[i], dtfi, style, ref innerResult))
                {
                    result.parsedDate = innerResult.parsedDate;
                    result.timeZoneOffset = innerResult.timeZoneOffset;
                    return (true);
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return (false);
        }

        internal enum DTT : int
        {
            End = 0,
            NumEnd = 1,
            NumAmpm = 2,
            NumSpace = 3,
            NumDatesep = 4,
            NumTimesep = 5,
            MonthEnd = 6,
            MonthSpace = 7,
            MonthDatesep = 8,
            NumDatesuff = 9,
            NumTimesuff = 10,
            DayOfWeek = 11,
            YearSpace = 12,
            YearDateSep = 13,
            YearEnd = 14,
            TimeZone = 15,
            Era = 16,
            NumUTCTimeMark = 17,
            Unk = 18,
            NumLocalTimeMark = 19,
            Max = 20
        }

        internal enum TM
        {
            NotSet = -1,
            AM = 0,
            PM = 1
        }

        internal enum DS
        {
            BEGIN = 0,
            N = 1,
            NN = 2,
            D_Nd = 3,
            D_NN = 4,
            D_NNd = 5,
            D_M = 6,
            D_MN = 7,
            D_NM = 8,
            D_MNd = 9,
            D_NDS = 10,
            D_Y = 11,
            D_YN = 12,
            D_YNd = 13,
            D_YM = 14,
            D_YMd = 15,
            D_S = 16,
            T_S = 17,
            T_Nt = 18,
            T_NNt = 19,
            ERROR = 20,
            DX_NN = 21,
            DX_NNN = 22,
            DX_MN = 23,
            DX_NM = 24,
            DX_MNN = 25,
            DX_DS = 26,
            DX_DSN = 27,
            DX_NDS = 28,
            DX_NNDS = 29,
            DX_YNN = 30,
            DX_YMN = 31,
            DX_YN = 32,
            DX_YM = 33,
            TX_N = 34,
            TX_NN = 35,
            TX_NNN = 36,
            TX_TS = 37,
            DX_NNY = 38
        }

        private static DS[][] dateParsingStates = {new DS[]{DS.BEGIN, DS.ERROR, DS.TX_N, DS.N, DS.D_Nd, DS.T_Nt, DS.ERROR, DS.D_M, DS.D_M, DS.D_S, DS.T_S, DS.BEGIN, DS.D_Y, DS.D_Y, DS.ERROR, DS.BEGIN, DS.BEGIN, DS.ERROR}, new DS[]{DS.ERROR, DS.DX_NN, DS.ERROR, DS.NN, DS.D_NNd, DS.ERROR, DS.DX_NM, DS.D_NM, DS.D_MNd, DS.D_NDS, DS.ERROR, DS.N, DS.D_YN, DS.D_YNd, DS.DX_YN, DS.N, DS.N, DS.ERROR}, new DS[]{DS.DX_NN, DS.DX_NNN, DS.TX_N, DS.DX_NNN, DS.ERROR, DS.T_Nt, DS.DX_MNN, DS.DX_MNN, DS.ERROR, DS.ERROR, DS.T_S, DS.NN, DS.DX_NNY, DS.ERROR, DS.DX_NNY, DS.NN, DS.NN, DS.ERROR}, new DS[]{DS.ERROR, DS.DX_NN, DS.ERROR, DS.D_NN, DS.D_NNd, DS.ERROR, DS.DX_NM, DS.D_MN, DS.D_MNd, DS.ERROR, DS.ERROR, DS.D_Nd, DS.D_YN, DS.D_YNd, DS.DX_YN, DS.ERROR, DS.D_Nd, DS.ERROR}, new DS[]{DS.DX_NN, DS.DX_NNN, DS.TX_N, DS.DX_NNN, DS.ERROR, DS.T_Nt, DS.DX_MNN, DS.DX_MNN, DS.ERROR, DS.DX_DS, DS.T_S, DS.D_NN, DS.DX_NNY, DS.ERROR, DS.DX_NNY, DS.ERROR, DS.D_NN, DS.ERROR}, new DS[]{DS.ERROR, DS.DX_NNN, DS.DX_NNN, DS.DX_NNN, DS.ERROR, DS.ERROR, DS.DX_MNN, DS.DX_MNN, DS.ERROR, DS.DX_DS, DS.ERROR, DS.D_NNd, DS.DX_NNY, DS.ERROR, DS.DX_NNY, DS.ERROR, DS.D_NNd, DS.ERROR}, new DS[]{DS.ERROR, DS.DX_MN, DS.ERROR, DS.D_MN, DS.D_MNd, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_M, DS.D_YM, DS.D_YMd, DS.DX_YM, DS.ERROR, DS.D_M, DS.ERROR}, new DS[]{DS.DX_MN, DS.DX_MNN, DS.DX_MNN, DS.DX_MNN, DS.ERROR, DS.T_Nt, DS.ERROR, DS.ERROR, DS.ERROR, DS.DX_DS, DS.T_S, DS.D_MN, DS.DX_YMN, DS.ERROR, DS.DX_YMN, DS.ERROR, DS.D_MN, DS.ERROR}, new DS[]{DS.DX_NM, DS.DX_MNN, DS.DX_MNN, DS.DX_MNN, DS.ERROR, DS.T_Nt, DS.ERROR, DS.ERROR, DS.ERROR, DS.DX_DS, DS.T_S, DS.D_NM, DS.DX_YMN, DS.ERROR, DS.DX_YMN, DS.ERROR, DS.D_NM, DS.ERROR}, new DS[]{DS.ERROR, DS.DX_MNN, DS.ERROR, DS.DX_MNN, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_MNd, DS.DX_YMN, DS.ERROR, DS.DX_YMN, DS.ERROR, DS.D_MNd, DS.ERROR}, new DS[]{DS.DX_NDS, DS.DX_NNDS, DS.DX_NNDS, DS.DX_NNDS, DS.ERROR, DS.T_Nt, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_NDS, DS.T_S, DS.D_NDS, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_NDS, DS.ERROR}, new DS[]{DS.ERROR, DS.DX_YN, DS.ERROR, DS.D_YN, DS.D_YNd, DS.ERROR, DS.DX_YM, DS.D_YM, DS.D_YMd, DS.D_YM, DS.ERROR, DS.D_Y, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_Y, DS.ERROR}, new DS[]{DS.DX_YN, DS.DX_YNN, DS.DX_YNN, DS.DX_YNN, DS.ERROR, DS.ERROR, DS.DX_YMN, DS.DX_YMN, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_YN, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_YN, DS.ERROR}, new DS[]{DS.ERROR, DS.DX_YNN, DS.DX_YNN, DS.DX_YNN, DS.ERROR, DS.ERROR, DS.DX_YMN, DS.DX_YMN, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_YN, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_YN, DS.ERROR}, new DS[]{DS.DX_YM, DS.DX_YMN, DS.DX_YMN, DS.DX_YMN, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_YM, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_YM, DS.ERROR}, new DS[]{DS.ERROR, DS.DX_YMN, DS.DX_YMN, DS.DX_YMN, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_YM, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_YM, DS.ERROR}, new DS[]{DS.DX_DS, DS.DX_DSN, DS.TX_N, DS.T_Nt, DS.ERROR, DS.T_Nt, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_S, DS.T_S, DS.D_S, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_S, DS.ERROR}, new DS[]{DS.TX_TS, DS.TX_TS, DS.TX_TS, DS.T_Nt, DS.D_Nd, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.D_S, DS.T_S, DS.T_S, DS.ERROR, DS.ERROR, DS.ERROR, DS.T_S, DS.T_S, DS.ERROR}, new DS[]{DS.ERROR, DS.TX_NN, DS.TX_NN, DS.TX_NN, DS.ERROR, DS.T_NNt, DS.DX_NM, DS.D_NM, DS.ERROR, DS.ERROR, DS.T_S, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.T_Nt, DS.T_Nt, DS.TX_NN}, new DS[]{DS.ERROR, DS.TX_NNN, DS.TX_NNN, DS.TX_NNN, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.ERROR, DS.T_S, DS.T_NNt, DS.ERROR, DS.ERROR, DS.ERROR, DS.T_NNt, DS.T_NNt, DS.TX_NNN}, };
        internal const String GMTName = "GMT";
        internal const String ZuluName = "Z";
        private static bool MatchWord(ref __DTString str, String target)
        {
            int length = target.Length;
            if (length > (str.Value.Length - str.Index))
            {
                return false;
            }

            if (str.CompareInfo.Compare(str.Value, str.Index, length, target, 0, length, CompareOptions.IgnoreCase) != 0)
            {
                return (false);
            }

            int nextCharIndex = str.Index + target.Length;
            if (nextCharIndex < str.Value.Length)
            {
                char nextCh = str.Value[nextCharIndex];
                if (Char.IsLetter(nextCh))
                {
                    return (false);
                }
            }

            str.Index = nextCharIndex;
            if (str.Index < str.len)
            {
                str.m_current = str.Value[str.Index];
            }

            return (true);
        }

        private static bool GetTimeZoneName(ref __DTString str)
        {
            if (MatchWord(ref str, GMTName))
            {
                return (true);
            }

            if (MatchWord(ref str, ZuluName))
            {
                return (true);
            }

            return (false);
        }

        internal static bool IsDigit(char ch)
        {
            return (ch >= '0' && ch <= '9');
        }

        private static bool ParseFraction(ref __DTString str, out double result)
        {
            result = 0;
            double decimalBase = 0.1;
            int digits = 0;
            char ch;
            while (str.GetNext() && IsDigit(ch = str.m_current))
            {
                result += (ch - '0') * decimalBase;
                decimalBase *= 0.1;
                digits++;
            }

            return (digits > 0);
        }

        private static bool ParseTimeZone(ref __DTString str, ref TimeSpan result)
        {
            int hourOffset = 0;
            int minuteOffset = 0;
            DTSubString sub;
            sub = str.GetSubString();
            if (sub.length != 1)
            {
                return false;
            }

            char offsetChar = sub[0];
            if (offsetChar != '+' && offsetChar != '-')
            {
                return false;
            }

            str.ConsumeSubString(sub);
            sub = str.GetSubString();
            if (sub.type != DTSubStringType.Number)
            {
                return false;
            }

            int value = sub.value;
            int length = sub.length;
            if (length == 1 || length == 2)
            {
                hourOffset = value;
                str.ConsumeSubString(sub);
                sub = str.GetSubString();
                if (sub.length == 1 && sub[0] == ':')
                {
                    str.ConsumeSubString(sub);
                    sub = str.GetSubString();
                    if (sub.type != DTSubStringType.Number || sub.length < 1 || sub.length > 2)
                    {
                        return false;
                    }

                    minuteOffset = sub.value;
                    str.ConsumeSubString(sub);
                }
            }
            else if (length == 3 || length == 4)
            {
                hourOffset = value / 100;
                minuteOffset = value % 100;
                str.ConsumeSubString(sub);
            }
            else
            {
                return false;
            }

            Contract.Assert(hourOffset >= 0 && hourOffset <= 99, "hourOffset >= 0 && hourOffset <= 99");
            Contract.Assert(minuteOffset >= 0 && minuteOffset <= 99, "minuteOffset >= 0 && minuteOffset <= 99");
            if (minuteOffset < 0 || minuteOffset >= 60)
            {
                return false;
            }

            result = new TimeSpan(hourOffset, minuteOffset, 0);
            if (offsetChar == '-')
            {
                result = result.Negate();
            }

            return true;
        }

        private static bool HandleTimeZone(ref __DTString str, ref DateTimeResult result)
        {
            if ((str.Index < str.len - 1))
            {
                char nextCh = str.Value[str.Index];
                int whitespaceCount = 0;
                while (Char.IsWhiteSpace(nextCh) && str.Index + whitespaceCount < str.len - 1)
                {
                    whitespaceCount++;
                    nextCh = str.Value[str.Index + whitespaceCount];
                }

                if (nextCh == '+' || nextCh == '-')
                {
                    str.Index += whitespaceCount;
                    if ((result.flags & ParseFlags.TimeZoneUsed) != 0)
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    result.flags |= ParseFlags.TimeZoneUsed;
                    if (!ParseTimeZone(ref str, ref result.timeZoneOffset))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }
                }
            }

            return true;
        }

        private static Boolean Lex(DS dps, ref __DTString str, ref DateTimeToken dtok, ref DateTimeRawInfo raw, ref DateTimeResult result, ref DateTimeFormatInfo dtfi, DateTimeStyles styles)
        {
            TokenType tokenType;
            int tokenValue;
            int indexBeforeSeparator;
            char charBeforeSeparator;
            TokenType sep;
            dtok.dtt = DTT.Unk;
            str.GetRegularToken(out tokenType, out tokenValue, dtfi);
            if (_tracingEnabled)
            {
                BCLDebug.Trace("DATETIME", "[DATETIME] Lex({0})\tpos:{1}({2}), {3}, DS.{4}", Hex(str.Value), str.Index, Hex(str.m_current), tokenType, dps);
            }

            switch (tokenType)
            {
                case TokenType.NumberToken:
                case TokenType.YearNumberToken:
                    if (raw.numCount == 3 || tokenValue == -1)
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0010", dps);
                        return false;
                    }

                    if (dps == DS.T_NNt)
                    {
                        if ((str.Index < str.len - 1))
                        {
                            char nextCh = str.Value[str.Index];
                            if (nextCh == '.')
                            {
                                ParseFraction(ref str, out raw.fraction);
                            }
                        }
                    }

                    if (dps == DS.T_NNt || dps == DS.T_Nt)
                    {
                        if ((str.Index < str.len - 1))
                        {
                            if (false == HandleTimeZone(ref str, ref result))
                            {
                                LexTraceExit("0020 (value like \"12:01\" or \"12:\" followed by a non-TZ number", dps);
                                return false;
                            }
                        }
                    }

                    dtok.num = tokenValue;
                    if (tokenType == TokenType.YearNumberToken)
                    {
                        if (raw.year == -1)
                        {
                            raw.year = tokenValue;
                            switch (sep = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
                            {
                                case TokenType.SEP_End:
                                    dtok.dtt = DTT.YearEnd;
                                    break;
                                case TokenType.SEP_Am:
                                case TokenType.SEP_Pm:
                                    if (raw.timeMark == TM.NotSet)
                                    {
                                        raw.timeMark = (sep == TokenType.SEP_Am ? TM.AM : TM.PM);
                                        dtok.dtt = DTT.YearSpace;
                                    }
                                    else
                                    {
                                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                        LexTraceExit("0030 (TM.AM/TM.PM Happened more than 1x)", dps);
                                    }

                                    break;
                                case TokenType.SEP_Space:
                                    dtok.dtt = DTT.YearSpace;
                                    break;
                                case TokenType.SEP_Date:
                                    dtok.dtt = DTT.YearDateSep;
                                    break;
                                case TokenType.SEP_Time:
                                    if (!raw.hasSameDateAndTimeSeparators)
                                    {
                                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                        LexTraceExit("0040 (Invalid separator after number)", dps);
                                        return false;
                                    }

                                    dtok.dtt = DTT.YearDateSep;
                                    break;
                                case TokenType.SEP_DateOrOffset:
                                    if ((dateParsingStates[(int)dps][(int)DTT.YearDateSep] == DS.ERROR) && (dateParsingStates[(int)dps][(int)DTT.YearSpace] > DS.ERROR))
                                    {
                                        str.Index = indexBeforeSeparator;
                                        str.m_current = charBeforeSeparator;
                                        dtok.dtt = DTT.YearSpace;
                                    }
                                    else
                                    {
                                        dtok.dtt = DTT.YearDateSep;
                                    }

                                    break;
                                case TokenType.SEP_YearSuff:
                                case TokenType.SEP_MonthSuff:
                                case TokenType.SEP_DaySuff:
                                    dtok.dtt = DTT.NumDatesuff;
                                    dtok.suffix = sep;
                                    break;
                                case TokenType.SEP_HourSuff:
                                case TokenType.SEP_MinuteSuff:
                                case TokenType.SEP_SecondSuff:
                                    dtok.dtt = DTT.NumTimesuff;
                                    dtok.suffix = sep;
                                    break;
                                default:
                                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                    LexTraceExit("0040 (Invalid separator after number)", dps);
                                    return false;
                            }

                            LexTraceExit("0050 (success)", dps);
                            return true;
                        }

                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0060", dps);
                        return false;
                    }

                    switch (sep = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
                    {
                        case TokenType.SEP_End:
                            dtok.dtt = DTT.NumEnd;
                            raw.AddNumber(dtok.num);
                            break;
                        case TokenType.SEP_Am:
                        case TokenType.SEP_Pm:
                            if (raw.timeMark == TM.NotSet)
                            {
                                raw.timeMark = (sep == TokenType.SEP_Am ? TM.AM : TM.PM);
                                dtok.dtt = DTT.NumAmpm;
                                if (dps == DS.D_NN)
                                {
                                    if (!ProcessTerminaltState(DS.DX_NN, ref result, ref styles, ref raw, dtfi))
                                    {
                                        return false;
                                    }
                                }

                                raw.AddNumber(dtok.num);
                            }
                            else
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                break;
                            }

                            if (dps == DS.T_NNt || dps == DS.T_Nt)
                            {
                                if (false == HandleTimeZone(ref str, ref result))
                                {
                                    LexTraceExit("0070 (HandleTimeZone returned false)", dps);
                                    return false;
                                }
                            }

                            break;
                        case TokenType.SEP_Space:
                            dtok.dtt = DTT.NumSpace;
                            raw.AddNumber(dtok.num);
                            break;
                        case TokenType.SEP_Date:
                            dtok.dtt = DTT.NumDatesep;
                            raw.AddNumber(dtok.num);
                            break;
                        case TokenType.SEP_DateOrOffset:
                            if ((dateParsingStates[(int)dps][(int)DTT.NumDatesep] == DS.ERROR) && (dateParsingStates[(int)dps][(int)DTT.NumSpace] > DS.ERROR))
                            {
                                str.Index = indexBeforeSeparator;
                                str.m_current = charBeforeSeparator;
                                dtok.dtt = DTT.NumSpace;
                            }
                            else
                            {
                                dtok.dtt = DTT.NumDatesep;
                            }

                            raw.AddNumber(dtok.num);
                            break;
                        case TokenType.SEP_Time:
                            if (raw.hasSameDateAndTimeSeparators && (dps == DS.D_Y || dps == DS.D_YN || dps == DS.D_YNd || dps == DS.D_YM || dps == DS.D_YMd))
                            {
                                dtok.dtt = DTT.NumDatesep;
                                raw.AddNumber(dtok.num);
                                break;
                            }

                            dtok.dtt = DTT.NumTimesep;
                            raw.AddNumber(dtok.num);
                            break;
                        case TokenType.SEP_YearSuff:
                            try
                            {
                                dtok.num = dtfi.Calendar.ToFourDigitYear(tokenValue);
                            }
                            catch (ArgumentOutOfRangeException e)
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", e);
                                LexTraceExit("0075 (Calendar.ToFourDigitYear failed)", dps);
                                return false;
                            }

                            dtok.dtt = DTT.NumDatesuff;
                            dtok.suffix = sep;
                            break;
                        case TokenType.SEP_MonthSuff:
                        case TokenType.SEP_DaySuff:
                            dtok.dtt = DTT.NumDatesuff;
                            dtok.suffix = sep;
                            break;
                        case TokenType.SEP_HourSuff:
                        case TokenType.SEP_MinuteSuff:
                        case TokenType.SEP_SecondSuff:
                            dtok.dtt = DTT.NumTimesuff;
                            dtok.suffix = sep;
                            break;
                        case TokenType.SEP_LocalTimeMark:
                            dtok.dtt = DTT.NumLocalTimeMark;
                            raw.AddNumber(dtok.num);
                            break;
                        default:
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            LexTraceExit("0080", dps);
                            return false;
                    }

                    break;
                case TokenType.HebrewNumber:
                    if (tokenValue >= 100)
                    {
                        if (raw.year == -1)
                        {
                            raw.year = tokenValue;
                            switch (sep = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
                            {
                                case TokenType.SEP_End:
                                    dtok.dtt = DTT.YearEnd;
                                    break;
                                case TokenType.SEP_Space:
                                    dtok.dtt = DTT.YearSpace;
                                    break;
                                case TokenType.SEP_DateOrOffset:
                                    if (dateParsingStates[(int)dps][(int)DTT.YearSpace] > DS.ERROR)
                                    {
                                        str.Index = indexBeforeSeparator;
                                        str.m_current = charBeforeSeparator;
                                        dtok.dtt = DTT.YearSpace;
                                        break;
                                    }

                                    goto default;
                                default:
                                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                    LexTraceExit("0090", dps);
                                    return false;
                            }
                        }
                        else
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            LexTraceExit("0100", dps);
                            return false;
                        }
                    }
                    else
                    {
                        dtok.num = tokenValue;
                        raw.AddNumber(dtok.num);
                        switch (sep = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
                        {
                            case TokenType.SEP_End:
                                dtok.dtt = DTT.NumEnd;
                                break;
                            case TokenType.SEP_Space:
                            case TokenType.SEP_Date:
                                dtok.dtt = DTT.NumDatesep;
                                break;
                            case TokenType.SEP_DateOrOffset:
                                if ((dateParsingStates[(int)dps][(int)DTT.NumDatesep] == DS.ERROR) && (dateParsingStates[(int)dps][(int)DTT.NumSpace] > DS.ERROR))
                                {
                                    str.Index = indexBeforeSeparator;
                                    str.m_current = charBeforeSeparator;
                                    dtok.dtt = DTT.NumSpace;
                                }
                                else
                                {
                                    dtok.dtt = DTT.NumDatesep;
                                }

                                break;
                            default:
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                LexTraceExit("0110", dps);
                                return false;
                        }
                    }

                    break;
                case TokenType.DayOfWeekToken:
                    if (raw.dayOfWeek == -1)
                    {
                        raw.dayOfWeek = tokenValue;
                        dtok.dtt = DTT.DayOfWeek;
                    }
                    else
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0120 (DayOfWeek seen more than 1x)", dps);
                        return false;
                    }

                    break;
                case TokenType.MonthToken:
                    if (raw.month == -1)
                    {
                        switch (sep = str.GetSeparatorToken(dtfi, out indexBeforeSeparator, out charBeforeSeparator))
                        {
                            case TokenType.SEP_End:
                                dtok.dtt = DTT.MonthEnd;
                                break;
                            case TokenType.SEP_Space:
                                dtok.dtt = DTT.MonthSpace;
                                break;
                            case TokenType.SEP_Date:
                                dtok.dtt = DTT.MonthDatesep;
                                break;
                            case TokenType.SEP_Time:
                                if (!raw.hasSameDateAndTimeSeparators)
                                {
                                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                    LexTraceExit("0130 (Invalid separator after month name)", dps);
                                    return false;
                                }

                                dtok.dtt = DTT.MonthDatesep;
                                break;
                            case TokenType.SEP_DateOrOffset:
                                if ((dateParsingStates[(int)dps][(int)DTT.MonthDatesep] == DS.ERROR) && (dateParsingStates[(int)dps][(int)DTT.MonthSpace] > DS.ERROR))
                                {
                                    str.Index = indexBeforeSeparator;
                                    str.m_current = charBeforeSeparator;
                                    dtok.dtt = DTT.MonthSpace;
                                }
                                else
                                {
                                    dtok.dtt = DTT.MonthDatesep;
                                }

                                break;
                            default:
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                LexTraceExit("0130 (Invalid separator after month name)", dps);
                                return false;
                        }

                        raw.month = tokenValue;
                    }
                    else
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0140 (MonthToken seen more than 1x)", dps);
                        return false;
                    }

                    break;
                case TokenType.EraToken:
                    if (result.era != -1)
                    {
                        result.era = tokenValue;
                        dtok.dtt = DTT.Era;
                    }
                    else
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0150 (EraToken seen when result.era already set)", dps);
                        return false;
                    }

                    break;
                case TokenType.JapaneseEraToken:
                    result.calendar = JapaneseCalendar.GetDefaultInstance();
                    dtfi = DateTimeFormatInfo.GetJapaneseCalendarDTFI();
                    if (result.era != -1)
                    {
                        result.era = tokenValue;
                        dtok.dtt = DTT.Era;
                    }
                    else
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0160 (JapaneseEraToken seen when result.era already set)", dps);
                        return false;
                    }

                    break;
                case TokenType.TEraToken:
                    result.calendar = TaiwanCalendar.GetDefaultInstance();
                    dtfi = DateTimeFormatInfo.GetTaiwanCalendarDTFI();
                    if (result.era != -1)
                    {
                        result.era = tokenValue;
                        dtok.dtt = DTT.Era;
                    }
                    else
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0170 (TEraToken seen when result.era already set)", dps);
                        return false;
                    }

                    break;
                case TokenType.TimeZoneToken:
                    if ((result.flags & ParseFlags.TimeZoneUsed) != 0)
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0180 (seen GMT or Z more than 1x)", dps);
                        return false;
                    }

                    dtok.dtt = DTT.TimeZone;
                    result.flags |= ParseFlags.TimeZoneUsed;
                    result.timeZoneOffset = new TimeSpan(0);
                    result.flags |= ParseFlags.TimeZoneUtc;
                    break;
                case TokenType.EndOfString:
                    dtok.dtt = DTT.End;
                    break;
                case TokenType.DateWordToken:
                case TokenType.IgnorableSymbol:
                    break;
                case TokenType.Am:
                case TokenType.Pm:
                    if (raw.timeMark == TM.NotSet)
                    {
                        raw.timeMark = (TM)tokenValue;
                    }
                    else
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        LexTraceExit("0190 (AM/PM timeMark already set)", dps);
                        return false;
                    }

                    break;
                case TokenType.UnknownToken:
                    if (Char.IsLetter(str.m_current))
                    {
                        result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_UnknowDateTimeWord", str.Index);
                        LexTraceExit("0200", dps);
                        return (false);
                    }

                    if ((str.m_current == '-' || str.m_current == '+') && ((result.flags & ParseFlags.TimeZoneUsed) == 0))
                    {
                        Int32 originalIndex = str.Index;
                        if (ParseTimeZone(ref str, ref result.timeZoneOffset))
                        {
                            result.flags |= ParseFlags.TimeZoneUsed;
                            LexTraceExit("0220 (success)", dps);
                            return true;
                        }
                        else
                        {
                            str.Index = originalIndex;
                        }
                    }

                    if (VerifyValidPunctuation(ref str))
                    {
                        LexTraceExit("0230 (success)", dps);
                        return true;
                    }

                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                    LexTraceExit("0240", dps);
                    return false;
            }

            LexTraceExit("0250 (success)", dps);
            return true;
        }

        private static Boolean VerifyValidPunctuation(ref __DTString str)
        {
            Char ch = str.Value[str.Index];
            if (ch == '#')
            {
                bool foundStart = false;
                bool foundEnd = false;
                for (int i = 0; i < str.len; i++)
                {
                    ch = str.Value[i];
                    if (ch == '#')
                    {
                        if (foundStart)
                        {
                            if (foundEnd)
                            {
                                return false;
                            }
                            else
                            {
                                foundEnd = true;
                            }
                        }
                        else
                        {
                            foundStart = true;
                        }
                    }
                    else if (ch == '\0')
                    {
                        if (!foundEnd)
                        {
                            return false;
                        }
                    }
                    else if ((!Char.IsWhiteSpace(ch)))
                    {
                        if (!foundStart || foundEnd)
                        {
                            return false;
                        }
                    }
                }

                if (!foundEnd)
                {
                    return false;
                }

                str.GetNext();
                return true;
            }
            else if (ch == '\0')
            {
                for (int i = str.Index; i < str.len; i++)
                {
                    if (str.Value[i] != '\0')
                    {
                        return false;
                    }
                }

                str.Index = str.len;
                return true;
            }

            return false;
        }

        private const int ORDER_YMD = 0;
        private const int ORDER_MDY = 1;
        private const int ORDER_DMY = 2;
        private const int ORDER_YDM = 3;
        private const int ORDER_YM = 4;
        private const int ORDER_MY = 5;
        private const int ORDER_MD = 6;
        private const int ORDER_DM = 7;
        private static Boolean GetYearMonthDayOrder(String datePattern, DateTimeFormatInfo dtfi, out int order)
        {
            int yearOrder = -1;
            int monthOrder = -1;
            int dayOrder = -1;
            int orderCount = 0;
            bool inQuote = false;
            for (int i = 0; i < datePattern.Length && orderCount < 3; i++)
            {
                char ch = datePattern[i];
                if (ch == '\\' || ch == '%')
                {
                    i++;
                    continue;
                }

                if (ch == '\'' || ch == '"')
                {
                    inQuote = !inQuote;
                }

                if (!inQuote)
                {
                    if (ch == 'y')
                    {
                        yearOrder = orderCount++;
                        for (; i + 1 < datePattern.Length && datePattern[i + 1] == 'y'; i++)
                        {
                        }
                    }
                    else if (ch == 'M')
                    {
                        monthOrder = orderCount++;
                        for (; i + 1 < datePattern.Length && datePattern[i + 1] == 'M'; i++)
                        {
                        }
                    }
                    else if (ch == 'd')
                    {
                        int patternCount = 1;
                        for (; i + 1 < datePattern.Length && datePattern[i + 1] == 'd'; i++)
                        {
                            patternCount++;
                        }

                        if (patternCount <= 2)
                        {
                            dayOrder = orderCount++;
                        }
                    }
                }
            }

            if (yearOrder == 0 && monthOrder == 1 && dayOrder == 2)
            {
                order = ORDER_YMD;
                return true;
            }

            if (monthOrder == 0 && dayOrder == 1 && yearOrder == 2)
            {
                order = ORDER_MDY;
                return true;
            }

            if (dayOrder == 0 && monthOrder == 1 && yearOrder == 2)
            {
                order = ORDER_DMY;
                return true;
            }

            if (yearOrder == 0 && dayOrder == 1 && monthOrder == 2)
            {
                order = ORDER_YDM;
                return true;
            }

            order = -1;
            return false;
        }

        private static Boolean GetYearMonthOrder(String pattern, DateTimeFormatInfo dtfi, out int order)
        {
            int yearOrder = -1;
            int monthOrder = -1;
            int orderCount = 0;
            bool inQuote = false;
            for (int i = 0; i < pattern.Length && orderCount < 2; i++)
            {
                char ch = pattern[i];
                if (ch == '\\' || ch == '%')
                {
                    i++;
                    continue;
                }

                if (ch == '\'' || ch == '"')
                {
                    inQuote = !inQuote;
                }

                if (!inQuote)
                {
                    if (ch == 'y')
                    {
                        yearOrder = orderCount++;
                        for (; i + 1 < pattern.Length && pattern[i + 1] == 'y'; i++)
                        {
                        }
                    }
                    else if (ch == 'M')
                    {
                        monthOrder = orderCount++;
                        for (; i + 1 < pattern.Length && pattern[i + 1] == 'M'; i++)
                        {
                        }
                    }
                }
            }

            if (yearOrder == 0 && monthOrder == 1)
            {
                order = ORDER_YM;
                return true;
            }

            if (monthOrder == 0 && yearOrder == 1)
            {
                order = ORDER_MY;
                return true;
            }

            order = -1;
            return false;
        }

        private static Boolean GetMonthDayOrder(String pattern, DateTimeFormatInfo dtfi, out int order)
        {
            int monthOrder = -1;
            int dayOrder = -1;
            int orderCount = 0;
            bool inQuote = false;
            for (int i = 0; i < pattern.Length && orderCount < 2; i++)
            {
                char ch = pattern[i];
                if (ch == '\\' || ch == '%')
                {
                    i++;
                    continue;
                }

                if (ch == '\'' || ch == '"')
                {
                    inQuote = !inQuote;
                }

                if (!inQuote)
                {
                    if (ch == 'd')
                    {
                        int patternCount = 1;
                        for (; i + 1 < pattern.Length && pattern[i + 1] == 'd'; i++)
                        {
                            patternCount++;
                        }

                        if (patternCount <= 2)
                        {
                            dayOrder = orderCount++;
                        }
                    }
                    else if (ch == 'M')
                    {
                        monthOrder = orderCount++;
                        for (; i + 1 < pattern.Length && pattern[i + 1] == 'M'; i++)
                        {
                        }
                    }
                }
            }

            if (monthOrder == 0 && dayOrder == 1)
            {
                order = ORDER_MD;
                return true;
            }

            if (dayOrder == 0 && monthOrder == 1)
            {
                order = ORDER_DM;
                return true;
            }

            order = -1;
            return false;
        }

        private static bool TryAdjustYear(ref DateTimeResult result, int year, out int adjustedYear)
        {
            if (year < 100)
            {
                try
                {
                    year = result.calendar.ToFourDigitYear(year);
                }
                catch (ArgumentOutOfRangeException)
                {
                    adjustedYear = -1;
                    return false;
                }
            }

            adjustedYear = year;
            return true;
        }

        private static bool SetDateYMD(ref DateTimeResult result, int year, int month, int day)
        {
            if (result.calendar.IsValidDay(year, month, day, result.era))
            {
                result.SetDate(year, month, day);
                return (true);
            }

            return (false);
        }

        private static bool SetDateMDY(ref DateTimeResult result, int month, int day, int year)
        {
            return (SetDateYMD(ref result, year, month, day));
        }

        private static bool SetDateDMY(ref DateTimeResult result, int day, int month, int year)
        {
            return (SetDateYMD(ref result, year, month, day));
        }

        private static bool SetDateYDM(ref DateTimeResult result, int year, int day, int month)
        {
            return (SetDateYMD(ref result, year, month, day));
        }

        private static void GetDefaultYear(ref DateTimeResult result, ref DateTimeStyles styles)
        {
            result.Year = result.calendar.GetYear(GetDateTimeNow(ref result, ref styles));
            result.flags |= ParseFlags.YearDefault;
        }

        private static Boolean GetDayOfNN(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            int n1 = raw.GetNumber(0);
            int n2 = raw.GetNumber(1);
            GetDefaultYear(ref result, ref styles);
            int order;
            if (!GetMonthDayOrder(dtfi.MonthDayPattern, dtfi, out order))
            {
                result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
                return false;
            }

            if (order == ORDER_MD)
            {
                if (SetDateYMD(ref result, result.Year, n1, n2))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }
            else
            {
                if (SetDateYMD(ref result, result.Year, n2, n1))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static Boolean GetDayOfNNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            int n1 = raw.GetNumber(0);
            int n2 = raw.GetNumber(1);
            ;
            int n3 = raw.GetNumber(2);
            int order;
            if (!GetYearMonthDayOrder(dtfi.ShortDatePattern, dtfi, out order))
            {
                result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
                return false;
            }

            int year;
            if (order == ORDER_YMD)
            {
                if (TryAdjustYear(ref result, n1, out year) && SetDateYMD(ref result, year, n2, n3))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }
            else if (order == ORDER_MDY)
            {
                if (TryAdjustYear(ref result, n3, out year) && SetDateMDY(ref result, n1, n2, year))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }
            else if (order == ORDER_DMY)
            {
                if (TryAdjustYear(ref result, n3, out year) && SetDateDMY(ref result, n1, n2, year))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }
            else if (order == ORDER_YDM)
            {
                if (TryAdjustYear(ref result, n1, out year) && SetDateYDM(ref result, year, n2, n3))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static Boolean GetDayOfMN(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            int monthDayOrder;
            if (!GetMonthDayOrder(dtfi.MonthDayPattern, dtfi, out monthDayOrder))
            {
                result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
                return false;
            }

            if (monthDayOrder == ORDER_DM)
            {
                int yearMonthOrder;
                if (!GetYearMonthOrder(dtfi.YearMonthPattern, dtfi, out yearMonthOrder))
                {
                    result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.YearMonthPattern);
                    return false;
                }

                if (yearMonthOrder == ORDER_MY)
                {
                    int year;
                    if (!TryAdjustYear(ref result, raw.GetNumber(0), out year) || !SetDateYMD(ref result, year, raw.month, 1))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    return true;
                }
            }

            GetDefaultYear(ref result, ref styles);
            if (!SetDateYMD(ref result, result.Year, raw.month, raw.GetNumber(0)))
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            return true;
        }

        private static Boolean GetHebrewDayOfNM(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            int monthDayOrder;
            if (!GetMonthDayOrder(dtfi.MonthDayPattern, dtfi, out monthDayOrder))
            {
                result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
                return false;
            }

            result.Month = raw.month;
            if (monthDayOrder == ORDER_DM || monthDayOrder == ORDER_MD)
            {
                if (result.calendar.IsValidDay(result.Year, result.Month, raw.GetNumber(0), result.era))
                {
                    result.Day = raw.GetNumber(0);
                    return true;
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static Boolean GetDayOfNM(ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            int monthDayOrder;
            if (!GetMonthDayOrder(dtfi.MonthDayPattern, dtfi, out monthDayOrder))
            {
                result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.MonthDayPattern);
                return false;
            }

            if (monthDayOrder == ORDER_MD)
            {
                int yearMonthOrder;
                if (!GetYearMonthOrder(dtfi.YearMonthPattern, dtfi, out yearMonthOrder))
                {
                    result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.YearMonthPattern);
                    return false;
                }

                if (yearMonthOrder == ORDER_YM)
                {
                    int year;
                    if (!TryAdjustYear(ref result, raw.GetNumber(0), out year) || !SetDateYMD(ref result, year, raw.month, 1))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    return true;
                }
            }

            GetDefaultYear(ref result, ref styles);
            if (!SetDateYMD(ref result, result.Year, raw.month, raw.GetNumber(0)))
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            return true;
        }

        private static Boolean GetDayOfMNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            int n1 = raw.GetNumber(0);
            int n2 = raw.GetNumber(1);
            int order;
            if (!GetYearMonthDayOrder(dtfi.ShortDatePattern, dtfi, out order))
            {
                result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
                return false;
            }

            int year;
            if (order == ORDER_MDY)
            {
                if (TryAdjustYear(ref result, n2, out year) && result.calendar.IsValidDay(year, raw.month, n1, result.era))
                {
                    result.SetDate(year, raw.month, n1);
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
                else if (TryAdjustYear(ref result, n1, out year) && result.calendar.IsValidDay(year, raw.month, n2, result.era))
                {
                    result.SetDate(year, raw.month, n2);
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }
            else if (order == ORDER_YMD)
            {
                if (TryAdjustYear(ref result, n1, out year) && result.calendar.IsValidDay(year, raw.month, n2, result.era))
                {
                    result.SetDate(year, raw.month, n2);
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
                else if (TryAdjustYear(ref result, n2, out year) && result.calendar.IsValidDay(year, raw.month, n1, result.era))
                {
                    result.SetDate(year, raw.month, n1);
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }
            else if (order == ORDER_DMY)
            {
                if (TryAdjustYear(ref result, n2, out year) && result.calendar.IsValidDay(year, raw.month, n1, result.era))
                {
                    result.SetDate(year, raw.month, n1);
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
                else if (TryAdjustYear(ref result, n1, out year) && result.calendar.IsValidDay(year, raw.month, n2, result.era))
                {
                    result.SetDate(year, raw.month, n2);
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static Boolean GetDayOfYNN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            int n1 = raw.GetNumber(0);
            int n2 = raw.GetNumber(1);
            String pattern = dtfi.ShortDatePattern;
            int order;
            if (GetYearMonthDayOrder(pattern, dtfi, out order) && order == ORDER_YDM)
            {
                if (SetDateYMD(ref result, raw.year, n2, n1))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }
            else
            {
                if (SetDateYMD(ref result, raw.year, n1, n2))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static Boolean GetDayOfNNY(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            int n1 = raw.GetNumber(0);
            int n2 = raw.GetNumber(1);
            int order;
            if (!GetYearMonthDayOrder(dtfi.ShortDatePattern, dtfi, out order))
            {
                result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
                return false;
            }

            if (order == ORDER_MDY || order == ORDER_YMD)
            {
                if (SetDateYMD(ref result, raw.year, n1, n2))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }
            else
            {
                if (SetDateYMD(ref result, raw.year, n2, n1))
                {
                    result.flags |= ParseFlags.HaveDate;
                    return true;
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static Boolean GetDayOfYMN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (SetDateYMD(ref result, raw.year, raw.month, raw.GetNumber(0)))
            {
                result.flags |= ParseFlags.HaveDate;
                return true;
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static Boolean GetDayOfYN(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (SetDateYMD(ref result, raw.year, raw.GetNumber(0), 1))
            {
                result.flags |= ParseFlags.HaveDate;
                return true;
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static Boolean GetDayOfYM(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveDate) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (SetDateYMD(ref result, raw.year, raw.month, 1))
            {
                result.flags |= ParseFlags.HaveDate;
                return true;
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static void AdjustTimeMark(DateTimeFormatInfo dtfi, ref DateTimeRawInfo raw)
        {
            if (raw.timeMark == TM.NotSet)
            {
                if (dtfi.AMDesignator != null && dtfi.PMDesignator != null)
                {
                    if (dtfi.AMDesignator.Length == 0 && dtfi.PMDesignator.Length != 0)
                    {
                        raw.timeMark = TM.AM;
                    }

                    if (dtfi.PMDesignator.Length == 0 && dtfi.AMDesignator.Length != 0)
                    {
                        raw.timeMark = TM.PM;
                    }
                }
            }
        }

        private static Boolean AdjustHour(ref int hour, TM timeMark)
        {
            if (timeMark != TM.NotSet)
            {
                if (timeMark == TM.AM)
                {
                    if (hour < 0 || hour > 12)
                    {
                        return false;
                    }

                    hour = (hour == 12) ? 0 : hour;
                }
                else
                {
                    if (hour < 0 || hour > 23)
                    {
                        return false;
                    }

                    if (hour < 12)
                    {
                        hour += 12;
                    }
                }
            }

            return true;
        }

        private static Boolean GetTimeOfN(DateTimeFormatInfo dtfi, ref DateTimeResult result, ref DateTimeRawInfo raw)
        {
            if ((result.flags & ParseFlags.HaveTime) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (raw.timeMark == TM.NotSet)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            result.Hour = raw.GetNumber(0);
            result.flags |= ParseFlags.HaveTime;
            return true;
        }

        private static Boolean GetTimeOfNN(DateTimeFormatInfo dtfi, ref DateTimeResult result, ref DateTimeRawInfo raw)
        {
            Contract.Assert(raw.numCount >= 2, "raw.numCount >= 2");
            if ((result.flags & ParseFlags.HaveTime) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            result.Hour = raw.GetNumber(0);
            result.Minute = raw.GetNumber(1);
            result.flags |= ParseFlags.HaveTime;
            return true;
        }

        private static Boolean GetTimeOfNNN(DateTimeFormatInfo dtfi, ref DateTimeResult result, ref DateTimeRawInfo raw)
        {
            if ((result.flags & ParseFlags.HaveTime) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            Contract.Assert(raw.numCount >= 3, "raw.numCount >= 3");
            result.Hour = raw.GetNumber(0);
            result.Minute = raw.GetNumber(1);
            result.Second = raw.GetNumber(2);
            result.flags |= ParseFlags.HaveTime;
            return true;
        }

        private static Boolean GetDateOfDSN(ref DateTimeResult result, ref DateTimeRawInfo raw)
        {
            if (raw.numCount != 1 || result.Day != -1)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            result.Day = raw.GetNumber(0);
            return true;
        }

        private static Boolean GetDateOfNDS(ref DateTimeResult result, ref DateTimeRawInfo raw)
        {
            if (result.Month == -1)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (result.Year != -1)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (!TryAdjustYear(ref result, raw.GetNumber(0), out result.Year))
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            result.Day = 1;
            return true;
        }

        private static Boolean GetDateOfNNDS(ref DateTimeResult result, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            if ((result.flags & ParseFlags.HaveYear) != 0)
            {
                if (((result.flags & ParseFlags.HaveMonth) == 0) && ((result.flags & ParseFlags.HaveDay) == 0))
                {
                    if (TryAdjustYear(ref result, raw.year, out result.Year) && SetDateYMD(ref result, result.Year, raw.GetNumber(0), raw.GetNumber(1)))
                    {
                        return true;
                    }
                }
            }
            else if ((result.flags & ParseFlags.HaveMonth) != 0)
            {
                if (((result.flags & ParseFlags.HaveYear) == 0) && ((result.flags & ParseFlags.HaveDay) == 0))
                {
                    int order;
                    if (!GetYearMonthDayOrder(dtfi.ShortDatePattern, dtfi, out order))
                    {
                        result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadDatePattern", dtfi.ShortDatePattern);
                        return false;
                    }

                    int year;
                    if (order == ORDER_YMD)
                    {
                        if (TryAdjustYear(ref result, raw.GetNumber(0), out year) && SetDateYMD(ref result, year, result.Month, raw.GetNumber(1)))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (TryAdjustYear(ref result, raw.GetNumber(1), out year) && SetDateYMD(ref result, year, result.Month, raw.GetNumber(0)))
                        {
                            return true;
                        }
                    }
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
            return false;
        }

        private static bool ProcessDateTimeSuffix(ref DateTimeResult result, ref DateTimeRawInfo raw, ref DateTimeToken dtok)
        {
            switch (dtok.suffix)
            {
                case TokenType.SEP_YearSuff:
                    if ((result.flags & ParseFlags.HaveYear) != 0)
                    {
                        return false;
                    }

                    result.flags |= ParseFlags.HaveYear;
                    result.Year = raw.year = dtok.num;
                    break;
                case TokenType.SEP_MonthSuff:
                    if ((result.flags & ParseFlags.HaveMonth) != 0)
                    {
                        return false;
                    }

                    result.flags |= ParseFlags.HaveMonth;
                    result.Month = raw.month = dtok.num;
                    break;
                case TokenType.SEP_DaySuff:
                    if ((result.flags & ParseFlags.HaveDay) != 0)
                    {
                        return false;
                    }

                    result.flags |= ParseFlags.HaveDay;
                    result.Day = dtok.num;
                    break;
                case TokenType.SEP_HourSuff:
                    if ((result.flags & ParseFlags.HaveHour) != 0)
                    {
                        return false;
                    }

                    result.flags |= ParseFlags.HaveHour;
                    result.Hour = dtok.num;
                    break;
                case TokenType.SEP_MinuteSuff:
                    if ((result.flags & ParseFlags.HaveMinute) != 0)
                    {
                        return false;
                    }

                    result.flags |= ParseFlags.HaveMinute;
                    result.Minute = dtok.num;
                    break;
                case TokenType.SEP_SecondSuff:
                    if ((result.flags & ParseFlags.HaveSecond) != 0)
                    {
                        return false;
                    }

                    result.flags |= ParseFlags.HaveSecond;
                    result.Second = dtok.num;
                    break;
            }

            return true;
        }

        internal static Boolean ProcessHebrewTerminalState(DS dps, ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            switch (dps)
            {
                case DS.DX_MNN:
                    raw.year = raw.GetNumber(1);
                    if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, true))
                    {
                        result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
                        return false;
                    }

                    if (!GetDayOfMNN(ref result, ref raw, dtfi))
                    {
                        return false;
                    }

                    break;
                case DS.DX_YMN:
                    if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, true))
                    {
                        result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
                        return false;
                    }

                    if (!GetDayOfYMN(ref result, ref raw, dtfi))
                    {
                        return false;
                    }

                    break;
                case DS.DX_NM:
                case DS.DX_MN:
                    GetDefaultYear(ref result, ref styles);
                    if (!dtfi.YearMonthAdjustment(ref result.Year, ref raw.month, true))
                    {
                        result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
                        return false;
                    }

                    if (!GetHebrewDayOfNM(ref result, ref raw, dtfi))
                    {
                        return false;
                    }

                    break;
                case DS.DX_YM:
                    if (!dtfi.YearMonthAdjustment(ref raw.year, ref raw.month, true))
                    {
                        result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
                        return false;
                    }

                    if (!GetDayOfYM(ref result, ref raw, dtfi))
                    {
                        return false;
                    }

                    break;
                case DS.TX_N:
                    if (!GetTimeOfN(dtfi, ref result, ref raw))
                    {
                        return false;
                    }

                    break;
                case DS.TX_NN:
                    if (!GetTimeOfNN(dtfi, ref result, ref raw))
                    {
                        return false;
                    }

                    break;
                case DS.TX_NNN:
                    if (!GetTimeOfNNN(dtfi, ref result, ref raw))
                    {
                        return false;
                    }

                    break;
                default:
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                    return false;
            }

            if (dps > DS.ERROR)
            {
                raw.numCount = 0;
            }

            return true;
        }

        internal static Boolean ProcessTerminaltState(DS dps, ref DateTimeResult result, ref DateTimeStyles styles, ref DateTimeRawInfo raw, DateTimeFormatInfo dtfi)
        {
            bool passed = true;
            switch (dps)
            {
                case DS.DX_NN:
                    passed = GetDayOfNN(ref result, ref styles, ref raw, dtfi);
                    break;
                case DS.DX_NNN:
                    passed = GetDayOfNNN(ref result, ref raw, dtfi);
                    break;
                case DS.DX_MN:
                    passed = GetDayOfMN(ref result, ref styles, ref raw, dtfi);
                    break;
                case DS.DX_NM:
                    passed = GetDayOfNM(ref result, ref styles, ref raw, dtfi);
                    break;
                case DS.DX_MNN:
                    passed = GetDayOfMNN(ref result, ref raw, dtfi);
                    break;
                case DS.DX_DS:
                    passed = true;
                    break;
                case DS.DX_YNN:
                    passed = GetDayOfYNN(ref result, ref raw, dtfi);
                    break;
                case DS.DX_NNY:
                    passed = GetDayOfNNY(ref result, ref raw, dtfi);
                    break;
                case DS.DX_YMN:
                    passed = GetDayOfYMN(ref result, ref raw, dtfi);
                    break;
                case DS.DX_YN:
                    passed = GetDayOfYN(ref result, ref raw, dtfi);
                    break;
                case DS.DX_YM:
                    passed = GetDayOfYM(ref result, ref raw, dtfi);
                    break;
                case DS.TX_N:
                    passed = GetTimeOfN(dtfi, ref result, ref raw);
                    break;
                case DS.TX_NN:
                    passed = GetTimeOfNN(dtfi, ref result, ref raw);
                    break;
                case DS.TX_NNN:
                    passed = GetTimeOfNNN(dtfi, ref result, ref raw);
                    break;
                case DS.TX_TS:
                    passed = true;
                    break;
                case DS.DX_DSN:
                    passed = GetDateOfDSN(ref result, ref raw);
                    break;
                case DS.DX_NDS:
                    passed = GetDateOfNDS(ref result, ref raw);
                    break;
                case DS.DX_NNDS:
                    passed = GetDateOfNNDS(ref result, ref raw, dtfi);
                    break;
            }

            PTSTraceExit(dps, passed);
            if (!passed)
            {
                return false;
            }

            if (dps > DS.ERROR)
            {
                raw.numCount = 0;
            }

            return true;
        }

        internal static DateTime Parse(String s, DateTimeFormatInfo dtfi, DateTimeStyles styles)
        {
            DateTimeResult result = new DateTimeResult();
            result.Init();
            if (TryParse(s, dtfi, styles, ref result))
            {
                return result.parsedDate;
            }
            else
            {
                throw GetDateTimeParseException(ref result);
            }
        }

        internal static DateTime Parse(String s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out TimeSpan offset)
        {
            DateTimeResult result = new DateTimeResult();
            result.Init();
            result.flags |= ParseFlags.CaptureOffset;
            if (TryParse(s, dtfi, styles, ref result))
            {
                offset = result.timeZoneOffset;
                return result.parsedDate;
            }
            else
            {
                throw GetDateTimeParseException(ref result);
            }
        }

        internal static bool TryParse(String s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out DateTime result)
        {
            result = DateTime.MinValue;
            DateTimeResult resultData = new DateTimeResult();
            resultData.Init();
            if (TryParse(s, dtfi, styles, ref resultData))
            {
                result = resultData.parsedDate;
                return true;
            }

            return false;
        }

        internal static bool TryParse(String s, DateTimeFormatInfo dtfi, DateTimeStyles styles, out DateTime result, out TimeSpan offset)
        {
            result = DateTime.MinValue;
            offset = TimeSpan.Zero;
            DateTimeResult parseResult = new DateTimeResult();
            parseResult.Init();
            parseResult.flags |= ParseFlags.CaptureOffset;
            if (TryParse(s, dtfi, styles, ref parseResult))
            {
                result = parseResult.parsedDate;
                offset = parseResult.timeZoneOffset;
                return true;
            }

            return false;
        }

        internal static bool TryParse(String s, DateTimeFormatInfo dtfi, DateTimeStyles styles, ref DateTimeResult result)
        {
            if (s == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "s");
                return false;
            }

            if (s.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            Contract.Assert(dtfi != null, "dtfi == null");
            DTFITrace(dtfi);
            DateTime time;
            DS dps = DS.BEGIN;
            bool reachTerminalState = false;
            DateTimeToken dtok = new DateTimeToken();
            dtok.suffix = TokenType.SEP_Unk;
            DateTimeRawInfo raw = new DateTimeRawInfo();
            unsafe
            {
                Int32*numberPointer = stackalloc Int32[3];
                raw.Init(numberPointer);
            }

            raw.hasSameDateAndTimeSeparators = dtfi.DateSeparator.Equals(dtfi.TimeSeparator, StringComparison.Ordinal);
            result.calendar = dtfi.Calendar;
            result.era = Calendar.CurrentEra;
            __DTString str = new __DTString(s, dtfi);
            str.GetNext();
            do
            {
                if (!Lex(dps, ref str, ref dtok, ref raw, ref result, ref dtfi, styles))
                {
                    TPTraceExit("0000", dps);
                    return false;
                }

                if (dtok.dtt != DTT.Unk)
                {
                    if (dtok.suffix != TokenType.SEP_Unk)
                    {
                        if (!ProcessDateTimeSuffix(ref result, ref raw, ref dtok))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            TPTraceExit("0010", dps);
                            return false;
                        }

                        dtok.suffix = TokenType.SEP_Unk;
                    }

                    if (dtok.dtt == DTT.NumLocalTimeMark)
                    {
                        if (dps == DS.D_YNd || dps == DS.D_YN)
                        {
                            TPTraceExit("0020", dps);
                            return (ParseISO8601(ref raw, ref str, styles, ref result));
                        }
                        else
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            TPTraceExit("0030", dps);
                            return false;
                        }
                    }

                    if (raw.hasSameDateAndTimeSeparators)
                    {
                        if (dtok.dtt == DTT.YearEnd || dtok.dtt == DTT.YearSpace || dtok.dtt == DTT.YearDateSep)
                        {
                            if (dps == DS.T_Nt)
                            {
                                dps = DS.D_Nd;
                            }

                            if (dps == DS.T_NNt)
                            {
                                dps = DS.D_NNd;
                            }
                        }

                        bool atEnd = str.AtEnd();
                        if (dateParsingStates[(int)dps][(int)dtok.dtt] == DS.ERROR || atEnd)
                        {
                            switch (dtok.dtt)
                            {
                                case DTT.YearDateSep:
                                    dtok.dtt = atEnd ? DTT.YearEnd : DTT.YearSpace;
                                    break;
                                case DTT.NumDatesep:
                                    dtok.dtt = atEnd ? DTT.NumEnd : DTT.NumSpace;
                                    break;
                                case DTT.NumTimesep:
                                    dtok.dtt = atEnd ? DTT.NumEnd : DTT.NumSpace;
                                    break;
                                case DTT.MonthDatesep:
                                    dtok.dtt = atEnd ? DTT.MonthEnd : DTT.MonthSpace;
                                    break;
                            }
                        }
                    }

                    dps = dateParsingStates[(int)dps][(int)dtok.dtt];
                    if (dps == DS.ERROR)
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        TPTraceExit("0040 (invalid state transition)", dps);
                        return false;
                    }
                    else if (dps > DS.ERROR)
                    {
                        if ((dtfi.FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0)
                        {
                            if (!ProcessHebrewTerminalState(dps, ref result, ref styles, ref raw, dtfi))
                            {
                                TPTraceExit("0050 (ProcessHebrewTerminalState)", dps);
                                return false;
                            }
                        }
                        else
                        {
                            if (!ProcessTerminaltState(dps, ref result, ref styles, ref raw, dtfi))
                            {
                                TPTraceExit("0060 (ProcessTerminaltState)", dps);
                                return false;
                            }
                        }

                        reachTerminalState = true;
                        dps = DS.BEGIN;
                    }
                }
            }
            while (dtok.dtt != DTT.End && dtok.dtt != DTT.NumEnd && dtok.dtt != DTT.MonthEnd);
            if (!reachTerminalState)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                TPTraceExit("0070 (did not reach terminal state)", dps);
                return false;
            }

            AdjustTimeMark(dtfi, ref raw);
            if (!AdjustHour(ref result.Hour, raw.timeMark))
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                TPTraceExit("0080 (AdjustHour)", dps);
                return false;
            }

            bool bTimeOnly = (result.Year == -1 && result.Month == -1 && result.Day == -1);
            if (!CheckDefaultDateTime(ref result, ref result.calendar, styles))
            {
                TPTraceExit("0090 (failed to fill in missing year/month/day defaults)", dps);
                return false;
            }

            if (!result.calendar.TryToDateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, 0, result.era, out time))
            {
                result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
                TPTraceExit("0100 (result.calendar.TryToDateTime)", dps);
                return false;
            }

            if (raw.fraction > 0)
            {
                time = time.AddTicks((long)Math.Round(raw.fraction * Calendar.TicksPerSecond));
            }

            if (raw.dayOfWeek != -1)
            {
                if (raw.dayOfWeek != (int)result.calendar.GetDayOfWeek(time))
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDayOfWeek", null);
                    TPTraceExit("0110 (dayOfWeek check)", dps);
                    return false;
                }
            }

            result.parsedDate = time;
            if (!DetermineTimeZoneAdjustments(ref result, styles, bTimeOnly))
            {
                TPTraceExit("0120 (DetermineTimeZoneAdjustments)", dps);
                return false;
            }

            TPTraceExit("0130 (success)", dps);
            return true;
        }

        private static Boolean DetermineTimeZoneAdjustments(ref DateTimeResult result, DateTimeStyles styles, Boolean bTimeOnly)
        {
            if ((result.flags & ParseFlags.CaptureOffset) != 0)
            {
                return DateTimeOffsetTimeZonePostProcessing(ref result, styles);
            }
            else
            {
                Int64 offsetTicks = result.timeZoneOffset.Ticks;
                if (offsetTicks < DateTimeOffset.MinOffset || offsetTicks > DateTimeOffset.MaxOffset)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_OffsetOutOfRange", null);
                    return false;
                }
            }

            if ((result.flags & ParseFlags.TimeZoneUsed) == 0)
            {
                if ((styles & DateTimeStyles.AssumeLocal) != 0)
                {
                    if ((styles & DateTimeStyles.AdjustToUniversal) != 0)
                    {
                        result.flags |= ParseFlags.TimeZoneUsed;
                        result.timeZoneOffset = TimeZoneInfo.GetLocalUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                    }
                    else
                    {
                        result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Local);
                        return true;
                    }
                }
                else if ((styles & DateTimeStyles.AssumeUniversal) != 0)
                {
                    if ((styles & DateTimeStyles.AdjustToUniversal) != 0)
                    {
                        result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Utc);
                        return true;
                    }
                    else
                    {
                        result.flags |= ParseFlags.TimeZoneUsed;
                        result.timeZoneOffset = TimeSpan.Zero;
                    }
                }
                else
                {
                    Contract.Assert(result.parsedDate.Kind == DateTimeKind.Unspecified, "result.parsedDate.Kind == DateTimeKind.Unspecified");
                    return true;
                }
            }

            if (((styles & DateTimeStyles.RoundtripKind) != 0) && ((result.flags & ParseFlags.TimeZoneUtc) != 0))
            {
                result.parsedDate = DateTime.SpecifyKind(result.parsedDate, DateTimeKind.Utc);
                return true;
            }

            if ((styles & DateTimeStyles.AdjustToUniversal) != 0)
            {
                return (AdjustTimeZoneToUniversal(ref result));
            }

            return (AdjustTimeZoneToLocal(ref result, bTimeOnly));
        }

        private static Boolean DateTimeOffsetTimeZonePostProcessing(ref DateTimeResult result, DateTimeStyles styles)
        {
            if ((result.flags & ParseFlags.TimeZoneUsed) == 0)
            {
                if ((styles & DateTimeStyles.AssumeUniversal) != 0)
                {
                    result.timeZoneOffset = TimeSpan.Zero;
                }
                else
                {
                    result.timeZoneOffset = TimeZoneInfo.GetLocalUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                }
            }

            Int64 offsetTicks = result.timeZoneOffset.Ticks;
            Int64 utcTicks = result.parsedDate.Ticks - offsetTicks;
            if (utcTicks < DateTime.MinTicks || utcTicks > DateTime.MaxTicks)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_UTCOutOfRange", null);
                return false;
            }

            if (offsetTicks < DateTimeOffset.MinOffset || offsetTicks > DateTimeOffset.MaxOffset)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_OffsetOutOfRange", null);
                return false;
            }

            if ((styles & DateTimeStyles.AdjustToUniversal) != 0)
            {
                if (((result.flags & ParseFlags.TimeZoneUsed) == 0) && ((styles & DateTimeStyles.AssumeUniversal) == 0))
                {
                    Boolean toUtcResult = AdjustTimeZoneToUniversal(ref result);
                    result.timeZoneOffset = TimeSpan.Zero;
                    return toUtcResult;
                }

                result.parsedDate = new DateTime(utcTicks, DateTimeKind.Utc);
                result.timeZoneOffset = TimeSpan.Zero;
            }

            return true;
        }

        private static Boolean AdjustTimeZoneToUniversal(ref DateTimeResult result)
        {
            long resultTicks = result.parsedDate.Ticks;
            resultTicks -= result.timeZoneOffset.Ticks;
            if (resultTicks < 0)
            {
                resultTicks += Calendar.TicksPerDay;
            }

            if (resultTicks < DateTime.MinTicks || resultTicks > DateTime.MaxTicks)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_DateOutOfRange", null);
                return false;
            }

            result.parsedDate = new DateTime(resultTicks, DateTimeKind.Utc);
            return true;
        }

        private static Boolean AdjustTimeZoneToLocal(ref DateTimeResult result, bool bTimeOnly)
        {
            long resultTicks = result.parsedDate.Ticks;
            TimeZoneInfo tz = TimeZoneInfo.Local;
            Boolean isAmbiguousLocalDst = false;
            if (resultTicks < Calendar.TicksPerDay)
            {
                resultTicks -= result.timeZoneOffset.Ticks;
                resultTicks += tz.GetUtcOffset(bTimeOnly ? DateTime.Now : result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
                if (resultTicks < 0)
                {
                    resultTicks += Calendar.TicksPerDay;
                }
            }
            else
            {
                resultTicks -= result.timeZoneOffset.Ticks;
                if (resultTicks < DateTime.MinTicks || resultTicks > DateTime.MaxTicks)
                {
                    resultTicks += tz.GetUtcOffset(result.parsedDate, TimeZoneInfoOptions.NoThrowOnInvalidTime).Ticks;
                }
                else
                {
                    DateTime utcDt = new DateTime(resultTicks, DateTimeKind.Utc);
                    Boolean isDaylightSavings = false;
                    resultTicks += TimeZoneInfo.GetUtcOffsetFromUtc(utcDt, TimeZoneInfo.Local, out isDaylightSavings, out isAmbiguousLocalDst).Ticks;
                }
            }

            if (resultTicks < DateTime.MinTicks || resultTicks > DateTime.MaxTicks)
            {
                result.parsedDate = DateTime.MinValue;
                result.SetFailure(ParseFailureKind.Format, "Format_DateOutOfRange", null);
                return false;
            }

            result.parsedDate = new DateTime(resultTicks, DateTimeKind.Local, isAmbiguousLocalDst);
            return true;
        }

        private static bool ParseISO8601(ref DateTimeRawInfo raw, ref __DTString str, DateTimeStyles styles, ref DateTimeResult result)
        {
            if (raw.year < 0 || raw.GetNumber(0) < 0 || raw.GetNumber(1) < 0)
            {
            }

            str.Index--;
            int hour, minute;
            int second = 0;
            double partSecond = 0;
            str.SkipWhiteSpaces();
            if (!ParseDigits(ref str, 2, out hour))
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            str.SkipWhiteSpaces();
            if (!str.Match(':'))
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            str.SkipWhiteSpaces();
            if (!ParseDigits(ref str, 2, out minute))
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            str.SkipWhiteSpaces();
            if (str.Match(':'))
            {
                str.SkipWhiteSpaces();
                if (!ParseDigits(ref str, 2, out second))
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                    return false;
                }

                if (str.Match('.'))
                {
                    if (!ParseFraction(ref str, out partSecond))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    str.Index--;
                }

                str.SkipWhiteSpaces();
            }

            if (str.GetNext())
            {
                char ch = str.GetChar();
                if (ch == '+' || ch == '-')
                {
                    result.flags |= ParseFlags.TimeZoneUsed;
                    if (!ParseTimeZone(ref str, ref result.timeZoneOffset))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }
                }
                else if (ch == 'Z' || ch == 'z')
                {
                    result.flags |= ParseFlags.TimeZoneUsed;
                    result.timeZoneOffset = TimeSpan.Zero;
                    result.flags |= ParseFlags.TimeZoneUtc;
                }
                else
                {
                    str.Index--;
                }

                str.SkipWhiteSpaces();
                if (str.Match('#'))
                {
                    if (!VerifyValidPunctuation(ref str))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    str.SkipWhiteSpaces();
                }

                if (str.Match('\0'))
                {
                    if (!VerifyValidPunctuation(ref str))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }
                }

                if (str.GetNext())
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                    return false;
                }
            }

            DateTime time;
            Calendar calendar = GregorianCalendar.GetDefaultInstance();
            if (!calendar.TryToDateTime(raw.year, raw.GetNumber(0), raw.GetNumber(1), hour, minute, second, 0, result.era, out time))
            {
                result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
                return false;
            }

            time = time.AddTicks((long)Math.Round(partSecond * Calendar.TicksPerSecond));
            result.parsedDate = time;
            if (!DetermineTimeZoneAdjustments(ref result, styles, false))
            {
                return false;
            }

            return true;
        }

        internal static bool MatchHebrewDigits(ref __DTString str, int digitLen, out int number)
        {
            number = 0;
            HebrewNumberParsingContext context = new HebrewNumberParsingContext(0);
            HebrewNumberParsingState state = HebrewNumberParsingState.ContinueParsing;
            while (state == HebrewNumberParsingState.ContinueParsing && str.GetNext())
            {
                state = HebrewNumber.ParseByChar(str.GetChar(), ref context);
            }

            if (state == HebrewNumberParsingState.FoundEndOfHebrewNumber)
            {
                number = context.result;
                return (true);
            }

            return false;
        }

        internal static bool ParseDigits(ref __DTString str, int digitLen, out int result)
        {
            if (digitLen == 1)
            {
                return ParseDigits(ref str, 1, 2, out result);
            }
            else
            {
                return ParseDigits(ref str, digitLen, digitLen, out result);
            }
        }

        internal static bool ParseDigits(ref __DTString str, int minDigitLen, int maxDigitLen, out int result)
        {
            Contract.Assert(minDigitLen > 0, "minDigitLen > 0");
            Contract.Assert(maxDigitLen < 9, "maxDigitLen < 9");
            Contract.Assert(minDigitLen <= maxDigitLen, "minDigitLen <= maxDigitLen");
            result = 0;
            int startingIndex = str.Index;
            int tokenLength = 0;
            while (tokenLength < maxDigitLen)
            {
                if (!str.GetNextDigit())
                {
                    str.Index--;
                    break;
                }

                result = result * 10 + str.GetDigit();
                tokenLength++;
            }

            if (tokenLength < minDigitLen)
            {
                str.Index = startingIndex;
                return false;
            }

            return true;
        }

        private static bool ParseFractionExact(ref __DTString str, int maxDigitLen, ref double result)
        {
            if (!str.GetNextDigit())
            {
                str.Index--;
                return false;
            }

            result = str.GetDigit();
            int digitLen = 1;
            for (; digitLen < maxDigitLen; digitLen++)
            {
                if (!str.GetNextDigit())
                {
                    str.Index--;
                    break;
                }

                result = result * 10 + str.GetDigit();
            }

            result = ((double)result / Math.Pow(10, digitLen));
            return (digitLen == maxDigitLen);
        }

        private static bool ParseSign(ref __DTString str, ref bool result)
        {
            if (!str.GetNext())
            {
                return false;
            }

            char ch = str.GetChar();
            if (ch == '+')
            {
                result = true;
                return (true);
            }
            else if (ch == '-')
            {
                result = false;
                return (true);
            }

            return false;
        }

        private static bool ParseTimeZoneOffset(ref __DTString str, int len, ref TimeSpan result)
        {
            bool isPositive = true;
            int hourOffset;
            int minuteOffset = 0;
            switch (len)
            {
                case 1:
                case 2:
                    if (!ParseSign(ref str, ref isPositive))
                    {
                        return (false);
                    }

                    if (!ParseDigits(ref str, len, out hourOffset))
                    {
                        return (false);
                    }

                    break;
                default:
                    if (!ParseSign(ref str, ref isPositive))
                    {
                        return (false);
                    }

                    if (!ParseDigits(ref str, 1, out hourOffset))
                    {
                        return (false);
                    }

                    if (str.Match(":"))
                    {
                        if (!ParseDigits(ref str, 2, out minuteOffset))
                        {
                            return (false);
                        }
                    }
                    else
                    {
                        str.Index--;
                        if (!ParseDigits(ref str, 2, out minuteOffset))
                        {
                            return (false);
                        }
                    }

                    break;
            }

            if (minuteOffset < 0 || minuteOffset >= 60)
            {
                return false;
            }

            result = (new TimeSpan(hourOffset, minuteOffset, 0));
            if (!isPositive)
            {
                result = result.Negate();
            }

            return (true);
        }

        private static bool MatchAbbreviatedMonthName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
        {
            int maxMatchStrLen = 0;
            result = -1;
            if (str.GetNext())
            {
                int monthsInYear = (dtfi.GetMonthName(13).Length == 0 ? 12 : 13);
                for (int i = 1; i <= monthsInYear; i++)
                {
                    String searchStr = dtfi.GetAbbreviatedMonthName(i);
                    int matchStrLen = searchStr.Length;
                    if (dtfi.HasSpacesInMonthNames ? str.MatchSpecifiedWords(searchStr, false, ref matchStrLen) : str.MatchSpecifiedWord(searchStr))
                    {
                        if (matchStrLen > maxMatchStrLen)
                        {
                            maxMatchStrLen = matchStrLen;
                            result = i;
                        }
                    }
                }

                if ((dtfi.FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != 0)
                {
                    int tempResult = str.MatchLongestWords(dtfi.internalGetLeapYearMonthNames(), ref maxMatchStrLen);
                    if (tempResult >= 0)
                    {
                        result = tempResult + 1;
                    }
                }
            }

            if (result > 0)
            {
                str.Index += (maxMatchStrLen - 1);
                return (true);
            }

            return false;
        }

        private static bool MatchMonthName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
        {
            int maxMatchStrLen = 0;
            result = -1;
            if (str.GetNext())
            {
                int monthsInYear = (dtfi.GetMonthName(13).Length == 0 ? 12 : 13);
                for (int i = 1; i <= monthsInYear; i++)
                {
                    String searchStr = dtfi.GetMonthName(i);
                    int matchStrLen = searchStr.Length;
                    if (dtfi.HasSpacesInMonthNames ? str.MatchSpecifiedWords(searchStr, false, ref matchStrLen) : str.MatchSpecifiedWord(searchStr))
                    {
                        if (matchStrLen > maxMatchStrLen)
                        {
                            maxMatchStrLen = matchStrLen;
                            result = i;
                        }
                    }
                }

                if ((dtfi.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
                {
                    int tempResult = str.MatchLongestWords(dtfi.MonthGenitiveNames, ref maxMatchStrLen);
                    if (tempResult >= 0)
                    {
                        result = tempResult + 1;
                    }
                }

                if ((dtfi.FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != 0)
                {
                    int tempResult = str.MatchLongestWords(dtfi.internalGetLeapYearMonthNames(), ref maxMatchStrLen);
                    if (tempResult >= 0)
                    {
                        result = tempResult + 1;
                    }
                }
            }

            if (result > 0)
            {
                str.Index += (maxMatchStrLen - 1);
                return (true);
            }

            return false;
        }

        private static bool MatchAbbreviatedDayName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
        {
            int maxMatchStrLen = 0;
            result = -1;
            if (str.GetNext())
            {
                for (DayOfWeek i = DayOfWeek.Sunday; i <= DayOfWeek.Saturday; i++)
                {
                    String searchStr = dtfi.GetAbbreviatedDayName(i);
                    int matchStrLen = searchStr.Length;
                    if (dtfi.HasSpacesInDayNames ? str.MatchSpecifiedWords(searchStr, false, ref matchStrLen) : str.MatchSpecifiedWord(searchStr))
                    {
                        if (matchStrLen > maxMatchStrLen)
                        {
                            maxMatchStrLen = matchStrLen;
                            result = (int)i;
                        }
                    }
                }
            }

            if (result >= 0)
            {
                str.Index += maxMatchStrLen - 1;
                return (true);
            }

            return false;
        }

        private static bool MatchDayName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
        {
            int maxMatchStrLen = 0;
            result = -1;
            if (str.GetNext())
            {
                for (DayOfWeek i = DayOfWeek.Sunday; i <= DayOfWeek.Saturday; i++)
                {
                    String searchStr = dtfi.GetDayName(i);
                    int matchStrLen = searchStr.Length;
                    if (dtfi.HasSpacesInDayNames ? str.MatchSpecifiedWords(searchStr, false, ref matchStrLen) : str.MatchSpecifiedWord(searchStr))
                    {
                        if (matchStrLen > maxMatchStrLen)
                        {
                            maxMatchStrLen = matchStrLen;
                            result = (int)i;
                        }
                    }
                }
            }

            if (result >= 0)
            {
                str.Index += maxMatchStrLen - 1;
                return (true);
            }

            return false;
        }

        private static bool MatchEraName(ref __DTString str, DateTimeFormatInfo dtfi, ref int result)
        {
            if (str.GetNext())
            {
                int[] eras = dtfi.Calendar.Eras;
                if (eras != null)
                {
                    for (int i = 0; i < eras.Length; i++)
                    {
                        String searchStr = dtfi.GetEraName(eras[i]);
                        if (str.MatchSpecifiedWord(searchStr))
                        {
                            str.Index += (searchStr.Length - 1);
                            result = eras[i];
                            return (true);
                        }

                        searchStr = dtfi.GetAbbreviatedEraName(eras[i]);
                        if (str.MatchSpecifiedWord(searchStr))
                        {
                            str.Index += (searchStr.Length - 1);
                            result = eras[i];
                            return (true);
                        }
                    }
                }
            }

            return false;
        }

        private static bool MatchTimeMark(ref __DTString str, DateTimeFormatInfo dtfi, ref TM result)
        {
            result = TM.NotSet;
            if (dtfi.AMDesignator.Length == 0)
            {
                result = TM.AM;
            }

            if (dtfi.PMDesignator.Length == 0)
            {
                result = TM.PM;
            }

            if (str.GetNext())
            {
                String searchStr = dtfi.AMDesignator;
                if (searchStr.Length > 0)
                {
                    if (str.MatchSpecifiedWord(searchStr))
                    {
                        str.Index += (searchStr.Length - 1);
                        result = TM.AM;
                        return (true);
                    }
                }

                searchStr = dtfi.PMDesignator;
                if (searchStr.Length > 0)
                {
                    if (str.MatchSpecifiedWord(searchStr))
                    {
                        str.Index += (searchStr.Length - 1);
                        result = TM.PM;
                        return (true);
                    }
                }

                str.Index--;
            }

            if (result != TM.NotSet)
            {
                return (true);
            }

            return false;
        }

        private static bool MatchAbbreviatedTimeMark(ref __DTString str, DateTimeFormatInfo dtfi, ref TM result)
        {
            if (str.GetNext())
            {
                if (str.GetChar() == dtfi.AMDesignator[0])
                {
                    result = TM.AM;
                    return (true);
                }

                if (str.GetChar() == dtfi.PMDesignator[0])
                {
                    result = TM.PM;
                    return (true);
                }
            }

            return false;
        }

        private static bool CheckNewValue(ref int currentValue, int newValue, char patternChar, ref DateTimeResult result)
        {
            if (currentValue == -1)
            {
                currentValue = newValue;
                return (true);
            }
            else
            {
                if (newValue != currentValue)
                {
                    result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", patternChar);
                    return (false);
                }
            }

            return (true);
        }

        private static DateTime GetDateTimeNow(ref DateTimeResult result, ref DateTimeStyles styles)
        {
            if ((result.flags & ParseFlags.CaptureOffset) != 0)
            {
                if ((result.flags & ParseFlags.TimeZoneUsed) != 0)
                {
                    return new DateTime(DateTime.UtcNow.Ticks + result.timeZoneOffset.Ticks, DateTimeKind.Unspecified);
                }
                else if ((styles & DateTimeStyles.AssumeUniversal) != 0)
                {
                    return DateTime.UtcNow;
                }
            }

            return DateTime.Now;
        }

        private static bool CheckDefaultDateTime(ref DateTimeResult result, ref Calendar cal, DateTimeStyles styles)
        {
            if ((result.flags & ParseFlags.CaptureOffset) != 0)
            {
                if (((result.Month != -1) || (result.Day != -1)) && ((result.Year == -1 || ((result.flags & ParseFlags.YearDefault) != 0)) && (result.flags & ParseFlags.TimeZoneUsed) != 0))
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_MissingIncompleteDate", null);
                    return false;
                }
            }

            if ((result.Year == -1) || (result.Month == -1) || (result.Day == -1))
            {
                DateTime now = GetDateTimeNow(ref result, ref styles);
                if (result.Month == -1 && result.Day == -1)
                {
                    if (result.Year == -1)
                    {
                        if ((styles & DateTimeStyles.NoCurrentDateDefault) != 0)
                        {
                            cal = GregorianCalendar.GetDefaultInstance();
                            result.Year = result.Month = result.Day = 1;
                        }
                        else
                        {
                            result.Year = cal.GetYear(now);
                            result.Month = cal.GetMonth(now);
                            result.Day = cal.GetDayOfMonth(now);
                        }
                    }
                    else
                    {
                        result.Month = 1;
                        result.Day = 1;
                    }
                }
                else
                {
                    if (result.Year == -1)
                    {
                        result.Year = cal.GetYear(now);
                    }

                    if (result.Month == -1)
                    {
                        result.Month = 1;
                    }

                    if (result.Day == -1)
                    {
                        result.Day = 1;
                    }
                }
            }

            if (result.Hour == -1)
                result.Hour = 0;
            if (result.Minute == -1)
                result.Minute = 0;
            if (result.Second == -1)
                result.Second = 0;
            if (result.era == -1)
                result.era = Calendar.CurrentEra;
            return true;
        }

        private static String ExpandPredefinedFormat(String format, ref DateTimeFormatInfo dtfi, ref ParsingInfo parseInfo, ref DateTimeResult result)
        {
            switch (format[0])
            {
                case 'o':
                case 'O':
                    parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    break;
                case 'r':
                case 'R':
                    parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    if ((result.flags & ParseFlags.CaptureOffset) != 0)
                    {
                        result.flags |= ParseFlags.Rfc1123Pattern;
                    }

                    break;
                case 's':
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
                    break;
                case 'u':
                    parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    if ((result.flags & ParseFlags.CaptureOffset) != 0)
                    {
                        result.flags |= ParseFlags.UtcSortPattern;
                    }

                    break;
                case 'U':
                    parseInfo.calendar = GregorianCalendar.GetDefaultInstance();
                    result.flags |= ParseFlags.TimeZoneUsed;
                    result.timeZoneOffset = new TimeSpan(0);
                    result.flags |= ParseFlags.TimeZoneUtc;
                    if (dtfi.Calendar.GetType() != typeof (GregorianCalendar))
                    {
                        dtfi = (DateTimeFormatInfo)dtfi.Clone();
                        dtfi.Calendar = GregorianCalendar.GetDefaultInstance();
                    }

                    break;
            }

            return (DateTimeFormat.GetRealFormat(format, dtfi));
        }

        private static bool ParseByFormat(ref __DTString str, ref __DTString format, ref ParsingInfo parseInfo, DateTimeFormatInfo dtfi, ref DateTimeResult result)
        {
            int tokenLen = 0;
            int tempYear = 0, tempMonth = 0, tempDay = 0, tempDayOfWeek = 0, tempHour = 0, tempMinute = 0, tempSecond = 0;
            double tempFraction = 0;
            TM tempTimeMark = 0;
            char ch = format.GetChar();
            switch (ch)
            {
                case 'y':
                    tokenLen = format.GetRepeatCount();
                    bool parseResult;
                    if (dtfi.HasForceTwoDigitYears)
                    {
                        parseResult = ParseDigits(ref str, 1, 4, out tempYear);
                    }
                    else
                    {
                        if (tokenLen <= 2)
                        {
                            parseInfo.fUseTwoDigitYear = true;
                        }

                        parseResult = ParseDigits(ref str, tokenLen, out tempYear);
                    }

                    if (!parseResult && parseInfo.fCustomNumberParser)
                    {
                        parseResult = parseInfo.parseNumberDelegate(ref str, tokenLen, out tempYear);
                    }

                    if (!parseResult)
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return (false);
                    }

                    if (!CheckNewValue(ref result.Year, tempYear, ch, ref result))
                    {
                        return (false);
                    }

                    break;
                case 'M':
                    tokenLen = format.GetRepeatCount();
                    if (tokenLen <= 2)
                    {
                        if (!ParseDigits(ref str, tokenLen, out tempMonth))
                        {
                            if (!parseInfo.fCustomNumberParser || !parseInfo.parseNumberDelegate(ref str, tokenLen, out tempMonth))
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return (false);
                            }
                        }
                    }
                    else
                    {
                        if (tokenLen == 3)
                        {
                            if (!MatchAbbreviatedMonthName(ref str, dtfi, ref tempMonth))
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return (false);
                            }
                        }
                        else
                        {
                            if (!MatchMonthName(ref str, dtfi, ref tempMonth))
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return (false);
                            }
                        }

                        result.flags |= ParseFlags.ParsedMonthName;
                    }

                    if (!CheckNewValue(ref result.Month, tempMonth, ch, ref result))
                    {
                        return (false);
                    }

                    break;
                case 'd':
                    tokenLen = format.GetRepeatCount();
                    if (tokenLen <= 2)
                    {
                        if (!ParseDigits(ref str, tokenLen, out tempDay))
                        {
                            if (!parseInfo.fCustomNumberParser || !parseInfo.parseNumberDelegate(ref str, tokenLen, out tempDay))
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return (false);
                            }
                        }

                        if (!CheckNewValue(ref result.Day, tempDay, ch, ref result))
                        {
                            return (false);
                        }
                    }
                    else
                    {
                        if (tokenLen == 3)
                        {
                            if (!MatchAbbreviatedDayName(ref str, dtfi, ref tempDayOfWeek))
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return (false);
                            }
                        }
                        else
                        {
                            if (!MatchDayName(ref str, dtfi, ref tempDayOfWeek))
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return (false);
                            }
                        }

                        if (!CheckNewValue(ref parseInfo.dayOfWeek, tempDayOfWeek, ch, ref result))
                        {
                            return (false);
                        }
                    }

                    break;
                case 'g':
                    tokenLen = format.GetRepeatCount();
                    if (!MatchEraName(ref str, dtfi, ref result.era))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return (false);
                    }

                    break;
                case 'h':
                    parseInfo.fUseHour12 = true;
                    tokenLen = format.GetRepeatCount();
                    if (!ParseDigits(ref str, (tokenLen < 2 ? 1 : 2), out tempHour))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return (false);
                    }

                    if (!CheckNewValue(ref result.Hour, tempHour, ch, ref result))
                    {
                        return (false);
                    }

                    break;
                case 'H':
                    tokenLen = format.GetRepeatCount();
                    if (!ParseDigits(ref str, (tokenLen < 2 ? 1 : 2), out tempHour))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return (false);
                    }

                    if (!CheckNewValue(ref result.Hour, tempHour, ch, ref result))
                    {
                        return (false);
                    }

                    break;
                case 'm':
                    tokenLen = format.GetRepeatCount();
                    if (!ParseDigits(ref str, (tokenLen < 2 ? 1 : 2), out tempMinute))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return (false);
                    }

                    if (!CheckNewValue(ref result.Minute, tempMinute, ch, ref result))
                    {
                        return (false);
                    }

                    break;
                case 's':
                    tokenLen = format.GetRepeatCount();
                    if (!ParseDigits(ref str, (tokenLen < 2 ? 1 : 2), out tempSecond))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return (false);
                    }

                    if (!CheckNewValue(ref result.Second, tempSecond, ch, ref result))
                    {
                        return (false);
                    }

                    break;
                case 'f':
                case 'F':
                    tokenLen = format.GetRepeatCount();
                    if (tokenLen <= DateTimeFormat.MaxSecondsFractionDigits)
                    {
                        if (!ParseFractionExact(ref str, tokenLen, ref tempFraction))
                        {
                            if (ch == 'f')
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return (false);
                            }
                        }

                        if (result.fraction < 0)
                        {
                            result.fraction = tempFraction;
                        }
                        else
                        {
                            if (tempFraction != result.fraction)
                            {
                                result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", ch);
                                return (false);
                            }
                        }
                    }
                    else
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return (false);
                    }

                    break;
                case 't':
                    tokenLen = format.GetRepeatCount();
                    if (tokenLen == 1)
                    {
                        if (!MatchAbbreviatedTimeMark(ref str, dtfi, ref tempTimeMark))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            return (false);
                        }
                    }
                    else
                    {
                        if (!MatchTimeMark(ref str, dtfi, ref tempTimeMark))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            return (false);
                        }
                    }

                    if (parseInfo.timeMark == TM.NotSet)
                    {
                        parseInfo.timeMark = tempTimeMark;
                    }
                    else
                    {
                        if (parseInfo.timeMark != tempTimeMark)
                        {
                            result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", ch);
                            return (false);
                        }
                    }

                    break;
                case 'z':
                    tokenLen = format.GetRepeatCount();
                {
                    TimeSpan tempTimeZoneOffset = new TimeSpan(0);
                    if (!ParseTimeZoneOffset(ref str, tokenLen, ref tempTimeZoneOffset))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return (false);
                    }

                    if ((result.flags & ParseFlags.TimeZoneUsed) != 0 && tempTimeZoneOffset != result.timeZoneOffset)
                    {
                        result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'z');
                        return (false);
                    }

                    result.timeZoneOffset = tempTimeZoneOffset;
                    result.flags |= ParseFlags.TimeZoneUsed;
                }

                    break;
                case 'Z':
                    if ((result.flags & ParseFlags.TimeZoneUsed) != 0 && result.timeZoneOffset != TimeSpan.Zero)
                    {
                        result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'Z');
                        return (false);
                    }

                    result.flags |= ParseFlags.TimeZoneUsed;
                    result.timeZoneOffset = new TimeSpan(0);
                    result.flags |= ParseFlags.TimeZoneUtc;
                    str.Index++;
                    if (!GetTimeZoneName(ref str))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    str.Index--;
                    break;
                case 'K':
                    if (str.Match('Z'))
                    {
                        if ((result.flags & ParseFlags.TimeZoneUsed) != 0 && result.timeZoneOffset != TimeSpan.Zero)
                        {
                            result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'K');
                            return (false);
                        }

                        result.flags |= ParseFlags.TimeZoneUsed;
                        result.timeZoneOffset = new TimeSpan(0);
                        result.flags |= ParseFlags.TimeZoneUtc;
                    }
                    else if (str.Match('+') || str.Match('-'))
                    {
                        str.Index--;
                        TimeSpan tempTimeZoneOffset = new TimeSpan(0);
                        if (!ParseTimeZoneOffset(ref str, 3, ref tempTimeZoneOffset))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            return (false);
                        }

                        if ((result.flags & ParseFlags.TimeZoneUsed) != 0 && tempTimeZoneOffset != result.timeZoneOffset)
                        {
                            result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_RepeatDateTimePattern", 'K');
                            return (false);
                        }

                        result.timeZoneOffset = tempTimeZoneOffset;
                        result.flags |= ParseFlags.TimeZoneUsed;
                    }

                    break;
                case ':':
                    if (((dtfi.TimeSeparator.Length > 1 && dtfi.TimeSeparator[0] == ':') || !str.Match(':')) && !str.Match(dtfi.TimeSeparator))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    break;
                case '/':
                    if (((dtfi.DateSeparator.Length > 1 && dtfi.DateSeparator[0] == '/') || !str.Match('/')) && !str.Match(dtfi.DateSeparator))
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    break;
                case '\"':
                case '\'':
                    StringBuilder enquotedString = new StringBuilder();
                    if (!TryParseQuoteString(format.Value, format.Index, enquotedString, out tokenLen))
                    {
                        result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadQuote", ch);
                        return (false);
                    }

                    format.Index += tokenLen - 1;
                    String quotedStr = enquotedString.ToString();
                    for (int i = 0; i < quotedStr.Length; i++)
                    {
                        if (quotedStr[i] == ' ' && parseInfo.fAllowInnerWhite)
                        {
                            str.SkipWhiteSpaces();
                        }
                        else if (!str.Match(quotedStr[i]))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            return false;
                        }
                    }

                    if ((result.flags & ParseFlags.CaptureOffset) != 0)
                    {
                        if ((result.flags & ParseFlags.Rfc1123Pattern) != 0 && quotedStr == GMTName)
                        {
                            result.flags |= ParseFlags.TimeZoneUsed;
                            result.timeZoneOffset = TimeSpan.Zero;
                        }
                        else if ((result.flags & ParseFlags.UtcSortPattern) != 0 && quotedStr == ZuluName)
                        {
                            result.flags |= ParseFlags.TimeZoneUsed;
                            result.timeZoneOffset = TimeSpan.Zero;
                        }
                    }

                    break;
                case '%':
                    if (format.Index >= format.Value.Length - 1 || format.Value[format.Index + 1] == '%')
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
                        return false;
                    }

                    break;
                case '\\':
                    if (format.GetNext())
                    {
                        if (!str.Match(format.GetChar()))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            return false;
                        }
                    }
                    else
                    {
                        result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
                        return false;
                    }

                    break;
                case '.':
                    if (!str.Match(ch))
                    {
                        if (format.GetNext())
                        {
                            if (format.Match('F'))
                            {
                                format.GetRepeatCount();
                                break;
                            }
                        }

                        result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                        return false;
                    }

                    break;
                default:
                    if (ch == ' ')
                    {
                        if (parseInfo.fAllowInnerWhite)
                        {
                        }
                        else
                        {
                            if (!str.Match(ch))
                            {
                                if (parseInfo.fAllowTrailingWhite)
                                {
                                    if (format.GetNext())
                                    {
                                        if (ParseByFormat(ref str, ref format, ref parseInfo, dtfi, ref result))
                                        {
                                            return (true);
                                        }
                                    }
                                }

                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        if (format.MatchSpecifiedWord(GMTName))
                        {
                            format.Index += (GMTName.Length - 1);
                            result.flags |= ParseFlags.TimeZoneUsed;
                            result.timeZoneOffset = TimeSpan.Zero;
                            if (!str.Match(GMTName))
                            {
                                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                                return false;
                            }
                        }
                        else if (!str.Match(ch))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                            return false;
                        }
                    }

                    break;
            }

            return (true);
        }

        internal static bool TryParseQuoteString(String format, int pos, StringBuilder result, out int returnValue)
        {
            returnValue = 0;
            int formatLen = format.Length;
            int beginPos = pos;
            char quoteChar = format[pos++];
            bool foundQuote = false;
            while (pos < formatLen)
            {
                char ch = format[pos++];
                if (ch == quoteChar)
                {
                    foundQuote = true;
                    break;
                }
                else if (ch == '\\')
                {
                    if (pos < formatLen)
                    {
                        result.Append(format[pos++]);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    result.Append(ch);
                }
            }

            if (!foundQuote)
            {
                return false;
            }

            returnValue = (pos - beginPos);
            return true;
        }

        private static bool DoStrictParse(String s, String formatParam, DateTimeStyles styles, DateTimeFormatInfo dtfi, ref DateTimeResult result)
        {
            ParsingInfo parseInfo = new ParsingInfo();
            parseInfo.Init();
            parseInfo.calendar = dtfi.Calendar;
            parseInfo.fAllowInnerWhite = ((styles & DateTimeStyles.AllowInnerWhite) != 0);
            parseInfo.fAllowTrailingWhite = ((styles & DateTimeStyles.AllowTrailingWhite) != 0);
            String originalFormat = formatParam;
            if (formatParam.Length == 1)
            {
                if (((result.flags & ParseFlags.CaptureOffset) != 0) && formatParam[0] == 'U')
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier", null);
                    return false;
                }

                formatParam = ExpandPredefinedFormat(formatParam, ref dtfi, ref parseInfo, ref result);
            }

            bool bTimeOnly = false;
            result.calendar = parseInfo.calendar;
            if (parseInfo.calendar.ID == Calendar.CAL_HEBREW)
            {
                parseInfo.parseNumberDelegate = m_hebrewNumberParser;
                parseInfo.fCustomNumberParser = true;
            }

            result.Hour = result.Minute = result.Second = -1;
            __DTString format = new __DTString(formatParam, dtfi, false);
            __DTString str = new __DTString(s, dtfi, false);
            if (parseInfo.fAllowTrailingWhite)
            {
                format.TrimTail();
                format.RemoveTrailingInQuoteSpaces();
                str.TrimTail();
            }

            if ((styles & DateTimeStyles.AllowLeadingWhite) != 0)
            {
                format.SkipWhiteSpaces();
                format.RemoveLeadingInQuoteSpaces();
                str.SkipWhiteSpaces();
            }

            while (format.GetNext())
            {
                if (parseInfo.fAllowInnerWhite)
                {
                    str.SkipWhiteSpaces();
                }

                if (!ParseByFormat(ref str, ref format, ref parseInfo, dtfi, ref result))
                {
                    return (false);
                }
            }

            if (str.Index < str.Value.Length - 1)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                return false;
            }

            if (parseInfo.fUseTwoDigitYear && ((dtfi.FormatFlags & DateTimeFormatFlags.UseHebrewRule) == 0))
            {
                if (result.Year >= 100)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                    return false;
                }

                try
                {
                    result.Year = parseInfo.calendar.ToFourDigitYear(result.Year);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", e);
                    return false;
                }
            }

            if (parseInfo.fUseHour12)
            {
                if (parseInfo.timeMark == TM.NotSet)
                {
                    parseInfo.timeMark = TM.AM;
                }

                if (result.Hour > 12)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                    return false;
                }

                if (parseInfo.timeMark == TM.AM)
                {
                    if (result.Hour == 12)
                    {
                        result.Hour = 0;
                    }
                }
                else
                {
                    result.Hour = (result.Hour == 12) ? 12 : result.Hour + 12;
                }
            }
            else
            {
                if ((parseInfo.timeMark == TM.AM && result.Hour >= 12) || (parseInfo.timeMark == TM.PM && result.Hour < 12))
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDateTime", null);
                    return false;
                }
            }

            bTimeOnly = (result.Year == -1 && result.Month == -1 && result.Day == -1);
            if (!CheckDefaultDateTime(ref result, ref parseInfo.calendar, styles))
            {
                return false;
            }

            if (!bTimeOnly && dtfi.HasYearMonthAdjustment)
            {
                if (!dtfi.YearMonthAdjustment(ref result.Year, ref result.Month, ((result.flags & ParseFlags.ParsedMonthName) != 0)))
                {
                    result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
                    return false;
                }
            }

            if (!parseInfo.calendar.TryToDateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, 0, result.era, out result.parsedDate))
            {
                result.SetFailure(ParseFailureKind.FormatBadDateTimeCalendar, "Format_BadDateTimeCalendar", null);
                return false;
            }

            if (result.fraction > 0)
            {
                result.parsedDate = result.parsedDate.AddTicks((long)Math.Round(result.fraction * Calendar.TicksPerSecond));
            }

            if (parseInfo.dayOfWeek != -1)
            {
                if (parseInfo.dayOfWeek != (int)parseInfo.calendar.GetDayOfWeek(result.parsedDate))
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadDayOfWeek", null);
                    return false;
                }
            }

            if (!DetermineTimeZoneAdjustments(ref result, styles, bTimeOnly))
            {
                return false;
            }

            return true;
        }

        private static Exception GetDateTimeParseException(ref DateTimeResult result)
        {
            switch (result.failure)
            {
                case ParseFailureKind.ArgumentNull:
                    return new ArgumentNullException(result.failureArgumentName, Environment.GetResourceString(result.failureMessageID));
                case ParseFailureKind.Format:
                    return new FormatException(Environment.GetResourceString(result.failureMessageID));
                case ParseFailureKind.FormatWithParameter:
                    return new FormatException(Environment.GetResourceString(result.failureMessageID, result.failureMessageFormatArgument));
                case ParseFailureKind.FormatBadDateTimeCalendar:
                    return new FormatException(Environment.GetResourceString(result.failureMessageID, result.calendar));
                default:
                    Contract.Assert(false, "Unkown DateTimeParseFailure: " + result);
                    return null;
            }
        }

        internal static void LexTraceExit(string message, DS dps)
        {
            if (!_tracingEnabled)
                return;
            BCLDebug.Trace("DATETIME", "[DATETIME] Lex return {0}, DS.{1}", message, dps);
        }

        internal static void PTSTraceExit(DS dps, bool passed)
        {
            if (!_tracingEnabled)
                return;
            BCLDebug.Trace("DATETIME", "[DATETIME] ProcessTerminalState {0} @ DS.{1}", passed ? "passed" : "failed", dps);
        }

        internal static void TPTraceExit(string message, DS dps)
        {
            if (!_tracingEnabled)
                return;
            BCLDebug.Trace("DATETIME", "[DATETIME] TryParse return {0}, DS.{1}", message, dps);
        }

        internal static void DTFITrace(DateTimeFormatInfo dtfi)
        {
            if (!_tracingEnabled)
                return;
            BCLDebug.Trace("DATETIME", "[DATETIME] DateTimeFormatInfo Properties");
            BCLDebug.Trace("DATETIME", " NativeCalendarName {0}", Hex(dtfi.NativeCalendarName));
            BCLDebug.Trace("DATETIME", "       AMDesignator {0}", Hex(dtfi.AMDesignator));
            BCLDebug.Trace("DATETIME", "       PMDesignator {0}", Hex(dtfi.PMDesignator));
            BCLDebug.Trace("DATETIME", "      TimeSeparator {0}", Hex(dtfi.TimeSeparator));
            BCLDebug.Trace("DATETIME", "      AbbrvDayNames {0}", Hex(dtfi.AbbreviatedDayNames));
            BCLDebug.Trace("DATETIME", "   ShortestDayNames {0}", Hex(dtfi.ShortestDayNames));
            BCLDebug.Trace("DATETIME", "           DayNames {0}", Hex(dtfi.DayNames));
            BCLDebug.Trace("DATETIME", "    AbbrvMonthNames {0}", Hex(dtfi.AbbreviatedMonthNames));
            BCLDebug.Trace("DATETIME", "         MonthNames {0}", Hex(dtfi.MonthNames));
            BCLDebug.Trace("DATETIME", " AbbrvMonthGenNames {0}", Hex(dtfi.AbbreviatedMonthGenitiveNames));
            BCLDebug.Trace("DATETIME", "      MonthGenNames {0}", Hex(dtfi.MonthGenitiveNames));
        }

        internal static string Hex(string[] strs)
        {
            if (strs == null || strs.Length == 0)
                return String.Empty;
            if (strs.Length == 1)
                return Hex(strs[0]);
            int curLineLength = 0;
            int maxLineLength = 55;
            int newLinePadding = 20;
            StringBuilder buffer = new StringBuilder();
            buffer.Append(Hex(strs[0]));
            curLineLength = buffer.Length;
            String s;
            for (int i = 1; i < strs.Length - 1; i++)
            {
                s = Hex(strs[i]);
                if (s.Length > maxLineLength || (curLineLength + s.Length + 2) > maxLineLength)
                {
                    buffer.Append(',');
                    buffer.Append(Environment.NewLine);
                    buffer.Append(' ', newLinePadding);
                    curLineLength = 0;
                }
                else
                {
                    buffer.Append(", ");
                    curLineLength += 2;
                }

                buffer.Append(s);
                curLineLength += s.Length;
            }

            buffer.Append(',');
            s = Hex(strs[strs.Length - 1]);
            if (s.Length > maxLineLength || (curLineLength + s.Length + 6) > maxLineLength)
            {
                buffer.Append(Environment.NewLine);
                buffer.Append(' ', newLinePadding);
            }
            else
            {
                buffer.Append(' ');
            }

            buffer.Append(s);
            return buffer.ToString();
        }

        internal static string Hex(string str)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("\"");
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] <= '\x007f')
                    buffer.Append(str[i]);
                else
                    buffer.Append("\\u" + ((int)str[i]).ToString("x4", CultureInfo.InvariantCulture));
            }

            buffer.Append("\"");
            return buffer.ToString();
        }

        internal static String Hex(char c)
        {
            if (c <= '\x007f')
                return c.ToString(CultureInfo.InvariantCulture);
            else
                return "\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture);
        }

        internal static bool _tracingEnabled = BCLDebug.CheckEnabled("DATETIME");
    }

    internal struct __DTString
    {
        internal String Value;
        internal int Index;
        internal int len;
        internal char m_current;
        private CompareInfo m_info;
        private bool m_checkDigitToken;
        internal __DTString(String str, DateTimeFormatInfo dtfi, bool checkDigitToken): this (str, dtfi)
        {
            m_checkDigitToken = checkDigitToken;
        }

        internal __DTString(String str, DateTimeFormatInfo dtfi)
        {
            Index = -1;
            Value = str;
            len = Value.Length;
            m_current = '\0';
            if (dtfi != null)
            {
                m_info = dtfi.CompareInfo;
                m_checkDigitToken = ((dtfi.FormatFlags & DateTimeFormatFlags.UseDigitPrefixInTokens) != 0);
            }
            else
            {
                m_info = Thread.CurrentThread.CurrentCulture.CompareInfo;
                m_checkDigitToken = false;
            }
        }

        internal CompareInfo CompareInfo
        {
            get
            {
                return m_info;
            }
        }

        internal bool GetNext()
        {
            Index++;
            if (Index < len)
            {
                m_current = Value[Index];
                return (true);
            }

            return (false);
        }

        internal bool AtEnd()
        {
            return Index < len ? false : true;
        }

        internal bool Advance(int count)
        {
            Contract.Assert(Index + count <= len, "__DTString::Advance: Index + count <= len");
            Index += count;
            if (Index < len)
            {
                m_current = Value[Index];
                return (true);
            }

            return (false);
        }

        internal void GetRegularToken(out TokenType tokenType, out int tokenValue, DateTimeFormatInfo dtfi)
        {
            tokenValue = 0;
            if (Index >= len)
            {
                tokenType = TokenType.EndOfString;
                return;
            }

            tokenType = TokenType.UnknownToken;
            Start:
                if (DateTimeParse.IsDigit(m_current))
                {
                    tokenValue = m_current - '0';
                    int value;
                    int start = Index;
                    while (++Index < len)
                    {
                        m_current = Value[Index];
                        value = m_current - '0';
                        if (value >= 0 && value <= 9)
                        {
                            tokenValue = tokenValue * 10 + value;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (Index - start > DateTimeParse.MaxDateTimeNumberDigits)
                    {
                        tokenType = TokenType.NumberToken;
                        tokenValue = -1;
                    }
                    else if (Index - start < 3)
                    {
                        tokenType = TokenType.NumberToken;
                    }
                    else
                    {
                        tokenType = TokenType.YearNumberToken;
                    }

                    if (m_checkDigitToken)
                    {
                        int save = Index;
                        char saveCh = m_current;
                        Index = start;
                        m_current = Value[Index];
                        TokenType tempType;
                        int tempValue;
                        if (dtfi.Tokenize(TokenType.RegularTokenMask, out tempType, out tempValue, ref this))
                        {
                            tokenType = tempType;
                            tokenValue = tempValue;
                        }
                        else
                        {
                            Index = save;
                            m_current = saveCh;
                        }
                    }
                }
                else if (Char.IsWhiteSpace(m_current))
                {
                    while (++Index < len)
                    {
                        m_current = Value[Index];
                        if (!(Char.IsWhiteSpace(m_current)))
                        {
                            goto Start;
                        }
                    }

                    tokenType = TokenType.EndOfString;
                }
                else
                {
                    dtfi.Tokenize(TokenType.RegularTokenMask, out tokenType, out tokenValue, ref this);
                }
        }

        internal TokenType GetSeparatorToken(DateTimeFormatInfo dtfi, out int indexBeforeSeparator, out char charBeforeSeparator)
        {
            indexBeforeSeparator = Index;
            charBeforeSeparator = m_current;
            TokenType tokenType;
            if (!SkipWhiteSpaceCurrent())
            {
                return (TokenType.SEP_End);
            }

            if (!DateTimeParse.IsDigit(m_current))
            {
                int tokenValue;
                bool found = dtfi.Tokenize(TokenType.SeparatorTokenMask, out tokenType, out tokenValue, ref this);
                if (!found)
                {
                    tokenType = TokenType.SEP_Space;
                }
            }
            else
            {
                tokenType = TokenType.SEP_Space;
            }

            return (tokenType);
        }

        internal bool MatchSpecifiedWord(String target)
        {
            return MatchSpecifiedWord(target, target.Length + Index);
        }

        internal bool MatchSpecifiedWord(String target, int endIndex)
        {
            int count = endIndex - Index;
            if (count != target.Length)
            {
                return false;
            }

            if (Index + count > len)
            {
                return false;
            }

            return (m_info.Compare(Value, Index, count, target, 0, count, CompareOptions.IgnoreCase) == 0);
        }

        private static Char[] WhiteSpaceChecks = new Char[]{' ', '\u00A0'};
        internal bool MatchSpecifiedWords(String target, bool checkWordBoundary, ref int matchLength)
        {
            int valueRemaining = Value.Length - Index;
            matchLength = target.Length;
            if (matchLength > valueRemaining || m_info.Compare(Value, Index, matchLength, target, 0, matchLength, CompareOptions.IgnoreCase) != 0)
            {
                int targetPosition = 0;
                int thisPosition = Index;
                int wsIndex = target.IndexOfAny(WhiteSpaceChecks, targetPosition);
                if (wsIndex == -1)
                {
                    return false;
                }

                do
                {
                    int segmentLength = wsIndex - targetPosition;
                    if (thisPosition >= Value.Length - segmentLength)
                    {
                        return false;
                    }

                    if (segmentLength == 0)
                    {
                        matchLength--;
                    }
                    else
                    {
                        if (!Char.IsWhiteSpace(Value[thisPosition + segmentLength]))
                        {
                            return false;
                        }

                        if (m_info.Compare(Value, thisPosition, segmentLength, target, targetPosition, segmentLength, CompareOptions.IgnoreCase) != 0)
                        {
                            return false;
                        }

                        thisPosition = thisPosition + segmentLength + 1;
                    }

                    targetPosition = wsIndex + 1;
                    while (thisPosition < Value.Length && Char.IsWhiteSpace(Value[thisPosition]))
                    {
                        thisPosition++;
                        matchLength++;
                    }
                }
                while ((wsIndex = target.IndexOfAny(WhiteSpaceChecks, targetPosition)) >= 0);
                if (targetPosition < target.Length)
                {
                    int segmentLength = target.Length - targetPosition;
                    if (thisPosition > Value.Length - segmentLength)
                    {
                        return false;
                    }

                    if (m_info.Compare(Value, thisPosition, segmentLength, target, targetPosition, segmentLength, CompareOptions.IgnoreCase) != 0)
                    {
                        return false;
                    }
                }
            }

            if (checkWordBoundary)
            {
                int nextCharIndex = Index + matchLength;
                if (nextCharIndex < Value.Length)
                {
                    if (Char.IsLetter(Value[nextCharIndex]))
                    {
                        return (false);
                    }
                }
            }

            return (true);
        }

        internal bool Match(String str)
        {
            if (++Index >= len)
            {
                return (false);
            }

            if (str.Length > (Value.Length - Index))
            {
                return false;
            }

            if (m_info.Compare(Value, Index, str.Length, str, 0, str.Length, CompareOptions.Ordinal) == 0)
            {
                Index += (str.Length - 1);
                return (true);
            }

            return (false);
        }

        internal bool Match(char ch)
        {
            if (++Index >= len)
            {
                return (false);
            }

            if (Value[Index] == ch)
            {
                m_current = ch;
                return (true);
            }

            Index--;
            return (false);
        }

        internal int MatchLongestWords(String[] words, ref int maxMatchStrLen)
        {
            int result = -1;
            for (int i = 0; i < words.Length; i++)
            {
                String word = words[i];
                int matchLength = word.Length;
                if (MatchSpecifiedWords(word, false, ref matchLength))
                {
                    if (matchLength > maxMatchStrLen)
                    {
                        maxMatchStrLen = matchLength;
                        result = i;
                    }
                }
            }

            return (result);
        }

        internal int GetRepeatCount()
        {
            char repeatChar = Value[Index];
            int pos = Index + 1;
            while ((pos < len) && (Value[pos] == repeatChar))
            {
                pos++;
            }

            int repeatCount = (pos - Index);
            Index = pos - 1;
            return (repeatCount);
        }

        internal bool GetNextDigit()
        {
            if (++Index >= len)
            {
                return (false);
            }

            return (DateTimeParse.IsDigit(Value[Index]));
        }

        internal char GetChar()
        {
            Contract.Assert(Index >= 0 && Index < len, "Index >= 0 && Index < len");
            return (Value[Index]);
        }

        internal int GetDigit()
        {
            Contract.Assert(Index >= 0 && Index < len, "Index >= 0 && Index < len");
            Contract.Assert(DateTimeParse.IsDigit(Value[Index]), "IsDigit(Value[Index])");
            return (Value[Index] - '0');
        }

        internal void SkipWhiteSpaces()
        {
            while (Index + 1 < len)
            {
                char ch = Value[Index + 1];
                if (!Char.IsWhiteSpace(ch))
                {
                    return;
                }

                Index++;
            }

            return;
        }

        internal bool SkipWhiteSpaceCurrent()
        {
            if (Index >= len)
            {
                return (false);
            }

            if (!Char.IsWhiteSpace(m_current))
            {
                return (true);
            }

            while (++Index < len)
            {
                m_current = Value[Index];
                if (!Char.IsWhiteSpace(m_current))
                {
                    return (true);
                }
            }

            return (false);
        }

        internal void TrimTail()
        {
            int i = len - 1;
            while (i >= 0 && Char.IsWhiteSpace(Value[i]))
            {
                i--;
            }

            Value = Value.Substring(0, i + 1);
            len = Value.Length;
        }

        internal void RemoveTrailingInQuoteSpaces()
        {
            int i = len - 1;
            if (i <= 1)
            {
                return;
            }

            char ch = Value[i];
            if (ch == '\'' || ch == '\"')
            {
                if (Char.IsWhiteSpace(Value[i - 1]))
                {
                    i--;
                    while (i >= 1 && Char.IsWhiteSpace(Value[i - 1]))
                    {
                        i--;
                    }

                    Value = Value.Remove(i, Value.Length - 1 - i);
                    len = Value.Length;
                }
            }
        }

        internal void RemoveLeadingInQuoteSpaces()
        {
            if (len <= 2)
            {
                return;
            }

            int i = 0;
            char ch = Value[i];
            if (ch == '\'' || ch == '\"')
            {
                while ((i + 1) < len && Char.IsWhiteSpace(Value[i + 1]))
                {
                    i++;
                }

                if (i != 0)
                {
                    Value = Value.Remove(1, i);
                    len = Value.Length;
                }
            }
        }

        internal DTSubString GetSubString()
        {
            DTSubString sub = new DTSubString();
            sub.index = Index;
            sub.s = Value;
            while (Index + sub.length < len)
            {
                DTSubStringType currentType;
                Char ch = Value[Index + sub.length];
                if (ch >= '0' && ch <= '9')
                {
                    currentType = DTSubStringType.Number;
                }
                else
                {
                    currentType = DTSubStringType.Other;
                }

                if (sub.length == 0)
                {
                    sub.type = currentType;
                }
                else
                {
                    if (sub.type != currentType)
                    {
                        break;
                    }
                }

                sub.length++;
                if (currentType == DTSubStringType.Number)
                {
                    if (sub.length > DateTimeParse.MaxDateTimeNumberDigits)
                    {
                        sub.type = DTSubStringType.Invalid;
                        return sub;
                    }

                    int number = ch - '0';
                    Contract.Assert(number >= 0 && number <= 9, "number >= 0 && number <= 9");
                    sub.value = sub.value * 10 + number;
                }
                else
                {
                    break;
                }
            }

            if (sub.length == 0)
            {
                sub.type = DTSubStringType.End;
                return sub;
            }

            return sub;
        }

        internal void ConsumeSubString(DTSubString sub)
        {
            Contract.Assert(sub.index == Index, "sub.index == Index");
            Contract.Assert(sub.index + sub.length <= len, "sub.index + sub.length <= len");
            Index = sub.index + sub.length;
            if (Index < len)
            {
                m_current = Value[Index];
            }
        }
    }

    internal enum DTSubStringType
    {
        Unknown = 0,
        Invalid = 1,
        Number = 2,
        End = 3,
        Other = 4
    }

    internal struct DTSubString
    {
        internal String s;
        internal Int32 index;
        internal Int32 length;
        internal DTSubStringType type;
        internal Int32 value;
        internal Char this[Int32 relativeIndex]
        {
            get
            {
                return s[index + relativeIndex];
            }
        }
    }

    internal struct DateTimeToken
    {
        internal DateTimeParse.DTT dtt;
        internal TokenType suffix;
        internal int num;
    }

    internal unsafe struct DateTimeRawInfo
    {
        private int *num;
        internal int numCount;
        internal int month;
        internal int year;
        internal int dayOfWeek;
        internal int era;
        internal DateTimeParse.TM timeMark;
        internal double fraction;
        internal bool hasSameDateAndTimeSeparators;
        internal bool timeZone;
        internal void Init(int *numberBuffer)
        {
            month = -1;
            year = -1;
            dayOfWeek = -1;
            era = -1;
            timeMark = DateTimeParse.TM.NotSet;
            fraction = -1;
            num = numberBuffer;
        }

        internal unsafe void AddNumber(int value)
        {
            num[numCount++] = value;
        }

        internal unsafe int GetNumber(int index)
        {
            return num[index];
        }
    }

    internal enum ParseFailureKind
    {
        None = 0,
        ArgumentNull = 1,
        Format = 2,
        FormatWithParameter = 3,
        FormatBadDateTimeCalendar = 4
    }

    ;
    [Flags]
    internal enum ParseFlags
    {
        HaveYear = 0x00000001,
        HaveMonth = 0x00000002,
        HaveDay = 0x00000004,
        HaveHour = 0x00000008,
        HaveMinute = 0x00000010,
        HaveSecond = 0x00000020,
        HaveTime = 0x00000040,
        HaveDate = 0x00000080,
        TimeZoneUsed = 0x00000100,
        TimeZoneUtc = 0x00000200,
        ParsedMonthName = 0x00000400,
        CaptureOffset = 0x00000800,
        YearDefault = 0x00001000,
        Rfc1123Pattern = 0x00002000,
        UtcSortPattern = 0x00004000
    }

    internal struct DateTimeResult
    {
        internal int Year;
        internal int Month;
        internal int Day;
        internal int Hour;
        internal int Minute;
        internal int Second;
        internal double fraction;
        internal int era;
        internal ParseFlags flags;
        internal TimeSpan timeZoneOffset;
        internal Calendar calendar;
        internal DateTime parsedDate;
        internal ParseFailureKind failure;
        internal string failureMessageID;
        internal object failureMessageFormatArgument;
        internal string failureArgumentName;
        internal void Init()
        {
            Year = -1;
            Month = -1;
            Day = -1;
            fraction = -1;
            era = -1;
        }

        internal void SetDate(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument)
        {
            this.failure = failure;
            this.failureMessageID = failureMessageID;
            this.failureMessageFormatArgument = failureMessageFormatArgument;
        }

        internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument, string failureArgumentName)
        {
            this.failure = failure;
            this.failureMessageID = failureMessageID;
            this.failureMessageFormatArgument = failureMessageFormatArgument;
            this.failureArgumentName = failureArgumentName;
        }
    }

    internal struct ParsingInfo
    {
        internal Calendar calendar;
        internal int dayOfWeek;
        internal DateTimeParse.TM timeMark;
        internal bool fUseHour12;
        internal bool fUseTwoDigitYear;
        internal bool fAllowInnerWhite;
        internal bool fAllowTrailingWhite;
        internal bool fCustomNumberParser;
        internal DateTimeParse.MatchNumberDelegate parseNumberDelegate;
        internal void Init()
        {
            dayOfWeek = -1;
            timeMark = DateTimeParse.TM.NotSet;
        }
    }

    internal enum TokenType
    {
        NumberToken = 1,
        YearNumberToken = 2,
        Am = 3,
        Pm = 4,
        MonthToken = 5,
        EndOfString = 6,
        DayOfWeekToken = 7,
        TimeZoneToken = 8,
        EraToken = 9,
        DateWordToken = 10,
        UnknownToken = 11,
        HebrewNumber = 12,
        JapaneseEraToken = 13,
        TEraToken = 14,
        IgnorableSymbol = 15,
        SEP_Unk = 0x100,
        SEP_End = 0x200,
        SEP_Space = 0x300,
        SEP_Am = 0x400,
        SEP_Pm = 0x500,
        SEP_Date = 0x600,
        SEP_Time = 0x700,
        SEP_YearSuff = 0x800,
        SEP_MonthSuff = 0x900,
        SEP_DaySuff = 0xa00,
        SEP_HourSuff = 0xb00,
        SEP_MinuteSuff = 0xc00,
        SEP_SecondSuff = 0xd00,
        SEP_LocalTimeMark = 0xe00,
        SEP_DateOrOffset = 0xf00,
        RegularTokenMask = 0x00ff,
        SeparatorTokenMask = 0xff00
    }
}