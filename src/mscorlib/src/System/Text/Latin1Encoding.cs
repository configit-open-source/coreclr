
using System.Runtime.Serialization;

namespace System.Text
{
    internal class Latin1Encoding : EncodingNLS, ISerializable
    {
        public Latin1Encoding(): base (Encoding.ISO_8859_1)
        {
        }

        internal Latin1Encoding(SerializationInfo info, StreamingContext context): base (Encoding.ISO_8859_1)
        {
            DeserializeEncoding(info, context);
        }

        internal override unsafe int GetByteCount(char *chars, int charCount, EncoderNLS encoder)
        {
                                                char charLeftOver = (char)0;
            EncoderReplacementFallback fallback;
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                                fallback = encoder.Fallback as EncoderReplacementFallback;
                            }
            else
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            if ((fallback != null && fallback.MaxCharCount == 1))
            {
                if (charLeftOver > 0)
                    charCount++;
                return (charCount);
            }

            int byteCount = 0;
            char *charEnd = chars + charCount;
            EncoderFallbackBuffer fallbackBuffer = null;
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

                if (ch > 0xff)
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
            if (encoder != null)
            {
                charLeftOver = encoder.charLeftOver;
                fallback = encoder.Fallback as EncoderReplacementFallback;
                                            }
            else
            {
                fallback = this.EncoderFallback as EncoderReplacementFallback;
            }

            char *charEnd = chars + charCount;
            byte *byteStart = bytes;
            char *charStart = chars;
            if (fallback != null && fallback.MaxCharCount == 1)
            {
                char cReplacement = fallback.DefaultString[0];
                if (cReplacement <= (char)0xff)
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
                        if (ch2 > 0x00ff)
                            *(bytes++) = (byte)cReplacement;
                        else
                            *(bytes++) = (byte)ch2;
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
            EncoderFallbackBuffer fallbackBuffer = null;
            if (charLeftOver > 0)
            {
                                fallbackBuffer = encoder.FallbackBuffer;
                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, true);
                fallbackBuffer.InternalFallback(charLeftOver, ref chars);
                if (fallbackBuffer.Remaining > byteEnd - bytes)
                {
                    ThrowBytesOverflow(encoder, true);
                }
            }

            char ch;
            while ((ch = (fallbackBuffer == null) ? '\0' : fallbackBuffer.InternalGetNextChar()) != 0 || chars < charEnd)
            {
                if (ch == 0)
                {
                    ch = *chars;
                    chars++;
                }

                if (ch > 0xff)
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
                    if (fallbackBuffer.Remaining > byteEnd - bytes)
                    {
                                                chars--;
                        fallbackBuffer.InternalReset();
                        ThrowBytesOverflow(encoder, chars == charStart);
                        break;
                    }

                    continue;
                }

                if (bytes >= byteEnd)
                {
                                        if (fallbackBuffer == null || fallbackBuffer.bFallingBack == false)
                    {
                                                chars--;
                    }

                    ThrowBytesOverflow(encoder, chars == charStart);
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
                                    return count;
        }

