namespace System.Globalization
{
    using System;
    using System.Security;
    using System.Globalization;
    using System.Text;
    using System.Runtime.Versioning;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

    public sealed class IdnMapping
    {
        const int M_labelLimit = 63;
        const int M_defaultNameLimit = 255;
        const String M_strAcePrefix = "xn--";
        static char[] M_Dots = {'.', '\u3002', '\uFF0E', '\uFF61'};
        bool m_bAllowUnassigned;
        bool m_bUseStd3AsciiRules;
        public IdnMapping()
        {
        }

        public bool AllowUnassigned
        {
            get
            {
                return this.m_bAllowUnassigned;
            }

            set
            {
                this.m_bAllowUnassigned = value;
            }
        }

        public bool UseStd3AsciiRules
        {
            get
            {
                return this.m_bUseStd3AsciiRules;
            }

            set
            {
                this.m_bUseStd3AsciiRules = value;
            }
        }

        public String GetAscii(String unicode)
        {
            return GetAscii(unicode, 0);
        }

        public String GetAscii(String unicode, int index)
        {
            if (unicode == null)
                throw new ArgumentNullException("unicode");
            Contract.EndContractBlock();
            return GetAscii(unicode, index, unicode.Length - index);
        }

        public String GetAscii(String unicode, int index, int count)
        {
            if (unicode == null)
                throw new ArgumentNullException("unicode");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (index > unicode.Length)
                throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (index > unicode.Length - count)
                throw new ArgumentOutOfRangeException("unicode", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();
            unicode = unicode.Substring(index, count);
            if (Environment.IsWindows8OrAbove)
            {
                return GetAsciiUsingOS(unicode);
            }

            if (ValidateStd3AndAscii(unicode, UseStd3AsciiRules, true))
            {
                return unicode;
            }

            Contract.Assert(unicode.Length >= 1, "[IdnMapping.GetAscii]Expected 0 length strings to fail before now.");
            if (unicode[unicode.Length - 1] <= 0x1f)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequence", unicode.Length - 1), "unicode");
            }

            bool bHasLastDot = (unicode.Length > 0) && IsDot(unicode[unicode.Length - 1]);
            unicode = unicode.Normalize((NormalizationForm)(m_bAllowUnassigned ? ExtendedNormalizationForms.FormIdna : ExtendedNormalizationForms.FormIdnaDisallowUnassigned));
            if ((!bHasLastDot) && unicode.Length > 0 && IsDot(unicode[unicode.Length - 1]))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "unicode");
            }

            if (UseStd3AsciiRules)
            {
                ValidateStd3AndAscii(unicode, true, false);
            }

