using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
    internal sealed class EnumerableTypeInfo : TraceLoggingTypeInfo
    {
        private readonly TraceLoggingTypeInfo elementInfo;
        public EnumerableTypeInfo(Type type, TraceLoggingTypeInfo elementInfo): base (type)
        {
            this.elementInfo = elementInfo;
        }

        public override void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format)
        {
            collector.BeginBufferedArray();
            this.elementInfo.WriteMetadata(collector, name, format);
            collector.EndBufferedArray();
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            var bookmark = collector.BeginBufferedArray();
            var count = 0;
            IEnumerable enumerable = (IEnumerable)value.ReferenceValue;
            if (enumerable != null)
            {
                foreach (var element in enumerable)
                {
                    this.elementInfo.WriteData(collector, elementInfo.PropertyValueFactory(element));
                    count++;
                }
            }

            collector.EndBufferedArray(bookmark, count);
        }

        public override object GetData(object value)
        {
            var iterType = (IEnumerable)value;
            List<object> serializedEnumerable = new List<object>();
            foreach (var element in iterType)
            {
                serializedEnumerable.Add(elementInfo.GetData(element));
            }

            return serializedEnumerable.ToArray();
        }
    }
}