namespace System.Threading
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;

    public class SemaphoreFullException : SystemException
    {
        public SemaphoreFullException(): base (Environment.GetResourceString("Threading_SemaphoreFullException"))
        {
        }

        public SemaphoreFullException(String message): base (message)
        {
        }

        public SemaphoreFullException(String message, Exception innerException): base (message, innerException)
        {
        }

        protected SemaphoreFullException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}