using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public abstract class Calendar : ICloneable
    {
        internal const long TicksPerMillisecond = 10000;
        internal const long TicksPerSecond = TicksPerMillisecond * 1000;
        internal const long TicksPerMinute = TicksPerSecond * 60;
        internal const long TicksPerHour = TicksPerMinute * 60;
        internal const long TicksPerDay = TicksPerHour * 24;
        internal const int MillisPerSecond = 1000;
        internal const int MillisPerMinute = MillisPerSecond * 60;
        internal const int MillisPerHour = MillisPerMinute * 60;
        internal const int MillisPerDay = MillisPerHour * 24;
        internal const int DaysPerYear = 365;
        internal const int DaysPer4Years = DaysPerYear * 4 + 1;
        internal const int DaysPer100Years = DaysPer4Years * 25 - 1;
        internal const int DaysPer400Years = DaysPer100Years * 4 + 1;
        internal const int DaysTo10000 = DaysPer400Years * 25 - 366;
        internal const long MaxMillis = (long)DaysTo10000 * MillisPerDay;
        internal const int CAL_GREGORIAN = 1;
        internal const int CAL_GREGORIAN_US = 2;
        internal const int CAL_JAPAN = 3;
        internal const int CAL_TAIWAN = 4;
        internal const int CAL_KOREA = 5;
        internal const int CAL_HIJRI = 6;
        internal const int CAL_THAI = 7;
        internal const int CAL_HEBREW = 8;
        internal const int CAL_GREGORIAN_ME_FRENCH = 9;
        internal const int CAL_GREGORIAN_ARABIC = 10;
        internal const int CAL_GREGORIAN_XLIT_ENGLISH = 11;
        internal const int CAL_GREGORIAN_XLIT_FRENCH = 12;
        internal const int CAL_JULIAN = 13;
        internal const int CAL_JAPANESELUNISOLAR = 14;
        internal const int CAL_CHINESELUNISOLAR = 15;
        internal const int CAL_SAKA = 16;
        internal const int CAL_LUNAR_ETO_CHN = 17;
        internal const int CAL_LUNAR_ETO_KOR = 18;
        internal const int CAL_LUNAR_ETO_ROKUYOU = 19;
        internal const int CAL_KOREANLUNISOLAR = 20;
        internal const int CAL_TAIWANLUNISOLAR = 21;
        internal const int CAL_PERSIAN = 22;
        internal const int CAL_UMALQURA = 23;
        internal int m_currentEraValue = -1;
        private bool m_isReadOnly = false;
        public virtual DateTime MinSupportedDateTime
        {
            get
            {
                return (DateTime.MinValue);
            }
        }

        public virtual DateTime MaxSupportedDateTime
        {
            get
            {
                return (DateTime.MaxValue);
            }
        }

        protected Calendar()
        {
        }

        internal virtual int ID
        {
            get
            {
                return (-1);
            }
        }

        internal virtual int BaseCalendarID
        {
            get
            {
                return ID;
            }
        }

        public virtual CalendarAlgorithmType AlgorithmType
        {
            get
            {
                return CalendarAlgorithmType.Unknown;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return (m_isReadOnly);
            }
        }

        public virtual Object Clone()
        {
            object o = MemberwiseClone();
            ((Calendar)o).SetReadOnlyState(false);
            return (o);
        }

        public static Calendar ReadOnly(Calendar calendar)
        {
            if (calendar == null)
            {
                throw new ArgumentNullException("calendar");
            }

            Contract.EndContractBlock();
            if (calendar.IsReadOnly)
            {
                return (calendar);
            }

            Calendar clonedCalendar = (Calendar)(calendar.MemberwiseClone());
            clonedCalendar.SetReadOnlyState(true);
            return (clonedCalendar);
        }

        internal void VerifyWritable()
        {
            if (m_isReadOnly)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }
        }

        internal void SetReadOnlyState(bool readOnly)
        {
            m_isReadOnly = readOnly;
        }

        internal virtual int CurrentEraValue
        {
            get
            {
                if (m_currentEraValue == -1)
                {
                    Contract.Assert(BaseCalendarID > 0, "[Calendar.CurrentEraValue] Expected ID > 0");
                    m_currentEraValue = CalendarData.GetCalendarData(BaseCalendarID).iCurrentEra;
                }

                return (m_currentEraValue);
            }
        }

        public const int CurrentEra = 0;
        internal int twoDigitYearMax = -1;
        internal static void CheckAddResult(long ticks, DateTime minValue, DateTime maxValue)
        {
            if (ticks < minValue.Ticks || ticks > maxValue.Ticks)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Argument_ResultCalendarRange"), minValue, maxValue));
            }

            Contract.EndContractBlock();
        }

        internal DateTime Add(DateTime time, double value, int scale)
        {
            double tempMillis = (value * scale + (value >= 0 ? 0.5 : -0.5));
            if (!((tempMillis > -(double)MaxMillis) && (tempMillis < (double)MaxMillis)))
            {
                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_AddValue"));
            }

            long millis = (long)tempMillis;
            long ticks = time.Ticks + millis * TicksPerMillisecond;
            CheckAddResult(ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return (new DateTime(ticks));
        }

        public virtual DateTime AddMilliseconds(DateTime time, double milliseconds)
        {
            return (Add(time, milliseconds, 1));
        }

        public virtual DateTime AddDays(DateTime time, int days)
        {
            return (Add(time, days, MillisPerDay));
        }

        public virtual DateTime AddHours(DateTime time, int hours)
        {
            return (Add(time, hours, MillisPerHour));
        }

        public virtual DateTime AddMinutes(DateTime time, int minutes)
        {
            return (Add(time, minutes, MillisPerMinute));
        }

        public abstract DateTime AddMonths(DateTime time, int months);
        public virtual DateTime AddSeconds(DateTime time, int seconds)
        {
            return Add(time, seconds, MillisPerSecond);
        }

        public virtual DateTime AddWeeks(DateTime time, int weeks)
        {
            return (AddDays(time, weeks * 7));
        }

        public abstract DateTime AddYears(DateTime time, int years);
        public abstract int GetDayOfMonth(DateTime time);
        public abstract DayOfWeek GetDayOfWeek(DateTime time);
        public abstract int GetDayOfYear(DateTime time);
        public virtual int GetDaysInMonth(int year, int month)
        {
            return (GetDaysInMonth(year, month, CurrentEra));
        }

        public abstract int GetDaysInMonth(int year, int month, int era);
        public virtual int GetDaysInYear(int year)
        {
            return (GetDaysInYear(year, CurrentEra));
        }

        public abstract int GetDaysInYear(int year, int era);
        public abstract int GetEra(DateTime time);
        public abstract int[] Eras
        {
            get;
        }

        public virtual int GetHour(DateTime time)
        {
            return ((int)((time.Ticks / TicksPerHour) % 24));
        }

        public virtual double GetMilliseconds(DateTime time)
        {
            return (double)((time.Ticks / TicksPerMillisecond) % 1000);
        }

        public virtual int GetMinute(DateTime time)
        {
            return ((int)((time.Ticks / TicksPerMinute) % 60));
        }

        public abstract int GetMonth(DateTime time);
        public virtual int GetMonthsInYear(int year)
        {
            return (GetMonthsInYear(year, CurrentEra));
        }

        public abstract int GetMonthsInYear(int year, int era);
        public virtual int GetSecond(DateTime time)
        {
            return ((int)((time.Ticks / TicksPerSecond) % 60));
        }

        internal int GetFirstDayWeekOfYear(DateTime time, int firstDayOfWeek)
        {
            int dayOfYear = GetDayOfYear(time) - 1;
            int dayForJan1 = (int)GetDayOfWeek(time) - (dayOfYear % 7);
            int offset = (dayForJan1 - firstDayOfWeek + 14) % 7;
            Contract.Assert(offset >= 0, "Calendar.GetFirstDayWeekOfYear(): offset >= 0");
            return ((dayOfYear + offset) / 7 + 1);
        }

        private int GetWeekOfYearFullDays(DateTime time, int firstDayOfWeek, int fullDays)
        {
            int dayForJan1;
            int offset;
            int day;
            int dayOfYear = GetDayOfYear(time) - 1;
            dayForJan1 = (int)GetDayOfWeek(time) - (dayOfYear % 7);
            offset = (firstDayOfWeek - dayForJan1 + 14) % 7;
            if (offset != 0 && offset >= fullDays)
            {
                offset -= 7;
            }

            day = dayOfYear - offset;
            if (day >= 0)
            {
                return (day / 7 + 1);
            }

            if (time <= MinSupportedDateTime.AddDays(dayOfYear))
            {
                return GetWeekOfYearOfMinSupportedDateTime(firstDayOfWeek, fullDays);
            }

            return (GetWeekOfYearFullDays(time.AddDays(-(dayOfYear + 1)), firstDayOfWeek, fullDays));
        }

        private int GetWeekOfYearOfMinSupportedDateTime(int firstDayOfWeek, int minimumDaysInFirstWeek)
        {
            int dayOfYear = GetDayOfYear(MinSupportedDateTime) - 1;
            int dayOfWeekOfFirstOfYear = (int)GetDayOfWeek(MinSupportedDateTime) - dayOfYear % 7;
            int offset = (firstDayOfWeek + 7 - dayOfWeekOfFirstOfYear) % 7;
            if (offset == 0 || offset >= minimumDaysInFirstWeek)
            {
                return 1;
            }

            int daysInYearBeforeMinSupportedYear = DaysInYearBeforeMinSupportedYear - 1;
            int dayOfWeekOfFirstOfPreviousYear = dayOfWeekOfFirstOfYear - 1 - (daysInYearBeforeMinSupportedYear % 7);
            int daysInInitialPartialWeek = (firstDayOfWeek - dayOfWeekOfFirstOfPreviousYear + 14) % 7;
            int day = daysInYearBeforeMinSupportedYear - daysInInitialPartialWeek;
            if (daysInInitialPartialWeek >= minimumDaysInFirstWeek)
            {
                day += 7;
            }

            return (day / 7 + 1);
        }

        protected virtual int DaysInYearBeforeMinSupportedYear
        {
            get
            {
                return 365;
            }
        }

        public virtual int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            if ((int)firstDayOfWeek < 0 || (int)firstDayOfWeek > 6)
            {
                throw new ArgumentOutOfRangeException("firstDayOfWeek", Environment.GetResourceString("ArgumentOutOfRange_Range", DayOfWeek.Sunday, DayOfWeek.Saturday));
            }

            Contract.EndContractBlock();
            switch (rule)
            {
                case CalendarWeekRule.FirstDay:
                    return (GetFirstDayWeekOfYear(time, (int)firstDayOfWeek));
                case CalendarWeekRule.FirstFullWeek:
                    return (GetWeekOfYearFullDays(time, (int)firstDayOfWeek, 7));
                case CalendarWeekRule.FirstFourDayWeek:
                    return (GetWeekOfYearFullDays(time, (int)firstDayOfWeek, 4));
            }

            throw new ArgumentOutOfRangeException("rule", Environment.GetResourceString("ArgumentOutOfRange_Range", CalendarWeekRule.FirstDay, CalendarWeekRule.FirstFourDayWeek));
        }

        public abstract int GetYear(DateTime time);
        public virtual bool IsLeapDay(int year, int month, int day)
        {
            return (IsLeapDay(year, month, day, CurrentEra));
        }

        public abstract bool IsLeapDay(int year, int month, int day, int era);
        public virtual bool IsLeapMonth(int year, int month)
        {
            return (IsLeapMonth(year, month, CurrentEra));
        }

        public abstract bool IsLeapMonth(int year, int month, int era);
        public virtual int GetLeapMonth(int year)
        {
            return (GetLeapMonth(year, CurrentEra));
        }

        public virtual int GetLeapMonth(int year, int era)
        {
            if (!IsLeapYear(year, era))
                return 0;
            int monthsCount = GetMonthsInYear(year, era);
            for (int month = 1; month <= monthsCount; month++)
            {
                if (IsLeapMonth(year, month, era))
                    return month;
            }

            return 0;
        }

        public virtual bool IsLeapYear(int year)
        {
            return (IsLeapYear(year, CurrentEra));
        }

        public abstract bool IsLeapYear(int year, int era);
        public virtual DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond)
        {
            return (ToDateTime(year, month, day, hour, minute, second, millisecond, CurrentEra));
        }

        public abstract DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era);
        internal virtual Boolean TryToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era, out DateTime result)
        {
            result = DateTime.MinValue;
            try
            {
                result = ToDateTime(year, month, day, hour, minute, second, millisecond, era);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        internal virtual bool IsValidYear(int year, int era)
        {
            return (year >= GetYear(MinSupportedDateTime) && year <= GetYear(MaxSupportedDateTime));
        }

        internal virtual bool IsValidMonth(int year, int month, int era)
        {
            return (IsValidYear(year, era) && month >= 1 && month <= GetMonthsInYear(year, era));
        }

        internal virtual bool IsValidDay(int year, int month, int day, int era)
        {
            return (IsValidMonth(year, month, era) && day >= 1 && day <= GetDaysInMonth(year, month, era));
        }

        public virtual int TwoDigitYearMax
        {
            get
            {
                return (twoDigitYearMax);
            }

            set
            {
                VerifyWritable();
                twoDigitYearMax = value;
            }
        }

        public virtual int ToFourDigitYear(int year)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.EndContractBlock();
            if (year < 100)
            {
                return ((TwoDigitYearMax / 100 - (year > TwoDigitYearMax % 100 ? 1 : 0)) * 100 + year);
            }

            return (year);
        }

        internal static long TimeToTicks(int hour, int minute, int second, int millisecond)
        {
            if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60 && second >= 0 && second < 60)
            {
                if (millisecond < 0 || millisecond >= MillisPerSecond)
                {
                    throw new ArgumentOutOfRangeException("millisecond", String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, MillisPerSecond - 1));
                }

                return TimeSpan.TimeToTicks(hour, minute, second) + millisecond * TicksPerMillisecond;
            }

            throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadHourMinuteSecond"));
        }

        internal static int GetSystemTwoDigitYearSetting(int CalID, int defaultYearValue)
        {
            int twoDigitYearMax = CalendarData.nativeGetTwoDigitYearMax(CalID);
            if (twoDigitYearMax < 0)
            {
                twoDigitYearMax = defaultYearValue;
            }

            return (twoDigitYearMax);
        }
    }
}