namespace System.Diagnostics.Tracing
{
    internal sealed class ArrayTypeInfo : TraceLoggingTypeInfo
    {
        private readonly TraceLoggingTypeInfo elementInfo;
        public ArrayTypeInfo(Type type, TraceLoggingTypeInfo elementInfo): base (type)
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
            Array array = (Array)value.ReferenceValue;
            if (array != null)
            {
                count = array.Length;
                for (int i = 0; i < array.Length; i++)
                {
                    this.elementInfo.WriteData(collector, elementInfo.PropertyValueFactory(array.GetValue(i)));
                }
            }

            collector.EndBufferedArray(bookmark, count);
        }

        public override object GetData(object value)
        {
            var array = (Array)value;
            var serializedArray = new object[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                serializedArray[i] = this.elementInfo.GetData(array.GetValue(i));
            }

            return serializedArray;
        }
    }
}