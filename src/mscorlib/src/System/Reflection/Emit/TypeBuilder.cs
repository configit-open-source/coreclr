namespace System.Reflection.Emit
{
    using System;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Collections.Generic;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    public enum PackingSize
    {
        Unspecified = 0,
        Size1 = 1,
        Size2 = 2,
        Size4 = 4,
        Size8 = 8,
        Size16 = 16,
        Size32 = 32,
        Size64 = 64,
        Size128 = 128
    }

    public sealed class TypeBuilder : TypeInfo, _TypeBuilder
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo)
        {
            if (typeInfo == null)
                return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        private class CustAttr
        {
            private ConstructorInfo m_con;
            private byte[] m_binaryAttribute;
            private CustomAttributeBuilder m_customBuilder;
            public CustAttr(ConstructorInfo con, byte[] binaryAttribute)
            {
                if (con == null)
                    throw new ArgumentNullException("con");
                if (binaryAttribute == null)
                    throw new ArgumentNullException("binaryAttribute");
                Contract.EndContractBlock();
                m_con = con;
                m_binaryAttribute = binaryAttribute;
            }

            public CustAttr(CustomAttributeBuilder customBuilder)
            {
                if (customBuilder == null)
                    throw new ArgumentNullException("customBuilder");
                Contract.EndContractBlock();
                m_customBuilder = customBuilder;
            }

            public void Bake(ModuleBuilder module, int token)
            {
                if (m_customBuilder == null)
                {
                    TypeBuilder.DefineCustomAttribute(module, token, module.GetConstructorToken(m_con).Token, m_binaryAttribute, false, false);
                }
                else
                {
                    m_customBuilder.CreateCustomAttribute(module, token);
                }
            }
        }

        public static MethodInfo GetMethod(Type type, MethodInfo method)
        {
            if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeTypeBuilder"));
            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedGenericMethodDefinition"), "method");
            if (method.DeclaringType == null || !method.DeclaringType.IsGenericTypeDefinition)
                throw new ArgumentException(Environment.GetResourceString("Argument_MethodNeedGenericDeclaringType"), "method");
            if (type.GetGenericTypeDefinition() != method.DeclaringType)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidMethodDeclaringType"), "type");
            Contract.EndContractBlock();
            if (type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());
            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type");
            return MethodOnTypeBuilderInstantiation.GetMethod(method, type as TypeBuilderInstantiation);
        }

        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {
            if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeTypeBuilder"));
            if (!constructor.DeclaringType.IsGenericTypeDefinition)
                throw new ArgumentException(Environment.GetResourceString("Argument_ConstructorNeedGenericDeclaringType"), "constructor");
            Contract.EndContractBlock();
            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type");
            if (type is TypeBuilder && type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());
            if (type.GetGenericTypeDefinition() != constructor.DeclaringType)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidConstructorDeclaringType"), "type");
            return ConstructorOnTypeBuilderInstantiation.GetConstructor(constructor, type as TypeBuilderInstantiation);
        }

        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeTypeBuilder"));
            if (!field.DeclaringType.IsGenericTypeDefinition)
                throw new ArgumentException(Environment.GetResourceString("Argument_FieldNeedGenericDeclaringType"), "field");
            Contract.EndContractBlock();
            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type");
            if (type is TypeBuilder && type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());
            if (type.GetGenericTypeDefinition() != field.DeclaringType)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFieldDeclaringType"), "type");
            return FieldOnTypeBuilderInstantiation.GetField(field, type as TypeBuilderInstantiation);
        }

        public const int UnspecifiedTypeSize = 0;
        private static extern void SetParentType(RuntimeModule module, int tdTypeDef, int tkParent);
        private static extern void AddInterfaceImpl(RuntimeModule module, int tdTypeDef, int tkInterface);
        internal static extern int DefineMethod(RuntimeModule module, int tkParent, String name, byte[] signature, int sigLength, MethodAttributes attributes);
        internal static extern int DefineMethodSpec(RuntimeModule module, int tkParent, byte[] signature, int sigLength);
        internal static extern int DefineField(RuntimeModule module, int tkParent, String name, byte[] signature, int sigLength, FieldAttributes attributes);
        private static extern void SetMethodIL(RuntimeModule module, int tk, bool isInitLocals, byte[] body, int bodyLength, byte[] LocalSig, int sigLength, int maxStackSize, ExceptionHandler[] exceptions, int numExceptions, int[] tokenFixups, int numTokenFixups);
        private static extern void DefineCustomAttribute(RuntimeModule module, int tkAssociate, int tkConstructor, byte[] attr, int attrLength, bool toDisk, bool updateCompilerFlags);
        internal static void DefineCustomAttribute(ModuleBuilder module, int tkAssociate, int tkConstructor, byte[] attr, bool toDisk, bool updateCompilerFlags)
        {
            byte[] localAttr = null;
            if (attr != null)
            {
                localAttr = new byte[attr.Length];
                Array.Copy(attr, localAttr, attr.Length);
            }

            DefineCustomAttribute(module.GetNativeHandle(), tkAssociate, tkConstructor, localAttr, (localAttr != null) ? localAttr.Length : 0, toDisk, updateCompilerFlags);
        }

        internal static extern void SetPInvokeData(RuntimeModule module, String DllName, String name, int token, int linkFlags);
        internal static extern int DefineProperty(RuntimeModule module, int tkParent, String name, PropertyAttributes attributes, byte[] signature, int sigLength);
        internal static extern int DefineEvent(RuntimeModule module, int tkParent, String name, EventAttributes attributes, int tkEventType);
        internal static extern void DefineMethodSemantics(RuntimeModule module, int tkAssociation, MethodSemanticsAttributes semantics, int tkMethod);
        internal static extern void DefineMethodImpl(RuntimeModule module, int tkType, int tkBody, int tkDecl);
        internal static extern void SetMethodImpl(RuntimeModule module, int tkMethod, MethodImplAttributes MethodImplAttributes);
        internal static extern int SetParamInfo(RuntimeModule module, int tkMethod, int iSequence, ParameterAttributes iParamAttributes, String strParamName);
        internal static extern int GetTokenFromSig(RuntimeModule module, byte[] signature, int sigLength);
        internal static extern void SetFieldLayoutOffset(RuntimeModule module, int fdToken, int iOffset);
        internal static extern void SetClassLayout(RuntimeModule module, int tk, PackingSize iPackingSize, int iTypeSize);
        internal static extern void SetFieldMarshal(RuntimeModule module, int tk, byte[] ubMarshal, int ubSize);
        private static extern unsafe void SetConstantValue(RuntimeModule module, int tk, int corType, void *pValue);
        private static bool IsPublicComType(Type type)
        {
            Type enclosingType = type.DeclaringType;
            if (enclosingType != null)
            {
                if (IsPublicComType(enclosingType))
                {
                    if ((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if ((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsTypeEqual(Type t1, Type t2)
        {
            if (t1 == t2)
                return true;
            TypeBuilder tb1 = null;
            TypeBuilder tb2 = null;
            Type runtimeType1 = null;
            Type runtimeType2 = null;
            if (t1 is TypeBuilder)
            {
                tb1 = (TypeBuilder)t1;
                runtimeType1 = tb1.m_bakedRuntimeType;
            }
            else
            {
                runtimeType1 = t1;
            }

            if (t2 is TypeBuilder)
            {
                tb2 = (TypeBuilder)t2;
                runtimeType2 = tb2.m_bakedRuntimeType;
            }
            else
            {
                runtimeType2 = t2;
            }

            if (tb1 != null && tb2 != null && Object.ReferenceEquals(tb1, tb2))
                return true;
            if (runtimeType1 != null && runtimeType2 != null && runtimeType1 == runtimeType2)
                return true;
            return false;
        }

        internal static unsafe void SetConstantValue(ModuleBuilder module, int tk, Type destType, Object value)
        {
            if (value != null)
            {
                Type type = value.GetType();
                if (destType.IsByRef)
                    destType = destType.GetElementType();
                if (destType.IsEnum)
                {
                    Type underlyingType;
                    EnumBuilder enumBldr;
                    TypeBuilder typeBldr;
                    if ((enumBldr = destType as EnumBuilder) != null)
                    {
                        underlyingType = enumBldr.GetEnumUnderlyingType();
                        if (type != enumBldr.m_typeBuilder.m_bakedRuntimeType && type != underlyingType)
                            throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                    }
                    else if ((typeBldr = destType as TypeBuilder) != null)
                    {
                        underlyingType = typeBldr.m_enumUnderlyingType;
                        if (underlyingType == null || (type != typeBldr.UnderlyingSystemType && type != underlyingType))
                            throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                    }
                    else
                    {
                        Contract.Assert(destType is RuntimeType, "destType is not a runtime type, an EnumBuilder, or a TypeBuilder.");
                        underlyingType = Enum.GetUnderlyingType(destType);
                        if (type != destType && type != underlyingType)
                            throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                    }

                    type = underlyingType;
                }
                else
                {
                    if (!destType.IsAssignableFrom(type))
                        throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                }

                CorElementType corType = RuntimeTypeHandle.GetCorElementType((RuntimeType)type);
                switch (corType)
                {
                    case CorElementType.I1:
                    case CorElementType.U1:
                    case CorElementType.Boolean:
                    case CorElementType.I2:
                    case CorElementType.U2:
                    case CorElementType.Char:
                    case CorElementType.I4:
                    case CorElementType.U4:
                    case CorElementType.R4:
                    case CorElementType.I8:
                    case CorElementType.U8:
                    case CorElementType.R8:
                        fixed (byte *pData = &JitHelpers.GetPinningHelper(value).m_data)
                            SetConstantValue(module.GetNativeHandle(), tk, (int)corType, pData);
                        break;
                    default:
                        if (type == typeof (String))
                        {
                            fixed (char *pString = (string)value)
                                SetConstantValue(module.GetNativeHandle(), tk, (int)CorElementType.String, pString);
                        }
                        else if (type == typeof (DateTime))
                        {
                            long ticks = ((DateTime)value).Ticks;
                            SetConstantValue(module.GetNativeHandle(), tk, (int)CorElementType.I8, &ticks);
                        }
                        else
                        {
                            throw new ArgumentException(Environment.GetResourceString("Argument_ConstantNotSupported", type.ToString()));
                        }

                        break;
                }
            }
            else
            {
                if (destType.IsValueType)
                {
                    if (!(destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof (Nullable<>)))
                        throw new ArgumentException(Environment.GetResourceString("Argument_ConstantNull"));
                }

                SetConstantValue(module.GetNativeHandle(), tk, (int)CorElementType.Class, null);
            }
        }

        private List<CustAttr> m_ca;
        private TypeToken m_tdType;
        private ModuleBuilder m_module;
        private String m_strName;
        private String m_strNameSpace;
        private String m_strFullQualName;
        private Type m_typeParent;
        private List<Type> m_typeInterfaces;
        private TypeAttributes m_iAttr;
        private GenericParameterAttributes m_genParamAttributes;
        internal List<MethodBuilder> m_listMethods;
        internal int m_lastTokenizedMethod;
        private int m_constructorCount;
        private int m_iTypeSize;
        private PackingSize m_iPackingSize;
        private TypeBuilder m_DeclaringType;
        private Type m_enumUnderlyingType;
        internal bool m_isHiddenGlobalType;
        private bool m_hasBeenCreated;
        private RuntimeType m_bakedRuntimeType;
        private int m_genParamPos;
        private GenericTypeParameterBuilder[] m_inst;
        private bool m_bIsGenParam;
        private MethodBuilder m_declMeth;
        private TypeBuilder m_genTypeDef;
        internal TypeBuilder(ModuleBuilder module)
        {
            m_tdType = new TypeToken((int)MetadataTokenType.TypeDef);
            m_isHiddenGlobalType = true;
            m_module = (ModuleBuilder)module;
            m_listMethods = new List<MethodBuilder>();
            m_lastTokenizedMethod = -1;
        }

        internal TypeBuilder(string szName, int genParamPos, MethodBuilder declMeth)
        {
            Contract.Requires(declMeth != null);
            m_declMeth = declMeth;
            m_DeclaringType = m_declMeth.GetTypeBuilder();
            m_module = declMeth.GetModuleBuilder();
            InitAsGenericParam(szName, genParamPos);
        }

        private TypeBuilder(string szName, int genParamPos, TypeBuilder declType)
        {
            Contract.Requires(declType != null);
            m_DeclaringType = declType;
            m_module = declType.GetModuleBuilder();
            InitAsGenericParam(szName, genParamPos);
        }

        private void InitAsGenericParam(string szName, int genParamPos)
        {
            m_strName = szName;
            m_genParamPos = genParamPos;
            m_bIsGenParam = true;
            m_typeInterfaces = new List<Type>();
        }

        internal TypeBuilder(String name, TypeAttributes attr, Type parent, Type[] interfaces, ModuleBuilder module, PackingSize iPackingSize, int iTypeSize, TypeBuilder enclosingType)
        {
            Init(name, attr, parent, interfaces, module, iPackingSize, iTypeSize, enclosingType);
        }

        private void Init(String fullname, TypeAttributes attr, Type parent, Type[] interfaces, ModuleBuilder module, PackingSize iPackingSize, int iTypeSize, TypeBuilder enclosingType)
        {
            if (fullname == null)
                throw new ArgumentNullException("fullname");
            if (fullname.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "fullname");
            if (fullname[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "fullname");
            if (fullname.Length > 1023)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeNameTooLong"), "fullname");
            Contract.EndContractBlock();
            int i;
            m_module = module;
            m_DeclaringType = enclosingType;
            AssemblyBuilder containingAssem = m_module.ContainingAssemblyBuilder;
            containingAssem.m_assemblyData.CheckTypeNameConflict(fullname, enclosingType);
            if (enclosingType != null)
            {
                if (((attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public) || ((attr & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadNestedTypeFlags"), "attr");
            }

            int[] interfaceTokens = null;
            if (interfaces != null)
            {
                for (i = 0; i < interfaces.Length; i++)
                {
                    if (interfaces[i] == null)
                    {
                        throw new ArgumentNullException("interfaces");
                    }
                }

                interfaceTokens = new int[interfaces.Length + 1];
                for (i = 0; i < interfaces.Length; i++)
                {
                    interfaceTokens[i] = m_module.GetTypeTokenInternal(interfaces[i]).Token;
                }
            }

            int iLast = fullname.LastIndexOf('.');
            if (iLast == -1 || iLast == 0)
            {
                m_strNameSpace = String.Empty;
                m_strName = fullname;
            }
            else
            {
                m_strNameSpace = fullname.Substring(0, iLast);
                m_strName = fullname.Substring(iLast + 1);
            }

            VerifyTypeAttributes(attr);
            m_iAttr = attr;
            SetParent(parent);
            m_listMethods = new List<MethodBuilder>();
            m_lastTokenizedMethod = -1;
            SetInterfaces(interfaces);
            int tkParent = 0;
            if (m_typeParent != null)
                tkParent = m_module.GetTypeTokenInternal(m_typeParent).Token;
            int tkEnclosingType = 0;
            if (enclosingType != null)
            {
                tkEnclosingType = enclosingType.m_tdType.Token;
            }

            m_tdType = new TypeToken(DefineType(m_module.GetNativeHandle(), fullname, tkParent, m_iAttr, tkEnclosingType, interfaceTokens));
            m_iPackingSize = iPackingSize;
            m_iTypeSize = iTypeSize;
            if ((m_iPackingSize != 0) || (m_iTypeSize != 0))
                SetClassLayout(GetModuleBuilder().GetNativeHandle(), m_tdType.Token, m_iPackingSize, m_iTypeSize);
            m_module.AddType(FullName, this);
        }

        private MethodBuilder DefinePInvokeMethodHelper(String name, String dllName, String importName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);
            AppDomain.CheckDefinePInvokeSupported();
            lock (SyncRoot)
            {
                return DefinePInvokeMethodHelperNoLock(name, dllName, importName, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, nativeCallConv, nativeCharSet);
            }
        }

        private MethodBuilder DefinePInvokeMethodHelperNoLock(String name, String dllName, String importName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (dllName == null)
                throw new ArgumentNullException("dllName");
            if (dllName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "dllName");
            if (importName == null)
                throw new ArgumentNullException("importName");
            if (importName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "importName");
            if ((attributes & MethodAttributes.Abstract) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadPInvokeMethod"));
            Contract.EndContractBlock();
            if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadPInvokeOnInterface"));
            ThrowIfCreated();
            attributes = attributes | MethodAttributes.PinvokeImpl;
            MethodBuilder method = new MethodBuilder(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, m_module, this, false);
            int sigLength;
            byte[] sigBytes = method.GetMethodSignature().InternalGetSignature(out sigLength);
            if (m_listMethods.Contains(method))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_MethodRedefined"));
            }

            m_listMethods.Add(method);
            MethodToken token = method.GetToken();
            int linkFlags = 0;
            switch (nativeCallConv)
            {
                case CallingConvention.Winapi:
                    linkFlags = (int)PInvokeMap.CallConvWinapi;
                    break;
                case CallingConvention.Cdecl:
                    linkFlags = (int)PInvokeMap.CallConvCdecl;
                    break;
                case CallingConvention.StdCall:
                    linkFlags = (int)PInvokeMap.CallConvStdcall;
                    break;
                case CallingConvention.ThisCall:
                    linkFlags = (int)PInvokeMap.CallConvThiscall;
                    break;
                case CallingConvention.FastCall:
                    linkFlags = (int)PInvokeMap.CallConvFastcall;
                    break;
            }

            switch (nativeCharSet)
            {
                case CharSet.None:
                    linkFlags |= (int)PInvokeMap.CharSetNotSpec;
                    break;
                case CharSet.Ansi:
                    linkFlags |= (int)PInvokeMap.CharSetAnsi;
                    break;
                case CharSet.Unicode:
                    linkFlags |= (int)PInvokeMap.CharSetUnicode;
                    break;
                case CharSet.Auto:
                    linkFlags |= (int)PInvokeMap.CharSetAuto;
                    break;
            }

            SetPInvokeData(m_module.GetNativeHandle(), dllName, importName, token.Token, linkFlags);
            method.SetToken(token);
            return method;
        }

        private FieldBuilder DefineDataHelper(String name, byte[] data, int size, FieldAttributes attributes)
        {
            String strValueClassName;
            TypeBuilder valueClassType;
            FieldBuilder fdBuilder;
            TypeAttributes typeAttributes;
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (size <= 0 || size >= 0x003f0000)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadSizeForData"));
            Contract.EndContractBlock();
            ThrowIfCreated();
            strValueClassName = ModuleBuilderData.MULTI_BYTE_VALUE_CLASS + size;
            Type temp = m_module.FindTypeBuilderWithName(strValueClassName, false);
            valueClassType = temp as TypeBuilder;
            if (valueClassType == null)
            {
                typeAttributes = TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass;
                valueClassType = m_module.DefineType(strValueClassName, typeAttributes, typeof (System.ValueType), PackingSize.Size1, size);
                valueClassType.CreateType();
            }

            fdBuilder = DefineField(name, valueClassType, (attributes | FieldAttributes.Static));
            fdBuilder.SetData(data, size);
            return fdBuilder;
        }

        private void VerifyTypeAttributes(TypeAttributes attr)
        {
            if (DeclaringType == null)
            {
                if (((attr & TypeAttributes.VisibilityMask) != TypeAttributes.NotPublic) && ((attr & TypeAttributes.VisibilityMask) != TypeAttributes.Public))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeAttrNestedVisibilityOnNonNestedType"));
                }
            }
            else
            {
                if (((attr & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic) || ((attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeAttrNonNestedVisibilityNestedType"));
                }
            }

            if (((attr & TypeAttributes.LayoutMask) != TypeAttributes.AutoLayout) && ((attr & TypeAttributes.LayoutMask) != TypeAttributes.SequentialLayout) && ((attr & TypeAttributes.LayoutMask) != TypeAttributes.ExplicitLayout))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeAttrInvalidLayout"));
            }

            if ((attr & TypeAttributes.ReservedMask) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeAttrReservedBitsSet"));
            }
        }

        public bool IsCreated()
        {
            return m_hasBeenCreated;
        }

        private extern static int DefineType(RuntimeModule module, String fullname, int tkParent, TypeAttributes attributes, int tkEnclosingType, int[] interfaceTokens);
        private extern static int DefineGenericParam(RuntimeModule module, String name, int tkParent, GenericParameterAttributes attributes, int position, int[] constraints);
        private static extern void TermCreateClass(RuntimeModule module, int tk, ObjectHandleOnStack type);
        internal void ThrowIfCreated()
        {
            if (IsCreated())
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TypeHasBeenCreated"));
        }

        internal object SyncRoot
        {
            get
            {
                return m_module.SyncRoot;
            }
        }

        internal ModuleBuilder GetModuleBuilder()
        {
            return m_module;
        }

        internal RuntimeType BakedRuntimeType
        {
            get
            {
                return m_bakedRuntimeType;
            }
        }

        internal void SetGenParamAttributes(GenericParameterAttributes genericParameterAttributes)
        {
            m_genParamAttributes = genericParameterAttributes;
        }

        internal void SetGenParamCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            CustAttr ca = new CustAttr(con, binaryAttribute);
            lock (SyncRoot)
            {
                SetGenParamCustomAttributeNoLock(ca);
            }
        }

        internal void SetGenParamCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            CustAttr ca = new CustAttr(customBuilder);
            lock (SyncRoot)
            {
                SetGenParamCustomAttributeNoLock(ca);
            }
        }

        private void SetGenParamCustomAttributeNoLock(CustAttr ca)
        {
            if (m_ca == null)
                m_ca = new List<TypeBuilder.CustAttr>();
            m_ca.Add(ca);
        }

        public override String ToString()
        {
            return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
        }

        public override Type DeclaringType
        {
            get
            {
                return m_DeclaringType;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_DeclaringType;
            }
        }

        public override String Name
        {
            get
            {
                return m_strName;
            }
        }

        public override Module Module
        {
            get
            {
                return GetModuleBuilder();
            }
        }

        internal int MetadataTokenInternal
        {
            get
            {
                return m_tdType.Token;
            }
        }

        public override Guid GUID
        {
            get
            {
                if (!IsCreated())
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
                Contract.EndContractBlock();
                return m_bakedRuntimeType.GUID;
            }
        }

        public override Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

        public override Assembly Assembly
        {
            get
            {
                return m_module.Assembly;
            }
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
            }
        }

        public override String FullName
        {
            get
            {
                if (m_strFullQualName == null)
                    m_strFullQualName = TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);
                return m_strFullQualName;
            }
        }

        public override String Namespace
        {
            get
            {
                return m_strNameSpace;
            }
        }

        public override String AssemblyQualifiedName
        {
            get
            {
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);
            }
        }

        public override Type BaseType
        {
            get
            {
                return m_typeParent;
            }
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetConstructors(bindingAttr);
        }

        protected override MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            if (types == null)
            {
                return m_bakedRuntimeType.GetMethod(name, bindingAttr);
            }
            else
            {
                return m_bakedRuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            }
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetMethods(bindingAttr);
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetField(name, bindingAttr);
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetFields(bindingAttr);
        }

        public override Type GetInterface(String name, bool ignoreCase)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetInterface(name, ignoreCase);
        }

        public override Type[] GetInterfaces()
        {
            if (m_bakedRuntimeType != null)
            {
                return m_bakedRuntimeType.GetInterfaces();
            }

            if (m_typeInterfaces == null)
            {
                return EmptyArray<Type>.Value;
            }

            return m_typeInterfaces.ToArray();
        }

        public override EventInfo GetEvent(String name, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetEvent(name, bindingAttr);
        }

        public override EventInfo[] GetEvents()
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetEvents();
        }

        protected override PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetProperties(bindingAttr);
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetNestedTypes(bindingAttr);
        }

        public override Type GetNestedType(String name, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetNestedType(name, bindingAttr);
        }

        public override MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetMember(name, type, bindingAttr);
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetInterfaceMap(interfaceType);
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetEvents(bindingAttr);
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return m_bakedRuntimeType.GetMembers(bindingAttr);
        }

        public override bool IsAssignableFrom(Type c)
        {
            if (TypeBuilder.IsTypeEqual(c, this))
                return true;
            Type fromRuntimeType = null;
            TypeBuilder fromTypeBuilder = c as TypeBuilder;
            if (fromTypeBuilder != null)
                fromRuntimeType = fromTypeBuilder.m_bakedRuntimeType;
            else
                fromRuntimeType = c;
            if (fromRuntimeType != null && fromRuntimeType is RuntimeType)
            {
                if (m_bakedRuntimeType == null)
                    return false;
                return m_bakedRuntimeType.IsAssignableFrom(fromRuntimeType);
            }

            if (fromTypeBuilder == null)
                return false;
            if (fromTypeBuilder.IsSubclassOf(this))
                return true;
            if (this.IsInterface == false)
                return false;
            Type[] interfaces = fromTypeBuilder.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                if (TypeBuilder.IsTypeEqual(interfaces[i], this))
                    return true;
                if (interfaces[i].IsSubclassOf(this))
                    return true;
            }

            return false;
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return m_iAttr;
        }

        protected override bool IsArrayImpl()
        {
            return false;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return false;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return ((GetAttributeFlagsImpl() & TypeAttributes.Import) != 0) ? true : false;
        }

        public override Type GetElementType()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
        }

        public override bool IsSecurityCritical
        {
            get
            {
                if (!IsCreated())
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
                Contract.EndContractBlock();
                return m_bakedRuntimeType.IsSecurityCritical;
            }
        }

        public override bool IsSecuritySafeCritical
        {
            get
            {
                if (!IsCreated())
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
                Contract.EndContractBlock();
                return m_bakedRuntimeType.IsSecuritySafeCritical;
            }
        }

        public override bool IsSecurityTransparent
        {
            get
            {
                if (!IsCreated())
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
                Contract.EndContractBlock();
                return m_bakedRuntimeType.IsSecurityTransparent;
            }
        }

        public override bool IsSubclassOf(Type c)
        {
            Type p = this;
            if (TypeBuilder.IsTypeEqual(p, c))
                return false;
            p = p.BaseType;
            while (p != null)
            {
                if (TypeBuilder.IsTypeEqual(p, c))
                    return true;
                p = p.BaseType;
            }

            return false;
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                if (m_bakedRuntimeType != null)
                    return m_bakedRuntimeType;
                if (IsEnum)
                {
                    if (m_enumUnderlyingType == null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoUnderlyingTypeOnEnum"));
                    return m_enumUnderlyingType;
                }
                else
                {
                    return this;
                }
            }
        }

        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType("*".ToCharArray(), this, 0);
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType("&".ToCharArray(), this, 0);
        }

        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]".ToCharArray(), this, 0);
        }

        public override Type MakeArrayType(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();
            Contract.EndContractBlock();
            string szrank = "";
            if (rank == 1)
            {
                szrank = "*";
            }
            else
            {
                for (int i = 1; i < rank; i++)
                    szrank += ",";
            }

            string s = String.Format(CultureInfo.InvariantCulture, "[{0}]", szrank);
            return SymbolType.FormCompoundType((s).ToCharArray(), this, 0);
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, typeof (object) as RuntimeType, inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, attributeRuntimeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "caType");
            return CustomAttribute.IsDefined(m_bakedRuntimeType, attributeRuntimeType, inherit);
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                return m_genParamAttributes;
            }
        }

        internal void SetInterfaces(params Type[] interfaces)
        {
            ThrowIfCreated();
            m_typeInterfaces = new List<Type>();
            if (interfaces != null)
            {
                m_typeInterfaces.AddRange(interfaces);
            }
        }

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            if (names == null)
                throw new ArgumentNullException("names");
            if (names.Length == 0)
                throw new ArgumentException();
            Contract.EndContractBlock();
            for (int i = 0; i < names.Length; i++)
                if (names[i] == null)
                    throw new ArgumentNullException("names");
            if (m_inst != null)
                throw new InvalidOperationException();
            m_inst = new GenericTypeParameterBuilder[names.Length];
            for (int i = 0; i < names.Length; i++)
                m_inst[i] = new GenericTypeParameterBuilder(new TypeBuilder(names[i], i, this));
            return m_inst;
        }

        public override Type MakeGenericType(params Type[] typeArguments)
        {
            CheckContext(typeArguments);
            return TypeBuilderInstantiation.MakeGenericType(this, typeArguments);
        }

        public override Type[] GetGenericArguments()
        {
            return m_inst;
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                return IsGenericType;
            }
        }

        public override bool IsGenericType
        {
            get
            {
                return m_inst != null;
            }
        }

        public override bool IsGenericParameter
        {
            get
            {
                return m_bIsGenParam;
            }
        }

        public override bool IsConstructedGenericType
        {
            get
            {
                return false;
            }
        }

        public override int GenericParameterPosition
        {
            get
            {
                return m_genParamPos;
            }
        }

        public override MethodBase DeclaringMethod
        {
            get
            {
                return m_declMeth;
            }
        }

        public override Type GetGenericTypeDefinition()
        {
            if (IsGenericTypeDefinition)
                return this;
            if (m_genTypeDef == null)
                throw new InvalidOperationException();
            return m_genTypeDef;
        }

        public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            lock (SyncRoot)
            {
                DefineMethodOverrideNoLock(methodInfoBody, methodInfoDeclaration);
            }
        }

        private void DefineMethodOverrideNoLock(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            if (methodInfoBody == null)
                throw new ArgumentNullException("methodInfoBody");
            if (methodInfoDeclaration == null)
                throw new ArgumentNullException("methodInfoDeclaration");
            Contract.EndContractBlock();
            ThrowIfCreated();
            if (!object.ReferenceEquals(methodInfoBody.DeclaringType, this))
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_BadMethodImplBody"));
            MethodToken tkBody;
            MethodToken tkDecl;
            tkBody = m_module.GetMethodTokenInternal(methodInfoBody);
            tkDecl = m_module.GetMethodTokenInternal(methodInfoDeclaration);
            DefineMethodImpl(m_module.GetNativeHandle(), m_tdType.Token, tkBody.Token, tkDecl.Token);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            return DefineMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            return DefineMethod(name, attributes, CallingConventions.Standard, null, null);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes, CallingConventions callingConvention)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            return DefineMethod(name, attributes, callingConvention, null, null);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            return DefineMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineMethodNoLock(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
            }
        }

        private MethodBuilder DefineMethodNoLock(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            Contract.EndContractBlock();
            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);
            if (parameterTypes != null)
            {
                if (parameterTypeOptionalCustomModifiers != null && parameterTypeOptionalCustomModifiers.Length != parameterTypes.Length)
                    throw new ArgumentException(Environment.GetResourceString("Argument_MismatchedArrays", "parameterTypeOptionalCustomModifiers", "parameterTypes"));
                if (parameterTypeRequiredCustomModifiers != null && parameterTypeRequiredCustomModifiers.Length != parameterTypes.Length)
                    throw new ArgumentException(Environment.GetResourceString("Argument_MismatchedArrays", "parameterTypeRequiredCustomModifiers", "parameterTypes"));
            }

            ThrowIfCreated();
            if (!m_isHiddenGlobalType)
            {
                if (((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface) && (attributes & MethodAttributes.Abstract) == 0 && (attributes & MethodAttributes.Static) == 0)
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadAttributeOnInterfaceMethod"));
            }

            MethodBuilder method = new MethodBuilder(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, m_module, this, false);
            if (!m_isHiddenGlobalType)
            {
                if ((method.Attributes & MethodAttributes.SpecialName) != 0 && method.Name.Equals(ConstructorInfo.ConstructorName))
                {
                    m_constructorCount++;
                }
            }

            m_listMethods.Add(method);
            return method;
        }

        public ConstructorBuilder DefineTypeInitializer()
        {
            lock (SyncRoot)
            {
                return DefineTypeInitializerNoLock();
            }
        }

        private ConstructorBuilder DefineTypeInitializerNoLock()
        {
            ThrowIfCreated();
            MethodAttributes attr = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName;
            ConstructorBuilder constBuilder = new ConstructorBuilder(ConstructorInfo.TypeConstructorName, attr, CallingConventions.Standard, null, m_module, this);
            return constBuilder;
        }

        public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
        {
            if ((m_iAttr & TypeAttributes.Interface) == TypeAttributes.Interface)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ConstructorNotAllowedOnInterface"));
            }

            lock (SyncRoot)
            {
                return DefineDefaultConstructorNoLock(attributes);
            }
        }

        private ConstructorBuilder DefineDefaultConstructorNoLock(MethodAttributes attributes)
        {
            ConstructorBuilder constBuilder;
            ConstructorInfo con = null;
            if (m_typeParent is TypeBuilderInstantiation)
            {
                Type genericTypeDefinition = m_typeParent.GetGenericTypeDefinition();
                if (genericTypeDefinition is TypeBuilder)
                    genericTypeDefinition = ((TypeBuilder)genericTypeDefinition).m_bakedRuntimeType;
                if (genericTypeDefinition == null)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
                Type inst = genericTypeDefinition.MakeGenericType(m_typeParent.GetGenericArguments());
                if (inst is TypeBuilderInstantiation)
                    con = TypeBuilder.GetConstructor(inst, genericTypeDefinition.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
                else
                    con = inst.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }

            if (con == null)
            {
                con = m_typeParent.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }

            if (con == null)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoParentDefaultConstructor"));
            constBuilder = DefineConstructor(attributes, CallingConventions.Standard, null);
            m_constructorCount++;
            ILGenerator il = constBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, con);
            il.Emit(OpCodes.Ret);
            constBuilder.m_isDefaultConstructor = true;
            return constBuilder;
        }

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes)
        {
            return DefineConstructor(attributes, callingConvention, parameterTypes, null, null);
        }

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
        {
            if ((m_iAttr & TypeAttributes.Interface) == TypeAttributes.Interface && (attributes & MethodAttributes.Static) != MethodAttributes.Static)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ConstructorNotAllowedOnInterface"));
            }

            lock (SyncRoot)
            {
                return DefineConstructorNoLock(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
            }
        }

        private ConstructorBuilder DefineConstructorNoLock(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
        {
            CheckContext(parameterTypes);
            CheckContext(requiredCustomModifiers);
            CheckContext(optionalCustomModifiers);
            ThrowIfCreated();
            String name;
            if ((attributes & MethodAttributes.Static) == 0)
            {
                name = ConstructorInfo.ConstructorName;
            }
            else
            {
                name = ConstructorInfo.TypeConstructorName;
            }

            attributes = attributes | MethodAttributes.SpecialName;
            ConstructorBuilder constBuilder = new ConstructorBuilder(name, attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, m_module, this);
            m_constructorCount++;
            return constBuilder;
        }

        public MethodBuilder DefinePInvokeMethod(String name, String dllName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(name, dllName, name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null, nativeCallConv, nativeCharSet);
            return method;
        }

        public MethodBuilder DefinePInvokeMethod(String name, String dllName, String entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(name, dllName, entryName, attributes, callingConvention, returnType, null, null, parameterTypes, null, null, nativeCallConv, nativeCharSet);
            return method;
        }

        public MethodBuilder DefinePInvokeMethod(String name, String dllName, String entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(name, dllName, entryName, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, nativeCallConv, nativeCharSet);
            return method;
        }

        public TypeBuilder DefineNestedType(String name)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, TypeAttributes.NestedPrivate, null, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent, Type[] interfaces)
        {
            lock (SyncRoot)
            {
                CheckContext(parent);
                CheckContext(interfaces);
                return DefineNestedTypeNoLock(name, attr, parent, interfaces, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(String name, TypeAttributes attr)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, null, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent, int typeSize)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, typeSize);
            }
        }

        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent, PackingSize packSize)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, packSize, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent, PackingSize packSize, int typeSize)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, packSize, typeSize);
            }
        }

        private TypeBuilder DefineNestedTypeNoLock(String name, TypeAttributes attr, Type parent, Type[] interfaces, PackingSize packSize, int typeSize)
        {
            return new TypeBuilder(name, attr, parent, interfaces, m_module, packSize, typeSize, this);
        }

        public FieldBuilder DefineField(String fieldName, Type type, FieldAttributes attributes)
        {
            return DefineField(fieldName, type, null, null, attributes);
        }

        public FieldBuilder DefineField(String fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                return DefineFieldNoLock(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
            }
        }

        private FieldBuilder DefineFieldNoLock(String fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
        {
            ThrowIfCreated();
            CheckContext(type);
            CheckContext(requiredCustomModifiers);
            if (m_enumUnderlyingType == null && IsEnum == true)
            {
                if ((attributes & FieldAttributes.Static) == 0)
                {
                    m_enumUnderlyingType = type;
                }
            }

            return new FieldBuilder(this, fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
        }

        public FieldBuilder DefineInitializedData(String name, byte[] data, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                return DefineInitializedDataNoLock(name, data, attributes);
            }
        }

        private FieldBuilder DefineInitializedDataNoLock(String name, byte[] data, FieldAttributes attributes)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            Contract.EndContractBlock();
            return DefineDataHelper(name, data, data.Length, attributes);
        }

        public FieldBuilder DefineUninitializedData(String name, int size, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                return DefineUninitializedDataNoLock(name, size, attributes);
            }
        }

        private FieldBuilder DefineUninitializedDataNoLock(String name, int size, FieldAttributes attributes)
        {
            return DefineDataHelper(name, null, size, attributes);
        }

        public PropertyBuilder DefineProperty(String name, PropertyAttributes attributes, Type returnType, Type[] parameterTypes)
        {
            return DefineProperty(name, attributes, returnType, null, null, parameterTypes, null, null);
        }

        public PropertyBuilder DefineProperty(String name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            return DefineProperty(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
        }

        public PropertyBuilder DefineProperty(String name, PropertyAttributes attributes, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            return DefineProperty(name, attributes, (CallingConventions)0, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
        }

        public PropertyBuilder DefineProperty(String name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            lock (SyncRoot)
            {
                return DefinePropertyNoLock(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
            }
        }

        private PropertyBuilder DefinePropertyNoLock(String name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            Contract.EndContractBlock();
            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);
            SignatureHelper sigHelper;
            int sigLength;
            byte[] sigBytes;
            ThrowIfCreated();
            sigHelper = SignatureHelper.GetPropertySigHelper(m_module, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
            sigBytes = sigHelper.InternalGetSignature(out sigLength);
            PropertyToken prToken = new PropertyToken(DefineProperty(m_module.GetNativeHandle(), m_tdType.Token, name, attributes, sigBytes, sigLength));
            return new PropertyBuilder(m_module, name, sigHelper, attributes, returnType, prToken, this);
        }

        public EventBuilder DefineEvent(String name, EventAttributes attributes, Type eventtype)
        {
            lock (SyncRoot)
            {
                return DefineEventNoLock(name, attributes, eventtype);
            }
        }

        private EventBuilder DefineEventNoLock(String name, EventAttributes attributes, Type eventtype)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "name");
            Contract.EndContractBlock();
            int tkType;
            EventToken evToken;
            CheckContext(eventtype);
            ThrowIfCreated();
            tkType = m_module.GetTypeTokenInternal(eventtype).Token;
            evToken = new EventToken(DefineEvent(m_module.GetNativeHandle(), m_tdType.Token, name, attributes, tkType));
            return new EventBuilder(m_module, name, attributes, this, evToken);
        }

        public TypeInfo CreateTypeInfo()
        {
            lock (SyncRoot)
            {
                return CreateTypeNoLock();
            }
        }

        public Type CreateType()
        {
            lock (SyncRoot)
            {
                return CreateTypeNoLock();
            }
        }

        internal void CheckContext(params Type[][] typess)
        {
            m_module.CheckContext(typess);
        }

        internal void CheckContext(params Type[] types)
        {
            m_module.CheckContext(types);
        }

        private TypeInfo CreateTypeNoLock()
        {
            if (IsCreated())
                return m_bakedRuntimeType;
            ThrowIfCreated();
            if (m_typeInterfaces == null)
                m_typeInterfaces = new List<Type>();
            int[] interfaceTokens = new int[m_typeInterfaces.Count];
            for (int i = 0; i < m_typeInterfaces.Count; i++)
            {
                interfaceTokens[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]).Token;
            }

            int tkParent = 0;
            if (m_typeParent != null)
                tkParent = m_module.GetTypeTokenInternal(m_typeParent).Token;
            if (IsGenericParameter)
            {
                int[] constraints;
                if (m_typeParent != null)
                {
                    constraints = new int[m_typeInterfaces.Count + 2];
                    constraints[constraints.Length - 2] = tkParent;
                }
                else
                {
                    constraints = new int[m_typeInterfaces.Count + 1];
                }

                for (int i = 0; i < m_typeInterfaces.Count; i++)
                {
                    constraints[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]).Token;
                }

                int declMember = m_declMeth == null ? m_DeclaringType.m_tdType.Token : m_declMeth.GetToken().Token;
                m_tdType = new TypeToken(DefineGenericParam(m_module.GetNativeHandle(), m_strName, declMember, m_genParamAttributes, m_genParamPos, constraints));
                if (m_ca != null)
                {
                    foreach (CustAttr ca in m_ca)
                        ca.Bake(m_module, MetadataTokenInternal);
                }

                m_hasBeenCreated = true;
                return this;
            }
            else
            {
                if (((m_tdType.Token & 0x00FFFFFF) != 0) && ((tkParent & 0x00FFFFFF) != 0))
                    SetParentType(m_module.GetNativeHandle(), m_tdType.Token, tkParent);
                if (m_inst != null)
                    foreach (Type tb in m_inst)
                        if (tb is GenericTypeParameterBuilder)
                            ((GenericTypeParameterBuilder)tb).m_type.CreateType();
            }

            byte[] body;
            MethodAttributes methodAttrs;
            if (!m_isHiddenGlobalType)
            {
                if (m_constructorCount == 0 && ((m_iAttr & TypeAttributes.Interface) == 0) && !IsValueType && ((m_iAttr & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != (TypeAttributes.Abstract | TypeAttributes.Sealed)))
                {
                    DefineDefaultConstructor(MethodAttributes.Public);
                }
            }

            int size = m_listMethods.Count;
            for (int i = 0; i < size; i++)
            {
                MethodBuilder meth = m_listMethods[i];
                if (meth.IsGenericMethodDefinition)
                    meth.GetToken();
                methodAttrs = meth.Attributes;
                if (((meth.GetMethodImplementationFlags() & (MethodImplAttributes.CodeTypeMask | MethodImplAttributes.PreserveSig | MethodImplAttributes.Unmanaged)) != MethodImplAttributes.IL) || ((methodAttrs & MethodAttributes.PinvokeImpl) != (MethodAttributes)0))
                {
                    continue;
                }

                int sigLength;
                byte[] localSig = meth.GetLocalSignature(out sigLength);
                if (((methodAttrs & MethodAttributes.Abstract) != 0) && ((m_iAttr & TypeAttributes.Abstract) == 0))
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadTypeAttributesNotAbstract"));
                }

                body = meth.GetBody();
                if ((methodAttrs & MethodAttributes.Abstract) != 0)
                {
                    if (body != null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadMethodBody"));
                }
                else if (body == null || body.Length == 0)
                {
                    if (meth.m_ilGenerator != null)
                    {
                        meth.CreateMethodBodyHelper(meth.GetILGenerator());
                    }

                    body = meth.GetBody();
                    if ((body == null || body.Length == 0) && !meth.m_canBeRuntimeImpl)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadEmptyMethodBody", meth.Name));
                }

                int maxStack = meth.GetMaxStack();
                ExceptionHandler[] exceptions = meth.GetExceptionHandlers();
                int[] tokenFixups = meth.GetTokenFixups();
                SetMethodIL(m_module.GetNativeHandle(), meth.GetToken().Token, meth.InitLocals, body, (body != null) ? body.Length : 0, localSig, sigLength, maxStack, exceptions, (exceptions != null) ? exceptions.Length : 0, tokenFixups, (tokenFixups != null) ? tokenFixups.Length : 0);
                if (m_module.ContainingAssemblyBuilder.m_assemblyData.m_access == AssemblyBuilderAccess.Run)
                {
                    meth.ReleaseBakedStructures();
                }
            }

            m_hasBeenCreated = true;
            RuntimeType cls = null;
            TermCreateClass(m_module.GetNativeHandle(), m_tdType.Token, JitHelpers.GetObjectHandleOnStack(ref cls));
            if (!m_isHiddenGlobalType)
            {
                m_bakedRuntimeType = cls;
                if (m_DeclaringType != null && m_DeclaringType.m_bakedRuntimeType != null)
                {
                    m_DeclaringType.m_bakedRuntimeType.InvalidateCachedNestedType();
                }

                return cls;
            }
            else
            {
                return null;
            }
        }

        public int Size
        {
            get
            {
                return m_iTypeSize;
            }
        }

        public PackingSize PackingSize
        {
            get
            {
                return m_iPackingSize;
            }
        }

        public void SetParent(Type parent)
        {
            ThrowIfCreated();
            if (parent != null)
            {
                CheckContext(parent);
                if (parent.IsInterface)
                    throw new ArgumentException(Environment.GetResourceString("Argument_CannotSetParentToInterface"));
                m_typeParent = parent;
            }
            else
            {
                if ((m_iAttr & TypeAttributes.Interface) != TypeAttributes.Interface)
                {
                    m_typeParent = typeof (Object);
                }
                else
                {
                    if ((m_iAttr & TypeAttributes.Abstract) == 0)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadInterfaceNotAbstract"));
                    m_typeParent = null;
                }
            }
        }

        public void AddInterfaceImplementation(Type interfaceType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }

            Contract.EndContractBlock();
            CheckContext(interfaceType);
            ThrowIfCreated();
            TypeToken tkInterface = m_module.GetTypeTokenInternal(interfaceType);
            AddInterfaceImpl(m_module.GetNativeHandle(), m_tdType.Token, tkInterface.Token);
            m_typeInterfaces.Add(interfaceType);
        }

        public TypeToken TypeToken
        {
            get
            {
                if (IsGenericParameter)
                    ThrowIfCreated();
                return m_tdType;
            }
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();
            TypeBuilder.DefineCustomAttribute(m_module, m_tdType.Token, ((ModuleBuilder)m_module).GetConstructorToken(con).Token, binaryAttribute, false, false);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException("customBuilder");
            Contract.EndContractBlock();
            customBuilder.CreateCustomAttribute((ModuleBuilder)m_module, m_tdType.Token);
        }
    }
}