using System;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeViewOfFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeViewOfFileHandle(): base (true)
        {
        }

        internal SafeViewOfFileHandle(IntPtr handle, bool ownsHandle): base (ownsHandle)
        {
            SetHandle(handle);
        }

        override protected bool ReleaseHandle()
        {
            if (Win32Native.UnmapViewOfFile(handle))
            {
                handle = IntPtr.Zero;
                return true;
            }

            return false;
        }
    }
}