using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.Globalization
{
    [Flags]
    internal enum MonthNameStyles
    {
        Regular = 0x00000000,
        Genitive = 0x00000001,
        LeapYear = 0x00000002
    }

    [Flags]
    internal enum DateTimeFormatFlags
    {
        None = 0x00000000,
        UseGenitiveMonth = 0x00000001,
        UseLeapYearMonth = 0x00000002,
        UseSpacesInMonthNames = 0x00000004,
        UseHebrewRule = 0x00000008,
        UseSpacesInDayNames = 0x00000010,
        UseDigitPrefixInTokens = 0x00000020,
        NotInitialized = -1
    }

    public sealed class DateTimeFormatInfo : ICloneable, IFormatProvider
    {
        private static volatile DateTimeFormatInfo invariantInfo;
        private CultureData m_cultureData;
        internal String m_name = null;
        private String m_langName = null;
        private CompareInfo m_compareInfo = null;
        private CultureInfo m_cultureInfo = null;
        internal String amDesignator = null;
        internal String pmDesignator = null;
        internal String dateSeparator = null;
        internal String generalShortTimePattern = null;
        internal String generalLongTimePattern = null;
        internal String timeSeparator = null;
        internal String monthDayPattern = null;
        internal String dateTimeOffsetPattern = null;
        internal const String rfc1123Pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
        internal const String sortableDateTimePattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss";
        internal const String universalSortableDateTimePattern = "yyyy'-'MM'-'dd HH':'mm':'ss'Z'";
        internal Calendar calendar = null;
        internal int firstDayOfWeek = -1;
        internal int calendarWeekRule = -1;
        internal String fullDateTimePattern = null;
        internal String[] abbreviatedDayNames = null;
        internal String[] m_superShortDayNames = null;
        internal String[] dayNames = null;
        internal String[] abbreviatedMonthNames = null;
        internal String[] monthNames = null;
        internal String[] genitiveMonthNames = null;
        internal String[] m_genitiveAbbreviatedMonthNames = null;
        internal String[] leapYearMonthNames = null;
        internal String longDatePattern = null;
        internal String shortDatePattern = null;
        internal String yearMonthPattern = null;
        internal String longTimePattern = null;
        internal String shortTimePattern = null;
        private String[] allYearMonthPatterns = null;
        internal String[] allShortDatePatterns = null;
        internal String[] allLongDatePatterns = null;
        internal String[] allShortTimePatterns = null;
        internal String[] allLongTimePatterns = null;
        internal String[] m_eraNames = null;
        internal String[] m_abbrevEraNames = null;
        internal String[] m_abbrevEnglishEraNames = null;
        internal int[] optionalCalendars = null;
        private const int DEFAULT_ALL_DATETIMES_SIZE = 132;
        internal bool m_isReadOnly = false;
        internal DateTimeFormatFlags formatFlags = DateTimeFormatFlags.NotInitialized;
        internal static bool preferExistingTokens = InitPreferExistingTokens();
        static bool InitPreferExistingTokens()
        {
            bool ret = false;
            return ret;
        }

        private String CultureName
        {
            get
            {
                if (m_name == null)
                {
                    m_name = this.m_cultureData.CultureName;
                }

                return (m_name);
            }
        }

        private CultureInfo Culture
        {
            get
            {
                if (m_cultureInfo == null)
                {
                    m_cultureInfo = CultureInfo.GetCultureInfo(this.CultureName);
                }

                return m_cultureInfo;
            }
        }

        private String LanguageName
        {
            [System.Security.SecurityCritical]
            get
            {
                if (m_langName == null)
                {
                    m_langName = this.m_cultureData.SISO639LANGNAME;
                }

                return (m_langName);
            }
        }

        private String[] internalGetAbbreviatedDayOfWeekNames()
        {
            if (this.abbreviatedDayNames == null)
            {
                this.abbreviatedDayNames = this.m_cultureData.AbbreviatedDayNames(Calendar.ID);
                            }

            return (this.abbreviatedDayNames);
        }

        private String[] internalGetSuperShortDayNames()
        {
            if (this.m_superShortDayNames == null)
            {
                this.m_superShortDayNames = this.m_cultureData.SuperShortDayNames(Calendar.ID);
                            }

            return (this.m_superShortDayNames);
        }

        private String[] internalGetDayOfWeekNames()
        {
            if (this.dayNames == null)
            {
                this.dayNames = this.m_cultureData.DayNames(Calendar.ID);
                            }

            return (this.dayNames);
        }

        private String[] internalGetAbbreviatedMonthNames()
        {
            if (this.abbreviatedMonthNames == null)
            {
                this.abbreviatedMonthNames = this.m_cultureData.AbbreviatedMonthNames(Calendar.ID);
                            }

            return (this.abbreviatedMonthNames);
        }

        private String[] internalGetMonthNames()
        {
            if (this.monthNames == null)
            {
                this.monthNames = this.m_cultureData.MonthNames(Calendar.ID);
                            }

            return (this.monthNames);
        }

        public DateTimeFormatInfo(): this (CultureInfo.InvariantCulture.m_cultureData, GregorianCalendar.GetDefaultInstance())
        {
        }

        internal DateTimeFormatInfo(CultureData cultureData, Calendar cal)
        {
                                    this.m_cultureData = cultureData;
            this.Calendar = cal;
        }

        private void InitializeOverridableProperties(CultureData cultureData, int calendarID)
        {
        }

        private int CultureID;
        private bool m_useUserOverride;
        internal String[] m_dateWords = null;
        private void OnDeserialized(StreamingContext ctx)
        {
            if (this.m_name != null)
            {
                m_cultureData = CultureData.GetCultureData(m_name, m_useUserOverride);
                if (this.m_cultureData == null)
                    throw new CultureNotFoundException("m_name", m_name, Environment.GetResourceString("Argument_CultureNotSupported"));
            }

            if (calendar == null)
            {
                calendar = (Calendar)GregorianCalendar.GetDefaultInstance().Clone();
                calendar.SetReadOnlyState(m_isReadOnly);
            }
            else
            {
                CultureInfo.CheckDomainSafetyObject(calendar, this);
            }

            InitializeOverridableProperties(m_cultureData, calendar.ID);
            bool isReadOnly = m_isReadOnly;
            m_isReadOnly = false;
            if (longDatePattern != null)
                this.LongDatePattern = longDatePattern;
            if (shortDatePattern != null)
                this.ShortDatePattern = shortDatePattern;
            if (yearMonthPattern != null)
                this.YearMonthPattern = yearMonthPattern;
            if (longTimePattern != null)
                this.LongTimePattern = longTimePattern;
            if (shortTimePattern != null)
                this.ShortTimePattern = shortTimePattern;
            m_isReadOnly = isReadOnly;
        }

        private void OnSerializing(StreamingContext ctx)
        {
            m_useUserOverride = this.m_cultureData.UseUserOverride;
            m_name = this.CultureName;
            Object o;
            o = this.LongTimePattern;
            o = this.LongDatePattern;
            o = this.ShortTimePattern;
            o = this.ShortDatePattern;
            o = this.YearMonthPattern;
            o = this.AllLongTimePatterns;
            o = this.AllLongDatePatterns;
            o = this.AllShortTimePatterns;
            o = this.AllShortDatePatterns;
            o = this.AllYearMonthPatterns;
        }

        public static DateTimeFormatInfo InvariantInfo
        {
            get
            {
                                if (invariantInfo == null)
                {
                    DateTimeFormatInfo info = new DateTimeFormatInfo();
                    info.Calendar.SetReadOnlyState(true);
                    info.m_isReadOnly = true;
                    invariantInfo = info;
                }

                return (invariantInfo);
            }
        }

        public static DateTimeFormatInfo CurrentInfo
        {
            get
            {
                                System.Globalization.CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentCulture;
                if (!culture.m_isInherited)
                {
                    DateTimeFormatInfo info = culture.dateTimeInfo;
                    if (info != null)
                    {
                        return info;
                    }
                }

                return (DateTimeFormatInfo)culture.GetFormat(typeof (DateTimeFormatInfo));
            }
        }

        public static DateTimeFormatInfo GetInstance(IFormatProvider provider)
        {
            DateTimeFormatInfo info;
            CultureInfo cultureProvider = provider as CultureInfo;
            if (cultureProvider != null && !cultureProvider.m_isInherited)
            {
                return cultureProvider.DateTimeFormat;
            }

            info = provider as DateTimeFormatInfo;
            if (info != null)
            {
                return info;
            }

            if (provider != null)
            {
                info = provider.GetFormat(typeof (DateTimeFormatInfo)) as DateTimeFormatInfo;
                if (info != null)
                {
                    return info;
                }
            }

            return CurrentInfo;
        }

        public Object GetFormat(Type formatType)
        {
            return (formatType == typeof (DateTimeFormatInfo) ? this : null);
        }

        public Object Clone()
        {
            DateTimeFormatInfo n = (DateTimeFormatInfo)MemberwiseClone();
            n.calendar = (Calendar)this.Calendar.Clone();
            n.m_isReadOnly = false;
            return n;
        }

        public String AMDesignator
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (this.amDesignator == null)
                {
                    this.amDesignator = this.m_cultureData.SAM1159;
                }

                                return (this.amDesignator);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                ClearTokenHashTable();
                amDesignator = value;
            }
        }

        public Calendar Calendar
        {
            get
            {
                                                return (this.calendar);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Obj"));
                }

                                if (value == calendar)
                {
                    return;
                }

                CultureInfo.CheckDomainSafetyObject(value, this);
                for (int i = 0; i < this.OptionalCalendars.Length; i++)
                {
                    if (this.OptionalCalendars[i] == value.ID)
                    {
                        if (calendar != null)
                        {
                            m_eraNames = null;
                            m_abbrevEraNames = null;
                            m_abbrevEnglishEraNames = null;
                            monthDayPattern = null;
                            dayNames = null;
                            abbreviatedDayNames = null;
                            m_superShortDayNames = null;
                            monthNames = null;
                            abbreviatedMonthNames = null;
                            genitiveMonthNames = null;
                            m_genitiveAbbreviatedMonthNames = null;
                            leapYearMonthNames = null;
                            formatFlags = DateTimeFormatFlags.NotInitialized;
                            allShortDatePatterns = null;
                            allLongDatePatterns = null;
                            allYearMonthPatterns = null;
                            dateTimeOffsetPattern = null;
                            longDatePattern = null;
                            shortDatePattern = null;
                            yearMonthPattern = null;
                            fullDateTimePattern = null;
                            generalShortTimePattern = null;
                            generalLongTimePattern = null;
                            dateSeparator = null;
                            ClearTokenHashTable();
                        }

                        calendar = value;
                        InitializeOverridableProperties(m_cultureData, calendar.ID);
                        return;
                    }
                }

                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("Argument_InvalidCalendar"));
            }
        }

        private int[] OptionalCalendars
        {
            get
            {
                if (this.optionalCalendars == null)
                {
                    this.optionalCalendars = this.m_cultureData.CalendarIds;
                }

                return (this.optionalCalendars);
            }
        }

        public int GetEra(String eraName)
        {
            if (eraName == null)
            {
                throw new ArgumentNullException("eraName", Environment.GetResourceString("ArgumentNull_String"));
            }

                        if (eraName.Length == 0)
            {
                return (-1);
            }

            for (int i = 0; i < EraNames.Length; i++)
            {
                if (m_eraNames[i].Length > 0)
                {
                    if (String.Compare(eraName, m_eraNames[i], this.Culture, CompareOptions.IgnoreCase) == 0)
                    {
                        return (i + 1);
                    }
                }
            }

            for (int i = 0; i < AbbreviatedEraNames.Length; i++)
            {
                if (String.Compare(eraName, m_abbrevEraNames[i], this.Culture, CompareOptions.IgnoreCase) == 0)
                {
                    return (i + 1);
                }
            }

            for (int i = 0; i < AbbreviatedEnglishEraNames.Length; i++)
            {
                if (String.Compare(eraName, m_abbrevEnglishEraNames[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return (i + 1);
                }
            }

            return (-1);
        }

        internal String[] EraNames
        {
            get
            {
                if (this.m_eraNames == null)
                {
                    this.m_eraNames = this.m_cultureData.EraNames(Calendar.ID);
                    ;
                }

                return (this.m_eraNames);
            }
        }

        public String GetEraName(int era)
        {
            if (era == Calendar.CurrentEra)
            {
                era = Calendar.CurrentEraValue;
            }

            if ((--era) < EraNames.Length && (era >= 0))
            {
                return (m_eraNames[era]);
            }

            throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
        }

        internal String[] AbbreviatedEraNames
        {
            get
            {
                if (this.m_abbrevEraNames == null)
                {
                    this.m_abbrevEraNames = this.m_cultureData.AbbrevEraNames(Calendar.ID);
                }

                return (this.m_abbrevEraNames);
            }
        }

        public String GetAbbreviatedEraName(int era)
        {
            if (AbbreviatedEraNames.Length == 0)
            {
                return (GetEraName(era));
            }

            if (era == Calendar.CurrentEra)
            {
                era = Calendar.CurrentEraValue;
            }

            if ((--era) < m_abbrevEraNames.Length && (era >= 0))
            {
                return (m_abbrevEraNames[era]);
            }

            throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
        }

        internal String[] AbbreviatedEnglishEraNames
        {
            get
            {
                if (this.m_abbrevEnglishEraNames == null)
                {
                                        this.m_abbrevEnglishEraNames = this.m_cultureData.AbbreviatedEnglishEraNames(Calendar.ID);
                }

                return (this.m_abbrevEnglishEraNames);
            }
        }

        public String DateSeparator
        {
            get
            {
                if (this.dateSeparator == null)
                {
                    this.dateSeparator = this.m_cultureData.DateSeparator(Calendar.ID);
                }

                                return (this.dateSeparator);
            }
        }

        public DayOfWeek FirstDayOfWeek
        {
            get
            {
                if (this.firstDayOfWeek == -1)
                {
                    this.firstDayOfWeek = this.m_cultureData.IFIRSTDAYOFWEEK;
                }

                                return ((DayOfWeek)this.firstDayOfWeek);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value >= DayOfWeek.Sunday && value <= DayOfWeek.Saturday)
                {
                    firstDayOfWeek = (int)value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_Range", DayOfWeek.Sunday, DayOfWeek.Saturday));
                }
            }
        }

        public CalendarWeekRule CalendarWeekRule
        {
            get
            {
                if (this.calendarWeekRule == -1)
                {
                    this.calendarWeekRule = this.m_cultureData.IFIRSTWEEKOFYEAR;
                }

                                return ((CalendarWeekRule)this.calendarWeekRule);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value >= CalendarWeekRule.FirstDay && value <= CalendarWeekRule.FirstFourDayWeek)
                {
                    calendarWeekRule = (int)value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_Range", CalendarWeekRule.FirstDay, CalendarWeekRule.FirstFourDayWeek));
                }
            }
        }

        public String FullDateTimePattern
        {
            get
            {
                if (fullDateTimePattern == null)
                {
                    fullDateTimePattern = LongDatePattern + " " + LongTimePattern;
                }

                return (fullDateTimePattern);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                fullDateTimePattern = value;
            }
        }

        public String LongDatePattern
        {
            get
            {
                if (this.longDatePattern == null)
                {
                    this.longDatePattern = this.UnclonedLongDatePatterns[0];
                }

                return this.longDatePattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                this.longDatePattern = value;
                ClearTokenHashTable();
                this.fullDateTimePattern = null;
            }
        }

        public String LongTimePattern
        {
            get
            {
                if (this.longTimePattern == null)
                {
                    this.longTimePattern = this.UnclonedLongTimePatterns[0];
                }

                return this.longTimePattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                this.longTimePattern = value;
                ClearTokenHashTable();
                this.fullDateTimePattern = null;
                this.generalLongTimePattern = null;
                this.dateTimeOffsetPattern = null;
            }
        }

        public String MonthDayPattern
        {
            get
            {
                if (this.monthDayPattern == null)
                {
                                        this.monthDayPattern = this.m_cultureData.MonthDay(Calendar.ID);
                }

                                return (this.monthDayPattern);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                this.monthDayPattern = value;
            }
        }

        public String PMDesignator
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (this.pmDesignator == null)
                {
                    this.pmDesignator = this.m_cultureData.SPM2359;
                }

                                return (this.pmDesignator);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                ClearTokenHashTable();
                pmDesignator = value;
            }
        }

        public String RFC1123Pattern
        {
            get
            {
                return (rfc1123Pattern);
            }
        }

        public String ShortDatePattern
        {
            get
            {
                if (this.shortDatePattern == null)
                {
                    this.shortDatePattern = this.UnclonedShortDatePatterns[0];
                }

                return this.shortDatePattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                                this.shortDatePattern = value;
                ClearTokenHashTable();
                generalLongTimePattern = null;
                generalShortTimePattern = null;
                dateTimeOffsetPattern = null;
            }
        }

        public String ShortTimePattern
        {
            get
            {
                if (this.shortTimePattern == null)
                {
                    this.shortTimePattern = this.UnclonedShortTimePatterns[0];
                }

                return this.shortTimePattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                this.shortTimePattern = value;
                ClearTokenHashTable();
                generalShortTimePattern = null;
            }
        }

        public String SortableDateTimePattern
        {
            get
            {
                return (sortableDateTimePattern);
            }
        }

        internal String GeneralShortTimePattern
        {
            get
            {
                if (generalShortTimePattern == null)
                {
                    generalShortTimePattern = ShortDatePattern + " " + ShortTimePattern;
                }

                return (generalShortTimePattern);
            }
        }

        internal String GeneralLongTimePattern
        {
            get
            {
                if (generalLongTimePattern == null)
                {
                    generalLongTimePattern = ShortDatePattern + " " + LongTimePattern;
                }

                return (generalLongTimePattern);
            }
        }

        internal String DateTimeOffsetPattern
        {
            get
            {
                if (dateTimeOffsetPattern == null)
                {
                    string dateTimePattern = ShortDatePattern + " " + LongTimePattern;
                    bool foundZ = false;
                    bool inQuote = false;
                    char quote = '\'';
                    for (int i = 0; !foundZ && i < LongTimePattern.Length; i++)
                    {
                        switch (LongTimePattern[i])
                        {
                            case 'z':
                                foundZ = !inQuote;
                                break;
                            case '\'':
                            case '\"':
                                if (inQuote && (quote == LongTimePattern[i]))
                                {
                                    inQuote = false;
                                }
                                else if (!inQuote)
                                {
                                    quote = LongTimePattern[i];
                                    inQuote = true;
                                }
                                else
                                {
                                }

                                break;
                            case '%':
                            case '\\':
                                i++;
                                break;
                            default:
                                break;
                        }
                    }

                    if (!foundZ)
                    {
                        dateTimePattern = dateTimePattern + " zzz";
                    }

                    dateTimeOffsetPattern = dateTimePattern;
                }

                return (dateTimeOffsetPattern);
            }
        }

        public String TimeSeparator
        {
            get
            {
                if (timeSeparator == null)
                {
                    timeSeparator = this.m_cultureData.TimeSeparator;
                }

                                return (timeSeparator);
            }
        }

        public String UniversalSortableDateTimePattern
        {
            get
            {
                return (universalSortableDateTimePattern);
            }
        }

        public String YearMonthPattern
        {
            get
            {
                if (this.yearMonthPattern == null)
                {
                    this.yearMonthPattern = this.UnclonedYearMonthPatterns[0];
                }

                return this.yearMonthPattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                this.yearMonthPattern = value;
                ClearTokenHashTable();
            }
        }

        static private void CheckNullValue(String[] values, int length)
        {
                                    for (int i = 0; i < length; i++)
            {
                if (values[i] == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_ArrayValue"));
                }
            }
        }

        public String[] AbbreviatedDayNames
        {
            get
            {
                return ((String[])internalGetAbbreviatedDayOfWeekNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
                }

                if (value.Length != 7)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", 7), "value");
                }

                                CheckNullValue(value, value.Length);
                ClearTokenHashTable();
                abbreviatedDayNames = value;
            }
        }

        public String[] ShortestDayNames
        {
            get
            {
                return ((String[])internalGetSuperShortDayNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
                }

                if (value.Length != 7)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", 7), "value");
                }

                                CheckNullValue(value, value.Length);
                this.m_superShortDayNames = value;
            }
        }

        public String[] DayNames
        {
            get
            {
                return ((String[])internalGetDayOfWeekNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
                }

                if (value.Length != 7)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", 7), "value");
                }

                                CheckNullValue(value, value.Length);
                ClearTokenHashTable();
                dayNames = value;
            }
        }

        public String[] AbbreviatedMonthNames
        {
            get
            {
                return ((String[])internalGetAbbreviatedMonthNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
                }

                if (value.Length != 13)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", 13), "value");
                }

                                CheckNullValue(value, value.Length - 1);
                ClearTokenHashTable();
                abbreviatedMonthNames = value;
            }
        }

        public String[] MonthNames
        {
            get
            {
                return ((String[])internalGetMonthNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
                }

                if (value.Length != 13)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", 13), "value");
                }

                                CheckNullValue(value, value.Length - 1);
                monthNames = value;
                ClearTokenHashTable();
            }
        }

        static char[] MonthSpaces = {' ', '\u00a0'};
        internal bool HasSpacesInMonthNames
        {
            get
            {
                return (FormatFlags & DateTimeFormatFlags.UseSpacesInMonthNames) != 0;
            }
        }

        internal bool HasSpacesInDayNames
        {
            get
            {
                return (FormatFlags & DateTimeFormatFlags.UseSpacesInDayNames) != 0;
            }
        }

        internal String internalGetMonthName(int month, MonthNameStyles style, bool abbreviated)
        {
            String[] monthNamesArray = null;
            switch (style)
            {
                case MonthNameStyles.Genitive:
                    monthNamesArray = internalGetGenitiveMonthNames(abbreviated);
                    break;
                case MonthNameStyles.LeapYear:
                    monthNamesArray = internalGetLeapYearMonthNames();
                    break;
                default:
                    monthNamesArray = (abbreviated ? internalGetAbbreviatedMonthNames() : internalGetMonthNames());
                    break;
            }

            if ((month < 1) || (month > monthNamesArray.Length))
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", 1, monthNamesArray.Length));
            }

            return (monthNamesArray[month - 1]);
        }

        private String[] internalGetGenitiveMonthNames(bool abbreviated)
        {
            if (abbreviated)
            {
                if (this.m_genitiveAbbreviatedMonthNames == null)
                {
                    this.m_genitiveAbbreviatedMonthNames = this.m_cultureData.AbbreviatedGenitiveMonthNames(this.Calendar.ID);
                                    }

                return (this.m_genitiveAbbreviatedMonthNames);
            }

            if (this.genitiveMonthNames == null)
            {
                this.genitiveMonthNames = this.m_cultureData.GenitiveMonthNames(this.Calendar.ID);
                            }

            return (this.genitiveMonthNames);
        }

        internal String[] internalGetLeapYearMonthNames()
        {
            if (this.leapYearMonthNames == null)
            {
                                this.leapYearMonthNames = this.m_cultureData.LeapYearMonthNames(Calendar.ID);
                            }

            return (leapYearMonthNames);
        }

        public String GetAbbreviatedDayName(DayOfWeek dayofweek)
        {
            if ((int)dayofweek < 0 || (int)dayofweek > 6)
            {
                throw new ArgumentOutOfRangeException("dayofweek", Environment.GetResourceString("ArgumentOutOfRange_Range", DayOfWeek.Sunday, DayOfWeek.Saturday));
            }

                        return (internalGetAbbreviatedDayOfWeekNames()[(int)dayofweek]);
        }

        public String GetShortestDayName(DayOfWeek dayOfWeek)
        {
            if ((int)dayOfWeek < 0 || (int)dayOfWeek > 6)
            {
                throw new ArgumentOutOfRangeException("dayOfWeek", Environment.GetResourceString("ArgumentOutOfRange_Range", DayOfWeek.Sunday, DayOfWeek.Saturday));
            }

                        return (internalGetSuperShortDayNames()[(int)dayOfWeek]);
        }

        static private String[] GetCombinedPatterns(String[] patterns1, String[] patterns2, String connectString)
        {
                                    String[] result = new String[patterns1.Length * patterns2.Length];
            int k = 0;
            for (int i = 0; i < patterns1.Length; i++)
            {
                for (int j = 0; j < patterns2.Length; j++)
                {
                    result[k++] = patterns1[i] + connectString + patterns2[j];
                }
            }

            return (result);
        }

        public String[] GetAllDateTimePatterns()
        {
            List<String> results = new List<String>(DEFAULT_ALL_DATETIMES_SIZE);
            for (int i = 0; i < DateTimeFormat.allStandardFormats.Length; i++)
            {
                String[] strings = GetAllDateTimePatterns(DateTimeFormat.allStandardFormats[i]);
                for (int j = 0; j < strings.Length; j++)
                {
                    results.Add(strings[j]);
                }
            }

            return results.ToArray();
        }

        public String[] GetAllDateTimePatterns(char format)
        {
                        String[] result = null;
            switch (format)
            {
                case 'd':
                    result = this.AllShortDatePatterns;
                    break;
                case 'D':
                    result = this.AllLongDatePatterns;
                    break;
                case 'f':
                    result = GetCombinedPatterns(AllLongDatePatterns, AllShortTimePatterns, " ");
                    break;
                case 'F':
                case 'U':
                    result = GetCombinedPatterns(AllLongDatePatterns, AllLongTimePatterns, " ");
                    break;
                case 'g':
                    result = GetCombinedPatterns(AllShortDatePatterns, AllShortTimePatterns, " ");
                    break;
                case 'G':
                    result = GetCombinedPatterns(AllShortDatePatterns, AllLongTimePatterns, " ");
                    break;
                case 'm':
                case 'M':
                    result = new String[]{MonthDayPattern};
                    break;
                case 'o':
                case 'O':
                    result = new String[]{DateTimeFormat.RoundtripFormat};
                    break;
                case 'r':
                case 'R':
                    result = new String[]{rfc1123Pattern};
                    break;
                case 's':
                    result = new String[]{sortableDateTimePattern};
                    break;
                case 't':
                    result = this.AllShortTimePatterns;
                    break;
                case 'T':
                    result = this.AllLongTimePatterns;
                    break;
                case 'u':
                    result = new String[]{UniversalSortableDateTimePattern};
                    break;
                case 'y':
                case 'Y':
                    result = this.AllYearMonthPatterns;
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Format_BadFormatSpecifier"), "format");
            }

            return (result);
        }

        public String GetDayName(DayOfWeek dayofweek)
        {
            if ((int)dayofweek < 0 || (int)dayofweek > 6)
            {
                throw new ArgumentOutOfRangeException("dayofweek", Environment.GetResourceString("ArgumentOutOfRange_Range", DayOfWeek.Sunday, DayOfWeek.Saturday));
            }

                        return (internalGetDayOfWeekNames()[(int)dayofweek]);
        }

        public String GetAbbreviatedMonthName(int month)
        {
            if (month < 1 || month > 13)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", 1, 13));
            }

                        return (internalGetAbbreviatedMonthNames()[month - 1]);
        }

        public String GetMonthName(int month)
        {
            if (month < 1 || month > 13)
            {
                throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", 1, 13));
            }

                        return (internalGetMonthNames()[month - 1]);
        }

        private static string[] GetMergedPatterns(string[] patterns, string defaultPattern)
        {
                                    if (defaultPattern == patterns[0])
            {
                return (string[])patterns.Clone();
            }

            int i;
            for (i = 0; i < patterns.Length; i++)
            {
                if (defaultPattern == patterns[i])
                    break;
            }

            string[] newPatterns;
            if (i < patterns.Length)
            {
                newPatterns = (string[])patterns.Clone();
                newPatterns[i] = newPatterns[0];
            }
            else
            {
                newPatterns = new String[patterns.Length + 1];
                Array.Copy(patterns, 0, newPatterns, 1, patterns.Length);
            }

            newPatterns[0] = defaultPattern;
            return newPatterns;
        }

        private String[] AllYearMonthPatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedYearMonthPatterns, this.YearMonthPattern);
            }
        }

        private String[] AllShortDatePatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedShortDatePatterns, this.ShortDatePattern);
            }
        }

        private String[] AllShortTimePatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedShortTimePatterns, this.ShortTimePattern);
            }
        }

        private String[] AllLongDatePatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedLongDatePatterns, this.LongDatePattern);
            }
        }

        private String[] AllLongTimePatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedLongTimePatterns, this.LongTimePattern);
            }
        }

        private String[] UnclonedYearMonthPatterns
        {
            get
            {
                if (this.allYearMonthPatterns == null)
                {
                                        this.allYearMonthPatterns = this.m_cultureData.YearMonths(this.Calendar.ID);
                                    }

                return this.allYearMonthPatterns;
            }
        }

        private String[] UnclonedShortDatePatterns
        {
            get
            {
                if (allShortDatePatterns == null)
                {
                                        this.allShortDatePatterns = this.m_cultureData.ShortDates(this.Calendar.ID);
                                    }

                return this.allShortDatePatterns;
            }
        }

        private String[] UnclonedLongDatePatterns
        {
            get
            {
                if (allLongDatePatterns == null)
                {
                                        this.allLongDatePatterns = this.m_cultureData.LongDates(this.Calendar.ID);
                                    }

                return this.allLongDatePatterns;
            }
        }

        private String[] UnclonedShortTimePatterns
        {
            get
            {
                if (this.allShortTimePatterns == null)
                {
                    this.allShortTimePatterns = this.m_cultureData.ShortTimes;
                                    }

                return this.allShortTimePatterns;
            }
        }

        private String[] UnclonedLongTimePatterns
        {
            get
            {
                if (this.allLongTimePatterns == null)
                {
                    this.allLongTimePatterns = this.m_cultureData.LongTimes;
                                    }

                return this.allLongTimePatterns;
            }
        }

        public static DateTimeFormatInfo ReadOnly(DateTimeFormatInfo dtfi)
        {
            if (dtfi == null)
            {
                throw new ArgumentNullException("dtfi", Environment.GetResourceString("ArgumentNull_Obj"));
            }

                        if (dtfi.IsReadOnly)
            {
                return (dtfi);
            }

            DateTimeFormatInfo newInfo = (DateTimeFormatInfo)(dtfi.MemberwiseClone());
            newInfo.calendar = Calendar.ReadOnly(dtfi.Calendar);
            newInfo.m_isReadOnly = true;
            return (newInfo);
        }

        public bool IsReadOnly
        {
            get
            {
                return (m_isReadOnly);
            }
        }

        public String NativeCalendarName
        {
            get
            {
                return m_cultureData.CalendarName(Calendar.ID);
            }
        }

        public void SetAllDateTimePatterns(String[] patterns, char format)
        {
            if (IsReadOnly)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            if (patterns == null)
            {
                throw new ArgumentNullException("patterns", Environment.GetResourceString("ArgumentNull_Array"));
            }

            if (patterns.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayZeroError"), "patterns");
            }

                        for (int i = 0; i < patterns.Length; i++)
            {
                if (patterns[i] == null)
                {
                    throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_ArrayValue"));
                }
            }

            switch (format)
            {
                case 'd':
                    this.allShortDatePatterns = patterns;
                    this.shortDatePattern = this.allShortDatePatterns[0];
                    break;
                case 'D':
                    this.allLongDatePatterns = patterns;
                    this.longDatePattern = this.allLongDatePatterns[0];
                    break;
                case 't':
                    this.allShortTimePatterns = patterns;
                    this.shortTimePattern = this.allShortTimePatterns[0];
                    break;
                case 'T':
                    this.allLongTimePatterns = patterns;
                    this.longTimePattern = this.allLongTimePatterns[0];
                    break;
                case 'y':
                case 'Y':
                    this.allYearMonthPatterns = patterns;
                    this.yearMonthPattern = this.allYearMonthPatterns[0];
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Format_BadFormatSpecifier"), "format");
            }

            ClearTokenHashTable();
            return;
        }

        public String[] AbbreviatedMonthGenitiveNames
        {
            get
            {
                return ((String[])internalGetGenitiveMonthNames(true).Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
                }

                if (value.Length != 13)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", 13), "value");
                }

                                CheckNullValue(value, value.Length - 1);
                ClearTokenHashTable();
                this.m_genitiveAbbreviatedMonthNames = value;
            }
        }

        public String[] MonthGenitiveNames
        {
            get
            {
                return ((String[])internalGetGenitiveMonthNames(false).Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
                }

                if (value.Length != 13)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", 13), "value");
                }

                                CheckNullValue(value, value.Length - 1);
                genitiveMonthNames = value;
                ClearTokenHashTable();
            }
        }

        private string m_fullTimeSpanPositivePattern;
        internal String FullTimeSpanPositivePattern
        {
            get
            {
                if (m_fullTimeSpanPositivePattern == null)
                {
                    CultureData cultureDataWithoutUserOverrides;
                    if (m_cultureData.UseUserOverride)
                        cultureDataWithoutUserOverrides = CultureData.GetCultureData(m_cultureData.CultureName, false);
                    else
                        cultureDataWithoutUserOverrides = m_cultureData;
                    String decimalSeparator = new NumberFormatInfo(cultureDataWithoutUserOverrides).NumberDecimalSeparator;
                    m_fullTimeSpanPositivePattern = "d':'h':'mm':'ss'" + decimalSeparator + "'FFFFFFF";
                }

                return m_fullTimeSpanPositivePattern;
            }
        }

        private string m_fullTimeSpanNegativePattern;
        internal String FullTimeSpanNegativePattern
        {
            get
            {
                if (m_fullTimeSpanNegativePattern == null)
                    m_fullTimeSpanNegativePattern = "'-'" + FullTimeSpanPositivePattern;
                return m_fullTimeSpanNegativePattern;
            }
        }

        internal CompareInfo CompareInfo
        {
            get
            {
                if (m_compareInfo == null)
                {
                    m_compareInfo = CompareInfo.GetCompareInfo(m_cultureData.SCOMPAREINFO);
                }

                return m_compareInfo;
            }
        }

        internal const DateTimeStyles InvalidDateTimeStyles = ~(DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowInnerWhite | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal | DateTimeStyles.RoundtripKind);
        internal static void ValidateStyles(DateTimeStyles style, String parameterName)
        {
            if ((style & InvalidDateTimeStyles) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDateTimeStyles"), parameterName);
            }

            if (((style & (DateTimeStyles.AssumeLocal)) != 0) && ((style & (DateTimeStyles.AssumeUniversal)) != 0))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ConflictingDateTimeStyles"), parameterName);
            }

                        if (((style & DateTimeStyles.RoundtripKind) != 0) && ((style & (DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)) != 0))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ConflictingDateTimeRoundtripStyles"), parameterName);
            }
        }

        internal DateTimeFormatFlags FormatFlags
        {
            get
            {
                if (formatFlags == DateTimeFormatFlags.NotInitialized)
                {
                    formatFlags = DateTimeFormatFlags.None;
                    formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagGenitiveMonth(MonthNames, internalGetGenitiveMonthNames(false), AbbreviatedMonthNames, internalGetGenitiveMonthNames(true));
                    formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInMonthNames(MonthNames, internalGetGenitiveMonthNames(false), AbbreviatedMonthNames, internalGetGenitiveMonthNames(true));
                    formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInDayNames(DayNames, AbbreviatedDayNames);
                    formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseHebrewCalendar((int)Calendar.ID);
                }

                return (formatFlags);
            }
        }

        internal Boolean HasForceTwoDigitYears
        {
            get
            {
                switch (calendar.ID)
                {
                    case (Calendar.CAL_JAPAN):
                    case (Calendar.CAL_TAIWAN):
                        return true;
                }

                return false;
            }
        }

        internal Boolean HasYearMonthAdjustment
        {
            get
            {
                return ((FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0);
            }
        }

        internal Boolean YearMonthAdjustment(ref int year, ref int month, Boolean parsedMonthName)
        {
            if ((FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0)
            {
                if (year < 1000)
                {
                    year += 5000;
                }

                if (year < Calendar.GetYear(Calendar.MinSupportedDateTime) || year > Calendar.GetYear(Calendar.MaxSupportedDateTime))
                {
                    return false;
                }

                if (parsedMonthName)
                {
                    if (!Calendar.IsLeapYear(year))
                    {
                        if (month >= 8)
                        {
                            month--;
                        }
                        else if (month == 7)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        TokenHashValue[] m_dtfiTokenHash;
        private const int TOKEN_HASH_SIZE = 199;
        private const int SECOND_PRIME = 197;
        private const String dateSeparatorOrTimeZoneOffset = "-";
        private const String invariantDateSeparator = "/";
        private const String invariantTimeSeparator = ":";
        internal const String IgnorablePeriod = ".";
        internal const String IgnorableComma = ",";
        internal const String CJKYearSuff = "\u5e74";
        internal const String CJKMonthSuff = "\u6708";
        internal const String CJKDaySuff = "\u65e5";
        internal const String KoreanYearSuff = "\ub144";
        internal const String KoreanMonthSuff = "\uc6d4";
        internal const String KoreanDaySuff = "\uc77c";
        internal const String KoreanHourSuff = "\uc2dc";
        internal const String KoreanMinuteSuff = "\ubd84";
        internal const String KoreanSecondSuff = "\ucd08";
        internal const String CJKHourSuff = "\u6642";
        internal const String ChineseHourSuff = "\u65f6";
        internal const String CJKMinuteSuff = "\u5206";
        internal const String CJKSecondSuff = "\u79d2";
        internal const String LocalTimeMark = "T";
        internal const String KoreanLangName = "ko";
        internal const String JapaneseLangName = "ja";
        internal const String EnglishLangName = "en";
        private static volatile DateTimeFormatInfo s_jajpDTFI;
        private static volatile DateTimeFormatInfo s_zhtwDTFI;
        internal static DateTimeFormatInfo GetJapaneseCalendarDTFI()
        {
            DateTimeFormatInfo temp = s_jajpDTFI;
            if (temp == null)
            {
                temp = new CultureInfo("ja-JP", false).DateTimeFormat;
                temp.Calendar = JapaneseCalendar.GetDefaultInstance();
                s_jajpDTFI = temp;
            }

            return (temp);
        }

        internal static DateTimeFormatInfo GetTaiwanCalendarDTFI()
        {
            DateTimeFormatInfo temp = s_zhtwDTFI;
            if (temp == null)
            {
                temp = new CultureInfo("zh-TW", false).DateTimeFormat;
                temp.Calendar = TaiwanCalendar.GetDefaultInstance();
                s_zhtwDTFI = temp;
            }

            return (temp);
        }

        private void ClearTokenHashTable()
        {
            m_dtfiTokenHash = null;
            formatFlags = DateTimeFormatFlags.NotInitialized;
        }

        internal TokenHashValue[] CreateTokenHashTable()
        {
            TokenHashValue[] temp = m_dtfiTokenHash;
            if (temp == null)
            {
                temp = new TokenHashValue[TOKEN_HASH_SIZE];
                bool koreanLanguage = LanguageName.Equals(KoreanLangName);
                string sep = this.TimeSeparator.Trim();
                if (IgnorableComma != sep)
                    InsertHash(temp, IgnorableComma, TokenType.IgnorableSymbol, 0);
                if (IgnorablePeriod != sep)
                    InsertHash(temp, IgnorablePeriod, TokenType.IgnorableSymbol, 0);
                if (KoreanHourSuff != sep && CJKHourSuff != sep && ChineseHourSuff != sep)
                {
                    InsertHash(temp, this.TimeSeparator, TokenType.SEP_Time, 0);
                }

                InsertHash(temp, this.AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                InsertHash(temp, this.PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);
                if (LanguageName.Equals("sq"))
                {
                    InsertHash(temp, IgnorablePeriod + this.AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                    InsertHash(temp, IgnorablePeriod + this.PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);
                }

                InsertHash(temp, CJKYearSuff, TokenType.SEP_YearSuff, 0);
                InsertHash(temp, KoreanYearSuff, TokenType.SEP_YearSuff, 0);
                InsertHash(temp, CJKMonthSuff, TokenType.SEP_MonthSuff, 0);
                InsertHash(temp, KoreanMonthSuff, TokenType.SEP_MonthSuff, 0);
                InsertHash(temp, CJKDaySuff, TokenType.SEP_DaySuff, 0);
                InsertHash(temp, KoreanDaySuff, TokenType.SEP_DaySuff, 0);
                InsertHash(temp, CJKHourSuff, TokenType.SEP_HourSuff, 0);
                InsertHash(temp, ChineseHourSuff, TokenType.SEP_HourSuff, 0);
                InsertHash(temp, CJKMinuteSuff, TokenType.SEP_MinuteSuff, 0);
                InsertHash(temp, CJKSecondSuff, TokenType.SEP_SecondSuff, 0);
                if (koreanLanguage)
                {
                    InsertHash(temp, KoreanHourSuff, TokenType.SEP_HourSuff, 0);
                    InsertHash(temp, KoreanMinuteSuff, TokenType.SEP_MinuteSuff, 0);
                    InsertHash(temp, KoreanSecondSuff, TokenType.SEP_SecondSuff, 0);
                }

                if (LanguageName.Equals("ky"))
                {
                    InsertHash(temp, dateSeparatorOrTimeZoneOffset, TokenType.IgnorableSymbol, 0);
                }
                else
                {
                    InsertHash(temp, dateSeparatorOrTimeZoneOffset, TokenType.SEP_DateOrOffset, 0);
                }

                String[] dateWords = null;
                DateTimeFormatInfoScanner scanner = null;
                scanner = new DateTimeFormatInfoScanner();
                m_dateWords = dateWords = scanner.GetDateWordsOfDTFI(this);
                DateTimeFormatFlags flag = FormatFlags;
                bool useDateSepAsIgnorableSymbol = false;
                String monthPostfix = null;
                if (dateWords != null)
                {
                    for (int i = 0; i < dateWords.Length; i++)
                    {
                        switch (dateWords[i][0])
                        {
                            case DateTimeFormatInfoScanner.MonthPostfixChar:
                                monthPostfix = dateWords[i].Substring(1);
                                AddMonthNames(temp, monthPostfix);
                                break;
                            case DateTimeFormatInfoScanner.IgnorableSymbolChar:
                                String symbol = dateWords[i].Substring(1);
                                InsertHash(temp, symbol, TokenType.IgnorableSymbol, 0);
                                if (this.DateSeparator.Trim(null).Equals(symbol))
                                {
                                    useDateSepAsIgnorableSymbol = true;
                                }

                                break;
                            default:
                                InsertHash(temp, dateWords[i], TokenType.DateWordToken, 0);
                                if (LanguageName.Equals("eu"))
                                {
                                    InsertHash(temp, IgnorablePeriod + dateWords[i], TokenType.DateWordToken, 0);
                                }

                                break;
                        }
                    }
                }

                if (!useDateSepAsIgnorableSymbol)
                {
                    InsertHash(temp, this.DateSeparator, TokenType.SEP_Date, 0);
                }

                AddMonthNames(temp, null);
                for (int i = 1; i <= 13; i++)
                {
                    InsertHash(temp, GetAbbreviatedMonthName(i), TokenType.MonthToken, i);
                }

                if ((FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
                {
                    for (int i = 1; i <= 13; i++)
                    {
                        String str;
                        str = internalGetMonthName(i, MonthNameStyles.Genitive, false);
                        InsertHash(temp, str, TokenType.MonthToken, i);
                    }
                }

                if ((FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != 0)
                {
                    for (int i = 1; i <= 13; i++)
                    {
                        String str;
                        str = internalGetMonthName(i, MonthNameStyles.LeapYear, false);
                        InsertHash(temp, str, TokenType.MonthToken, i);
                    }
                }

                for (int i = 0; i < 7; i++)
                {
                    String str = GetDayName((DayOfWeek)i);
                    InsertHash(temp, str, TokenType.DayOfWeekToken, i);
                    str = GetAbbreviatedDayName((DayOfWeek)i);
                    InsertHash(temp, str, TokenType.DayOfWeekToken, i);
                }

                int[] eras = calendar.Eras;
                for (int i = 1; i <= eras.Length; i++)
                {
                    InsertHash(temp, GetEraName(i), TokenType.EraToken, i);
                    InsertHash(temp, GetAbbreviatedEraName(i), TokenType.EraToken, i);
                }

                if (LanguageName.Equals(JapaneseLangName))
                {
                    for (int i = 0; i < 7; i++)
                    {
                        String specialDayOfWeek = "(" + GetAbbreviatedDayName((DayOfWeek)i) + ")";
                        InsertHash(temp, specialDayOfWeek, TokenType.DayOfWeekToken, i);
                    }

                    if (this.Calendar.GetType() != typeof (JapaneseCalendar))
                    {
                        DateTimeFormatInfo jaDtfi = GetJapaneseCalendarDTFI();
                        for (int i = 1; i <= jaDtfi.Calendar.Eras.Length; i++)
                        {
                            InsertHash(temp, jaDtfi.GetEraName(i), TokenType.JapaneseEraToken, i);
                            InsertHash(temp, jaDtfi.GetAbbreviatedEraName(i), TokenType.JapaneseEraToken, i);
                            InsertHash(temp, jaDtfi.AbbreviatedEnglishEraNames[i - 1], TokenType.JapaneseEraToken, i);
                        }
                    }
                }
                else if (CultureName.Equals("zh-TW"))
                {
                    DateTimeFormatInfo twDtfi = GetTaiwanCalendarDTFI();
                    for (int i = 1; i <= twDtfi.Calendar.Eras.Length; i++)
                    {
                        if (twDtfi.GetEraName(i).Length > 0)
                        {
                            InsertHash(temp, twDtfi.GetEraName(i), TokenType.TEraToken, i);
                        }
                    }
                }

                InsertHash(temp, InvariantInfo.AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                InsertHash(temp, InvariantInfo.PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);
                for (int i = 1; i <= 12; i++)
                {
                    String str;
                    str = InvariantInfo.GetMonthName(i);
                    InsertHash(temp, str, TokenType.MonthToken, i);
                    str = InvariantInfo.GetAbbreviatedMonthName(i);
                    InsertHash(temp, str, TokenType.MonthToken, i);
                }

                for (int i = 0; i < 7; i++)
                {
                    String str = InvariantInfo.GetDayName((DayOfWeek)i);
                    InsertHash(temp, str, TokenType.DayOfWeekToken, i);
                    str = InvariantInfo.GetAbbreviatedDayName((DayOfWeek)i);
                    InsertHash(temp, str, TokenType.DayOfWeekToken, i);
                }

                for (int i = 0; i < AbbreviatedEnglishEraNames.Length; i++)
                {
                    InsertHash(temp, AbbreviatedEnglishEraNames[i], TokenType.EraToken, i + 1);
                }

                InsertHash(temp, LocalTimeMark, TokenType.SEP_LocalTimeMark, 0);
                InsertHash(temp, DateTimeParse.GMTName, TokenType.TimeZoneToken, 0);
                InsertHash(temp, DateTimeParse.ZuluName, TokenType.TimeZoneToken, 0);
                InsertHash(temp, invariantDateSeparator, TokenType.SEP_Date, 0);
                InsertHash(temp, invariantTimeSeparator, TokenType.SEP_Time, 0);
                m_dtfiTokenHash = temp;
            }

            return (temp);
        }

        private void AddMonthNames(TokenHashValue[] temp, String monthPostfix)
        {
            for (int i = 1; i <= 13; i++)
            {
                String str;
                str = GetMonthName(i);
                if (str.Length > 0)
                {
                    if (monthPostfix != null)
                    {
                        InsertHash(temp, str + monthPostfix, TokenType.MonthToken, i);
                    }
                    else
                    {
                        InsertHash(temp, str, TokenType.MonthToken, i);
                    }
                }

                str = GetAbbreviatedMonthName(i);
                InsertHash(temp, str, TokenType.MonthToken, i);
            }
        }

        private static bool TryParseHebrewNumber(ref __DTString str, out Boolean badFormat, out int number)
        {
            number = -1;
            badFormat = false;
            int i = str.Index;
            if (!HebrewNumber.IsDigit(str.Value[i]))
            {
                return (false);
            }

            HebrewNumberParsingContext context = new HebrewNumberParsingContext(0);
            HebrewNumberParsingState state;
            do
            {
                state = HebrewNumber.ParseByChar(str.Value[i++], ref context);
                switch (state)
                {
                    case HebrewNumberParsingState.InvalidHebrewNumber:
                    case HebrewNumberParsingState.NotHebrewDigit:
                        return (false);
                }
            }
            while (i < str.Value.Length && (state != HebrewNumberParsingState.FoundEndOfHebrewNumber));
                        if (state != HebrewNumberParsingState.FoundEndOfHebrewNumber)
            {
                return (false);
            }

            str.Advance(i - str.Index);
            number = context.result;
            return (true);
        }

        private static bool IsHebrewChar(char ch)
        {
            return (ch >= '\x0590' && ch <= '\x05ff');
        }

        internal bool Tokenize(TokenType TokenMask, out TokenType tokenType, out int tokenValue, ref __DTString str)
        {
            tokenType = TokenType.UnknownToken;
            tokenValue = 0;
            TokenHashValue value;
                        char ch = str.m_current;
            bool isLetter = Char.IsLetter(ch);
            if (isLetter)
            {
                ch = Char.ToLower(ch, this.Culture);
                if (IsHebrewChar(ch) && TokenMask == TokenType.RegularTokenMask)
                {
                    bool badFormat;
                    if (TryParseHebrewNumber(ref str, out badFormat, out tokenValue))
                    {
                        if (badFormat)
                        {
                            tokenType = TokenType.UnknownToken;
                            return (false);
                        }

                        tokenType = TokenType.HebrewNumber;
                        return (true);
                    }
                }
            }

            int hashcode = ch % TOKEN_HASH_SIZE;
            int hashProbe = 1 + ch % SECOND_PRIME;
            int remaining = str.len - str.Index;
            int i = 0;
            TokenHashValue[] hashTable = m_dtfiTokenHash;
            if (hashTable == null)
            {
                hashTable = CreateTokenHashTable();
            }

            do
            {
                value = hashTable[hashcode];
                if (value == null)
                {
                    break;
                }

                if (((int)value.tokenType & (int)TokenMask) > 0 && value.tokenString.Length <= remaining)
                {
                    if (String.Compare(str.Value, str.Index, value.tokenString, 0, value.tokenString.Length, this.Culture, CompareOptions.IgnoreCase) == 0)
                    {
                        if (isLetter)
                        {
                            int nextCharIndex;
                            if ((nextCharIndex = str.Index + value.tokenString.Length) < str.len)
                            {
                                char nextCh = str.Value[nextCharIndex];
                                if (Char.IsLetter(nextCh))
                                {
                                    return (false);
                                }
                            }
                        }

                        tokenType = value.tokenType & TokenMask;
                        tokenValue = value.tokenValue;
                        str.Advance(value.tokenString.Length);
                        return (true);
                    }
                    else if (value.tokenType == TokenType.MonthToken && HasSpacesInMonthNames)
                    {
                        int matchStrLen = 0;
                        if (str.MatchSpecifiedWords(value.tokenString, true, ref matchStrLen))
                        {
                            tokenType = value.tokenType & TokenMask;
                            tokenValue = value.tokenValue;
                            str.Advance(matchStrLen);
                            return (true);
                        }
                    }
                    else if (value.tokenType == TokenType.DayOfWeekToken && HasSpacesInDayNames)
                    {
                        int matchStrLen = 0;
                        if (str.MatchSpecifiedWords(value.tokenString, true, ref matchStrLen))
                        {
                            tokenType = value.tokenType & TokenMask;
                            tokenValue = value.tokenValue;
                            str.Advance(matchStrLen);
                            return (true);
                        }
                    }
                }

                i++;
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE)
                    hashcode -= TOKEN_HASH_SIZE;
            }
            while (i < TOKEN_HASH_SIZE);
            return (false);
        }

        void InsertAtCurrentHashNode(TokenHashValue[] hashTable, String str, char ch, TokenType tokenType, int tokenValue, int pos, int hashcode, int hashProbe)
        {
            TokenHashValue previousNode = hashTable[hashcode];
            hashTable[hashcode] = new TokenHashValue(str, tokenType, tokenValue);
            ;
            while (++pos < TOKEN_HASH_SIZE)
            {
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE)
                    hashcode -= TOKEN_HASH_SIZE;
                TokenHashValue temp = hashTable[hashcode];
                if (temp != null && Char.ToLower(temp.tokenString[0], this.Culture) != ch)
                {
                    continue;
                }

                hashTable[hashcode] = previousNode;
                if (temp == null)
                {
                    return;
                }

                previousNode = temp;
            }

            ;
                    }

        void InsertHash(TokenHashValue[] hashTable, String str, TokenType tokenType, int tokenValue)
        {
            if (str == null || str.Length == 0)
            {
                return;
            }

            TokenHashValue value;
            int i = 0;
            if (Char.IsWhiteSpace(str[0]) || Char.IsWhiteSpace(str[str.Length - 1]))
            {
                str = str.Trim(null);
                if (str.Length == 0)
                    return;
            }

            char ch = Char.ToLower(str[0], this.Culture);
            int hashcode = ch % TOKEN_HASH_SIZE;
            int hashProbe = 1 + ch % SECOND_PRIME;
            do
            {
                value = hashTable[hashcode];
                if (value == null)
                {
                    hashTable[hashcode] = new TokenHashValue(str, tokenType, tokenValue);
                    return;
                }
                else
                {
                    if (str.Length >= value.tokenString.Length)
                    {
                        if (String.Compare(str, 0, value.tokenString, 0, value.tokenString.Length, this.Culture, CompareOptions.IgnoreCase) == 0)
                        {
                            if (str.Length > value.tokenString.Length)
                            {
                                InsertAtCurrentHashNode(hashTable, str, ch, tokenType, tokenValue, i, hashcode, hashProbe);
                                return;
                            }
                            else
                            {
                                int nTokenType = (int)tokenType;
                                int nCurrentTokenTypeInHash = (int)value.tokenType;
                                if (preferExistingTokens || BinaryCompatibility.TargetsAtLeast_Desktop_V4_5_1)
                                {
                                    if (((nCurrentTokenTypeInHash & (int)TokenType.RegularTokenMask) == 0) && ((nTokenType & (int)TokenType.RegularTokenMask) != 0) || ((nCurrentTokenTypeInHash & (int)TokenType.SeparatorTokenMask) == 0) && ((nTokenType & (int)TokenType.SeparatorTokenMask) != 0))
                                    {
                                        value.tokenType |= tokenType;
                                        if (tokenValue != 0)
                                        {
                                            value.tokenValue = tokenValue;
                                        }
                                    }
                                }
                                else
                                {
                                    if ((((nTokenType | nCurrentTokenTypeInHash) & (int)TokenType.RegularTokenMask) == nTokenType) || (((nTokenType | nCurrentTokenTypeInHash) & (int)TokenType.SeparatorTokenMask) == nTokenType))
                                    {
                                        value.tokenType |= tokenType;
                                        if (tokenValue != 0)
                                        {
                                            value.tokenValue = tokenValue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                i++;
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE)
                    hashcode -= TOKEN_HASH_SIZE;
            }
            while (i < TOKEN_HASH_SIZE);
                    }
    }

    internal class TokenHashValue
    {
        internal String tokenString;
        internal TokenType tokenType;
        internal int tokenValue;
        internal TokenHashValue(String tokenString, TokenType tokenType, int tokenValue)
        {
            this.tokenString = tokenString;
            this.tokenType = tokenType;
            this.tokenValue = tokenValue;
        }
    }
}