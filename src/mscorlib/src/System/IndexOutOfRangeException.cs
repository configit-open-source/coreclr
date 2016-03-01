using System.Runtime.Serialization;

namespace System
{
    public sealed class IndexOutOfRangeException : SystemException
    {
        public IndexOutOfRangeException(): base (Environment.GetResourceString("Arg_IndexOutOfRangeException"))
        {
            SetErrorCode(__HResults.COR_E_INDEXOUTOFRANGE);
        }

        public IndexOutOfRangeException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INDEXOUTOFRANGE);
        }

        public IndexOutOfRangeException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_INDEXOUTOFRANGE);
        }

        internal IndexOutOfRangeException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}