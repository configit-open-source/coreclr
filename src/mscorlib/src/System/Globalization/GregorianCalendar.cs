namespace System.Globalization
{
    using System.Threading;
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    public class GregorianCalendar : Calendar
    {
        public const int ADEra = 1;
        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;
        internal const int MaxYear = 9999;
        internal GregorianCalendarTypes m_type;
        internal static readonly int[] DaysToMonth365 = {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};
        internal static readonly int[] DaysToMonth366 = {0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366};
        private static volatile Calendar s_defaultInstance;
        private void OnDeserialized(StreamingContext ctx)
        {
            if (m_type < GregorianCalendarTypes.Localized || m_type > GregorianCalendarTypes.TransliteratedFrench)
            {
                throw new SerializationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Serialization_MemberOutOfRange"), "type", "GregorianCalendar"));
            }
        }

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

        internal static Calendar GetDefaultInstance()
        {
            if (s_defaultInstance == null)
            {
                s_defaultInstance = new GregorianCalendar();
            }

            return (s_defaultInstance);
        }

        public GregorianCalendar(): this (GregorianCalendarTypes.Localized)
        {
        }

        public GregorianCalendar(GregorianCalendarTypes type)
        {
            if ((int)type < (int)GregorianCalendarTypes.Localized || (int)type > (int)GregorianCalendarTypes.TransliteratedFrench)
            {
                throw new ArgumentOutOfRangeException("type", Environment.GetResourceString("ArgumentOutOfRange_Range", GregorianCalendarTypes.Localized, GregorianCalendarTypes.TransliteratedFrench));
            }

            Contract.EndContractBlock();
            this.m_type = type;
        }

        public virtual GregorianCalendarTypes CalendarType
        {
            get
            {
                return (m_type);
            }

            set
            {
                VerifyWritable();
                switch (value)
                {
                    case GregorianCalendarTypes.Localized:
                    case GregorianCalendarTypes.USEnglish:
                    case GregorianCalendarTypes.MiddleEastFrench:
                    case GregorianCalendarTypes.Arabic:
                    case GregorianCalendarTypes.TransliteratedEnglish:
                    case GregorianCalendarTypes.TransliteratedFrench:
                        m_type = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("m_type", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                }
            }
        }

        internal override int ID
        {
            get
            {
                return ((int)m_type);
            }
        }

        internal virtual int GetDatePart(long ticks, int part)
        {
            int n = (int)(ticks / TicksPerDay);
            int y400 = n / DaysPer400Years;
            n -= y400 * DaysPer400Years;
            int y100 = n / DaysPer100Years;
            if (y100 == 4)
                y100 = 3;
            n -= y100 * DaysPer100Years;
            int y4 = n / DaysPer4Years;
            n -= y4 * DaysPer4Years;
            int y1 = n / DaysPerYear;
            if (y1 == 4)
                y1 = 3;
            if (part == DatePartYear)
            {
                return (y400 * 400 + y100 * 100 + y4 * 4 + y1 + 1);
            }

            n -= y1 * DaysPerYear;
            if (part == DatePartDayOfYear)
            {
                return (n + 1);
            }

            bool leapYear = (y1 == 3 && (y4 != 24 || y100 == 3));
            int[] days = leapYear ? DaysToMonth366 : DaysToMonth365;
            int m = n >> 5 + 1;
            while (n >= days[m])
                m++;
            if (part == DatePartMonth)
                return (m);
            return (n - days[m - 1] + 1);
        }

        internal static long GetAbsoluteDate(int year, int month, int day)
        {
            if (year >= 1 && year <= MaxYear && month >= 1 && month <= 12)
            {
                int[] days = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0))) ? DaysToMonth366 : DaysToMonth365;
                if (day >= 1 && (day <= days[month] - days[month - 1]))
                {
                    int y = year - 1;
                    int absoluteDate = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
                    return (absoluteDate);
                }
            }

            throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
        }

        internal virtual long DateToTicks(int year, int month, int day)
        {
            return (GetAbsoluteDate(year, month, day) * TicksPerDay);
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
            if (era == CurrentEra || era == ADEra)
            {
                if (year < 1 || year > MaxYear)
                {
                    throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_Range", 1, MaxYear));
                }

                if (month < 1 || month > 12)
                {
                    throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
                }

                int[] days = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366 : DaysToMonth365);
                return (days[month] - days[month - 1]);
            }

            throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
        }

        public override int GetDaysInYear(int year, int era)
        {
            if (era == CurrentEra || era == ADEra)
            {
                if (year >= 1 && year <= MaxYear)
                {
                    return ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? 366 : 365);
                }

                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }

            throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
        }

        public override int GetEra(DateTime time)
        {
            return (ADEra);
        }

        public override int[] Eras
        {
            get
            {
                return (new int[]{ADEra});
            }
        }

        public override int GetMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartMonth));
        }

        public override int GetMonthsInYear(int year, int era)
        {
            if (era == CurrentEra || era == ADEra)
            {
                if (year >= 1 && year <= MaxYear)
                {
                    return (12);
                }

                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }

            throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
        }

        public override int GetYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartYear));
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", 1, 12));
            }

            Contract.EndContractBlock();
            if (era != CurrentEra && era != ADEra)
            {
                throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
            }

            if (year < 1 || year > MaxYear)
            {
                throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_Range", 1, MaxYear));
            }

            if (day < 1 || day > GetDaysInMonth(year, month))
            {
                throw new ArgumentOutOfRangeException("day", Environment.GetResourceString("ArgumentOutOfRange_Range", 1, GetDaysInMonth(year, month)));
            }

            if (!IsLeapYear(year))
            {
                return (false);
            }

            if (month == 2 && day == 29)
            {
                return (true);
            }

            return (false);
        }

        public override int GetLeapMonth(int year, int era)
        {
            if (era != CurrentEra && era != ADEra)
            {
                throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
            }

            if (year < 1 || year > MaxYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }

            Contract.EndContractBlock();
            return (0);
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            if (era != CurrentEra && era != ADEra)
            {
                throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
            }

            if (year < 1 || year > MaxYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", 1, 12));
            }

            Contract.EndContractBlock();
            return (false);
        }

        public override bool IsLeapYear(int year, int era)
        {
            if (era == CurrentEra || era == ADEra)
            {
                if (year >= 1 && year <= MaxYear)
                {
                    return (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));
                }

                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }

            throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            if (era == CurrentEra || era == ADEra)
            {
                return new DateTime(year, month, day, hour, minute, second, millisecond);
            }

            throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
        }

        internal override Boolean TryToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era, out DateTime result)
        {
            if (era == CurrentEra || era == ADEra)
            {
                return DateTime.TryCreate(year, month, day, hour, minute, second, millisecond, out result);
            }

            result = DateTime.MinValue;
            return false;
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 2029;
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
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxYear));
            }

            return (base.ToFourDigitYear(year));
        }
    }
}