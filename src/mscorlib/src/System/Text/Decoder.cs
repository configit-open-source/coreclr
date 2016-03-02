
using System.Runtime.Serialization;

namespace System.Text
{
    public abstract class Decoder
    {
        internal DecoderFallback m_fallback = null;
        internal DecoderFallbackBuffer m_fallbackBuffer = null;
        internal void SerializeDecoder(SerializationInfo info)
        {
            info.AddValue("m_fallback", this.m_fallback);
        }

        protected Decoder()
        {
        }

        public DecoderFallback Fallback
        {
            get
            {
                return m_fallback;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                                if (m_fallbackBuffer != null && m_fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(Environment.GetResourceString("Argument_FallbackBufferNotEmpty"), "value");
                m_fallback = value;
                m_fallbackBuffer = null;
            }
        }

        public DecoderFallbackBuffer FallbackBuffer
        {
            get
            {
                if (m_fallbackBuffer == null)
                {
                    if (m_fallback != null)
                        m_fallbackBuffer = m_fallback.CreateFallbackBuffer();
                    else
                        m_fallbackBuffer = DecoderFallback.ReplacementFallback.CreateFallbackBuffer();
                }

                return m_fallbackBuffer;
            }
        }

        internal bool InternalHasFallbackBuffer
        {
            get
            {
                return m_fallbackBuffer != null;
            }
        }

        public virtual void Reset()
        {
            byte[] byteTemp = {};
            char[] charTemp = new char[GetCharCount(byteTemp, 0, 0, true)];
            GetChars(byteTemp, 0, 0, charTemp, 0, true);
            if (m_fallbackBuffer != null)
                m_fallbackBuffer.Reset();
        }

        public abstract int GetCharCount(byte[] bytes, int index, int count);
        public virtual int GetCharCount(byte[] bytes, int index, int count, bool flush)
        {
            return GetCharCount(bytes, index, count);
        }

        public virtual unsafe int GetCharCount(byte *bytes, int count, bool flush)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        byte[] arrbyte = new byte[count];
            int index;
            for (index = 0; index < count; index++)
                arrbyte[index] = bytes[index];
            return GetCharCount(arrbyte, 0, count);
        }

        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        public virtual int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
        {
            return GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public virtual unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, bool flush)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? "byteCount" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        byte[] arrByte = new byte[byteCount];
            int index;
            for (index = 0; index < byteCount; index++)
                arrByte[index] = bytes[index];
            char[] arrChar = new char[charCount];
            int result = GetChars(arrByte, 0, byteCount, arrChar, 0, flush);
                        if (result < charCount)
                charCount = result;
            for (index = 0; index < charCount; index++)
                chars[index] = arrChar[index];
            return charCount;
        }

        public virtual void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException((bytes == null ? "bytes" : "chars"), Environment.GetResourceString("ArgumentNull_Array"));
            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? "byteIndex" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? "charIndex" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
                        bytesUsed = byteCount;
            while (bytesUsed > 0)
            {
                if (GetCharCount(bytes, byteIndex, bytesUsed, flush) <= charCount)
                {
                    charsUsed = GetChars(bytes, byteIndex, bytesUsed, chars, charIndex, flush);
                    completed = (bytesUsed == byteCount && (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0));
                    return;
                }

                flush = false;
                bytesUsed /= 2;
            }

            throw new ArgumentException(Environment.GetResourceString("Argument_ConversionOverflow"));
        }

        public virtual unsafe void Convert(byte *bytes, int byteCount, char *chars, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? "byteCount" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        bytesUsed = byteCount;
            while (bytesUsed > 0)
            {
                if (GetCharCount(bytes, bytesUsed, flush) <= charCount)
                {
                    charsUsed = GetChars(bytes, bytesUsed, chars, charCount, flush);
                    completed = (bytesUsed == byteCount && (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0));
                    return;
                }

                flush = false;
                bytesUsed /= 2;
            }

            throw new ArgumentException(Environment.GetResourceString("Argument_ConversionOverflow"));
        }
    }
}