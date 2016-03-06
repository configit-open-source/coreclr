using System.Runtime.Serialization;

namespace System
{
    public class ArgumentNullException : ArgumentException
    {
        public ArgumentNullException(): base (Environment.GetResourceString("ArgumentNull_Generic"))
        {
            SetErrorCode(__HResults.E_POINTER);
        }

        public ArgumentNullException(String paramName): base (Environment.GetResourceString("ArgumentNull_Generic"), paramName)
        {
            SetErrorCode(__HResults.E_POINTER);
        }

        public ArgumentNullException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.E_POINTER);
        }

        public ArgumentNullException(String paramName, String message): base (message, paramName)
        {
            SetErrorCode(__HResults.E_POINTER);
        }
    }
}