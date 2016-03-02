using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    public class InvalidComObjectException : SystemException
    {
        public InvalidComObjectException(): base (Environment.GetResourceString("Arg_InvalidComObjectException"))
        {
            SetErrorCode(__HResults.COR_E_INVALIDCOMOBJECT);
        }

        public InvalidComObjectException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INVALIDCOMOBJECT);
        }

        public InvalidComObjectException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_INVALIDCOMOBJECT);
        }

        protected InvalidComObjectException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}