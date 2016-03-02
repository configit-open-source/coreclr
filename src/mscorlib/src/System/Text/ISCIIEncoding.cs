
using System.Runtime.Serialization;

namespace System.Text
{
    internal class ISCIIEncoding : EncodingNLS, ISerializable
    {
        private const int CodeDefault = 0;
        private const int CodeRoman = 1;
        private const int CodeDevanagari = 2;
        private const int CodeBengali = 3;
        private const int CodeTamil = 4;
        private const int CodeTelugu = 5;
        private const int CodeAssamese = 6;
        private const int CodeOriya = 7;
        private const int CodeKannada = 8;
        private const int CodeMalayalam = 9;
        private const int CodeGujarati = 10;
        private const int CodePunjabi = 11;
        private const int MultiByteBegin = 0xa0;
        private const int IndicBegin = 0x0901;
        private const int IndicEnd = 0x0d6f;
        private const byte ControlATR = 0xef;
        private const byte ControlCodePageStart = 0x40;
        private const byte Virama = 0xe8;
        private const byte Nukta = 0xe9;
        private const byte DevenagariExt = 0xf0;
        private const char ZWNJ = (char)0x200c;
        private const char ZWJ = (char)0x200d;
        private int defaultCodePage;
        public ISCIIEncoding(int codePage): base (codePage)
        {
            defaultCodePage = codePage - 57000;
                        if (defaultCodePage < CodeDevanagari || defaultCodePage > CodePunjabi)
                throw new ArgumentException(Environment.GetResourceString("Argument_CodepageNotSupported", codePage), "codePage");
        }

        internal ISCIIEncoding(SerializationInfo info, StreamingContext context): base (0)
        {
                        throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException("charCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
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
                        long charCount = ((long)byteCount + 1);
            if (DecoderFallback.MaxCharCount > 1)
                charCount *= DecoderFallback.MaxCharCount;
            if (charCount > 0x7fffffff)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_GetCharCountOverflow"));
            return (int)charCount;
        }

        internal override unsafe int GetByteCount(char *chars, int count, EncoderNLS baseEncoder)
        {
            return GetBytes(chars, count, null, 0, baseEncoder);
        }

        internal override unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, EncoderNLS baseEncoder)
        {
                                                ISCIIEncoder encoder = (ISCIIEncoder)baseEncoder;
            Encoding.EncodingByteBuffer buffer = new Encoding.EncodingByteBuffer(this, encoder, bytes, byteCount, chars, charCount);
            int currentCodePage = this.defaultCodePage;
            bool bLastVirama = false;
            if (encoder != null)
            {
                currentCodePage = encoder.currentCodePage;
                bLastVirama = encoder.bLastVirama;
                if (encoder.charLeftOver > 0)
                {
                    buffer.Fallback(encoder.charLeftOver);
                    bLastVirama = false;
                }
            }

            while (buffer.MoreData)
            {
                char ch = buffer.GetNextChar();
                if (ch < MultiByteBegin)
                {
                    if (!buffer.AddByte((byte)ch))
                        break;
                    bLastVirama = false;
                    continue;
                }

                if ((ch < IndicBegin) || (ch > IndicEnd))
                {
                    if (bLastVirama && (ch == ZWNJ || ch == ZWJ))
                    {
                        if (ch == ZWNJ)
                        {
                            if (!buffer.AddByte(Virama))
                                break;
                        }
                        else
                        {
                            if (!buffer.AddByte(Nukta))
                                break;
                        }

                        bLastVirama = false;
                        continue;
                    }

                    buffer.Fallback(ch);
                    bLastVirama = false;
                    continue;
                }

                int indicInfo = UnicodeToIndicChar[ch - IndicBegin];
                byte byteIndic = (byte)indicInfo;
                int indicScript = (0x000f & (indicInfo >> 8));
                int indicTwoBytes = (0xf000 & indicInfo);
                if (indicInfo == 0)
                {
                    buffer.Fallback(ch);
                    bLastVirama = false;
                    continue;
                }

                                if (indicScript != currentCodePage)
                {
                    if (!buffer.AddByte(ControlATR, (byte)(indicScript | ControlCodePageStart)))
                        break;
                    currentCodePage = indicScript;
                                    }

                if (!buffer.AddByte(byteIndic, indicTwoBytes != 0 ? 1 : 0))
                    break;
                bLastVirama = (byteIndic == Virama);
                if (indicTwoBytes != 0)
                {
                                        if (!buffer.AddByte(SecondIndicByte[indicTwoBytes >> 12]))
                        break;
                }
            }

            if (currentCodePage != defaultCodePage && (encoder == null || encoder.MustFlush))
            {
                if (buffer.AddByte(ControlATR, (byte)(defaultCodePage | ControlCodePageStart)))
                    currentCodePage = defaultCodePage;
                else
                    buffer.GetNextChar();
                bLastVirama = false;
            }

            if (encoder != null && bytes != null)
            {
                if (!buffer.fallbackBuffer.bUsedEncoder)
                {
                    encoder.charLeftOver = (char)0;
                }

                encoder.currentCodePage = currentCodePage;
                encoder.bLastVirama = bLastVirama;
                encoder.m_charsUsed = buffer.CharsUsed;
            }

            return buffer.Count;
        }

        internal override unsafe int GetCharCount(byte *bytes, int count, DecoderNLS baseDecoder)
        {
            return GetChars(bytes, count, null, 0, baseDecoder);
        }

