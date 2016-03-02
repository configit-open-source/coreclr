using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class SafeProvHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeProvHandle(): base (true)
        {
            SetHandle(IntPtr.Zero);
        }

        private SafeProvHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeProvHandle InvalidHandle
        {
            get
            {
                return new SafeProvHandle();
            }
        }

        private static extern void FreeCsp(IntPtr pProviderContext);
        protected override bool ReleaseHandle()
        {
            FreeCsp(handle);
            return true;
        }
    }

    internal sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeKeyHandle(): base (true)
        {
            SetHandle(IntPtr.Zero);
        }

        private SafeKeyHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeKeyHandle InvalidHandle
        {
            get
            {
                return new SafeKeyHandle();
            }
        }

        private static extern void FreeKey(IntPtr pKeyCotext);
        protected override bool ReleaseHandle()
        {
            FreeKey(handle);
            return true;
        }
    }

    internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeHashHandle(): base (true)
        {
            SetHandle(IntPtr.Zero);
        }

        private SafeHashHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeHashHandle InvalidHandle
        {
            get
            {
                return new SafeHashHandle();
            }
        }

        private static extern void FreeHash(IntPtr pHashContext);
        protected override bool ReleaseHandle()
        {
            FreeHash(handle);
            return true;
        }
    }
}