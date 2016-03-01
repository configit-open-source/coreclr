namespace System.Reflection.Emit
{
    using System.Runtime.InteropServices;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.SymbolStore;
    using System.Globalization;
    using System.Reflection;
    using System.Diagnostics;
    using System.IO;
    using System.Resources;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    internal sealed class InternalModuleBuilder : RuntimeModule
    {
        private InternalModuleBuilder()
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is InternalModuleBuilder)
                return ((object)this == obj);
            return obj.Equals(this);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class ModuleBuilder : Module, _ModuleBuilder
    {
        internal static extern IntPtr nCreateISymWriterForDynamicModule(Module module, String filename);
        static internal String UnmangleTypeName(String typeName)
        {
            int i = typeName.Length - 1;
            while (true)
            {
                i = typeName.LastIndexOf('+', i);
                if (i == -1)
                    break;
                bool evenSlashes = true;
                int iSlash = i;
                while (typeName[--iSlash] == '\\')
                    evenSlashes = !evenSlashes;
                if (evenSlashes)
                    break;
                i = iSlash;
            }

            return typeName.Substring(i + 1);
        }

        private Dictionary<string, Type> m_TypeBuilderDict;
        private ISymbolWriter m_iSymWriter;
        internal ModuleBuilderData m_moduleData;
        internal InternalModuleBuilder m_internalModuleBuilder;
        private AssemblyBuilder m_assemblyBuilder;
        internal AssemblyBuilder ContainingAssemblyBuilder
        {
            get
            {
                return m_assemblyBuilder;
            }
        }

        internal ModuleBuilder(AssemblyBuilder assemblyBuilder, InternalModuleBuilder internalModuleBuilder)
        {
            m_internalModuleBuilder = internalModuleBuilder;
            m_assemblyBuilder = assemblyBuilder;
        }

        internal void AddType(string name, Type type)
        {
            m_TypeBuilderDict.Add(name, type);
        }

        internal void CheckTypeNameConflict(String strTypeName, Type enclosingType)
        {
            Type foundType = null;
            if (m_TypeBuilderDict.TryGetValue(strTypeName, out foundType) && object.ReferenceEquals(foundType.DeclaringType, enclosingType))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_DuplicateTypeName"));
            }
        }

        private Type GetType(String strFormat, Type baseType)
        {
            if (strFormat == null || strFormat.Equals(String.Empty))
            {
                return baseType;
            }

            char[] bFormat = strFormat.ToCharArray();
            return SymbolType.FormCompoundType(bFormat, baseType, 0);
        }

        internal void CheckContext(params Type[][] typess)
        {
            ContainingAssemblyBuilder.CheckContext(typess);
        }

        internal void CheckContext(params Type[] types)
        {
            ContainingAssemblyBuilder.CheckContext(types);
        }

        private extern static int GetTypeRef(RuntimeModule module, String strFullName, RuntimeModule refedModule, String strRefedModuleFileName, int tkResolution);
        private extern static int GetMemberRef(RuntimeModule module, RuntimeModule refedModule, int tr, int defToken);
        private int GetMemberRef(Module refedModule, int tr, int defToken)
        {
            return GetMemberRef(GetNativeHandle(), GetRuntimeModuleFromModule(refedModule).GetNativeHandle(), tr, defToken);
        }

        private extern static int GetMemberRefFromSignature(RuntimeModule module, int tr, String methodName, byte[] signature, int length);
        private int GetMemberRefFromSignature(int tr, String methodName, byte[] signature, int length)
        {
            return GetMemberRefFromSignature(GetNativeHandle(), tr, methodName, signature, length);
        }

        private extern static int GetMemberRefOfMethodInfo(RuntimeModule module, int tr, IRuntimeMethodInfo method);
        private int GetMemberRefOfMethodInfo(int tr, RuntimeMethodInfo method)
        {
            Contract.Assert(method != null);
            if (ContainingAssemblyBuilder.ProfileAPICheck)
            {
                if ((method.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", method.FullName));
            }

            return GetMemberRefOfMethodInfo(GetNativeHandle(), tr, method);
        }

        private int GetMemberRefOfMethodInfo(int tr, RuntimeConstructorInfo method)
        {
            Contract.Assert(method != null);
            if (ContainingAssemblyBuilder.ProfileAPICheck)
            {
                if ((method.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", method.FullName));
            }

            return GetMemberRefOfMethodInfo(GetNativeHandle(), tr, method);
        }

        private extern static int GetMemberRefOfFieldInfo(RuntimeModule module, int tkType, RuntimeTypeHandle declaringType, int tkField);
        private int GetMemberRefOfFieldInfo(int tkType, RuntimeTypeHandle declaringType, RuntimeFieldInfo runtimeField)
        {
            Contract.Assert(runtimeField != null);
            if (ContainingAssemblyBuilder.ProfileAPICheck)
            {
                RtFieldInfo rtField = runtimeField as RtFieldInfo;
                if (rtField != null && (rtField.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtField.FullName));
            }

            return GetMemberRefOfFieldInfo(GetNativeHandle(), tkType, declaringType, runtimeField.MetadataToken);
        }

        private extern static int GetTokenFromTypeSpec(RuntimeModule pModule, byte[] signature, int length);
        private int GetTokenFromTypeSpec(byte[] signature, int length)
        {
            return GetTokenFromTypeSpec(GetNativeHandle(), signature, length);
        }

        private extern static int GetArrayMethodToken(RuntimeModule module, int tkTypeSpec, String methodName, byte[] signature, int sigLength);
        private extern static int GetStringConstant(RuntimeModule module, String str, int length);
        private extern static void PreSavePEFile(RuntimeModule module, int portableExecutableKind, int imageFileMachine);
        private extern static void SavePEFile(RuntimeModule module, String fileName, int entryPoint, int isExe, bool isManifestFile);
        private extern static void AddResource(RuntimeModule module, String strName, byte[] resBytes, int resByteCount, int tkFile, int attribute, int portableExecutableKind, int imageFileMachine);
        private extern static void SetModuleName(RuntimeModule module, String strModuleName);
        internal extern static void SetFieldRVAContent(RuntimeModule module, int fdToken, byte[] data, int length);
        private extern static void DefineNativeResourceFile(RuntimeModule module, String strFilename, int portableExecutableKind, int ImageFileMachine);
        private extern static void DefineNativeResourceBytes(RuntimeModule module, byte[] pbResource, int cbResource, int portableExecutableKind, int imageFileMachine);
        internal void DefineNativeResource(PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            string strResourceFileName = m_moduleData.m_strResourceFileName;
            byte[] resourceBytes = m_moduleData.m_resourceBytes;
            if (strResourceFileName != null)
            {
                DefineNativeResourceFile(GetNativeHandle(), strResourceFileName, (int)portableExecutableKind, (int)imageFileMachine);
            }
            else if (resourceBytes != null)
            {
                DefineNativeResourceBytes(GetNativeHandle(), resourceBytes, resourceBytes.Length, (int)portableExecutableKind, (int)imageFileMachine);
            }
        }

        internal virtual Type FindTypeBuilderWithName(String strTypeName, bool ignoreCase)
        {
            if (ignoreCase)
            {
                foreach (string name in m_TypeBuilderDict.Keys)
                {
                    if (String.Compare(name, strTypeName, (StringComparison.OrdinalIgnoreCase)) == 0)
                        return m_TypeBuilderDict[name];
                }
            }
            else
            {
                Type foundType;
                if (m_TypeBuilderDict.TryGetValue(strTypeName, out foundType))
                    return foundType;
            }

            return null;
        }

        private int GetTypeRefNested(Type type, Module refedModule, String strRefedModuleFileName)
        {
            Type enclosingType = type.DeclaringType;
            int tkResolution = 0;
            String typeName = type.FullName;
            if (enclosingType != null)
            {
                tkResolution = GetTypeRefNested(enclosingType, refedModule, strRefedModuleFileName);
                typeName = UnmangleTypeName(typeName);
            }

            Contract.Assert(!type.IsByRef, "Must not be ByRef.");
            Contract.Assert(!type.IsGenericType || type.IsGenericTypeDefinition, "Must not have generic arguments.");
            if (ContainingAssemblyBuilder.ProfileAPICheck)
            {
                RuntimeType rtType = type as RuntimeType;
                if (rtType != null && (rtType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", rtType.FullName));
                }
            }

            return GetTypeRef(GetNativeHandle(), typeName, GetRuntimeModuleFromModule(refedModule).GetNativeHandle(), strRefedModuleFileName, tkResolution);
        }

        internal MethodToken InternalGetConstructorToken(ConstructorInfo con, bool usingRef)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            Contract.EndContractBlock();
            int tr;
            int mr = 0;
            ConstructorBuilder conBuilder = null;
            ConstructorOnTypeBuilderInstantiation conOnTypeBuilderInst = null;
            RuntimeConstructorInfo rtCon = null;
            if ((conBuilder = con as ConstructorBuilder) != null)
            {
                if (usingRef == false && conBuilder.Module.Equals(this))
                    return conBuilder.GetToken();
                tr = GetTypeTokenInternal(con.ReflectedType).Token;
                mr = GetMemberRef(con.ReflectedType.Module, tr, conBuilder.GetToken().Token);
            }
            else if ((conOnTypeBuilderInst = con as ConstructorOnTypeBuilderInstantiation) != null)
            {
                if (usingRef == true)
                    throw new InvalidOperationException();
                tr = GetTypeTokenInternal(con.DeclaringType).Token;
                mr = GetMemberRef(con.DeclaringType.Module, tr, conOnTypeBuilderInst.MetadataTokenInternal);
            }
            else if ((rtCon = con as RuntimeConstructorInfo) != null && con.ReflectedType.IsArray == false)
            {
                tr = GetTypeTokenInternal(con.ReflectedType).Token;
                mr = GetMemberRefOfMethodInfo(tr, rtCon);
            }
            else
            {
                ParameterInfo[] parameters = con.GetParameters();
                if (parameters == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidConstructorInfo"));
                int count = parameters.Length;
                Type[] parameterTypes = new Type[count];
                Type[][] requiredCustomModifiers = new Type[count][];
                Type[][] optionalCustomModifiers = new Type[count][];
                for (int i = 0; i < count; i++)
                {
                    if (parameters[i] == null)
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidConstructorInfo"));
                    parameterTypes[i] = parameters[i].ParameterType;
                    requiredCustomModifiers[i] = parameters[i].GetRequiredCustomModifiers();
                    optionalCustomModifiers[i] = parameters[i].GetOptionalCustomModifiers();
                }

                tr = GetTypeTokenInternal(con.ReflectedType).Token;
                SignatureHelper sigHelp = SignatureHelper.GetMethodSigHelper(this, con.CallingConvention, null, null, null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
                int length;
                byte[] sigBytes = sigHelp.InternalGetSignature(out length);
                mr = GetMemberRefFromSignature(tr, con.Name, sigBytes, length);
            }

            return new MethodToken(mr);
        }

        internal void Init(String strModuleName, String strFileName, int tkFile)
        {
            m_moduleData = new ModuleBuilderData(this, strModuleName, strFileName, tkFile);
            m_TypeBuilderDict = new Dictionary<string, Type>();
        }

        internal void ModifyModuleName(string name)
        {
            m_moduleData.ModifyModuleName(name);
            ModuleBuilder.SetModuleName(GetNativeHandle(), name);
        }

        internal void SetSymWriter(ISymbolWriter writer)
        {
            m_iSymWriter = writer;
        }

        internal object SyncRoot
        {
            get
            {
                return ContainingAssemblyBuilder.SyncRoot;
            }
        }

        internal InternalModuleBuilder InternalModule
        {
            get
            {
                return m_internalModuleBuilder;
            }
        }

        internal override ModuleHandle GetModuleHandle()
        {
            return new ModuleHandle(GetNativeHandle());
        }

        internal RuntimeModule GetNativeHandle()
        {
            return InternalModule.GetNativeHandle();
        }

        private static RuntimeModule GetRuntimeModuleFromModule(Module m)
        {
            ModuleBuilder mb = m as ModuleBuilder;
            if (mb != null)
            {
                return mb.InternalModule;
            }

            return m as RuntimeModule;
        }

        private int GetMemberRefToken(MethodBase method, IEnumerable<Type> optionalParameterTypes)
        {
            Type[] parameterTypes;
            Type returnType;
            int tkParent;
            int cGenericParameters = 0;
            if (method.IsGenericMethod)
            {
                if (!method.IsGenericMethodDefinition)
                    throw new InvalidOperationException();
                cGenericParameters = method.GetGenericArguments().Length;
            }

            if (optionalParameterTypes != null)
            {
                if ((method.CallingConvention & CallingConventions.VarArgs) == 0)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAVarArgCallingConvention"));
                }
            }

            MethodInfo masmi = method as MethodInfo;
            if (method.DeclaringType.IsGenericType)
            {
                MethodBase methDef = null;
                MethodOnTypeBuilderInstantiation motbi;
                ConstructorOnTypeBuilderInstantiation cotbi;
                if ((motbi = method as MethodOnTypeBuilderInstantiation) != null)
                {
                    methDef = motbi.m_method;
                }
                else if ((cotbi = method as ConstructorOnTypeBuilderInstantiation) != null)
                {
                    methDef = cotbi.m_ctor;
                }
                else if (method is MethodBuilder || method is ConstructorBuilder)
                {
                    methDef = method;
                }
                else
                {
                    Contract.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);
                    if (method.IsGenericMethod)
                    {
                        Contract.Assert(masmi != null);
                        methDef = masmi.GetGenericMethodDefinition();
                        methDef = methDef.Module.ResolveMethod(method.MetadataToken, methDef.DeclaringType != null ? methDef.DeclaringType.GetGenericArguments() : null, methDef.GetGenericArguments());
                    }
                    else
                    {
                        methDef = method.Module.ResolveMethod(method.MetadataToken, method.DeclaringType != null ? method.DeclaringType.GetGenericArguments() : null, null);
                    }
                }

                parameterTypes = methDef.GetParameterTypes();
                returnType = MethodBuilder.GetMethodBaseReturnType(methDef);
            }
            else
            {
                parameterTypes = method.GetParameterTypes();
                returnType = MethodBuilder.GetMethodBaseReturnType(method);
            }

            int sigLength;
            byte[] sigBytes = GetMemberRefSignature(method.CallingConvention, returnType, parameterTypes, optionalParameterTypes, cGenericParameters).InternalGetSignature(out sigLength);
            if (method.DeclaringType.IsGenericType)
            {
                int length;
                byte[] sig = SignatureHelper.GetTypeSigToken(this, method.DeclaringType).InternalGetSignature(out length);
                tkParent = GetTokenFromTypeSpec(sig, length);
            }
            else if (!method.Module.Equals(this))
            {
                tkParent = GetTypeToken(method.DeclaringType).Token;
            }
            else
            {
                if (masmi != null)
                    tkParent = GetMethodToken(masmi).Token;
                else
                    tkParent = GetConstructorToken(method as ConstructorInfo).Token;
            }

            return GetMemberRefFromSignature(tkParent, method.Name, sigBytes, sigLength);
        }

        internal SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, IEnumerable<Type> optionalParameterTypes, int cGenericParameters)
        {
            int cParams = (parameterTypes == null) ? 0 : parameterTypes.Length;
            SignatureHelper sig = SignatureHelper.GetMethodSigHelper(this, call, returnType, cGenericParameters);
            for (int i = 0; i < cParams; i++)
            {
                sig.AddArgument(parameterTypes[i]);
            }

            if (optionalParameterTypes != null)
            {
                int i = 0;
                foreach (Type type in optionalParameterTypes)
                {
                    if (i == 0)
                    {
                        sig.AddSentinel();
                    }

                    sig.AddArgument(type);
                    i++;
                }
            }

            return sig;
        }

        public override bool Equals(object obj)
        {
            return InternalModule.Equals(obj);
        }

        public override int GetHashCode()
        {
            return InternalModule.GetHashCode();
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return InternalModule.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return InternalModule.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return InternalModule.IsDefined(attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return InternalModule.GetCustomAttributesData();
        }

        public override Type[] GetTypes()
        {
            lock (SyncRoot)
            {
                return GetTypesNoLock();
            }
        }

        internal Type[] GetTypesNoLock()
        {
            int size = m_TypeBuilderDict.Count;
            Type[] typeList = new Type[m_TypeBuilderDict.Count];
            int i = 0;
            foreach (Type builder in m_TypeBuilderDict.Values)
            {
                EnumBuilder enumBldr = builder as EnumBuilder;
                TypeBuilder tmpTypeBldr;
                if (enumBldr != null)
                    tmpTypeBldr = enumBldr.m_typeBuilder;
                else
                    tmpTypeBldr = (TypeBuilder)builder;
                if (tmpTypeBldr.IsCreated())
                    typeList[i++] = tmpTypeBldr.UnderlyingSystemType;
                else
                    typeList[i++] = builder;
            }

            return typeList;
        }

        public override Type GetType(String className)
        {
            return GetType(className, false, false);
        }

        public override Type GetType(String className, bool ignoreCase)
        {
            return GetType(className, false, ignoreCase);
        }

        public override Type GetType(String className, bool throwOnError, bool ignoreCase)
        {
            lock (SyncRoot)
            {
                return GetTypeNoLock(className, throwOnError, ignoreCase);
            }
        }

        private Type GetTypeNoLock(String className, bool throwOnError, bool ignoreCase)
        {
            Type baseType = InternalModule.GetType(className, throwOnError, ignoreCase);
            if (baseType != null)
                return baseType;
            String baseName = null;
            String parameters = null;
            int startIndex = 0;
            while (startIndex <= className.Length)
            {
                int i = className.IndexOfAny(new char[]{'[', '*', '&'}, startIndex);
                if (i == -1)
                {
                    baseName = className;
                    parameters = null;
                    break;
                }

                int slashes = 0;
                for (int j = i - 1; j >= 0 && className[j] == '\\'; j--)
                    slashes++;
                if (slashes % 2 == 1)
                {
                    startIndex = i + 1;
                    continue;
                }

                baseName = className.Substring(0, i);
                parameters = className.Substring(i);
                break;
            }

            if (baseName == null)
            {
                baseName = className;
                parameters = null;
            }

            baseName = baseName.Replace(@"\\", @"\").Replace(@"\[", @"[").Replace(@"\*", @"*").Replace(@"\&", @"&");
            if (parameters != null)
            {
                baseType = InternalModule.GetType(baseName, false, ignoreCase);
            }

            if (baseType == null)
            {
                baseType = FindTypeBuilderWithName(baseName, ignoreCase);
                if (baseType == null && Assembly is AssemblyBuilder)
                {
                    int size;
                    List<ModuleBuilder> modList;
                    modList = ContainingAssemblyBuilder.m_assemblyData.m_moduleBuilderList;
                    size = modList.Count;
                    for (int i = 0; i < size && baseType == null; i++)
                    {
                        ModuleBuilder mBuilder = modList[i];
                        baseType = mBuilder.FindTypeBuilderWithName(baseName, ignoreCase);
                    }
                }

                if (baseType == null)
                    return null;
            }

            if (parameters == null)
                return baseType;
            return GetType(parameters, baseType);
        }

        public override String FullyQualifiedName
        {
            [System.Security.SecurityCritical]
            get
            {
                String fullyQualifiedName = m_moduleData.m_strFileName;
                if (fullyQualifiedName == null)
                    return null;
                if (ContainingAssemblyBuilder.m_assemblyData.m_strDir != null)
                {
                    fullyQualifiedName = Path.Combine(ContainingAssemblyBuilder.m_assemblyData.m_strDir, fullyQualifiedName);
                    fullyQualifiedName = Path.UnsafeGetFullPath(fullyQualifiedName);
                }

                if (ContainingAssemblyBuilder.m_assemblyData.m_strDir != null && fullyQualifiedName != null)
                {
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, fullyQualifiedName).Demand();
                }

                return fullyQualifiedName;
            }
        }

        public override byte[] ResolveSignature(int metadataToken)
        {
            return InternalModule.ResolveSignature(metadataToken);
        }

        public override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return InternalModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return InternalModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return InternalModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return InternalModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override string ResolveString(int metadataToken)
        {
            return InternalModule.ResolveString(metadataToken);
        }

        public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            InternalModule.GetPEKind(out peKind, out machine);
        }

        public override int MDStreamVersion
        {
            get
            {
                return InternalModule.MDStreamVersion;
            }
        }

        public override Guid ModuleVersionId
        {
            get
            {
                return InternalModule.ModuleVersionId;
            }
        }

        public override int MetadataToken
        {
            get
            {
                return InternalModule.MetadataToken;
            }
        }

        public override bool IsResource()
        {
            return InternalModule.IsResource();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            return InternalModule.GetFields(bindingFlags);
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            return InternalModule.GetField(name, bindingAttr);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            return InternalModule.GetMethods(bindingFlags);
        }

        protected override MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return InternalModule.GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public override String ScopeName
        {
            get
            {
                return InternalModule.ScopeName;
            }
        }

        public override String Name
        {
            get
            {
                return InternalModule.Name;
            }
        }

        public override Assembly Assembly
        {
            [Pure]
            get
            {
                return m_assemblyBuilder;
            }
        }

        public TypeBuilder DefineType(String name)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineTypeNoLock(name, TypeAttributes.NotPublic, null, null, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineType(String name, TypeAttributes attr)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineTypeNoLock(name, attr, null, null, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            lock (SyncRoot)
            {
                CheckContext(parent);
                return DefineTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent, int typesize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, typesize);
            }
        }

        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent, PackingSize packingSize, int typesize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineTypeNoLock(name, attr, parent, null, packingSize, typesize);
            }
        }

        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent, Type[] interfaces)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineTypeNoLock(name, attr, parent, interfaces, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
            }
        }

        private TypeBuilder DefineTypeNoLock(String name, TypeAttributes attr, Type parent, Type[] interfaces, PackingSize packingSize, int typesize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            return new TypeBuilder(name, attr, parent, interfaces, this, packingSize, typesize, null);
            ;
        }

        public TypeBuilder DefineType(String name, TypeAttributes attr, Type parent, PackingSize packsize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineTypeNoLock(name, attr, parent, packsize);
            }
        }

        private TypeBuilder DefineTypeNoLock(String name, TypeAttributes attr, Type parent, PackingSize packsize)
        {
            Contract.Ensures(Contract.Result<TypeBuilder>() != null);
            return new TypeBuilder(name, attr, parent, null, this, packsize, TypeBuilder.UnspecifiedTypeSize, null);
        }

        public EnumBuilder DefineEnum(String name, TypeAttributes visibility, Type underlyingType)
        {
            Contract.Ensures(Contract.Result<EnumBuilder>() != null);
            CheckContext(underlyingType);
            lock (SyncRoot)
            {
                EnumBuilder enumBuilder = DefineEnumNoLock(name, visibility, underlyingType);
                Contract.Assert(name == enumBuilder.FullName);
                Contract.Assert(enumBuilder.m_typeBuilder == m_TypeBuilderDict[name]);
                m_TypeBuilderDict[name] = enumBuilder;
                return enumBuilder;
            }
        }

        private EnumBuilder DefineEnumNoLock(String name, TypeAttributes visibility, Type underlyingType)
        {
            Contract.Ensures(Contract.Result<EnumBuilder>() != null);
            return new EnumBuilder(name, underlyingType, visibility, this);
        }

        public void DefineManifestResource(String name, Stream stream, ResourceAttributes attribute)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (stream == null)
                throw new ArgumentNullException("stream");
            Contract.EndContractBlock();
            lock (SyncRoot)
            {
                DefineManifestResourceNoLock(name, stream, attribute);
            }
        }

        private void DefineManifestResourceNoLock(String name, Stream stream, ResourceAttributes attribute)
        {
            if (IsTransient())
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadResourceContainer"));
            Contract.EndContractBlock();
        }

        public void DefineUnmanagedResource(Byte[] resource)
        {
            lock (SyncRoot)
            {
                DefineUnmanagedResourceInternalNoLock(resource);
            }
        }

        internal void DefineUnmanagedResourceInternalNoLock(Byte[] resource)
        {
            if (resource == null)
                throw new ArgumentNullException("resource");
            Contract.EndContractBlock();
            if (m_moduleData.m_strResourceFileName != null || m_moduleData.m_resourceBytes != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            m_moduleData.m_resourceBytes = new byte[resource.Length];
            System.Array.Copy(resource, m_moduleData.m_resourceBytes, resource.Length);
        }

        public void DefineUnmanagedResource(String resourceFileName)
        {
            lock (SyncRoot)
            {
                DefineUnmanagedResourceFileInternalNoLock(resourceFileName);
            }
        }

        internal void DefineUnmanagedResourceFileInternalNoLock(String resourceFileName)
        {
            if (resourceFileName == null)
                throw new ArgumentNullException("resourceFileName");
            Contract.EndContractBlock();
            if (m_moduleData.m_resourceBytes != null || m_moduleData.m_strResourceFileName != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            string strFullFileName;
            strFullFileName = Path.UnsafeGetFullPath(resourceFileName);
            new FileIOPermission(FileIOPermissionAccess.Read, strFullFileName).Demand();
            new EnvironmentPermission(PermissionState.Unrestricted).Assert();
            try
            {
                if (File.UnsafeExists(resourceFileName) == false)
                    throw new FileNotFoundException(Environment.GetResourceString("IO.FileNotFound_FileName", resourceFileName), resourceFileName);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }

            m_moduleData.m_strResourceFileName = strFullFileName;
        }

        public MethodBuilder DefineGlobalMethod(String name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            return DefineGlobalMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
        }

        public MethodBuilder DefineGlobalMethod(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            return DefineGlobalMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
        }

        public MethodBuilder DefineGlobalMethod(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            lock (SyncRoot)
            {
                return DefineGlobalMethodNoLock(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
            }
        }

        private MethodBuilder DefineGlobalMethodNoLock(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            if (m_moduleData.m_fGlobalBeenCreated == true)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if ((attributes & MethodAttributes.Static) == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_GlobalFunctionHasToBeStatic"));
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            Contract.EndContractBlock();
            CheckContext(returnType);
            CheckContext(requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes);
            CheckContext(requiredParameterTypeCustomModifiers);
            CheckContext(optionalParameterTypeCustomModifiers);
            m_moduleData.m_fHasGlobal = true;
            return m_moduleData.m_globalTypeBuilder.DefineMethod(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
        }

        public MethodBuilder DefinePInvokeMethod(String name, String dllName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            return DefinePInvokeMethod(name, dllName, name, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
        }

        public MethodBuilder DefinePInvokeMethod(String name, String dllName, String entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            lock (SyncRoot)
            {
                return DefinePInvokeMethodNoLock(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
            }
        }

        private MethodBuilder DefinePInvokeMethodNoLock(String name, String dllName, String entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            if ((attributes & MethodAttributes.Static) == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_GlobalFunctionHasToBeStatic"));
            }

            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            Contract.EndContractBlock();
            CheckContext(returnType);
            CheckContext(parameterTypes);
            m_moduleData.m_fHasGlobal = true;
            return m_moduleData.m_globalTypeBuilder.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);
        }

        public void CreateGlobalFunctions()
        {
            lock (SyncRoot)
            {
                CreateGlobalFunctionsNoLock();
            }
        }

        private void CreateGlobalFunctionsNoLock()
        {
            if (m_moduleData.m_fGlobalBeenCreated)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }

            m_moduleData.m_globalTypeBuilder.CreateType();
            m_moduleData.m_fGlobalBeenCreated = true;
        }

        public FieldBuilder DefineInitializedData(String name, byte[] data, FieldAttributes attributes)
        {
            Contract.Ensures(Contract.Result<FieldBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineInitializedDataNoLock(name, data, attributes);
            }
        }

        private FieldBuilder DefineInitializedDataNoLock(String name, byte[] data, FieldAttributes attributes)
        {
            if (m_moduleData.m_fGlobalBeenCreated == true)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
            }

            Contract.Ensures(Contract.Result<FieldBuilder>() != null);
            Contract.EndContractBlock();
            m_moduleData.m_fHasGlobal = true;
            return m_moduleData.m_globalTypeBuilder.DefineInitializedData(name, data, attributes);
        }

        public FieldBuilder DefineUninitializedData(String name, int size, FieldAttributes attributes)
        {
            Contract.Ensures(Contract.Result<FieldBuilder>() != null);
            lock (SyncRoot)
            {
                return DefineUninitializedDataNoLock(name, size, attributes);
            }
        }

        private FieldBuilder DefineUninitializedDataNoLock(String name, int size, FieldAttributes attributes)
        {
            if (m_moduleData.m_fGlobalBeenCreated == true)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GlobalsHaveBeenCreated"));
            }

            Contract.Ensures(Contract.Result<FieldBuilder>() != null);
            Contract.EndContractBlock();
            m_moduleData.m_fHasGlobal = true;
            return m_moduleData.m_globalTypeBuilder.DefineUninitializedData(name, size, attributes);
        }

        internal TypeToken GetTypeTokenInternal(Type type)
        {
            return GetTypeTokenInternal(type, false);
        }

        private TypeToken GetTypeTokenInternal(Type type, bool getGenericDefinition)
        {
            lock (SyncRoot)
            {
                return GetTypeTokenWorkerNoLock(type, getGenericDefinition);
            }
        }

        public TypeToken GetTypeToken(Type type)
        {
            return GetTypeTokenInternal(type, true);
        }

        private TypeToken GetTypeTokenWorkerNoLock(Type type, bool getGenericDefinition)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            CheckContext(type);
            if (type.IsByRef)
                throw new ArgumentException(Environment.GetResourceString("Argument_CannotGetTypeTokenForByRef"));
            if ((type.IsGenericType && (!type.IsGenericTypeDefinition || !getGenericDefinition)) || type.IsGenericParameter || type.IsArray || type.IsPointer)
            {
                int length;
                byte[] sig = SignatureHelper.GetTypeSigToken(this, type).InternalGetSignature(out length);
                return new TypeToken(GetTokenFromTypeSpec(sig, length));
            }

            Module refedModule = type.Module;
            if (refedModule.Equals(this))
            {
                TypeBuilder typeBuilder = null;
                GenericTypeParameterBuilder paramBuilder = null;
                EnumBuilder enumBuilder = type as EnumBuilder;
                if (enumBuilder != null)
                    typeBuilder = enumBuilder.m_typeBuilder;
                else
                    typeBuilder = type as TypeBuilder;
                if (typeBuilder != null)
                {
                    return typeBuilder.TypeToken;
                }
                else if ((paramBuilder = type as GenericTypeParameterBuilder) != null)
                {
                    return new TypeToken(paramBuilder.MetadataTokenInternal);
                }

                return new TypeToken(GetTypeRefNested(type, this, String.Empty));
            }

            ModuleBuilder refedModuleBuilder = refedModule as ModuleBuilder;
            String strRefedModuleFileName = String.Empty;
            if (refedModule.Assembly.Equals(this.Assembly))
            {
                if (refedModuleBuilder == null)
                {
                    refedModuleBuilder = this.ContainingAssemblyBuilder.GetModuleBuilder((InternalModuleBuilder)refedModule);
                }

                strRefedModuleFileName = refedModuleBuilder.m_moduleData.m_strFileName;
            }

            return new TypeToken(GetTypeRefNested(type, refedModule, strRefedModuleFileName));
        }

        public TypeToken GetTypeToken(String name)
        {
            return GetTypeToken(InternalModule.GetType(name, false, true));
        }

        public MethodToken GetMethodToken(MethodInfo method)
        {
            lock (SyncRoot)
            {
                return GetMethodTokenNoLock(method, true);
            }
        }

        internal MethodToken GetMethodTokenInternal(MethodInfo method)
        {
            lock (SyncRoot)
            {
                return GetMethodTokenNoLock(method, false);
            }
        }

        private MethodToken GetMethodTokenNoLock(MethodInfo method, bool getGenericTypeDefinition)
        {
            if (method == null)
                throw new ArgumentNullException("method");
            Contract.EndContractBlock();
            int tr;
            int mr = 0;
            SymbolMethod symMethod = null;
            MethodBuilder methBuilder = null;
            if ((methBuilder = method as MethodBuilder) != null)
            {
                int methodToken = methBuilder.MetadataTokenInternal;
                if (method.Module.Equals(this))
                    return new MethodToken(methodToken);
                if (method.DeclaringType == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
                tr = getGenericTypeDefinition ? GetTypeToken(method.DeclaringType).Token : GetTypeTokenInternal(method.DeclaringType).Token;
                mr = GetMemberRef(method.DeclaringType.Module, tr, methodToken);
            }
            else if (method is MethodOnTypeBuilderInstantiation)
            {
                return new MethodToken(GetMemberRefToken(method, null));
            }
            else if ((symMethod = method as SymbolMethod) != null)
            {
                if (symMethod.GetModule() == this)
                    return symMethod.GetToken();
                return symMethod.GetToken(this);
            }
            else
            {
                Type declaringType = method.DeclaringType;
                if (declaringType == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
                RuntimeMethodInfo rtMeth = null;
                if (declaringType.IsArray == true)
                {
                    ParameterInfo[] paramInfo = method.GetParameters();
                    Type[] tt = new Type[paramInfo.Length];
                    for (int i = 0; i < paramInfo.Length; i++)
                        tt[i] = paramInfo[i].ParameterType;
                    return GetArrayMethodToken(declaringType, method.Name, method.CallingConvention, method.ReturnType, tt);
                }
                else if ((rtMeth = method as RuntimeMethodInfo) != null)
                {
                    tr = getGenericTypeDefinition ? GetTypeToken(method.DeclaringType).Token : GetTypeTokenInternal(method.DeclaringType).Token;
                    mr = GetMemberRefOfMethodInfo(tr, rtMeth);
                }
                else
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    Type[] parameterTypes = new Type[parameters.Length];
                    Type[][] requiredCustomModifiers = new Type[parameterTypes.Length][];
                    Type[][] optionalCustomModifiers = new Type[parameterTypes.Length][];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        parameterTypes[i] = parameters[i].ParameterType;
                        requiredCustomModifiers[i] = parameters[i].GetRequiredCustomModifiers();
                        optionalCustomModifiers[i] = parameters[i].GetOptionalCustomModifiers();
                    }

                    tr = getGenericTypeDefinition ? GetTypeToken(method.DeclaringType).Token : GetTypeTokenInternal(method.DeclaringType).Token;
                    SignatureHelper sigHelp;
                    try
                    {
                        sigHelp = SignatureHelper.GetMethodSigHelper(this, method.CallingConvention, method.ReturnType, method.ReturnParameter.GetRequiredCustomModifiers(), method.ReturnParameter.GetOptionalCustomModifiers(), parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
                    }
                    catch (NotImplementedException)
                    {
                        sigHelp = SignatureHelper.GetMethodSigHelper(this, method.ReturnType, parameterTypes);
                    }

                    int length;
                    byte[] sigBytes = sigHelp.InternalGetSignature(out length);
                    mr = GetMemberRefFromSignature(tr, method.Name, sigBytes, length);
                }
            }

            return new MethodToken(mr);
        }

        public MethodToken GetConstructorToken(ConstructorInfo constructor, IEnumerable<Type> optionalParameterTypes)
        {
            if (constructor == null)
            {
                throw new ArgumentNullException("constructor");
            }

            lock (SyncRoot)
            {
                return new MethodToken(GetMethodTokenInternal(constructor, optionalParameterTypes, false));
            }
        }

        public MethodToken GetMethodToken(MethodInfo method, IEnumerable<Type> optionalParameterTypes)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            lock (SyncRoot)
            {
                return new MethodToken(GetMethodTokenInternal(method, optionalParameterTypes, true));
            }
        }

        internal int GetMethodTokenInternal(MethodBase method, IEnumerable<Type> optionalParameterTypes, bool useMethodDef)
        {
            int tk = 0;
            MethodInfo methodInfo = method as MethodInfo;
            if (method.IsGenericMethod)
            {
                Contract.Assert(methodInfo != null);
                MethodInfo methodInfoUnbound = methodInfo;
                bool isGenericMethodDef = methodInfo.IsGenericMethodDefinition;
                if (!isGenericMethodDef)
                {
                    methodInfoUnbound = methodInfo.GetGenericMethodDefinition();
                }

                if (!this.Equals(methodInfoUnbound.Module) || (methodInfoUnbound.DeclaringType != null && methodInfoUnbound.DeclaringType.IsGenericType))
                {
                    tk = GetMemberRefToken(methodInfoUnbound, null);
                }
                else
                {
                    tk = GetMethodTokenInternal(methodInfoUnbound).Token;
                }

                if (isGenericMethodDef && useMethodDef)
                {
                    return tk;
                }

                int sigLength;
                byte[] sigBytes = SignatureHelper.GetMethodSpecSigHelper(this, methodInfo.GetGenericArguments()).InternalGetSignature(out sigLength);
                tk = TypeBuilder.DefineMethodSpec(this.GetNativeHandle(), tk, sigBytes, sigLength);
            }
            else
            {
                if (((method.CallingConvention & CallingConventions.VarArgs) == 0) && (method.DeclaringType == null || !method.DeclaringType.IsGenericType))
                {
                    if (methodInfo != null)
                    {
                        tk = GetMethodTokenInternal(methodInfo).Token;
                    }
                    else
                    {
                        tk = GetConstructorToken(method as ConstructorInfo).Token;
                    }
                }
                else
                {
                    tk = GetMemberRefToken(method, optionalParameterTypes);
                }
            }

            return tk;
        }

        public MethodToken GetArrayMethodToken(Type arrayClass, String methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            lock (SyncRoot)
            {
                return GetArrayMethodTokenNoLock(arrayClass, methodName, callingConvention, returnType, parameterTypes);
            }
        }

        private MethodToken GetArrayMethodTokenNoLock(Type arrayClass, String methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            if (arrayClass == null)
                throw new ArgumentNullException("arrayClass");
            if (methodName == null)
                throw new ArgumentNullException("methodName");
            if (methodName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "methodName");
            if (arrayClass.IsArray == false)
                throw new ArgumentException(Environment.GetResourceString("Argument_HasToBeArrayClass"));
            Contract.EndContractBlock();
            CheckContext(returnType, arrayClass);
            CheckContext(parameterTypes);
            int length;
            SignatureHelper sigHelp = SignatureHelper.GetMethodSigHelper(this, callingConvention, returnType, null, null, parameterTypes, null, null);
            byte[] sigBytes = sigHelp.InternalGetSignature(out length);
            TypeToken typeSpec = GetTypeTokenInternal(arrayClass);
            return new MethodToken(GetArrayMethodToken(GetNativeHandle(), typeSpec.Token, methodName, sigBytes, length));
        }

        public MethodInfo GetArrayMethod(Type arrayClass, String methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            CheckContext(returnType, arrayClass);
            CheckContext(parameterTypes);
            MethodToken token = GetArrayMethodToken(arrayClass, methodName, callingConvention, returnType, parameterTypes);
            return new SymbolMethod(this, token, arrayClass, methodName, callingConvention, returnType, parameterTypes);
        }

        public MethodToken GetConstructorToken(ConstructorInfo con)
        {
            return InternalGetConstructorToken(con, false);
        }

        public FieldToken GetFieldToken(FieldInfo field)
        {
            lock (SyncRoot)
            {
                return GetFieldTokenNoLock(field);
            }
        }

        private FieldToken GetFieldTokenNoLock(FieldInfo field)
        {
            if (field == null)
            {
                throw new ArgumentNullException("con");
            }

            Contract.EndContractBlock();
            int tr;
            int mr = 0;
            FieldBuilder fdBuilder = null;
            RuntimeFieldInfo rtField = null;
            FieldOnTypeBuilderInstantiation fOnTB = null;
            if ((fdBuilder = field as FieldBuilder) != null)
            {
                if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
                {
                    int length;
                    byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
                    tr = GetTokenFromTypeSpec(sig, length);
                    mr = GetMemberRef(this, tr, fdBuilder.GetToken().Token);
                }
                else if (fdBuilder.Module.Equals(this))
                {
                    return fdBuilder.GetToken();
                }
                else
                {
                    if (field.DeclaringType == null)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
                    }

                    tr = GetTypeTokenInternal(field.DeclaringType).Token;
                    mr = GetMemberRef(field.ReflectedType.Module, tr, fdBuilder.GetToken().Token);
                }
            }
            else if ((rtField = field as RuntimeFieldInfo) != null)
            {
                if (field.DeclaringType == null)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotImportGlobalFromDifferentModule"));
                }

                if (field.DeclaringType != null && field.DeclaringType.IsGenericType)
                {
                    int length;
                    byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
                    tr = GetTokenFromTypeSpec(sig, length);
                    mr = GetMemberRefOfFieldInfo(tr, field.DeclaringType.GetTypeHandleInternal(), rtField);
                }
                else
                {
                    tr = GetTypeTokenInternal(field.DeclaringType).Token;
                    mr = GetMemberRefOfFieldInfo(tr, field.DeclaringType.GetTypeHandleInternal(), rtField);
                }
            }
            else if ((fOnTB = field as FieldOnTypeBuilderInstantiation) != null)
            {
                FieldInfo fb = fOnTB.FieldInfo;
                int length;
                byte[] sig = SignatureHelper.GetTypeSigToken(this, field.DeclaringType).InternalGetSignature(out length);
                tr = GetTokenFromTypeSpec(sig, length);
                mr = GetMemberRef(fb.ReflectedType.Module, tr, fOnTB.MetadataTokenInternal);
            }
            else
            {
                tr = GetTypeTokenInternal(field.ReflectedType).Token;
                SignatureHelper sigHelp = SignatureHelper.GetFieldSigHelper(this);
                sigHelp.AddArgument(field.FieldType, field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers());
                int length;
                byte[] sigBytes = sigHelp.InternalGetSignature(out length);
                mr = GetMemberRefFromSignature(tr, field.Name, sigBytes, length);
            }

            return new FieldToken(mr, field.GetType());
        }

        public StringToken GetStringConstant(String str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

            Contract.EndContractBlock();
            return new StringToken(GetStringConstant(GetNativeHandle(), str, str.Length));
        }

        public SignatureToken GetSignatureToken(SignatureHelper sigHelper)
        {
            if (sigHelper == null)
            {
                throw new ArgumentNullException("sigHelper");
            }

            Contract.EndContractBlock();
            int sigLength;
            byte[] sigBytes;
            sigBytes = sigHelper.InternalGetSignature(out sigLength);
            return new SignatureToken(TypeBuilder.GetTokenFromSig(GetNativeHandle(), sigBytes, sigLength), this);
        }

        public SignatureToken GetSignatureToken(byte[] sigBytes, int sigLength)
        {
            if (sigBytes == null)
                throw new ArgumentNullException("sigBytes");
            Contract.EndContractBlock();
            byte[] localSigBytes = new byte[sigBytes.Length];
            Array.Copy(sigBytes, localSigBytes, sigBytes.Length);
            return new SignatureToken(TypeBuilder.GetTokenFromSig(GetNativeHandle(), localSigBytes, sigLength), this);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();
            TypeBuilder.DefineCustomAttribute(this, 1, this.GetConstructorToken(con).Token, binaryAttribute, false, false);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException("customBuilder");
            }

            Contract.EndContractBlock();
            customBuilder.CreateCustomAttribute(this, 1);
        }

        public ISymbolWriter GetSymWriter()
        {
            return m_iSymWriter;
        }

        public ISymbolDocumentWriter DefineDocument(String url, Guid language, Guid languageVendor, Guid documentType)
        {
            if (url == null)
                throw new ArgumentNullException("url");
            Contract.EndContractBlock();
            lock (SyncRoot)
            {
                return DefineDocumentNoLock(url, language, languageVendor, documentType);
            }
        }

        private ISymbolDocumentWriter DefineDocumentNoLock(String url, Guid language, Guid languageVendor, Guid documentType)
        {
            if (m_iSymWriter == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }

            return m_iSymWriter.DefineDocument(url, language, languageVendor, documentType);
        }

        public void SetUserEntryPoint(MethodInfo entryPoint)
        {
            lock (SyncRoot)
            {
                SetUserEntryPointNoLock(entryPoint);
            }
        }

        private void SetUserEntryPointNoLock(MethodInfo entryPoint)
        {
            if (entryPoint == null)
            {
                throw new ArgumentNullException("entryPoint");
            }

            Contract.EndContractBlock();
            if (m_iSymWriter == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }

            if (entryPoint.DeclaringType != null)
            {
                if (!entryPoint.Module.Equals(this))
                {
                    throw new InvalidOperationException(Environment.GetResourceString("Argument_NotInTheSameModuleBuilder"));
                }
            }
            else
            {
                MethodBuilder mb = entryPoint as MethodBuilder;
                if (mb != null && mb.GetModuleBuilder() != this)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("Argument_NotInTheSameModuleBuilder"));
                }
            }

            SymbolToken tkMethod = new SymbolToken(GetMethodTokenInternal(entryPoint).Token);
            m_iSymWriter.SetUserEntryPoint(tkMethod);
        }

        public void SetSymCustomAttribute(String name, byte[] data)
        {
            lock (SyncRoot)
            {
                SetSymCustomAttributeNoLock(name, data);
            }
        }

        private void SetSymCustomAttributeNoLock(String name, byte[] data)
        {
            if (m_iSymWriter == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }
        }

        public bool IsTransient()
        {
            return InternalModule.IsTransientInternal();
        }
    }
}