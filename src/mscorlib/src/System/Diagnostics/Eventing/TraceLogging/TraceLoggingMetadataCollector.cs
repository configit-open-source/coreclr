using System.Collections.Generic;

namespace System.Diagnostics.Tracing
{
    internal class TraceLoggingMetadataCollector
    {
        private readonly Impl impl;
        private readonly FieldMetadata currentGroup;
        private int bufferedArrayFieldCount = int.MinValue;
        internal TraceLoggingMetadataCollector()
        {
            this.impl = new Impl();
        }

        private TraceLoggingMetadataCollector(TraceLoggingMetadataCollector other, FieldMetadata group)
        {
            this.impl = other.impl;
            this.currentGroup = group;
        }

        internal EventFieldTags Tags
        {
            get;
            set;
        }

        internal int ScratchSize
        {
            get
            {
                return this.impl.scratchSize;
            }
        }

        internal int DataCount
        {
            get
            {
                return this.impl.dataCount;
            }
        }

        internal int PinCount
        {
            get
            {
                return this.impl.pinCount;
            }
        }

        private bool BeginningBufferedArray
        {
            get
            {
                return this.bufferedArrayFieldCount == 0;
            }
        }

        public TraceLoggingMetadataCollector AddGroup(string name)
        {
            TraceLoggingMetadataCollector result = this;
            if (name != null || this.BeginningBufferedArray)
            {
                var newGroup = new FieldMetadata(name, TraceLoggingDataType.Struct, 0, this.BeginningBufferedArray);
                this.AddField(newGroup);
                result = new TraceLoggingMetadataCollector(this, newGroup);
            }

            return result;
        }

        public void AddScalar(string name, TraceLoggingDataType type)
        {
            int size;
            switch ((TraceLoggingDataType)((int)type & Statics.InTypeMask))
            {
                case TraceLoggingDataType.Int8:
                case TraceLoggingDataType.UInt8:
                case TraceLoggingDataType.Char8:
                    size = 1;
                    break;
                case TraceLoggingDataType.Int16:
                case TraceLoggingDataType.UInt16:
                case TraceLoggingDataType.Char16:
                    size = 2;
                    break;
                case TraceLoggingDataType.Int32:
                case TraceLoggingDataType.UInt32:
                case TraceLoggingDataType.HexInt32:
                case TraceLoggingDataType.Float:
                case TraceLoggingDataType.Boolean32:
                    size = 4;
                    break;
                case TraceLoggingDataType.Int64:
                case TraceLoggingDataType.UInt64:
                case TraceLoggingDataType.HexInt64:
                case TraceLoggingDataType.Double:
                case TraceLoggingDataType.FileTime:
                    size = 8;
                    break;
                case TraceLoggingDataType.Guid:
                case TraceLoggingDataType.SystemTime:
                    size = 16;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }

            this.impl.AddScalar(size);
            this.AddField(new FieldMetadata(name, type, this.Tags, this.BeginningBufferedArray));
        }

        public void AddBinary(string name, TraceLoggingDataType type)
        {
            switch ((TraceLoggingDataType)((int)type & Statics.InTypeMask))
            {
                case TraceLoggingDataType.Binary:
                case TraceLoggingDataType.CountedMbcsString:
                case TraceLoggingDataType.CountedUtf16String:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }

            this.impl.AddScalar(2);
            this.impl.AddNonscalar();
            this.AddField(new FieldMetadata(name, type, this.Tags, this.BeginningBufferedArray));
        }

