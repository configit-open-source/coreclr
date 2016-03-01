using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Microsoft.Win32;

namespace System
{
    public sealed class String : IComparable, ICloneable, IConvertible, IEnumerable, IComparable<String>, IEnumerable<char>, IEquatable<String>
    {
        private int m_stringLength;
        private char m_firstChar;
        private const int TrimHead = 0;
        private const int TrimTail = 1;
        private const int TrimBoth = 2;
        public static readonly String Empty;
        public static String Join(String separator, params String[] value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            return Join(separator, value, 0, value.Length);
        }

        public static String Join(String separator, params Object[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.EndContractBlock();
            if (values.Length == 0 || values[0] == null)
                return String.Empty;
            if (separator == null)
                separator = String.Empty;
            StringBuilder result = StringBuilderCache.Acquire();
            String value = values[0].ToString();
            if (value != null)
                result.Append(value);
            for (int i = 1; i < values.Length; i++)
            {
                result.Append(separator);
                if (values[i] != null)
                {
                    value = values[i].ToString();
                    if (value != null)
                        result.Append(value);
                }
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        public static String Join<T>(String separator, IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            if (separator == null)
                separator = String.Empty;
            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return String.Empty;
                StringBuilder result = StringBuilderCache.Acquire();
                if (en.Current != null)
                {
                    string value = en.Current.ToString();
                    if (value != null)
                        result.Append(value);
                }

                while (en.MoveNext())
                {
                    result.Append(separator);
                    if (en.Current != null)
                    {
                        string value = en.Current.ToString();
                        if (value != null)
                            result.Append(value);
                    }
                }

                return StringBuilderCache.GetStringAndRelease(result);
            }
        }

        public static String Join(String separator, IEnumerable<String> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            using (IEnumerator<String> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return String.Empty;
                String firstValue = en.Current;
                if (!en.MoveNext())
                {
                    return firstValue ?? String.Empty;
                }

                StringBuilder result = StringBuilderCache.Acquire();
                result.Append(firstValue);
                do
                {
                    result.Append(separator);
                    result.Append(en.Current);
                }
                while (en.MoveNext());
                return StringBuilderCache.GetStringAndRelease(result);
            }
        }

        private const int charPtrAlignConst = 3;
        private const int alignConst = 7;
        internal char FirstChar
        {
            get
            {
                return m_firstChar;
            }
        }

        public unsafe static String Join(String separator, String[] value, int startIndex, int count)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (startIndex > value.Length - count)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();
            if (separator == null)
            {
                separator = String.Empty;
            }

            if (count == 0)
            {
                return String.Empty;
            }

            if (count == 1)
            {
                return value[startIndex] ?? String.Empty;
            }

            int jointLength = 0;
            int endIndex = startIndex + count - 1;
            for (int stringToJoinIndex = startIndex; stringToJoinIndex <= endIndex; stringToJoinIndex++)
            {
                string currentValue = value[stringToJoinIndex];
                if (currentValue != null)
                {
                    jointLength += currentValue.Length;
                }
            }

            jointLength += (count - 1) * separator.Length;
            if ((jointLength < 0) || ((jointLength + 1) < 0))
            {
                throw new OutOfMemoryException();
            }

            if (jointLength == 0)
            {
                return String.Empty;
            }

            string jointString = FastAllocateString(jointLength);
            fixed (char *pointerToJointString = &jointString.m_firstChar)
            {
                UnSafeCharBuffer charBuffer = new UnSafeCharBuffer(pointerToJointString, jointLength);
                charBuffer.AppendString(value[startIndex]);
                for (int stringToJoinIndex = startIndex + 1; stringToJoinIndex <= endIndex; stringToJoinIndex++)
                {
                    charBuffer.AppendString(separator);
                    charBuffer.AppendString(value[stringToJoinIndex]);
                }

                Contract.Assert(*(pointerToJointString + charBuffer.Length) == '\0', "String must be null-terminated!");
            }

            return jointString;
        }

        private unsafe static int CompareOrdinalIgnoreCaseHelper(String strA, String strB)
        {
            Contract.Requires(strA != null);
            Contract.Requires(strB != null);
            Contract.EndContractBlock();
            int length = Math.Min(strA.Length, strB.Length);
            fixed (char *ap = &strA.m_firstChar)
                fixed (char *bp = &strB.m_firstChar)
                {
                    char *a = ap;
                    char *b = bp;
                    while (length != 0)
                    {
                        int charA = *a;
                        int charB = *b;
                        Contract.Assert((charA | charB) <= 0x7F, "strings have to be ASCII");
                        if ((uint)(charA - 'a') <= (uint)('z' - 'a'))
                            charA -= 0x20;
                        if ((uint)(charB - 'a') <= (uint)('z' - 'a'))
                            charB -= 0x20;
                        if (charA != charB)
                            return charA - charB;
                        a++;
                        b++;
                        length--;
                    }

                    return strA.Length - strB.Length;
                }
        }

        internal static extern int nativeCompareOrdinalEx(String strA, int indexA, String strB, int indexB, int count);
        unsafe internal static extern int nativeCompareOrdinalIgnoreCaseWC(String strA, sbyte *strBBytes);
        internal unsafe static string SmallCharToUpper(string strIn)
        {
            Contract.Requires(strIn != null);
            Contract.EndContractBlock();
            int length = strIn.Length;
            String strOut = FastAllocateString(length);
            fixed (char *inBuff = &strIn.m_firstChar, outBuff = &strOut.m_firstChar)
            {
                for (int i = 0; i < length; i++)
                {
                    int c = inBuff[i];
                    Contract.Assert(c <= 0x7F, "string has to be ASCII");
                    if ((uint)(c - 'a') <= (uint)('z' - 'a'))
                        c -= 0x20;
                    outBuff[i] = (char)c;
                }

                Contract.Assert(outBuff[length] == '\0', "outBuff[length]=='\0'");
            }

            return strOut;
        }

        private unsafe static bool EqualsHelper(String strA, String strB)
        {
            Contract.Requires(strA != null);
            Contract.Requires(strB != null);
            Contract.Requires(strA.Length == strB.Length);
            int length = strA.Length;
            fixed (char *ap = &strA.m_firstChar)
                fixed (char *bp = &strB.m_firstChar)
                {
                    char *a = ap;
                    char *b = bp;
                    if (*(int *)a != *(int *)b)
                        return false;
                    length -= 2;
                    a += 2;
                    b += 2;
                    while (length >= 12)
                    {
                        if (*(long *)a != *(long *)b)
                            return false;
                        if (*(long *)(a + 4) != *(long *)(b + 4))
                            return false;
                        if (*(long *)(a + 8) != *(long *)(b + 8))
                            return false;
                        length -= 12;
                        a += 12;
                        b += 12;
                    }

                    while (length > 0)
                    {
                        if (*(int *)a != *(int *)b)
                            break;
                        length -= 2;
                        a += 2;
                        b += 2;
                    }

                    return (length <= 0);
                }
        }

        private unsafe static bool StartsWithOrdinalHelper(String str, String startsWith)
        {
            Contract.Requires(str != null);
            Contract.Requires(startsWith != null);
            Contract.Requires(str.Length >= startsWith.Length);
            int length = startsWith.Length;
            fixed (char *ap = &str.m_firstChar)
                fixed (char *bp = &startsWith.m_firstChar)
                {
                    char *a = ap;
                    char *b = bp;
                    Contract.Assert(length >= 2);
                    if (*(int *)a != *(int *)b)
                        goto ReturnFalse;
                    length -= 2;
                    a += 2;
                    b += 2;
                    while (length >= 12)
                    {
                        if (*(long *)a != *(long *)b)
                            goto ReturnFalse;
                        if (*(long *)(a + 4) != *(long *)(b + 4))
                            goto ReturnFalse;
                        if (*(long *)(a + 8) != *(long *)(b + 8))
                            goto ReturnFalse;
                        length -= 12;
                        a += 12;
                        b += 12;
                    }

                    while (length >= 2)
                    {
                        if (*(int *)a != *(int *)b)
                            goto ReturnFalse;
                        length -= 2;
                        a += 2;
                        b += 2;
                    }

                    return length == 0 | *a == *b;
                    ReturnFalse:
                        return false;
                }
        }

        private unsafe static int CompareOrdinalHelper(String strA, String strB)
        {
            Contract.Requires(strA != null);
            Contract.Requires(strB != null);
            int length = Math.Min(strA.Length, strB.Length);
            int diffOffset = -1;
            fixed (char *ap = &strA.m_firstChar)
                fixed (char *bp = &strB.m_firstChar)
                {
                    char *a = ap;
                    char *b = bp;
                    while (length >= 10)
                    {
                        if (*(int *)a != *(int *)b)
                        {
                            diffOffset = 0;
                            break;
                        }

                        if (*(int *)(a + 2) != *(int *)(b + 2))
                        {
                            diffOffset = 2;
                            break;
                        }

                        if (*(int *)(a + 4) != *(int *)(b + 4))
                        {
                            diffOffset = 4;
                            break;
                        }

                        if (*(int *)(a + 6) != *(int *)(b + 6))
                        {
                            diffOffset = 6;
                            break;
                        }

                        if (*(int *)(a + 8) != *(int *)(b + 8))
                        {
                            diffOffset = 8;
                            break;
                        }

                        length -= 10;
                        a += 10;
                        b += 10;
                    }

                    if (diffOffset != -1)
                    {
                        a += diffOffset;
                        b += diffOffset;
                        int order;
                        if ((order = (int)*a - (int)*b) != 0)
                        {
                            return order;
                        }

                        Contract.Assert(*(a + 1) != *(b + 1), "This byte must be different if we reach here!");
                        return ((int)*(a + 1) - (int)*(b + 1));
                    }

                    while (length > 0)
                    {
                        if (*(int *)a != *(int *)b)
                        {
                            break;
                        }

                        length -= 2;
                        a += 2;
                        b += 2;
                    }

                    if (length > 0)
                    {
                        int c;
                        if ((c = (int)*a - (int)*b) != 0)
                        {
                            return c;
                        }

                        Contract.Assert(*(a + 1) != *(b + 1), "This byte must be different if we reach here!");
                        return ((int)*(a + 1) - (int)*(b + 1));
                    }

                    return strA.Length - strB.Length;
                }
        }

        public override bool Equals(Object obj)
        {
            if (this == null)
                throw new NullReferenceException();
            String str = obj as String;
            if (str == null)
                return false;
            if (Object.ReferenceEquals(this, obj))
                return true;
            if (this.Length != str.Length)
                return false;
            return EqualsHelper(this, str);
        }

        public bool Equals(String value)
        {
            if (this == null)
                throw new NullReferenceException();
            if (value == null)
                return false;
            if (Object.ReferenceEquals(this, value))
                return true;
            if (this.Length != value.Length)
                return false;
            return EqualsHelper(this, value);
        }

        public bool Equals(String value, StringComparison comparisonType)
        {
            if (comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase)
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            Contract.EndContractBlock();
            if ((Object)this == (Object)value)
            {
                return true;
            }

            if ((Object)value == null)
            {
                return false;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return (CultureInfo.CurrentCulture.CompareInfo.Compare(this, value, CompareOptions.None) == 0);
                case StringComparison.CurrentCultureIgnoreCase:
                    return (CultureInfo.CurrentCulture.CompareInfo.Compare(this, value, CompareOptions.IgnoreCase) == 0);
                case StringComparison.InvariantCulture:
                    return (CultureInfo.InvariantCulture.CompareInfo.Compare(this, value, CompareOptions.None) == 0);
                case StringComparison.InvariantCultureIgnoreCase:
                    return (CultureInfo.InvariantCulture.CompareInfo.Compare(this, value, CompareOptions.IgnoreCase) == 0);
                case StringComparison.Ordinal:
                    if (this.Length != value.Length)
                        return false;
                    return EqualsHelper(this, value);
                case StringComparison.OrdinalIgnoreCase:
                    if (this.Length != value.Length)
                        return false;
                    if (this.IsAscii() && value.IsAscii())
                    {
                        return (CompareOrdinalIgnoreCaseHelper(this, value) == 0);
                    }

                    return (TextInfo.CompareOrdinalIgnoreCase(this, value) == 0);
                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
        }

        public static bool Equals(String a, String b)
        {
            if ((Object)a == (Object)b)
            {
                return true;
            }

            if ((Object)a == null || (Object)b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
                return false;
            return EqualsHelper(a, b);
        }

        public static bool Equals(String a, String b, StringComparison comparisonType)
        {
            if (comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase)
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            Contract.EndContractBlock();
            if ((Object)a == (Object)b)
            {
                return true;
            }

            if ((Object)a == null || (Object)b == null)
            {
                return false;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return (CultureInfo.CurrentCulture.CompareInfo.Compare(a, b, CompareOptions.None) == 0);
                case StringComparison.CurrentCultureIgnoreCase:
                    return (CultureInfo.CurrentCulture.CompareInfo.Compare(a, b, CompareOptions.IgnoreCase) == 0);
                case StringComparison.InvariantCulture:
                    return (CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.None) == 0);
                case StringComparison.InvariantCultureIgnoreCase:
                    return (CultureInfo.InvariantCulture.CompareInfo.Compare(a, b, CompareOptions.IgnoreCase) == 0);
                case StringComparison.Ordinal:
                    if (a.Length != b.Length)
                        return false;
                    return EqualsHelper(a, b);
                case StringComparison.OrdinalIgnoreCase:
                    if (a.Length != b.Length)
                        return false;
                    else
                    {
                        if (a.IsAscii() && b.IsAscii())
                        {
                            return (CompareOrdinalIgnoreCaseHelper(a, b) == 0);
                        }

                        return (TextInfo.CompareOrdinalIgnoreCase(a, b) == 0);
                    }

                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
        }

        public static bool operator ==(String a, String b)
        {
            return String.Equals(a, b);
        }

        public static bool operator !=(String a, String b)
        {
            return !String.Equals(a, b);
        }

        public extern char this[int index]
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [System.Security.SecuritySafeCritical]
            get;
        }

        unsafe public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count > Length - sourceIndex)
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            if (destinationIndex > destination.Length - count || destinationIndex < 0)
                throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            Contract.EndContractBlock();
            if (count > 0)
            {
                fixed (char *src = &this.m_firstChar)
                    fixed (char *dest = destination)
                        wstrcpy(dest + destinationIndex, src + sourceIndex, count);
            }
        }

