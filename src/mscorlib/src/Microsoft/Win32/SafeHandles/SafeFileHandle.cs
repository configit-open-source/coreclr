using System;

namespace Microsoft.Win32.SafeHandles
{
    public sealed class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeFileHandle(): base (true)
        {
        }

        public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle): base (ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }
}