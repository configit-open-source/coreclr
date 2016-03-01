namespace System.Reflection
{
    using System;
    using System.Diagnostics.SymbolStore;
    using System.Runtime.Remoting;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Security.Permissions;
    using System.IO;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [Flags]
    public enum PortableExecutableKinds
    {
        NotAPortableExecutableImage = 0x0,
        ILOnly = 0x1,
        Required32Bit = 0x2,
        PE32Plus = 0x4,
        Unmanaged32Bit = 0x8,
        Preferred32Bit = 0x10
    }

    public enum ImageFileMachine
    {
        I386 = 0x014c,
        IA64 = 0x0200,
        AMD64 = 0x8664,
        ARM = 0x01c4
    }

    public abstract class Module : _Module, ISerializable, ICustomAttributeProvider
    {
        static Module()
        {
            __Filters _fltObj;
            _fltObj = new __Filters();
            FilterTypeName = new TypeFilter(_fltObj.FilterTypeName);
            FilterTypeNameIgnoreCase = new TypeFilter(_fltObj.FilterTypeNameIgnoreCase);
        }

        protected Module()
        {
        }

        public static readonly TypeFilter FilterTypeName;
        public static readonly TypeFilter FilterTypeNameIgnoreCase;
        public override bool Equals(object o)
        {
            return base.Equals(o);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        public override String ToString()
        {
            return ScopeName;
        }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GetCustomAttributesData();
            }
        }

        public virtual Object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public virtual IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public MethodBase ResolveMethod(int metadataToken)
        {
            return ResolveMethod(metadataToken, null, null);
        }

        public virtual MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
            throw new NotImplementedException();
        }

        public FieldInfo ResolveField(int metadataToken)
        {
            return ResolveField(metadataToken, null, null);
        }

        public virtual FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
            throw new NotImplementedException();
        }

        public Type ResolveType(int metadataToken)
        {
            return ResolveType(metadataToken, null, null);
        }

        public virtual Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
            throw new NotImplementedException();
        }

        public MemberInfo ResolveMember(int metadataToken)
        {
            return ResolveMember(metadataToken, null, null);
        }

        public virtual MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveMember(metadataToken, genericTypeArguments, genericMethodArguments);
            throw new NotImplementedException();
        }

        public virtual byte[] ResolveSignature(int metadataToken)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveSignature(metadataToken);
            throw new NotImplementedException();
        }

        public virtual string ResolveString(int metadataToken)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.ResolveString(metadataToken);
            throw new NotImplementedException();
        }

        public virtual void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                rtModule.GetPEKind(out peKind, out machine);
            throw new NotImplementedException();
        }

        public virtual int MDStreamVersion
        {
            get
            {
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.MDStreamVersion;
                throw new NotImplementedException();
            }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public virtual Type GetType(String className, bool ignoreCase)
        {
            return GetType(className, false, ignoreCase);
        }

        public virtual Type GetType(String className)
        {
            return GetType(className, false, false);
        }

        public virtual Type GetType(String className, bool throwOnError, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public virtual String FullyQualifiedName
        {
            [System.Security.SecurityCritical]
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Type[] FindTypes(TypeFilter filter, Object filterCriteria)
        {
            Type[] c = GetTypes();
            int cnt = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (filter != null && !filter(c[i], filterCriteria))
                    c[i] = null;
                else
                    cnt++;
            }

            if (cnt == c.Length)
                return c;
            Type[] ret = new Type[cnt];
            cnt = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] != null)
                    ret[cnt++] = c[i];
            }

            return ret;
        }

        public virtual Type[] GetTypes()
        {
            throw new NotImplementedException();
        }

        public virtual Guid ModuleVersionId
        {
            get
            {
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.ModuleVersionId;
                throw new NotImplementedException();
            }
        }

        public virtual int MetadataToken
        {
            get
            {
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.MetadataToken;
                throw new NotImplementedException();
            }
        }

        public virtual bool IsResource()
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.IsResource();
            throw new NotImplementedException();
        }

        public FieldInfo[] GetFields()
        {
            return GetFields(Module.DefaultLookup);
        }

        public virtual FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.GetFields(bindingFlags);
            throw new NotImplementedException();
        }

        public FieldInfo GetField(String name)
        {
            return GetField(name, Module.DefaultLookup);
        }

        public virtual FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.GetField(name, bindingAttr);
            throw new NotImplementedException();
        }

        public MethodInfo[] GetMethods()
        {
            return GetMethods(Module.DefaultLookup);
        }

        public virtual MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            RuntimeModule rtModule = this as RuntimeModule;
            if (rtModule != null)
                return rtModule.GetMethods(bindingFlags);
            throw new NotImplementedException();
        }

        public MethodInfo GetMethod(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            }

            return GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public MethodInfo GetMethod(String name, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
            Contract.EndContractBlock();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            }

            return GetMethodImpl(name, Module.DefaultLookup, null, CallingConventions.Any, types, null);
        }

        public MethodInfo GetMethod(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            return GetMethodImpl(name, Module.DefaultLookup, null, CallingConventions.Any, null, null);
        }

        protected virtual MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public virtual String ScopeName
        {
            get
            {
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.ScopeName;
                throw new NotImplementedException();
            }
        }

        public virtual String Name
        {
            get
            {
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.Name;
                throw new NotImplementedException();
            }
        }

        public virtual Assembly Assembly
        {
            [Pure]
            get
            {
                RuntimeModule rtModule = this as RuntimeModule;
                if (rtModule != null)
                    return rtModule.Assembly;
                throw new NotImplementedException();
            }
        }

        public ModuleHandle ModuleHandle
        {
            get
            {
                return GetModuleHandle();
            }
        }

        internal virtual ModuleHandle GetModuleHandle()
        {
            return ModuleHandle.EmptyHandle;
        }
    }

    internal class RuntimeModule : Module
    {
        internal RuntimeModule()
        {
            throw new NotSupportedException();
        }

        private extern static void GetType(RuntimeModule module, String className, bool ignoreCase, bool throwOnError, ObjectHandleOnStack type);
        private static extern bool nIsTransientInternal(RuntimeModule module);
        private extern static void GetScopeName(RuntimeModule module, StringHandleOnStack retString);
        private extern static void GetFullyQualifiedName(RuntimeModule module, StringHandleOnStack retString);
        private extern static RuntimeType[] GetTypes(RuntimeModule module);
        internal RuntimeType[] GetDefinedTypes()
        {
            return GetTypes(GetNativeHandle());
        }

        private extern static bool IsResource(RuntimeModule module);
        private static RuntimeTypeHandle[] ConvertToTypeHandleArray(Type[] genericArguments)
        {
            if (genericArguments == null)
                return null;
            int size = genericArguments.Length;
            RuntimeTypeHandle[] typeHandleArgs = new RuntimeTypeHandle[size];
            for (int i = 0; i < size; i++)
            {
                Type typeArg = genericArguments[i];
                if (typeArg == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray"));
                typeArg = typeArg.UnderlyingSystemType;
                if (typeArg == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray"));
                if (!(typeArg is RuntimeType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGenericInstArray"));
                typeHandleArgs[i] = typeArg.GetTypeHandleInternal();
            }

            return typeHandleArgs;
        }

        public override byte[] ResolveSignature(int metadataToken)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", tk, this));
            if (!tk.IsMemberRef && !tk.IsMethodDef && !tk.IsTypeSpec && !tk.IsSignature && !tk.IsFieldDef)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidToken", tk, this), "metadataToken");
            ConstArray signature;
            if (tk.IsMemberRef)
                signature = MetadataImport.GetMemberRefProps(metadataToken);
            else
                signature = MetadataImport.GetSignatureFromToken(metadataToken);
            byte[] sig = new byte[signature.Length];
            for (int i = 0; i < signature.Length; i++)
                sig[i] = signature[i];
            return sig;
        }

        public override MethodBase ResolveMethod(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", tk, this));
            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
            try
            {
                if (!tk.IsMethodDef && !tk.IsMethodSpec)
                {
                    if (!tk.IsMemberRef)
                        throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveMethod", tk, this));
                    unsafe
                    {
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk);
                        if (*(MdSigCallingConvention*)sig.Signature.ToPointer() == MdSigCallingConvention.Field)
                            throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveMethod", tk, this));
                    }
                }

                IRuntimeMethodInfo methodHandle = ModuleHandle.ResolveMethodHandleInternal(GetNativeHandle(), tk, typeArgs, methodArgs);
                Type declaringType = RuntimeMethodHandle.GetDeclaringType(methodHandle);
                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    MetadataToken tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tk));
                    if (tk.IsMethodSpec)
                        tkDeclaringType = new MetadataToken(MetadataImport.GetParentToken(tkDeclaringType));
                    declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
                }

                return System.RuntimeType.GetMethodBase(declaringType as RuntimeType, methodHandle);
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e);
            }
        }

        private FieldInfo ResolveLiteralField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
            if (!MetadataImport.IsValidToken(tk) || !tk.IsFieldDef)
                throw new ArgumentOutOfRangeException("metadataToken", String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)));
            int tkDeclaringType;
            string fieldName;
            fieldName = MetadataImport.GetName(tk).ToString();
            tkDeclaringType = MetadataImport.GetParentToken(tk);
            Type declaringType = ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
            declaringType.GetFields();
            try
            {
                return declaringType.GetField(fieldName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            }
            catch
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ResolveField", tk, this), "metadataToken");
            }
        }

        public override FieldInfo ResolveField(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", tk, this));
            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
            try
            {
                IRuntimeFieldInfo fieldHandle = null;
                if (!tk.IsFieldDef)
                {
                    if (!tk.IsMemberRef)
                        throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveField", tk, this));
                    unsafe
                    {
                        ConstArray sig = MetadataImport.GetMemberRefProps(tk);
                        if (*(MdSigCallingConvention*)sig.Signature.ToPointer() != MdSigCallingConvention.Field)
                            throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveField", tk, this));
                    }

                    fieldHandle = ModuleHandle.ResolveFieldHandleInternal(GetNativeHandle(), tk, typeArgs, methodArgs);
                }

                fieldHandle = ModuleHandle.ResolveFieldHandleInternal(GetNativeHandle(), metadataToken, typeArgs, methodArgs);
                RuntimeType declaringType = RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle.Value);
                if (declaringType.IsGenericType || declaringType.IsArray)
                {
                    int tkDeclaringType = ModuleHandle.GetMetadataImport(GetNativeHandle()).GetParentToken(metadataToken);
                    declaringType = (RuntimeType)ResolveType(tkDeclaringType, genericTypeArguments, genericMethodArguments);
                }

                return System.RuntimeType.GetFieldInfo(declaringType, fieldHandle);
            }
            catch (MissingFieldException)
            {
                return ResolveLiteralField(tk, genericTypeArguments, genericMethodArguments);
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e);
            }
        }

        public override Type ResolveType(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
            if (tk.IsGlobalTypeDefToken)
                throw new ArgumentException(Environment.GetResourceString("Argument_ResolveModuleType", tk), "metadataToken");
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", tk, this));
            if (!tk.IsTypeDef && !tk.IsTypeSpec && !tk.IsTypeRef)
                throw new ArgumentException(Environment.GetResourceString("Argument_ResolveType", tk, this), "metadataToken");
            RuntimeTypeHandle[] typeArgs = ConvertToTypeHandleArray(genericTypeArguments);
            RuntimeTypeHandle[] methodArgs = ConvertToTypeHandleArray(genericMethodArguments);
            try
            {
                Type t = GetModuleHandle().ResolveTypeHandle(metadataToken, typeArgs, methodArgs).GetRuntimeType();
                if (t == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_ResolveType", tk, this), "metadataToken");
                return t;
            }
            catch (BadImageFormatException e)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadImageFormatExceptionResolve"), e);
            }
        }

        public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
            if (tk.IsProperty)
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_PropertyInfoNotAvailable"));
            if (tk.IsEvent)
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_EventInfoNotAvailable"));
            if (tk.IsMethodSpec || tk.IsMethodDef)
                return ResolveMethod(metadataToken, genericTypeArguments, genericMethodArguments);
            if (tk.IsFieldDef)
                return ResolveField(metadataToken, genericTypeArguments, genericMethodArguments);
            if (tk.IsTypeRef || tk.IsTypeDef || tk.IsTypeSpec)
                return ResolveType(metadataToken, genericTypeArguments, genericMethodArguments);
            if (tk.IsMemberRef)
            {
                if (!MetadataImport.IsValidToken(tk))
                    throw new ArgumentOutOfRangeException("metadataToken", Environment.GetResourceString("Argument_InvalidToken", tk, this));
                ConstArray sig = MetadataImport.GetMemberRefProps(tk);
                unsafe
                {
                    if (*(MdSigCallingConvention*)sig.Signature.ToPointer() == MdSigCallingConvention.Field)
                    {
                        return ResolveField(tk, genericTypeArguments, genericMethodArguments);
                    }
                    else
                    {
                        return ResolveMethod(tk, genericTypeArguments, genericMethodArguments);
                    }
                }
            }

            throw new ArgumentException("metadataToken", Environment.GetResourceString("Argument_ResolveMember", tk, this));
        }

        public override string ResolveString(int metadataToken)
        {
            MetadataToken tk = new MetadataToken(metadataToken);
            if (!tk.IsString)
                throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_ResolveString"), metadataToken, ToString()));
            if (!MetadataImport.IsValidToken(tk))
                throw new ArgumentOutOfRangeException("metadataToken", String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_InvalidToken", tk, this)));
            string str = MetadataImport.GetUserString(metadataToken);
            if (str == null)
                throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Argument_ResolveString"), metadataToken, ToString()));
            return str;
        }

        public override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            ModuleHandle.GetPEKind(GetNativeHandle(), out peKind, out machine);
        }

        public override int MDStreamVersion
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return ModuleHandle.GetMDStreamVersion(GetNativeHandle());
            }
        }

        private RuntimeType m_runtimeType;
        private RuntimeAssembly m_runtimeAssembly;
        private IntPtr m_pRefClass;
        private IntPtr m_pData;
        private IntPtr m_pGlobals;
        private IntPtr m_pFields;
        protected override MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return GetMethodInternal(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        internal MethodInfo GetMethodInternal(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (RuntimeType == null)
                return null;
            if (types == null)
            {
                return RuntimeType.GetMethod(name, bindingAttr);
            }
            else
            {
                return RuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            }
        }

        internal RuntimeType RuntimeType
        {
            get
            {
                if (m_runtimeType == null)
                    m_runtimeType = ModuleHandle.GetModuleType(GetNativeHandle());
                return m_runtimeType;
            }
        }

        internal bool IsTransientInternal()
        {
            return RuntimeModule.nIsTransientInternal(this.GetNativeHandle());
        }

        internal MetadataImport MetadataImport
        {
            [System.Security.SecurityCritical]
            get
            {
                unsafe
                {
                    return ModuleHandle.GetMetadataImport(GetNativeHandle());
                }
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof (object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Contract.EndContractBlock();
            UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.ModuleUnity, this.ScopeName, this.GetRuntimeAssembly());
        }

        public override Type GetType(String className, bool throwOnError, bool ignoreCase)
        {
            if (className == null)
                throw new ArgumentNullException("className");
            RuntimeType retType = null;
            GetType(GetNativeHandle(), className, throwOnError, ignoreCase, JitHelpers.GetObjectHandleOnStack(ref retType));
            return retType;
        }

        internal string GetFullyQualifiedName()
        {
            String fullyQualifiedName = null;
            GetFullyQualifiedName(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref fullyQualifiedName));
            return fullyQualifiedName;
        }

        public override String FullyQualifiedName
        {
            [System.Security.SecurityCritical]
            get
            {
                String fullyQualifiedName = GetFullyQualifiedName();
                if (fullyQualifiedName != null)
                {
                    bool checkPermission = true;
                    try
                    {
                        Path.GetFullPathInternal(fullyQualifiedName);
                    }
                    catch (ArgumentException)
                    {
                        checkPermission = false;
                    }

                    if (checkPermission)
                    {
                        new FileIOPermission(FileIOPermissionAccess.PathDiscovery, fullyQualifiedName).Demand();
                    }
                }

                return fullyQualifiedName;
            }
        }

        public override Type[] GetTypes()
        {
            return GetTypes(GetNativeHandle());
        }

        public override Guid ModuleVersionId
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                unsafe
                {
                    Guid mvid;
                    MetadataImport.GetScopeProps(out mvid);
                    return mvid;
                }
            }
        }

        public override int MetadataToken
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return ModuleHandle.GetToken(GetNativeHandle());
            }
        }

        public override bool IsResource()
        {
            return IsResource(GetNativeHandle());
        }

        public override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            if (RuntimeType == null)
                return new FieldInfo[0];
            return RuntimeType.GetFields(bindingFlags);
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (RuntimeType == null)
                return null;
            return RuntimeType.GetField(name, bindingAttr);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            if (RuntimeType == null)
                return new MethodInfo[0];
            return RuntimeType.GetMethods(bindingFlags);
        }

        public override String ScopeName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                string scopeName = null;
                GetScopeName(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref scopeName));
                return scopeName;
            }
        }

        public override String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String s = GetFullyQualifiedName();
                int i = s.LastIndexOf('\\');
                if (i == -1)
                    return s;
                return new String(s.ToCharArray(), i + 1, s.Length - i - 1);
            }
        }

        public override Assembly Assembly
        {
            [Pure]
            get
            {
                return GetRuntimeAssembly();
            }
        }

        internal RuntimeAssembly GetRuntimeAssembly()
        {
            return m_runtimeAssembly;
        }

        internal override ModuleHandle GetModuleHandle()
        {
            return new ModuleHandle(this);
        }

        internal RuntimeModule GetNativeHandle()
        {
            return this;
        }
    }
}