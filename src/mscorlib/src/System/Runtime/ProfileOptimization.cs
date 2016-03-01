namespace System.Runtime
{
    using System;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;

    public static class ProfileOptimization
    {
        internal static extern void InternalSetProfileRoot(string directoryPath);
        internal static extern void InternalStartProfile(string profile, IntPtr ptrNativeAssemblyLoadContext);
        public static void SetProfileRoot(string directoryPath)
        {
            InternalSetProfileRoot(directoryPath);
        }

        public static void StartProfile(string profile)
        {
            InternalStartProfile(profile, IntPtr.Zero);
        }
    }
}