using System;

namespace Microsoft.Win32
{
    public static class Registry
    {
        static Registry()
        {
        }

        public static readonly RegistryKey CurrentUser = RegistryKey.GetBaseKey(RegistryKey.HKEY_CURRENT_USER);
        public static readonly RegistryKey LocalMachine = RegistryKey.GetBaseKey(RegistryKey.HKEY_LOCAL_MACHINE);
        public static readonly RegistryKey ClassesRoot = RegistryKey.GetBaseKey(RegistryKey.HKEY_CLASSES_ROOT);
        public static readonly RegistryKey Users = RegistryKey.GetBaseKey(RegistryKey.HKEY_USERS);
        public static readonly RegistryKey PerformanceData = RegistryKey.GetBaseKey(RegistryKey.HKEY_PERFORMANCE_DATA);
        public static readonly RegistryKey CurrentConfig = RegistryKey.GetBaseKey(RegistryKey.HKEY_CURRENT_CONFIG);
        public static readonly RegistryKey DynData = RegistryKey.GetBaseKey(RegistryKey.HKEY_DYN_DATA);
        private static RegistryKey GetBaseKeyFromKeyName(string keyName, out string subKeyName)
        {
            if (keyName == null)
            {
                throw new ArgumentNullException("keyName");
            }

            string basekeyName;
            int i = keyName.IndexOf('\\');
            if (i != -1)
            {
                basekeyName = keyName.Substring(0, i).ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                basekeyName = keyName.ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            }

            RegistryKey basekey = null;
            switch (basekeyName)
            {
                case "HKEY_CURRENT_USER":
                    basekey = Registry.CurrentUser;
                    break;
                case "HKEY_LOCAL_MACHINE":
                    basekey = Registry.LocalMachine;
                    break;
                case "HKEY_CLASSES_ROOT":
                    basekey = Registry.ClassesRoot;
                    break;
                case "HKEY_USERS":
                    basekey = Registry.Users;
                    break;
                case "HKEY_PERFORMANCE_DATA":
                    basekey = Registry.PerformanceData;
                    break;
                case "HKEY_CURRENT_CONFIG":
                    basekey = Registry.CurrentConfig;
                    break;
                case "HKEY_DYN_DATA":
                    basekey = RegistryKey.GetBaseKey(RegistryKey.HKEY_DYN_DATA);
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Arg_RegInvalidKeyName", "keyName"));
            }

            if (i == -1 || i == keyName.Length)
            {
                subKeyName = string.Empty;
            }
            else
            {
                subKeyName = keyName.Substring(i + 1, keyName.Length - i - 1);
            }

            return basekey;
        }

        public static object GetValue(string keyName, string valueName, object defaultValue)
        {
            string subKeyName;
            RegistryKey basekey = GetBaseKeyFromKeyName(keyName, out subKeyName);
            BCLDebug.Assert(basekey != null, "basekey can't be null.");
            RegistryKey key = basekey.OpenSubKey(subKeyName);
            if (key == null)
            {
                return null;
            }

            try
            {
                return key.GetValue(valueName, defaultValue);
            }
            finally
            {
                key.Close();
            }
        }

        public static void SetValue(string keyName, string valueName, object value)
        {
            SetValue(keyName, valueName, value, RegistryValueKind.Unknown);
        }

        public static void SetValue(string keyName, string valueName, object value, RegistryValueKind valueKind)
        {
            string subKeyName;
            RegistryKey basekey = GetBaseKeyFromKeyName(keyName, out subKeyName);
            BCLDebug.Assert(basekey != null, "basekey can't be null!");
            RegistryKey key = basekey.CreateSubKey(subKeyName);
            BCLDebug.Assert(key != null, "An exception should be thrown if failed!");
            try
            {
                key.SetValue(valueName, value, valueKind);
            }
            finally
            {
                key.Close();
            }
        }
    }
}