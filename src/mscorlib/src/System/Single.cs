namespace System
{
    using System.Globalization;
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;

    public struct Single : IComparable, IFormattable, IConvertible, IComparable<Single>, IEquatable<Single>
    {
        internal float m_value;
        public const float MinValue = (float)-3.40282346638528859e+38;
        public const float Epsilon = (float)1.4e-45;
        public const float MaxValue = (float)3.40282346638528859e+38;
        public const float PositiveInfinity = (float)1.0 / (float)0.0;
        public const float NegativeInfinity = (float)-1.0 / (float)0.0;
        public const float NaN = (float)0.0 / (float)0.0;
        public unsafe static bool IsInfinity(float f)
        {
            return (*(int *)(&f) & 0x7FFFFFFF) == 0x7F800000;
        }

        public unsafe static bool IsPositiveInfinity(float f)
        {
            return *(int *)(&f) == 0x7F800000;
        }

        public unsafe static bool IsNegativeInfinity(float f)
        {
            return *(int *)(&f) == unchecked ((int)0xFF800000);
        }

        public unsafe static bool IsNaN(float f)
        {
            return (*(int *)(&f) & 0x7FFFFFFF) > 0x7F800000;
        }

        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is Single)
            {
                float f = (float)value;
                if (m_value < f)
                    return -1;
                if (m_value > f)
                    return 1;
                if (m_value == f)
                    return 0;
                if (IsNaN(m_value))
                    return (IsNaN(f) ? 0 : -1);
                else
                    return 1;
            }

            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeSingle"));
        }

        public int CompareTo(Single value)
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

        public static bool operator ==(Single left, Single right)
        {
            return left == right;
        }

        public static bool operator !=(Single left, Single right)
        {
            return left != right;
        }

        public static bool operator <(Single left, Single right)
        {
            return left < right;
        }

        public static bool operator>(Single left, Single right)
        {
            return left > right;
        }

        public static bool operator <=(Single left, Single right)
        {
            return left <= right;
        }

        public static bool operator >=(Single left, Single right)
        {
            return left >= right;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is Single))
            {
                return false;
            }

            float temp = ((Single)obj).m_value;
            if (temp == m_value)
            {
                return true;
            }

            return IsNaN(temp) && IsNaN(m_value);
        }

        public bool Equals(Single obj)
        {
            if (obj == m_value)
            {
                return true;
            }

            return IsNaN(obj) && IsNaN(m_value);
        }

        public unsafe override int GetHashCode()
        {
            float f = m_value;
            if (f == 0)
            {
                return 0;
            }

            int v = *(int *)(&f);
            return v;
        }

        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatSingle(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatSingle(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatSingle(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatSingle(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public static float Parse(String s)
        {
            return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
        }

        public static float Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static float Parse(String s, IFormatProvider provider)
        {
            return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
        }

        public static float Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static float Parse(String s, NumberStyles style, NumberFormatInfo info)
        {
            return Number.ParseSingle(s, style, info);
        }

        public static Boolean TryParse(String s, out Single result)
        {
            return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static Boolean TryParse(String s, NumberStyles style, IFormatProvider provider, out Single result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static Boolean TryParse(String s, NumberStyles style, NumberFormatInfo info, out Single result)
        {
            if (s == null)
            {
                result = 0;
                return false;
            }

            bool success = Number.TryParseSingle(s, style, info, out result);
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
            return TypeCode.Single;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(m_value);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Single", "Char"));
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
            return m_value;
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
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Single", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}