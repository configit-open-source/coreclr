using System.Runtime.Serialization;

namespace System
{
    public class ApplicationException : Exception
    {
        public ApplicationException(): base (Environment.GetResourceString("Arg_ApplicationException"))
        {
            SetErrorCode(__HResults.COR_E_APPLICATION);
        }

        public ApplicationException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_APPLICATION);
        }

        public ApplicationException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_APPLICATION);
        }

        protected ApplicationException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}