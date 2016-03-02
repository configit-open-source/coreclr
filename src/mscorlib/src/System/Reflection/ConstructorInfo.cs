using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Reflection
{
    public abstract class ConstructorInfo : MethodBase, _ConstructorInfo
    {
        public readonly static String ConstructorName = ".ctor";
        public readonly static String TypeConstructorName = ".cctor";
        protected ConstructorInfo()
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

        internal virtual Type GetReturnType()
        {
            throw new NotImplementedException();
        }

        public override MemberTypes MemberType
        {
            get
            {
                return System.Reflection.MemberTypes.Constructor;
            }
        }

        public abstract Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);
        public Object Invoke(Object[] parameters)
        {
            return Invoke(BindingFlags.Default, null, parameters, null);
        }
    }

    internal sealed class RuntimeConstructorInfo : ConstructorInfo, ISerializable, IRuntimeMethodInfo
    {
        private volatile RuntimeType m_declaringType;
        private RuntimeType.RuntimeTypeCache m_reflectedTypeCache;
        private string m_toString;
        private ParameterInfo[] m_parameters = null;
        private object _empty1;
        private object _empty2;
        private object _empty3;
        private IntPtr m_handle;
        private MethodAttributes m_methodAttributes;
        private BindingFlags m_bindingFlags;
        private volatile Signature m_signature;
        private INVOCATION_FLAGS m_invocationFlags;
        private bool IsNonW8PFrameworkAPI()
        {
            if (DeclaringType.IsArray && IsPublic && !IsStatic)
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
                    INVOCATION_FLAGS invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_IS_CTOR;
                    Type declaringType = DeclaringType;
                    if (declaringType == typeof (void) || (declaringType != null && declaringType.ContainsGenericParameters) || ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs) || ((Attributes & MethodAttributes.RequireSecObject) == MethodAttributes.RequireSecObject))
                    {
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
                    }
                    else if (IsStatic || declaringType != null && declaringType.IsAbstract)
                    {
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE;
                    }
                    else
                    {
                        invocationFlags |= RuntimeMethodHandle.GetSecurityFlags(this);
                        if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) == 0 && ((Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public || (declaringType != null && declaringType.NeedsReflectionSecurityCheck)))
                        {
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
                        }

                        if (typeof (Delegate).IsAssignableFrom(DeclaringType))
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_IS_DELEGATE_CTOR;
                    }

                    if (AppDomain.ProfileAPICheck && IsNonW8PFrameworkAPI())
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API;
                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }

        internal RuntimeConstructorInfo(RuntimeMethodHandleInternal handle, RuntimeType declaringType, RuntimeType.RuntimeTypeCache reflectedTypeCache, MethodAttributes methodAttributes, BindingFlags bindingFlags)
        {
                        m_bindingFlags = bindingFlags;
            m_reflectedTypeCache = reflectedTypeCache;
            m_declaringType = declaringType;
            m_handle = handle.Value;
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

        internal override bool CacheEquals(object o)
        {
            RuntimeConstructorInfo m = o as RuntimeConstructorInfo;
            if ((object)m == null)
                return false;
            return m.m_handle == m_handle;
        }

        private Signature Signature
        {
            get
            {
                if (m_signature == null)
                    m_signature = new Signature(this, m_declaringType);
                return m_signature;
            }
        }

        private RuntimeType ReflectedTypeInternal
        {
            get
            {
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        private void CheckConsistency(Object target)
        {
            if (target == null && IsStatic)
                return;
            if (!m_declaringType.IsInstanceOfType(target))
            {
                if (target == null)
                    throw new TargetException(Environment.GetResourceString("RFLCT.Targ_StatMethReqTarg"));
                throw new TargetException(Environment.GetResourceString("RFLCT.Targ_ITargMismatch"));
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

        internal bool IsOverloaded
        {
            get
            {
                return m_reflectedTypeCache.GetConstructorList(RuntimeType.MemberListType.CaseSensitive, Name).Length > 1;
            }
        }

        public override String ToString()
        {
            if (m_toString == null)
                m_toString = "Void " + FormatNameAndSig();
            return m_toString;
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof (object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
                        RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
                        RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.IsDefined(this, attributeRuntimeType);
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
                return RuntimeMethodHandle.GetName(this);
            }
        }

        public override MemberTypes MemberType
        {
            get
            {
                return MemberTypes.Constructor;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_reflectedTypeCache.IsGlobal ? null : m_declaringType;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_reflectedTypeCache.IsGlobal ? null : ReflectedTypeInternal;
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
            return RuntimeTypeHandle.GetModule(m_declaringType);
        }

        internal RuntimeAssembly GetRuntimeAssembly()
        {
            return GetRuntimeModule().GetRuntimeAssembly();
        }

        internal override Type GetReturnType()
        {
            return Signature.ReturnType;
        }

        internal override ParameterInfo[] GetParametersNoCopy()
        {
            if (m_parameters == null)
                m_parameters = RuntimeParameterInfo.GetParameters(this, this, Signature);
            return m_parameters;
        }

        public override ParameterInfo[] GetParameters()
        {
            ParameterInfo[] parameters = GetParametersNoCopy();
            if (parameters.Length == 0)
                return parameters;
            ParameterInfo[] ret = new ParameterInfo[parameters.Length];
            Array.Copy(parameters, ret, parameters.Length);
            return ret;
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return RuntimeMethodHandle.GetImplAttributes(this);
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

        internal static void CheckCanCreateInstance(Type declaringType, bool isVarArg)
        {
            if (declaringType == null)
                throw new ArgumentNullException("declaringType");
                        if (declaringType is ReflectionOnlyType)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyInvoke"));
            else if (declaringType.IsInterface)
                throw new MemberAccessException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Acc_CreateInterfaceEx"), declaringType));
            else if (declaringType.IsAbstract)
                throw new MemberAccessException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Acc_CreateAbstEx"), declaringType));
            else if (declaringType.GetRootElementType() == typeof (ArgIterator))
                throw new NotSupportedException();
            else if (isVarArg)
                throw new NotSupportedException();
            else if (declaringType.ContainsGenericParameters)
            {
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Acc_CreateGenericEx"), declaringType));
                else
                    throw new MemberAccessException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Acc_CreateGenericEx"), declaringType));
            }
            else if (declaringType == typeof (void))
                throw new MemberAccessException(Environment.GetResourceString("Access_Void"));
        }

        internal void ThrowNoInvokeException()
        {
            CheckCanCreateInstance(DeclaringType, (CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);
            if ((Attributes & MethodAttributes.Static) == MethodAttributes.Static)
                throw new MemberAccessException(Environment.GetResourceString("Acc_NotClassInit"));
            throw new TargetException();
        }

        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
                ThrowNoInvokeException();
            CheckConsistency(obj);
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }

            if (obj != null)
            {
                invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
            }

            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD | INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY)) != 0)
            {
                RuntimeMethodHandle.PerformSecurityCheck(obj, this, m_declaringType, (uint)m_invocationFlags);
            }

            Signature sig = Signature;
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
            if (actualCount > 0)
            {
                Object[] arguments = CheckArguments(parameters, binder, invokeAttr, culture, sig);
                Object retValue = RuntimeMethodHandle.InvokeMethod(obj, arguments, sig, false);
                for (int index = 0; index < arguments.Length; index++)
                    parameters[index] = arguments[index];
                return retValue;
            }

            return RuntimeMethodHandle.InvokeMethod(obj, null, sig, false);
        }

        public override MethodBody GetMethodBody()
        {
            MethodBody mb = RuntimeMethodHandle.GetMethodBody(this, ReflectedTypeInternal);
            if (mb != null)
                mb.m_methodBase = this;
            return mb;
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

        public override bool ContainsGenericParameters
        {
            get
            {
                return (DeclaringType != null && DeclaringType.ContainsGenericParameters);
            }
        }

        public override Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeTypeHandle declaringTypeHandle = m_declaringType.TypeHandle;
            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE | INVOCATION_FLAGS.INVOCATION_FLAGS_CONTAINS_STACK_POINTERS | INVOCATION_FLAGS.INVOCATION_FLAGS_NO_CTOR_INVOKE)) != 0)
                ThrowNoInvokeException();
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }

            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_RISKY_METHOD | INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY | INVOCATION_FLAGS.INVOCATION_FLAGS_IS_DELEGATE_CTOR)) != 0)
            {
                RuntimeMethodHandle.PerformSecurityCheck(null, this, m_declaringType, (uint)(m_invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_CONSTRUCTOR_INVOKE));
            }

            Signature sig = Signature;
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));
            if (actualCount > 0)
            {
                Object[] arguments = CheckArguments(parameters, binder, invokeAttr, culture, sig);
                Object retValue = RuntimeMethodHandle.InvokeMethod(null, arguments, sig, true);
                for (int index = 0; index < arguments.Length; index++)
                    parameters[index] = arguments[index];
                return retValue;
            }

            return RuntimeMethodHandle.InvokeMethod(null, null, sig, true);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
                        MemberInfoSerializationHolder.GetSerializationInfo(info, Name, ReflectedTypeInternal, ToString(), SerializationToString(), MemberTypes.Constructor, null);
        }

        internal string SerializationToString()
        {
            return FormatNameAndSig(true);
        }

        internal void SerializationInvoke(Object target, SerializationInfo info, StreamingContext context)
        {
            RuntimeMethodHandle.SerializationInvoke(this, target, info, ref context);
        }
    }
}