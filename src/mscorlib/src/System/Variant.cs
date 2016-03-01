
using System.Globalization;
using System.Runtime.InteropServices;

namespace System
{
    internal struct Variant
    {
        private Object m_objref;
        private int m_data1;
        private int m_data2;
        private int m_flags;
        internal const int CV_EMPTY = 0x0;
        internal const int CV_VOID = 0x1;
        internal const int CV_BOOLEAN = 0x2;
        internal const int CV_CHAR = 0x3;
        internal const int CV_I1 = 0x4;
        internal const int CV_U1 = 0x5;
        internal const int CV_I2 = 0x6;
        internal const int CV_U2 = 0x7;
        internal const int CV_I4 = 0x8;
        internal const int CV_U4 = 0x9;
        internal const int CV_I8 = 0xa;
        internal const int CV_U8 = 0xb;
        internal const int CV_R4 = 0xc;
        internal const int CV_R8 = 0xd;
        internal const int CV_STRING = 0xe;
        internal const int CV_PTR = 0xf;
        internal const int CV_DATETIME = 0x10;
        internal const int CV_TIMESPAN = 0x11;
        internal const int CV_OBJECT = 0x12;
        internal const int CV_DECIMAL = 0x13;
        internal const int CV_ENUM = 0x15;
        internal const int CV_MISSING = 0x16;
        internal const int CV_NULL = 0x17;
        internal const int CV_LAST = 0x18;
        internal const int TypeCodeBitMask = 0xffff;
        internal const int VTBitMask = unchecked ((int)0xff000000);
        internal const int VTBitShift = 24;
        internal const int ArrayBitMask = 0x10000;
        internal const int EnumI1 = 0x100000;
        internal const int EnumU1 = 0x200000;
        internal const int EnumI2 = 0x300000;
        internal const int EnumU2 = 0x400000;
        internal const int EnumI4 = 0x500000;
        internal const int EnumU4 = 0x600000;
        internal const int EnumI8 = 0x700000;
        internal const int EnumU8 = 0x800000;
        internal const int EnumMask = 0xF00000;
        internal static readonly Type[] ClassTypes = {typeof (System.Empty), typeof (void), typeof (Boolean), typeof (Char), typeof (SByte), typeof (Byte), typeof (Int16), typeof (UInt16), typeof (Int32), typeof (UInt32), typeof (Int64), typeof (UInt64), typeof (Single), typeof (Double), typeof (String), typeof (void), typeof (DateTime), typeof (TimeSpan), typeof (Object), typeof (Decimal), typeof (Object), typeof (System.Reflection.Missing), typeof (System.DBNull), };
        internal static readonly Variant Empty = new Variant();
        internal static readonly Variant Missing = new Variant(Variant.CV_MISSING, Type.Missing, 0, 0);
        internal static readonly Variant DBNull = new Variant(Variant.CV_NULL, System.DBNull.Value, 0, 0);
        internal extern double GetR8FromVar();
        internal extern float GetR4FromVar();
        internal extern void SetFieldsR4(float val);
        internal extern void SetFieldsR8(double val);
        internal extern void SetFieldsObject(Object val);
        internal long GetI8FromVar()
        {
            return ((long)m_data2 << 32 | ((long)m_data1 & 0xFFFFFFFFL));
        }

        internal Variant(int flags, Object or, int data1, int data2)
        {
            m_flags = flags;
            m_objref = or;
            m_data1 = data1;
            m_data2 = data2;
        }

        public Variant(bool val)
        {
            m_objref = null;
            m_flags = CV_BOOLEAN;
            m_data1 = (val) ? Boolean.True : Boolean.False;
            m_data2 = 0;
        }

        public Variant(sbyte val)
        {
            m_objref = null;
            m_flags = CV_I1;
            m_data1 = (int)val;
            m_data2 = (int)(((long)val) >> 32);
        }

