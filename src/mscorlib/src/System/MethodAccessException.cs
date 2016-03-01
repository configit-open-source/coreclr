using System.Runtime.Serialization;

namespace System
{
    public class MethodAccessException : MemberAccessException
    {
        public MethodAccessException(): base (Environment.GetResourceString("Arg_MethodAccessException"))
        {
            SetErrorCode(__HResults.COR_E_METHODACCESS);
        }

        public MethodAccessException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_METHODACCESS);
        }

        public MethodAccessException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_METHODACCESS);
        }

        protected MethodAccessException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}