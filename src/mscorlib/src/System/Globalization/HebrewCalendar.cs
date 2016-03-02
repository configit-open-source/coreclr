

namespace System.Globalization
{
    public class HebrewCalendar : Calendar
    {
        public static readonly int HebrewEra = 1;
        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;
        internal const int DatePartDayOfWeek = 4;
        private const int HebrewYearOf1AD = 3760;
        private const int FirstGregorianTableYear = 1583;
        private const int LastGregorianTableYear = 2239;
        private const int TABLESIZE = (LastGregorianTableYear - FirstGregorianTableYear);
        private const int MinHebrewYear = HebrewYearOf1AD + FirstGregorianTableYear;
        private const int MaxHebrewYear = HebrewYearOf1AD + LastGregorianTableYear;
        private static readonly int[] HebrewTable = {7, 3, 17, 3, 0, 4, 11, 2, 21, 6, 1, 3, 13, 2, 25, 4, 5, 3, 16, 2, 27, 6, 9, 1, 20, 2, 0, 6, 11, 3, 23, 4, 4, 2, 14, 3, 27, 4, 8, 2, 18, 3, 28, 6, 11, 1, 22, 5, 2, 3, 12, 3, 25, 4, 6, 2, 16, 3, 26, 6, 8, 2, 20, 1, 0, 6, 11, 2, 24, 4, 4, 3, 15, 2, 25, 6, 8, 1, 19, 2, 29, 6, 9, 3, 22, 4, 3, 2, 13, 3, 25, 4, 6, 3, 17, 2, 27, 6, 7, 3, 19, 2, 31, 4, 11, 3, 23, 4, 5, 2, 15, 3, 25, 6, 6, 2, 19, 1, 29, 6, 10, 2, 22, 4, 3, 3, 14, 2, 24, 6, 6, 1, 17, 3, 28, 5, 8, 3, 20, 1, 32, 5, 12, 3, 22, 6, 4, 1, 16, 2, 26, 6, 6, 3, 17, 2, 0, 4, 10, 3, 22, 4, 3, 2, 14, 3, 24, 6, 5, 2, 17, 1, 28, 6, 9, 2, 19, 3, 31, 4, 13, 2, 23, 6, 3, 3, 15, 1, 27, 5, 7, 3, 17, 3, 29, 4, 11, 2, 21, 6, 3, 1, 14, 2, 25, 6, 5, 3, 16, 2, 28, 4, 9, 3, 20, 2, 0, 6, 12, 1, 23, 6, 4, 2, 14, 3, 26, 4, 8, 2, 18, 3, 0, 4, 10, 3, 21, 5, 1, 3, 13, 1, 24, 5, 5, 3, 15, 3, 27, 4, 8, 2, 19, 3, 29, 6, 10, 2, 22, 4, 3, 3, 14, 2, 26, 4, 6, 3, 18, 2, 28, 6, 10, 1, 20, 6, 2, 2, 12, 3, 24, 4, 5, 2, 16, 3, 28, 4, 8, 3, 19, 2, 0, 6, 12, 1, 23, 5, 3, 3, 14, 3, 26, 4, 7, 2, 17, 3, 28, 6, 9, 2, 21, 4, 1, 3, 13, 2, 25, 4, 5, 3, 16, 2, 27, 6, 9, 1, 19, 3, 0, 5, 11, 3, 23, 4, 4, 2, 14, 3, 25, 6, 7, 1, 18, 2, 28, 6, 9, 3, 21, 4, 2, 2, 12, 3, 25, 4, 6, 2, 16, 3, 26, 6, 8, 2, 20, 1, 0, 6, 11, 2, 22, 6, 4, 1, 15, 2, 25, 6, 6, 3, 18, 1, 29, 5, 9, 3, 22, 4, 2, 3, 13, 2, 23, 6, 4, 3, 15, 2, 27, 4, 7, 3, 19, 2, 31, 4, 11, 3, 21, 6, 3, 2, 15, 1, 25, 6, 6, 2, 17, 3, 29, 4, 10, 2, 20, 6, 3, 1, 13, 3, 24, 5, 4, 3, 16, 1, 27, 5, 7, 3, 17, 3, 0, 4, 11, 2, 21, 6, 1, 3, 13, 2, 25, 4, 5, 3, 16, 2, 29, 4, 9, 3, 19, 6, 30, 2, 13, 1, 23, 6, 4, 2, 14, 3, 27, 4, 8, 2, 18, 3, 0, 4, 11, 3, 22, 5, 2, 3, 14, 1, 26, 5, 6, 3, 16, 3, 28, 4, 10, 2, 20, 6, 30, 3, 11, 2, 24, 4, 4, 3, 15, 2, 25, 6, 8, 1, 19, 2, 29, 6, 9, 3, 22, 4, 3, 2, 13, 3, 25, 4, 7, 2, 17, 3, 27, 6, 9, 1, 21, 5, 1, 3, 11, 3, 23, 4, 5, 2, 15, 3, 25, 6, 6, 2, 19, 1, 29, 6, 10, 2, 22, 4, 3, 3, 14, 2, 24, 6, 6, 1, 18, 2, 28, 6, 8, 3, 20, 4, 2, 2, 12, 3, 24, 4, 4, 3, 16, 2, 26, 6, 6, 3, 17, 2, 0, 4, 10, 3, 22, 4, 3, 2, 14, 3, 24, 6, 5, 2, 17, 1, 28, 6, 9, 2, 21, 4, 1, 3, 13, 2, 23, 6, 5, 1, 15, 3, 27, 5, 7, 3, 19, 1, 0, 5, 10, 3, 22, 4, 2, 3, 13, 2, 24, 6, 4, 3, 15, 2, 27, 4, 8, 3, 20, 4, 1, 2, 11, 3, 22, 6, 3, 2, 15, 1, 25, 6, 7, 2, 17, 3, 29, 4, 10, 2, 21, 6, 1, 3, 13, 1, 24, 5, 5, 3, 15, 3, 27, 4, 8, 2, 19, 6, 1, 1, 12, 2, 22, 6, 3, 3, 14, 2, 26, 4, 6, 3, 18, 2, 28, 6, 10, 1, 20, 6, 2, 2, 12, 3, 24, 4, 5, 2, 16, 3, 28, 4, 9, 2, 19, 6, 30, 3, 12, 1, 23, 5, 3, 3, 14, 3, 26, 4, 7, 2, 17, 3, 28, 6, 9, 2, 21, 4, 1, 3, 13, 2, 25, 4, 5, 3, 16, 2, 27, 6, 9, 1, 19, 6, 30, 2, 11, 3, 23, 4, 4, 2, 14, 3, 27, 4, 7, 3, 18, 2, 28, 6, 11, 1, 22, 5, 2, 3, 12, 3, 25, 4, 6, 2, 16, 3, 26, 6, 8, 2, 20, 4, 30, 3, 11, 2, 24, 4, 4, 3, 15, 2, 25, 6, 8, 1, 18, 3, 29, 5, 9, 3, 22, 4, 3, 2, 13, 3, 23, 6, 6, 1, 17, 2, 27, 6, 7, 3, 20, 4, 1, 2, 11, 3, 23, 4, 5, 2, 15, 3, 25, 6, 6, 2, 19, 1, 29, 6, 10, 2, 20, 6, 3, 1, 14, 2, 24, 6, 4, 3, 17, 1, 28, 5, 8, 3, 20, 4, 1, 3, 12, 2, 22, 6, 2, 3, 14, 2, 26, 4, 6, 3, 17, 2, 0, 4, 10, 3, 20, 6, 1, 2, 14, 1, 24, 6, 5, 2, 15, 3, 28, 4, 9, 2, 19, 6, 1, 1, 12, 3, 23, 5, 3, 3, 15, 1, 27, 5, 7, 3, 17, 3, 29, 4, 11, 2, 21, 6, 1, 3, 12, 2, 25, 4, 5, 3, 16, 2, 28, 4, 9, 3, 19, 6, 30, 2, 12, 1, 23, 6, 4, 2, 14, 3, 26, 4, 8, 2, 18, 3, 0, 4, 10, 3, 22, 5, 2, 3, 14, 1, 25, 5, 6, 3, 16, 3, 28, 4, 9, 2, 20, 6, 30, 3, 11, 2, 23, 4, 4, 3, 15, 2, 27, 4, 7, 3, 19, 2, 29, 6, 11, 1, 21, 6, 3, 2, 13, 3, 25, 4, 6, 2, 17, 3, 27, 6, 9, 1, 20, 5, 30, 3, 10, 3, 22, 4, 3, 2, 14, 3, 24, 6, 5, 2, 17, 1, 28, 6, 9, 2, 21, 4, 1, 3, 13, 2, 23, 6, 5, 1, 16, 2, 27, 6, 7, 3, 19, 4, 30, 2, 11, 3, 23, 4, 3, 3, 14, 2, 25, 6, 5, 3, 16, 2, 28, 4, 9, 3, 21, 4, 2, 2, 12, 3, 23, 6, 4, 2, 16, 1, 26, 6, 8, 2, 20, 4, 30, 3, 11, 2, 22, 6, 4, 1, 14, 3, 25, 5, 6, 3, 18, 1, 29, 5, 9, 3, 22, 4, 2, 3, 13, 2, 23, 6, 4, 3, 15, 2, 27, 4, 7, 3, 20, 4, 1, 2, 11, 3, 21, 6, 3, 2, 15, 1, 25, 6, 6, 2, 17, 3, 29, 4, 10, 2, 20, 6, 3, 1, 13, 3, 24, 5, 4, 3, 17, 1, 28, 5, 8, 3, 18, 6, 1, 1, 12, 2, 22, 6, 2, 3, 14, 2, 26, 4, 6, 3, 17, 2, 28, 6, 10, 1, 20, 6, 1, 2, 12, 3, 24, 4, 5, 2, 15, 3, 28, 4, 9, 2, 19, 6, 33, 3, 12, 1, 23, 5, 3, 3, 13, 3, 25, 4, 6, 2, 16, 3, 26, 6, 8, 2, 20, 4, 30, 3, 11, 2, 24, 4, 4, 3, 15, 2, 25, 6, 8, 1, 18, 6, 33, 2, 9, 3, 22, 4, 3, 2, 13, 3, 25, 4, 6, 3, 17, 2, 27, 6, 9, 1, 21, 5, 1, 3, 11, 3, 23, 4, 5, 2, 15, 3, 25, 6, 6, 2, 19, 4, 33, 3, 10, 2, 22, 4, 3, 3, 14, 2, 24, 6, 6, 1};
        private static readonly int[, ] LunarMonthLen = {{0, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 0}, {0, 30, 29, 29, 29, 30, 29, 30, 29, 30, 29, 30, 29, 0}, {0, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 0}, {0, 30, 30, 30, 29, 30, 29, 30, 29, 30, 29, 30, 29, 0}, {0, 30, 29, 29, 29, 30, 30, 29, 30, 29, 30, 29, 30, 29}, {0, 30, 29, 30, 29, 30, 30, 29, 30, 29, 30, 29, 30, 29}, {0, 30, 30, 30, 29, 30, 30, 29, 30, 29, 30, 29, 30, 29}};
        internal static readonly DateTime calendarMinValue = new DateTime(1583, 1, 1);
        internal static readonly DateTime calendarMaxValue = new DateTime((new DateTime(2239, 9, 29, 23, 59, 59, 999)).Ticks + 9999);
        public override DateTime MinSupportedDateTime
        {
            get
            {
                return (calendarMinValue);
            }
        }

