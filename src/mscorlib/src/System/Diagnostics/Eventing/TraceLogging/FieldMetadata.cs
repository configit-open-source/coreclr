using System.Text;

namespace System.Diagnostics.Tracing
{
    internal class FieldMetadata
    {
        private readonly string name;
        private readonly int nameSize;
        private readonly EventFieldTags tags;
        private readonly byte[] custom;
        private readonly ushort fixedCount;
        private byte inType;
        private byte outType;
        public FieldMetadata(string name, TraceLoggingDataType type, EventFieldTags tags, bool variableCount): this (name, type, tags, variableCount ? Statics.InTypeVariableCountFlag : (byte)0, 0, null)
        {
            return;
        }

        public FieldMetadata(string name, TraceLoggingDataType type, EventFieldTags tags, ushort fixedCount): this (name, type, tags, Statics.InTypeFixedCountFlag, fixedCount, null)
        {
            return;
        }

        public FieldMetadata(string name, TraceLoggingDataType type, EventFieldTags tags, byte[] custom): this (name, type, tags, Statics.InTypeCustomCountFlag, checked ((ushort)(custom == null ? 0 : custom.Length)), custom)
        {
            return;
        }

        private FieldMetadata(string name, TraceLoggingDataType dataType, EventFieldTags tags, byte countFlags, ushort fixedCount = 0, byte[] custom = null)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", "This usually means that the object passed to Write is of a type that" + " does not support being used as the top-level object in an event," + " e.g. a primitive or built-in type.");
            }

            Statics.CheckName(name);
            var coreType = (int)dataType & Statics.InTypeMask;
            this.name = name;
            this.nameSize = Encoding.UTF8.GetByteCount(this.name) + 1;
            this.inType = (byte)(coreType | countFlags);
            this.outType = (byte)(((int)dataType >> 8) & Statics.OutTypeMask);
            this.tags = tags;
            this.fixedCount = fixedCount;
            this.custom = custom;
            if (countFlags != 0)
            {
                if (coreType == (int)TraceLoggingDataType.Nil)
                {
                    throw new NotSupportedException(Resources.GetResourceString("EventSource_NotSupportedArrayOfNil"));
                }

                if (coreType == (int)TraceLoggingDataType.Binary)
                {
                    throw new NotSupportedException(Resources.GetResourceString("EventSource_NotSupportedArrayOfBinary"));
                }

                if (coreType == (int)TraceLoggingDataType.Utf16String || coreType == (int)TraceLoggingDataType.MbcsString)
                {
                    throw new NotSupportedException(Resources.GetResourceString("EventSource_NotSupportedArrayOfNullTerminatedString"));
                }
            }

            if (((int)this.tags & 0xfffffff) != 0)
            {
                this.outType |= Statics.OutTypeChainFlag;
            }

            if (this.outType != 0)
            {
                this.inType |= Statics.InTypeChainFlag;
            }
        }

        public void IncrementStructFieldCount()
        {
            this.inType |= Statics.InTypeChainFlag;
            this.outType++;
            if ((this.outType & Statics.OutTypeMask) == 0)
            {
                throw new NotSupportedException(Resources.GetResourceString("EventSource_TooManyFields"));
            }
        }

        public void Encode(ref int pos, byte[] metadata)
        {
            if (metadata != null)
            {
                Encoding.UTF8.GetBytes(this.name, 0, this.name.Length, metadata, pos);
            }

            pos += this.nameSize;
            if (metadata != null)
            {
                metadata[pos] = this.inType;
            }

            pos += 1;
            if (0 != (this.inType & Statics.InTypeChainFlag))
            {
                if (metadata != null)
                {
                    metadata[pos] = this.outType;
                }

                pos += 1;
                if (0 != (this.outType & Statics.OutTypeChainFlag))
                {
                    Statics.EncodeTags((int)this.tags, ref pos, metadata);
                }
            }

            if (0 != (this.inType & Statics.InTypeFixedCountFlag))
            {
                if (metadata != null)
                {
                    metadata[pos + 0] = unchecked ((byte)this.fixedCount);
                    metadata[pos + 1] = (byte)(this.fixedCount >> 8);
                }

                pos += 2;
                if (Statics.InTypeCustomCountFlag == (this.inType & Statics.InTypeCountMask) && this.fixedCount != 0)
                {
                    if (metadata != null)
                    {
                        Buffer.BlockCopy(this.custom, 0, metadata, pos, this.fixedCount);
                    }

                    pos += this.fixedCount;
                }
            }
        }
    }
}