        internal override unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, DecoderNLS baseDecoder)
        {
                                                ISCIIDecoder decoder = (ISCIIDecoder)baseDecoder;
            Encoding.EncodingCharBuffer buffer = new Encoding.EncodingCharBuffer(this, decoder, chars, charCount, bytes, byteCount);
            int currentCodePage = this.defaultCodePage;
            bool bLastATR = false;
            bool bLastVirama = false;
            bool bLastDevenagariStressAbbr = false;
            char cLastCharForNextNukta = '\0';
            char cLastCharForNoNextNukta = '\0';
            if (decoder != null)
            {
                currentCodePage = decoder.currentCodePage;
                bLastATR = decoder.bLastATR;
                bLastVirama = decoder.bLastVirama;
                bLastDevenagariStressAbbr = decoder.bLastDevenagariStressAbbr;
                cLastCharForNextNukta = decoder.cLastCharForNextNukta;
                cLastCharForNoNextNukta = decoder.cLastCharForNoNextNukta;
            }

            bool bLastSpecial = bLastVirama | bLastATR | bLastDevenagariStressAbbr | (cLastCharForNextNukta != '\0');
            int currentCodePageIndex = -1;
                        if (currentCodePage >= CodeDevanagari && currentCodePage <= CodePunjabi)
            {
                currentCodePageIndex = IndicMappingIndex[currentCodePage];
            }

            while (buffer.MoreData)
            {
                byte b = buffer.GetNextByte();
                if (bLastSpecial)
                {
                    bLastSpecial = false;
                                        if (bLastATR)
                    {
                        if (b >= (0x40 | CodeDevanagari) && b <= (0x40 | CodePunjabi))
                        {
                            currentCodePage = b & 0xf;
                            currentCodePageIndex = IndicMappingIndex[currentCodePage];
                            bLastATR = false;
                            continue;
                        }

                        if (b == 0x40)
                        {
                            currentCodePage = this.defaultCodePage;
                            currentCodePageIndex = -1;
                            if (currentCodePage >= CodeDevanagari && currentCodePage <= CodePunjabi)
                            {
                                currentCodePageIndex = IndicMappingIndex[currentCodePage];
                            }

                            bLastATR = false;
                            continue;
                        }

                        if (b == 0x41)
                        {
                            currentCodePage = this.defaultCodePage;
                            currentCodePageIndex = -1;
                            if (currentCodePage >= CodeDevanagari && currentCodePage <= CodePunjabi)
                            {
                                currentCodePageIndex = IndicMappingIndex[currentCodePage];
                            }

                            bLastATR = false;
                            continue;
                        }

                        if (!buffer.Fallback(ControlATR))
                            break;
                        bLastATR = false;
                                                                                                                    }
                    else if (bLastVirama)
                    {
                        if (b == Virama)
                        {
                            if (!buffer.AddChar(ZWNJ))
                                break;
                            bLastVirama = false;
                            continue;
                        }

                        if (b == Nukta)
                        {
                            if (!buffer.AddChar(ZWJ))
                                break;
                            bLastVirama = false;
                            continue;
                        }

                        bLastVirama = false;
                                                                                                                    }
                    else if (bLastDevenagariStressAbbr)
                    {
                        if (b == 0xb8)
                        {
                            if (!buffer.AddChar('\x0952'))
                                break;
                            bLastDevenagariStressAbbr = false;
                            continue;
                        }

                        if (b == 0xbf)
                        {
                            if (!buffer.AddChar('\x0970'))
                                break;
                            bLastDevenagariStressAbbr = false;
                            continue;
                        }

                        if (!buffer.Fallback(DevenagariExt))
                            break;
                        bLastDevenagariStressAbbr = false;
                                                                                                                    }
                    else
                    {
                                                if (b == Nukta)
                        {
                            if (!buffer.AddChar(cLastCharForNextNukta))
                                break;
                            cLastCharForNextNukta = cLastCharForNoNextNukta = '\0';
                            continue;
                        }

                        if (!buffer.AddChar(cLastCharForNoNextNukta))
                            break;
                        cLastCharForNextNukta = cLastCharForNoNextNukta = '\0';
                                                                                            }
                }

                                if (b < MultiByteBegin)
                {
                    if (!buffer.AddChar((char)b))
                        break;
                    continue;
                }

                if (b == ControlATR)
                {
                    bLastATR = bLastSpecial = true;
                    continue;
                }

                                char ch = IndicMapping[currentCodePageIndex, 0, b - MultiByteBegin];
                char cAlt = IndicMapping[currentCodePageIndex, 1, b - MultiByteBegin];
                if (cAlt == 0 || b == Nukta)
                {
                    if (ch == 0)
                    {
                        if (!buffer.Fallback(b))
                            break;
                    }
                    else
                    {
                        if (!buffer.AddChar(ch))
                            break;
                    }

                    continue;
                }

                if (b == Virama)
                {
                    if (!buffer.AddChar(ch))
                        break;
                    bLastVirama = bLastSpecial = true;
                    continue;
                }

                if ((cAlt & 0xF000) == 0)
                {
                    bLastSpecial = true;
                    cLastCharForNextNukta = cAlt;
                    cLastCharForNoNextNukta = ch;
                    continue;
                }

                                bLastDevenagariStressAbbr = bLastSpecial = true;
            }

            if (decoder == null || decoder.MustFlush)
            {
                if (bLastATR)
                {
                    if (buffer.Fallback(ControlATR))
                        bLastATR = false;
                    else
                        buffer.GetNextByte();
                }
                else if (bLastDevenagariStressAbbr)
                {
                    if (buffer.Fallback(DevenagariExt))
                        bLastDevenagariStressAbbr = false;
                    else
                        buffer.GetNextByte();
                }
                else if (cLastCharForNoNextNukta != '\0')
                {
                    if (buffer.AddChar(cLastCharForNoNextNukta))
                        cLastCharForNoNextNukta = cLastCharForNextNukta = '\0';
                    else
                        buffer.GetNextByte();
                }
            }

            if (decoder != null && chars != null)
            {
                if (!decoder.MustFlush || cLastCharForNoNextNukta != '\0' || bLastATR || bLastDevenagariStressAbbr)
                {
                                        decoder.currentCodePage = currentCodePage;
                    decoder.bLastVirama = bLastVirama;
                    decoder.bLastATR = bLastATR;
                    decoder.bLastDevenagariStressAbbr = bLastDevenagariStressAbbr;
                    decoder.cLastCharForNextNukta = cLastCharForNextNukta;
                    decoder.cLastCharForNoNextNukta = cLastCharForNoNextNukta;
                }
                else
                {
                    decoder.currentCodePage = this.defaultCodePage;
                    decoder.bLastVirama = false;
                    decoder.bLastATR = false;
                    decoder.bLastDevenagariStressAbbr = false;
                    decoder.cLastCharForNextNukta = '\0';
                    decoder.cLastCharForNoNextNukta = '\0';
                }

                decoder.m_bytesUsed = buffer.BytesUsed;
            }

            return buffer.Count;
        }

