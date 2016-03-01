namespace System.Threading
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;

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