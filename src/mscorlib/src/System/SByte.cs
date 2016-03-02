
using System.Globalization;

namespace System
{
    public struct SByte : IComparable, IFormattable, IConvertible, IComparable<SByte>, IEquatable<SByte>
    {
        private sbyte m_value;
        public const sbyte MaxValue = (sbyte)0x7F;
        public const sbyte MinValue = unchecked ((sbyte)0x80);
        public int CompareTo(Object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (!(obj is SByte))
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeSByte"));
            }

            return m_value - ((SByte)obj).m_value;
        }

        public int CompareTo(SByte value)
        {
            return m_value - value;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is SByte))
            {
                return false;
            }

            return m_value == ((SByte)obj).m_value;
        }

        public bool Equals(SByte obj)
        {
            return m_value == obj;
        }

        public override int GetHashCode()
        {
            return ((int)m_value ^ (int)m_value << 8);
        }

        public override String ToString()
        {
                        return Number.FormatInt32(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
                        return Number.FormatInt32(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format)
        {
                        return ToString(format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format, IFormatProvider provider)
        {
                        return ToString(format, NumberFormatInfo.GetInstance(provider));
        }

        private String ToString(String format, NumberFormatInfo info)
        {
                        if (m_value < 0 && format != null && format.Length > 0 && (format[0] == 'X' || format[0] == 'x'))
            {
                uint temp = (uint)(m_value & 0x000000FF);
                return Number.FormatUInt32(temp, format, info);
            }

            return Number.FormatInt32(m_value, format, info);
        }

        public static sbyte Parse(String s)
        {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        public static sbyte Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static sbyte Parse(String s, IFormatProvider provider)
        {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static sbyte Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static sbyte Parse(String s, NumberStyles style, NumberFormatInfo info)
        {
            int i = 0;
            try
            {
                i = Number.ParseInt32(s, style, info);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_SByte"), e);
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if ((i < 0) || i > Byte.MaxValue)
                {
                    throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
                }

                return (sbyte)i;
            }

            if (i < MinValue || i > MaxValue)
                throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
            return (sbyte)i;
        }

        public static bool TryParse(String s, out SByte result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out SByte result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(String s, NumberStyles style, NumberFormatInfo info, out SByte result)
        {
            result = 0;
            int i;
            if (!Number.TryParseInt32(s, style, info, out i))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if ((i < 0) || i > Byte.MaxValue)
                {
                    return false;
                }

                result = (sbyte)i;
                return true;
            }

            if (i < MinValue || i > MaxValue)
            {
                return false;
            }

            result = (sbyte)i;
            return true;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.SByte;
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
            return m_value;
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
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "SByte", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}