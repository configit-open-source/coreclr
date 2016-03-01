using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;

namespace System
{
    internal class Number
    {
        private Number()
        {
        }

        public static extern String FormatDecimal(Decimal value, String format, NumberFormatInfo info);
        public static extern String FormatDouble(double value, String format, NumberFormatInfo info);
        public static extern String FormatInt32(int value, String format, NumberFormatInfo info);
        public static extern String FormatUInt32(uint value, String format, NumberFormatInfo info);
        public static extern String FormatInt64(long value, String format, NumberFormatInfo info);
        public static extern String FormatUInt64(ulong value, String format, NumberFormatInfo info);
        public static extern String FormatSingle(float value, String format, NumberFormatInfo info);
        public unsafe static extern Boolean NumberBufferToDecimal(byte *number, ref Decimal value);
        internal unsafe static extern Boolean NumberBufferToDouble(byte *number, ref Double value);
        internal static extern unsafe string FormatNumberBuffer(byte *number, string format, NumberFormatInfo info, char *allDigits);
        private const Int32 NumberMaxDigits = 50;
        private const Int32 Int32Precision = 10;
        private const Int32 UInt32Precision = Int32Precision;
        private const Int32 Int64Precision = 19;
        private const Int32 UInt64Precision = 20;
        internal unsafe struct NumberBuffer
        {
            public static readonly Int32 NumberBufferBytes = 12 + ((NumberMaxDigits + 1) * 2) + IntPtr.Size;
            private Byte*baseAddress;
            public Char*digits;
            public Int32 precision;
            public Int32 scale;
            public Boolean sign;
            public NumberBuffer(Byte*stackBuffer)
            {
                this.baseAddress = stackBuffer;
                this.digits = (((Char*)stackBuffer) + 6);
                this.precision = 0;
                this.scale = 0;
                this.sign = false;
            }

            public Byte*PackForNative()
            {
                Int32*baseInteger = (Int32*)baseAddress;
                baseInteger[0] = precision;
                baseInteger[1] = scale;
                baseInteger[2] = sign ? 1 : 0;
                return baseAddress;
            }
        }

        private static Boolean HexNumberToInt32(ref NumberBuffer number, ref Int32 value)
        {
            UInt32 passedValue = 0;
            Boolean returnValue = HexNumberToUInt32(ref number, ref passedValue);
            value = (Int32)passedValue;
            return returnValue;
        }

        private static Boolean HexNumberToInt64(ref NumberBuffer number, ref Int64 value)
        {
            UInt64 passedValue = 0;
            Boolean returnValue = HexNumberToUInt64(ref number, ref passedValue);
            value = (Int64)passedValue;
            return returnValue;
        }

        private unsafe static Boolean HexNumberToUInt32(ref NumberBuffer number, ref UInt32 value)
        {
            Int32 i = number.scale;
            if (i > UInt32Precision || i < number.precision)
            {
                return false;
            }

            Char*p = number.digits;
            Contract.Assert(p != null, "");
            UInt32 n = 0;
            while (--i >= 0)
            {
                if (n > ((UInt32)0xFFFFFFFF / 16))
                {
                    return false;
                }

                n *= 16;
                if (*p != '\0')
                {
                    UInt32 newN = n;
                    if (*p != '\0')
                    {
                        if (*p >= '0' && *p <= '9')
                        {
                            newN += (UInt32)(*p - '0');
                        }
                        else
                        {
                            if (*p >= 'A' && *p <= 'F')
                            {
                                newN += (UInt32)((*p - 'A') + 10);
                            }
                            else
                            {
                                Contract.Assert(*p >= 'a' && *p <= 'f', "");
                                newN += (UInt32)((*p - 'a') + 10);
                            }
                        }

                        p++;
                    }

                    if (newN < n)
                    {
                        return false;
                    }

                    n = newN;
                }
            }

            value = n;
            return true;
        }

