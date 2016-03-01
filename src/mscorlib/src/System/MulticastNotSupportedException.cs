namespace System
{
    using System;
    using System.Runtime.Serialization;

    public sealed class MulticastNotSupportedException : SystemException
    {
        public MulticastNotSupportedException(): base (Environment.GetResourceString("Arg_MulticastNotSupportedException"))
        {
            SetErrorCode(__HResults.COR_E_MULTICASTNOTSUPPORTED);
        }

        public MulticastNotSupportedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_MULTICASTNOTSUPPORTED);
        }

        public MulticastNotSupportedException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_MULTICASTNOTSUPPORTED);
        }

        internal MulticastNotSupportedException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}