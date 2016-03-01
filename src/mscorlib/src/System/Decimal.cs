
using System.Globalization;
using System.Runtime.Serialization;

namespace System
{
    public struct Decimal : IFormattable, IComparable, IConvertible, IDeserializationCallback, IComparable<Decimal>, IEquatable<Decimal>
    {
        private const int SignMask = unchecked ((int)0x80000000);
        private const byte DECIMAL_NEG = 0x80;
        private const byte DECIMAL_ADD = 0x00;
        private const int ScaleMask = 0x00FF0000;
        private const int ScaleShift = 16;
        private const Int32 MaxInt32Scale = 9;
        private static UInt32[] Powers10 = new UInt32[]{1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000};
        public const Decimal Zero = 0m;
        public const Decimal One = 1m;
        public const Decimal MinusOne = -1m;
        public const Decimal MaxValue = 79228162514264337593543950335m;
        public const Decimal MinValue = -79228162514264337593543950335m;
        private const Decimal NearNegativeZero = -0.000000000000000000000000001m;
        private const Decimal NearPositiveZero = +0.000000000000000000000000001m;
        private int flags;
        private int hi;
        private int lo;
        private int mid;
        public Decimal(int value)
        {
            int value_copy = value;
            if (value_copy >= 0)
            {
                flags = 0;
            }
            else
            {
                flags = SignMask;
                value_copy = -value_copy;
            }

            lo = value_copy;
            mid = 0;
            hi = 0;
        }

        public Decimal(uint value)
        {
            flags = 0;
            lo = (int)value;
            mid = 0;
            hi = 0;
        }

        public Decimal(long value)
        {
            long value_copy = value;
            if (value_copy >= 0)
            {
                flags = 0;
            }
            else
            {
                flags = SignMask;
                value_copy = -value_copy;
            }

            lo = (int)value_copy;
            mid = (int)(value_copy >> 32);
            hi = 0;
        }

        public Decimal(ulong value)
        {
            flags = 0;
            lo = (int)value;
            mid = (int)(value >> 32);
            hi = 0;
        }

        public extern Decimal(float value);
        public extern Decimal(double value);
        internal Decimal(Currency value)
        {
            this = Currency.ToDecimal(value);
        }

        public static long ToOACurrency(Decimal value)
        {
            return new Currency(value).ToOACurrency();
        }

        public static Decimal FromOACurrency(long cy)
        {
            return Currency.ToDecimal(Currency.FromOACurrency(cy));
        }

        public Decimal(int[] bits)
        {
            this.lo = 0;
            this.mid = 0;
            this.hi = 0;
            this.flags = 0;
            SetBits(bits);
        }

        private void SetBits(int[] bits)
        {
            if (bits == null)
                throw new ArgumentNullException("bits");
                        if (bits.Length == 4)
            {
                int f = bits[3];
                if ((f & ~(SignMask | ScaleMask)) == 0 && (f & ScaleMask) <= (28 << 16))
                {
                    lo = bits[0];
                    mid = bits[1];
                    hi = bits[2];
                    flags = f;
                    return;
                }
            }

            throw new ArgumentException(Environment.GetResourceString("Arg_DecBitCtor"));
        }

        public Decimal(int lo, int mid, int hi, bool isNegative, byte scale)
        {
            if (scale > 28)
                throw new ArgumentOutOfRangeException("scale", Environment.GetResourceString("ArgumentOutOfRange_DecimalScale"));
                        this.lo = lo;
            this.mid = mid;
            this.hi = hi;
            this.flags = ((int)scale) << 16;
            if (isNegative)
                this.flags |= SignMask;
        }

        private Decimal(int lo, int mid, int hi, int flags)
        {
            if ((flags & ~(SignMask | ScaleMask)) == 0 && (flags & ScaleMask) <= (28 << 16))
            {
                this.lo = lo;
                this.mid = mid;
                this.hi = hi;
                this.flags = flags;
                return;
            }

            throw new ArgumentException(Environment.GetResourceString("Arg_DecBitCtor"));
        }

        internal static Decimal Abs(Decimal d)
        {
            return new Decimal(d.lo, d.mid, d.hi, d.flags & ~SignMask);
        }

