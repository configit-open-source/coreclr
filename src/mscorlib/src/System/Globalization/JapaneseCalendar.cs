namespace System.Globalization
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using Microsoft.Win32;
    using PermissionSet = System.Security.PermissionSet;
    using System.Security.Permissions;

    public class JapaneseCalendar : Calendar
    {
        internal static readonly DateTime calendarMinValue = new DateTime(1868, 9, 8);
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

        static internal volatile EraInfo[] japaneseEraInfo;
        internal static EraInfo[] GetEraInfo()
        {
            if (japaneseEraInfo == null)
            {
                japaneseEraInfo = GetErasFromRegistry();
                if (japaneseEraInfo == null)
                {
                    EraInfo[] defaultEraRanges = new EraInfo[4];
                    defaultEraRanges[0] = new EraInfo(4, 1989, 1, 8, 1988, 1, GregorianCalendar.MaxYear - 1988, "\x5e73\x6210", "\x5e73", "H");
                    defaultEraRanges[1] = new EraInfo(3, 1926, 12, 25, 1925, 1, 1989 - 1925, "\x662d\x548c", "\x662d", "S");
                    defaultEraRanges[2] = new EraInfo(2, 1912, 7, 30, 1911, 1, 1926 - 1911, "\x5927\x6b63", "\x5927", "T");
                    defaultEraRanges[3] = new EraInfo(1, 1868, 1, 1, 1867, 1, 1912 - 1867, "\x660e\x6cbb", "\x660e", "M");
                    japaneseEraInfo = defaultEraRanges;
                }
            }

            return japaneseEraInfo;
        }

        private const string c_japaneseErasHive = @"System\CurrentControlSet\Control\Nls\Calendars\Japanese\Eras";
        private const string c_japaneseErasHivePermissionList = @"HKEY_LOCAL_MACHINE\" + c_japaneseErasHive;
        private static EraInfo[] GetErasFromRegistry()
        {
            int iFoundEras = 0;
            EraInfo[] registryEraRanges = null;
            try
            {
                PermissionSet permSet = new PermissionSet(PermissionState.None);
                permSet.AddPermission(new RegistryPermission(RegistryPermissionAccess.Read, c_japaneseErasHivePermissionList));
                permSet.Assert();
                RegistryKey key = RegistryKey.GetBaseKey(RegistryKey.HKEY_LOCAL_MACHINE).OpenSubKey(c_japaneseErasHive, false);
                if (key == null)
                    return null;
                String[] valueNames = key.GetValueNames();
                if (valueNames != null && valueNames.Length > 0)
                {
                    registryEraRanges = new EraInfo[valueNames.Length];
                    for (int i = 0; i < valueNames.Length; i++)
                    {
                        EraInfo era = GetEraFromValue(valueNames[i], key.GetValue(valueNames[i]).ToString());
                        if (era == null)
                            continue;
                        registryEraRanges[iFoundEras] = era;
                        iFoundEras++;
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                return null;
            }
            catch (System.IO.IOException)
            {
                return null;
            }
            catch (System.UnauthorizedAccessException)
            {
                return null;
            }

            if (iFoundEras < 4)
                return null;
            Array.Resize(ref registryEraRanges, iFoundEras);
            Array.Sort(registryEraRanges, CompareEraRanges);
            for (int i = 0; i < registryEraRanges.Length; i++)
            {
                registryEraRanges[i].era = registryEraRanges.Length - i;
                if (i == 0)
                {
                    registryEraRanges[0].maxEraYear = GregorianCalendar.MaxYear - registryEraRanges[0].yearOffset;
                }
                else
                {
                    registryEraRanges[i].maxEraYear = registryEraRanges[i - 1].yearOffset + 1 - registryEraRanges[i].yearOffset;
                }
            }

            return registryEraRanges;
        }

        private static int CompareEraRanges(EraInfo a, EraInfo b)
        {
            return b.ticks.CompareTo(a.ticks);
        }

        private static EraInfo GetEraFromValue(String value, String data)
        {
            if (value == null || data == null)
                return null;
            if (value.Length != 10)
                return null;
            int year;
            int month;
            int day;
            if (!Number.TryParseInt32(value.Substring(0, 4), NumberStyles.None, NumberFormatInfo.InvariantInfo, out year) || !Number.TryParseInt32(value.Substring(5, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo, out month) || !Number.TryParseInt32(value.Substring(8, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo, out day))
            {
                return null;
            }

            String[] names = data.Split(new char[]{'_'});
            if (names.Length != 4)
                return null;
            if (names[0].Length == 0 || names[1].Length == 0 || names[2].Length == 0 || names[3].Length == 0)
                return null;
            return new EraInfo(0, year, month, day, year - 1, 1, 0, names[0], names[1], names[3]);
        }

        internal static volatile Calendar s_defaultInstance;
        internal GregorianCalendarHelper helper;
        internal static Calendar GetDefaultInstance()
        {
            if (s_defaultInstance == null)
            {
                s_defaultInstance = new JapaneseCalendar();
            }

            return (s_defaultInstance);
        }

        public JapaneseCalendar()
        {
            try
            {
                new CultureInfo("ja-JP");
            }
            catch (ArgumentException e)
            {
                throw new TypeInitializationException(this.GetType().FullName, e);
            }

            helper = new GregorianCalendarHelper(this, GetEraInfo());
        }

        internal override int ID
        {
            get
            {
                return (CAL_JAPAN);
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

        public override int ToFourDigitYear(int year)
        {
            if (year <= 0)
            {
                throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            Contract.EndContractBlock();
            if (year > helper.MaxYear)
            {
                throw new ArgumentOutOfRangeException("year", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, helper.MaxYear));
            }

            return (year);
        }

        public override int[] Eras
        {
            get
            {
                return (helper.Eras);
            }
        }

        internal static String[] EraNames()
        {
            EraInfo[] eras = GetEraInfo();
            String[] eraNames = new String[eras.Length];
            for (int i = 0; i < eras.Length; i++)
            {
                eraNames[i] = eras[eras.Length - i - 1].eraName;
            }

            return eraNames;
        }

        internal static String[] AbbrevEraNames()
        {
            EraInfo[] eras = GetEraInfo();
            String[] erasAbbrev = new String[eras.Length];
            for (int i = 0; i < eras.Length; i++)
            {
                erasAbbrev[i] = eras[eras.Length - i - 1].abbrevEraName;
            }

            return erasAbbrev;
        }

        internal static String[] EnglishEraNames()
        {
            EraInfo[] eras = GetEraInfo();
            String[] erasEnglish = new String[eras.Length];
            for (int i = 0; i < eras.Length; i++)
            {
                erasEnglish[i] = eras[eras.Length - i - 1].englishEraName;
            }

            return erasEnglish;
        }

        private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 99;
        internal override bool IsValidYear(int year, int era)
        {
            return helper.IsValidYear(year, era);
        }

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
    }
}