using System.Reflection;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System
{
    public abstract class Enum : ValueType, IComparable, IFormattable, IConvertible
    {
        private const char enumSeparatorChar = ',';
        private const String enumSeparatorString = ", ";
        private static ValuesAndNames GetCachedValuesAndNames(RuntimeType enumType, bool getNames)
        {
            ValuesAndNames entry = enumType.GenericCache as ValuesAndNames;
            if (entry == null || (getNames && entry.Names == null))
            {
                ulong[] values = null;
                String[] names = null;
                GetEnumValuesAndNames(enumType.GetTypeHandleInternal(), JitHelpers.GetObjectHandleOnStack(ref values), JitHelpers.GetObjectHandleOnStack(ref names), getNames);
                entry = new ValuesAndNames(values, names);
                enumType.GenericCache = entry;
            }

            return entry;
        }

        private static String InternalFormattedHexString(Object value)
        {
            TypeCode typeCode = Convert.GetTypeCode(value);
            switch (typeCode)
            {
                case TypeCode.SByte:
                {
                    Byte result = (byte)(sbyte)value;
                    return result.ToString("X2", null);
                }

                case TypeCode.Byte:
                {
                    Byte result = (byte)value;
                    return result.ToString("X2", null);
                }

                case TypeCode.Boolean:
                {
                    Byte result = Convert.ToByte((bool)value);
                    return result.ToString("X2", null);
                }

                case TypeCode.Int16:
                {
                    UInt16 result = (UInt16)(Int16)value;
                    return result.ToString("X4", null);
                }

                case TypeCode.UInt16:
                {
                    UInt16 result = (UInt16)value;
                    return result.ToString("X4", null);
                }

                case TypeCode.Char:
                {
                    UInt16 result = (UInt16)(Char)value;
                    return result.ToString("X4", null);
                }

                case TypeCode.UInt32:
                {
                    UInt32 result = (UInt32)value;
                    return result.ToString("X8", null);
                }

                case TypeCode.Int32:
                {
                    UInt32 result = (UInt32)(int)value;
                    return result.ToString("X8", null);
                }

                case TypeCode.UInt64:
                {
                    UInt64 result = (UInt64)value;
                    return result.ToString("X16", null);
                }

                case TypeCode.Int64:
                {
                    UInt64 result = (UInt64)(Int64)value;
                    return result.ToString("X16", null);
                }

                default:
                    Contract.Assert(false, "Invalid Object type in Format");
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
            }
        }

        private static String InternalFormat(RuntimeType eT, Object value)
        {
            Contract.Requires(eT != null);
            Contract.Requires(value != null);
            if (!eT.IsDefined(typeof (System.FlagsAttribute), false))
            {
                String retval = GetName(eT, value);
                if (retval == null)
                    return value.ToString();
                else
                    return retval;
            }
            else
            {
                return InternalFlagsFormat(eT, value);
            }
        }

        private static String InternalFlagsFormat(RuntimeType eT, Object value)
        {
            Contract.Requires(eT != null);
            Contract.Requires(value != null);
            ulong result = ToUInt64(value);
            ValuesAndNames entry = GetCachedValuesAndNames(eT, true);
            String[] names = entry.Names;
            ulong[] values = entry.Values;
            Contract.Assert(names.Length == values.Length);
            int index = values.Length - 1;
            StringBuilder retval = new StringBuilder();
            bool firstTime = true;
            ulong saveResult = result;
            while (index >= 0)
            {
                if ((index == 0) && (values[index] == 0))
                    break;
                if ((result & values[index]) == values[index])
                {
                    result -= values[index];
                    if (!firstTime)
                        retval.Insert(0, enumSeparatorString);
                    retval.Insert(0, names[index]);
                    firstTime = false;
                }

                index--;
            }

            if (result != 0)
                return value.ToString();
            if (saveResult == 0)
            {
                if (values.Length > 0 && values[0] == 0)
                    return names[0];
                else
                    return "0";
            }
            else
                return retval.ToString();
        }

        internal static ulong ToUInt64(Object value)
        {
            TypeCode typeCode = Convert.GetTypeCode(value);
            ulong result;
            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    result = (UInt64)Convert.ToInt64(value, CultureInfo.InvariantCulture);
                    break;
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Boolean:
                case TypeCode.Char:
                    result = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                    break;
                default:
                    Contract.Assert(false, "Invalid Object type in ToUInt64");
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
            }

            return result;
        }

        private static extern int InternalCompareTo(Object o1, Object o2);
        internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);
        private static extern void GetEnumValuesAndNames(RuntimeTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, bool getNames);
        private static extern Object InternalBoxEnum(RuntimeType enumType, long value);
        private enum ParseFailureKind
        {
            None = 0,
            Argument = 1,
            ArgumentNull = 2,
            ArgumentWithParameter = 3,
            UnhandledException = 4
        }

        private struct EnumResult
        {
            internal object parsedEnum;
            internal bool canThrow;
            internal ParseFailureKind m_failure;
            internal string m_failureMessageID;
            internal string m_failureParameter;
            internal object m_failureMessageFormatArgument;
            internal Exception m_innerException;
            internal void SetFailure(Exception unhandledException)
            {
                m_failure = ParseFailureKind.UnhandledException;
                m_innerException = unhandledException;
            }

            internal void SetFailure(ParseFailureKind failure, string failureParameter)
            {
                m_failure = failure;
                m_failureParameter = failureParameter;
                if (canThrow)
                    throw GetEnumParseException();
            }

            internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument)
            {
                m_failure = failure;
                m_failureMessageID = failureMessageID;
                m_failureMessageFormatArgument = failureMessageFormatArgument;
                if (canThrow)
                    throw GetEnumParseException();
            }

            internal Exception GetEnumParseException()
            {
                switch (m_failure)
                {
                    case ParseFailureKind.Argument:
                        return new ArgumentException(Environment.GetResourceString(m_failureMessageID));
                    case ParseFailureKind.ArgumentNull:
                        return new ArgumentNullException(m_failureParameter);
                    case ParseFailureKind.ArgumentWithParameter:
                        return new ArgumentException(Environment.GetResourceString(m_failureMessageID, m_failureMessageFormatArgument));
                    case ParseFailureKind.UnhandledException:
                        return m_innerException;
                    default:
                        Contract.Assert(false, "Unknown EnumParseFailure: " + m_failure);
                        return new ArgumentException(Environment.GetResourceString("Arg_EnumValueNotFound"));
                }
            }
        }

        public static bool TryParse<TEnum>(String value, out TEnum result)where TEnum : struct
        {
            return TryParse(value, false, out result);
        }

        public static bool TryParse<TEnum>(String value, bool ignoreCase, out TEnum result)where TEnum : struct
        {
            result = default (TEnum);
            EnumResult parseResult = new EnumResult();
            bool retValue;
            if (retValue = TryParseEnum(typeof (TEnum), value, ignoreCase, ref parseResult))
                result = (TEnum)parseResult.parsedEnum;
            return retValue;
        }

        public static Object Parse(Type enumType, String value)
        {
            return Parse(enumType, value, false);
        }

        public static Object Parse(Type enumType, String value, bool ignoreCase)
        {
            EnumResult parseResult = new EnumResult()
            {canThrow = true};
            if (TryParseEnum(enumType, value, ignoreCase, ref parseResult))
                return parseResult.parsedEnum;
            else
                throw parseResult.GetEnumParseException();
        }

        private static bool TryParseEnum(Type enumType, String value, bool ignoreCase, ref EnumResult parseResult)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            if (value == null)
            {
                parseResult.SetFailure(ParseFailureKind.ArgumentNull, "value");
                return false;
            }

            int firstNonWhitespaceIndex = -1;
            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i]))
                {
                    firstNonWhitespaceIndex = i;
                    break;
                }
            }

            if (firstNonWhitespaceIndex == -1)
            {
                parseResult.SetFailure(ParseFailureKind.Argument, "Arg_MustContainEnumInfo", null);
                return false;
            }

            ulong result = 0;
            char firstNonWhitespaceChar = value[firstNonWhitespaceIndex];
            if (Char.IsDigit(firstNonWhitespaceChar) || firstNonWhitespaceChar == '-' || firstNonWhitespaceChar == '+')
            {
                Type underlyingType = GetUnderlyingType(enumType);
                Object temp;
                try
                {
                    value = value.Trim();
                    temp = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
                    parseResult.parsedEnum = ToObject(enumType, temp);
                    return true;
                }
                catch (FormatException)
                {
                }
                catch (Exception ex)
                {
                    if (parseResult.canThrow)
                        throw;
                    else
                    {
                        parseResult.SetFailure(ex);
                        return false;
                    }
                }
            }

            ValuesAndNames entry = GetCachedValuesAndNames(rtType, true);
            String[] enumNames = entry.Names;
            ulong[] enumValues = entry.Values;
            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            int valueIndex = firstNonWhitespaceIndex;
            while (valueIndex <= value.Length)
            {
                int endIndex = value.IndexOf(enumSeparatorChar, valueIndex);
                if (endIndex == -1)
                {
                    endIndex = value.Length;
                }

                int endIndexNoWhitespace = endIndex;
                while (valueIndex < endIndex && Char.IsWhiteSpace(value[valueIndex]))
                    valueIndex++;
                while (endIndexNoWhitespace > valueIndex && Char.IsWhiteSpace(value[endIndexNoWhitespace - 1]))
                    endIndexNoWhitespace--;
                int valueSubstringLength = endIndexNoWhitespace - valueIndex;
                bool success = false;
                for (int i = 0; i < enumNames.Length; i++)
                {
                    if (enumNames[i].Length == valueSubstringLength && string.Compare(enumNames[i], 0, value, valueIndex, valueSubstringLength, comparison) == 0)
                    {
                        result |= enumValues[i];
                        success = true;
                        break;
                    }
                }

                if (!success)
                {
                    parseResult.SetFailure(ParseFailureKind.ArgumentWithParameter, "Arg_EnumValueNotFound", value);
                    return false;
                }

                valueIndex = endIndex + 1;
            }

            try
            {
                parseResult.parsedEnum = ToObject(enumType, result);
                return true;
            }
            catch (Exception ex)
            {
                if (parseResult.canThrow)
                    throw;
                else
                {
                    parseResult.SetFailure(ex);
                    return false;
                }
            }
        }

        public static Type GetUnderlyingType(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            Contract.Ensures(Contract.Result<Type>() != null);
            Contract.EndContractBlock();
            return enumType.GetEnumUnderlyingType();
        }

        public static Array GetValues(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.EndContractBlock();
            return enumType.GetEnumValues();
        }

        internal static ulong[] InternalGetValues(RuntimeType enumType)
        {
            return GetCachedValuesAndNames(enumType, false).Values;
        }

        public static String GetName(Type enumType, Object value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            Contract.EndContractBlock();
            return enumType.GetEnumName(value);
        }

        public static String[] GetNames(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();
            return enumType.GetEnumNames();
        }

        internal static String[] InternalGetNames(RuntimeType enumType)
        {
            return GetCachedValuesAndNames(enumType, true).Names;
        }

        public static Object ToObject(Type enumType, Object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            TypeCode typeCode = Convert.GetTypeCode(value);
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && ((typeCode == TypeCode.Boolean) || (typeCode == TypeCode.Char)))
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");
            }

            switch (typeCode)
            {
                case TypeCode.Int32:
                    return ToObject(enumType, (int)value);
                case TypeCode.SByte:
                    return ToObject(enumType, (sbyte)value);
                case TypeCode.Int16:
                    return ToObject(enumType, (short)value);
                case TypeCode.Int64:
                    return ToObject(enumType, (long)value);
                case TypeCode.UInt32:
                    return ToObject(enumType, (uint)value);
                case TypeCode.Byte:
                    return ToObject(enumType, (byte)value);
                case TypeCode.UInt16:
                    return ToObject(enumType, (ushort)value);
                case TypeCode.UInt64:
                    return ToObject(enumType, (ulong)value);
                case TypeCode.Char:
                    return ToObject(enumType, (char)value);
                case TypeCode.Boolean:
                    return ToObject(enumType, (bool)value);
                default:
                    throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");
            }
        }

        public static bool IsDefined(Type enumType, Object value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            Contract.EndContractBlock();
            return enumType.IsEnumDefined(value);
        }

        public static String Format(Type enumType, Object value, String format)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            if (value == null)
                throw new ArgumentNullException("value");
            if (format == null)
                throw new ArgumentNullException("format");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            Type valueType = value.GetType();
            Type underlyingType = GetUnderlyingType(enumType);
            if (valueType.IsEnum)
            {
                Type valueUnderlyingType = GetUnderlyingType(valueType);
                if (!valueType.IsEquivalentTo(enumType))
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", valueType.ToString(), enumType.ToString()));
                valueType = valueUnderlyingType;
                value = ((Enum)value).GetValue();
            }
            else if (valueType != underlyingType)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumFormatUnderlyingTypeAndObjectMustBeSameType", valueType.ToString(), underlyingType.ToString()));
            }

            if (format.Length != 1)
            {
                throw new FormatException(Environment.GetResourceString("Format_InvalidEnumFormatSpecification"));
            }

            char formatCh = format[0];
            if (formatCh == 'D' || formatCh == 'd')
            {
                return value.ToString();
            }

            if (formatCh == 'X' || formatCh == 'x')
            {
                return InternalFormattedHexString(value);
            }

            if (formatCh == 'G' || formatCh == 'g')
            {
                return InternalFormat(rtType, value);
            }

            if (formatCh == 'F' || formatCh == 'f')
            {
                return InternalFlagsFormat(rtType, value);
            }

            throw new FormatException(Environment.GetResourceString("Format_InvalidEnumFormatSpecification"));
        }

        private class ValuesAndNames
        {
            public ValuesAndNames(ulong[] values, String[] names)
            {
                this.Values = values;
                this.Names = names;
            }

            public ulong[] Values;
            public String[] Names;
        }

        internal unsafe Object GetValue()
        {
            fixed (void *pValue = &JitHelpers.GetPinningHelper(this).m_data)
            {
                switch (InternalGetCorElementType())
                {
                    case CorElementType.I1:
                        return *(sbyte *)pValue;
                    case CorElementType.U1:
                        return *(byte *)pValue;
                    case CorElementType.Boolean:
                        return *(bool *)pValue;
                    case CorElementType.I2:
                        return *(short *)pValue;
                    case CorElementType.U2:
                        return *(ushort *)pValue;
                    case CorElementType.Char:
                        return *(char *)pValue;
                    case CorElementType.I4:
                        return *(int *)pValue;
                    case CorElementType.U4:
                        return *(uint *)pValue;
                    case CorElementType.R4:
                        return *(float *)pValue;
                    case CorElementType.I8:
                        return *(long *)pValue;
                    case CorElementType.U8:
                        return *(ulong *)pValue;
                    case CorElementType.R8:
                        return *(double *)pValue;
                    case CorElementType.I:
                        return *(IntPtr*)pValue;
                    case CorElementType.U:
                        return *(UIntPtr*)pValue;
                    default:
                        Contract.Assert(false, "Invalid primitive type");
                        return null;
                }
            }
        }

        private extern bool InternalHasFlag(Enum flags);
        private extern CorElementType InternalGetCorElementType();
        public extern override bool Equals(Object obj);
        public override unsafe int GetHashCode()
        {
            fixed (void *pValue = &JitHelpers.GetPinningHelper(this).m_data)
            {
                switch (InternalGetCorElementType())
                {
                    case CorElementType.I1:
                        return (*(sbyte *)pValue).GetHashCode();
                    case CorElementType.U1:
                        return (*(byte *)pValue).GetHashCode();
                    case CorElementType.Boolean:
                        return (*(bool *)pValue).GetHashCode();
                    case CorElementType.I2:
                        return (*(short *)pValue).GetHashCode();
                    case CorElementType.U2:
                        return (*(ushort *)pValue).GetHashCode();
                    case CorElementType.Char:
                        return (*(char *)pValue).GetHashCode();
                    case CorElementType.I4:
                        return (*(int *)pValue).GetHashCode();
                    case CorElementType.U4:
                        return (*(uint *)pValue).GetHashCode();
                    case CorElementType.R4:
                        return (*(float *)pValue).GetHashCode();
                    case CorElementType.I8:
                        return (*(long *)pValue).GetHashCode();
                    case CorElementType.U8:
                        return (*(ulong *)pValue).GetHashCode();
                    case CorElementType.R8:
                        return (*(double *)pValue).GetHashCode();
                    case CorElementType.I:
                        return (*(IntPtr*)pValue).GetHashCode();
                    case CorElementType.U:
                        return (*(UIntPtr*)pValue).GetHashCode();
                    default:
                        Contract.Assert(false, "Invalid primitive type");
                        return 0;
                }
            }
        }

        public override String ToString()
        {
            return Enum.InternalFormat((RuntimeType)GetType(), GetValue());
        }

        public String ToString(String format, IFormatProvider provider)
        {
            return ToString(format);
        }

        public int CompareTo(Object target)
        {
            const int retIncompatibleMethodTables = 2;
            const int retInvalidEnumType = 3;
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();
            int ret = InternalCompareTo(this, target);
            if (ret < retIncompatibleMethodTables)
            {
                return ret;
            }
            else if (ret == retIncompatibleMethodTables)
            {
                Type thisType = this.GetType();
                Type targetType = target.GetType();
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", targetType.ToString(), thisType.ToString()));
            }
            else
            {
                Contract.Assert(ret == retInvalidEnumType, "Enum.InternalCompareTo return code was invalid");
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
            }
        }

        public String ToString(String format)
        {
            if (format == null || format.Length == 0)
                format = "G";
            if (String.Compare(format, "G", StringComparison.OrdinalIgnoreCase) == 0)
                return ToString();
            if (String.Compare(format, "D", StringComparison.OrdinalIgnoreCase) == 0)
                return GetValue().ToString();
            if (String.Compare(format, "X", StringComparison.OrdinalIgnoreCase) == 0)
                return InternalFormattedHexString(GetValue());
            if (String.Compare(format, "F", StringComparison.OrdinalIgnoreCase) == 0)
                return InternalFlagsFormat((RuntimeType)GetType(), GetValue());
            throw new FormatException(Environment.GetResourceString("Format_InvalidEnumFormatSpecification"));
        }

        public String ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public Boolean HasFlag(Enum flag)
        {
            if (flag == null)
                throw new ArgumentNullException("flag");
            Contract.EndContractBlock();
            if (!this.GetType().IsEquivalentTo(flag.GetType()))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EnumTypeDoesNotMatch", flag.GetType(), this.GetType()));
            }

            return InternalHasFlag(flag);
        }

        public TypeCode GetTypeCode()
        {
            Type enumType = this.GetType();
            Type underlyingType = GetUnderlyingType(enumType);
            if (underlyingType == typeof (Int32))
            {
                return TypeCode.Int32;
            }

            if (underlyingType == typeof (sbyte))
            {
                return TypeCode.SByte;
            }

            if (underlyingType == typeof (Int16))
            {
                return TypeCode.Int16;
            }

            if (underlyingType == typeof (Int64))
            {
                return TypeCode.Int64;
            }

            if (underlyingType == typeof (UInt32))
            {
                return TypeCode.UInt32;
            }

            if (underlyingType == typeof (byte))
            {
                return TypeCode.Byte;
            }

            if (underlyingType == typeof (UInt16))
            {
                return TypeCode.UInt16;
            }

            if (underlyingType == typeof (UInt64))
            {
                return TypeCode.UInt64;
            }

            if (underlyingType == typeof (Boolean))
            {
                return TypeCode.Boolean;
            }

            if (underlyingType == typeof (Char))
            {
                return TypeCode.Char;
            }

            Contract.Assert(false, "Unknown underlying type.");
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(GetValue(), CultureInfo.CurrentCulture);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(GetValue(), CultureInfo.CurrentCulture);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(GetValue(), CultureInfo.CurrentCulture);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(GetValue(), CultureInfo.CurrentCulture);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(GetValue(), CultureInfo.CurrentCulture);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(GetValue(), CultureInfo.CurrentCulture);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(GetValue(), CultureInfo.CurrentCulture);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(GetValue(), CultureInfo.CurrentCulture);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(GetValue(), CultureInfo.CurrentCulture);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(GetValue(), CultureInfo.CurrentCulture);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(GetValue(), CultureInfo.CurrentCulture);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(GetValue(), CultureInfo.CurrentCulture);
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(GetValue(), CultureInfo.CurrentCulture);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Enum", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        public static Object ToObject(Type enumType, sbyte value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, short value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, int value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, byte value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, ushort value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, uint value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, long value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value);
        }

        public static Object ToObject(Type enumType, ulong value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, unchecked ((long)value));
        }

        private static Object ToObject(Type enumType, char value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value);
        }

        private static Object ToObject(Type enumType, bool value)
        {
            if (enumType == null)
                throw new ArgumentNullException("enumType");
            if (!enumType.IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            RuntimeType rtType = enumType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "enumType");
            return InternalBoxEnum(rtType, value ? 1 : 0);
        }
    }
}