        public static Decimal Add(Decimal d1, Decimal d2)
        {
            FCallAddSub(ref d1, ref d2, DECIMAL_ADD);
            return d1;
        }

        private static extern void FCallAddSub(ref Decimal d1, ref Decimal d2, byte bSign);
        private static extern void FCallAddSubOverflowed(ref Decimal d1, ref Decimal d2, byte bSign, ref bool overflowed);
        public static Decimal Ceiling(Decimal d)
        {
            return (-(Decimal.Floor(-d)));
        }

        public static int Compare(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2);
        }

        private static extern int FCallCompare(ref Decimal d1, ref Decimal d2);
        public int CompareTo(Object value)
        {
            if (value == null)
                return 1;
            if (!(value is Decimal))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDecimal"));
            Decimal other = (Decimal)value;
            return FCallCompare(ref this, ref other);
        }

        public int CompareTo(Decimal value)
        {
            return FCallCompare(ref this, ref value);
        }

        public static Decimal Divide(Decimal d1, Decimal d2)
        {
            FCallDivide(ref d1, ref d2);
            return d1;
        }

        private static extern void FCallDivide(ref Decimal d1, ref Decimal d2);
        private static extern void FCallDivideOverflowed(ref Decimal d1, ref Decimal d2, ref bool overflowed);
        public override bool Equals(Object value)
        {
            if (value is Decimal)
            {
                Decimal other = (Decimal)value;
                return FCallCompare(ref this, ref other) == 0;
            }

            return false;
        }

        public bool Equals(Decimal value)
        {
            return FCallCompare(ref this, ref value) == 0;
        }

