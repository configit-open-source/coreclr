namespace System.Globalization
{
    using System.Text;
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    internal static class TimeSpanFormat
    {
        private static String IntToString(int n, int digits)
        {
            return ParseNumbers.IntToString(n, 10, digits, '0', 0);
        }

        internal static readonly FormatLiterals PositiveInvariantFormatLiterals = TimeSpanFormat.FormatLiterals.InitInvariant(false);
        internal static readonly FormatLiterals NegativeInvariantFormatLiterals = TimeSpanFormat.FormatLiterals.InitInvariant(true);
        internal enum Pattern
        {
            None = 0,
            Minimum = 1,
            Full = 2
        }

        internal static String Format(TimeSpan value, String format, IFormatProvider formatProvider)
        {
            if (format == null || format.Length == 0)
                format = "c";
            if (format.Length == 1)
            {
                char f = format[0];
                if (f == 'c' || f == 't' || f == 'T')
                    return FormatStandard(value, true, format, Pattern.Minimum);
                if (f == 'g' || f == 'G')
                {
                    Pattern pattern;
                    DateTimeFormatInfo dtfi = DateTimeFormatInfo.GetInstance(formatProvider);
                    if (value._ticks < 0)
                        format = dtfi.FullTimeSpanNegativePattern;
                    else
                        format = dtfi.FullTimeSpanPositivePattern;
                    if (f == 'g')
                        pattern = Pattern.Minimum;
                    else
                        pattern = Pattern.Full;
                    return FormatStandard(value, false, format, pattern);
                }

                throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
            }

            return FormatCustomized(value, format, DateTimeFormatInfo.GetInstance(formatProvider));
        }

        private static String FormatStandard(TimeSpan value, bool isInvariant, String format, Pattern pattern)
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            int day = (int)(value._ticks / TimeSpan.TicksPerDay);
            long time = value._ticks % TimeSpan.TicksPerDay;
            if (value._ticks < 0)
            {
                day = -day;
                time = -time;
            }

            int hours = (int)(time / TimeSpan.TicksPerHour % 24);
            int minutes = (int)(time / TimeSpan.TicksPerMinute % 60);
            int seconds = (int)(time / TimeSpan.TicksPerSecond % 60);
            int fraction = (int)(time % TimeSpan.TicksPerSecond);
            FormatLiterals literal;
            if (isInvariant)
            {
                if (value._ticks < 0)
                    literal = NegativeInvariantFormatLiterals;
                else
                    literal = PositiveInvariantFormatLiterals;
            }
            else
            {
                literal = new FormatLiterals();
                literal.Init(format, pattern == Pattern.Full);
            }

            if (fraction != 0)
            {
                fraction = (int)((long)fraction / (long)Math.Pow(10, DateTimeFormat.MaxSecondsFractionDigits - literal.ff));
            }

            sb.Append(literal.Start);
            if (pattern == Pattern.Full || day != 0)
            {
                sb.Append(day);
                sb.Append(literal.DayHourSep);
            }

            sb.Append(IntToString(hours, literal.hh));
            sb.Append(literal.HourMinuteSep);
            sb.Append(IntToString(minutes, literal.mm));
            sb.Append(literal.MinuteSecondSep);
            sb.Append(IntToString(seconds, literal.ss));
            if (!isInvariant && pattern == Pattern.Minimum)
            {
                int effectiveDigits = literal.ff;
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
                    sb.Append(literal.SecondFractionSep);
                    sb.Append((fraction).ToString(DateTimeFormat.fixedNumberFormats[effectiveDigits - 1], CultureInfo.InvariantCulture));
                }
            }
            else if (pattern == Pattern.Full || fraction != 0)
            {
                sb.Append(literal.SecondFractionSep);
                sb.Append(IntToString(fraction, literal.ff));
            }

            sb.Append(literal.End);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal static String FormatCustomized(TimeSpan value, String format, DateTimeFormatInfo dtfi)
        {
            Contract.Assert(dtfi != null, "dtfi == null");
            int day = (int)(value._ticks / TimeSpan.TicksPerDay);
            long time = value._ticks % TimeSpan.TicksPerDay;
            if (value._ticks < 0)
            {
                day = -day;
                time = -time;
            }

            int hours = (int)(time / TimeSpan.TicksPerHour % 24);
            int minutes = (int)(time / TimeSpan.TicksPerMinute % 60);
            int seconds = (int)(time / TimeSpan.TicksPerSecond % 60);
            int fraction = (int)(time % TimeSpan.TicksPerSecond);
            long tmp = 0;
            int i = 0;
            int tokenLen;
            StringBuilder result = StringBuilderCache.Acquire();
            while (i < format.Length)
            {
                char ch = format[i];
                int nextChar;
                switch (ch)
                {
                    case 'h':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        DateTimeFormat.FormatDigits(result, hours, tokenLen);
                        break;
                    case 'm':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        DateTimeFormat.FormatDigits(result, minutes, tokenLen);
                        break;
                    case 's':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 2)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        DateTimeFormat.FormatDigits(result, seconds, tokenLen);
                        break;
                    case 'f':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        tmp = (long)fraction;
                        tmp /= (long)Math.Pow(10, DateTimeFormat.MaxSecondsFractionDigits - tokenLen);
                        result.Append((tmp).ToString(DateTimeFormat.fixedNumberFormats[tokenLen - 1], CultureInfo.InvariantCulture));
                        break;
                    case 'F':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > DateTimeFormat.MaxSecondsFractionDigits)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        tmp = (long)fraction;
                        tmp /= (long)Math.Pow(10, DateTimeFormat.MaxSecondsFractionDigits - tokenLen);
                        int effectiveDigits = tokenLen;
                        while (effectiveDigits > 0)
                        {
                            if (tmp % 10 == 0)
                            {
                                tmp = tmp / 10;
                                effectiveDigits--;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (effectiveDigits > 0)
                        {
                            result.Append((tmp).ToString(DateTimeFormat.fixedNumberFormats[effectiveDigits - 1], CultureInfo.InvariantCulture));
                        }

                        break;
                    case 'd':
                        tokenLen = DateTimeFormat.ParseRepeatPattern(format, i, ch);
                        if (tokenLen > 8)
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        DateTimeFormat.FormatDigits(result, day, tokenLen, true);
                        break;
                    case '\'':
                    case '\"':
                        tokenLen = DateTimeFormat.ParseQuoteString(format, i, result);
                        break;
                    case '%':
                        nextChar = DateTimeFormat.ParseNextChar(format, i);
                        if (nextChar >= 0 && nextChar != (int)'%')
                        {
                            result.Append(TimeSpanFormat.FormatCustomized(value, ((char)nextChar).ToString(), dtfi));
                            tokenLen = 2;
                        }
                        else
                        {
                            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                        }

                        break;
                    case '\\':
                        nextChar = DateTimeFormat.ParseNextChar(format, i);
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
                        throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                }

                i += tokenLen;
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        internal struct FormatLiterals
        {
            internal String Start
            {
                get
                {
                    return literals[0];
                }
            }

            internal String DayHourSep
            {
                get
                {
                    return literals[1];
                }
            }

            internal String HourMinuteSep
            {
                get
                {
                    return literals[2];
                }
            }

            internal String MinuteSecondSep
            {
                get
                {
                    return literals[3];
                }
            }

            internal String SecondFractionSep
            {
                get
                {
                    return literals[4];
                }
            }

            internal String End
            {
                get
                {
                    return literals[5];
                }
            }

            internal String AppCompatLiteral;
            internal int dd;
            internal int hh;
            internal int mm;
            internal int ss;
            internal int ff;
            private String[] literals;
            internal static FormatLiterals InitInvariant(bool isNegative)
            {
                FormatLiterals x = new FormatLiterals();
                x.literals = new String[6];
                x.literals[0] = isNegative ? "-" : String.Empty;
                x.literals[1] = ".";
                x.literals[2] = ":";
                x.literals[3] = ":";
                x.literals[4] = ".";
                x.literals[5] = String.Empty;
                x.AppCompatLiteral = ":.";
                x.dd = 2;
                x.hh = 2;
                x.mm = 2;
                x.ss = 2;
                x.ff = DateTimeFormat.MaxSecondsFractionDigits;
                return x;
            }

            internal void Init(String format, bool useInvariantFieldLengths)
            {
                literals = new String[6];
                for (int i = 0; i < literals.Length; i++)
                    literals[i] = String.Empty;
                dd = 0;
                hh = 0;
                mm = 0;
                ss = 0;
                ff = 0;
                StringBuilder sb = StringBuilderCache.Acquire();
                bool inQuote = false;
                char quote = '\'';
                int field = 0;
                for (int i = 0; i < format.Length; i++)
                {
                    switch (format[i])
                    {
                        case '\'':
                        case '\"':
                            if (inQuote && (quote == format[i]))
                            {
                                Contract.Assert(field >= 0 && field <= 5, "field >= 0 && field <= 5");
                                if (field >= 0 && field <= 5)
                                {
                                    literals[field] = sb.ToString();
                                    sb.Length = 0;
                                    inQuote = false;
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else if (!inQuote)
                            {
                                quote = format[i];
                                inQuote = true;
                            }
                            else
                            {
                            }

                            break;
                        case '%':
                            Contract.Assert(false, "Unexpected special token '%', Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                            goto default;
                        case '\\':
                            if (!inQuote)
                            {
                                i++;
                                break;
                            }

                            goto default;
                        case 'd':
                            if (!inQuote)
                            {
                                Contract.Assert((field == 0 && sb.Length == 0) || field == 1, "field == 0 || field == 1, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 1;
                                dd++;
                            }

                            break;
                        case 'h':
                            if (!inQuote)
                            {
                                Contract.Assert((field == 1 && sb.Length == 0) || field == 2, "field == 1 || field == 2, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 2;
                                hh++;
                            }

                            break;
                        case 'm':
                            if (!inQuote)
                            {
                                Contract.Assert((field == 2 && sb.Length == 0) || field == 3, "field == 2 || field == 3, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 3;
                                mm++;
                            }

                            break;
                        case 's':
                            if (!inQuote)
                            {
                                Contract.Assert((field == 3 && sb.Length == 0) || field == 4, "field == 3 || field == 4, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 4;
                                ss++;
                            }

                            break;
                        case 'f':
                        case 'F':
                            if (!inQuote)
                            {
                                Contract.Assert((field == 4 && sb.Length == 0) || field == 5, "field == 4 || field == 5, Bug in DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                                field = 5;
                                ff++;
                            }

                            break;
                        default:
                            sb.Append(format[i]);
                            break;
                    }
                }

                Contract.Assert(field == 5);
                AppCompatLiteral = MinuteSecondSep + SecondFractionSep;
                Contract.Assert(0 < dd && dd < 3, "0 < dd && dd < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Contract.Assert(0 < hh && hh < 3, "0 < hh && hh < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Contract.Assert(0 < mm && mm < 3, "0 < mm && mm < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Contract.Assert(0 < ss && ss < 3, "0 < ss && ss < 3, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                Contract.Assert(0 < ff && ff < 8, "0 < ff && ff < 8, Bug in System.Globalization.DateTimeFormatInfo.FullTimeSpan[Positive|Negative]Pattern");
                if (useInvariantFieldLengths)
                {
                    dd = 2;
                    hh = 2;
                    mm = 2;
                    ss = 2;
                    ff = DateTimeFormat.MaxSecondsFractionDigits;
                }
                else
                {
                    if (dd < 1 || dd > 2)
                        dd = 2;
                    if (hh < 1 || hh > 2)
                        hh = 2;
                    if (mm < 1 || mm > 2)
                        mm = 2;
                    if (ss < 1 || ss > 2)
                        ss = 2;
                    if (ff < 1 || ff > 7)
                        ff = 7;
                }

                StringBuilderCache.Release(sb);
            }
        }
    }
}