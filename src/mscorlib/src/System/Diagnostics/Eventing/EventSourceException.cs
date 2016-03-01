using System;
using System.Runtime.Serialization;

namespace System.Diagnostics.Tracing
{
    public class EventSourceException : Exception
    {
        public EventSourceException(): base (Resources.GetResourceString("EventSource_ListenerWriteFailure"))
        {
        }

        public EventSourceException(string message): base (message)
        {
        }

        public EventSourceException(string message, Exception innerException): base (message, innerException)
        {
        }

        protected EventSourceException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        internal EventSourceException(Exception innerException): base (Resources.GetResourceString("EventSource_ListenerWriteFailure"), innerException)
        {
        }
    }
}