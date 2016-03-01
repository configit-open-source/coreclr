using System;
using System.Runtime.Serialization;

namespace System
{
    public class UnauthorizedAccessException : SystemException
    {
        public UnauthorizedAccessException(): base (Environment.GetResourceString("Arg_UnauthorizedAccessException"))
        {
            SetErrorCode(__HResults.COR_E_UNAUTHORIZEDACCESS);
        }

        public UnauthorizedAccessException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_UNAUTHORIZEDACCESS);
        }

        public UnauthorizedAccessException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_UNAUTHORIZEDACCESS);
        }

        protected UnauthorizedAccessException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}