using System;
using Interlocked = System.Threading.Interlocked;

namespace System.Diagnostics.Tracing
{
    internal static class SimpleEventTypes<T>
    {
        private static TraceLoggingEventTypes instance;
        public static TraceLoggingEventTypes Instance
        {
            get
            {
                return instance ?? InitInstance();
            }
        }

        private static TraceLoggingEventTypes InitInstance()
        {
            var info = TraceLoggingTypeInfo.GetInstance(typeof (T), null);
            var newInstance = new TraceLoggingEventTypes(info.Name, info.Tags, new TraceLoggingTypeInfo[]{info});
            Interlocked.CompareExchange(ref instance, newInstance, null);
            return instance;
        }
    }
}