
using System.Runtime.Serialization;

namespace System.Text
{
    public abstract class Encoder
    {
        internal EncoderFallback m_fallback = null;
        internal EncoderFallbackBuffer m_fallbackBuffer = null;
        internal void SerializeEncoder(SerializationInfo info)
        {
            info.AddValue("m_fallback", this.m_fallback);
        }

        protected Encoder()
        {
        }

        public EncoderFallback Fallback
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

        public EncoderFallbackBuffer FallbackBuffer
        {
            get
            {
                if (m_fallbackBuffer == null)
                {
                    if (m_fallback != null)
                        m_fallbackBuffer = m_fallback.CreateFallbackBuffer();
                    else
                        m_fallbackBuffer = EncoderFallback.ReplacementFallback.CreateFallbackBuffer();
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
            char[] charTemp = {};
            byte[] byteTemp = new byte[GetByteCount(charTemp, 0, 0, true)];
            GetBytes(charTemp, 0, 0, byteTemp, 0, true);
            if (m_fallbackBuffer != null)
                m_fallbackBuffer.Reset();
        }

        public abstract int GetByteCount(char[] chars, int index, int count, bool flush);
        public virtual unsafe int GetByteCount(char *chars, int count, bool flush)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        char[] arrChar = new char[count];
            int index;
            for (index = 0; index < count; index++)
                arrChar[index] = chars[index];
            return GetByteCount(arrChar, 0, count, flush);
        }

        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush);
        public virtual unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, bool flush)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? "charCount" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        char[] arrChar = new char[charCount];
            int index;
            for (index = 0; index < charCount; index++)
                arrChar[index] = chars[index];
            byte[] arrByte = new byte[byteCount];
            int result = GetBytes(arrChar, 0, charCount, arrByte, 0, flush);
                        if (result < byteCount)
                byteCount = result;
            for (index = 0; index < byteCount; index++)
                bytes[index] = arrByte[index];
            return byteCount;
        }

        public virtual void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"), Environment.GetResourceString("ArgumentNull_Array"));
            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? "charIndex" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? "byteIndex" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
                        charsUsed = charCount;
            while (charsUsed > 0)
            {
                if (GetByteCount(chars, charIndex, charsUsed, flush) <= byteCount)
                {
                    bytesUsed = GetBytes(chars, charIndex, charsUsed, bytes, byteIndex, flush);
                    completed = (charsUsed == charCount && (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0));
                    return;
                }

                flush = false;
                charsUsed /= 2;
            }

            throw new ArgumentException(Environment.GetResourceString("Argument_ConversionOverflow"));
        }

        public virtual unsafe void Convert(char *chars, int charCount, byte *bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? "charCount" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        charsUsed = charCount;
            while (charsUsed > 0)
            {
                if (GetByteCount(chars, charsUsed, flush) <= byteCount)
                {
                    bytesUsed = GetBytes(chars, charsUsed, bytes, byteCount, flush);
                    completed = (charsUsed == charCount && (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0));
                    return;
                }

                flush = false;
                charsUsed /= 2;
            }

            throw new ArgumentException(Environment.GetResourceString("Argument_ConversionOverflow"));
        }
    }
}