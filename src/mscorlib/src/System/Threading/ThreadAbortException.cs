using System.Runtime.Serialization;

namespace System.Threading
{
    public sealed class ThreadAbortException : SystemException
    {
        private ThreadAbortException(): base (GetMessageFromNativeResources(ExceptionMessageKind.ThreadAbort))
        {
            SetErrorCode(__HResults.COR_E_THREADABORTED);
        }

        internal ThreadAbortException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public Object ExceptionState
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return Thread.CurrentThread.AbortReason;
            }
        }
    }
}