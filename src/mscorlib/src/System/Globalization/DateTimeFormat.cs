using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace System
{
    internal static class DateTimeFormat
    {
        internal const int MaxSecondsFractionDigits = 7;
        internal static readonly TimeSpan NullOffset = TimeSpan.MinValue;
        internal static char[] allStandardFormats = {'d', 'D', 'f', 'F', 'g', 'G', 'm', 'M', 'o', 'O', 'r', 'R', 's', 't', 'T', 'u', 'U', 'y', 'Y', };
        internal const String RoundtripFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";
        internal const String RoundtripDateTimeUnfixed = "yyyy'-'MM'-'ddTHH':'mm':'ss zzz";
        private const int DEFAULT_ALL_DATETIMES_SIZE = 132;
        internal static String[] fixedNumberFormats = new String[]{"0", "00", "000", "0000", "00000", "000000", "0000000", };
        internal static void FormatDigits(StringBuilder outputBuffer, int value, int len)
        {
                        FormatDigits(outputBuffer, value, len, false);
        }

        internal unsafe static void FormatDigits(StringBuilder outputBuffer, int value, int len, bool overrideLengthLimit)
        {
                        if (!overrideLengthLimit && len > 2)
            {
                len = 2;
            }

            char *buffer = stackalloc char[16];
            char *p = buffer + 16;
            int n = value;
            do
            {
                *--p = (char)(n % 10 + '0');
                n /= 10;
            }
            while ((n != 0) && (p > buffer));
            int digits = (int)(buffer + 16 - p);
            while ((digits < len) && (p > buffer))
            {
                *--p = '0';
                digits++;
            }

            outputBuffer.Append(p, digits);
        }

        private static void HebrewFormatDigits(StringBuilder outputBuffer, int digits)
        {
            outputBuffer.Append(HebrewNumber.ToString(digits));
        }

        internal static int ParseRepeatPattern(String format, int pos, char patternChar)
        {
            int len = format.Length;
            int index = pos + 1;
            while ((index < len) && (format[index] == patternChar))
            {
                index++;
            }

            return (index - pos);
        }

        private static String FormatDayOfWeek(int dayOfWeek, int repeat, DateTimeFormatInfo dtfi)
        {
                        if (repeat == 3)
            {
                return (dtfi.GetAbbreviatedDayName((DayOfWeek)dayOfWeek));
            }

            return (dtfi.GetDayName((DayOfWeek)dayOfWeek));
        }

        private static String FormatMonth(int month, int repeatCount, DateTimeFormatInfo dtfi)
        {
                        if (repeatCount == 3)
            {
                return (dtfi.GetAbbreviatedMonthName(month));
            }

            return (dtfi.GetMonthName(month));
        }

        private static String FormatHebrewMonthName(DateTime time, int month, int repeatCount, DateTimeFormatInfo dtfi)
        {
                        if (dtfi.Calendar.IsLeapYear(dtfi.Calendar.GetYear(time)))
            {
                return (dtfi.internalGetMonthName(month, MonthNameStyles.LeapYear, (repeatCount == 3)));
            }

            if (month >= 7)
            {
                month++;
            }

            if (repeatCount == 3)
            {
                return (dtfi.GetAbbreviatedMonthName(month));
            }

            return (dtfi.GetMonthName(month));
        }

        internal static int ParseQuoteString(String format, int pos, StringBuilder result)
        {
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
                        throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                    }
                }
                else
                {
                    result.Append(ch);
                }
            }

            if (!foundQuote)
            {
                throw new FormatException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Format_BadQuote"), quoteChar));
            }

            return (pos - beginPos);
        }

        internal static int ParseNextChar(String format, int pos)
        {
            if (pos >= format.Length - 1)
            {
                return (-1);
            }

            return ((int)format[pos + 1]);
        }

        private static bool IsUseGenitiveForm(String format, int index, int tokenLen, char patternToMatch)
        {
            int i;
            int repeat = 0;
            for (i = index - 1; i >= 0 && format[i] != patternToMatch; i--)
            {
            }

            ;
            if (i >= 0)
            {
                while (--i >= 0 && format[i] == patternToMatch)
                {
                    repeat++;
                }

                if (repeat <= 1)
                {
                    return (true);
                }
            }

            for (i = index + tokenLen; i < format.Length && format[i] != patternToMatch; i++)
            {
            }

            ;
            if (i < format.Length)
            {
                repeat = 0;
                while (++i < format.Length && format[i] == patternToMatch)
                {
                    repeat++;
                }

                if (repeat <= 1)
                {
                    return (true);
                }
            }

            return (false);
        }

        private static String FormatCustomized(DateTime dateTime, String format, DateTimeFormatInfo dtfi, TimeSpan offset)
        {
            Calendar cal = dtfi.Calendar;
            StringBuilder result = StringBuilderCache.Acquire();
            bool isHebrewCalendar = (cal.ID == Calendar.CAL_HEBREW);
            bool bTimeOnly = true;
            int i = 0;
            int tokenLen, hour12;
            while (i < format.Length)
            {
                char ch = format[i];
                int nextChar;
                switch (ch)
                {
                    case 'g':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        result.Append(dtfi.GetEraName(cal.GetEra(dateTime)));
                        break;
                    case 'h':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        hour12 = dateTime.Hour % 12;
                        if (hour12 == 0)
                        {
                            hour12 = 12;
                        }

                        FormatDigits(result, hour12, tokenLen);
                        break;
                    case 'H':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(result, dateTime.Hour, tokenLen);
                        break;
                    case 'm':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(result, dateTime.Minute, tokenLen);
                        break;
                    case 's':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatDigits(result, dateTime.Second, tokenLen);
                        break;
                    case 'f':
                    case 'F':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        if (tokenLen <= MaxSecondsFractionDigits)
                        {
                            long fraction = (dateTime.Ticks % Calendar.TicksPerSecond);
                            fraction = fraction / (long)Math.Pow(10, 7 - tokenLen);
                            if (ch == 'f')
                            {
                                result.Append(((int)fraction).ToString(fixedNumberFormats[tokenLen - 1], CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                int effectiveDigits = tokenLen;
                                while (effectiveDigits > 0)
                                {
                                    if (fraction % 10 == 0)
                                    {
                                        fraction = fraction / 10;
                                        effectiveDigits--;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                if (effectiveDigits > 0)
                                {
                                    result.Append(((int)fraction).ToString(fixedNumberFormats[effectiveDigits - 1], CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    if (result.Length > 0 && result[result.Length - 1] == '.')
                                    {
                                        result.Remove(result.Length - 1, 1);
                                    }
                                }
                            }
                        }
                        else
                        {
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        }

                        break;
                    case 't':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        if (tokenLen == 1)
                        {
                            if (dateTime.Hour < 12)
                            {
                                if (dtfi.AMDesignator.Length >= 1)
                                {
                                    result.Append(dtfi.AMDesignator[0]);
                                }
                            }
                            else
                            {
                                if (dtfi.PMDesignator.Length >= 1)
                                {
                                    result.Append(dtfi.PMDesignator[0]);
                                }
                            }
                        }
                        else
                        {
                            result.Append((dateTime.Hour < 12 ? dtfi.AMDesignator : dtfi.PMDesignator));
                        }

                        break;
                    case 'd':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        if (tokenLen <= 2)
                        {
                            int day = cal.GetDayOfMonth(dateTime);
                            if (isHebrewCalendar)
                            {
                                HebrewFormatDigits(result, day);
                            }
                            else
                            {
                                FormatDigits(result, day, tokenLen);
                            }
                        }
                        else
                        {
                            int dayOfWeek = (int)cal.GetDayOfWeek(dateTime);
                            result.Append(FormatDayOfWeek(dayOfWeek, tokenLen, dtfi));
                        }

                        bTimeOnly = false;
                        break;
                    case 'M':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        int month = cal.GetMonth(dateTime);
                        if (tokenLen <= 2)
                        {
                            if (isHebrewCalendar)
                            {
                                HebrewFormatDigits(result, month);
                            }
                            else
                            {
                                FormatDigits(result, month, tokenLen);
                            }
                        }
                        else
                        {
                            if (isHebrewCalendar)
                            {
                                result.Append(FormatHebrewMonthName(dateTime, month, tokenLen, dtfi));
                            }
                            else
                            {
                                if ((dtfi.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0 && tokenLen >= 4)
                                {
                                    result.Append(dtfi.internalGetMonthName(month, IsUseGenitiveForm(format, i, tokenLen, 'd') ? MonthNameStyles.Genitive : MonthNameStyles.Regular, false));
                                }
                                else
                                {
                                    result.Append(FormatMonth(month, tokenLen, dtfi));
                                }
                            }
                        }

                        bTimeOnly = false;
                        break;
                    case 'y':
                        int year = cal.GetYear(dateTime);
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        if (dtfi.HasForceTwoDigitYears)
                        {
                            FormatDigits(result, year, tokenLen <= 2 ? tokenLen : 2);
                        }
                        else if (cal.ID == Calendar.CAL_HEBREW)
                        {
                            HebrewFormatDigits(result, year);
                        }
                        else
                        {
                            if (tokenLen <= 2)
                            {
                                FormatDigits(result, year % 100, tokenLen);
                            }
                            else
                            {
                                String fmtPattern = "D" + tokenLen;
                                result.Append(year.ToString(fmtPattern, CultureInfo.InvariantCulture));
                            }
                        }

                        bTimeOnly = false;
                        break;
                    case 'z':
                        tokenLen = ParseRepeatPattern(format, i, ch);
                        FormatCustomizedTimeZone(dateTime, offset, format, tokenLen, bTimeOnly, result);
                        break;
                    case 'K':
                        tokenLen = 1;
                        FormatCustomizedRoundripTimeZone(dateTime, offset, result);
                        break;
                    case ':':
                        result.Append(dtfi.TimeSeparator);
                        tokenLen = 1;
                        break;
                    case '/':
                        result.Append(dtfi.DateSeparator);
                        tokenLen = 1;
                        break;
                    case '\'':
                    case '\"':
                        tokenLen = ParseQuoteString(format, i, result);
                        break;
                    case '%':
                        nextChar = ParseNextChar(format, i);
                        if (nextChar >= 0 && nextChar != (int)'%')
                        {
                            result.Append(FormatCustomized(dateTime, ((char)nextChar).ToString(), dtfi, offset));
                            tokenLen = 2;
                        }
                        else
                        {
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        }

                        break;
                    case '\\':
                        nextChar = ParseNextChar(format, i);
                        if (nextChar >= 0)
                        {
                            result.Append(((char)nextChar));
                            tokenLen = 2;
                        }
                        else
                        {
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        }

                        break;
                    default:
                        result.Append(ch);
                        tokenLen = 1;
                        break;
                }

                i += tokenLen;
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        private static void FormatCustomizedTimeZone(DateTime dateTime, TimeSpan offset, String format, Int32 tokenLen, Boolean timeOnly, StringBuilder result)
        {
            Boolean dateTimeFormat = (offset == NullOffset);
            if (dateTimeFormat)
            {
                if (timeOnly && dateTime.Ticks < Calendar.TicksPerDay)
                {
                    offset = TimeZoneInfo.GetLocalUtcOffset(DateTime.Now, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                }
                else if (dateTime.Kind == DateTimeKind.Utc)
                {
                    offset = TimeSpan.Zero;
                }
                else
                {
                    offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                }
            }

            if (offset >= TimeSpan.Zero)
            {
                result.Append('+');
            }
            else
            {
                result.Append('-');
                offset = offset.Negate();
            }

            if (tokenLen <= 1)
            {
                result.AppendFormat(CultureInfo.InvariantCulture, "{0:0}", offset.Hours);
            }
            else
            {
                result.AppendFormat(CultureInfo.InvariantCulture, "{0:00}", offset.Hours);
                if (tokenLen >= 3)
                {
                    result.AppendFormat(CultureInfo.InvariantCulture, ":{0:00}", offset.Minutes);
                }
            }
        }

        private static void FormatCustomizedRoundripTimeZone(DateTime dateTime, TimeSpan offset, StringBuilder result)
        {
            if (offset == NullOffset)
            {
                switch (dateTime.Kind)
                {
                    case DateTimeKind.Local:
                        offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
                        break;
                    case DateTimeKind.Utc:
                        result.Append("Z");
                        return;
                    default:
                        return;
                }
            }

            if (offset >= TimeSpan.Zero)
            {
                result.Append('+');
            }
            else
            {
                result.Append('-');
                offset = offset.Negate();
            }

            result.AppendFormat(CultureInfo.InvariantCulture, "{0:00}:{1:00}", offset.Hours, offset.Minutes);
        }

        internal static String GetRealFormat(String format, DateTimeFormatInfo dtfi)
        {
            String realFormat = null;
            switch (format[0])
            {
                case 'd':
                    realFormat = dtfi.ShortDatePattern;
                    break;
                case 'D':
                    realFormat = dtfi.LongDatePattern;
                    break;
                case 'f':
                    realFormat = dtfi.LongDatePattern + " " + dtfi.ShortTimePattern;
                    break;
                case 'F':
                    realFormat = dtfi.FullDateTimePattern;
                    break;
                case 'g':
                    realFormat = dtfi.GeneralShortTimePattern;
                    break;
                case 'G':
                    realFormat = dtfi.GeneralLongTimePattern;
                    break;
                case 'm':
                case 'M':
                    realFormat = dtfi.MonthDayPattern;
                    break;
                case 'o':
                case 'O':
                    realFormat = RoundtripFormat;
                    break;
                case 'r':
                case 'R':
                    realFormat = dtfi.RFC1123Pattern;
                    break;
                case 's':
                    realFormat = dtfi.SortableDateTimePattern;
                    break;
                case 't':
                    realFormat = dtfi.ShortTimePattern;
                    break;
                case 'T':
                    realFormat = dtfi.LongTimePattern;
                    break;
                case 'u':
                    realFormat = dtfi.UniversalSortableDateTimePattern;
                    break;
                case 'U':
                    realFormat = dtfi.FullDateTimePattern;
                    break;
                case 'y':
                case 'Y':
                    realFormat = dtfi.YearMonthPattern;
                    break;
                default:
                    throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
            }

            return (realFormat);
        }

        private static String ExpandPredefinedFormat(String format, ref DateTime dateTime, ref DateTimeFormatInfo dtfi, ref TimeSpan offset)
        {
            switch (format[0])
            {
                case 'o':
                case 'O':
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    break;
                case 'r':
                case 'R':
                    if (offset != NullOffset)
                    {
                        dateTime = dateTime - offset;
                    }
                    else if (dateTime.Kind == DateTimeKind.Local)
                    {
                        InvalidFormatForLocal(format, dateTime);
                    }

                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    break;
                case 's':
                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    break;
                case 'u':
                    if (offset != NullOffset)
                    {
                        dateTime = dateTime - offset;
                    }
                    else if (dateTime.Kind == DateTimeKind.Local)
                    {
                        InvalidFormatForLocal(format, dateTime);
                    }

                    dtfi = DateTimeFormatInfo.InvariantInfo;
                    break;
                case 'U':
                    if (offset != NullOffset)
                    {
                        throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                    }

                    dtfi = (DateTimeFormatInfo)dtfi.Clone();
                    if (dtfi.Calendar.GetType() != typeof (GregorianCalendar))
                    {
                        dtfi.Calendar = GregorianCalendar.GetDefaultInstance();
                    }

                    dateTime = dateTime.ToUniversalTime();
                    break;
            }

            format = GetRealFormat(format, dtfi);
            return (format);
        }

        internal static String Format(DateTime dateTime, String format, DateTimeFormatInfo dtfi)
        {
            return Format(dateTime, format, dtfi, NullOffset);
        }

        internal static String Format(DateTime dateTime, String format, DateTimeFormatInfo dtfi, TimeSpan offset)
        {
                        if (format == null || format.Length == 0)
            {
                Boolean timeOnlySpecialCase = false;
                if (dateTime.Ticks < Calendar.TicksPerDay)
                {
                    switch (dtfi.Calendar.ID)
                    {
                        case Calendar.CAL_JAPAN:
                        case Calendar.CAL_TAIWAN:
                        case Calendar.CAL_HIJRI:
                        case Calendar.CAL_HEBREW:
                        case Calendar.CAL_JULIAN:
                        case Calendar.CAL_UMALQURA:
                        case Calendar.CAL_PERSIAN:
                            timeOnlySpecialCase = true;
                            dtfi = DateTimeFormatInfo.InvariantInfo;
                            break;
                    }
                }

                if (offset == NullOffset)
                {
                    if (timeOnlySpecialCase)
                    {
                        format = "s";
                    }
                    else
                    {
                        format = "G";
                    }
                }
                else
                {
                    if (timeOnlySpecialCase)
                    {
                        format = RoundtripDateTimeUnfixed;
                    }
                    else
                    {
                        format = dtfi.DateTimeOffsetPattern;
                    }
                }
            }

            if (format.Length == 1)
            {
                format = ExpandPredefinedFormat(format, ref dateTime, ref dtfi, ref offset);
            }

            return (FormatCustomized(dateTime, format, dtfi, offset));
        }

        internal static String[] GetAllDateTimes(DateTime dateTime, char format, DateTimeFormatInfo dtfi)
        {
                        String[] allFormats = null;
            String[] results = null;
            switch (format)
            {
                case 'd':
                case 'D':
                case 'f':
                case 'F':
                case 'g':
                case 'G':
                case 'm':
                case 'M':
                case 't':
                case 'T':
                case 'y':
                case 'Y':
                    allFormats = dtfi.GetAllDateTimePatterns(format);
                    results = new String[allFormats.Length];
                    for (int i = 0; i < allFormats.Length; i++)
                    {
                        results[i] = Format(dateTime, allFormats[i], dtfi);
                    }

                    break;
                case 'U':
                    DateTime universalTime = dateTime.ToUniversalTime();
                    allFormats = dtfi.GetAllDateTimePatterns(format);
                    results = new String[allFormats.Length];
                    for (int i = 0; i < allFormats.Length; i++)
                    {
                        results[i] = Format(universalTime, allFormats[i], dtfi);
                    }

                    break;
                case 'r':
                case 'R':
                case 'o':
                case 'O':
                case 's':
                case 'u':
                    results = new String[]{Format(dateTime, new String(new char[]{format}), dtfi)};
                    break;
                default:
                    throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
            }

            return (results);
        }

        internal static String[] GetAllDateTimes(DateTime dateTime, DateTimeFormatInfo dtfi)
        {
            List<String> results = new List<String>(DEFAULT_ALL_DATETIMES_SIZE);
            for (int i = 0; i < allStandardFormats.Length; i++)
            {
                String[] strings = GetAllDateTimes(dateTime, allStandardFormats[i], dtfi);
                for (int j = 0; j < strings.Length; j++)
                {
                    results.Add(strings[j]);
                }
            }

            String[] value = new String[results.Count];
            results.CopyTo(0, value, 0, results.Count);
            return (value);
        }

        internal static void InvalidFormatForLocal(String format, DateTime dateTime)
        {
        }

        internal static void InvalidFormatForUtc(String format, DateTime dateTime)
        {
        }
    }
}