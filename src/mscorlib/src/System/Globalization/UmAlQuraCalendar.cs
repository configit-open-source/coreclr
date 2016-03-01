

namespace System.Globalization
{
    public class UmAlQuraCalendar : Calendar
    {
        internal const int MinCalendarYear = 1318;
        internal const int MaxCalendarYear = 1500;
        internal struct DateMapping
        {
            internal DateMapping(int MonthsLengthFlags, int GYear, int GMonth, int GDay)
            {
                HijriMonthsLengthFlags = MonthsLengthFlags;
                GregorianDate = new DateTime(GYear, GMonth, GDay);
            }

            internal int HijriMonthsLengthFlags;
            internal DateTime GregorianDate;
        }

        static readonly DateMapping[] HijriYearInfo = InitDateMapping();
        static DateMapping[] InitDateMapping()
        {
            short[] rawData = new short[]{0x02EA, 1900, 4, 30, 0x06E9, 1901, 4, 19, 0x0ED2, 1902, 4, 9, 0x0EA4, 1903, 3, 30, 0x0D4A, 1904, 3, 18, 0x0A96, 1905, 3, 7, 0x0536, 1906, 2, 24, 0x0AB5, 1907, 2, 13, 0x0DAA, 1908, 2, 3, 0x0BA4, 1909, 1, 23, 0x0B49, 1910, 1, 12, 0x0A93, 1911, 1, 1, 0x052B, 1911, 12, 21, 0x0A57, 1912, 12, 9, 0x04B6, 1913, 11, 29, 0x0AB5, 1914, 11, 18, 0x05AA, 1915, 11, 8, 0x0D55, 1916, 10, 27, 0x0D2A, 1917, 10, 17, 0x0A56, 1918, 10, 6, 0x04AE, 1919, 9, 25, 0x095D, 1920, 9, 13, 0x02EC, 1921, 9, 3, 0x06D5, 1922, 8, 23, 0x06AA, 1923, 8, 13, 0x0555, 1924, 8, 1, 0x04AB, 1925, 7, 21, 0x095B, 1926, 7, 10, 0x02BA, 1927, 6, 30, 0x0575, 1928, 6, 18, 0x0BB2, 1929, 6, 8, 0x0764, 1930, 5, 29, 0x0749, 1931, 5, 18, 0x0655, 1932, 5, 6, 0x02AB, 1933, 4, 25, 0x055B, 1934, 4, 14, 0x0ADA, 1935, 4, 4, 0x06D4, 1936, 3, 24, 0x0EC9, 1937, 3, 13, 0x0D92, 1938, 3, 3, 0x0D25, 1939, 2, 20, 0x0A4D, 1940, 2, 9, 0x02AD, 1941, 1, 28, 0x056D, 1942, 1, 17, 0x0B6A, 1943, 1, 7, 0x0B52, 1943, 12, 28, 0x0AA5, 1944, 12, 16, 0x0A4B, 1945, 12, 5, 0x0497, 1946, 11, 24, 0x0937, 1947, 11, 13, 0x02B6, 1948, 11, 2, 0x0575, 1949, 10, 22, 0x0D6A, 1950, 10, 12, 0x0D52, 1951, 10, 2, 0x0A96, 1952, 9, 20, 0x092D, 1953, 9, 9, 0x025D, 1954, 8, 29, 0x04DD, 1955, 8, 18, 0x0ADA, 1956, 8, 7, 0x05D4, 1957, 7, 28, 0x0DA9, 1958, 7, 17, 0x0D52, 1959, 7, 7, 0x0AAA, 1960, 6, 25, 0x04D6, 1961, 6, 14, 0x09B6, 1962, 6, 3, 0x0374, 1963, 5, 24, 0x0769, 1964, 5, 12, 0x0752, 1965, 5, 2, 0x06A5, 1966, 4, 21, 0x054B, 1967, 4, 10, 0x0AAB, 1968, 3, 29, 0x055A, 1969, 3, 19, 0x0AD5, 1970, 3, 8, 0x0DD2, 1971, 2, 26, 0x0DA4, 1972, 2, 16, 0x0D49, 1973, 2, 4, 0x0A95, 1974, 1, 24, 0x052D, 1975, 1, 13, 0x0A5D, 1976, 1, 2, 0x055A, 1976, 12, 22, 0x0AD5, 1977, 12, 11, 0x06AA, 1978, 12, 1, 0x0695, 1979, 11, 20, 0x052B, 1980, 11, 8, 0x0A57, 1981, 10, 28, 0x04AE, 1982, 10, 18, 0x0976, 1983, 10, 7, 0x056C, 1984, 9, 26, 0x0B55, 1985, 9, 15, 0x0AAA, 1986, 9, 5, 0x0A55, 1987, 8, 25, 0x04AD, 1988, 8, 13, 0x095D, 1989, 8, 2, 0x02DA, 1990, 7, 23, 0x05D9, 1991, 7, 12, 0x0DB2, 1992, 7, 1, 0x0BA4, 1993, 6, 21, 0x0B4A, 1994, 6, 10, 0x0A55, 1995, 5, 30, 0x02B5, 1996, 5, 18, 0x0575, 1997, 5, 7, 0x0B6A, 1998, 4, 27, 0x0BD2, 1999, 4, 17, 0x0BC4, 2000, 4, 6, 0x0B89, 2001, 3, 26, 0x0A95, 2002, 3, 15, 0x052D, 2003, 3, 4, 0x05AD, 2004, 2, 21, 0x0B6A, 2005, 2, 10, 0x06D4, 2006, 1, 31, 0x0DC9, 2007, 1, 20, 0x0D92, 2008, 1, 10, 0x0AA6, 2008, 12, 29, 0x0956, 2009, 12, 18, 0x02AE, 2010, 12, 7, 0x056D, 2011, 11, 26, 0x036A, 2012, 11, 15, 0x0B55, 2013, 11, 4, 0x0AAA, 2014, 10, 25, 0x094D, 2015, 10, 14, 0x049D, 2016, 10, 2, 0x095D, 2017, 9, 21, 0x02BA, 2018, 9, 11, 0x05B5, 2019, 8, 31, 0x05AA, 2020, 8, 20, 0x0D55, 2021, 8, 9, 0x0A9A, 2022, 7, 30, 0x092E, 2023, 7, 19, 0x026E, 2024, 7, 7, 0x055D, 2025, 6, 26, 0x0ADA, 2026, 6, 16, 0x06D4, 2027, 6, 6, 0x06A5, 2028, 5, 25, 0x054B, 2029, 5, 14, 0x0A97, 2030, 5, 3, 0x054E, 2031, 4, 23, 0x0AAE, 2032, 4, 11, 0x05AC, 2033, 4, 1, 0x0BA9, 2034, 3, 21, 0x0D92, 2035, 3, 11, 0x0B25, 2036, 2, 28, 0x064B, 2037, 2, 16, 0x0CAB, 2038, 2, 5, 0x055A, 2039, 1, 26, 0x0B55, 2040, 1, 15, 0x06D2, 2041, 1, 4, 0x0EA5, 2041, 12, 24, 0x0E4A, 2042, 12, 14, 0x0A95, 2043, 12, 3, 0x052D, 2044, 11, 21, 0x0AAD, 2045, 11, 10, 0x036C, 2046, 10, 31, 0x0759, 2047, 10, 20, 0x06D2, 2048, 10, 9, 0x0695, 2049, 9, 28, 0x052D, 2050, 9, 17, 0x0A5B, 2051, 9, 6, 0x04BA, 2052, 8, 26, 0x09BA, 2053, 8, 15, 0x03B4, 2054, 8, 5, 0x0B69, 2055, 7, 25, 0x0B52, 2056, 7, 14, 0x0AA6, 2057, 7, 3, 0x04B6, 2058, 6, 22, 0x096D, 2059, 6, 11, 0x02EC, 2060, 5, 31, 0x06D9, 2061, 5, 20, 0x0EB2, 2062, 5, 10, 0x0D54, 2063, 4, 30, 0x0D2A, 2064, 4, 18, 0x0A56, 2065, 4, 7, 0x04AE, 2066, 3, 27, 0x096D, 2067, 3, 16, 0x0D6A, 2068, 3, 5, 0x0B54, 2069, 2, 23, 0x0B29, 2070, 2, 12, 0x0A93, 2071, 2, 1, 0x052B, 2072, 1, 21, 0x0A57, 2073, 1, 9, 0x0536, 2073, 12, 30, 0x0AB5, 2074, 12, 19, 0x06AA, 2075, 12, 9, 0x0E93, 2076, 11, 27, 0, 2077, 11, 17, };
            DateMapping[] mapping = new DateMapping[rawData.Length / 4];
            for (int i = 0; i < mapping.Length; i++)
                mapping[i] = new DateMapping(rawData[i * 4], rawData[i * 4 + 1], rawData[i * 4 + 2], rawData[i * 4 + 3]);
            return mapping;
        }

