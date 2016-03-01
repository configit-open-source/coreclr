namespace System
{
    using System;
    using System.Runtime.Serialization;

    public class InvalidOperationException : SystemException
    {
        public InvalidOperationException(): base (Environment.GetResourceString("Arg_InvalidOperationException"))
        {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }

        public InvalidOperationException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }

        public InvalidOperationException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_INVALIDOPERATION);
        }

        protected InvalidOperationException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}