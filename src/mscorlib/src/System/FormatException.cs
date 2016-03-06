using System.Runtime.Serialization;

namespace System
{
    public class FormatException : SystemException
    {
        public FormatException(): base (Environment.GetResourceString("Arg_FormatException"))
        {
            SetErrorCode(__HResults.COR_E_FORMAT);
        }

        public FormatException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_FORMAT);
        }

        public FormatException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_FORMAT);
        }
    }
}