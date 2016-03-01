namespace System
{
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;

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