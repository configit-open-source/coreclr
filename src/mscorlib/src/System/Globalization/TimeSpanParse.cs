using System.Diagnostics.Contracts;
using System.Text;

namespace System.Globalization
{
    internal static class TimeSpanParse
    {
        internal static void ValidateStyles(TimeSpanStyles style, String parameterName)
        {
            if (style != TimeSpanStyles.None && style != TimeSpanStyles.AssumeNegative)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTimeSpanStyles"), parameterName);
        }

        internal const int unlimitedDigits = -1;
        internal const int maxFractionDigits = 7;
        internal const int maxDays = 10675199;
        internal const int maxHours = 23;
        internal const int maxMinutes = 59;
        internal const int maxSeconds = 59;
        internal const int maxFraction = 9999999;
        enum TimeSpanThrowStyle
        {
            None = 0,
            All = 1
        }

        private enum ParseFailureKind
        {
            None = 0,
            ArgumentNull = 1,
            Format = 2,
            FormatWithParameter = 3,
            Overflow = 4
        }

        [Flags]
        enum TimeSpanStandardStyles
        {
            None = 0x00000000,
            Invariant = 0x00000001,
            Localized = 0x00000002,
            RequireFull = 0x00000004,
            Any = Invariant | Localized
        }

        private enum TTT
        {
            None = 0,
            End = 1,
            Num = 2,
            Sep = 3,
            NumOverflow = 4
        }

        private static readonly TimeSpanToken zero = new TimeSpanToken(0);
        struct TimeSpanToken
        {
            internal TTT ttt;
            internal int num;
            internal int zeroes;
            internal String sep;
            public TimeSpanToken(int number)
            {
                ttt = TTT.Num;
                num = number;
                zeroes = 0;
                sep = null;
            }

            public TimeSpanToken(int leadingZeroes, int number)
            {
                ttt = TTT.Num;
                num = number;
                zeroes = leadingZeroes;
                sep = null;
            }

            public bool IsInvalidNumber(int maxValue, int maxPrecision)
            {
                Contract.Assert(ttt == TTT.Num);
                Contract.Assert(num > -1);
                Contract.Assert(maxValue > 0);
                Contract.Assert(maxPrecision == maxFractionDigits || maxPrecision == unlimitedDigits);
                if (num > maxValue)
                    return true;
                if (maxPrecision == unlimitedDigits)
                    return false;
                if (zeroes > maxPrecision)
                    return true;
                if (num == 0 || zeroes == 0)
                    return false;
                return (num >= (maxValue / (long)Math.Pow(10, zeroes - 1)));
            }
        }

        struct TimeSpanTokenizer
        {
            private int m_pos;
            private String m_value;
            internal void Init(String input)
            {
                Init(input, 0);
            }

            internal void Init(String input, int startPosition)
            {
                m_pos = startPosition;
                m_value = input;
            }

            internal TimeSpanToken GetNextToken()
            {
                Contract.Assert(m_pos > -1);
                TimeSpanToken tok = new TimeSpanToken();
                char ch = CurrentChar;
                if (ch == (char)0)
                {
                    tok.ttt = TTT.End;
                    return tok;
                }

                if (ch >= '0' && ch <= '9')
                {
                    tok.ttt = TTT.Num;
                    tok.num = 0;
                    tok.zeroes = 0;
                    do
                    {
                        if ((tok.num & 0xF0000000) != 0)
                        {
                            tok.ttt = TTT.NumOverflow;
                            return tok;
                        }

                        tok.num = tok.num * 10 + ch - '0';
                        if (tok.num == 0)
                            tok.zeroes++;
                        if (tok.num < 0)
                        {
                            tok.ttt = TTT.NumOverflow;
                            return tok;
                        }

                        ch = NextChar;
                    }
                    while (ch >= '0' && ch <= '9');
                    return tok;
                }
                else
                {
                    tok.ttt = TTT.Sep;
                    int startIndex = m_pos;
                    int length = 0;
                    while (ch != (char)0 && (ch < '0' || '9' < ch))
                    {
                        ch = NextChar;
                        length++;
                    }

                    tok.sep = m_value.Substring(startIndex, length);
                    return tok;
                }
            }

            internal Boolean EOL
            {
                get
                {
                    return m_pos >= (m_value.Length - 1);
                }
            }

            internal void BackOne()
            {
                if (m_pos > 0)
                    --m_pos;
            }

