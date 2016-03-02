using System;

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