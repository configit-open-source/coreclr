namespace System.Collections.Generic
{
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;

    public class KeyNotFoundException : SystemException, ISerializable
    {
        public KeyNotFoundException(): base (Environment.GetResourceString("Arg_KeyNotFound"))
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }

        public KeyNotFoundException(String message): base (message)
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }

        public KeyNotFoundException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }

        protected KeyNotFoundException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}