        public const int UmAlQuraEra = 1;
        internal const int DateCycle = 30;
        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;
        internal static DateTime minDate = new DateTime(1900, 4, 30);
        internal static DateTime maxDate = new DateTime((new DateTime(2077, 11, 16, 23, 59, 59, 999)).Ticks + 9999);
        public override DateTime MinSupportedDateTime
        {
            get
            {
                return (minDate);
            }
        }

        public override DateTime MaxSupportedDateTime
        {
            get
            {
                return (maxDate);
            }
        }

        public override CalendarAlgorithmType AlgorithmType
        {
            get
            {
                return CalendarAlgorithmType.LunarCalendar;
            }
        }

        public UmAlQuraCalendar()
        {
        }

        internal override int BaseCalendarID
        {
            get
            {
                return (CAL_HIJRI);
            }
        }

        internal override int ID
        {
            get
            {
                return (CAL_UMALQURA);
            }
        }

        protected override int DaysInYearBeforeMinSupportedYear
        {
            get
            {
                return 355;
            }
        }

        static void ConvertHijriToGregorian(int HijriYear, int HijriMonth, int HijriDay, ref int yg, ref int mg, ref int dg)
        {
                                                int index, b, nDays = HijriDay - 1;
            DateTime dt;
            index = HijriYear - MinCalendarYear;
            dt = HijriYearInfo[index].GregorianDate;
            b = HijriYearInfo[index].HijriMonthsLengthFlags;
            for (int m = 1; m < HijriMonth; m++)
            {
                nDays += 29 + (b & 0x1);
                b = b >> 1;
            }

            dt = dt.AddDays(nDays);
            yg = dt.Year;
            mg = dt.Month;
            dg = dt.Day;
        }

