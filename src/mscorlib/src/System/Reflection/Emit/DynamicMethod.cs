using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Reflection.Emit
{
    public sealed class DynamicMethod : MethodInfo
    {
        private RuntimeType[] m_parameterTypes;
        internal IRuntimeMethodInfo m_methodHandle;
        private RuntimeType m_returnType;
        private DynamicILGenerator m_ilGenerator;
        private DynamicILInfo m_DynamicILInfo;
        private bool m_fInitLocals;
        private RuntimeModule m_module;
        internal bool m_skipVisibility;
        internal RuntimeType m_typeOwner;
        private RTDynamicMethod m_dynMethod;
        internal DynamicResolver m_resolver;
        private bool m_profileAPICheck;
        private RuntimeAssembly m_creatorAssembly;
        internal bool m_restrictedSkipVisibility;
        private static volatile InternalModuleBuilder s_anonymouslyHostedDynamicMethodsModule;
        private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object ();
        private DynamicMethod()
        {
        }

        public DynamicMethod(string name, Type returnType, Type[] parameterTypes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, false, true, ref stackMark);
        }

        public DynamicMethod(string name, Type returnType, Type[] parameterTypes, bool restrictedSkipVisibility)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, restrictedSkipVisibility, true, ref stackMark);
        }

        public DynamicMethod(string name, Type returnType, Type[] parameterTypes, Module m)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(m, ref stackMark, false);
            Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, m, false, false, ref stackMark);
        }

        public DynamicMethod(string name, Type returnType, Type[] parameterTypes, Module m, bool skipVisibility)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(m, ref stackMark, skipVisibility);
            Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, m, skipVisibility, false, ref stackMark);
        }

        public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Module m, bool skipVisibility)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(m, ref stackMark, skipVisibility);
            Init(name, attributes, callingConvention, returnType, parameterTypes, null, m, skipVisibility, false, ref stackMark);
        }

        public DynamicMethod(string name, Type returnType, Type[] parameterTypes, Type owner)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(owner, ref stackMark, false);
            Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, null, false, false, ref stackMark);
        }

        public DynamicMethod(string name, Type returnType, Type[] parameterTypes, Type owner, bool skipVisibility)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(owner, ref stackMark, skipVisibility);
            Init(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, null, skipVisibility, false, ref stackMark);
        }

        public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type owner, bool skipVisibility)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(owner, ref stackMark, skipVisibility);
            Init(name, attributes, callingConvention, returnType, parameterTypes, owner, null, skipVisibility, false, ref stackMark);
        }

        static private void CheckConsistency(MethodAttributes attributes, CallingConventions callingConvention)
        {
            if ((attributes & ~MethodAttributes.MemberAccessMask) != MethodAttributes.Static)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
            if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
            Contract.EndContractBlock();
            if (callingConvention != CallingConventions.Standard && callingConvention != CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
            if (callingConvention == CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
        }

        private static RuntimeModule GetDynamicMethodsModule()
        {
            if (s_anonymouslyHostedDynamicMethodsModule != null)
                return s_anonymouslyHostedDynamicMethodsModule;
            lock (s_anonymouslyHostedDynamicMethodsModuleLock)
            {
                if (s_anonymouslyHostedDynamicMethodsModule != null)
                    return s_anonymouslyHostedDynamicMethodsModule;
                ConstructorInfo transparencyCtor = typeof (SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes);
                CustomAttributeBuilder transparencyAttribute = new CustomAttributeBuilder(transparencyCtor, EmptyArray<Object>.Value);
                List<CustomAttributeBuilder> assemblyAttributes = new List<CustomAttributeBuilder>();
                assemblyAttributes.Add(transparencyAttribute);
                AssemblyName assemblyName = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");
                StackCrawlMark stackMark = StackCrawlMark.LookForMe;
                AssemblyBuilder assembly = AssemblyBuilder.InternalDefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run, null, null, null, null, null, ref stackMark, assemblyAttributes, SecurityContextSource.CurrentAssembly);
                AppDomain.PublishAnonymouslyHostedDynamicMethodsAssembly(assembly.GetNativeHandle());
                s_anonymouslyHostedDynamicMethodsModule = (InternalModuleBuilder)assembly.ManifestModule;
            }

            return s_anonymouslyHostedDynamicMethodsModule;
        }

        private unsafe void Init(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] signature, Type owner, Module m, bool skipVisibility, bool transparentMethod, ref StackCrawlMark stackMark)
        {
            DynamicMethod.CheckConsistency(attributes, callingConvention);
            if (signature != null)
            {
                m_parameterTypes = new RuntimeType[signature.Length];
                for (int i = 0; i < signature.Length; i++)
                {
                    if (signature[i] == null)
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature"));
                    m_parameterTypes[i] = signature[i].UnderlyingSystemType as RuntimeType;
                    if (m_parameterTypes[i] == null || !(m_parameterTypes[i] is RuntimeType) || m_parameterTypes[i] == (RuntimeType)typeof (void))
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature"));
                }
            }
            else
            {
                m_parameterTypes = Array.Empty<RuntimeType>();
            }

            m_returnType = (returnType == null) ? (RuntimeType)typeof (void) : returnType.UnderlyingSystemType as RuntimeType;
            if ((m_returnType == null) || !(m_returnType is RuntimeType) || m_returnType.IsByRef)
                throw new NotSupportedException(Environment.GetResourceString("Arg_InvalidTypeInRetType"));
            if (transparentMethod)
            {
                Contract.Assert(owner == null && m == null, "owner and m cannot be set for transparent methods");
                m_module = GetDynamicMethodsModule();
                if (skipVisibility)
                {
                    m_restrictedSkipVisibility = true;
                }
            }
            else
            {
                Contract.Assert(m != null || owner != null, "PerformSecurityCheck should ensure that either m or owner is set");
                Contract.Assert(m == null || !m.Equals(s_anonymouslyHostedDynamicMethodsModule), "The user cannot explicitly use this assembly");
                Contract.Assert(m == null || owner == null, "m and owner cannot both be set");
                if (m != null)
                    m_module = m.ModuleHandle.GetRuntimeModule();
                else
                {
                    RuntimeType rtOwner = null;
                    if (owner != null)
                        rtOwner = owner.UnderlyingSystemType as RuntimeType;
                    if (rtOwner != null)
                    {
                        if (rtOwner.HasElementType || rtOwner.ContainsGenericParameters || rtOwner.IsGenericParameter || rtOwner.IsInterface)
                            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeForDynamicMethod"));
                        m_typeOwner = rtOwner;
                        m_module = rtOwner.GetRuntimeModule();
                    }
                }

                m_skipVisibility = skipVisibility;
            }

            m_ilGenerator = null;
            m_fInitLocals = true;
            m_methodHandle = null;
            if (name == null)
                throw new ArgumentNullException("name");
            if (AppDomain.ProfileAPICheck)
            {
                if (m_creatorAssembly == null)
                    m_creatorAssembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (m_creatorAssembly != null && !m_creatorAssembly.IsFrameworkAssembly())
                    m_profileAPICheck = true;
            }

            m_dynMethod = new RTDynamicMethod(this, name, attributes, callingConvention);
        }

        private void PerformSecurityCheck(Module m, ref StackCrawlMark stackMark, bool skipVisibility)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            Contract.EndContractBlock();
            RuntimeModule rtModule;
            ModuleBuilder mb = m as ModuleBuilder;
            if (mb != null)
                rtModule = mb.InternalModule;
            else
                rtModule = m as RuntimeModule;
            if (rtModule == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeModule"), "m");
            }

            if (rtModule == s_anonymouslyHostedDynamicMethodsModule)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"), "m");
            if (skipVisibility)
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
            new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
        }

        private void PerformSecurityCheck(Type owner, ref StackCrawlMark stackMark, bool skipVisibility)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");
            RuntimeType rtOwner = owner as RuntimeType;
            if (rtOwner == null)
                rtOwner = owner.UnderlyingSystemType as RuntimeType;
            if (rtOwner == null)
                throw new ArgumentNullException("owner", Environment.GetResourceString("Argument_MustBeRuntimeType"));
            RuntimeType callingType = RuntimeMethodHandle.GetCallerType(ref stackMark);
            if (skipVisibility)
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
            else
            {
                if (callingType != rtOwner)
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
            }

            new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
        }

        public sealed override Delegate CreateDelegate(Type delegateType)
        {
            if (m_restrictedSkipVisibility)
            {
                GetMethodDescriptor();
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(m_methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, null, GetMethodDescriptor());
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }

        public sealed override Delegate CreateDelegate(Type delegateType, Object target)
        {
            if (m_restrictedSkipVisibility)
            {
                GetMethodDescriptor();
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(m_methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, GetMethodDescriptor());
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }

        internal bool ProfileAPICheck
        {
            get
            {
                return m_profileAPICheck;
            }

            [FriendAccessAllowed]
            set
            {
                m_profileAPICheck = value;
            }
        }

        internal unsafe RuntimeMethodHandle GetMethodDescriptor()
        {
            if (m_methodHandle == null)
            {
                lock (this)
                {
                    if (m_methodHandle == null)
                    {
                        if (m_DynamicILInfo != null)
                            m_DynamicILInfo.GetCallableMethod(m_module, this);
                        else
                        {
                            if (m_ilGenerator == null || m_ilGenerator.ILOffset == 0)
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadEmptyMethodBody", Name));
                            m_ilGenerator.GetCallableMethod(m_module, this);
                        }
                    }
                }
            }

            return new RuntimeMethodHandle(m_methodHandle);
        }

        public override String ToString()
        {
            return m_dynMethod.ToString();
        }

        public override String Name
        {
            get
            {
                return m_dynMethod.Name;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_dynMethod.DeclaringType;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_dynMethod.ReflectedType;
            }
        }

        public override Module Module
        {
            get
            {
                return m_dynMethod.Module;
            }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
            }
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return m_dynMethod.Attributes;
            }
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                return m_dynMethod.CallingConvention;
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }

        public override ParameterInfo[] GetParameters()
        {
            return m_dynMethod.GetParameters();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_dynMethod.GetMethodImplementationFlags();
        }

        public override bool IsSecurityCritical
        {
            [SecuritySafeCritical]
            get
            {
                if (m_methodHandle != null)
                {
                    return RuntimeMethodHandle.IsSecurityCritical(m_methodHandle);
                }
                else if (m_typeOwner != null)
                {
                    RuntimeAssembly assembly = m_typeOwner.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);
                    return assembly.IsAllSecurityCritical();
                }
                else
                {
                    RuntimeAssembly assembly = m_module.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);
                    return assembly.IsAllSecurityCritical();
                }
            }
        }

        public override bool IsSecuritySafeCritical
        {
            [SecuritySafeCritical]
            get
            {
                if (m_methodHandle != null)
                {
                    return RuntimeMethodHandle.IsSecuritySafeCritical(m_methodHandle);
                }
                else if (m_typeOwner != null)
                {
                    RuntimeAssembly assembly = m_typeOwner.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);
                    return assembly.IsAllPublicAreaSecuritySafeCritical();
                }
                else
                {
                    RuntimeAssembly assembly = m_module.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);
                    return assembly.IsAllSecuritySafeCritical();
                }
            }
        }

        public override bool IsSecurityTransparent
        {
            [SecuritySafeCritical]
            get
            {
                if (m_methodHandle != null)
                {
                    return RuntimeMethodHandle.IsSecurityTransparent(m_methodHandle);
                }
                else if (m_typeOwner != null)
                {
                    RuntimeAssembly assembly = m_typeOwner.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);
                    return !assembly.IsAllSecurityCritical();
                }
                else
                {
                    RuntimeAssembly assembly = m_module.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);
                    return !assembly.IsAllSecurityCritical();
                }
            }
        }

        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CallToVarArg"));
            Contract.EndContractBlock();
            RuntimeMethodHandle method = GetMethodDescriptor();
            Signature sig = new Signature(this.m_methodHandle, m_parameterTypes, m_returnType, CallingConvention);
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
            Object retValue = null;
            if (actualCount > 0)
            {
                Object[] arguments = CheckArguments(parameters, binder, invokeAttr, culture, sig);
                retValue = RuntimeMethodHandle.InvokeMethod(null, arguments, sig, false);
                for (int index = 0; index < arguments.Length; index++)
                    parameters[index] = arguments[index];
            }
            else
            {
                retValue = RuntimeMethodHandle.InvokeMethod(null, null, sig, false);
            }

            GC.KeepAlive(this);
            return retValue;
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_dynMethod.GetCustomAttributes(attributeType, inherit);
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return m_dynMethod.GetCustomAttributes(inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_dynMethod.IsDefined(attributeType, inherit);
        }

        public override Type ReturnType
        {
            get
            {
                return m_dynMethod.ReturnType;
            }
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                return m_dynMethod.ReturnParameter;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                return m_dynMethod.ReturnTypeCustomAttributes;
            }
        }

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, String parameterName)
        {
            if (position < 0 || position > m_parameterTypes.Length)
                throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_ParamSequence"));
            position--;
            if (position >= 0)
            {
                ParameterInfo[] parameters = m_dynMethod.LoadParameters();
                parameters[position].SetName(parameterName);
                parameters[position].SetAttributes(attributes);
            }

            return null;
        }

        public DynamicILInfo GetDynamicILInfo()
        {
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            if (m_DynamicILInfo != null)
                return m_DynamicILInfo;
            return GetDynamicILInfo(new DynamicScope());
        }

        internal DynamicILInfo GetDynamicILInfo(DynamicScope scope)
        {
            if (m_DynamicILInfo == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_DynamicILInfo = new DynamicILInfo(scope, this, methodSignature);
            }

            return m_DynamicILInfo;
        }

        public ILGenerator GetILGenerator()
        {
            return GetILGenerator(64);
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            if (m_ilGenerator == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_ilGenerator = new DynamicILGenerator(this, methodSignature, streamSize);
            }

            return m_ilGenerator;
        }

        public bool InitLocals
        {
            get
            {
                return m_fInitLocals;
            }

            set
            {
                m_fInitLocals = value;
            }
        }

        internal MethodInfo GetMethodInfo()
        {
            return m_dynMethod;
        }

        internal class RTDynamicMethod : MethodInfo
        {
            internal DynamicMethod m_owner;
            ParameterInfo[] m_parameters;
            String m_name;
            MethodAttributes m_attributes;
            CallingConventions m_callingConvention;
            private RTDynamicMethod()
            {
            }

            internal RTDynamicMethod(DynamicMethod owner, String name, MethodAttributes attributes, CallingConventions callingConvention)
            {
                m_owner = owner;
                m_name = name;
                m_attributes = attributes;
                m_callingConvention = callingConvention;
            }

            public override String ToString()
            {
                return ReturnType.FormatTypeName() + " " + FormatNameAndSig();
            }

            public override String Name
            {
                get
                {
                    return m_name;
                }
            }

            public override Type DeclaringType
            {
                get
                {
                    return null;
                }
            }

            public override Type ReflectedType
            {
                get
                {
                    return null;
                }
            }

            public override Module Module
            {
                get
                {
                    return m_owner.m_module;
                }
            }

            public override RuntimeMethodHandle MethodHandle
            {
                get
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod"));
                }
            }

            public override MethodAttributes Attributes
            {
                get
                {
                    return m_attributes;
                }
            }

            public override CallingConventions CallingConvention
            {
                get
                {
                    return m_callingConvention;
                }
            }

            public override MethodInfo GetBaseDefinition()
            {
                return this;
            }

            public override ParameterInfo[] GetParameters()
            {
                ParameterInfo[] privateParameters = LoadParameters();
                ParameterInfo[] parameters = new ParameterInfo[privateParameters.Length];
                Array.Copy(privateParameters, parameters, privateParameters.Length);
                return parameters;
            }

            public override MethodImplAttributes GetMethodImplementationFlags()
            {
                return MethodImplAttributes.IL | MethodImplAttributes.NoInlining;
            }

            public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "this");
            }

            public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                if (attributeType == null)
                    throw new ArgumentNullException("attributeType");
                Contract.EndContractBlock();
                if (attributeType.IsAssignableFrom(typeof (MethodImplAttribute)))
                    return new Object[]{new MethodImplAttribute(GetMethodImplementationFlags())};
                else
                    return EmptyArray<Object>.Value;
            }

            public override Object[] GetCustomAttributes(bool inherit)
            {
                return new Object[]{new MethodImplAttribute(GetMethodImplementationFlags())};
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                if (attributeType == null)
                    throw new ArgumentNullException("attributeType");
                Contract.EndContractBlock();
                if (attributeType.IsAssignableFrom(typeof (MethodImplAttribute)))
                    return true;
                else
                    return false;
            }

            public override bool IsSecurityCritical
            {
                get
                {
                    return m_owner.IsSecurityCritical;
                }
            }

            public override bool IsSecuritySafeCritical
            {
                get
                {
                    return m_owner.IsSecuritySafeCritical;
                }
            }

            public override bool IsSecurityTransparent
            {
                get
                {
                    return m_owner.IsSecurityTransparent;
                }
            }

            public override Type ReturnType
            {
                get
                {
                    return m_owner.m_returnType;
                }
            }

            public override ParameterInfo ReturnParameter
            {
                get
                {
                    return null;
                }
            }

            public override ICustomAttributeProvider ReturnTypeCustomAttributes
            {
                get
                {
                    return GetEmptyCAHolder();
                }
            }

            internal ParameterInfo[] LoadParameters()
            {
                if (m_parameters == null)
                {
                    Type[] parameterTypes = m_owner.m_parameterTypes;
                    ParameterInfo[] parameters = new ParameterInfo[parameterTypes.Length];
                    for (int i = 0; i < parameterTypes.Length; i++)
                        parameters[i] = new RuntimeParameterInfo(this, null, parameterTypes[i], i);
                    if (m_parameters == null)
                        m_parameters = parameters;
                }

                return m_parameters;
            }

            private ICustomAttributeProvider GetEmptyCAHolder()
            {
                return new EmptyCAHolder();
            }

            private class EmptyCAHolder : ICustomAttributeProvider
            {
                internal EmptyCAHolder()
                {
                }

                Object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit)
                {
                    return EmptyArray<Object>.Value;
                }

                Object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit)
                {
                    return EmptyArray<Object>.Value;
                }

                bool ICustomAttributeProvider.IsDefined(Type attributeType, bool inherit)
                {
                    return false;
                }
            }
        }
    }
}