        public Variant(byte val)
        {
            m_objref = null;
            m_flags = CV_U1;
            m_data1 = (int)val;
            m_data2 = 0;
        }

        public Variant(short val)
        {
            m_objref = null;
            m_flags = CV_I2;
            m_data1 = (int)val;
            m_data2 = (int)(((long)val) >> 32);
        }

        public Variant(ushort val)
        {
            m_objref = null;
            m_flags = CV_U2;
            m_data1 = (int)val;
            m_data2 = 0;
        }

        public Variant(char val)
        {
            m_objref = null;
            m_flags = CV_CHAR;
            m_data1 = (int)val;
            m_data2 = 0;
        }

        public Variant(int val)
        {
            m_objref = null;
            m_flags = CV_I4;
            m_data1 = val;
            m_data2 = val >> 31;
        }

        public Variant(uint val)
        {
            m_objref = null;
            m_flags = CV_U4;
            m_data1 = (int)val;
            m_data2 = 0;
        }

        public Variant(long val)
        {
            m_objref = null;
            m_flags = CV_I8;
            m_data1 = (int)val;
            m_data2 = (int)(val >> 32);
        }

        public Variant(ulong val)
        {
            m_objref = null;
            m_flags = CV_U8;
            m_data1 = (int)val;
            m_data2 = (int)(val >> 32);
        }

        public Variant(float val)
        {
            m_objref = null;
            m_flags = CV_R4;
            m_data1 = 0;
            m_data2 = 0;
            SetFieldsR4(val);
        }

        public Variant(double val)
        {
            m_objref = null;
            m_flags = CV_R8;
            m_data1 = 0;
            m_data2 = 0;
            SetFieldsR8(val);
        }

        public Variant(DateTime val)
        {
            m_objref = null;
            m_flags = CV_DATETIME;
            ulong ticks = (ulong)val.Ticks;
            m_data1 = (int)ticks;
            m_data2 = (int)(ticks >> 32);
        }

        public Variant(Decimal val)
        {
            m_objref = (Object)val;
            m_flags = CV_DECIMAL;
            m_data1 = 0;
            m_data2 = 0;
        }

        public Variant(Object obj)
        {
            m_data1 = 0;
            m_data2 = 0;
            VarEnum vt = VarEnum.VT_EMPTY;
            if (obj is DateTime)
            {
                m_objref = null;
                m_flags = CV_DATETIME;
                ulong ticks = (ulong)((DateTime)obj).Ticks;
                m_data1 = (int)ticks;
                m_data2 = (int)(ticks >> 32);
                return;
            }

            if (obj is String)
            {
                m_flags = CV_STRING;
                m_objref = obj;
                return;
            }

            if (obj == null)
            {
                this = Empty;
                return;
            }

            if (obj == System.DBNull.Value)
            {
                this = DBNull;
                return;
            }

            if (obj == Type.Missing)
            {
                this = Missing;
                return;
            }

            if (obj is Array)
            {
                m_flags = CV_OBJECT | ArrayBitMask;
                m_objref = obj;
                return;
            }

            m_flags = CV_EMPTY;
            m_objref = null;
            if (obj is UnknownWrapper)
            {
                vt = VarEnum.VT_UNKNOWN;
                obj = ((UnknownWrapper)obj).WrappedObject;
            }
            else if (obj is DispatchWrapper)
            {
                vt = VarEnum.VT_DISPATCH;
                obj = ((DispatchWrapper)obj).WrappedObject;
            }
            else if (obj is ErrorWrapper)
            {
                vt = VarEnum.VT_ERROR;
                obj = (Object)(((ErrorWrapper)obj).ErrorCode);
                            }
            else if (obj is CurrencyWrapper)
            {
                vt = VarEnum.VT_CY;
                obj = (Object)(((CurrencyWrapper)obj).WrappedObject);
                            }
            else if (obj is BStrWrapper)
            {
                vt = VarEnum.VT_BSTR;
                obj = (Object)(((BStrWrapper)obj).WrappedObject);
            }

            if (obj != null)
            {
                SetFieldsObject(obj);
            }

            if (vt != VarEnum.VT_EMPTY)
                m_flags |= ((int)vt << VTBitShift);
        }

