using System.Diagnostics.Contracts;

namespace System.Globalization
{
    internal class EraInfo
    {
        internal int era;
        internal long ticks;
        internal int yearOffset;
        internal int minEraYear;
        internal int maxEraYear;
        internal String eraName;
        internal String abbrevEraName;
        internal String englishEraName;
        internal EraInfo(int era, int startYear, int startMonth, int startDay, int yearOffset, int minEraYear, int maxEraYear)
        {
            this.era = era;
            this.yearOffset = yearOffset;
            this.minEraYear = minEraYear;
            this.maxEraYear = maxEraYear;
            this.ticks = new DateTime(startYear, startMonth, startDay).Ticks;
        }

        internal EraInfo(int era, int startYear, int startMonth, int startDay, int yearOffset, int minEraYear, int maxEraYear, String eraName, String abbrevEraName, String englishEraName)
        {
            this.era = era;
            this.yearOffset = yearOffset;
            this.minEraYear = minEraYear;
            this.maxEraYear = maxEraYear;
            this.ticks = new DateTime(startYear, startMonth, startDay).Ticks;
            this.eraName = eraName;
            this.abbrevEraName = abbrevEraName;
            this.englishEraName = englishEraName;
        }
    }

    internal class GregorianCalendarHelper
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
        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;
        internal int MaxYear
        {
            get
            {
                return (m_maxYear);
            }
        }

        internal static readonly int[] DaysToMonth365 = {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};
        internal static readonly int[] DaysToMonth366 = {0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366};
        internal int m_maxYear = 9999;
        internal int m_minYear;
        internal Calendar m_Cal;
        internal EraInfo[] m_EraInfo;
        internal int[] m_eras = null;
        internal DateTime m_minDate;
        internal GregorianCalendarHelper(Calendar cal, EraInfo[] eraInfo)
        {
            m_Cal = cal;
            m_EraInfo = eraInfo;
            m_minDate = m_Cal.MinSupportedDateTime;
            m_maxYear = m_EraInfo[0].maxEraYear;
            m_minYear = m_EraInfo[0].minEraYear;
            ;
        }

