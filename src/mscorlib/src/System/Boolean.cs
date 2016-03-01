using System.Diagnostics.Contracts;

namespace System
{
    public struct Boolean : IComparable, IConvertible, IComparable<Boolean>, IEquatable<Boolean>
    {
        private bool m_value;
        internal const int True = 1;
        internal const int False = 0;
        internal const String TrueLiteral = "True";
        internal const String FalseLiteral = "False";
        public static readonly String TrueString = TrueLiteral;
        public static readonly String FalseString = FalseLiteral;
        public override int GetHashCode()
        {
            return (m_value) ? True : False;
        }

        public override String ToString()
        {
            if (false == m_value)
            {
                return FalseLiteral;
            }

            return TrueLiteral;
        }

        public String ToString(IFormatProvider provider)
        {
            if (false == m_value)
            {
                return FalseLiteral;
            }

            return TrueLiteral;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is Boolean))
            {
                return false;
            }

            return (m_value == ((Boolean)obj).m_value);
        }

        public bool Equals(Boolean obj)
        {
            return m_value == obj;
        }

        public int CompareTo(Object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (!(obj is Boolean))
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeBoolean"));
            }

            if (m_value == ((Boolean)obj).m_value)
            {
                return 0;
            }
            else if (m_value == false)
            {
                return -1;
            }

            return 1;
        }

        public int CompareTo(Boolean value)
        {
            if (m_value == value)
            {
                return 0;
            }
            else if (m_value == false)
            {
                return -1;
            }

            return 1;
        }

        public static Boolean Parse(String value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            Boolean result = false;
            if (!TryParse(value, out result))
            {
                throw new FormatException(Environment.GetResourceString("Format_BadBoolean"));
            }
            else
            {
                return result;
            }
        }

        public static Boolean TryParse(String value, out Boolean result)
        {
            result = false;
            if (value == null)
            {
                return false;
            }

            if (TrueLiteral.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (FalseLiteral.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            value = TrimWhiteSpaceAndNull(value);
            if (TrueLiteral.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (FalseLiteral.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            return false;
        }

        private static String TrimWhiteSpaceAndNull(String value)
        {
            int start = 0;
            int end = value.Length - 1;
            char nullChar = (char)0x0000;
            while (start < value.Length)
            {
                if (!Char.IsWhiteSpace(value[start]) && value[start] != nullChar)
                {
                    break;
                }

                start++;
            }

            while (end >= start)
            {
                if (!Char.IsWhiteSpace(value[end]) && value[end] != nullChar)
                {
                    break;
                }

                end--;
            }

            return value.Substring(start, end - start + 1);
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Boolean;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return m_value;
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Boolean", "Char"));
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
            return Convert.ToSingle(m_value);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(m_value);
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Boolean", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}