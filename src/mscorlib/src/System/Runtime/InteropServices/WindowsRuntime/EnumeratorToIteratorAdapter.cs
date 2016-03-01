using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class EnumerableToIterableAdapter
    {
        private EnumerableToIterableAdapter()
        {
                    }

        internal IIterator<T> First_Stub<T>()
        {
            IEnumerable<T> _this = JitHelpers.UnsafeCast<IEnumerable<T>>(this);
            return new EnumeratorToIteratorAdapter<T>(_this.GetEnumerator());
        }
    }

    internal sealed class EnumerableToBindableIterableAdapter
    {
        private EnumerableToBindableIterableAdapter()
        {
                    }

        internal sealed class NonGenericToGenericEnumerator : IEnumerator<object>
        {
            private IEnumerator enumerator;
            public NonGenericToGenericEnumerator(IEnumerator enumerator)
            {
                this.enumerator = enumerator;
            }

            public object Current
            {
                get
                {
                    return enumerator.Current;
                }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }

            public void Dispose()
            {
            }
        }

        internal IBindableIterator First_Stub()
        {
            IEnumerable _this = JitHelpers.UnsafeCast<IEnumerable>(this);
            return new EnumeratorToIteratorAdapter<object>(new NonGenericToGenericEnumerator(_this.GetEnumerator()));
        }
    }

    internal sealed class EnumeratorToIteratorAdapter<T> : IIterator<T>, IBindableIterator
    {
        private IEnumerator<T> m_enumerator;
        private bool m_firstItem = true;
        private bool m_hasCurrent;
        internal EnumeratorToIteratorAdapter(IEnumerator<T> enumerator)
        {
                        m_enumerator = enumerator;
        }

        public T Current
        {
            get
            {
                if (m_firstItem)
                {
                    m_firstItem = false;
                    MoveNext();
                }

                if (!m_hasCurrent)
                {
                    throw WindowsRuntimeMarshal.GetExceptionForHR(__HResults.E_BOUNDS, null);
                }

                return m_enumerator.Current;
            }
        }

        object IBindableIterator.Current
        {
            get
            {
                return (object)((IIterator<T>)this).Current;
            }
        }

        public bool HasCurrent
        {
            get
            {
                if (m_firstItem)
                {
                    m_firstItem = false;
                    MoveNext();
                }

                return m_hasCurrent;
            }
        }

        public bool MoveNext()
        {
            try
            {
                m_hasCurrent = m_enumerator.MoveNext();
            }
            catch (InvalidOperationException e)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(__HResults.E_CHANGED_STATE, e);
            }

            return m_hasCurrent;
        }

        public int GetMany(T[] items)
        {
            if (items == null)
            {
                return 0;
            }

            int index = 0;
            while (index < items.Length && HasCurrent)
            {
                items[index] = Current;
                MoveNext();
                ++index;
            }

            if (typeof (T) == typeof (string))
            {
                string[] stringItems = items as string[];
                for (int i = index; i < items.Length; ++i)
                    stringItems[i] = String.Empty;
            }

            return index;
        }
    }
}