using System.Diagnostics.Contracts;

namespace System.Text
{
    internal abstract class EncodingNLS : Encoding
    {
        protected EncodingNLS(int codePage): base (codePage)
        {
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

        public override Decoder GetDecoder()
        {
            return new DecoderNLS(this);
        }

        public override Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }
    }
}