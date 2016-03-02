
using System.Security;

namespace System.Threading
{
    public sealed class AsyncLocal<T> : IAsyncLocal
    {
        private readonly Action<AsyncLocalValueChangedArgs<T>> m_valueChangedHandler;
        public AsyncLocal()
        {
        }

        public AsyncLocal(Action<AsyncLocalValueChangedArgs<T>> valueChangedHandler)
        {
            m_valueChangedHandler = valueChangedHandler;
        }

        public T Value
        {
            [SecuritySafeCritical]
            get
            {
                object obj = ExecutionContext.GetLocalValue(this);
                return (obj == null) ? default (T) : (T)obj;
            }

            [SecuritySafeCritical]
            set
            {
                ExecutionContext.SetLocalValue(this, value, m_valueChangedHandler != null);
            }
        }

        void IAsyncLocal.OnValueChanged(object previousValueObj, object currentValueObj, bool contextChanged)
        {
                        T previousValue = previousValueObj == null ? default (T) : (T)previousValueObj;
            T currentValue = currentValueObj == null ? default (T) : (T)currentValueObj;
            m_valueChangedHandler(new AsyncLocalValueChangedArgs<T>(previousValue, currentValue, contextChanged));
        }
    }

    internal interface IAsyncLocal
    {
        void OnValueChanged(object previousValue, object currentValue, bool contextChanged);
    }

    public struct AsyncLocalValueChangedArgs<T>
    {
        public T PreviousValue
        {
            get;
            private set;
        }

        public T CurrentValue
        {
            get;
            private set;
        }

        public bool ThreadContextChanged
        {
            get;
            private set;
        }

        internal AsyncLocalValueChangedArgs(T previousValue, T currentValue, bool contextChanged): this ()
        {
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            ThreadContextChanged = contextChanged;
        }
    }
}