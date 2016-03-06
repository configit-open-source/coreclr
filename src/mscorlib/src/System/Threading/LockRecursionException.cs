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

        public LockRecursionException(string message, Exception innerException): base (message, innerException)
        {
        }
    }
}