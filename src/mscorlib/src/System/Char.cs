
using System.Globalization;

namespace System
{
    public struct Char : IComparable, IConvertible, IComparable<Char>, IEquatable<Char>
    {
        internal char m_value;
        public const char MaxValue = (char)0xFFFF;
        public const char MinValue = (char)0x00;
        private readonly static byte[] categoryForLatin1 = {(byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.SpaceSeparator, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OpenPunctuation, (byte)UnicodeCategory.ClosePunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.DashPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.OpenPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.ClosePunctuation, (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.ConnectorPunctuation, (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.OpenPunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.ClosePunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.SpaceSeparator, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.InitialQuotePunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.DashPunctuation, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.FinalQuotePunctuation, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, };
        private static bool IsLatin1(char ch)
        {
            return (ch <= '\x00ff');
        }

        private static bool IsAscii(char ch)
        {
            return (ch <= '\x007f');
        }

        private static UnicodeCategory GetLatin1UnicodeCategory(char ch)
        {
                        return (UnicodeCategory)(categoryForLatin1[(int)ch]);
        }

        public override int GetHashCode()
        {
            return (int)m_value | ((int)m_value << 16);
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is Char))
            {
                return false;
            }

            return (m_value == ((Char)obj).m_value);
        }

        public bool Equals(Char obj)
        {
            return m_value == obj;
        }

        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            if (!(value is Char))
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeChar"));
            }

            return (m_value - ((Char)value).m_value);
        }

        public int CompareTo(Char value)
        {
            return (m_value - value);
        }

        public override String ToString()
        {
                        return Char.ToString(m_value);
        }

        public String ToString(IFormatProvider provider)
        {
                        return Char.ToString(m_value);
        }

        public static String ToString(char c)
        {
                        return new String(c, 1);
        }

        public static char Parse(String s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

                        if (s.Length != 1)
            {
                throw new FormatException(Environment.GetResourceString("Format_NeedSingleChar"));
            }

            return s[0];
        }

        public static bool TryParse(String s, out Char result)
        {
            result = '\0';
            if (s == null)
            {
                return false;
            }

            if (s.Length != 1)
            {
                return false;
            }

            result = s[0];
            return true;
        }

