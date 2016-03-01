namespace System.Runtime.InteropServices
{
    using System;
    using System.Runtime.Remoting;

    public static class ComEventsHelper
    {
        public static void Combine(object rcw, Guid iid, int dispid, System.Delegate d)
        {
            rcw = UnwrapIfTransparentProxy(rcw);
            lock (rcw)
            {
                ComEventsInfo eventsInfo = ComEventsInfo.FromObject(rcw);
                ComEventsSink sink = eventsInfo.FindSink(ref iid);
                if (sink == null)
                {
                    sink = eventsInfo.AddSink(ref iid);
                }

                ComEventsMethod method = sink.FindMethod(dispid);
                if (method == null)
                {
                    method = sink.AddMethod(dispid);
                }

                method.AddDelegate(d);
            }
        }

        public static Delegate Remove(object rcw, Guid iid, int dispid, System.Delegate d)
        {
            rcw = UnwrapIfTransparentProxy(rcw);
            lock (rcw)
            {
                ComEventsInfo eventsInfo = ComEventsInfo.Find(rcw);
                if (eventsInfo == null)
                    return null;
                ComEventsSink sink = eventsInfo.FindSink(ref iid);
                if (sink == null)
                    return null;
                ComEventsMethod method = sink.FindMethod(dispid);
                if (method == null)
                    return null;
                method.RemoveDelegate(d);
                if (method.Empty)
                {
                    method = sink.RemoveMethod(method);
                }

                if (method == null)
                {
                    sink = eventsInfo.RemoveSink(sink);
                }

                if (sink == null)
                {
                    Marshal.SetComObjectData(rcw, typeof (ComEventsInfo), null);
                    GC.SuppressFinalize(eventsInfo);
                }

                return d;
            }
        }

        internal static object UnwrapIfTransparentProxy(object rcw)
        {
            return rcw;
        }
    }
}