
using System.Globalization;

namespace System
{
    public struct Double : IComparable, IFormattable, IConvertible, IComparable<Double>, IEquatable<Double>
    {
        internal double m_value;
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;
        public const double Epsilon = 4.9406564584124654E-324;
        public const double NegativeInfinity = (double)-1.0 / (double)(0.0);
        public const double PositiveInfinity = (double)1.0 / (double)(0.0);
        public const double NaN = (double)0.0 / (double)0.0;
        internal static double NegativeZero = BitConverter.Int64BitsToDouble(unchecked ((long)0x8000000000000000));
        public unsafe static bool IsInfinity(double d)
        {
            return (*(long *)(&d) & 0x7FFFFFFFFFFFFFFF) == 0x7FF0000000000000;
        }

        public static bool IsPositiveInfinity(double d)
        {
            if (d == double.PositiveInfinity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsNegativeInfinity(double d)
        {
            if (d == double.NegativeInfinity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal unsafe static bool IsNegative(double d)
        {
            return (*(UInt64*)(&d) & 0x8000000000000000) == 0x8000000000000000;
        }

        public unsafe static bool IsNaN(double d)
        {
            return (*(UInt64*)(&d) & 0x7FFFFFFFFFFFFFFFL) > 0x7FF0000000000000L;
        }

        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is Double)
            {
                double d = (double)value;
                if (m_value < d)
                    return -1;
                if (m_value > d)
                    return 1;
                if (m_value == d)
                    return 0;
                if (IsNaN(m_value))
                    return (IsNaN(d) ? 0 : -1);
                else
                    return 1;
            }

            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDouble"));
        }

        public int CompareTo(Double value)
        {
            if (m_value < value)
                return -1;
            if (m_value > value)
                return 1;
            if (m_value == value)
                return 0;
            if (IsNaN(m_value))
                return (IsNaN(value) ? 0 : -1);
            else
                return 1;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is Double))
            {
                return false;
            }

            double temp = ((Double)obj).m_value;
            if (temp == m_value)
            {
                return true;
            }

            return IsNaN(temp) && IsNaN(m_value);
        }

        public static bool operator ==(Double left, Double right)
        {
            return left == right;
        }

        public static bool operator !=(Double left, Double right)
        {
            return left != right;
        }

        public static bool operator <(Double left, Double right)
        {
            return left < right;
        }

        public static bool operator>(Double left, Double right)
        {
            return left > right;
        }

        public static bool operator <=(Double left, Double right)
        {
            return left <= right;
        }

        public static bool operator >=(Double left, Double right)
        {
            return left >= right;
        }

        public bool Equals(Double obj)
        {
            if (obj == m_value)
            {
                return true;
            }

            return IsNaN(obj) && IsNaN(m_value);
        }

        public unsafe override int GetHashCode()
        {
            double d = m_value;
            if (d == 0)
            {
                return 0;
            }

            long value = *(long *)(&d);
            return unchecked ((int)value) ^ ((int)(value >> 32));
        }

        public override String ToString()
        {
                        return Number.FormatDouble(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format)
        {
                        return Number.FormatDouble(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
                        return Number.FormatDouble(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format, IFormatProvider provider)
        {
                        return Number.FormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public static double Parse(String s)
        {
            return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(String s, IFormatProvider provider)
        {
            return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
        }

        public static double Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static double Parse(String s, NumberStyles style, NumberFormatInfo info)
        {
            return Number.ParseDouble(s, style, info);
        }

        public static bool TryParse(String s, out double result)
        {
            return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out double result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(String s, NumberStyles style, NumberFormatInfo info, out double result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            bool success = Number.TryParseDouble(s, style, info, out result);
            if (!success)
            {
                String sTrim = s.Trim();
                if (sTrim.Equals(info.PositiveInfinitySymbol))
                {
                    result = PositiveInfinity;
                }
                else if (sTrim.Equals(info.NegativeInfinitySymbol))
                {
                    result = NegativeInfinity;
                }
                else if (sTrim.Equals(info.NaNSymbol))
                {
                    result = NaN;
                }
                else
                    return false;
            }

            return true;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Double;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(m_value);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Double", "Char"));
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
            return m_value;
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Double", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}