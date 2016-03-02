using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLocalAllocHandle : SafeBuffer
    {
        private SafeLocalAllocHandle(): base (true)
        {
        }

        internal SafeLocalAllocHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeLocalAllocHandle InvalidHandle
        {
            get
            {
                return new SafeLocalAllocHandle(IntPtr.Zero);
            }
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.LocalFree(handle) == IntPtr.Zero;
        }
    }
}