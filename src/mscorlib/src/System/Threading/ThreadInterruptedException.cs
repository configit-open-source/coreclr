using System.Runtime.Serialization;

namespace System.Threading
{
    public class ThreadInterruptedException : SystemException
    {
        public ThreadInterruptedException(): base (GetMessageFromNativeResources(ExceptionMessageKind.ThreadInterrupted))
        {
            SetErrorCode(__HResults.COR_E_THREADINTERRUPTED);
        }

        public ThreadInterruptedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_THREADINTERRUPTED);
        }

        public ThreadInterruptedException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_THREADINTERRUPTED);
        }

        protected ThreadInterruptedException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}