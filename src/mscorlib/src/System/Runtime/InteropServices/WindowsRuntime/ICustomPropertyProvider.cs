using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.StubHelpers;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal static class ICustomPropertyProviderImpl
    {
        static internal ICustomProperty CreateProperty(object target, string propertyName)
        {
                                    IGetProxyTarget proxy = target as IGetProxyTarget;
            if (proxy != null)
                target = proxy.GetTarget();
            PropertyInfo propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            if (propertyInfo == null)
                return null;
            else
                return new CustomPropertyImpl(propertyInfo);
        }

        static internal unsafe ICustomProperty CreateIndexedProperty(object target, string propertyName, TypeNameNative*pIndexedParamType)
        {
                                    Type indexedParamType = null;
            SystemTypeMarshaler.ConvertToManaged(pIndexedParamType, ref indexedParamType);
            return CreateIndexedProperty(target, propertyName, indexedParamType);
        }

        static internal ICustomProperty CreateIndexedProperty(object target, string propertyName, Type indexedParamType)
        {
                                    IGetProxyTarget proxy = target as IGetProxyTarget;
            if (proxy != null)
                target = proxy.GetTarget();
            PropertyInfo propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, null, new Type[]{indexedParamType}, null);
            if (propertyInfo == null)
                return null;
            else
                return new CustomPropertyImpl(propertyInfo);
        }

        static internal unsafe void GetType(object target, TypeNameNative*pIndexedParamType)
        {
            IGetProxyTarget proxy = target as IGetProxyTarget;
            if (proxy != null)
                target = proxy.GetTarget();
            SystemTypeMarshaler.ConvertToNative(target.GetType(), pIndexedParamType);
        }
    }

    [Flags]
    enum InterfaceForwardingSupport
    {
        None = 0,
        IBindableVector = 0x1,
        IVector = 0x2,
        IBindableVectorView = 0x4,
        IVectorView = 0x8,
        IBindableIterableOrIIterable = 0x10
    }

    internal interface IGetProxyTarget
    {
        object GetTarget();
    }

    internal class ICustomPropertyProviderProxy<T1, T2> : IGetProxyTarget, ICustomQueryInterface, IEnumerable, IBindableVector, IBindableVectorView
    {
        private object _target;
        private InterfaceForwardingSupport _flags;
        internal ICustomPropertyProviderProxy(object target, InterfaceForwardingSupport flags)
        {
            _target = target;
            _flags = flags;
        }

        internal static object CreateInstance(object target)
        {
            InterfaceForwardingSupport supportFlags = InterfaceForwardingSupport.None;
            if (target as IList != null)
                supportFlags |= InterfaceForwardingSupport.IBindableVector;
            if (target as IList<T1> != null)
                supportFlags |= InterfaceForwardingSupport.IVector;
            if (target as IBindableVectorView != null)
                supportFlags |= InterfaceForwardingSupport.IBindableVectorView;
            if (target as IReadOnlyList<T2> != null)
                supportFlags |= InterfaceForwardingSupport.IVectorView;
            if (target as IEnumerable != null)
                supportFlags |= InterfaceForwardingSupport.IBindableIterableOrIIterable;
            return new ICustomPropertyProviderProxy<T1, T2>(target, supportFlags);
        }

        public override string ToString()
        {
            return WindowsRuntime.IStringableHelper.ToString(_target);
        }

        object IGetProxyTarget.GetTarget()
        {
            return _target;
        }

        public CustomQueryInterfaceResult GetInterface([In] ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            if (iid == typeof (IBindableIterable).GUID)
            {
                if ((_flags & (InterfaceForwardingSupport.IBindableIterableOrIIterable)) == 0)
                    return CustomQueryInterfaceResult.Failed;
            }

            if (iid == typeof (IBindableVector).GUID)
            {
                if ((_flags & (InterfaceForwardingSupport.IBindableVector | InterfaceForwardingSupport.IVector)) == 0)
                    return CustomQueryInterfaceResult.Failed;
            }

            if (iid == typeof (IBindableVectorView).GUID)
            {
                if ((_flags & (InterfaceForwardingSupport.IBindableVectorView | InterfaceForwardingSupport.IVectorView)) == 0)
                    return CustomQueryInterfaceResult.Failed;
            }

            return CustomQueryInterfaceResult.NotHandled;
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_target).GetEnumerator();
        }

        object IBindableVector.GetAt(uint index)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                return bindableVector.GetAt(index);
            }
            else
            {
                return GetVectorOfT().GetAt(index);
            }
        }

        uint IBindableVector.Size
        {
            get
            {
                IBindableVector bindableVector = GetIBindableVectorNoThrow();
                if (bindableVector != null)
                {
                    return bindableVector.Size;
                }
                else
                {
                    return GetVectorOfT().Size;
                }
            }
        }

        IBindableVectorView IBindableVector.GetView()
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                return bindableVector.GetView();
            }
            else
            {
                return new IVectorViewToIBindableVectorViewAdapter<T1>(GetVectorOfT().GetView());
            }
        }

        private sealed class IVectorViewToIBindableVectorViewAdapter<T> : IBindableVectorView
        {
            private IVectorView<T> _vectorView;
            public IVectorViewToIBindableVectorViewAdapter(IVectorView<T> vectorView)
            {
                this._vectorView = vectorView;
            }

            object IBindableVectorView.GetAt(uint index)
            {
                return _vectorView.GetAt(index);
            }

            uint IBindableVectorView.Size
            {
                get
                {
                    return _vectorView.Size;
                }
            }

            bool IBindableVectorView.IndexOf(object value, out uint index)
            {
                return _vectorView.IndexOf(ConvertTo<T>(value), out index);
            }

            IBindableIterator IBindableIterable.First()
            {
                return new IteratorOfTToIteratorAdapter<T>(_vectorView.First());
            }
        }

        bool IBindableVector.IndexOf(object value, out uint index)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                return bindableVector.IndexOf(value, out index);
            }
            else
            {
                return GetVectorOfT().IndexOf(ConvertTo<T1>(value), out index);
            }
        }

        void IBindableVector.SetAt(uint index, object value)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                bindableVector.SetAt(index, value);
            }
            else
            {
                GetVectorOfT().SetAt(index, ConvertTo<T1>(value));
            }
        }

        void IBindableVector.InsertAt(uint index, object value)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                bindableVector.InsertAt(index, value);
            }
            else
            {
                GetVectorOfT().InsertAt(index, ConvertTo<T1>(value));
            }
        }

        void IBindableVector.RemoveAt(uint index)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                bindableVector.RemoveAt(index);
            }
            else
            {
                GetVectorOfT().RemoveAt(index);
            }
        }

        void IBindableVector.Append(object value)
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                bindableVector.Append(value);
            }
            else
            {
                GetVectorOfT().Append(ConvertTo<T1>(value));
            }
        }

        void IBindableVector.RemoveAtEnd()
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                bindableVector.RemoveAtEnd();
            }
            else
            {
                GetVectorOfT().RemoveAtEnd();
            }
        }

        void IBindableVector.Clear()
        {
            IBindableVector bindableVector = GetIBindableVectorNoThrow();
            if (bindableVector != null)
            {
                bindableVector.Clear();
            }
            else
            {
                GetVectorOfT().Clear();
            }
        }

        private IBindableVector GetIBindableVectorNoThrow()
        {
            if ((_flags & InterfaceForwardingSupport.IBindableVector) != 0)
                return JitHelpers.UnsafeCast<IBindableVector>(_target);
            else
                return null;
        }

        private IVector_Raw<T1> GetVectorOfT()
        {
            if ((_flags & InterfaceForwardingSupport.IVector) != 0)
                return JitHelpers.UnsafeCast<IVector_Raw<T1>>(_target);
            else
                throw new InvalidOperationException();
        }

        object IBindableVectorView.GetAt(uint index)
        {
            IBindableVectorView bindableVectorView = GetIBindableVectorViewNoThrow();
            if (bindableVectorView != null)
                return bindableVectorView.GetAt(index);
            else
                return GetVectorViewOfT().GetAt(index);
        }

        uint IBindableVectorView.Size
        {
            get
            {
                IBindableVectorView bindableVectorView = GetIBindableVectorViewNoThrow();
                if (bindableVectorView != null)
                    return bindableVectorView.Size;
                else
                    return GetVectorViewOfT().Size;
            }
        }

        bool IBindableVectorView.IndexOf(object value, out uint index)
        {
            IBindableVectorView bindableVectorView = GetIBindableVectorViewNoThrow();
            if (bindableVectorView != null)
                return bindableVectorView.IndexOf(value, out index);
            else
                return GetVectorViewOfT().IndexOf(ConvertTo<T2>(value), out index);
        }

        IBindableIterator IBindableIterable.First()
        {
            IBindableVectorView bindableVectorView = GetIBindableVectorViewNoThrow();
            if (bindableVectorView != null)
                return bindableVectorView.First();
            else
                return new IteratorOfTToIteratorAdapter<T2>(GetVectorViewOfT().First());
        }

        private sealed class IteratorOfTToIteratorAdapter<T> : IBindableIterator
        {
            private IIterator<T> _iterator;
            public IteratorOfTToIteratorAdapter(IIterator<T> iterator)
            {
                this._iterator = iterator;
            }

            public bool HasCurrent
            {
                get
                {
                    return _iterator.HasCurrent;
                }
            }

            public object Current
            {
                get
                {
                    return (object)_iterator.Current;
                }
            }

            public bool MoveNext()
            {
                return _iterator.MoveNext();
            }
        }

        private IBindableVectorView GetIBindableVectorViewNoThrow()
        {
            if ((_flags & InterfaceForwardingSupport.IBindableVectorView) != 0)
                return JitHelpers.UnsafeCast<IBindableVectorView>(_target);
            else
                return null;
        }

        private IVectorView<T2> GetVectorViewOfT()
        {
            if ((_flags & InterfaceForwardingSupport.IVectorView) != 0)
                return JitHelpers.UnsafeCast<IVectorView<T2>>(_target);
            else
                throw new InvalidOperationException();
        }

        private static T ConvertTo<T>(object value)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);
            return (T)value;
        }
    }
}