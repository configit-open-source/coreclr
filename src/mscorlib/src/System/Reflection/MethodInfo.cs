namespace System.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using MemberListType = System.RuntimeType.MemberListType;
    using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;
    using System.Runtime.CompilerServices;

    public abstract class MethodInfo : MethodBase, _MethodInfo
    {
        protected MethodInfo()
        {
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override MemberTypes MemberType
        {
            get
            {
                return System.Reflection.MemberTypes.Method;
            }
        }

        public virtual Type ReturnType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual ParameterInfo ReturnParameter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public abstract ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get;
        }

        public abstract MethodInfo GetBaseDefinition();
        public override Type[] GetGenericArguments()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        public virtual MethodInfo GetGenericMethodDefinition()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        public virtual MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        public virtual Delegate CreateDelegate(Type delegateType)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        public virtual Delegate CreateDelegate(Type delegateType, Object target)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }
    }

    internal sealed class RuntimeMethodInfo : MethodInfo, ISerializable, IRuntimeMethodInfo
    {
        private IntPtr m_handle;
        private RuntimeTypeCache m_reflectedTypeCache;
        private string m_name;
        private string m_toString;
        private ParameterInfo[] m_parameters;
        private ParameterInfo m_returnParameter;
        private BindingFlags m_bindingFlags;
        private MethodAttributes m_methodAttributes;
        private Signature m_signature;
        private RuntimeType m_declaringType;
        private object m_keepalive;
        private INVOCATION_FLAGS m_invocationFlags;
        private bool IsNonW8PFrameworkAPI()
        {
            if (m_declaringType.IsArray && IsPublic && !IsStatic)
                return false;
            RuntimeAssembly rtAssembly = GetRuntimeAssembly();
            if (rtAssembly.IsFrameworkAssembly())
            {
                int ctorToken = rtAssembly.InvocableAttributeCtorToken;
                if (System.Reflection.MetadataToken.IsNullToken(ctorToken) || !CustomAttribute.IsAttributeDefined(GetRuntimeModule(), MetadataToken, ctorToken))
                    return true;
            }

            if (GetRuntimeType().IsNonW8PFrameworkAPI())
                return true;
            if (IsGenericMethod && !IsGenericMethodDefinition)
            {
                foreach (Type t in GetGenericArguments())
                {
                    if (((RuntimeType)t).IsNonW8PFrameworkAPI())
                        return true;
                }
            }

            return false;
        }

        internal override bool IsDynamicallyInvokable
        {
            get
            {
                return !AppDomain.ProfileAPICheck || !IsNonW8PFrameworkAPI();
            }
        }

        internal INVOCATION_FLAGS InvocationFlags
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    INVOCATION_FLAGS invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN;
                    Type declaringType = DeclaringType;
                    if (ContainsGenericParameters || ReturnType.IsByRef || (declaringType != null && declaringType.ContainsGenericParameters) || ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs) || ((Attributes & MethodAttributes.RequireSecObject) == MethodAttributes.RequireSecObject))
                    {
                        invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
                    }
                    else
                    {
                        invocationFlags = RuntimeMethodHandle.GetSecurityFlags(this);
                        if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) == 0)
                        {
                            if ((Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public || (declaringType != null && declaringType.NeedsReflectionSecurityCheck))
                            {
                                invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
                            }
                            else if (IsGenericMethod)
                            {
                                Type[] genericArguments = GetGenericArguments();
                                for (int i = 0; i < genericArguments.Length; i++)
                                {
                                    if (genericArguments[i].NeedsReflectionSecurityCheck)
                                    {
                                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (AppDomain.ProfileAPICheck && IsNonW8PFrameworkAPI())
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API;
                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }

        internal RuntimeMethodInfo(RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeTypeCache reflectedTypeCache, MethodAttributes methodAttributes, BindingFlags bindingFlags, object keepalive)
        {
            Contract.Ensures(!m_handle.IsNull());
            Contract.Assert(!handle.IsNullHandle());
            Contract.Assert(methodAttributes == RuntimeMethodHandle.GetAttributes(handle));
            m_bindingFlags = bindingFlags;
            m_declaringType = declaringType;
            m_keepalive = keepalive;
            m_handle = handle.Value;
            m_reflectedTypeCache = reflectedTypeCache;
            m_methodAttributes = methodAttributes;
        }

        RuntimeMethodHandleInternal IRuntimeMethodInfo.Value
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return new RuntimeMethodHandleInternal(m_handle);
            }
        }

        private RuntimeType ReflectedTypeInternal
        {
            get
            {
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        private ParameterInfo[] FetchNonReturnParameters()
        {
            if (m_parameters == null)
                m_parameters = RuntimeParameterInfo.GetParameters(this, this, Signature);
            return m_parameters;
        }

        private ParameterInfo FetchReturnParameter()
        {
            if (m_returnParameter == null)
                m_returnParameter = RuntimeParameterInfo.GetReturnParameter(this, this, Signature);
            return m_returnParameter;
        }

        internal override string FormatNameAndSig(bool serialization)
        {
            StringBuilder sbName = new StringBuilder(Name);
            TypeNameFormatFlags format = serialization ? TypeNameFormatFlags.FormatSerialization : TypeNameFormatFlags.FormatBasic;
            if (IsGenericMethod)
                sbName.Append(RuntimeMethodHandle.ConstructInstantiation(this, format));
            sbName.Append("(");
            sbName.Append(ConstructParameters(GetParameterTypes(), CallingConvention, serialization));
            sbName.Append(")");
            return sbName.ToString();
        }

        internal override bool CacheEquals(object o)
        {
            RuntimeMethodInfo m = o as RuntimeMethodInfo;
            if ((object)m == null)
                return false;
            return m.m_handle == m_handle;
        }

        internal Signature Signature
        {
            get
            {
                if (m_signature == null)
                    m_signature = new Signature(this, m_declaringType);
                return m_signature;
            }
        }

        internal BindingFlags BindingFlags
        {
            get
            {
                return m_bindingFlags;
            }
        }

        internal RuntimeMethodHandle GetMethodHandle()
        {
            return new RuntimeMethodHandle(this);
        }

        internal RuntimeMethodInfo GetParentDefinition()
        {
            if (!IsVirtual || m_declaringType.IsInterface)
                return null;
            RuntimeType parent = (RuntimeType)m_declaringType.BaseType;
            if (parent == null)
                return null;
            int slot = RuntimeMethodHandle.GetSlot(this);
            if (RuntimeTypeHandle.GetNumVirtuals(parent) <= slot)
                return null;
            return (RuntimeMethodInfo)RuntimeType.GetMethodBase(parent, RuntimeTypeHandle.GetMethodAt(parent, slot));
        }

        internal RuntimeType GetDeclaringTypeInternal()
        {
            return m_declaringType;
        }

        public override String ToString()
        {
            if (m_toString == null)
                m_toString = ReturnType.FormatTypeName() + " " + FormatNameAndSig();
            return m_toString;
        }

        public override int GetHashCode()
        {
            if (IsGenericMethod)
                return ValueType.GetHashCodeOfPtr(m_handle);
            else
                return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!IsGenericMethod)
                return obj == (object)this;
            RuntimeMethodInfo mi = obj as RuntimeMethodInfo;
            if (mi == null || !mi.IsGenericMethod)
                return false;
            IRuntimeMethodInfo handle1 = RuntimeMethodHandle.StripMethodInstantiation(this);
            IRuntimeMethodInfo handle2 = RuntimeMethodHandle.StripMethodInstantiation(mi);
            if (handle1.Value.Value != handle2.Value.Value)
                return false;
            Type[] lhs = GetGenericArguments();
            Type[] rhs = mi.GetGenericArguments();
            if (lhs.Length != rhs.Length)
                return false;
            for (int i = 0; i < lhs.Length; i++)
            {
                if (lhs[i] != rhs[i])
                    return false;
            }

            if (DeclaringType != mi.DeclaringType)
                return false;
            if (ReflectedType != mi.ReflectedType)
                return false;
            return true;
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof (object) as RuntimeType as RuntimeType, inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.IsDefined(this, attributeRuntimeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }

        public override String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_name == null)
                    m_name = RuntimeMethodHandle.GetName(this);
                return m_name;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                if (m_reflectedTypeCache.IsGlobal)
                    return null;
                return m_declaringType;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                if (m_reflectedTypeCache.IsGlobal)
                    return null;
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        public override MemberTypes MemberType
        {
            get
            {
                return MemberTypes.Method;
            }
        }

        public override int MetadataToken
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return RuntimeMethodHandle.GetMethodDef(this);
            }
        }

        public override Module Module
        {
            get
            {
                return GetRuntimeModule();
            }
        }

        internal RuntimeType GetRuntimeType()
        {
            return m_declaringType;
        }

        internal RuntimeModule GetRuntimeModule()
        {
            return m_declaringType.GetRuntimeModule();
        }

        internal RuntimeAssembly GetRuntimeAssembly()
        {
            return GetRuntimeModule().GetRuntimeAssembly();
        }

        public override bool IsSecurityCritical
        {
            get
            {
                return RuntimeMethodHandle.IsSecurityCritical(this);
            }
        }

        public override bool IsSecuritySafeCritical
        {
            get
            {
                return RuntimeMethodHandle.IsSecuritySafeCritical(this);
            }
        }

        public override bool IsSecurityTransparent
        {
            get
            {
                return RuntimeMethodHandle.IsSecurityTransparent(this);
            }
        }

        internal override ParameterInfo[] GetParametersNoCopy()
        {
            FetchNonReturnParameters();
            return m_parameters;
        }

        public override ParameterInfo[] GetParameters()
        {
            FetchNonReturnParameters();
            if (m_parameters.Length == 0)
                return m_parameters;
            ParameterInfo[] ret = new ParameterInfo[m_parameters.Length];
            Array.Copy(m_parameters, ret, m_parameters.Length);
            return ret;
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return RuntimeMethodHandle.GetImplAttributes(this);
        }

        internal bool IsOverloaded
        {
            get
            {
                return m_reflectedTypeCache.GetMethodList(MemberListType.CaseSensitive, Name).Length > 1;
            }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                Type declaringType = DeclaringType;
                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
                return new RuntimeMethodHandle(this);
            }
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return m_methodAttributes;
            }
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                return Signature.CallingConvention;
            }
        }

        public override MethodBody GetMethodBody()
        {
            MethodBody mb = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
            if (mb != null)
                mb.m_methodBase = this;
            return mb;
        }

        private void CheckConsistency(Object target)
        {
            if ((m_methodAttributes & MethodAttributes.Static) != MethodAttributes.Static)
            {
                if (!m_declaringType.IsInstanceOfType(target))
                {
                    if (target == null)
                        throw new TargetException(Environment.GetResourceString("RFLCT.Targ_StatMethReqTarg"));
                    else
                        throw new TargetException(Environment.GetResourceString("RFLCT.Targ_ITargMismatch"));
                }
            }
        }

        private void ThrowNoInvokeException()
        {
            Type declaringType = DeclaringType;
            if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyInvoke"));
            }
            else if ((InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS) != 0)
            {
                throw new NotSupportedException();
            }
            else if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
            {
                throw new NotSupportedException();
            }
            else if (DeclaringType.ContainsGenericParameters || ContainsGenericParameters)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Arg_UnboundGenParam"));
            }
            else if (IsAbstract)
            {
                throw new MemberAccessException();
            }
            else if (ReturnType.IsByRef)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ByRefReturn"));
            }

            throw new TargetException();
        }

        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            object[] arguments = InvokeArgumentsCheck(obj, invokeAttr, binder, parameters, culture);
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }

            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD | INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY)) != 0)
            {
                RuntimeMethodHandle.PerformSecurityCheck(obj, this, m_declaringType, (uint)m_invocationFlags);
            }

            return UnsafeInvokeInternal(obj, parameters, arguments);
        }

        internal object UnsafeInvoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            object[] arguments = InvokeArgumentsCheck(obj, invokeAttr, binder, parameters, culture);
            return UnsafeInvokeInternal(obj, parameters, arguments);
        }

        private object UnsafeInvokeInternal(Object obj, Object[] parameters, Object[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
                return RuntimeMethodHandle.InvokeMethod(obj, null, Signature, false);
            else
            {
                Object retValue = RuntimeMethodHandle.InvokeMethod(obj, arguments, Signature, false);
                for (int index = 0; index < arguments.Length; index++)
                    parameters[index] = arguments[index];
                return retValue;
            }
        }

        private object[] InvokeArgumentsCheck(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            Signature sig = Signature;
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS)) != 0)
                ThrowNoInvokeException();
            CheckConsistency(obj);
            if (formalCount != actualCount)
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
            if (actualCount != 0)
                return CheckArguments(parameters, binder, invokeAttr, culture, sig);
            else
                return null;
        }

        public override Type ReturnType
        {
            get
            {
                return Signature.ReturnType;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                return ReturnParameter;
            }
        }

        public override ParameterInfo ReturnParameter
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Contract.Ensures(m_returnParameter != null);
                FetchReturnParameter();
                return m_returnParameter as ParameterInfo;
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            if (!IsVirtual || IsStatic || m_declaringType == null || m_declaringType.IsInterface)
                return this;
            int slot = RuntimeMethodHandle.GetSlot(this);
            RuntimeType declaringType = (RuntimeType)DeclaringType;
            RuntimeType baseDeclaringType = declaringType;
            RuntimeMethodHandleInternal baseMethodHandle = new RuntimeMethodHandleInternal();
            do
            {
                int cVtblSlots = RuntimeTypeHandle.GetNumVirtuals(declaringType);
                if (cVtblSlots <= slot)
                    break;
                baseMethodHandle = RuntimeTypeHandle.GetMethodAt(declaringType, slot);
                baseDeclaringType = declaringType;
                declaringType = (RuntimeType)declaringType.BaseType;
            }
            while (declaringType != null);
            return (MethodInfo)RuntimeType.GetMethodBase(baseDeclaringType, baseMethodHandle);
        }

        public override Delegate CreateDelegate(Type delegateType)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateDelegateInternal(delegateType, null, DelegateBindingFlags.OpenDelegateOnly | DelegateBindingFlags.RelaxedSignature, ref stackMark);
        }

        public override Delegate CreateDelegate(Type delegateType, Object target)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateDelegateInternal(delegateType, target, DelegateBindingFlags.RelaxedSignature, ref stackMark);
        }

        private Delegate CreateDelegateInternal(Type delegateType, Object firstArgument, DelegateBindingFlags bindingFlags, ref StackCrawlMark stackMark)
        {
            if (delegateType == null)
                throw new ArgumentNullException("delegateType");
            Contract.EndContractBlock();
            RuntimeType rtType = delegateType as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "delegateType");
            if (!rtType.IsDelegate())
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "delegateType");
            Delegate d = Delegate.CreateDelegateInternal(rtType, this, firstArgument, bindingFlags, ref stackMark);
            if (d == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
            }

            return d;
        }

        public override MethodInfo MakeGenericMethod(params Type[] methodInstantiation)
        {
            if (methodInstantiation == null)
                throw new ArgumentNullException("methodInstantiation");
            Contract.EndContractBlock();
            RuntimeType[] methodInstantionRuntimeType = new RuntimeType[methodInstantiation.Length];
            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericMethodDefinition", this));
            for (int i = 0; i < methodInstantiation.Length; i++)
            {
                Type methodInstantiationElem = methodInstantiation[i];
                if (methodInstantiationElem == null)
                    throw new ArgumentNullException();
                RuntimeType rtMethodInstantiationElem = methodInstantiationElem as RuntimeType;
                if (rtMethodInstantiationElem == null)
                {
                    Type[] methodInstantiationCopy = new Type[methodInstantiation.Length];
                    for (int iCopy = 0; iCopy < methodInstantiation.Length; iCopy++)
                        methodInstantiationCopy[iCopy] = methodInstantiation[iCopy];
                    methodInstantiation = methodInstantiationCopy;
                    return System.Reflection.Emit.MethodBuilderInstantiation.MakeGenericMethod(this, methodInstantiation);
                }

                methodInstantionRuntimeType[i] = rtMethodInstantiationElem;
            }

            RuntimeType[] genericParameters = GetGenericArgumentsInternal();
            RuntimeType.SanityCheckGenericArguments(methodInstantionRuntimeType, genericParameters);
            MethodInfo ret = null;
            try
            {
                ret = RuntimeType.GetMethodBase(ReflectedTypeInternal, RuntimeMethodHandle.GetStubIfNeeded(new RuntimeMethodHandleInternal(this.m_handle), m_declaringType, methodInstantionRuntimeType)) as MethodInfo;
            }
            catch (VerificationException e)
            {
                RuntimeType.ValidateGenericArguments(this, methodInstantionRuntimeType, e);
                throw;
            }

            return ret;
        }

        internal RuntimeType[] GetGenericArgumentsInternal()
        {
            return RuntimeMethodHandle.GetMethodInstantiationInternal(this);
        }

        public override Type[] GetGenericArguments()
        {
            Type[] types = RuntimeMethodHandle.GetMethodInstantiationPublic(this);
            if (types == null)
            {
                types = EmptyArray<Type>.Value;
            }

            return types;
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            if (!IsGenericMethod)
                throw new InvalidOperationException();
            Contract.EndContractBlock();
            return RuntimeType.GetMethodBase(m_declaringType, RuntimeMethodHandle.StripMethodInstantiation(this)) as MethodInfo;
        }

        public override bool IsGenericMethod
        {
            get
            {
                return RuntimeMethodHandle.HasMethodInstantiation(this);
            }
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return RuntimeMethodHandle.IsGenericMethodDefinition(this);
            }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                if (DeclaringType != null && DeclaringType.ContainsGenericParameters)
                    return true;
                if (!IsGenericMethod)
                    return false;
                Type[] pis = GetGenericArguments();
                for (int i = 0; i < pis.Length; i++)
                {
                    if (pis[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            if (m_reflectedTypeCache.IsGlobal)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_GlobalMethodSerialization"));
            MemberInfoSerializationHolder.GetSerializationInfo(info, Name, ReflectedTypeInternal, ToString(), SerializationToString(), MemberTypes.Method, IsGenericMethod & !IsGenericMethodDefinition ? GetGenericArguments() : null);
        }

        internal string SerializationToString()
        {
            return ReturnType.FormatTypeName(true) + " " + FormatNameAndSig(true);
        }

        internal static MethodBase InternalGetCurrentMethod(ref StackCrawlMark stackMark)
        {
            IRuntimeMethodInfo method = RuntimeMethodHandle.GetCurrentMethod(ref stackMark);
            if (method == null)
                return null;
            return RuntimeType.GetMethodBase(method);
        }
    }
}