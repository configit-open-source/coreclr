using System.Runtime.Serialization;

namespace System
{
    public class FieldAccessException : MemberAccessException
    {
        public FieldAccessException(): base (Environment.GetResourceString("Arg_FieldAccessException"))
        {
            SetErrorCode(__HResults.COR_E_FIELDACCESS);
        }

        public FieldAccessException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_FIELDACCESS);
        }

        public FieldAccessException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_FIELDACCESS);
        }

        protected FieldAccessException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}