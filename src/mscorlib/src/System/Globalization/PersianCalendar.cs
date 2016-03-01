using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public class PersianCalendar : Calendar
    {
        public static readonly int PersianEra = 1;
        internal static long PersianEpoch = new DateTime(622, 3, 22).Ticks / GregorianCalendar.TicksPerDay;
        const int ApproximateHalfYear = 180;
        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;
        internal const int MonthsPerYear = 12;
        internal static int[] DaysToMonth = {0, 31, 62, 93, 124, 155, 186, 216, 246, 276, 306, 336, 366};
        internal const int MaxCalendarYear = 9378;
        internal const int MaxCalendarMonth = 10;
        internal const int MaxCalendarDay = 13;
        internal static DateTime minDate = new DateTime(622, 3, 22);
        internal static DateTime maxDate = DateTime.MaxValue;
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
                return CalendarAlgorithmType.SolarCalendar;
            }
        }

        public PersianCalendar()
        {
        }

        internal override int BaseCalendarID
        {
            get
            {
                return (CAL_GREGORIAN);
            }
        }

        internal override int ID
        {
            get
            {
                return (CAL_PERSIAN);
            }
        }

        long GetAbsoluteDatePersian(int year, int month, int day)
        {
            if (year >= 1 && year <= MaxCalendarYear && month >= 1 && month <= 12)
            {
                int ordinalDay = DaysInPreviousMonths(month) + day - 1;
                int approximateDaysFromEpochForYearStart = (int)(CalendricalCalculationsHelper.MeanTropicalYearInDays * (year - 1));
                long yearStart = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(PersianEpoch + approximateDaysFromEpochForYearStart + ApproximateHalfYear);
                yearStart += ordinalDay;
                return yearStart;
            }

            throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
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
            if (era != CurrentEra && era != PersianEra)
            {
                throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
            }
        }

        static internal void CheckYearRange(int year, int era)
        {
            CheckEraRange(era);
            if (year < 1 || year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxCalendarYear));
            }
        }

        static internal void CheckYearMonthRange(int year, int month, int era)
        {
            CheckYearRange(year, era);
            if (year == MaxCalendarYear)
            {
                if (month > MaxCalendarMonth)
                {
                    throw new ArgumentOutOfRangeException("month", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxCalendarMonth));
                }
            }

            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
            }
        }

        static int MonthFromOrdinalDay(int ordinalDay)
        {
            Contract.Assert(ordinalDay <= 366);
            int index = 0;
            while (ordinalDay > DaysToMonth[index])
                index++;
            return index;
        }

        static int DaysInPreviousMonths(int month)
        {
            Contract.Assert(1 <= month && month <= 12);
            --month;
            return DaysToMonth[month];
        }

        internal int GetDatePart(long ticks, int part)
        {
            long NumDays;
            CheckTicksRange(ticks);
            NumDays = ticks / GregorianCalendar.TicksPerDay + 1;
            long yearStart = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(NumDays);
            int y = (int)(Math.Floor(((yearStart - PersianEpoch) / CalendricalCalculationsHelper.MeanTropicalYearInDays) + 0.5)) + 1;
            Contract.Assert(y >= 1);
            if (part == DatePartYear)
            {
                return y;
            }

            int ordinalDay = (int)(NumDays - CalendricalCalculationsHelper.GetNumberOfDays(this.ToDateTime(y, 1, 1, 0, 0, 0, 0, 1)));
            if (part == DatePartDayOfYear)
            {
                return ordinalDay;
            }

            int m = MonthFromOrdinalDay(ordinalDay);
            Contract.Assert(ordinalDay >= 1);
            Contract.Assert(m >= 1 && m <= 12);
            if (part == DatePartMonth)
            {
                return m;
            }

            int d = ordinalDay - DaysInPreviousMonths(m);
            Contract.Assert(1 <= d);
            Contract.Assert(d <= 31);
            if (part == DatePartDay)
            {
                return (d);
            }

            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DateTimeParsing"));
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

            int days = GetDaysInMonth(y, m);
            if (d > days)
            {
                d = days;
            }

            long ticks = GetAbsoluteDatePersian(y, m, d) * TicksPerDay + time.Ticks % TicksPerDay;
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
            CheckYearMonthRange(year, month, era);
            if ((month == MaxCalendarMonth) && (year == MaxCalendarYear))
            {
                return MaxCalendarDay;
            }

            int daysInMonth = DaysToMonth[month] - DaysToMonth[month - 1];
            if ((month == MonthsPerYear) && !IsLeapYear(year))
            {
                Contract.Assert(daysInMonth == 30);
                --daysInMonth;
            }

            return daysInMonth;
        }

        public override int GetDaysInYear(int year, int era)
        {
            CheckYearRange(year, era);
            if (year == MaxCalendarYear)
            {
                return DaysToMonth[MaxCalendarMonth - 1] + MaxCalendarDay;
            }

            return (IsLeapYear(year, CurrentEra) ? 366 : 365);
        }

        public override int GetEra(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return (PersianEra);
        }

        public override int[] Eras
        {
            get
            {
                return (new int[]{PersianEra});
            }
        }

        public override int GetMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartMonth));
        }

        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearRange(year, era);
            if (year == MaxCalendarYear)
            {
                return MaxCalendarMonth;
            }

            return (12);
        }

        public override int GetYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartYear));
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException("day", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
            }

            return (IsLeapYear(year, era) && month == 12 && day == 30);
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
            if (year == MaxCalendarYear)
            {
                return false;
            }

            return (GetAbsoluteDatePersian(year + 1, 1, 1) - GetAbsoluteDatePersian(year, 1, 1)) == 366;
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                BCLDebug.Log("year = " + year + ", month = " + month + ", day = " + day);
                throw new ArgumentOutOfRangeException("day", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
            }

            long lDate = GetAbsoluteDatePersian(year, month, day);
            if (lDate >= 0)
            {
                return (new DateTime(lDate * GregorianCalendar.TicksPerDay + TimeToTicks(hour, minute, second, millisecond)));
            }
            else
            {
                throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
            }
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 1410;
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
                if (value < 99 || value > MaxCalendarYear)
                {
                    throw new ArgumentOutOfRangeException("value", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 99, MaxCalendarYear));
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
            if (year < 100)
            {
                return (base.ToFourDigitYear(year));
            }

            if (year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, MaxCalendarYear));
            }

            return (year);
        }
    }
}