        internal int GetGregorianYear(int year, int era)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.EndContractBlock();
            if (era == Calendar.CurrentEra)
            {
                era = m_Cal.CurrentEraValue;
            }

            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                if (era == m_EraInfo[i].era)
                {
                    if (year < m_EraInfo[i].minEraYear || year > m_EraInfo[i].maxEraYear)
                    {
                        throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), m_EraInfo[i].minEraYear, m_EraInfo[i].maxEraYear));
                    }

                    return (m_EraInfo[i].yearOffset + year);
                }
            }

            throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
        }

        internal bool IsValidYear(int year, int era)
        {
            if (year < 0)
            {
                return false;
            }

            if (era == Calendar.CurrentEra)
            {
                era = m_Cal.CurrentEraValue;
            }

            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                if (era == m_EraInfo[i].era)
                {
                    if (year < m_EraInfo[i].minEraYear || year > m_EraInfo[i].maxEraYear)
                    {
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        internal virtual int GetDatePart(long ticks, int part)
        {
            CheckTicksRange(ticks);
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
            if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
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

        internal static long DateToTicks(int year, int month, int day)
        {
            return (GetAbsoluteDate(year, month, day) * TicksPerDay);
        }

        internal static long TimeToTicks(int hour, int minute, int second, int millisecond)
        {
            if (hour >= 0 && hour < 24 && minute >= 0 && minute < 60 && second >= 0 && second < 60)
            {
                if (millisecond < 0 || millisecond >= MillisPerSecond)
                {
                    throw new ArgumentOutOfRangeException("millisecond", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, MillisPerSecond - 1));
                }

                return (TimeSpan.TimeToTicks(hour, minute, second) + millisecond * TicksPerMillisecond);
                ;
            }

            throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadHourMinuteSecond"));
        }

        internal void CheckTicksRange(long ticks)
        {
            if (ticks < m_Cal.MinSupportedDateTime.Ticks || ticks > m_Cal.MaxSupportedDateTime.Ticks)
            {
                throw new ArgumentOutOfRangeException("time", String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"), m_Cal.MinSupportedDateTime, m_Cal.MaxSupportedDateTime));
            }

            Contract.EndContractBlock();
        }

        public DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException("months", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), -120000, 120000));
            }

            Contract.EndContractBlock();
            CheckTicksRange(time.Ticks);
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

            long ticks = DateToTicks(y, m, d) + (time.Ticks % TicksPerDay);
            Calendar.CheckAddResult(ticks, m_Cal.MinSupportedDateTime, m_Cal.MaxSupportedDateTime);
            return (new DateTime(ticks));
        }

        public DateTime AddYears(DateTime time, int years)
        {
            return (AddMonths(time, years * 12));
        }

        public int GetDayOfMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDay));
        }

        public DayOfWeek GetDayOfWeek(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return ((DayOfWeek)((time.Ticks / TicksPerDay + 1) % 7));
        }

        public int GetDayOfYear(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartDayOfYear));
        }

        public int GetDaysInMonth(int year, int month, int era)
        {
            year = GetGregorianYear(year, era);
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
            }

            int[] days = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366 : DaysToMonth365);
            return (days[month] - days[month - 1]);
        }

        public int GetDaysInYear(int year, int era)
        {
            year = GetGregorianYear(year, era);
            return ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? 366 : 365);
        }

        public int GetEra(DateTime time)
        {
            long ticks = time.Ticks;
            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                if (ticks >= m_EraInfo[i].ticks)
                {
                    return (m_EraInfo[i].era);
                }
            }

            throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Era"));
        }

        public int[] Eras
        {
            get
            {
                if (m_eras == null)
                {
                    m_eras = new int[m_EraInfo.Length];
                    for (int i = 0; i < m_EraInfo.Length; i++)
                    {
                        m_eras[i] = m_EraInfo[i].era;
                    }
                }

                return ((int[])m_eras.Clone());
            }
        }

        public int GetMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartMonth));
        }

        public int GetMonthsInYear(int year, int era)
        {
            year = GetGregorianYear(year, era);
            return (12);
        }

        public int GetYear(DateTime time)
        {
            long ticks = time.Ticks;
            int year = GetDatePart(ticks, DatePartYear);
            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                if (ticks >= m_EraInfo[i].ticks)
                {
                    return (year - m_EraInfo[i].yearOffset);
                }
            }

            throw new ArgumentException(Environment.GetResourceString("Argument_NoEra"));
        }

        public int GetYear(int year, DateTime time)
        {
            long ticks = time.Ticks;
            for (int i = 0; i < m_EraInfo.Length; i++)
            {
                if (ticks >= m_EraInfo[i].ticks)
                {
                    return (year - m_EraInfo[i].yearOffset);
                }
            }

            throw new ArgumentException(Environment.GetResourceString("Argument_NoEra"));
        }

        public bool IsLeapDay(int year, int month, int day, int era)
        {
            if (day < 1 || day > GetDaysInMonth(year, month, era))
            {
                throw new ArgumentOutOfRangeException("day", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, GetDaysInMonth(year, month, era)));
            }

            Contract.EndContractBlock();
            if (!IsLeapYear(year, era))
            {
                return (false);
            }

            if (month == 2 && day == 29)
            {
                return (true);
            }

            return (false);
        }

        public int GetLeapMonth(int year, int era)
        {
            year = GetGregorianYear(year, era);
            return (0);
        }

        public bool IsLeapMonth(int year, int month, int era)
        {
            year = GetGregorianYear(year, era);
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 12));
            }

            return (false);
        }

        public bool IsLeapYear(int year, int era)
        {
            year = GetGregorianYear(year, era);
            return (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));
        }

        public DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            year = GetGregorianYear(year, era);
            long ticks = DateToTicks(year, month, day) + TimeToTicks(hour, minute, second, millisecond);
            CheckTicksRange(ticks);
            return (new DateTime(ticks));
        }

        public virtual int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
        {
            CheckTicksRange(time.Ticks);
            return (GregorianCalendar.GetDefaultInstance().GetWeekOfYear(time, rule, firstDayOfWeek));
        }

        public int ToFourDigitYear(int year, int twoDigitYearMax)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            Contract.EndContractBlock();
            if (year < 100)
            {
                int y = year % 100;
                return ((twoDigitYearMax / 100 - (y > twoDigitYearMax % 100 ? 1 : 0)) * 100 + y);
            }

            if (year < m_minYear || year > m_maxYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), m_minYear, m_maxYear));
            }

            return (year);
        }
    }
}