        unsafe public Variant(void *voidPointer, Type pointerType)
        {
            if (pointerType == null)
                throw new ArgumentNullException("pointerType");
            if (!pointerType.IsPointer)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePointer"), "pointerType");
                        m_objref = pointerType;
            m_flags = CV_PTR;
            m_data1 = (int)voidPointer;
            m_data2 = 0;
        }

        internal int CVType
        {
            get
            {
                return (m_flags & TypeCodeBitMask);
            }
        }

        public Object ToObject()
        {
            switch (CVType)
            {
                case CV_EMPTY:
                    return null;
                case CV_BOOLEAN:
                    return (Object)(m_data1 != 0);
                case CV_I1:
                    return (Object)((sbyte)m_data1);
                case CV_U1:
                    return (Object)((byte)m_data1);
                case CV_CHAR:
                    return (Object)((char)m_data1);
                case CV_I2:
                    return (Object)((short)m_data1);
                case CV_U2:
                    return (Object)((ushort)m_data1);
                case CV_I4:
                    return (Object)(m_data1);
                case CV_U4:
                    return (Object)((uint)m_data1);
                case CV_I8:
                    return (Object)(GetI8FromVar());
                case CV_U8:
                    return (Object)((ulong)GetI8FromVar());
                case CV_R4:
                    return (Object)(GetR4FromVar());
                case CV_R8:
                    return (Object)(GetR8FromVar());
                case CV_DATETIME:
                    return new DateTime(GetI8FromVar());
                case CV_TIMESPAN:
                    return new TimeSpan(GetI8FromVar());
                case CV_ENUM:
                    return BoxEnum();
                case CV_MISSING:
                    return Type.Missing;
                case CV_NULL:
                    return System.DBNull.Value;
                case CV_DECIMAL:
                case CV_STRING:
                case CV_OBJECT:
                default:
                    return m_objref;
            }
        }

        private extern Object BoxEnum();
        internal static void MarshalHelperConvertObjectToVariant(Object o, ref Variant v)
        {
            IConvertible ic = o as IConvertible;
            if (o == null)
            {
                v = Empty;
            }
            else if (ic == null)
            {
                v = new Variant(o);
            }
            else
            {
                IFormatProvider provider = CultureInfo.InvariantCulture;
                switch (ic.GetTypeCode())
                {
                    case TypeCode.Empty:
                        v = Empty;
                        break;
                    case TypeCode.Object:
                        v = new Variant((Object)o);
                        break;
                    case TypeCode.DBNull:
                        v = DBNull;
                        break;
                    case TypeCode.Boolean:
                        v = new Variant(ic.ToBoolean(provider));
                        break;
                    case TypeCode.Char:
                        v = new Variant(ic.ToChar(provider));
                        break;
                    case TypeCode.SByte:
                        v = new Variant(ic.ToSByte(provider));
                        break;
                    case TypeCode.Byte:
                        v = new Variant(ic.ToByte(provider));
                        break;
                    case TypeCode.Int16:
                        v = new Variant(ic.ToInt16(provider));
                        break;
                    case TypeCode.UInt16:
                        v = new Variant(ic.ToUInt16(provider));
                        break;
                    case TypeCode.Int32:
                        v = new Variant(ic.ToInt32(provider));
                        break;
                    case TypeCode.UInt32:
                        v = new Variant(ic.ToUInt32(provider));
                        break;
                    case TypeCode.Int64:
                        v = new Variant(ic.ToInt64(provider));
                        break;
                    case TypeCode.UInt64:
                        v = new Variant(ic.ToUInt64(provider));
                        break;
                    case TypeCode.Single:
                        v = new Variant(ic.ToSingle(provider));
                        break;
                    case TypeCode.Double:
                        v = new Variant(ic.ToDouble(provider));
                        break;
                    case TypeCode.Decimal:
                        v = new Variant(ic.ToDecimal(provider));
                        break;
                    case TypeCode.DateTime:
                        v = new Variant(ic.ToDateTime(provider));
                        break;
                    case TypeCode.String:
                        v = new Variant(ic.ToString(provider));
                        break;
                    default:
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnknownTypeCode", ic.GetTypeCode()));
                }
            }
        }

