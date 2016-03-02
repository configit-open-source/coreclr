namespace System.Runtime.InteropServices
{
    internal struct Variant
    {
        static Variant()
        {
            int variantSize = Marshal.SizeOf(typeof (Variant));
            if (IntPtr.Size == 4)
            {
                BCLDebug.Assert(variantSize == (4 * IntPtr.Size), "variant");
            }
            else
            {
                BCLDebug.Assert(IntPtr.Size == 8, "variant");
                BCLDebug.Assert(variantSize == (3 * IntPtr.Size), "variant");
            }
        }

        private TypeUnion _typeUnion;
        private Decimal _decimal;
        private struct TypeUnion
        {
            internal ushort _vt;
            internal ushort _wReserved1;
            internal ushort _wReserved2;
            internal ushort _wReserved3;
            internal UnionTypes _unionTypes;
        }

        private struct Record
        {
            private IntPtr _record;
            private IntPtr _recordInfo;
        }

        private struct UnionTypes
        {
            internal SByte _i1;
            internal Int16 _i2;
            internal Int32 _i4;
            internal Int64 _i8;
            internal Byte _ui1;
            internal UInt16 _ui2;
            internal UInt32 _ui4;
            internal UInt64 _ui8;
            internal Int32 _int;
            internal UInt32 _uint;
            internal Int16 _bool;
            internal Int32 _error;
            internal Single _r4;
            internal Double _r8;
            internal Int64 _cy;
            internal double _date;
            internal IntPtr _bstr;
            internal IntPtr _unknown;
            internal IntPtr _dispatch;
            internal IntPtr _pvarVal;
            internal IntPtr _byref;
            internal Record _record;
        }

        internal static bool IsPrimitiveType(VarEnum varEnum)
        {
            switch (varEnum)
            {
                case VarEnum.VT_I1:
                case VarEnum.VT_I2:
                case VarEnum.VT_I4:
                case VarEnum.VT_I8:
                case VarEnum.VT_UI1:
                case VarEnum.VT_UI2:
                case VarEnum.VT_UI4:
                case VarEnum.VT_UI8:
                case VarEnum.VT_INT:
                case VarEnum.VT_UINT:
                case VarEnum.VT_BOOL:
                case VarEnum.VT_R4:
                case VarEnum.VT_R8:
                case VarEnum.VT_DECIMAL:
                case VarEnum.VT_DATE:
                case VarEnum.VT_BSTR:
                    return true;
            }

            return false;
        }

        unsafe public void CopyFromIndirect(object value)
        {
            VarEnum vt = (VarEnum)(((int)this.VariantType) & ~((int)VarEnum.VT_BYREF));
            if (value == null)
            {
                if (vt == VarEnum.VT_DISPATCH || vt == VarEnum.VT_UNKNOWN || vt == VarEnum.VT_BSTR)
                {
                    *(IntPtr*)this._typeUnion._unionTypes._byref = IntPtr.Zero;
                }

                return;
            }

            switch (vt)
            {
                case VarEnum.VT_I1:
                    *(sbyte *)this._typeUnion._unionTypes._byref = (sbyte)value;
                    break;
                case VarEnum.VT_UI1:
                    *(byte *)this._typeUnion._unionTypes._byref = (byte)value;
                    break;
                case VarEnum.VT_I2:
                    *(short *)this._typeUnion._unionTypes._byref = (short)value;
                    break;
                case VarEnum.VT_UI2:
                    *(ushort *)this._typeUnion._unionTypes._byref = (ushort)value;
                    break;
                case VarEnum.VT_BOOL:
                    *(short *)this._typeUnion._unionTypes._byref = (bool)value ? (short)-1 : (short)0;
                    break;
                case VarEnum.VT_I4:
                case VarEnum.VT_INT:
                    *(int *)this._typeUnion._unionTypes._byref = (int)value;
                    break;
                case VarEnum.VT_UI4:
                case VarEnum.VT_UINT:
                    *(uint *)this._typeUnion._unionTypes._byref = (uint)value;
                    break;
                case VarEnum.VT_ERROR:
                    *(int *)this._typeUnion._unionTypes._byref = ((ErrorWrapper)value).ErrorCode;
                    break;
                case VarEnum.VT_I8:
                    *(Int64*)this._typeUnion._unionTypes._byref = (Int64)value;
                    break;
                case VarEnum.VT_UI8:
                    *(UInt64*)this._typeUnion._unionTypes._byref = (UInt64)value;
                    break;
                case VarEnum.VT_R4:
                    *(float *)this._typeUnion._unionTypes._byref = (float)value;
                    break;
                case VarEnum.VT_R8:
                    *(double *)this._typeUnion._unionTypes._byref = (double)value;
                    break;
                case VarEnum.VT_DATE:
                    *(double *)this._typeUnion._unionTypes._byref = ((DateTime)value).ToOADate();
                    break;
                case VarEnum.VT_UNKNOWN:
                    *(IntPtr*)this._typeUnion._unionTypes._byref = Marshal.GetIUnknownForObject(value);
                    break;
                case VarEnum.VT_DISPATCH:
                    *(IntPtr*)this._typeUnion._unionTypes._byref = Marshal.GetIDispatchForObject(value);
                    break;
                case VarEnum.VT_BSTR:
                    *(IntPtr*)this._typeUnion._unionTypes._byref = Marshal.StringToBSTR((string)value);
                    break;
                case VarEnum.VT_CY:
                    *(long *)this._typeUnion._unionTypes._byref = decimal.ToOACurrency((decimal)value);
                    break;
                case VarEnum.VT_DECIMAL:
                    *(decimal *)this._typeUnion._unionTypes._byref = (decimal)value;
                    break;
                case VarEnum.VT_VARIANT:
                    Marshal.GetNativeVariantForObject(value, this._typeUnion._unionTypes._byref);
                    break;
                default:
                    throw new ArgumentException("invalid argument type");
            }
        }

        public object ToObject()
        {
            if (IsEmpty)
            {
                return null;
            }

            switch (VariantType)
            {
                case VarEnum.VT_NULL:
                    return DBNull.Value;
                case VarEnum.VT_I1:
                    return AsI1;
                case VarEnum.VT_I2:
                    return AsI2;
                case VarEnum.VT_I4:
                    return AsI4;
                case VarEnum.VT_I8:
                    return AsI8;
                case VarEnum.VT_UI1:
                    return AsUi1;
                case VarEnum.VT_UI2:
                    return AsUi2;
                case VarEnum.VT_UI4:
                    return AsUi4;
                case VarEnum.VT_UI8:
                    return AsUi8;
                case VarEnum.VT_INT:
                    return AsInt;
                case VarEnum.VT_UINT:
                    return AsUint;
                case VarEnum.VT_BOOL:
                    return AsBool;
                case VarEnum.VT_ERROR:
                    return AsError;
                case VarEnum.VT_R4:
                    return AsR4;
                case VarEnum.VT_R8:
                    return AsR8;
                case VarEnum.VT_DECIMAL:
                    return AsDecimal;
                case VarEnum.VT_CY:
                    return AsCy;
                case VarEnum.VT_DATE:
                    return AsDate;
                case VarEnum.VT_BSTR:
                    return AsBstr;
                case VarEnum.VT_UNKNOWN:
                    return AsUnknown;
                case VarEnum.VT_DISPATCH:
                    return AsDispatch;
                default:
                    try
                    {
                        unsafe
                        {
                            fixed (void *pThis = &this)
                            {
                                return Marshal.GetObjectForNativeVariant((System.IntPtr)pThis);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new NotImplementedException("Variant.ToObject cannot handle" + VariantType, ex);
                    }
            }
        }

        public void Clear()
        {
            VarEnum vt = VariantType;
            if ((vt & VarEnum.VT_BYREF) != 0)
            {
                VariantType = VarEnum.VT_EMPTY;
            }
            else if (((vt & VarEnum.VT_ARRAY) != 0) || ((vt) == VarEnum.VT_BSTR) || ((vt) == VarEnum.VT_UNKNOWN) || ((vt) == VarEnum.VT_DISPATCH) || ((vt) == VarEnum.VT_VARIANT) || ((vt) == VarEnum.VT_RECORD) || ((vt) == VarEnum.VT_VARIANT))
            {
                unsafe
                {
                    fixed (void *pThis = &this)
                    {
                        NativeMethods.VariantClear((IntPtr)pThis);
                    }
                }

                BCLDebug.Assert(IsEmpty, "variant");
            }
            else
            {
                VariantType = VarEnum.VT_EMPTY;
            }
        }

        public VarEnum VariantType
        {
            get
            {
                return (VarEnum)_typeUnion._vt;
            }

            set
            {
                _typeUnion._vt = (ushort)value;
            }
        }

        internal bool IsEmpty
        {
            get
            {
                return _typeUnion._vt == ((ushort)VarEnum.VT_EMPTY);
            }
        }

        internal bool IsByRef
        {
            get
            {
                return (_typeUnion._vt & ((ushort)VarEnum.VT_BYREF)) != 0;
            }
        }

        public void SetAsNULL()
        {
            BCLDebug.Assert(IsEmpty, "variant");
            VariantType = VarEnum.VT_NULL;
        }

        public SByte AsI1
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_I1, "variant");
                return _typeUnion._unionTypes._i1;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_I1;
                _typeUnion._unionTypes._i1 = value;
            }
        }

        public Int16 AsI2
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_I2, "variant");
                return _typeUnion._unionTypes._i2;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_I2;
                _typeUnion._unionTypes._i2 = value;
            }
        }

        public Int32 AsI4
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_I4, "variant");
                return _typeUnion._unionTypes._i4;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_I4;
                _typeUnion._unionTypes._i4 = value;
            }
        }

        public Int64 AsI8
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_I8, "variant");
                return _typeUnion._unionTypes._i8;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_I8;
                _typeUnion._unionTypes._i8 = value;
            }
        }

        public Byte AsUi1
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_UI1, "variant");
                return _typeUnion._unionTypes._ui1;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_UI1;
                _typeUnion._unionTypes._ui1 = value;
            }
        }

        public UInt16 AsUi2
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_UI2, "variant");
                return _typeUnion._unionTypes._ui2;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_UI2;
                _typeUnion._unionTypes._ui2 = value;
            }
        }

        public UInt32 AsUi4
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_UI4, "variant");
                return _typeUnion._unionTypes._ui4;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_UI4;
                _typeUnion._unionTypes._ui4 = value;
            }
        }

        public UInt64 AsUi8
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_UI8, "variant");
                return _typeUnion._unionTypes._ui8;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_UI8;
                _typeUnion._unionTypes._ui8 = value;
            }
        }

        public Int32 AsInt
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_INT, "variant");
                return _typeUnion._unionTypes._int;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_INT;
                _typeUnion._unionTypes._int = value;
            }
        }

        public UInt32 AsUint
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_UINT, "variant");
                return _typeUnion._unionTypes._uint;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_UINT;
                _typeUnion._unionTypes._uint = value;
            }
        }

        public bool AsBool
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_BOOL, "variant");
                return _typeUnion._unionTypes._bool != 0;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_BOOL;
                _typeUnion._unionTypes._bool = value ? (short)-1 : (short)0;
            }
        }

        public Int32 AsError
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_ERROR, "variant");
                return _typeUnion._unionTypes._error;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_ERROR;
                _typeUnion._unionTypes._error = value;
            }
        }

        public Single AsR4
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_R4, "variant");
                return _typeUnion._unionTypes._r4;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_R4;
                _typeUnion._unionTypes._r4 = value;
            }
        }

        public Double AsR8
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_R8, "variant");
                return _typeUnion._unionTypes._r8;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_R8;
                _typeUnion._unionTypes._r8 = value;
            }
        }

        public Decimal AsDecimal
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_DECIMAL, "variant");
                Variant v = this;
                v._typeUnion._vt = 0;
                return v._decimal;
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_DECIMAL;
                _decimal = value;
                _typeUnion._vt = (ushort)VarEnum.VT_DECIMAL;
            }
        }

        public Decimal AsCy
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_CY, "variant");
                return Decimal.FromOACurrency(_typeUnion._unionTypes._cy);
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_CY;
                _typeUnion._unionTypes._cy = Decimal.ToOACurrency(value);
            }
        }

        public DateTime AsDate
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_DATE, "variant");
                return DateTime.FromOADate(_typeUnion._unionTypes._date);
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_DATE;
                _typeUnion._unionTypes._date = value.ToOADate();
            }
        }

        public String AsBstr
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_BSTR, "variant");
                return (string)Marshal.PtrToStringBSTR(this._typeUnion._unionTypes._bstr);
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_BSTR;
                this._typeUnion._unionTypes._bstr = Marshal.StringToBSTR(value);
            }
        }

        public Object AsUnknown
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_UNKNOWN, "variant");
                if (_typeUnion._unionTypes._unknown == IntPtr.Zero)
                    return null;
                return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._unknown);
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_UNKNOWN;
                if (value == null)
                    _typeUnion._unionTypes._unknown = IntPtr.Zero;
                else
                    _typeUnion._unionTypes._unknown = Marshal.GetIUnknownForObject(value);
            }
        }

        public Object AsDispatch
        {
            get
            {
                BCLDebug.Assert(VariantType == VarEnum.VT_DISPATCH, "variant");
                if (_typeUnion._unionTypes._dispatch == IntPtr.Zero)
                    return null;
                return Marshal.GetObjectForIUnknown(_typeUnion._unionTypes._dispatch);
            }

            set
            {
                BCLDebug.Assert(IsEmpty, "variant");
                VariantType = VarEnum.VT_DISPATCH;
                if (value == null)
                    _typeUnion._unionTypes._dispatch = IntPtr.Zero;
                else
                    _typeUnion._unionTypes._dispatch = Marshal.GetIDispatchForObject(value);
            }
        }

        internal IntPtr AsByRefVariant
        {
            get
            {
                BCLDebug.Assert(VariantType == (VarEnum.VT_BYREF | VarEnum.VT_VARIANT), "variant");
                return _typeUnion._unionTypes._pvarVal;
            }
        }
    }
}