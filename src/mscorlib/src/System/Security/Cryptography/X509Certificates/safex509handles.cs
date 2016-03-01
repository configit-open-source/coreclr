namespace System.Security.Cryptography.X509Certificates
{
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using Microsoft.Win32.SafeHandles;

    internal sealed class SafeCertContextHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCertContextHandle(): base (true)
        {
        }

        internal SafeCertContextHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeCertContextHandle InvalidHandle
        {
            get
            {
                return new SafeCertContextHandle(IntPtr.Zero);
            }
        }

        internal IntPtr pCertContext
        {
            get
            {
                if (handle == IntPtr.Zero)
                    return IntPtr.Zero;
                return Marshal.ReadIntPtr(handle);
            }
        }

        private static extern void _FreePCertContext(IntPtr pCert);
        override protected bool ReleaseHandle()
        {
            _FreePCertContext(handle);
            return true;
        }
    }

    internal sealed class SafeCertStoreHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCertStoreHandle(): base (true)
        {
        }

        internal SafeCertStoreHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeCertStoreHandle InvalidHandle
        {
            get
            {
                return new SafeCertStoreHandle(IntPtr.Zero);
            }
        }

        private static extern void _FreeCertStoreContext(IntPtr hCertStore);
        override protected bool ReleaseHandle()
        {
            _FreeCertStoreContext(handle);
            return true;
        }
    }
}