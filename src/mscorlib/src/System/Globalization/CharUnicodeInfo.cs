namespace System.Globalization
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Reflection;
    using System.Security;
    using System.Diagnostics.Contracts;

    public static class CharUnicodeInfo
    {
        internal const char HIGH_SURROGATE_START = '\ud800';
        internal const char HIGH_SURROGATE_END = '\udbff';
        internal const char LOW_SURROGATE_START = '\udc00';
        internal const char LOW_SURROGATE_END = '\udfff';
        internal const int UNICODE_CATEGORY_OFFSET = 0;
        internal const int BIDI_CATEGORY_OFFSET = 1;
        static bool s_initialized = InitTable();
        unsafe static ushort *s_pCategoryLevel1Index;
        unsafe static byte *s_pCategoriesValue;
        unsafe static ushort *s_pNumericLevel1Index;
        unsafe static byte *s_pNumericValues;
        unsafe static DigitValues*s_pDigitValues;
        internal const String UNICODE_INFO_FILE_NAME = "charinfo.nlp";
        internal const int UNICODE_PLANE01_START = 0x10000;
        internal unsafe struct UnicodeDataHeader
        {
            internal char TableName;
            internal ushort version;
            internal uint OffsetToCategoriesIndex;
            internal uint OffsetToCategoriesValue;
            internal uint OffsetToNumbericIndex;
            internal uint OffsetToDigitValue;
            internal uint OffsetToNumbericValue;
        }

        internal struct DigitValues
        {
            internal sbyte decimalDigit;
            internal sbyte digit;
        }

        unsafe static bool InitTable()
        {
            byte *pDataTable = GlobalizationAssembly.GetGlobalizationResourceBytePtr(typeof (CharUnicodeInfo).Assembly, UNICODE_INFO_FILE_NAME);
            UnicodeDataHeader*mainHeader = (UnicodeDataHeader*)pDataTable;
            s_pCategoryLevel1Index = (ushort *)(pDataTable + mainHeader->OffsetToCategoriesIndex);
            s_pCategoriesValue = (byte *)(pDataTable + mainHeader->OffsetToCategoriesValue);
            s_pNumericLevel1Index = (ushort *)(pDataTable + mainHeader->OffsetToNumbericIndex);
            s_pNumericValues = (byte *)(pDataTable + mainHeader->OffsetToNumbericValue);
            s_pDigitValues = (DigitValues*)(pDataTable + mainHeader->OffsetToDigitValue);
            return true;
        }

        internal static int InternalConvertToUtf32(String s, int index)
        {
            Contract.Assert(s != null, "s != null");
            Contract.Assert(index >= 0 && index < s.Length, "index < s.Length");
            if (index < s.Length - 1)
            {
                int temp1 = (int)s[index] - HIGH_SURROGATE_START;
                if (temp1 >= 0 && temp1 <= 0x3ff)
                {
                    int temp2 = (int)s[index + 1] - LOW_SURROGATE_START;
                    if (temp2 >= 0 && temp2 <= 0x3ff)
                    {
                        return ((temp1 * 0x400) + temp2 + UNICODE_PLANE01_START);
                    }
                }
            }

            return ((int)s[index]);
        }

        internal static int InternalConvertToUtf32(String s, int index, out int charLength)
        {
            Contract.Assert(s != null, "s != null");
            Contract.Assert(s.Length > 0, "s.Length > 0");
            Contract.Assert(index >= 0 && index < s.Length, "index >= 0 && index < s.Length");
            charLength = 1;
            if (index < s.Length - 1)
            {
                int temp1 = (int)s[index] - HIGH_SURROGATE_START;
                if (temp1 >= 0 && temp1 <= 0x3ff)
                {
                    int temp2 = (int)s[index + 1] - LOW_SURROGATE_START;
                    if (temp2 >= 0 && temp2 <= 0x3ff)
                    {
                        charLength++;
                        return ((temp1 * 0x400) + temp2 + UNICODE_PLANE01_START);
                    }
                }
            }

            return ((int)s[index]);
        }

        internal static bool IsWhiteSpace(String s, int index)
        {
            Contract.Assert(s != null, "s!=null");
            Contract.Assert(index >= 0 && index < s.Length, "index >= 0 && index < s.Length");
            UnicodeCategory uc = GetUnicodeCategory(s, index);
            switch (uc)
            {
                case (UnicodeCategory.SpaceSeparator):
                case (UnicodeCategory.LineSeparator):
                case (UnicodeCategory.ParagraphSeparator):
                    return (true);
            }

            return (false);
        }

        internal static bool IsWhiteSpace(char c)
        {
            UnicodeCategory uc = GetUnicodeCategory(c);
            switch (uc)
            {
                case (UnicodeCategory.SpaceSeparator):
                case (UnicodeCategory.LineSeparator):
                case (UnicodeCategory.ParagraphSeparator):
                    return (true);
            }

            return (false);
        }

        internal unsafe static double InternalGetNumericValue(int ch)
        {
            Contract.Assert(ch >= 0 && ch <= 0x10ffff, "ch is not in valid Unicode range.");
            ushort index = s_pNumericLevel1Index[ch >> 8];
            index = s_pNumericLevel1Index[index + ((ch >> 4) & 0x000f)];
            byte *pBytePtr = (byte *)&(s_pNumericLevel1Index[index]);
            byte *pSourcePtr = &(s_pNumericValues[pBytePtr[(ch & 0x000f)] * sizeof (double)]);
            if (((long)pSourcePtr % 8) != 0)
            {
                double ret;
                byte *retPtr = (byte *)&ret;
                Buffer.Memcpy(retPtr, pSourcePtr, sizeof (double));
                return (ret);
            }

            return (((double *)s_pNumericValues)[pBytePtr[(ch & 0x000f)]]);
        }

        internal unsafe static DigitValues*InternalGetDigitValues(int ch)
        {
            Contract.Assert(ch >= 0 && ch <= 0x10ffff, "ch is not in valid Unicode range.");
            ushort index = s_pNumericLevel1Index[ch >> 8];
            index = s_pNumericLevel1Index[index + ((ch >> 4) & 0x000f)];
            byte *pBytePtr = (byte *)&(s_pNumericLevel1Index[index]);
            return &(s_pDigitValues[pBytePtr[(ch & 0x000f)]]);
        }

        internal unsafe static sbyte InternalGetDecimalDigitValue(int ch)
        {
            return (InternalGetDigitValues(ch)->decimalDigit);
        }

        internal unsafe static sbyte InternalGetDigitValue(int ch)
        {
            return (InternalGetDigitValues(ch)->digit);
        }

        public static double GetNumericValue(char ch)
        {
            return (InternalGetNumericValue(ch));
        }

        public static double GetNumericValue(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            Contract.EndContractBlock();
            return (InternalGetNumericValue(InternalConvertToUtf32(s, index)));
        }

        public static int GetDecimalDigitValue(char ch)
        {
            return (InternalGetDecimalDigitValue(ch));
        }

        public static int GetDecimalDigitValue(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            Contract.EndContractBlock();
            return (InternalGetDecimalDigitValue(InternalConvertToUtf32(s, index)));
        }

        public static int GetDigitValue(char ch)
        {
            return (InternalGetDigitValue(ch));
        }

        public static int GetDigitValue(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            Contract.EndContractBlock();
            return (InternalGetDigitValue(InternalConvertToUtf32(s, index)));
        }

        public static UnicodeCategory GetUnicodeCategory(char ch)
        {
            return (InternalGetUnicodeCategory(ch));
        }

        public static UnicodeCategory GetUnicodeCategory(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

            Contract.EndContractBlock();
            return InternalGetUnicodeCategory(s, index);
        }

        internal unsafe static UnicodeCategory InternalGetUnicodeCategory(int ch)
        {
            return ((UnicodeCategory)InternalGetCategoryValue(ch, UNICODE_CATEGORY_OFFSET));
        }

        internal unsafe static byte InternalGetCategoryValue(int ch, int offset)
        {
            Contract.Assert(ch >= 0 && ch <= 0x10ffff, "ch is not in valid Unicode range.");
            ushort index = s_pCategoryLevel1Index[ch >> 8];
            index = s_pCategoryLevel1Index[index + ((ch >> 4) & 0x000f)];
            byte *pBytePtr = (byte *)&(s_pCategoryLevel1Index[index]);
            byte valueIndex = pBytePtr[(ch & 0x000f)];
            byte uc = s_pCategoriesValue[valueIndex * 2 + offset];
            return (uc);
        }

        internal static BidiCategory GetBidiCategory(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

            Contract.EndContractBlock();
            return ((BidiCategory)InternalGetCategoryValue(InternalConvertToUtf32(s, index), BIDI_CATEGORY_OFFSET));
        }

        internal static UnicodeCategory InternalGetUnicodeCategory(String value, int index)
        {
            Contract.Assert(value != null, "value can not be null");
            Contract.Assert(index < value.Length, "index < value.Length");
            return (InternalGetUnicodeCategory(InternalConvertToUtf32(value, index)));
        }

        internal static UnicodeCategory InternalGetUnicodeCategory(String str, int index, out int charLength)
        {
            Contract.Assert(str != null, "str can not be null");
            Contract.Assert(str.Length > 0, "str.Length > 0");
            ;
            Contract.Assert(index >= 0 && index < str.Length, "index >= 0 && index < str.Length");
            return (InternalGetUnicodeCategory(InternalConvertToUtf32(str, index, out charLength)));
        }

        internal static bool IsCombiningCategory(UnicodeCategory uc)
        {
            Contract.Assert(uc >= 0, "uc >= 0");
            return (uc == UnicodeCategory.NonSpacingMark || uc == UnicodeCategory.SpacingCombiningMark || uc == UnicodeCategory.EnclosingMark);
        }
    }
}