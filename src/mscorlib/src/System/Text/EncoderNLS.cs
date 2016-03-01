namespace System.Text
{
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;
    using System;
    using System.Diagnostics.Contracts;

    internal class EncoderNLS : Encoder, ISerializable
    {
        internal char charLeftOver;
        protected Encoding m_encoding;
        protected bool m_mustFlush;
        internal bool m_throwOnOverflow;
        internal int m_charsUsed;
        internal EncoderNLS(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException(String.Format(System.Globalization.CultureInfo.CurrentCulture, Environment.GetResourceString("NotSupported_TypeCannotDeserialized"), this.GetType()));
        }

        internal EncoderNLS(Encoding encoding)
        {
            this.m_encoding = encoding;
            this.m_fallback = this.m_encoding.EncoderFallback;
            this.Reset();
        }

        internal EncoderNLS()
        {
            this.m_encoding = null;
            this.Reset();
        }

        public override void Reset()
        {
            this.charLeftOver = (char)0;
            if (m_fallbackBuffer != null)
                m_fallbackBuffer.Reset();
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count, bool flush)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();
            if (chars.Length == 0)
                chars = new char[1];
            int result = -1;
            fixed (char *pChars = chars)
            {
                result = GetByteCount(pChars + index, count, flush);
            }

            return result;
        }

        public unsafe override int GetByteCount(char *chars, int count, bool flush)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = true;
            return m_encoding.GetByteCount(chars, count, this);
        }

        public override unsafe int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
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
                chars = new char[1];
            int byteCount = bytes.Length - byteIndex;
            if (bytes.Length == 0)
                bytes = new byte[1];
            fixed (char *pChars = chars)
                fixed (byte *pBytes = bytes)
                    return GetBytes(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, flush);
        }

        public unsafe override int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, bool flush)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"), Environment.GetResourceString("ArgumentNull_Array"));
            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? "byteCount" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = true;
            return m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
        }

        public override unsafe void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
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
            Contract.EndContractBlock();
            if (chars.Length == 0)
                chars = new char[1];
            if (bytes.Length == 0)
                bytes = new byte[1];
            fixed (char *pChars = chars)
            {
                fixed (byte *pBytes = bytes)
                {
                    Convert(pChars + charIndex, charCount, pBytes + byteIndex, byteCount, flush, out charsUsed, out bytesUsed, out completed);
                }
            }
        }

        public override unsafe void Convert(char *chars, int charCount, byte *bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? "charCount" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            this.m_mustFlush = flush;
            this.m_throwOnOverflow = false;
            this.m_charsUsed = 0;
            bytesUsed = this.m_encoding.GetBytes(chars, charCount, bytes, byteCount, this);
            charsUsed = this.m_charsUsed;
            completed = (charsUsed == charCount) && (!flush || !this.HasState) && (m_fallbackBuffer == null || m_fallbackBuffer.Remaining == 0);
        }

        public Encoding Encoding
        {
            get
            {
                return m_encoding;
            }
        }

        public bool MustFlush
        {
            get
            {
                return m_mustFlush;
            }
        }

        internal virtual bool HasState
        {
            get
            {
                return (this.charLeftOver != (char)0);
            }
        }

        internal void ClearMustFlush()
        {
            m_mustFlush = false;
        }
    }
}