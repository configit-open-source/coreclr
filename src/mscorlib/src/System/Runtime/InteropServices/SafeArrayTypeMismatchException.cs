namespace System.Runtime.InteropServices
{
    using System;
    using System.Runtime.Serialization;

    public class SafeArrayTypeMismatchException : SystemException
    {
        public SafeArrayTypeMismatchException(): base (Environment.GetResourceString("Arg_SafeArrayTypeMismatchException"))
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYTYPEMISMATCH);
        }

        public SafeArrayTypeMismatchException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYTYPEMISMATCH);
        }

        public SafeArrayTypeMismatchException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYTYPEMISMATCH);
        }

        protected SafeArrayTypeMismatchException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}