        unsafe public char[] ToCharArray()
        {
            int length = Length;
            char[] chars = new char[length];
            if (length > 0)
            {
                fixed (char *src = &this.m_firstChar)
                    fixed (char *dest = chars)
                    {
                        wstrcpy(dest, src, length);
                    }
            }

            return chars;
        }

        unsafe public char[] ToCharArray(int startIndex, int length)
        {
            if (startIndex < 0 || startIndex > Length || startIndex > Length - length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            char[] chars = new char[length];
            if (length > 0)
            {
                fixed (char *src = &this.m_firstChar)
                    fixed (char *dest = chars)
                    {
                        wstrcpy(dest, src + startIndex, length);
                    }
            }

            return chars;
        }

        public static bool IsNullOrEmpty(String value)
        {
            return (value == null || value.Length == 0);
        }

        public static bool IsNullOrWhiteSpace(String value)
        {
            if (value == null)
                return true;
            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i]))
                    return false;
            }

            return true;
        }

        internal static extern int InternalMarvin32HashString(string s, int strLen, long additionalEntropy);
        internal static bool UseRandomizedHashing()
        {
            return InternalUseRandomizedHashing();
        }

        private static extern bool InternalUseRandomizedHashing();
        public override int GetHashCode()
        {
            if (HashHelpers.s_UseRandomizedStringHashing)
            {
                return InternalMarvin32HashString(this, this.Length, 0);
            }

            unsafe
            {
                fixed (char *src = &m_firstChar)
                {
                    Contract.Assert(src[this.Length] == '\0', "src[this.Length] == '\\0'");
                    Contract.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");
                    int hash1 = 5381;
                    int hash2 = hash1;
                    int c;
                    char *s = src;
                    while ((c = s[0]) != 0)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = s[1];
                        if (c == 0)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        s += 2;
                    }

                    hash1 ^= ThisAssembly.DailyBuildNumber;
                    return hash1 + (hash2 * 1566083941);
                }
            }
        }

        internal int GetLegacyNonRandomizedHashCode()
        {
            unsafe
            {
                fixed (char *src = &m_firstChar)
                {
                    Contract.Assert(src[this.Length] == '\0', "src[this.Length] == '\\0'");
                    Contract.Assert(((int)src) % 4 == 0, "Managed string should start at 4 bytes boundary");
                    int hash1 = 5381;
                    int hash2 = hash1;
                    int c;
                    char *s = src;
                    while ((c = s[0]) != 0)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = s[1];
                        if (c == 0)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        s += 2;
                    }

                    hash1 ^= ThisAssembly.DailyBuildNumber;
                    return hash1 + (hash2 * 1566083941);
                }
            }
        }

        public extern int Length
        {
            [System.Security.SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        public String[] Split(params char[] separator)
        {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, StringSplitOptions.None);
        }

        public string[] Split(char[] separator, int count)
        {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, count, StringSplitOptions.None);
        }

        public String[] Split(char[] separator, StringSplitOptions options)
        {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, options);
        }

        public String[] Split(char[] separator, int count, StringSplitOptions options)
        {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, count, options);
        }

        internal String[] SplitInternal(char[] separator, int count, StringSplitOptions options)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (options < StringSplitOptions.None || options > StringSplitOptions.RemoveEmptyEntries)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", options));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();
            bool omitEmptyEntries = (options == StringSplitOptions.RemoveEmptyEntries);
            if ((count == 0) || (omitEmptyEntries && this.Length == 0))
            {
                return EmptyArray<String>.Value;
            }

            if (count == 1)
            {
                return new String[]{this};
            }

            int[] sepList = new int[Length];
            int numReplaces = MakeSeparatorList(separator, ref sepList);
            if (0 == numReplaces)
            {
                return new String[]{this};
            }

            if (omitEmptyEntries)
            {
                return InternalSplitOmitEmptyEntries(sepList, null, numReplaces, count);
            }
            else
            {
                return InternalSplitKeepEmptyEntries(sepList, null, numReplaces, count);
            }
        }

        public String[] Split(String[] separator, StringSplitOptions options)
        {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return Split(separator, Int32.MaxValue, options);
        }

        public String[] Split(String[] separator, Int32 count, StringSplitOptions options)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }

            if (options < StringSplitOptions.None || options > StringSplitOptions.RemoveEmptyEntries)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options));
            }

            Contract.EndContractBlock();
            bool omitEmptyEntries = (options == StringSplitOptions.RemoveEmptyEntries);
            if (separator == null || separator.Length == 0)
            {
                return SplitInternal((char[])null, count, options);
            }

            if ((count == 0) || (omitEmptyEntries && this.Length == 0))
            {
                return EmptyArray<String>.Value;
            }

            if (count == 1)
            {
                return new String[]{this};
            }

            int[] sepList = new int[Length];
            int[] lengthList = new int[Length];
            int numReplaces = MakeSeparatorList(separator, ref sepList, ref lengthList);
            if (0 == numReplaces)
            {
                return new String[]{this};
            }

            if (omitEmptyEntries)
            {
                return InternalSplitOmitEmptyEntries(sepList, lengthList, numReplaces, count);
            }
            else
            {
                return InternalSplitKeepEmptyEntries(sepList, lengthList, numReplaces, count);
            }
        }

        private String[] InternalSplitKeepEmptyEntries(Int32[] sepList, Int32[] lengthList, Int32 numReplaces, int count)
        {
            Contract.Requires(numReplaces >= 0);
            Contract.Requires(count >= 2);
            Contract.Ensures(Contract.Result<String[]>() != null);
            int currIndex = 0;
            int arrIndex = 0;
            count--;
            int numActualReplaces = (numReplaces < count) ? numReplaces : count;
            String[] splitStrings = new String[numActualReplaces + 1];
            for (int i = 0; i < numActualReplaces && currIndex < Length; i++)
            {
                splitStrings[arrIndex++] = Substring(currIndex, sepList[i] - currIndex);
                currIndex = sepList[i] + ((lengthList == null) ? 1 : lengthList[i]);
            }

            if (currIndex < Length && numActualReplaces >= 0)
            {
                splitStrings[arrIndex] = Substring(currIndex);
            }
            else if (arrIndex == numActualReplaces)
            {
                splitStrings[arrIndex] = String.Empty;
            }

            return splitStrings;
        }

        private String[] InternalSplitOmitEmptyEntries(Int32[] sepList, Int32[] lengthList, Int32 numReplaces, int count)
        {
            Contract.Requires(numReplaces >= 0);
            Contract.Requires(count >= 2);
            Contract.Ensures(Contract.Result<String[]>() != null);
            int maxItems = (numReplaces < count) ? (numReplaces + 1) : count;
            String[] splitStrings = new String[maxItems];
            int currIndex = 0;
            int arrIndex = 0;
            for (int i = 0; i < numReplaces && currIndex < Length; i++)
            {
                if (sepList[i] - currIndex > 0)
                {
                    splitStrings[arrIndex++] = Substring(currIndex, sepList[i] - currIndex);
                }

                currIndex = sepList[i] + ((lengthList == null) ? 1 : lengthList[i]);
                if (arrIndex == count - 1)
                {
                    while (i < numReplaces - 1 && currIndex == sepList[++i])
                    {
                        currIndex += ((lengthList == null) ? 1 : lengthList[i]);
                    }

                    break;
                }
            }

            Contract.Assert(arrIndex < maxItems);
            if (currIndex < Length)
            {
                splitStrings[arrIndex++] = Substring(currIndex);
            }

            String[] stringArray = splitStrings;
            if (arrIndex != maxItems)
            {
                stringArray = new String[arrIndex];
                for (int j = 0; j < arrIndex; j++)
                {
                    stringArray[j] = splitStrings[j];
                }
            }

            return stringArray;
        }

        private unsafe int MakeSeparatorList(char[] separator, ref int[] sepList)
        {
            int foundCount = 0;
            if (separator == null || separator.Length == 0)
            {
                fixed (char *pwzChars = &this.m_firstChar)
                {
                    for (int i = 0; i < Length && foundCount < sepList.Length; i++)
                    {
                        if (Char.IsWhiteSpace(pwzChars[i]))
                        {
                            sepList[foundCount++] = i;
                        }
                    }
                }
            }
            else
            {
                int sepListCount = sepList.Length;
                int sepCount = separator.Length;
                fixed (char *pwzChars = &this.m_firstChar, pSepChars = separator)
                {
                    for (int i = 0; i < Length && foundCount < sepListCount; i++)
                    {
                        char *pSep = pSepChars;
                        for (int j = 0; j < sepCount; j++, pSep++)
                        {
                            if (pwzChars[i] == *pSep)
                            {
                                sepList[foundCount++] = i;
                                break;
                            }
                        }
                    }
                }
            }

            return foundCount;
        }

        private unsafe int MakeSeparatorList(String[] separators, ref int[] sepList, ref int[] lengthList)
        {
            Contract.Assert(separators != null && separators.Length > 0, "separators != null && separators.Length > 0");
            int foundCount = 0;
            int sepListCount = sepList.Length;
            int sepCount = separators.Length;
            fixed (char *pwzChars = &this.m_firstChar)
            {
                for (int i = 0; i < Length && foundCount < sepListCount; i++)
                {
                    for (int j = 0; j < separators.Length; j++)
                    {
                        String separator = separators[j];
                        if (String.IsNullOrEmpty(separator))
                        {
                            continue;
                        }

                        Int32 currentSepLength = separator.Length;
                        if (pwzChars[i] == separator[0] && currentSepLength <= Length - i)
                        {
                            if (currentSepLength == 1 || String.CompareOrdinal(this, i, separator, 0, currentSepLength) == 0)
                            {
                                sepList[foundCount] = i;
                                lengthList[foundCount] = currentSepLength;
                                foundCount++;
                                i += currentSepLength - 1;
                                break;
                            }
                        }
                    }
                }
            }

            return foundCount;
        }

        public String Substring(int startIndex)
        {
            return this.Substring(startIndex, Length - startIndex);
        }

        public String Substring(int startIndex, int length)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (startIndex > Length)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndexLargerThanLength"));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex > Length - length)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }

            Contract.EndContractBlock();
            if (length == 0)
            {
                return String.Empty;
            }

            if (startIndex == 0 && length == this.Length)
            {
                return this;
            }

            return InternalSubString(startIndex, length);
        }

        unsafe string InternalSubString(int startIndex, int length)
        {
            Contract.Assert(startIndex >= 0 && startIndex <= this.Length, "StartIndex is out of range!");
            Contract.Assert(length >= 0 && startIndex <= this.Length - length, "length is out of range!");
            String result = FastAllocateString(length);
            fixed (char *dest = &result.m_firstChar)
                fixed (char *src = &this.m_firstChar)
                {
                    wstrcpy(dest, src + startIndex, length);
                }

            return result;
        }

        public String Trim(params char[] trimChars)
        {
            if (null == trimChars || trimChars.Length == 0)
            {
                return TrimHelper(TrimBoth);
            }

            return TrimHelper(trimChars, TrimBoth);
        }

        public String TrimStart(params char[] trimChars)
        {
            if (null == trimChars || trimChars.Length == 0)
            {
                return TrimHelper(TrimHead);
            }

            return TrimHelper(trimChars, TrimHead);
        }

        public String TrimEnd(params char[] trimChars)
        {
            if (null == trimChars || trimChars.Length == 0)
            {
                return TrimHelper(TrimTail);
            }

            return TrimHelper(trimChars, TrimTail);
        }

        unsafe public extern String(char *value);
        unsafe public extern String(char *value, int startIndex, int length);
        unsafe public extern String(sbyte *value);
        unsafe public extern String(sbyte *value, int startIndex, int length);
        unsafe public extern String(sbyte *value, int startIndex, int length, Encoding enc);
        unsafe static private String CreateString(sbyte *value, int startIndex, int length, Encoding enc)
        {
            if (enc == null)
                return new String(value, startIndex, length);
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if ((value + startIndex) < value)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }

            byte[] b = new byte[length];
            try
            {
                Buffer.Memcpy(b, 0, (byte *)value, startIndex, length);
            }
            catch (NullReferenceException)
            {
                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }

            return enc.GetString(b);
        }

        unsafe static internal String CreateStringFromEncoding(byte *bytes, int byteLength, Encoding encoding)
        {
            Contract.Requires(bytes != null);
            Contract.Requires(byteLength >= 0);
            int stringLength = encoding.GetCharCount(bytes, byteLength, null);
            Contract.Assert(stringLength >= 0, "stringLength >= 0");
            if (stringLength == 0)
                return String.Empty;
            String s = FastAllocateString(stringLength);
            fixed (char *pTempChars = &s.m_firstChar)
            {
                int doubleCheck = encoding.GetChars(bytes, byteLength, pTempChars, stringLength, null);
                Contract.Assert(stringLength == doubleCheck, "Expected encoding.GetChars to return same length as encoding.GetCharCount");
            }

            return s;
        }

        unsafe internal int ConvertToAnsi(byte *pbNativeBuffer, int cbNativeBuffer, bool fBestFit, bool fThrowOnUnmappableChar)
        {
            Contract.Assert(cbNativeBuffer >= (Length + 1) * Marshal.SystemMaxDBCSCharSize, "Insufficient buffer length passed to ConvertToAnsi");
            const uint CP_ACP = 0;
            int nb;
            const uint WC_NO_BEST_FIT_CHARS = 0x00000400;
            uint flgs = (fBestFit ? 0 : WC_NO_BEST_FIT_CHARS);
            uint DefaultCharUsed = 0;
            fixed (char *pwzChar = &this.m_firstChar)
            {
                nb = Win32Native.WideCharToMultiByte(CP_ACP, flgs, pwzChar, this.Length, pbNativeBuffer, cbNativeBuffer, IntPtr.Zero, (fThrowOnUnmappableChar ? new IntPtr(&DefaultCharUsed) : IntPtr.Zero));
            }

            if (0 != DefaultCharUsed)
            {
                throw new ArgumentException(Environment.GetResourceString("Interop_Marshal_Unmappable_Char"));
            }

            pbNativeBuffer[nb] = 0;
            return nb;
        }

        public bool IsNormalized()
        {
            return IsNormalized((NormalizationForm)ExtendedNormalizationForms.FormIdna);
        }

        public bool IsNormalized(NormalizationForm normalizationForm)
        {
            return Normalization.IsNormalized(this, normalizationForm);
        }

        public String Normalize()
        {
            return Normalize((NormalizationForm)ExtendedNormalizationForms.FormIdna);
        }

        public String Normalize(NormalizationForm normalizationForm)
        {
            return Normalization.Normalize(this, normalizationForm);
        }

        internal extern static String FastAllocateString(int length);
        unsafe private static void FillStringChecked(String dest, int destPos, String src)
        {
            Contract.Requires(dest != null);
            Contract.Requires(src != null);
            if (src.Length > dest.Length - destPos)
            {
                throw new IndexOutOfRangeException();
            }

            Contract.EndContractBlock();
            fixed (char *pDest = &dest.m_firstChar)
                fixed (char *pSrc = &src.m_firstChar)
                {
                    wstrcpy(pDest + destPos, pSrc, src.Length);
                }
        }

        public extern String(char[] value, int startIndex, int length);
        public extern String(char[] value);
        internal static unsafe void wstrcpy(char *dmem, char *smem, int charCount)
        {
            Buffer.Memcpy((byte *)dmem, (byte *)smem, charCount * 2);
        }

        private String CtorCharArray(char[] value)
        {
            if (value != null && value.Length != 0)
            {
                String result = FastAllocateString(value.Length);
                unsafe
                {
                    fixed (char *dest = &result.m_firstChar, source = value)
                    {
                        wstrcpy(dest, source, value.Length);
                    }
                }

                return result;
            }
            else
                return String.Empty;
        }

        private String CtorCharArrayStartLength(char[] value, int startIndex, int length)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            if (startIndex > value.Length - length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            if (length > 0)
            {
                String result = FastAllocateString(length);
                unsafe
                {
                    fixed (char *dest = &result.m_firstChar, source = value)
                    {
                        wstrcpy(dest, source + startIndex, length);
                    }
                }

                return result;
            }
            else
                return String.Empty;
        }

        private String CtorCharCount(char c, int count)
        {
            if (count > 0)
            {
                String result = FastAllocateString(count);
                if (c != 0)
                {
                    unsafe
                    {
                        fixed (char *dest = &result.m_firstChar)
                        {
                            char *dmem = dest;
                            while (((uint)dmem & 3) != 0 && count > 0)
                            {
                                *dmem++ = c;
                                count--;
                            }

                            uint cc = (uint)((c << 16) | c);
                            if (count >= 4)
                            {
                                count -= 4;
                                do
                                {
                                    ((uint *)dmem)[0] = cc;
                                    ((uint *)dmem)[1] = cc;
                                    dmem += 4;
                                    count -= 4;
                                }
                                while (count >= 0);
                            }

                            if ((count & 2) != 0)
                            {
                                ((uint *)dmem)[0] = cc;
                                dmem += 2;
                            }

                            if ((count & 1) != 0)
                                dmem[0] = c;
                        }
                    }
                }

                return result;
            }
            else if (count == 0)
                return String.Empty;
            else
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "count"));
        }

        private static unsafe int wcslen(char *ptr)
        {
            char *end = ptr;
            while (((uint)end & 3) != 0 && *end != 0)
                end++;
            if (*end != 0)
            {
                while ((end[0] & end[1]) != 0 || (end[0] != 0 && end[1] != 0))
                {
                    end += 2;
                }
            }

            for (; *end != 0; end++)
                ;
            int count = (int)(end - ptr);
            return count;
        }

        private unsafe String CtorCharPtr(char *ptr)
        {
            if (ptr == null)
                return String.Empty;
            if (ptr < (char *)64000)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeStringPtrNotAtom"));
            Contract.Assert(this == null, "this == null");
            try
            {
                int count = wcslen(ptr);
                if (count == 0)
                    return String.Empty;
                String result = FastAllocateString(count);
                fixed (char *dest = &result.m_firstChar)
                    wstrcpy(dest, ptr, count);
                return result;
            }
            catch (NullReferenceException)
            {
                throw new ArgumentOutOfRangeException("ptr", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }
        }

        private unsafe String CtorCharPtrStartLength(char *ptr, int startIndex, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            Contract.EndContractBlock();
            Contract.Assert(this == null, "this == null");
            char *pFrom = ptr + startIndex;
            if (pFrom < ptr)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }

            if (length == 0)
                return String.Empty;
            String result = FastAllocateString(length);
            try
            {
                fixed (char *dest = &result.m_firstChar)
                    wstrcpy(dest, pFrom, length);
                return result;
            }
            catch (NullReferenceException)
            {
                throw new ArgumentOutOfRangeException("ptr", Environment.GetResourceString("ArgumentOutOfRange_PartialWCHAR"));
            }
        }

        public extern String(char c, int count);
        public static int Compare(String strA, String strB)
        {
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.None);
        }

        public static int Compare(String strA, String strB, bool ignoreCase)
        {
            if (ignoreCase)
            {
                return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreCase);
            }

            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.None);
        }

        public static int Compare(String strA, String strB, StringComparison comparisonType)
        {
            if ((uint)(comparisonType - StringComparison.CurrentCulture) > (uint)(StringComparison.OrdinalIgnoreCase - StringComparison.CurrentCulture))
            {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }

            Contract.EndContractBlock();
            if ((Object)strA == (Object)strB)
            {
                return 0;
            }

            if (strA == null)
            {
                return -1;
            }

            if (strB == null)
            {
                return 1;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreCase);
                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(strA, strB, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreCase);
                case StringComparison.Ordinal:
                    if ((strA.m_firstChar - strB.m_firstChar) != 0)
                    {
                        return strA.m_firstChar - strB.m_firstChar;
                    }

                    return CompareOrdinalHelper(strA, strB);
                case StringComparison.OrdinalIgnoreCase:
                    if (strA.IsAscii() && strB.IsAscii())
                    {
                        return (CompareOrdinalIgnoreCaseHelper(strA, strB));
                    }

                    return TextInfo.CompareOrdinalIgnoreCase(strA, strB);
                default:
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_StringComparison"));
            }
        }

        public static int Compare(String strA, String strB, CultureInfo culture, CompareOptions options)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            Contract.EndContractBlock();
            return culture.CompareInfo.Compare(strA, strB, options);
        }

        public static int Compare(String strA, String strB, bool ignoreCase, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            Contract.EndContractBlock();
            if (ignoreCase)
            {
                return culture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreCase);
            }

            return culture.CompareInfo.Compare(strA, strB, CompareOptions.None);
        }

        public static int Compare(String strA, int indexA, String strB, int indexB, int length)
        {
            int lengthA = length;
            int lengthB = length;
            if (strA != null)
            {
                if (strA.Length - indexA < lengthA)
                {
                    lengthA = (strA.Length - indexA);
                }
            }

            if (strB != null)
            {
                if (strB.Length - indexB < lengthB)
                {
                    lengthB = (strB.Length - indexB);
                }
            }

            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.None);
        }

        public static int Compare(String strA, int indexA, String strB, int indexB, int length, bool ignoreCase)
        {
            int lengthA = length;
            int lengthB = length;
            if (strA != null)
            {
                if (strA.Length - indexA < lengthA)
                {
                    lengthA = (strA.Length - indexA);
                }
            }

            if (strB != null)
            {
                if (strB.Length - indexB < lengthB)
                {
                    lengthB = (strB.Length - indexB);
                }
            }

            if (ignoreCase)
            {
                return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.IgnoreCase);
            }

            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.None);
        }

        public static int Compare(String strA, int indexA, String strB, int indexB, int length, bool ignoreCase, CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            Contract.EndContractBlock();
            int lengthA = length;
            int lengthB = length;
            if (strA != null)
            {
                if (strA.Length - indexA < lengthA)
                {
                    lengthA = (strA.Length - indexA);
                }
            }

            if (strB != null)
            {
                if (strB.Length - indexB < lengthB)
                {
                    lengthB = (strB.Length - indexB);
                }
            }

            if (ignoreCase)
            {
                return culture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.IgnoreCase);
            }
            else
            {
                return culture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.None);
            }
        }

        public static int Compare(String strA, int indexA, String strB, int indexB, int length, CultureInfo culture, CompareOptions options)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            Contract.EndContractBlock();
            int lengthA = length;
            int lengthB = length;
            if (strA != null)
            {
                if (strA.Length - indexA < lengthA)
                {
                    lengthA = (strA.Length - indexA);
                }
            }

            if (strB != null)
            {
                if (strB.Length - indexB < lengthB)
                {
                    lengthB = (strB.Length - indexB);
                }
            }

            return culture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, options);
        }

        public static int Compare(String strA, int indexA, String strB, int indexB, int length, StringComparison comparisonType)
        {
            if (comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase)
            {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }

            Contract.EndContractBlock();
            if (strA == null || strB == null)
            {
                if ((Object)strA == (Object)strB)
                {
                    return 0;
                }

                return (strA == null) ? -1 : 1;
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (indexA < 0)
            {
                throw new ArgumentOutOfRangeException("indexA", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (indexB < 0)
            {
                throw new ArgumentOutOfRangeException("indexB", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (strA.Length - indexA < 0)
            {
                throw new ArgumentOutOfRangeException("indexA", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (strB.Length - indexB < 0)
            {
                throw new ArgumentOutOfRangeException("indexB", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if ((length == 0) || ((strA == strB) && (indexA == indexB)))
            {
                return 0;
            }

            int lengthA = length;
            int lengthB = length;
            if (strA != null)
            {
                if (strA.Length - indexA < lengthA)
                {
                    lengthA = (strA.Length - indexA);
                }
            }

            if (strB != null)
            {
                if (strB.Length - indexB < lengthB)
                {
                    lengthB = (strB.Length - indexB);
                }
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.IgnoreCase);
                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(strA, indexA, lengthA, strB, indexB, lengthB, CompareOptions.IgnoreCase);
                case StringComparison.Ordinal:
                    return nativeCompareOrdinalEx(strA, indexA, strB, indexB, length);
                case StringComparison.OrdinalIgnoreCase:
                    return (TextInfo.CompareOrdinalIgnoreCaseEx(strA, indexA, strB, indexB, lengthA, lengthB));
                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"));
            }
        }

        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            if (!(value is String))
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeString"));
            }

            return String.Compare(this, (String)value, StringComparison.CurrentCulture);
        }

        public int CompareTo(String strB)
        {
            if (strB == null)
            {
                return 1;
            }

            return CultureInfo.CurrentCulture.CompareInfo.Compare(this, strB, 0);
        }

        public static int CompareOrdinal(String strA, String strB)
        {
            if ((Object)strA == (Object)strB)
            {
                return 0;
            }

            if (strA == null)
            {
                return -1;
            }

            if (strB == null)
            {
                return 1;
            }

            if ((strA.m_firstChar - strB.m_firstChar) != 0)
            {
                return strA.m_firstChar - strB.m_firstChar;
            }

            return CompareOrdinalHelper(strA, strB);
        }

        public static int CompareOrdinal(String strA, int indexA, String strB, int indexB, int length)
        {
            if (strA == null || strB == null)
            {
                if ((Object)strA == (Object)strB)
                {
                    return 0;
                }

                return (strA == null) ? -1 : 1;
            }

            return nativeCompareOrdinalEx(strA, indexA, strB, indexB, length);
        }

        public bool Contains(string value)
        {
            return (IndexOf(value, StringComparison.Ordinal) >= 0);
        }

        public Boolean EndsWith(String value)
        {
            return EndsWith(value, StringComparison.CurrentCulture);
        }

        public Boolean EndsWith(String value, StringComparison comparisonType)
        {
            if ((Object)value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase)
            {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }

            Contract.EndContractBlock();
            if ((Object)this == (Object)value)
            {
                return true;
            }

            if (value.Length == 0)
            {
                return true;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.IsSuffix(this, value, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IsSuffix(this, value, CompareOptions.IgnoreCase);
                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.IsSuffix(this, value, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.IsSuffix(this, value, CompareOptions.IgnoreCase);
                case StringComparison.Ordinal:
                    return this.Length < value.Length ? false : (nativeCompareOrdinalEx(this, this.Length - value.Length, value, 0, value.Length) == 0);
                case StringComparison.OrdinalIgnoreCase:
                    return this.Length < value.Length ? false : (TextInfo.CompareOrdinalIgnoreCaseEx(this, this.Length - value.Length, value, 0, value.Length, value.Length) == 0);
                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
        }

        public Boolean EndsWith(String value, Boolean ignoreCase, CultureInfo culture)
        {
            if (null == value)
            {
                throw new ArgumentNullException("value");
            }

            Contract.EndContractBlock();
            if ((object)this == (object)value)
            {
                return true;
            }

            CultureInfo referenceCulture;
            if (culture == null)
                referenceCulture = CultureInfo.CurrentCulture;
            else
                referenceCulture = culture;
            return referenceCulture.CompareInfo.IsSuffix(this, value, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        internal bool EndsWith(char value)
        {
            int thisLen = this.Length;
            if (thisLen != 0)
            {
                if (this[thisLen - 1] == value)
                    return true;
            }

            return false;
        }

        public int IndexOf(char value)
        {
            return IndexOf(value, 0, this.Length);
        }

        public int IndexOf(char value, int startIndex)
        {
            return IndexOf(value, startIndex, this.Length - startIndex);
        }

        public extern int IndexOf(char value, int startIndex, int count);
        public int IndexOfAny(char[] anyOf)
        {
            return IndexOfAny(anyOf, 0, this.Length);
        }

        public int IndexOfAny(char[] anyOf, int startIndex)
        {
            return IndexOfAny(anyOf, startIndex, this.Length - startIndex);
        }

        public extern int IndexOfAny(char[] anyOf, int startIndex, int count);
        public int IndexOf(String value)
        {
            return IndexOf(value, StringComparison.CurrentCulture);
        }

        public int IndexOf(String value, int startIndex)
        {
            return IndexOf(value, startIndex, StringComparison.CurrentCulture);
        }

        public int IndexOf(String value, int startIndex, int count)
        {
            if (startIndex < 0 || startIndex > this.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || count > this.Length - startIndex)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            }

            Contract.EndContractBlock();
            return IndexOf(value, startIndex, count, StringComparison.CurrentCulture);
        }

        public int IndexOf(String value, StringComparison comparisonType)
        {
            return IndexOf(value, 0, this.Length, comparisonType);
        }

        public int IndexOf(String value, int startIndex, StringComparison comparisonType)
        {
            return IndexOf(value, startIndex, this.Length - startIndex, comparisonType);
        }

        public int IndexOf(String value, int startIndex, int count, StringComparison comparisonType)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count < 0 || startIndex > this.Length - count)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            Contract.EndContractBlock();
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                case StringComparison.Ordinal:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.Ordinal);
                case StringComparison.OrdinalIgnoreCase:
                    if (value.IsAscii() && this.IsAscii())
                        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                    else
                        return TextInfo.IndexOfStringOrdinalIgnoreCase(this, value, startIndex, count);
                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
        }

        public int LastIndexOf(char value)
        {
            return LastIndexOf(value, this.Length - 1, this.Length);
        }

        public int LastIndexOf(char value, int startIndex)
        {
            return LastIndexOf(value, startIndex, startIndex + 1);
        }

        public extern int LastIndexOf(char value, int startIndex, int count);
        public int LastIndexOfAny(char[] anyOf)
        {
            return LastIndexOfAny(anyOf, this.Length - 1, this.Length);
        }

        public int LastIndexOfAny(char[] anyOf, int startIndex)
        {
            return LastIndexOfAny(anyOf, startIndex, startIndex + 1);
        }

        public extern int LastIndexOfAny(char[] anyOf, int startIndex, int count);
        public int LastIndexOf(String value)
        {
            return LastIndexOf(value, this.Length - 1, this.Length, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(String value, int startIndex)
        {
            return LastIndexOf(value, startIndex, startIndex + 1, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(String value, int startIndex, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            }

            Contract.EndContractBlock();
            return LastIndexOf(value, startIndex, count, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(String value, StringComparison comparisonType)
        {
            return LastIndexOf(value, this.Length - 1, this.Length, comparisonType);
        }

        public int LastIndexOf(String value, int startIndex, StringComparison comparisonType)
        {
            return LastIndexOf(value, startIndex, startIndex + 1, comparisonType);
        }

        public int LastIndexOf(String value, int startIndex, int count, StringComparison comparisonType)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            if (this.Length == 0 && (startIndex == -1 || startIndex == 0))
                return (value.Length == 0) ? 0 : -1;
            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (startIndex == this.Length)
            {
                startIndex--;
                if (count > 0)
                    count--;
                if (value.Length == 0 && count >= 0 && startIndex - count + 1 >= 0)
                    return startIndex;
            }

            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                case StringComparison.Ordinal:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.Ordinal);
                case StringComparison.OrdinalIgnoreCase:
                    if (value.IsAscii() && this.IsAscii())
                        return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                    else
                        return TextInfo.LastIndexOfStringOrdinalIgnoreCase(this, value, startIndex, count);
                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
        }

        public String PadLeft(int totalWidth)
        {
            return PadHelper(totalWidth, ' ', false);
        }

        public String PadLeft(int totalWidth, char paddingChar)
        {
            return PadHelper(totalWidth, paddingChar, false);
        }

        public String PadRight(int totalWidth)
        {
            return PadHelper(totalWidth, ' ', true);
        }

        public String PadRight(int totalWidth, char paddingChar)
        {
            return PadHelper(totalWidth, paddingChar, true);
        }

        private extern String PadHelper(int totalWidth, char paddingChar, bool isRightPadded);
        public Boolean StartsWith(String value)
        {
            if ((Object)value == null)
            {
                throw new ArgumentNullException("value");
            }

            Contract.EndContractBlock();
            return StartsWith(value, StringComparison.CurrentCulture);
        }

        public Boolean StartsWith(String value, StringComparison comparisonType)
        {
            if ((Object)value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (comparisonType < StringComparison.CurrentCulture || comparisonType > StringComparison.OrdinalIgnoreCase)
            {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }

            Contract.EndContractBlock();
            if ((Object)this == (Object)value)
            {
                return true;
            }

            if (value.Length == 0)
            {
                return true;
            }

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.IsPrefix(this, value, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IsPrefix(this, value, CompareOptions.IgnoreCase);
                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(this, value, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(this, value, CompareOptions.IgnoreCase);
                case StringComparison.Ordinal:
                    if (this.Length < value.Length || m_firstChar != value.m_firstChar)
                    {
                        return false;
                    }

                    return (value.Length == 1) ? true : StartsWithOrdinalHelper(this, value);
                case StringComparison.OrdinalIgnoreCase:
                    if (this.Length < value.Length)
                    {
                        return false;
                    }

                    return (TextInfo.CompareOrdinalIgnoreCaseEx(this, 0, value, 0, value.Length, value.Length) == 0);
                default:
                    throw new ArgumentException(Environment.GetResourceString("NotSupported_StringComparison"), "comparisonType");
            }
        }

        public Boolean StartsWith(String value, Boolean ignoreCase, CultureInfo culture)
        {
            if (null == value)
            {
                throw new ArgumentNullException("value");
            }

            Contract.EndContractBlock();
            if ((object)this == (object)value)
            {
                return true;
            }

            CultureInfo referenceCulture;
            if (culture == null)
                referenceCulture = CultureInfo.CurrentCulture;
            else
                referenceCulture = culture;
            return referenceCulture.CompareInfo.IsPrefix(this, value, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public String ToLower()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToLower(CultureInfo.CurrentCulture);
        }

        public String ToLower(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return culture.TextInfo.ToLower(this);
        }

        public String ToLowerInvariant()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToLower(CultureInfo.InvariantCulture);
        }

        public String ToUpper()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToUpper(CultureInfo.CurrentCulture);
        }

        public String ToUpper(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException("culture");
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return culture.TextInfo.ToUpper(this);
        }

        public String ToUpperInvariant()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToUpper(CultureInfo.InvariantCulture);
        }

        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this;
        }

        public String ToString(IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this;
        }

        public Object Clone()
        {
            Contract.Ensures(Contract.Result<Object>() != null);
            Contract.EndContractBlock();
            return this;
        }

        private static bool IsBOMWhitespace(char c)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && c == '\xFEFF')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public String Trim()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return TrimHelper(TrimBoth);
        }

        private String TrimHelper(int trimType)
        {
            int end = this.Length - 1;
            int start = 0;
            if (trimType != TrimTail)
            {
                for (start = 0; start < this.Length; start++)
                {
                    if (!Char.IsWhiteSpace(this[start]) && !IsBOMWhitespace(this[start]))
                        break;
                }
            }

            if (trimType != TrimHead)
            {
                for (end = Length - 1; end >= start; end--)
                {
                    if (!Char.IsWhiteSpace(this[end]) && !IsBOMWhitespace(this[start]))
                        break;
                }
            }

            return CreateTrimmedString(start, end);
        }

        private String TrimHelper(char[] trimChars, int trimType)
        {
            int end = this.Length - 1;
            int start = 0;
            if (trimType != TrimTail)
            {
                for (start = 0; start < this.Length; start++)
                {
                    int i = 0;
                    char ch = this[start];
                    for (i = 0; i < trimChars.Length; i++)
                    {
                        if (trimChars[i] == ch)
                            break;
                    }

                    if (i == trimChars.Length)
                    {
                        break;
                    }
                }
            }

            if (trimType != TrimHead)
            {
                for (end = Length - 1; end >= start; end--)
                {
                    int i = 0;
                    char ch = this[end];
                    for (i = 0; i < trimChars.Length; i++)
                    {
                        if (trimChars[i] == ch)
                            break;
                    }

                    if (i == trimChars.Length)
                    {
                        break;
                    }
                }
            }

            return CreateTrimmedString(start, end);
        }

        private String CreateTrimmedString(int start, int end)
        {
            int len = end - start + 1;
            if (len == this.Length)
            {
                return this;
            }

            if (len == 0)
            {
                return String.Empty;
            }

            return InternalSubString(start, len);
        }

        public String Insert(int startIndex, String value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length + value.Length);
            Contract.EndContractBlock();
            int oldLength = Length;
            int insertLength = value.Length;
            int newLength = oldLength + insertLength;
            if (newLength == 0)
                return String.Empty;
            String result = FastAllocateString(newLength);
            unsafe
            {
                fixed (char *srcThis = &m_firstChar)
                {
                    fixed (char *srcInsert = &value.m_firstChar)
                    {
                        fixed (char *dst = &result.m_firstChar)
                        {
                            wstrcpy(dst, srcThis, startIndex);
                            wstrcpy(dst + startIndex, srcInsert, insertLength);
                            wstrcpy(dst + startIndex + insertLength, srcThis + startIndex, oldLength - startIndex);
                        }
                    }
                }
            }

            return result;
        }

        private extern String ReplaceInternal(char oldChar, char newChar);
        public String Replace(char oldChar, char newChar)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length);
            Contract.EndContractBlock();
            return ReplaceInternal(oldChar, newChar);
        }

        private extern String ReplaceInternal(String oldValue, String newValue);
        public String Replace(String oldValue, String newValue)
        {
            if (oldValue == null)
                throw new ArgumentNullException("oldValue");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            string s = ReplaceInternal(oldValue, newValue);
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhoneMango)
            {
                int i = s.IndexOf('\0');
                if (i > 0)
                    return s.Substring(0, i);
                else
                    return s;
            }
            else
            {
                return s;
            }
        }

        public String Remove(int startIndex, int count)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (count > Length - startIndex)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length - count);
            Contract.EndContractBlock();
            int newLength = Length - count;
            if (newLength == 0)
                return String.Empty;
            String result = FastAllocateString(newLength);
            unsafe
            {
                fixed (char *src = &m_firstChar)
                {
                    fixed (char *dst = &result.m_firstChar)
                    {
                        wstrcpy(dst, src, startIndex);
                        wstrcpy(dst + startIndex, src + startIndex + count, newLength - startIndex);
                    }
                }
            }

            return result;
        }

        public string Remove(int startIndex)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (startIndex >= Length)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndexLessThanLength"));
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return Substring(0, startIndex);
        }

        public static String Format(String format, Object arg0)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0));
        }

        public static String Format(String format, Object arg0, Object arg1)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0, arg1));
        }

        public static String Format(String format, Object arg0, Object arg1, Object arg2)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));
        }

        public static String Format(String format, params Object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return FormatHelper(null, format, new ParamsArray(args));
        }

        public static String Format(IFormatProvider provider, String format, Object arg0)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0));
        }

        public static String Format(IFormatProvider provider, String format, Object arg0, Object arg1)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0, arg1));
        }

        public static String Format(IFormatProvider provider, String format, Object arg0, Object arg1, Object arg2)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));
        }

        public static String Format(IFormatProvider provider, String format, params Object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return FormatHelper(provider, format, new ParamsArray(args));
        }

        private static String FormatHelper(IFormatProvider provider, String format, ParamsArray args)
        {
            if (format == null)
                throw new ArgumentNullException("format");
            return StringBuilderCache.GetStringAndRelease(StringBuilderCache.Acquire(format.Length + args.Length * 8).AppendFormatHelper(provider, format, args));
        }

        unsafe public static String Copy(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            int length = str.Length;
            String result = FastAllocateString(length);
            fixed (char *dest = &result.m_firstChar)
                fixed (char *src = &str.m_firstChar)
                {
                    wstrcpy(dest, src, length);
                }

            return result;
        }

        public static String Concat(Object arg0)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            if (arg0 == null)
            {
                return String.Empty;
            }

            return arg0.ToString();
        }

        public static String Concat(Object arg0, Object arg1)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            if (arg0 == null)
            {
                arg0 = String.Empty;
            }

            if (arg1 == null)
            {
                arg1 = String.Empty;
            }

            return Concat(arg0.ToString(), arg1.ToString());
        }

        public static String Concat(Object arg0, Object arg1, Object arg2)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            if (arg0 == null)
            {
                arg0 = String.Empty;
            }

            if (arg1 == null)
            {
                arg1 = String.Empty;
            }

            if (arg2 == null)
            {
                arg2 = String.Empty;
            }

            return Concat(arg0.ToString(), arg1.ToString(), arg2.ToString());
        }

        public static String Concat(Object arg0, Object arg1, Object arg2, Object arg3, __arglist)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            Object[] objArgs;
            int argCount;
            ArgIterator args = new ArgIterator(__arglist);
            argCount = args.GetRemainingCount() + 4;
            objArgs = new Object[argCount];
            objArgs[0] = arg0;
            objArgs[1] = arg1;
            objArgs[2] = arg2;
            objArgs[3] = arg3;
            for (int i = 4; i < argCount; i++)
            {
                objArgs[i] = TypedReference.ToObject(args.GetNextArg());
            }

            return Concat(objArgs);
        }

        public static String Concat(params Object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            String[] sArgs = new String[args.Length];
            int totalLength = 0;
            for (int i = 0; i < args.Length; i++)
            {
                object value = args[i];
                sArgs[i] = ((value == null) ? (String.Empty) : (value.ToString()));
                if (sArgs[i] == null)
                    sArgs[i] = String.Empty;
                totalLength += sArgs[i].Length;
                if (totalLength < 0)
                {
                    throw new OutOfMemoryException();
                }
            }

            return ConcatArray(sArgs, totalLength);
        }

        public static String Concat<T>(IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            StringBuilder result = StringBuilderCache.Acquire();
            using (IEnumerator<T> en = values.GetEnumerator())
            {
                while (en.MoveNext())
                {
                    if (en.Current != null)
                    {
                        string value = en.Current.ToString();
                        if (value != null)
                            result.Append(value);
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        public static String Concat(IEnumerable<String> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            StringBuilder result = StringBuilderCache.Acquire();
            using (IEnumerator<String> en = values.GetEnumerator())
            {
                while (en.MoveNext())
                {
                    if (en.Current != null)
                    {
                        result.Append(en.Current);
                    }
                }
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        public static String Concat(String str0, String str1)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == (str0 == null ? 0 : str0.Length) + (str1 == null ? 0 : str1.Length));
            Contract.EndContractBlock();
            if (IsNullOrEmpty(str0))
            {
                if (IsNullOrEmpty(str1))
                {
                    return String.Empty;
                }

                return str1;
            }

            if (IsNullOrEmpty(str1))
            {
                return str0;
            }

            int str0Length = str0.Length;
            String result = FastAllocateString(str0Length + str1.Length);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0Length, str1);
            return result;
        }

        public static String Concat(String str0, String str1, String str2)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == (str0 == null ? 0 : str0.Length) + (str1 == null ? 0 : str1.Length) + (str2 == null ? 0 : str2.Length));
            Contract.EndContractBlock();
            if (str0 == null && str1 == null && str2 == null)
            {
                return String.Empty;
            }

            if (str0 == null)
            {
                str0 = String.Empty;
            }

            if (str1 == null)
            {
                str1 = String.Empty;
            }

            if (str2 == null)
            {
                str2 = String.Empty;
            }

            int totalLength = str0.Length + str1.Length + str2.Length;
            String result = FastAllocateString(totalLength);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0.Length, str1);
            FillStringChecked(result, str0.Length + str1.Length, str2);
            return result;
        }

        public static String Concat(String str0, String str1, String str2, String str3)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == (str0 == null ? 0 : str0.Length) + (str1 == null ? 0 : str1.Length) + (str2 == null ? 0 : str2.Length) + (str3 == null ? 0 : str3.Length));
            Contract.EndContractBlock();
            if (str0 == null && str1 == null && str2 == null && str3 == null)
            {
                return String.Empty;
            }

            if (str0 == null)
            {
                str0 = String.Empty;
            }

            if (str1 == null)
            {
                str1 = String.Empty;
            }

            if (str2 == null)
            {
                str2 = String.Empty;
            }

            if (str3 == null)
            {
                str3 = String.Empty;
            }

            int totalLength = str0.Length + str1.Length + str2.Length + str3.Length;
            String result = FastAllocateString(totalLength);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0.Length, str1);
            FillStringChecked(result, str0.Length + str1.Length, str2);
            FillStringChecked(result, str0.Length + str1.Length + str2.Length, str3);
            return result;
        }

        private static String ConcatArray(String[] values, int totalLength)
        {
            String result = FastAllocateString(totalLength);
            int currPos = 0;
            for (int i = 0; i < values.Length; i++)
            {
                Contract.Assert((currPos <= totalLength - values[i].Length), "[String.ConcatArray](currPos <= totalLength - values[i].Length)");
                FillStringChecked(result, currPos, values[i]);
                currPos += values[i].Length;
            }

            return result;
        }

        public static String Concat(params String[] values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            int totalLength = 0;
            String[] internalValues = new String[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                internalValues[i] = ((value == null) ? (String.Empty) : (value));
                totalLength += internalValues[i].Length;
                if (totalLength < 0)
                {
                    throw new OutOfMemoryException();
                }
            }

            return ConcatArray(internalValues, totalLength);
        }

        public static String Intern(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            Contract.Ensures(Contract.Result<String>().Length == str.Length);
            Contract.Ensures(str.Equals(Contract.Result<String>()));
            Contract.EndContractBlock();
            return Thread.GetDomain().GetOrInternString(str);
        }

        public static String IsInterned(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            Contract.Ensures(Contract.Result<String>() == null || Contract.Result<String>().Length == str.Length);
            Contract.EndContractBlock();
            return Thread.GetDomain().IsStringInterned(str);
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.String;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(this, provider);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(this, provider);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(this, provider);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(this, provider);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(this, provider);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(this, provider);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(this, provider);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(this, provider);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(this, provider);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(this, provider);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(this, provider);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(this, provider);
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(this, provider);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return Convert.ToDateTime(this, provider);
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        internal extern bool IsFastSort();
        internal extern bool IsAscii();
        internal extern void SetTrailByte(byte data);
        internal extern bool TryGetTrailByte(out byte data);
        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            Contract.Ensures(Contract.Result<IEnumerator<char>>() != null);
            Contract.EndContractBlock();
            BCLDebug.Perf(false, "Avoid using String's CharEnumerator until C# special cases foreach on String - use the indexed property on String instead.");
            return new CharEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Contract.Ensures(Contract.Result<IEnumerator>() != null);
            Contract.EndContractBlock();
            BCLDebug.Perf(false, "Avoid using String's CharEnumerator until C# special cases foreach on String - use the indexed property on String instead.");
            return new CharEnumerator(this);
        }

        internal unsafe static void InternalCopy(String src, IntPtr dest, int len)
        {
            if (len == 0)
                return;
            fixed (char *charPtr = &src.m_firstChar)
            {
                byte *srcPtr = (byte *)charPtr;
                byte *dstPtr = (byte *)dest;
                Buffer.Memcpy(dstPtr, srcPtr, len);
            }
        }
    }

    [Flags]
    public enum StringSplitOptions
    {
        None = 0,
        RemoveEmptyEntries = 1
    }
}