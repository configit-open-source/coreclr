using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public class JulianCalendar : Calendar
    {
        public static readonly int JulianEra = 1;
        private const int DatePartYear = 0;
        private const int DatePartDayOfYear = 1;
        private const int DatePartMonth = 2;
        private const int DatePartDay = 3;
        private const int JulianDaysPerYear = 365;
        private const int JulianDaysPer4Years = JulianDaysPerYear * 4 + 1;
        private static readonly int[] DaysToMonth365 = {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};
        private static readonly int[] DaysToMonth366 = {0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366};
        internal int MaxYear = 9999;
        public override DateTime MinSupportedDateTime
        {
            get
            {
                return (DateTime.MinValue);
            }
        }

        public override DateTime MaxSupportedDateTime
        {
            get
            {
                return (DateTime.MaxValue);
            }
        }

        public override CalendarAlgorithmType AlgorithmType
        {
            get
            {
                return CalendarAlgorithmType.SolarCalendar;
            }
        }

        public JulianCalendar()
        {
            twoDigitYearMax = 2029;
        }

        internal override int ID
        {
            get
            {
                return (CAL_JULIAN);
            }
        }

        static internal void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != JulianEra)
            {
                throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
            }
        }

        internal void CheckYearEraRange(int year, int era)
        {
            CheckEraRange(era);
            if (year <= 0 || year > MaxYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }
        }

        static internal void CheckMonthRange(int month)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
            }
        }

        static internal void CheckDayRange(int year, int month, int day)
        {
            if (year == 1 && month == 1)
            {
                if (day < 3)
                {
                    throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
                }
            }

            bool isLeapYear = (year % 4) == 0;
            int[] days = isLeapYear ? DaysToMonth366 : DaysToMonth365;
            int monthDays = days[month] - days[month - 1];
            if (day < 1 || day > monthDays)
            {
                throw new ArgumentOutOfRangeException("day", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, monthDays));
            }
        }

        static internal int GetDatePart(long ticks, int part)
        {
            long julianTicks = ticks + TicksPerDay * 2;
            int n = (int)(julianTicks / TicksPerDay);
            int y4 = n / JulianDaysPer4Years;
            n -= y4 * JulianDaysPer4Years;
            int y1 = n / JulianDaysPerYear;
            if (y1 == 4)
                y1 = 3;
            if (part == DatePartYear)
            {
                return (y4 * 4 + y1 + 1);
            }

            n -= y1 * JulianDaysPerYear;
            if (part == DatePartDayOfYear)
            {
                return (n + 1);
            }

            bool leapYear = (y1 == 3);
            int[] days = leapYear ? DaysToMonth366 : DaysToMonth365;
            int m = n >> 5 + 1;
            while (n >= days[m])
                m++;
            if (part == DatePartMonth)
                return (m);
            return (n - days[m - 1] + 1);
        }

        static internal long DateToTicks(int year, int month, int day)
        {
            int[] days = (year % 4 == 0) ? DaysToMonth366 : DaysToMonth365;
            int y = year - 1;
            int n = y * 365 + y / 4 + days[month - 1] + day - 1;
            return ((n - 2) * TicksPerDay);
        }

        public override DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException("months", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), -120000, 120000));
            }

            Contract.EndContractBlock();
            int y = GetDatePart(time.Ticks, DatePartYear);
            int m = GetDatePart(time.Ticks, DatePartMonth);
            int d = GetDatePart(time.Ticks, DatePartDay);
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

            int[] daysArray = (y % 4 == 0 && (y % 100 != 0 || y % 400 == 0)) ? DaysToMonth366 : DaysToMonth365;
            int days = (daysArray[m] - daysArray[m - 1]);
            if (d > days)
            {
                d = days;
            }

            long ticks = DateToTicks(y, m, d) + time.Ticks % TicksPerDay;
            Calendar.CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return (new DateTime(ticks));
        }

        public override DateTime AddYears(DateTime time, int years)
        {
            return (AddMonths(time, years * 12));
        }

        public override int GetDayOfMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDay));
        }

        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            return ((DayOfWeek)((int)(time.Ticks / TicksPerDay + 1) % 7));
        }

        public override int GetDayOfYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDayOfYear));
        }

        public override int GetDaysInMonth(int year, int month, int era)
        {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            int[] days = (year % 4 == 0) ? DaysToMonth366 : DaysToMonth365;
            return (days[month] - days[month - 1]);
        }

        public override int GetDaysInYear(int year, int era)
        {
            return (IsLeapYear(year, era) ? 366 : 365);
        }

        public override int GetEra(DateTime time)
        {
            return (JulianEra);
        }

        public override int GetMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartMonth));
        }

        public override int[] Eras
        {
            get
            {
                return (new int[]{JulianEra});
            }
        }

        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearEraRange(year, era);
            return (12);
        }

        public override int GetYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartYear));
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            CheckMonthRange(month);
            if (IsLeapYear(year, era))
            {
                CheckDayRange(year, month, day);
                return (month == 2 && day == 29);
            }

            CheckDayRange(year, month, day);
            return (false);
        }

        public override int GetLeapMonth(int year, int era)
        {
            CheckYearEraRange(year, era);
            return (0);
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            return (false);
        }

        public override bool IsLeapYear(int year, int era)
        {
            CheckYearEraRange(year, era);
            return (year % 4 == 0);
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            CheckYearEraRange(year, era);
            CheckMonthRange(month);
            CheckDayRange(year, month, day);
            if (millisecond < 0 || millisecond >= MillisPerSecond)
            {
                throw new ArgumentOutOfRangeException("millisecond", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, MillisPerSecond - 1));
            }

            if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60 && second >= 0 && second < 60)
            {
                return new DateTime(DateToTicks(year, month, day) + (new TimeSpan(0, hour, minute, second, millisecond)).Ticks);
            }
            else
            {
                throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadHourMinuteSecond"));
            }
        }

        public override int TwoDigitYearMax
        {
            get
            {
                return (twoDigitYearMax);
            }

            set
            {
                VerifyWritable();
                if (value < 99 || value > MaxYear)
                {
                    throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 99, MaxYear));
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

            Contract.EndContractBlock();
            if (year > MaxYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"), 1, MaxYear));
            }

            return (base.ToFourDigitYear(year));
        }
    }
}