        public override Decoder GetDecoder()
        {
            return new ISCIIDecoder(this);
        }

        public override Encoder GetEncoder()
        {
            return new ISCIIEncoder(this);
        }

        public override int GetHashCode()
        {
            return defaultCodePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode();
        }

        internal class ISCIIEncoder : EncoderNLS
        {
            internal int defaultCodePage = 0;
            internal int currentCodePage = 0;
            internal bool bLastVirama = false;
            public ISCIIEncoder(Encoding encoding): base (encoding)
            {
                this.currentCodePage = this.defaultCodePage = encoding.CodePage - 57000;
            }

            public override void Reset()
            {
                bLastVirama = false;
                charLeftOver = (char)0;
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            internal override bool HasState
            {
                get
                {
                    return (this.charLeftOver != (char)0 || this.currentCodePage != this.defaultCodePage);
                }
            }
        }

        internal class ISCIIDecoder : DecoderNLS
        {
            internal int currentCodePage = 0;
            internal bool bLastATR = false;
            internal bool bLastVirama = false;
            internal bool bLastDevenagariStressAbbr = false;
            internal char cLastCharForNextNukta = '\0';
            internal char cLastCharForNoNextNukta = '\0';
            public ISCIIDecoder(Encoding encoding): base (encoding)
            {
                this.currentCodePage = encoding.CodePage - 57000;
            }

            public override void Reset()
            {
                bLastATR = false;
                bLastVirama = false;
                bLastDevenagariStressAbbr = false;
                cLastCharForNextNukta = '\0';
                cLastCharForNoNextNukta = '\0';
                if (m_fallbackBuffer != null)
                    m_fallbackBuffer.Reset();
            }

            internal override bool HasState
            {
                get
                {
                    return (this.cLastCharForNextNukta != '\0' || this.cLastCharForNoNextNukta != '\0' || this.bLastATR || this.bLastDevenagariStressAbbr);
                }
            }
        }

