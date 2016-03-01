using System.Collections;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Text
{
    public abstract class Encoding : ICloneable
    {
        private static volatile Encoding defaultEncoding;
        private static volatile Encoding unicodeEncoding;
        private static volatile Encoding bigEndianUnicode;
        private static volatile Encoding utf7Encoding;
        private static volatile Encoding utf8Encoding;
        private static volatile Encoding utf32Encoding;
        private static volatile Encoding asciiEncoding;
        private static volatile Encoding latin1Encoding;
        static volatile Hashtable encodings;
        internal const int MIMECONTF_MAILNEWS = 0x00000001;
        internal const int MIMECONTF_BROWSER = 0x00000002;
        internal const int MIMECONTF_SAVABLE_MAILNEWS = 0x00000100;
        internal const int MIMECONTF_SAVABLE_BROWSER = 0x00000200;
        private const int CodePageDefault = 0;
        private const int CodePageNoOEM = 1;
        private const int CodePageNoMac = 2;
        private const int CodePageNoThread = 3;
        private const int CodePageNoSymbol = 42;
        private const int CodePageUnicode = 1200;
        private const int CodePageBigEndian = 1201;
        private const int CodePageWindows1252 = 1252;
        private const int CodePageMacGB2312 = 10008;
        private const int CodePageGB2312 = 20936;
        private const int CodePageMacKorean = 10003;
        private const int CodePageDLLKorean = 20949;
        private const int ISO2022JP = 50220;
        private const int ISO2022JPESC = 50221;
        private const int ISO2022JPSISO = 50222;
        private const int ISOKorean = 50225;
        private const int ISOSimplifiedCN = 50227;
        private const int EUCJP = 51932;
        private const int ChineseHZ = 52936;
        private const int DuplicateEUCCN = 51936;
        private const int EUCCN = 936;
        private const int EUCKR = 51949;
        internal const int CodePageASCII = 20127;
        internal const int ISO_8859_1 = 28591;
        private const int ISCIIAssemese = 57006;
        private const int ISCIIBengali = 57003;
        private const int ISCIIDevanagari = 57002;
        private const int ISCIIGujarathi = 57010;
        private const int ISCIIKannada = 57008;
        private const int ISCIIMalayalam = 57009;
        private const int ISCIIOriya = 57007;
        private const int ISCIIPanjabi = 57011;
        private const int ISCIITamil = 57004;
        private const int ISCIITelugu = 57005;
        private const int GB18030 = 54936;
        private const int ISO_8859_8I = 38598;
        private const int ISO_8859_8_Visual = 28598;
        private const int ENC50229 = 50229;
        private const int CodePageUTF7 = 65000;
        private const int CodePageUTF8 = 65001;
        private const int CodePageUTF32 = 12000;
        private const int CodePageUTF32BE = 12001;
        internal int m_codePage = 0;
        internal CodePageDataItem dataItem = null;
        internal bool m_deserializedFromEverett = false;
        private bool m_isReadOnly = true;
        internal EncoderFallback encoderFallback = null;
        internal DecoderFallback decoderFallback = null;
        protected Encoding(): this (0)
        {
        }

        protected Encoding(int codePage)
        {
            if (codePage < 0)
            {
                throw new ArgumentOutOfRangeException("codePage");
            }

            Contract.EndContractBlock();
            m_codePage = codePage;
            this.SetDefaultFallbacks();
        }

        protected Encoding(int codePage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            if (codePage < 0)
            {
                throw new ArgumentOutOfRangeException("codePage");
            }

            Contract.EndContractBlock();
            m_codePage = codePage;
            this.encoderFallback = encoderFallback ?? new InternalEncoderBestFitFallback(this);
            this.decoderFallback = decoderFallback ?? new InternalDecoderBestFitFallback(this);
        }

        internal virtual void SetDefaultFallbacks()
        {
            this.encoderFallback = new InternalEncoderBestFitFallback(this);
            this.decoderFallback = new InternalDecoderBestFitFallback(this);
        }

        internal void OnDeserializing()
        {
            encoderFallback = null;
            decoderFallback = null;
            m_isReadOnly = true;
        }

        internal void OnDeserialized()
        {
            if (encoderFallback == null || decoderFallback == null)
            {
                m_deserializedFromEverett = true;
                SetDefaultFallbacks();
            }

            dataItem = null;
        }

        private void OnDeserializing(StreamingContext ctx)
        {
            OnDeserializing();
        }

        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserialized();
        }

        private void OnSerializing(StreamingContext ctx)
        {
            dataItem = null;
        }

        internal void DeserializeEncoding(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            this.m_codePage = (int)info.GetValue("m_codePage", typeof (int));
            this.dataItem = null;
            try
            {
                this.m_isReadOnly = (bool)info.GetValue("m_isReadOnly", typeof (bool));
                this.encoderFallback = (EncoderFallback)info.GetValue("encoderFallback", typeof (EncoderFallback));
                this.decoderFallback = (DecoderFallback)info.GetValue("decoderFallback", typeof (DecoderFallback));
            }
            catch (SerializationException)
            {
                this.m_deserializedFromEverett = true;
                this.m_isReadOnly = true;
                SetDefaultFallbacks();
            }
        }

        internal void SerializeEncoding(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            info.AddValue("m_isReadOnly", this.m_isReadOnly);
            info.AddValue("encoderFallback", this.EncoderFallback);
            info.AddValue("decoderFallback", this.DecoderFallback);
            info.AddValue("m_codePage", this.m_codePage);
            info.AddValue("dataItem", null);
            info.AddValue("Encoding+m_codePage", this.m_codePage);
            info.AddValue("Encoding+dataItem", null);
        }

        public static byte[] Convert(Encoding srcEncoding, Encoding dstEncoding, byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            Contract.Ensures(Contract.Result<byte[]>() != null);
            return Convert(srcEncoding, dstEncoding, bytes, 0, bytes.Length);
        }

        public static byte[] Convert(Encoding srcEncoding, Encoding dstEncoding, byte[] bytes, int index, int count)
        {
            if (srcEncoding == null || dstEncoding == null)
            {
                throw new ArgumentNullException((srcEncoding == null ? "srcEncoding" : "dstEncoding"), Environment.GetResourceString("ArgumentNull_Array"));
            }

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }

            Contract.Ensures(Contract.Result<byte[]>() != null);
            return dstEncoding.GetBytes(srcEncoding.GetChars(bytes, index, count));
        }

        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_InternalSyncObject, o, null);
                }

                return s_InternalSyncObject;
            }
        }

        public static void RegisterProvider(EncodingProvider provider)
        {
            EncodingProvider.AddProvider(provider);
        }

        public static Encoding GetEncoding(int codepage)
        {
            Encoding result = EncodingProvider.GetEncodingFromProvider(codepage);
            if (result != null)
                return result;
            if (codepage < 0 || codepage > 65535)
            {
                throw new ArgumentOutOfRangeException("codepage", Environment.GetResourceString("ArgumentOutOfRange_Range", 0, 65535));
            }

            Contract.EndContractBlock();
            if (encodings != null)
            {
                result = (Encoding)encodings[codepage];
            }

            if (result == null)
            {
                lock (InternalSyncObject)
                {
                    if (encodings == null)
                    {
                        encodings = new Hashtable();
                    }

                    if ((result = (Encoding)encodings[codepage]) != null)
                        return result;
                    switch (codepage)
                    {
                        case CodePageDefault:
                            result = Encoding.Default;
                            break;
                        case CodePageUnicode:
                            result = Unicode;
                            break;
                        case CodePageBigEndian:
                            result = BigEndianUnicode;
                            break;
                        case CodePageUTF7:
                            result = UTF7;
                            break;
                        case CodePageUTF32:
                            result = UTF32;
                            break;
                        case CodePageUTF32BE:
                            result = new UTF32Encoding(true, true);
                            break;
                        case CodePageUTF8:
                            result = UTF8;
                            break;
                        case CodePageNoOEM:
                        case CodePageNoMac:
                        case CodePageNoThread:
                        case CodePageNoSymbol:
                            throw new ArgumentException(Environment.GetResourceString("Argument_CodepageNotSupported", codepage), "codepage");
                        case CodePageASCII:
                            result = ASCII;
                            break;
                        case ISO_8859_1:
                            result = Latin1;
                            break;
                        default:
                        {
                            if (EncodingTable.GetCodePageDataItem(codepage) == null)
                            {
                                throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoCodepageData", codepage));
                            }

                            result = UTF8;
                            break;
                        }
                    }

                    encodings.Add(codepage, result);
                }
            }

            return result;
        }

        public static Encoding GetEncoding(int codepage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            Encoding baseEncoding = EncodingProvider.GetEncodingFromProvider(codepage, encoderFallback, decoderFallback);
            if (baseEncoding != null)
                return baseEncoding;
            baseEncoding = GetEncoding(codepage);
            Encoding fallbackEncoding = (Encoding)baseEncoding.Clone();
            fallbackEncoding.EncoderFallback = encoderFallback;
            fallbackEncoding.DecoderFallback = decoderFallback;
            return fallbackEncoding;
        }

        public static Encoding GetEncoding(String name)
        {
            Encoding baseEncoding = EncodingProvider.GetEncodingFromProvider(name);
            if (baseEncoding != null)
                return baseEncoding;
            return (GetEncoding(EncodingTable.GetCodePageFromName(name)));
        }

        public static Encoding GetEncoding(String name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            Encoding baseEncoding = EncodingProvider.GetEncodingFromProvider(name, encoderFallback, decoderFallback);
            if (baseEncoding != null)
                return baseEncoding;
            return (GetEncoding(EncodingTable.GetCodePageFromName(name), encoderFallback, decoderFallback));
        }

        public static EncodingInfo[] GetEncodings()
        {
            return EncodingTable.GetEncodings();
        }

        public virtual byte[] GetPreamble()
        {
            return EmptyArray<Byte>.Value;
        }

        private void GetDataItem()
        {
            if (dataItem == null)
            {
                dataItem = EncodingTable.GetCodePageDataItem(m_codePage);
                if (dataItem == null)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoCodepageData", m_codePage));
                }
            }
        }

        public virtual String BodyName
        {
            get
            {
                if (dataItem == null)
                {
                    GetDataItem();
                }

                return (dataItem.BodyName);
            }
        }

        public virtual String EncodingName
        {
            get
            {
                return (Environment.GetResourceString("Globalization.cp_" + m_codePage));
            }
        }

        public virtual String HeaderName
        {
            get
            {
                if (dataItem == null)
                {
                    GetDataItem();
                }

                return (dataItem.HeaderName);
            }
        }

        public virtual String WebName
        {
            get
            {
                if (dataItem == null)
                {
                    GetDataItem();
                }

                return (dataItem.WebName);
            }
        }

        public virtual int WindowsCodePage
        {
            get
            {
                if (dataItem == null)
                {
                    GetDataItem();
                }

                return (dataItem.UIFamilyCodePage);
            }
        }

        public virtual bool IsBrowserDisplay
        {
            get
            {
                if (dataItem == null)
                {
                    GetDataItem();
                }

                return ((dataItem.Flags & MIMECONTF_BROWSER) != 0);
            }
        }

        public virtual bool IsBrowserSave
        {
            get
            {
                if (dataItem == null)
                {
                    GetDataItem();
                }

                return ((dataItem.Flags & MIMECONTF_SAVABLE_BROWSER) != 0);
            }
        }

        public virtual bool IsMailNewsDisplay
        {
            get
            {
                if (dataItem == null)
                {
                    GetDataItem();
                }

                return ((dataItem.Flags & MIMECONTF_MAILNEWS) != 0);
            }
        }

        public virtual bool IsMailNewsSave
        {
            get
            {
                if (dataItem == null)
                {
                    GetDataItem();
                }

                return ((dataItem.Flags & MIMECONTF_SAVABLE_MAILNEWS) != 0);
            }
        }

        public virtual bool IsSingleByte
        {
            get
            {
                return false;
            }
        }

        public EncoderFallback EncoderFallback
        {
            get
            {
                return encoderFallback;
            }

            set
            {
                if (this.IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                    throw new ArgumentNullException("value");
                Contract.EndContractBlock();
                encoderFallback = value;
            }
        }

        public DecoderFallback DecoderFallback
        {
            get
            {
                return decoderFallback;
            }

            set
            {
                if (this.IsReadOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
                if (value == null)
                    throw new ArgumentNullException("value");
                Contract.EndContractBlock();
                decoderFallback = value;
            }
        }

        public virtual Object Clone()
        {
            Encoding newEncoding = (Encoding)this.MemberwiseClone();
            newEncoding.m_isReadOnly = false;
            return newEncoding;
        }

        public bool IsReadOnly
        {
            get
            {
                return (m_isReadOnly);
            }
        }

        public static Encoding ASCII
        {
            get
            {
                if (asciiEncoding == null)
                    asciiEncoding = new ASCIIEncoding();
                return asciiEncoding;
            }
        }

        private static Encoding Latin1
        {
            get
            {
                if (latin1Encoding == null)
                    latin1Encoding = new Latin1Encoding();
                return latin1Encoding;
            }
        }

        public virtual int GetByteCount(char[] chars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            }

            Contract.EndContractBlock();
            return GetByteCount(chars, 0, chars.Length);
        }

        public virtual int GetByteCount(String s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            Contract.EndContractBlock();
            char[] chars = s.ToCharArray();
            return GetByteCount(chars, 0, chars.Length);
        }

        public abstract int GetByteCount(char[] chars, int index, int count);
        public virtual unsafe int GetByteCount(char *chars, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            char[] arrChar = new char[count];
            int index;
            for (index = 0; index < count; index++)
                arrChar[index] = chars[index];
            return GetByteCount(arrChar, 0, count);
        }

        internal virtual unsafe int GetByteCount(char *chars, int count, EncoderNLS encoder)
        {
            Contract.Requires(chars != null);
            Contract.Requires(count >= 0);
            return GetByteCount(chars, count);
        }

        public virtual byte[] GetBytes(char[] chars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException("chars", Environment.GetResourceString("ArgumentNull_Array"));
            }

            Contract.EndContractBlock();
            return GetBytes(chars, 0, chars.Length);
        }

        public virtual byte[] GetBytes(char[] chars, int index, int count)
        {
            byte[] result = new byte[GetByteCount(chars, index, count)];
            GetBytes(chars, index, count, result, 0);
            return result;
        }

        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex);
        public virtual byte[] GetBytes(String s)
        {
            if (s == null)
                throw new ArgumentNullException("s", Environment.GetResourceString("ArgumentNull_String"));
            Contract.EndContractBlock();
            int byteCount = GetByteCount(s);
            byte[] bytes = new byte[byteCount];
            int bytesReceived = GetBytes(s, 0, s.Length, bytes, 0);
            Contract.Assert(byteCount == bytesReceived);
            return bytes;
        }

        public virtual int GetBytes(String s, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            Contract.EndContractBlock();
            return GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);
        }

        internal virtual unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, EncoderNLS encoder)
        {
            return GetBytes(chars, charCount, bytes, byteCount);
        }

        public virtual unsafe int GetBytes(char *chars, int charCount, byte *bytes, int byteCount)
        {
            if (bytes == null || chars == null)
                throw new ArgumentNullException(bytes == null ? "bytes" : "chars", Environment.GetResourceString("ArgumentNull_Array"));
            if (charCount < 0 || byteCount < 0)
                throw new ArgumentOutOfRangeException((charCount < 0 ? "charCount" : "byteCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            char[] arrChar = new char[charCount];
            int index;
            for (index = 0; index < charCount; index++)
                arrChar[index] = chars[index];
            byte[] arrByte = new byte[byteCount];
            int result = GetBytes(arrChar, 0, charCount, arrByte, 0);
            Contract.Assert(result <= byteCount, "[Encoding.GetBytes]Returned more bytes than we have space for");
            if (result < byteCount)
                byteCount = result;
            for (index = 0; index < byteCount; index++)
                bytes[index] = arrByte[index];
            return byteCount;
        }

        public virtual int GetCharCount(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }

            Contract.EndContractBlock();
            return GetCharCount(bytes, 0, bytes.Length);
        }

        public abstract int GetCharCount(byte[] bytes, int index, int count);
        public virtual unsafe int GetCharCount(byte *bytes, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            byte[] arrbyte = new byte[count];
            int index;
            for (index = 0; index < count; index++)
                arrbyte[index] = bytes[index];
            return GetCharCount(arrbyte, 0, count);
        }

        internal virtual unsafe int GetCharCount(byte *bytes, int count, DecoderNLS decoder)
        {
            return GetCharCount(bytes, count);
        }

        public virtual char[] GetChars(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            }

            Contract.EndContractBlock();
            return GetChars(bytes, 0, bytes.Length);
        }

        public virtual char[] GetChars(byte[] bytes, int index, int count)
        {
            char[] result = new char[GetCharCount(bytes, index, count)];
            GetChars(bytes, index, count, result, 0);
            return result;
        }

        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        public virtual unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount)
        {
            if (chars == null || bytes == null)
                throw new ArgumentNullException(chars == null ? "chars" : "bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (byteCount < 0 || charCount < 0)
                throw new ArgumentOutOfRangeException((byteCount < 0 ? "byteCount" : "charCount"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            byte[] arrByte = new byte[byteCount];
            int index;
            for (index = 0; index < byteCount; index++)
                arrByte[index] = bytes[index];
            char[] arrChar = new char[charCount];
            int result = GetChars(arrByte, 0, byteCount, arrChar, 0);
            Contract.Assert(result <= charCount, "[Encoding.GetChars]Returned more chars than we have space for");
            if (result < charCount)
                charCount = result;
            for (index = 0; index < charCount; index++)
                chars[index] = arrChar[index];
            return charCount;
        }

        internal virtual unsafe int GetChars(byte *bytes, int byteCount, char *chars, int charCount, DecoderNLS decoder)
        {
            return GetChars(bytes, byteCount, chars, charCount);
        }

        public unsafe string GetString(byte *bytes, int byteCount)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException("byteCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            return String.CreateStringFromEncoding(bytes, byteCount, this);
        }

        public virtual int CodePage
        {
            get
            {
                return m_codePage;
            }
        }

        public bool IsAlwaysNormalized()
        {
            return this.IsAlwaysNormalized((NormalizationForm)ExtendedNormalizationForms.FormIdna);
        }

        public virtual bool IsAlwaysNormalized(NormalizationForm form)
        {
            return false;
        }

        public virtual Decoder GetDecoder()
        {
            return new DefaultDecoder(this);
        }

        private static Encoding CreateDefaultEncoding()
        {
            Encoding enc;
            enc = UTF8;
            return (enc);
        }

        public static Encoding Default
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (defaultEncoding == null)
                {
                    defaultEncoding = CreateDefaultEncoding();
                }

                return defaultEncoding;
            }
        }

        public virtual Encoder GetEncoder()
        {
            return new DefaultEncoder(this);
        }

        public abstract int GetMaxByteCount(int charCount);
        public abstract int GetMaxCharCount(int byteCount);
        public virtual String GetString(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes", Environment.GetResourceString("ArgumentNull_Array"));
            Contract.EndContractBlock();
            return GetString(bytes, 0, bytes.Length);
        }

        public virtual String GetString(byte[] bytes, int index, int count)
        {
            return new String(GetChars(bytes, index, count));
        }

        public static Encoding Unicode
        {
            get
            {
                if (unicodeEncoding == null)
                    unicodeEncoding = new UnicodeEncoding(false, true);
                return unicodeEncoding;
            }
        }

        public static Encoding BigEndianUnicode
        {
            get
            {
                if (bigEndianUnicode == null)
                    bigEndianUnicode = new UnicodeEncoding(true, true);
                return bigEndianUnicode;
            }
        }

        public static Encoding UTF7
        {
            get
            {
                if (utf7Encoding == null)
                    utf7Encoding = new UTF7Encoding();
                return utf7Encoding;
            }
        }

        public static Encoding UTF8
        {
            get
            {
                if (utf8Encoding == null)
                    utf8Encoding = new UTF8Encoding(true);
                return utf8Encoding;
            }
        }

        public static Encoding UTF32
        {
            get
            {
                if (utf32Encoding == null)
                    utf32Encoding = new UTF32Encoding(false, true);
                return utf32Encoding;
            }
        }

        public override bool Equals(Object value)
        {
            Encoding that = value as Encoding;
            if (that != null)
                return (m_codePage == that.m_codePage) && (EncoderFallback.Equals(that.EncoderFallback)) && (DecoderFallback.Equals(that.DecoderFallback));
            return (false);
        }

        public override int GetHashCode()
        {
            return m_codePage + this.EncoderFallback.GetHashCode() + this.DecoderFallback.GetHashCode();
        }

        internal virtual char[] GetBestFitUnicodeToBytesData()
        {
            return EmptyArray<Char>.Value;
        }

        internal virtual char[] GetBestFitBytesToUnicodeData()
        {
            return EmptyArray<Char>.Value;
        }

        internal void ThrowBytesOverflow()
        {
            throw new ArgumentException(Environment.GetResourceString("Argument_EncodingConversionOverflowBytes", EncodingName, EncoderFallback.GetType()), "bytes");
        }

        internal void ThrowBytesOverflow(EncoderNLS encoder, bool nothingEncoded)
        {
            if (encoder == null || encoder.m_throwOnOverflow || nothingEncoded)
            {
                if (encoder != null && encoder.InternalHasFallbackBuffer)
                    encoder.FallbackBuffer.InternalReset();
                ThrowBytesOverflow();
            }

            encoder.ClearMustFlush();
        }

        internal void ThrowCharsOverflow()
        {
            throw new ArgumentException(Environment.GetResourceString("Argument_EncodingConversionOverflowChars", EncodingName, DecoderFallback.GetType()), "chars");
        }

        internal void ThrowCharsOverflow(DecoderNLS decoder, bool nothingDecoded)
        {
            if (decoder == null || decoder.m_throwOnOverflow || nothingDecoded)
            {
                if (decoder != null && decoder.InternalHasFallbackBuffer)
                    decoder.FallbackBuffer.InternalReset();
                ThrowCharsOverflow();
            }

            decoder.ClearMustFlush();
        }

        internal class DefaultEncoder : Encoder, ISerializable, IObjectReference
        {
            private Encoding m_encoding;
            private bool m_hasInitializedEncoding;
            internal char charLeftOver;
            public DefaultEncoder(Encoding encoding)
            {
                m_encoding = encoding;
                m_hasInitializedEncoding = true;
            }

            internal DefaultEncoder(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");
                Contract.EndContractBlock();
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof (Encoding));
                try
                {
                    this.m_fallback = (EncoderFallback)info.GetValue("m_fallback", typeof (EncoderFallback));
                    this.charLeftOver = (Char)info.GetValue("charLeftOver", typeof (Char));
                }
                catch (SerializationException)
                {
                }
            }

            public Object GetRealObject(StreamingContext context)
            {
                if (m_hasInitializedEncoding)
                {
                    return this;
                }

                Encoder encoder = m_encoding.GetEncoder();
                if (m_fallback != null)
                    encoder.m_fallback = m_fallback;
                if (charLeftOver != (char)0)
                {
                    EncoderNLS encoderNls = encoder as EncoderNLS;
                    if (encoderNls != null)
                        encoderNls.charLeftOver = charLeftOver;
                }

                return encoder;
            }

            public override int GetByteCount(char[] chars, int index, int count, bool flush)
            {
                return m_encoding.GetByteCount(chars, index, count);
            }

            public unsafe override int GetByteCount(char *chars, int count, bool flush)
            {
                return m_encoding.GetByteCount(chars, count);
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
            {
                return m_encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
            }

            public unsafe override int GetBytes(char *chars, int charCount, byte *bytes, int byteCount, bool flush)
            {
                return m_encoding.GetBytes(chars, charCount, bytes, byteCount);
            }
        }

        internal class DefaultDecoder : Decoder, ISerializable, IObjectReference
        {
            private Encoding m_encoding;
            private bool m_hasInitializedEncoding;
            public DefaultDecoder(Encoding encoding)
            {
                m_encoding = encoding;
                m_hasInitializedEncoding = true;
            }

            internal DefaultDecoder(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                    throw new ArgumentNullException("info");
                Contract.EndContractBlock();
                this.m_encoding = (Encoding)info.GetValue("encoding", typeof (Encoding));
                try
                {
                    this.m_fallback = (DecoderFallback)info.GetValue("m_fallback", typeof (DecoderFallback));
                }
                catch (SerializationException)
                {
                    m_fallback = null;
                }
            }

            public Object GetRealObject(StreamingContext context)
            {
                if (m_hasInitializedEncoding)
                {
                    return this;
                }

                Decoder decoder = m_encoding.GetDecoder();
                if (m_fallback != null)
                    decoder.m_fallback = m_fallback;
                return decoder;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return GetCharCount(bytes, index, count, false);
            }

            public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
            {
                return m_encoding.GetCharCount(bytes, index, count);
            }

            public unsafe override int GetCharCount(byte *bytes, int count, bool flush)
            {
                return m_encoding.GetCharCount(bytes, count);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
            {
                return m_encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }

            public unsafe override int GetChars(byte *bytes, int byteCount, char *chars, int charCount, bool flush)
            {
                return m_encoding.GetChars(bytes, byteCount, chars, charCount);
            }
        }

        internal class EncodingCharBuffer
        {
            unsafe char *chars;
            unsafe char *charStart;
            unsafe char *charEnd;
            int charCountResult = 0;
            Encoding enc;
            DecoderNLS decoder;
            unsafe byte *byteStart;
            unsafe byte *byteEnd;
            unsafe byte *bytes;
            DecoderFallbackBuffer fallbackBuffer;
            internal unsafe EncodingCharBuffer(Encoding enc, DecoderNLS decoder, char *charStart, int charCount, byte *byteStart, int byteCount)
            {
                this.enc = enc;
                this.decoder = decoder;
                this.chars = charStart;
                this.charStart = charStart;
                this.charEnd = charStart + charCount;
                this.byteStart = byteStart;
                this.bytes = byteStart;
                this.byteEnd = byteStart + byteCount;
                if (this.decoder == null)
                    this.fallbackBuffer = enc.DecoderFallback.CreateFallbackBuffer();
                else
                    this.fallbackBuffer = this.decoder.FallbackBuffer;
                Contract.Assert(fallbackBuffer.Remaining == 0, "[Encoding.EncodingCharBuffer.EncodingCharBuffer]Expected empty fallback buffer for getchars/charcount");
                fallbackBuffer.InternalInitialize(bytes, charEnd);
            }

            internal unsafe bool AddChar(char ch, int numBytes)
            {
                if (chars != null)
                {
                    if (chars >= charEnd)
                    {
                        bytes -= numBytes;
                        enc.ThrowCharsOverflow(decoder, bytes <= byteStart);
                        return false;
                    }

                    *(chars++) = ch;
                }

                charCountResult++;
                return true;
            }

            internal unsafe bool AddChar(char ch)
            {
                return AddChar(ch, 1);
            }

            internal unsafe bool AddChar(char ch1, char ch2, int numBytes)
            {
                if (chars >= charEnd - 1)
                {
                    bytes -= numBytes;
                    enc.ThrowCharsOverflow(decoder, bytes <= byteStart);
                    return false;
                }

                return AddChar(ch1, numBytes) && AddChar(ch2, numBytes);
            }

            internal unsafe void AdjustBytes(int count)
            {
                bytes += count;
            }

            internal unsafe bool MoreData
            {
                [System.Security.SecurityCritical]
                get
                {
                    return bytes < byteEnd;
                }
            }

            internal unsafe bool EvenMoreData(int count)
            {
                return (bytes <= byteEnd - count);
            }

            internal unsafe byte GetNextByte()
            {
                Contract.Assert(bytes < byteEnd, "[EncodingCharBuffer.GetNextByte]Expected more date");
                if (bytes >= byteEnd)
                    return 0;
                return *(bytes++);
            }

            internal unsafe int BytesUsed
            {
                [System.Security.SecurityCritical]
                get
                {
                    return (int)(bytes - byteStart);
                }
            }

            internal unsafe bool Fallback(byte fallbackByte)
            {
                byte[] byteBuffer = new byte[]{fallbackByte};
                return Fallback(byteBuffer);
            }

            internal unsafe bool Fallback(byte byte1, byte byte2)
            {
                byte[] byteBuffer = new byte[]{byte1, byte2};
                return Fallback(byteBuffer);
            }

            internal unsafe bool Fallback(byte byte1, byte byte2, byte byte3, byte byte4)
            {
                byte[] byteBuffer = new byte[]{byte1, byte2, byte3, byte4};
                return Fallback(byteBuffer);
            }

            internal unsafe bool Fallback(byte[] byteBuffer)
            {
                if (chars != null)
                {
                    char *pTemp = chars;
                    if (fallbackBuffer.InternalFallback(byteBuffer, bytes, ref chars) == false)
                    {
                        bytes -= byteBuffer.Length;
                        fallbackBuffer.InternalReset();
                        enc.ThrowCharsOverflow(decoder, chars == charStart);
                        return false;
                    }

                    charCountResult += unchecked ((int)(chars - pTemp));
                }
                else
                {
                    charCountResult += fallbackBuffer.InternalFallback(byteBuffer, bytes);
                }

                return true;
            }

            internal unsafe int Count
            {
                get
                {
                    return charCountResult;
                }
            }
        }

        internal class EncodingByteBuffer
        {
            unsafe byte *bytes;
            unsafe byte *byteStart;
            unsafe byte *byteEnd;
            unsafe char *chars;
            unsafe char *charStart;
            unsafe char *charEnd;
            int byteCountResult = 0;
            Encoding enc;
            EncoderNLS encoder;
            internal EncoderFallbackBuffer fallbackBuffer;
            internal unsafe EncodingByteBuffer(Encoding inEncoding, EncoderNLS inEncoder, byte *inByteStart, int inByteCount, char *inCharStart, int inCharCount)
            {
                this.enc = inEncoding;
                this.encoder = inEncoder;
                this.charStart = inCharStart;
                this.chars = inCharStart;
                this.charEnd = inCharStart + inCharCount;
                this.bytes = inByteStart;
                this.byteStart = inByteStart;
                this.byteEnd = inByteStart + inByteCount;
                if (this.encoder == null)
                    this.fallbackBuffer = enc.EncoderFallback.CreateFallbackBuffer();
                else
                {
                    this.fallbackBuffer = this.encoder.FallbackBuffer;
                    if (encoder.m_throwOnOverflow && encoder.InternalHasFallbackBuffer && this.fallbackBuffer.Remaining > 0)
                        throw new ArgumentException(Environment.GetResourceString("Argument_EncoderFallbackNotEmpty", encoder.Encoding.EncodingName, encoder.Fallback.GetType()));
                }

                fallbackBuffer.InternalInitialize(chars, charEnd, encoder, bytes != null);
            }

            internal unsafe bool AddByte(byte b, int moreBytesExpected)
            {
                Contract.Assert(moreBytesExpected >= 0, "[EncodingByteBuffer.AddByte]expected non-negative moreBytesExpected");
                if (bytes != null)
                {
                    if (bytes >= byteEnd - moreBytesExpected)
                    {
                        this.MovePrevious(true);
                        return false;
                    }

                    *(bytes++) = b;
                }

                byteCountResult++;
                return true;
            }

            internal unsafe bool AddByte(byte b1)
            {
                return (AddByte(b1, 0));
            }

            internal unsafe bool AddByte(byte b1, byte b2)
            {
                return (AddByte(b1, b2, 0));
            }

            internal unsafe bool AddByte(byte b1, byte b2, int moreBytesExpected)
            {
                return (AddByte(b1, 1 + moreBytesExpected) && AddByte(b2, moreBytesExpected));
            }

            internal unsafe bool AddByte(byte b1, byte b2, byte b3)
            {
                return AddByte(b1, b2, b3, (int)0);
            }

            internal unsafe bool AddByte(byte b1, byte b2, byte b3, int moreBytesExpected)
            {
                return (AddByte(b1, 2 + moreBytesExpected) && AddByte(b2, 1 + moreBytesExpected) && AddByte(b3, moreBytesExpected));
            }

            internal unsafe bool AddByte(byte b1, byte b2, byte b3, byte b4)
            {
                return (AddByte(b1, 3) && AddByte(b2, 2) && AddByte(b3, 1) && AddByte(b4, 0));
            }

            internal unsafe void MovePrevious(bool bThrow)
            {
                if (fallbackBuffer.bFallingBack)
                    fallbackBuffer.MovePrevious();
                else
                {
                    Contract.Assert(chars > charStart || ((bThrow == true) && (bytes == byteStart)), "[EncodingByteBuffer.MovePrevious]expected previous data or throw");
                    if (chars > charStart)
                        chars--;
                }

                if (bThrow)
                    enc.ThrowBytesOverflow(encoder, bytes == byteStart);
            }

            internal unsafe bool Fallback(char charFallback)
            {
                return fallbackBuffer.InternalFallback(charFallback, ref chars);
            }

            internal unsafe bool MoreData
            {
                [System.Security.SecurityCritical]
                get
                {
                    return ((fallbackBuffer.Remaining > 0) || (chars < charEnd));
                }
            }

            internal unsafe char GetNextChar()
            {
                char cReturn = fallbackBuffer.InternalGetNextChar();
                if (cReturn == 0)
                {
                    if (chars < charEnd)
                        cReturn = *(chars++);
                }

                return cReturn;
            }

            internal unsafe int CharsUsed
            {
                [System.Security.SecurityCritical]
                get
                {
                    return (int)(chars - charStart);
                }
            }

            internal unsafe int Count
            {
                get
                {
                    return byteCountResult;
                }
            }
        }
    }
}