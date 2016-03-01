using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Globalization
{
    internal class CultureData
    {
        const int undef = -1;
        private String sRealName;
        private String sWindowsName;
        private String sName;
        private String sParent;
        private String sLocalizedDisplayName;
        private String sEnglishDisplayName;
        private String sNativeDisplayName;
        private String sSpecificCulture;
        private String sISO639Language;
        private String sLocalizedLanguage;
        private String sEnglishLanguage;
        private String sNativeLanguage;
        private String sRegionName;
        private int iGeoId = undef;
        private String sLocalizedCountry;
        private String sEnglishCountry;
        private String sNativeCountry;
        private String sISO3166CountryName;
        private String sPositiveSign;
        private String sNegativeSign;
        private String[] saNativeDigits;
        private int iDigitSubstitution;
        private int iLeadingZeros;
        private int iDigits;
        private int iNegativeNumber;
        private int[] waGrouping;
        private String sDecimalSeparator;
        private String sThousandSeparator;
        private String sNaN;
        private String sPositiveInfinity;
        private String sNegativeInfinity;
        private int iNegativePercent = undef;
        private int iPositivePercent = undef;
        private String sPercent;
        private String sPerMille;
        private String sCurrency;
        private String sIntlMonetarySymbol;
        private String sEnglishCurrency;
        private String sNativeCurrency;
        private int iCurrencyDigits;
        private int iCurrency;
        private int iNegativeCurrency;
        private int[] waMonetaryGrouping;
        private String sMonetaryDecimal;
        private String sMonetaryThousand;
        private int iMeasure = undef;
        private String sListSeparator;
        private String sAM1159;
        private String sPM2359;
        private String sTimeSeparator;
        private volatile String[] saLongTimes;
        private volatile String[] saShortTimes;
        private volatile String[] saDurationFormats;
        private int iFirstDayOfWeek = undef;
        private int iFirstWeekOfYear = undef;
        private volatile int[] waCalendars;
        private CalendarData[] calendars;
        private int iReadingLayout = undef;
        private String sTextInfo;
        private String sCompareInfo;
        private String sScripts;
        private bool bUseOverrides;
        private bool bNeutral;
        internal static CultureData Invariant
        {
            get
            {
                if (s_Invariant == null)
                {
                    CultureData invariant = new CultureData();
                    invariant.bUseOverrides = false;
                    invariant.sRealName = "";
                    invariant.sWindowsName = "";
                    invariant.sName = "";
                    invariant.sParent = "";
                    invariant.bNeutral = false;
                    invariant.sEnglishDisplayName = "Invariant Language (Invariant Country)";
                    invariant.sNativeDisplayName = "Invariant Language (Invariant Country)";
                    invariant.sSpecificCulture = "";
                    invariant.sISO639Language = "iv";
                    invariant.sLocalizedLanguage = "Invariant Language";
                    invariant.sEnglishLanguage = "Invariant Language";
                    invariant.sNativeLanguage = "Invariant Language";
                    invariant.sRegionName = "IV";
                    invariant.iGeoId = 244;
                    invariant.sEnglishCountry = "Invariant Country";
                    invariant.sNativeCountry = "Invariant Country";
                    invariant.sISO3166CountryName = "IV";
                    invariant.sPositiveSign = "+";
                    invariant.sNegativeSign = "-";
                    invariant.saNativeDigits = new String[]{"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};
                    invariant.iDigitSubstitution = 1;
                    invariant.iLeadingZeros = 1;
                    invariant.iDigits = 2;
                    invariant.iNegativeNumber = 1;
                    invariant.waGrouping = new int[]{3};
                    invariant.sDecimalSeparator = ".";
                    invariant.sThousandSeparator = ",";
                    invariant.sNaN = "NaN";
                    invariant.sPositiveInfinity = "Infinity";
                    invariant.sNegativeInfinity = "-Infinity";
                    invariant.iNegativePercent = 0;
                    invariant.iPositivePercent = 0;
                    invariant.sPercent = "%";
                    invariant.sPerMille = "\x2030";
                    invariant.sCurrency = "\x00a4";
                    invariant.sIntlMonetarySymbol = "XDR";
                    invariant.sEnglishCurrency = "International Monetary Fund";
                    invariant.sNativeCurrency = "International Monetary Fund";
                    invariant.iCurrencyDigits = 2;
                    invariant.iCurrency = 0;
                    invariant.iNegativeCurrency = 0;
                    invariant.waMonetaryGrouping = new int[]{3};
                    invariant.sMonetaryDecimal = ".";
                    invariant.sMonetaryThousand = ",";
                    invariant.iMeasure = 0;
                    invariant.sListSeparator = ",";
                    invariant.sAM1159 = "AM";
                    invariant.sPM2359 = "PM";
                    invariant.saLongTimes = new String[]{"HH:mm:ss"};
                    invariant.saShortTimes = new String[]{"HH:mm", "hh:mm tt", "H:mm", "h:mm tt"};
                    invariant.saDurationFormats = new String[]{"HH:mm:ss"};
                    invariant.iFirstDayOfWeek = 0;
                    invariant.iFirstWeekOfYear = 0;
                    invariant.waCalendars = new int[]{(int)CalendarId.GREGORIAN};
                    invariant.calendars = new CalendarData[CalendarData.MAX_CALENDARS];
                    invariant.calendars[0] = CalendarData.Invariant;
                    invariant.iReadingLayout = 0;
                    invariant.sTextInfo = "";
                    invariant.sCompareInfo = "";
                    invariant.sScripts = "Latn;";
                    s_Invariant = invariant;
                }

                return s_Invariant;
            }
        }

        private volatile static CultureData s_Invariant;
        private static volatile Dictionary<String, CultureData> s_cachedCultures;
        internal static CultureData GetCultureData(String cultureName, bool useUserOverride)
        {
            if (String.IsNullOrEmpty(cultureName))
            {
                return CultureData.Invariant;
            }

            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                if (cultureName.Equals("iw", StringComparison.OrdinalIgnoreCase))
                {
                    cultureName = "he";
                }
                else if (cultureName.Equals("tl", StringComparison.OrdinalIgnoreCase))
                {
                    cultureName = "fil";
                }
                else if (cultureName.Equals("english", StringComparison.OrdinalIgnoreCase))
                {
                    cultureName = "en";
                }
            }

            String hashName = AnsiToLower(useUserOverride ? cultureName : cultureName + '*');
            Dictionary<String, CultureData> tempHashTable = s_cachedCultures;
            if (tempHashTable == null)
            {
                tempHashTable = new Dictionary<String, CultureData>();
            }
            else
            {
                CultureData retVal;
                lock (((ICollection)tempHashTable).SyncRoot)
                {
                    tempHashTable.TryGetValue(hashName, out retVal);
                }

                if (retVal != null)
                {
                    return retVal;
                }
            }

            CultureData culture = CreateCultureData(cultureName, useUserOverride);
            if (culture == null)
            {
                return null;
            }

            lock (((ICollection)tempHashTable).SyncRoot)
            {
                tempHashTable[hashName] = culture;
            }

            s_cachedCultures = tempHashTable;
            return culture;
        }

        private static CultureData CreateCultureData(string cultureName, bool useUserOverride)
        {
            CultureData culture = new CultureData();
            culture.bUseOverrides = useUserOverride;
            culture.sRealName = cultureName;
            if (culture.InitCultureData() == false)
            {
                {
                    return null;
                }
            }

            return culture;
        }

        private bool InitCultureData()
        {
            if (nativeInitCultureData(this) == false)
            {
                return false;
            }

            return true;
        }

        private static volatile Dictionary<String, CultureData> s_cachedRegions;
        internal static CultureData GetCultureDataForRegion(String cultureName, bool useUserOverride)
        {
            if (String.IsNullOrEmpty(cultureName))
            {
                return CultureData.Invariant;
            }

            CultureData retVal = GetCultureData(cultureName, useUserOverride);
            if (retVal != null && (retVal.IsNeutralCulture == false))
                return retVal;
            CultureData neutral = retVal;
            String hashName = AnsiToLower(useUserOverride ? cultureName : cultureName + '*');
            Dictionary<String, CultureData> tempHashTable = s_cachedRegions;
            if (tempHashTable == null)
            {
                tempHashTable = new Dictionary<String, CultureData>();
            }
            else
            {
                lock (((ICollection)tempHashTable).SyncRoot)
                {
                    tempHashTable.TryGetValue(hashName, out retVal);
                }

                if (retVal != null)
                {
                    return retVal;
                }
            }

            if (retVal == null || (retVal.IsNeutralCulture == true))
            {
                CultureInfo[] specifics = SpecificCultures;
                for (int i = 0; i < specifics.Length; i++)
                {
                    if (String.Compare(specifics[i].m_cultureData.SREGIONNAME, cultureName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        retVal = specifics[i].m_cultureData;
                        break;
                    }
                }
            }

            if (retVal != null && (retVal.IsNeutralCulture == false))
            {
                lock (((ICollection)tempHashTable).SyncRoot)
                {
                    tempHashTable[hashName] = retVal;
                }

                s_cachedRegions = tempHashTable;
            }
            else
            {
                retVal = neutral;
            }

            return retVal;
        }

        internal static void ClearCachedData()
        {
            s_cachedCultures = null;
        }

        internal static CultureInfo[] GetCultures(CultureTypes types)
        {
            if ((int)types <= 0 || ((int)types & (int)~(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures | CultureTypes.InstalledWin32Cultures | CultureTypes.UserCustomCulture | CultureTypes.ReplacementCultures | CultureTypes.WindowsOnlyCultures | CultureTypes.FrameworkCultures)) != 0)
            {
                throw new ArgumentOutOfRangeException("types", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), CultureTypes.NeutralCultures, CultureTypes.FrameworkCultures));
            }

            if ((types & CultureTypes.WindowsOnlyCultures) != 0)
            {
                types &= (~CultureTypes.WindowsOnlyCultures);
            }

            String[] cultureNames = null;
            if (nativeEnumCultureNames((int)types, JitHelpers.GetObjectHandleOnStack(ref cultureNames)) == 0)
            {
                return new CultureInfo[0];
            }

            int arrayLength = cultureNames.Length;
            if ((types & (CultureTypes.NeutralCultures | CultureTypes.FrameworkCultures)) != 0)
            {
                arrayLength += 2;
            }

            CultureInfo[] cultures = new CultureInfo[arrayLength];
            for (int i = 0; i < cultureNames.Length; i++)
            {
                cultures[i] = new CultureInfo(cultureNames[i]);
            }

            if ((types & (CultureTypes.NeutralCultures | CultureTypes.FrameworkCultures)) != 0)
            {
                Contract.Assert(arrayLength == cultureNames.Length + 2, "CultureData.nativeEnumCultureNames() Incorrect array size");
                cultures[cultureNames.Length] = new CultureInfo("zh-CHS");
                cultures[cultureNames.Length + 1] = new CultureInfo("zh-CHT");
            }

            return cultures;
        }

        internal static volatile CultureInfo[] specificCultures;
        private static CultureInfo[] SpecificCultures
        {
            get
            {
                if (specificCultures == null)
                    specificCultures = GetCultures(CultureTypes.SpecificCultures);
                return specificCultures;
            }
        }

        internal String CultureName
        {
            get
            {
                Contract.Assert(this.sRealName != null, "[CultureData.CultureName] Expected this.sRealName to be populated by COMNlsInfo::nativeInitCultureData already");
                switch (this.sName)
                {
                    case "zh-CHS":
                    case "zh-CHT":
                        return this.sName;
                }

                return this.sRealName;
            }
        }

        internal bool UseUserOverride
        {
            get
            {
                return this.bUseOverrides;
            }
        }

        internal String SNAME
        {
            get
            {
                if (this.sName == null)
                {
                    this.sName = String.Empty;
                }

                return this.sName;
            }
        }

        internal String SPARENT
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sParent == null)
                {
                    this.sParent = DoGetLocaleInfo(this.sRealName, LOCALE_SPARENT);
                }

                return this.sParent;
            }
        }

        internal String SLOCALIZEDDISPLAYNAME
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sLocalizedDisplayName == null)
                {
                    if (String.IsNullOrEmpty(this.sLocalizedDisplayName))
                    {
                        if (this.IsNeutralCulture)
                        {
                            this.sLocalizedDisplayName = this.SLOCALIZEDLANGUAGE;
                        }
                        else if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && object.ReferenceEquals(this, s_Invariant))
                        {
                            this.sLocalizedDisplayName = this.SLOCALIZEDLANGUAGE;
                        }
                        else
                        {
                            if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
                            {
                                this.sLocalizedDisplayName = DoGetLocaleInfo(LOCALE_SLOCALIZEDDISPLAYNAME);
                            }

                            if (String.IsNullOrEmpty(this.sLocalizedDisplayName))
                            {
                                this.sLocalizedDisplayName = this.SNATIVEDISPLAYNAME;
                            }
                        }
                    }
                }

                return this.sLocalizedDisplayName;
            }
        }

        internal String SENGDISPLAYNAME
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sEnglishDisplayName == null)
                {
                    if (this.IsNeutralCulture)
                    {
                        this.sEnglishDisplayName = this.SENGLISHLANGUAGE;
                    }
                    else
                    {
                        this.sEnglishDisplayName = DoGetLocaleInfo(LOCALE_SENGLISHDISPLAYNAME);
                        if (String.IsNullOrEmpty(this.sEnglishDisplayName))
                        {
                            if (this.SENGLISHLANGUAGE.EndsWith(')'))
                            {
                                this.sEnglishDisplayName = this.SENGLISHLANGUAGE.Substring(0, this.sEnglishLanguage.Length - 1) + ", " + this.SENGCOUNTRY + ")";
                            }
                            else
                            {
                                this.sEnglishDisplayName = this.SENGLISHLANGUAGE + " (" + this.SENGCOUNTRY + ")";
                            }
                        }
                    }
                }

                return this.sEnglishDisplayName;
            }
        }

        internal String SNATIVEDISPLAYNAME
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sNativeDisplayName == null)
                {
                    if (this.IsNeutralCulture)
                    {
                        this.sNativeDisplayName = this.SNATIVELANGUAGE;
                    }
                    else
                    {
                        {
                            this.sNativeDisplayName = DoGetLocaleInfo(LOCALE_SNATIVEDISPLAYNAME);
                        }

                        if (String.IsNullOrEmpty(this.sNativeDisplayName))
                        {
                            this.sNativeDisplayName = this.SNATIVELANGUAGE + " (" + this.SNATIVECOUNTRY + ")";
                        }
                    }
                }

                return this.sNativeDisplayName;
            }
        }

        internal String SSPECIFICCULTURE
        {
            get
            {
                Contract.Assert(this.sSpecificCulture != null, "[CultureData.SSPECIFICCULTURE] Expected this.sSpecificCulture to be populated by COMNlsInfo::nativeInitCultureData already");
                return this.sSpecificCulture;
            }
        }

        internal String SISO639LANGNAME
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sISO639Language == null)
                {
                    this.sISO639Language = DoGetLocaleInfo(LOCALE_SISO639LANGNAME);
                }

                return this.sISO639Language;
            }
        }

        internal String SLOCALIZEDLANGUAGE
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sLocalizedLanguage == null)
                {
                    if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
                    {
                        this.sLocalizedLanguage = DoGetLocaleInfo(LOCALE_SLOCALIZEDLANGUAGENAME);
                    }

                    if (String.IsNullOrEmpty(this.sLocalizedLanguage))
                    {
                        this.sLocalizedLanguage = SNATIVELANGUAGE;
                    }
                }

                return this.sLocalizedLanguage;
            }
        }

        internal String SENGLISHLANGUAGE
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sEnglishLanguage == null)
                {
                    this.sEnglishLanguage = DoGetLocaleInfo(LOCALE_SENGLISHLANGUAGENAME);
                }

                return this.sEnglishLanguage;
            }
        }

        internal String SNATIVELANGUAGE
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sNativeLanguage == null)
                {
                    {
                        this.sNativeLanguage = DoGetLocaleInfo(LOCALE_SNATIVELANGUAGENAME);
                    }
                }

                return this.sNativeLanguage;
            }
        }

        internal String SREGIONNAME
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sRegionName == null)
                {
                    this.sRegionName = DoGetLocaleInfo(LOCALE_SISO3166CTRYNAME);
                }

                return this.sRegionName;
            }
        }

        internal int IGEOID
        {
            get
            {
                if (this.iGeoId == undef)
                {
                    this.iGeoId = DoGetLocaleInfoInt(LOCALE_IGEOID);
                }

                return this.iGeoId;
            }
        }

        internal string SLOCALIZEDCOUNTRY
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sLocalizedCountry == null)
                {
                    if (String.IsNullOrEmpty(this.sLocalizedCountry))
                    {
                        if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
                        {
                            this.sLocalizedCountry = DoGetLocaleInfo(LOCALE_SLOCALIZEDCOUNTRYNAME);
                        }

                        if (String.IsNullOrEmpty(this.sLocalizedDisplayName))
                        {
                            this.sLocalizedCountry = SNATIVECOUNTRY;
                        }
                    }
                }

                return this.sLocalizedCountry;
            }
        }

        internal String SENGCOUNTRY
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sEnglishCountry == null)
                {
                    this.sEnglishCountry = DoGetLocaleInfo(LOCALE_SENGLISHCOUNTRYNAME);
                }

                return this.sEnglishCountry;
            }
        }

        internal String SNATIVECOUNTRY
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sNativeCountry == null)
                {
                    this.sNativeCountry = DoGetLocaleInfo(LOCALE_SNATIVECOUNTRYNAME);
                }

                return this.sNativeCountry;
            }
        }

        internal String SISO3166CTRYNAME
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sISO3166CountryName == null)
                {
                    this.sISO3166CountryName = DoGetLocaleInfo(LOCALE_SISO3166CTRYNAME);
                }

                return this.sISO3166CountryName;
            }
        }

        internal int[] WAGROUPING
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.waGrouping == null)
                {
                    this.waGrouping = ConvertWin32GroupString(DoGetLocaleInfo(LOCALE_SGROUPING));
                }

                return this.waGrouping;
            }
        }

        internal String SNAN
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sNaN == null)
                {
                    this.sNaN = DoGetLocaleInfo(LOCALE_SNAN);
                }

                return this.sNaN;
            }
        }

        internal String SPOSINFINITY
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sPositiveInfinity == null)
                {
                    this.sPositiveInfinity = DoGetLocaleInfo(LOCALE_SPOSINFINITY);
                }

                return this.sPositiveInfinity;
            }
        }

        internal String SNEGINFINITY
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sNegativeInfinity == null)
                {
                    this.sNegativeInfinity = DoGetLocaleInfo(LOCALE_SNEGINFINITY);
                }

                return this.sNegativeInfinity;
            }
        }

        internal int INEGATIVEPERCENT
        {
            get
            {
                if (this.iNegativePercent == undef)
                {
                    this.iNegativePercent = DoGetLocaleInfoInt(LOCALE_INEGATIVEPERCENT);
                }

                return this.iNegativePercent;
            }
        }

        internal int IPOSITIVEPERCENT
        {
            get
            {
                if (this.iPositivePercent == undef)
                {
                    this.iPositivePercent = DoGetLocaleInfoInt(LOCALE_IPOSITIVEPERCENT);
                }

                return this.iPositivePercent;
            }
        }

        internal String SPERCENT
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sPercent == null)
                {
                    this.sPercent = DoGetLocaleInfo(LOCALE_SPERCENT);
                }

                return this.sPercent;
            }
        }

        internal String SPERMILLE
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sPerMille == null)
                {
                    this.sPerMille = DoGetLocaleInfo(LOCALE_SPERMILLE);
                }

                return this.sPerMille;
            }
        }

        internal String SCURRENCY
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sCurrency == null)
                {
                    this.sCurrency = DoGetLocaleInfo(LOCALE_SCURRENCY);
                }

                return this.sCurrency;
            }
        }

        internal String SINTLSYMBOL
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sIntlMonetarySymbol == null)
                {
                    this.sIntlMonetarySymbol = DoGetLocaleInfo(LOCALE_SINTLSYMBOL);
                }

                return this.sIntlMonetarySymbol;
            }
        }

        internal String SENGLISHCURRENCY
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sEnglishCurrency == null)
                {
                    this.sEnglishCurrency = DoGetLocaleInfo(LOCALE_SENGCURRNAME);
                }

                return this.sEnglishCurrency;
            }
        }

        internal String SNATIVECURRENCY
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sNativeCurrency == null)
                {
                    this.sNativeCurrency = DoGetLocaleInfo(LOCALE_SNATIVECURRNAME);
                }

                return this.sNativeCurrency;
            }
        }

        internal int[] WAMONGROUPING
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.waMonetaryGrouping == null)
                {
                    this.waMonetaryGrouping = ConvertWin32GroupString(DoGetLocaleInfo(LOCALE_SMONGROUPING));
                }

                return this.waMonetaryGrouping;
            }
        }

        internal int IMEASURE
        {
            get
            {
                if (this.iMeasure == undef)
                {
                    this.iMeasure = DoGetLocaleInfoInt(LOCALE_IMEASURE);
                }

                return this.iMeasure;
            }
        }

        internal String SLIST
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sListSeparator == null)
                {
                    this.sListSeparator = DoGetLocaleInfo(LOCALE_SLIST);
                }

                return this.sListSeparator;
            }
        }

        internal String SAM1159
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sAM1159 == null)
                {
                    this.sAM1159 = DoGetLocaleInfo(LOCALE_S1159);
                }

                return this.sAM1159;
            }
        }

        internal String SPM2359
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.sPM2359 == null)
                {
                    this.sPM2359 = DoGetLocaleInfo(LOCALE_S2359);
                }

                return this.sPM2359;
            }
        }

        internal String[] LongTimes
        {
            get
            {
                if (this.saLongTimes == null)
                {
                    String[] longTimes = DoEnumTimeFormats();
                    if (longTimes == null || longTimes.Length == 0)
                    {
                        this.saLongTimes = Invariant.saLongTimes;
                    }
                    else
                    {
                        this.saLongTimes = longTimes;
                    }
                }

                return this.saLongTimes;
            }
        }

        internal String[] ShortTimes
        {
            get
            {
                if (this.saShortTimes == null)
                {
                    String[] shortTimes = DoEnumShortTimeFormats();
                    if (shortTimes == null || shortTimes.Length == 0)
                    {
                        shortTimes = DeriveShortTimesFromLong();
                    }

                    this.saShortTimes = shortTimes;
                }

                return this.saShortTimes;
            }
        }

        private string[] DeriveShortTimesFromLong()
        {
            string[] shortTimes = new string[LongTimes.Length];
            for (int i = 0; i < LongTimes.Length; i++)
            {
                shortTimes[i] = StripSecondsFromPattern(LongTimes[i]);
            }

            return shortTimes;
        }

        private static string StripSecondsFromPattern(string time)
        {
            bool bEscape = false;
            int iLastToken = -1;
            for (int j = 0; j < time.Length; j++)
            {
                if (time[j] == '\'')
                {
                    bEscape = !bEscape;
                    continue;
                }

                if (time[j] == '\\')
                {
                    j++;
                    continue;
                }

                if (bEscape)
                {
                    continue;
                }

                switch (time[j])
                {
                    case 's':
                        if ((j - iLastToken) <= 4 && (j - iLastToken) > 1 && (time[iLastToken + 1] != '\'') && (time[j - 1] != '\''))
                        {
                            if (iLastToken >= 0)
                            {
                                j = iLastToken + 1;
                            }
                        }

                        bool containsSpace;
                        int endIndex = GetIndexOfNextTokenAfterSeconds(time, j, out containsSpace);
                        StringBuilder sb = new StringBuilder(time.Substring(0, j));
                        if (containsSpace)
                        {
                            sb.Append(' ');
                        }

                        sb.Append(time.Substring(endIndex));
                        time = sb.ToString();
                        break;
                    case 'm':
                    case 'H':
                    case 'h':
                        iLastToken = j;
                        break;
                }
            }

            return time;
        }

        private static int GetIndexOfNextTokenAfterSeconds(string time, int index, out bool containsSpace)
        {
            bool bEscape = false;
            containsSpace = false;
            for (; index < time.Length; index++)
            {
                switch (time[index])
                {
                    case '\'':
                        bEscape = !bEscape;
                        continue;
                    case '\\':
                        index++;
                        if (time[index] == ' ')
                        {
                            containsSpace = true;
                        }

                        continue;
                    case ' ':
                        containsSpace = true;
                        break;
                    case 't':
                    case 'm':
                    case 'H':
                    case 'h':
                        if (bEscape)
                        {
                            continue;
                        }

                        return index;
                }
            }

            containsSpace = false;
            return index;
        }

        internal String[] SADURATION
        {
            [System.Security.SecurityCritical]
            get
            {
                if (this.saDurationFormats == null)
                {
                    String durationFormat = DoGetLocaleInfo(LOCALE_SDURATION);
                    this.saDurationFormats = new String[]{ReescapeWin32String(durationFormat)};
                }

                return this.saDurationFormats;
            }
        }

        internal int IFIRSTDAYOFWEEK
        {
            get
            {
                if (this.iFirstDayOfWeek == undef)
                {
                    this.iFirstDayOfWeek = ConvertFirstDayOfWeekMonToSun(DoGetLocaleInfoInt(LOCALE_IFIRSTDAYOFWEEK));
                }

                return this.iFirstDayOfWeek;
            }
        }

        internal int IFIRSTWEEKOFYEAR
        {
            get
            {
                if (this.iFirstWeekOfYear == undef)
                {
                    this.iFirstWeekOfYear = DoGetLocaleInfoInt(LOCALE_IFIRSTWEEKOFYEAR);
                }

                return this.iFirstWeekOfYear;
            }
        }

        internal String[] ShortDates(int calendarId)
        {
            return GetCalendar(calendarId).saShortDates;
        }

        internal String[] LongDates(int calendarId)
        {
            return GetCalendar(calendarId).saLongDates;
        }

        internal String[] YearMonths(int calendarId)
        {
            return GetCalendar(calendarId).saYearMonths;
        }

        internal string[] DayNames(int calendarId)
        {
            return GetCalendar(calendarId).saDayNames;
        }

        internal string[] AbbreviatedDayNames(int calendarId)
        {
            return GetCalendar(calendarId).saAbbrevDayNames;
        }

        internal string[] SuperShortDayNames(int calendarId)
        {
            return GetCalendar(calendarId).saSuperShortDayNames;
        }

        internal string[] MonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saMonthNames;
        }

        internal string[] GenitiveMonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saMonthGenitiveNames;
        }

        internal string[] AbbreviatedMonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saAbbrevMonthNames;
        }

        internal string[] AbbreviatedGenitiveMonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saAbbrevMonthGenitiveNames;
        }

        internal string[] LeapYearMonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saLeapYearMonthNames;
        }

        internal String MonthDay(int calendarId)
        {
            return GetCalendar(calendarId).sMonthDay;
        }

        internal int[] CalendarIds
        {
            get
            {
                if (this.waCalendars == null)
                {
                    int[] calendarInts = new int[23];
                    Contract.Assert(this.sWindowsName != null, "[CultureData.CalendarIds] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
                    int count = CalendarData.nativeGetCalendars(this.sWindowsName, this.bUseOverrides, calendarInts);
                    if (count == 0)
                    {
                        this.waCalendars = Invariant.waCalendars;
                    }
                    else
                    {
                        if (this.sWindowsName == "zh-TW")
                        {
                            bool found = false;
                            for (int i = 0; i < count; i++)
                            {
                                if (calendarInts[i] == Calendar.CAL_TAIWAN)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                count++;
                                Array.Copy(calendarInts, 1, calendarInts, 2, 23 - 1 - 1);
                                calendarInts[1] = Calendar.CAL_TAIWAN;
                            }
                        }

                        int[] temp = new int[count];
                        Array.Copy(calendarInts, temp, count);
                        if (temp.Length > 1)
                        {
                            int i = DoGetLocaleInfoInt(LOCALE_ICALENDARTYPE);
                            if (temp[1] == i)
                            {
                                temp[1] = temp[0];
                                temp[0] = i;
                            }
                        }

                        this.waCalendars = temp;
                    }
                }

                return this.waCalendars;
            }
        }

        internal String CalendarName(int calendarId)
        {
            return GetCalendar(calendarId).sNativeName;
        }

        internal CalendarData GetCalendar(int calendarId)
        {
            Contract.Assert(calendarId > 0 && calendarId <= CalendarData.MAX_CALENDARS, "[CultureData.GetCalendar] Expect calendarId to be in a valid range");
            int calendarIndex = calendarId - 1;
            if (calendars == null)
            {
                calendars = new CalendarData[CalendarData.MAX_CALENDARS];
            }

            CalendarData calendarData = calendars[calendarIndex];
            if (calendarData == null)
            {
                Contract.Assert(this.sWindowsName != null, "[CultureData.GetCalendar] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
                calendarData = new CalendarData(this.sWindowsName, calendarId, this.UseUserOverride);
                calendars[calendarIndex] = calendarData;
            }

            return calendarData;
        }

        internal int CurrentEra(int calendarId)
        {
            return GetCalendar(calendarId).iCurrentEra;
        }

        internal bool IsRightToLeft
        {
            get
            {
                return (this.IREADINGLAYOUT == 1);
            }
        }

        private int IREADINGLAYOUT
        {
            get
            {
                if (this.iReadingLayout == undef)
                {
                    Contract.Assert(this.sRealName != null, "[CultureData.IsRightToLeft] Expected this.sRealName to be populated by COMNlsInfo::nativeInitCultureData already");
                    this.iReadingLayout = DoGetLocaleInfoInt(LOCALE_IREADINGLAYOUT);
                }

                return (this.iReadingLayout);
            }
        }

        internal String STEXTINFO
        {
            get
            {
                Contract.Assert(this.sWindowsName != null, "[CultureData.STEXTINFO] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
                return (this.sWindowsName);
            }
        }

        internal String SCOMPAREINFO
        {
            get
            {
                Contract.Assert(this.sWindowsName != null, "[CultureData.SCOMPAREINFO] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
                return (this.sWindowsName);
            }
        }

        internal bool IsSupplementalCustomCulture
        {
            get
            {
                return false;
            }
        }

        internal bool IsNeutralCulture
        {
            get
            {
                return this.bNeutral;
            }
        }

        internal bool IsInvariantCulture
        {
            get
            {
                return String.IsNullOrEmpty(this.SNAME);
            }
        }

        internal Calendar DefaultCalendar
        {
            get
            {
                int defaultCalId = DoGetLocaleInfoInt(LOCALE_ICALENDARTYPE);
                if (defaultCalId == 0)
                {
                    defaultCalId = this.CalendarIds[0];
                }

                return CultureInfo.GetCalendarInstance(defaultCalId);
            }
        }

        internal String[] EraNames(int calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saEraNames] Expected Calendar.ID > 0");
            return this.GetCalendar(calendarId).saEraNames;
        }

        internal String[] AbbrevEraNames(int calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saAbbrevEraNames] Expected Calendar.ID > 0");
            return this.GetCalendar(calendarId).saAbbrevEraNames;
        }

        internal String[] AbbreviatedEnglishEraNames(int calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saAbbrevEraNames] Expected Calendar.ID > 0");
            return this.GetCalendar(calendarId).saAbbrevEnglishEraNames;
        }

        internal String TimeSeparator
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (sTimeSeparator == null)
                {
                    string longTimeFormat = ReescapeWin32String(DoGetLocaleInfo(LOCALE_STIMEFORMAT));
                    if (String.IsNullOrEmpty(longTimeFormat))
                    {
                        longTimeFormat = LongTimes[0];
                    }

                    sTimeSeparator = GetTimeSeparator(longTimeFormat);
                }

                return sTimeSeparator;
            }
        }

        internal String DateSeparator(int calendarId)
        {
            return GetDateSeparator(ShortDates(calendarId)[0]);
        }

        static private String UnescapeNlsString(String str, int start, int end)
        {
            Contract.Requires(str != null);
            Contract.Requires(start >= 0);
            Contract.Requires(end >= 0);
            StringBuilder result = null;
            for (int i = start; i < str.Length && i <= end; i++)
            {
                switch (str[i])
                {
                    case '\'':
                        if (result == null)
                        {
                            result = new StringBuilder(str, start, i - start, str.Length);
                        }

                        break;
                    case '\\':
                        if (result == null)
                        {
                            result = new StringBuilder(str, start, i - start, str.Length);
                        }

                        ++i;
                        if (i < str.Length)
                        {
                            result.Append(str[i]);
                        }

                        break;
                    default:
                        if (result != null)
                        {
                            result.Append(str[i]);
                        }

                        break;
                }
            }

            if (result == null)
                return (str.Substring(start, end - start + 1));
            return (result.ToString());
        }

        static internal String ReescapeWin32String(String str)
        {
            if (str == null)
                return null;
            StringBuilder result = null;
            bool inQuote = false;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\'')
                {
                    if (inQuote)
                    {
                        if (i + 1 < str.Length && str[i + 1] == '\'')
                        {
                            if (result == null)
                                result = new StringBuilder(str, 0, i, str.Length * 2);
                            result.Append("\\'");
                            i++;
                            continue;
                        }

                        inQuote = false;
                    }
                    else
                    {
                        inQuote = true;
                    }
                }
                else if (str[i] == '\\')
                {
                    if (result == null)
                        result = new StringBuilder(str, 0, i, str.Length * 2);
                    result.Append("\\\\");
                    continue;
                }

                if (result != null)
                    result.Append(str[i]);
            }

            if (result == null)
                return str;
            return result.ToString();
        }

        static internal String[] ReescapeWin32Strings(String[] array)
        {
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = ReescapeWin32String(array[i]);
                }
            }

            return array;
        }

        static private String GetTimeSeparator(String format)
        {
            return GetSeparator(format, "Hhms");
        }

        static private String GetDateSeparator(String format)
        {
            return GetSeparator(format, "dyM");
        }

        private static string GetSeparator(string format, string timeParts)
        {
            int index = IndexOfTimePart(format, 0, timeParts);
            if (index != -1)
            {
                char cTimePart = format[index];
                do
                {
                    index++;
                }
                while (index < format.Length && format[index] == cTimePart);
                int separatorStart = index;
                if (separatorStart < format.Length)
                {
                    int separatorEnd = IndexOfTimePart(format, separatorStart, timeParts);
                    if (separatorEnd != -1)
                    {
                        return UnescapeNlsString(format, separatorStart, separatorEnd - 1);
                    }
                }
            }

            return String.Empty;
        }

        private static int IndexOfTimePart(string format, int startIndex, string timeParts)
        {
            Contract.Assert(startIndex >= 0, "startIndex cannot be negative");
            Contract.Assert(timeParts.IndexOfAny(new char[]{'\'', '\\'}) == -1, "timeParts cannot include quote characters");
            bool inQuote = false;
            for (int i = startIndex; i < format.Length; ++i)
            {
                if (!inQuote && timeParts.IndexOf(format[i]) != -1)
                {
                    return i;
                }

                switch (format[i])
                {
                    case '\\':
                        if (i + 1 < format.Length)
                        {
                            ++i;
                            switch (format[i])
                            {
                                case '\'':
                                case '\\':
                                    break;
                                default:
                                    --i;
                                    break;
                            }
                        }

                        break;
                    case '\'':
                        inQuote = !inQuote;
                        break;
                }
            }

            return -1;
        }

        string DoGetLocaleInfo(uint lctype)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoGetLocaleInfo] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
            return DoGetLocaleInfo(this.sWindowsName, lctype);
        }

        string DoGetLocaleInfo(string localeName, uint lctype)
        {
            if (!UseUserOverride)
            {
                lctype |= LOCALE_NOUSEROVERRIDE;
            }

            Contract.Assert(localeName != null, "[CultureData.DoGetLocaleInfo] Expected localeName to be not be null");
            string result = CultureInfo.nativeGetLocaleInfoEx(localeName, lctype);
            if (result == null)
            {
                result = String.Empty;
            }

            return result;
        }

        int DoGetLocaleInfoInt(uint lctype)
        {
            if (!UseUserOverride)
            {
                lctype |= LOCALE_NOUSEROVERRIDE;
            }

            Contract.Assert(this.sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
            int result = CultureInfo.nativeGetLocaleInfoExInt(this.sWindowsName, lctype);
            return result;
        }

        String[] DoEnumTimeFormats()
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoEnumTimeFormats] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
            String[] result = ReescapeWin32Strings(nativeEnumTimeFormats(this.sWindowsName, 0, UseUserOverride));
            return result;
        }

        String[] DoEnumShortTimeFormats()
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoEnumShortTimeFormats] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
            String[] result = ReescapeWin32Strings(nativeEnumTimeFormats(this.sWindowsName, TIME_NOSECONDS, UseUserOverride));
            return result;
        }

        internal static bool IsCustomCultureId(int cultureId)
        {
            if (cultureId == CultureInfo.LOCALE_CUSTOM_DEFAULT || cultureId == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED)
                return true;
            return false;
        }

        internal void GetNFIValues(NumberFormatInfo nfi)
        {
            if (this.IsInvariantCulture)
            {
                nfi.positiveSign = this.sPositiveSign;
                nfi.negativeSign = this.sNegativeSign;
                nfi.numberGroupSeparator = this.sThousandSeparator;
                nfi.numberDecimalSeparator = this.sDecimalSeparator;
                nfi.numberDecimalDigits = this.iDigits;
                nfi.numberNegativePattern = this.iNegativeNumber;
                nfi.currencySymbol = this.sCurrency;
                nfi.currencyGroupSeparator = this.sMonetaryThousand;
                nfi.currencyDecimalSeparator = this.sMonetaryDecimal;
                nfi.currencyDecimalDigits = this.iCurrencyDigits;
                nfi.currencyNegativePattern = this.iNegativeCurrency;
                nfi.currencyPositivePattern = this.iCurrency;
            }
            else
            {
                Contract.Assert(this.sWindowsName != null, "[CultureData.GetNFIValues] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
                CultureData.nativeGetNumberFormatInfoValues(this.sWindowsName, nfi, UseUserOverride);
            }

            nfi.numberGroupSizes = this.WAGROUPING;
            nfi.currencyGroupSizes = this.WAMONGROUPING;
            nfi.percentNegativePattern = this.INEGATIVEPERCENT;
            nfi.percentPositivePattern = this.IPOSITIVEPERCENT;
            nfi.percentSymbol = this.SPERCENT;
            nfi.perMilleSymbol = this.SPERMILLE;
            nfi.negativeInfinitySymbol = this.SNEGINFINITY;
            nfi.positiveInfinitySymbol = this.SPOSINFINITY;
            nfi.nanSymbol = this.SNAN;
            nfi.percentDecimalDigits = nfi.numberDecimalDigits;
            nfi.percentDecimalSeparator = nfi.numberDecimalSeparator;
            nfi.percentGroupSizes = nfi.numberGroupSizes;
            nfi.percentGroupSeparator = nfi.numberGroupSeparator;
            if (nfi.positiveSign == null || nfi.positiveSign.Length == 0)
                nfi.positiveSign = "+";
            if (nfi.currencyDecimalSeparator == null || nfi.currencyDecimalSeparator.Length == 0)
            {
                nfi.currencyDecimalSeparator = nfi.numberDecimalSeparator;
            }
        }

        static private int ConvertFirstDayOfWeekMonToSun(int iTemp)
        {
            iTemp++;
            if (iTemp > 6)
            {
                iTemp = 0;
            }

            return iTemp;
        }

        internal static string AnsiToLower(string testString)
        {
            StringBuilder sb = new StringBuilder(testString.Length);
            for (int ich = 0; ich < testString.Length; ich++)
            {
                char ch = testString[ich];
                sb.Append(ch <= 'Z' && ch >= 'A' ? (char)(ch - 'A' + 'a') : ch);
            }

            return (sb.ToString());
        }

        static private int[] ConvertWin32GroupString(String win32Str)
        {
            if (win32Str == null || win32Str.Length == 0)
            {
                return (new int[]{3});
            }

            if (win32Str[0] == '0')
            {
                return (new int[]{0});
            }

            int[] values;
            if (win32Str[win32Str.Length - 1] == '0')
            {
                values = new int[(win32Str.Length / 2)];
            }
            else
            {
                values = new int[(win32Str.Length / 2) + 2];
                values[values.Length - 1] = 0;
            }

            int i;
            int j;
            for (i = 0, j = 0; i < win32Str.Length && j < values.Length; i += 2, j++)
            {
                if (win32Str[i] < '1' || win32Str[i] > '9')
                    return new int[]{3};
                values[j] = (int)(win32Str[i] - '0');
            }

            return (values);
        }

        private const uint LOCALE_NOUSEROVERRIDE = 0x80000000;
        private const uint LOCALE_RETURN_NUMBER = 0x20000000;
        private const uint LOCALE_RETURN_GENITIVE_NAMES = 0x10000000;
        private const uint LOCALE_SLOCALIZEDDISPLAYNAME = 0x00000002;
        private const uint LOCALE_SENGLISHDISPLAYNAME = 0x00000072;
        private const uint LOCALE_SNATIVEDISPLAYNAME = 0x00000073;
        private const uint LOCALE_SLOCALIZEDLANGUAGENAME = 0x0000006f;
        private const uint LOCALE_SENGLISHLANGUAGENAME = 0x00001001;
        private const uint LOCALE_SNATIVELANGUAGENAME = 0x00000004;
        private const uint LOCALE_SLOCALIZEDCOUNTRYNAME = 0x00000006;
        private const uint LOCALE_SENGLISHCOUNTRYNAME = 0x00001002;
        private const uint LOCALE_SNATIVECOUNTRYNAME = 0x00000008;
        private const uint LOCALE_SABBREVLANGNAME = 0x00000003;
        private const uint LOCALE_ICOUNTRY = 0x00000005;
        private const uint LOCALE_SABBREVCTRYNAME = 0x00000007;
        private const uint LOCALE_IGEOID = 0x0000005B;
        private const uint LOCALE_IDEFAULTLANGUAGE = 0x00000009;
        private const uint LOCALE_IDEFAULTCOUNTRY = 0x0000000A;
        private const uint LOCALE_IDEFAULTCODEPAGE = 0x0000000B;
        private const uint LOCALE_IDEFAULTANSICODEPAGE = 0x00001004;
        private const uint LOCALE_IDEFAULTMACCODEPAGE = 0x00001011;
        private const uint LOCALE_SLIST = 0x0000000C;
        private const uint LOCALE_IMEASURE = 0x0000000D;
        private const uint LOCALE_SDECIMAL = 0x0000000E;
        private const uint LOCALE_STHOUSAND = 0x0000000F;
        private const uint LOCALE_SGROUPING = 0x00000010;
        private const uint LOCALE_IDIGITS = 0x00000011;
        private const uint LOCALE_ILZERO = 0x00000012;
        private const uint LOCALE_INEGNUMBER = 0x00001010;
        private const uint LOCALE_SNATIVEDIGITS = 0x00000013;
        private const uint LOCALE_SCURRENCY = 0x00000014;
        private const uint LOCALE_SINTLSYMBOL = 0x00000015;
        private const uint LOCALE_SMONDECIMALSEP = 0x00000016;
        private const uint LOCALE_SMONTHOUSANDSEP = 0x00000017;
        private const uint LOCALE_SMONGROUPING = 0x00000018;
        private const uint LOCALE_ICURRDIGITS = 0x00000019;
        private const uint LOCALE_IINTLCURRDIGITS = 0x0000001A;
        private const uint LOCALE_ICURRENCY = 0x0000001B;
        private const uint LOCALE_INEGCURR = 0x0000001C;
        private const uint LOCALE_SDATE = 0x0000001D;
        private const uint LOCALE_STIME = 0x0000001E;
        private const uint LOCALE_SSHORTDATE = 0x0000001F;
        private const uint LOCALE_SLONGDATE = 0x00000020;
        private const uint LOCALE_STIMEFORMAT = 0x00001003;
        private const uint LOCALE_IDATE = 0x00000021;
        private const uint LOCALE_ILDATE = 0x00000022;
        private const uint LOCALE_ITIME = 0x00000023;
        private const uint LOCALE_ITIMEMARKPOSN = 0x00001005;
        private const uint LOCALE_ICENTURY = 0x00000024;
        private const uint LOCALE_ITLZERO = 0x00000025;
        private const uint LOCALE_IDAYLZERO = 0x00000026;
        private const uint LOCALE_IMONLZERO = 0x00000027;
        private const uint LOCALE_S1159 = 0x00000028;
        private const uint LOCALE_S2359 = 0x00000029;
        private const uint LOCALE_ICALENDARTYPE = 0x00001009;
        private const uint LOCALE_IOPTIONALCALENDAR = 0x0000100B;
        private const uint LOCALE_IFIRSTDAYOFWEEK = 0x0000100C;
        private const uint LOCALE_IFIRSTWEEKOFYEAR = 0x0000100D;
        private const uint LOCALE_SDAYNAME1 = 0x0000002A;
        private const uint LOCALE_SDAYNAME2 = 0x0000002B;
        private const uint LOCALE_SDAYNAME3 = 0x0000002C;
        private const uint LOCALE_SDAYNAME4 = 0x0000002D;
        private const uint LOCALE_SDAYNAME5 = 0x0000002E;
        private const uint LOCALE_SDAYNAME6 = 0x0000002F;
        private const uint LOCALE_SDAYNAME7 = 0x00000030;
        private const uint LOCALE_SABBREVDAYNAME1 = 0x00000031;
        private const uint LOCALE_SABBREVDAYNAME2 = 0x00000032;
        private const uint LOCALE_SABBREVDAYNAME3 = 0x00000033;
        private const uint LOCALE_SABBREVDAYNAME4 = 0x00000034;
        private const uint LOCALE_SABBREVDAYNAME5 = 0x00000035;
        private const uint LOCALE_SABBREVDAYNAME6 = 0x00000036;
        private const uint LOCALE_SABBREVDAYNAME7 = 0x00000037;
        private const uint LOCALE_SMONTHNAME1 = 0x00000038;
        private const uint LOCALE_SMONTHNAME2 = 0x00000039;
        private const uint LOCALE_SMONTHNAME3 = 0x0000003A;
        private const uint LOCALE_SMONTHNAME4 = 0x0000003B;
        private const uint LOCALE_SMONTHNAME5 = 0x0000003C;
        private const uint LOCALE_SMONTHNAME6 = 0x0000003D;
        private const uint LOCALE_SMONTHNAME7 = 0x0000003E;
        private const uint LOCALE_SMONTHNAME8 = 0x0000003F;
        private const uint LOCALE_SMONTHNAME9 = 0x00000040;
        private const uint LOCALE_SMONTHNAME10 = 0x00000041;
        private const uint LOCALE_SMONTHNAME11 = 0x00000042;
        private const uint LOCALE_SMONTHNAME12 = 0x00000043;
        private const uint LOCALE_SMONTHNAME13 = 0x0000100E;
        private const uint LOCALE_SABBREVMONTHNAME1 = 0x00000044;
        private const uint LOCALE_SABBREVMONTHNAME2 = 0x00000045;
        private const uint LOCALE_SABBREVMONTHNAME3 = 0x00000046;
        private const uint LOCALE_SABBREVMONTHNAME4 = 0x00000047;
        private const uint LOCALE_SABBREVMONTHNAME5 = 0x00000048;
        private const uint LOCALE_SABBREVMONTHNAME6 = 0x00000049;
        private const uint LOCALE_SABBREVMONTHNAME7 = 0x0000004A;
        private const uint LOCALE_SABBREVMONTHNAME8 = 0x0000004B;
        private const uint LOCALE_SABBREVMONTHNAME9 = 0x0000004C;
        private const uint LOCALE_SABBREVMONTHNAME10 = 0x0000004D;
        private const uint LOCALE_SABBREVMONTHNAME11 = 0x0000004E;
        private const uint LOCALE_SABBREVMONTHNAME12 = 0x0000004F;
        private const uint LOCALE_SABBREVMONTHNAME13 = 0x0000100F;
        private const uint LOCALE_SPOSITIVESIGN = 0x00000050;
        private const uint LOCALE_SNEGATIVESIGN = 0x00000051;
        private const uint LOCALE_IPOSSIGNPOSN = 0x00000052;
        private const uint LOCALE_INEGSIGNPOSN = 0x00000053;
        private const uint LOCALE_IPOSSYMPRECEDES = 0x00000054;
        private const uint LOCALE_IPOSSEPBYSPACE = 0x00000055;
        private const uint LOCALE_INEGSYMPRECEDES = 0x00000056;
        private const uint LOCALE_INEGSEPBYSPACE = 0x00000057;
        private const uint LOCALE_FONTSIGNATURE = 0x00000058;
        private const uint LOCALE_SISO639LANGNAME = 0x00000059;
        private const uint LOCALE_SISO3166CTRYNAME = 0x0000005A;
        private const uint LOCALE_IDEFAULTEBCDICCODEPAGE = 0x00001012;
        private const uint LOCALE_IPAPERSIZE = 0x0000100A;
        private const uint LOCALE_SENGCURRNAME = 0x00001007;
        private const uint LOCALE_SNATIVECURRNAME = 0x00001008;
        private const uint LOCALE_SYEARMONTH = 0x00001006;
        private const uint LOCALE_SSORTNAME = 0x00001013;
        private const uint LOCALE_IDIGITSUBSTITUTION = 0x00001014;
        private const uint LOCALE_SNAME = 0x0000005c;
        private const uint LOCALE_SDURATION = 0x0000005d;
        private const uint LOCALE_SKEYBOARDSTOINSTALL = 0x0000005e;
        private const uint LOCALE_SSHORTESTDAYNAME1 = 0x00000060;
        private const uint LOCALE_SSHORTESTDAYNAME2 = 0x00000061;
        private const uint LOCALE_SSHORTESTDAYNAME3 = 0x00000062;
        private const uint LOCALE_SSHORTESTDAYNAME4 = 0x00000063;
        private const uint LOCALE_SSHORTESTDAYNAME5 = 0x00000064;
        private const uint LOCALE_SSHORTESTDAYNAME6 = 0x00000065;
        private const uint LOCALE_SSHORTESTDAYNAME7 = 0x00000066;
        private const uint LOCALE_SISO639LANGNAME2 = 0x00000067;
        private const uint LOCALE_SISO3166CTRYNAME2 = 0x00000068;
        private const uint LOCALE_SNAN = 0x00000069;
        private const uint LOCALE_SPOSINFINITY = 0x0000006a;
        private const uint LOCALE_SNEGINFINITY = 0x0000006b;
        private const uint LOCALE_SSCRIPTS = 0x0000006c;
        private const uint LOCALE_SPARENT = 0x0000006d;
        private const uint LOCALE_SCONSOLEFALLBACKNAME = 0x0000006e;
        private const uint LOCALE_IREADINGLAYOUT = 0x00000070;
        private const uint LOCALE_INEUTRAL = 0x00000071;
        private const uint LOCALE_INEGATIVEPERCENT = 0x00000074;
        private const uint LOCALE_IPOSITIVEPERCENT = 0x00000075;
        private const uint LOCALE_SPERCENT = 0x00000076;
        private const uint LOCALE_SPERMILLE = 0x00000077;
        private const uint LOCALE_SMONTHDAY = 0x00000078;
        private const uint LOCALE_SSHORTTIME = 0x00000079;
        private const uint LOCALE_SOPENTYPELANGUAGETAG = 0x0000007a;
        private const uint LOCALE_SSORTLOCALE = 0x0000007b;
        internal const uint TIME_NOSECONDS = 0x00000002;
        internal static extern bool nativeInitCultureData(CultureData cultureData);
        internal static extern bool nativeGetNumberFormatInfoValues(String localeName, NumberFormatInfo nfi, bool useUserOverride);
        private static extern String[] nativeEnumTimeFormats(String localeName, uint dwFlags, bool useUserOverride);
        internal static extern int nativeEnumCultureNames(int cultureTypes, ObjectHandleOnStack retStringArray);
    }
}