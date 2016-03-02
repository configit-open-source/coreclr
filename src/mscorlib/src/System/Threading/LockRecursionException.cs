using System.Runtime.Serialization;

namespace System.Threading
{
    public class LockRecursionException : System.Exception
    {
        public LockRecursionException()
        {
        }

        public LockRecursionException(string message): base (message)
        {
        }

        protected LockRecursionException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public LockRecursionException(string message, Exception innerException): base (message, innerException)
        {
        }
    }
}