namespace System.Text
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    public class UTF7Encoding : Encoding
    {
        private const String base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        private const String directChars = "\t\n\r '(),-./0123456789:?ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const String optionalChars = "!\"#$%&*;<=>@[]^_`{|}";
        private byte[] base64Bytes;
        private sbyte[] base64Values;
        private bool[] directEncode;
        private bool m_allowOptionals;
        private const int UTF7_CODEPAGE = 65000;
        public UTF7Encoding(): this (false)
        {
        }

        public UTF7Encoding(bool allowOptionals): base (UTF7_CODEPAGE)
        {
            this.m_allowOptionals = allowOptionals;
            MakeTables();
        }

        private void MakeTables()
        {
            base64Bytes = new byte[64];
            for (int i = 0; i < 64; i++)
                base64Bytes[i] = (byte)base64Chars[i];
            base64Values = new sbyte[128];
            for (int i = 0; i < 128; i++)
                base64Values[i] = -1;
            for (int i = 0; i < 64; i++)
                base64Values[base64Bytes[i]] = (sbyte)i;
            directEncode = new bool[128];
            int count = directChars.Length;
            for (int i = 0; i < count; i++)
            {
                directEncode[directChars[i]] = true;
            }

            if (this.m_allowOptionals)
            {
                count = optionalChars.Length;
                for (int i = 0; i < count; i++)
                {
                    directEncode[optionalChars[i]] = true;
                }
            }
        }

        internal override void SetDefaultFallbacks()
        {
            this.encoderFallback = new EncoderReplacementFallback(String.Empty);
            this.decoderFallback = new DecoderUTF7Fallback();
        }

        private void OnDeserializing(StreamingContext ctx)
        {
            base.OnDeserializing();
        }

        private void OnDeserialized(StreamingContext ctx)
        {
            base.OnDeserialized();
            if (m_deserializedFromEverett)
            {
                m_allowOptionals = directEncode[optionalChars[0]];
            }

            MakeTables();
        }

        public override bool Equals(Object value)
        {
            UTF7Encoding that = value as UTF7Encoding;
            if (that != null)
            {
                return (m_allowOptionals == that.m_allowOptionals) && (EncoderFallback.Equals(that.EncoderFallback)) && (DecoderFallback.Equals(that.DecoderFallback));
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return this.CodePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode();
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();
            if (chars.Length == 0)
                return 0;
            fixed (char *pChars = chars)
                return GetByteCount(pChars + index, count, null);
        }

        public override unsafe int GetByteCount(String s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            Contract.EndContractBlock();
            fixed (char *pChars = s)
                return GetByteCount(pChars, s.Length, null);
        }

        public override unsafe int GetByteCount(char *chars, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            return GetByteCount(chars, count, null);
        }

        public override unsafe int GetBytes(String s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (s == null || bytes == null)
                throw new ArgumentNullException((s == null ? "s" : "bytes"), Environment.GetResourceString("ArgumentNull_Array"));
            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? "charIndex" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (s.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("s", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            int byteCount = bytes.Length - byteIndex;
            if (bytes.Length == 0)
                bytes = new byte[1];
            fixed (char *pChars = s)
                fixed (byte *pBytes = bytes)
                    return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, null);
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"), Environment.GetResourceString("ArgumentNull_Array"));
            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? "charIndex" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            if (chars.Length == 0)
                return 0;
            int byteCount = bytes.Length - byteIndex;
            if (bytes.Length == 0)
                bytes = new byte[1];
            fixed (char *pChars = chars)
                fixed (byte *pBytes = bytes)
                    return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, null);
        }

        public override unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? "charCount" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            return GetBytes(chars, charCount, bytes, byteCount, null);
        }

        public override unsafe int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();
            if (bytes.Length == 0)
                return 0;
            fixed (byte *pBytes = bytes)
                return GetCharCount(pBytes + index, count, null);
        }

        public override unsafe int GetCharCount(byte *bytes, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            return GetCharCount(bytes, count, null);
        }

        public override unsafe int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? "byteIndex" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            if (charIndex < 0 || charIndex > chars.Length)
                throw new ArgumentOutOfRangeException("charIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            if (bytes.Length == 0)
                return 0;
            int charCount = chars.Length - charIndex;
            if (chars.Length == 0)
                chars = new char[1];
            fixed (byte *pBytes = bytes)
                fixed (char *pChars = chars)
                    return GetChars(pBytes + byteIndex, byteCount, pChars + charIndex, charCount, null);
        }

        public unsafe override int GetChars(byte *bytes, int byteCount, char *chars, int charCount)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? "charCount" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            return GetChars(bytes, byteCount, chars, charCount, null);
        }

        public override unsafe String GetString(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (bytes.Length - index < count)
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();
            if (bytes.Length == 0)
                return String.Empty;
            fixed (byte *pBytes = bytes)
                return String.CreateStringFromEncoding(pBytes + index, count, this);
        }

        internal override unsafe int GetByteCount(char *chars, int count, EncoderNLS baseEncoder)
        {
            Contract.Assert(chars != null, "[UTF7Encoding.GetByteCount]chars!=null");
            Contract.Assert(count >= 0, "[UTF7Encoding.GetByteCount]count >=0");
            return GetBytes(chars, count, null, 0, baseEncoder);
        }

        internal override unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, EncoderNLS baseEncoder)
        {
            Contract.Assert(byteCount >= 0, "[UTF7Encoding.GetBytes]byteCount >=0");
            Contract.Assert(chars != null, "[UTF7Encoding.GetBytes]chars!=null");
            Contract.Assert(charCount >= 0, "[UTF7Encoding.GetBytes]charCount >=0");
            UTF7Encoding.Encoder encoder = (UTF7Encoding.Encoder)baseEncoder;
            int bits = 0;
            int bitCount = -1;
            Encoding.EncodingByteBuffer buffer = new Encoding.EncodingByteBuffer(this, encoder, bytes, byteCount, chars, charCount);
            if (encoder != null)
            {
                bits = encoder.bits;
                bitCount = encoder.bitCount;
                while (bitCount >= 6)
                {
                    bitCount -= 6;
                    if (!buffer.AddByte(base64Bytes[(bits >> bitCount) & 0x3F]))
                        ThrowBytesOverflow(encoder, buffer.Count == 0);
                }
            }

            while (buffer.MoreData)
            {
                char currentChar = buffer.GetNextChar();
                if (currentChar < 0x80 && directEncode[currentChar])
                {
                    if (bitCount >= 0)
                    {
                        if (bitCount > 0)
                        {
                            if (!buffer.AddByte(base64Bytes[bits << 6 - bitCount & 0x3F]))
                                break;
                            bitCount = 0;
                        }

                        if (!buffer.AddByte((byte)'-'))
                            break;
                        bitCount = -1;
                    }

                    if (!buffer.AddByte((byte)currentChar))
                        break;
                }
                else if (bitCount < 0 && currentChar == '+')
                {
                    if (!buffer.AddByte((byte)'+', (byte)'-'))
                        break;
                }
                else
                {
                    if (bitCount < 0)
                    {
                        if (!buffer.AddByte((byte)'+'))
                            break;
                        bitCount = 0;
                    }

                    bits = bits << 16 | currentChar;
                    bitCount += 16;
                    while (bitCount >= 6)
                    {
                        bitCount -= 6;
                        if (!buffer.AddByte(base64Bytes[(bits >> bitCount) & 0x3F]))
                        {
                            bitCount += 6;
                            currentChar = buffer.GetNextChar();
                            break;
                        }
                    }

                    if (bitCount >= 6)
                        break;
                }
            }

            if (bitCount >= 0 && (encoder == null || encoder.MustFlush))
            {
                if (bitCount > 0)
                {
                    if (buffer.AddByte(base64Bytes[(bits << (6 - bitCount)) & 0x3F]))
                    {
                        bitCount = 0;
                    }
                }

                if (buffer.AddByte((byte)'-'))
                {
                    bits = 0;
                    bitCount = -1;
                }
                else
                    buffer.GetNextChar();
            }

            if (bytes != null && encoder != null)
            {
                encoder.bits = bits;
                encoder.bitCount = bitCount;
                encoder.m_charsUsed = buffer.CharsUsed;
            }

            return buffer.Count;
        }

        internal override unsafe int GetCharCount(byte *bytes, int count, DecoderNLS baseDecoder)
        {
            Contract.Assert(count >= 0, "[UTF7Encoding.GetCharCount]count >=0");
            Contract.Assert(bytes != null, "[UTF7Encoding.GetCharCount]bytes!=null");
            return GetChars(bytes, count, null, 0, baseDecoder);
        }

        internal override unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, DecoderNLS baseDecoder)
        {
            Contract.Assert(byteCount >= 0, "[UTF7Encoding.GetChars]byteCount >=0");
            Contract.Assert(bytes != null, "[UTF7Encoding.GetChars]bytes!=null");
            Contract.Assert(charCount >= 0, "[UTF7Encoding.GetChars]charCount >=0");
            UTF7Encoding.Decoder decoder = (UTF7Encoding.Decoder)baseDecoder;
            Encoding.EncodingCharBuffer buffer = new Encoding.EncodingCharBuffer(this, decoder, chars, charCount, bytes, byteCount);
            int bits = 0;
            int bitCount = -1;
            bool firstByte = false;
            if (decoder != null)
            {
                bits = decoder.bits;
                bitCount = decoder.bitCount;
                firstByte = decoder.firstByte;
                Contract.Assert(firstByte == false || decoder.bitCount <= 0, "[UTF7Encoding.GetChars]If remembered bits, then first byte flag shouldn't be set");
            }

            if (bitCount >= 16)
            {
                if (!buffer.AddChar((char)((bits >> (bitCount - 16)) & 0xFFFF)))
                    ThrowCharsOverflow(decoder, true);
                bitCount -= 16;
            }

            while (buffer.MoreData)
            {
                byte currentByte = buffer.GetNextByte();
                int c;
                if (bitCount >= 0)
                {
                    sbyte v;
                    if (currentByte < 0x80 && ((v = base64Values[currentByte]) >= 0))
                    {
                        firstByte = false;
                        bits = (bits << 6) | ((byte)v);
                        bitCount += 6;
                        if (bitCount >= 16)
                        {
                            c = (bits >> (bitCount - 16)) & 0xFFFF;
                            bitCount -= 16;
                        }
                        else
                            continue;
                    }
                    else
                    {
                        bitCount = -1;
                        if (currentByte != '-')
                        {
                            if (!buffer.Fallback(currentByte))
                                break;
                            continue;
                        }

                        if (firstByte)
                            c = '+';
                        else
                            continue;
                    }
                }
                else if (currentByte == '+')
                {
                    bitCount = 0;
                    firstByte = true;
                    continue;
                }
                else
                {
                    if (currentByte >= 0x80)
                    {
                        if (!buffer.Fallback(currentByte))
                            break;
                        continue;
                    }

                    c = currentByte;
                }

                if (c >= 0)
                {
                    if (!buffer.AddChar((char)c))
                    {
                        if (bitCount >= 0)
                        {
                            buffer.AdjustBytes(+1);
                            bitCount += 16;
                        }

                        break;
                    }
                }
            }

            if (chars != null && decoder != null)
            {
                if (decoder.MustFlush)
                {
                    decoder.bits = 0;
                    decoder.bitCount = -1;
                    decoder.firstByte = false;
                }
                else
                {
                    decoder.bits = bits;
                    decoder.bitCount = bitCount;
                    decoder.firstByte = firstByte;
                }

                decoder.m_bytesUsed = buffer.BytesUsed;
            }

            return buffer.Count;
        }

        public override System.Text.Decoder GetDecoder()
        {
            return new UTF7Encoding.Decoder(this);
        }

        public override System.Text.Encoder GetEncoder()
        {
            return new UTF7Encoding.Encoder(this);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            long byteCount = (long)charCount * 3 + 2;
            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));
            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            int charCount = byteCount;
            if (charCount == 0)
                charCount = 1;
            return charCount;
        }

        private class Decoder : DecoderNLS, ISerializable
        {
            internal int bits;
            internal int bitCount;
            internal bool firstByte;
            public Decoder(UTF7Encoding encoding): base (encoding)
            {
            }

            internal Decoder(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");
                Contract.EndContractBlock();
                this.bits = (int)info.GetValue("bits", typeof (int));
                this.bitCount = (int)info.GetValue("bitCount", typeof (int));
                this.firstByte = (bool)info.GetValue("firstByte", typeof (bool));
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof (Encoding));
            }

            public override void Reset()
            {
                this.bits = 0;
                this.bitCount = -1;
                this.firstByte = false;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            internal override bool HasState
            {
                get
                {
                    return (this.bitCount != -1);
                }
            }
        }

        private class Encoder : EncoderNLS, ISerializable
        {
            internal int bits;
            internal int bitCount;
            public Encoder(UTF7Encoding encoding): base (encoding)
            {
            }

            internal Encoder(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");
                Contract.EndContractBlock();
                this.bits = (int)info.GetValue("bits", typeof (int));
                this.bitCount = (int)info.GetValue("bitCount", typeof (int));
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof (Encoding));
            }

            public override void Reset()
            {
                this.bitCount = -1;
                this.bits = 0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            internal override bool HasState
            {
                get
                {
                    return (this.bits != 0 || this.bitCount != -1);
                }
            }
        }

        internal sealed class DecoderUTF7Fallback : DecoderFallback
        {
            public DecoderUTF7Fallback()
            {
            }

            public override DecoderFallbackBuffer CreateFallbackBuffer()
            {
                return new DecoderUTF7FallbackBuffer(this);
            }

            public override int MaxCharCount
            {
                get
                {
                    return 1;
                }
            }

            public override bool Equals(Object value)
            {
                DecoderUTF7Fallback that = value as DecoderUTF7Fallback;
                if (that != null)
                {
                    return true;
                }

                return (false);
            }

            public override int GetHashCode()
            {
                return 984;
            }
        }

        internal sealed class DecoderUTF7FallbackBuffer : DecoderFallbackBuffer
        {
            char cFallback = (char)0;
            int iCount = -1;
            int iSize;
            public DecoderUTF7FallbackBuffer(DecoderUTF7Fallback fallback)
            {
            }

            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                Contract.Assert(iCount < 0, "[DecoderUTF7FallbackBuffer.Fallback] Can't have recursive fallbacks");
                Contract.Assert(bytesUnknown.Length == 1, "[DecoderUTF7FallbackBuffer.Fallback] Only possible fallback case should be 1 unknown byte");
                cFallback = (char)bytesUnknown[0];
                if (cFallback == 0)
                {
                    return false;
                }

                iCount = iSize = 1;
                return true;
            }

            public override char GetNextChar()
            {
                if (iCount-- > 0)
                    return cFallback;
                return (char)0;
            }

            public override bool MovePrevious()
            {
                if (iCount >= 0)
                {
                    iCount++;
                }

                return (iCount >= 0 && iCount <= iSize);
            }

            public override int Remaining
            {
                get
                {
                    return (iCount > 0) ? iCount : 0;
                }
            }

            public override unsafe void Reset()
            {
                iCount = -1;
                byteStart = null;
            }

            internal unsafe override int InternalFallback(byte[] bytes, byte *pBytes)
            {
                Contract.Assert(iCount < 0, "[DecoderUTF7FallbackBuffer.InternalFallback] Can't have recursive fallbacks");
                if (bytes.Length != 1)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"));
                }

                return bytes[0] == 0 ? 0 : 1;
            }
        }
    }
}