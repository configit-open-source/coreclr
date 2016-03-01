namespace System.Diagnostics.Tracing
{
    internal unsafe class TraceLoggingDataCollector
    {
        internal static readonly TraceLoggingDataCollector Instance = new TraceLoggingDataCollector();
        private TraceLoggingDataCollector()
        {
            return;
        }

        public int BeginBufferedArray()
        {
            return DataCollector.ThreadInstance.BeginBufferedArray();
        }

        public void EndBufferedArray(int bookmark, int count)
        {
            DataCollector.ThreadInstance.EndBufferedArray(bookmark, count);
        }

        public TraceLoggingDataCollector AddGroup()
        {
            return this;
        }

        public void AddScalar(PropertyValue value)
        {
            var scalar = value.ScalarValue;
            DataCollector.ThreadInstance.AddScalar(&scalar, value.ScalarLength);
        }

        public void AddScalar(long value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof (long));
        }

        public void AddScalar(double value)
        {
            DataCollector.ThreadInstance.AddScalar(&value, sizeof (double));
        }

        public void AddBinary(string value)
        {
            DataCollector.ThreadInstance.AddBinary(value, value == null ? 0 : value.Length * 2);
        }

        public void AddArray(PropertyValue value, int elementSize)
        {
            Array array = (Array)value.ReferenceValue;
            DataCollector.ThreadInstance.AddArray(array, array == null ? 0 : array.Length, elementSize);
        }
    }
}