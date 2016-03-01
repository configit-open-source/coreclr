using System.Diagnostics.Contracts;
using System.Globalization;

namespace System
{
    public struct UInt32 : IComparable, IFormattable, IConvertible, IComparable<UInt32>, IEquatable<UInt32>
    {
        private uint m_value;
        public const uint MaxValue = (uint)0xffffffff;
        public const uint MinValue = 0U;
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is UInt32)
            {
                uint i = (uint)value;
                if (m_value < i)
                    return -1;
                if (m_value > i)
                    return 1;
                return 0;
            }

            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeUInt32"));
        }

        public int CompareTo(UInt32 value)
        {
            if (m_value < value)
                return -1;
            if (m_value > value)
                return 1;
            return 0;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is UInt32))
            {
                return false;
            }

            return m_value == ((UInt32)obj).m_value;
        }

        public bool Equals(UInt32 obj)
        {
            return m_value == obj;
        }

        public override int GetHashCode()
        {
            return ((int)m_value);
        }

        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt32(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt32(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt32(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt32(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public static uint Parse(String s)
        {
            return Number.ParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static uint Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt32(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static uint Parse(String s, IFormatProvider provider)
        {
            return Number.ParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static uint Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt32(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse(String s, out UInt32 result)
        {
            return Number.TryParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out UInt32 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseUInt32(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.UInt32;
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
            return m_value;
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
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "UInt32", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}