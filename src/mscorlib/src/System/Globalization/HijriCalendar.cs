

namespace System.Globalization
{
    public class HijriCalendar : Calendar
    {
        public static readonly int HijriEra = 1;
        internal const int DatePartYear = 0;
        internal const int DatePartDayOfYear = 1;
        internal const int DatePartMonth = 2;
        internal const int DatePartDay = 3;
        internal const int MinAdvancedHijri = -2;
        internal const int MaxAdvancedHijri = 2;
        internal static readonly int[] HijriMonthDays = {0, 30, 59, 89, 118, 148, 177, 207, 236, 266, 295, 325, 355};
        private const String InternationalRegKey = "Control Panel\\International";
        private const String HijriAdvanceRegKeyEntry = "AddHijriDate";
        private int m_HijriAdvance = Int32.MinValue;
        internal const int MaxCalendarYear = 9666;
        internal const int MaxCalendarMonth = 4;
        internal const int MaxCalendarDay = 3;
        internal static readonly DateTime calendarMinValue = new DateTime(622, 7, 18);
        internal static readonly DateTime calendarMaxValue = DateTime.MaxValue;
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
                return CalendarAlgorithmType.LunarCalendar;
            }
        }

        public HijriCalendar()
        {
        }

        internal override int ID
        {
            get
            {
                return (CAL_HIJRI);
            }
        }

        protected override int DaysInYearBeforeMinSupportedYear
        {
            get
            {
                return 354;
            }
        }

        long GetAbsoluteDateHijri(int y, int m, int d)
        {
            return (long)(DaysUpToHijriYear(y) + HijriMonthDays[m - 1] + d - 1 - HijriAdjustment);
        }

        long DaysUpToHijriYear(int HijriYear)
        {
            long NumDays;
            int NumYear30;
            int NumYearsLeft;
            NumYear30 = ((HijriYear - 1) / 30) * 30;
            NumYearsLeft = HijriYear - NumYear30 - 1;
            NumDays = ((NumYear30 * 10631L) / 30L) + 227013L;
            while (NumYearsLeft > 0)
            {
                NumDays += 354 + (IsLeapYear(NumYearsLeft, CurrentEra) ? 1 : 0);
                NumYearsLeft--;
            }

            return (NumDays);
        }

        public int HijriAdjustment
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_HijriAdvance == Int32.MinValue)
                {
                    m_HijriAdvance = GetAdvanceHijriDate();
                }

                return (m_HijriAdvance);
            }

            set
            {
                if (value < MinAdvancedHijri || value > MaxAdvancedHijri)
                {
                    throw new ArgumentOutOfRangeException("HijriAdjustment", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"), MinAdvancedHijri, MaxAdvancedHijri));
                }

                                VerifyWritable();
                m_HijriAdvance = value;
            }
        }

        static int GetAdvanceHijriDate()
        {
            int hijriAdvance = 0;
            Microsoft.Win32.RegistryKey key = null;
            try
            {
                key = Microsoft.Win32.Registry.CurrentUser.InternalOpenSubKey(InternationalRegKey, false);
            }
            catch (ObjectDisposedException)
            {
                return 0;
            }
            catch (ArgumentException)
            {
                return 0;
            }

            if (key != null)
            {
                try
                {
                    Object value = key.InternalGetValue(HijriAdvanceRegKeyEntry, null, false, false);
                    if (value == null)
                    {
                        return (0);
                    }

                    String str = value.ToString();
                    if (String.Compare(str, 0, HijriAdvanceRegKeyEntry, 0, HijriAdvanceRegKeyEntry.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (str.Length == HijriAdvanceRegKeyEntry.Length)
                            hijriAdvance = -1;
                        else
                        {
                            str = str.Substring(HijriAdvanceRegKeyEntry.Length);
                            try
                            {
                                int advance = Int32.Parse(str.ToString(), CultureInfo.InvariantCulture);
                                if ((advance >= MinAdvancedHijri) && (advance <= MaxAdvancedHijri))
                                {
                                    hijriAdvance = advance;
                                }
                            }
                            catch (ArgumentException)
                            {
                            }
                            catch (FormatException)
                            {
                            }
                            catch (OverflowException)
                            {
                            }
                        }
                    }
                }
                finally
                {
                    key.Close();
                }
            }

            return (hijriAdvance);
        }

        static internal void CheckTicksRange(long ticks)
        {
            if (ticks < calendarMinValue.Ticks || ticks > calendarMaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException("time", String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"), calendarMinValue, calendarMaxValue));
            }
        }

        static internal void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != HijriEra)
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

        internal virtual int GetDatePart(long ticks, int part)
        {
            int HijriYear;
            int HijriMonth;
            int HijriDay;
            long NumDays;
            CheckTicksRange(ticks);
            NumDays = ticks / GregorianCalendar.TicksPerDay + 1;
            NumDays += HijriAdjustment;
            HijriYear = (int)(((NumDays - 227013) * 30) / 10631) + 1;
            long daysToHijriYear = DaysUpToHijriYear(HijriYear);
            long daysOfHijriYear = GetDaysInYear(HijriYear, CurrentEra);
            if (NumDays < daysToHijriYear)
            {
                daysToHijriYear -= daysOfHijriYear;
                HijriYear--;
            }
            else if (NumDays == daysToHijriYear)
            {
                HijriYear--;
                daysToHijriYear -= GetDaysInYear(HijriYear, CurrentEra);
            }
            else
            {
                if (NumDays > daysToHijriYear + daysOfHijriYear)
                {
                    daysToHijriYear += daysOfHijriYear;
                    HijriYear++;
                }
            }

            if (part == DatePartYear)
            {
                return (HijriYear);
            }

            HijriMonth = 1;
            NumDays -= daysToHijriYear;
            if (part == DatePartDayOfYear)
            {
                return ((int)NumDays);
            }

            while ((HijriMonth <= 12) && (NumDays > HijriMonthDays[HijriMonth - 1]))
            {
                HijriMonth++;
            }

            HijriMonth--;
            if (part == DatePartMonth)
            {
                return (HijriMonth);
            }

            HijriDay = (int)(NumDays - HijriMonthDays[HijriMonth - 1]);
            if (part == DatePartDay)
            {
                return (HijriDay);
            }

            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DateTimeParsing"));
        }

        public override DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException("months", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), -120000, 120000));
            }

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

            long ticks = GetAbsoluteDateHijri(y, m, d) * TicksPerDay + (time.Ticks % TicksPerDay);
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
            if (month == 12)
            {
                return (IsLeapYear(year, CurrentEra) ? 30 : 29);
            }

            return (((month % 2) == 1) ? 30 : 29);
        }

        public override int GetDaysInYear(int year, int era)
        {
            CheckYearRange(year, era);
            return (IsLeapYear(year, CurrentEra) ? 355 : 354);
        }

        public override int GetEra(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return (HijriEra);
        }

        public override int[] Eras
        {
            get
            {
                return (new int[]{HijriEra});
            }
        }

        public override int GetMonth(DateTime time)
        {
            return (GetDatePart(time.Ticks, DatePartMonth));
        }

        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearRange(year, era);
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
            return ((((year * 11) + 14) % 30) < 11);
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                BCLDebug.Log("year = " + year + ", month = " + month + ", day = " + day);
                throw new ArgumentOutOfRangeException("day", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
            }

            long lDate = GetAbsoluteDateHijri(year, month, day);
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