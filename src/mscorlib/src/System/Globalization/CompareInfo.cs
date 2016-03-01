namespace System.Globalization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Threading;
    using System.Security.Permissions;
    using Microsoft.Win32;
    using System.Security;
    using System.Security.Principal;
    using System.Diagnostics.Contracts;

    [Flags]
    public enum CompareOptions
    {
        None = 0x00000000,
        IgnoreCase = 0x00000001,
        IgnoreNonSpace = 0x00000002,
        IgnoreSymbols = 0x00000004,
        IgnoreKanaType = 0x00000008,
        IgnoreWidth = 0x00000010,
        OrdinalIgnoreCase = 0x10000000,
        StringSort = 0x20000000,
        Ordinal = 0x40000000
    }

    public class CompareInfo
    {
        private const CompareOptions ValidIndexMaskOffFlags = ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType);
        private const CompareOptions ValidCompareMaskOffFlags = ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.StringSort);
        private const CompareOptions ValidHashCodeOfStringMaskOffFlags = ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType);
        private String m_name;
        private String m_sortName;
        private IntPtr m_dataHandle;
        private IntPtr m_handleOrigin;
        internal CompareInfo(CultureInfo culture)
        {
            this.m_name = culture.m_name;
            this.m_sortName = culture.SortName;
            IntPtr handleOrigin;
            this.m_dataHandle = InternalInitSortHandle(m_sortName, out handleOrigin);
            this.m_handleOrigin = handleOrigin;
        }

        public static CompareInfo GetCompareInfo(String name, Assembly assembly)
        {
            if (name == null || assembly == null)
            {
                throw new ArgumentNullException(name == null ? "name" : "assembly");
            }

            Contract.EndContractBlock();
            if (assembly != typeof (Object).Module.Assembly)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_OnlyMscorlib"));
            }

            return GetCompareInfo(name);
        }

        public static CompareInfo GetCompareInfo(String name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            Contract.EndContractBlock();
            return CultureInfo.GetCultureInfo(name).CompareInfo;
        }

        public static bool IsSortable(char ch)
        {
            return (IsSortable(ch.ToString()));
        }

        public static bool IsSortable(String text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (0 == text.Length)
            {
                return (false);
            }

            CompareInfo c = CultureInfo.InvariantCulture.CompareInfo;
            return (InternalIsSortable(c.m_dataHandle, c.m_handleOrigin, c.m_sortName, text, text.Length));
        }

        public virtual String Name
        {
            get
            {
                Contract.Assert(m_name != null, "CompareInfo.Name Expected m_name to be set");
                if (m_name == "zh-CHT" || m_name == "zh-CHS")
                {
                    return m_name;
                }

                return (m_sortName);
            }
        }

        private const int LINGUISTIC_IGNORECASE = 0x00000010;
        private const int NORM_IGNORECASE = 0x00000001;
        private const int NORM_IGNOREKANATYPE = 0x00010000;
        private const int LINGUISTIC_IGNOREDIACRITIC = 0x00000020;
        private const int NORM_IGNORENONSPACE = 0x00000002;
        private const int NORM_IGNORESYMBOLS = 0x00000004;
        private const int NORM_IGNOREWIDTH = 0x00020000;
        private const int SORT_STRINGSORT = 0x00001000;
        private const int COMPARE_OPTIONS_ORDINAL = 0x40000000;
        internal const int NORM_LINGUISTIC_CASING = 0x08000000;
        private const int RESERVED_FIND_ASCII_STRING = 0x20000000;
        internal static int GetNativeCompareFlags(CompareOptions options)
        {
            Contract.Assert(options != CompareOptions.OrdinalIgnoreCase, "[CompareInfo.GetNativeCompareFlags]CompareOptions.OrdinalIgnoreCase should be handled separately");
            int nativeCompareFlags = NORM_LINGUISTIC_CASING;
            if ((options & CompareOptions.IgnoreCase) != 0)
            {
                nativeCompareFlags |= NORM_IGNORECASE;
            }

            if ((options & CompareOptions.IgnoreKanaType) != 0)
            {
                nativeCompareFlags |= NORM_IGNOREKANATYPE;
            }

            if ((options & CompareOptions.IgnoreNonSpace) != 0)
            {
                nativeCompareFlags |= NORM_IGNORENONSPACE;
            }

            if ((options & CompareOptions.IgnoreSymbols) != 0)
            {
                nativeCompareFlags |= NORM_IGNORESYMBOLS;
            }

            if ((options & CompareOptions.IgnoreWidth) != 0)
            {
                nativeCompareFlags |= NORM_IGNOREWIDTH;
            }

            if ((options & CompareOptions.StringSort) != 0)
            {
                nativeCompareFlags |= SORT_STRINGSORT;
            }

            if (options == CompareOptions.Ordinal)
            {
                nativeCompareFlags = COMPARE_OPTIONS_ORDINAL;
            }

            Contract.Assert(((options & ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreWidth | CompareOptions.StringSort)) == 0) || (options == CompareOptions.Ordinal), "[CompareInfo.GetNativeCompareFlags]Expected all flags to be handled");
            Contract.Assert((nativeCompareFlags & RESERVED_FIND_ASCII_STRING) == 0, "[CompareInfo.GetNativeCompareFlags] RESERVED_FIND_ASCII_STRING shouldn't be set here");
            return nativeCompareFlags;
        }

        public virtual int Compare(String string1, String string2)
        {
            return (Compare(string1, string2, CompareOptions.None));
        }

        public unsafe virtual int Compare(String string1, String string2, CompareOptions options)
        {
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return String.Compare(string1, string2, StringComparison.OrdinalIgnoreCase);
            }

            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_CompareOptionOrdinal"), "options");
                }

                return String.CompareOrdinal(string1, string2);
            }

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            if (string1 == null)
            {
                if (string2 == null)
                {
                    return (0);
                }

                return (-1);
            }

            if (string2 == null)
            {
                return (1);
            }

            return InternalCompareString(m_dataHandle, m_handleOrigin, m_sortName, string1, 0, string1.Length, string2, 0, string2.Length, GetNativeCompareFlags(options));
        }

        public unsafe virtual int Compare(String string1, int offset1, int length1, String string2, int offset2, int length2)
        {
            return Compare(string1, offset1, length1, string2, offset2, length2, 0);
        }

        public unsafe virtual int Compare(String string1, int offset1, String string2, int offset2, CompareOptions options)
        {
            return Compare(string1, offset1, string1 == null ? 0 : string1.Length - offset1, string2, offset2, string2 == null ? 0 : string2.Length - offset2, options);
        }

        public unsafe virtual int Compare(String string1, int offset1, String string2, int offset2)
        {
            return Compare(string1, offset1, string2, offset2, 0);
        }

        public unsafe virtual int Compare(String string1, int offset1, int length1, String string2, int offset2, int length2, CompareOptions options)
        {
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                int result = String.Compare(string1, offset1, string2, offset2, length1 < length2 ? length1 : length2, StringComparison.OrdinalIgnoreCase);
                if ((length1 != length2) && result == 0)
                    return (length1 > length2 ? 1 : -1);
                return (result);
            }

            if (length1 < 0 || length2 < 0)
            {
                throw new ArgumentOutOfRangeException((length1 < 0) ? "length1" : "length2", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            if (offset1 < 0 || offset2 < 0)
            {
                throw new ArgumentOutOfRangeException((offset1 < 0) ? "offset1" : "offset2", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            if (offset1 > (string1 == null ? 0 : string1.Length) - length1)
            {
                throw new ArgumentOutOfRangeException("string1", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
            }

            if (offset2 > (string2 == null ? 0 : string2.Length) - length2)
            {
                throw new ArgumentOutOfRangeException("string2", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
            }

            if ((options & CompareOptions.Ordinal) != 0)
            {
                if (options != CompareOptions.Ordinal)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_CompareOptionOrdinal"), "options");
                }
            }
            else if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            if (string1 == null)
            {
                if (string2 == null)
                {
                    return (0);
                }

                return (-1);
            }

            if (string2 == null)
            {
                return (1);
            }

            if (options == CompareOptions.Ordinal)
            {
                return CompareOrdinal(string1, offset1, length1, string2, offset2, length2);
            }

            return InternalCompareString(this.m_dataHandle, this.m_handleOrigin, this.m_sortName, string1, offset1, length1, string2, offset2, length2, GetNativeCompareFlags(options));
        }

        private static int CompareOrdinal(string string1, int offset1, int length1, string string2, int offset2, int length2)
        {
            int result = String.nativeCompareOrdinalEx(string1, offset1, string2, offset2, (length1 < length2 ? length1 : length2));
            if ((length1 != length2) && result == 0)
            {
                return (length1 > length2 ? 1 : -1);
            }

            return (result);
        }

        public unsafe virtual bool IsPrefix(String source, String prefix, CompareOptions options)
        {
            if (source == null || prefix == null)
            {
                throw new ArgumentNullException((source == null ? "source" : "prefix"), Environment.GetResourceString("ArgumentNull_String"));
            }

            Contract.EndContractBlock();
            int prefixLen = prefix.Length;
            if (prefixLen == 0)
            {
                return (true);
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.StartsWith(prefix, StringComparison.Ordinal);
            }

            if ((options & ValidIndexMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            return (InternalFindNLSStringEx(m_dataHandle, m_handleOrigin, m_sortName, GetNativeCompareFlags(options) | Win32Native.FIND_STARTSWITH | ((source.IsAscii() && prefix.IsAscii()) ? RESERVED_FIND_ASCII_STRING : 0), source, source.Length, 0, prefix, prefix.Length) > -1);
        }

        public virtual bool IsPrefix(String source, String prefix)
        {
            return (IsPrefix(source, prefix, 0));
        }

        public unsafe virtual bool IsSuffix(String source, String suffix, CompareOptions options)
        {
            if (source == null || suffix == null)
            {
                throw new ArgumentNullException((source == null ? "source" : "suffix"), Environment.GetResourceString("ArgumentNull_String"));
            }

            Contract.EndContractBlock();
            int suffixLen = suffix.Length;
            if (suffixLen == 0)
            {
                return (true);
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.EndsWith(suffix, StringComparison.Ordinal);
            }

            if ((options & ValidIndexMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            return InternalFindNLSStringEx(m_dataHandle, m_handleOrigin, m_sortName, GetNativeCompareFlags(options) | Win32Native.FIND_ENDSWITH | ((source.IsAscii() && suffix.IsAscii()) ? RESERVED_FIND_ASCII_STRING : 0), source, source.Length, source.Length - 1, suffix, suffix.Length) >= 0;
        }

        public virtual bool IsSuffix(String source, String suffix)
        {
            return (IsSuffix(source, suffix, 0));
        }

        public unsafe virtual int IndexOf(String source, char value)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return IndexOf(source, value, 0, source.Length, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(String source, String value)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return IndexOf(source, value, 0, source.Length, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(String source, char value, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return IndexOf(source, value, 0, source.Length, options);
        }

        public unsafe virtual int IndexOf(String source, String value, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return IndexOf(source, value, 0, source.Length, options);
        }

        public unsafe virtual int IndexOf(String source, char value, int startIndex)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(String source, String value, int startIndex)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return IndexOf(source, value, startIndex, source.Length - startIndex, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(String source, char value, int startIndex, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return IndexOf(source, value, startIndex, source.Length - startIndex, options);
        }

        public unsafe virtual int IndexOf(String source, String value, int startIndex, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return IndexOf(source, value, startIndex, source.Length - startIndex, options);
        }

        public unsafe virtual int IndexOf(String source, char value, int startIndex, int count)
        {
            return IndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(String source, String value, int startIndex, int count)
        {
            return IndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public unsafe virtual int IndexOf(String source, char value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count < 0 || startIndex > source.Length - count)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            Contract.EndContractBlock();
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.IndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            return InternalFindNLSStringEx(m_dataHandle, m_handleOrigin, m_sortName, GetNativeCompareFlags(options) | Win32Native.FIND_FROMSTART | ((source.IsAscii() && (value <= '\x007f')) ? RESERVED_FIND_ASCII_STRING : 0), source, count, startIndex, new String(value, 1), 1);
        }

        public unsafe virtual int IndexOf(String source, String value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex > source.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            Contract.EndContractBlock();
            if (source.Length == 0)
            {
                if (value.Length == 0)
                {
                    return 0;
                }

                return -1;
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || startIndex > source.Length - count)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.IndexOf(value, startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            return InternalFindNLSStringEx(m_dataHandle, m_handleOrigin, m_sortName, GetNativeCompareFlags(options) | Win32Native.FIND_FROMSTART | ((source.IsAscii() && value.IsAscii()) ? RESERVED_FIND_ASCII_STRING : 0), source, count, startIndex, value, value.Length);
        }

        public unsafe virtual int LastIndexOf(String source, char value)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return LastIndexOf(source, value, source.Length - 1, source.Length, CompareOptions.None);
        }

        public virtual int LastIndexOf(String source, String value)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return LastIndexOf(source, value, source.Length - 1, source.Length, CompareOptions.None);
        }

        public virtual int LastIndexOf(String source, char value, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return LastIndexOf(source, value, source.Length - 1, source.Length, options);
        }

        public unsafe virtual int LastIndexOf(String source, String value, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            return LastIndexOf(source, value, source.Length - 1, source.Length, options);
        }

        public unsafe virtual int LastIndexOf(String source, char value, int startIndex)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
        }

        public unsafe virtual int LastIndexOf(String source, String value, int startIndex)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, CompareOptions.None);
        }

        public unsafe virtual int LastIndexOf(String source, char value, int startIndex, CompareOptions options)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, options);
        }

        public unsafe virtual int LastIndexOf(String source, String value, int startIndex, CompareOptions options)
        {
            return LastIndexOf(source, value, startIndex, startIndex + 1, options);
        }

        public unsafe virtual int LastIndexOf(String source, char value, int startIndex, int count)
        {
            return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public unsafe virtual int LastIndexOf(String source, String value, int startIndex, int count)
        {
            return LastIndexOf(source, value, startIndex, count, CompareOptions.None);
        }

        public unsafe virtual int LastIndexOf(String source, char value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Contract.EndContractBlock();
            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal) && (options != CompareOptions.OrdinalIgnoreCase))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
                return -1;
            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (startIndex == source.Length)
            {
                startIndex--;
                if (count > 0)
                    count--;
            }

            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.LastIndexOf(value.ToString(), startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            return InternalFindNLSStringEx(m_dataHandle, m_handleOrigin, m_sortName, GetNativeCompareFlags(options) | Win32Native.FIND_FROMEND | ((source.IsAscii() && (value <= '\x007f')) ? RESERVED_FIND_ASCII_STRING : 0), source, count, startIndex, new String(value, 1), 1);
        }

        public unsafe virtual int LastIndexOf(String source, String value, int startIndex, int count, CompareOptions options)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            if ((options & ValidIndexMaskOffFlags) != 0 && (options != CompareOptions.Ordinal) && (options != CompareOptions.OrdinalIgnoreCase))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            if (source.Length == 0 && (startIndex == -1 || startIndex == 0))
                return (value.Length == 0) ? 0 : -1;
            if (startIndex < 0 || startIndex > source.Length)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (startIndex == source.Length)
            {
                startIndex--;
                if (count > 0)
                    count--;
                if (value.Length == 0 && count >= 0 && startIndex - count + 1 >= 0)
                    return startIndex;
            }

            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return source.LastIndexOf(value, startIndex, count, StringComparison.OrdinalIgnoreCase);
            }

            return InternalFindNLSStringEx(m_dataHandle, m_handleOrigin, m_sortName, GetNativeCompareFlags(options) | Win32Native.FIND_FROMEND | ((source.IsAscii() && value.IsAscii()) ? RESERVED_FIND_ASCII_STRING : 0), source, count, startIndex, value, value.Length);
        }

        public unsafe virtual SortKey GetSortKey(String source, CompareOptions options)
        {
            return CreateSortKey(source, options);
        }

        public unsafe virtual SortKey GetSortKey(String source)
        {
            return CreateSortKey(source, CompareOptions.None);
        }

        private SortKey CreateSortKey(String source, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            Contract.EndContractBlock();
            const CompareOptions ValidSortkeyCtorMaskOffFlags = ~(CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType | CompareOptions.StringSort);
            if ((options & ValidSortkeyCtorMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            byte[] keyData = null;
            if (String.IsNullOrEmpty(source))
            {
                keyData = EmptyArray<Byte>.Value;
                source = "\x0000";
            }

            int flags = GetNativeCompareFlags(options);
            int length = InternalGetSortKey(m_dataHandle, m_handleOrigin, m_sortName, flags, source, source.Length, null, 0);
            if (length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "source");
            }

            if (keyData == null)
            {
                keyData = new byte[length];
                length = InternalGetSortKey(m_dataHandle, m_handleOrigin, m_sortName, flags, source, source.Length, keyData, keyData.Length);
            }
            else
            {
                source = String.Empty;
            }

            return new SortKey(Name, source, options, keyData);
        }

        public override bool Equals(Object value)
        {
            CompareInfo that = value as CompareInfo;
            if (that != null)
            {
                return this.Name == that.Name;
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return (this.Name.GetHashCode());
        }

        public virtual int GetHashCode(string source, CompareOptions options)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (options == CompareOptions.Ordinal)
            {
                return source.GetHashCode();
            }

            if (options == CompareOptions.OrdinalIgnoreCase)
            {
                return TextInfo.GetHashCodeOrdinalIgnoreCase(source);
            }

            return GetHashCodeOfString(source, options, false, 0);
        }

        internal int GetHashCodeOfString(string source, CompareOptions options)
        {
            return GetHashCodeOfString(source, options, false, 0);
        }

        internal int GetHashCodeOfString(string source, CompareOptions options, bool forceRandomizedHashing, long additionalEntropy)
        {
            if (null == source)
            {
                throw new ArgumentNullException("source");
            }

            if ((options & ValidHashCodeOfStringMaskOffFlags) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "options");
            }

            Contract.EndContractBlock();
            if (0 == source.Length)
            {
                return (0);
            }

            return (InternalGetGlobalizedHashCode(m_dataHandle, m_handleOrigin, this.m_sortName, source, source.Length, GetNativeCompareFlags(options), forceRandomizedHashing, additionalEntropy));
        }

        public override String ToString()
        {
            return ("CompareInfo - " + this.Name);
        }

        internal static IntPtr InternalInitSortHandle(String localeName, out IntPtr handleOrigin)
        {
            return NativeInternalInitSortHandle(localeName, out handleOrigin);
        }

        private static extern IntPtr NativeInternalInitSortHandle(String localeName, out IntPtr handleOrigin);
        private static extern int InternalGetGlobalizedHashCode(IntPtr handle, IntPtr handleOrigin, string localeName, string source, int length, int dwFlags, bool forceRandomizedHashing, long additionalEntropy);
        private static extern bool InternalIsSortable(IntPtr handle, IntPtr handleOrigin, String localeName, String source, int length);
        private static extern int InternalCompareString(IntPtr handle, IntPtr handleOrigin, String localeName, String string1, int offset1, int length1, String string2, int offset2, int length2, int flags);
        private static extern int InternalFindNLSStringEx(IntPtr handle, IntPtr handleOrigin, String localeName, int flags, String source, int sourceCount, int startIndex, string target, int targetCount);
        private static extern int InternalGetSortKey(IntPtr handle, IntPtr handleOrigin, String localeName, int flags, String source, int sourceCount, byte[] target, int targetCount);
    }
}