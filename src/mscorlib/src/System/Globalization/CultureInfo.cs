using System.Collections;
using System.Diagnostics.Contracts;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;

namespace System.Globalization
{
    public class CultureInfo : ICloneable, IFormatProvider
    {
        internal bool m_isReadOnly;
        internal CompareInfo compareInfo;
        internal TextInfo textInfo;
        internal NumberFormatInfo numInfo;
        internal DateTimeFormatInfo dateTimeInfo;
        internal Calendar calendar;
        internal CultureData m_cultureData;
        internal bool m_isInherited;
        internal string m_name;
        private string m_nonSortName;
        private string m_sortName;
        private static volatile CultureInfo s_userDefaultCulture;
        private static volatile CultureInfo s_InvariantCultureInfo;
        private static volatile CultureInfo s_userDefaultUICulture;
        private static volatile CultureInfo s_InstalledUICultureInfo;
        private static volatile CultureInfo s_DefaultThreadCurrentUICulture;
        private static volatile CultureInfo s_DefaultThreadCurrentCulture;
        private static volatile Hashtable s_LcidCachedCultures;
        private static volatile Hashtable s_NameCachedCultures;
        private static volatile WindowsRuntimeResourceManagerBase s_WindowsRuntimeResourceManager;
        private static bool ts_IsDoingAppXCultureInfoLookup;
        private CultureInfo m_parent;
        internal const int LOCALE_NEUTRAL = 0x0000;
        private const int LOCALE_USER_DEFAULT = 0x0400;
        private const int LOCALE_SYSTEM_DEFAULT = 0x0800;
        internal const int LOCALE_CUSTOM_DEFAULT = 0x0c00;
        internal const int LOCALE_CUSTOM_UNSPECIFIED = 0x1000;
        internal const int LOCALE_INVARIANT = 0x007F;
        private const int LOCALE_TRADITIONAL_SPANISH = 0x040a;
        private static readonly bool init = Init();
        private static bool Init()
        {
            if (s_InvariantCultureInfo == null)
            {
                CultureInfo temp = new CultureInfo("", false);
                temp.m_isReadOnly = true;
                s_InvariantCultureInfo = temp;
            }

            s_userDefaultCulture = s_userDefaultUICulture = s_InvariantCultureInfo;
            s_userDefaultCulture = InitUserDefaultCulture();
            s_userDefaultUICulture = InitUserDefaultUICulture();
            return true;
        }

        static CultureInfo InitUserDefaultCulture()
        {
            String strDefault = GetDefaultLocaleName(LOCALE_USER_DEFAULT);
            if (strDefault == null)
            {
                strDefault = GetDefaultLocaleName(LOCALE_SYSTEM_DEFAULT);
                if (strDefault == null)
                {
                    return (CultureInfo.InvariantCulture);
                }
            }

            CultureInfo temp = GetCultureByName(strDefault, true);
            temp.m_isReadOnly = true;
            return (temp);
        }

        static CultureInfo InitUserDefaultUICulture()
        {
            String strDefault = GetUserDefaultUILanguage();
            if (strDefault == UserDefaultCulture.Name)
            {
                return (UserDefaultCulture);
            }

            CultureInfo temp = GetCultureByName(strDefault, true);
            if (temp == null)
            {
                return (CultureInfo.InvariantCulture);
            }

            temp.m_isReadOnly = true;
            return (temp);
        }

        internal static CultureInfo GetCultureInfoForUserPreferredLanguageInAppX()
        {
            if (ts_IsDoingAppXCultureInfoLookup)
            {
                return null;
            }

            if (AppDomain.IsAppXNGen)
            {
                return null;
            }

            CultureInfo toReturn = null;
            try
            {
                ts_IsDoingAppXCultureInfoLookup = true;
                if (s_WindowsRuntimeResourceManager == null)
                {
                    s_WindowsRuntimeResourceManager = ResourceManager.GetWinRTResourceManager();
                }

                toReturn = s_WindowsRuntimeResourceManager.GlobalResourceContextBestFitCultureInfo;
            }
            finally
            {
                ts_IsDoingAppXCultureInfoLookup = false;
            }

            return toReturn;
        }

