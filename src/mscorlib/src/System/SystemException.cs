namespace System
{
    using System;
    using System.Runtime.Serialization;

    public class SystemException : Exception
    {
        public SystemException(): base (Environment.GetResourceString("Arg_SystemException"))
        {
            SetErrorCode(__HResults.COR_E_SYSTEM);
        }

        public SystemException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_SYSTEM);
        }

        public SystemException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_SYSTEM);
        }

        protected SystemException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}