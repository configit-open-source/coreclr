
using System.Runtime.Serialization;

namespace System.Globalization
{
    public class TextInfo : ICloneable, IDeserializationCallback
    {
        private String m_listSeparator;
        private bool m_isReadOnly = false;
        private String m_cultureName;
        private CultureData m_cultureData;
        private String m_textInfoName;
        private IntPtr m_dataHandle;
        private IntPtr m_handleOrigin;
        private bool ? m_IsAsciiCasingSameAsInvariant;
        internal static TextInfo Invariant
        {
            get
            {
                if (s_Invariant == null)
                    s_Invariant = new TextInfo(CultureData.Invariant);
                return s_Invariant;
            }
        }

        internal volatile static TextInfo s_Invariant;
        internal TextInfo(CultureData cultureData)
        {
            this.m_cultureData = cultureData;
            this.m_cultureName = this.m_cultureData.CultureName;
            this.m_textInfoName = this.m_cultureData.STEXTINFO;
        }

        private string customCultureName;
        private void OnDeserializing(StreamingContext ctx)
        {
            this.m_cultureData = null;
            this.m_cultureName = null;
        }

        private void OnDeserialized()
        {
            if (this.m_cultureData == null)
            {
                if (this.m_cultureName == null)
                {
                    if (this.customCultureName != null)
                    {
                        this.m_cultureName = this.customCultureName;
                    }
                }

                this.m_cultureData = CultureInfo.GetCultureInfo(m_cultureName).m_cultureData;
                this.m_textInfoName = this.m_cultureData.STEXTINFO;
            }
        }

        private void OnDeserialized(StreamingContext ctx)
        {
            OnDeserialized();
        }

        private void OnSerializing(StreamingContext ctx)
        {
            this.customCultureName = this.m_cultureName;
        }

        internal static int GetHashCodeOrdinalIgnoreCase(String s)
        {
            return GetHashCodeOrdinalIgnoreCase(s, false, 0);
        }

        internal static int GetHashCodeOrdinalIgnoreCase(String s, bool forceRandomizedHashing, long additionalEntropy)
        {
            return (Invariant.GetCaseInsensitiveHashCode(s, forceRandomizedHashing, additionalEntropy));
        }

        internal static unsafe bool TryFastFindStringOrdinalIgnoreCase(int searchFlags, String source, int startIndex, String value, int count, ref int foundIndex)
        {
            return InternalTryFindStringOrdinalIgnoreCase(searchFlags, source, count, startIndex, value, value.Length, ref foundIndex);
        }

        internal static unsafe int CompareOrdinalIgnoreCase(String str1, String str2)
        {
            return InternalCompareStringOrdinalIgnoreCase(str1, 0, str2, 0, str1.Length, str2.Length);
        }

        internal static unsafe int CompareOrdinalIgnoreCaseEx(String strA, int indexA, String strB, int indexB, int lengthA, int lengthB)
        {
                                    return InternalCompareStringOrdinalIgnoreCase(strA, indexA, strB, indexB, lengthA, lengthB);
        }

        internal static int IndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
        {
                                                if (source.Length == 0 && value.Length == 0)
            {
                return 0;
            }

            int ret = -1;
            if (TryFastFindStringOrdinalIgnoreCase(Microsoft.Win32.Win32Native.FIND_FROMSTART, source, startIndex, value, count, ref ret))
                return ret;
            int end = startIndex + count;
            int maxStartIndex = end - value.Length;
            for (; startIndex <= maxStartIndex; startIndex++)
            {
                                if (CompareOrdinalIgnoreCaseEx(source, startIndex, value, 0, value.Length, value.Length) == 0)
                {
                    return startIndex;
                }
            }

            return -1;
        }

        internal static int LastIndexOfStringOrdinalIgnoreCase(String source, String value, int startIndex, int count)
        {
                                                            if (value.Length == 0)
            {
                return startIndex;
            }

            int ret = -1;
            if (TryFastFindStringOrdinalIgnoreCase(Microsoft.Win32.Win32Native.FIND_FROMEND, source, startIndex, value, count, ref ret))
                return ret;
            int minIndex = startIndex - count + 1;
            if (value.Length > 0)
            {
                startIndex -= (value.Length - 1);
            }

            for (; startIndex >= minIndex; startIndex--)
            {
                if (CompareOrdinalIgnoreCaseEx(source, startIndex, value, 0, value.Length, value.Length) == 0)
                {
                    return startIndex;
                }
            }

            return -1;
        }

        public string CultureName
        {
            get
            {
                return (this.m_textInfoName);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return (m_isReadOnly);
            }
        }

