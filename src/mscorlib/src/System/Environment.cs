using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;

using Microsoft.Win32;

namespace System
{
    public enum EnvironmentVariableTarget
    {
        Process = 0,
        User = 1,
        Machine = 2
    }

    public static class Environment
    {
        const int MaxEnvVariableValueLength = 32767;
        const int MaxSystemEnvVariableLength = 1024;
        const int MaxUserEnvVariableLength = 255;
        internal sealed class ResourceHelper
        {
            internal ResourceHelper(String name)
            {
                m_name = name;
            }

            private String m_name;
            private ResourceManager SystemResMgr;
            private Stack currentlyLoading;
            internal bool resourceManagerInited = false;
            private int infinitelyRecursingCount;
            internal class GetResourceStringUserData
            {
                public ResourceHelper m_resourceHelper;
                public String m_key;
                public CultureInfo m_culture;
                public String m_retVal;
                public bool m_lockWasTaken;
                public GetResourceStringUserData(ResourceHelper resourceHelper, String key, CultureInfo culture)
                {
                    m_resourceHelper = resourceHelper;
                    m_key = key;
                    m_culture = culture;
                }
            }

            internal String GetResourceString(String key)
            {
                if (key == null || key.Length == 0)
                {
                                        return "[Resource lookup failed - null or empty resource name]";
                }

                return GetResourceString(key, null);
            }

            internal String GetResourceString(String key, CultureInfo culture)
            {
                if (key == null || key.Length == 0)
                {
                                        return "[Resource lookup failed - null or empty resource name]";
                }

                GetResourceStringUserData userData = new GetResourceStringUserData(this, key, culture);
                RuntimeHelpers.TryCode tryCode = new RuntimeHelpers.TryCode(GetResourceStringCode);
                RuntimeHelpers.CleanupCode cleanupCode = new RuntimeHelpers.CleanupCode(GetResourceStringBackoutCode);
                RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(tryCode, cleanupCode, userData);
                return userData.m_retVal;
            }

            private void GetResourceStringCode(Object userDataIn)
            {
                GetResourceStringUserData userData = (GetResourceStringUserData)userDataIn;
                ResourceHelper rh = userData.m_resourceHelper;
                String key = userData.m_key;
                CultureInfo culture = userData.m_culture;
                Monitor.Enter(rh, ref userData.m_lockWasTaken);
                if (rh.currentlyLoading != null && rh.currentlyLoading.Count > 0 && rh.currentlyLoading.Contains(key))
                {
                    if (rh.infinitelyRecursingCount > 0)
                    {
                        userData.m_retVal = "[Resource lookup failed - infinite recursion or critical failure detected.]";
                        return;
                    }

                    rh.infinitelyRecursingCount++;
                    String message = "Infinite recursion during resource lookup within mscorlib.  This may be a bug in mscorlib, or potentially in certain extensibility points such as assembly resolve events or CultureInfo names.  Resource name: " + key;
                    Assert.Fail("[mscorlib recursive resource lookup bug]", message, Assert.COR_E_FAILFAST, System.Diagnostics.StackTrace.TraceFormat.NoResourceLookup);
                    Environment.FailFast(message);
                }

                if (rh.currentlyLoading == null)
                    rh.currentlyLoading = new Stack(4);
                if (!rh.resourceManagerInited)
                {
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                    }
                    finally
                    {
                        RuntimeHelpers.RunClassConstructor(typeof (ResourceManager).TypeHandle);
                        RuntimeHelpers.RunClassConstructor(typeof (ResourceReader).TypeHandle);
                        RuntimeHelpers.RunClassConstructor(typeof (RuntimeResourceSet).TypeHandle);
                        RuntimeHelpers.RunClassConstructor(typeof (BinaryReader).TypeHandle);
                        rh.resourceManagerInited = true;
                    }
                }

                rh.currentlyLoading.Push(key);
                if (rh.SystemResMgr == null)
                {
                    rh.SystemResMgr = new ResourceManager(m_name, typeof (Object).Assembly);
                }

