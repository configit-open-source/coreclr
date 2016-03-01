using System.Runtime.Serialization;

namespace System
{
    public class InvalidTimeZoneException : Exception
    {
        public InvalidTimeZoneException(String message): base (message)
        {
        }

        public InvalidTimeZoneException(String message, Exception innerException): base (message, innerException)
        {
        }

        protected InvalidTimeZoneException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public InvalidTimeZoneException()
        {
        }
    }
}