        public virtual Object Clone()
        {
            object o = MemberwiseClone();
            ((TextInfo)o).SetReadOnlyState(false);
            return (o);
        }

        public static TextInfo ReadOnly(TextInfo textInfo)
        {
            if (textInfo == null)
            {
                throw new ArgumentNullException("textInfo");
            }

                        if (textInfo.IsReadOnly)
            {
                return (textInfo);
            }

            TextInfo clonedTextInfo = (TextInfo)(textInfo.MemberwiseClone());
            clonedTextInfo.SetReadOnlyState(true);
            return (clonedTextInfo);
        }

        private void VerifyWritable()
        {
            if (m_isReadOnly)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }

                    }

        internal void SetReadOnlyState(bool readOnly)
        {
            m_isReadOnly = readOnly;
        }

        public virtual String ListSeparator
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_listSeparator == null)
                {
                    m_listSeparator = this.m_cultureData.SLIST;
                }

                return (m_listSeparator);
            }

            [System.Runtime.InteropServices.ComVisible(false)]
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
                }

                                VerifyWritable();
                m_listSeparator = value;
            }
        }

        public unsafe virtual char ToLower(char c)
        {
            if (IsAscii(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToLowerAsciiInvariant(c);
            }

            return (InternalChangeCaseChar(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, c, false));
        }

        public unsafe virtual String ToLower(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

                        return InternalChangeCaseString(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, false);
        }

        static private Char ToLowerAsciiInvariant(Char c)
        {
            if ('A' <= c && c <= 'Z')
            {
                c = (Char)(c | 0x20);
            }

            return c;
        }

        public unsafe virtual char ToUpper(char c)
        {
            if (IsAscii(c) && IsAsciiCasingSameAsInvariant)
            {
                return ToUpperAsciiInvariant(c);
            }

            return (InternalChangeCaseChar(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, c, true));
        }

        public unsafe virtual String ToUpper(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

                        return InternalChangeCaseString(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, true);
        }

        static private Char ToUpperAsciiInvariant(Char c)
        {
            if ('a' <= c && c <= 'z')
            {
                c = (Char)(c & ~0x20);
            }

            return c;
        }

        static private bool IsAscii(Char c)
        {
            return c < 0x80;
        }

        private bool IsAsciiCasingSameAsInvariant
        {
            get
            {
                if (m_IsAsciiCasingSameAsInvariant == null)
                {
                    m_IsAsciiCasingSameAsInvariant = CultureInfo.GetCultureInfo(m_textInfoName).CompareInfo.Compare("abcdefghijklmnopqrstuvwxyz", "ABCDEFGHIJKLMNOPQRSTUVWXYZ", CompareOptions.IgnoreCase) == 0;
                }

                return (bool)m_IsAsciiCasingSameAsInvariant;
            }
        }

        public override bool Equals(Object obj)
        {
            TextInfo that = obj as TextInfo;
            if (that != null)
            {
                return this.CultureName.Equals(that.CultureName);
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return (this.CultureName.GetHashCode());
        }

        public override String ToString()
        {
            return ("TextInfo - " + this.m_cultureData.CultureName);
        }

        public bool IsRightToLeft
        {
            get
            {
                return this.m_cultureData.IsRightToLeft;
            }
        }

        internal unsafe int GetCaseInsensitiveHashCode(String str)
        {
            return GetCaseInsensitiveHashCode(str, false, 0);
        }

        internal unsafe int GetCaseInsensitiveHashCode(String str, bool forceRandomizedHashing, long additionalEntropy)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

                        return (InternalGetCaseInsHash(this.m_dataHandle, this.m_handleOrigin, this.m_textInfoName, str, forceRandomizedHashing, additionalEntropy));
        }

        private static unsafe extern char InternalChangeCaseChar(IntPtr handle, IntPtr handleOrigin, String localeName, char ch, bool isToUpper);
        private static unsafe extern String InternalChangeCaseString(IntPtr handle, IntPtr handleOrigin, String localeName, String str, bool isToUpper);
        private static unsafe extern int InternalGetCaseInsHash(IntPtr handle, IntPtr handleOrigin, String localeName, String str, bool forceRandomizedHashing, long additionalEntropy);
        private static unsafe extern int InternalCompareStringOrdinalIgnoreCase(String string1, int index1, String string2, int index2, int length1, int length2);
        private static unsafe extern bool InternalTryFindStringOrdinalIgnoreCase(int searchFlags, String source, int sourceCount, int startIndex, String target, int targetCount, ref int foundIndex);
    }
}