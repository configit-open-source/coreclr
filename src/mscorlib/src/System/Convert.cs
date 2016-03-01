using System.Diagnostics.Contracts;
using System.Globalization;
using System.Threading;

namespace System
{
    [Flags]
    public enum Base64FormattingOptions
    {
        None = 0,
        InsertLineBreaks = 1
    }

    public static class Convert
    {
        internal static readonly RuntimeType[] ConvertTypes = {(RuntimeType)typeof (System.Empty), (RuntimeType)typeof (Object), (RuntimeType)typeof (System.DBNull), (RuntimeType)typeof (Boolean), (RuntimeType)typeof (Char), (RuntimeType)typeof (SByte), (RuntimeType)typeof (Byte), (RuntimeType)typeof (Int16), (RuntimeType)typeof (UInt16), (RuntimeType)typeof (Int32), (RuntimeType)typeof (UInt32), (RuntimeType)typeof (Int64), (RuntimeType)typeof (UInt64), (RuntimeType)typeof (Single), (RuntimeType)typeof (Double), (RuntimeType)typeof (Decimal), (RuntimeType)typeof (DateTime), (RuntimeType)typeof (Object), (RuntimeType)typeof (String)};
        private static readonly RuntimeType EnumType = (RuntimeType)typeof (Enum);
        internal static readonly char[] base64Table = {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/', '='};
        private const Int32 base64LineBreakPosition = 76;
        private static bool TriggerAsserts = DoAsserts();
        private static bool DoAsserts()
        {
            Contract.Assert(ConvertTypes != null, "[Convert.cctor]ConvertTypes!=null");
            Contract.Assert(ConvertTypes.Length == ((int)TypeCode.String + 1), "[Convert.cctor]ConvertTypes.Length == ((int)TypeCode.String + 1)");
            Contract.Assert(ConvertTypes[(int)TypeCode.Empty] == typeof (System.Empty), "[Convert.cctor]ConvertTypes[(int)TypeCode.Empty]==typeof(System.Empty)");
            Contract.Assert(ConvertTypes[(int)TypeCode.String] == typeof (String), "[Convert.cctor]ConvertTypes[(int)TypeCode.String]==typeof(System.String)");
            Contract.Assert(ConvertTypes[(int)TypeCode.Int32] == typeof (int), "[Convert.cctor]ConvertTypes[(int)TypeCode.Int32]==typeof(int)");
            return true;
        }

        public static readonly Object DBNull = System.DBNull.Value;
        public static TypeCode GetTypeCode(object value)
        {
            if (value == null)
                return TypeCode.Empty;
            IConvertible temp = value as IConvertible;
            if (temp != null)
            {
                return temp.GetTypeCode();
            }

            return TypeCode.Object;
        }

        public static bool IsDBNull(object value)
        {
            if (value == System.DBNull.Value)
                return true;
            IConvertible convertible = value as IConvertible;
            return convertible != null ? convertible.GetTypeCode() == TypeCode.DBNull : false;
        }

        public static Object ChangeType(Object value, TypeCode typeCode)
        {
            return ChangeType(value, typeCode, Thread.CurrentThread.CurrentCulture);
        }

        public static Object ChangeType(Object value, TypeCode typeCode, IFormatProvider provider)
        {
            if (value == null && (typeCode == TypeCode.Empty || typeCode == TypeCode.String || typeCode == TypeCode.Object))
            {
                return null;
            }

            IConvertible v = value as IConvertible;
            if (v == null)
            {
                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_IConvertible"));
            }

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return v.ToBoolean(provider);
                case TypeCode.Char:
                    return v.ToChar(provider);
                case TypeCode.SByte:
                    return v.ToSByte(provider);
                case TypeCode.Byte:
                    return v.ToByte(provider);
                case TypeCode.Int16:
                    return v.ToInt16(provider);
                case TypeCode.UInt16:
                    return v.ToUInt16(provider);
                case TypeCode.Int32:
                    return v.ToInt32(provider);
                case TypeCode.UInt32:
                    return v.ToUInt32(provider);
                case TypeCode.Int64:
                    return v.ToInt64(provider);
                case TypeCode.UInt64:
                    return v.ToUInt64(provider);
                case TypeCode.Single:
                    return v.ToSingle(provider);
                case TypeCode.Double:
                    return v.ToDouble(provider);
                case TypeCode.Decimal:
                    return v.ToDecimal(provider);
                case TypeCode.DateTime:
                    return v.ToDateTime(provider);
                case TypeCode.String:
                    return v.ToString(provider);
                case TypeCode.Object:
                    return value;
                case TypeCode.DBNull:
                    throw new InvalidCastException(Environment.GetResourceString("InvalidCast_DBNull"));
                case TypeCode.Empty:
                    throw new InvalidCastException(Environment.GetResourceString("InvalidCast_Empty"));
                default:
                    throw new ArgumentException(Environment.GetResourceString("Arg_UnknownTypeCode"));
            }
        }

