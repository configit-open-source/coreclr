namespace System.Runtime.InteropServices
{
    using System;
    using System.Runtime.Serialization;

    public class MarshalDirectiveException : SystemException
    {
        public MarshalDirectiveException(): base (Environment.GetResourceString("Arg_MarshalDirectiveException"))
        {
            SetErrorCode(__HResults.COR_E_MARSHALDIRECTIVE);
        }

        public MarshalDirectiveException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_MARSHALDIRECTIVE);
        }

        public MarshalDirectiveException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_MARSHALDIRECTIVE);
        }

        protected MarshalDirectiveException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}