
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace System
{
    public abstract class MulticastDelegate : Delegate
    {
        private Object _invocationList;
        private IntPtr _invocationCount;
        protected MulticastDelegate(Object target, String method): base (target, method)
        {
        }

        protected MulticastDelegate(Type target, String method): base (target, method)
        {
        }

        internal bool IsUnmanagedFunctionPtr()
        {
            return (_invocationCount == (IntPtr)(-1));
        }

        internal bool InvocationListLogicallyNull()
        {
            return (_invocationList == null) || (_invocationList is LoaderAllocator) || (_invocationList is DynamicResolver);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            int targetIndex = 0;
            Object[] invocationList = _invocationList as Object[];
            if (invocationList == null)
            {
                MethodInfo method = Method;
                if (!(method is RuntimeMethodInfo) || IsUnmanagedFunctionPtr())
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidDelegateType"));
                if (!InvocationListLogicallyNull() && !_invocationCount.IsNull() && !_methodPtrAux.IsNull())
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidDelegateType"));
                DelegateSerializationHolder.GetDelegateSerializationInfo(info, this.GetType(), Target, method, targetIndex);
            }
            else
            {
                DelegateSerializationHolder.DelegateEntry nextDe = null;
                int invocationCount = (int)_invocationCount;
                for (int i = invocationCount; --i >= 0;)
                {
                    MulticastDelegate d = (MulticastDelegate)invocationList[i];
                    MethodInfo method = d.Method;
                    if (!(method is RuntimeMethodInfo) || IsUnmanagedFunctionPtr())
                        continue;
                    if (!d.InvocationListLogicallyNull() && !d._invocationCount.IsNull() && !d._methodPtrAux.IsNull())
                        continue;
                    DelegateSerializationHolder.DelegateEntry de = DelegateSerializationHolder.GetDelegateSerializationInfo(info, d.GetType(), d.Target, method, targetIndex++);
                    if (nextDe != null)
                        nextDe.Entry = de;
                    nextDe = de;
                }

                if (nextDe == null)
                    throw new SerializationException(Environment.GetResourceString("Serialization_InvalidDelegateType"));
            }
        }

        public override sealed bool Equals(Object obj)
        {
            if (obj == null || !InternalEqualTypes(this, obj))
                return false;
            MulticastDelegate d = obj as MulticastDelegate;
            if (d == null)
                return false;
            if (_invocationCount != (IntPtr)0)
            {
                if (InvocationListLogicallyNull())
                {
                    if (IsUnmanagedFunctionPtr())
                    {
                        if (!d.IsUnmanagedFunctionPtr())
                            return false;
                        return CompareUnmanagedFunctionPtrs(this, d);
                    }

                    if ((d._invocationList as Delegate) != null)
                        return Equals(d._invocationList);
                    return base.Equals(obj);
                }
                else
                {
                    if ((_invocationList as Delegate) != null)
                    {
                        return _invocationList.Equals(obj);
                    }
                    else
                    {
                                                return InvocationListEquals(d);
                    }
                }
            }
            else
            {
                if (!InvocationListLogicallyNull())
                {
                    if (!_invocationList.Equals(d._invocationList))
                        return false;
                    return base.Equals(d);
                }

                if ((d._invocationList as Delegate) != null)
                    return Equals(d._invocationList);
                return base.Equals(d);
            }
        }

        private bool InvocationListEquals(MulticastDelegate d)
        {
                        Object[] invocationList = _invocationList as Object[];
            if (d._invocationCount != _invocationCount)
                return false;
            int invocationCount = (int)_invocationCount;
            for (int i = 0; i < invocationCount; i++)
            {
                Delegate dd = (Delegate)invocationList[i];
                Object[] dInvocationList = d._invocationList as Object[];
                if (!dd.Equals(dInvocationList[i]))
                    return false;
            }

            return true;
        }

        private bool TrySetSlot(Object[] a, int index, Object o)
        {
            if (a[index] == null && System.Threading.Interlocked.CompareExchange<Object>(ref a[index], o, null) == null)
                return true;
            if (a[index] != null)
            {
                MulticastDelegate d = (MulticastDelegate)o;
                MulticastDelegate dd = (MulticastDelegate)a[index];
                if (dd._methodPtr == d._methodPtr && dd._target == d._target && dd._methodPtrAux == d._methodPtrAux)
                {
                    return true;
                }
            }

            return false;
        }

        private MulticastDelegate NewMulticastDelegate(Object[] invocationList, int invocationCount, bool thisIsMultiCastAlready)
        {
            MulticastDelegate result = (MulticastDelegate)InternalAllocLike(this);
            if (thisIsMultiCastAlready)
            {
                result._methodPtr = this._methodPtr;
                result._methodPtrAux = this._methodPtrAux;
            }
            else
            {
                result._methodPtr = GetMulticastInvoke();
                result._methodPtrAux = GetInvokeMethod();
            }

            result._target = result;
            result._invocationList = invocationList;
            result._invocationCount = (IntPtr)invocationCount;
            return result;
        }

        internal MulticastDelegate NewMulticastDelegate(Object[] invocationList, int invocationCount)
        {
            return NewMulticastDelegate(invocationList, invocationCount, false);
        }

        internal void StoreDynamicMethod(MethodInfo dynamicMethod)
        {
            if (_invocationCount != (IntPtr)0)
            {
                                MulticastDelegate d = (MulticastDelegate)_invocationList;
                d._methodBase = dynamicMethod;
            }
            else
                _methodBase = dynamicMethod;
        }

        protected override sealed Delegate CombineImpl(Delegate follow)
        {
            if ((Object)follow == null)
                return this;
            if (!InternalEqualTypes(this, follow))
                throw new ArgumentException(Environment.GetResourceString("Arg_DlgtTypeMis"));
            MulticastDelegate dFollow = (MulticastDelegate)follow;
            Object[] resultList;
            int followCount = 1;
            Object[] followList = dFollow._invocationList as Object[];
            if (followList != null)
                followCount = (int)dFollow._invocationCount;
            int resultCount;
            Object[] invocationList = _invocationList as Object[];
            if (invocationList == null)
            {
                resultCount = 1 + followCount;
                resultList = new Object[resultCount];
                resultList[0] = this;
                if (followList == null)
                {
                    resultList[1] = dFollow;
                }
                else
                {
                    for (int i = 0; i < followCount; i++)
                        resultList[1 + i] = followList[i];
                }

                return NewMulticastDelegate(resultList, resultCount);
            }
            else
            {
                int invocationCount = (int)_invocationCount;
                resultCount = invocationCount + followCount;
                resultList = null;
                if (resultCount <= invocationList.Length)
                {
                    resultList = invocationList;
                    if (followList == null)
                    {
                        if (!TrySetSlot(resultList, invocationCount, dFollow))
                            resultList = null;
                    }
                    else
                    {
                        for (int i = 0; i < followCount; i++)
                        {
                            if (!TrySetSlot(resultList, invocationCount + i, followList[i]))
                            {
                                resultList = null;
                                break;
                            }
                        }
                    }
                }

                if (resultList == null)
                {
                    int allocCount = invocationList.Length;
                    while (allocCount < resultCount)
                        allocCount *= 2;
                    resultList = new Object[allocCount];
                    for (int i = 0; i < invocationCount; i++)
                        resultList[i] = invocationList[i];
                    if (followList == null)
                    {
                        resultList[invocationCount] = dFollow;
                    }
                    else
                    {
                        for (int i = 0; i < followCount; i++)
                            resultList[invocationCount + i] = followList[i];
                    }
                }

                return NewMulticastDelegate(resultList, resultCount, true);
            }
        }

        private Object[] DeleteFromInvocationList(Object[] invocationList, int invocationCount, int deleteIndex, int deleteCount)
        {
            Object[] thisInvocationList = _invocationList as Object[];
            int allocCount = thisInvocationList.Length;
            while (allocCount / 2 >= invocationCount - deleteCount)
                allocCount /= 2;
            Object[] newInvocationList = new Object[allocCount];
            for (int i = 0; i < deleteIndex; i++)
                newInvocationList[i] = invocationList[i];
            for (int i = deleteIndex + deleteCount; i < invocationCount; i++)
                newInvocationList[i - deleteCount] = invocationList[i];
            return newInvocationList;
        }

        private bool EqualInvocationLists(Object[] a, Object[] b, int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!(a[start + i].Equals(b[i])))
                    return false;
            }

            return true;
        }

        protected override sealed Delegate RemoveImpl(Delegate value)
        {
            MulticastDelegate v = value as MulticastDelegate;
            if (v == null)
                return this;
            if (v._invocationList as Object[] == null)
            {
                Object[] invocationList = _invocationList as Object[];
                if (invocationList == null)
                {
                    if (this.Equals(value))
                        return null;
                }
                else
                {
                    int invocationCount = (int)_invocationCount;
                    for (int i = invocationCount; --i >= 0;)
                    {
                        if (value.Equals(invocationList[i]))
                        {
                            if (invocationCount == 2)
                            {
                                return (Delegate)invocationList[1 - i];
                            }
                            else
                            {
                                Object[] list = DeleteFromInvocationList(invocationList, invocationCount, i, 1);
                                return NewMulticastDelegate(list, invocationCount - 1, true);
                            }
                        }
                    }
                }
            }
            else
            {
                Object[] invocationList = _invocationList as Object[];
                if (invocationList != null)
                {
                    int invocationCount = (int)_invocationCount;
                    int vInvocationCount = (int)v._invocationCount;
                    for (int i = invocationCount - vInvocationCount; i >= 0; i--)
                    {
                        if (EqualInvocationLists(invocationList, v._invocationList as Object[], i, vInvocationCount))
                        {
                            if (invocationCount - vInvocationCount == 0)
                            {
                                return null;
                            }
                            else if (invocationCount - vInvocationCount == 1)
                            {
                                return (Delegate)invocationList[i != 0 ? 0 : invocationCount - 1];
                            }
                            else
                            {
                                Object[] list = DeleteFromInvocationList(invocationList, invocationCount, i, vInvocationCount);
                                return NewMulticastDelegate(list, invocationCount - vInvocationCount, true);
                            }
                        }
                    }
                }
            }

            return this;
        }

        public override sealed Delegate[] GetInvocationList()
        {
                        Delegate[] del;
            Object[] invocationList = _invocationList as Object[];
            if (invocationList == null)
            {
                del = new Delegate[1];
                del[0] = this;
            }
            else
            {
                int invocationCount = (int)_invocationCount;
                del = new Delegate[invocationCount];
                for (int i = 0; i < invocationCount; i++)
                    del[i] = (Delegate)invocationList[i];
            }

            return del;
        }

        public static bool operator ==(MulticastDelegate d1, MulticastDelegate d2)
        {
            if ((Object)d1 == null)
                return (Object)d2 == null;
            return d1.Equals(d2);
        }

        public static bool operator !=(MulticastDelegate d1, MulticastDelegate d2)
        {
            if ((Object)d1 == null)
                return (Object)d2 != null;
            return !d1.Equals(d2);
        }

        public override sealed int GetHashCode()
        {
            if (IsUnmanagedFunctionPtr())
                return ValueType.GetHashCodeOfPtr(_methodPtr) ^ ValueType.GetHashCodeOfPtr(_methodPtrAux);
            Object[] invocationList = _invocationList as Object[];
            if (invocationList == null)
            {
                return base.GetHashCode();
            }
            else
            {
                int hash = 0;
                for (int i = 0; i < (int)_invocationCount; i++)
                {
                    hash = hash * 33 + invocationList[i].GetHashCode();
                }

                return hash;
            }
        }

        internal override Object GetTarget()
        {
            if (_invocationCount != (IntPtr)0)
            {
                if (InvocationListLogicallyNull())
                {
                    return null;
                }
                else
                {
                    Object[] invocationList = _invocationList as Object[];
                    if (invocationList != null)
                    {
                        int invocationCount = (int)_invocationCount;
                        return ((Delegate)invocationList[invocationCount - 1]).GetTarget();
                    }
                    else
                    {
                        Delegate receiver = _invocationList as Delegate;
                        if (receiver != null)
                            return receiver.GetTarget();
                    }
                }
            }

            return base.GetTarget();
        }

        protected override MethodInfo GetMethodImpl()
        {
            if (_invocationCount != (IntPtr)0 && _invocationList != null)
            {
                Object[] invocationList = _invocationList as Object[];
                if (invocationList != null)
                {
                    int index = (int)_invocationCount - 1;
                    return ((Delegate)invocationList[index]).Method;
                }

                MulticastDelegate innerDelegate = _invocationList as MulticastDelegate;
                if (innerDelegate != null)
                {
                    return innerDelegate.GetMethodImpl();
                }
            }
            else if (IsUnmanagedFunctionPtr())
            {
                if ((_methodBase == null) || !(_methodBase is MethodInfo))
                {
                    IRuntimeMethodInfo method = FindMethodHandle();
                    RuntimeType declaringType = RuntimeMethodHandle.GetDeclaringType(method);
                    if (RuntimeTypeHandle.IsGenericTypeDefinition(declaringType) || RuntimeTypeHandle.HasInstantiation(declaringType))
                    {
                        RuntimeType reflectedType = GetType() as RuntimeType;
                        declaringType = reflectedType;
                    }

                    _methodBase = (MethodInfo)RuntimeType.GetMethodBase(declaringType, method);
                }

                return (MethodInfo)_methodBase;
            }

            return base.GetMethodImpl();
        }

        private void ThrowNullThisInDelegateToInstance()
        {
            throw new ArgumentException(Environment.GetResourceString("Arg_DlgtNullInst"));
        }

        private void CtorClosed(Object target, IntPtr methodPtr)
        {
            if (target == null)
                ThrowNullThisInDelegateToInstance();
            this._target = target;
            this._methodPtr = methodPtr;
        }

        private void CtorClosedStatic(Object target, IntPtr methodPtr)
        {
            this._target = target;
            this._methodPtr = methodPtr;
        }

        private void CtorRTClosed(Object target, IntPtr methodPtr)
        {
            this._target = target;
            this._methodPtr = AdjustTarget(target, methodPtr);
        }

        private void CtorOpened(Object target, IntPtr methodPtr, IntPtr shuffleThunk)
        {
            this._target = this;
            this._methodPtr = shuffleThunk;
            this._methodPtrAux = methodPtr;
        }

        private void CtorSecureClosed(Object target, IntPtr methodPtr, IntPtr callThunk, IntPtr creatorMethod)
        {
            MulticastDelegate realDelegate = (MulticastDelegate)Delegate.InternalAllocLike(this);
            realDelegate.CtorClosed(target, methodPtr);
            this._invocationList = realDelegate;
            this._target = this;
            this._methodPtr = callThunk;
            this._methodPtrAux = creatorMethod;
            this._invocationCount = GetInvokeMethod();
        }

        private void CtorSecureClosedStatic(Object target, IntPtr methodPtr, IntPtr callThunk, IntPtr creatorMethod)
        {
            MulticastDelegate realDelegate = (MulticastDelegate)Delegate.InternalAllocLike(this);
            realDelegate.CtorClosedStatic(target, methodPtr);
            this._invocationList = realDelegate;
            this._target = this;
            this._methodPtr = callThunk;
            this._methodPtrAux = creatorMethod;
            this._invocationCount = GetInvokeMethod();
        }

        private void CtorSecureRTClosed(Object target, IntPtr methodPtr, IntPtr callThunk, IntPtr creatorMethod)
        {
            MulticastDelegate realDelegate = Delegate.InternalAllocLike(this);
            realDelegate.CtorRTClosed(target, methodPtr);
            this._invocationList = realDelegate;
            this._target = this;
            this._methodPtr = callThunk;
            this._methodPtrAux = creatorMethod;
            this._invocationCount = GetInvokeMethod();
        }

        private void CtorSecureOpened(Object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr callThunk, IntPtr creatorMethod)
        {
            MulticastDelegate realDelegate = Delegate.InternalAllocLike(this);
            realDelegate.CtorOpened(target, methodPtr, shuffleThunk);
            this._invocationList = realDelegate;
            this._target = this;
            this._methodPtr = callThunk;
            this._methodPtrAux = creatorMethod;
            this._invocationCount = GetInvokeMethod();
        }

        private void CtorVirtualDispatch(Object target, IntPtr methodPtr, IntPtr shuffleThunk)
        {
            this._target = this;
            this._methodPtr = shuffleThunk;
            this._methodPtrAux = GetCallStub(methodPtr);
        }

        private void CtorSecureVirtualDispatch(Object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr callThunk, IntPtr creatorMethod)
        {
            MulticastDelegate realDelegate = Delegate.InternalAllocLike(this);
            realDelegate.CtorVirtualDispatch(target, methodPtr, shuffleThunk);
            this._invocationList = realDelegate;
            this._target = this;
            this._methodPtr = callThunk;
            this._methodPtrAux = creatorMethod;
            this._invocationCount = GetInvokeMethod();
        }

        private void CtorCollectibleClosedStatic(Object target, IntPtr methodPtr, IntPtr gchandle)
        {
            this._target = target;
            this._methodPtr = methodPtr;
            this._methodBase = System.Runtime.InteropServices.GCHandle.InternalGet(gchandle);
        }

        private void CtorCollectibleOpened(Object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
        {
            this._target = this;
            this._methodPtr = shuffleThunk;
            this._methodPtrAux = methodPtr;
            this._methodBase = System.Runtime.InteropServices.GCHandle.InternalGet(gchandle);
        }

        private void CtorCollectibleVirtualDispatch(Object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
        {
            this._target = this;
            this._methodPtr = shuffleThunk;
            this._methodPtrAux = GetCallStub(methodPtr);
            this._methodBase = System.Runtime.InteropServices.GCHandle.InternalGet(gchandle);
        }
    }
}