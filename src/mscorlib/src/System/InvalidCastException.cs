using System.Runtime.Serialization;

namespace System
{
    public class InvalidCastException : SystemException
    {
        public InvalidCastException(): base (Environment.GetResourceString("Arg_InvalidCastException"))
        {
            SetErrorCode(__HResults.COR_E_INVALIDCAST);
        }

        public InvalidCastException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INVALIDCAST);
        }

        public InvalidCastException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_INVALIDCAST);
        }

        public InvalidCastException(String message, int errorCode): base (message)
        {
            SetErrorCode(errorCode);
        }
    }
}