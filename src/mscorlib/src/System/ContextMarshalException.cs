using System.Runtime.Serialization;

namespace System
{
    public class ContextMarshalException : SystemException
    {
        public ContextMarshalException(): base (Environment.GetResourceString("Arg_ContextMarshalException"))
        {
            SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        }

        public ContextMarshalException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        }

        public ContextMarshalException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_CONTEXTMARSHAL);
        }

        protected ContextMarshalException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}