        internal static Object MarshalHelperConvertVariantToObject(ref Variant v)
        {
            return v.ToObject();
        }

        internal static void MarshalHelperCastVariant(Object pValue, int vt, ref Variant v)
        {
            IConvertible iv = pValue as IConvertible;
            if (iv == null)
            {
                switch (vt)
                {
                    case 9:
                        v = new Variant(new DispatchWrapper(pValue));
                        break;
                    case 12:
                        v = new Variant(pValue);
                        break;
                    case 13:
                        v = new Variant(new UnknownWrapper(pValue));
                        break;
                    case 36:
                        v = new Variant(pValue);
                        break;
                    case 8:
                        if (pValue == null)
                        {
                            v = new Variant(null);
                            v.m_flags = CV_STRING;
                        }
                        else
                        {
                            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_CannotCoerceByRefVariant"));
                        }

                        break;
                    default:
                        throw new InvalidCastException(Environment.GetResourceString("InvalidCast_CannotCoerceByRefVariant"));
                }
            }
            else
            {
                IFormatProvider provider = CultureInfo.InvariantCulture;
                switch (vt)
                {
                    case 0:
                        v = Empty;
                        break;
                    case 1:
                        v = DBNull;
                        break;
                    case 2:
                        v = new Variant(iv.ToInt16(provider));
                        break;
                    case 3:
                        v = new Variant(iv.ToInt32(provider));
                        break;
                    case 4:
                        v = new Variant(iv.ToSingle(provider));
                        break;
                    case 5:
                        v = new Variant(iv.ToDouble(provider));
                        break;
                    case 6:
                        v = new Variant(new CurrencyWrapper(iv.ToDecimal(provider)));
                        break;
                    case 7:
                        v = new Variant(iv.ToDateTime(provider));
                        break;
                    case 8:
                        v = new Variant(iv.ToString(provider));
                        break;
                    case 9:
                        v = new Variant(new DispatchWrapper((Object)iv));
                        break;
                    case 10:
                        v = new Variant(new ErrorWrapper(iv.ToInt32(provider)));
                        break;
                    case 11:
                        v = new Variant(iv.ToBoolean(provider));
                        break;
                    case 12:
                        v = new Variant((Object)iv);
                        break;
                    case 13:
                        v = new Variant(new UnknownWrapper((Object)iv));
                        break;
                    case 14:
                        v = new Variant(iv.ToDecimal(provider));
                        break;
                    case 16:
                        v = new Variant(iv.ToSByte(provider));
                        break;
                    case 17:
                        v = new Variant(iv.ToByte(provider));
                        break;
                    case 18:
                        v = new Variant(iv.ToUInt16(provider));
                        break;
                    case 19:
                        v = new Variant(iv.ToUInt32(provider));
                        break;
                    case 20:
                        v = new Variant(iv.ToInt64(provider));
                        break;
                    case 21:
                        v = new Variant(iv.ToUInt64(provider));
                        break;
                    case 22:
                        v = new Variant(iv.ToInt32(provider));
                        break;
                    case 23:
                        v = new Variant(iv.ToUInt32(provider));
                        break;
                    default:
                        throw new InvalidCastException(Environment.GetResourceString("InvalidCast_CannotCoerceByRefVariant"));
                }
            }
        }
    }
}