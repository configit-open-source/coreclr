namespace Microsoft.Win32.SafeHandles
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Security;

    public sealed class SafeAccessTokenHandle : SafeHandle
    {
        private SafeAccessTokenHandle(): base (IntPtr.Zero, true)
        {
        }

        public SafeAccessTokenHandle(IntPtr handle): base (IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public static SafeAccessTokenHandle InvalidHandle
        {
            [SecurityCritical]
            get
            {
                return new SafeAccessTokenHandle(IntPtr.Zero);
            }
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get
            {
                return handle == IntPtr.Zero || handle == new IntPtr(-1);
            }
        }

        protected override bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }

    internal sealed class SafeLsaLogonProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeLsaLogonProcessHandle(): base (true)
        {
        }

        internal SafeLsaLogonProcessHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeLsaLogonProcessHandle InvalidHandle
        {
            get
            {
                return new SafeLsaLogonProcessHandle(IntPtr.Zero);
            }
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.LsaDeregisterLogonProcess(handle) >= 0;
        }
    }

    internal sealed class SafeLsaMemoryHandle : SafeBuffer
    {
        private SafeLsaMemoryHandle(): base (true)
        {
        }

        internal SafeLsaMemoryHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeLsaMemoryHandle InvalidHandle
        {
            get
            {
                return new SafeLsaMemoryHandle(IntPtr.Zero);
            }
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.LsaFreeMemory(handle) == 0;
        }
    }

    internal sealed class SafeLsaPolicyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeLsaPolicyHandle(): base (true)
        {
        }

        internal SafeLsaPolicyHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeLsaPolicyHandle InvalidHandle
        {
            get
            {
                return new SafeLsaPolicyHandle(IntPtr.Zero);
            }
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.LsaClose(handle) == 0;
        }
    }

    internal sealed class SafeLsaReturnBufferHandle : SafeBuffer
    {
        private SafeLsaReturnBufferHandle(): base (true)
        {
        }

        internal SafeLsaReturnBufferHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeLsaReturnBufferHandle InvalidHandle
        {
            get
            {
                return new SafeLsaReturnBufferHandle(IntPtr.Zero);
            }
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.LsaFreeReturnBuffer(handle) >= 0;
        }
    }

    internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeProcessHandle(): base (true)
        {
        }

        internal SafeProcessHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        internal static SafeProcessHandle InvalidHandle
        {
            get
            {
                return new SafeProcessHandle(IntPtr.Zero);
            }
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }

    internal sealed class SafeThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeThreadHandle(): base (true)
        {
        }

        internal SafeThreadHandle(IntPtr handle): base (true)
        {
            SetHandle(handle);
        }

        override protected bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(handle);
        }
    }
}