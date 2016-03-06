using System.Runtime.Serialization;

namespace System
{
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

    }
}