        public extern override int GetHashCode();
        public static bool Equals(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) == 0;
        }

        public static Decimal Floor(Decimal d)
        {
            FCallFloor(ref d);
            return d;
        }

        private static extern void FCallFloor(ref Decimal d);
        public override String ToString()
        {
                        return Number.FormatDecimal(this, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format)
        {
                        return Number.FormatDecimal(this, format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
                        return Number.FormatDecimal(this, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format, IFormatProvider provider)
        {
                        return Number.FormatDecimal(this, format, NumberFormatInfo.GetInstance(provider));
        }

        public static Decimal Parse(String s)
        {
            return Number.ParseDecimal(s, NumberStyles.Number, NumberFormatInfo.CurrentInfo);
        }

        public static Decimal Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimal(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static Decimal Parse(String s, IFormatProvider provider)
        {
            return Number.ParseDecimal(s, NumberStyles.Number, NumberFormatInfo.GetInstance(provider));
        }

        public static Decimal Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseDecimal(s, style, NumberFormatInfo.GetInstance(provider));
        }

        public static Boolean TryParse(String s, out Decimal result)
        {
            return Number.TryParseDecimal(s, NumberStyles.Number, NumberFormatInfo.CurrentInfo, out result);
        }

        public static Boolean TryParse(String s, NumberStyles style, IFormatProvider provider, out Decimal result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseDecimal(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static int[] GetBits(Decimal d)
        {
            return new int[]{d.lo, d.mid, d.hi, d.flags};
        }

        internal static void GetBytes(Decimal d, byte[] buffer)
        {
                        buffer[0] = (byte)d.lo;
            buffer[1] = (byte)(d.lo >> 8);
            buffer[2] = (byte)(d.lo >> 16);
            buffer[3] = (byte)(d.lo >> 24);
            buffer[4] = (byte)d.mid;
            buffer[5] = (byte)(d.mid >> 8);
            buffer[6] = (byte)(d.mid >> 16);
            buffer[7] = (byte)(d.mid >> 24);
            buffer[8] = (byte)d.hi;
            buffer[9] = (byte)(d.hi >> 8);
            buffer[10] = (byte)(d.hi >> 16);
            buffer[11] = (byte)(d.hi >> 24);
            buffer[12] = (byte)d.flags;
            buffer[13] = (byte)(d.flags >> 8);
            buffer[14] = (byte)(d.flags >> 16);
            buffer[15] = (byte)(d.flags >> 24);
        }

        internal static decimal ToDecimal(byte[] buffer)
        {
                        int lo = ((int)buffer[0]) | ((int)buffer[1] << 8) | ((int)buffer[2] << 16) | ((int)buffer[3] << 24);
            int mid = ((int)buffer[4]) | ((int)buffer[5] << 8) | ((int)buffer[6] << 16) | ((int)buffer[7] << 24);
            int hi = ((int)buffer[8]) | ((int)buffer[9] << 8) | ((int)buffer[10] << 16) | ((int)buffer[11] << 24);
            int flags = ((int)buffer[12]) | ((int)buffer[13] << 8) | ((int)buffer[14] << 16) | ((int)buffer[15] << 24);
            return new Decimal(lo, mid, hi, flags);
        }

        private static void InternalAddUInt32RawUnchecked(ref Decimal value, UInt32 i)
        {
            UInt32 v;
            UInt32 sum;
            v = (UInt32)value.lo;
            sum = v + i;
            value.lo = (Int32)sum;
            if (sum < v || sum < i)
            {
                v = (UInt32)value.mid;
                sum = v + 1;
                value.mid = (Int32)sum;
                if (sum < v || sum < 1)
                {
                    value.hi = (Int32)((UInt32)value.hi + 1);
                }
            }
        }

        private static UInt32 InternalDivRemUInt32(ref Decimal value, UInt32 divisor)
        {
            UInt32 remainder = 0;
            UInt64 n;
            if (value.hi != 0)
            {
                n = ((UInt32)value.hi);
                value.hi = (Int32)((UInt32)(n / divisor));
                remainder = (UInt32)(n % divisor);
            }

            if (value.mid != 0 || remainder != 0)
            {
                n = ((UInt64)remainder << 32) | (UInt32)value.mid;
                value.mid = (Int32)((UInt32)(n / divisor));
                remainder = (UInt32)(n % divisor);
            }

            if (value.lo != 0 || remainder != 0)
            {
                n = ((UInt64)remainder << 32) | (UInt32)value.lo;
                value.lo = (Int32)((UInt32)(n / divisor));
                remainder = (UInt32)(n % divisor);
            }

            return remainder;
        }

        private static void InternalRoundFromZero(ref Decimal d, int decimalCount)
        {
            Int32 scale = (d.flags & ScaleMask) >> ScaleShift;
            Int32 scaleDifference = scale - decimalCount;
            if (scaleDifference <= 0)
            {
                return;
            }

            UInt32 lastRemainder;
            UInt32 lastDivisor;
            do
            {
                Int32 diffChunk = (scaleDifference > MaxInt32Scale) ? MaxInt32Scale : scaleDifference;
                lastDivisor = Powers10[diffChunk];
                lastRemainder = InternalDivRemUInt32(ref d, lastDivisor);
                scaleDifference -= diffChunk;
            }
            while (scaleDifference > 0);
            if (lastRemainder >= (lastDivisor >> 1))
            {
                InternalAddUInt32RawUnchecked(ref d, 1);
            }

            d.flags = ((decimalCount << ScaleShift) & ScaleMask) | (d.flags & SignMask);
        }

        internal static Decimal Max(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) >= 0 ? d1 : d2;
        }

        internal static Decimal Min(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) < 0 ? d1 : d2;
        }

        public static Decimal Remainder(Decimal d1, Decimal d2)
        {
            d2.flags = (d2.flags & ~SignMask) | (d1.flags & SignMask);
            if (Abs(d1) < Abs(d2))
            {
                return d1;
            }

            d1 -= d2;
            if (d1 == 0)
            {
                d1.flags = (d1.flags & ~SignMask) | (d2.flags & SignMask);
            }

            Decimal dividedResult = Truncate(d1 / d2);
            Decimal multipliedResult = dividedResult * d2;
            Decimal result = d1 - multipliedResult;
            if ((d1.flags & SignMask) != (result.flags & SignMask))
            {
                if (NearNegativeZero <= result && result <= NearPositiveZero)
                {
                    result.flags = (result.flags & ~SignMask) | (d1.flags & SignMask);
                }
                else
                {
                    result += d2;
                }
            }

            return result;
        }

        public static Decimal Multiply(Decimal d1, Decimal d2)
        {
            FCallMultiply(ref d1, ref d2);
            return d1;
        }

        private static extern void FCallMultiply(ref Decimal d1, ref Decimal d2);
        private static extern void FCallMultiplyOverflowed(ref Decimal d1, ref Decimal d2, ref bool overflowed);
        public static Decimal Negate(Decimal d)
        {
            return new Decimal(d.lo, d.mid, d.hi, d.flags ^ SignMask);
        }

        public static Decimal Round(Decimal d)
        {
            return Round(d, 0);
        }

        public static Decimal Round(Decimal d, int decimals)
        {
            FCallRound(ref d, decimals);
            return d;
        }

        public static Decimal Round(Decimal d, MidpointRounding mode)
        {
            return Round(d, 0, mode);
        }

        public static Decimal Round(Decimal d, int decimals, MidpointRounding mode)
        {
            if ((decimals < 0) || (decimals > 28))
                throw new ArgumentOutOfRangeException("decimals", Environment.GetResourceString("ArgumentOutOfRange_DecimalRound"));
            if (mode < MidpointRounding.ToEven || mode > MidpointRounding.AwayFromZero)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEnumValue", mode, "MidpointRounding"), "mode");
            }

                        if (mode == MidpointRounding.ToEven)
            {
                FCallRound(ref d, decimals);
            }
            else
            {
                InternalRoundFromZero(ref d, decimals);
            }

            return d;
        }

        private static extern void FCallRound(ref Decimal d, int decimals);
        public static Decimal Subtract(Decimal d1, Decimal d2)
        {
            FCallAddSub(ref d1, ref d2, DECIMAL_NEG);
            return d1;
        }

        public static byte ToByte(Decimal value)
        {
            uint temp;
            try
            {
                temp = ToUInt32(value);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_Byte"), e);
            }

            if (temp < Byte.MinValue || temp > Byte.MaxValue)
                throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
            return (byte)temp;
        }

        public static sbyte ToSByte(Decimal value)
        {
            int temp;
            try
            {
                temp = ToInt32(value);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_SByte"), e);
            }

            if (temp < SByte.MinValue || temp > SByte.MaxValue)
                throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
            return (sbyte)temp;
        }

        public static short ToInt16(Decimal value)
        {
            int temp;
            try
            {
                temp = ToInt32(value);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_Int16"), e);
            }

            if (temp < Int16.MinValue || temp > Int16.MaxValue)
                throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
            return (short)temp;
        }

        internal static Currency ToCurrency(Decimal d)
        {
            Currency result = new Currency();
            FCallToCurrency(ref result, d);
            return result;
        }

        private static extern void FCallToCurrency(ref Currency result, Decimal d);
        public static extern double ToDouble(Decimal d);
        internal static extern int FCallToInt32(Decimal d);
        public static int ToInt32(Decimal d)
        {
            if ((d.flags & ScaleMask) != 0)
                FCallTruncate(ref d);
            if (d.hi == 0 && d.mid == 0)
            {
                int i = d.lo;
                if (d.flags >= 0)
                {
                    if (i >= 0)
                        return i;
                }
                else
                {
                    i = -i;
                    if (i <= 0)
                        return i;
                }
            }

            throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
        }

        public static long ToInt64(Decimal d)
        {
            if ((d.flags & ScaleMask) != 0)
                FCallTruncate(ref d);
            if (d.hi == 0)
            {
                long l = d.lo & 0xFFFFFFFFL | (long)d.mid << 32;
                if (d.flags >= 0)
                {
                    if (l >= 0)
                        return l;
                }
                else
                {
                    l = -l;
                    if (l <= 0)
                        return l;
                }
            }

            throw new OverflowException(Environment.GetResourceString("Overflow_Int64"));
        }

        public static ushort ToUInt16(Decimal value)
        {
            uint temp;
            try
            {
                temp = ToUInt32(value);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"), e);
            }

            if (temp < UInt16.MinValue || temp > UInt16.MaxValue)
                throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
            return (ushort)temp;
        }

        public static uint ToUInt32(Decimal d)
        {
            if ((d.flags & ScaleMask) != 0)
                FCallTruncate(ref d);
            if (d.hi == 0 && d.mid == 0)
            {
                uint i = (uint)d.lo;
                if (d.flags >= 0 || i == 0)
                    return i;
            }

            throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
        }

        public static ulong ToUInt64(Decimal d)
        {
            if ((d.flags & ScaleMask) != 0)
                FCallTruncate(ref d);
            if (d.hi == 0)
            {
                ulong l = ((ulong)(uint)d.lo) | ((ulong)(uint)d.mid << 32);
                if (d.flags >= 0 || l == 0)
                    return l;
            }

            throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
        }

        public static extern float ToSingle(Decimal d);
        public static Decimal Truncate(Decimal d)
        {
            FCallTruncate(ref d);
            return d;
        }

        private static extern void FCallTruncate(ref Decimal d);
        public static implicit operator Decimal(byte value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(sbyte value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(short value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(ushort value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(char value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(int value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(uint value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(long value)
        {
            return new Decimal(value);
        }

        public static implicit operator Decimal(ulong value)
        {
            return new Decimal(value);
        }

        public static explicit operator Decimal(float value)
        {
            return new Decimal(value);
        }

        public static explicit operator Decimal(double value)
        {
            return new Decimal(value);
        }

        public static explicit operator byte (Decimal value)
        {
            return ToByte(value);
        }

        public static explicit operator sbyte (Decimal value)
        {
            return ToSByte(value);
        }

        public static explicit operator char (Decimal value)
        {
            UInt16 temp;
            try
            {
                temp = ToUInt16(value);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(Environment.GetResourceString("Overflow_Char"), e);
            }

            return (char)temp;
        }

        public static explicit operator short (Decimal value)
        {
            return ToInt16(value);
        }

        public static explicit operator ushort (Decimal value)
        {
            return ToUInt16(value);
        }

        public static explicit operator int (Decimal value)
        {
            return ToInt32(value);
        }

        public static explicit operator uint (Decimal value)
        {
            return ToUInt32(value);
        }

        public static explicit operator long (Decimal value)
        {
            return ToInt64(value);
        }

        public static explicit operator ulong (Decimal value)
        {
            return ToUInt64(value);
        }

        public static explicit operator float (Decimal value)
        {
            return ToSingle(value);
        }

        public static explicit operator double (Decimal value)
        {
            return ToDouble(value);
        }

        public static Decimal operator +(Decimal d)
        {
            return d;
        }

        public static Decimal operator -(Decimal d)
        {
            return Negate(d);
        }

        public static Decimal operator ++(Decimal d)
        {
            return Add(d, One);
        }

        public static Decimal operator --(Decimal d)
        {
            return Subtract(d, One);
        }

        public static Decimal operator +(Decimal d1, Decimal d2)
        {
            FCallAddSub(ref d1, ref d2, DECIMAL_ADD);
            return d1;
        }

        public static Decimal operator -(Decimal d1, Decimal d2)
        {
            FCallAddSub(ref d1, ref d2, DECIMAL_NEG);
            return d1;
        }

        public static Decimal operator *(Decimal d1, Decimal d2)
        {
            FCallMultiply(ref d1, ref d2);
            return d1;
        }

        public static Decimal operator /(Decimal d1, Decimal d2)
        {
            FCallDivide(ref d1, ref d2);
            return d1;
        }

        public static Decimal operator %(Decimal d1, Decimal d2)
        {
            return Remainder(d1, d2);
        }

        public static bool operator ==(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) == 0;
        }

        public static bool operator !=(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) != 0;
        }

        public static bool operator <(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) < 0;
        }

        public static bool operator <=(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) <= 0;
        }

        public static bool operator>(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) > 0;
        }

        public static bool operator >=(Decimal d1, Decimal d2)
        {
            return FCallCompare(ref d1, ref d2) >= 0;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Decimal;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(this);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Decimal", "Char"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(this);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(this);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(this);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(this);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(this);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(this);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(this);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(this);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(this);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(this);
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return this;
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Decimal", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}