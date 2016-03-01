using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal class CalendarData
    {
        internal const int MAX_CALENDARS = 23;
        internal String sNativeName;
        internal String[] saShortDates;
        internal String[] saYearMonths;
        internal String[] saLongDates;
        internal String sMonthDay;
        internal String[] saEraNames;
        internal String[] saAbbrevEraNames;
        internal String[] saAbbrevEnglishEraNames;
        internal String[] saDayNames;
        internal String[] saAbbrevDayNames;
        internal String[] saSuperShortDayNames;
        internal String[] saMonthNames;
        internal String[] saAbbrevMonthNames;
        internal String[] saMonthGenitiveNames;
        internal String[] saAbbrevMonthGenitiveNames;
        internal String[] saLeapYearMonthNames;
        internal int iTwoDigitYearMax = 2029;
        internal int iCurrentEra = 0;
        internal bool bUseUserOverrides;
        internal static CalendarData Invariant;
        private CalendarData()
        {
        }

        static CalendarData()
        {
            CalendarData invariant = new CalendarData();
            invariant.sNativeName = "Gregorian Calendar";
            invariant.iTwoDigitYearMax = 2029;
            invariant.iCurrentEra = 1;
            invariant.saShortDates = new String[]{"MM/dd/yyyy", "yyyy-MM-dd"};
            invariant.saLongDates = new String[]{"dddd, dd MMMM yyyy"};
            invariant.saYearMonths = new String[]{"yyyy MMMM"};
            invariant.sMonthDay = "MMMM dd";
            invariant.saEraNames = new String[]{"A.D."};
            invariant.saAbbrevEraNames = new String[]{"AD"};
            invariant.saAbbrevEnglishEraNames = new String[]{"AD"};
            invariant.saDayNames = new String[]{"Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"};
            invariant.saAbbrevDayNames = new String[]{"Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"};
            invariant.saSuperShortDayNames = new String[]{"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"};
            invariant.saMonthNames = new String[]{"January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", String.Empty};
            invariant.saAbbrevMonthNames = new String[]{"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", String.Empty};
            invariant.saMonthGenitiveNames = invariant.saMonthNames;
            invariant.saAbbrevMonthGenitiveNames = invariant.saAbbrevMonthNames;
            invariant.saLeapYearMonthNames = invariant.saMonthNames;
            invariant.bUseUserOverrides = false;
            Invariant = invariant;
        }

        internal CalendarData(String localeName, int calendarId, bool bUseUserOverrides)
        {
            this.bUseUserOverrides = bUseUserOverrides;
            if (!nativeGetCalendarData(this, localeName, calendarId))
            {
                Contract.Assert(false, "[CalendarData] nativeGetCalendarData call isn't expected to fail for calendar " + calendarId + " locale " + localeName);
                if (this.sNativeName == null)
                    this.sNativeName = String.Empty;
                if (this.saShortDates == null)
                    this.saShortDates = Invariant.saShortDates;
                if (this.saYearMonths == null)
                    this.saYearMonths = Invariant.saYearMonths;
                if (this.saLongDates == null)
                    this.saLongDates = Invariant.saLongDates;
                if (this.sMonthDay == null)
                    this.sMonthDay = Invariant.sMonthDay;
                if (this.saEraNames == null)
                    this.saEraNames = Invariant.saEraNames;
                if (this.saAbbrevEraNames == null)
                    this.saAbbrevEraNames = Invariant.saAbbrevEraNames;
                if (this.saAbbrevEnglishEraNames == null)
                    this.saAbbrevEnglishEraNames = Invariant.saAbbrevEnglishEraNames;
                if (this.saDayNames == null)
                    this.saDayNames = Invariant.saDayNames;
                if (this.saAbbrevDayNames == null)
                    this.saAbbrevDayNames = Invariant.saAbbrevDayNames;
                if (this.saSuperShortDayNames == null)
                    this.saSuperShortDayNames = Invariant.saSuperShortDayNames;
                if (this.saMonthNames == null)
                    this.saMonthNames = Invariant.saMonthNames;
                if (this.saAbbrevMonthNames == null)
                    this.saAbbrevMonthNames = Invariant.saAbbrevMonthNames;
            }

            this.saShortDates = CultureData.ReescapeWin32Strings(this.saShortDates);
            this.saLongDates = CultureData.ReescapeWin32Strings(this.saLongDates);
            this.saYearMonths = CultureData.ReescapeWin32Strings(this.saYearMonths);
            this.sMonthDay = CultureData.ReescapeWin32String(this.sMonthDay);
            if ((CalendarId)calendarId == CalendarId.TAIWAN)
            {
                if (CultureInfo.IsTaiwanSku)
                {
                    this.sNativeName = "\x4e2d\x83ef\x6c11\x570b\x66c6";
                }
                else
                {
                    this.sNativeName = String.Empty;
                }
            }

            if (this.saMonthGenitiveNames == null || String.IsNullOrEmpty(this.saMonthGenitiveNames[0]))
                this.saMonthGenitiveNames = this.saMonthNames;
            if (this.saAbbrevMonthGenitiveNames == null || String.IsNullOrEmpty(this.saAbbrevMonthGenitiveNames[0]))
                this.saAbbrevMonthGenitiveNames = this.saAbbrevMonthNames;
            if (this.saLeapYearMonthNames == null || String.IsNullOrEmpty(this.saLeapYearMonthNames[0]))
                this.saLeapYearMonthNames = this.saMonthNames;
            InitializeEraNames(localeName, calendarId);
            InitializeAbbreviatedEraNames(localeName, calendarId);
            if (calendarId == (int)CalendarId.JAPAN)
            {
                this.saAbbrevEnglishEraNames = JapaneseCalendar.EnglishEraNames();
            }
            else
            {
                this.saAbbrevEnglishEraNames = new String[]{""};
            }

            this.iCurrentEra = this.saEraNames.Length;
        }

        private void InitializeEraNames(string localeName, int calendarId)
        {
            switch ((CalendarId)calendarId)
            {
                case CalendarId.GREGORIAN:
                    if (this.saEraNames == null || this.saEraNames.Length == 0 || String.IsNullOrEmpty(this.saEraNames[0]))
                    {
                        this.saEraNames = new String[]{"A.D."};
                    }

                    break;
                case CalendarId.GREGORIAN_US:
                case CalendarId.JULIAN:
                    this.saEraNames = new String[]{"A.D."};
                    break;
                case CalendarId.HEBREW:
                    this.saEraNames = new String[]{"C.E."};
                    break;
                case CalendarId.HIJRI:
                case CalendarId.UMALQURA:
                    if (localeName == "dv-MV")
                    {
                        this.saEraNames = new String[]{"\x0780\x07a8\x0796\x07b0\x0783\x07a9"};
                    }
                    else
                    {
                        this.saEraNames = new String[]{"\x0628\x0639\x062F \x0627\x0644\x0647\x062C\x0631\x0629"};
                    }

                    break;
                case CalendarId.GREGORIAN_ARABIC:
                case CalendarId.GREGORIAN_XLIT_ENGLISH:
                case CalendarId.GREGORIAN_XLIT_FRENCH:
                    this.saEraNames = new String[]{"\x0645"};
                    break;
                case CalendarId.GREGORIAN_ME_FRENCH:
                    this.saEraNames = new String[]{"ap. J.-C."};
                    break;
                case CalendarId.TAIWAN:
                    if (CultureInfo.IsTaiwanSku)
                    {
                        this.saEraNames = new String[]{"\x4e2d\x83ef\x6c11\x570b"};
                    }
                    else
                    {
                        this.saEraNames = new String[]{String.Empty};
                    }

                    break;
                case CalendarId.KOREA:
                    this.saEraNames = new String[]{"\xb2e8\xae30"};
                    break;
                case CalendarId.THAI:
                    this.saEraNames = new String[]{"\x0e1e\x002e\x0e28\x002e"};
                    break;
                case CalendarId.JAPAN:
                case CalendarId.JAPANESELUNISOLAR:
                    this.saEraNames = JapaneseCalendar.EraNames();
                    break;
                case CalendarId.PERSIAN:
                    if (this.saEraNames == null || this.saEraNames.Length == 0 || String.IsNullOrEmpty(this.saEraNames[0]))
                    {
                        this.saEraNames = new String[]{"\x0647\x002e\x0634"};
                    }

                    break;
                default:
                    this.saEraNames = Invariant.saEraNames;
                    break;
            }
        }

        private void InitializeAbbreviatedEraNames(string localeName, int calendarId)
        {
            switch ((CalendarId)calendarId)
            {
                case CalendarId.GREGORIAN:
                    if (this.saAbbrevEraNames == null || this.saAbbrevEraNames.Length == 0 || String.IsNullOrEmpty(this.saAbbrevEraNames[0]))
                    {
                        this.saAbbrevEraNames = new String[]{"AD"};
                    }

                    break;
                case CalendarId.GREGORIAN_US:
                case CalendarId.JULIAN:
                    this.saAbbrevEraNames = new String[]{"AD"};
                    break;
                case CalendarId.JAPAN:
                case CalendarId.JAPANESELUNISOLAR:
                    this.saAbbrevEraNames = JapaneseCalendar.AbbrevEraNames();
                    break;
                case CalendarId.HIJRI:
                case CalendarId.UMALQURA:
                    if (localeName == "dv-MV")
                    {
                        this.saAbbrevEraNames = new String[]{"\x0780\x002e"};
                    }
                    else
                    {
                        this.saAbbrevEraNames = new String[]{"\x0647\x0640"};
                    }

                    break;
                case CalendarId.TAIWAN:
                    this.saAbbrevEraNames = new String[1];
                    if (this.saEraNames[0].Length == 4)
                    {
                        this.saAbbrevEraNames[0] = this.saEraNames[0].Substring(2, 2);
                    }
                    else
                    {
                        this.saAbbrevEraNames[0] = this.saEraNames[0];
                    }

                    break;
                case CalendarId.PERSIAN:
                    if (this.saAbbrevEraNames == null || this.saAbbrevEraNames.Length == 0 || String.IsNullOrEmpty(this.saAbbrevEraNames[0]))
                    {
                        this.saAbbrevEraNames = this.saEraNames;
                    }

                    break;
                default:
                    this.saAbbrevEraNames = this.saEraNames;
                    break;
            }
        }

        internal static CalendarData GetCalendarData(int calendarId)
        {
            String culture = CalendarIdToCultureName(calendarId);
            return CultureInfo.GetCultureInfo(culture).m_cultureData.GetCalendar(calendarId);
        }

        private static String CalendarIdToCultureName(int calendarId)
        {
            switch (calendarId)
            {
                case Calendar.CAL_GREGORIAN_US:
                    return "fa-IR";
                case Calendar.CAL_JAPAN:
                    return "ja-JP";
                case Calendar.CAL_TAIWAN:
                    return "zh-TW";
                case Calendar.CAL_KOREA:
                    return "ko-KR";
                case Calendar.CAL_HIJRI:
                case Calendar.CAL_GREGORIAN_ARABIC:
                case Calendar.CAL_UMALQURA:
                    return "ar-SA";
                case Calendar.CAL_THAI:
                    return "th-TH";
                case Calendar.CAL_HEBREW:
                    return "he-IL";
                case Calendar.CAL_GREGORIAN_ME_FRENCH:
                    return "ar-DZ";
                case Calendar.CAL_GREGORIAN_XLIT_ENGLISH:
                case Calendar.CAL_GREGORIAN_XLIT_FRENCH:
                    return "ar-IQ";
                default:
                    break;
            }

            return "en-US";
        }

        internal void FixupWin7MonthDaySemicolonBug()
        {
            int unescapedCharacterIndex = FindUnescapedCharacter(sMonthDay, ';');
            if (unescapedCharacterIndex > 0)
            {
                sMonthDay = sMonthDay.Substring(0, unescapedCharacterIndex);
            }
        }

        private static int FindUnescapedCharacter(string s, char charToFind)
        {
            bool inComment = false;
            int length = s.Length;
            for (int i = 0; i < length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\'':
                        inComment = !inComment;
                        break;
                    case '\\':
                        i++;
                        break;
                    default:
                        if (!inComment && charToFind == c)
                        {
                            return i;
                        }

                        break;
                }
            }

            return -1;
        }

        internal static extern int nativeGetTwoDigitYearMax(int calID);
        private static extern bool nativeGetCalendarData(CalendarData data, String localeName, int calendar);
        internal static extern int nativeGetCalendars(String localeName, bool useUserOverride, [In, Out] int[] calendars);
    }
}