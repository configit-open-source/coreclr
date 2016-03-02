using System.Runtime.Serialization;

namespace System
{
    public class TypeAccessException : TypeLoadException
    {
        public TypeAccessException(): base (Environment.GetResourceString("Arg_TypeAccessException"))
        {
            SetErrorCode(__HResults.COR_E_TYPEACCESS);
        }

        public TypeAccessException(string message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TYPEACCESS);
        }

        public TypeAccessException(string message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_TYPEACCESS);
        }

        protected TypeAccessException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            SetErrorCode(__HResults.COR_E_TYPEACCESS);
        }
    }
}