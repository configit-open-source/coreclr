using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;

using Microsoft.Win32;

namespace System
{
    [Flags]
    internal enum TimeZoneInfoOptions
    {
        None = 1,
        NoThrowOnInvalidTime = 2
    }

    ;
    sealed public class TimeZoneInfo : IEquatable<TimeZoneInfo>, ISerializable, IDeserializationCallback
    {
        private String m_id;
        private String m_displayName;
        private String m_standardDisplayName;
        private String m_daylightDisplayName;
        private TimeSpan m_baseUtcOffset;
        private Boolean m_supportsDaylightSavingTime;
        private AdjustmentRule[] m_adjustmentRules;
        private enum TimeZoneInfoResult
        {
            Success = 0,
            TimeZoneNotFoundException = 1,
            InvalidTimeZoneException = 2,
            SecurityException = 3
        }

        ;
        private const string c_timeZonesRegistryHive = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Time Zones";
        private const string c_timeZonesRegistryHivePermissionList = @"HKEY_LOCAL_MACHINE\" + c_timeZonesRegistryHive;
        private const string c_displayValue = "Display";
        private const string c_daylightValue = "Dlt";
        private const string c_standardValue = "Std";
        private const string c_muiDisplayValue = "MUI_Display";
        private const string c_muiDaylightValue = "MUI_Dlt";
        private const string c_muiStandardValue = "MUI_Std";
        private const string c_timeZoneInfoValue = "TZI";
        private const string c_firstEntryValue = "FirstEntry";
        private const string c_lastEntryValue = "LastEntry";
        private const string c_utcId = "UTC";
        private const string c_localId = "Local";
        private const int c_maxKeyLength = 255;
        private const int c_regByteLength = 44;
        private const long c_ticksPerMillisecond = 10000;
        private const long c_ticksPerSecond = c_ticksPerMillisecond * 1000;
        private const long c_ticksPerMinute = c_ticksPerSecond * 60;
        private const long c_ticksPerHour = c_ticksPerMinute * 60;
        private const long c_ticksPerDay = c_ticksPerHour * 24;
        private const long c_ticksPerDayRange = c_ticksPerDay - c_ticksPerMillisecond;
        class CachedData
        {
            private volatile TimeZoneInfo m_localTimeZone;
            private volatile TimeZoneInfo m_utcTimeZone;
            private TimeZoneInfo CreateLocal()
            {
                lock (this)
                {
                    TimeZoneInfo timeZone = m_localTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = TimeZoneInfo.GetLocalTimeZone(this);
                        timeZone = new TimeZoneInfo(timeZone.m_id, timeZone.m_baseUtcOffset, timeZone.m_displayName, timeZone.m_standardDisplayName, timeZone.m_daylightDisplayName, timeZone.m_adjustmentRules, false);
                        m_localTimeZone = timeZone;
                    }

                    return timeZone;
                }
            }

            public TimeZoneInfo Local
            {
                get
                {
                    TimeZoneInfo timeZone = m_localTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = CreateLocal();
                    }

                    return timeZone;
                }
            }

