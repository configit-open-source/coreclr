using System.Diagnostics.Contracts;
using System.Globalization;

namespace System
{
    public struct UInt16 : IComparable, IFormattable, IConvertible, IComparable<UInt16>, IEquatable<UInt16>
    {
        private ushort m_value;
        public const ushort MaxValue = (ushort)0xFFFF;
        public const ushort MinValue = 0;
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is UInt16)
            {
                return ((int)m_value - (int)(((UInt16)value).m_value));
            }

            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeUInt16"));
        }

        public int CompareTo(UInt16 value)
        {
            return ((int)m_value - (int)value);
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is UInt16))
            {
                return false;
            }

            return m_value == ((UInt16)obj).m_value;
        }

        public bool Equals(UInt16 obj)
        {
            return m_value == obj;
        }

        public override int GetHashCode()
        {
            return (int)m_value;
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

        public static ushort Parse(String s)
        {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static ushort Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static ushort Parse(String s, IFormatProvider provider)
        {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static ushort Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static ushort Parse(String s, NumberStyles style, NumberFormatInfo info)
        {
            uint i = 0;
            try
            {
                i = Number.ParseUInt32(s, style, info);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"), e);
            }

            if (i > MaxValue)
                throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
            return (ushort)i;
        }

        public static bool TryParse(String s, out UInt16 result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out UInt16 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(String s, NumberStyles style, NumberFormatInfo info, out UInt16 result)
        {
            result = 0;
            UInt32 i;
            if (!Number.TryParseUInt32(s, style, info, out i))
            {
                return false;
            }

            if (i > MaxValue)
            {
                return false;
            }

            result = (UInt16)i;
            return true;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.UInt16;
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
            return m_value;
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
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "UInt16", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}