namespace System.Text
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    public class UTF8Encoding : Encoding
    {
        private const int UTF8_CODEPAGE = 65001;
        private bool emitUTF8Identifier = false;
        private bool isThrowException = false;
        public UTF8Encoding(): this (false)
        {
        }

        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier): this (encoderShouldEmitUTF8Identifier, false)
        {
        }

        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes): base (UTF8_CODEPAGE)
        {
            this.emitUTF8Identifier = encoderShouldEmitUTF8Identifier;
            this.isThrowException = throwOnInvalidBytes;
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

        public override unsafe int GetByteCount(String chars)
        {
            if (chars == null)
                throw new ArgumentNullException("s");
            Contract.EndContractBlock();
            fixed (char *pChars = chars)
                return GetByteCount(pChars, chars.Length, null);
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
            EncoderFallbackBuffer fallbackBuffer = null;
            char *pSrc = chars;
            char *pEnd = pSrc + count;
            int byteCount = count;
            int ch = 0;
            if (baseEncoder != null)
            {
                UTF8Encoder encoder = (UTF8Encoder)baseEncoder;
                ch = encoder.surrogateChar;
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", this.EncodingName, encoder.Fallback.GetType()));
                    fallbackBuffer.InternalInitialize(chars, pEnd, encoder, false);
                }
            }

            for (;;)
            {
                if (pSrc >= pEnd)
                {
                    if (ch == 0)
                    {
                        ch = fallbackBuffer != null ? fallbackBuffer.InternalGetNextChar() : 0;
                        if (ch > 0)
                        {
                            byteCount++;
                            goto ProcessChar;
                        }
                    }
                    else
                    {
                        if (fallbackBuffer != null && fallbackBuffer.bFallingBack)
                        {
                            Contract.Assert(ch >= 0xD800 && ch <= 0xDBFF, "[UTF8Encoding.GetBytes]expected high surrogate, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                            ch = fallbackBuffer.InternalGetNextChar();
                            byteCount++;
                            if (InRange(ch, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                            {
                                ch = 0xfffd;
                                byteCount++;
                                goto EncodeChar;
                            }
                            else if (ch > 0)
                            {
                                goto ProcessChar;
                            }
                            else
                            {
                                byteCount--;
                                break;
                            }
                        }
                    }

                    if (ch <= 0)
                    {
                        break;
                    }

                    if (baseEncoder != null && !baseEncoder.MustFlush)
                    {
                        break;
                    }

                    byteCount++;
                    goto EncodeChar;
                }

                if (ch > 0)
                {
                    Contract.Assert(ch >= 0xD800 && ch <= 0xDBFF, "[UTF8Encoding.GetBytes]expected high surrogate, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                    int cha = *pSrc;
                    byteCount++;
                    if (InRange(cha, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                    {
                        ch = 0xfffd;
                        pSrc++;
                    }

                    goto EncodeChar;
                }

                if (fallbackBuffer != null)
                {
                    ch = fallbackBuffer.InternalGetNextChar();
                    if (ch > 0)
                    {
                        byteCount++;
                        goto ProcessChar;
                    }
                }

                ch = *pSrc;
                pSrc++;
                ProcessChar:
                    if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.HIGH_SURROGATE_END))
                    {
                        byteCount--;
                        continue;
                    }

                EncodeChar:
                    if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                    {
                        if (fallbackBuffer == null)
                        {
                            if (baseEncoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = baseEncoder.FallbackBuffer;
                            fallbackBuffer.InternalInitialize(chars, chars + count, baseEncoder, false);
                        }

                        fallbackBuffer.InternalFallback(unchecked ((char)ch), ref pSrc);
                        byteCount--;
                        ch = 0;
                        continue;
                    }

                if (ch > 0x7F)
                {
                    if (ch > 0x7FF)
                    {
                        byteCount++;
                    }

                    byteCount++;
                }

                if (byteCount < 0)
                {
                    break;
                }

                if (fallbackBuffer != null && (ch = fallbackBuffer.InternalGetNextChar()) != 0)
                {
                    byteCount++;
                    goto ProcessChar;
                }

                int availableChars = PtrDiff(pEnd, pSrc);
                if (availableChars <= 13)
                {
                    char *pLocalEnd = pEnd;
                    while (pSrc < pLocalEnd)
                    {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                            goto ProcessChar;
                    }

                    break;
                }

                availableChars &= 0x0FFFFFFF;
                char *pStop = pSrc + availableChars - (3 + 4);
                while (pSrc < pStop)
                {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F)
                    {
                        if (ch > 0x7FF)
                        {
                            if ((ch & 0xF800) == 0xD800)
                                goto LongCode;
                            byteCount++;
                        }

                        byteCount++;
                    }

                    if ((unchecked ((int)pSrc) & 0x2) != 0)
                    {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                        {
                            if (ch > 0x7FF)
                            {
                                if ((ch & 0xF800) == 0xD800)
                                    goto LongCode;
                                byteCount++;
                            }

                            byteCount++;
                        }
                    }

                    while (pSrc < pStop)
                    {
                        ch = *(int *)pSrc;
                        int chc = *(int *)(pSrc + 2);
                        if (((ch | chc) & unchecked ((int)0xFF80FF80)) != 0)
                        {
                            if (((ch | chc) & unchecked ((int)0xF800F800)) != 0)
                            {
                                goto LongCodeWithMask;
                            }

                            if ((ch & unchecked ((int)0xFF800000)) != 0)
                                byteCount++;
                            if ((ch & unchecked ((int)0xFF80)) != 0)
                                byteCount++;
                            if ((chc & unchecked ((int)0xFF800000)) != 0)
                                byteCount++;
                            if ((chc & unchecked ((int)0xFF80)) != 0)
                                byteCount++;
                        }

                        pSrc += 4;
                        ch = *(int *)pSrc;
                        chc = *(int *)(pSrc + 2);
                        if (((ch | chc) & unchecked ((int)0xFF80FF80)) != 0)
                        {
                            if (((ch | chc) & unchecked ((int)0xF800F800)) != 0)
                            {
                                goto LongCodeWithMask;
                            }

                            if ((ch & unchecked ((int)0xFF800000)) != 0)
                                byteCount++;
                            if ((ch & unchecked ((int)0xFF80)) != 0)
                                byteCount++;
                            if ((chc & unchecked ((int)0xFF800000)) != 0)
                                byteCount++;
                            if ((chc & unchecked ((int)0xFF80)) != 0)
                                byteCount++;
                        }

                        pSrc += 4;
                    }

                    break;
                    LongCodeWithMask:
                        ch = (char)ch;
                    pSrc++;
                    if (ch <= 0x7F)
                    {
                        continue;
                    }

                    LongCode:
                        if (ch > 0x7FF)
                        {
                            if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                            {
                                int chd = *pSrc;
                                if (ch > CharUnicodeInfo.HIGH_SURROGATE_END || !InRange(chd, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                                {
                                    pSrc--;
                                    break;
                                }

                                pSrc++;
                            }

                            byteCount++;
                        }

                    byteCount++;
                }

                ch = 0;
            }

            if (byteCount < 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ConversionOverflow"));
            }

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0, "[UTF8Encoding.GetByteCount]Expected Empty fallback buffer");
            return byteCount;
        }

        unsafe private static int PtrDiff(char *a, char *b)
        {
            return (int)(((uint)((byte *)a - (byte *)b)) >> 1);
        }

        unsafe private static int PtrDiff(byte *a, byte *b)
        {
            return (int)(a - b);
        }

        private static bool InRange(int ch, int start, int end)
        {
            return (uint)(ch - start) <= (uint)(end - start);
        }

        internal override unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, EncoderNLS baseEncoder)
        {
            Contract.Assert(chars != null, "[UTF8Encoding.GetBytes]chars!=null");
            Contract.Assert(byteCount >= 0, "[UTF8Encoding.GetBytes]byteCount >=0");
            Contract.Assert(charCount >= 0, "[UTF8Encoding.GetBytes]charCount >=0");
            Contract.Assert(bytes != null, "[UTF8Encoding.GetBytes]bytes!=null");
            UTF8Encoder encoder = null;
            EncoderFallbackBuffer fallbackBuffer = null;
            char *pSrc = chars;
            byte *pTarget = bytes;
            char *pEnd = pSrc + charCount;
            byte *pAllocatedBufferEnd = pTarget + byteCount;
            int ch = 0;
            if (baseEncoder != null)
            {
                encoder = (UTF8Encoder)baseEncoder;
                ch = encoder.surrogateChar;
                if (encoder.InternalHasFallbackBuffer)
                {
                    fallbackBuffer = encoder.FallbackBuffer;
                    if (fallbackBuffer.Remaining > 0 && encoder.m_throwOnOverflow)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", this.EncodingName, encoder.Fallback.GetType()));
                    fallbackBuffer.InternalInitialize(chars, pEnd, encoder, true);
                }
            }

            for (;;)
            {
                if (pSrc >= pEnd)
                {
                    if (ch == 0)
                    {
                        ch = fallbackBuffer != null ? fallbackBuffer.InternalGetNextChar() : 0;
                        if (ch > 0)
                        {
                            goto ProcessChar;
                        }
                    }
                    else
                    {
                        if (fallbackBuffer != null && fallbackBuffer.bFallingBack)
                        {
                            Contract.Assert(ch >= 0xD800 && ch <= 0xDBFF, "[UTF8Encoding.GetBytes]expected high surrogate, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                            int cha = ch;
                            ch = fallbackBuffer.InternalGetNextChar();
                            if (InRange(ch, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                            {
                                ch = ch + (cha << 10) + (0x10000 - CharUnicodeInfo.LOW_SURROGATE_START - (CharUnicodeInfo.HIGH_SURROGATE_START << 10));
                                goto EncodeChar;
                            }
                            else if (ch > 0)
                            {
                                goto ProcessChar;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (ch > 0 && (encoder == null || encoder.MustFlush))
                        goto EncodeChar;
                    break;
                }

                if (ch > 0)
                {
                    Contract.Assert(ch >= 0xD800 && ch <= 0xDBFF, "[UTF8Encoding.GetBytes]expected high surrogate, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));
                    int cha = *pSrc;
                    if (InRange(cha, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                    {
                        ch = cha + (ch << 10) + (0x10000 - CharUnicodeInfo.LOW_SURROGATE_START - (CharUnicodeInfo.HIGH_SURROGATE_START << 10));
                        pSrc++;
                    }

                    goto EncodeChar;
                }

                if (fallbackBuffer != null)
                {
                    ch = fallbackBuffer.InternalGetNextChar();
                    if (ch > 0)
                        goto ProcessChar;
                }

                ch = *pSrc;
                pSrc++;
                ProcessChar:
                    if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.HIGH_SURROGATE_END))
                    {
                        continue;
                    }

                EncodeChar:
                    if (InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                    {
                        if (fallbackBuffer == null)
                        {
                            if (baseEncoder == null)
                                fallbackBuffer = this.encoderFallback.CreateFallbackBuffer();
                            else
                                fallbackBuffer = baseEncoder.FallbackBuffer;
                            fallbackBuffer.InternalInitialize(chars, pEnd, baseEncoder, true);
                        }

                        fallbackBuffer.InternalFallback(unchecked ((char)ch), ref pSrc);
                        ch = 0;
                        continue;
                    }

                int bytesNeeded = 1;
                if (ch > 0x7F)
                {
                    if (ch > 0x7FF)
                    {
                        if (ch > 0xFFFF)
                        {
                            bytesNeeded++;
                        }

                        bytesNeeded++;
                    }

                    bytesNeeded++;
                }

                if (pTarget > pAllocatedBufferEnd - bytesNeeded)
                {
                    if (fallbackBuffer != null && fallbackBuffer.bFallingBack)
                    {
                        fallbackBuffer.MovePrevious();
                        if (ch > 0xFFFF)
                            fallbackBuffer.MovePrevious();
                    }
                    else
                    {
                        pSrc--;
                        if (ch > 0xFFFF)
                            pSrc--;
                    }

                    Contract.Assert(pSrc >= chars || pTarget == bytes, "[UTF8Encoding.GetBytes]Expected pSrc to be within buffer or to throw with insufficient room.");
                    ThrowBytesOverflow(encoder, pTarget == bytes);
                    ch = 0;
                    break;
                }

                if (ch <= 0x7F)
                {
                    *pTarget = (byte)ch;
                }
                else
                {
                    int chb;
                    if (ch <= 0x7FF)
                    {
                        chb = (byte)(unchecked ((sbyte)0xC0) | (ch >> 6));
                    }
                    else
                    {
                        if (ch <= 0xFFFF)
                        {
                            chb = (byte)(unchecked ((sbyte)0xE0) | (ch >> 12));
                        }
                        else
                        {
                            *pTarget = (byte)(unchecked ((sbyte)0xF0) | (ch >> 18));
                            pTarget++;
                            chb = unchecked ((sbyte)0x80) | (ch >> 12) & 0x3F;
                        }

                        *pTarget = (byte)chb;
                        pTarget++;
                        chb = unchecked ((sbyte)0x80) | (ch >> 6) & 0x3F;
                    }

                    *pTarget = (byte)chb;
                    pTarget++;
                    *pTarget = (byte)(unchecked ((sbyte)0x80) | ch & 0x3F);
                }

                pTarget++;
                if (fallbackBuffer != null && (ch = fallbackBuffer.InternalGetNextChar()) != 0)
                    goto ProcessChar;
                int availableChars = PtrDiff(pEnd, pSrc);
                int availableBytes = PtrDiff(pAllocatedBufferEnd, pTarget);
                if (availableChars <= 13)
                {
                    if (availableBytes < availableChars)
                    {
                        ch = 0;
                        continue;
                    }

                    char *pLocalEnd = pEnd;
                    while (pSrc < pLocalEnd)
                    {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                            goto ProcessChar;
                        *pTarget = (byte)ch;
                        pTarget++;
                    }

                    ch = 0;
                    break;
                }

                if (availableBytes < availableChars)
                {
                    availableChars = availableBytes;
                }

                char *pStop = pSrc + availableChars - 5;
                while (pSrc < pStop)
                {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F)
                    {
                        goto LongCode;
                    }

                    *pTarget = (byte)ch;
                    pTarget++;
                    if ((unchecked ((int)pSrc) & 0x2) != 0)
                    {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                        {
                            goto LongCode;
                        }

                        *pTarget = (byte)ch;
                        pTarget++;
                    }

                    while (pSrc < pStop)
                    {
                        ch = *(int *)pSrc;
                        int chc = *(int *)(pSrc + 2);
                        if (((ch | chc) & unchecked ((int)0xFF80FF80)) != 0)
                        {
                            goto LongCodeWithMask;
                        }

                        *pTarget = (byte)ch;
                        *(pTarget + 1) = (byte)(ch >> 16);
                        pSrc += 4;
                        *(pTarget + 2) = (byte)chc;
                        *(pTarget + 3) = (byte)(chc >> 16);
                        pTarget += 4;
                    }

                    continue;
                    LongCodeWithMask:
                        ch = (char)ch;
                    pSrc++;
                    if (ch > 0x7F)
                    {
                        goto LongCode;
                    }

                    *pTarget = (byte)ch;
                    pTarget++;
                    continue;
                    LongCode:
                        int chd;
                    if (ch <= 0x7FF)
                    {
                        chd = unchecked ((sbyte)0xC0) | (ch >> 6);
                    }
                    else
                    {
                        if (!InRange(ch, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                        {
                            chd = unchecked ((sbyte)0xE0) | (ch >> 12);
                        }
                        else
                        {
                            if (ch > CharUnicodeInfo.HIGH_SURROGATE_END)
                            {
                                pSrc -= 1;
                                break;
                            }

                            chd = *pSrc;
                            pSrc++;
                            if (!InRange(chd, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END))
                            {
                                pSrc -= 2;
                                break;
                            }

                            ch = chd + (ch << 10) + (0x10000 - CharUnicodeInfo.LOW_SURROGATE_START - (CharUnicodeInfo.HIGH_SURROGATE_START << 10));
                            *pTarget = (byte)(unchecked ((sbyte)0xF0) | (ch >> 18));
                            pTarget++;
                            chd = unchecked ((sbyte)0x80) | (ch >> 12) & 0x3F;
                        }

                        *pTarget = (byte)chd;
                        pStop--;
                        pTarget++;
                        chd = unchecked ((sbyte)0x80) | (ch >> 6) & 0x3F;
                    }

                    *pTarget = (byte)chd;
                    pStop--;
                    pTarget++;
                    *pTarget = (byte)(unchecked ((sbyte)0x80) | ch & 0x3F);
                    pTarget++;
                }

                Contract.Assert(pTarget <= pAllocatedBufferEnd, "[UTF8Encoding.GetBytes]pTarget <= pAllocatedBufferEnd");
                ch = 0;
            }

            if (encoder != null)
            {
                Contract.Assert(!encoder.MustFlush || ch == 0, "[UTF8Encoding.GetBytes] Expected no mustflush or 0 leftover ch " + ch.ToString("X2", CultureInfo.InvariantCulture));
                encoder.surrogateChar = ch;
                encoder.m_charsUsed = (int)(pSrc - chars);
            }

            Contract.Assert(fallbackBuffer == null || fallbackBuffer.Remaining == 0 || baseEncoder == null || !baseEncoder.m_throwOnOverflow, "[UTF8Encoding.GetBytes]Expected empty fallback buffer if not converting");
            return (int)(pTarget - bytes);
        }

        private const int FinalByte = 1 << 29;
        private const int SupplimentarySeq = 1 << 28;
        private const int ThreeByteSeq = 1 << 27;
        internal override unsafe int GetCharCount(byte *bytes, int count, DecoderNLS baseDecoder)
        {
            Contract.Assert(count >= 0, "[UTF8Encoding.GetCharCount]count >=0");
            Contract.Assert(bytes != null, "[UTF8Encoding.GetCharCount]bytes!=null");
            byte *pSrc = bytes;
            byte *pEnd = pSrc + count;
            int charCount = count;
            int ch = 0;
            DecoderFallbackBuffer fallback = null;
            if (baseDecoder != null)
            {
                UTF8Decoder decoder = (UTF8Decoder)baseDecoder;
                ch = decoder.bits;
                charCount -= (ch >> 30);
                Contract.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0, "[UTF8Encoding.GetCharCount]Expected empty fallback buffer at start");
            }

            for (;;)
            {
                if (pSrc >= pEnd)
                {
                    break;
                }

                if (ch == 0)
                {
                    goto ReadChar;
                }

                int cha = *pSrc;
                pSrc++;
                if ((cha & unchecked ((sbyte)0xC0)) != 0x80)
                {
                    pSrc--;
                    charCount += (ch >> 30);
                    goto InvalidByteSequence;
                }

                ch = (ch << 6) | (cha & 0x3F);
                if ((ch & FinalByte) == 0)
                {
                    Contract.Assert((ch & (SupplimentarySeq | ThreeByteSeq)) != 0, "[UTF8Encoding.GetChars]Invariant volation");
                    if ((ch & SupplimentarySeq) != 0)
                    {
                        if ((ch & (FinalByte >> 6)) != 0)
                        {
                            continue;
                        }

                        if (!InRange(ch & 0x1F0, 0x10, 0x100))
                        {
                            goto InvalidByteSequence;
                        }
                    }
                    else
                    {
                        if ((ch & (0x1F << 5)) == 0 || (ch & (0xF800 >> 6)) == (0xD800 >> 6))
                        {
                            goto InvalidByteSequence;
                        }
                    }

                    continue;
                }

                if ((ch & (SupplimentarySeq | 0x1F0000)) == SupplimentarySeq)
                {
                    charCount--;
                }

                goto EncodeChar;
                InvalidByteSequence:
                    if (fallback == null)
                    {
                        if (baseDecoder == null)
                            fallback = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallback = baseDecoder.FallbackBuffer;
                        fallback.InternalInitialize(bytes, null);
                    }

                charCount += FallbackInvalidByteSequence(pSrc, ch, fallback);
                ch = 0;
                continue;
                ReadChar:
                    ch = *pSrc;
                pSrc++;
                ProcessChar:
                    if (ch > 0x7F)
                    {
                        charCount--;
                        if ((ch & 0x40) == 0)
                        {
                            goto InvalidByteSequence;
                        }

                        if ((ch & 0x20) != 0)
                        {
                            if ((ch & 0x10) != 0)
                            {
                                ch &= 0x0F;
                                if (ch > 0x04)
                                {
                                    ch |= 0xf0;
                                    goto InvalidByteSequence;
                                }

                                ch |= (FinalByte >> 3 * 6) | (1 << 30) | (3 << (30 - 2 * 6)) | (SupplimentarySeq) | (SupplimentarySeq >> 6) | (SupplimentarySeq >> 2 * 6) | (SupplimentarySeq >> 3 * 6);
                                charCount--;
                            }
                            else
                            {
                                ch = (ch & 0x0F) | ((FinalByte >> 2 * 6) | (1 << 30) | (ThreeByteSeq) | (ThreeByteSeq >> 6) | (ThreeByteSeq >> 2 * 6));
                                charCount--;
                            }
                        }
                        else
                        {
                            ch &= 0x1F;
                            if (ch <= 1)
                            {
                                ch |= 0xc0;
                                goto InvalidByteSequence;
                            }

                            ch |= (FinalByte >> 6);
                        }

                        continue;
                    }

                EncodeChar:
                    int availableBytes = PtrDiff(pEnd, pSrc);
                if (availableBytes <= 13)
                {
                    byte *pLocalEnd = pEnd;
                    while (pSrc < pLocalEnd)
                    {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                            goto ProcessChar;
                    }

                    ch = 0;
                    break;
                }

                byte *pStop = pSrc + availableBytes - 7;
                while (pSrc < pStop)
                {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F)
                    {
                        goto LongCode;
                    }

                    if ((unchecked ((int)pSrc) & 0x1) != 0)
                    {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                        {
                            goto LongCode;
                        }
                    }

                    if ((unchecked ((int)pSrc) & 0x2) != 0)
                    {
                        ch = *(ushort *)pSrc;
                        if ((ch & 0x8080) != 0)
                        {
                            goto LongCodeWithMask16;
                        }

                        pSrc += 2;
                    }

                    while (pSrc < pStop)
                    {
                        ch = *(int *)pSrc;
                        int chb = *(int *)(pSrc + 4);
                        if (((ch | chb) & unchecked ((int)0x80808080)) != 0)
                        {
                            goto LongCodeWithMask32;
                        }

                        pSrc += 8;
                        if (pSrc >= pStop)
                            break;
                        ch = *(int *)pSrc;
                        chb = *(int *)(pSrc + 4);
                        if (((ch | chb) & unchecked ((int)0x80808080)) != 0)
                        {
                            goto LongCodeWithMask32;
                        }

                        pSrc += 8;
                    }

                    break;
                    LongCodeWithMask32:
                        LongCodeWithMask16:
                            ch &= 0xFF;
                    pSrc++;
                    if (ch <= 0x7F)
                    {
                        continue;
                    }

                    LongCode:
                        int chc = *pSrc;
                    pSrc++;
                    if ((ch & 0x40) == 0 || (chc & unchecked ((sbyte)0xC0)) != 0x80)
                    {
                        goto BadLongCode;
                    }

                    chc &= 0x3F;
                    if ((ch & 0x20) != 0)
                    {
                        chc |= (ch & 0x0F) << 6;
                        if ((ch & 0x10) != 0)
                        {
                            ch = *pSrc;
                            if (!InRange(chc >> 4, 0x01, 0x10) || (ch & unchecked ((sbyte)0xC0)) != 0x80)
                            {
                                goto BadLongCode;
                            }

                            chc = (chc << 6) | (ch & 0x3F);
                            ch = *(pSrc + 1);
                            if ((ch & unchecked ((sbyte)0xC0)) != 0x80)
                            {
                                goto BadLongCode;
                            }

                            pSrc += 2;
                            charCount--;
                        }
                        else
                        {
                            ch = *pSrc;
                            if ((chc & (0x1F << 5)) == 0 || (chc & (0xF800 >> 6)) == (0xD800 >> 6) || (ch & unchecked ((sbyte)0xC0)) != 0x80)
                            {
                                goto BadLongCode;
                            }

                            pSrc++;
                            charCount--;
                        }
                    }
                    else
                    {
                        if ((ch & 0x1E) == 0)
                        {
                            goto BadLongCode;
                        }
                    }

                    charCount--;
                }

                ch = 0;
                continue;
                BadLongCode:
                    pSrc -= 2;
                ch = 0;
                continue;
            }

            if (ch != 0)
            {
                charCount += (ch >> 30);
                if (baseDecoder == null || baseDecoder.MustFlush)
                {
                    if (fallback == null)
                    {
                        if (baseDecoder == null)
                            fallback = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallback = baseDecoder.FallbackBuffer;
                        fallback.InternalInitialize(bytes, null);
                    }

                    charCount += FallbackInvalidByteSequence(pSrc, ch, fallback);
                }
            }

            Contract.Assert(fallback == null || fallback.Remaining == 0, "[UTF8Encoding.GetCharCount]Expected empty fallback buffer at end");
            return charCount;
        }

        internal override unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, DecoderNLS baseDecoder)
        {
            Contract.Assert(chars != null, "[UTF8Encoding.GetChars]chars!=null");
            Contract.Assert(byteCount >= 0, "[UTF8Encoding.GetChars]count >=0");
            Contract.Assert(charCount >= 0, "[UTF8Encoding.GetChars]charCount >=0");
            Contract.Assert(bytes != null, "[UTF8Encoding.GetChars]bytes!=null");
            byte *pSrc = bytes;
            char *pTarget = chars;
            byte *pEnd = pSrc + byteCount;
            char *pAllocatedBufferEnd = pTarget + charCount;
            int ch = 0;
            DecoderFallbackBuffer fallback = null;
            if (baseDecoder != null)
            {
                UTF8Decoder decoder = (UTF8Decoder)baseDecoder;
                ch = decoder.bits;
                Contract.Assert(!decoder.InternalHasFallbackBuffer || decoder.FallbackBuffer.Remaining == 0, "[UTF8Encoding.GetChars]Expected empty fallback buffer at start");
            }

            for (;;)
            {
                if (pSrc >= pEnd)
                {
                    break;
                }

                if (ch == 0)
                {
                    goto ReadChar;
                }

                int cha = *pSrc;
                pSrc++;
                if ((cha & unchecked ((sbyte)0xC0)) != 0x80)
                {
                    pSrc--;
                    goto InvalidByteSequence;
                }

                ch = (ch << 6) | (cha & 0x3F);
                if ((ch & FinalByte) == 0)
                {
                    Contract.Assert((ch & (SupplimentarySeq | ThreeByteSeq)) != 0, "[UTF8Encoding.GetChars]Invariant volation");
                    if ((ch & SupplimentarySeq) != 0)
                    {
                        if ((ch & (FinalByte >> 6)) != 0)
                        {
                            continue;
                        }

                        if (!InRange(ch & 0x1F0, 0x10, 0x100))
                        {
                            goto InvalidByteSequence;
                        }
                    }
                    else
                    {
                        if ((ch & (0x1F << 5)) == 0 || (ch & (0xF800 >> 6)) == (0xD800 >> 6))
                        {
                            goto InvalidByteSequence;
                        }
                    }

                    continue;
                }

                if ((ch & (SupplimentarySeq | 0x1F0000)) > SupplimentarySeq)
                {
                    if (pTarget < pAllocatedBufferEnd)
                    {
                        *pTarget = (char)(((ch >> 10) & 0x7FF) + unchecked ((short)((CharUnicodeInfo.HIGH_SURROGATE_START - (0x10000 >> 10)))));
                        pTarget++;
                        ch = (ch & 0x3FF) + unchecked ((int)(CharUnicodeInfo.LOW_SURROGATE_START));
                    }
                }

                goto EncodeChar;
                InvalidByteSequence:
                    if (fallback == null)
                    {
                        if (baseDecoder == null)
                            fallback = this.decoderFallback.CreateFallbackBuffer();
                        else
                            fallback = baseDecoder.FallbackBuffer;
                        fallback.InternalInitialize(bytes, pAllocatedBufferEnd);
                    }

                if (!FallbackInvalidByteSequence(ref pSrc, ch, fallback, ref pTarget))
                {
                    Contract.Assert(pSrc >= bytes || pTarget == chars, "[UTF8Encoding.GetChars]Expected to throw or remain in byte buffer after fallback");
                    fallback.InternalReset();
                    ThrowCharsOverflow(baseDecoder, pTarget == chars);
                    ch = 0;
                    break;
                }

                Contract.Assert(pSrc >= bytes, "[UTF8Encoding.GetChars]Expected invalid byte sequence to have remained within the byte array");
                ch = 0;
                continue;
                ReadChar:
                    ch = *pSrc;
                pSrc++;
                ProcessChar:
                    if (ch > 0x7F)
                    {
                        if ((ch & 0x40) == 0)
                        {
                            goto InvalidByteSequence;
                        }

                        if ((ch & 0x20) != 0)
                        {
                            if ((ch & 0x10) != 0)
                            {
                                ch &= 0x0F;
                                if (ch > 0x04)
                                {
                                    ch |= 0xf0;
                                    goto InvalidByteSequence;
                                }

                                ch |= (FinalByte >> 3 * 6) | (1 << 30) | (3 << (30 - 2 * 6)) | (SupplimentarySeq) | (SupplimentarySeq >> 6) | (SupplimentarySeq >> 2 * 6) | (SupplimentarySeq >> 3 * 6);
                            }
                            else
                            {
                                ch = (ch & 0x0F) | ((FinalByte >> 2 * 6) | (1 << 30) | (ThreeByteSeq) | (ThreeByteSeq >> 6) | (ThreeByteSeq >> 2 * 6));
                            }
                        }
                        else
                        {
                            ch &= 0x1F;
                            if (ch <= 1)
                            {
                                ch |= 0xc0;
                                goto InvalidByteSequence;
                            }

                            ch |= (FinalByte >> 6);
                        }

                        continue;
                    }

                EncodeChar:
                    if (pTarget >= pAllocatedBufferEnd)
                    {
                        ch &= 0x1fffff;
                        if (ch > 0x7f)
                        {
                            if (ch > 0x7ff)
                            {
                                if (ch >= CharUnicodeInfo.LOW_SURROGATE_START && ch <= CharUnicodeInfo.LOW_SURROGATE_END)
                                {
                                    pSrc--;
                                    pTarget--;
                                }
                                else if (ch > 0xffff)
                                {
                                    pSrc--;
                                }

                                pSrc--;
                            }

                            pSrc--;
                        }

                        pSrc--;
                        Contract.Assert(pSrc >= bytes || pTarget == chars, "[UTF8Encoding.GetChars]Expected pSrc to be within input buffer or throw due to no output]");
                        ThrowCharsOverflow(baseDecoder, pTarget == chars);
                        ch = 0;
                        break;
                    }

                *pTarget = (char)ch;
                pTarget++;
                int availableChars = PtrDiff(pAllocatedBufferEnd, pTarget);
                int availableBytes = PtrDiff(pEnd, pSrc);
                if (availableBytes <= 13)
                {
                    if (availableChars < availableBytes)
                    {
                        ch = 0;
                        continue;
                    }

                    byte *pLocalEnd = pEnd;
                    while (pSrc < pLocalEnd)
                    {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                            goto ProcessChar;
                        *pTarget = (char)ch;
                        pTarget++;
                    }

                    ch = 0;
                    break;
                }

                if (availableChars < availableBytes)
                {
                    availableBytes = availableChars;
                }

                char *pStop = pTarget + availableBytes - 7;
                while (pTarget < pStop)
                {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F)
                    {
                        goto LongCode;
                    }

                    *pTarget = (char)ch;
                    pTarget++;
                    if ((unchecked ((int)pSrc) & 0x1) != 0)
                    {
                        ch = *pSrc;
                        pSrc++;
                        if (ch > 0x7F)
                        {
                            goto LongCode;
                        }

                        *pTarget = (char)ch;
                        pTarget++;
                    }

                    if ((unchecked ((int)pSrc) & 0x2) != 0)
                    {
                        ch = *(ushort *)pSrc;
                        if ((ch & 0x8080) != 0)
                        {
                            goto LongCodeWithMask16;
                        }

                        *pTarget = (char)(ch & 0x7F);
                        pSrc += 2;
                        *(pTarget + 1) = (char)((ch >> 8) & 0x7F);
                        pTarget += 2;
                    }

                    while (pTarget < pStop)
                    {
                        ch = *(int *)pSrc;
                        int chb = *(int *)(pSrc + 4);
                        if (((ch | chb) & unchecked ((int)0x80808080)) != 0)
                        {
                            goto LongCodeWithMask32;
                        }

                        *pTarget = (char)(ch & 0x7F);
                        *(pTarget + 1) = (char)((ch >> 8) & 0x7F);
                        *(pTarget + 2) = (char)((ch >> 16) & 0x7F);
                        *(pTarget + 3) = (char)((ch >> 24) & 0x7F);
                        pSrc += 8;
                        *(pTarget + 4) = (char)(chb & 0x7F);
                        *(pTarget + 5) = (char)((chb >> 8) & 0x7F);
                        *(pTarget + 6) = (char)((chb >> 16) & 0x7F);
                        *(pTarget + 7) = (char)((chb >> 24) & 0x7F);
                        pTarget += 8;
                    }

                    break;
                    LongCodeWithMask32:
                        LongCodeWithMask16:
                            ch &= 0xFF;
                    pSrc++;
                    if (ch <= 0x7F)
                    {
                        *pTarget = (char)ch;
                        pTarget++;
                        continue;
                    }

                    LongCode:
                        int chc = *pSrc;
                    pSrc++;
                    if ((ch & 0x40) == 0 || (chc & unchecked ((sbyte)0xC0)) != 0x80)
                    {
                        goto BadLongCode;
                    }

                    chc &= 0x3F;
                    if ((ch & 0x20) != 0)
                    {
                        chc |= (ch & 0x0F) << 6;
                        if ((ch & 0x10) != 0)
                        {
                            ch = *pSrc;
                            if (!InRange(chc >> 4, 0x01, 0x10) || (ch & unchecked ((sbyte)0xC0)) != 0x80)
                            {
                                goto BadLongCode;
                            }

                            chc = (chc << 6) | (ch & 0x3F);
                            ch = *(pSrc + 1);
                            if ((ch & unchecked ((sbyte)0xC0)) != 0x80)
                            {
                                goto BadLongCode;
                            }

                            pSrc += 2;
                            ch = (chc << 6) | (ch & 0x3F);
                            *pTarget = (char)(((ch >> 10) & 0x7FF) + unchecked ((short)(CharUnicodeInfo.HIGH_SURROGATE_START - (0x10000 >> 10))));
                            pTarget++;
                            ch = (ch & 0x3FF) + unchecked ((short)(CharUnicodeInfo.LOW_SURROGATE_START));
                            pStop--;
                        }
                        else
                        {
                            ch = *pSrc;
                            if ((chc & (0x1F << 5)) == 0 || (chc & (0xF800 >> 6)) == (0xD800 >> 6) || (ch & unchecked ((sbyte)0xC0)) != 0x80)
                            {
                                goto BadLongCode;
                            }

                            pSrc++;
                            ch = (chc << 6) | (ch & 0x3F);
                            pStop--;
                        }
                    }
                    else
                    {
                        ch &= 0x1F;
                        if (ch <= 1)
                        {
                            goto BadLongCode;
                        }

                        ch = (ch << 6) | chc;
                    }

                    *pTarget = (char)ch;
                    pTarget++;
                    pStop--;
                }

                Contract.Assert(pTarget <= pAllocatedBufferEnd, "[UTF8Encoding.GetChars]pTarget <= pAllocatedBufferEnd");
                ch = 0;
                continue;
                BadLongCode:
                    pSrc -= 2;
                ch = 0;
                continue;
            }

            if (ch != 0 && (baseDecoder == null || baseDecoder.MustFlush))
            {
                if (fallback == null)
                {
                    if (baseDecoder == null)
                        fallback = this.decoderFallback.CreateFallbackBuffer();
                    else
                        fallback = baseDecoder.FallbackBuffer;
                    fallback.InternalInitialize(bytes, pAllocatedBufferEnd);
                }

                if (!FallbackInvalidByteSequence(ref pSrc, ch, fallback, ref pTarget))
                {
                    Contract.Assert(pSrc >= bytes || pTarget == chars, "[UTF8Encoding.GetChars]Expected to throw or remain in byte buffer while flushing");
                    fallback.InternalReset();
                    ThrowCharsOverflow(baseDecoder, pTarget == chars);
                }

                Contract.Assert(pSrc >= bytes, "[UTF8Encoding.GetChars]Expected flushing invalid byte sequence to have remained within the byte array");
                ch = 0;
            }

            if (baseDecoder != null)
            {
                UTF8Decoder decoder = (UTF8Decoder)baseDecoder;
                Contract.Assert(!baseDecoder.MustFlush || ch == 0 || !baseDecoder.m_throwOnOverflow, "[UTF8Encoding.GetChars]Expected no must flush or no left over bits or no throw on overflow.");
                decoder.bits = ch;
                baseDecoder.m_bytesUsed = (int)(pSrc - bytes);
            }

            Contract.Assert(fallback == null || fallback.Remaining == 0, "[UTF8Encoding.GetChars]Expected empty fallback buffer at end");
            return PtrDiff(pTarget, chars);
        }

        private unsafe bool FallbackInvalidByteSequence(ref byte *pSrc, int ch, DecoderFallbackBuffer fallback, ref char *pTarget)
        {
            byte *pStart = pSrc;
            byte[] bytesUnknown = GetBytesUnknown(ref pStart, ch);
            if (!fallback.InternalFallback(bytesUnknown, pSrc, ref pTarget))
            {
                pSrc = pStart;
                return false;
            }

            return true;
        }

        private unsafe int FallbackInvalidByteSequence(byte *pSrc, int ch, DecoderFallbackBuffer fallback)
        {
            byte[] bytesUnknown = GetBytesUnknown(ref pSrc, ch);
            int count = fallback.InternalFallback(bytesUnknown, pSrc);
            return count;
        }

        private unsafe byte[] GetBytesUnknown(ref byte *pSrc, int ch)
        {
            byte[] bytesUnknown = null;
            if (ch < 0x100 && ch >= 0)
            {
                pSrc--;
                bytesUnknown = new byte[]{unchecked ((byte)ch)};
            }
            else if ((ch & (SupplimentarySeq | ThreeByteSeq)) == 0)
            {
                pSrc--;
                bytesUnknown = new byte[]{unchecked ((byte)((ch & 0x1F) | 0xc0))};
            }
            else if ((ch & SupplimentarySeq) != 0)
            {
                if ((ch & (FinalByte >> 6)) != 0)
                {
                    pSrc -= 3;
                    bytesUnknown = new byte[]{unchecked ((byte)(((ch >> 12) & 0x07) | 0xF0)), unchecked ((byte)(((ch >> 6) & 0x3F) | 0x80)), unchecked ((byte)(((ch) & 0x3F) | 0x80))};
                }
                else if ((ch & (FinalByte >> 12)) != 0)
                {
                    pSrc -= 2;
                    bytesUnknown = new byte[]{unchecked ((byte)(((ch >> 6) & 0x07) | 0xF0)), unchecked ((byte)(((ch) & 0x3F) | 0x80))};
                }
                else
                {
                    pSrc--;
                    bytesUnknown = new byte[]{unchecked ((byte)(((ch) & 0x07) | 0xF0))};
                }
            }
            else
            {
                if ((ch & (FinalByte >> 6)) != 0)
                {
                    pSrc -= 2;
                    bytesUnknown = new byte[]{unchecked ((byte)(((ch >> 6) & 0x0F) | 0xE0)), unchecked ((byte)(((ch) & 0x3F) | 0x80))};
                }
                else
                {
                    pSrc--;
                    bytesUnknown = new byte[]{unchecked ((byte)(((ch) & 0x0F) | 0xE0))};
                }
            }

            return bytesUnknown;
        }

        public override Decoder GetDecoder()
        {
            return new UTF8Decoder(this);
        }

        public override Encoder GetEncoder()
        {
            return new UTF8Encoder(this);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            long byteCount = (long)charCount + 1;
            if (EncoderFallback.MaxCharCount > 1)
                byteCount *= EncoderFallback.MaxCharCount;
            byteCount *= 3;
            if (byteCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_GetByteCountOverflow"));
            return (int)byteCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            long charCount = ((long)byteCount + 1);
            if (DecoderFallback.MaxCharCount > 1)
            {
                charCount *= DecoderFallback.MaxCharCount;
            }

            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));
            return (int)charCount;
        }

        public override byte[] GetPreamble()
        {
            if (emitUTF8Identifier)
            {
                return new byte[3]{0xEF, 0xBB, 0xBF};
            }
            else
                return EmptyArray<Byte>.Value;
        }

        public override bool Equals(Object value)
        {
            UTF8Encoding that = value as UTF8Encoding;
            if (that != null)
            {
                return (emitUTF8Identifier == that.emitUTF8Identifier) && (EncoderFallback.Equals(that.EncoderFallback)) && (DecoderFallback.Equals(that.DecoderFallback));
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode() + UTF8_CODEPAGE + (emitUTF8Identifier ? 1 : 0);
        }

        internal class UTF8Encoder : EncoderNLS, ISerializable
        {
            internal int surrogateChar;
            public UTF8Encoder(UTF8Encoding encoding): base (encoding)
            {
            }

            internal UTF8Encoder(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");
                Contract.EndContractBlock();
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof (Encoding));
                this.surrogateChar = (int)info.GetValue("surrogateChar", typeof (int));
                try
                {
                    this.m_fallback = (EncoderFallback)info.GetValue("m_fallback", typeof (EncoderFallback));
                }
                catch (SerializationException)
                {
                    this.m_fallback = null;
                }
            }

            public override void Reset()
            {
                this.surrogateChar = 0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            internal override bool HasState
            {
                get
                {
                    return (this.surrogateChar != 0);
                }
            }
        }

        internal class UTF8Decoder : DecoderNLS, ISerializable
        {
            internal int bits;
            public UTF8Decoder(UTF8Encoding encoding): base (encoding)
            {
            }

            internal UTF8Decoder(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");
                Contract.EndContractBlock();
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof (Encoding));
                try
                {
                    this.bits = (int)info.GetValue("wbits", typeof (int));
                    this.m_fallback = (DecoderFallback)info.GetValue("m_fallback", typeof (DecoderFallback));
                }
                catch (SerializationException)
                {
                    this.bits = 0;
                    this.m_fallback = null;
                }
            }

            public override void Reset()
            {
                this.bits = 0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            internal override bool HasState
            {
                get
                {
                    return (this.bits != 0);
                }
            }
        }
    }
}