            private TimeZoneInfo CreateUtc()
            {
                lock (this)
                {
                    TimeZoneInfo timeZone = m_utcTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = CreateCustomTimeZone(c_utcId, TimeSpan.Zero, c_utcId, c_utcId);
                        m_utcTimeZone = timeZone;
                    }

                    return timeZone;
                }
            }

            public TimeZoneInfo Utc
            {
                get
                {
                                        TimeZoneInfo timeZone = m_utcTimeZone;
                    if (timeZone == null)
                    {
                        timeZone = CreateUtc();
                    }

                    return timeZone;
                }
            }

            public DateTimeKind GetCorrespondingKind(TimeZoneInfo timeZone)
            {
                DateTimeKind kind;
                if ((object)timeZone == (object)m_utcTimeZone)
                {
                    kind = DateTimeKind.Utc;
                }
                else if ((object)timeZone == (object)m_localTimeZone)
                {
                    kind = DateTimeKind.Local;
                }
                else
                {
                    kind = DateTimeKind.Unspecified;
                }

                return kind;
            }

            public Dictionary<string, TimeZoneInfo> m_systemTimeZones;
            public ReadOnlyCollection<TimeZoneInfo> m_readOnlySystemTimeZones;
            public bool m_allSystemTimeZonesRead;
            private static TimeZoneInfo GetCurrentOneYearLocal()
            {
                TimeZoneInfo match;
                Win32Native.TimeZoneInformation timeZoneInformation = new Win32Native.TimeZoneInformation();
                long result = UnsafeNativeMethods.GetTimeZoneInformation(out timeZoneInformation);
                if (result == Win32Native.TIME_ZONE_ID_INVALID)
                    match = CreateCustomTimeZone(c_localId, TimeSpan.Zero, c_localId, c_localId);
                else
                    match = GetLocalTimeZoneFromWin32Data(timeZoneInformation, false);
                return match;
            }

            private volatile OffsetAndRule m_oneYearLocalFromUtc;
            public OffsetAndRule GetOneYearLocalFromUtc(int year)
            {
                OffsetAndRule oneYearLocFromUtc = m_oneYearLocalFromUtc;
                if (oneYearLocFromUtc == null || oneYearLocFromUtc.year != year)
                {
                    TimeZoneInfo currentYear = GetCurrentOneYearLocal();
                    AdjustmentRule rule = currentYear.m_adjustmentRules == null ? null : currentYear.m_adjustmentRules[0];
                    oneYearLocFromUtc = new OffsetAndRule(year, currentYear.BaseUtcOffset, rule);
                    m_oneYearLocalFromUtc = oneYearLocFromUtc;
                }

                return oneYearLocFromUtc;
            }
        }

        ;
        static CachedData s_cachedData = new CachedData();
        private class OffsetAndRule
        {
            public int year;
            public TimeSpan offset;
            public AdjustmentRule rule;
            public OffsetAndRule(int year, TimeSpan offset, AdjustmentRule rule)
            {
                this.year = year;
                this.offset = offset;
                this.rule = rule;
            }
        }

        private static DateTime s_maxDateOnly = new DateTime(9999, 12, 31);
        private static DateTime s_minDateOnly = new DateTime(1, 1, 2);
        public String Id
        {
            get
            {
                return m_id;
            }
        }

        public String DisplayName
        {
            get
            {
                return (m_displayName == null ? String.Empty : m_displayName);
            }
        }

        public String StandardName
        {
            get
            {
                return (m_standardDisplayName == null ? String.Empty : m_standardDisplayName);
            }
        }

        public String DaylightName
        {
            get
            {
                return (m_daylightDisplayName == null ? String.Empty : m_daylightDisplayName);
            }
        }

        public TimeSpan BaseUtcOffset
        {
            get
            {
                return m_baseUtcOffset;
            }
        }

        public Boolean SupportsDaylightSavingTime
        {
            get
            {
                return m_supportsDaylightSavingTime;
            }
        }

        public AdjustmentRule[] GetAdjustmentRules()
        {
            if (m_adjustmentRules == null)
            {
                return new AdjustmentRule[0];
            }
            else
            {
                return (AdjustmentRule[])m_adjustmentRules.Clone();
            }
        }

        public TimeSpan[] GetAmbiguousTimeOffsets(DateTimeOffset dateTimeOffset)
        {
            if (!SupportsDaylightSavingTime)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeOffsetIsNotAmbiguous"), "dateTimeOffset");
            }

                        DateTime adjustedTime = (TimeZoneInfo.ConvertTime(dateTimeOffset, this)).DateTime;
            Boolean isAmbiguous = false;
            AdjustmentRule rule = GetAdjustmentRuleForAmbiguousOffsets(adjustedTime);
            if (rule != null && rule.HasDaylightSaving)
            {
                DaylightTime daylightTime = GetDaylightTime(adjustedTime.Year, rule);
                isAmbiguous = GetIsAmbiguousTime(adjustedTime, rule, daylightTime);
            }

            if (!isAmbiguous)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeOffsetIsNotAmbiguous"), "dateTimeOffset");
            }

            TimeSpan[] timeSpans = new TimeSpan[2];
            TimeSpan actualUtcOffset = m_baseUtcOffset + rule.BaseUtcOffsetDelta;
            if (rule.DaylightDelta > TimeSpan.Zero)
            {
                timeSpans[0] = actualUtcOffset;
                timeSpans[1] = actualUtcOffset + rule.DaylightDelta;
            }
            else
            {
                timeSpans[0] = actualUtcOffset + rule.DaylightDelta;
                timeSpans[1] = actualUtcOffset;
            }

            return timeSpans;
        }

        public TimeSpan[] GetAmbiguousTimeOffsets(DateTime dateTime)
        {
            if (!SupportsDaylightSavingTime)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeIsNotAmbiguous"), "dateTime");
            }

                        DateTime adjustedTime;
            if (dateTime.Kind == DateTimeKind.Local)
            {
                CachedData cachedData = s_cachedData;
                adjustedTime = TimeZoneInfo.ConvertTime(dateTime, cachedData.Local, this, TimeZoneInfoOptions.None, cachedData);
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                CachedData cachedData = s_cachedData;
                adjustedTime = TimeZoneInfo.ConvertTime(dateTime, cachedData.Utc, this, TimeZoneInfoOptions.None, cachedData);
            }
            else
            {
                adjustedTime = dateTime;
            }

            Boolean isAmbiguous = false;
            AdjustmentRule rule = GetAdjustmentRuleForAmbiguousOffsets(adjustedTime);
            if (rule != null && rule.HasDaylightSaving)
            {
                DaylightTime daylightTime = GetDaylightTime(adjustedTime.Year, rule);
                isAmbiguous = GetIsAmbiguousTime(adjustedTime, rule, daylightTime);
            }

            if (!isAmbiguous)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeIsNotAmbiguous"), "dateTime");
            }

            TimeSpan[] timeSpans = new TimeSpan[2];
            TimeSpan actualUtcOffset = m_baseUtcOffset + rule.BaseUtcOffsetDelta;
            if (rule.DaylightDelta > TimeSpan.Zero)
            {
                timeSpans[0] = actualUtcOffset;
                timeSpans[1] = actualUtcOffset + rule.DaylightDelta;
            }
            else
            {
                timeSpans[0] = actualUtcOffset + rule.DaylightDelta;
                timeSpans[1] = actualUtcOffset;
            }

            return timeSpans;
        }

        private AdjustmentRule GetAdjustmentRuleForAmbiguousOffsets(DateTime adjustedTime)
        {
            AdjustmentRule rule = GetAdjustmentRuleForTime(adjustedTime);
            if (rule != null && rule.NoDaylightTransitions && !rule.HasDaylightSaving)
            {
                return GetPreviousAdjustmentRule(rule);
            }

            return rule;
        }

        private AdjustmentRule GetPreviousAdjustmentRule(AdjustmentRule rule)
        {
            AdjustmentRule result = rule;
            for (int i = 1; i < m_adjustmentRules.Length; i++)
            {
                if (rule.Equals(m_adjustmentRules[i]))
                {
                    result = m_adjustmentRules[i - 1];
                    break;
                }
            }

            return result;
        }

        public TimeSpan GetUtcOffset(DateTimeOffset dateTimeOffset)
        {
            return GetUtcOffsetFromUtc(dateTimeOffset.UtcDateTime, this);
        }

        public TimeSpan GetUtcOffset(DateTime dateTime)
        {
            return GetUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime, s_cachedData);
        }

        internal static TimeSpan GetLocalUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags)
        {
            CachedData cachedData = s_cachedData;
            return cachedData.Local.GetUtcOffset(dateTime, flags, cachedData);
        }

        internal TimeSpan GetUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags)
        {
            return GetUtcOffset(dateTime, flags, s_cachedData);
        }

        private TimeSpan GetUtcOffset(DateTime dateTime, TimeZoneInfoOptions flags, CachedData cachedData)
        {
            if (dateTime.Kind == DateTimeKind.Local)
            {
                if (cachedData.GetCorrespondingKind(this) != DateTimeKind.Local)
                {
                    DateTime adjustedTime = TimeZoneInfo.ConvertTime(dateTime, cachedData.Local, cachedData.Utc, flags);
                    return GetUtcOffsetFromUtc(adjustedTime, this);
                }
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                if (cachedData.GetCorrespondingKind(this) == DateTimeKind.Utc)
                {
                    return m_baseUtcOffset;
                }
                else
                {
                    return GetUtcOffsetFromUtc(dateTime, this);
                }
            }

            return GetUtcOffset(dateTime, this, flags);
        }

        public Boolean IsAmbiguousTime(DateTimeOffset dateTimeOffset)
        {
            if (!m_supportsDaylightSavingTime)
            {
                return false;
            }

            DateTimeOffset adjustedTime = TimeZoneInfo.ConvertTime(dateTimeOffset, this);
            return IsAmbiguousTime(adjustedTime.DateTime);
        }

        public Boolean IsAmbiguousTime(DateTime dateTime)
        {
            return IsAmbiguousTime(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
        }

        internal Boolean IsAmbiguousTime(DateTime dateTime, TimeZoneInfoOptions flags)
        {
            if (!m_supportsDaylightSavingTime)
            {
                return false;
            }

            DateTime adjustedTime;
            if (dateTime.Kind == DateTimeKind.Local)
            {
                CachedData cachedData = s_cachedData;
                adjustedTime = TimeZoneInfo.ConvertTime(dateTime, cachedData.Local, this, flags, cachedData);
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                CachedData cachedData = s_cachedData;
                adjustedTime = TimeZoneInfo.ConvertTime(dateTime, cachedData.Utc, this, flags, cachedData);
            }
            else
            {
                adjustedTime = dateTime;
            }

            AdjustmentRule rule = GetAdjustmentRuleForTime(adjustedTime);
            if (rule != null && rule.HasDaylightSaving)
            {
                DaylightTime daylightTime = GetDaylightTime(adjustedTime.Year, rule);
                return GetIsAmbiguousTime(adjustedTime, rule, daylightTime);
            }

            return false;
        }

        public Boolean IsDaylightSavingTime(DateTimeOffset dateTimeOffset)
        {
            Boolean isDaylightSavingTime;
            GetUtcOffsetFromUtc(dateTimeOffset.UtcDateTime, this, out isDaylightSavingTime);
            return isDaylightSavingTime;
        }

        public Boolean IsDaylightSavingTime(DateTime dateTime)
        {
            return IsDaylightSavingTime(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime, s_cachedData);
        }

        internal Boolean IsDaylightSavingTime(DateTime dateTime, TimeZoneInfoOptions flags)
        {
            return IsDaylightSavingTime(dateTime, flags, s_cachedData);
        }

        private Boolean IsDaylightSavingTime(DateTime dateTime, TimeZoneInfoOptions flags, CachedData cachedData)
        {
            if (!m_supportsDaylightSavingTime || m_adjustmentRules == null)
            {
                return false;
            }

            DateTime adjustedTime;
            if (dateTime.Kind == DateTimeKind.Local)
            {
                adjustedTime = TimeZoneInfo.ConvertTime(dateTime, cachedData.Local, this, flags, cachedData);
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                if (cachedData.GetCorrespondingKind(this) == DateTimeKind.Utc)
                {
                    return false;
                }
                else
                {
                    Boolean isDaylightSavings;
                    GetUtcOffsetFromUtc(dateTime, this, out isDaylightSavings);
                    return isDaylightSavings;
                }
            }
            else
            {
                adjustedTime = dateTime;
            }

            AdjustmentRule rule = GetAdjustmentRuleForTime(adjustedTime);
            if (rule != null && rule.HasDaylightSaving)
            {
                DaylightTime daylightTime = GetDaylightTime(adjustedTime.Year, rule);
                return GetIsDaylightSavings(adjustedTime, rule, daylightTime, flags);
            }
            else
            {
                return false;
            }
        }

        public Boolean IsInvalidTime(DateTime dateTime)
        {
            Boolean isInvalid = false;
            if ((dateTime.Kind == DateTimeKind.Unspecified) || (dateTime.Kind == DateTimeKind.Local && s_cachedData.GetCorrespondingKind(this) == DateTimeKind.Local))
            {
                AdjustmentRule rule = GetAdjustmentRuleForTime(dateTime);
                if (rule != null && rule.HasDaylightSaving)
                {
                    DaylightTime daylightTime = GetDaylightTime(dateTime.Year, rule);
                    isInvalid = GetIsInvalidTime(dateTime, rule, daylightTime);
                }
                else
                {
                    isInvalid = false;
                }
            }

            return isInvalid;
        }

        static public void ClearCachedData()
        {
            s_cachedData = new CachedData();
        }

        static public DateTimeOffset ConvertTimeBySystemTimeZoneId(DateTimeOffset dateTimeOffset, String destinationTimeZoneId)
        {
            return ConvertTime(dateTimeOffset, FindSystemTimeZoneById(destinationTimeZoneId));
        }

        static public DateTime ConvertTimeBySystemTimeZoneId(DateTime dateTime, String destinationTimeZoneId)
        {
            return ConvertTime(dateTime, FindSystemTimeZoneById(destinationTimeZoneId));
        }

        static public DateTime ConvertTimeBySystemTimeZoneId(DateTime dateTime, String sourceTimeZoneId, String destinationTimeZoneId)
        {
            if (dateTime.Kind == DateTimeKind.Local && String.Compare(sourceTimeZoneId, TimeZoneInfo.Local.Id, StringComparison.OrdinalIgnoreCase) == 0)
            {
                CachedData cachedData = s_cachedData;
                return ConvertTime(dateTime, cachedData.Local, FindSystemTimeZoneById(destinationTimeZoneId), TimeZoneInfoOptions.None, cachedData);
            }
            else if (dateTime.Kind == DateTimeKind.Utc && String.Compare(sourceTimeZoneId, TimeZoneInfo.Utc.Id, StringComparison.OrdinalIgnoreCase) == 0)
            {
                CachedData cachedData = s_cachedData;
                return ConvertTime(dateTime, cachedData.Utc, FindSystemTimeZoneById(destinationTimeZoneId), TimeZoneInfoOptions.None, cachedData);
            }
            else
            {
                return ConvertTime(dateTime, FindSystemTimeZoneById(sourceTimeZoneId), FindSystemTimeZoneById(destinationTimeZoneId));
            }
        }

        static public DateTimeOffset ConvertTime(DateTimeOffset dateTimeOffset, TimeZoneInfo destinationTimeZone)
        {
            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException("destinationTimeZone");
            }

                        DateTime utcDateTime = dateTimeOffset.UtcDateTime;
            TimeSpan destinationOffset = GetUtcOffsetFromUtc(utcDateTime, destinationTimeZone);
            Int64 ticks = utcDateTime.Ticks + destinationOffset.Ticks;
            if (ticks > DateTimeOffset.MaxValue.Ticks)
            {
                return DateTimeOffset.MaxValue;
            }
            else if (ticks < DateTimeOffset.MinValue.Ticks)
            {
                return DateTimeOffset.MinValue;
            }
            else
            {
                return new DateTimeOffset(ticks, destinationOffset);
            }
        }

        static public DateTime ConvertTime(DateTime dateTime, TimeZoneInfo destinationTimeZone)
        {
            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException("destinationTimeZone");
            }

                        if (dateTime.Ticks == 0)
            {
                ClearCachedData();
            }

            CachedData cachedData = s_cachedData;
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return ConvertTime(dateTime, cachedData.Utc, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
            }
            else
            {
                return ConvertTime(dateTime, cachedData.Local, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
            }
        }

        static public DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
        {
            return ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, TimeZoneInfoOptions.None, s_cachedData);
        }

        static internal DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags)
        {
            return ConvertTime(dateTime, sourceTimeZone, destinationTimeZone, flags, s_cachedData);
        }

        static private DateTime ConvertTime(DateTime dateTime, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone, TimeZoneInfoOptions flags, CachedData cachedData)
        {
            if (sourceTimeZone == null)
            {
                throw new ArgumentNullException("sourceTimeZone");
            }

            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException("destinationTimeZone");
            }

                        DateTimeKind sourceKind = cachedData.GetCorrespondingKind(sourceTimeZone);
            if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && (dateTime.Kind != DateTimeKind.Unspecified) && (dateTime.Kind != sourceKind))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ConvertMismatch"), "sourceTimeZone");
            }

            AdjustmentRule sourceRule = sourceTimeZone.GetAdjustmentRuleForTime(dateTime);
            TimeSpan sourceOffset = sourceTimeZone.BaseUtcOffset;
            if (sourceRule != null)
            {
                sourceOffset = sourceOffset + sourceRule.BaseUtcOffsetDelta;
                if (sourceRule.HasDaylightSaving)
                {
                    Boolean sourceIsDaylightSavings = false;
                    DaylightTime sourceDaylightTime = sourceTimeZone.GetDaylightTime(dateTime.Year, sourceRule);
                    if (((flags & TimeZoneInfoOptions.NoThrowOnInvalidTime) == 0) && GetIsInvalidTime(dateTime, sourceRule, sourceDaylightTime))
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeIsInvalid"), "dateTime");
                    }

                    sourceIsDaylightSavings = GetIsDaylightSavings(dateTime, sourceRule, sourceDaylightTime, flags);
                    sourceOffset += (sourceIsDaylightSavings ? sourceRule.DaylightDelta : TimeSpan.Zero);
                }
            }

            DateTimeKind targetKind = cachedData.GetCorrespondingKind(destinationTimeZone);
            if (dateTime.Kind != DateTimeKind.Unspecified && sourceKind != DateTimeKind.Unspecified && sourceKind == targetKind)
            {
                return dateTime;
            }

            Int64 utcTicks = dateTime.Ticks - sourceOffset.Ticks;
            Boolean isAmbiguousLocalDst = false;
            DateTime targetConverted = ConvertUtcToTimeZone(utcTicks, destinationTimeZone, out isAmbiguousLocalDst);
            if (targetKind == DateTimeKind.Local)
            {
                return new DateTime(targetConverted.Ticks, DateTimeKind.Local, isAmbiguousLocalDst);
            }
            else
            {
                return new DateTime(targetConverted.Ticks, targetKind);
            }
        }

        static public DateTime ConvertTimeFromUtc(DateTime dateTime, TimeZoneInfo destinationTimeZone)
        {
            CachedData cachedData = s_cachedData;
            return ConvertTime(dateTime, cachedData.Utc, destinationTimeZone, TimeZoneInfoOptions.None, cachedData);
        }

        static public DateTime ConvertTimeToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }

            CachedData cachedData = s_cachedData;
            return ConvertTime(dateTime, cachedData.Local, cachedData.Utc, TimeZoneInfoOptions.None, cachedData);
        }

        static internal DateTime ConvertTimeToUtc(DateTime dateTime, TimeZoneInfoOptions flags)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }

            CachedData cachedData = s_cachedData;
            return ConvertTime(dateTime, cachedData.Local, cachedData.Utc, flags, cachedData);
        }

        static public DateTime ConvertTimeToUtc(DateTime dateTime, TimeZoneInfo sourceTimeZone)
        {
            CachedData cachedData = s_cachedData;
            return ConvertTime(dateTime, sourceTimeZone, cachedData.Utc, TimeZoneInfoOptions.None, cachedData);
        }

        public bool Equals(TimeZoneInfo other)
        {
            return (other != null && String.Compare(this.m_id, other.m_id, StringComparison.OrdinalIgnoreCase) == 0 && HasSameRules(other));
        }

        public override bool Equals(object obj)
        {
            TimeZoneInfo tzi = obj as TimeZoneInfo;
            if (null == tzi)
            {
                return false;
            }

            return Equals(tzi);
        }

        static public TimeZoneInfo FromSerializedString(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (source.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSerializedString", source), "source");
            }

                        return StringSerializer.GetDeserializedTimeZoneInfo(source);
        }

        public override int GetHashCode()
        {
            return m_id.ToUpper(CultureInfo.InvariantCulture).GetHashCode();
        }

        static public ReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones()
        {
            CachedData cachedData = s_cachedData;
            lock (cachedData)
            {
                if (cachedData.m_readOnlySystemTimeZones == null)
                {
                    PopulateAllSystemTimeZones(cachedData);
                    cachedData.m_allSystemTimeZonesRead = true;
                    List<TimeZoneInfo> list;
                    if (cachedData.m_systemTimeZones != null)
                    {
                        list = new List<TimeZoneInfo>(cachedData.m_systemTimeZones.Values);
                    }
                    else
                    {
                        list = new List<TimeZoneInfo>();
                    }

                    list.Sort(new TimeZoneInfoComparer());
                    cachedData.m_readOnlySystemTimeZones = new ReadOnlyCollection<TimeZoneInfo>(list);
                }
            }

            return cachedData.m_readOnlySystemTimeZones;
        }

        private static void PopulateAllSystemTimeZones(CachedData cachedData)
        {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
            permSet.AddPermission(new RegistryPermission(RegistryPermissionAccess.Read, c_timeZonesRegistryHivePermissionList));
            permSet.Assert();
            using (RegistryKey reg = Registry.LocalMachine.OpenSubKey(c_timeZonesRegistryHive, false))
            {
                if (reg != null)
                {
                    foreach (string keyName in reg.GetSubKeyNames())
                    {
                        TimeZoneInfo value;
                        Exception ex;
                        TryGetTimeZone(keyName, false, out value, out ex, cachedData);
                    }
                }
            }
        }

        public Boolean HasSameRules(TimeZoneInfo other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

                        if (this.m_baseUtcOffset != other.m_baseUtcOffset || this.m_supportsDaylightSavingTime != other.m_supportsDaylightSavingTime)
            {
                return false;
            }

            bool sameRules;
            AdjustmentRule[] currentRules = this.m_adjustmentRules;
            AdjustmentRule[] otherRules = other.m_adjustmentRules;
            sameRules = (currentRules == null && otherRules == null) || (currentRules != null && otherRules != null);
            if (!sameRules)
            {
                return false;
            }

            if (currentRules != null)
            {
                if (currentRules.Length != otherRules.Length)
                {
                    return false;
                }

                for (int i = 0; i < currentRules.Length; i++)
                {
                    if (!(currentRules[i]).Equals(otherRules[i]))
                    {
                        return false;
                    }
                }
            }

            return sameRules;
        }

        static public TimeZoneInfo Local
        {
            get
            {
                                return s_cachedData.Local;
            }
        }

        public String ToSerializedString()
        {
            return StringSerializer.GetSerializedString(this);
        }

        public override string ToString()
        {
            return this.DisplayName;
        }

        static public TimeZoneInfo Utc
        {
            get
            {
                                return s_cachedData.Utc;
            }
        }

        private TimeZoneInfo(Win32Native.TimeZoneInformation zone, Boolean dstDisabled)
        {
            if (String.IsNullOrEmpty(zone.StandardName))
            {
                m_id = c_localId;
            }
            else
            {
                m_id = zone.StandardName;
            }

            m_baseUtcOffset = new TimeSpan(0, -(zone.Bias), 0);
            if (!dstDisabled)
            {
                Win32Native.RegistryTimeZoneInformation regZone = new Win32Native.RegistryTimeZoneInformation(zone);
                AdjustmentRule rule = CreateAdjustmentRuleFromTimeZoneInformation(regZone, DateTime.MinValue.Date, DateTime.MaxValue.Date, zone.Bias);
                if (rule != null)
                {
                    m_adjustmentRules = new AdjustmentRule[1];
                    m_adjustmentRules[0] = rule;
                }
            }

            ValidateTimeZoneInfo(m_id, m_baseUtcOffset, m_adjustmentRules, out m_supportsDaylightSavingTime);
            m_displayName = zone.StandardName;
            m_standardDisplayName = zone.StandardName;
            m_daylightDisplayName = zone.DaylightName;
        }

        private TimeZoneInfo(String id, TimeSpan baseUtcOffset, String displayName, String standardDisplayName, String daylightDisplayName, AdjustmentRule[] adjustmentRules, Boolean disableDaylightSavingTime)
        {
            Boolean adjustmentRulesSupportDst;
            ValidateTimeZoneInfo(id, baseUtcOffset, adjustmentRules, out adjustmentRulesSupportDst);
            if (!disableDaylightSavingTime && adjustmentRules != null && adjustmentRules.Length > 0)
            {
                m_adjustmentRules = (AdjustmentRule[])adjustmentRules.Clone();
            }

            m_id = id;
            m_baseUtcOffset = baseUtcOffset;
            m_displayName = displayName;
            m_standardDisplayName = standardDisplayName;
            m_daylightDisplayName = (disableDaylightSavingTime ? null : daylightDisplayName);
            m_supportsDaylightSavingTime = adjustmentRulesSupportDst && !disableDaylightSavingTime;
        }

        static public TimeZoneInfo CreateCustomTimeZone(String id, TimeSpan baseUtcOffset, String displayName, String standardDisplayName)
        {
            return new TimeZoneInfo(id, baseUtcOffset, displayName, standardDisplayName, standardDisplayName, null, false);
        }

        static public TimeZoneInfo CreateCustomTimeZone(String id, TimeSpan baseUtcOffset, String displayName, String standardDisplayName, String daylightDisplayName, AdjustmentRule[] adjustmentRules)
        {
            return new TimeZoneInfo(id, baseUtcOffset, displayName, standardDisplayName, daylightDisplayName, adjustmentRules, false);
        }

        static public TimeZoneInfo CreateCustomTimeZone(String id, TimeSpan baseUtcOffset, String displayName, String standardDisplayName, String daylightDisplayName, AdjustmentRule[] adjustmentRules, Boolean disableDaylightSavingTime)
        {
            return new TimeZoneInfo(id, baseUtcOffset, displayName, standardDisplayName, daylightDisplayName, adjustmentRules, disableDaylightSavingTime);
        }

        private AdjustmentRule GetAdjustmentRuleForTime(DateTime dateTime, bool dateTimeisUtc = false)
        {
            if (m_adjustmentRules == null || m_adjustmentRules.Length == 0)
            {
                return null;
            }

            DateTime date;
            if (dateTimeisUtc)
            {
                date = (dateTime + BaseUtcOffset).Date;
            }
            else
            {
                date = dateTime.Date;
            }

            for (int i = 0; i < m_adjustmentRules.Length; i++)
            {
                AdjustmentRule rule = m_adjustmentRules[i];
                AdjustmentRule previousRule = i > 0 ? m_adjustmentRules[i - 1] : rule;
                if (IsAdjustmentRuleValid(rule, previousRule, dateTime, date, dateTimeisUtc))
                {
                    return rule;
                }
            }

            return null;
        }

        private bool IsAdjustmentRuleValid(AdjustmentRule rule, AdjustmentRule previousRule, DateTime dateTime, DateTime dateOnly, bool dateTimeisUtc)
        {
            bool isAfterStart;
            if (rule.DateStart.Kind == DateTimeKind.Utc)
            {
                DateTime dateTimeToCompare;
                if (dateTimeisUtc)
                {
                    dateTimeToCompare = dateTime;
                }
                else
                {
                    dateTimeToCompare = ConvertToUtc(dateTime, previousRule.DaylightDelta, previousRule.BaseUtcOffsetDelta);
                }

                isAfterStart = dateTimeToCompare >= rule.DateStart;
            }
            else
            {
                isAfterStart = dateOnly >= rule.DateStart;
            }

            if (!isAfterStart)
            {
                return false;
            }

            bool isBeforeEnd;
            if (rule.DateEnd.Kind == DateTimeKind.Utc)
            {
                DateTime dateTimeToCompare;
                if (dateTimeisUtc)
                {
                    dateTimeToCompare = dateTime;
                }
                else
                {
                    dateTimeToCompare = ConvertToUtc(dateTime, rule.DaylightDelta, rule.BaseUtcOffsetDelta);
                }

                isBeforeEnd = dateTimeToCompare <= rule.DateEnd;
            }
            else
            {
                isBeforeEnd = dateOnly <= rule.DateEnd;
            }

            return isBeforeEnd;
        }

        private DateTime ConvertToUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta)
        {
            return ConvertToFromUtc(dateTime, daylightDelta, baseUtcOffsetDelta, convertToUtc: true);
        }

        private DateTime ConvertFromUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta)
        {
            return ConvertToFromUtc(dateTime, daylightDelta, baseUtcOffsetDelta, convertToUtc: false);
        }

        private DateTime ConvertToFromUtc(DateTime dateTime, TimeSpan daylightDelta, TimeSpan baseUtcOffsetDelta, bool convertToUtc)
        {
            TimeSpan offset = BaseUtcOffset + daylightDelta + baseUtcOffsetDelta;
            if (convertToUtc)
            {
                offset = offset.Negate();
            }

            long ticks = dateTime.Ticks + offset.Ticks;
            DateTime result;
            if (ticks > DateTime.MaxValue.Ticks)
            {
                result = DateTime.MaxValue;
            }
            else if (ticks < DateTime.MinValue.Ticks)
            {
                result = DateTime.MinValue;
            }
            else
            {
                result = new DateTime(ticks);
            }

            return result;
        }

        static private Boolean CheckDaylightSavingTimeNotSupported(Win32Native.TimeZoneInformation timeZone)
        {
            return (timeZone.DaylightDate.Year == timeZone.StandardDate.Year && timeZone.DaylightDate.Month == timeZone.StandardDate.Month && timeZone.DaylightDate.DayOfWeek == timeZone.StandardDate.DayOfWeek && timeZone.DaylightDate.Day == timeZone.StandardDate.Day && timeZone.DaylightDate.Hour == timeZone.StandardDate.Hour && timeZone.DaylightDate.Minute == timeZone.StandardDate.Minute && timeZone.DaylightDate.Second == timeZone.StandardDate.Second && timeZone.DaylightDate.Milliseconds == timeZone.StandardDate.Milliseconds);
        }

        static private DateTime ConvertUtcToTimeZone(Int64 ticks, TimeZoneInfo destinationTimeZone, out Boolean isAmbiguousLocalDst)
        {
            DateTime utcConverted;
            DateTime localConverted;
            if (ticks > DateTime.MaxValue.Ticks)
            {
                utcConverted = DateTime.MaxValue;
            }
            else if (ticks < DateTime.MinValue.Ticks)
            {
                utcConverted = DateTime.MinValue;
            }
            else
            {
                utcConverted = new DateTime(ticks);
            }

            TimeSpan offset = GetUtcOffsetFromUtc(utcConverted, destinationTimeZone, out isAmbiguousLocalDst);
            ticks += offset.Ticks;
            if (ticks > DateTime.MaxValue.Ticks)
            {
                localConverted = DateTime.MaxValue;
            }
            else if (ticks < DateTime.MinValue.Ticks)
            {
                localConverted = DateTime.MinValue;
            }
            else
            {
                localConverted = new DateTime(ticks);
            }

            return localConverted;
        }

        static private AdjustmentRule CreateAdjustmentRuleFromTimeZoneInformation(Win32Native.RegistryTimeZoneInformation timeZoneInformation, DateTime startDate, DateTime endDate, int defaultBaseUtcOffset)
        {
            AdjustmentRule rule;
            bool supportsDst = (timeZoneInformation.StandardDate.Month != 0);
            if (!supportsDst)
            {
                if (timeZoneInformation.Bias == defaultBaseUtcOffset)
                {
                    return null;
                }

                return rule = AdjustmentRule.CreateAdjustmentRule(startDate, endDate, TimeSpan.Zero, TransitionTime.CreateFixedDateRule(DateTime.MinValue, 1, 1), TransitionTime.CreateFixedDateRule(DateTime.MinValue.AddMilliseconds(1), 1, 1), new TimeSpan(0, defaultBaseUtcOffset - timeZoneInformation.Bias, 0), noDaylightTransitions: false);
            }

            TransitionTime daylightTransitionStart;
            if (!TransitionTimeFromTimeZoneInformation(timeZoneInformation, out daylightTransitionStart, true))
            {
                return null;
            }

            TransitionTime daylightTransitionEnd;
            if (!TransitionTimeFromTimeZoneInformation(timeZoneInformation, out daylightTransitionEnd, false))
            {
                return null;
            }

            if (daylightTransitionStart.Equals(daylightTransitionEnd))
            {
                return null;
            }

            rule = AdjustmentRule.CreateAdjustmentRule(startDate, endDate, new TimeSpan(0, -timeZoneInformation.DaylightBias, 0), (TransitionTime)daylightTransitionStart, (TransitionTime)daylightTransitionEnd, new TimeSpan(0, defaultBaseUtcOffset - timeZoneInformation.Bias, 0), noDaylightTransitions: false);
            return rule;
        }

        static private String FindIdFromTimeZoneInformation(Win32Native.TimeZoneInformation timeZone, out Boolean dstDisabled)
        {
            dstDisabled = false;
            try
            {
                PermissionSet permSet = new PermissionSet(PermissionState.None);
                permSet.AddPermission(new RegistryPermission(RegistryPermissionAccess.Read, c_timeZonesRegistryHivePermissionList));
                permSet.Assert();
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(c_timeZonesRegistryHive, false))
                {
                    if (key == null)
                    {
                        return null;
                    }

                    foreach (string keyName in key.GetSubKeyNames())
                    {
                        if (TryCompareTimeZoneInformationToRegistry(timeZone, keyName, out dstDisabled))
                        {
                            return keyName;
                        }
                    }
                }
            }
            finally
            {
                PermissionSet.RevertAssert();
            }

            return null;
        }

        private DaylightTime GetDaylightTime(Int32 year, AdjustmentRule rule)
        {
            TimeSpan delta = rule.DaylightDelta;
            DateTime startTime;
            DateTime endTime;
            if (rule.NoDaylightTransitions)
            {
                AdjustmentRule previousRule = GetPreviousAdjustmentRule(rule);
                startTime = ConvertFromUtc(rule.DateStart, previousRule.DaylightDelta, previousRule.BaseUtcOffsetDelta);
                endTime = ConvertFromUtc(rule.DateEnd, rule.DaylightDelta, rule.BaseUtcOffsetDelta);
            }
            else
            {
                startTime = TransitionTimeToDateTime(year, rule.DaylightTransitionStart);
                endTime = TransitionTimeToDateTime(year, rule.DaylightTransitionEnd);
            }

            return new DaylightTime(startTime, endTime, delta);
        }

        static private Boolean GetIsDaylightSavings(DateTime time, AdjustmentRule rule, DaylightTime daylightTime, TimeZoneInfoOptions flags)
        {
            if (rule == null)
            {
                return false;
            }

            DateTime startTime;
            DateTime endTime;
            if (time.Kind == DateTimeKind.Local)
            {
                startTime = rule.IsStartDateMarkerForBeginningOfYear() ? new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) : daylightTime.Start + daylightTime.Delta;
                endTime = rule.IsEndDateMarkerForEndOfYear() ? new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1) : daylightTime.End;
            }
            else
            {
                Boolean invalidAtStart = rule.DaylightDelta > TimeSpan.Zero;
                startTime = rule.IsStartDateMarkerForBeginningOfYear() ? new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) : daylightTime.Start + (invalidAtStart ? rule.DaylightDelta : TimeSpan.Zero);
                endTime = rule.IsEndDateMarkerForEndOfYear() ? new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1) : daylightTime.End + (invalidAtStart ? -rule.DaylightDelta : TimeSpan.Zero);
            }

            Boolean isDst = CheckIsDst(startTime, time, endTime, false, rule);
            if (isDst && time.Kind == DateTimeKind.Local)
            {
                if (GetIsAmbiguousTime(time, rule, daylightTime))
                {
                    isDst = time.IsAmbiguousDaylightSavingTime();
                }
            }

            return isDst;
        }

        private TimeSpan GetDaylightSavingsStartOffsetFromUtc(TimeSpan baseUtcOffset, AdjustmentRule rule)
        {
            if (rule.NoDaylightTransitions)
            {
                AdjustmentRule previousRule = GetPreviousAdjustmentRule(rule);
                return baseUtcOffset + previousRule.BaseUtcOffsetDelta + previousRule.DaylightDelta;
            }
            else
            {
                return baseUtcOffset + rule.BaseUtcOffsetDelta;
            }
        }

        private TimeSpan GetDaylightSavingsEndOffsetFromUtc(TimeSpan baseUtcOffset, AdjustmentRule rule)
        {
            return baseUtcOffset + rule.BaseUtcOffsetDelta + rule.DaylightDelta;
        }

        static private Boolean GetIsDaylightSavingsFromUtc(DateTime time, Int32 Year, TimeSpan utc, AdjustmentRule rule, out Boolean isAmbiguousLocalDst, TimeZoneInfo zone)
        {
            isAmbiguousLocalDst = false;
            if (rule == null)
            {
                return false;
            }

            DaylightTime daylightTime = zone.GetDaylightTime(Year, rule);
            bool ignoreYearAdjustment = false;
            TimeSpan dstStartOffset = zone.GetDaylightSavingsStartOffsetFromUtc(utc, rule);
            DateTime startTime;
            if (rule.IsStartDateMarkerForBeginningOfYear() && daylightTime.Start.Year > DateTime.MinValue.Year)
            {
                AdjustmentRule previousYearRule = zone.GetAdjustmentRuleForTime(new DateTime(daylightTime.Start.Year - 1, 12, 31));
                if (previousYearRule != null && previousYearRule.IsEndDateMarkerForEndOfYear())
                {
                    DaylightTime previousDaylightTime = zone.GetDaylightTime(daylightTime.Start.Year - 1, previousYearRule);
                    startTime = previousDaylightTime.Start - utc - previousYearRule.BaseUtcOffsetDelta;
                    ignoreYearAdjustment = true;
                }
                else
                {
                    startTime = new DateTime(daylightTime.Start.Year, 1, 1, 0, 0, 0) - dstStartOffset;
                }
            }
            else
            {
                startTime = daylightTime.Start - dstStartOffset;
            }

            TimeSpan dstEndOffset = zone.GetDaylightSavingsEndOffsetFromUtc(utc, rule);
            DateTime endTime;
            if (rule.IsEndDateMarkerForEndOfYear() && daylightTime.End.Year < DateTime.MaxValue.Year)
            {
                AdjustmentRule nextYearRule = zone.GetAdjustmentRuleForTime(new DateTime(daylightTime.End.Year + 1, 1, 1));
                if (nextYearRule != null && nextYearRule.IsStartDateMarkerForBeginningOfYear())
                {
                    if (nextYearRule.IsEndDateMarkerForEndOfYear())
                    {
                        endTime = new DateTime(daylightTime.End.Year + 1, 12, 31) - utc - nextYearRule.BaseUtcOffsetDelta - nextYearRule.DaylightDelta;
                    }
                    else
                    {
                        DaylightTime nextdaylightTime = zone.GetDaylightTime(daylightTime.End.Year + 1, nextYearRule);
                        endTime = nextdaylightTime.End - utc - nextYearRule.BaseUtcOffsetDelta - nextYearRule.DaylightDelta;
                    }

                    ignoreYearAdjustment = true;
                }
                else
                {
                    endTime = new DateTime(daylightTime.End.Year + 1, 1, 1, 0, 0, 0).AddTicks(-1) - dstEndOffset;
                }
            }
            else
            {
                endTime = daylightTime.End - dstEndOffset;
            }

            DateTime ambiguousStart;
            DateTime ambiguousEnd;
            if (daylightTime.Delta.Ticks > 0)
            {
                ambiguousStart = endTime - daylightTime.Delta;
                ambiguousEnd = endTime;
            }
            else
            {
                ambiguousStart = startTime;
                ambiguousEnd = startTime - daylightTime.Delta;
            }

            Boolean isDst = CheckIsDst(startTime, time, endTime, ignoreYearAdjustment, rule);
            if (isDst)
            {
                isAmbiguousLocalDst = (time >= ambiguousStart && time < ambiguousEnd);
                if (!isAmbiguousLocalDst && ambiguousStart.Year != ambiguousEnd.Year)
                {
                    DateTime ambiguousStartModified;
                    DateTime ambiguousEndModified;
                    try
                    {
                        ambiguousStartModified = ambiguousStart.AddYears(1);
                        ambiguousEndModified = ambiguousEnd.AddYears(1);
                        isAmbiguousLocalDst = (time >= ambiguousStart && time < ambiguousEnd);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }

                    if (!isAmbiguousLocalDst)
                    {
                        try
                        {
                            ambiguousStartModified = ambiguousStart.AddYears(-1);
                            ambiguousEndModified = ambiguousEnd.AddYears(-1);
                            isAmbiguousLocalDst = (time >= ambiguousStart && time < ambiguousEnd);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                    }
                }
            }

            return isDst;
        }

        static private Boolean CheckIsDst(DateTime startTime, DateTime time, DateTime endTime, bool ignoreYearAdjustment, AdjustmentRule rule)
        {
            Boolean isDst;
            if (!ignoreYearAdjustment && !rule.NoDaylightTransitions)
            {
                int startTimeYear = startTime.Year;
                int endTimeYear = endTime.Year;
                if (startTimeYear != endTimeYear)
                {
                    endTime = endTime.AddYears(startTimeYear - endTimeYear);
                }

                int timeYear = time.Year;
                if (startTimeYear != timeYear)
                {
                    time = time.AddYears(startTimeYear - timeYear);
                }
            }

            if (startTime > endTime)
            {
                isDst = (time < endTime || time >= startTime);
            }
            else if (rule.NoDaylightTransitions)
            {
                isDst = (time >= startTime && time <= endTime);
            }
            else
            {
                isDst = (time >= startTime && time < endTime);
            }

            return isDst;
        }

        static private Boolean GetIsAmbiguousTime(DateTime time, AdjustmentRule rule, DaylightTime daylightTime)
        {
            Boolean isAmbiguous = false;
            if (rule == null || rule.DaylightDelta == TimeSpan.Zero)
            {
                return isAmbiguous;
            }

            DateTime startAmbiguousTime;
            DateTime endAmbiguousTime;
            if (rule.DaylightDelta > TimeSpan.Zero)
            {
                if (rule.IsEndDateMarkerForEndOfYear())
                {
                    return false;
                }

                startAmbiguousTime = daylightTime.End;
                endAmbiguousTime = daylightTime.End - rule.DaylightDelta;
            }
            else
            {
                if (rule.IsStartDateMarkerForBeginningOfYear())
                {
                    return false;
                }

                startAmbiguousTime = daylightTime.Start;
                endAmbiguousTime = daylightTime.Start + rule.DaylightDelta;
            }

            isAmbiguous = (time >= endAmbiguousTime && time < startAmbiguousTime);
            if (!isAmbiguous && startAmbiguousTime.Year != endAmbiguousTime.Year)
            {
                DateTime startModifiedAmbiguousTime;
                DateTime endModifiedAmbiguousTime;
                try
                {
                    startModifiedAmbiguousTime = startAmbiguousTime.AddYears(1);
                    endModifiedAmbiguousTime = endAmbiguousTime.AddYears(1);
                    isAmbiguous = (time >= endModifiedAmbiguousTime && time < startModifiedAmbiguousTime);
                }
                catch (ArgumentOutOfRangeException)
                {
                }

                if (!isAmbiguous)
                {
                    try
                    {
                        startModifiedAmbiguousTime = startAmbiguousTime.AddYears(-1);
                        endModifiedAmbiguousTime = endAmbiguousTime.AddYears(-1);
                        isAmbiguous = (time >= endModifiedAmbiguousTime && time < startModifiedAmbiguousTime);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }
            }

            return isAmbiguous;
        }

        static private Boolean GetIsInvalidTime(DateTime time, AdjustmentRule rule, DaylightTime daylightTime)
        {
            Boolean isInvalid = false;
            if (rule == null || rule.DaylightDelta == TimeSpan.Zero)
            {
                return isInvalid;
            }

            DateTime startInvalidTime;
            DateTime endInvalidTime;
            if (rule.DaylightDelta < TimeSpan.Zero)
            {
                if (rule.IsEndDateMarkerForEndOfYear())
                    return false;
                startInvalidTime = daylightTime.End;
                endInvalidTime = daylightTime.End - rule.DaylightDelta;
            }
            else
            {
                if (rule.IsStartDateMarkerForBeginningOfYear())
                    return false;
                startInvalidTime = daylightTime.Start;
                endInvalidTime = daylightTime.Start + rule.DaylightDelta;
            }

            isInvalid = (time >= startInvalidTime && time < endInvalidTime);
            if (!isInvalid && startInvalidTime.Year != endInvalidTime.Year)
            {
                DateTime startModifiedInvalidTime;
                DateTime endModifiedInvalidTime;
                try
                {
                    startModifiedInvalidTime = startInvalidTime.AddYears(1);
                    endModifiedInvalidTime = endInvalidTime.AddYears(1);
                    isInvalid = (time >= startModifiedInvalidTime && time < endModifiedInvalidTime);
                }
                catch (ArgumentOutOfRangeException)
                {
                }

                if (!isInvalid)
                {
                    try
                    {
                        startModifiedInvalidTime = startInvalidTime.AddYears(-1);
                        endModifiedInvalidTime = endInvalidTime.AddYears(-1);
                        isInvalid = (time >= startModifiedInvalidTime && time < endModifiedInvalidTime);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                }
            }

            return isInvalid;
        }

        static private TimeZoneInfo GetLocalTimeZone(CachedData cachedData)
        {
            String id = null;
            Win32Native.DynamicTimeZoneInformation dynamicTimeZoneInformation = new Win32Native.DynamicTimeZoneInformation();
            long result = UnsafeNativeMethods.GetDynamicTimeZoneInformation(out dynamicTimeZoneInformation);
            if (result == Win32Native.TIME_ZONE_ID_INVALID)
            {
                return CreateCustomTimeZone(c_localId, TimeSpan.Zero, c_localId, c_localId);
            }

            Win32Native.TimeZoneInformation timeZoneInformation = new Win32Native.TimeZoneInformation(dynamicTimeZoneInformation);
            Boolean dstDisabled = dynamicTimeZoneInformation.DynamicDaylightTimeDisabled;
            if (!String.IsNullOrEmpty(dynamicTimeZoneInformation.TimeZoneKeyName))
            {
                TimeZoneInfo zone;
                Exception ex;
                if (TryGetTimeZone(dynamicTimeZoneInformation.TimeZoneKeyName, dstDisabled, out zone, out ex, cachedData) == TimeZoneInfoResult.Success)
                {
                    return zone;
                }
            }

            id = FindIdFromTimeZoneInformation(timeZoneInformation, out dstDisabled);
            if (id != null)
            {
                TimeZoneInfo zone;
                Exception ex;
                if (TryGetTimeZone(id, dstDisabled, out zone, out ex, cachedData) == TimeZoneInfoResult.Success)
                {
                    return zone;
                }
            }

            return GetLocalTimeZoneFromWin32Data(timeZoneInformation, dstDisabled);
        }

        static private TimeZoneInfo GetLocalTimeZoneFromWin32Data(Win32Native.TimeZoneInformation timeZoneInformation, Boolean dstDisabled)
        {
            try
            {
                return new TimeZoneInfo(timeZoneInformation, dstDisabled);
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }

            if (!dstDisabled)
            {
                try
                {
                    return new TimeZoneInfo(timeZoneInformation, true);
                }
                catch (ArgumentException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return CreateCustomTimeZone(c_localId, TimeSpan.Zero, c_localId, c_localId);
        }

        static public TimeZoneInfo FindSystemTimeZoneById(string id)
        {
            if (String.Compare(id, c_utcId, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return TimeZoneInfo.Utc;
            }

            if (id == null)
            {
                throw new ArgumentNullException("id");
            }
            else if (!IsValidSystemTimeZoneId(id))
            {
                throw new TimeZoneNotFoundException(Environment.GetResourceString("TimeZoneNotFound_MissingData", id));
            }

            TimeZoneInfo value;
            Exception e;
            TimeZoneInfoResult result;
            CachedData cachedData = s_cachedData;
            lock (cachedData)
            {
                result = TryGetTimeZone(id, false, out value, out e, cachedData);
            }

            if (result == TimeZoneInfoResult.Success)
            {
                return value;
            }
            else if (result == TimeZoneInfoResult.InvalidTimeZoneException)
            {
                throw new InvalidTimeZoneException(Environment.GetResourceString("InvalidTimeZone_InvalidRegistryData", id), e);
            }
            else if (result == TimeZoneInfoResult.SecurityException)
            {
                throw new SecurityException(Environment.GetResourceString("Security_CannotReadRegistryData", id), e);
            }
            else
            {
                throw new TimeZoneNotFoundException(Environment.GetResourceString("TimeZoneNotFound_MissingData", id), e);
            }
        }

        private static bool IsValidSystemTimeZoneId(string id)
        {
            bool isValid = id.Length != 0 && !id.Contains("\0");
            isValid &= id.Length <= c_maxKeyLength;
            return isValid;
        }

        static private TimeSpan GetUtcOffset(DateTime time, TimeZoneInfo zone, TimeZoneInfoOptions flags)
        {
            TimeSpan baseOffset = zone.BaseUtcOffset;
            AdjustmentRule rule = zone.GetAdjustmentRuleForTime(time);
            if (rule != null)
            {
                baseOffset = baseOffset + rule.BaseUtcOffsetDelta;
                if (rule.HasDaylightSaving)
                {
                    DaylightTime daylightTime = zone.GetDaylightTime(time.Year, rule);
                    Boolean isDaylightSavings = GetIsDaylightSavings(time, rule, daylightTime, flags);
                    baseOffset += (isDaylightSavings ? rule.DaylightDelta : TimeSpan.Zero);
                }
            }

            return baseOffset;
        }

        static private TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone)
        {
            Boolean isDaylightSavings;
            return GetUtcOffsetFromUtc(time, zone, out isDaylightSavings);
        }

        static private TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone, out Boolean isDaylightSavings)
        {
            Boolean isAmbiguousLocalDst;
            return GetUtcOffsetFromUtc(time, zone, out isDaylightSavings, out isAmbiguousLocalDst);
        }

        static internal TimeSpan GetDateTimeNowUtcOffsetFromUtc(DateTime time, out Boolean isAmbiguousLocalDst)
        {
            Boolean isDaylightSavings = false;
            isAmbiguousLocalDst = false;
            TimeSpan baseOffset;
            int timeYear = time.Year;
            OffsetAndRule match = s_cachedData.GetOneYearLocalFromUtc(timeYear);
            baseOffset = match.offset;
            if (match.rule != null)
            {
                baseOffset = baseOffset + match.rule.BaseUtcOffsetDelta;
                if (match.rule.HasDaylightSaving)
                {
                    isDaylightSavings = GetIsDaylightSavingsFromUtc(time, timeYear, match.offset, match.rule, out isAmbiguousLocalDst, TimeZoneInfo.Local);
                    baseOffset += (isDaylightSavings ? match.rule.DaylightDelta : TimeSpan.Zero);
                }
            }

            return baseOffset;
        }

        static internal TimeSpan GetUtcOffsetFromUtc(DateTime time, TimeZoneInfo zone, out Boolean isDaylightSavings, out Boolean isAmbiguousLocalDst)
        {
            isDaylightSavings = false;
            isAmbiguousLocalDst = false;
            TimeSpan baseOffset = zone.BaseUtcOffset;
            Int32 year;
            AdjustmentRule rule;
            if (time > s_maxDateOnly)
            {
                rule = zone.GetAdjustmentRuleForTime(DateTime.MaxValue);
                year = 9999;
            }
            else if (time < s_minDateOnly)
            {
                rule = zone.GetAdjustmentRuleForTime(DateTime.MinValue);
                year = 1;
            }
            else
            {
                rule = zone.GetAdjustmentRuleForTime(time, dateTimeisUtc: true);
                DateTime targetTime = time + baseOffset;
                year = targetTime.Year;
            }

            if (rule != null)
            {
                baseOffset = baseOffset + rule.BaseUtcOffsetDelta;
                if (rule.HasDaylightSaving)
                {
                    isDaylightSavings = GetIsDaylightSavingsFromUtc(time, year, zone.m_baseUtcOffset, rule, out isAmbiguousLocalDst, zone);
                    baseOffset += (isDaylightSavings ? rule.DaylightDelta : TimeSpan.Zero);
                }
            }

            return baseOffset;
        }

        static private bool TransitionTimeFromTimeZoneInformation(Win32Native.RegistryTimeZoneInformation timeZoneInformation, out TransitionTime transitionTime, bool readStartDate)
        {
            bool supportsDst = (timeZoneInformation.StandardDate.Month != 0);
            if (!supportsDst)
            {
                transitionTime = default (TransitionTime);
                return false;
            }

            if (readStartDate)
            {
                if (timeZoneInformation.DaylightDate.Year == 0)
                {
                    transitionTime = TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, timeZoneInformation.DaylightDate.Hour, timeZoneInformation.DaylightDate.Minute, timeZoneInformation.DaylightDate.Second, timeZoneInformation.DaylightDate.Milliseconds), timeZoneInformation.DaylightDate.Month, timeZoneInformation.DaylightDate.Day, (DayOfWeek)timeZoneInformation.DaylightDate.DayOfWeek);
                }
                else
                {
                    transitionTime = TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, timeZoneInformation.DaylightDate.Hour, timeZoneInformation.DaylightDate.Minute, timeZoneInformation.DaylightDate.Second, timeZoneInformation.DaylightDate.Milliseconds), timeZoneInformation.DaylightDate.Month, timeZoneInformation.DaylightDate.Day);
                }
            }
            else
            {
                if (timeZoneInformation.StandardDate.Year == 0)
                {
                    transitionTime = TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, timeZoneInformation.StandardDate.Hour, timeZoneInformation.StandardDate.Minute, timeZoneInformation.StandardDate.Second, timeZoneInformation.StandardDate.Milliseconds), timeZoneInformation.StandardDate.Month, timeZoneInformation.StandardDate.Day, (DayOfWeek)timeZoneInformation.StandardDate.DayOfWeek);
                }
                else
                {
                    transitionTime = TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, timeZoneInformation.StandardDate.Hour, timeZoneInformation.StandardDate.Minute, timeZoneInformation.StandardDate.Second, timeZoneInformation.StandardDate.Milliseconds), timeZoneInformation.StandardDate.Month, timeZoneInformation.StandardDate.Day);
                }
            }

            return true;
        }

        static private DateTime TransitionTimeToDateTime(Int32 year, TransitionTime transitionTime)
        {
            DateTime value;
            DateTime timeOfDay = transitionTime.TimeOfDay;
            if (transitionTime.IsFixedDateRule)
            {
                Int32 day = DateTime.DaysInMonth(year, transitionTime.Month);
                value = new DateTime(year, transitionTime.Month, (day < transitionTime.Day) ? day : transitionTime.Day, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
            }
            else
            {
                if (transitionTime.Week <= 4)
                {
                    value = new DateTime(year, transitionTime.Month, 1, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = (int)transitionTime.DayOfWeek - dayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }

                    delta += 7 * (transitionTime.Week - 1);
                    if (delta > 0)
                    {
                        value = value.AddDays(delta);
                    }
                }
                else
                {
                    Int32 daysInMonth = DateTime.DaysInMonth(year, transitionTime.Month);
                    value = new DateTime(year, transitionTime.Month, daysInMonth, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = dayOfWeek - (int)transitionTime.DayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }

                    if (delta > 0)
                    {
                        value = value.AddDays(-delta);
                    }
                }
            }

            return value;
        }

        static private bool TryCreateAdjustmentRules(string id, Win32Native.RegistryTimeZoneInformation defaultTimeZoneInformation, out AdjustmentRule[] rules, out Exception e, int defaultBaseUtcOffset)
        {
            e = null;
            try
            {
                using (RegistryKey dynamicKey = Registry.LocalMachine.OpenSubKey(String.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\Dynamic DST", c_timeZonesRegistryHive, id), false))
                {
                    if (dynamicKey == null)
                    {
                        AdjustmentRule rule = CreateAdjustmentRuleFromTimeZoneInformation(defaultTimeZoneInformation, DateTime.MinValue.Date, DateTime.MaxValue.Date, defaultBaseUtcOffset);
                        if (rule == null)
                        {
                            rules = null;
                        }
                        else
                        {
                            rules = new AdjustmentRule[1];
                            rules[0] = rule;
                        }

                        return true;
                    }

                    Int32 first = (Int32)dynamicKey.GetValue(c_firstEntryValue, -1, RegistryValueOptions.None);
                    Int32 last = (Int32)dynamicKey.GetValue(c_lastEntryValue, -1, RegistryValueOptions.None);
                    if (first == -1 || last == -1 || first > last)
                    {
                        rules = null;
                        return false;
                    }

                    Win32Native.RegistryTimeZoneInformation dtzi;
                    Byte[] regValue = dynamicKey.GetValue(first.ToString(CultureInfo.InvariantCulture), null, RegistryValueOptions.None) as Byte[];
                    if (regValue == null || regValue.Length != c_regByteLength)
                    {
                        rules = null;
                        return false;
                    }

                    dtzi = new Win32Native.RegistryTimeZoneInformation(regValue);
                    if (first == last)
                    {
                        AdjustmentRule rule = CreateAdjustmentRuleFromTimeZoneInformation(dtzi, DateTime.MinValue.Date, DateTime.MaxValue.Date, defaultBaseUtcOffset);
                        if (rule == null)
                        {
                            rules = null;
                        }
                        else
                        {
                            rules = new AdjustmentRule[1];
                            rules[0] = rule;
                        }

                        return true;
                    }

                    List<AdjustmentRule> rulesList = new List<AdjustmentRule>(1);
                    AdjustmentRule firstRule = CreateAdjustmentRuleFromTimeZoneInformation(dtzi, DateTime.MinValue.Date, new DateTime(first, 12, 31), defaultBaseUtcOffset);
                    if (firstRule != null)
                    {
                        rulesList.Add(firstRule);
                    }

                    for (Int32 i = first + 1; i < last; i++)
                    {
                        regValue = dynamicKey.GetValue(i.ToString(CultureInfo.InvariantCulture), null, RegistryValueOptions.None) as Byte[];
                        if (regValue == null || regValue.Length != c_regByteLength)
                        {
                            rules = null;
                            return false;
                        }

                        dtzi = new Win32Native.RegistryTimeZoneInformation(regValue);
                        AdjustmentRule middleRule = CreateAdjustmentRuleFromTimeZoneInformation(dtzi, new DateTime(i, 1, 1), new DateTime(i, 12, 31), defaultBaseUtcOffset);
                        if (middleRule != null)
                        {
                            rulesList.Add(middleRule);
                        }
                    }

                    regValue = dynamicKey.GetValue(last.ToString(CultureInfo.InvariantCulture), null, RegistryValueOptions.None) as Byte[];
                    dtzi = new Win32Native.RegistryTimeZoneInformation(regValue);
                    if (regValue == null || regValue.Length != c_regByteLength)
                    {
                        rules = null;
                        return false;
                    }

                    AdjustmentRule lastRule = CreateAdjustmentRuleFromTimeZoneInformation(dtzi, new DateTime(last, 1, 1), DateTime.MaxValue.Date, defaultBaseUtcOffset);
                    if (lastRule != null)
                    {
                        rulesList.Add(lastRule);
                    }

                    rules = rulesList.ToArray();
                    if (rules != null && rules.Length == 0)
                    {
                        rules = null;
                    }
                }
            }
            catch (InvalidCastException ex)
            {
                rules = null;
                e = ex;
                return false;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                rules = null;
                e = ex;
                return false;
            }
            catch (ArgumentException ex)
            {
                rules = null;
                e = ex;
                return false;
            }

            return true;
        }

        static private Boolean TryCompareStandardDate(Win32Native.TimeZoneInformation timeZone, Win32Native.RegistryTimeZoneInformation registryTimeZoneInfo)
        {
            return timeZone.Bias == registryTimeZoneInfo.Bias && timeZone.StandardBias == registryTimeZoneInfo.StandardBias && timeZone.StandardDate.Year == registryTimeZoneInfo.StandardDate.Year && timeZone.StandardDate.Month == registryTimeZoneInfo.StandardDate.Month && timeZone.StandardDate.DayOfWeek == registryTimeZoneInfo.StandardDate.DayOfWeek && timeZone.StandardDate.Day == registryTimeZoneInfo.StandardDate.Day && timeZone.StandardDate.Hour == registryTimeZoneInfo.StandardDate.Hour && timeZone.StandardDate.Minute == registryTimeZoneInfo.StandardDate.Minute && timeZone.StandardDate.Second == registryTimeZoneInfo.StandardDate.Second && timeZone.StandardDate.Milliseconds == registryTimeZoneInfo.StandardDate.Milliseconds;
        }

        static private Boolean TryCompareTimeZoneInformationToRegistry(Win32Native.TimeZoneInformation timeZone, string id, out Boolean dstDisabled)
        {
            dstDisabled = false;
            try
            {
                PermissionSet permSet = new PermissionSet(PermissionState.None);
                permSet.AddPermission(new RegistryPermission(RegistryPermissionAccess.Read, c_timeZonesRegistryHivePermissionList));
                permSet.Assert();
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format(CultureInfo.InvariantCulture, "{0}\\{1}", c_timeZonesRegistryHive, id), false))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    Win32Native.RegistryTimeZoneInformation registryTimeZoneInfo;
                    Byte[] regValue = (Byte[])key.GetValue(c_timeZoneInfoValue, null, RegistryValueOptions.None) as Byte[];
                    if (regValue == null || regValue.Length != c_regByteLength)
                        return false;
                    registryTimeZoneInfo = new Win32Native.RegistryTimeZoneInformation(regValue);
                    Boolean result = TryCompareStandardDate(timeZone, registryTimeZoneInfo);
                    if (!result)
                    {
                        return false;
                    }

                    result = dstDisabled || CheckDaylightSavingTimeNotSupported(timeZone) || (timeZone.DaylightBias == registryTimeZoneInfo.DaylightBias && timeZone.DaylightDate.Year == registryTimeZoneInfo.DaylightDate.Year && timeZone.DaylightDate.Month == registryTimeZoneInfo.DaylightDate.Month && timeZone.DaylightDate.DayOfWeek == registryTimeZoneInfo.DaylightDate.DayOfWeek && timeZone.DaylightDate.Day == registryTimeZoneInfo.DaylightDate.Day && timeZone.DaylightDate.Hour == registryTimeZoneInfo.DaylightDate.Hour && timeZone.DaylightDate.Minute == registryTimeZoneInfo.DaylightDate.Minute && timeZone.DaylightDate.Second == registryTimeZoneInfo.DaylightDate.Second && timeZone.DaylightDate.Milliseconds == registryTimeZoneInfo.DaylightDate.Milliseconds);
                    if (result)
                    {
                        String registryStandardName = key.GetValue(c_standardValue, String.Empty, RegistryValueOptions.None) as String;
                        result = String.Compare(registryStandardName, timeZone.StandardName, StringComparison.Ordinal) == 0;
                    }

                    return result;
                }
            }
            finally
            {
                PermissionSet.RevertAssert();
            }
        }

        static private string TryGetLocalizedNameByMuiNativeResource(string resource)
        {
            if (String.IsNullOrEmpty(resource))
            {
                return String.Empty;
            }

            string[] resources = resource.Split(new char[]{','}, StringSplitOptions.None);
            if (resources.Length != 2)
            {
                return String.Empty;
            }

            string filePath;
            int resourceId;
            string system32 = Environment.UnsafeGetFolderPath(Environment.SpecialFolder.System);
            string tzresDll = resources[0].TrimStart(new char[]{'@'});
            try
            {
                filePath = Path.Combine(system32, tzresDll);
            }
            catch (ArgumentException)
            {
                return String.Empty;
            }

            if (!Int32.TryParse(resources[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out resourceId))
            {
                return String.Empty;
            }

            resourceId = -resourceId;
            try
            {
                StringBuilder fileMuiPath = StringBuilderCache.Acquire(Path.MaxPath);
                fileMuiPath.Length = Path.MaxPath;
                int fileMuiPathLength = Path.MaxPath;
                int languageLength = 0;
                Int64 enumerator = 0;
                Boolean succeeded = UnsafeNativeMethods.GetFileMUIPath(Win32Native.MUI_PREFERRED_UI_LANGUAGES, filePath, null, ref languageLength, fileMuiPath, ref fileMuiPathLength, ref enumerator);
                if (!succeeded)
                {
                    StringBuilderCache.Release(fileMuiPath);
                    return String.Empty;
                }

                return TryGetLocalizedNameByNativeResource(StringBuilderCache.GetStringAndRelease(fileMuiPath), resourceId);
            }
            catch (EntryPointNotFoundException)
            {
                return String.Empty;
            }
        }

        static private string TryGetLocalizedNameByNativeResource(string filePath, int resource)
        {
            using (SafeLibraryHandle handle = UnsafeNativeMethods.LoadLibraryEx(filePath, IntPtr.Zero, Win32Native.LOAD_LIBRARY_AS_DATAFILE))
            {
                if (!handle.IsInvalid)
                {
                    StringBuilder localizedResource = StringBuilderCache.Acquire(Win32Native.LOAD_STRING_MAX_LENGTH);
                    localizedResource.Length = Win32Native.LOAD_STRING_MAX_LENGTH;
                    int result = UnsafeNativeMethods.LoadString(handle, resource, localizedResource, localizedResource.Length);
                    if (result != 0)
                    {
                        return StringBuilderCache.GetStringAndRelease(localizedResource);
                    }
                }
            }

            return String.Empty;
        }

        static private Boolean TryGetLocalizedNamesByRegistryKey(RegistryKey key, out String displayName, out String standardName, out String daylightName)
        {
            displayName = String.Empty;
            standardName = String.Empty;
            daylightName = String.Empty;
            String displayNameMuiResource = key.GetValue(c_muiDisplayValue, String.Empty, RegistryValueOptions.None) as String;
            String standardNameMuiResource = key.GetValue(c_muiStandardValue, String.Empty, RegistryValueOptions.None) as String;
            String daylightNameMuiResource = key.GetValue(c_muiDaylightValue, String.Empty, RegistryValueOptions.None) as String;
            if (!String.IsNullOrEmpty(displayNameMuiResource))
            {
                displayName = TryGetLocalizedNameByMuiNativeResource(displayNameMuiResource);
            }

            if (!String.IsNullOrEmpty(standardNameMuiResource))
            {
                standardName = TryGetLocalizedNameByMuiNativeResource(standardNameMuiResource);
            }

            if (!String.IsNullOrEmpty(daylightNameMuiResource))
            {
                daylightName = TryGetLocalizedNameByMuiNativeResource(daylightNameMuiResource);
            }

            if (String.IsNullOrEmpty(displayName))
            {
                displayName = key.GetValue(c_displayValue, String.Empty, RegistryValueOptions.None) as String;
            }

            if (String.IsNullOrEmpty(standardName))
            {
                standardName = key.GetValue(c_standardValue, String.Empty, RegistryValueOptions.None) as String;
            }

            if (String.IsNullOrEmpty(daylightName))
            {
                daylightName = key.GetValue(c_daylightValue, String.Empty, RegistryValueOptions.None) as String;
            }

            return true;
        }

        static private TimeZoneInfoResult TryGetTimeZoneByRegistryKey(string id, out TimeZoneInfo value, out Exception e)
        {
            e = null;
            try
            {
                PermissionSet permSet = new PermissionSet(PermissionState.None);
                permSet.AddPermission(new RegistryPermission(RegistryPermissionAccess.Read, c_timeZonesRegistryHivePermissionList));
                permSet.Assert();
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(String.Format(CultureInfo.InvariantCulture, "{0}\\{1}", c_timeZonesRegistryHive, id), false))
                {
                    if (key == null)
                    {
                        value = null;
                        return TimeZoneInfoResult.TimeZoneNotFoundException;
                    }

                    Win32Native.RegistryTimeZoneInformation defaultTimeZoneInformation;
                    Byte[] regValue = key.GetValue(c_timeZoneInfoValue, null, RegistryValueOptions.None) as Byte[];
                    if (regValue == null || regValue.Length != c_regByteLength)
                    {
                        value = null;
                        return TimeZoneInfoResult.InvalidTimeZoneException;
                    }

                    defaultTimeZoneInformation = new Win32Native.RegistryTimeZoneInformation(regValue);
                    AdjustmentRule[] adjustmentRules;
                    if (!TryCreateAdjustmentRules(id, defaultTimeZoneInformation, out adjustmentRules, out e, defaultTimeZoneInformation.Bias))
                    {
                        value = null;
                        return TimeZoneInfoResult.InvalidTimeZoneException;
                    }

                    string displayName;
                    string standardName;
                    string daylightName;
                    if (!TryGetLocalizedNamesByRegistryKey(key, out displayName, out standardName, out daylightName))
                    {
                        value = null;
                        return TimeZoneInfoResult.InvalidTimeZoneException;
                    }

                    try
                    {
                        value = new TimeZoneInfo(id, new TimeSpan(0, -(defaultTimeZoneInformation.Bias), 0), displayName, standardName, daylightName, adjustmentRules, false);
                        return TimeZoneInfoResult.Success;
                    }
                    catch (ArgumentException ex)
                    {
                        value = null;
                        e = ex;
                        return TimeZoneInfoResult.InvalidTimeZoneException;
                    }
                    catch (InvalidTimeZoneException ex)
                    {
                        value = null;
                        e = ex;
                        return TimeZoneInfoResult.InvalidTimeZoneException;
                    }
                }
            }
            finally
            {
                PermissionSet.RevertAssert();
            }
        }

        static private TimeZoneInfoResult TryGetTimeZone(string id, Boolean dstDisabled, out TimeZoneInfo value, out Exception e, CachedData cachedData)
        {
            TimeZoneInfoResult result = TimeZoneInfoResult.Success;
            e = null;
            TimeZoneInfo match = null;
            if (cachedData.m_systemTimeZones != null)
            {
                if (cachedData.m_systemTimeZones.TryGetValue(id, out match))
                {
                    if (dstDisabled && match.m_supportsDaylightSavingTime)
                    {
                        value = CreateCustomTimeZone(match.m_id, match.m_baseUtcOffset, match.m_displayName, match.m_standardDisplayName);
                    }
                    else
                    {
                        value = new TimeZoneInfo(match.m_id, match.m_baseUtcOffset, match.m_displayName, match.m_standardDisplayName, match.m_daylightDisplayName, match.m_adjustmentRules, false);
                    }

                    return result;
                }
            }

            if (!cachedData.m_allSystemTimeZonesRead)
            {
                result = TryGetTimeZoneFromLocalMachine(id, dstDisabled, out value, out e, cachedData);
            }
            else
            {
                result = TimeZoneInfoResult.TimeZoneNotFoundException;
                value = null;
            }

            return result;
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachine(string id, bool dstDisabled, out TimeZoneInfo value, out Exception e, CachedData cachedData)
        {
            TimeZoneInfoResult result;
            TimeZoneInfo match;
            result = TryGetTimeZoneByRegistryKey(id, out match, out e);
            if (result == TimeZoneInfoResult.Success)
            {
                if (cachedData.m_systemTimeZones == null)
                    cachedData.m_systemTimeZones = new Dictionary<string, TimeZoneInfo>();
                cachedData.m_systemTimeZones.Add(id, match);
                if (dstDisabled && match.m_supportsDaylightSavingTime)
                {
                    value = CreateCustomTimeZone(match.m_id, match.m_baseUtcOffset, match.m_displayName, match.m_standardDisplayName);
                }
                else
                {
                    value = new TimeZoneInfo(match.m_id, match.m_baseUtcOffset, match.m_displayName, match.m_standardDisplayName, match.m_daylightDisplayName, match.m_adjustmentRules, false);
                }
            }
            else
            {
                value = null;
            }

            return result;
        }

        static internal Boolean UtcOffsetOutOfRange(TimeSpan offset)
        {
            return (offset.TotalHours < -14.0 || offset.TotalHours > 14.0);
        }

        static private void ValidateTimeZoneInfo(String id, TimeSpan baseUtcOffset, AdjustmentRule[] adjustmentRules, out Boolean adjustmentRulesSupportDst)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (id.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidId", id), "id");
            }

            if (UtcOffsetOutOfRange(baseUtcOffset))
            {
                throw new ArgumentOutOfRangeException("baseUtcOffset", Environment.GetResourceString("ArgumentOutOfRange_UtcOffset"));
            }

            if (baseUtcOffset.Ticks % TimeSpan.TicksPerMinute != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_TimeSpanHasSeconds"), "baseUtcOffset");
            }

                        adjustmentRulesSupportDst = false;
            if (adjustmentRules != null && adjustmentRules.Length != 0)
            {
                adjustmentRulesSupportDst = true;
                AdjustmentRule prev = null;
                AdjustmentRule current = null;
                for (int i = 0; i < adjustmentRules.Length; i++)
                {
                    prev = current;
                    current = adjustmentRules[i];
                    if (current == null)
                    {
                        throw new InvalidTimeZoneException(Environment.GetResourceString("Argument_AdjustmentRulesNoNulls"));
                    }

                    if (UtcOffsetOutOfRange(baseUtcOffset + current.DaylightDelta))
                    {
                        throw new InvalidTimeZoneException(Environment.GetResourceString("ArgumentOutOfRange_UtcOffsetAndDaylightDelta"));
                    }

                    if (prev != null && current.DateStart <= prev.DateEnd)
                    {
                        throw new InvalidTimeZoneException(Environment.GetResourceString("Argument_AdjustmentRulesOutOfOrder"));
                    }
                }
            }
        }

        sealed public class AdjustmentRule : IEquatable<AdjustmentRule>, ISerializable, IDeserializationCallback
        {
            private DateTime m_dateStart;
            private DateTime m_dateEnd;
            private TimeSpan m_daylightDelta;
            private TransitionTime m_daylightTransitionStart;
            private TransitionTime m_daylightTransitionEnd;
            private TimeSpan m_baseUtcOffsetDelta;
            private bool m_noDaylightTransitions;
            public DateTime DateStart
            {
                get
                {
                    return this.m_dateStart;
                }
            }

            public DateTime DateEnd
            {
                get
                {
                    return this.m_dateEnd;
                }
            }

            public TimeSpan DaylightDelta
            {
                get
                {
                    return this.m_daylightDelta;
                }
            }

            public TransitionTime DaylightTransitionStart
            {
                get
                {
                    return this.m_daylightTransitionStart;
                }
            }

            public TransitionTime DaylightTransitionEnd
            {
                get
                {
                    return this.m_daylightTransitionEnd;
                }
            }

            internal TimeSpan BaseUtcOffsetDelta
            {
                get
                {
                    return this.m_baseUtcOffsetDelta;
                }
            }

            internal bool NoDaylightTransitions
            {
                get
                {
                    return this.m_noDaylightTransitions;
                }
            }

            internal bool HasDaylightSaving
            {
                get
                {
                    return this.DaylightDelta != TimeSpan.Zero || (this.DaylightTransitionStart != default (TransitionTime) && this.DaylightTransitionStart.TimeOfDay != DateTime.MinValue) || (this.DaylightTransitionEnd != default (TransitionTime) && this.DaylightTransitionEnd.TimeOfDay != DateTime.MinValue.AddMilliseconds(1));
                }
            }

            public bool Equals(AdjustmentRule other)
            {
                bool equals = (other != null && this.m_dateStart == other.m_dateStart && this.m_dateEnd == other.m_dateEnd && this.m_daylightDelta == other.m_daylightDelta && this.m_baseUtcOffsetDelta == other.m_baseUtcOffsetDelta);
                equals = equals && this.m_daylightTransitionEnd.Equals(other.m_daylightTransitionEnd) && this.m_daylightTransitionStart.Equals(other.m_daylightTransitionStart);
                return equals;
            }

            public override int GetHashCode()
            {
                return m_dateStart.GetHashCode();
            }

            private AdjustmentRule()
            {
            }

            static internal AdjustmentRule CreateAdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd, bool noDaylightTransitions)
            {
                ValidateAdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd, noDaylightTransitions);
                AdjustmentRule rule = new AdjustmentRule();
                rule.m_dateStart = dateStart;
                rule.m_dateEnd = dateEnd;
                rule.m_daylightDelta = daylightDelta;
                rule.m_daylightTransitionStart = daylightTransitionStart;
                rule.m_daylightTransitionEnd = daylightTransitionEnd;
                rule.m_baseUtcOffsetDelta = TimeSpan.Zero;
                rule.m_noDaylightTransitions = noDaylightTransitions;
                return rule;
            }

            static public AdjustmentRule CreateAdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd)
            {
                return CreateAdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd, noDaylightTransitions: false);
            }

            static internal AdjustmentRule CreateAdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd, TimeSpan baseUtcOffsetDelta, bool noDaylightTransitions)
            {
                AdjustmentRule rule = CreateAdjustmentRule(dateStart, dateEnd, daylightDelta, daylightTransitionStart, daylightTransitionEnd, noDaylightTransitions);
                rule.m_baseUtcOffsetDelta = baseUtcOffsetDelta;
                return rule;
            }

            internal bool IsStartDateMarkerForBeginningOfYear()
            {
                return !NoDaylightTransitions && DaylightTransitionStart.Month == 1 && DaylightTransitionStart.Day == 1 && DaylightTransitionStart.TimeOfDay.Hour == 0 && DaylightTransitionStart.TimeOfDay.Minute == 0 && DaylightTransitionStart.TimeOfDay.Second == 0 && m_dateStart.Year == m_dateEnd.Year;
            }

            internal bool IsEndDateMarkerForEndOfYear()
            {
                return !NoDaylightTransitions && DaylightTransitionEnd.Month == 1 && DaylightTransitionEnd.Day == 1 && DaylightTransitionEnd.TimeOfDay.Hour == 0 && DaylightTransitionEnd.TimeOfDay.Minute == 0 && DaylightTransitionEnd.TimeOfDay.Second == 0 && m_dateStart.Year == m_dateEnd.Year;
            }

            static private void ValidateAdjustmentRule(DateTime dateStart, DateTime dateEnd, TimeSpan daylightDelta, TransitionTime daylightTransitionStart, TransitionTime daylightTransitionEnd, bool noDaylightTransitions)
            {
                if (dateStart.Kind != DateTimeKind.Unspecified && dateStart.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeKindMustBeUnspecifiedOrUtc"), "dateStart");
                }

                if (dateEnd.Kind != DateTimeKind.Unspecified && dateEnd.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeKindMustBeUnspecifiedOrUtc"), "dateEnd");
                }

                if (daylightTransitionStart.Equals(daylightTransitionEnd) && !noDaylightTransitions)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_TransitionTimesAreIdentical"), "daylightTransitionEnd");
                }

                if (dateStart > dateEnd)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_OutOfOrderDateTimes"), "dateStart");
                }

                if (daylightDelta.TotalHours < -23.0 || daylightDelta.TotalHours > 14.0)
                {
                    throw new ArgumentOutOfRangeException("daylightDelta", daylightDelta, Environment.GetResourceString("ArgumentOutOfRange_UtcOffset"));
                }

                if (daylightDelta.Ticks % TimeSpan.TicksPerMinute != 0)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_TimeSpanHasSeconds"), "daylightDelta");
                }

                if (dateStart != DateTime.MinValue && dateStart.Kind == DateTimeKind.Unspecified && dateStart.TimeOfDay != TimeSpan.Zero)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeHasTimeOfDay"), "dateStart");
                }

                if (dateEnd != DateTime.MaxValue && dateEnd.Kind == DateTimeKind.Unspecified && dateEnd.TimeOfDay != TimeSpan.Zero)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeHasTimeOfDay"), "dateEnd");
                }

                            }
        }

        public struct TransitionTime : IEquatable<TransitionTime>, ISerializable, IDeserializationCallback
        {
            private DateTime m_timeOfDay;
            private byte m_month;
            private byte m_week;
            private byte m_day;
            private DayOfWeek m_dayOfWeek;
            private Boolean m_isFixedDateRule;
            public DateTime TimeOfDay
            {
                get
                {
                    return m_timeOfDay;
                }
            }

            public Int32 Month
            {
                get
                {
                    return (int)m_month;
                }
            }

            public Int32 Week
            {
                get
                {
                    return (int)m_week;
                }
            }

            public Int32 Day
            {
                get
                {
                    return (int)m_day;
                }
            }

            public DayOfWeek DayOfWeek
            {
                get
                {
                    return m_dayOfWeek;
                }
            }

            public Boolean IsFixedDateRule
            {
                get
                {
                    return m_isFixedDateRule;
                }
            }

            public override bool Equals(Object obj)
            {
                if (obj is TransitionTime)
                {
                    return Equals((TransitionTime)obj);
                }

                return false;
            }

            public static bool operator ==(TransitionTime t1, TransitionTime t2)
            {
                return t1.Equals(t2);
            }

            public static bool operator !=(TransitionTime t1, TransitionTime t2)
            {
                return (!t1.Equals(t2));
            }

            public bool Equals(TransitionTime other)
            {
                bool equal = (this.m_isFixedDateRule == other.m_isFixedDateRule && this.m_timeOfDay == other.m_timeOfDay && this.m_month == other.m_month);
                if (equal)
                {
                    if (other.m_isFixedDateRule)
                    {
                        equal = (this.m_day == other.m_day);
                    }
                    else
                    {
                        equal = (this.m_week == other.m_week && this.m_dayOfWeek == other.m_dayOfWeek);
                    }
                }

                return equal;
            }

            public override int GetHashCode()
            {
                return ((int)m_month ^ (int)m_week << 8);
            }

            static public TransitionTime CreateFixedDateRule(DateTime timeOfDay, Int32 month, Int32 day)
            {
                return CreateTransitionTime(timeOfDay, month, 1, day, DayOfWeek.Sunday, true);
            }

            static public TransitionTime CreateFloatingDateRule(DateTime timeOfDay, Int32 month, Int32 week, DayOfWeek dayOfWeek)
            {
                return CreateTransitionTime(timeOfDay, month, week, 1, dayOfWeek, false);
            }

            static private TransitionTime CreateTransitionTime(DateTime timeOfDay, Int32 month, Int32 week, Int32 day, DayOfWeek dayOfWeek, Boolean isFixedDateRule)
            {
                ValidateTransitionTime(timeOfDay, month, week, day, dayOfWeek);
                TransitionTime t = new TransitionTime();
                t.m_isFixedDateRule = isFixedDateRule;
                t.m_timeOfDay = timeOfDay;
                t.m_dayOfWeek = dayOfWeek;
                t.m_day = (byte)day;
                t.m_week = (byte)week;
                t.m_month = (byte)month;
                return t;
            }

            static private void ValidateTransitionTime(DateTime timeOfDay, Int32 month, Int32 week, Int32 day, DayOfWeek dayOfWeek)
            {
                if (timeOfDay.Kind != DateTimeKind.Unspecified)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeKindMustBeUnspecified"), "timeOfDay");
                }

                if (month < 1 || month > 12)
                {
                    throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_MonthParam"));
                }

                if (day < 1 || day > 31)
                {
                    throw new ArgumentOutOfRangeException("day", Environment.GetResourceString("ArgumentOutOfRange_DayParam"));
                }

                if (week < 1 || week > 5)
                {
                    throw new ArgumentOutOfRangeException("week", Environment.GetResourceString("ArgumentOutOfRange_Week"));
                }

                if ((int)dayOfWeek < 0 || (int)dayOfWeek > 6)
                {
                    throw new ArgumentOutOfRangeException("dayOfWeek", Environment.GetResourceString("ArgumentOutOfRange_DayOfWeek"));
                }

                                if (timeOfDay.Year != 1 || timeOfDay.Month != 1 || timeOfDay.Day != 1 || (timeOfDay.Ticks % TimeSpan.TicksPerMillisecond != 0))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeHasTicks"), "timeOfDay");
                }
            }
        }

        sealed private class StringSerializer
        {
            private enum State
            {
                Escaped = 0,
                NotEscaped = 1,
                StartOfToken = 2,
                EndOfLine = 3
            }

            private String m_serializedText;
            private int m_currentTokenStartIndex;
            private State m_state;
            private const int initialCapacityForString = 64;
            private const char esc = '\\';
            private const char sep = ';';
            private const char lhs = '[';
            private const char rhs = ']';
            private const string escString = "\\";
            private const string sepString = ";";
            private const string lhsString = "[";
            private const string rhsString = "]";
            private const string escapedEsc = "\\\\";
            private const string escapedSep = "\\;";
            private const string escapedLhs = "\\[";
            private const string escapedRhs = "\\]";
            private const string dateTimeFormat = "MM:dd:yyyy";
            private const string timeOfDayFormat = "HH:mm:ss.FFF";
            static public String GetSerializedString(TimeZoneInfo zone)
            {
                StringBuilder serializedText = StringBuilderCache.Acquire();
                serializedText.Append(SerializeSubstitute(zone.Id));
                serializedText.Append(sep);
                serializedText.Append(SerializeSubstitute(zone.BaseUtcOffset.TotalMinutes.ToString(CultureInfo.InvariantCulture)));
                serializedText.Append(sep);
                serializedText.Append(SerializeSubstitute(zone.DisplayName));
                serializedText.Append(sep);
                serializedText.Append(SerializeSubstitute(zone.StandardName));
                serializedText.Append(sep);
                serializedText.Append(SerializeSubstitute(zone.DaylightName));
                serializedText.Append(sep);
                AdjustmentRule[] rules = zone.GetAdjustmentRules();
                if (rules != null && rules.Length > 0)
                {
                    for (int i = 0; i < rules.Length; i++)
                    {
                        AdjustmentRule rule = rules[i];
                        serializedText.Append(lhs);
                        serializedText.Append(SerializeSubstitute(rule.DateStart.ToString(dateTimeFormat, DateTimeFormatInfo.InvariantInfo)));
                        serializedText.Append(sep);
                        serializedText.Append(SerializeSubstitute(rule.DateEnd.ToString(dateTimeFormat, DateTimeFormatInfo.InvariantInfo)));
                        serializedText.Append(sep);
                        serializedText.Append(SerializeSubstitute(rule.DaylightDelta.TotalMinutes.ToString(CultureInfo.InvariantCulture)));
                        serializedText.Append(sep);
                        SerializeTransitionTime(rule.DaylightTransitionStart, serializedText);
                        serializedText.Append(sep);
                        SerializeTransitionTime(rule.DaylightTransitionEnd, serializedText);
                        serializedText.Append(sep);
                        if (rule.BaseUtcOffsetDelta != TimeSpan.Zero)
                        {
                            serializedText.Append(SerializeSubstitute(rule.BaseUtcOffsetDelta.TotalMinutes.ToString(CultureInfo.InvariantCulture)));
                            serializedText.Append(sep);
                        }

                        if (rule.NoDaylightTransitions)
                        {
                            serializedText.Append(SerializeSubstitute("1"));
                            serializedText.Append(sep);
                        }

                        serializedText.Append(rhs);
                    }
                }

                serializedText.Append(sep);
                return StringBuilderCache.GetStringAndRelease(serializedText);
            }

            static public TimeZoneInfo GetDeserializedTimeZoneInfo(String source)
            {
                StringSerializer s = new StringSerializer(source);
                String id = s.GetNextStringValue(false);
                TimeSpan baseUtcOffset = s.GetNextTimeSpanValue(false);
                String displayName = s.GetNextStringValue(false);
                String standardName = s.GetNextStringValue(false);
                String daylightName = s.GetNextStringValue(false);
                AdjustmentRule[] rules = s.GetNextAdjustmentRuleArrayValue(false);
                try
                {
                    return TimeZoneInfo.CreateCustomTimeZone(id, baseUtcOffset, displayName, standardName, daylightName, rules);
                }
                catch (ArgumentException ex)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"), ex);
                }
                catch (InvalidTimeZoneException ex)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"), ex);
                }
            }

            private StringSerializer(String str)
            {
                m_serializedText = str;
                m_state = State.StartOfToken;
            }

            static private String SerializeSubstitute(String text)
            {
                text = text.Replace(escString, escapedEsc);
                text = text.Replace(lhsString, escapedLhs);
                text = text.Replace(rhsString, escapedRhs);
                return text.Replace(sepString, escapedSep);
            }

            static private void SerializeTransitionTime(TransitionTime time, StringBuilder serializedText)
            {
                serializedText.Append(lhs);
                Int32 fixedDate = (time.IsFixedDateRule ? 1 : 0);
                serializedText.Append(fixedDate.ToString(CultureInfo.InvariantCulture));
                serializedText.Append(sep);
                if (time.IsFixedDateRule)
                {
                    serializedText.Append(SerializeSubstitute(time.TimeOfDay.ToString(timeOfDayFormat, DateTimeFormatInfo.InvariantInfo)));
                    serializedText.Append(sep);
                    serializedText.Append(SerializeSubstitute(time.Month.ToString(CultureInfo.InvariantCulture)));
                    serializedText.Append(sep);
                    serializedText.Append(SerializeSubstitute(time.Day.ToString(CultureInfo.InvariantCulture)));
                    serializedText.Append(sep);
                }
                else
                {
                    serializedText.Append(SerializeSubstitute(time.TimeOfDay.ToString(timeOfDayFormat, DateTimeFormatInfo.InvariantInfo)));
                    serializedText.Append(sep);
                    serializedText.Append(SerializeSubstitute(time.Month.ToString(CultureInfo.InvariantCulture)));
                    serializedText.Append(sep);
                    serializedText.Append(SerializeSubstitute(time.Week.ToString(CultureInfo.InvariantCulture)));
                    serializedText.Append(sep);
                    serializedText.Append(SerializeSubstitute(((int)time.DayOfWeek).ToString(CultureInfo.InvariantCulture)));
                    serializedText.Append(sep);
                }

                serializedText.Append(rhs);
            }

            static private void VerifyIsEscapableCharacter(char c)
            {
                if (c != esc && c != sep && c != lhs && c != rhs)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidEscapeSequence", c));
                }
            }

            private void SkipVersionNextDataFields(Int32 depth)
            {
                if (m_currentTokenStartIndex < 0 || m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                State tokenState = State.NotEscaped;
                for (int i = m_currentTokenStartIndex; i < m_serializedText.Length; i++)
                {
                    if (tokenState == State.Escaped)
                    {
                        VerifyIsEscapableCharacter(m_serializedText[i]);
                        tokenState = State.NotEscaped;
                    }
                    else if (tokenState == State.NotEscaped)
                    {
                        switch (m_serializedText[i])
                        {
                            case esc:
                                tokenState = State.Escaped;
                                break;
                            case lhs:
                                depth++;
                                break;
                            case rhs:
                                depth--;
                                if (depth == 0)
                                {
                                    m_currentTokenStartIndex = i + 1;
                                    if (m_currentTokenStartIndex >= m_serializedText.Length)
                                    {
                                        m_state = State.EndOfLine;
                                    }
                                    else
                                    {
                                        m_state = State.StartOfToken;
                                    }

                                    return;
                                }

                                break;
                            case '\0':
                                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                            default:
                                break;
                        }
                    }
                }

                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
            }

            private String GetNextStringValue(Boolean canEndWithoutSeparator)
            {
                if (m_state == State.EndOfLine)
                {
                    if (canEndWithoutSeparator)
                    {
                        return null;
                    }
                    else
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                    }
                }

                if (m_currentTokenStartIndex < 0 || m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                State tokenState = State.NotEscaped;
                StringBuilder token = StringBuilderCache.Acquire(initialCapacityForString);
                for (int i = m_currentTokenStartIndex; i < m_serializedText.Length; i++)
                {
                    if (tokenState == State.Escaped)
                    {
                        VerifyIsEscapableCharacter(m_serializedText[i]);
                        token.Append(m_serializedText[i]);
                        tokenState = State.NotEscaped;
                    }
                    else if (tokenState == State.NotEscaped)
                    {
                        switch (m_serializedText[i])
                        {
                            case esc:
                                tokenState = State.Escaped;
                                break;
                            case lhs:
                                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                            case rhs:
                                if (canEndWithoutSeparator)
                                {
                                    m_currentTokenStartIndex = i;
                                    m_state = State.StartOfToken;
                                    return token.ToString();
                                }
                                else
                                {
                                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                                }

                            case sep:
                                m_currentTokenStartIndex = i + 1;
                                if (m_currentTokenStartIndex >= m_serializedText.Length)
                                {
                                    m_state = State.EndOfLine;
                                }
                                else
                                {
                                    m_state = State.StartOfToken;
                                }

                                return StringBuilderCache.GetStringAndRelease(token);
                            case '\0':
                                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                            default:
                                token.Append(m_serializedText[i]);
                                break;
                        }
                    }
                }

                if (tokenState == State.Escaped)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidEscapeSequence", String.Empty));
                }

                if (!canEndWithoutSeparator)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                m_currentTokenStartIndex = m_serializedText.Length;
                m_state = State.EndOfLine;
                return StringBuilderCache.GetStringAndRelease(token);
            }

            private DateTime GetNextDateTimeValue(Boolean canEndWithoutSeparator, string format)
            {
                String token = GetNextStringValue(canEndWithoutSeparator);
                DateTime time;
                if (!DateTime.TryParseExact(token, format, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out time))
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                return time;
            }

            private TimeSpan GetNextTimeSpanValue(Boolean canEndWithoutSeparator)
            {
                Int32 token = GetNextInt32Value(canEndWithoutSeparator);
                try
                {
                    return new TimeSpan(0, token, 0);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"), e);
                }
            }

            private Int32 GetNextInt32Value(Boolean canEndWithoutSeparator)
            {
                String token = GetNextStringValue(canEndWithoutSeparator);
                Int32 value;
                if (!Int32.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value))
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                return value;
            }

            private AdjustmentRule[] GetNextAdjustmentRuleArrayValue(Boolean canEndWithoutSeparator)
            {
                List<AdjustmentRule> rules = new List<AdjustmentRule>(1);
                int count = 0;
                AdjustmentRule rule = GetNextAdjustmentRuleValue(true);
                while (rule != null)
                {
                    rules.Add(rule);
                    count++;
                    rule = GetNextAdjustmentRuleValue(true);
                }

                if (!canEndWithoutSeparator)
                {
                    if (m_state == State.EndOfLine)
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                    }

                    if (m_currentTokenStartIndex < 0 || m_currentTokenStartIndex >= m_serializedText.Length)
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                    }
                }

                return (count != 0 ? rules.ToArray() : null);
            }

            private AdjustmentRule GetNextAdjustmentRuleValue(Boolean canEndWithoutSeparator)
            {
                if (m_state == State.EndOfLine)
                {
                    if (canEndWithoutSeparator)
                    {
                        return null;
                    }
                    else
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                    }
                }

                if (m_currentTokenStartIndex < 0 || m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                if (m_serializedText[m_currentTokenStartIndex] == sep)
                {
                    return null;
                }

                if (m_serializedText[m_currentTokenStartIndex] != lhs)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                m_currentTokenStartIndex++;
                DateTime dateStart = GetNextDateTimeValue(false, dateTimeFormat);
                DateTime dateEnd = GetNextDateTimeValue(false, dateTimeFormat);
                TimeSpan daylightDelta = GetNextTimeSpanValue(false);
                TransitionTime daylightStart = GetNextTransitionTimeValue(false);
                TransitionTime daylightEnd = GetNextTransitionTimeValue(false);
                TimeSpan baseUtcOffsetDelta = TimeSpan.Zero;
                Int32 noDaylightTransitions = 0;
                if (m_state == State.EndOfLine || m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                if ((m_serializedText[m_currentTokenStartIndex] >= '0' && m_serializedText[m_currentTokenStartIndex] <= '9') || m_serializedText[m_currentTokenStartIndex] == '-' || m_serializedText[m_currentTokenStartIndex] == '+')
                {
                    baseUtcOffsetDelta = GetNextTimeSpanValue(false);
                }

                if ((m_serializedText[m_currentTokenStartIndex] >= '0' && m_serializedText[m_currentTokenStartIndex] <= '1'))
                {
                    noDaylightTransitions = GetNextInt32Value(false);
                }

                if (m_state == State.EndOfLine || m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                if (m_serializedText[m_currentTokenStartIndex] != rhs)
                {
                    SkipVersionNextDataFields(1);
                }
                else
                {
                    m_currentTokenStartIndex++;
                }

                AdjustmentRule rule;
                try
                {
                    rule = AdjustmentRule.CreateAdjustmentRule(dateStart, dateEnd, daylightDelta, daylightStart, daylightEnd, baseUtcOffsetDelta, noDaylightTransitions > 0);
                }
                catch (ArgumentException e)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"), e);
                }

                if (m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    m_state = State.EndOfLine;
                }
                else
                {
                    m_state = State.StartOfToken;
                }

                return rule;
            }

            private TransitionTime GetNextTransitionTimeValue(Boolean canEndWithoutSeparator)
            {
                if (m_state == State.EndOfLine || (m_currentTokenStartIndex < m_serializedText.Length && m_serializedText[m_currentTokenStartIndex] == rhs))
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                if (m_currentTokenStartIndex < 0 || m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                if (m_serializedText[m_currentTokenStartIndex] != lhs)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                m_currentTokenStartIndex++;
                Int32 isFixedDate = GetNextInt32Value(false);
                if (isFixedDate != 0 && isFixedDate != 1)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                TransitionTime transition;
                DateTime timeOfDay = GetNextDateTimeValue(false, timeOfDayFormat);
                timeOfDay = new DateTime(1, 1, 1, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
                Int32 month = GetNextInt32Value(false);
                if (isFixedDate == 1)
                {
                    Int32 day = GetNextInt32Value(false);
                    try
                    {
                        transition = TransitionTime.CreateFixedDateRule(timeOfDay, month, day);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"), e);
                    }
                }
                else
                {
                    Int32 week = GetNextInt32Value(false);
                    Int32 dayOfWeek = GetNextInt32Value(false);
                    try
                    {
                        transition = TransitionTime.CreateFloatingDateRule(timeOfDay, month, week, (DayOfWeek)dayOfWeek);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"), e);
                    }
                }

                if (m_state == State.EndOfLine || m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                if (m_serializedText[m_currentTokenStartIndex] != rhs)
                {
                    SkipVersionNextDataFields(1);
                }
                else
                {
                    m_currentTokenStartIndex++;
                }

                Boolean sepFound = false;
                if (m_currentTokenStartIndex < m_serializedText.Length && m_serializedText[m_currentTokenStartIndex] == sep)
                {
                    m_currentTokenStartIndex++;
                    sepFound = true;
                }

                if (!sepFound && !canEndWithoutSeparator)
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"));
                }

                if (m_currentTokenStartIndex >= m_serializedText.Length)
                {
                    m_state = State.EndOfLine;
                }
                else
                {
                    m_state = State.StartOfToken;
                }

                return transition;
            }
        }

        private class TimeZoneInfoComparer : System.Collections.Generic.IComparer<TimeZoneInfo>
        {
            int System.Collections.Generic.IComparer<TimeZoneInfo>.Compare(TimeZoneInfo x, TimeZoneInfo y)
            {
                int comparison = x.BaseUtcOffset.CompareTo(y.BaseUtcOffset);
                return comparison == 0 ? String.Compare(x.DisplayName, y.DisplayName, StringComparison.Ordinal) : comparison;
            }
        }
    }
}