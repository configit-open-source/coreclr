namespace System.Globalization
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;

    public class KoreanCalendar : Calendar
    {
        public const int KoreanEra = 1;
        static internal EraInfo[] koreanEraInfo = new EraInfo[]{new EraInfo(1, 1, 1, 1, -2333, 2334, GregorianCalendar.MaxYear + 2333)};
        internal GregorianCalendarHelper helper;
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

        public KoreanCalendar()
        {
            try
            {
                new CultureInfo("ko-KR");
            }
            catch (ArgumentException e)
            {
                throw new TypeInitializationException(this.GetType().FullName, e);
            }

            helper = new GregorianCalendarHelper(this, koreanEraInfo);
        }

        internal override int ID
        {
            get
            {
                return (CAL_KOREA);
            }
        }

        public override DateTime AddMonths(DateTime time, int months)
        {
            return (helper.AddMonths(time, months));
        }

        public override DateTime AddYears(DateTime time, int years)
        {
            return (helper.AddYears(time, years));
        }

        public override int GetDaysInMonth(int year, int month, int era)
        {
            return (helper.GetDaysInMonth(year, month, era));
        }

        public override int GetDaysInYear(int year, int era)
        {
            return (helper.GetDaysInYear(year, era));
        }

        public override int GetDayOfMonth(DateTime time)
        {
            return (helper.GetDayOfMonth(time));
        }

        public override DayOfWeek GetDayOfWeek(DateTime time)
        {
            return (helper.GetDayOfWeek(time));
        }

        public override int GetDayOfYear(DateTime time)
        {
            return (helper.GetDayOfYear(time));
        }

        public override int GetMonthsInYear(int year, int era)
        {
            return (helper.GetMonthsInYear(year, era));
        }

        public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            return (helper.GetWeekOfYear(time, rule, firstDayOfWeek));
        }

        public override int GetEra(DateTime time)
        {
            return (helper.GetEra(time));
        }

        public override int GetMonth(DateTime time)
        {
            return (helper.GetMonth(time));
        }

        public override int GetYear(DateTime time)
        {
            return (helper.GetYear(time));
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            return (helper.IsLeapDay(year, month, day, era));
        }

        public override bool IsLeapYear(int year, int era)
        {
            return (helper.IsLeapYear(year, era));
        }

        public override int GetLeapMonth(int year, int era)
        {
            return (helper.GetLeapMonth(year, era));
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            return (helper.IsLeapMonth(year, month, era));
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            return (helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era));
        }

        public override int[] Eras
        {
            get
            {
                return (helper.Eras);
            }
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 4362;
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
                if (value < 99 || value > helper.MaxYear)
                {
                    throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 99, helper.MaxYear));
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
            return (helper.ToFourDigitYear(year, this.TwoDigitYearMax));
        }
    }
}