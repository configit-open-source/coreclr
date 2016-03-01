
using System.Globalization;

namespace System
{
    public struct Int32 : IComparable, IFormattable, IConvertible, IComparable<Int32>, IEquatable<Int32>
    {
        internal int m_value;
        public const int MaxValue = 0x7fffffff;
        public const int MinValue = unchecked ((int)0x80000000);
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is Int32)
            {
                int i = (int)value;
                if (m_value < i)
                    return -1;
                if (m_value > i)
                    return 1;
                return 0;
            }

            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeInt32"));
        }

        public int CompareTo(int value)
        {
            if (m_value < value)
                return -1;
            if (m_value > value)
                return 1;
            return 0;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is Int32))
            {
                return false;
            }

            return m_value == ((Int32)obj).m_value;
        }

        public bool Equals(Int32 obj)
        {
            return m_value == obj;
        }

        public override int GetHashCode()
        {
            return m_value;
        }

        public override String ToString()
        {
                        return Number.FormatInt32(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format)
        {
                        return Number.FormatInt32(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
                        return Number.FormatInt32(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format, IFormatProvider provider)
        {
                        return Number.FormatInt32(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public static int Parse(String s)
        {
            return Number.ParseInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static int Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseInt32(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static int Parse(String s, IFormatProvider provider)
        {
            return Number.ParseInt32(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static int Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseInt32(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse(String s, out Int32 result)
        {
            return Number.TryParseInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out Int32 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseInt32(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Int32;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(m_value);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(m_value);
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
            return m_value;
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
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Int32", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}