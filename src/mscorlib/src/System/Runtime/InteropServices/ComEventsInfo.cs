namespace System.Runtime.InteropServices
{
    internal class ComEventsInfo
    {
        private ComEventsSink _sinks;
        private object _rcw;
        ComEventsInfo(object rcw)
        {
            _rcw = rcw;
        }

        ~ComEventsInfo()
        {
            _sinks = ComEventsSink.RemoveAll(_sinks);
        }

        internal static ComEventsInfo Find(object rcw)
        {
            return (ComEventsInfo)Marshal.GetComObjectData(rcw, typeof (ComEventsInfo));
        }

        internal static ComEventsInfo FromObject(object rcw)
        {
            ComEventsInfo eventsInfo = Find(rcw);
            if (eventsInfo == null)
            {
                eventsInfo = new ComEventsInfo(rcw);
                Marshal.SetComObjectData(rcw, typeof (ComEventsInfo), eventsInfo);
            }

            return eventsInfo;
        }

        internal ComEventsSink FindSink(ref Guid iid)
        {
            return ComEventsSink.Find(_sinks, ref iid);
        }

        internal ComEventsSink AddSink(ref Guid iid)
        {
            ComEventsSink sink = new ComEventsSink(_rcw, iid);
            _sinks = ComEventsSink.Add(_sinks, sink);
            return _sinks;
        }

        internal ComEventsSink RemoveSink(ComEventsSink sink)
        {
            _sinks = ComEventsSink.Remove(_sinks, sink);
            return _sinks;
        }
    }
}