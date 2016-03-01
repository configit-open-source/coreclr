using System.Collections.Generic;

namespace System
{
    public static class AppContext
    {
        [Flags]
        private enum SwitchValueState
        {
            HasFalseValue = 0x1,
            HasTrueValue = 0x2,
            HasLookedForOverride = 0x4,
            UnknownValue = 0x8
        }

        private static readonly Dictionary<string, SwitchValueState> s_switchMap = new Dictionary<string, SwitchValueState>();
        public static string BaseDirectory
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (string)AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") ?? AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public static string TargetFrameworkName
        {
            get
            {
                return AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
            }
        }

        static AppContext()
        {
            AppContextDefaultValues.PopulateDefaultValues();
        }

        public static bool TryGetSwitch(string switchName, out bool isEnabled)
        {
            if (switchName == null)
                throw new ArgumentNullException("switchName");
            if (switchName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "switchName");
            isEnabled = false;
            SwitchValueState switchValue;
            lock (s_switchMap)
            {
                if (s_switchMap.TryGetValue(switchName, out switchValue))
                {
                    if (switchValue == SwitchValueState.UnknownValue)
                    {
                        isEnabled = false;
                        return false;
                    }

                    isEnabled = (switchValue & SwitchValueState.HasTrueValue) == SwitchValueState.HasTrueValue;
                    if ((switchValue & SwitchValueState.HasLookedForOverride) == SwitchValueState.HasLookedForOverride)
                    {
                        return true;
                    }

                    bool overrideValue;
                    if (AppContextDefaultValues.TryGetSwitchOverride(switchName, out overrideValue))
                    {
                        isEnabled = overrideValue;
                    }

                    s_switchMap[switchName] = (isEnabled ? SwitchValueState.HasTrueValue : SwitchValueState.HasFalseValue) | SwitchValueState.HasLookedForOverride;
                    return true;
                }
                else
                {
                    bool overrideValue;
                    if (AppContextDefaultValues.TryGetSwitchOverride(switchName, out overrideValue))
                    {
                        isEnabled = overrideValue;
                        s_switchMap[switchName] = (isEnabled ? SwitchValueState.HasTrueValue : SwitchValueState.HasFalseValue) | SwitchValueState.HasLookedForOverride;
                        return true;
                    }

                    s_switchMap[switchName] = SwitchValueState.UnknownValue;
                }
            }

            return false;
        }

        public static void SetSwitch(string switchName, bool isEnabled)
        {
            if (switchName == null)
                throw new ArgumentNullException("switchName");
            if (switchName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "switchName");
            SwitchValueState switchValue = (isEnabled ? SwitchValueState.HasTrueValue : SwitchValueState.HasFalseValue) | SwitchValueState.HasLookedForOverride;
            lock (s_switchMap)
            {
                s_switchMap[switchName] = switchValue;
            }
        }

        internal static void DefineSwitchDefault(string switchName, bool isEnabled)
        {
            s_switchMap[switchName] = isEnabled ? SwitchValueState.HasTrueValue : SwitchValueState.HasFalseValue;
        }
    }
}