        private unsafe static Boolean HexNumberToUInt64(ref NumberBuffer number, ref UInt64 value)
        {
            Int32 i = number.scale;
            if (i > UInt64Precision || i < number.precision)
            {
                return false;
            }

            Char*p = number.digits;
            Contract.Assert(p != null, "");
            UInt64 n = 0;
            while (--i >= 0)
            {
                if (n > (0xFFFFFFFFFFFFFFFF / 16))
                {
                    return false;
                }

                n *= 16;
                if (*p != '\0')
                {
                    UInt64 newN = n;
                    if (*p != '\0')
                    {
                        if (*p >= '0' && *p <= '9')
                        {
                            newN += (UInt64)(*p - '0');
                        }
                        else
                        {
                            if (*p >= 'A' && *p <= 'F')
                            {
                                newN += (UInt64)((*p - 'A') + 10);
                            }
                            else
                            {
                                Contract.Assert(*p >= 'a' && *p <= 'f', "");
                                newN += (UInt64)((*p - 'a') + 10);
                            }
                        }

                        p++;
                    }

                    if (newN < n)
                    {
                        return false;
                    }

                    n = newN;
                }
            }

            value = n;
            return true;
        }

        private static Boolean IsWhite(char ch)
        {
            return (((ch) == 0x20) || ((ch) >= 0x09 && (ch) <= 0x0D));
        }

        private unsafe static Boolean NumberToInt32(ref NumberBuffer number, ref Int32 value)
        {
            Int32 i = number.scale;
            if (i > Int32Precision || i < number.precision)
            {
                return false;
            }

            char *p = number.digits;
            Contract.Assert(p != null, "");
            Int32 n = 0;
            while (--i >= 0)
            {
                if ((UInt32)n > (0x7FFFFFFF / 10))
                {
                    return false;
                }

                n *= 10;
                if (*p != '\0')
                {
                    n += (Int32)(*p++ - '0');
                }
            }

            if (number.sign)
            {
                n = -n;
                if (n > 0)
                {
                    return false;
                }
            }
            else
            {
                if (n < 0)
                {
                    return false;
                }
            }

            value = n;
            return true;
        }

        private unsafe static Boolean NumberToInt64(ref NumberBuffer number, ref Int64 value)
        {
            Int32 i = number.scale;
            if (i > Int64Precision || i < number.precision)
            {
                return false;
            }

            char *p = number.digits;
            Contract.Assert(p != null, "");
            Int64 n = 0;
            while (--i >= 0)
            {
                if ((UInt64)n > (0x7FFFFFFFFFFFFFFF / 10))
                {
                    return false;
                }

                n *= 10;
                if (*p != '\0')
                {
                    n += (Int32)(*p++ - '0');
                }
            }

            if (number.sign)
            {
                n = -n;
                if (n > 0)
                {
                    return false;
                }
            }
            else
            {
                if (n < 0)
                {
                    return false;
                }
            }

            value = n;
            return true;
        }

        private unsafe static Boolean NumberToUInt32(ref NumberBuffer number, ref UInt32 value)
        {
            Int32 i = number.scale;
            if (i > UInt32Precision || i < number.precision || number.sign)
            {
                return false;
            }

            char *p = number.digits;
            Contract.Assert(p != null, "");
            UInt32 n = 0;
            while (--i >= 0)
            {
                if (n > (0xFFFFFFFF / 10))
                {
                    return false;
                }

                n *= 10;
                if (*p != '\0')
                {
                    UInt32 newN = n + (UInt32)(*p++ - '0');
                    if (newN < n)
                    {
                        return false;
                    }

                    n = newN;
                }
            }

            value = n;
            return true;
        }

        private unsafe static Boolean NumberToUInt64(ref NumberBuffer number, ref UInt64 value)
        {
            Int32 i = number.scale;
            if (i > UInt64Precision || i < number.precision || number.sign)
            {
                return false;
            }

            char *p = number.digits;
            Contract.Assert(p != null, "");
            UInt64 n = 0;
            while (--i >= 0)
            {
                if (n > (0xFFFFFFFFFFFFFFFF / 10))
                {
                    return false;
                }

                n *= 10;
                if (*p != '\0')
                {
                    UInt64 newN = n + (UInt64)(*p++ - '0');
                    if (newN < n)
                    {
                        return false;
                    }

                    n = newN;
                }
            }

            value = n;
            return true;
        }