        public void AddArray(string name, TraceLoggingDataType type)
        {
            switch ((TraceLoggingDataType)((int)type & Statics.InTypeMask))
            {
                case TraceLoggingDataType.Utf16String:
                case TraceLoggingDataType.MbcsString:
                case TraceLoggingDataType.Int8:
                case TraceLoggingDataType.UInt8:
                case TraceLoggingDataType.Int16:
                case TraceLoggingDataType.UInt16:
                case TraceLoggingDataType.Int32:
                case TraceLoggingDataType.UInt32:
                case TraceLoggingDataType.Int64:
                case TraceLoggingDataType.UInt64:
                case TraceLoggingDataType.Float:
                case TraceLoggingDataType.Double:
                case TraceLoggingDataType.Boolean32:
                case TraceLoggingDataType.Guid:
                case TraceLoggingDataType.FileTime:
                case TraceLoggingDataType.HexInt32:
                case TraceLoggingDataType.HexInt64:
                case TraceLoggingDataType.Char16:
                case TraceLoggingDataType.Char8:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }

            if (this.BeginningBufferedArray)
            {
                throw new NotSupportedException(Resources.GetResourceString("EventSource_NotSupportedNestedArraysEnums"));
            }

            this.impl.AddScalar(2);
            this.impl.AddNonscalar();
            this.AddField(new FieldMetadata(name, type, this.Tags, true));
        }

        public void BeginBufferedArray()
        {
            if (this.bufferedArrayFieldCount >= 0)
            {
                throw new NotSupportedException(Resources.GetResourceString("EventSource_NotSupportedNestedArraysEnums"));
            }

            this.bufferedArrayFieldCount = 0;
            this.impl.BeginBuffered();
        }

        public void EndBufferedArray()
        {
            if (this.bufferedArrayFieldCount != 1)
            {
                throw new InvalidOperationException(Resources.GetResourceString("EventSource_IncorrentlyAuthoredTypeInfo"));
            }

            this.bufferedArrayFieldCount = int.MinValue;
            this.impl.EndBuffered();
        }

        public void AddCustom(string name, TraceLoggingDataType type, byte[] metadata)
        {
            if (this.BeginningBufferedArray)
            {
                throw new NotSupportedException(Resources.GetResourceString("EventSource_NotSupportedCustomSerializedData"));
            }

            this.impl.AddScalar(2);
            this.impl.AddNonscalar();
            this.AddField(new FieldMetadata(name, type, this.Tags, metadata));
        }

        internal byte[] GetMetadata()
        {
            var size = this.impl.Encode(null);
            var metadata = new byte[size];
            this.impl.Encode(metadata);
            return metadata;
        }

        private void AddField(FieldMetadata fieldMetadata)
        {
            this.Tags = EventFieldTags.None;
            this.bufferedArrayFieldCount++;
            this.impl.fields.Add(fieldMetadata);
            if (this.currentGroup != null)
            {
                this.currentGroup.IncrementStructFieldCount();
            }
        }

        private class Impl
        {
            internal readonly List<FieldMetadata> fields = new List<FieldMetadata>();
            internal short scratchSize;
            internal sbyte dataCount;
            internal sbyte pinCount;
            private int bufferNesting;
            private bool scalar;
            public void AddScalar(int size)
            {
                if (this.bufferNesting == 0)
                {
                    if (!this.scalar)
                    {
                        this.dataCount = checked ((sbyte)(this.dataCount + 1));
                    }

                    this.scalar = true;
                    this.scratchSize = checked ((short)(this.scratchSize + size));
                }
            }

            public void AddNonscalar()
            {
                if (this.bufferNesting == 0)
                {
                    this.scalar = false;
                    this.pinCount = checked ((sbyte)(this.pinCount + 1));
                    this.dataCount = checked ((sbyte)(this.dataCount + 1));
                }
            }

            public void BeginBuffered()
            {
                if (this.bufferNesting == 0)
                {
                    this.AddNonscalar();
                }

                this.bufferNesting++;
            }

            public void EndBuffered()
            {
                this.bufferNesting--;
            }

            public int Encode(byte[] metadata)
            {
                int size = 0;
                foreach (var field in this.fields)
                {
                    field.Encode(ref size, metadata);
                }

                return size;
            }
        }
    }
}