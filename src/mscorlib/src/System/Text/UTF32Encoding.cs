using System.Diagnostics.Contracts;

namespace System.Text
{
    public sealed class UTF32Encoding : Encoding
    {
        private bool emitUTF32ByteOrderMark = false;
        private bool isThrowException = false;
        private bool bigEndian = false;
        public UTF32Encoding(): this (false, true, false)
        {
        }

        public UTF32Encoding(bool bigEndian, bool byteOrderMark): this (bigEndian, byteOrderMark, false)
        {
        }

        public UTF32Encoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidCharacters): base (bigEndian ? 12001 : 12000)
        {
            this.bigEndian = bigEndian;
            this.emitUTF32ByteOrderMark = byteOrderMark;
            this.isThrowException = throwOnInvalidCharacters;
            if (this.isThrowException)
                SetDefaultFallbacks();
        }

        internal override void SetDefaultFallbacks()
        {
            if (this.isThrowException)
            {
                this.encoderFallback = EncoderFallback.ExceptionFallback;
                this.decoderFallback = DecoderFallback.ExceptionFallback;
            }
            else
            {
                this.encoderFallback = new EncoderReplacementFallback("\xFFFD");
                this.decoderFallback = new DecoderReplacementFallback("\xFFFD");
            }
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

        internal override unsafe int GetByteCount(char *chars, int count, EncoderNLS encoder)
        {
            Contract.Assert(chars != null, "[UTF32Encoding.GetByteCount]chars!=null");
            Contract.Assert(count >= 0, "[UTF32Encoding.GetByteCount]count >=0");
            char *end = chars + count;
            char *charStart = chars;
            int byteCount = 0;
            char highSurrogate = '\0';
            EncoderFallbackBuffer fallbackBuffer = null;
            if (encoder != null)
            {
                highSurrogate = encoder.charLeftOver;
                fallbackBuffer = encoder.FallbackBuffer;
                if (fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", this.EncodingName, encoder.Fallback.GetType()));
            }
            else
            {
                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
            }

            fallbackBuffer.InternalInitialize(charStart, end, encoder, false);
            char ch;
            TryAgain:
                while (((ch = fallbackBuffer.InternalGetNextChar()) != 0) || chars < end)
                {
                    if (ch == 0)
                    {
                        ch = *chars;
                        chars++;
                    }

                    if (highSurrogate != '\0')
                    {
                        if (Char.IsLowSurrogate(ch))
                        {
                            highSurrogate = '\0';
                            byteCount += 4;
                            continue;
                        }

                        Contract.Assert(chars > charStart, "[UTF32Encoding.GetByteCount]Expected chars to have advanced if no low surrogate");
                        chars--;
                        fallbackBuffer.InternalFallback(highSurrogate, ref chars);
                        highSurrogate = '\0';
                        continue;
                    }

                    if (Char.IsHighSurrogate(ch))
                    {
                        highSurrogate = ch;
                        continue;
                    }

                    if (Char.IsLowSurrogate(ch))
                    {
                        fallbackBuffer.InternalFallback(ch, ref chars);
                        continue;
                    }

                    byteCount += 4;
                }

            if ((encoder == null || encoder.MustFlush) && highSurrogate > 0)
            {
                fallbackBuffer.InternalFallback(highSurrogate, ref chars);
                highSurrogate = (char)0;
                goto TryAgain;
            }

            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));
            Contract.Assert(fallbackBuffer.Remaining == 0, "[UTF32Encoding.GetByteCount]Expected empty fallback buffer at end");
            return byteCount;
        }

