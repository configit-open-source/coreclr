using System.Runtime.Serialization;

namespace System.Reflection
{
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
    }
}