            return punycode_encode(unicode);
        }

        private String GetAsciiUsingOS(String unicode)
        {
            if (unicode.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "unicode");
            }

            if (unicode[unicode.Length - 1] == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequence", unicode.Length - 1), "unicode");
            }

            uint flags = (uint)((AllowUnassigned ? IDN_ALLOW_UNASSIGNED : 0) | (UseStd3AsciiRules ? IDN_USE_STD3_ASCII_RULES : 0));
            int length = IdnToAscii(flags, unicode, unicode.Length, null, 0);
            int lastError;
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "unicode");
                }

                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"), "unicode");
            }

            char[] output = new char[length];
            length = IdnToAscii(flags, unicode, unicode.Length, output, length);
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "unicode");
                }

                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"), "unicode");
            }

            return new String(output, 0, length);
        }

        public String GetUnicode(String ascii)
        {
            return GetUnicode(ascii, 0);
        }

        public String GetUnicode(String ascii, int index)
        {
            if (ascii == null)
                throw new ArgumentNullException("ascii");
            Contract.EndContractBlock();
            return GetUnicode(ascii, index, ascii.Length - index);
        }

        public String GetUnicode(String ascii, int index, int count)
        {
            if (ascii == null)
                throw new ArgumentNullException("ascii");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0) ? "index" : "count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (index > ascii.Length)
                throw new ArgumentOutOfRangeException("byteIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (index > ascii.Length - count)
                throw new ArgumentOutOfRangeException("ascii", Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            if (count > 0 && ascii[index + count - 1] == (char)0)
                throw new ArgumentException("ascii", Environment.GetResourceString("Argument_IdnBadPunycode"));
            Contract.EndContractBlock();
            ascii = ascii.Substring(index, count);
            if (Environment.IsWindows8OrAbove)
            {
                return GetUnicodeUsingOS(ascii);
            }

            String strUnicode = punycode_decode(ascii);
            if (!ascii.Equals(GetAscii(strUnicode), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "ascii");
            return strUnicode;
        }

        private string GetUnicodeUsingOS(string ascii)
        {
            uint flags = (uint)((AllowUnassigned ? IDN_ALLOW_UNASSIGNED : 0) | (UseStd3AsciiRules ? IDN_USE_STD3_ASCII_RULES : 0));
            int length = IdnToUnicode(flags, ascii, ascii.Length, null, 0);
            int lastError;
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "ascii");
                }

                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
            }

            char[] output = new char[length];
            length = IdnToUnicode(flags, ascii, ascii.Length, output, length);
            if (length == 0)
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INVALID_NAME)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnIllegalName"), "ascii");
                }

                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
            }

            return new String(output, 0, length);
        }

        public override bool Equals(Object obj)
        {
            IdnMapping that = obj as IdnMapping;
            if (that != null)
            {
                return this.m_bAllowUnassigned == that.m_bAllowUnassigned && this.m_bUseStd3AsciiRules == that.m_bUseStd3AsciiRules;
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return (this.m_bAllowUnassigned ? 100 : 200) + (this.m_bUseStd3AsciiRules ? 1000 : 2000);
        }

        static bool IsSupplementary(int cTest)
        {
            return cTest >= 0x10000;
        }

        static bool IsDot(char c)
        {
            return c == '.' || c == '\u3002' || c == '\uFF0E' || c == '\uFF61';
        }

        static bool ValidateStd3AndAscii(string unicode, bool bUseStd3, bool bCheckAscii)
        {
            if (unicode.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "unicode");
            Contract.EndContractBlock();
            int iLastDot = -1;
            for (int i = 0; i < unicode.Length; i++)
            {
                if (unicode[i] <= 0x1f)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequence", i), "unicode");
                }

                if (bCheckAscii && unicode[i] >= 0x7f)
                    return false;
                if (IsDot(unicode[i]))
                {
                    if (i == iLastDot + 1)
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "unicode");
                    if (i - iLastDot > M_labelLimit + 1)
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "Unicode");
                    if (bUseStd3 && i > 0)
                        ValidateStd3(unicode[i - 1], true);
                    iLastDot = i;
                    continue;
                }

                if (bUseStd3)
                {
                    ValidateStd3(unicode[i], (i == iLastDot + 1));
                }
            }

            if (iLastDot == -1 && unicode.Length > M_labelLimit)
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "unicode");
            if (unicode.Length > M_defaultNameLimit - (IsDot(unicode[unicode.Length - 1]) ? 0 : 1))
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadNameSize", M_defaultNameLimit - (IsDot(unicode[unicode.Length - 1]) ? 0 : 1)), "unicode");
            if (bUseStd3 && !IsDot(unicode[unicode.Length - 1]))
                ValidateStd3(unicode[unicode.Length - 1], true);
            return true;
        }

        static void ValidateStd3(char c, bool bNextToDot)
        {
            if ((c <= ',' || c == '/' || (c >= ':' && c <= '@') || (c >= '[' && c <= '`') || (c >= '{' && c <= (char)0x7F)) || (c == '-' && bNextToDot))
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadStd3", c), "Unicode");
        }

        static bool HasUpperCaseFlag(char punychar)
        {
            return (punychar >= 'A' && punychar <= 'Z');
        }

        const int punycodeBase = 36;
        const int tmin = 1;
        const int tmax = 26;
        const int skew = 38;
        const int damp = 700;
        const int initial_bias = 72;
        const int initial_n = 0x80;
        const char delimiter = '-';
        static bool basic(uint cp)
        {
            return cp < 0x80;
        }

        static int decode_digit(char cp)
        {
            if (cp >= '0' && cp <= '9')
                return cp - '0' + 26;
            if (cp >= 'a' && cp <= 'z')
                return cp - 'a';
            if (cp >= 'A' && cp <= 'Z')
                return cp - 'A';
            throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
        }

        static char encode_digit(int d)
        {
            Contract.Assert(d >= 0 && d < punycodeBase, "[IdnMapping.encode_digit]Expected 0 <= d < punycodeBase");
            if (d > 25)
                return (char)(d - 26 + '0');
            return (char)(d + 'a');
        }

        static char encode_basic(char bcp)
        {
            if (HasUpperCaseFlag(bcp))
                bcp += (char)('a' - 'A');
            return bcp;
        }

        const int maxint = 0x7ffffff;
        static int adapt(int delta, int numpoints, bool firsttime)
        {
            uint k;
            delta = firsttime ? delta / damp : delta / 2;
            Contract.Assert(numpoints != 0, "[IdnMapping.adapt]Expected non-zero numpoints.");
            delta += delta / numpoints;
            for (k = 0; delta > ((punycodeBase - tmin) * tmax) / 2; k += punycodeBase)
            {
                delta /= punycodeBase - tmin;
            }

            Contract.Assert(delta + skew != 0, "[IdnMapping.adapt]Expected non-zero delta+skew.");
            return (int)(k + (punycodeBase - tmin + 1) * delta / (delta + skew));
        }

        static String punycode_encode(String unicode)
        {
            if (unicode.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "unicode");
            Contract.EndContractBlock();
            StringBuilder output = new StringBuilder(unicode.Length);
            int iNextDot = 0;
            int iAfterLastDot = 0;
            int iOutputAfterLastDot = 0;
            while (iNextDot < unicode.Length)
            {
                iNextDot = unicode.IndexOfAny(M_Dots, iAfterLastDot);
                Contract.Assert(iNextDot <= unicode.Length, "[IdnMapping.punycode_encode]IndexOfAny is broken");
                if (iNextDot < 0)
                    iNextDot = unicode.Length;
                if (iNextDot == iAfterLastDot)
                {
                    if (iNextDot != unicode.Length)
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "unicode");
                    break;
                }

                output.Append(M_strAcePrefix);
                bool bRightToLeft = false;
                BidiCategory eBidi = CharUnicodeInfo.GetBidiCategory(unicode, iAfterLastDot);
                if (eBidi == BidiCategory.RightToLeft || eBidi == BidiCategory.RightToLeftArabic)
                {
                    bRightToLeft = true;
                    int iTest = iNextDot - 1;
                    if (Char.IsLowSurrogate(unicode, iTest))
                    {
                        iTest--;
                    }

                    eBidi = CharUnicodeInfo.GetBidiCategory(unicode, iTest);
                    if (eBidi != BidiCategory.RightToLeft && eBidi != BidiCategory.RightToLeftArabic)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadBidi"), "unicode");
                    }
                }

                int basicCount;
                int numProcessed = 0;
                for (basicCount = iAfterLastDot; basicCount < iNextDot; basicCount++)
                {
                    Contract.Assert(Char.IsLowSurrogate(unicode, basicCount) == false, "[IdnMapping.punycode_encode]Unexpected low surrogate");
                    BidiCategory testBidi = CharUnicodeInfo.GetBidiCategory(unicode, basicCount);
                    if (bRightToLeft && testBidi == BidiCategory.LeftToRight)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadBidi"), "unicode");
                    }

                    if (!bRightToLeft && (testBidi == BidiCategory.RightToLeft || testBidi == BidiCategory.RightToLeftArabic))
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadBidi"), "unicode");
                    }

                    if (basic(unicode[basicCount]))
                    {
                        output.Append(encode_basic(unicode[basicCount]));
                        numProcessed++;
                    }
                    else if (Char.IsSurrogatePair(unicode, basicCount))
                        basicCount++;
                }

                int numBasicCodePoints = numProcessed;
                if (numBasicCodePoints == iNextDot - iAfterLastDot)
                {
                    output.Remove(iOutputAfterLastDot, M_strAcePrefix.Length);
                }
                else
                {
                    if (unicode.Length - iAfterLastDot >= M_strAcePrefix.Length && unicode.Substring(iAfterLastDot, M_strAcePrefix.Length).Equals(M_strAcePrefix, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "unicode");
                    int numSurrogatePairs = 0;
                    if (numBasicCodePoints > 0)
                    {
                        output.Append(delimiter);
                    }

                    int n = initial_n;
                    int delta = 0;
                    int bias = initial_bias;
                    while (numProcessed < (iNextDot - iAfterLastDot))
                    {
                        int j;
                        int m;
                        int test = 0;
                        for (m = maxint, j = iAfterLastDot; j < iNextDot; j += IsSupplementary(test) ? 2 : 1)
                        {
                            test = Char.ConvertToUtf32(unicode, j);
                            if (test >= n && test < m)
                                m = test;
                        }

                        delta += (int)((m - n) * ((numProcessed - numSurrogatePairs) + 1));
                        Contract.Assert(delta > 0, "[IdnMapping.cs]1 punycode_encode - delta overflowed int");
                        n = m;
                        for (j = iAfterLastDot; j < iNextDot; j += IsSupplementary(test) ? 2 : 1)
                        {
                            test = Char.ConvertToUtf32(unicode, j);
                            if (test < n)
                            {
                                delta++;
                                Contract.Assert(delta > 0, "[IdnMapping.cs]2 punycode_encode - delta overflowed int");
                            }

                            if (test == n)
                            {
                                int q, k;
                                for (q = delta, k = punycodeBase;; k += punycodeBase)
                                {
                                    int t = k <= bias ? tmin : k >= bias + tmax ? tmax : k - bias;
                                    if (q < t)
                                        break;
                                    Contract.Assert(punycodeBase != t, "[IdnMapping.punycode_encode]Expected punycodeBase (36) to be != t");
                                    output.Append(encode_digit(t + (q - t) % (punycodeBase - t)));
                                    q = (q - t) / (punycodeBase - t);
                                }

                                output.Append(encode_digit(q));
                                bias = adapt(delta, (numProcessed - numSurrogatePairs) + 1, numProcessed == numBasicCodePoints);
                                delta = 0;
                                numProcessed++;
                                if (IsSupplementary(m))
                                {
                                    numProcessed++;
                                    numSurrogatePairs++;
                                }
                            }
                        }

                        ++delta;
                        ++n;
                        Contract.Assert(delta > 0, "[IdnMapping.cs]3 punycode_encode - delta overflowed int");
                    }
                }

                if (output.Length - iOutputAfterLastDot > M_labelLimit)
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "unicode");
                if (iNextDot != unicode.Length)
                    output.Append('.');
                iAfterLastDot = iNextDot + 1;
                iOutputAfterLastDot = output.Length;
            }

            if (output.Length > M_defaultNameLimit - (IsDot(unicode[unicode.Length - 1]) ? 0 : 1))
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadNameSize", M_defaultNameLimit - (IsDot(unicode[unicode.Length - 1]) ? 0 : 1)), "unicode");
            return output.ToString();
        }

        static String punycode_decode(String ascii)
        {
            if (ascii.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "ascii");
            Contract.EndContractBlock();
            if (ascii.Length > M_defaultNameLimit - (IsDot(ascii[ascii.Length - 1]) ? 0 : 1))
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadNameSize", M_defaultNameLimit - (IsDot(ascii[ascii.Length - 1]) ? 0 : 1)), "ascii");
            StringBuilder output = new StringBuilder(ascii.Length);
            int iNextDot = 0;
            int iAfterLastDot = 0;
            int iOutputAfterLastDot = 0;
            while (iNextDot < ascii.Length)
            {
                iNextDot = ascii.IndexOf('.', iAfterLastDot);
                if (iNextDot < 0 || iNextDot > ascii.Length)
                    iNextDot = ascii.Length;
                if (iNextDot == iAfterLastDot)
                {
                    if (iNextDot != ascii.Length)
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "ascii");
                    break;
                }

                if (iNextDot - iAfterLastDot > M_labelLimit)
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "ascii");
                if (ascii.Length < M_strAcePrefix.Length + iAfterLastDot || !ascii.Substring(iAfterLastDot, M_strAcePrefix.Length).Equals(M_strAcePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    output.Append(ascii.Substring(iAfterLastDot, iNextDot - iAfterLastDot));
                }
                else
                {
                    iAfterLastDot += M_strAcePrefix.Length;
                    int iTemp = ascii.LastIndexOf(delimiter, iNextDot - 1);
                    if (iTemp == iNextDot - 1)
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
                    int numBasicCodePoints;
                    if (iTemp <= iAfterLastDot)
                        numBasicCodePoints = 0;
                    else
                    {
                        numBasicCodePoints = iTemp - iAfterLastDot;
                        for (int copyAscii = iAfterLastDot; copyAscii < iAfterLastDot + numBasicCodePoints; copyAscii++)
                        {
                            if (ascii[copyAscii] > 0x7f)
                                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
                            output.Append((char)(ascii[copyAscii] >= 'A' && ascii[copyAscii] <= 'Z' ? ascii[copyAscii] - 'A' + 'a' : ascii[copyAscii]));
                        }
                    }

                    int asciiIndex = iAfterLastDot + (numBasicCodePoints > 0 ? numBasicCodePoints + 1 : 0);
                    int n = initial_n;
                    int bias = initial_bias;
                    int i = 0;
                    int w, k;
                    int numSurrogatePairs = 0;
                    while (asciiIndex < iNextDot)
                    {
                        int oldi = i;
                        for (w = 1, k = punycodeBase;; k += punycodeBase)
                        {
                            if (asciiIndex >= iNextDot)
                                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
                            int digit = decode_digit(ascii[asciiIndex++]);
                            Contract.Assert(w > 0, "[IdnMapping.punycode_decode]Expected w > 0");
                            if (digit > (maxint - i) / w)
                                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
                            i += (int)(digit * w);
                            int t = k <= bias ? tmin : k >= bias + tmax ? tmax : k - bias;
                            if (digit < t)
                                break;
                            Contract.Assert(punycodeBase != t, "[IdnMapping.punycode_decode]Expected t != punycodeBase (36)");
                            if (w > maxint / (punycodeBase - t))
                                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
                            w *= (punycodeBase - t);
                        }

                        bias = adapt(i - oldi, (output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1, oldi == 0);
                        Contract.Assert((output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1 > 0, "[IdnMapping.punycode_decode]Expected to have added > 0 characters this segment");
                        if (i / ((output.Length - iOutputAfterLastDot - numSurrogatePairs) + 1) > maxint - n)
                            throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
                        n += (int)(i / (output.Length - iOutputAfterLastDot - numSurrogatePairs + 1));
                        i %= (output.Length - iOutputAfterLastDot - numSurrogatePairs + 1);
                        if ((n < 0 || n > 0x10ffff) || (n >= 0xD800 && n <= 0xDFFF))
                            throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
                        int iUseInsertLocation;
                        String strTemp = Char.ConvertFromUtf32(n);
                        if (numSurrogatePairs > 0)
                        {
                            int iCount;
                            for (iCount = i, iUseInsertLocation = iOutputAfterLastDot; iCount > 0; iCount--, iUseInsertLocation++)
                            {
                                if (iUseInsertLocation >= output.Length)
                                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadPunycode"), "ascii");
                                if (Char.IsSurrogate(output[iUseInsertLocation]))
                                    iUseInsertLocation++;
                            }
                        }
                        else
                        {
                            iUseInsertLocation = iOutputAfterLastDot + i;
                        }

                        output.Insert(iUseInsertLocation, strTemp);
                        if (IsSupplementary(n))
                            numSurrogatePairs++;
                        i++;
                    }

                    bool bRightToLeft = false;
                    BidiCategory eBidi = CharUnicodeInfo.GetBidiCategory(output.ToString(), iOutputAfterLastDot);
                    if (eBidi == BidiCategory.RightToLeft || eBidi == BidiCategory.RightToLeftArabic)
                    {
                        bRightToLeft = true;
                    }

                    for (int iTest = iOutputAfterLastDot; iTest < output.Length; iTest++)
                    {
                        if (Char.IsLowSurrogate(output.ToString(), iTest))
                            continue;
                        eBidi = CharUnicodeInfo.GetBidiCategory(output.ToString(), iTest);
                        if ((bRightToLeft && eBidi == BidiCategory.LeftToRight) || (!bRightToLeft && (eBidi == BidiCategory.RightToLeft || eBidi == BidiCategory.RightToLeftArabic)))
                            throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadBidi"), "ascii");
                    }

                    if (bRightToLeft && eBidi != BidiCategory.RightToLeft && eBidi != BidiCategory.RightToLeftArabic)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadBidi"), "ascii");
                    }
                }

                if (iNextDot - iAfterLastDot > M_labelLimit)
                    throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadLabelSize"), "ascii");
                if (iNextDot != ascii.Length)
                    output.Append('.');
                iAfterLastDot = iNextDot + 1;
                iOutputAfterLastDot = output.Length;
            }

            if (output.Length > M_defaultNameLimit - (IsDot(output[output.Length - 1]) ? 0 : 1))
                throw new ArgumentException(Environment.GetResourceString("Argument_IdnBadNameSize", M_defaultNameLimit - (IsDot(output[output.Length - 1]) ? 0 : 1)), "ascii");
            return output.ToString();
        }

        private const int IDN_ALLOW_UNASSIGNED = 0x1;
        private const int IDN_USE_STD3_ASCII_RULES = 0x2;
        private const int ERROR_INVALID_NAME = 123;
        private static extern int IdnToAscii(uint dwFlags, [InAttribute()][MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] String lpUnicodeCharStr, int cchUnicodeChar, [System.Runtime.InteropServices.OutAttribute()] char[] lpASCIICharStr, int cchASCIIChar);
        private static extern int IdnToUnicode(uint dwFlags, [InAttribute()][MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpASCIICharStr, int cchASCIIChar, [System.Runtime.InteropServices.OutAttribute()] char[] lpUnicodeCharStr, int cchUnicodeChar);
    }
}