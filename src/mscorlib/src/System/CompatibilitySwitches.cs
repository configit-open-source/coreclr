using System.Runtime;
using System.Runtime.CompilerServices;

namespace System
{
    internal static class CompatibilitySwitches
    {
        private static bool s_AreSwitchesSet;
        private static bool s_isAppEarlierThanWindowsPhone8;
        private static bool s_isAppEarlierThanWindowsPhoneMango;
        private static bool s_isAppSilverlight81;
        private static bool s_useLatestBehaviorWhenTFMNotSpecified;
        public static bool IsCompatibilityBehaviorDefined
        {
            get
            {
                return s_AreSwitchesSet;
            }
        }

        private static bool IsCompatibilitySwitchSet(string compatibilitySwitch)
        {
            bool ? result = AppDomain.CurrentDomain.IsCompatibilitySwitchSet(compatibilitySwitch);
            return (result.HasValue && result.Value);
        }

        internal static void InitializeSwitches()
        {
            s_isAppSilverlight81 = IsCompatibilitySwitchSet("WindowsPhone_5.1.0.0");
            s_useLatestBehaviorWhenTFMNotSpecified = IsCompatibilitySwitchSet("UseLatestBehaviorWhenTFMNotSpecified");
            s_isAppEarlierThanWindowsPhoneMango = IsCompatibilitySwitchSet("WindowsPhone_3.7.0.0");
            s_isAppEarlierThanWindowsPhone8 = s_isAppEarlierThanWindowsPhoneMango || IsCompatibilitySwitchSet("WindowsPhone_3.8.0.0");
            s_AreSwitchesSet = true;
        }

        public static bool IsAppEarlierThanSilverlight4
        {
            get
            {
                return false;
            }
        }

        internal static bool IsAppSilverlight81
        {
            get
            {
                return s_isAppSilverlight81;
            }
        }

        internal static bool UseLatestBehaviorWhenTFMNotSpecified
        {
            get
            {
                return s_useLatestBehaviorWhenTFMNotSpecified;
            }
        }

        public static bool IsAppEarlierThanWindowsPhone8
        {
            get
            {
                return s_isAppEarlierThanWindowsPhone8;
            }
        }

        public static bool IsAppEarlierThanWindowsPhoneMango
        {
            get
            {
                return s_isAppEarlierThanWindowsPhoneMango;
            }
        }

        public static bool IsNetFx40TimeSpanLegacyFormatMode
        {
            get
            {
                return false;
            }
        }

        public static bool IsNetFx40LegacySecurityPolicy
        {
            get
            {
                return false;
            }
        }

        public static bool IsNetFx45LegacyManagedDeflateStream
        {
            get
            {
                return false;
            }
        }
    }
}