        static int[] UnicodeToIndicChar = {0x02a1, 0x02a2, 0x02a3, 0x0000, 0x02a4, 0x02a5, 0x02a6, 0x02a7, 0x02a8, 0x02a9, 0x02aa, 0x12a6, 0x02ae, 0x02ab, 0x02ac, 0x02ad, 0x02b2, 0x02af, 0x02b0, 0x02b1, 0x02b3, 0x02b4, 0x02b5, 0x02b6, 0x02b7, 0x02b8, 0x02b9, 0x02ba, 0x02bb, 0x02bc, 0x02bd, 0x02be, 0x02bf, 0x02c0, 0x02c1, 0x02c2, 0x02c3, 0x02c4, 0x02c5, 0x02c6, 0x02c7, 0x02c8, 0x02c9, 0x02ca, 0x02cb, 0x02cc, 0x02cd, 0x02cf, 0x02d0, 0x02d1, 0x02d2, 0x02d3, 0x02d4, 0x02d5, 0x02d6, 0x02d7, 0x02d8, 0x0000, 0x0000, 0x02e9, 0x12ea, 0x02da, 0x02db, 0x02dc, 0x02dd, 0x02de, 0x02df, 0x12df, 0x02e3, 0x02e0, 0x02e1, 0x02e2, 0x02e7, 0x02e4, 0x02e5, 0x02e6, 0x02e8, 0x0000, 0x0000, 0x12a1, 0x0000, 0x22f0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x12b3, 0x12b4, 0x12b5, 0x12ba, 0x12bf, 0x12c0, 0x12c9, 0x02ce, 0x12aa, 0x12a7, 0x12db, 0x12dc, 0x02ea, 0x0000, 0x02f1, 0x02f2, 0x02f3, 0x02f4, 0x02f5, 0x02f6, 0x02f7, 0x02f8, 0x02f9, 0x02fa, 0x32f0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x03a1, 0x03a2, 0x03a3, 0x0000, 0x03a4, 0x03a5, 0x03a6, 0x03a7, 0x03a8, 0x03a9, 0x03aa, 0x13a6, 0x0000, 0x0000, 0x03ab, 0x03ad, 0x0000, 0x0000, 0x03af, 0x03b1, 0x03b3, 0x03b4, 0x03b5, 0x03b6, 0x03b7, 0x03b8, 0x03b9, 0x03ba, 0x03bb, 0x03bc, 0x03bd, 0x03be, 0x03bf, 0x03c0, 0x03c1, 0x03c2, 0x03c3, 0x03c4, 0x03c5, 0x03c6, 0x0000, 0x03c8, 0x03c9, 0x03ca, 0x03cb, 0x03cc, 0x03cd, 0x03cf, 0x0000, 0x03d1, 0x0000, 0x0000, 0x0000, 0x03d5, 0x03d6, 0x03d7, 0x03d8, 0x0000, 0x0000, 0x03e9, 0x0000, 0x03da, 0x03db, 0x03dc, 0x03dd, 0x03de, 0x03df, 0x13df, 0x0000, 0x0000, 0x03e0, 0x03e2, 0x0000, 0x0000, 0x03e4, 0x03e6, 0x03e8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x13bf, 0x13c0, 0x0000, 0x03ce, 0x13aa, 0x13a7, 0x13db, 0x13dc, 0x0000, 0x0000, 0x03f1, 0x03f2, 0x03f3, 0x03f4, 0x03f5, 0x03f6, 0x03f7, 0x03f8, 0x03f9, 0x03fa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0ba2, 0x0000, 0x0000, 0x0ba4, 0x0ba5, 0x0ba6, 0x0ba7, 0x0ba8, 0x0ba9, 0x0000, 0x0000, 0x0000, 0x0000, 0x0bab, 0x0bad, 0x0000, 0x0000, 0x0bb0, 0x0bb1, 0x0bb3, 0x0bb4, 0x0bb5, 0x0bb6, 0x0bb7, 0x0bb8, 0x0bb9, 0x0bba, 0x0bbb, 0x0bbc, 0x0bbd, 0x0bbe, 0x0bbf, 0x0bc0, 0x0bc1, 0x0bc2, 0x0bc3, 0x0bc4, 0x0bc5, 0x0bc6, 0x0000, 0x0bc8, 0x0bc9, 0x0bca, 0x0bcb, 0x0bcc, 0x0bcd, 0x0bcf, 0x0000, 0x0bd1, 0x0bd2, 0x0000, 0x0bd4, 0x0bd5, 0x0000, 0x0bd7, 0x0bd8, 0x0000, 0x0000, 0x0be9, 0x0000, 0x0bda, 0x0bdb, 0x0bdc, 0x0bdd, 0x0bde, 0x0000, 0x0000, 0x0000, 0x0000, 0x0be0, 0x0be2, 0x0000, 0x0000, 0x0be4, 0x0be6, 0x0be8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x1bb4, 0x1bb5, 0x1bba, 0x1bc0, 0x0000, 0x1bc9, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0bf1, 0x0bf2, 0x0bf3, 0x0bf4, 0x0bf5, 0x0bf6, 0x0bf7, 0x0bf8, 0x0bf9, 0x0bfa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0aa1, 0x0aa2, 0x0aa3, 0x0000, 0x0aa4, 0x0aa5, 0x0aa6, 0x0aa7, 0x0aa8, 0x0aa9, 0x0aaa, 0x0000, 0x0aae, 0x0000, 0x0aab, 0x0aad, 0x0ab2, 0x0000, 0x0ab0, 0x0ab1, 0x0ab3, 0x0ab4, 0x0ab5, 0x0ab6, 0x0ab7, 0x0ab8, 0x0ab9, 0x0aba, 0x0abb, 0x0abc, 0x0abd, 0x0abe, 0x0abf, 0x0ac0, 0x0ac1, 0x0ac2, 0x0ac3, 0x0ac4, 0x0ac5, 0x0ac6, 0x0000, 0x0ac8, 0x0ac9, 0x0aca, 0x0acb, 0x0acc, 0x0acd, 0x0acf, 0x0000, 0x0ad1, 0x0ad2, 0x0000, 0x0ad4, 0x0ad5, 0x0ad6, 0x0ad7, 0x0ad8, 0x0000, 0x0000, 0x0ae9, 0x1aea, 0x0ada, 0x0adb, 0x0adc, 0x0add, 0x0ade, 0x0adf, 0x1adf, 0x0ae3, 0x0000, 0x0ae0, 0x0ae2, 0x0ae7, 0x0000, 0x0ae4, 0x0ae6, 0x0ae8, 0x0000, 0x0000, 0x1aa1, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x1aaa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0af1, 0x0af2, 0x0af3, 0x0af4, 0x0af5, 0x0af6, 0x0af7, 0x0af8, 0x0af9, 0x0afa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x07a1, 0x07a2, 0x07a3, 0x0000, 0x07a4, 0x07a5, 0x07a6, 0x07a7, 0x07a8, 0x07a9, 0x07aa, 0x17a6, 0x0000, 0x0000, 0x07ab, 0x07ad, 0x0000, 0x0000, 0x07b0, 0x07b1, 0x07b3, 0x07b4, 0x07b5, 0x07b6, 0x07b7, 0x07b8, 0x07b9, 0x07ba, 0x07bb, 0x07bc, 0x07bd, 0x07be, 0x07bf, 0x07c0, 0x07c1, 0x07c2, 0x07c3, 0x07c4, 0x07c5, 0x07c6, 0x0000, 0x07c8, 0x07c9, 0x07ca, 0x07cb, 0x07cc, 0x07cd, 0x07cf, 0x0000, 0x07d1, 0x07d2, 0x0000, 0x0000, 0x07d5, 0x07d6, 0x07d7, 0x07d8, 0x0000, 0x0000, 0x07e9, 0x17ea, 0x07da, 0x07db, 0x07dc, 0x07dd, 0x07de, 0x07df, 0x0000, 0x0000, 0x0000, 0x07e0, 0x07e2, 0x0000, 0x0000, 0x07e4, 0x07e6, 0x07e8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x17bf, 0x17c0, 0x0000, 0x07ce, 0x17aa, 0x17a7, 0x0000, 0x0000, 0x0000, 0x0000, 0x07f1, 0x07f2, 0x07f3, 0x07f4, 0x07f5, 0x07f6, 0x07f7, 0x07f8, 0x07f9, 0x07fa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x04a2, 0x04a3, 0x0000, 0x04a4, 0x04a5, 0x04a6, 0x04a7, 0x04a8, 0x04a9, 0x0000, 0x0000, 0x0000, 0x0000, 0x04ab, 0x04ad, 0x0000, 0x04af, 0x04b0, 0x04b1, 0x04b3, 0x0000, 0x0000, 0x0000, 0x04b7, 0x04b8, 0x0000, 0x04ba, 0x0000, 0x04bc, 0x04bd, 0x0000, 0x0000, 0x0000, 0x04c1, 0x04c2, 0x0000, 0x0000, 0x0000, 0x04c6, 0x04c7, 0x04c8, 0x0000, 0x0000, 0x0000, 0x04cc, 0x04cd, 0x04cf, 0x04d0, 0x04d1, 0x04d2, 0x04d3, 0x04d4, 0x0000, 0x04d5, 0x04d7, 0x04d8, 0x0000, 0x0000, 0x0000, 0x0000, 0x04da, 0x04db, 0x04dc, 0x04dd, 0x04de, 0x0000, 0x0000, 0x0000, 0x04e0, 0x04e1, 0x04e2, 0x0000, 0x04e4, 0x04e5, 0x04e6, 0x04e8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x04f2, 0x04f3, 0x04f4, 0x04f5, 0x04f6, 0x04f7, 0x04f8, 0x04f9, 0x04fa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x05a1, 0x05a2, 0x05a3, 0x0000, 0x05a4, 0x05a5, 0x05a6, 0x05a7, 0x05a8, 0x05a9, 0x05aa, 0x15a6, 0x0000, 0x05ab, 0x05ac, 0x05ad, 0x0000, 0x05af, 0x05b0, 0x05b1, 0x05b3, 0x05b4, 0x05b5, 0x05b6, 0x05b7, 0x05b8, 0x05b9, 0x05ba, 0x05bb, 0x05bc, 0x05bd, 0x05be, 0x05bf, 0x05c0, 0x05c1, 0x05c2, 0x05c3, 0x05c4, 0x05c5, 0x05c6, 0x0000, 0x05c8, 0x05c9, 0x05ca, 0x05cb, 0x05cc, 0x05cd, 0x05cf, 0x05d0, 0x05d1, 0x05d2, 0x0000, 0x05d4, 0x05d5, 0x05d6, 0x05d7, 0x05d8, 0x0000, 0x0000, 0x0000, 0x0000, 0x05da, 0x05db, 0x05dc, 0x05dd, 0x05de, 0x05df, 0x15df, 0x0000, 0x05e0, 0x05e1, 0x05e2, 0x0000, 0x05e4, 0x05e5, 0x05e6, 0x05e8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x15aa, 0x15a7, 0x0000, 0x0000, 0x0000, 0x0000, 0x05f1, 0x05f2, 0x05f3, 0x05f4, 0x05f5, 0x05f6, 0x05f7, 0x05f8, 0x05f9, 0x05fa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x08a2, 0x08a3, 0x0000, 0x08a4, 0x08a5, 0x08a6, 0x08a7, 0x08a8, 0x08a9, 0x08aa, 0x18a6, 0x0000, 0x08ab, 0x08ac, 0x08ad, 0x0000, 0x08af, 0x08b0, 0x08b1, 0x08b3, 0x08b4, 0x08b5, 0x08b6, 0x08b7, 0x08b8, 0x08b9, 0x08ba, 0x08bb, 0x08bc, 0x08bd, 0x08be, 0x08bf, 0x08c0, 0x08c1, 0x08c2, 0x08c3, 0x08c4, 0x08c5, 0x08c6, 0x0000, 0x08c8, 0x08c9, 0x08ca, 0x08cb, 0x08cc, 0x08cd, 0x08cf, 0x08d0, 0x08d1, 0x08d2, 0x0000, 0x08d4, 0x08d5, 0x08d6, 0x08d7, 0x08d8, 0x0000, 0x0000, 0x0000, 0x0000, 0x08da, 0x08db, 0x08dc, 0x08dd, 0x08de, 0x08df, 0x18df, 0x0000, 0x08e0, 0x08e1, 0x08e2, 0x0000, 0x08e4, 0x08e5, 0x08e6, 0x08e8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x18c9, 0x0000, 0x18aa, 0x18a7, 0x0000, 0x0000, 0x0000, 0x0000, 0x08f1, 0x08f2, 0x08f3, 0x08f4, 0x08f5, 0x08f6, 0x08f7, 0x08f8, 0x08f9, 0x08fa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x09a2, 0x09a3, 0x0000, 0x09a4, 0x09a5, 0x09a6, 0x09a7, 0x09a8, 0x09a9, 0x09aa, 0x19a6, 0x0000, 0x09ab, 0x09ac, 0x09ad, 0x0000, 0x09af, 0x09b0, 0x09b1, 0x09b3, 0x09b4, 0x09b5, 0x09b6, 0x09b7, 0x09b8, 0x09b9, 0x09ba, 0x09bb, 0x09bc, 0x09bd, 0x09be, 0x09bf, 0x09c0, 0x09c1, 0x09c2, 0x09c3, 0x09c4, 0x09c5, 0x09c6, 0x0000, 0x09c8, 0x09c9, 0x09ca, 0x09cb, 0x09cc, 0x09cd, 0x09cf, 0x09d0, 0x09d1, 0x09d2, 0x09d3, 0x09d4, 0x09d5, 0x09d6, 0x09d7, 0x09d8, 0x0000, 0x0000, 0x0000, 0x0000, 0x09da, 0x09db, 0x09dc, 0x09dd, 0x09de, 0x09df, 0x0000, 0x0000, 0x09e0, 0x09e1, 0x09e2, 0x0000, 0x09e4, 0x09e5, 0x09e6, 0x09e8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x19aa, 0x19a7, 0x0000, 0x0000, 0x0000, 0x0000, 0x09f1, 0x09f2, 0x09f3, 0x09f4, 0x09f5, 0x09f6, 0x09f7, 0x09f8, 0x09f9, 0x09fa};
        static byte[] SecondIndicByte = {0x00, 0xe9, 0xb8, 0xbf};
        static int[] IndicMappingIndex = {-1, -1, 0, 1, 2, 3, 1, 4, 5, 6, 7, 8};
        static char[,, ] IndicMapping = {{{'\x0000', '\x0901', '\x0902', '\x0903', '\x0905', '\x0906', '\x0907', '\x0908', '\x0909', '\x090a', '\x090b', '\x090e', '\x090f', '\x0910', '\x090d', '\x0912', '\x0913', '\x0914', '\x0911', '\x0915', '\x0916', '\x0917', '\x0918', '\x0919', '\x091a', '\x091b', '\x091c', '\x091d', '\x091e', '\x091f', '\x0920', '\x0921', '\x0922', '\x0923', '\x0924', '\x0925', '\x0926', '\x0927', '\x0928', '\x0929', '\x092a', '\x092b', '\x092c', '\x092d', '\x092e', '\x092f', '\x095f', '\x0930', '\x0931', '\x0932', '\x0933', '\x0934', '\x0935', '\x0936', '\x0937', '\x0938', '\x0939', '\x0000', '\x093e', '\x093f', '\x0940', '\x0941', '\x0942', '\x0943', '\x0946', '\x0947', '\x0948', '\x0945', '\x094a', '\x094b', '\x094c', '\x0949', '\x094d', '\x093c', '\x0964', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0966', '\x0967', '\x0968', '\x0969', '\x096a', '\x096b', '\x096c', '\x096d', '\x096e', '\x096f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0950', '\x0', '\x0', '\x0', '\x0', '\x090c', '\x0961', '\x0', '\x0', '\x0960', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0958', '\x0959', '\x095a', '\x0', '\x0', '\x0', '\x0', '\x095b', '\x0', '\x0', '\x0', '\x0', '\x095c', '\x095d', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x095e', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0962', '\x0963', '\x0', '\x0', '\x0944', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x093d', '\x0', '\x0', '\x0', '\x0', '\x0', '\xB8BF', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}, {{'\x0000', '\x0981', '\x0982', '\x0983', '\x0985', '\x0986', '\x0987', '\x0988', '\x0989', '\x098a', '\x098b', '\x098f', '\x098f', '\x0990', '\x0990', '\x0993', '\x0993', '\x0994', '\x0994', '\x0995', '\x0996', '\x0997', '\x0998', '\x0999', '\x099a', '\x099b', '\x099c', '\x099d', '\x099e', '\x099f', '\x09a0', '\x09a1', '\x09a2', '\x09a3', '\x09a4', '\x09a5', '\x09a6', '\x09a7', '\x09a8', '\x09a8', '\x09aa', '\x09ab', '\x09ac', '\x09ad', '\x09ae', '\x09af', '\x09df', '\x09b0', '\x09b0', '\x09b2', '\x09b2', '\x09b2', '\x09ac', '\x09b6', '\x09b7', '\x09b8', '\x09b9', '\x0000', '\x09be', '\x09bf', '\x09c0', '\x09c1', '\x09c2', '\x09c3', '\x09c7', '\x09c7', '\x09c8', '\x09c8', '\x09cb', '\x09cb', '\x09cc', '\x09cc', '\x09cd', '\x09bc', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x09e6', '\x09e7', '\x09e8', '\x09e9', '\x09ea', '\x09eb', '\x09ec', '\x09ed', '\x09ee', '\x09ef', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x098c', '\x09e1', '\x0', '\x0', '\x09e0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x09dc', '\x09dd', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x09e2', '\x09e3', '\x0', '\x0', '\x09c4', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}, {{'\x0000', '\x0000', '\x0b82', '\x0b83', '\x0b85', '\x0b86', '\x0b87', '\x0b88', '\x0b89', '\x0b8a', '\x0000', '\x0b8f', '\x0b8f', '\x0b90', '\x0b90', '\x0b92', '\x0b93', '\x0b94', '\x0b94', '\x0b95', '\x0b95', '\x0b95', '\x0b95', '\x0b99', '\x0b9a', '\x0b9a', '\x0b9c', '\x0b9c', '\x0b9e', '\x0b9f', '\x0b9f', '\x0b9f', '\x0b9f', '\x0ba3', '\x0ba4', '\x0ba4', '\x0ba4', '\x0ba4', '\x0ba8', '\x0ba9', '\x0baa', '\x0baa', '\x0baa', '\x0baa', '\x0bae', '\x0baf', '\x0baf', '\x0bb0', '\x0bb1', '\x0bb2', '\x0bb3', '\x0bb4', '\x0bb5', '\x0bb7', '\x0bb7', '\x0bb8', '\x0bb9', '\x0000', '\x0bbe', '\x0bbf', '\x0bc0', '\x0bc1', '\x0bc2', '\x0000', '\x0bc6', '\x0bc7', '\x0bc8', '\x0bc8', '\x0bca', '\x0bcb', '\x0bcc', '\x0bcc', '\x0bcd', '\x0000', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0030', '\x0be7', '\x0be8', '\x0be9', '\x0bea', '\x0beb', '\x0bec', '\x0bed', '\x0bee', '\x0bef', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}, {{'\x0000', '\x0c01', '\x0c02', '\x0c03', '\x0c05', '\x0c06', '\x0c07', '\x0c08', '\x0c09', '\x0c0a', '\x0c0b', '\x0c0e', '\x0c0f', '\x0c10', '\x0c10', '\x0c12', '\x0c13', '\x0c14', '\x0c14', '\x0c15', '\x0c16', '\x0c17', '\x0c18', '\x0c19', '\x0c1a', '\x0c1b', '\x0c1c', '\x0c1d', '\x0c1e', '\x0c1f', '\x0c20', '\x0c21', '\x0c22', '\x0c23', '\x0c24', '\x0c25', '\x0c26', '\x0c27', '\x0c28', '\x0c28', '\x0c2a', '\x0c2b', '\x0c2c', '\x0c2d', '\x0c2e', '\x0c2f', '\x0c2f', '\x0c30', '\x0c31', '\x0c32', '\x0c33', '\x0c33', '\x0c35', '\x0c36', '\x0c37', '\x0c38', '\x0c39', '\x0000', '\x0c3e', '\x0c3f', '\x0c40', '\x0c41', '\x0c42', '\x0c43', '\x0c46', '\x0c47', '\x0c48', '\x0c48', '\x0c4a', '\x0c4b', '\x0c4c', '\x0c4c', '\x0c4d', '\x0000', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0c66', '\x0c67', '\x0c68', '\x0c69', '\x0c6a', '\x0c6b', '\x0c6c', '\x0c6d', '\x0c6e', '\x0c6f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0c0c', '\x0c61', '\x0', '\x0', '\x0c60', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0c44', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}, {{'\x0000', '\x0b01', '\x0b02', '\x0b03', '\x0b05', '\x0b06', '\x0b07', '\x0b08', '\x0b09', '\x0b0a', '\x0b0b', '\x0b0f', '\x0b0f', '\x0b10', '\x0b10', '\x0b10', '\x0b13', '\x0b14', '\x0b14', '\x0b15', '\x0b16', '\x0b17', '\x0b18', '\x0b19', '\x0b1a', '\x0b1b', '\x0b1c', '\x0b1d', '\x0b1e', '\x0b1f', '\x0b20', '\x0b21', '\x0b22', '\x0b23', '\x0b24', '\x0b25', '\x0b26', '\x0b27', '\x0b28', '\x0b28', '\x0b2a', '\x0b2b', '\x0b2c', '\x0b2d', '\x0b2e', '\x0b2f', '\x0b5f', '\x0b30', '\x0b30', '\x0b32', '\x0b33', '\x0b33', '\x0b2c', '\x0b36', '\x0b37', '\x0b38', '\x0b39', '\x0000', '\x0b3e', '\x0b3f', '\x0b40', '\x0b41', '\x0b42', '\x0b43', '\x0b47', '\x0b47', '\x0b48', '\x0b48', '\x0b4b', '\x0b4b', '\x0b4c', '\x0b4c', '\x0b4d', '\x0b3c', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0b66', '\x0b67', '\x0b68', '\x0b69', '\x0b6a', '\x0b6b', '\x0b6c', '\x0b6d', '\x0b6e', '\x0b6f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0c0c', '\x0c61', '\x0', '\x0', '\x0c60', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0b5c', '\x0b5d', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0c44', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x0b3d', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}, {{'\x0000', '\x0000', '\x0c82', '\x0c83', '\x0c85', '\x0c86', '\x0c87', '\x0c88', '\x0c89', '\x0c8a', '\x0c8b', '\x0c8e', '\x0c8f', '\x0c90', '\x0c90', '\x0c92', '\x0c93', '\x0c94', '\x0c94', '\x0c95', '\x0c96', '\x0c97', '\x0c98', '\x0c99', '\x0c9a', '\x0c9b', '\x0c9c', '\x0c9d', '\x0c9e', '\x0c9f', '\x0ca0', '\x0ca1', '\x0ca2', '\x0ca3', '\x0ca4', '\x0ca5', '\x0ca6', '\x0ca7', '\x0ca8', '\x0ca8', '\x0caa', '\x0cab', '\x0cac', '\x0cad', '\x0cae', '\x0caf', '\x0caf', '\x0cb0', '\x0cb1', '\x0cb2', '\x0cb3', '\x0cb3', '\x0cb5', '\x0cb6', '\x0cb7', '\x0cb8', '\x0cb9', '\x0000', '\x0cbe', '\x0cbf', '\x0cc0', '\x0cc1', '\x0cc2', '\x0cc3', '\x0cc6', '\x0cc7', '\x0cc8', '\x0cc8', '\x0cca', '\x0ccb', '\x0ccc', '\x0ccc', '\x0ccd', '\x0000', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0ce6', '\x0ce7', '\x0ce8', '\x0ce9', '\x0cea', '\x0ceb', '\x0cec', '\x0ced', '\x0cee', '\x0cef', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0c8c', '\x0ce1', '\x0', '\x0', '\x0ce0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0cde', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0cc4', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}, {{'\x0000', '\x0000', '\x0d02', '\x0d03', '\x0d05', '\x0d06', '\x0d07', '\x0d08', '\x0d09', '\x0d0a', '\x0d0b', '\x0d0e', '\x0d0f', '\x0d10', '\x0d10', '\x0d12', '\x0d13', '\x0d14', '\x0d14', '\x0d15', '\x0d16', '\x0d17', '\x0d18', '\x0d19', '\x0d1a', '\x0d1b', '\x0d1c', '\x0d1d', '\x0d1e', '\x0d1f', '\x0d20', '\x0d21', '\x0d22', '\x0d23', '\x0d24', '\x0d25', '\x0d26', '\x0d27', '\x0d28', '\x0d28', '\x0d2a', '\x0d2b', '\x0d2c', '\x0d2d', '\x0d2e', '\x0d2f', '\x0d2f', '\x0d30', '\x0d31', '\x0d32', '\x0d33', '\x0d34', '\x0d35', '\x0d36', '\x0d37', '\x0d38', '\x0d39', '\x0000', '\x0d3e', '\x0d3f', '\x0d40', '\x0d41', '\x0d42', '\x0d43', '\x0d46', '\x0d47', '\x0d48', '\x0d48', '\x0d4a', '\x0d4b', '\x0d4c', '\x0d4c', '\x0d4d', '\x0000', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0d66', '\x0d67', '\x0d68', '\x0d69', '\x0d6a', '\x0d6b', '\x0d6c', '\x0d6d', '\x0d6e', '\x0d6f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0d0c', '\x0d61', '\x0', '\x0', '\x0d60', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}, {{'\x0000', '\x0a81', '\x0a82', '\x0a83', '\x0a85', '\x0a86', '\x0a87', '\x0a88', '\x0a89', '\x0a8a', '\x0a8b', '\x0a8f', '\x0a8f', '\x0a90', '\x0a8d', '\x0a8d', '\x0a93', '\x0a94', '\x0a91', '\x0a95', '\x0a96', '\x0a97', '\x0a98', '\x0a99', '\x0a9a', '\x0a9b', '\x0a9c', '\x0a9d', '\x0a9e', '\x0a9f', '\x0aa0', '\x0aa1', '\x0aa2', '\x0aa3', '\x0aa4', '\x0aa5', '\x0aa6', '\x0aa7', '\x0aa8', '\x0aa8', '\x0aaa', '\x0aab', '\x0aac', '\x0aad', '\x0aae', '\x0aaf', '\x0aaf', '\x0ab0', '\x0ab0', '\x0ab2', '\x0ab3', '\x0ab3', '\x0ab5', '\x0ab6', '\x0ab7', '\x0ab8', '\x0ab9', '\x0000', '\x0abe', '\x0abf', '\x0ac0', '\x0ac1', '\x0ac2', '\x0ac3', '\x0ac7', '\x0ac7', '\x0ac8', '\x0ac5', '\x0acb', '\x0acb', '\x0acc', '\x0ac9', '\x0acd', '\x0abc', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0ae6', '\x0ae7', '\x0ae8', '\x0ae9', '\x0aea', '\x0aeb', '\x0aec', '\x0aed', '\x0aee', '\x0aef', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0ad0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0ae0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0ac4', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x0abd', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}, {{'\x0000', '\x0000', '\x0a02', '\x0000', '\x0a05', '\x0a06', '\x0a07', '\x0a08', '\x0a09', '\x0a0a', '\x0000', '\x0a0f', '\x0a0f', '\x0a10', '\x0a10', '\x0a10', '\x0a13', '\x0a14', '\x0a14', '\x0a15', '\x0a16', '\x0a17', '\x0a18', '\x0a19', '\x0a1a', '\x0a1b', '\x0a1c', '\x0a1d', '\x0a1e', '\x0a1f', '\x0a20', '\x0a21', '\x0a22', '\x0a23', '\x0a24', '\x0a25', '\x0a26', '\x0a27', '\x0a28', '\x0a28', '\x0a2a', '\x0a2b', '\x0a2c', '\x0a2d', '\x0a2e', '\x0a2f', '\x0a2f', '\x0a30', '\x0a30', '\x0a32', '\x0a33', '\x0a33', '\x0a35', '\x0a36', '\x0a36', '\x0a38', '\x0a39', '\x0000', '\x0a3e', '\x0a3f', '\x0a40', '\x0a41', '\x0a42', '\x0000', '\x0a47', '\x0a47', '\x0a48', '\x0a48', '\x0a4b', '\x0a4b', '\x0a4c', '\x0a4c', '\x0a4d', '\x0a3c', '\x002e', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000', '\x0a66', '\x0a67', '\x0a68', '\x0a69', '\x0a6a', '\x0a6b', '\x0a6c', '\x0a6d', '\x0a6e', '\x0a6f', '\x0000', '\x0000', '\x0000', '\x0000', '\x0000'}, {'\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0a59', '\x0a5a', '\x0', '\x0', '\x0', '\x0', '\x0a5b', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0a5c', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0a5e', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x200C', '\x200D', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0', '\x0'}}};
    }
}