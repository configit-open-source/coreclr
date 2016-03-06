using System.Runtime.Serialization;

namespace System.Threading
{
    public sealed class ThreadAbortException : SystemException
    {
        private ThreadAbortException(): base (null)
        {
            SetErrorCode(__HResults.COR_E_THREADABORTED);
        }

        public Object ExceptionState
        {
            get
            {
                return Thread.CurrentThread.AbortReason;
            }
        }
    }
}