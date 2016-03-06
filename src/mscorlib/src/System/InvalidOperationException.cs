using System.Runtime.Serialization;

namespace System
{
    public class InvalidOperationException : SystemException
    {
        public InvalidOperationException(): base (Environment.GetResourceString("Arg_InvalidOperationException"))
        {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }

        public InvalidOperationException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }

        public InvalidOperationException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }
    }
}