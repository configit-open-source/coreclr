namespace System
{
    using System;
    using System.Runtime.Serialization;

    public sealed class InsufficientMemoryException : OutOfMemoryException
    {
        public InsufficientMemoryException(): base (GetMessageFromNativeResources(ExceptionMessageKind.OutOfMemory))
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

        private InsufficientMemoryException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}