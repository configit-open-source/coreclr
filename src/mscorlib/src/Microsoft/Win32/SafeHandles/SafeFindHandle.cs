namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeFindHandle(): base (true)
        {
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.FindClose(handle);
        }
    }
}