using System;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed class SafeWaitHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeWaitHandle(): base (true)
        {
        }

        public SafeWaitHandle(IntPtr existingHandle, bool ownsHandle): base (ownsHandle)
        {
            SetHandle(existingHandle);
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }
}