        public override DateTime MaxSupportedDateTime
        {
            get
            {
                return (calendarMaxValue);
            }
        }

        public override CalendarAlgorithmType AlgorithmType
        {
            get
            {
                return CalendarAlgorithmType.LunisolarCalendar;
            }
        }

        public HebrewCalendar()
        {
        }

        internal override int ID
        {
            get
            {
                return (CAL_HEBREW);
            }
        }

        static private void CheckHebrewYearValue(int y, int era, String varName)
        {
            CheckEraRange(era);
            if (y > MaxHebrewYear || y < MinHebrewYear)
            {
                throw new ArgumentOutOfRangeException(varName, String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), MinHebrewYear, MaxHebrewYear));
            }
        }

        private void CheckHebrewMonthValue(int year, int month, int era)
        {
            int monthsInYear = GetMonthsInYear(year, era);
            if (month < 1 || month > monthsInYear)
            {
                throw new ArgumentOutOfRangeException("month", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, monthsInYear));
            }
        }

        private void CheckHebrewDayValue(int year, int month, int day, int era)
        {
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException("day", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, daysInMonth));
            }
        }

        static internal void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != HebrewEra)
            {
                throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
            }
        }

        static private void CheckTicksRange(long ticks)
        {
            if (ticks < calendarMinValue.Ticks || ticks > calendarMaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException("time", String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"), calendarMinValue, calendarMaxValue));
            }
        }

        static internal int GetResult(__DateBuffer result, int part)
        {
            switch (part)
            {
                case DatePartYear:
                    return (result.year);
                case DatePartMonth:
                    return (result.month);
                case DatePartDay:
                    return (result.day);
            }

            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DateTimeParsing"));
        }

        static internal int GetLunarMonthDay(int gregorianYear, __DateBuffer lunarDate)
        {
            int index = gregorianYear - FirstGregorianTableYear;
            if (index < 0 || index > TABLESIZE)
            {
                throw new ArgumentOutOfRangeException("gregorianYear");
            }

            index *= 2;
            lunarDate.day = HebrewTable[index];
            int LunarYearType = HebrewTable[index + 1];
            switch (lunarDate.day)
            {
                case (0):
                    lunarDate.month = 5;
                    lunarDate.day = 1;
                    break;
                case (30):
                    lunarDate.month = 3;
                    break;
                case (31):
                    lunarDate.month = 5;
                    lunarDate.day = 2;
                    break;
                case (32):
                    lunarDate.month = 5;
                    lunarDate.day = 3;
                    break;
                case (33):
                    lunarDate.month = 3;
                    lunarDate.day = 29;
                    break;
                default:
                    lunarDate.month = 4;
                    break;
            }

            return (LunarYearType);
        }

        internal virtual int GetDatePart(long ticks, int part)
        {
            int gregorianYear, gregorianMonth, gregorianDay;
            int hebrewYearType;
            long AbsoluteDate;
            CheckTicksRange(ticks);
            DateTime time = new DateTime(ticks);
            gregorianYear = time.Year;
            gregorianMonth = time.Month;
            gregorianDay = time.Day;
            __DateBuffer lunarDate = new __DateBuffer();
            lunarDate.year = gregorianYear + HebrewYearOf1AD;
            hebrewYearType = GetLunarMonthDay(gregorianYear, lunarDate);
            __DateBuffer result = new __DateBuffer();
            result.year = lunarDate.year;
            result.month = lunarDate.month;
            result.day = lunarDate.day;
            AbsoluteDate = GregorianCalendar.GetAbsoluteDate(gregorianYear, gregorianMonth, gregorianDay);
            if ((gregorianMonth == 1) && (gregorianDay == 1))
            {
                return (GetResult(result, part));
            }

            long NumDays;
            NumDays = AbsoluteDate - GregorianCalendar.GetAbsoluteDate(gregorianYear, 1, 1);
            if ((NumDays + (long)lunarDate.day) <= (long)(LunarMonthLen[hebrewYearType, lunarDate.month]))
            {
                result.day += (int)NumDays;
                return (GetResult(result, part));
            }

            result.month++;
            result.day = 1;
            NumDays -= (long)(LunarMonthLen[hebrewYearType, lunarDate.month] - lunarDate.day);
                        if (NumDays > 1)
            {
                while (NumDays > (long)(LunarMonthLen[hebrewYearType, result.month]))
                {
                    NumDays -= (long)(LunarMonthLen[hebrewYearType, result.month++]);
                    if ((result.month > 13) || (LunarMonthLen[hebrewYearType, result.month] == 0))
                    {
                        result.year++;
                        hebrewYearType = HebrewTable[(gregorianYear + 1 - FirstGregorianTableYear) * 2 + 1];
                        result.month = 1;
                    }
                }

                result.day += (int)(NumDays - 1);
            }

            return (GetResult(result, part));
        }

        public override DateTime AddMonths(DateTime time, int months)
        {
            try
            {
                int y = GetDatePart(time.Ticks, DatePartYear);
                int m = GetDatePart(time.Ticks, DatePartMonth);
                int d = GetDatePart(time.Ticks, DatePartDay);
                int monthsInYear;
                int i;
                if (months >= 0)
                {
                    i = m + months;
                    while (i > (monthsInYear = GetMonthsInYear(y, CurrentEra)))
                    {
                        y++;
                        i -= monthsInYear;
                    }
                }
                else
                {
                    if ((i = m + months) <= 0)
                    {
                        months = -months;
                        months -= m;
                        y--;
                        while (months > (monthsInYear = GetMonthsInYear(y, CurrentEra)))
                        {
                            y--;
                            months -= monthsInYear;
                        }

                        monthsInYear = GetMonthsInYear(y, CurrentEra);
                        i = monthsInYear - months;
                    }
                }

                int days = GetDaysInMonth(y, i);
                if (d > days)
                {
                    d = days;
                }

                return (new DateTime(ToDateTime(y, i, d, 0, 0, 0, 0).Ticks + (time.Ticks % TicksPerDay)));
            }
            catch (ArgumentException)
            {
                throw new ArgumentOutOfRangeException("months", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_AddValue")));
            }
        }

        public override DateTime AddYears(DateTime time, int years)
        {
            int y = GetDatePart(time.Ticks, DatePartYear);
            int m = GetDatePart(time.Ticks, DatePartMonth);
            int d = GetDatePart(time.Ticks, DatePartDay);
            y += years;
            CheckHebrewYearValue(y, Calendar.CurrentEra, "years");
            int months = GetMonthsInYear(y, CurrentEra);
            if (m > months)
            {
                m = months;
            }

            int days = GetDaysInMonth(y, m);
            if (d > days)
            {
                d = days;
            }

            long ticks = ToDateTime(y, m, d, 0, 0, 0, 0).Ticks + (time.Ticks % TicksPerDay);
            Calendar.CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return (new DateTime(ticks));
        }

        public override int GetDayOfMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDay));
        }

        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            return ((DayOfWeek)((int)(time.Ticks / TicksPerDay + 1) % 7));
        }

        static internal int GetHebrewYearType(int year, int era)
        {
            CheckHebrewYearValue(year, era, "year");
            return (HebrewTable[(year - HebrewYearOf1AD - FirstGregorianTableYear) * 2 + 1]);
        }

        public override int GetDayOfYear(DateTime time)
        {
            int year = GetYear(time);
            DateTime beginOfYearDate;
            if (year == 5343)
            {
                beginOfYearDate = new DateTime(1582, 9, 27);
            }
            else
            {
                beginOfYearDate = ToDateTime(year, 1, 1, 0, 0, 0, 0, CurrentEra);
            }

            return ((int)((time.Ticks - beginOfYearDate.Ticks) / TicksPerDay) + 1);
        }

        public override int GetDaysInMonth(int year, int month, int era)
        {
            CheckEraRange(era);
            int hebrewYearType = GetHebrewYearType(year, era);
            CheckHebrewMonthValue(year, month, era);
                        int monthDays = LunarMonthLen[hebrewYearType, month];
            if (monthDays == 0)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
            }

            return (monthDays);
        }

        public override int GetDaysInYear(int year, int era)
        {
            CheckEraRange(era);
            int LunarYearType = GetHebrewYearType(year, era);
            if (LunarYearType < 4)
            {
                return (352 + LunarYearType);
            }

            return (382 + (LunarYearType - 3));
        }

        public override int GetEra(DateTime time)
        {
            return (HebrewEra);
        }

        public override int[] Eras
        {
            get
            {
                return (new int[]{HebrewEra});
            }
        }

        public override int GetMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartMonth));
        }

        public override int GetMonthsInYear(int year, int era)
        {
            return (IsLeapYear(year, era) ? 13 : 12);
        }

        public override int GetYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartYear));
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            if (IsLeapMonth(year, month, era))
            {
                CheckHebrewDayValue(year, month, day, era);
                return (true);
            }
            else if (IsLeapYear(year, Calendar.CurrentEra))
            {
                if (month == 6 && day == 30)
                {
                    return (true);
                }
            }

            CheckHebrewDayValue(year, month, day, era);
            return (false);
        }

        public override int GetLeapMonth(int year, int era)
        {
            if (IsLeapYear(year, era))
            {
                return (7);
            }

            return (0);
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            bool isLeapYear = IsLeapYear(year, era);
            CheckHebrewMonthValue(year, month, era);
            if (isLeapYear)
            {
                if (month == 7)
                {
                    return (true);
                }
            }

            return (false);
        }

        public override bool IsLeapYear(int year, int era)
        {
            CheckHebrewYearValue(year, era, "year");
            return (((7 * (long)year + 1) % 19) < 7);
        }

        static int GetDayDifference(int lunarYearType, int month1, int day1, int month2, int day2)
        {
            if (month1 == month2)
            {
                return (day1 - day2);
            }

            bool swap = (month1 > month2);
            if (swap)
            {
                int tempMonth, tempDay;
                tempMonth = month1;
                tempDay = day1;
                month1 = month2;
                day1 = day2;
                month2 = tempMonth;
                day2 = tempDay;
            }

            int days = LunarMonthLen[lunarYearType, month1] - day1;
            month1++;
            while (month1 < month2)
            {
                days += LunarMonthLen[lunarYearType, month1++];
            }

            days += day2;
            return (swap ? days : -days);
        }

        static DateTime HebrewToGregorian(int hebrewYear, int hebrewMonth, int hebrewDay, int hour, int minute, int second, int millisecond)
        {
            int gregorianYear = hebrewYear - HebrewYearOf1AD;
            __DateBuffer hebrewDateOfJan1 = new __DateBuffer();
            int lunarYearType = GetLunarMonthDay(gregorianYear, hebrewDateOfJan1);
            if ((hebrewMonth == hebrewDateOfJan1.month) && (hebrewDay == hebrewDateOfJan1.day))
            {
                return (new DateTime(gregorianYear, 1, 1, hour, minute, second, millisecond));
            }

            int days = GetDayDifference(lunarYearType, hebrewMonth, hebrewDay, hebrewDateOfJan1.month, hebrewDateOfJan1.day);
            DateTime gregorianNewYear = new DateTime(gregorianYear, 1, 1);
            return (new DateTime(gregorianNewYear.Ticks + days * TicksPerDay + TimeToTicks(hour, minute, second, millisecond)));
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            CheckHebrewYearValue(year, era, "year");
            CheckHebrewMonthValue(year, month, era);
            CheckHebrewDayValue(year, month, day, era);
            DateTime dt = HebrewToGregorian(year, month, day, hour, minute, second, millisecond);
            CheckTicksRange(dt.Ticks);
            return (dt);
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 5790;
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
                VerifyWritable();
                if (value == 99)
                {
                }
                else
                {
                    CheckHebrewYearValue(value, HebrewEra, "value");
                }

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

            if (year > MaxHebrewYear || year < MinHebrewYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), MinHebrewYear, MaxHebrewYear));
            }

            return (year);
        }

        internal class __DateBuffer
        {
            internal int year;
            internal int month;
            internal int day;
        }
    }
}