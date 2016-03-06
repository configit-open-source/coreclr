using System.Runtime.Serialization;

namespace System
{
    public class OutOfMemoryException : SystemException
    {
        public OutOfMemoryException(): base (null)
        {
            SetErrorCode(__HResults.COR_E_OUTOFMEMORY);
        }

        public OutOfMemoryException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_OUTOFMEMORY);
        }

        public OutOfMemoryException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_OUTOFMEMORY);
        }
    }
}