        internal override unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, EncoderNLS encoder)
        {
            Contract.Assert(chars != null, "[UTF32Encoding.GetBytes]chars!=null");
            Contract.Assert(bytes != null, "[UTF32Encoding.GetBytes]bytes!=null");
            Contract.Assert(byteCount >= 0, "[UTF32Encoding.GetBytes]byteCount >=0");
            Contract.Assert(charCount >= 0, "[UTF32Encoding.GetBytes]charCount >=0");
            char *charStart = chars;
            char *charEnd = chars + charCount;
            byte *byteStart = bytes;
            byte *byteEnd = bytes + byteCount;
            char highSurrogate = '\0';
            EncoderFallbackBuffer fallbackBuffer = null;
            if (encoder != null)
            {
                highSurrogate = encoder.charLeftOver;
                fallbackBuffer = encoder.FallbackBuffer;
                if (encoder.m_throwOnOverflow && fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", this.EncodingName, encoder.Fallback.GetType()));
            }
            else
            {
                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
            }

            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
            char ch;
            TryAgain:
                while (((ch = fallbackBuffer.InternalGetNextChar()) != 0) || chars < charEnd)
                {
                    if (ch == 0)
                    {
                        ch = *chars;
                        chars++;
                    }

                    if (highSurrogate != '\0')
                    {
                        if (Char.IsLowSurrogate(ch))
                        {
                            uint iTemp = GetSurrogate(highSurrogate, ch);
                            highSurrogate = '\0';
                            if (bytes + 3 >= byteEnd)
                            {
                                if (fallbackBuffer.bFallingBack)
                                {
                                    fallbackBuffer.MovePrevious();
                                    fallbackBuffer.MovePrevious();
                                }
                                else
                                {
                                    Contract.Assert(chars > charStart + 1 || bytes == byteStart, "[UnicodeEncoding.GetBytes]Expected chars to have when no room to add surrogate pair");
                                    chars -= 2;
                                }

                                ThrowBytesOverflow(encoder, bytes == byteStart);
                                highSurrogate = (char)0;
                                break;
                            }

                            if (bigEndian)
                            {
                                *(bytes++) = (byte)(0x00);
                                *(bytes++) = (byte)(iTemp >> 16);
                                *(bytes++) = (byte)(iTemp >> 8);
                                *(bytes++) = (byte)(iTemp);
                            }
                            else
                            {
                                *(bytes++) = (byte)(iTemp);
                                *(bytes++) = (byte)(iTemp >> 8);
                                *(bytes++) = (byte)(iTemp >> 16);
                                *(bytes++) = (byte)(0x00);
                            }

                            continue;
                        }

                        Contract.Assert(chars > charStart, "[UTF32Encoding.GetBytes]Expected chars to have advanced if no low surrogate");
                        chars--;
                        fallbackBuffer.InternalFallback(highSurrogate, ref chars);
                        highSurrogate = '\0';
                        continue;
                    }

                    if (Char.IsHighSurrogate(ch))
                    {
                        highSurrogate = ch;
                        continue;
                    }

                    if (Char.IsLowSurrogate(ch))
                    {
                        fallbackBuffer.InternalFallback(ch, ref chars);
                        continue;
                    }

                    if (bytes + 3 >= byteEnd)
                    {
                        if (fallbackBuffer.bFallingBack)
                            fallbackBuffer.MovePrevious();
                        else
                        {
                            Contract.Assert(chars > charStart, "[UTF32Encoding.GetBytes]Expected chars to have advanced if normal character");
                            chars--;
                        }

                        ThrowBytesOverflow(encoder, bytes == byteStart);
                        break;
                    }

                    if (bigEndian)
                    {
                        *(bytes++) = (byte)(0x00);
                        *(bytes++) = (byte)(0x00);
                        *(bytes++) = (byte)((uint)ch >> 8);
                        *(bytes++) = (byte)(ch);
                    }
                    else
                    {
                        *(bytes++) = (byte)(ch);
                        *(bytes++) = (byte)((uint)ch >> 8);
                        *(bytes++) = (byte)(0x00);
                        *(bytes++) = (byte)(0x00);
                    }
                }

            if ((encoder == null || encoder.MustFlush) && highSurrogate > 0)
            {
                fallbackBuffer.InternalFallback(highSurrogate, ref chars);
                highSurrogate = (char)0;
                goto TryAgain;
            }

            Contract.Assert(highSurrogate == 0 || (encoder != null && !encoder.MustFlush), "[UTF32Encoding.GetBytes]Expected encoder to be flushed.");
            if (encoder != null)
            {
                encoder.charLeftOver = highSurrogate;
                encoder.m_charsUsed = (int)(chars - charStart);
            }

            return (int)(bytes - byteStart);
        }

        internal override unsafe int GetCharCount(byte *bytes, int count, DecoderNLS baseDecoder)
        {
            Contract.Assert(bytes != null, "[UTF32Encoding.GetCharCount]bytes!=null");
            Contract.Assert(count >= 0, "[UTF32Encoding.GetCharCount]count >=0");
            UTF32Decoder decoder = (UTF32Decoder)baseDecoder;
            int charCount = 0;
            byte *end = bytes + count;
            byte *byteStart = bytes;
            int readCount = 0;
            uint iChar = 0;
            DecoderFallbackBuffer fallbackBuffer = null;
            if (decoder != null)
            {
                readCount = decoder.readByteCount;
                iChar = (uint)decoder.iChar;
                fallbackBuffer = decoder.FallbackBuffer;
                Contract.Assert(fallbackBuffer.Remaining == 0, "[UTF32Encoding.GetCharCount]Expected empty fallback buffer at start");
            }
            else
            {
                fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
            }

            fallbackBuffer.InternalInitialize(byteStart, null);
            while (bytes < end && charCount >= 0)
            {
                if (bigEndian)
                {
                    iChar <<= 8;
                    iChar += *(bytes++);
                }
                else
                {
                    iChar >>= 8;
                    iChar += (uint)(*(bytes++)) << 24;
                }

                readCount++;
                if (readCount < 4)
                    continue;
                readCount = 0;
                if (iChar > 0x10FFFF || (iChar >= 0xD800 && iChar <= 0xDFFF))
                {
                    byte[] fallbackBytes;
                    if (this.bigEndian)
                    {
                        fallbackBytes = new byte[]{unchecked ((byte)(iChar >> 24)), unchecked ((byte)(iChar >> 16)), unchecked ((byte)(iChar >> 8)), unchecked ((byte)(iChar))};
                    }
                    else
                    {
                        fallbackBytes = new byte[]{unchecked ((byte)(iChar)), unchecked ((byte)(iChar >> 8)), unchecked ((byte)(iChar >> 16)), unchecked ((byte)(iChar >> 24))};
                    }

                    charCount += fallbackBuffer.InternalFallback(fallbackBytes, bytes);
                    iChar = 0;
                    continue;
                }

                if (iChar >= 0x10000)
                {
                    charCount++;
                }

                charCount++;
                iChar = 0;
            }

            if (readCount > 0 && (decoder == null || decoder.MustFlush))
            {
                byte[] fallbackBytes = new byte[readCount];
                if (this.bigEndian)
                {
                    while (readCount > 0)
                    {
                        fallbackBytes[--readCount] = unchecked ((byte)iChar);
                        iChar >>= 8;
                    }
                }
                else
                {
                    while (readCount > 0)
                    {
                        fallbackBytes[--readCount] = unchecked ((byte)(iChar >> 24));
                        iChar <<= 8;
                    }
                }

                charCount += fallbackBuffer.InternalFallback(fallbackBytes, bytes);
            }

            if (charCount < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));
            Contract.Assert(fallbackBuffer.Remaining == 0, "[UTF32Encoding.GetCharCount]Expected empty fallback buffer at end");
            return charCount;
        }

        internal override unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, DecoderNLS baseDecoder)
        {
            Contract.Assert(chars != null, "[UTF32Encoding.GetChars]chars!=null");
            Contract.Assert(bytes != null, "[UTF32Encoding.GetChars]bytes!=null");
            Contract.Assert(byteCount >= 0, "[UTF32Encoding.GetChars]byteCount >=0");
            Contract.Assert(charCount >= 0, "[UTF32Encoding.GetChars]charCount >=0");
            UTF32Decoder decoder = (UTF32Decoder)baseDecoder;
            char *charStart = chars;
            char *charEnd = chars + charCount;
            byte *byteStart = bytes;
            byte *byteEnd = bytes + byteCount;
            int readCount = 0;
            uint iChar = 0;
            DecoderFallbackBuffer fallbackBuffer = null;
            if (decoder != null)
            {
                readCount = decoder.readByteCount;
                iChar = (uint)decoder.iChar;
                fallbackBuffer = baseDecoder.FallbackBuffer;
                Contract.Assert(fallbackBuffer.Remaining == 0, "[UTF32Encoding.GetChars]Expected empty fallback buffer at start");
            }
            else
            {
                fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
            }

            fallbackBuffer.InternalInitialize(bytes, chars + charCount);
            while (bytes < byteEnd)
            {
                if (bigEndian)
                {
                    iChar <<= 8;
                    iChar += *(bytes++);
                }
                else
                {
                    iChar >>= 8;
                    iChar += (uint)(*(bytes++)) << 24;
                }

                readCount++;
                if (readCount < 4)
                    continue;
                readCount = 0;
                if (iChar > 0x10FFFF || (iChar >= 0xD800 && iChar <= 0xDFFF))
                {
                    byte[] fallbackBytes;
                    if (this.bigEndian)
                    {
                        fallbackBytes = new byte[]{unchecked ((byte)(iChar >> 24)), unchecked ((byte)(iChar >> 16)), unchecked ((byte)(iChar >> 8)), unchecked ((byte)(iChar))};
                    }
                    else
                    {
                        fallbackBytes = new byte[]{unchecked ((byte)(iChar)), unchecked ((byte)(iChar >> 8)), unchecked ((byte)(iChar >> 16)), unchecked ((byte)(iChar >> 24))};
                    }

                    if (!fallbackBuffer.InternalFallback(fallbackBytes, bytes, ref chars))
                    {
                        Contract.Assert(bytes >= byteStart + 4 || chars == charStart, "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (bad surrogate)");
                        bytes -= 4;
                        iChar = 0;
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);
                        break;
                    }

                    iChar = 0;
                    continue;
                }

                if (iChar >= 0x10000)
                {
                    if (chars >= charEnd - 1)
                    {
                        Contract.Assert(bytes >= byteStart + 4 || chars == charStart, "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (surrogate)");
                        bytes -= 4;
                        iChar = 0;
                        ThrowCharsOverflow(decoder, chars == charStart);
                        break;
                    }

                    *(chars++) = GetHighSurrogate(iChar);
                    iChar = GetLowSurrogate(iChar);
                }
                else if (chars >= charEnd)
                {
                    Contract.Assert(bytes >= byteStart + 4 || chars == charStart, "[UTF32Encoding.GetChars]Expected to have consumed bytes or throw (normal char)");
                    bytes -= 4;
                    iChar = 0;
                    ThrowCharsOverflow(decoder, chars == charStart);
                    break;
                }

                *(chars++) = (char)iChar;
                iChar = 0;
            }

            if (readCount > 0 && (decoder == null || decoder.MustFlush))
            {
                byte[] fallbackBytes = new byte[readCount];
                int tempCount = readCount;
                if (this.bigEndian)
                {
                    while (tempCount > 0)
                    {
                        fallbackBytes[--tempCount] = unchecked ((byte)iChar);
                        iChar >>= 8;
                    }
                }
                else
                {
                    while (tempCount > 0)
                    {
                        fallbackBytes[--tempCount] = unchecked ((byte)(iChar >> 24));
                        iChar <<= 8;
                    }
                }

                if (!fallbackBuffer.InternalFallback(fallbackBytes, bytes, ref chars))
                {
                    fallbackBuffer.InternalReset();
                    ThrowCharsOverflow(decoder, chars == charStart);
                }
                else
                {
                    readCount = 0;
                    iChar = 0;
                }
            }

            if (decoder != null)
            {
                decoder.iChar = (int)iChar;
                decoder.readByteCount = readCount;
                decoder.m_bytesUsed = (int)(bytes - byteStart);
            }

            Contract.Assert(fallbackBuffer.Remaining == 0, "[UTF32Encoding.GetChars]Expected empty fallback buffer at end");
            return (int)(chars - charStart);
        }

        private uint GetSurrogate(char cHigh, char cLow)
        {
            return (((uint)cHigh - 0xD800) * 0x400) + ((uint)cLow - 0xDC00) + 0x10000;
        }

        private char GetHighSurrogate(uint iChar)
        {
            return (char)((iChar - 0x10000) / 0x400 + 0xD800);
        }

        private char GetLowSurrogate(uint iChar)
        {
            return (char)((iChar - 0x10000) % 0x400 + 0xDC00);
        }

        public override Decoder GetDecoder()
        {
            return new UTF32Decoder(this);
        }

        public override Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            long byteCount = (long)charCount + 1;
            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;
            byteCount *= 4;
            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));
            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            int charCount = (byteCount / 2) + 2;
            if (DecoderFallback.MaxCharCount > 2)
            {
                charCount *= DecoderFallback.MaxCharCount;
                charCount /= 2;
            }

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));
            return (int)charCount;
        }

        public override byte[] GetPreamble()
        {
            if (emitUTF32ByteOrderMark)
            {
                if (bigEndian)
                {
                    return new byte[4]{0x00, 0x00, 0xFE, 0xFF};
                }
                else
                {
                    return new byte[4]{0xFF, 0xFE, 0x00, 0x00};
                }
            }
            else
                return EmptyArray<Byte>.Value;
        }

        public override bool Equals(Object value)
        {
            UTF32Encoding that = value as UTF32Encoding;
            if (that != null)
            {
                return (emitUTF32ByteOrderMark == that.emitUTF32ByteOrderMark) && (bigEndian == that.bigEndian) && (EncoderFallback.Equals(that.EncoderFallback)) && (DecoderFallback.Equals(that.DecoderFallback));
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() + CodePage + (emitUTF32ByteOrderMark ? 4 : 0) + (bigEndian ? 8 : 0);
        }

        internal class UTF32Decoder : DecoderNLS
        {
            internal int iChar = 0;
            internal int readByteCount = 0;
            public UTF32Decoder(UTF32Encoding encoding): base (encoding)
            {
            }

            public override void Reset()
            {
                this.iChar = 0;
                this.readByteCount = 0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            internal override bool HasState
            {
                get
                {
                    return (this.readByteCount != 0);
                }
            }
        }
    }
}