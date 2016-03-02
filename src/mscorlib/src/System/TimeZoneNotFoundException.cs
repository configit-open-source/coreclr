using System.Runtime.Serialization;

namespace System
{
    public class TimeZoneNotFoundException : Exception
    {
        public TimeZoneNotFoundException(String message): base (message)
        {
        }

        public TimeZoneNotFoundException(String message, Exception innerException): base (message, innerException)
        {
        }

        protected TimeZoneNotFoundException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public TimeZoneNotFoundException()
        {
        }
    }
}