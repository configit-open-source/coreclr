using System.Runtime.Serialization;

namespace System
{
    public sealed class InsufficientMemoryException : OutOfMemoryException
    {
        public InsufficientMemoryException(): base (null)
        {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTMEMORY);
        }

        public InsufficientMemoryException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTMEMORY);
        }

        public InsufficientMemoryException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTMEMORY);
        }

    }
}