using Microsoft.Win32.SafeHandles;

namespace Microsoft.Win32
{
    sealed internal class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeLibraryHandle(): base (true)
        {
        }

        override protected bool ReleaseHandle()
        {
            return UnsafeNativeMethods.FreeLibrary(handle);
        }
    }
}