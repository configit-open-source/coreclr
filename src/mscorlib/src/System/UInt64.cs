
using System.Globalization;

namespace System
{
    public struct UInt64 : IComparable, IFormattable, IConvertible, IComparable<UInt64>, IEquatable<UInt64>
    {
        private ulong m_value;
        public const ulong MaxValue = (ulong)0xffffffffffffffffL;
        public const ulong MinValue = 0x0;
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is UInt64)
            {
                ulong i = (ulong)value;
                if (m_value < i)
                    return -1;
                if (m_value > i)
                    return 1;
                return 0;
            }

            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeUInt64"));
        }

        public int CompareTo(UInt64 value)
        {
            if (m_value < value)
                return -1;
            if (m_value > value)
                return 1;
            return 0;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is UInt64))
            {
                return false;
            }

            return m_value == ((UInt64)obj).m_value;
        }

        public bool Equals(UInt64 obj)
        {
            return m_value == obj;
        }

        public override int GetHashCode()
        {
            return ((int)m_value) ^ (int)(m_value >> 32);
        }

        public override String ToString()
        {
                        return Number.FormatUInt64(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
                        return Number.FormatUInt64(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format)
        {
                        return Number.FormatUInt64(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format, IFormatProvider provider)
        {
                        return Number.FormatUInt64(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public static ulong Parse(String s)
        {
            return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static ulong Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt64(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static ulong Parse(string s, IFormatProvider provider)
        {
            return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static ulong Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt64(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static Boolean TryParse(String s, out UInt64 result)
        {
            return Number.TryParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static Boolean TryParse(String s, NumberStyles style, IFormatProvider provider, out UInt64 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseUInt64(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.UInt64;
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
            return m_value;
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
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "UInt64", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}