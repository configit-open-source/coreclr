using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Text
{
    public class UnicodeEncoding : Encoding
    {
        internal bool isThrowException = false;
        internal bool bigEndian = false;
        internal bool byteOrderMark = true;
        public const int CharSize = 2;
        public UnicodeEncoding(): this (false, true)
        {
        }

        public UnicodeEncoding(bool bigEndian, bool byteOrderMark): this (bigEndian, byteOrderMark, false)
        {
        }

        public UnicodeEncoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes): base (bigEndian ? 1201 : 1200)
        {
            this.isThrowException = throwOnInvalidBytes;
            this.bigEndian = bigEndian;
            this.byteOrderMark = byteOrderMark;
            if (this.isThrowException)
                SetDefaultFallbacks();
        }

        private void OnDeserializing(StreamingContext ctx)
        {
            isThrowException = false;
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
            Contract.Assert(chars != null, "[UnicodeEncoding.GetByteCount]chars!=null");
            Contract.Assert(count >= 0, "[UnicodeEncoding.GetByteCount]count >=0");
            int byteCount = count << 1;
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));
            char *charStart = chars;
            char *charEnd = chars + count;
            char charLeftOver = (char)0;
            bool wasHereBefore = false;
            ulong *longEnd = (ulong *)(charEnd - 3);
            EncoderFallbackBuffer fallbackBuffer = null;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                if (charLeftOver > 0)
                    byteCount += 2;
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", this.EncodingName, encoder.Fallback.GetType()));
                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                }
            }

            char ch;
            TryAgain:
                while (((ch = (fallbackBuffer == null) ? (char)0 : fallbackBuffer.InternalGetNextChar()) != 0) || chars < charEnd)
                {
                    if (ch == 0)
                    {
                        if (!bigEndian && charLeftOver == 0 && (unchecked ((long)chars) & 7) == 0)
                        {
                            ulong *longChars = (ulong *)chars;
                            while (longChars < longEnd)
                            {
                                if ((0x8000800080008000 & *longChars) != 0)
                                {
                                    ulong uTemp = (0xf800f800f800f800 & *longChars) ^ 0xd800d800d800d800;
                                    if ((uTemp & 0xFFFF000000000000) == 0 || (uTemp & 0x0000FFFF00000000) == 0 || (uTemp & 0x00000000FFFF0000) == 0 || (uTemp & 0x000000000000FFFF) == 0)
                                    {
                                        if (((0xfc00fc00fc00fc00 & *longChars) ^ 0xdc00d800dc00d800) != 0)
                                        {
                                            break;
                                        }
                                    }
                                }

                                longChars++;
                            }

                            chars = (char *)longChars;
                            if (chars >= charEnd)
                                break;
                        }

                        ch = *chars;
                        chars++;
                    }
                    else
                    {
                        byteCount += 2;
                    }

                    if (ch >= 0xd800 && ch <= 0xdfff)
                    {
                        if (ch <= 0xdbff)
                        {
                            if (charLeftOver > 0)
                            {
                                Contract.Assert(chars > charStart, "[UnicodeEncoding.GetByteCount]Expected chars to have advanced in unexpected high surrogate");
                                chars--;
                                byteCount -= 2;
                                if (fallbackBuffer == null)
                                {
                                    if (encoder == null)
                                        fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                                    else
                                        fallbackBuffer = encoder.FallbackBuffer;
                                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                                }

                                fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                                charLeftOver = (char)0;
                                continue;
                            }

                            charLeftOver = ch;
                            continue;
                        }

                        if (charLeftOver == 0)
                        {
                            byteCount -= 2;
                            if (fallbackBuffer == null)
                            {
                                if (encoder == null)
                                    fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                                else
                                    fallbackBuffer = encoder.FallbackBuffer;
                                fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                            }

                            fallbackBuffer.InternalFallback(ch, ref chars);
                            continue;
                        }

                        charLeftOver = (char)0;
                        continue;
                    }
                    else if (charLeftOver > 0)
                    {
                        Contract.Assert(chars > charStart, "[UnicodeEncoding.GetByteCount]Expected chars to have advanced when expected low surrogate");
                        chars--;
                        if (fallbackBuffer == null)
                        {
                            if (encoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = encoder.FallbackBuffer;
                            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                        }

                        fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                        byteCount -= 2;
                        charLeftOver = (char)0;
                        continue;
                    }
                }

            if (charLeftOver > 0)
            {
                byteCount -= 2;
                if (encoder == null || encoder.MustFlush)
                {
                    if (wasHereBefore)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_RecursiveFallback", charLeftOver), "chars");
                    }
                    else
                    {
                        if (fallbackBuffer == null)
                        {
                            if (encoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = encoder.FallbackBuffer;
                            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                        }

                        fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                        charLeftOver = (char)0;
                        wasHereBefore = true;
                        goto TryAgain;
                    }
                }
            }

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0, "[UnicodeEncoding.GetByteCount]Expected empty fallback buffer at end");
            return byteCount;
        }

        internal override unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, EncoderNLS encoder)
        {
            Contract.Assert(chars != null, "[UnicodeEncoding.GetBytes]chars!=null");
            Contract.Assert(byteCount >= 0, "[UnicodeEncoding.GetBytes]byteCount >=0");
            Contract.Assert(charCount >= 0, "[UnicodeEncoding.GetBytes]charCount >=0");
            Contract.Assert(bytes != null, "[UnicodeEncoding.GetBytes]bytes!=null");
            char charLeftOver = (char)0;
            char ch;
            bool wasHereBefore = false;
            byte *byteEnd = bytes + byteCount;
            char *charEnd = chars + charCount;
            byte *byteStart = bytes;
            char *charStart = chars;
            EncoderFallbackBuffer fallbackBuffer = null;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder.m_throwOnOverflow)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", this.EncodingName, encoder.Fallback.GetType()));
                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, false);
                }
            }

            TryAgain:
                while (((ch = (fallbackBuffer == null) ? (char)0 : fallbackBuffer.InternalGetNextChar()) != 0) || chars < charEnd)
                {
                    if (ch == 0)
                    {
                        if (!bigEndian && (unchecked ((long)chars) & 7) == 0 && (unchecked ((long)bytes) & 7) == 0 && charLeftOver == 0)
                        {
                            ulong *longEnd = (ulong *)(chars - 3 + (((byteEnd - bytes) >> 1 < charEnd - chars) ? (byteEnd - bytes) >> 1 : charEnd - chars));
                            ulong *longChars = (ulong *)chars;
                            ulong *longBytes = (ulong *)bytes;
                            while (longChars < longEnd)
                            {
                                if ((0x8000800080008000 & *longChars) != 0)
                                {
                                    ulong uTemp = (0xf800f800f800f800 & *longChars) ^ 0xd800d800d800d800;
                                    if ((uTemp & 0xFFFF000000000000) == 0 || (uTemp & 0x0000FFFF00000000) == 0 || (uTemp & 0x00000000FFFF0000) == 0 || (uTemp & 0x000000000000FFFF) == 0)
                                    {
                                        if (((0xfc00fc00fc00fc00 & *longChars) ^ 0xdc00d800dc00d800) != 0)
                                        {
                                            break;
                                        }
                                    }
                                }

                                *longBytes = *longChars;
                                longChars++;
                                longBytes++;
                            }

                            chars = (char *)longChars;
                            bytes = (byte *)longBytes;
                            if (chars >= charEnd)
                                break;
                        }
                        else if ((charLeftOver == 0) && !bigEndian && (unchecked ((long)chars) & 7) != (unchecked ((long)bytes) & 7) && (unchecked ((int)(bytes)) & 1) == 0)
                        {
                            long iCount = ((byteEnd - bytes) >> 1 < charEnd - chars) ? (byteEnd - bytes) >> 1 : charEnd - chars;
                            char *charOut = ((char *)bytes);
                            char *tempEnd = chars + iCount - 1;
                            while (chars < tempEnd)
                            {
                                if (*chars >= (char)0xd800 && *chars <= (char)0xdfff)
                                {
                                    if (*chars >= 0xdc00)
                                        break;
                                    if (*(chars + 1) < 0xdc00 || *(chars + 1) > 0xdfff)
                                        break;
                                }
                                else if (*(chars + 1) >= (char)0xd800 && *(chars + 1) <= 0xdfff)
                                {
                                    *charOut = *chars;
                                    charOut++;
                                    chars++;
                                    continue;
                                }

                                *charOut = *chars;
                                *(charOut + 1) = *(chars + 1);
                                charOut += 2;
                                chars += 2;
                            }

                            bytes = (byte *)charOut;
                            if (chars >= charEnd)
                                break;
                        }

                        ch = *chars;
                        chars++;
                    }

                    if (ch >= 0xd800 && ch <= 0xdfff)
                    {
                        if (ch <= 0xdbff)
                        {
                            if (charLeftOver > 0)
                            {
                                Contract.Assert(chars > charStart, "[UnicodeEncoding.GetBytes]Expected chars to have advanced in unexpected high surrogate");
                                chars--;
                                if (fallbackBuffer == null)
                                {
                                    if (encoder == null)
                                        fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                                    else
                                        fallbackBuffer = encoder.FallbackBuffer;
                                    fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                                }

                                fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                                charLeftOver = (char)0;
                                continue;
                            }

                            charLeftOver = ch;
                            continue;
                        }

                        if (charLeftOver == 0)
                        {
                            if (fallbackBuffer == null)
                            {
                                if (encoder == null)
                                    fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                                else
                                    fallbackBuffer = encoder.FallbackBuffer;
                                fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                            }

                            fallbackBuffer.InternalFallback(ch, ref chars);
                            continue;
                        }

                        if (bytes + 3 >= byteEnd)
                        {
                            if (fallbackBuffer != null && fallbackBuffer.bFallingBack)
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
                            charLeftOver = (char)0;
                            break;
                        }

                        if (bigEndian)
                        {
                            *(bytes++) = (byte)(charLeftOver >> 8);
                            *(bytes++) = (byte)charLeftOver;
                        }
                        else
                        {
                            *(bytes++) = (byte)charLeftOver;
                            *(bytes++) = (byte)(charLeftOver >> 8);
                        }

                        charLeftOver = (char)0;
                    }
                    else if (charLeftOver > 0)
                    {
                        Contract.Assert(chars > charStart, "[UnicodeEncoding.GetBytes]Expected chars to have advanced after expecting low surrogate");
                        chars--;
                        if (fallbackBuffer == null)
                        {
                            if (encoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = encoder.FallbackBuffer;
                            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                        }

                        fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                        charLeftOver = (char)0;
                        continue;
                    }

                    if (bytes + 1 >= byteEnd)
                    {
                        if (fallbackBuffer != null && fallbackBuffer.bFallingBack)
                            fallbackBuffer.MovePrevious();
                        else
                        {
                            Contract.Assert(chars > charStart, "[UnicodeEncoding.GetBytes]Expected chars to have advanced for failed fallback");
                            chars--;
                        }

                        ThrowBytesOverflow(encoder, bytes == byteStart);
                        break;
                    }

                    if (bigEndian)
                    {
                        *(bytes++) = (byte)(ch >> 8);
                        *(bytes++) = (byte)ch;
                    }
                    else
                    {
                        *(bytes++) = (byte)ch;
                        *(bytes++) = (byte)(ch >> 8);
                    }
                }

            if (charLeftOver > 0)
            {
                if (encoder == null || encoder.MustFlush)
                {
                    if (wasHereBefore)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_RecursiveFallback", charLeftOver), "chars");
                    }
                    else
                    {
                        if (fallbackBuffer == null)
                        {
                            if (encoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = encoder.FallbackBuffer;
                            fallbackBuffer.InternalInitialize(charStart, charEnd, encoder, true);
                        }

                        fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                        charLeftOver = (char)0;
                        wasHereBefore = true;
                        goto TryAgain;
                    }
                }
            }

            if (encoder != null)
            {
                encoder.charLeftOver = charLeftOver;
                encoder.m_charsUsed = (int)(chars - charStart);
            }

            Contract.Assert((encoder != null && !encoder.MustFlush) || charLeftOver == (char)0, "[UnicodeEncoding.GetBytes] Expected no left over characters if flushing");
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0 || encoder == null || !encoder.m_throwOnOverflow, "[UnicodeEncoding.GetBytes]Expected empty fallback buffer if not converting");
            return (int)(bytes - byteStart);
        }

        internal override unsafe int GetCharCount(byte *bytes, int count, DecoderNLS baseDecoder)
        {
            Contract.Assert(bytes != null, "[UnicodeEncoding.GetCharCount]bytes!=null");
            Contract.Assert(count >= 0, "[UnicodeEncoding.GetCharCount]count >=0");
            UnicodeEncoding.Decoder decoder = (UnicodeEncoding.Decoder)baseDecoder;
            byte *byteEnd = bytes + count;
            byte *byteStart = bytes;
            int lastByte = -1;
            char lastChar = (char)0;
            int charCount = count >> 1;
            ulong *longEnd = (ulong *)(byteEnd - 7);
            DecoderFallbackBuffer fallbackBuffer = null;
            if (decoder != null)
            {
                lastByte = decoder.lastByte;
                lastChar = decoder.lastChar;
                if (lastChar > 0)
                    charCount++;
                if (lastByte >= 0 && (count & 1) == 1)
                {
                    charCount++;
                }

                Contract.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0, "[UnicodeEncoding.GetCharCount]Expected empty fallback buffer at start");
            }

            while (bytes < byteEnd)
            {
                if (!bigEndian && (unchecked ((long)bytes) & 7) == 0 && lastByte == -1 && lastChar == 0)
                {
                    ulong *longBytes = (ulong *)bytes;
                    while (longBytes < longEnd)
                    {
                        if ((0x8000800080008000 & *longBytes) != 0)
                        {
                            ulong uTemp = (0xf800f800f800f800 & *longBytes) ^ 0xd800d800d800d800;
                            if ((uTemp & 0xFFFF000000000000) == 0 || (uTemp & 0x0000FFFF00000000) == 0 || (uTemp & 0x00000000FFFF0000) == 0 || (uTemp & 0x000000000000FFFF) == 0)
                            {
                                if (((0xfc00fc00fc00fc00 & *longBytes) ^ 0xdc00d800dc00d800) != 0)
                                {
                                    break;
                                }
                            }
                        }

                        longBytes++;
                    }

                    bytes = (byte *)longBytes;
                    if (bytes >= byteEnd)
                        break;
                }

                if (lastByte < 0)
                {
                    lastByte = *bytes++;
                    if (bytes >= byteEnd)
                        break;
                }

                char ch;
                if (bigEndian)
                {
                    ch = (char)(lastByte << 8 | *(bytes++));
                }
                else
                {
                    ch = (char)(*(bytes++) << 8 | lastByte);
                }

                lastByte = -1;
                if (ch >= 0xd800 && ch <= 0xdfff)
                {
                    if (ch <= 0xdbff)
                    {
                        if (lastChar > 0)
                        {
                            charCount--;
                            byte[] byteBuffer = null;
                            if (bigEndian)
                            {
                                byteBuffer = new byte[]{unchecked ((byte)(lastChar >> 8)), unchecked ((byte)lastChar)};
                            }
                            else
                            {
                                byteBuffer = new byte[]{unchecked ((byte)lastChar), unchecked ((byte)(lastChar >> 8))};
                            }

                            if (fallbackBuffer == null)
                            {
                                if (decoder == null)
                                    fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                                else
                                    fallbackBuffer = decoder.FallbackBuffer;
                                fallbackBuffer.InternalInitialize(byteStart, null);
                            }

                            charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                        }

                        lastChar = ch;
                        continue;
                    }

                    if (lastChar == 0)
                    {
                        charCount--;
                        byte[] byteBuffer = null;
                        if (bigEndian)
                        {
                            byteBuffer = new byte[]{unchecked ((byte)(ch >> 8)), unchecked ((byte)ch)};
                        }
                        else
                        {
                            byteBuffer = new byte[]{unchecked ((byte)ch), unchecked ((byte)(ch >> 8))};
                        }

                        if (fallbackBuffer == null)
                        {
                            if (decoder == null)
                                fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = decoder.FallbackBuffer;
                            fallbackBuffer.InternalInitialize(byteStart, null);
                        }

                        charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                        continue;
                    }

                    lastChar = (char)0;
                }
                else if (lastChar > 0)
                {
                    charCount--;
                    byte[] byteBuffer = null;
                    if (bigEndian)
                    {
                        byteBuffer = new byte[]{unchecked ((byte)(lastChar >> 8)), unchecked ((byte)lastChar)};
                    }
                    else
                    {
                        byteBuffer = new byte[]{unchecked ((byte)lastChar), unchecked ((byte)(lastChar >> 8))};
                    }

                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteStart, null);
                    }

                    charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                    lastChar = (char)0;
                }
            }

            if (decoder == null || decoder.MustFlush)
            {
                if (lastChar > 0)
                {
                    charCount--;
                    byte[] byteBuffer = null;
                    if (bigEndian)
                    {
                        byteBuffer = new byte[]{unchecked ((byte)(lastChar >> 8)), unchecked ((byte)lastChar)};
                    }
                    else
                    {
                        byteBuffer = new byte[]{unchecked ((byte)lastChar), unchecked ((byte)(lastChar >> 8))};
                    }

                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteStart, null);
                    }

                    charCount += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                    lastChar = (char)0;
                }

                if (lastByte >= 0)
                {
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteStart, null);
                    }

                    charCount += fallbackBuffer.InternalFallback(new byte[]{unchecked ((byte)lastByte)}, bytes);
                    lastByte = -1;
                }
            }

            if (lastChar > 0)
                charCount--;
            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0, "[UnicodeEncoding.GetCharCount]Expected empty fallback buffer at end");
            return charCount;
        }

        internal override unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, DecoderNLS baseDecoder)
        {
            Contract.Assert(chars != null, "[UnicodeEncoding.GetChars]chars!=null");
            Contract.Assert(byteCount >= 0, "[UnicodeEncoding.GetChars]byteCount >=0");
            Contract.Assert(charCount >= 0, "[UnicodeEncoding.GetChars]charCount >=0");
            Contract.Assert(bytes != null, "[UnicodeEncoding.GetChars]bytes!=null");
            UnicodeEncoding.Decoder decoder = (UnicodeEncoding.Decoder)baseDecoder;
            int lastByte = -1;
            char lastChar = (char)0;
            if (decoder != null)
            {
                lastByte = decoder.lastByte;
                lastChar = decoder.lastChar;
                Contract.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0, "[UnicodeEncoding.GetChars]Expected empty fallback buffer at start");
            }

            DecoderFallbackBuffer fallbackBuffer = null;
            byte *byteEnd = bytes + byteCount;
            char *charEnd = chars + charCount;
            byte *byteStart = bytes;
            char *charStart = chars;
            while (bytes < byteEnd)
            {
                if (!bigEndian && (unchecked ((long)chars) & 7) == 0 && (unchecked ((long)bytes) & 7) == 0 && lastByte == -1 && lastChar == 0)
                {
                    ulong *longEnd = (ulong *)(bytes - 7 + (((byteEnd - bytes) >> 1 < charEnd - chars) ? (byteEnd - bytes) : (charEnd - chars) << 1));
                    ulong *longBytes = (ulong *)bytes;
                    ulong *longChars = (ulong *)chars;
                    while (longBytes < longEnd)
                    {
                        if ((0x8000800080008000 & *longBytes) != 0)
                        {
                            ulong uTemp = (0xf800f800f800f800 & *longBytes) ^ 0xd800d800d800d800;
                            if ((uTemp & 0xFFFF000000000000) == 0 || (uTemp & 0x0000FFFF00000000) == 0 || (uTemp & 0x00000000FFFF0000) == 0 || (uTemp & 0x000000000000FFFF) == 0)
                            {
                                if (((0xfc00fc00fc00fc00 & *longBytes) ^ 0xdc00d800dc00d800) != 0)
                                {
                                    break;
                                }
                            }
                        }

                        *longChars = *longBytes;
                        longBytes++;
                        longChars++;
                    }

                    chars = (char *)longChars;
                    bytes = (byte *)longBytes;
                    if (bytes >= byteEnd)
                        break;
                }

                if (lastByte < 0)
                {
                    lastByte = *bytes++;
                    continue;
                }

                char ch;
                if (bigEndian)
                {
                    ch = (char)(lastByte << 8 | *(bytes++));
                }
                else
                {
                    ch = (char)(*(bytes++) << 8 | lastByte);
                }

                lastByte = -1;
                if (ch >= 0xd800 && ch <= 0xdfff)
                {
                    if (ch <= 0xdbff)
                    {
                        if (lastChar > 0)
                        {
                            byte[] byteBuffer = null;
                            if (bigEndian)
                            {
                                byteBuffer = new byte[]{unchecked ((byte)(lastChar >> 8)), unchecked ((byte)lastChar)};
                            }
                            else
                            {
                                byteBuffer = new byte[]{unchecked ((byte)lastChar), unchecked ((byte)(lastChar >> 8))};
                            }

                            if (fallbackBuffer == null)
                            {
                                if (decoder == null)
                                    fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                                else
                                    fallbackBuffer = decoder.FallbackBuffer;
                                fallbackBuffer.InternalInitialize(byteStart, charEnd);
                            }

                            if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                            {
                                Contract.Assert(bytes >= byteStart + 2 || chars == charStart, "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (bad surrogate)");
                                bytes -= 2;
                                fallbackBuffer.InternalReset();
                                ThrowCharsOverflow(decoder, chars == charStart);
                                break;
                            }
                        }

                        lastChar = ch;
                        continue;
                    }

                    if (lastChar == 0)
                    {
                        byte[] byteBuffer = null;
                        if (bigEndian)
                        {
                            byteBuffer = new byte[]{unchecked ((byte)(ch >> 8)), unchecked ((byte)ch)};
                        }
                        else
                        {
                            byteBuffer = new byte[]{unchecked ((byte)ch), unchecked ((byte)(ch >> 8))};
                        }

                        if (fallbackBuffer == null)
                        {
                            if (decoder == null)
                                fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = decoder.FallbackBuffer;
                            fallbackBuffer.InternalInitialize(byteStart, charEnd);
                        }

                        if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                        {
                            Contract.Assert(bytes >= byteStart + 2 || chars == charStart, "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (lonely surrogate)");
                            bytes -= 2;
                            fallbackBuffer.InternalReset();
                            ThrowCharsOverflow(decoder, chars == charStart);
                            break;
                        }

                        continue;
                    }

                    if (chars >= charEnd - 1)
                    {
                        Contract.Assert(bytes >= byteStart + 2 || chars == charStart, "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (surrogate pair)");
                        bytes -= 2;
                        ThrowCharsOverflow(decoder, chars == charStart);
                        break;
                    }

                    *chars++ = lastChar;
                    lastChar = (char)0;
                }
                else if (lastChar > 0)
                {
                    byte[] byteBuffer = null;
                    if (bigEndian)
                    {
                        byteBuffer = new byte[]{unchecked ((byte)(lastChar >> 8)), unchecked ((byte)lastChar)};
                    }
                    else
                    {
                        byteBuffer = new byte[]{unchecked ((byte)lastChar), unchecked ((byte)(lastChar >> 8))};
                    }

                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteStart, charEnd);
                    }

                    if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                    {
                        Contract.Assert(bytes >= byteStart + 2 || chars == charStart, "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (no low surrogate)");
                        bytes -= 2;
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);
                        break;
                    }

                    lastChar = (char)0;
                }

                if (chars >= charEnd)
                {
                    Contract.Assert(bytes >= byteStart + 2 || chars == charStart, "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (normal)");
                    bytes -= 2;
                    ThrowCharsOverflow(decoder, chars == charStart);
                    break;
                }

                *chars++ = ch;
            }

            if (decoder == null || decoder.MustFlush)
            {
                if (lastChar > 0)
                {
                    byte[] byteBuffer = null;
                    if (bigEndian)
                    {
                        byteBuffer = new byte[]{unchecked ((byte)(lastChar >> 8)), unchecked ((byte)lastChar)};
                    }
                    else
                    {
                        byteBuffer = new byte[]{unchecked ((byte)lastChar), unchecked ((byte)(lastChar >> 8))};
                    }

                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteStart, charEnd);
                    }

                    if (!fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars))
                    {
                        Contract.Assert(bytes >= byteStart + 2 || chars == charStart, "[UnicodeEncoding.GetChars]Expected bytes to have advanced or no output (decoder)");
                        bytes -= 2;
                        if (lastByte >= 0)
                            bytes--;
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);
                        bytes += 2;
                        if (lastByte >= 0)
                            bytes++;
                        goto End;
                    }

                    lastChar = (char)0;
                }

                if (lastByte >= 0)
                {
                    if (fallbackBuffer == null)
                    {
                        if (decoder == null)
                            fallbackBuffer = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallbackBuffer = decoder.FallbackBuffer;
                        fallbackBuffer.InternalInitialize(byteStart, charEnd);
                    }

                    if (!fallbackBuffer.InternalFallback(new byte[]{unchecked ((byte)lastByte)}, bytes, ref chars))
                    {
                        bytes--;
                        fallbackBuffer.InternalReset();
                        ThrowCharsOverflow(decoder, chars == charStart);
                        bytes++;
                        goto End;
                    }

                    lastByte = -1;
                }
            }

            End:
                if (decoder != null)
                {
                    Contract.Assert((decoder.MustFlush == false) || ((lastChar == (char)0) && (lastByte == -1)), "[UnicodeEncoding.GetChars] Expected no left over chars or bytes if flushing");
                    decoder.m_bytesUsed = (int)(bytes - byteStart);
                    decoder.lastChar = lastChar;
                    decoder.lastByte = lastByte;
                }

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0, "[UnicodeEncoding.GetChars]Expected empty fallback buffer at end");
            return (int)(chars - charStart);
        }

        public override System.Text.Encoder GetEncoder()
        {
            return new EncoderNLS(this);
        }

        public override System.Text.Decoder GetDecoder()
        {
            return new UnicodeEncoding.Decoder(this);
        }

        public override byte[] GetPreamble()
        {
            if (byteOrderMark)
            {
                if (bigEndian)
                    return new byte[2]{0xfe, 0xff};
                else
                    return new byte[2]{0xff, 0xfe};
            }

            return EmptyArray<Byte>.Value;
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            long byteCount = (long)charCount + 1;
            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;
            byteCount <<= 1;
            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));
            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            long charCount = (long)(byteCount >> 1) + (byteCount & 1) + 1;
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;
            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));
            return (int)charCount;
        }

        public override bool Equals(Object value)
        {
            UnicodeEncoding that = value as UnicodeEncoding;
            if (that != null)
            {
                return (CodePage == that.CodePage) && byteOrderMark == that.byteOrderMark && bigEndian == that.bigEndian && (EncoderFallback.Equals(that.EncoderFallback)) && (DecoderFallback.Equals(that.DecoderFallback));
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return CodePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() + (byteOrderMark ? 4 : 0) + (bigEndian ? 8 : 0);
        }

        private class Decoder : System.Text.DecoderNLS, ISerializable
        {
            internal int lastByte = -1;
            internal char lastChar = '\0';
            public Decoder(UnicodeEncoding encoding): base (encoding)
            {
            }

            internal Decoder(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");
                Contract.EndContractBlock();
                this.lastByte = (int)info.GetValue("lastByte", typeof (int));
                try
                {
                    this.m_encoding = (Encoding)info.GetValue("m_encoding", typeof (Encoding));
                    this.lastChar = (char)info.GetValue("lastChar", typeof (char));
                    this.m_fallback = (DecoderFallback)info.GetValue("m_fallback", typeof (DecoderFallback));
                }
                catch (SerializationException)
                {
                    bool bigEndian = (bool)info.GetValue("bigEndian", typeof (bool));
                    this.m_encoding = new UnicodeEncoding(bigEndian, false);
                }
            }

            public override void Reset()
            {
                lastByte = -1;
                lastChar = '\0';
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            internal override bool HasState
            {
                get
                {
                    return (this.lastByte != -1 || this.lastChar != '\0');
                }
            }
        }
    }
}