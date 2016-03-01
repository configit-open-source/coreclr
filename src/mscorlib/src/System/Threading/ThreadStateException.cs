namespace System.Threading
{
    using System;
    using System.Runtime.Serialization;

    public class ThreadStateException : SystemException
    {
        public ThreadStateException(): base (Environment.GetResourceString("Arg_ThreadStateException"))
        {
            SetErrorCode(__HResults.COR_E_THREADSTATE);
        }

        public ThreadStateException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_THREADSTATE);
        }

        public ThreadStateException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_THREADSTATE);
        }

        protected ThreadStateException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}