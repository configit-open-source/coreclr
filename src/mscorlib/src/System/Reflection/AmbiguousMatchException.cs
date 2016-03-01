namespace System.Reflection
{
    using System;
    using SystemException = System.SystemException;
    using System.Runtime.Serialization;

    public sealed class AmbiguousMatchException : SystemException
    {
        public AmbiguousMatchException(): base (Environment.GetResourceString("RFLCT.Ambiguous"))
        {
            SetErrorCode(__HResults.COR_E_AMBIGUOUSMATCH);
        }

        public AmbiguousMatchException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_AMBIGUOUSMATCH);
        }

        public AmbiguousMatchException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_AMBIGUOUSMATCH);
        }

        internal AmbiguousMatchException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}