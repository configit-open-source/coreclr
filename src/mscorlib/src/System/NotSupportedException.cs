using System.Runtime.Serialization;

namespace System
{
    public class NotSupportedException : SystemException
    {
        public NotSupportedException(): base (Environment.GetResourceString("Arg_NotSupportedException"))
        {
            SetErrorCode(__HResults.COR_E_NOTSUPPORTED);
        }

        public NotSupportedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_NOTSUPPORTED);
        }

        public NotSupportedException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_NOTSUPPORTED);
        }

        protected NotSupportedException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}