        static long GetAbsoluteDateUmAlQura(int year, int month, int day)
        {
            int yg = 0, mg = 0, dg = 0;
            ConvertHijriToGregorian(year, month, day, ref yg, ref mg, ref dg);
            return GregorianCalendar.GetAbsoluteDate(yg, mg, dg);
        }

        static internal void CheckTicksRange(long ticks)
        {
            if (ticks < minDate.Ticks || ticks > maxDate.Ticks)
            {
                throw new ArgumentOutOfRangeException("time", String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"), minDate, maxDate));
            }
        }

        static internal void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != UmAlQuraEra)
            {
                throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
            }
        }

        static internal void CheckYearRange(int year, int era)
        {
            CheckEraRange(era);
            if (year < MinCalendarYear || year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), MinCalendarYear, MaxCalendarYear));
            }
        }

        static internal void CheckYearMonthRange(int year, int month, int era)
        {
            CheckYearRange(year, era);
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
            }
        }

        static void ConvertGregorianToHijri(DateTime time, ref int HijriYear, ref int HijriMonth, ref int HijriDay)
        {
            int index, b, DaysPerThisMonth;
            double nDays;
            TimeSpan ts;
            int yh1 = 0, mh1 = 0, dh1 = 0;
                        index = (int)((time.Ticks - minDate.Ticks) / Calendar.TicksPerDay) / 355;
            do
            {
            }
            while (time.CompareTo(HijriYearInfo[++index].GregorianDate) > 0);
            if (time.CompareTo(HijriYearInfo[index].GregorianDate) != 0)
            {
                index--;
            }

            ts = time.Subtract(HijriYearInfo[index].GregorianDate);
            yh1 = index + MinCalendarYear;
            mh1 = 1;
            dh1 = 1;
            nDays = ts.TotalDays;
            b = HijriYearInfo[index].HijriMonthsLengthFlags;
            DaysPerThisMonth = 29 + (b & 1);
            while (nDays >= DaysPerThisMonth)
            {
                nDays -= DaysPerThisMonth;
                b = b >> 1;
                DaysPerThisMonth = 29 + (b & 1);
                mh1++;
            }

            dh1 += (int)nDays;
            HijriDay = dh1;
            HijriMonth = mh1;
            HijriYear = yh1;
        }

        internal virtual int GetDatePart(DateTime time, int part)
        {
            int UmAlQuraYear = 0;
            int UmAlQuraMonth = 0;
            int UmAlQuraDay = 0;
            long ticks = time.Ticks;
            CheckTicksRange(ticks);
            ConvertGregorianToHijri(time, ref UmAlQuraYear, ref UmAlQuraMonth, ref UmAlQuraDay);
            if (part == DatePartYear)
                return (UmAlQuraYear);
            if (part == DatePartMonth)
                return (UmAlQuraMonth);
            if (part == DatePartDay)
                return (UmAlQuraDay);
            if (part == DatePartDayOfYear)
                return (int)(GetAbsoluteDateUmAlQura(UmAlQuraYear, UmAlQuraMonth, UmAlQuraDay) - GetAbsoluteDateUmAlQura(UmAlQuraYear, 1, 1) + 1);
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DateTimeParsing"));
        }

        public override DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException("months", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), -120000, 120000));
            }

                        int y = GetDatePart(time, DatePartYear);
            int m = GetDatePart(time, DatePartMonth);
            int d = GetDatePart(time, DatePartDay);
            int i = m - 1 + months;
            if (i >= 0)
            {
                m = i % 12 + 1;
                y = y + i / 12;
            }
            else
            {
                m = 12 + (i + 1) % 12;
                y = y + (i - 11) / 12;
            }

            if (d > 29)
            {
                int days = GetDaysInMonth(y, m);
                if (d > days)
                {
                    d = days;
                }
            }

            CheckYearRange(y, UmAlQuraEra);
            DateTime dt = new DateTime(GetAbsoluteDateUmAlQura(y, m, d) * TicksPerDay + time.Ticks % TicksPerDay);
            Calendar.CheckAddResult(dt.Ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return (dt);
        }

        public override DateTime AddYears(DateTime time, int years)
        {
            return (AddMonths(time, years * 12));
        }

        public override int GetDayOfMonth(DateTime time)
        {
            return (GetDatePart(time, DatePartDay));
        }

        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            return ((DayOfWeek)((int)(time.Ticks / TicksPerDay + 1) % 7));
        }

        public override int GetDayOfYear(DateTime time)
        {
            return (GetDatePart(time, DatePartDayOfYear));
        }

        public override int GetDaysInMonth(int year, int month, int era)
        {
            CheckYearMonthRange(year, month, era);
            if ((HijriYearInfo[year - MinCalendarYear].HijriMonthsLengthFlags & (1 << month - 1)) == 0)
                return 29;
            else
                return 30;
        }

        static internal int RealGetDaysInYear(int year)
        {
            int days = 0, b;
                        b = HijriYearInfo[year - MinCalendarYear].HijriMonthsLengthFlags;
            for (int m = 1; m <= 12; m++)
            {
                days += 29 + (b & 0x1);
                b = b >> 1;
            }

                        return days;
        }

        public override int GetDaysInYear(int year, int era)
        {
            CheckYearRange(year, era);
            return (RealGetDaysInYear(year));
        }

        public override int GetEra(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return (UmAlQuraEra);
        }

        public override int[] Eras
        {
            get
            {
                return (new int[]{UmAlQuraEra});
            }
        }

        public override int GetMonth(DateTime time)
        {
            return (GetDatePart(time, DatePartMonth));
        }

        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearRange(year, era);
            return (12);
        }

        public override int GetYear(DateTime time)
        {
            return (GetDatePart(time, DatePartYear));
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            if (day >= 1 && day <= 29)
            {
                CheckYearMonthRange(year, month, era);
                return (false);
            }

            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException("day", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
            }

            return (false);
        }

        public override int GetLeapMonth(int year, int era)
        {
            CheckYearRange(year, era);
            return (0);
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            CheckYearMonthRange(year, month, era);
            return (false);
        }

        public override bool IsLeapYear(int year, int era)
        {
            CheckYearRange(year, era);
            if (RealGetDaysInYear(year) == 355)
                return true;
            else
                return false;
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            if (day >= 1 && day <= 29)
            {
                CheckYearMonthRange(year, month, era);
                goto DayInRang;
            }

            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                BCLDebug.Log("year = " + year + ", month = " + month + ", day = " + day);
                throw new ArgumentOutOfRangeException("day", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
            }

            DayInRang:
                long lDate = GetAbsoluteDateUmAlQura(year, month, day);
            if (lDate >= 0)
            {
                return (new DateTime(lDate * GregorianCalendar.TicksPerDay + TimeToTicks(hour, minute, second, millisecond)));
            }
            else
            {
                throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
            }
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 1451;
        public override int TwoDigitYearMax
        {
            get
            {
                if (twoDigitYearMax == -1)
                {
                    twoDigitYearMax = GetSystemTwoDigitYearSetting(ID, DEFAULT_TWO_DIGIT_YEAR_MAX);
                }

                return (twoDigitYearMax);
            }

            set
            {
                if (value != 99 && (value < MinCalendarYear || value > MaxCalendarYear))
                {
                    throw new ArgumentOutOfRangeException("value", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), MinCalendarYear, MaxCalendarYear));
                }

                                VerifyWritable();
                twoDigitYearMax = value;
            }
        }

        public override int ToFourDigitYear(int year)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

                        if (year < 100)
            {
                return (base.ToFourDigitYear(year));
            }

            if ((year < MinCalendarYear) || (year > MaxCalendarYear))
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), MinCalendarYear, MaxCalendarYear));
            }

            return (year);
        }
    }
}