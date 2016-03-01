

namespace System.Text
{
    public class ASCIIEncoding : Encoding
    {
        public ASCIIEncoding(): base (Encoding.CodePageASCII)
        {
        }

        internal override void SetDefaultFallbacks()
        {
            this.encoderFallback = EncoderFallback.ReplacementFallback;
            this.decoderFallback = DecoderFallback.ReplacementFallback;
        }

        public override unsafe int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (chars.Length - index < count)
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
                        if (chars.Length == 0)
                return 0;
            fixed (char *pChars = chars)
                return GetByteCount(pChars + index, count, null);
        }

        public override unsafe int GetByteCount(String chars)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
                        fixed (char *pChars = chars)
                return GetByteCount(pChars, chars.Length, null);
        }

        public override unsafe int GetByteCount(char *chars, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        return GetByteCount(chars, count, null);
        }

        public override unsafe int GetBytes(String chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException((chars == null ? "chars" : "bytes"), Environment.GetResourceString("ArgumentNull_Array"));
            if (charIndex < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((charIndex < 0 ? "charIndex" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (chars.Length - charIndex < charCount)
                throw new ArgumentOutOfRangeException("chars", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            if (byteIndex < 0 || byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                        int byteCount = bytes.Length - byteIndex;
            if (bytes.Length == 0)
                bytes = new byte[1];
            fixed (char *pChars = chars)
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
                        return GetChars(bytes, byteCount, chars, charCount, null);
        }

        public override unsafe String GetString(byte[] bytes, int byteIndex, int byteCount)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (byteIndex < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((byteIndex < 0 ? "byteIndex" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (bytes.Length - byteIndex < byteCount)
                throw new ArgumentOutOfRangeException("bytes", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
                        if (bytes.Length == 0)
                return String.Empty;
            fixed (byte *pBytes = bytes)
                return String.CreateStringFromEncoding(pBytes + byteIndex, byteCount, this);
        }

        internal override unsafe int GetByteCount(char *chars, int charCount, EncoderNLS encoder)
        {
                                                char charLeftOver = (char)0;
            EncoderReplacementFallback fallback = null;
            char *charEnd = chars + charCount;
            EncoderFallbackBuffer fallbackBuffer = null;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                                fallback = encoder.Fallback as EncoderReplacementFallback;
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder.m_throwOnOverflow)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", this.EncodingName, encoder.Fallback.GetType()));
                    fallbackBuffer.InternalInitialize(chars, charEnd, encoder, false);
                }

                            }
            else
            {
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }

            if (fallback != null && fallback.MaxCharCount == 1)
            {
                if (charLeftOver > 0)
                    charCount++;
                return (charCount);
            }

            int byteCount = 0;
            if (charLeftOver > 0)
            {
                                                fallbackBuffer = encoder.FallbackBuffer;
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, false);
                fallbackBuffer.InternalFallback(charLeftOver, ref chars);
            }

            char ch;
            while ((ch = (fallbackBuffer == null) ? '\0' : fallbackBuffer.InternalGetNextChar()) != 0 || chars < charEnd)
            {
                if (ch == 0)
                {
                    ch = *chars;
                    chars++;
                }

                if (ch > 0x7f)
                {
                    if (fallbackBuffer == null)
                    {
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(charEnd - charCount, charEnd, encoder, false);
                    }

                    fallbackBuffer.InternalFallback(ch, ref chars);
                    continue;
                }

                byteCount++;
            }

                        return byteCount;
        }

        internal override unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, EncoderNLS encoder)
        {
                                                                        char charLeftOver = (char)0;
            EncoderReplacementFallback fallback = null;
            EncoderFallbackBuffer fallbackBuffer = null;
            char *charEnd = chars + charCount;
            byte *byteStart = bytes;
            char *charStart = chars;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                fallback = encoder.Fallback as EncoderReplacementFallback;
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder.m_throwOnOverflow)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", this.EncodingName, encoder.Fallback.GetType()));
                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                }

                                            }
            else
            {
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }

            if (fallback != null && fallback.MaxCharCount == 1)
            {
                char cReplacement = fallback.DefaultString[0];
                if (cReplacement <= (char)0x7f)
                {
                    if (charLeftOver > 0)
                    {
                        if (byteCount == 0)
                            ThrowBytesOverflow(encoder, true);
                        *(bytes++) = (byte)cReplacement;
                        byteCount--;
                    }

                    if (byteCount < charCount)
                    {
                        ThrowBytesOverflow(encoder, byteCount < 1);
                        charEnd = chars + byteCount;
                    }

                    while (chars < charEnd)
                    {
                        char ch2 = *(chars++);
                        if (ch2 >= 0x0080)
                            *(bytes++) = (byte)cReplacement;
                        else
                            *(bytes++) = unchecked ((byte)(ch2));
                    }

                    if (encoder != null)
                    {
                        encoder.charLeftOver = (char)0;
                        encoder.m_charsUsed = (int)(chars - charStart);
                    }

                    return (int)(bytes - byteStart);
                }
            }

            byte *byteEnd = bytes + byteCount;
            if (charLeftOver > 0)
            {
                                fallbackBuffer = encoder.FallbackBuffer;
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, true);
                fallbackBuffer.InternalFallback(charLeftOver, ref chars);
            }

            char ch;
            while ((ch = (fallbackBuffer == null) ? '\0' : fallbackBuffer.InternalGetNextChar()) != 0 || chars < charEnd)
            {
                if (ch == 0)
                {
                    ch = *chars;
                    chars++;
                }

                if (ch > 0x7f)
                {
                    if (fallbackBuffer == null)
                    {
                        if (encoder == null)
                            fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = encoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(charEnd - charCount, charEnd, encoder, true);
                    }

                    fallbackBuffer.InternalFallback(ch, ref chars);
                    continue;
                }

                if (bytes >= byteEnd)
                {
                    if (fallbackBuffer == null || fallbackBuffer.bFallingBack == false)
                    {
                                                chars--;
                    }
                    else
                        fallbackBuffer.MovePrevious();
                    ThrowBytesOverflow(encoder, bytes == byteStart);
                    break;
                }

                *bytes = unchecked ((byte)ch);
                bytes++;
            }

            if (encoder != null)
            {
                if (fallbackBuffer != null && !fallbackBuffer.bUsedEncoder)
                    encoder.charLeftOver = (char)0;
                encoder.m_charsUsed = (int)(chars - charStart);
            }

                        return (int)(bytes - byteStart);
        }

        internal override unsafe int GetCharCount(byte *bytes, int count, DecoderNLS decoder)
        {
                                    DecoderReplacementFallback fallback = null;
            if (decoder == null)
                fallback = this.DecoderFallback as DecoderReplacementFallback;
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                            }

            if (fallback != null && fallback.MaxCharCount == 1)
            {
                return count;
            }

            DecoderFallbackBuffer fallbackBuffer = null;
            int charCount = count;
            byte[] byteBuffer = new byte[1];
            byte *byteEnd = bytes + count;
            while (bytes < byteEnd)
            {
                byte b = *bytes;
                bytes++;
                if (b >= 0x80)
                {
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteEnd - count, null);
                    }

                    byteBuffer[0] = b;
                    charCount--;
                    charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                }
            }

                        return charCount;
        }

        internal override unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, DecoderNLS decoder)
        {
                                                            byte *byteEnd = bytes + byteCount;
            byte *byteStart = bytes;
            char *charStart = chars;
            DecoderReplacementFallback fallback = null;
            if (decoder == null)
                fallback = this.DecoderFallback as DecoderReplacementFallback;
            else
            {
                fallback = decoder.Fallback as DecoderReplacementFallback;
                            }

            if (fallback != null && fallback.MaxCharCount == 1)
            {
                char replacementChar = fallback.DefaultString[0];
                if (charCount < byteCount)
                {
                    ThrowCharsOverflow(decoder, charCount < 1);
                    byteEnd = bytes + charCount;
                }

                while (bytes < byteEnd)
                {
                    byte b = *(bytes++);
                    if (b >= 0x80)
                        *(chars++) = replacementChar;
                    else
                        *(chars++) = unchecked ((char)b);
                }

                if (decoder != null)
                    decoder.m_bytesUsed = (int)(bytes - byteStart);
                return (int)(chars - charStart);
            }

            DecoderFallbackBuffer fallbackBuffer = null;
            byte[] byteBuffer = new byte[1];
            char *charEnd = chars + charCount;
            while (bytes < byteEnd)
            {
                byte b = *(bytes);
                bytes++;
                if (b >= 0x80)
                {
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.DecoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteEnd - byteCount, charEnd);
                    }

                    byteBuffer[0] = b;
                    if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                    {
                                                bytes--;
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);
                        break;
                    }
                }
                else
                {
                    if (chars >= charEnd)
                    {
                                                bytes--;
                        ThrowCharsOverflow(decoder, chars == charStart);
                        break;
                    }

                    *(chars) = unchecked ((char)b);
                    chars++;
                }
            }

            if (decoder != null)
                decoder.m_bytesUsed = (int)(bytes - byteStart);
                        return (int)(chars - charStart);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        long byteCount = (long)charCount + 1;
            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;
            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));
            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        long charCount = (long)byteCount;
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;
            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));
            return (int)charCount;
        }

        public override bool IsSingleByte
        {
            get
            {
                return true;
            }
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