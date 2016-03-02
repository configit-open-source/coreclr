using System;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeFileMappingHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeFileMappingHandle(): base (true)
        {
        }

        internal SafeFileMappingHandle(IntPtr handle, bool ownsHandle): base (ownsHandle)
        {
            SetHandle(handle);
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }
}