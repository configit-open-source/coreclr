using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal delegate IEnumerator<T> GetEnumerator_Delegate<out T>();
    internal sealed class IterableToEnumerableAdapter
    {
        private IterableToEnumerableAdapter()
        {
                    }

        internal IEnumerator<T> GetEnumerator_Stub<T>()
        {
            IIterable<T> _this = JitHelpers.UnsafeCast<IIterable<T>>(this);
            return new IteratorToEnumeratorAdapter<T>(_this.First());
        }

        internal IEnumerator<T> GetEnumerator_Variance_Stub<T>()where T : class
        {
            bool fUseString;
            Delegate target = System.StubHelpers.StubHelpers.GetTargetForAmbiguousVariantCall(this, typeof (IEnumerable<T>).TypeHandle.Value, out fUseString);
            if (target != null)
            {
                return (JitHelpers.UnsafeCast<GetEnumerator_Delegate<T>>(target))();
            }

            if (fUseString)
            {
                return JitHelpers.UnsafeCast<IEnumerator<T>>(GetEnumerator_Stub<string>());
            }

            return GetEnumerator_Stub<T>();
        }
    }

    internal sealed class BindableIterableToEnumerableAdapter
    {
        private BindableIterableToEnumerableAdapter()
        {
                    }

        private sealed class NonGenericToGenericIterator : IIterator<object>
        {
            private IBindableIterator iterator;
            public NonGenericToGenericIterator(IBindableIterator iterator)
            {
                this.iterator = iterator;
            }

            public object Current
            {
                get
                {
                    return iterator.Current;
                }
            }

            public bool HasCurrent
            {
                get
                {
                    return iterator.HasCurrent;
                }
            }

            public bool MoveNext()
            {
                return iterator.MoveNext();
            }

            public int GetMany(object[] items)
            {
                throw new NotSupportedException();
            }
        }

        internal IEnumerator GetEnumerator_Stub()
        {
            IBindableIterable _this = JitHelpers.UnsafeCast<IBindableIterable>(this);
            return new IteratorToEnumeratorAdapter<object>(new NonGenericToGenericIterator(_this.First()));
        }
    }

    internal sealed class IteratorToEnumeratorAdapter<T> : IEnumerator<T>
    {
        private IIterator<T> m_iterator;
        private bool m_hadCurrent;
        private T m_current;
        private bool m_isInitialized;
        internal IteratorToEnumeratorAdapter(IIterator<T> iterator)
        {
                        m_iterator = iterator;
            m_hadCurrent = true;
            m_isInitialized = false;
        }

        public T Current
        {
            get
            {
                if (!m_isInitialized)
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumNotStarted);
                if (!m_hadCurrent)
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumEnded);
                return m_current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                if (!m_isInitialized)
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumNotStarted);
                if (!m_hadCurrent)
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumEnded);
                return m_current;
            }
        }

        public bool MoveNext()
        {
            if (!m_hadCurrent)
            {
                return false;
            }

            try
            {
                if (!m_isInitialized)
                {
                    m_hadCurrent = m_iterator.HasCurrent;
                    m_isInitialized = true;
                }
                else
                {
                    m_hadCurrent = m_iterator.MoveNext();
                }

                if (m_hadCurrent)
                {
                    m_current = m_iterator.Current;
                }
            }
            catch (Exception e)
            {
                if (Marshal.GetHRForException(e) == __HResults.E_CHANGED_STATE)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }
                else
                {
                    throw;
                }
            }

            return m_hadCurrent;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}