using System.Runtime.ConstrainedExecution;

namespace System.Runtime.InteropServices
{
    public abstract class SafeHandle : CriticalFinalizerObject, IDisposable
    {
        private String _stackTrace;
        protected IntPtr handle;
        private int _state;
        private bool _ownsHandle;
        private bool _fullyInitialized;
        protected SafeHandle(IntPtr invalidHandleValue, bool ownsHandle)
        {
            handle = invalidHandleValue;
            _state = 4;
            _ownsHandle = ownsHandle;
            if (!ownsHandle)
                GC.SuppressFinalize(this);
            if (BCLDebug.SafeHandleStackTracesEnabled)
                _stackTrace = Environment.GetStackTrace(null, false);
            else
                _stackTrace = "For a stack trace showing who allocated this SafeHandle, set SafeHandleStackTraces to 1 and rerun your app.";
            _fullyInitialized = true;
        }

        protected SafeHandle()
        {
            BCLDebug.Assert(false, "SafeHandle's protected default ctor should never be used!");
            throw new NotImplementedException();
        }

        ~SafeHandle()
        {
            Dispose(false);
        }

        extern void InternalFinalize();
        protected void SetHandle(IntPtr handle)
        {
            this.handle = handle;
        }

        public IntPtr DangerousGetHandle()
        {
            return handle;
        }

        public bool IsClosed
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return (_state & 1) == 1;
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
            if (disposing)
                InternalDispose();
            else
                InternalFinalize();
        }

        private extern void InternalDispose();
        public extern void SetHandleAsInvalid();
        protected abstract bool ReleaseHandle();
        public extern void DangerousAddRef(ref bool success);
        public extern void DangerousRelease();
    }
}