using System.Runtime.Serialization;

namespace System.IO
{
    public class EndOfStreamException : IOException
    {
        public EndOfStreamException(): base (Environment.GetResourceString("Arg_EndOfStreamException"))
        {
            SetErrorCode(__HResults.COR_E_ENDOFSTREAM);
        }

        public EndOfStreamException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ENDOFSTREAM);
        }

        public EndOfStreamException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ENDOFSTREAM);
        }

        protected EndOfStreamException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}