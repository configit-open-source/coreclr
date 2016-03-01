namespace System
{
    using System.Runtime.Serialization;

    public class TimeoutException : SystemException
    {
        public TimeoutException(): base (Environment.GetResourceString("Arg_TimeoutException"))
        {
            SetErrorCode(__HResults.COR_E_TIMEOUT);
        }

        public TimeoutException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TIMEOUT);
        }

        public TimeoutException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_TIMEOUT);
        }

        protected TimeoutException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}