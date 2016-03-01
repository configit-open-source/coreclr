
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
    public abstract class Delegate : ICloneable, ISerializable
    {
        internal Object _target;
        internal Object _methodBase;
        internal IntPtr _methodPtr;
        internal IntPtr _methodPtrAux;
        protected Delegate(Object target, String method)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (method == null)
                throw new ArgumentNullException("method");
                        if (!BindToMethodName(target, (RuntimeType)target.GetType(), method, DelegateBindingFlags.InstanceMethodOnly | DelegateBindingFlags.ClosedDelegateOnly))
                throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
        }

        protected unsafe Delegate(Type target, String method)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (target.IsGenericType && target.ContainsGenericParameters)
                throw new ArgumentException(Environment.GetResourceString("Arg_UnboundGenParam"), "target");
            if (method == null)
                throw new ArgumentNullException("method");
                        RuntimeType rtTarget = target as RuntimeType;
            if (rtTarget == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "target");
            BindToMethodName(null, rtTarget, method, DelegateBindingFlags.StaticMethodOnly | DelegateBindingFlags.OpenDelegateOnly | DelegateBindingFlags.CaselessMatching);
        }

        private Delegate()
        {
        }

        public Object DynamicInvoke(params Object[] args)
        {
            return DynamicInvokeImpl(args);
        }

        protected virtual object DynamicInvokeImpl(object[] args)
        {
            RuntimeMethodHandleInternal method = new RuntimeMethodHandleInternal(GetInvokeMethod());
            RuntimeMethodInfo invoke = (RuntimeMethodInfo)RuntimeType.GetMethodBase((RuntimeType)this.GetType(), method);
            return invoke.UnsafeInvoke(this, BindingFlags.Default, null, args, null);
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || !InternalEqualTypes(this, obj))
                return false;
            Delegate d = (Delegate)obj;
            if (_target == d._target && _methodPtr == d._methodPtr && _methodPtrAux == d._methodPtrAux)
                return true;
            if (_methodPtrAux.IsNull())
            {
                if (!d._methodPtrAux.IsNull())
                    return false;
                if (_target != d._target)
                    return false;
            }
            else
            {
                if (d._methodPtrAux.IsNull())
                    return false;
                if (_methodPtrAux == d._methodPtrAux)
                    return true;
            }

            if (_methodBase == null || d._methodBase == null || !(_methodBase is MethodInfo) || !(d._methodBase is MethodInfo))
                return Delegate.InternalEqualMethodHandles(this, d);
            else
                return _methodBase.Equals(d._methodBase);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        public static Delegate Combine(Delegate a, Delegate b)
        {
            if ((Object)a == null)
                return b;
            return a.CombineImpl(b);
        }

        public static Delegate Combine(params Delegate[] delegates)
        {
            if (delegates == null || delegates.Length == 0)
                return null;
            Delegate d = delegates[0];
            for (int i = 1; i < delegates.Length; i++)
                d = Combine(d, delegates[i]);
            return d;
        }

        public virtual Delegate[] GetInvocationList()
        {
            Delegate[] d = new Delegate[1];
            d[0] = this;
            return d;
        }

        public MethodInfo Method
        {
            get
            {
                return GetMethodImpl();
            }
        }

        protected virtual MethodInfo GetMethodImpl()
        {
            if ((_methodBase == null) || !(_methodBase is MethodInfo))
            {
                IRuntimeMethodInfo method = FindMethodHandle();
                RuntimeType declaringType = RuntimeMethodHandle.GetDeclaringType(method);
                if (RuntimeTypeHandle.IsGenericTypeDefinition(declaringType) || RuntimeTypeHandle.HasInstantiation(declaringType))
                {
                    bool isStatic = (RuntimeMethodHandle.GetAttributes(method) & MethodAttributes.Static) != (MethodAttributes)0;
                    if (!isStatic)
                    {
                        if (_methodPtrAux == (IntPtr)0)
                        {
                            Type currentType = _target.GetType();
                            Type targetType = declaringType.GetGenericTypeDefinition();
                            while (currentType != null)
                            {
                                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == targetType)
                                {
                                    declaringType = currentType as RuntimeType;
                                    break;
                                }

                                currentType = currentType.BaseType;
                            }

                            BCLDebug.Assert(currentType != null || _target.GetType().IsCOMObject, "The class hierarchy should declare the method");
                        }
                        else
                        {
                            MethodInfo invoke = this.GetType().GetMethod("Invoke");
                            declaringType = (RuntimeType)invoke.GetParameters()[0].ParameterType;
                        }
                    }
                }

                _methodBase = (MethodInfo)RuntimeType.GetMethodBase(declaringType, method);
            }

            return (MethodInfo)_methodBase;
        }

        public Object Target
        {
            get
            {
                return GetTarget();
            }
        }

        public static Delegate Remove(Delegate source, Delegate value)
        {
            if (source == null)
                return null;
            if (value == null)
                return source;
            if (!InternalEqualTypes(source, value))
                throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTypeMis"));
            return source.RemoveImpl(value);
        }

        public static Delegate RemoveAll(Delegate source, Delegate value)
        {
            Delegate newDelegate = null;
            do
            {
                newDelegate = source;
                source = Remove(source, value);
            }
            while (newDelegate != source);
            return newDelegate;
        }

        protected virtual Delegate CombineImpl(Delegate d)
        {
            throw new MulticastNotSupportedException(Environment.GetResourceString("Multicast_Combine"));
        }

        protected virtual Delegate RemoveImpl(Delegate d)
        {
            return (d.Equals(this)) ? null : this;
        }

        public virtual Object Clone()
        {
            return MemberwiseClone();
        }

        public static Delegate CreateDelegate(Type type, Object target, String method)
        {
            return CreateDelegate(type, target, method, false, true);
        }

        public static Delegate CreateDelegate(Type type, Object target, String method, bool ignoreCase)
        {
            return CreateDelegate(type, target, method, ignoreCase, true);
        }

        private static void CheckGetMethodInfo_Quirk(Type type, Type target, String method, BindingFlags bindingFlags)
        {
            ParameterInfo[] parameters = type.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance).GetParameters();
            Type[] types = new Type[parameters.Length];
            for (Int32 i = 0; i < parameters.Length; i++)
                types[i] = parameters[i].ParameterType;
            MethodInfo mInfo = target.GetMethod(method, bindingFlags, null, types, null);
            if (mInfo == null)
                throw new MissingMethodException(target.FullName, method);
        }

        public static Delegate CreateDelegate(Type type, Object target, String method, bool ignoreCase, bool throwOnBindFailure)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (target == null)
                throw new ArgumentNullException("target");
            if (method == null)
                throw new ArgumentNullException("method");
                        RuntimeType rtType = type as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "type");
            if (!rtType.IsDelegate())
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "type");
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                CheckGetMethodInfo_Quirk(type, target.GetType(), method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | (ignoreCase ? BindingFlags.IgnoreCase : 0));
            Delegate d = InternalAlloc(rtType);
            if (!d.BindToMethodName(target, (RuntimeType)target.GetType(), method, DelegateBindingFlags.InstanceMethodOnly | DelegateBindingFlags.ClosedDelegateOnly | DelegateBindingFlags.NeverCloseOverNull | (ignoreCase ? DelegateBindingFlags.CaselessMatching : 0)))
            {
                if (throwOnBindFailure)
                    throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
                d = null;
            }

            return d;
        }

        public static Delegate CreateDelegate(Type type, Type target, String method)
        {
            return CreateDelegate(type, target, method, false, true);
        }

        public static Delegate CreateDelegate(Type type, Type target, String method, bool ignoreCase)
        {
            return CreateDelegate(type, target, method, ignoreCase, true);
        }

        public static Delegate CreateDelegate(Type type, Type target, String method, bool ignoreCase, bool throwOnBindFailure)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (target == null)
                throw new ArgumentNullException("target");
            if (target.IsGenericType && target.ContainsGenericParameters)
                throw new ArgumentException(Environment.GetResourceString("Arg_UnboundGenParam"), "target");
            if (method == null)
                throw new ArgumentNullException("method");
                        RuntimeType rtType = type as RuntimeType;
            RuntimeType rtTarget = target as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "type");
            if (rtTarget == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "target");
            if (!rtType.IsDelegate())
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "type");
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                CheckGetMethodInfo_Quirk(type, target, method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | (ignoreCase ? BindingFlags.IgnoreCase : 0));
            Delegate d = InternalAlloc(rtType);
            if (!d.BindToMethodName(null, rtTarget, method, DelegateBindingFlags.StaticMethodOnly | DelegateBindingFlags.OpenDelegateOnly | (ignoreCase ? DelegateBindingFlags.CaselessMatching : 0)))
            {
                if (throwOnBindFailure)
                    throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
                d = null;
            }

            return d;
        }

        public static Delegate CreateDelegate(Type type, MethodInfo method, bool throwOnBindFailure)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (method == null)
                throw new ArgumentNullException("method");
                        RuntimeType rtType = type as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "type");
            RuntimeMethodInfo rmi = method as RuntimeMethodInfo;
            if (rmi == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "method");
            if (!rtType.IsDelegate())
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "type");
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Delegate d = CreateDelegateInternal(rtType, rmi, null, DelegateBindingFlags.OpenDelegateOnly | DelegateBindingFlags.RelaxedSignature, ref stackMark);
            if (d == null && throwOnBindFailure)
                throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
            return d;
        }

        public static Delegate CreateDelegate(Type type, Object firstArgument, MethodInfo method)
        {
            return CreateDelegate(type, firstArgument, method, true);
        }

        public static Delegate CreateDelegate(Type type, Object firstArgument, MethodInfo method, bool throwOnBindFailure)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (method == null)
                throw new ArgumentNullException("method");
                        RuntimeType rtType = type as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "type");
            RuntimeMethodInfo rmi = method as RuntimeMethodInfo;
            if (rmi == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "method");
            if (!rtType.IsDelegate())
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "type");
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Delegate d = CreateDelegateInternal(rtType, rmi, firstArgument, DelegateBindingFlags.RelaxedSignature, ref stackMark);
            if (d == null && throwOnBindFailure)
                throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
            return d;
        }

        public static bool operator ==(Delegate d1, Delegate d2)
        {
            if ((Object)d1 == null)
                return (Object)d2 == null;
            return d1.Equals(d2);
        }

        public static bool operator !=(Delegate d1, Delegate d2)
        {
            if ((Object)d1 == null)
                return (Object)d2 != null;
            return !d1.Equals(d2);
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        internal unsafe static Delegate CreateDelegateNoSecurityCheck(Type type, Object target, RuntimeMethodHandle method)
        {
            if (type == null)
                throw new ArgumentNullException("type");
                        if (method.IsNullHandle())
                throw new ArgumentNullException("method");
            RuntimeType rtType = type as RuntimeType;
            if (rtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "type");
            if (!rtType.IsDelegate())
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "type");
            Delegate d = InternalAlloc(rtType);
            if (!d.BindToMethodInfo(target, method.GetMethodInfo(), RuntimeMethodHandle.GetDeclaringType(method.GetMethodInfo()), DelegateBindingFlags.RelaxedSignature | DelegateBindingFlags.SkipSecurityChecks))
                throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
            return d;
        }

        internal static Delegate CreateDelegateNoSecurityCheck(RuntimeType type, Object firstArgument, MethodInfo method)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (method == null)
                throw new ArgumentNullException("method");
                        RuntimeMethodInfo rtMethod = method as RuntimeMethodInfo;
            if (rtMethod == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "method");
            if (!type.IsDelegate())
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "type");
            Delegate d = UnsafeCreateDelegate(type, rtMethod, firstArgument, DelegateBindingFlags.SkipSecurityChecks | DelegateBindingFlags.RelaxedSignature);
            if (d == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTargMeth"));
            return d;
        }

        public static Delegate CreateDelegate(Type type, MethodInfo method)
        {
            return CreateDelegate(type, method, true);
        }

        internal static Delegate CreateDelegateInternal(RuntimeType rtType, RuntimeMethodInfo rtMethod, Object firstArgument, DelegateBindingFlags flags, ref StackCrawlMark stackMark)
        {
                        bool nonW8PMethod = (rtMethod.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0;
            bool nonW8PType = (rtType.InvocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0;
            if (nonW8PMethod || nonW8PType)
            {
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", nonW8PMethod ? rtMethod.FullName : rtType.FullName));
            }

            return UnsafeCreateDelegate(rtType, rtMethod, firstArgument, flags);
        }

        internal static Delegate UnsafeCreateDelegate(RuntimeType rtType, RuntimeMethodInfo rtMethod, Object firstArgument, DelegateBindingFlags flags)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                if (rtMethod.IsGenericMethodDefinition)
                    throw new MissingMethodException(rtMethod.DeclaringType.FullName, rtMethod.Name);
            Delegate d = InternalAlloc(rtType);
            if (d.BindToMethodInfo(firstArgument, rtMethod, rtMethod.GetDeclaringTypeInternal(), flags))
                return d;
            else
                return null;
        }

        private extern bool BindToMethodName(Object target, RuntimeType methodType, String method, DelegateBindingFlags flags);
        private extern bool BindToMethodInfo(Object target, IRuntimeMethodInfo method, RuntimeType methodType, DelegateBindingFlags flags);
        private extern static MulticastDelegate InternalAlloc(RuntimeType type);
        internal extern static MulticastDelegate InternalAllocLike(Delegate d);
        internal extern static bool InternalEqualTypes(object a, object b);
        private extern void DelegateConstruct(Object target, IntPtr slot);
        internal extern IntPtr GetMulticastInvoke();
        internal extern IntPtr GetInvokeMethod();
        internal extern IRuntimeMethodInfo FindMethodHandle();
        internal extern static bool InternalEqualMethodHandles(Delegate left, Delegate right);
        internal extern IntPtr AdjustTarget(Object target, IntPtr methodPtr);
        internal extern IntPtr GetCallStub(IntPtr methodPtr);
        internal virtual Object GetTarget()
        {
            return (_methodPtrAux.IsNull()) ? _target : null;
        }

        internal extern static bool CompareUnmanagedFunctionPtrs(Delegate d1, Delegate d2);
    }

    internal enum DelegateBindingFlags
    {
        StaticMethodOnly = 0x00000001,
        InstanceMethodOnly = 0x00000002,
        OpenDelegateOnly = 0x00000004,
        ClosedDelegateOnly = 0x00000008,
        NeverCloseOverNull = 0x00000010,
        CaselessMatching = 0x00000020,
        SkipSecurityChecks = 0x00000040,
        RelaxedSignature = 0x00000080
    }
}