                String s = rh.SystemResMgr.GetString(key, null);
                rh.currentlyLoading.Pop();
                                userData.m_retVal = s;
            }

            private void GetResourceStringBackoutCode(Object userDataIn, bool exceptionThrown)
            {
                GetResourceStringUserData userData = (GetResourceStringUserData)userDataIn;
                ResourceHelper rh = userData.m_resourceHelper;
                if (exceptionThrown)
                {
                    if (userData.m_lockWasTaken)
                    {
                        rh.SystemResMgr = null;
                        rh.currentlyLoading = null;
                    }
                }

                if (userData.m_lockWasTaken)
                {
                    Monitor.Exit(rh);
                }
            }
        }

        private static volatile ResourceHelper m_resHelper;
        private const int MaxMachineNameLength = 256;
        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get
            {
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_InternalSyncObject, o, null);
                }

                return s_InternalSyncObject;
            }
        }

        private static volatile OperatingSystem m_os;
        public static extern int TickCount
        {
            [System.Security.SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        internal static extern void _Exit(int exitCode);
        public static void Exit(int exitCode)
        {
            _Exit(exitCode);
        }

        public static extern int ExitCode
        {
            [System.Security.SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
            [System.Security.SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.InternalCall)]
            set;
        }

        public static extern void FailFast(String message);
        internal static extern void FailFast(String message, uint exitCode);
        public static extern void FailFast(String message, Exception exception);
        public static String CurrentDirectory
        {
            get
            {
                return Directory.GetCurrentDirectory();
            }

            [System.Security.SecurityCritical]
            set
            {
                Directory.SetCurrentDirectory(value);
            }
        }

        public static String SystemDirectory
        {
            [System.Security.SecurityCritical]
            get
            {
                StringBuilder sb = new StringBuilder(Path.MaxPath);
                int r = Win32Native.GetSystemDirectory(sb, Path.MaxPath);
                                if (r == 0)
                    __Error.WinIOError();
                String path = sb.ToString();
                return path;
            }
        }

        internal static String InternalWindowsDirectory
        {
            [System.Security.SecurityCritical]
            get
            {
                StringBuilder sb = new StringBuilder(Path.MaxPath);
                int r = Win32Native.GetWindowsDirectory(sb, Path.MaxPath);
                                if (r == 0)
                    __Error.WinIOError();
                String path = sb.ToString();
                return path;
            }
        }

        public static String ExpandEnvironmentVariables(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
                        if (name.Length == 0)
            {
                return name;
            }

            if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
            {
                return name;
            }

            int currentSize = 100;
            StringBuilder blob = new StringBuilder(currentSize);
            int size;
            blob.Length = 0;
            size = Win32Native.ExpandEnvironmentStrings(name, blob, currentSize);
            if (size == 0)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            while (size > currentSize)
            {
                currentSize = size;
                blob.Capacity = currentSize;
                blob.Length = 0;
                size = Win32Native.ExpandEnvironmentStrings(name, blob, currentSize);
                if (size == 0)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return blob.ToString();
        }

        public static String MachineName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
                {
                    throw new PlatformNotSupportedException();
                }

                StringBuilder buf = new StringBuilder(MaxMachineNameLength);
                int len = MaxMachineNameLength;
                if (Win32Native.GetComputerName(buf, ref len) == 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ComputerName"));
                return buf.ToString();
            }
        }

        private static extern Int32 GetProcessorCount();
        public static int ProcessorCount
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return GetProcessorCount();
            }
        }

        public static int SystemPageSize
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                (new EnvironmentPermission(PermissionState.Unrestricted)).Demand();
                Win32Native.SYSTEM_INFO info = new Win32Native.SYSTEM_INFO();
                Win32Native.GetSystemInfo(ref info);
                return info.dwPageSize;
            }
        }

        public static String[] GetCommandLineArgs()
        {
            new EnvironmentPermission(EnvironmentPermissionAccess.Read, "Path").Demand();
            if (s_CommandLineArgs != null)
                return (string[])s_CommandLineArgs.Clone();
            return GetCommandLineArgsNative();
        }

        private static extern String[] GetCommandLineArgsNative();
        private static string[] s_CommandLineArgs = null;
        private static void SetCommandLineArgs(string[] cmdLineArgs)
        {
            s_CommandLineArgs = cmdLineArgs;
        }

        public static String GetEnvironmentVariable(String variable)
        {
            if (variable == null)
                throw new ArgumentNullException("variable");
                        if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
            {
                return null;
            }

            StringBuilder blob = StringBuilderCache.Acquire(128);
            int requiredSize = Win32Native.GetEnvironmentVariable(variable, blob, blob.Capacity);
            if (requiredSize == 0)
            {
                if (Marshal.GetLastWin32Error() == Win32Native.ERROR_ENVVAR_NOT_FOUND)
                {
                    StringBuilderCache.Release(blob);
                    return null;
                }
            }

            while (requiredSize > blob.Capacity)
            {
                blob.Capacity = requiredSize;
                blob.Length = 0;
                requiredSize = Win32Native.GetEnvironmentVariable(variable, blob, blob.Capacity);
            }

            return StringBuilderCache.GetStringAndRelease(blob);
        }

        public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            if (variable == null)
            {
                throw new ArgumentNullException("variable");
            }

                        if (target == EnvironmentVariableTarget.Process)
            {
                return GetEnvironmentVariable(variable);
            }

            (new EnvironmentPermission(PermissionState.Unrestricted)).Demand();
            if (target == EnvironmentVariableTarget.Machine)
            {
                using (RegistryKey environmentKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Session Manager\Environment", false))
                {
                                        if (environmentKey == null)
                    {
                        return null;
                    }

                    string value = environmentKey.GetValue(variable) as string;
                    return value;
                }
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                using (RegistryKey environmentKey = Registry.CurrentUser.OpenSubKey("Environment", false))
                {
                                        if (environmentKey == null)
                    {
                        return null;
                    }

                    string value = environmentKey.GetValue(variable) as string;
                    return value;
                }
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)target));
            }
        }

        private unsafe static char[] GetEnvironmentCharArray()
        {
            char[] block = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                char *pStrings = null;
                try
                {
                    pStrings = Win32Native.GetEnvironmentStrings();
                    if (pStrings == null)
                    {
                        throw new OutOfMemoryException();
                    }

                    char *p = pStrings;
                    while (!(*p == '\0' && *(p + 1) == '\0'))
                        p++;
                    int len = (int)(p - pStrings + 1);
                    block = new char[len];
                    fixed (char *pBlock = block)
                        String.wstrcpy(pBlock, pStrings, len);
                }
                finally
                {
                    if (pStrings != null)
                        Win32Native.FreeEnvironmentStrings(pStrings);
                }
            }

            return block;
        }

        public static IDictionary GetEnvironmentVariables()
        {
            if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
            {
                return new Hashtable(0);
            }

            char[] block = GetEnvironmentCharArray();
            Hashtable table = new Hashtable(20);
            for (int i = 0; i < block.Length; i++)
            {
                int startKey = i;
                while (block[i] != '=' && block[i] != '\0')
                {
                    i++;
                }

                if (block[i] == '\0')
                {
                    continue;
                }

                if (i - startKey == 0)
                {
                    while (block[i] != 0)
                    {
                        i++;
                    }

                    continue;
                }

                String key = new String(block, startKey, i - startKey);
                i++;
                int startValue = i;
                while (block[i] != 0)
                {
                    i++;
                }

                String value = new String(block, startValue, i - startValue);
                table[key] = value;
            }

            return table;
        }

        internal static IDictionary GetRegistryKeyNameValuePairs(RegistryKey registryKey)
        {
            Hashtable table = new Hashtable(20);
            if (registryKey != null)
            {
                string[] names = registryKey.GetValueNames();
                foreach (string name in names)
                {
                    string value = registryKey.GetValue(name, "").ToString();
                    table.Add(name, value);
                }
            }

            return table;
        }

        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
            {
                return GetEnvironmentVariables();
            }

            (new EnvironmentPermission(PermissionState.Unrestricted)).Demand();
            if (target == EnvironmentVariableTarget.Machine)
            {
                using (RegistryKey environmentKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Session Manager\Environment", false))
                {
                    return GetRegistryKeyNameValuePairs(environmentKey);
                }
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                using (RegistryKey environmentKey = Registry.CurrentUser.OpenSubKey("Environment", false))
                {
                    return GetRegistryKeyNameValuePairs(environmentKey);
                }
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)target));
            }
        }

        public static void SetEnvironmentVariable(string variable, string value)
        {
            CheckEnvironmentVariableName(variable);
            if (String.IsNullOrEmpty(value) || value[0] == '\0')
            {
                value = null;
            }
            else
            {
                if (value.Length >= MaxEnvVariableValueLength)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_LongEnvVarValue"));
                }
            }

            if (AppDomain.IsAppXModel() && !AppDomain.IsAppXDesignMode())
            {
                throw new PlatformNotSupportedException();
            }

            if (!Win32Native.SetEnvironmentVariable(variable, value))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Win32Native.ERROR_ENVVAR_NOT_FOUND)
                {
                    return;
                }

                if (errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_LongEnvVarValue"));
                }

                throw new ArgumentException(Win32Native.GetMessage(errorCode));
            }
        }

        private static void CheckEnvironmentVariableName(string variable)
        {
            if (variable == null)
            {
                throw new ArgumentNullException("variable");
            }

            if (variable.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_StringZeroLength"), "variable");
            }

            if (variable[0] == '\0')
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_StringFirstCharIsZero"), "variable");
            }

            if (variable.Length >= MaxEnvVariableValueLength)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_LongEnvVarValue"));
            }

            if (variable.IndexOf('=') != -1)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalEnvVarName"));
            }

                    }

        public static void SetEnvironmentVariable(string variable, string value, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
            {
                SetEnvironmentVariable(variable, value);
                return;
            }

            CheckEnvironmentVariableName(variable);
            if (variable.Length >= MaxSystemEnvVariableLength)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_LongEnvVarName"));
            }

            new EnvironmentPermission(PermissionState.Unrestricted).Demand();
            if (String.IsNullOrEmpty(value) || value[0] == '\0')
            {
                value = null;
            }

            if (target == EnvironmentVariableTarget.Machine)
            {
                using (RegistryKey environmentKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Session Manager\Environment", true))
                {
                                        if (environmentKey != null)
                    {
                        if (value == null)
                            environmentKey.DeleteValue(variable, false);
                        else
                            environmentKey.SetValue(variable, value);
                    }
                }
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                if (variable.Length >= MaxUserEnvVariableLength)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_LongEnvVarValue"));
                }

                using (RegistryKey environmentKey = Registry.CurrentUser.OpenSubKey("Environment", true))
                {
                                        if (environmentKey != null)
                    {
                        if (value == null)
                            environmentKey.DeleteValue(variable, false);
                        else
                            environmentKey.SetValue(variable, value);
                    }
                }
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)target));
            }

            IntPtr r = Win32Native.SendMessageTimeout(new IntPtr(Win32Native.HWND_BROADCAST), Win32Native.WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0, 1000, IntPtr.Zero);
            if (r == IntPtr.Zero)
                BCLDebug.Assert(false, "SetEnvironmentVariable failed: " + Marshal.GetLastWin32Error());
        }

        public static String[] GetLogicalDrives()
        {
            new EnvironmentPermission(PermissionState.Unrestricted).Demand();
            int drives = Win32Native.GetLogicalDrives();
            if (drives == 0)
                __Error.WinIOError();
            uint d = (uint)drives;
            int count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0)
                    count++;
                d >>= 1;
            }

            String[] result = new String[count];
            char[] root = new char[]{'A', ':', '\\'};
            d = (uint)drives;
            count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0)
                {
                    result[count++] = new String(root);
                }

                d >>= 1;
                root[0]++;
            }

            return result;
        }

        public static String NewLine
        {
            get
            {
                                return "\r\n";
            }
        }

        public static Version Version
        {
            get
            {
                return new Version(4, 0, 30319, 42000);
            }
        }

        private static extern long GetWorkingSet();
        public static long WorkingSet
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                new EnvironmentPermission(PermissionState.Unrestricted).Demand();
                return GetWorkingSet();
            }
        }

        public static OperatingSystem OSVersion
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                                if (m_os == null)
                {
                    Microsoft.Win32.Win32Native.OSVERSIONINFO osvi = new Microsoft.Win32.Win32Native.OSVERSIONINFO();
                    if (!GetVersion(osvi))
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GetVersion"));
                    }

                    Microsoft.Win32.Win32Native.OSVERSIONINFOEX osviEx = new Microsoft.Win32.Win32Native.OSVERSIONINFOEX();
                    if (!GetVersionEx(osviEx))
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GetVersion"));
                    PlatformID id = PlatformID.Win32NT;
                    if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    {
                        id = PlatformID.WinCE;
                    }

                    Version v = new Version(osvi.MajorVersion, osvi.MinorVersion, osvi.BuildNumber, (osviEx.ServicePackMajor << 16) | osviEx.ServicePackMinor);
                    m_os = new OperatingSystem(id, v, osvi.CSDVersion);
                }

                                return m_os;
            }
        }

        internal static bool IsWindows8OrAbove
        {
            get
            {
                return true;
            }
        }

        internal static bool IsWinRTSupported
        {
            get
            {
                return true;
            }
        }

        internal static extern bool GetVersion(Microsoft.Win32.Win32Native.OSVERSIONINFO osVer);
        internal static extern bool GetVersionEx(Microsoft.Win32.Win32Native.OSVERSIONINFOEX osVer);
        public static String StackTrace
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                                new EnvironmentPermission(PermissionState.Unrestricted).Demand();
                return GetStackTrace(null, true);
            }
        }

        internal static String GetStackTrace(Exception e, bool needFileInfo)
        {
            StackTrace st;
            if (e == null)
                st = new StackTrace(needFileInfo);
            else
                st = new StackTrace(e, needFileInfo);
            return st.ToString(System.Diagnostics.StackTrace.TraceFormat.Normal);
        }

        private static void InitResourceHelper()
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(Environment.InternalSyncObject, ref tookLock);
                if (m_resHelper == null)
                {
                    ResourceHelper rh = new ResourceHelper("mscorlib");
                    System.Threading.Thread.MemoryBarrier();
                    m_resHelper = rh;
                }
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(Environment.InternalSyncObject);
            }
        }

        internal static String GetResourceStringLocal(String key)
        {
            if (m_resHelper == null)
                InitResourceHelper();
            return m_resHelper.GetResourceString(key);
        }

        internal static String GetResourceString(String key)
        {
            return GetResourceStringLocal(key);
        }

        internal static String GetResourceString(String key, params Object[] values)
        {
            String s = GetResourceString(key);
            return String.Format(CultureInfo.CurrentCulture, s, values);
        }

        internal static String GetRuntimeResourceString(String key)
        {
            return GetResourceString(key);
        }

        internal static String GetRuntimeResourceString(String key, params Object[] values)
        {
            return GetResourceString(key, values);
        }

        public static bool Is64BitProcess
        {
            get
            {
                return true;
            }
        }

        public static bool Is64BitOperatingSystem
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return true;
            }
        }

        public static extern bool HasShutdownStarted
        {
            [System.Security.SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        public static string UserName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                new EnvironmentPermission(EnvironmentPermissionAccess.Read, "UserName").Demand();
                StringBuilder sb = new StringBuilder(256);
                int size = sb.Capacity;
                if (Win32Native.GetUserName(sb, ref size))
                {
                    return sb.ToString();
                }

                return String.Empty;
            }
        }

        public static bool UserInteractive
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return true;
            }
        }

        public static string GetFolderPath(SpecialFolder folder)
        {
            if (!Enum.IsDefined(typeof (SpecialFolder), folder))
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)folder));
                        return InternalGetFolderPath(folder, SpecialFolderOption.None);
        }

        public static string GetFolderPath(SpecialFolder folder, SpecialFolderOption option)
        {
            if (!Enum.IsDefined(typeof (SpecialFolder), folder))
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)folder));
            if (!Enum.IsDefined(typeof (SpecialFolderOption), option))
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)option));
                        return InternalGetFolderPath(folder, option);
        }

        internal static string UnsafeGetFolderPath(SpecialFolder folder)
        {
            return InternalGetFolderPath(folder, SpecialFolderOption.None, suppressSecurityChecks: true);
        }

        private static string InternalGetFolderPath(SpecialFolder folder, SpecialFolderOption option, bool suppressSecurityChecks = false)
        {
            switch (folder)
            {
                case SpecialFolder.System:
                    return SystemDirectory;
                case SpecialFolder.ApplicationData:
                case SpecialFolder.Favorites:
                case SpecialFolder.Programs:
                case SpecialFolder.StartMenu:
                case SpecialFolder.Startup:
                case SpecialFolder.Personal:
                    throw new PlatformNotSupportedException();
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        public static string UserDomainName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                new EnvironmentPermission(EnvironmentPermissionAccess.Read, "UserDomain").Demand();
                byte[] sid = new byte[1024];
                int sidLen = sid.Length;
                StringBuilder domainName = new StringBuilder(1024);
                uint domainNameLen = (uint)domainName.Capacity;
                int peUse;
                byte ret = Win32Native.GetUserNameEx(Win32Native.NameSamCompatible, domainName, ref domainNameLen);
                if (ret == 1)
                {
                    string samName = domainName.ToString();
                    int index = samName.IndexOf('\\');
                    if (index != -1)
                    {
                        return samName.Substring(0, index);
                    }
                }

                domainNameLen = (uint)domainName.Capacity;
                bool success = Win32Native.LookupAccountName(null, UserName, sid, ref sidLen, domainName, ref domainNameLen, out peUse);
                if (!success)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException(Win32Native.GetMessage(errorCode));
                }

                return domainName.ToString();
            }
        }

        public enum SpecialFolderOption
        {
            None = 0,
            Create = Win32Native.CSIDL_FLAG_CREATE,
            DoNotVerify = Win32Native.CSIDL_FLAG_DONT_VERIFY
        }

        public enum SpecialFolder
        {
            ApplicationData = Win32Native.CSIDL_APPDATA,
            CommonApplicationData = Win32Native.CSIDL_COMMON_APPDATA,
            LocalApplicationData = Win32Native.CSIDL_LOCAL_APPDATA,
            Cookies = Win32Native.CSIDL_COOKIES,
            Desktop = Win32Native.CSIDL_DESKTOP,
            Favorites = Win32Native.CSIDL_FAVORITES,
            History = Win32Native.CSIDL_HISTORY,
            InternetCache = Win32Native.CSIDL_INTERNET_CACHE,
            Programs = Win32Native.CSIDL_PROGRAMS,
            MyComputer = Win32Native.CSIDL_DRIVES,
            MyMusic = Win32Native.CSIDL_MYMUSIC,
            MyPictures = Win32Native.CSIDL_MYPICTURES,
            MyVideos = Win32Native.CSIDL_MYVIDEO,
            Recent = Win32Native.CSIDL_RECENT,
            SendTo = Win32Native.CSIDL_SENDTO,
            StartMenu = Win32Native.CSIDL_STARTMENU,
            Startup = Win32Native.CSIDL_STARTUP,
            System = Win32Native.CSIDL_SYSTEM,
            Templates = Win32Native.CSIDL_TEMPLATES,
            DesktopDirectory = Win32Native.CSIDL_DESKTOPDIRECTORY,
            Personal = Win32Native.CSIDL_PERSONAL,
            MyDocuments = Win32Native.CSIDL_PERSONAL,
            ProgramFiles = Win32Native.CSIDL_PROGRAM_FILES,
            CommonProgramFiles = Win32Native.CSIDL_PROGRAM_FILES_COMMON
        }

        public static int CurrentManagedThreadId
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return Thread.CurrentThread.ManagedThreadId;
            }
        }
    }
}