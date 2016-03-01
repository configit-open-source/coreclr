namespace System.Runtime.InteropServices
{
    using System;
    using System.Reflection;
    using System.Diagnostics.Contracts;

    public sealed class UnmanagedFunctionPointerAttribute : Attribute
    {
        CallingConvention m_callingConvention;
        public UnmanagedFunctionPointerAttribute(CallingConvention callingConvention)
        {
            m_callingConvention = callingConvention;
        }

        public CallingConvention CallingConvention
        {
            get
            {
                return m_callingConvention;
            }
        }

        public CharSet CharSet;
        public bool BestFitMapping;
        public bool ThrowOnUnmappableChar;
        public bool SetLastError;
    }

    public sealed class TypeIdentifierAttribute : Attribute
    {
        public TypeIdentifierAttribute()
        {
        }

        public TypeIdentifierAttribute(string scope, string identifier)
        {
            Scope_ = scope;
            Identifier_ = identifier;
        }

        public String Scope
        {
            get
            {
                return Scope_;
            }
        }

        public String Identifier
        {
            get
            {
                return Identifier_;
            }
        }

        internal String Scope_;
        internal String Identifier_;
    }

    public sealed class AllowReversePInvokeCallsAttribute : Attribute
    {
        public AllowReversePInvokeCallsAttribute()
        {
        }
    }

    public sealed class DispIdAttribute : Attribute
    {
        internal int _val;
        public DispIdAttribute(int dispId)
        {
            _val = dispId;
        }

        public int Value
        {
            get
            {
                return _val;
            }
        }
    }

    public enum ComInterfaceType
    {
        InterfaceIsDual = 0,
        InterfaceIsIUnknown = 1,
        InterfaceIsIDispatch = 2,
        InterfaceIsIInspectable = 3
    }

    public sealed class InterfaceTypeAttribute : Attribute
    {
        internal ComInterfaceType _val;
        public InterfaceTypeAttribute(ComInterfaceType interfaceType)
        {
            _val = interfaceType;
        }

        public InterfaceTypeAttribute(short interfaceType)
        {
            _val = (ComInterfaceType)interfaceType;
        }

        public ComInterfaceType Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class ComDefaultInterfaceAttribute : Attribute
    {
        internal Type _val;
        public ComDefaultInterfaceAttribute(Type defaultInterface)
        {
            _val = defaultInterface;
        }

        public Type Value
        {
            get
            {
                return _val;
            }
        }
    }

    public enum ClassInterfaceType
    {
        None = 0,
        AutoDispatch = 1,
        AutoDual = 2
    }

    public sealed class ClassInterfaceAttribute : Attribute
    {
        internal ClassInterfaceType _val;
        public ClassInterfaceAttribute(ClassInterfaceType classInterfaceType)
        {
            _val = classInterfaceType;
        }

        public ClassInterfaceAttribute(short classInterfaceType)
        {
            _val = (ClassInterfaceType)classInterfaceType;
        }

        public ClassInterfaceType Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class ComVisibleAttribute : Attribute
    {
        internal bool _val;
        public ComVisibleAttribute(bool visibility)
        {
            _val = visibility;
        }

        public bool Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class TypeLibImportClassAttribute : Attribute
    {
        internal String _importClassName;
        public TypeLibImportClassAttribute(Type importClass)
        {
            _importClassName = importClass.ToString();
        }

        public String Value
        {
            get
            {
                return _importClassName;
            }
        }
    }

    public sealed class LCIDConversionAttribute : Attribute
    {
        internal int _val;
        public LCIDConversionAttribute(int lcid)
        {
            _val = lcid;
        }

        public int Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class ComRegisterFunctionAttribute : Attribute
    {
        public ComRegisterFunctionAttribute()
        {
        }
    }

    public sealed class ComUnregisterFunctionAttribute : Attribute
    {
        public ComUnregisterFunctionAttribute()
        {
        }
    }

    public sealed class ProgIdAttribute : Attribute
    {
        internal String _val;
        public ProgIdAttribute(String progId)
        {
            _val = progId;
        }

        public String Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class ImportedFromTypeLibAttribute : Attribute
    {
        internal String _val;
        public ImportedFromTypeLibAttribute(String tlbFile)
        {
            _val = tlbFile;
        }

        public String Value
        {
            get
            {
                return _val;
            }
        }
    }

    public enum IDispatchImplType
    {
        SystemDefinedImpl = 0,
        InternalImpl = 1,
        CompatibleImpl = 2
    }

    public sealed class IDispatchImplAttribute : Attribute
    {
        internal IDispatchImplType _val;
        public IDispatchImplAttribute(IDispatchImplType implType)
        {
            _val = implType;
        }

        public IDispatchImplAttribute(short implType)
        {
            _val = (IDispatchImplType)implType;
        }

        public IDispatchImplType Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class ComSourceInterfacesAttribute : Attribute
    {
        internal String _val;
        public ComSourceInterfacesAttribute(String sourceInterfaces)
        {
            _val = sourceInterfaces;
        }

        public ComSourceInterfacesAttribute(Type sourceInterface)
        {
            _val = sourceInterface.FullName;
        }

        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2)
        {
            _val = sourceInterface1.FullName + "\0" + sourceInterface2.FullName;
        }

        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2, Type sourceInterface3)
        {
            _val = sourceInterface1.FullName + "\0" + sourceInterface2.FullName + "\0" + sourceInterface3.FullName;
        }

        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2, Type sourceInterface3, Type sourceInterface4)
        {
            _val = sourceInterface1.FullName + "\0" + sourceInterface2.FullName + "\0" + sourceInterface3.FullName + "\0" + sourceInterface4.FullName;
        }

        public String Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class ComConversionLossAttribute : Attribute
    {
        public ComConversionLossAttribute()
        {
        }
    }

    public enum TypeLibTypeFlags
    {
        FAppObject = 0x0001,
        FCanCreate = 0x0002,
        FLicensed = 0x0004,
        FPreDeclId = 0x0008,
        FHidden = 0x0010,
        FControl = 0x0020,
        FDual = 0x0040,
        FNonExtensible = 0x0080,
        FOleAutomation = 0x0100,
        FRestricted = 0x0200,
        FAggregatable = 0x0400,
        FReplaceable = 0x0800,
        FDispatchable = 0x1000,
        FReverseBind = 0x2000
    }

    public enum TypeLibFuncFlags
    {
        FRestricted = 0x0001,
        FSource = 0x0002,
        FBindable = 0x0004,
        FRequestEdit = 0x0008,
        FDisplayBind = 0x0010,
        FDefaultBind = 0x0020,
        FHidden = 0x0040,
        FUsesGetLastError = 0x0080,
        FDefaultCollelem = 0x0100,
        FUiDefault = 0x0200,
        FNonBrowsable = 0x0400,
        FReplaceable = 0x0800,
        FImmediateBind = 0x1000
    }

    public enum TypeLibVarFlags
    {
        FReadOnly = 0x0001,
        FSource = 0x0002,
        FBindable = 0x0004,
        FRequestEdit = 0x0008,
        FDisplayBind = 0x0010,
        FDefaultBind = 0x0020,
        FHidden = 0x0040,
        FRestricted = 0x0080,
        FDefaultCollelem = 0x0100,
        FUiDefault = 0x0200,
        FNonBrowsable = 0x0400,
        FReplaceable = 0x0800,
        FImmediateBind = 0x1000
    }

    public sealed class TypeLibTypeAttribute : Attribute
    {
        internal TypeLibTypeFlags _val;
        public TypeLibTypeAttribute(TypeLibTypeFlags flags)
        {
            _val = flags;
        }

        public TypeLibTypeAttribute(short flags)
        {
            _val = (TypeLibTypeFlags)flags;
        }

        public TypeLibTypeFlags Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class TypeLibFuncAttribute : Attribute
    {
        internal TypeLibFuncFlags _val;
        public TypeLibFuncAttribute(TypeLibFuncFlags flags)
        {
            _val = flags;
        }

        public TypeLibFuncAttribute(short flags)
        {
            _val = (TypeLibFuncFlags)flags;
        }

        public TypeLibFuncFlags Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class TypeLibVarAttribute : Attribute
    {
        internal TypeLibVarFlags _val;
        public TypeLibVarAttribute(TypeLibVarFlags flags)
        {
            _val = flags;
        }

        public TypeLibVarAttribute(short flags)
        {
            _val = (TypeLibVarFlags)flags;
        }

        public TypeLibVarFlags Value
        {
            get
            {
                return _val;
            }
        }
    }

    public enum VarEnum
    {
        VT_EMPTY = 0,
        VT_NULL = 1,
        VT_I2 = 2,
        VT_I4 = 3,
        VT_R4 = 4,
        VT_R8 = 5,
        VT_CY = 6,
        VT_DATE = 7,
        VT_BSTR = 8,
        VT_DISPATCH = 9,
        VT_ERROR = 10,
        VT_BOOL = 11,
        VT_VARIANT = 12,
        VT_UNKNOWN = 13,
        VT_DECIMAL = 14,
        VT_I1 = 16,
        VT_UI1 = 17,
        VT_UI2 = 18,
        VT_UI4 = 19,
        VT_I8 = 20,
        VT_UI8 = 21,
        VT_INT = 22,
        VT_UINT = 23,
        VT_VOID = 24,
        VT_HRESULT = 25,
        VT_PTR = 26,
        VT_SAFEARRAY = 27,
        VT_CARRAY = 28,
        VT_USERDEFINED = 29,
        VT_LPSTR = 30,
        VT_LPWSTR = 31,
        VT_RECORD = 36,
        VT_FILETIME = 64,
        VT_BLOB = 65,
        VT_STREAM = 66,
        VT_STORAGE = 67,
        VT_STREAMED_OBJECT = 68,
        VT_STORED_OBJECT = 69,
        VT_BLOB_OBJECT = 70,
        VT_CF = 71,
        VT_CLSID = 72,
        VT_VECTOR = 0x1000,
        VT_ARRAY = 0x2000,
        VT_BYREF = 0x4000
    }

    public enum UnmanagedType
    {
        Bool = 0x2,
        I1 = 0x3,
        U1 = 0x4,
        I2 = 0x5,
        U2 = 0x6,
        I4 = 0x7,
        U4 = 0x8,
        I8 = 0x9,
        U8 = 0xa,
        R4 = 0xb,
        R8 = 0xc,
        Currency = 0xf,
        BStr = 0x13,
        LPStr = 0x14,
        LPWStr = 0x15,
        LPTStr = 0x16,
        ByValTStr = 0x17,
        IUnknown = 0x19,
        IDispatch = 0x1a,
        Struct = 0x1b,
        Interface = 0x1c,
        SafeArray = 0x1d,
        ByValArray = 0x1e,
        SysInt = 0x1f,
        SysUInt = 0x20,
        VBByRefStr = 0x22,
        AnsiBStr = 0x23,
        TBStr = 0x24,
        VariantBool = 0x25,
        FunctionPtr = 0x26,
        AsAny = 0x28,
        LPArray = 0x2a,
        LPStruct = 0x2b,
        CustomMarshaler = 0x2c,
        Error = 0x2d,
        IInspectable = 0x2e,
        HString = 0x2f
    }

    public unsafe sealed class MarshalAsAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
        {
            return GetCustomAttribute(parameter.MetadataToken, parameter.GetRuntimeModule());
        }

        internal static bool IsDefined(RuntimeParameterInfo parameter)
        {
            return GetCustomAttribute(parameter) != null;
        }

        internal static Attribute GetCustomAttribute(RuntimeFieldInfo field)
        {
            return GetCustomAttribute(field.MetadataToken, field.GetRuntimeModule());
            ;
        }

        internal static bool IsDefined(RuntimeFieldInfo field)
        {
            return GetCustomAttribute(field) != null;
        }

        internal static Attribute GetCustomAttribute(int token, RuntimeModule scope)
        {
            UnmanagedType unmanagedType, arraySubType;
            VarEnum safeArraySubType;
            int sizeParamIndex = 0, sizeConst = 0;
            string marshalTypeName = null, marshalCookie = null, safeArrayUserDefinedTypeName = null;
            int iidParamIndex = 0;
            ConstArray nativeType = ModuleHandle.GetMetadataImport(scope.GetNativeHandle()).GetFieldMarshal(token);
            if (nativeType.Length == 0)
                return null;
            MetadataImport.GetMarshalAs(nativeType, out unmanagedType, out safeArraySubType, out safeArrayUserDefinedTypeName, out arraySubType, out sizeParamIndex, out sizeConst, out marshalTypeName, out marshalCookie, out iidParamIndex);
            RuntimeType safeArrayUserDefinedType = safeArrayUserDefinedTypeName == null || safeArrayUserDefinedTypeName.Length == 0 ? null : RuntimeTypeHandle.GetTypeByNameUsingCARules(safeArrayUserDefinedTypeName, scope);
            RuntimeType marshalTypeRef = null;
            try
            {
                marshalTypeRef = marshalTypeName == null ? null : RuntimeTypeHandle.GetTypeByNameUsingCARules(marshalTypeName, scope);
            }
            catch (System.TypeLoadException)
            {
                Contract.Assert(marshalTypeName != null);
            }

            return new MarshalAsAttribute(unmanagedType, safeArraySubType, safeArrayUserDefinedType, arraySubType, (short)sizeParamIndex, sizeConst, marshalTypeName, marshalTypeRef, marshalCookie, iidParamIndex);
        }

        internal MarshalAsAttribute(UnmanagedType val, VarEnum safeArraySubType, RuntimeType safeArrayUserDefinedSubType, UnmanagedType arraySubType, short sizeParamIndex, int sizeConst, string marshalType, RuntimeType marshalTypeRef, string marshalCookie, int iidParamIndex)
        {
            _val = val;
            SafeArraySubType = safeArraySubType;
            SafeArrayUserDefinedSubType = safeArrayUserDefinedSubType;
            IidParameterIndex = iidParamIndex;
            ArraySubType = arraySubType;
            SizeParamIndex = sizeParamIndex;
            SizeConst = sizeConst;
            MarshalType = marshalType;
            MarshalTypeRef = marshalTypeRef;
            MarshalCookie = marshalCookie;
        }

        internal UnmanagedType _val;
        public MarshalAsAttribute(UnmanagedType unmanagedType)
        {
            _val = unmanagedType;
        }

        public MarshalAsAttribute(short unmanagedType)
        {
            _val = (UnmanagedType)unmanagedType;
        }

        public UnmanagedType Value
        {
            get
            {
                return _val;
            }
        }

        public VarEnum SafeArraySubType;
        public Type SafeArrayUserDefinedSubType;
        public int IidParameterIndex;
        public UnmanagedType ArraySubType;
        public short SizeParamIndex;
        public int SizeConst;
        public String MarshalType;
        public Type MarshalTypeRef;
        public String MarshalCookie;
    }

    public sealed class ComImportAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeType type)
        {
            if ((type.Attributes & TypeAttributes.Import) == 0)
                return null;
            return new ComImportAttribute();
        }

        internal static bool IsDefined(RuntimeType type)
        {
            return (type.Attributes & TypeAttributes.Import) != 0;
        }

        public ComImportAttribute()
        {
        }
    }

    public sealed class GuidAttribute : Attribute
    {
        internal String _val;
        public GuidAttribute(String guid)
        {
            _val = guid;
        }

        public String Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class PreserveSigAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeMethodInfo method)
        {
            if ((method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) == 0)
                return null;
            return new PreserveSigAttribute();
        }

        internal static bool IsDefined(RuntimeMethodInfo method)
        {
            return (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0;
        }

        public PreserveSigAttribute()
        {
        }
    }

    public sealed class InAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
        {
            return parameter.IsIn ? new InAttribute() : null;
        }

        internal static bool IsDefined(RuntimeParameterInfo parameter)
        {
            return parameter.IsIn;
        }

        public InAttribute()
        {
        }
    }

    public sealed class OutAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
        {
            return parameter.IsOut ? new OutAttribute() : null;
        }

        internal static bool IsDefined(RuntimeParameterInfo parameter)
        {
            return parameter.IsOut;
        }

        public OutAttribute()
        {
        }
    }

    public sealed class OptionalAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
        {
            return parameter.IsOptional ? new OptionalAttribute() : null;
        }

        internal static bool IsDefined(RuntimeParameterInfo parameter)
        {
            return parameter.IsOptional;
        }

        public OptionalAttribute()
        {
        }
    }

    [Flags]
    public enum DllImportSearchPath
    {
        UseDllDirectoryForDependencies = 0x100,
        ApplicationDirectory = 0x200,
        UserDirectories = 0x400,
        System32 = 0x800,
        SafeDirectories = 0x1000,
        AssemblyDirectory = 0x2,
        LegacyBehavior = 0x0
    }

    public sealed class DefaultDllImportSearchPathsAttribute : Attribute
    {
        internal DllImportSearchPath _paths;
        public DefaultDllImportSearchPathsAttribute(DllImportSearchPath paths)
        {
            _paths = paths;
        }

        public DllImportSearchPath Paths
        {
            get
            {
                return _paths;
            }
        }
    }

    public unsafe sealed class DllImportAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeMethodInfo method)
        {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
                return null;
            MetadataImport scope = ModuleHandle.GetMetadataImport(method.Module.ModuleHandle.GetRuntimeModule());
            string entryPoint, dllName = null;
            int token = method.MetadataToken;
            PInvokeAttributes flags = 0;
            scope.GetPInvokeMap(token, out flags, out entryPoint, out dllName);
            CharSet charSet = CharSet.None;
            switch (flags & PInvokeAttributes.CharSetMask)
            {
                case PInvokeAttributes.CharSetNotSpec:
                    charSet = CharSet.None;
                    break;
                case PInvokeAttributes.CharSetAnsi:
                    charSet = CharSet.Ansi;
                    break;
                case PInvokeAttributes.CharSetUnicode:
                    charSet = CharSet.Unicode;
                    break;
                case PInvokeAttributes.CharSetAuto:
                    charSet = CharSet.Auto;
                    break;
                default:
                    break;
            }

            CallingConvention callingConvention = CallingConvention.Cdecl;
            switch (flags & PInvokeAttributes.CallConvMask)
            {
                case PInvokeAttributes.CallConvWinapi:
                    callingConvention = CallingConvention.Winapi;
                    break;
                case PInvokeAttributes.CallConvCdecl:
                    callingConvention = CallingConvention.Cdecl;
                    break;
                case PInvokeAttributes.CallConvStdcall:
                    callingConvention = CallingConvention.StdCall;
                    break;
                case PInvokeAttributes.CallConvThiscall:
                    callingConvention = CallingConvention.ThisCall;
                    break;
                case PInvokeAttributes.CallConvFastcall:
                    callingConvention = CallingConvention.FastCall;
                    break;
                default:
                    break;
            }

            bool exactSpelling = (flags & PInvokeAttributes.NoMangle) != 0;
            bool setLastError = (flags & PInvokeAttributes.SupportsLastError) != 0;
            bool bestFitMapping = (flags & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled;
            bool throwOnUnmappableChar = (flags & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled;
            bool preserveSig = (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0;
            return new DllImportAttribute(dllName, entryPoint, charSet, exactSpelling, setLastError, preserveSig, callingConvention, bestFitMapping, throwOnUnmappableChar);
        }

        internal static bool IsDefined(RuntimeMethodInfo method)
        {
            return (method.Attributes & MethodAttributes.PinvokeImpl) != 0;
        }

        internal DllImportAttribute(string dllName, string entryPoint, CharSet charSet, bool exactSpelling, bool setLastError, bool preserveSig, CallingConvention callingConvention, bool bestFitMapping, bool throwOnUnmappableChar)
        {
            _val = dllName;
            EntryPoint = entryPoint;
            CharSet = charSet;
            ExactSpelling = exactSpelling;
            SetLastError = setLastError;
            PreserveSig = preserveSig;
            CallingConvention = callingConvention;
            BestFitMapping = bestFitMapping;
            ThrowOnUnmappableChar = throwOnUnmappableChar;
        }

        internal String _val;
        public DllImportAttribute(String dllName)
        {
            _val = dllName;
        }

        public String Value
        {
            get
            {
                return _val;
            }
        }

        public String EntryPoint;
        public CharSet CharSet;
        public bool SetLastError;
        public bool ExactSpelling;
        public bool PreserveSig;
        public CallingConvention CallingConvention;
        public bool BestFitMapping;
        public bool ThrowOnUnmappableChar;
    }

    public unsafe sealed class StructLayoutAttribute : Attribute
    {
        private const int DEFAULT_PACKING_SIZE = 8;
        internal static Attribute GetCustomAttribute(RuntimeType type)
        {
            if (!IsDefined(type))
                return null;
            int pack = 0, size = 0;
            LayoutKind layoutKind = LayoutKind.Auto;
            switch (type.Attributes & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.ExplicitLayout:
                    layoutKind = LayoutKind.Explicit;
                    break;
                case TypeAttributes.AutoLayout:
                    layoutKind = LayoutKind.Auto;
                    break;
                case TypeAttributes.SequentialLayout:
                    layoutKind = LayoutKind.Sequential;
                    break;
                default:
                    Contract.Assume(false);
                    break;
            }

            CharSet charSet = CharSet.None;
            switch (type.Attributes & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass:
                    charSet = CharSet.Ansi;
                    break;
                case TypeAttributes.AutoClass:
                    charSet = CharSet.Auto;
                    break;
                case TypeAttributes.UnicodeClass:
                    charSet = CharSet.Unicode;
                    break;
                default:
                    Contract.Assume(false);
                    break;
            }

            type.GetRuntimeModule().MetadataImport.GetClassLayout(type.MetadataToken, out pack, out size);
            if (pack == 0)
                pack = DEFAULT_PACKING_SIZE;
            return new StructLayoutAttribute(layoutKind, pack, size, charSet);
        }

        internal static bool IsDefined(RuntimeType type)
        {
            if (type.IsInterface || type.HasElementType || type.IsGenericParameter)
                return false;
            return true;
        }

        internal LayoutKind _val;
        internal StructLayoutAttribute(LayoutKind layoutKind, int pack, int size, CharSet charSet)
        {
            _val = layoutKind;
            Pack = pack;
            Size = size;
            CharSet = charSet;
        }

        public StructLayoutAttribute(LayoutKind layoutKind)
        {
            _val = layoutKind;
        }

        public StructLayoutAttribute(short layoutKind)
        {
            _val = (LayoutKind)layoutKind;
        }

        public LayoutKind Value
        {
            get
            {
                return _val;
            }
        }

        public int Pack;
        public int Size;
        public CharSet CharSet;
    }

    public unsafe sealed class FieldOffsetAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeFieldInfo field)
        {
            int fieldOffset;
            if (field.DeclaringType != null && field.GetRuntimeModule().MetadataImport.GetFieldOffset(field.DeclaringType.MetadataToken, field.MetadataToken, out fieldOffset))
                return new FieldOffsetAttribute(fieldOffset);
            return null;
        }

        internal static bool IsDefined(RuntimeFieldInfo field)
        {
            return GetCustomAttribute(field) != null;
        }

        internal int _val;
        public FieldOffsetAttribute(int offset)
        {
            _val = offset;
        }

        public int Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class ComAliasNameAttribute : Attribute
    {
        internal String _val;
        public ComAliasNameAttribute(String alias)
        {
            _val = alias;
        }

        public String Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class AutomationProxyAttribute : Attribute
    {
        internal bool _val;
        public AutomationProxyAttribute(bool val)
        {
            _val = val;
        }

        public bool Value
        {
            get
            {
                return _val;
            }
        }
    }

    public sealed class PrimaryInteropAssemblyAttribute : Attribute
    {
        internal int _major;
        internal int _minor;
        public PrimaryInteropAssemblyAttribute(int major, int minor)
        {
            _major = major;
            _minor = minor;
        }

        public int MajorVersion
        {
            get
            {
                return _major;
            }
        }

        public int MinorVersion
        {
            get
            {
                return _minor;
            }
        }
    }

    public sealed class CoClassAttribute : Attribute
    {
        internal Type _CoClass;
        public CoClassAttribute(Type coClass)
        {
            _CoClass = coClass;
        }

        public Type CoClass
        {
            get
            {
                return _CoClass;
            }
        }
    }

    public sealed class ComEventInterfaceAttribute : Attribute
    {
        internal Type _SourceInterface;
        internal Type _EventProvider;
        public ComEventInterfaceAttribute(Type SourceInterface, Type EventProvider)
        {
            _SourceInterface = SourceInterface;
            _EventProvider = EventProvider;
        }

        public Type SourceInterface
        {
            get
            {
                return _SourceInterface;
            }
        }

        public Type EventProvider
        {
            get
            {
                return _EventProvider;
            }
        }
    }

    public sealed class TypeLibVersionAttribute : Attribute
    {
        internal int _major;
        internal int _minor;
        public TypeLibVersionAttribute(int major, int minor)
        {
            _major = major;
            _minor = minor;
        }

        public int MajorVersion
        {
            get
            {
                return _major;
            }
        }

        public int MinorVersion
        {
            get
            {
                return _minor;
            }
        }
    }

    public sealed class ComCompatibleVersionAttribute : Attribute
    {
        internal int _major;
        internal int _minor;
        internal int _build;
        internal int _revision;
        public ComCompatibleVersionAttribute(int major, int minor, int build, int revision)
        {
            _major = major;
            _minor = minor;
            _build = build;
            _revision = revision;
        }

        public int MajorVersion
        {
            get
            {
                return _major;
            }
        }

        public int MinorVersion
        {
            get
            {
                return _minor;
            }
        }

        public int BuildNumber
        {
            get
            {
                return _build;
            }
        }

        public int RevisionNumber
        {
            get
            {
                return _revision;
            }
        }
    }

    public sealed class BestFitMappingAttribute : Attribute
    {
        internal bool _bestFitMapping;
        public BestFitMappingAttribute(bool BestFitMapping)
        {
            _bestFitMapping = BestFitMapping;
        }

        public bool BestFitMapping
        {
            get
            {
                return _bestFitMapping;
            }
        }

        public bool ThrowOnUnmappableChar;
    }

    public sealed class DefaultCharSetAttribute : Attribute
    {
        internal CharSet _CharSet;
        public DefaultCharSetAttribute(CharSet charSet)
        {
            _CharSet = charSet;
        }

        public CharSet CharSet
        {
            get
            {
                return _CharSet;
            }
        }
    }

    public sealed class SetWin32ContextInIDispatchAttribute : Attribute
    {
        public SetWin32ContextInIDispatchAttribute()
        {
        }
    }

    public sealed class ManagedToNativeComInteropStubAttribute : Attribute
    {
        internal Type _classType;
        internal String _methodName;
        public ManagedToNativeComInteropStubAttribute(Type classType, String methodName)
        {
            _classType = classType;
            _methodName = methodName;
        }

        public Type ClassType
        {
            get
            {
                return _classType;
            }
        }

        public String MethodName
        {
            get
            {
                return _methodName;
            }
        }
    }
}