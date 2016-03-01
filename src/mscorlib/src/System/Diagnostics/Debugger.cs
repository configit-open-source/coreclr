namespace System.Diagnostics
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.Versioning;

    public sealed class Debugger
    {
        public Debugger()
        {
        }

        public static void Break()
        {
            if (!Debugger.IsAttached)
            {
                try
                {
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                }
                catch (SecurityException)
                {
                    return;
                }
            }

            BreakInternal();
        }

        static void BreakCanThrow()
        {
            if (!Debugger.IsAttached)
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            }

            BreakInternal();
        }

        private static extern void BreakInternal();
        public static bool Launch()
        {
            if (Debugger.IsAttached)
                return (true);
            try
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            }
            catch (SecurityException)
            {
                return (false);
            }

            return (LaunchInternal());
        }

        private class CrossThreadDependencyNotification : ICustomDebuggerNotification
        {
            public CrossThreadDependencyNotification()
            {
            }
        }

        private static void NotifyOfCrossThreadDependencySlow()
        {
            CrossThreadDependencyNotification notification = new CrossThreadDependencyNotification();
            CustomNotification(notification);
        }

        public static void NotifyOfCrossThreadDependency()
        {
            if (Debugger.IsAttached)
            {
                NotifyOfCrossThreadDependencySlow();
            }
        }

        private static extern bool LaunchInternal();
        public static extern bool IsAttached
        {
            [System.Security.SecuritySafeCritical]
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        public static readonly String DefaultCategory = null;
        public static extern void Log(int level, String category, String message);
        public static extern bool IsLogging();
        private static extern void CustomNotification(ICustomDebuggerNotification data);
    }
}