        internal static Object DefaultToType(IConvertible value, Type targetType, IFormatProvider provider)
        {
            Contract.Requires(value != null, "[Convert.DefaultToType]value!=null");
            if (targetType == null)
            {
                throw new ArgumentNullException("targetType");
            }

            Contract.EndContractBlock();
            RuntimeType rtTargetType = targetType as RuntimeType;
            if (rtTargetType != null)
            {
                if (value.GetType() == targetType)
                {
                    return value;
                }

                if (rtTargetType == ConvertTypes[(int)TypeCode.Boolean])
                    return value.ToBoolean(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Char])
                    return value.ToChar(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.SByte])
                    return value.ToSByte(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Byte])
                    return value.ToByte(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Int16])
                    return value.ToInt16(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.UInt16])
                    return value.ToUInt16(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Int32])
                    return value.ToInt32(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.UInt32])
                    return value.ToUInt32(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Int64])
                    return value.ToInt64(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.UInt64])
                    return value.ToUInt64(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Single])
                    return value.ToSingle(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Double])
                    return value.ToDouble(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Decimal])
                    return value.ToDecimal(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.DateTime])
                    return value.ToDateTime(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.String])
                    return value.ToString(provider);
                if (rtTargetType == ConvertTypes[(int)TypeCode.Object])
                    return (Object)value;
                if (rtTargetType == EnumType)
                    return (Enum)value;
                if (rtTargetType == ConvertTypes[(int)TypeCode.DBNull])
                    throw new InvalidCastException(Environment.GetResourceString("InvalidCast_DBNull"));
                if (rtTargetType == ConvertTypes[(int)TypeCode.Empty])
                    throw new InvalidCastException(Environment.GetResourceString("InvalidCast_Empty"));
            }

            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", value.GetType().FullName, targetType.FullName));
        }

        public static Object ChangeType(Object value, Type conversionType)
        {
            return ChangeType(value, conversionType, Thread.CurrentThread.CurrentCulture);
        }

        public static Object ChangeType(Object value, Type conversionType, IFormatProvider provider)
        {
            if (conversionType == null)
            {
                throw new ArgumentNullException("conversionType");
            }

            Contract.EndContractBlock();
            if (value == null)
            {
                if (conversionType.IsValueType)
                {
                    throw new InvalidCastException(Environment.GetResourceString("InvalidCast_CannotCastNullToValueType"));
                }

                return null;
            }

            IConvertible ic = value as IConvertible;
            if (ic == null)
            {
                if (value.GetType() == conversionType)
                {
                    return value;
                }

                throw new InvalidCastException(Environment.GetResourceString("InvalidCast_IConvertible"));
            }

            RuntimeType rtConversionType = conversionType as RuntimeType;
            if (rtConversionType == ConvertTypes[(int)TypeCode.Boolean])
                return ic.ToBoolean(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Char])
                return ic.ToChar(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.SByte])
                return ic.ToSByte(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Byte])
                return ic.ToByte(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Int16])
                return ic.ToInt16(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.UInt16])
                return ic.ToUInt16(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Int32])
                return ic.ToInt32(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.UInt32])
                return ic.ToUInt32(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Int64])
                return ic.ToInt64(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.UInt64])
                return ic.ToUInt64(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Single])
                return ic.ToSingle(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Double])
                return ic.ToDouble(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Decimal])
                return ic.ToDecimal(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.DateTime])
                return ic.ToDateTime(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.String])
                return ic.ToString(provider);
            if (rtConversionType == ConvertTypes[(int)TypeCode.Object])
                return (Object)value;
            return ic.ToType(conversionType, provider);
        }

        private static void ThrowCharOverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_Char"));
        }

        private static void ThrowByteOverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_Byte"));
        }

        private static void ThrowSByteOverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_SByte"));
        }

        private static void ThrowInt16OverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
        }

        private static void ThrowUInt16OverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
        }

        private static void ThrowInt32OverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
        }

        private static void ThrowUInt32OverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
        }

        private static void ThrowInt64OverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_Int64"));
        }

        private static void ThrowUInt64OverflowException()
        {
            throw new OverflowException(Environment.GetResourceString("Overflow_UInt64"));
        }

        public static bool ToBoolean(Object value)
        {
            return value == null ? false : ((IConvertible)value).ToBoolean(null);
        }

        public static bool ToBoolean(Object value, IFormatProvider provider)
        {
            return value == null ? false : ((IConvertible)value).ToBoolean(provider);
        }

        public static bool ToBoolean(bool value)
        {
            return value;
        }

        public static bool ToBoolean(sbyte value)
        {
            return value != 0;
        }

        public static bool ToBoolean(char value)
        {
            return ((IConvertible)value).ToBoolean(null);
        }

        public static bool ToBoolean(byte value)
        {
            return value != 0;
        }

        public static bool ToBoolean(short value)
        {
            return value != 0;
        }

        public static bool ToBoolean(ushort value)
        {
            return value != 0;
        }

        public static bool ToBoolean(int value)
        {
            return value != 0;
        }

        public static bool ToBoolean(uint value)
        {
            return value != 0;
        }

        public static bool ToBoolean(long value)
        {
            return value != 0;
        }

        public static bool ToBoolean(ulong value)
        {
            return value != 0;
        }

        public static bool ToBoolean(String value)
        {
            if (value == null)
                return false;
            return Boolean.Parse(value);
        }

        public static bool ToBoolean(String value, IFormatProvider provider)
        {
            if (value == null)
                return false;
            return Boolean.Parse(value);
        }

        public static bool ToBoolean(float value)
        {
            return value != 0;
        }

        public static bool ToBoolean(double value)
        {
            return value != 0;
        }

        public static bool ToBoolean(decimal value)
        {
            return value != 0;
        }

        public static bool ToBoolean(DateTime value)
        {
            return ((IConvertible)value).ToBoolean(null);
        }

        public static char ToChar(object value)
        {
            return value == null ? (char)0 : ((IConvertible)value).ToChar(null);
        }

        public static char ToChar(object value, IFormatProvider provider)
        {
            return value == null ? (char)0 : ((IConvertible)value).ToChar(provider);
        }

        public static char ToChar(bool value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        public static char ToChar(char value)
        {
            return value;
        }

        public static char ToChar(sbyte value)
        {
            if (value < 0)
                ThrowCharOverflowException();
            Contract.EndContractBlock();
            return (char)value;
        }

        public static char ToChar(byte value)
        {
            return (char)value;
        }

        public static char ToChar(short value)
        {
            if (value < 0)
                ThrowCharOverflowException();
            Contract.EndContractBlock();
            return (char)value;
        }

        public static char ToChar(ushort value)
        {
            return (char)value;
        }

        public static char ToChar(int value)
        {
            if (value < 0 || value > Char.MaxValue)
                ThrowCharOverflowException();
            Contract.EndContractBlock();
            return (char)value;
        }

        public static char ToChar(uint value)
        {
            if (value > Char.MaxValue)
                ThrowCharOverflowException();
            Contract.EndContractBlock();
            return (char)value;
        }

        public static char ToChar(long value)
        {
            if (value < 0 || value > Char.MaxValue)
                ThrowCharOverflowException();
            Contract.EndContractBlock();
            return (char)value;
        }

        public static char ToChar(ulong value)
        {
            if (value > Char.MaxValue)
                ThrowCharOverflowException();
            Contract.EndContractBlock();
            return (char)value;
        }

        public static char ToChar(String value)
        {
            return ToChar(value, null);
        }

        public static char ToChar(String value, IFormatProvider provider)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            if (value.Length != 1)
                throw new FormatException(Environment.GetResourceString(ResId.Format_NeedSingleChar));
            return value[0];
        }

        public static char ToChar(float value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        public static char ToChar(double value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        public static char ToChar(decimal value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        public static char ToChar(DateTime value)
        {
            return ((IConvertible)value).ToChar(null);
        }

        public static sbyte ToSByte(object value)
        {
            return value == null ? (sbyte)0 : ((IConvertible)value).ToSByte(null);
        }

        public static sbyte ToSByte(object value, IFormatProvider provider)
        {
            return value == null ? (sbyte)0 : ((IConvertible)value).ToSByte(provider);
        }

        public static sbyte ToSByte(bool value)
        {
            return value ? (sbyte)Boolean.True : (sbyte)Boolean.False;
        }

        public static sbyte ToSByte(sbyte value)
        {
            return value;
        }

        public static sbyte ToSByte(char value)
        {
            if (value > SByte.MaxValue)
                ThrowSByteOverflowException();
            Contract.EndContractBlock();
            return (sbyte)value;
        }

        public static sbyte ToSByte(byte value)
        {
            if (value > SByte.MaxValue)
                ThrowSByteOverflowException();
            Contract.EndContractBlock();
            return (sbyte)value;
        }

        public static sbyte ToSByte(short value)
        {
            if (value < SByte.MinValue || value > SByte.MaxValue)
                ThrowSByteOverflowException();
            Contract.EndContractBlock();
            return (sbyte)value;
        }

        public static sbyte ToSByte(ushort value)
        {
            if (value > SByte.MaxValue)
                ThrowSByteOverflowException();
            Contract.EndContractBlock();
            return (sbyte)value;
        }

        public static sbyte ToSByte(int value)
        {
            if (value < SByte.MinValue || value > SByte.MaxValue)
                ThrowSByteOverflowException();
            Contract.EndContractBlock();
            return (sbyte)value;
        }

        public static sbyte ToSByte(uint value)
        {
            if (value > SByte.MaxValue)
                ThrowSByteOverflowException();
            Contract.EndContractBlock();
            return (sbyte)value;
        }

        public static sbyte ToSByte(long value)
        {
            if (value < SByte.MinValue || value > SByte.MaxValue)
                ThrowSByteOverflowException();
            Contract.EndContractBlock();
            return (sbyte)value;
        }

        public static sbyte ToSByte(ulong value)
        {
            if (value > (ulong)SByte.MaxValue)
                ThrowSByteOverflowException();
            Contract.EndContractBlock();
            return (sbyte)value;
        }

        public static sbyte ToSByte(float value)
        {
            return ToSByte((double)value);
        }

        public static sbyte ToSByte(double value)
        {
            return ToSByte(ToInt32(value));
        }

        public static sbyte ToSByte(decimal value)
        {
            return Decimal.ToSByte(Decimal.Round(value, 0));
        }

        public static sbyte ToSByte(String value)
        {
            if (value == null)
                return 0;
            return SByte.Parse(value, CultureInfo.CurrentCulture);
        }

        public static sbyte ToSByte(String value, IFormatProvider provider)
        {
            return SByte.Parse(value, NumberStyles.Integer, provider);
        }

        public static sbyte ToSByte(DateTime value)
        {
            return ((IConvertible)value).ToSByte(null);
        }

        public static byte ToByte(object value)
        {
            return value == null ? (byte)0 : ((IConvertible)value).ToByte(null);
        }

        public static byte ToByte(object value, IFormatProvider provider)
        {
            return value == null ? (byte)0 : ((IConvertible)value).ToByte(provider);
        }

        public static byte ToByte(bool value)
        {
            return value ? (byte)Boolean.True : (byte)Boolean.False;
        }

        public static byte ToByte(byte value)
        {
            return value;
        }

        public static byte ToByte(char value)
        {
            if (value > Byte.MaxValue)
                ThrowByteOverflowException();
            Contract.EndContractBlock();
            return (byte)value;
        }

        public static byte ToByte(sbyte value)
        {
            if (value < Byte.MinValue)
                ThrowByteOverflowException();
            Contract.EndContractBlock();
            return (byte)value;
        }

        public static byte ToByte(short value)
        {
            if (value < Byte.MinValue || value > Byte.MaxValue)
                ThrowByteOverflowException();
            Contract.EndContractBlock();
            return (byte)value;
        }

        public static byte ToByte(ushort value)
        {
            if (value > Byte.MaxValue)
                ThrowByteOverflowException();
            Contract.EndContractBlock();
            return (byte)value;
        }

        public static byte ToByte(int value)
        {
            if (value < Byte.MinValue || value > Byte.MaxValue)
                ThrowByteOverflowException();
            Contract.EndContractBlock();
            return (byte)value;
        }

        public static byte ToByte(uint value)
        {
            if (value > Byte.MaxValue)
                ThrowByteOverflowException();
            Contract.EndContractBlock();
            return (byte)value;
        }

        public static byte ToByte(long value)
        {
            if (value < Byte.MinValue || value > Byte.MaxValue)
                ThrowByteOverflowException();
            Contract.EndContractBlock();
            return (byte)value;
        }

        public static byte ToByte(ulong value)
        {
            if (value > Byte.MaxValue)
                ThrowByteOverflowException();
            Contract.EndContractBlock();
            return (byte)value;
        }

        public static byte ToByte(float value)
        {
            return ToByte((double)value);
        }

        public static byte ToByte(double value)
        {
            return ToByte(ToInt32(value));
        }

        public static byte ToByte(decimal value)
        {
            return Decimal.ToByte(Decimal.Round(value, 0));
        }

        public static byte ToByte(String value)
        {
            if (value == null)
                return 0;
            return Byte.Parse(value, CultureInfo.CurrentCulture);
        }

        public static byte ToByte(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return Byte.Parse(value, NumberStyles.Integer, provider);
        }

        public static byte ToByte(DateTime value)
        {
            return ((IConvertible)value).ToByte(null);
        }

        public static short ToInt16(object value)
        {
            return value == null ? (short)0 : ((IConvertible)value).ToInt16(null);
        }

        public static short ToInt16(object value, IFormatProvider provider)
        {
            return value == null ? (short)0 : ((IConvertible)value).ToInt16(provider);
        }

        public static short ToInt16(bool value)
        {
            return value ? (short)Boolean.True : (short)Boolean.False;
        }

        public static short ToInt16(char value)
        {
            if (value > Int16.MaxValue)
                ThrowInt16OverflowException();
            Contract.EndContractBlock();
            return (short)value;
        }

        public static short ToInt16(sbyte value)
        {
            return value;
        }

        public static short ToInt16(byte value)
        {
            return value;
        }

        public static short ToInt16(ushort value)
        {
            if (value > Int16.MaxValue)
                ThrowInt16OverflowException();
            Contract.EndContractBlock();
            return (short)value;
        }

        public static short ToInt16(int value)
        {
            if (value < Int16.MinValue || value > Int16.MaxValue)
                ThrowInt16OverflowException();
            Contract.EndContractBlock();
            return (short)value;
        }

        public static short ToInt16(uint value)
        {
            if (value > Int16.MaxValue)
                ThrowInt16OverflowException();
            Contract.EndContractBlock();
            return (short)value;
        }

        public static short ToInt16(short value)
        {
            return value;
        }

        public static short ToInt16(long value)
        {
            if (value < Int16.MinValue || value > Int16.MaxValue)
                ThrowInt16OverflowException();
            Contract.EndContractBlock();
            return (short)value;
        }

        public static short ToInt16(ulong value)
        {
            if (value > (ulong)Int16.MaxValue)
                ThrowInt16OverflowException();
            Contract.EndContractBlock();
            return (short)value;
        }

        public static short ToInt16(float value)
        {
            return ToInt16((double)value);
        }

        public static short ToInt16(double value)
        {
            return ToInt16(ToInt32(value));
        }

        public static short ToInt16(decimal value)
        {
            return Decimal.ToInt16(Decimal.Round(value, 0));
        }

        public static short ToInt16(String value)
        {
            if (value == null)
                return 0;
            return Int16.Parse(value, CultureInfo.CurrentCulture);
        }

        public static short ToInt16(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return Int16.Parse(value, NumberStyles.Integer, provider);
        }

        public static short ToInt16(DateTime value)
        {
            return ((IConvertible)value).ToInt16(null);
        }

        public static ushort ToUInt16(object value)
        {
            return value == null ? (ushort)0 : ((IConvertible)value).ToUInt16(null);
        }

        public static ushort ToUInt16(object value, IFormatProvider provider)
        {
            return value == null ? (ushort)0 : ((IConvertible)value).ToUInt16(provider);
        }

        public static ushort ToUInt16(bool value)
        {
            return value ? (ushort)Boolean.True : (ushort)Boolean.False;
        }

        public static ushort ToUInt16(char value)
        {
            return value;
        }

        public static ushort ToUInt16(sbyte value)
        {
            if (value < 0)
                ThrowUInt16OverflowException();
            Contract.EndContractBlock();
            return (ushort)value;
        }

        public static ushort ToUInt16(byte value)
        {
            return value;
        }

        public static ushort ToUInt16(short value)
        {
            if (value < 0)
                ThrowUInt16OverflowException();
            Contract.EndContractBlock();
            return (ushort)value;
        }

        public static ushort ToUInt16(int value)
        {
            if (value < 0 || value > UInt16.MaxValue)
                ThrowUInt16OverflowException();
            Contract.EndContractBlock();
            return (ushort)value;
        }

        public static ushort ToUInt16(ushort value)
        {
            return value;
        }

        public static ushort ToUInt16(uint value)
        {
            if (value > UInt16.MaxValue)
                ThrowUInt16OverflowException();
            Contract.EndContractBlock();
            return (ushort)value;
        }

        public static ushort ToUInt16(long value)
        {
            if (value < 0 || value > UInt16.MaxValue)
                ThrowUInt16OverflowException();
            Contract.EndContractBlock();
            return (ushort)value;
        }

        public static ushort ToUInt16(ulong value)
        {
            if (value > UInt16.MaxValue)
                ThrowUInt16OverflowException();
            Contract.EndContractBlock();
            return (ushort)value;
        }

        public static ushort ToUInt16(float value)
        {
            return ToUInt16((double)value);
        }

        public static ushort ToUInt16(double value)
        {
            return ToUInt16(ToInt32(value));
        }

        public static ushort ToUInt16(decimal value)
        {
            return Decimal.ToUInt16(Decimal.Round(value, 0));
        }

        public static ushort ToUInt16(String value)
        {
            if (value == null)
                return 0;
            return UInt16.Parse(value, CultureInfo.CurrentCulture);
        }

        public static ushort ToUInt16(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return UInt16.Parse(value, NumberStyles.Integer, provider);
        }

        public static ushort ToUInt16(DateTime value)
        {
            return ((IConvertible)value).ToUInt16(null);
        }

        public static int ToInt32(object value)
        {
            return value == null ? 0 : ((IConvertible)value).ToInt32(null);
        }

        public static int ToInt32(object value, IFormatProvider provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToInt32(provider);
        }

        public static int ToInt32(bool value)
        {
            return value ? Boolean.True : Boolean.False;
        }

        public static int ToInt32(char value)
        {
            return value;
        }

        public static int ToInt32(sbyte value)
        {
            return value;
        }

        public static int ToInt32(byte value)
        {
            return value;
        }

        public static int ToInt32(short value)
        {
            return value;
        }

        public static int ToInt32(ushort value)
        {
            return value;
        }

        public static int ToInt32(uint value)
        {
            if (value > Int32.MaxValue)
                ThrowInt32OverflowException();
            Contract.EndContractBlock();
            return (int)value;
        }

        public static int ToInt32(int value)
        {
            return value;
        }

        public static int ToInt32(long value)
        {
            if (value < Int32.MinValue || value > Int32.MaxValue)
                ThrowInt32OverflowException();
            Contract.EndContractBlock();
            return (int)value;
        }

        public static int ToInt32(ulong value)
        {
            if (value > Int32.MaxValue)
                ThrowInt32OverflowException();
            Contract.EndContractBlock();
            return (int)value;
        }

        public static int ToInt32(float value)
        {
            return ToInt32((double)value);
        }

        public static int ToInt32(double value)
        {
            if (value >= 0)
            {
                if (value < 2147483647.5)
                {
                    int result = (int)value;
                    double dif = value - result;
                    if (dif > 0.5 || dif == 0.5 && (result & 1) != 0)
                        result++;
                    return result;
                }
            }
            else
            {
                if (value >= -2147483648.5)
                {
                    int result = (int)value;
                    double dif = value - result;
                    if (dif < -0.5 || dif == -0.5 && (result & 1) != 0)
                        result--;
                    return result;
                }
            }

            throw new OverflowException(Environment.GetResourceString("Overflow_Int32"));
        }

        public static int ToInt32(decimal value)
        {
            return Decimal.FCallToInt32(value);
        }

        public static int ToInt32(String value)
        {
            if (value == null)
                return 0;
            return Int32.Parse(value, CultureInfo.CurrentCulture);
        }

        public static int ToInt32(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return Int32.Parse(value, NumberStyles.Integer, provider);
        }

        public static int ToInt32(DateTime value)
        {
            return ((IConvertible)value).ToInt32(null);
        }

        public static uint ToUInt32(object value)
        {
            return value == null ? 0 : ((IConvertible)value).ToUInt32(null);
        }

        public static uint ToUInt32(object value, IFormatProvider provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToUInt32(provider);
        }

        public static uint ToUInt32(bool value)
        {
            return value ? (uint)Boolean.True : (uint)Boolean.False;
        }

        public static uint ToUInt32(char value)
        {
            return value;
        }

        public static uint ToUInt32(sbyte value)
        {
            if (value < 0)
                ThrowUInt32OverflowException();
            Contract.EndContractBlock();
            return (uint)value;
        }

        public static uint ToUInt32(byte value)
        {
            return value;
        }

        public static uint ToUInt32(short value)
        {
            if (value < 0)
                ThrowUInt32OverflowException();
            Contract.EndContractBlock();
            return (uint)value;
        }

        public static uint ToUInt32(ushort value)
        {
            return value;
        }

        public static uint ToUInt32(int value)
        {
            if (value < 0)
                ThrowUInt32OverflowException();
            Contract.EndContractBlock();
            return (uint)value;
        }

        public static uint ToUInt32(uint value)
        {
            return value;
        }

        public static uint ToUInt32(long value)
        {
            if (value < 0 || value > UInt32.MaxValue)
                ThrowUInt32OverflowException();
            Contract.EndContractBlock();
            return (uint)value;
        }

        public static uint ToUInt32(ulong value)
        {
            if (value > UInt32.MaxValue)
                ThrowUInt32OverflowException();
            Contract.EndContractBlock();
            return (uint)value;
        }

        public static uint ToUInt32(float value)
        {
            return ToUInt32((double)value);
        }

        public static uint ToUInt32(double value)
        {
            if (value >= -0.5 && value < 4294967295.5)
            {
                uint result = (uint)value;
                double dif = value - result;
                if (dif > 0.5 || dif == 0.5 && (result & 1) != 0)
                    result++;
                return result;
            }

            throw new OverflowException(Environment.GetResourceString("Overflow_UInt32"));
        }

        public static uint ToUInt32(decimal value)
        {
            return Decimal.ToUInt32(Decimal.Round(value, 0));
        }

        public static uint ToUInt32(String value)
        {
            if (value == null)
                return 0;
            return UInt32.Parse(value, CultureInfo.CurrentCulture);
        }

        public static uint ToUInt32(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return UInt32.Parse(value, NumberStyles.Integer, provider);
        }

        public static uint ToUInt32(DateTime value)
        {
            return ((IConvertible)value).ToUInt32(null);
        }

        public static long ToInt64(object value)
        {
            return value == null ? 0 : ((IConvertible)value).ToInt64(null);
        }

        public static long ToInt64(object value, IFormatProvider provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToInt64(provider);
        }

        public static long ToInt64(bool value)
        {
            return value ? Boolean.True : Boolean.False;
        }

        public static long ToInt64(char value)
        {
            return value;
        }

        public static long ToInt64(sbyte value)
        {
            return value;
        }

        public static long ToInt64(byte value)
        {
            return value;
        }

        public static long ToInt64(short value)
        {
            return value;
        }

        public static long ToInt64(ushort value)
        {
            return value;
        }

        public static long ToInt64(int value)
        {
            return value;
        }

        public static long ToInt64(uint value)
        {
            return value;
        }

        public static long ToInt64(ulong value)
        {
            if (value > Int64.MaxValue)
                ThrowInt64OverflowException();
            Contract.EndContractBlock();
            return (long)value;
        }

        public static long ToInt64(long value)
        {
            return value;
        }

        public static long ToInt64(float value)
        {
            return ToInt64((double)value);
        }

        public static long ToInt64(double value)
        {
            return checked ((long)Math.Round(value));
        }

        public static long ToInt64(decimal value)
        {
            return Decimal.ToInt64(Decimal.Round(value, 0));
        }

        public static long ToInt64(string value)
        {
            if (value == null)
                return 0;
            return Int64.Parse(value, CultureInfo.CurrentCulture);
        }

        public static long ToInt64(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return Int64.Parse(value, NumberStyles.Integer, provider);
        }

        public static long ToInt64(DateTime value)
        {
            return ((IConvertible)value).ToInt64(null);
        }

        public static ulong ToUInt64(object value)
        {
            return value == null ? 0 : ((IConvertible)value).ToUInt64(null);
        }

        public static ulong ToUInt64(object value, IFormatProvider provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToUInt64(provider);
        }

        public static ulong ToUInt64(bool value)
        {
            return value ? (ulong)Boolean.True : (ulong)Boolean.False;
        }

        public static ulong ToUInt64(char value)
        {
            return value;
        }

        public static ulong ToUInt64(sbyte value)
        {
            if (value < 0)
                ThrowUInt64OverflowException();
            Contract.EndContractBlock();
            return (ulong)value;
        }

        public static ulong ToUInt64(byte value)
        {
            return value;
        }

        public static ulong ToUInt64(short value)
        {
            if (value < 0)
                ThrowUInt64OverflowException();
            Contract.EndContractBlock();
            return (ulong)value;
        }

        public static ulong ToUInt64(ushort value)
        {
            return value;
        }

        public static ulong ToUInt64(int value)
        {
            if (value < 0)
                ThrowUInt64OverflowException();
            Contract.EndContractBlock();
            return (ulong)value;
        }

        public static ulong ToUInt64(uint value)
        {
            return value;
        }

        public static ulong ToUInt64(long value)
        {
            if (value < 0)
                ThrowUInt64OverflowException();
            Contract.EndContractBlock();
            return (ulong)value;
        }

        public static ulong ToUInt64(UInt64 value)
        {
            return value;
        }

        public static ulong ToUInt64(float value)
        {
            return ToUInt64((double)value);
        }

        public static ulong ToUInt64(double value)
        {
            return checked ((ulong)Math.Round(value));
        }

        public static ulong ToUInt64(decimal value)
        {
            return Decimal.ToUInt64(Decimal.Round(value, 0));
        }

        public static ulong ToUInt64(String value)
        {
            if (value == null)
                return 0;
            return UInt64.Parse(value, CultureInfo.CurrentCulture);
        }

        public static ulong ToUInt64(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return UInt64.Parse(value, NumberStyles.Integer, provider);
        }

        public static ulong ToUInt64(DateTime value)
        {
            return ((IConvertible)value).ToUInt64(null);
        }

        public static float ToSingle(object value)
        {
            return value == null ? 0 : ((IConvertible)value).ToSingle(null);
        }

        public static float ToSingle(object value, IFormatProvider provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToSingle(provider);
        }

        public static float ToSingle(sbyte value)
        {
            return value;
        }

        public static float ToSingle(byte value)
        {
            return value;
        }

        public static float ToSingle(char value)
        {
            return ((IConvertible)value).ToSingle(null);
        }

        public static float ToSingle(short value)
        {
            return value;
        }

        public static float ToSingle(ushort value)
        {
            return value;
        }

        public static float ToSingle(int value)
        {
            return value;
        }

        public static float ToSingle(uint value)
        {
            return value;
        }

        public static float ToSingle(long value)
        {
            return value;
        }

        public static float ToSingle(ulong value)
        {
            return value;
        }

        public static float ToSingle(float value)
        {
            return value;
        }

        public static float ToSingle(double value)
        {
            return (float)value;
        }

        public static float ToSingle(decimal value)
        {
            return (float)value;
        }

        public static float ToSingle(String value)
        {
            if (value == null)
                return 0;
            return Single.Parse(value, CultureInfo.CurrentCulture);
        }

        public static float ToSingle(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return Single.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, provider);
        }

        public static float ToSingle(bool value)
        {
            return value ? Boolean.True : Boolean.False;
        }

        public static float ToSingle(DateTime value)
        {
            return ((IConvertible)value).ToSingle(null);
        }

        public static double ToDouble(object value)
        {
            return value == null ? 0 : ((IConvertible)value).ToDouble(null);
        }

        public static double ToDouble(object value, IFormatProvider provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToDouble(provider);
        }

        public static double ToDouble(sbyte value)
        {
            return value;
        }

        public static double ToDouble(byte value)
        {
            return value;
        }

        public static double ToDouble(short value)
        {
            return value;
        }

        public static double ToDouble(char value)
        {
            return ((IConvertible)value).ToDouble(null);
        }

        public static double ToDouble(ushort value)
        {
            return value;
        }

        public static double ToDouble(int value)
        {
            return value;
        }

        public static double ToDouble(uint value)
        {
            return value;
        }

        public static double ToDouble(long value)
        {
            return value;
        }

        public static double ToDouble(ulong value)
        {
            return value;
        }

        public static double ToDouble(float value)
        {
            return value;
        }

        public static double ToDouble(double value)
        {
            return value;
        }

        public static double ToDouble(decimal value)
        {
            return (double)value;
        }

        public static double ToDouble(String value)
        {
            if (value == null)
                return 0;
            return Double.Parse(value, CultureInfo.CurrentCulture);
        }

        public static double ToDouble(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0;
            return Double.Parse(value, NumberStyles.Float | NumberStyles.AllowThousands, provider);
        }

        public static double ToDouble(bool value)
        {
            return value ? Boolean.True : Boolean.False;
        }

        public static double ToDouble(DateTime value)
        {
            return ((IConvertible)value).ToDouble(null);
        }

        public static decimal ToDecimal(object value)
        {
            return value == null ? 0 : ((IConvertible)value).ToDecimal(null);
        }

        public static decimal ToDecimal(object value, IFormatProvider provider)
        {
            return value == null ? 0 : ((IConvertible)value).ToDecimal(provider);
        }

        public static decimal ToDecimal(sbyte value)
        {
            return value;
        }

        public static decimal ToDecimal(byte value)
        {
            return value;
        }

        public static decimal ToDecimal(char value)
        {
            return ((IConvertible)value).ToDecimal(null);
        }

        public static decimal ToDecimal(short value)
        {
            return value;
        }

        public static decimal ToDecimal(ushort value)
        {
            return value;
        }

        public static decimal ToDecimal(int value)
        {
            return value;
        }

        public static decimal ToDecimal(uint value)
        {
            return value;
        }

        public static decimal ToDecimal(long value)
        {
            return value;
        }

        public static decimal ToDecimal(ulong value)
        {
            return value;
        }

        public static decimal ToDecimal(float value)
        {
            return (decimal)value;
        }

        public static decimal ToDecimal(double value)
        {
            return (decimal)value;
        }

        public static decimal ToDecimal(String value)
        {
            if (value == null)
                return 0m;
            return Decimal.Parse(value, CultureInfo.CurrentCulture);
        }

        public static Decimal ToDecimal(String value, IFormatProvider provider)
        {
            if (value == null)
                return 0m;
            return Decimal.Parse(value, NumberStyles.Number, provider);
        }

        public static decimal ToDecimal(decimal value)
        {
            return value;
        }

        public static decimal ToDecimal(bool value)
        {
            return value ? Boolean.True : Boolean.False;
        }

        public static decimal ToDecimal(DateTime value)
        {
            return ((IConvertible)value).ToDecimal(null);
        }

        public static DateTime ToDateTime(DateTime value)
        {
            return value;
        }

        public static DateTime ToDateTime(object value)
        {
            return value == null ? DateTime.MinValue : ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(object value, IFormatProvider provider)
        {
            return value == null ? DateTime.MinValue : ((IConvertible)value).ToDateTime(provider);
        }

        public static DateTime ToDateTime(String value)
        {
            if (value == null)
                return new DateTime(0);
            return DateTime.Parse(value, CultureInfo.CurrentCulture);
        }

        public static DateTime ToDateTime(String value, IFormatProvider provider)
        {
            if (value == null)
                return new DateTime(0);
            return DateTime.Parse(value, provider);
        }

        public static DateTime ToDateTime(sbyte value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(byte value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(short value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(ushort value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(int value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(uint value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(long value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(ulong value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(bool value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(char value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(float value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(double value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static DateTime ToDateTime(decimal value)
        {
            return ((IConvertible)value).ToDateTime(null);
        }

        public static string ToString(Object value)
        {
            return ToString(value, null);
        }

        public static string ToString(Object value, IFormatProvider provider)
        {
            IConvertible ic = value as IConvertible;
            if (ic != null)
                return ic.ToString(provider);
            IFormattable formattable = value as IFormattable;
            if (formattable != null)
                return formattable.ToString(null, provider);
            return value == null ? String.Empty : value.ToString();
        }

        public static string ToString(bool value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString();
        }

        public static string ToString(bool value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(char value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return Char.ToString(value);
        }

        public static string ToString(char value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(sbyte value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(sbyte value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(byte value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(byte value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(short value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(short value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(ushort value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(ushort value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(int value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(int value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(uint value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(uint value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(long value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(long value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(ulong value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(ulong value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(float value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(float value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(double value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(double value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(decimal value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(CultureInfo.CurrentCulture);
        }

        public static string ToString(Decimal value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static string ToString(DateTime value)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString();
        }

        public static string ToString(DateTime value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() != null);
            return value.ToString(provider);
        }

        public static String ToString(String value)
        {
            Contract.Ensures(Contract.Result<string>() == value);
            return value;
        }

        public static String ToString(String value, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<string>() == value);
            return value;
        }

        public static byte ToByte(String value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            int r = ParseNumbers.StringToInt(value, fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsUnsigned);
            if (r < Byte.MinValue || r > Byte.MaxValue)
                ThrowByteOverflowException();
            return (byte)r;
        }

        public static sbyte ToSByte(String value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            int r = ParseNumbers.StringToInt(value, fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsI1);
            if (fromBase != 10 && r <= Byte.MaxValue)
                return (sbyte)r;
            if (r < SByte.MinValue || r > SByte.MaxValue)
                ThrowSByteOverflowException();
            return (sbyte)r;
        }

        public static short ToInt16(String value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            int r = ParseNumbers.StringToInt(value, fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsI2);
            if (fromBase != 10 && r <= UInt16.MaxValue)
                return (short)r;
            if (r < Int16.MinValue || r > Int16.MaxValue)
                ThrowInt16OverflowException();
            return (short)r;
        }

        public static ushort ToUInt16(String value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            int r = ParseNumbers.StringToInt(value, fromBase, ParseNumbers.IsTight | ParseNumbers.TreatAsUnsigned);
            if (r < UInt16.MinValue || r > UInt16.MaxValue)
                ThrowUInt16OverflowException();
            return (ushort)r;
        }

        public static int ToInt32(String value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            return ParseNumbers.StringToInt(value, fromBase, ParseNumbers.IsTight);
        }

        public static uint ToUInt32(String value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            return (uint)ParseNumbers.StringToInt(value, fromBase, ParseNumbers.TreatAsUnsigned | ParseNumbers.IsTight);
        }

        public static long ToInt64(String value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            return ParseNumbers.StringToLong(value, fromBase, ParseNumbers.IsTight);
        }

        public static ulong ToUInt64(String value, int fromBase)
        {
            if (fromBase != 2 && fromBase != 8 && fromBase != 10 && fromBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            return (ulong)ParseNumbers.StringToLong(value, fromBase, ParseNumbers.TreatAsUnsigned | ParseNumbers.IsTight);
        }

        public static String ToString(byte value, int toBase)
        {
            if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            return ParseNumbers.IntToString((int)value, toBase, -1, ' ', ParseNumbers.PrintAsI1);
        }

        public static String ToString(short value, int toBase)
        {
            if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            return ParseNumbers.IntToString((int)value, toBase, -1, ' ', ParseNumbers.PrintAsI2);
        }

        public static String ToString(int value, int toBase)
        {
            if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            return ParseNumbers.IntToString(value, toBase, -1, ' ', 0);
        }

        public static String ToString(long value, int toBase)
        {
            if (toBase != 2 && toBase != 8 && toBase != 10 && toBase != 16)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidBase"));
            }

            Contract.EndContractBlock();
            return ParseNumbers.LongToString(value, toBase, -1, ' ', 0);
        }

        public static String ToBase64String(byte[] inArray)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException("inArray");
            }

            Contract.Ensures(Contract.Result<string>() != null);
            Contract.EndContractBlock();
            return ToBase64String(inArray, 0, inArray.Length, Base64FormattingOptions.None);
        }

        public static String ToBase64String(byte[] inArray, Base64FormattingOptions options)
        {
            if (inArray == null)
            {
                throw new ArgumentNullException("inArray");
            }

            Contract.Ensures(Contract.Result<string>() != null);
            Contract.EndContractBlock();
            return ToBase64String(inArray, 0, inArray.Length, options);
        }

        public static String ToBase64String(byte[] inArray, int offset, int length)
        {
            return ToBase64String(inArray, offset, length, Base64FormattingOptions.None);
        }

        public static unsafe String ToBase64String(byte[] inArray, int offset, int length, Base64FormattingOptions options)
        {
            if (inArray == null)
                throw new ArgumentNullException("inArray");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            if (options < Base64FormattingOptions.None || options > Base64FormattingOptions.InsertLineBreaks)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options));
            Contract.Ensures(Contract.Result<string>() != null);
            Contract.EndContractBlock();
            int inArrayLength;
            int stringLength;
            inArrayLength = inArray.Length;
            if (offset > (inArrayLength - length))
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
            if (inArrayLength == 0)
                return String.Empty;
            bool insertLineBreaks = (options == Base64FormattingOptions.InsertLineBreaks);
            stringLength = ToBase64_CalculateAndValidateOutputLength(length, insertLineBreaks);
            string returnString = string.FastAllocateString(stringLength);
            fixed (char *outChars = returnString)
            {
                fixed (byte *inData = inArray)
                {
                    int j = ConvertToBase64Array(outChars, inData, offset, length, insertLineBreaks);
                    BCLDebug.Assert(returnString.Length == j, "returnString.Length == j");
                    return returnString;
                }
            }
        }

        public static int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut)
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= outArray.Length);
            Contract.EndContractBlock();
            return ToBase64CharArray(inArray, offsetIn, length, outArray, offsetOut, Base64FormattingOptions.None);
        }

        public static unsafe int ToBase64CharArray(byte[] inArray, int offsetIn, int length, char[] outArray, int offsetOut, Base64FormattingOptions options)
        {
            if (inArray == null)
                throw new ArgumentNullException("inArray");
            if (outArray == null)
                throw new ArgumentNullException("outArray");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (offsetIn < 0)
                throw new ArgumentOutOfRangeException("offsetIn", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            if (offsetOut < 0)
                throw new ArgumentOutOfRangeException("offsetOut", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            if (options < Base64FormattingOptions.None || options > Base64FormattingOptions.InsertLineBreaks)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options));
            }

            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= outArray.Length);
            Contract.EndContractBlock();
            int retVal;
            int inArrayLength;
            int outArrayLength;
            int numElementsToCopy;
            inArrayLength = inArray.Length;
            if (offsetIn > (int)(inArrayLength - length))
                throw new ArgumentOutOfRangeException("offsetIn", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
            if (inArrayLength == 0)
                return 0;
            bool insertLineBreaks = (options == Base64FormattingOptions.InsertLineBreaks);
            outArrayLength = outArray.Length;
            numElementsToCopy = ToBase64_CalculateAndValidateOutputLength(length, insertLineBreaks);
            if (offsetOut > (int)(outArrayLength - numElementsToCopy))
                throw new ArgumentOutOfRangeException("offsetOut", Environment.GetResourceString("ArgumentOutOfRange_OffsetOut"));
            fixed (char *outChars = &outArray[offsetOut])
            {
                fixed (byte *inData = inArray)
                {
                    retVal = ConvertToBase64Array(outChars, inData, offsetIn, length, insertLineBreaks);
                }
            }

            return retVal;
        }

        private static unsafe int ConvertToBase64Array(char *outChars, byte *inData, int offset, int length, bool insertLineBreaks)
        {
            int lengthmod3 = length % 3;
            int calcLength = offset + (length - lengthmod3);
            int j = 0;
            int charcount = 0;
            int i;
            fixed (char *base64 = base64Table)
            {
                for (i = offset; i < calcLength; i += 3)
                {
                    if (insertLineBreaks)
                    {
                        if (charcount == base64LineBreakPosition)
                        {
                            outChars[j++] = '\r';
                            outChars[j++] = '\n';
                            charcount = 0;
                        }

                        charcount += 4;
                    }

                    outChars[j] = base64[(inData[i] & 0xfc) >> 2];
                    outChars[j + 1] = base64[((inData[i] & 0x03) << 4) | ((inData[i + 1] & 0xf0) >> 4)];
                    outChars[j + 2] = base64[((inData[i + 1] & 0x0f) << 2) | ((inData[i + 2] & 0xc0) >> 6)];
                    outChars[j + 3] = base64[(inData[i + 2] & 0x3f)];
                    j += 4;
                }

                i = calcLength;
                if (insertLineBreaks && (lengthmod3 != 0) && (charcount == base64LineBreakPosition))
                {
                    outChars[j++] = '\r';
                    outChars[j++] = '\n';
                }

                switch (lengthmod3)
                {
                    case 2:
                        outChars[j] = base64[(inData[i] & 0xfc) >> 2];
                        outChars[j + 1] = base64[((inData[i] & 0x03) << 4) | ((inData[i + 1] & 0xf0) >> 4)];
                        outChars[j + 2] = base64[(inData[i + 1] & 0x0f) << 2];
                        outChars[j + 3] = base64[64];
                        j += 4;
                        break;
                    case 1:
                        outChars[j] = base64[(inData[i] & 0xfc) >> 2];
                        outChars[j + 1] = base64[(inData[i] & 0x03) << 4];
                        outChars[j + 2] = base64[64];
                        outChars[j + 3] = base64[64];
                        j += 4;
                        break;
                }
            }

            return j;
        }

        private static int ToBase64_CalculateAndValidateOutputLength(int inputLength, bool insertLineBreaks)
        {
            long outlen = ((long)inputLength) / 3 * 4;
            outlen += ((inputLength % 3) != 0) ? 4 : 0;
            if (outlen == 0)
                return 0;
            if (insertLineBreaks)
            {
                long newLines = outlen / base64LineBreakPosition;
                if ((outlen % base64LineBreakPosition) == 0)
                {
                    --newLines;
                }

                outlen += newLines * 2;
            }

            if (outlen > int.MaxValue)
                throw new OutOfMemoryException();
            return (int)outlen;
        }

        public static Byte[] FromBase64String(String s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            Contract.EndContractBlock();
            unsafe
            {
                fixed (Char*sPtr = s)
                {
                    return FromBase64CharPtr(sPtr, s.Length);
                }
            }
        }

        public static Byte[] FromBase64CharArray(Char[] inArray, Int32 offset, Int32 length)
        {
            if (inArray == null)
                throw new ArgumentNullException("inArray");
            Contract.EndContractBlock();
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                if (inArray.Length == 0)
                {
                    throw new FormatException();
                }
            }

            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            if (offset > inArray.Length - length)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"));
            unsafe
            {
                fixed (Char*inArrayPtr = inArray)
                {
                    return FromBase64CharPtr(inArrayPtr + offset, length);
                }
            }
        }

        private static unsafe Byte[] FromBase64CharPtr(Char*inputPtr, Int32 inputLength)
        {
            Contract.Assert(0 <= inputLength);
            while (inputLength > 0)
            {
                Int32 lastChar = inputPtr[inputLength - 1];
                if (lastChar != (Int32)' ' && lastChar != (Int32)'\n' && lastChar != (Int32)'\r' && lastChar != (Int32)'\t')
                    break;
                inputLength--;
            }

            Int32 resultLength = FromBase64_ComputeResultLength(inputPtr, inputLength);
            Contract.Assert(0 <= resultLength);
            Byte[] decodedBytes = new Byte[resultLength];
            Int32 actualResultLength;
            fixed (Byte*decodedBytesPtr = decodedBytes)
                actualResultLength = FromBase64_Decode(inputPtr, inputLength, decodedBytesPtr, resultLength);
            return decodedBytes;
        }

        private static unsafe Int32 FromBase64_Decode(Char*startInputPtr, Int32 inputLength, Byte*startDestPtr, Int32 destLength)
        {
            const UInt32 intA = (UInt32)'A';
            const UInt32 inta = (UInt32)'a';
            const UInt32 int0 = (UInt32)'0';
            const UInt32 intEq = (UInt32)'=';
            const UInt32 intPlus = (UInt32)'+';
            const UInt32 intSlash = (UInt32)'/';
            const UInt32 intSpace = (UInt32)' ';
            const UInt32 intTab = (UInt32)'\t';
            const UInt32 intNLn = (UInt32)'\n';
            const UInt32 intCRt = (UInt32)'\r';
            const UInt32 intAtoZ = (UInt32)('Z' - 'A');
            const UInt32 int0to9 = (UInt32)('9' - '0');
            Char*inputPtr = startInputPtr;
            Byte*destPtr = startDestPtr;
            Char*endInputPtr = inputPtr + inputLength;
            Byte*endDestPtr = destPtr + destLength;
            UInt32 currCode;
            UInt32 currBlockCodes = 0x000000FFu;
            unchecked
            {
                while (true)
                {
                    if (inputPtr >= endInputPtr)
                        goto _AllInputConsumed;
                    currCode = (UInt32)(*inputPtr);
                    inputPtr++;
                    if (currCode - intA <= intAtoZ)
                        currCode -= intA;
                    else if (currCode - inta <= intAtoZ)
                        currCode -= (inta - 26u);
                    else if (currCode - int0 <= int0to9)
                        currCode -= (int0 - 52u);
                    else
                    {
                        switch (currCode)
                        {
                            case intPlus:
                                currCode = 62u;
                                break;
                            case intSlash:
                                currCode = 63u;
                                break;
                            case intCRt:
                            case intNLn:
                            case intSpace:
                            case intTab:
                                continue;
                            case intEq:
                                goto _EqualityCharEncountered;
                            default:
                                throw new FormatException(Environment.GetResourceString("Format_BadBase64Char"));
                        }
                    }

                    currBlockCodes = (currBlockCodes << 6) | currCode;
                    if ((currBlockCodes & 0x80000000u) != 0u)
                    {
                        if ((Int32)(endDestPtr - destPtr) < 3)
                            return -1;
                        *(destPtr) = (Byte)(currBlockCodes >> 16);
                        *(destPtr + 1) = (Byte)(currBlockCodes >> 8);
                        *(destPtr + 2) = (Byte)(currBlockCodes);
                        destPtr += 3;
                        currBlockCodes = 0x000000FFu;
                    }
                }
            }

            _EqualityCharEncountered:
                Contract.Assert(currCode == intEq);
            if (inputPtr == endInputPtr)
            {
                currBlockCodes <<= 6;
                if ((currBlockCodes & 0x80000000u) == 0u)
                    throw new FormatException(Environment.GetResourceString("Format_BadBase64CharArrayLength"));
                if ((int)(endDestPtr - destPtr) < 2)
                    return -1;
                *(destPtr++) = (Byte)(currBlockCodes >> 16);
                *(destPtr++) = (Byte)(currBlockCodes >> 8);
                currBlockCodes = 0x000000FFu;
            }
            else
            {
                while (inputPtr < (endInputPtr - 1))
                {
                    Int32 lastChar = *(inputPtr);
                    if (lastChar != (Int32)' ' && lastChar != (Int32)'\n' && lastChar != (Int32)'\r' && lastChar != (Int32)'\t')
                        break;
                    inputPtr++;
                }

                if (inputPtr == (endInputPtr - 1) && *(inputPtr) == '=')
                {
                    currBlockCodes <<= 12;
                    if ((currBlockCodes & 0x80000000u) == 0u)
                        throw new FormatException(Environment.GetResourceString("Format_BadBase64CharArrayLength"));
                    if ((Int32)(endDestPtr - destPtr) < 1)
                        return -1;
                    *(destPtr++) = (Byte)(currBlockCodes >> 16);
                    currBlockCodes = 0x000000FFu;
                }
                else
                    throw new FormatException(Environment.GetResourceString("Format_BadBase64Char"));
            }

            _AllInputConsumed:
                if (currBlockCodes != 0x000000FFu)
                    throw new FormatException(Environment.GetResourceString("Format_BadBase64CharArrayLength"));
            return (Int32)(destPtr - startDestPtr);
        }

        private static unsafe Int32 FromBase64_ComputeResultLength(Char*inputPtr, Int32 inputLength)
        {
            const UInt32 intEq = (UInt32)'=';
            const UInt32 intSpace = (UInt32)' ';
            Contract.Assert(0 <= inputLength);
            Char*inputEndPtr = inputPtr + inputLength;
            Int32 usefulInputLength = inputLength;
            Int32 padding = 0;
            while (inputPtr < inputEndPtr)
            {
                UInt32 c = (UInt32)(*inputPtr);
                inputPtr++;
                if (c <= intSpace)
                    usefulInputLength--;
                else if (c == intEq)
                {
                    usefulInputLength--;
                    padding++;
                }
            }

            Contract.Assert(0 <= usefulInputLength);
            Contract.Assert(0 <= padding);
            if (padding != 0)
            {
                if (padding == 1)
                    padding = 2;
                else if (padding == 2)
                    padding = 1;
                else
                    throw new FormatException(Environment.GetResourceString("Format_BadBase64Char"));
            }

            return (usefulInputLength / 4) * 3 + padding;
        }
    }
}