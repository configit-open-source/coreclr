using System.Runtime.Serialization;

namespace System
{
    public class AccessViolationException : SystemException
    {
        public AccessViolationException(): base (Environment.GetResourceString("Arg_AccessViolationException"))
        {
            SetErrorCode(__HResults.E_POINTER);
        }

        public AccessViolationException(String message): base (message)
        {
            SetErrorCode(__HResults.E_POINTER);
        }

        public AccessViolationException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.E_POINTER);
        }
    }
}