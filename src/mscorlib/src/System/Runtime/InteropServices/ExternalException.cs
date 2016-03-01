using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    public class ExternalException : SystemException
    {
        public ExternalException(): base (Environment.GetResourceString("Arg_ExternalException"))
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public ExternalException(String message): base (message)
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public ExternalException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public ExternalException(String message, int errorCode): base (message)
        {
            SetErrorCode(errorCode);
        }

        protected ExternalException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public virtual int ErrorCode
        {
            get
            {
                return HResult;
            }
        }
    }
}