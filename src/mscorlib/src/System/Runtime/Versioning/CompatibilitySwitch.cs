using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Runtime.Versioning
{
    public static class CompatibilitySwitch
    {
        public static bool IsEnabled(string compatibilitySwitchName)
        {
            return IsEnabledInternalCall(compatibilitySwitchName, true);
        }

        public static string GetValue(string compatibilitySwitchName)
        {
            return GetValueInternalCall(compatibilitySwitchName, true);
        }

        internal static bool IsEnabledInternal(string compatibilitySwitchName)
        {
            return IsEnabledInternalCall(compatibilitySwitchName, false);
        }

        internal static string GetValueInternal(string compatibilitySwitchName)
        {
            return GetValueInternalCall(compatibilitySwitchName, false);
        }

        internal static extern string GetAppContextOverridesInternalCall();
        private static extern bool IsEnabledInternalCall(string compatibilitySwitchName, bool onlyDB);
        private static extern string GetValueInternalCall(string compatibilitySwitchName, bool onlyDB);
    }
}