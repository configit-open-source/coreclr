namespace System
{
    using System;
    using System.Runtime.Serialization;

    public class MemberAccessException : SystemException
    {
        public MemberAccessException(): base (Environment.GetResourceString("Arg_AccessException"))
        {
            SetErrorCode(__HResults.COR_E_MEMBERACCESS);
        }

        public MemberAccessException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_MEMBERACCESS);
        }

        public MemberAccessException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_MEMBERACCESS);
        }

        protected MemberAccessException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}