        internal override unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, DecoderNLS decoder)
        {
                                                            if (charCount < byteCount)
            {
                ThrowCharsOverflow(decoder, charCount < 1);
                byteCount = charCount;
            }

            byte *byteEnd = bytes + byteCount;
            while (bytes < byteEnd)
            {
                *(chars) = unchecked ((char)*(bytes));
                chars++;
                bytes++;
            }

            if (decoder != null)
                decoder.m_bytesUsed = byteCount;
            return byteCount;
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

        internal override char[] GetBestFitUnicodeToBytesData()
        {
            return Latin1Encoding.arrayCharBestFit;
        }

        private static readonly char[] arrayCharBestFit = {(char)0x0100, (char)0x0041, (char)0x0101, (char)0x0061, (char)0x0102, (char)0x0041, (char)0x0103, (char)0x0061, (char)0x0104, (char)0x0041, (char)0x0105, (char)0x0061, (char)0x0106, (char)0x0043, (char)0x0107, (char)0x0063, (char)0x0108, (char)0x0043, (char)0x0109, (char)0x0063, (char)0x010a, (char)0x0043, (char)0x010b, (char)0x0063, (char)0x010c, (char)0x0043, (char)0x010d, (char)0x0063, (char)0x010e, (char)0x0044, (char)0x010f, (char)0x0064, (char)0x0110, (char)0x0044, (char)0x0111, (char)0x0064, (char)0x0112, (char)0x0045, (char)0x0113, (char)0x0065, (char)0x0114, (char)0x0045, (char)0x0115, (char)0x0065, (char)0x0116, (char)0x0045, (char)0x0117, (char)0x0065, (char)0x0118, (char)0x0045, (char)0x0119, (char)0x0065, (char)0x011a, (char)0x0045, (char)0x011b, (char)0x0065, (char)0x011c, (char)0x0047, (char)0x011d, (char)0x0067, (char)0x011e, (char)0x0047, (char)0x011f, (char)0x0067, (char)0x0120, (char)0x0047, (char)0x0121, (char)0x0067, (char)0x0122, (char)0x0047, (char)0x0123, (char)0x0067, (char)0x0124, (char)0x0048, (char)0x0125, (char)0x0068, (char)0x0126, (char)0x0048, (char)0x0127, (char)0x0068, (char)0x0128, (char)0x0049, (char)0x0129, (char)0x0069, (char)0x012a, (char)0x0049, (char)0x012b, (char)0x0069, (char)0x012c, (char)0x0049, (char)0x012d, (char)0x0069, (char)0x012e, (char)0x0049, (char)0x012f, (char)0x0069, (char)0x0130, (char)0x0049, (char)0x0131, (char)0x0069, (char)0x0134, (char)0x004a, (char)0x0135, (char)0x006a, (char)0x0136, (char)0x004b, (char)0x0137, (char)0x006b, (char)0x0139, (char)0x004c, (char)0x013a, (char)0x006c, (char)0x013b, (char)0x004c, (char)0x013c, (char)0x006c, (char)0x013d, (char)0x004c, (char)0x013e, (char)0x006c, (char)0x0141, (char)0x004c, (char)0x0142, (char)0x006c, (char)0x0143, (char)0x004e, (char)0x0144, (char)0x006e, (char)0x0145, (char)0x004e, (char)0x0146, (char)0x006e, (char)0x0147, (char)0x004e, (char)0x0148, (char)0x006e, (char)0x014c, (char)0x004f, (char)0x014d, (char)0x006f, (char)0x014e, (char)0x004f, (char)0x014f, (char)0x006f, (char)0x0150, (char)0x004f, (char)0x0151, (char)0x006f, (char)0x0152, (char)0x004f, (char)0x0153, (char)0x006f, (char)0x0154, (char)0x0052, (char)0x0155, (char)0x0072, (char)0x0156, (char)0x0052, (char)0x0157, (char)0x0072, (char)0x0158, (char)0x0052, (char)0x0159, (char)0x0072, (char)0x015a, (char)0x0053, (char)0x015b, (char)0x0073, (char)0x015c, (char)0x0053, (char)0x015d, (char)0x0073, (char)0x015e, (char)0x0053, (char)0x015f, (char)0x0073, (char)0x0160, (char)0x0053, (char)0x0161, (char)0x0073, (char)0x0162, (char)0x0054, (char)0x0163, (char)0x0074, (char)0x0164, (char)0x0054, (char)0x0165, (char)0x0074, (char)0x0166, (char)0x0054, (char)0x0167, (char)0x0074, (char)0x0168, (char)0x0055, (char)0x0169, (char)0x0075, (char)0x016a, (char)0x0055, (char)0x016b, (char)0x0075, (char)0x016c, (char)0x0055, (char)0x016d, (char)0x0075, (char)0x016e, (char)0x0055, (char)0x016f, (char)0x0075, (char)0x0170, (char)0x0055, (char)0x0171, (char)0x0075, (char)0x0172, (char)0x0055, (char)0x0173, (char)0x0075, (char)0x0174, (char)0x0057, (char)0x0175, (char)0x0077, (char)0x0176, (char)0x0059, (char)0x0177, (char)0x0079, (char)0x0178, (char)0x0059, (char)0x0179, (char)0x005a, (char)0x017a, (char)0x007a, (char)0x017b, (char)0x005a, (char)0x017c, (char)0x007a, (char)0x017d, (char)0x005a, (char)0x017e, (char)0x007a, (char)0x0180, (char)0x0062, (char)0x0189, (char)0x0044, (char)0x0191, (char)0x0046, (char)0x0192, (char)0x0066, (char)0x0197, (char)0x0049, (char)0x019a, (char)0x006c, (char)0x019f, (char)0x004f, (char)0x01a0, (char)0x004f, (char)0x01a1, (char)0x006f, (char)0x01ab, (char)0x0074, (char)0x01ae, (char)0x0054, (char)0x01af, (char)0x0055, (char)0x01b0, (char)0x0075, (char)0x01b6, (char)0x007a, (char)0x01cd, (char)0x0041, (char)0x01ce, (char)0x0061, (char)0x01cf, (char)0x0049, (char)0x01d0, (char)0x0069, (char)0x01d1, (char)0x004f, (char)0x01d2, (char)0x006f, (char)0x01d3, (char)0x0055, (char)0x01d4, (char)0x0075, (char)0x01d5, (char)0x0055, (char)0x01d6, (char)0x0075, (char)0x01d7, (char)0x0055, (char)0x01d8, (char)0x0075, (char)0x01d9, (char)0x0055, (char)0x01da, (char)0x0075, (char)0x01db, (char)0x0055, (char)0x01dc, (char)0x0075, (char)0x01de, (char)0x0041, (char)0x01df, (char)0x0061, (char)0x01e4, (char)0x0047, (char)0x01e5, (char)0x0067, (char)0x01e6, (char)0x0047, (char)0x01e7, (char)0x0067, (char)0x01e8, (char)0x004b, (char)0x01e9, (char)0x006b, (char)0x01ea, (char)0x004f, (char)0x01eb, (char)0x006f, (char)0x01ec, (char)0x004f, (char)0x01ed, (char)0x006f, (char)0x01f0, (char)0x006a, (char)0x0261, (char)0x0067, (char)0x02b9, (char)0x0027, (char)0x02ba, (char)0x0022, (char)0x02bc, (char)0x0027, (char)0x02c4, (char)0x005e, (char)0x02c6, (char)0x005e, (char)0x02c8, (char)0x0027, (char)0x02c9, (char)0x003f, (char)0x02ca, (char)0x003f, (char)0x02cb, (char)0x0060, (char)0x02cd, (char)0x005f, (char)0x02da, (char)0x003f, (char)0x02dc, (char)0x007e, (char)0x0300, (char)0x0060, (char)0x0302, (char)0x005e, (char)0x0303, (char)0x007e, (char)0x030e, (char)0x0022, (char)0x0331, (char)0x005f, (char)0x0332, (char)0x005f, (char)0x2000, (char)0x0020, (char)0x2001, (char)0x0020, (char)0x2002, (char)0x0020, (char)0x2003, (char)0x0020, (char)0x2004, (char)0x0020, (char)0x2005, (char)0x0020, (char)0x2006, (char)0x0020, (char)0x2010, (char)0x002d, (char)0x2011, (char)0x002d, (char)0x2013, (char)0x002d, (char)0x2014, (char)0x002d, (char)0x2018, (char)0x0027, (char)0x2019, (char)0x0027, (char)0x201a, (char)0x002c, (char)0x201c, (char)0x0022, (char)0x201d, (char)0x0022, (char)0x201e, (char)0x0022, (char)0x2020, (char)0x003f, (char)0x2021, (char)0x003f, (char)0x2022, (char)0x002e, (char)0x2026, (char)0x002e, (char)0x2030, (char)0x003f, (char)0x2032, (char)0x0027, (char)0x2035, (char)0x0060, (char)0x2039, (char)0x003c, (char)0x203a, (char)0x003e, (char)0x2122, (char)0x0054, (char)0xff01, (char)0x0021, (char)0xff02, (char)0x0022, (char)0xff03, (char)0x0023, (char)0xff04, (char)0x0024, (char)0xff05, (char)0x0025, (char)0xff06, (char)0x0026, (char)0xff07, (char)0x0027, (char)0xff08, (char)0x0028, (char)0xff09, (char)0x0029, (char)0xff0a, (char)0x002a, (char)0xff0b, (char)0x002b, (char)0xff0c, (char)0x002c, (char)0xff0d, (char)0x002d, (char)0xff0e, (char)0x002e, (char)0xff0f, (char)0x002f, (char)0xff10, (char)0x0030, (char)0xff11, (char)0x0031, (char)0xff12, (char)0x0032, (char)0xff13, (char)0x0033, (char)0xff14, (char)0x0034, (char)0xff15, (char)0x0035, (char)0xff16, (char)0x0036, (char)0xff17, (char)0x0037, (char)0xff18, (char)0x0038, (char)0xff19, (char)0x0039, (char)0xff1a, (char)0x003a, (char)0xff1b, (char)0x003b, (char)0xff1c, (char)0x003c, (char)0xff1d, (char)0x003d, (char)0xff1e, (char)0x003e, (char)0xff1f, (char)0x003f, (char)0xff20, (char)0x0040, (char)0xff21, (char)0x0041, (char)0xff22, (char)0x0042, (char)0xff23, (char)0x0043, (char)0xff24, (char)0x0044, (char)0xff25, (char)0x0045, (char)0xff26, (char)0x0046, (char)0xff27, (char)0x0047, (char)0xff28, (char)0x0048, (char)0xff29, (char)0x0049, (char)0xff2a, (char)0x004a, (char)0xff2b, (char)0x004b, (char)0xff2c, (char)0x004c, (char)0xff2d, (char)0x004d, (char)0xff2e, (char)0x004e, (char)0xff2f, (char)0x004f, (char)0xff30, (char)0x0050, (char)0xff31, (char)0x0051, (char)0xff32, (char)0x0052, (char)0xff33, (char)0x0053, (char)0xff34, (char)0x0054, (char)0xff35, (char)0x0055, (char)0xff36, (char)0x0056, (char)0xff37, (char)0x0057, (char)0xff38, (char)0x0058, (char)0xff39, (char)0x0059, (char)0xff3a, (char)0x005a, (char)0xff3b, (char)0x005b, (char)0xff3c, (char)0x005c, (char)0xff3d, (char)0x005d, (char)0xff3e, (char)0x005e, (char)0xff3f, (char)0x005f, (char)0xff40, (char)0x0060, (char)0xff41, (char)0x0061, (char)0xff42, (char)0x0062, (char)0xff43, (char)0x0063, (char)0xff44, (char)0x0064, (char)0xff45, (char)0x0065, (char)0xff46, (char)0x0066, (char)0xff47, (char)0x0067, (char)0xff48, (char)0x0068, (char)0xff49, (char)0x0069, (char)0xff4a, (char)0x006a, (char)0xff4b, (char)0x006b, (char)0xff4c, (char)0x006c, (char)0xff4d, (char)0x006d, (char)0xff4e, (char)0x006e, (char)0xff4f, (char)0x006f, (char)0xff50, (char)0x0070, (char)0xff51, (char)0x0071, (char)0xff52, (char)0x0072, (char)0xff53, (char)0x0073, (char)0xff54, (char)0x0074, (char)0xff55, (char)0x0075, (char)0xff56, (char)0x0076, (char)0xff57, (char)0x0077, (char)0xff58, (char)0x0078, (char)0xff59, (char)0x0079, (char)0xff5a, (char)0x007a, (char)0xff5b, (char)0x007b, (char)0xff5c, (char)0x007c, (char)0xff5d, (char)0x007d, (char)0xff5e, (char)0x007e};
    }
}