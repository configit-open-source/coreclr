using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Text;

namespace System.Runtime.InteropServices
{
    static public class RuntimeEnvironment
    {
        internal static extern String GetModuleFileName();
        internal static extern String GetDeveloperPath();
        internal static extern String GetHostBindingFile();
        public static bool FromGlobalAccessCache(Assembly a)
        {
            return a.GlobalAssemblyCache;
        }

        public static String GetSystemVersion()
        {
            return Assembly.GetExecutingAssembly().ImageRuntimeVersion;
        }

        public static String GetRuntimeDirectory()
        {
            String dir = GetRuntimeDirectoryImpl();
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, dir).Demand();
            return dir;
        }

        internal static extern String GetRuntimeDirectoryImpl();
        public static String SystemConfigurationFile
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                StringBuilder sb = new StringBuilder(Path.MaxPath);
                sb.Append(GetRuntimeDirectory());
                sb.Append(AppDomainSetup.RuntimeConfigurationFile);
                String path = sb.ToString();
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery, path).Demand();
                return path;
            }
        }

        private static extern IntPtr GetRuntimeInterfaceImpl([In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);
        public static IntPtr GetRuntimeInterfaceAsIntPtr(Guid clsid, Guid riid)
        {
            return GetRuntimeInterfaceImpl(clsid, riid);
        }

        public static object GetRuntimeInterfaceAsObject(Guid clsid, Guid riid)
        {
            IntPtr p = IntPtr.Zero;
            try
            {
                p = GetRuntimeInterfaceImpl(clsid, riid);
                return Marshal.GetObjectForIUnknown(p);
            }
            finally
            {
                if (p != IntPtr.Zero)
                {
                    Marshal.Release(p);
                }
            }
        }
    }
}