using System.Runtime.Serialization;

namespace System
{
    public class NotImplementedException : SystemException
    {
        public NotImplementedException(): base (Environment.GetResourceString("Arg_NotImplementedException"))
        {
            SetErrorCode(__HResults.E_NOTIMPL);
        }

        public NotImplementedException(String message): base (message)
        {
            SetErrorCode(__HResults.E_NOTIMPL);
        }

        public NotImplementedException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.E_NOTIMPL);
        }

        protected NotImplementedException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}