            internal char NextChar
            {
                get
                {
                    m_pos++;
                    return CurrentChar;
                }
            }

            internal char CurrentChar
            {
                get
                {
                    if (m_pos > -1 && m_pos < m_value.Length)
                    {
                        return m_value[m_pos];
                    }
                    else
                    {
                        return (char)0;
                    }
                }
            }
        }

        struct TimeSpanRawInfo
        {
            internal TimeSpanFormat.FormatLiterals PositiveInvariant
            {
                get
                {
                    return TimeSpanFormat.PositiveInvariantFormatLiterals;
                }
            }

            internal TimeSpanFormat.FormatLiterals NegativeInvariant
            {
                get
                {
                    return TimeSpanFormat.NegativeInvariantFormatLiterals;
                }
            }

            internal TimeSpanFormat.FormatLiterals PositiveLocalized
            {
                get
                {
                    if (!m_posLocInit)
                    {
                        m_posLoc = new TimeSpanFormat.FormatLiterals();
                        m_posLoc.Init(m_fullPosPattern, false);
                        m_posLocInit = true;
                    }

                    return m_posLoc;
                }
            }

            internal TimeSpanFormat.FormatLiterals NegativeLocalized
            {
                get
                {
                    if (!m_negLocInit)
                    {
                        m_negLoc = new TimeSpanFormat.FormatLiterals();
                        m_negLoc.Init(m_fullNegPattern, false);
                        m_negLocInit = true;
                    }

                    return m_negLoc;
                }
            }

            internal Boolean FullAppCompatMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == 5 && NumCount == 4 && pattern.Start == literals[0] && pattern.DayHourSep == literals[1] && pattern.HourMinuteSep == literals[2] && pattern.AppCompatLiteral == literals[3] && pattern.End == literals[4];
            }

            internal Boolean PartialAppCompatMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == 4 && NumCount == 3 && pattern.Start == literals[0] && pattern.HourMinuteSep == literals[1] && pattern.AppCompatLiteral == literals[2] && pattern.End == literals[3];
            }

