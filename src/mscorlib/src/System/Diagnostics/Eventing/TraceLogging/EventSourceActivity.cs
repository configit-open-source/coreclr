using System.Diagnostics.Contracts;

namespace System.Diagnostics.Tracing
{
    internal sealed class EventSourceActivity : IDisposable
    {
        public EventSourceActivity(EventSource eventSource)
        {
            if (eventSource == null)
                throw new ArgumentNullException("eventSource");
            Contract.EndContractBlock();
            this.eventSource = eventSource;
        }

        public static implicit operator EventSourceActivity(EventSource eventSource)
        {
            return new EventSourceActivity(eventSource);
        }

        public EventSource EventSource
        {
            get
            {
                return this.eventSource;
            }
        }

        public Guid Id
        {
            get
            {
                return this.activityId;
            }
        }

        public EventSourceActivity Start<T>(string eventName, EventSourceOptions options, T data)
        {
            return this.Start(eventName, ref options, ref data);
        }

        public EventSourceActivity Start(string eventName)
        {
            var options = new EventSourceOptions();
            var data = new EmptyStruct();
            return this.Start(eventName, ref options, ref data);
        }

        public EventSourceActivity Start(string eventName, EventSourceOptions options)
        {
            var data = new EmptyStruct();
            return this.Start(eventName, ref options, ref data);
        }

        public EventSourceActivity Start<T>(string eventName, T data)
        {
            var options = new EventSourceOptions();
            return this.Start(eventName, ref options, ref data);
        }

        public void Stop<T>(T data)
        {
            this.Stop(null, ref data);
        }

        public void Stop<T>(string eventName)
        {
            var data = new EmptyStruct();
            this.Stop(eventName, ref data);
        }

        public void Stop<T>(string eventName, T data)
        {
            this.Stop(eventName, ref data);
        }

        public void Write<T>(string eventName, EventSourceOptions options, T data)
        {
            this.Write(this.eventSource, eventName, ref options, ref data);
        }

        public void Write<T>(string eventName, T data)
        {
            var options = new EventSourceOptions();
            this.Write(this.eventSource, eventName, ref options, ref data);
        }

        public void Write(string eventName, EventSourceOptions options)
        {
            var data = new EmptyStruct();
            this.Write(this.eventSource, eventName, ref options, ref data);
        }

        public void Write(string eventName)
        {
            var options = new EventSourceOptions();
            var data = new EmptyStruct();
            this.Write(this.eventSource, eventName, ref options, ref data);
        }

        public void Write<T>(EventSource source, string eventName, EventSourceOptions options, T data)
        {
            this.Write(source, eventName, ref options, ref data);
        }

        public void Dispose()
        {
            if (this.state == State.Started)
            {
                var data = new EmptyStruct();
                this.Stop(null, ref data);
            }
        }

        private EventSourceActivity Start<T>(string eventName, ref EventSourceOptions options, ref T data)
        {
            if (this.state != State.Started)
                throw new InvalidOperationException();
            if (!this.eventSource.IsEnabled())
                return this;
            var newActivity = new EventSourceActivity(eventSource);
            if (!this.eventSource.IsEnabled(options.Level, options.Keywords))
            {
                Guid relatedActivityId = this.Id;
                newActivity.activityId = Guid.NewGuid();
                newActivity.startStopOptions = options;
                newActivity.eventName = eventName;
                newActivity.startStopOptions.Opcode = EventOpcode.Start;
                this.eventSource.Write(eventName, ref newActivity.startStopOptions, ref newActivity.activityId, ref relatedActivityId, ref data);
            }
            else
            {
                newActivity.activityId = this.Id;
            }

            return newActivity;
        }

        private void Write<T>(EventSource eventSource, string eventName, ref EventSourceOptions options, ref T data)
        {
            if (this.state != State.Started)
                throw new InvalidOperationException();
            if (eventName == null)
                throw new ArgumentNullException();
            eventSource.Write(eventName, ref options, ref this.activityId, ref s_empty, ref data);
        }

        private void Stop<T>(string eventName, ref T data)
        {
            if (this.state != State.Started)
                throw new InvalidOperationException();
            if (!StartEventWasFired)
                return;
            this.state = State.Stopped;
            if (eventName == null)
            {
                eventName = this.eventName;
                if (eventName.EndsWith("Start"))
                    eventName = eventName.Substring(0, eventName.Length - 5);
                eventName = eventName + "Stop";
            }

            this.startStopOptions.Opcode = EventOpcode.Stop;
            this.eventSource.Write(eventName, ref this.startStopOptions, ref this.activityId, ref s_empty, ref data);
        }

        private enum State
        {
            Started,
            Stopped
        }

        private bool StartEventWasFired
        {
            get
            {
                return eventName != null;
            }
        }

        private readonly EventSource eventSource;
        private EventSourceOptions startStopOptions;
        internal Guid activityId;
        private State state;
        private string eventName;
        static internal Guid s_empty;
    }
}