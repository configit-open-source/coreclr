namespace System.Runtime.InteropServices
{
    using System;
    using System.Runtime.Serialization;

    public class SafeArrayRankMismatchException : SystemException
    {
        public SafeArrayRankMismatchException(): base (Environment.GetResourceString("Arg_SafeArrayRankMismatchException"))
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYRANKMISMATCH);
        }

        public SafeArrayRankMismatchException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYRANKMISMATCH);
        }

        public SafeArrayRankMismatchException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYRANKMISMATCH);
        }

        protected SafeArrayRankMismatchException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}