        public static bool IsDigit(char c)
        {
            if (IsLatin1(c))
            {
                return (c >= '0' && c <= '9');
            }

            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber);
        }

        internal static bool CheckLetter(UnicodeCategory uc)
        {
            switch (uc)
            {
                case (UnicodeCategory.UppercaseLetter):
                case (UnicodeCategory.LowercaseLetter):
                case (UnicodeCategory.TitlecaseLetter):
                case (UnicodeCategory.ModifierLetter):
                case (UnicodeCategory.OtherLetter):
                    return (true);
            }

            return (false);
        }

        public static bool IsLetter(char c)
        {
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    c |= (char)0x20;
                    return ((c >= 'a' && c <= 'z'));
                }

                return (CheckLetter(GetLatin1UnicodeCategory(c)));
            }

            return (CheckLetter(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        private static bool IsWhiteSpaceLatin1(char c)
        {
            if ((c == ' ') || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085')
            {
                return (true);
            }

            return (false);
        }

        public static bool IsWhiteSpace(char c)
        {
            if (IsLatin1(c))
            {
                return (IsWhiteSpaceLatin1(c));
            }

            return CharUnicodeInfo.IsWhiteSpace(c);
        }

        public static bool IsUpper(char c)
        {
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return (c >= 'A' && c <= 'Z');
                }

                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.UppercaseLetter);
            }

            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.UppercaseLetter);
        }

        public static bool IsLower(char c)
        {
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return (c >= 'a' && c <= 'z');
                }

                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.LowercaseLetter);
            }

            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.LowercaseLetter);
        }

        internal static bool CheckPunctuation(UnicodeCategory uc)
        {
            switch (uc)
            {
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.OtherPunctuation:
                    return (true);
            }

            return (false);
        }

        public static bool IsPunctuation(char c)
        {
            if (IsLatin1(c))
            {
                return (CheckPunctuation(GetLatin1UnicodeCategory(c)));
            }

            return (CheckPunctuation(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        internal static bool CheckLetterOrDigit(UnicodeCategory uc)
        {
            switch (uc)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    return (true);
            }

            return (false);
        }

        public static bool IsLetterOrDigit(char c)
        {
            if (IsLatin1(c))
            {
                return (CheckLetterOrDigit(GetLatin1UnicodeCategory(c)));
            }

            return (CheckLetterOrDigit(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        public static char ToUpper(char c, CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");
                        return culture.TextInfo.ToUpper(c);
        }

        public static char ToUpper(char c)
        {
            return ToUpper(c, CultureInfo.CurrentCulture);
        }

        public static char ToUpperInvariant(char c)
        {
            return ToUpper(c, CultureInfo.InvariantCulture);
        }

        public static char ToLower(char c, CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");
                        return culture.TextInfo.ToLower(c);
        }

        public static char ToLower(char c)
        {
            return ToLower(c, CultureInfo.CurrentCulture);
        }

        public static char ToLowerInvariant(char c)
        {
            return ToLower(c, CultureInfo.InvariantCulture);
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Char;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Char", "Boolean"));
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return m_value;
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(m_value);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(m_value);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(m_value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(m_value);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(m_value);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(m_value);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(m_value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(m_value);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Char", "Single"));
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Char", "Double"));
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Char", "Decimal"));
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Char", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        public static bool IsControl(char c)
        {
            if (IsLatin1(c))
            {
                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.Control);
            }

            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Control);
        }

        public static bool IsControl(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.Control);
            }

            return (CharUnicodeInfo.GetUnicodeCategory(s, index) == UnicodeCategory.Control);
        }

        public static bool IsDigit(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                return (c >= '0' && c <= '9');
            }

            return (CharUnicodeInfo.GetUnicodeCategory(s, index) == UnicodeCategory.DecimalDigitNumber);
        }

        public static bool IsLetter(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    c |= (char)0x20;
                    return ((c >= 'a' && c <= 'z'));
                }

                return (CheckLetter(GetLatin1UnicodeCategory(c)));
            }

            return (CheckLetter(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }

        public static bool IsLetterOrDigit(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                return CheckLetterOrDigit(GetLatin1UnicodeCategory(c));
            }

            return CheckLetterOrDigit(CharUnicodeInfo.GetUnicodeCategory(s, index));
        }

        public static bool IsLower(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return (c >= 'a' && c <= 'z');
                }

                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.LowercaseLetter);
            }

            return (CharUnicodeInfo.GetUnicodeCategory(s, index) == UnicodeCategory.LowercaseLetter);
        }

        internal static bool CheckNumber(UnicodeCategory uc)
        {
            switch (uc)
            {
                case (UnicodeCategory.DecimalDigitNumber):
                case (UnicodeCategory.LetterNumber):
                case (UnicodeCategory.OtherNumber):
                    return (true);
            }

            return (false);
        }

        public static bool IsNumber(char c)
        {
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return (c >= '0' && c <= '9');
                }

                return (CheckNumber(GetLatin1UnicodeCategory(c)));
            }

            return (CheckNumber(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        public static bool IsNumber(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return (c >= '0' && c <= '9');
                }

                return (CheckNumber(GetLatin1UnicodeCategory(c)));
            }

            return (CheckNumber(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }

        public static bool IsPunctuation(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                return (CheckPunctuation(GetLatin1UnicodeCategory(c)));
            }

            return (CheckPunctuation(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }

        internal static bool CheckSeparator(UnicodeCategory uc)
        {
            switch (uc)
            {
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                    return (true);
            }

            return (false);
        }

        private static bool IsSeparatorLatin1(char c)
        {
            return (c == '\x0020' || c == '\x00a0');
        }

        public static bool IsSeparator(char c)
        {
            if (IsLatin1(c))
            {
                return (IsSeparatorLatin1(c));
            }

            return (CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        public static bool IsSeparator(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                return (IsSeparatorLatin1(c));
            }

            return (CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }

        public static bool IsSurrogate(char c)
        {
            return (c >= HIGH_SURROGATE_START && c <= LOW_SURROGATE_END);
        }

        public static bool IsSurrogate(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        return (IsSurrogate(s[index]));
        }

        internal static bool CheckSymbol(UnicodeCategory uc)
        {
            switch (uc)
            {
                case (UnicodeCategory.MathSymbol):
                case (UnicodeCategory.CurrencySymbol):
                case (UnicodeCategory.ModifierSymbol):
                case (UnicodeCategory.OtherSymbol):
                    return (true);
            }

            return (false);
        }

        public static bool IsSymbol(char c)
        {
            if (IsLatin1(c))
            {
                return (CheckSymbol(GetLatin1UnicodeCategory(c)));
            }

            return (CheckSymbol(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        public static bool IsSymbol(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        if (IsLatin1(s[index]))
            {
                return (CheckSymbol(GetLatin1UnicodeCategory(s[index])));
            }

            return (CheckSymbol(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }

        public static bool IsUpper(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return (c >= 'A' && c <= 'Z');
                }

                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.UppercaseLetter);
            }

            return (CharUnicodeInfo.GetUnicodeCategory(s, index) == UnicodeCategory.UppercaseLetter);
        }

        public static bool IsWhiteSpace(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        if (IsLatin1(s[index]))
            {
                return IsWhiteSpaceLatin1(s[index]);
            }

            return CharUnicodeInfo.IsWhiteSpace(s, index);
        }

        public static UnicodeCategory GetUnicodeCategory(char c)
        {
            if (IsLatin1(c))
            {
                return (GetLatin1UnicodeCategory(c));
            }

            return CharUnicodeInfo.InternalGetUnicodeCategory(c);
        }

        public static UnicodeCategory GetUnicodeCategory(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        if (IsLatin1(s[index]))
            {
                return (GetLatin1UnicodeCategory(s[index]));
            }

            return CharUnicodeInfo.InternalGetUnicodeCategory(s, index);
        }

        public static double GetNumericValue(char c)
        {
            return CharUnicodeInfo.GetNumericValue(c);
        }

        public static double GetNumericValue(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        return CharUnicodeInfo.GetNumericValue(s, index);
        }

        public static bool IsHighSurrogate(char c)
        {
            return ((c >= CharUnicodeInfo.HIGH_SURROGATE_START) && (c <= CharUnicodeInfo.HIGH_SURROGATE_END));
        }

        public static bool IsHighSurrogate(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        return (IsHighSurrogate(s[index]));
        }

        public static bool IsLowSurrogate(char c)
        {
            return ((c >= CharUnicodeInfo.LOW_SURROGATE_START) && (c <= CharUnicodeInfo.LOW_SURROGATE_END));
        }

        public static bool IsLowSurrogate(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        return (IsLowSurrogate(s[index]));
        }

        public static bool IsSurrogatePair(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }

                        if (index + 1 < s.Length)
            {
                return (IsSurrogatePair(s[index], s[index + 1]));
            }

            return (false);
        }

        public static bool IsSurrogatePair(char highSurrogate, char lowSurrogate)
        {
            return ((highSurrogate >= CharUnicodeInfo.HIGH_SURROGATE_START && highSurrogate <= CharUnicodeInfo.HIGH_SURROGATE_END) && (lowSurrogate >= CharUnicodeInfo.LOW_SURROGATE_START && lowSurrogate <= CharUnicodeInfo.LOW_SURROGATE_END));
        }

        internal const int UNICODE_PLANE00_END = 0x00ffff;
        internal const int UNICODE_PLANE01_START = 0x10000;
        internal const int UNICODE_PLANE16_END = 0x10ffff;
        internal const int HIGH_SURROGATE_START = 0x00d800;
        internal const int LOW_SURROGATE_END = 0x00dfff;
        public static String ConvertFromUtf32(int utf32)
        {
            if ((utf32 < 0 || utf32 > UNICODE_PLANE16_END) || (utf32 >= HIGH_SURROGATE_START && utf32 <= LOW_SURROGATE_END))
            {
                throw new ArgumentOutOfRangeException("utf32", Environment.GetResourceString("ArgumentOutOfRange_InvalidUTF32"));
            }

                        if (utf32 < UNICODE_PLANE01_START)
            {
                return (Char.ToString((char)utf32));
            }

            utf32 -= UNICODE_PLANE01_START;
            char[] surrogate = new char[2];
            surrogate[0] = (char)((utf32 / 0x400) + (int)CharUnicodeInfo.HIGH_SURROGATE_START);
            surrogate[1] = (char)((utf32 % 0x400) + (int)CharUnicodeInfo.LOW_SURROGATE_START);
            return (new String(surrogate));
        }

        public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
        {
            if (!IsHighSurrogate(highSurrogate))
            {
                throw new ArgumentOutOfRangeException("highSurrogate", Environment.GetResourceString("ArgumentOutOfRange_InvalidHighSurrogate"));
            }

            if (!IsLowSurrogate(lowSurrogate))
            {
                throw new ArgumentOutOfRangeException("lowSurrogate", Environment.GetResourceString("ArgumentOutOfRange_InvalidLowSurrogate"));
            }

                        return (((highSurrogate - CharUnicodeInfo.HIGH_SURROGATE_START) * 0x400) + (lowSurrogate - CharUnicodeInfo.LOW_SURROGATE_START) + UNICODE_PLANE01_START);
        }

        public static int ConvertToUtf32(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

                        int temp1 = (int)s[index] - CharUnicodeInfo.HIGH_SURROGATE_START;
            if (temp1 >= 0 && temp1 <= 0x7ff)
            {
                if (temp1 <= 0x3ff)
                {
                    if (index < s.Length - 1)
                    {
                        int temp2 = (int)s[index + 1] - CharUnicodeInfo.LOW_SURROGATE_START;
                        if (temp2 >= 0 && temp2 <= 0x3ff)
                        {
                            return ((temp1 * 0x400) + temp2 + UNICODE_PLANE01_START);
                        }
                        else
                        {
                            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHighSurrogate", index), "s");
                        }
                    }
                    else
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHighSurrogate", index), "s");
                    }
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidLowSurrogate", index), "s");
                }
            }

            return ((int)s[index]);
        }
    }
}