        internal static bool SetCultureInfoForUserPreferredLanguageInAppX(CultureInfo ci)
        {
            if (AppDomain.IsAppXNGen)
            {
                return false;
            }

            if (s_WindowsRuntimeResourceManager == null)
            {
                s_WindowsRuntimeResourceManager = ResourceManager.GetWinRTResourceManager();
            }

            return s_WindowsRuntimeResourceManager.SetGlobalResourceContextDefaultCulture(ci);
        }

        public CultureInfo(String name): this (name, true)
        {
        }

        public CultureInfo(String name, bool useUserOverride)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_String"));
            }

            Contract.EndContractBlock();
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && (name == "bn" || name == "bn-BD" || name == "bn-IN" || name == "ml" || name == "or"))
                throw new ArgumentException(Environment.GetResourceString("Argument_CultureNotSupported"));
            this.m_cultureData = CultureData.GetCultureData(name, useUserOverride);
            if (this.m_cultureData == null)
            {
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    throw new PlatformNotSupportedException(Environment.GetResourceString("Argument_CultureNotSupported"));
                throw new CultureNotFoundException("name", name, Environment.GetResourceString("Argument_CultureNotSupported"));
            }

            this.m_name = this.m_cultureData.CultureName;
            this.m_isInherited = (this.GetType() != typeof (System.Globalization.CultureInfo));
        }

        internal static void CheckDomainSafetyObject(Object obj, Object container)
        {
            if (obj.GetType().Assembly != typeof (System.Globalization.CultureInfo).Assembly)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidOperation_SubclassedObject"), obj.GetType(), container.GetType()));
            }

            Contract.EndContractBlock();
        }

        private bool m_useUserOverride;
        private void OnDeserialized(StreamingContext ctx)
        {
            Contract.Assert(m_name != null, "[CultureInfo.OnDeserialized] m_name != null");
            this.m_cultureData = CultureData.GetCultureData(m_name, m_useUserOverride);
            if (this.m_cultureData == null)
                throw new CultureNotFoundException("m_name", m_name, Environment.GetResourceString("Argument_CultureNotSupported"));
            m_isInherited = (this.GetType() != typeof (System.Globalization.CultureInfo));
            if (this.GetType().Assembly == typeof (System.Globalization.CultureInfo).Assembly)
            {
                if (textInfo != null)
                {
                    CheckDomainSafetyObject(textInfo, this);
                }

                if (compareInfo != null)
                {
                    CheckDomainSafetyObject(compareInfo, this);
                }
            }
        }

        private void OnSerializing(StreamingContext ctx)
        {
            this.m_name = this.m_cultureData.CultureName;
            this.m_useUserOverride = this.m_cultureData.UseUserOverride;
        }

        internal bool CanSendCrossDomain()
        {
            bool isSafe = false;
            if (this.GetType() == typeof (System.Globalization.CultureInfo))
            {
                isSafe = true;
            }

            return isSafe;
        }

        internal CultureInfo(String cultureName, String textAndCompareCultureName)
        {
            if (cultureName == null)
            {
                throw new ArgumentNullException("cultureName", Environment.GetResourceString("ArgumentNull_String"));
            }

            Contract.EndContractBlock();
            this.m_cultureData = CultureData.GetCultureData(cultureName, false);
            if (this.m_cultureData == null)
                throw new CultureNotFoundException("cultureName", cultureName, Environment.GetResourceString("Argument_CultureNotSupported"));
            this.m_name = this.m_cultureData.CultureName;
            CultureInfo altCulture = GetCultureInfo(textAndCompareCultureName);
            this.compareInfo = altCulture.CompareInfo;
            this.textInfo = altCulture.TextInfo;
        }

        private static CultureInfo GetCultureByName(String name, bool userOverride)
        {
            try
            {
                return userOverride ? new CultureInfo(name) : CultureInfo.GetCultureInfo(name);
            }
            catch (ArgumentException)
            {
            }

            return null;
        }

        internal static bool VerifyCultureName(String cultureName, bool throwException)
        {
            for (int i = 0; i < cultureName.Length; i++)
            {
                char c = cultureName[i];
                if (Char.IsLetterOrDigit(c) || c == '-' || c == '_')
                {
                    continue;
                }

                if (throwException)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidResourceCultureName", cultureName));
                }

                return false;
            }

            return true;
        }

        internal static bool VerifyCultureName(CultureInfo culture, bool throwException)
        {
            Contract.Assert(culture != null, "[CultureInfo.VerifyCultureName]culture!=null");
            if (!culture.m_isInherited)
            {
                return true;
            }

            return VerifyCultureName(culture.Name, throwException);
        }

        public static CultureInfo CurrentCulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                if (AppDomain.IsAppXModel())
                {
                    CultureInfo culture = GetCultureInfoForUserPreferredLanguageInAppX();
                    if (culture != null)
                        return culture;
                }

                return Thread.m_CurrentCulture ?? s_DefaultThreadCurrentCulture ?? s_userDefaultCulture ?? UserDefaultCulture;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (AppDomain.IsAppXModel())
                {
                    if (SetCultureInfoForUserPreferredLanguageInAppX(value))
                    {
                        return;
                    }
                }

                Thread.CurrentThread.CurrentCulture = value;
            }
        }

        internal static CultureInfo UserDefaultCulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                CultureInfo temp = s_userDefaultCulture;
                if (temp == null)
                {
                    s_userDefaultCulture = CultureInfo.InvariantCulture;
                    temp = InitUserDefaultCulture();
                    s_userDefaultCulture = temp;
                }

                return (temp);
            }
        }

        internal static CultureInfo UserDefaultUICulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                CultureInfo temp = s_userDefaultUICulture;
                if (temp == null)
                {
                    s_userDefaultUICulture = CultureInfo.InvariantCulture;
                    temp = InitUserDefaultUICulture();
                    s_userDefaultUICulture = temp;
                }

                return (temp);
            }
        }

        public static CultureInfo CurrentUICulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                if (AppDomain.IsAppXModel())
                {
                    CultureInfo culture = GetCultureInfoForUserPreferredLanguageInAppX();
                    if (culture != null)
                        return culture;
                }

                return Thread.m_CurrentUICulture ?? s_DefaultThreadCurrentUICulture ?? s_userDefaultUICulture ?? UserDefaultUICulture;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (AppDomain.IsAppXModel())
                {
                    if (SetCultureInfoForUserPreferredLanguageInAppX(value))
                    {
                        return;
                    }
                }

                Thread.CurrentThread.CurrentUICulture = value;
            }
        }

        public static CultureInfo InstalledUICulture
        {
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                CultureInfo temp = s_InstalledUICultureInfo;
                if (temp == null)
                {
                    String strDefault = GetSystemDefaultUILanguage();
                    temp = GetCultureByName(strDefault, true);
                    if (temp == null)
                    {
                        temp = InvariantCulture;
                    }

                    temp.m_isReadOnly = true;
                    s_InstalledUICultureInfo = temp;
                }

                return (temp);
            }
        }

        public static CultureInfo DefaultThreadCurrentCulture
        {
            get
            {
                return s_DefaultThreadCurrentCulture;
            }

            [System.Security.SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Demand, ControlThread = true)]
            set
            {
                s_DefaultThreadCurrentCulture = value;
            }
        }

        public static CultureInfo DefaultThreadCurrentUICulture
        {
            get
            {
                return s_DefaultThreadCurrentUICulture;
            }

            [System.Security.SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Demand, ControlThread = true)]
            set
            {
                if (value != null)
                {
                    CultureInfo.VerifyCultureName(value, true);
                }

                s_DefaultThreadCurrentUICulture = value;
            }
        }

        internal static void SetCurrentUICultureQuirk(CultureInfo value)
        {
            s_DefaultThreadCurrentUICulture = value;
        }

        internal static void SetCurrentCultureQuirk(CultureInfo value)
        {
            s_DefaultThreadCurrentCulture = value;
        }

        public static CultureInfo InvariantCulture
        {
            [Pure]
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                return (s_InvariantCultureInfo);
            }
        }

        public virtual CultureInfo Parent
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Contract.Ensures(Contract.Result<CultureInfo>() != null);
                if (null == m_parent)
                {
                    try
                    {
                        string parentName = this.m_cultureData.SPARENT;
                        if (String.IsNullOrEmpty(parentName))
                        {
                            m_parent = InvariantCulture;
                        }
                        else
                        {
                            m_parent = new CultureInfo(parentName, this.m_cultureData.UseUserOverride);
                        }
                    }
                    catch (ArgumentException)
                    {
                        m_parent = InvariantCulture;
                    }
                }

                return m_parent;
            }
        }

        public virtual String Name
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                if (this.m_nonSortName == null)
                {
                    this.m_nonSortName = this.m_cultureData.SNAME;
                    if (this.m_nonSortName == null)
                    {
                        this.m_nonSortName = String.Empty;
                    }
                }

                return this.m_nonSortName;
            }
        }

        internal String SortName
        {
            get
            {
                if (this.m_sortName == null)
                {
                    this.m_sortName = this.m_cultureData.SCOMPAREINFO;
                }

                return this.m_sortName;
            }
        }

        public virtual String DisplayName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                Contract.Assert(m_name != null, "[CultureInfo.DisplayName]Always expect m_name to be set");
                return m_cultureData.SLOCALIZEDDISPLAYNAME;
            }
        }

        public virtual String NativeName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SNATIVEDISPLAYNAME);
            }
        }

        public virtual String EnglishName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SENGDISPLAYNAME);
            }
        }

        public virtual String TwoLetterISOLanguageName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return (this.m_cultureData.SISO639LANGNAME);
            }
        }

        public virtual CompareInfo CompareInfo
        {
            get
            {
                Contract.Ensures(Contract.Result<CompareInfo>() != null);
                if (this.compareInfo == null)
                {
                    CompareInfo temp = UseUserOverride ? GetCultureInfo(this.m_name).CompareInfo : new CompareInfo(this);
                    if (CompatibilitySwitches.IsCompatibilityBehaviorDefined)
                    {
                        this.compareInfo = temp;
                    }
                    else
                    {
                        return temp;
                    }
                }

                return (compareInfo);
            }
        }

        public virtual TextInfo TextInfo
        {
            get
            {
                Contract.Ensures(Contract.Result<TextInfo>() != null);
                if (textInfo == null)
                {
                    TextInfo tempTextInfo = new TextInfo(this.m_cultureData);
                    tempTextInfo.SetReadOnlyState(m_isReadOnly);
                    if (CompatibilitySwitches.IsCompatibilityBehaviorDefined)
                    {
                        textInfo = tempTextInfo;
                    }
                    else
                    {
                        return tempTextInfo;
                    }
                }

                return (textInfo);
            }
        }

        public override bool Equals(Object value)
        {
            if (Object.ReferenceEquals(this, value))
                return true;
            CultureInfo that = value as CultureInfo;
            if (that != null)
            {
                return (this.Name.Equals(that.Name) && this.CompareInfo.Equals(that.CompareInfo));
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return (this.Name.GetHashCode() + this.CompareInfo.GetHashCode());
        }

        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Assert(m_name != null, "[CultureInfo.ToString]Always expect m_name to be set");
            return m_name;
        }

        public virtual Object GetFormat(Type formatType)
        {
            if (formatType == typeof (NumberFormatInfo))
            {
                return (NumberFormat);
            }

            if (formatType == typeof (DateTimeFormatInfo))
            {
                return (DateTimeFormat);
            }

            return (null);
        }

        public virtual bool IsNeutralCulture
        {
            get
            {
                return this.m_cultureData.IsNeutralCulture;
            }
        }

        public virtual NumberFormatInfo NumberFormat
        {
            get
            {
                Contract.Ensures(Contract.Result<NumberFormatInfo>() != null);
                if (numInfo == null)
                {
                    NumberFormatInfo temp = new NumberFormatInfo(this.m_cultureData);
                    temp.isReadOnly = m_isReadOnly;
                    numInfo = temp;
                }

                return (numInfo);
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Obj"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                numInfo = value;
            }
        }

        public virtual DateTimeFormatInfo DateTimeFormat
        {
            get
            {
                Contract.Ensures(Contract.Result<DateTimeFormatInfo>() != null);
                if (dateTimeInfo == null)
                {
                    DateTimeFormatInfo temp = new DateTimeFormatInfo(this.m_cultureData, this.Calendar);
                    temp.m_isReadOnly = m_isReadOnly;
                    System.Threading.Thread.MemoryBarrier();
                    dateTimeInfo = temp;
                }

                return (dateTimeInfo);
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Obj"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                dateTimeInfo = value;
            }
        }

        public void ClearCachedData()
        {
            s_userDefaultUICulture = null;
            s_userDefaultCulture = null;
            RegionInfo.s_currentRegionInfo = null;
            TimeZoneInfo.ClearCachedData();
            s_LcidCachedCultures = null;
            s_NameCachedCultures = null;
            CultureData.ClearCachedData();
        }

        internal static Calendar GetCalendarInstance(int calType)
        {
            if (calType == Calendar.CAL_GREGORIAN)
            {
                return (new GregorianCalendar());
            }

            return GetCalendarInstanceRare(calType);
        }

        internal static Calendar GetCalendarInstanceRare(int calType)
        {
            Contract.Assert(calType != Calendar.CAL_GREGORIAN, "calType!=Calendar.CAL_GREGORIAN");
            switch (calType)
            {
                case Calendar.CAL_GREGORIAN_US:
                case Calendar.CAL_GREGORIAN_ME_FRENCH:
                case Calendar.CAL_GREGORIAN_ARABIC:
                case Calendar.CAL_GREGORIAN_XLIT_ENGLISH:
                case Calendar.CAL_GREGORIAN_XLIT_FRENCH:
                    return (new GregorianCalendar((GregorianCalendarTypes)calType));
                case Calendar.CAL_TAIWAN:
                    return (new TaiwanCalendar());
                case Calendar.CAL_JAPAN:
                    return (new JapaneseCalendar());
                case Calendar.CAL_KOREA:
                    return (new KoreanCalendar());
                case Calendar.CAL_THAI:
                    return (new ThaiBuddhistCalendar());
                case Calendar.CAL_HIJRI:
                    return (new HijriCalendar());
                case Calendar.CAL_HEBREW:
                    return (new HebrewCalendar());
                case Calendar.CAL_UMALQURA:
                    return (new UmAlQuraCalendar());
                case Calendar.CAL_PERSIAN:
                    return (new PersianCalendar());
                case Calendar.CAL_CHINESELUNISOLAR:
                    return (new ChineseLunisolarCalendar());
                case Calendar.CAL_JAPANESELUNISOLAR:
                    return (new JapaneseLunisolarCalendar());
                case Calendar.CAL_KOREANLUNISOLAR:
                    return (new KoreanLunisolarCalendar());
                case Calendar.CAL_TAIWANLUNISOLAR:
                    return (new TaiwanLunisolarCalendar());
            }

            return (new GregorianCalendar());
        }

        public virtual Calendar Calendar
        {
            get
            {
                Contract.Ensures(Contract.Result<Calendar>() != null);
                if (calendar == null)
                {
                    Contract.Assert(this.m_cultureData.CalendarIds.Length > 0, "this.m_cultureData.CalendarIds.Length > 0");
                    Calendar newObj = this.m_cultureData.DefaultCalendar;
                    System.Threading.Thread.MemoryBarrier();
                    newObj.SetReadOnlyState(m_isReadOnly);
                    calendar = newObj;
                }

                return (calendar);
            }
        }

        public virtual Calendar[] OptionalCalendars
        {
            get
            {
                Contract.Ensures(Contract.Result<Calendar[]>() != null);
                int[] calID = this.m_cultureData.CalendarIds;
                Calendar[] cals = new Calendar[calID.Length];
                for (int i = 0; i < cals.Length; i++)
                {
                    cals[i] = GetCalendarInstance(calID[i]);
                }

                return (cals);
            }
        }

        public bool UseUserOverride
        {
            get
            {
                return (this.m_cultureData.UseUserOverride);
            }
        }

        public virtual Object Clone()
        {
            Contract.Ensures(Contract.Result<Object>() != null);
            CultureInfo ci = (CultureInfo)MemberwiseClone();
            ci.m_isReadOnly = false;
            if (!m_isInherited)
            {
                if (this.dateTimeInfo != null)
                {
                    ci.dateTimeInfo = (DateTimeFormatInfo)this.dateTimeInfo.Clone();
                }

                if (this.numInfo != null)
                {
                    ci.numInfo = (NumberFormatInfo)this.numInfo.Clone();
                }
            }
            else
            {
                ci.DateTimeFormat = (DateTimeFormatInfo)this.DateTimeFormat.Clone();
                ci.NumberFormat = (NumberFormatInfo)this.NumberFormat.Clone();
            }

            if (textInfo != null)
            {
                ci.textInfo = (TextInfo)textInfo.Clone();
            }

            if (calendar != null)
            {
                ci.calendar = (Calendar)calendar.Clone();
            }

            return (ci);
        }

        public static CultureInfo ReadOnly(CultureInfo ci)
        {
            if (ci == null)
            {
                throw new ArgumentNullException("ci");
            }

            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();
            if (ci.IsReadOnly)
            {
                return (ci);
            }

            CultureInfo newInfo = (CultureInfo)(ci.MemberwiseClone());
            if (!ci.IsNeutralCulture)
            {
                if (!ci.m_isInherited)
                {
                    if (ci.dateTimeInfo != null)
                    {
                        newInfo.dateTimeInfo = DateTimeFormatInfo.ReadOnly(ci.dateTimeInfo);
                    }

                    if (ci.numInfo != null)
                    {
                        newInfo.numInfo = NumberFormatInfo.ReadOnly(ci.numInfo);
                    }
                }
                else
                {
                    newInfo.DateTimeFormat = DateTimeFormatInfo.ReadOnly(ci.DateTimeFormat);
                    newInfo.NumberFormat = NumberFormatInfo.ReadOnly(ci.NumberFormat);
                }
            }

            if (ci.textInfo != null)
            {
                newInfo.textInfo = TextInfo.ReadOnly(ci.textInfo);
            }

            if (ci.calendar != null)
            {
                newInfo.calendar = Calendar.ReadOnly(ci.calendar);
            }

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

        private void VerifyWritable()
        {
            if (m_isReadOnly)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }

            Contract.EndContractBlock();
        }

        internal bool HasInvariantCultureName
        {
            get
            {
                return Name == CultureInfo.InvariantCulture.Name;
            }
        }

        internal static CultureInfo GetCultureInfoHelper(int lcid, string name, string altName)
        {
            CultureInfo retval;
            Hashtable tempNameHT = s_NameCachedCultures;
            if (name != null)
            {
                name = CultureData.AnsiToLower(name);
            }

            if (altName != null)
            {
                altName = CultureData.AnsiToLower(altName);
            }

            if (tempNameHT == null)
            {
                tempNameHT = Hashtable.Synchronized(new Hashtable());
            }
            else
            {
                if (lcid == -1)
                {
                    retval = (CultureInfo)tempNameHT[name + '\xfffd' + altName];
                    if (retval != null)
                    {
                        return retval;
                    }
                }
                else if (lcid == 0)
                {
                    retval = (CultureInfo)tempNameHT[name];
                    if (retval != null)
                    {
                        return retval;
                    }
                }
            }

            try
            {
                switch (lcid)
                {
                    case -1:
                        retval = new CultureInfo(name, altName);
                        break;
                    case 0:
                        retval = new CultureInfo(name, false);
                        break;
                    default:
                        return null;
                }
            }
            catch (ArgumentException)
            {
                return null;
            }

            retval.m_isReadOnly = true;
            if (lcid == -1)
            {
                tempNameHT[name + '\xfffd' + altName] = retval;
                retval.TextInfo.SetReadOnlyState(true);
            }
            else
            {
                string newName = CultureData.AnsiToLower(retval.m_name);
                tempNameHT[newName] = retval;
            }

            s_NameCachedCultures = tempNameHT;
            return retval;
        }

        public static CultureInfo GetCultureInfo(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();
            CultureInfo retval = GetCultureInfoHelper(0, name, null);
            if (retval == null)
            {
                throw new CultureNotFoundException("name", name, Environment.GetResourceString("Argument_CultureNotSupported"));
            }

            return retval;
        }

        public static CultureInfo GetCultureInfo(string name, string altName)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (null == altName)
            {
                throw new ArgumentNullException("altName");
            }

            Contract.Ensures(Contract.Result<CultureInfo>() != null);
            Contract.EndContractBlock();
            CultureInfo retval = GetCultureInfoHelper(-1, name, altName);
            if (retval == null)
            {
                throw new CultureNotFoundException("name or altName", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_OneOfCulturesNotSupported"), name, altName));
            }

            return retval;
        }

        private static volatile bool s_isTaiwanSku;
        private static volatile bool s_haveIsTaiwanSku;
        internal static bool IsTaiwanSku
        {
            get
            {
                if (!s_haveIsTaiwanSku)
                {
                    s_isTaiwanSku = (GetSystemDefaultUILanguage() == "zh-TW");
                    s_haveIsTaiwanSku = true;
                }

                return (bool)s_isTaiwanSku;
            }
        }

        internal static extern String nativeGetLocaleInfoEx(String localeName, uint field);
        internal static extern int nativeGetLocaleInfoExInt(String localeName, uint field);
        internal static extern bool nativeSetThreadLocale(String localeName);
        private static String GetDefaultLocaleName(int localeType)
        {
            Contract.Assert(localeType == LOCALE_USER_DEFAULT || localeType == LOCALE_SYSTEM_DEFAULT, "[CultureInfo.GetDefaultLocaleName] localeType must be LOCALE_USER_DEFAULT or LOCALE_SYSTEM_DEFAULT");
            string localeName = null;
            if (InternalGetDefaultLocaleName(localeType, JitHelpers.GetStringHandleOnStack(ref localeName)))
            {
                return localeName;
            }

            return string.Empty;
        }

        private static extern bool InternalGetDefaultLocaleName(int localetype, StringHandleOnStack localeString);
        private static String GetUserDefaultUILanguage()
        {
            string userDefaultUiLanguage = null;
            if (InternalGetUserDefaultUILanguage(JitHelpers.GetStringHandleOnStack(ref userDefaultUiLanguage)))
            {
                return userDefaultUiLanguage;
            }

            return String.Empty;
        }

        private static extern bool InternalGetUserDefaultUILanguage(StringHandleOnStack userDefaultUiLanguage);
        private static String GetSystemDefaultUILanguage()
        {
            string systemDefaultUiLanguage = null;
            if (InternalGetSystemDefaultUILanguage(JitHelpers.GetStringHandleOnStack(ref systemDefaultUiLanguage)))
            {
                return systemDefaultUiLanguage;
            }

            return String.Empty;
        }

        private static extern bool InternalGetSystemDefaultUILanguage(StringHandleOnStack systemDefaultUiLanguage);
        internal static extern String[] nativeGetResourceFallbackArray();
    }
}