            internal Boolean FullMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == MaxLiteralTokens && NumCount == MaxNumericTokens && pattern.Start == literals[0] && pattern.DayHourSep == literals[1] && pattern.HourMinuteSep == literals[2] && pattern.MinuteSecondSep == literals[3] && pattern.SecondFractionSep == literals[4] && pattern.End == literals[5];
            }

            internal Boolean FullDMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == 2 && NumCount == 1 && pattern.Start == literals[0] && pattern.End == literals[1];
            }

            internal Boolean FullHMMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == 3 && NumCount == 2 && pattern.Start == literals[0] && pattern.HourMinuteSep == literals[1] && pattern.End == literals[2];
            }

            internal Boolean FullDHMMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == 4 && NumCount == 3 && pattern.Start == literals[0] && pattern.DayHourSep == literals[1] && pattern.HourMinuteSep == literals[2] && pattern.End == literals[3];
            }

            internal Boolean FullHMSMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == 4 && NumCount == 3 && pattern.Start == literals[0] && pattern.HourMinuteSep == literals[1] && pattern.MinuteSecondSep == literals[2] && pattern.End == literals[3];
            }

            internal Boolean FullDHMSMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == 5 && NumCount == 4 && pattern.Start == literals[0] && pattern.DayHourSep == literals[1] && pattern.HourMinuteSep == literals[2] && pattern.MinuteSecondSep == literals[3] && pattern.End == literals[4];
            }

            internal Boolean FullHMSFMatch(TimeSpanFormat.FormatLiterals pattern)
            {
                return SepCount == 5 && NumCount == 4 && pattern.Start == literals[0] && pattern.HourMinuteSep == literals[1] && pattern.MinuteSecondSep == literals[2] && pattern.SecondFractionSep == literals[3] && pattern.End == literals[4];
            }

            internal TTT lastSeenTTT;
            internal int tokenCount;
            internal int SepCount;
            internal int NumCount;
            internal String[] literals;
            internal TimeSpanToken[] numbers;
            private TimeSpanFormat.FormatLiterals m_posLoc;
            private TimeSpanFormat.FormatLiterals m_negLoc;
            private Boolean m_posLocInit;
            private Boolean m_negLocInit;
            private String m_fullPosPattern;
            private String m_fullNegPattern;
            private const int MaxTokens = 11;
            private const int MaxLiteralTokens = 6;
            private const int MaxNumericTokens = 5;
            internal void Init(DateTimeFormatInfo dtfi)
            {
                Contract.Assert(dtfi != null);
                lastSeenTTT = TTT.None;
                tokenCount = 0;
                SepCount = 0;
                NumCount = 0;
                literals = new String[MaxLiteralTokens];
                numbers = new TimeSpanToken[MaxNumericTokens];
                m_fullPosPattern = dtfi.FullTimeSpanPositivePattern;
                m_fullNegPattern = dtfi.FullTimeSpanNegativePattern;
                m_posLocInit = false;
                m_negLocInit = false;
            }

            internal Boolean ProcessToken(ref TimeSpanToken tok, ref TimeSpanResult result)
            {
                if (tok.ttt == TTT.NumOverflow)
                {
                    result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge", null);
                    return false;
                }

                if (tok.ttt != TTT.Sep && tok.ttt != TTT.Num)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan", null);
                    return false;
                }

                switch (tok.ttt)
                {
                    case TTT.Sep:
                        if (!AddSep(tok.sep, ref result))
                            return false;
                        break;
                    case TTT.Num:
                        if (tokenCount == 0)
                        {
                            if (!AddSep(String.Empty, ref result))
                                return false;
                        }

                        if (!AddNum(tok, ref result))
                            return false;
                        break;
                    default:
                        break;
                }

                lastSeenTTT = tok.ttt;
                Contract.Assert(tokenCount == (SepCount + NumCount), "tokenCount == (SepCount + NumCount)");
                return true;
            }

            private bool AddSep(String sep, ref TimeSpanResult result)
            {
                if (SepCount >= MaxLiteralTokens || tokenCount >= MaxTokens)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan", null);
                    return false;
                }

                literals[SepCount++] = sep;
                tokenCount++;
                return true;
            }

            private bool AddNum(TimeSpanToken num, ref TimeSpanResult result)
            {
                if (NumCount >= MaxNumericTokens || tokenCount >= MaxTokens)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan", null);
                    return false;
                }

                numbers[NumCount++] = num;
                tokenCount++;
                return true;
            }
        }

        struct TimeSpanResult
        {
            internal TimeSpan parsedTimeSpan;
            internal TimeSpanThrowStyle throwStyle;
            internal ParseFailureKind m_failure;
            internal string m_failureMessageID;
            internal object m_failureMessageFormatArgument;
            internal string m_failureArgumentName;
            internal void Init(TimeSpanThrowStyle canThrow)
            {
                parsedTimeSpan = default (TimeSpan);
                throwStyle = canThrow;
            }

            internal void SetFailure(ParseFailureKind failure, string failureMessageID)
            {
                SetFailure(failure, failureMessageID, null, null);
            }

            internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument)
            {
                SetFailure(failure, failureMessageID, failureMessageFormatArgument, null);
            }

            internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument, string failureArgumentName)
            {
                m_failure = failure;
                m_failureMessageID = failureMessageID;
                m_failureMessageFormatArgument = failureMessageFormatArgument;
                m_failureArgumentName = failureArgumentName;
                if (throwStyle != TimeSpanThrowStyle.None)
                {
                    throw GetTimeSpanParseException();
                }
            }

            internal Exception GetTimeSpanParseException()
            {
                switch (m_failure)
                {
                    case ParseFailureKind.ArgumentNull:
                        return new ArgumentNullException(m_failureArgumentName, Environment.GetResourceString(m_failureMessageID));
                    case ParseFailureKind.FormatWithParameter:
                        return new FormatException(Environment.GetResourceString(m_failureMessageID, m_failureMessageFormatArgument));
                    case ParseFailureKind.Format:
                        return new FormatException(Environment.GetResourceString(m_failureMessageID));
                    case ParseFailureKind.Overflow:
                        return new OverflowException(Environment.GetResourceString(m_failureMessageID));
                    default:
                        Contract.Assert(false, "Unknown TimeSpanParseFailure: " + m_failure);
                        return new FormatException(Environment.GetResourceString("Format_InvalidString"));
                }
            }
        }

        static bool TryTimeToTicks(bool positive, TimeSpanToken days, TimeSpanToken hours, TimeSpanToken minutes, TimeSpanToken seconds, TimeSpanToken fraction, out long result)
        {
            if (days.IsInvalidNumber(maxDays, unlimitedDigits) || hours.IsInvalidNumber(maxHours, unlimitedDigits) || minutes.IsInvalidNumber(maxMinutes, unlimitedDigits) || seconds.IsInvalidNumber(maxSeconds, unlimitedDigits) || fraction.IsInvalidNumber(maxFraction, maxFractionDigits))
            {
                result = 0;
                return false;
            }

            Int64 ticks = ((Int64)days.num * 3600 * 24 + (Int64)hours.num * 3600 + (Int64)minutes.num * 60 + seconds.num) * 1000;
            if (ticks > TimeSpan.MaxMilliSeconds || ticks < TimeSpan.MinMilliSeconds)
            {
                result = 0;
                return false;
            }

            long f = fraction.num;
            if (f != 0)
            {
                long lowerLimit = TimeSpan.TicksPerTenthSecond;
                if (fraction.zeroes > 0)
                {
                    long divisor = (long)Math.Pow(10, fraction.zeroes);
                    lowerLimit = lowerLimit / divisor;
                }

                while (f < lowerLimit)
                {
                    f *= 10;
                }
            }

            result = ((long)ticks * TimeSpan.TicksPerMillisecond) + f;
            if (positive && result < 0)
            {
                result = 0;
                return false;
            }

            return true;
        }

        internal static TimeSpan Parse(String input, IFormatProvider formatProvider)
        {
            TimeSpanResult parseResult = new TimeSpanResult();
            parseResult.Init(TimeSpanThrowStyle.All);
            if (TryParseTimeSpan(input, TimeSpanStandardStyles.Any, formatProvider, ref parseResult))
            {
                return parseResult.parsedTimeSpan;
            }
            else
            {
                throw parseResult.GetTimeSpanParseException();
            }
        }

        internal static Boolean TryParse(String input, IFormatProvider formatProvider, out TimeSpan result)
        {
            TimeSpanResult parseResult = new TimeSpanResult();
            parseResult.Init(TimeSpanThrowStyle.None);
            if (TryParseTimeSpan(input, TimeSpanStandardStyles.Any, formatProvider, ref parseResult))
            {
                result = parseResult.parsedTimeSpan;
                return true;
            }
            else
            {
                result = default (TimeSpan);
                return false;
            }
        }

        internal static TimeSpan ParseExact(String input, String format, IFormatProvider formatProvider, TimeSpanStyles styles)
        {
            TimeSpanResult parseResult = new TimeSpanResult();
            parseResult.Init(TimeSpanThrowStyle.All);
            if (TryParseExactTimeSpan(input, format, formatProvider, styles, ref parseResult))
            {
                return parseResult.parsedTimeSpan;
            }
            else
            {
                throw parseResult.GetTimeSpanParseException();
            }
        }

        internal static Boolean TryParseExact(String input, String format, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
        {
            TimeSpanResult parseResult = new TimeSpanResult();
            parseResult.Init(TimeSpanThrowStyle.None);
            if (TryParseExactTimeSpan(input, format, formatProvider, styles, ref parseResult))
            {
                result = parseResult.parsedTimeSpan;
                return true;
            }
            else
            {
                result = default (TimeSpan);
                return false;
            }
        }

        internal static TimeSpan ParseExactMultiple(String input, String[] formats, IFormatProvider formatProvider, TimeSpanStyles styles)
        {
            TimeSpanResult parseResult = new TimeSpanResult();
            parseResult.Init(TimeSpanThrowStyle.All);
            if (TryParseExactMultipleTimeSpan(input, formats, formatProvider, styles, ref parseResult))
            {
                return parseResult.parsedTimeSpan;
            }
            else
            {
                throw parseResult.GetTimeSpanParseException();
            }
        }

        internal static Boolean TryParseExactMultiple(String input, String[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
        {
            TimeSpanResult parseResult = new TimeSpanResult();
            parseResult.Init(TimeSpanThrowStyle.None);
            if (TryParseExactMultipleTimeSpan(input, formats, formatProvider, styles, ref parseResult))
            {
                result = parseResult.parsedTimeSpan;
                return true;
            }
            else
            {
                result = default (TimeSpan);
                return false;
            }
        }

        private static Boolean TryParseTimeSpan(String input, TimeSpanStandardStyles style, IFormatProvider formatProvider, ref TimeSpanResult result)
        {
            if (input == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "input");
                return false;
            }

            input = input.Trim();
            if (input == String.Empty)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            TimeSpanTokenizer tokenizer = new TimeSpanTokenizer();
            tokenizer.Init(input);
            TimeSpanRawInfo raw = new TimeSpanRawInfo();
            raw.Init(DateTimeFormatInfo.GetInstance(formatProvider));
            TimeSpanToken tok = tokenizer.GetNextToken();
            while (tok.ttt != TTT.End)
            {
                if (!raw.ProcessToken(ref tok, ref result))
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                    return false;
                }

                tok = tokenizer.GetNextToken();
            }

            if (!tokenizer.EOL)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            if (!ProcessTerminalState(ref raw, style, ref result))
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            return true;
        }

        private static Boolean ProcessTerminalState(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw.lastSeenTTT == TTT.Num)
            {
                TimeSpanToken tok = new TimeSpanToken();
                tok.ttt = TTT.Sep;
                tok.sep = String.Empty;
                if (!raw.ProcessToken(ref tok, ref result))
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                    return false;
                }
            }

            switch (raw.NumCount)
            {
                case 1:
                    return ProcessTerminal_D(ref raw, style, ref result);
                case 2:
                    return ProcessTerminal_HM(ref raw, style, ref result);
                case 3:
                    return ProcessTerminal_HM_S_D(ref raw, style, ref result);
                case 4:
                    return ProcessTerminal_HMS_F_D(ref raw, style, ref result);
                case 5:
                    return ProcessTerminal_DHMSF(ref raw, style, ref result);
                default:
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                    return false;
            }
        }

        private static Boolean ProcessTerminal_DHMSF(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw.SepCount != 6 || raw.NumCount != 5)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);
            bool positive = false;
            bool match = false;
            if (inv)
            {
                if (raw.FullMatch(raw.PositiveInvariant))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullMatch(raw.NegativeInvariant))
                {
                    match = true;
                    positive = false;
                }
            }

            if (loc)
            {
                if (!match && raw.FullMatch(raw.PositiveLocalized))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullMatch(raw.NegativeLocalized))
                {
                    match = true;
                    positive = false;
                }
            }

            long ticks;
            if (match)
            {
                if (!TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], raw.numbers[4], out ticks))
                {
                    result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                    return false;
                }

                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
            return false;
        }

        private static Boolean ProcessTerminal_HMS_F_D(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw.SepCount != 5 || raw.NumCount != 4 || (style & TimeSpanStandardStyles.RequireFull) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);
            long ticks = 0;
            bool positive = false;
            bool match = false;
            bool overflow = false;
            if (inv)
            {
                if (raw.FullHMSFMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMSMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullAppCompatMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, raw.numbers[3], out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullHMSFMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMSMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullAppCompatMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, raw.numbers[3], out ticks);
                    overflow = overflow || !match;
                }
            }

            if (loc)
            {
                if (!match && raw.FullHMSFMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMSMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullAppCompatMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, raw.numbers[3], out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullHMSFMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMSMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], raw.numbers[3], zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullAppCompatMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, raw.numbers[3], out ticks);
                    overflow = overflow || !match;
                }
            }

            if (match)
            {
                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            if (overflow)
            {
                result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                return false;
            }
            else
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }
        }

        private static Boolean ProcessTerminal_HM_S_D(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw.SepCount != 4 || raw.NumCount != 3 || (style & TimeSpanStandardStyles.RequireFull) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);
            bool positive = false;
            bool match = false;
            bool overflow = false;
            long ticks = 0;
            if (inv)
            {
                if (raw.FullHMSMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.PartialAppCompatMatch(raw.PositiveInvariant))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], zero, raw.numbers[2], out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullHMSMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.PartialAppCompatMatch(raw.NegativeInvariant))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], zero, raw.numbers[2], out ticks);
                    overflow = overflow || !match;
                }
            }

            if (loc)
            {
                if (!match && raw.FullHMSMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.PartialAppCompatMatch(raw.PositiveLocalized))
                {
                    positive = true;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], zero, raw.numbers[2], out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullHMSMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.FullDHMMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, raw.numbers[0], raw.numbers[1], raw.numbers[2], zero, zero, out ticks);
                    overflow = overflow || !match;
                }

                if (!match && raw.PartialAppCompatMatch(raw.NegativeLocalized))
                {
                    positive = false;
                    match = TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], zero, raw.numbers[2], out ticks);
                    overflow = overflow || !match;
                }
            }

            if (match)
            {
                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            if (overflow)
            {
                result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                return false;
            }
            else
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }
        }

        private static Boolean ProcessTerminal_HM(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw.SepCount != 3 || raw.NumCount != 2 || (style & TimeSpanStandardStyles.RequireFull) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);
            bool positive = false;
            bool match = false;
            if (inv)
            {
                if (raw.FullHMMatch(raw.PositiveInvariant))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullHMMatch(raw.NegativeInvariant))
                {
                    match = true;
                    positive = false;
                }
            }

            if (loc)
            {
                if (!match && raw.FullHMMatch(raw.PositiveLocalized))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullHMMatch(raw.NegativeLocalized))
                {
                    match = true;
                    positive = false;
                }
            }

            long ticks = 0;
            if (match)
            {
                if (!TryTimeToTicks(positive, zero, raw.numbers[0], raw.numbers[1], zero, zero, out ticks))
                {
                    result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                    return false;
                }

                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
            return false;
        }

        private static Boolean ProcessTerminal_D(ref TimeSpanRawInfo raw, TimeSpanStandardStyles style, ref TimeSpanResult result)
        {
            if (raw.SepCount != 2 || raw.NumCount != 1 || (style & TimeSpanStandardStyles.RequireFull) != 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            bool inv = ((style & TimeSpanStandardStyles.Invariant) != 0);
            bool loc = ((style & TimeSpanStandardStyles.Localized) != 0);
            bool positive = false;
            bool match = false;
            if (inv)
            {
                if (raw.FullDMatch(raw.PositiveInvariant))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullDMatch(raw.NegativeInvariant))
                {
                    match = true;
                    positive = false;
                }
            }

            if (loc)
            {
                if (!match && raw.FullDMatch(raw.PositiveLocalized))
                {
                    match = true;
                    positive = true;
                }

                if (!match && raw.FullDMatch(raw.NegativeLocalized))
                {
                    match = true;
                    positive = false;
                }
            }

            long ticks = 0;
            if (match)
            {
                if (!TryTimeToTicks(positive, raw.numbers[0], zero, zero, zero, zero, out ticks))
                {
                    result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                    return false;
                }

                if (!positive)
                {
                    ticks = -ticks;
                    if (ticks > 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }
                }

                result.parsedTimeSpan._ticks = ticks;
                return true;
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
            return false;
        }

        private static Boolean TryParseExactTimeSpan(String input, String format, IFormatProvider formatProvider, TimeSpanStyles styles, ref TimeSpanResult result)
        {
            if (input == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "input");
                return false;
            }

            if (format == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "format");
                return false;
            }

            if (format.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier");
                return false;
            }

            if (format.Length == 1)
            {
                TimeSpanStandardStyles style = TimeSpanStandardStyles.None;
                if (format[0] == 'c' || format[0] == 't' || format[0] == 'T')
                {
                    return TryParseTimeSpanConstant(input, ref result);
                }
                else if (format[0] == 'g')
                {
                    style = TimeSpanStandardStyles.Localized;
                }
                else if (format[0] == 'G')
                {
                    style = TimeSpanStandardStyles.Localized | TimeSpanStandardStyles.RequireFull;
                }
                else
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier");
                    return false;
                }

                return TryParseTimeSpan(input, style, formatProvider, ref result);
            }

            return TryParseByFormat(input, format, styles, ref result);
        }

        private static Boolean TryParseByFormat(String input, String format, TimeSpanStyles styles, ref TimeSpanResult result)
        {
            Contract.Assert(input != null, "input != null");
            Contract.Assert(format != null, "format != null");
            bool seenDD = false;
            bool seenHH = false;
            bool seenMM = false;
            bool seenSS = false;
            bool seenFF = false;
            int dd = 0;
            int hh = 0;
            int mm = 0;
            int ss = 0;
            int leadingZeroes = 0;
            int ff = 0;
            int i = 0;
            int tokenLen = 0;
            TimeSpanTokenizer tokenizer = new TimeSpanTokenizer();
            tokenizer.Init(input, -1);
            while (i < format.Length)
            {
                char ch = format[i];
                int nextFormatChar;
                switch (ch)
                {
                    case 'h':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2 || seenHH || !ParseExactDigits(ref tokenizer, tokenLen, out hh))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                        seenHH = true;
                        break;
                    case 'm':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2 || seenMM || !ParseExactDigits(ref tokenizer, tokenLen, out mm))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                        seenMM = true;
                        break;
                    case 's':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2 || seenSS || !ParseExactDigits(ref tokenizer, tokenLen, out ss))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                        seenSS = true;
                        break;
                    case 'f':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits || seenFF || !ParseExactDigits(ref tokenizer, tokenLen, tokenLen, out leadingZeroes, out ff))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                        seenFF = true;
                        break;
                    case 'F':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits || seenFF)
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                        ParseExactDigits(ref tokenizer, tokenLen, tokenLen, out leadingZeroes, out ff);
                        seenFF = true;
                        break;
                    case 'd':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        int tmp = 0;
                        if (tokenLen > 8 || seenDD || !ParseExactDigits(ref tokenizer, (tokenLen < 2) ? 1 : tokenLen, (tokenLen < 2) ? 8 : tokenLen, out tmp, out dd))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                        seenDD = true;
                        break;
                    case '\'':
                    case '\"':
                        StringBuilder enquotedString = new StringBuilder();
                        if (!DateTimeParse.TryParseQuoteString(format, i, enquotedString, out tokenLen))
                        {
                            result.SetFailure(ParseFailureKind.FormatWithParameter, "Format_BadQuote", ch);
                            return false;
                        }

                        if (!ParseExactLiteral(ref tokenizer, enquotedString))
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                        break;
                    case '%':
                        nextFormatChar = DateTimeFormat.ParseNextChar(format, i);
                        if (nextFormatChar >= 0 && nextFormatChar != (int)'%')
                        {
                            tokenLen = 1;
                            break;
                        }
                        else
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                    case '\\':
                        nextFormatChar = DateTimeFormat.ParseNextChar(format, i);
                        if (nextFormatChar >= 0 && tokenizer.NextChar == (char)nextFormatChar)
                        {
                            tokenLen = 2;
                        }
                        else
                        {
                            result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                            return false;
                        }

                        break;
                    default:
                        result.SetFailure(ParseFailureKind.Format, "Format_InvalidString");
                        return false;
                }

                i += tokenLen;
            }

            if (!tokenizer.EOL)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            long ticks = 0;
            bool positive = (styles & TimeSpanStyles.AssumeNegative) == 0;
            if (TryTimeToTicks(positive, new TimeSpanToken(dd), new TimeSpanToken(hh), new TimeSpanToken(mm), new TimeSpanToken(ss), new TimeSpanToken(leadingZeroes, ff), out ticks))
            {
                if (!positive)
                    ticks = -ticks;
                result.parsedTimeSpan._ticks = ticks;
                return true;
            }
            else
            {
                result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                return false;
            }
        }

        private static Boolean ParseExactDigits(ref TimeSpanTokenizer tokenizer, int minDigitLength, out int result)
        {
            result = 0;
            int zeroes = 0;
            int maxDigitLength = (minDigitLength == 1) ? 2 : minDigitLength;
            return ParseExactDigits(ref tokenizer, minDigitLength, maxDigitLength, out zeroes, out result);
        }

        private static Boolean ParseExactDigits(ref TimeSpanTokenizer tokenizer, int minDigitLength, int maxDigitLength, out int zeroes, out int result)
        {
            result = 0;
            zeroes = 0;
            int tokenLength = 0;
            while (tokenLength < maxDigitLength)
            {
                char ch = tokenizer.NextChar;
                if (ch < '0' || ch > '9')
                {
                    tokenizer.BackOne();
                    break;
                }

                result = result * 10 + (ch - '0');
                if (result == 0)
                    zeroes++;
                tokenLength++;
            }

            return (tokenLength >= minDigitLength);
        }

        private static Boolean ParseExactLiteral(ref TimeSpanTokenizer tokenizer, StringBuilder enquotedString)
        {
            for (int i = 0; i < enquotedString.Length; i++)
            {
                if (enquotedString[i] != tokenizer.NextChar)
                    return false;
            }

            return true;
        }

        private static Boolean TryParseTimeSpanConstant(String input, ref TimeSpanResult result)
        {
            return (new StringParser().TryParse(input, ref result));
        }

        private struct StringParser
        {
            private String str;
            private char ch;
            private int pos;
            private int len;
            internal void NextChar()
            {
                if (pos < len)
                    pos++;
                ch = pos < len ? str[pos] : (char)0;
            }

            internal char NextNonDigit()
            {
                int i = pos;
                while (i < len)
                {
                    char ch = str[i];
                    if (ch < '0' || ch > '9')
                        return ch;
                    i++;
                }

                return (char)0;
            }

            internal bool TryParse(String input, ref TimeSpanResult result)
            {
                result.parsedTimeSpan._ticks = 0;
                if (input == null)
                {
                    result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "input");
                    return false;
                }

                str = input;
                len = input.Length;
                pos = -1;
                NextChar();
                SkipBlanks();
                bool negative = false;
                if (ch == '-')
                {
                    negative = true;
                    NextChar();
                }

                long time;
                if (NextNonDigit() == ':')
                {
                    if (!ParseTime(out time, ref result))
                    {
                        return false;
                    }

                    ;
                }
                else
                {
                    int days;
                    if (!ParseInt((int)(0x7FFFFFFFFFFFFFFFL / TimeSpan.TicksPerDay), out days, ref result))
                    {
                        return false;
                    }

                    time = days * TimeSpan.TicksPerDay;
                    if (ch == '.')
                    {
                        NextChar();
                        long remainingTime;
                        if (!ParseTime(out remainingTime, ref result))
                        {
                            return false;
                        }

                        ;
                        time += remainingTime;
                    }
                }

                if (negative)
                {
                    time = -time;
                    if (time > 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }
                }
                else
                {
                    if (time < 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }
                }

                SkipBlanks();
                if (pos < len)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                    return false;
                }

                result.parsedTimeSpan._ticks = time;
                return true;
            }

            internal bool ParseInt(int max, out int i, ref TimeSpanResult result)
            {
                i = 0;
                int p = pos;
                while (ch >= '0' && ch <= '9')
                {
                    if ((i & 0xF0000000) != 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }

                    i = i * 10 + ch - '0';
                    if (i < 0)
                    {
                        result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                        return false;
                    }

                    NextChar();
                }

                if (p == pos)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                    return false;
                }

                if (i > max)
                {
                    result.SetFailure(ParseFailureKind.Overflow, "Overflow_TimeSpanElementTooLarge");
                    return false;
                }

                return true;
            }

            internal bool ParseTime(out long time, ref TimeSpanResult result)
            {
                time = 0;
                int unit;
                if (!ParseInt(23, out unit, ref result))
                {
                    return false;
                }

                time = unit * TimeSpan.TicksPerHour;
                if (ch != ':')
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                    return false;
                }

                NextChar();
                if (!ParseInt(59, out unit, ref result))
                {
                    return false;
                }

                time += unit * TimeSpan.TicksPerMinute;
                if (ch == ':')
                {
                    NextChar();
                    if (ch != '.')
                    {
                        if (!ParseInt(59, out unit, ref result))
                        {
                            return false;
                        }

                        time += unit * TimeSpan.TicksPerSecond;
                    }

                    if (ch == '.')
                    {
                        NextChar();
                        int f = (int)TimeSpan.TicksPerSecond;
                        while (f > 1 && ch >= '0' && ch <= '9')
                        {
                            f /= 10;
                            time += (ch - '0') * f;
                            NextChar();
                        }
                    }
                }

                return true;
            }

            internal void SkipBlanks()
            {
                while (ch == ' ' || ch == '\t')
                    NextChar();
            }
        }

        private static Boolean TryParseExactMultipleTimeSpan(String input, String[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, ref TimeSpanResult result)
        {
            if (input == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "input");
                return false;
            }

            if (formats == null)
            {
                result.SetFailure(ParseFailureKind.ArgumentNull, "ArgumentNull_String", null, "formats");
                return false;
            }

            if (input.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
                return false;
            }

            if (formats.Length == 0)
            {
                result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier");
                return false;
            }

            for (int i = 0; i < formats.Length; i++)
            {
                if (formats[i] == null || formats[i].Length == 0)
                {
                    result.SetFailure(ParseFailureKind.Format, "Format_BadFormatSpecifier");
                    return false;
                }

                TimeSpanResult innerResult = new TimeSpanResult();
                innerResult.Init(TimeSpanThrowStyle.None);
                if (TryParseExactTimeSpan(input, formats[i], formatProvider, styles, ref innerResult))
                {
                    result.parsedTimeSpan = innerResult.parsedTimeSpan;
                    return true;
                }
            }

            result.SetFailure(ParseFailureKind.Format, "Format_BadTimeSpan");
            return (false);
        }
    }
}