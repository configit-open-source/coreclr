namespace Microsoft.Win32.SafeHandles
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.Runtime.ConstrainedExecution;

    public abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle): base (IntPtr.Zero, ownsHandle)
        {
        }

        protected SafeHandleZeroOrMinusOneIsInvalid()
        {
            throw new NotImplementedException();
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle.IsNull() || handle == new IntPtr(-1);
            }
        }
    }

    public abstract class SafeHandleMinusOneIsInvalid : SafeHandle
    {
        protected SafeHandleMinusOneIsInvalid(bool ownsHandle): base (new IntPtr(-1), ownsHandle)
        {
        }

        protected SafeHandleMinusOneIsInvalid()
        {
            throw new NotImplementedException();
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle == new IntPtr(-1);
            }
        }
    }

    public abstract class CriticalHandleZeroOrMinusOneIsInvalid : CriticalHandle
    {
        protected CriticalHandleZeroOrMinusOneIsInvalid(): base (IntPtr.Zero)
        {
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle.IsNull() || handle == new IntPtr(-1);
            }
        }
    }

    public abstract class CriticalHandleMinusOneIsInvalid : CriticalHandle
    {
        protected CriticalHandleMinusOneIsInvalid(): base (new IntPtr(-1))
        {
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get
            {
                return handle == new IntPtr(-1);
            }
        }
    }
}