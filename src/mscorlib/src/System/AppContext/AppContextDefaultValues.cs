using System;
using System.Collections.Generic;

namespace System
{
    internal static partial class AppContextDefaultValues
    {
        public static void PopulateDefaultValues()
        {
            string platformIdentifier, profile;
            int version;
            ParseTargetFrameworkName(out platformIdentifier, out profile, out version);
            PopulateDefaultValuesPartial(platformIdentifier, profile, version);
        }

        private static void ParseTargetFrameworkName(out string identifier, out string profile, out int version)
        {
            if (CompatibilitySwitches.IsAppSilverlight81)
            {
                identifier = "WindowsPhone";
                version = 80100;
                profile = string.Empty;
                return;
            }

            string targetFrameworkMoniker = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
            if (!TryParseFrameworkName(targetFrameworkMoniker, out identifier, out version, out profile))
            {
                if (CompatibilitySwitches.UseLatestBehaviorWhenTFMNotSpecified)
                {
                    identifier = string.Empty;
                }
                else
                {
                    identifier = ".NETFramework";
                    version = 40000;
                    profile = string.Empty;
                }
            }
        }

        private static bool TryParseFrameworkName(String frameworkName, out String identifier, out int version, out String profile)
        {
            const char c_componentSeparator = ',';
            const char c_keyValueSeparator = '=';
            const char c_versionValuePrefix = 'v';
            const String c_versionKey = "Version";
            const String c_profileKey = "Profile";
            identifier = profile = string.Empty;
            version = 0;
            if (frameworkName == null || frameworkName.Length == 0)
            {
                return false;
            }

            String[] components = frameworkName.Split(c_componentSeparator);
            version = 0;
            if (components.Length < 2 || components.Length > 3)
            {
                return false;
            }

            identifier = components[0].Trim();
            if (identifier.Length == 0)
            {
                return false;
            }

            bool versionFound = false;
            profile = null;
            for (int i = 1; i < components.Length; i++)
            {
                string[] keyValuePair = components[i].Split(c_keyValueSeparator);
                if (keyValuePair.Length != 2)
                {
                    return false;
                }

                string key = keyValuePair[0].Trim();
                string value = keyValuePair[1].Trim();
                if (key.Equals(c_versionKey, StringComparison.OrdinalIgnoreCase))
                {
                    versionFound = true;
                    if (value.Length > 0 && (value[0] == c_versionValuePrefix || value[0] == 'V'))
                    {
                        value = value.Substring(1);
                    }

                    Version realVersion = new Version(value);
                    version = realVersion.Major * 10000;
                    if (realVersion.Minor > 0)
                        version += realVersion.Minor * 100;
                    if (realVersion.Build > 0)
                        version += realVersion.Build;
                }
                else if (key.Equals(c_profileKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        profile = value;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (!versionFound)
            {
                return false;
            }

            return true;
        }

        static partial void TryGetSwitchOverridePartial(string switchName, ref bool overrideFound, ref bool overrideValue);
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int version);
    }
}