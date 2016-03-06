using System.Runtime.Serialization;

namespace System.Threading
{
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
    }
}