        private unsafe static char *MatchChars(char *p, string str)
        {
            fixed (char *stringPointer = str)
            {
                return MatchChars(p, stringPointer);
            }
        }

        private unsafe static char *MatchChars(char *p, char *str)
        {
            Contract.Assert(p != null && str != null, "");
            if (*str == '\0')
            {
                return null;
            }

            for (; (*str != '\0'); p++, str++)
            {
                if (*p != *str)
                {
                    if ((*str == '\u00A0') && (*p == '\u0020'))
                    {
                        continue;
                    }

                    return null;
                }
            }

            return p;
        }

        internal unsafe static Decimal ParseDecimal(String value, NumberStyles options, NumberFormatInfo numfmt)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            Decimal result = 0;
            StringToNumber(value, options, ref number, numfmt, true);
            if (!NumberBufferToDecimal(number.PackForNative(), ref result))
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_Decimal"));
            }

            return result;
        }

        internal unsafe static Double ParseDouble(String value, NumberStyles options, NumberFormatInfo numfmt)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            Double d = 0;
            if (!TryStringToNumber(value, options, ref number, numfmt, false))
            {
                String sTrim = value.Trim();
                if (sTrim.Equals(numfmt.PositiveInfinitySymbol))
                {
                    return Double.PositiveInfinity;
                }

                if (sTrim.Equals(numfmt.NegativeInfinitySymbol))
                {
                    return Double.NegativeInfinity;
                }

                if (sTrim.Equals(numfmt.NaNSymbol))
                {
                    return Double.NaN;
                }

                throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
            }

            if (!NumberBufferToDouble(number.PackForNative(), ref d))
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_Double"));
            }

            return d;
        }

        internal unsafe static Int32 ParseInt32(String s, NumberStyles style, NumberFormatInfo info)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            Int32 i = 0;
            StringToNumber(s, style, ref number, info, false);
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToInt32(ref number, ref i))
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
                }
            }
            else
            {
                if (!NumberToInt32(ref number, ref i))
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
                }
            }

            return i;
        }

        internal unsafe static Int64 ParseInt64(String value, NumberStyles options, NumberFormatInfo numfmt)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            Int64 i = 0;
            StringToNumber(value, options, ref number, numfmt, false);
            if ((options & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToInt64(ref number, ref i))
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int64"));
                }
            }
            else
            {
                if (!NumberToInt64(ref number, ref i))
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int64"));
                }
            }

            return i;
        }

        private unsafe static Boolean ParseNumber(ref char *str, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, Boolean parseDecimal)
        {
            const Int32 StateSign = 0x0001;
            const Int32 StateParens = 0x0002;
            const Int32 StateDigits = 0x0004;
            const Int32 StateNonZero = 0x0008;
            const Int32 StateDecimal = 0x0010;
            const Int32 StateCurrency = 0x0020;
            number.scale = 0;
            number.sign = false;
            string decSep;
            string groupSep;
            string currSymbol = null;
            string ansicurrSymbol = null;
            string altdecSep = null;
            string altgroupSep = null;
            Boolean parsingCurrency = false;
            if ((options & NumberStyles.AllowCurrencySymbol) != 0)
            {
                currSymbol = numfmt.CurrencySymbol;
                if (numfmt.ansiCurrencySymbol != null)
                {
                    ansicurrSymbol = numfmt.ansiCurrencySymbol;
                }

                altdecSep = numfmt.NumberDecimalSeparator;
                altgroupSep = numfmt.NumberGroupSeparator;
                decSep = numfmt.CurrencyDecimalSeparator;
                groupSep = numfmt.CurrencyGroupSeparator;
                parsingCurrency = true;
            }
            else
            {
                decSep = numfmt.NumberDecimalSeparator;
                groupSep = numfmt.NumberGroupSeparator;
            }

            Int32 state = 0;
            Boolean signflag = false;
            Boolean bigNumber = (sb != null);
            Boolean bigNumberHex = (bigNumber && ((options & NumberStyles.AllowHexSpecifier) != 0));
            Int32 maxParseDigits = bigNumber ? Int32.MaxValue : NumberMaxDigits;
            char *p = str;
            char ch = *p;
            char *next;
            while (true)
            {
                if (IsWhite(ch) && ((options & NumberStyles.AllowLeadingWhite) != 0) && (((state & StateSign) == 0) || (((state & StateSign) != 0) && (((state & StateCurrency) != 0) || numfmt.numberNegativePattern == 2))))
                {
                }
                else if ((signflag = (((options & NumberStyles.AllowLeadingSign) != 0) && ((state & StateSign) == 0))) && ((next = MatchChars(p, numfmt.positiveSign)) != null))
                {
                    state |= StateSign;
                    p = next - 1;
                }
                else if (signflag && (next = MatchChars(p, numfmt.negativeSign)) != null)
                {
                    state |= StateSign;
                    number.sign = true;
                    p = next - 1;
                }
                else if (ch == '(' && ((options & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0))
                {
                    state |= StateSign | StateParens;
                    number.sign = true;
                }
                else if ((currSymbol != null && (next = MatchChars(p, currSymbol)) != null) || (ansicurrSymbol != null && (next = MatchChars(p, ansicurrSymbol)) != null))
                {
                    state |= StateCurrency;
                    currSymbol = null;
                    ansicurrSymbol = null;
                    p = next - 1;
                }
                else
                {
                    break;
                }

                ch = *++p;
            }

            Int32 digCount = 0;
            Int32 digEnd = 0;
            while (true)
            {
                if ((ch >= '0' && ch <= '9') || (((options & NumberStyles.AllowHexSpecifier) != 0) && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))))
                {
                    state |= StateDigits;
                    if (ch != '0' || (state & StateNonZero) != 0 || bigNumberHex)
                    {
                        if (digCount < maxParseDigits)
                        {
                            if (bigNumber)
                                sb.Append(ch);
                            else
                                number.digits[digCount++] = ch;
                            if (ch != '0' || parseDecimal)
                            {
                                digEnd = digCount;
                            }
                        }

                        if ((state & StateDecimal) == 0)
                        {
                            number.scale++;
                        }

                        state |= StateNonZero;
                    }
                    else if ((state & StateDecimal) != 0)
                    {
                        number.scale--;
                    }
                }
                else if (((options & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, decSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, altdecSep)) != null))
                {
                    state |= StateDecimal;
                    p = next - 1;
                }
                else if (((options & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, groupSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, altgroupSep)) != null))
                {
                    p = next - 1;
                }
                else
                {
                    break;
                }

                ch = *++p;
            }

            Boolean negExp = false;
            number.precision = digEnd;
            if (bigNumber)
                sb.Append('\0');
            else
                number.digits[digEnd] = '\0';
            if ((state & StateDigits) != 0)
            {
                if ((ch == 'E' || ch == 'e') && ((options & NumberStyles.AllowExponent) != 0))
                {
                    char *temp = p;
                    ch = *++p;
                    if ((next = MatchChars(p, numfmt.positiveSign)) != null)
                    {
                        ch = *(p = next);
                    }
                    else if ((next = MatchChars(p, numfmt.negativeSign)) != null)
                    {
                        ch = *(p = next);
                        negExp = true;
                    }

                    if (ch >= '0' && ch <= '9')
                    {
                        Int32 exp = 0;
                        do
                        {
                            exp = exp * 10 + (ch - '0');
                            ch = *++p;
                            if (exp > 1000)
                            {
                                exp = 9999;
                                while (ch >= '0' && ch <= '9')
                                {
                                    ch = *++p;
                                }
                            }
                        }
                        while (ch >= '0' && ch <= '9');
                        if (negExp)
                        {
                            exp = -exp;
                        }

                        number.scale += exp;
                    }
                    else
                    {
                        p = temp;
                        ch = *p;
                    }
                }

                while (true)
                {
                    if (IsWhite(ch) && ((options & NumberStyles.AllowTrailingWhite) != 0))
                    {
                    }
                    else if ((signflag = (((options & NumberStyles.AllowTrailingSign) != 0) && ((state & StateSign) == 0))) && (next = MatchChars(p, numfmt.positiveSign)) != null)
                    {
                        state |= StateSign;
                        p = next - 1;
                    }
                    else if (signflag && (next = MatchChars(p, numfmt.negativeSign)) != null)
                    {
                        state |= StateSign;
                        number.sign = true;
                        p = next - 1;
                    }
                    else if (ch == ')' && ((state & StateParens) != 0))
                    {
                        state &= ~StateParens;
                    }
                    else if ((currSymbol != null && (next = MatchChars(p, currSymbol)) != null) || (ansicurrSymbol != null && (next = MatchChars(p, ansicurrSymbol)) != null))
                    {
                        currSymbol = null;
                        ansicurrSymbol = null;
                        p = next - 1;
                    }
                    else
                    {
                        break;
                    }

                    ch = *++p;
                }

                if ((state & StateParens) == 0)
                {
                    if ((state & StateNonZero) == 0)
                    {
                        if (!parseDecimal)
                        {
                            number.scale = 0;
                        }

                        if ((state & StateDecimal) == 0)
                        {
                            number.sign = false;
                        }
                    }

                    str = p;
                    return true;
                }
            }

            str = p;
            return false;
        }

        internal unsafe static Single ParseSingle(String value, NumberStyles options, NumberFormatInfo numfmt)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            Double d = 0;
            if (!TryStringToNumber(value, options, ref number, numfmt, false))
            {
                String sTrim = value.Trim();
                if (sTrim.Equals(numfmt.PositiveInfinitySymbol))
                {
                    return Single.PositiveInfinity;
                }

                if (sTrim.Equals(numfmt.NegativeInfinitySymbol))
                {
                    return Single.NegativeInfinity;
                }

                if (sTrim.Equals(numfmt.NaNSymbol))
                {
                    return Single.NaN;
                }

                throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
            }

            if (!NumberBufferToDouble(number.PackForNative(), ref d))
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_Single"));
            }

            Single castSingle = (Single)d;
            if (Single.IsInfinity(castSingle))
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_Single"));
            }

            return castSingle;
        }

        internal unsafe static UInt32 ParseUInt32(String value, NumberStyles options, NumberFormatInfo numfmt)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            UInt32 i = 0;
            StringToNumber(value, options, ref number, numfmt, false);
            if ((options & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToUInt32(ref number, ref i))
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
                }
            }
            else
            {
                if (!NumberToUInt32(ref number, ref i))
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
                }
            }

            return i;
        }

        internal unsafe static UInt64 ParseUInt64(String value, NumberStyles options, NumberFormatInfo numfmt)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            UInt64 i = 0;
            StringToNumber(value, options, ref number, numfmt, false);
            if ((options & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToUInt64(ref number, ref i))
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
                }
            }
            else
            {
                if (!NumberToUInt64(ref number, ref i))
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
                }
            }

            return i;
        }

        private unsafe static void StringToNumber(String str, NumberStyles options, ref NumberBuffer number, NumberFormatInfo info, Boolean parseDecimal)
        {
            if (str == null)
            {
                throw new ArgumentNullException("String");
            }

            Contract.EndContractBlock();
            Contract.Assert(info != null, "");
            fixed (char *stringPointer = str)
            {
                char *p = stringPointer;
                if (!ParseNumber(ref p, options, ref number, null, info, parseDecimal) || (p - stringPointer < str.Length && !TrailingZeros(str, (int)(p - stringPointer))))
                {
                    throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
                }
            }
        }

        private static Boolean TrailingZeros(String s, Int32 index)
        {
            for (int i = index; i < s.Length; i++)
            {
                if (s[i] != '\0')
                {
                    return false;
                }
            }

            return true;
        }

        internal unsafe static Boolean TryParseDecimal(String value, NumberStyles options, NumberFormatInfo numfmt, out Decimal result)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
            if (!TryStringToNumber(value, options, ref number, numfmt, true))
            {
                return false;
            }

            if (!NumberBufferToDecimal(number.PackForNative(), ref result))
            {
                return false;
            }

            return true;
        }

        internal unsafe static Boolean TryParseDouble(String value, NumberStyles options, NumberFormatInfo numfmt, out Double result)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
            if (!TryStringToNumber(value, options, ref number, numfmt, false))
            {
                return false;
            }

            if (!NumberBufferToDouble(number.PackForNative(), ref result))
            {
                return false;
            }

            return true;
        }

        internal unsafe static Boolean TryParseInt32(String s, NumberStyles style, NumberFormatInfo info, out Int32 result)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
            if (!TryStringToNumber(s, style, ref number, info, false))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToInt32(ref number, ref result))
                {
                    return false;
                }
            }
            else
            {
                if (!NumberToInt32(ref number, ref result))
                {
                    return false;
                }
            }

            return true;
        }

        internal unsafe static Boolean TryParseInt64(String s, NumberStyles style, NumberFormatInfo info, out Int64 result)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
            if (!TryStringToNumber(s, style, ref number, info, false))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToInt64(ref number, ref result))
                {
                    return false;
                }
            }
            else
            {
                if (!NumberToInt64(ref number, ref result))
                {
                    return false;
                }
            }

            return true;
        }

        internal unsafe static Boolean TryParseSingle(String value, NumberStyles options, NumberFormatInfo numfmt, out Single result)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
            Double d = 0;
            if (!TryStringToNumber(value, options, ref number, numfmt, false))
            {
                return false;
            }

            if (!NumberBufferToDouble(number.PackForNative(), ref d))
            {
                return false;
            }

            Single castSingle = (Single)d;
            if (Single.IsInfinity(castSingle))
            {
                return false;
            }

            result = castSingle;
            return true;
        }

        internal unsafe static Boolean TryParseUInt32(String s, NumberStyles style, NumberFormatInfo info, out UInt32 result)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
            if (!TryStringToNumber(s, style, ref number, info, false))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToUInt32(ref number, ref result))
                {
                    return false;
                }
            }
            else
            {
                if (!NumberToUInt32(ref number, ref result))
                {
                    return false;
                }
            }

            return true;
        }

        internal unsafe static Boolean TryParseUInt64(String s, NumberStyles style, NumberFormatInfo info, out UInt64 result)
        {
            Byte*numberBufferBytes = stackalloc Byte[NumberBuffer.NumberBufferBytes];
            NumberBuffer number = new NumberBuffer(numberBufferBytes);
            result = 0;
            if (!TryStringToNumber(s, style, ref number, info, false))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToUInt64(ref number, ref result))
                {
                    return false;
                }
            }
            else
            {
                if (!NumberToUInt64(ref number, ref result))
                {
                    return false;
                }
            }

            return true;
        }

        internal static Boolean TryStringToNumber(String str, NumberStyles options, ref NumberBuffer number, NumberFormatInfo numfmt, Boolean parseDecimal)
        {
            return TryStringToNumber(str, options, ref number, null, numfmt, parseDecimal);
        }

        internal unsafe static Boolean TryStringToNumber(String str, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, Boolean parseDecimal)
        {
            if (str == null)
            {
                return false;
            }

            Contract.Assert(numfmt != null, "");
            fixed (char *stringPointer = str)
            {
                char *p = stringPointer;
                if (!ParseNumber(ref p, options, ref number, sb, numfmt, parseDecimal) || (p - stringPointer < str.Length && !TrailingZeros(str, (int)(p - stringPointer))))
                {
                    return false;
                }
            }

            return true;
        }
    }
}