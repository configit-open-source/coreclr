using System.Runtime.Serialization;

namespace System.Reflection
{
    public class TargetException : Exception
    {
        public TargetException(): base ()
        {
            SetErrorCode(__HResults.COR_E_TARGET);
        }

        public TargetException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TARGET);
        }

        public TargetException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_TARGET);
        }

        protected TargetException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}