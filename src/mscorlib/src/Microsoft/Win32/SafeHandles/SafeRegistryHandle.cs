namespace Microsoft.Win32.SafeHandles
{
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;

    public sealed class SafeRegistryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeRegistryHandle(): base (true)
        {
        }

        public SafeRegistryHandle(IntPtr preexistingHandle, bool ownsHandle): base (ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        override protected bool ReleaseHandle()
        {
            return (RegCloseKey(handle) == Win32Native.ERROR_SUCCESS);
        }

        internal static extern int RegCloseKey(IntPtr hKey);
    }
}