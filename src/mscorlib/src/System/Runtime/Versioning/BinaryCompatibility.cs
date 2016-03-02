
using System.Runtime.CompilerServices;

namespace System.Runtime.Versioning
{
    internal static class BinaryCompatibility
    {
        internal static bool TargetsAtLeast_Phone_V7_1
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Phone_V7_1;
            }
        }

        internal static bool TargetsAtLeast_Phone_V8_0
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Phone_V8_0;
            }
        }

        internal static bool TargetsAtLeast_Desktop_V4_5
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Desktop_V4_5;
            }
        }

        internal static bool TargetsAtLeast_Desktop_V4_5_1
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Desktop_V4_5_1;
            }
        }

        internal static bool TargetsAtLeast_Desktop_V4_5_2
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Desktop_V4_5_2;
            }
        }

        internal static bool TargetsAtLeast_Desktop_V4_5_3
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Desktop_V4_5_3;
            }
        }

        internal static bool TargetsAtLeast_Desktop_V4_5_4
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Desktop_V4_5_4;
            }
        }

        internal static bool TargetsAtLeast_Desktop_V5_0
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Desktop_V5_0;
            }
        }

        internal static bool TargetsAtLeast_Silverlight_V4
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Silverlight_V4;
            }
        }

        internal static bool TargetsAtLeast_Silverlight_V5
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Silverlight_V5;
            }
        }

        internal static bool TargetsAtLeast_Silverlight_V6
        {
            [FriendAccessAllowed]
            get
            {
                return s_map.TargetsAtLeast_Silverlight_V6;
            }
        }

        internal static TargetFrameworkId AppWasBuiltForFramework
        {
            [FriendAccessAllowed]
            get
            {
                                if (s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked)
                    ReadTargetFrameworkId();
                return s_AppWasBuiltForFramework;
            }
        }

        internal static int AppWasBuiltForVersion
        {
            [FriendAccessAllowed]
            get
            {
                                if (s_AppWasBuiltForFramework == TargetFrameworkId.NotYetChecked)
                    ReadTargetFrameworkId();
                                return s_AppWasBuiltForVersion;
            }
        }

        private static TargetFrameworkId s_AppWasBuiltForFramework;
        private static int s_AppWasBuiltForVersion;
        readonly static BinaryCompatibilityMap s_map = new BinaryCompatibilityMap();
        private const char c_componentSeparator = ',';
        private const char c_keyValueSeparator = '=';
        private const char c_versionValuePrefix = 'v';
        private const String c_versionKey = "Version";
        private const String c_profileKey = "Profile";
        private sealed class BinaryCompatibilityMap
        {
            internal bool TargetsAtLeast_Phone_V7_1;
            internal bool TargetsAtLeast_Phone_V8_0;
            internal bool TargetsAtLeast_Phone_V8_1;
            internal bool TargetsAtLeast_Desktop_V4_5;
            internal bool TargetsAtLeast_Desktop_V4_5_1;
            internal bool TargetsAtLeast_Desktop_V4_5_2;
            internal bool TargetsAtLeast_Desktop_V4_5_3;
            internal bool TargetsAtLeast_Desktop_V4_5_4;
            internal bool TargetsAtLeast_Desktop_V5_0;
            internal bool TargetsAtLeast_Silverlight_V4;
            internal bool TargetsAtLeast_Silverlight_V5;
            internal bool TargetsAtLeast_Silverlight_V6;
            internal BinaryCompatibilityMap()
            {
                AddQuirksForFramework(AppWasBuiltForFramework, AppWasBuiltForVersion);
            }

            private void AddQuirksForFramework(TargetFrameworkId builtAgainstFramework, int buildAgainstVersion)
            {
                                switch (builtAgainstFramework)
                {
                    case TargetFrameworkId.NetFramework:
                    case TargetFrameworkId.NetCore:
                        if (buildAgainstVersion >= 50000)
                            TargetsAtLeast_Desktop_V5_0 = true;
                        if (buildAgainstVersion >= 40504)
                            TargetsAtLeast_Desktop_V4_5_4 = true;
                        if (buildAgainstVersion >= 40503)
                            TargetsAtLeast_Desktop_V4_5_3 = true;
                        if (buildAgainstVersion >= 40502)
                            TargetsAtLeast_Desktop_V4_5_2 = true;
                        if (buildAgainstVersion >= 40501)
                            TargetsAtLeast_Desktop_V4_5_1 = true;
                        if (buildAgainstVersion >= 40500)
                        {
                            TargetsAtLeast_Desktop_V4_5 = true;
                            AddQuirksForFramework(TargetFrameworkId.Phone, 70100);
                            AddQuirksForFramework(TargetFrameworkId.Silverlight, 50000);
                        }

                        break;
                    case TargetFrameworkId.Phone:
                        if (buildAgainstVersion >= 80000)
                        {
                            TargetsAtLeast_Phone_V8_0 = true;
                        }

                        if (buildAgainstVersion >= 80100)
                        {
                            TargetsAtLeast_Desktop_V4_5 = true;
                            TargetsAtLeast_Desktop_V4_5_1 = true;
                        }

                        if (buildAgainstVersion >= 710)
                            TargetsAtLeast_Phone_V7_1 = true;
                        break;
                    case TargetFrameworkId.Silverlight:
                        if (buildAgainstVersion >= 40000)
                            TargetsAtLeast_Silverlight_V4 = true;
                        if (buildAgainstVersion >= 50000)
                            TargetsAtLeast_Silverlight_V5 = true;
                        if (buildAgainstVersion >= 60000)
                        {
                            TargetsAtLeast_Silverlight_V6 = true;
                        }

                        break;
                    case TargetFrameworkId.Unspecified:
                        break;
                    case TargetFrameworkId.NotYetChecked:
                    case TargetFrameworkId.Unrecognized:
                                                break;
                    default:
                                                break;
                }
            }
        }

        private static bool ParseTargetFrameworkMonikerIntoEnum(String targetFrameworkMoniker, out TargetFrameworkId targetFramework, out int targetFrameworkVersion)
        {
                        targetFramework = TargetFrameworkId.NotYetChecked;
            targetFrameworkVersion = 0;
            String identifier = null;
            String profile = null;
            ParseFrameworkName(targetFrameworkMoniker, out identifier, out targetFrameworkVersion, out profile);
            switch (identifier)
            {
                case ".NETFramework":
                    targetFramework = TargetFrameworkId.NetFramework;
                    break;
                case ".NETPortable":
                    targetFramework = TargetFrameworkId.Portable;
                    break;
                case ".NETCore":
                    targetFramework = TargetFrameworkId.NetCore;
                    break;
                case "WindowsPhone":
                    if (targetFrameworkVersion >= 80100)
                    {
                        targetFramework = TargetFrameworkId.Phone;
                    }
                    else
                    {
                        targetFramework = TargetFrameworkId.Unspecified;
                    }

                    break;
                case "WindowsPhoneApp":
                    targetFramework = TargetFrameworkId.Phone;
                    break;
                case "Silverlight":
                    targetFramework = TargetFrameworkId.Silverlight;
                    if (!String.IsNullOrEmpty(profile))
                    {
                        if (profile == "WindowsPhone")
                        {
                            targetFramework = TargetFrameworkId.Phone;
                            targetFrameworkVersion = 70000;
                        }
                        else if (profile == "WindowsPhone71")
                        {
                            targetFramework = TargetFrameworkId.Phone;
                            targetFrameworkVersion = 70100;
                        }
                        else if (profile == "WindowsPhone8")
                        {
                            targetFramework = TargetFrameworkId.Phone;
                            targetFrameworkVersion = 80000;
                        }
                        else if (profile.StartsWith("WindowsPhone", StringComparison.Ordinal))
                        {
                                                        targetFramework = TargetFrameworkId.Unrecognized;
                            targetFrameworkVersion = 70100;
                        }
                        else
                        {
                                                        targetFramework = TargetFrameworkId.Unrecognized;
                        }
                    }

                    break;
                default:
                                        targetFramework = TargetFrameworkId.Unrecognized;
                    break;
            }

            return true;
        }

        private static void ParseFrameworkName(String frameworkName, out String identifier, out int version, out String profile)
        {
            if (frameworkName == null)
            {
                throw new ArgumentNullException("frameworkName");
            }

            if (frameworkName.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_StringZeroLength"), "frameworkName");
            }

                        String[] components = frameworkName.Split(c_componentSeparator);
            version = 0;
            if (components.Length < 2 || components.Length > 3)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameTooShort"), "frameworkName");
            }

            identifier = components[0].Trim();
            if (identifier.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameInvalid"), "frameworkName");
            }

            bool versionFound = false;
            profile = null;
            for (int i = 1; i < components.Length; i++)
            {
                string[] keyValuePair = components[i].Split(c_keyValueSeparator);
                if (keyValuePair.Length != 2)
                {
                    throw new ArgumentException(Environment.GetResourceString("SR.Argument_FrameworkNameInvalid"), "frameworkName");
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
                    throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameInvalid"), "frameworkName");
                }
            }

            if (!versionFound)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_FrameworkNameMissingVersion"), "frameworkName");
            }
        }

        private static bool IsAppUnderSL81CompatMode()
        {
                        if (CompatibilitySwitches.IsAppSilverlight81)
            {
                s_AppWasBuiltForFramework = TargetFrameworkId.Phone;
                s_AppWasBuiltForVersion = 80100;
                return true;
            }

            return false;
        }

        private static void ReadTargetFrameworkId()
        {
            if (IsAppUnderSL81CompatMode())
            {
                return;
            }

            String targetFrameworkName = AppDomain.CurrentDomain.GetTargetFrameworkName();
            var overrideValue = System.Runtime.Versioning.CompatibilitySwitch.GetValueInternal("TargetFrameworkMoniker");
            if (!string.IsNullOrEmpty(overrideValue))
            {
                targetFrameworkName = overrideValue;
            }

            TargetFrameworkId fxId;
            int fxVersion = 0;
            if (targetFrameworkName == null)
            {
                if (CompatibilitySwitches.UseLatestBehaviorWhenTFMNotSpecified)
                {
                    fxId = TargetFrameworkId.NetFramework;
                    fxVersion = 50000;
                }
                else
                    fxId = TargetFrameworkId.Unspecified;
            }
            else if (!ParseTargetFrameworkMonikerIntoEnum(targetFrameworkName, out fxId, out fxVersion))
                fxId = TargetFrameworkId.Unrecognized;
            s_AppWasBuiltForFramework = fxId;
            s_AppWasBuiltForVersion = fxVersion;
        }
    }
}