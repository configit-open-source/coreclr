using System;
using System.Reflection;
using System.Threading;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Runtime.ConstrainedExecution;
using System.IO;

namespace System.Runtime.InteropServices
{
    public abstract class CriticalHandle : CriticalFinalizerObject, IDisposable
    {
        private String _stackTrace;
        protected IntPtr handle;
        private bool _isClosed;
        protected CriticalHandle(IntPtr invalidHandleValue)
        {
            handle = invalidHandleValue;
            _isClosed = false;
            if (BCLDebug.SafeHandleStackTracesEnabled)
                _stackTrace = Environment.GetStackTrace(null, false);
            else
                _stackTrace = "For a stack trace showing who allocated this CriticalHandle, set SafeHandleStackTraces to 1 and rerun your app.";
        }

        private CriticalHandle()
        {
        }

        ~CriticalHandle()
        {
            Dispose(false);
        }

        private void Cleanup()
        {
            if (IsClosed)
                return;
            _isClosed = true;
            if (IsInvalid)
                return;
            int lastError = Marshal.GetLastWin32Error();
            if (!ReleaseHandle())
                FireCustomerDebugProbe();
            Marshal.SetLastWin32Error(lastError);
            GC.SuppressFinalize(this);
        }

        private extern void FireCustomerDebugProbe();
        protected void SetHandle(IntPtr handle)
        {
            this.handle = handle;
        }

        public bool IsClosed
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return _isClosed;
            }
        }

        public abstract bool IsInvalid
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get;
        }

        public void Close()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            Cleanup();
        }

        public void SetHandleAsInvalid()
        {
            _isClosed = true;
            GC.SuppressFinalize(this);
        }

        protected abstract bool ReleaseHandle();
    }
}