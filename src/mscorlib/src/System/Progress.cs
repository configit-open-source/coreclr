
using System.Threading;

namespace System
{
    public class Progress<T> : IProgress<T>
    {
        private readonly SynchronizationContext m_synchronizationContext;
        private readonly Action<T> m_handler;
        private readonly SendOrPostCallback m_invokeHandlers;
        public Progress()
        {
            m_synchronizationContext = SynchronizationContext.CurrentNoFlow ?? ProgressStatics.DefaultContext;
                        m_invokeHandlers = new SendOrPostCallback(InvokeHandlers);
        }

        public Progress(Action<T> handler): this ()
        {
            if (handler == null)
                throw new ArgumentNullException("handler");
            m_handler = handler;
        }

        public event EventHandler<T> ProgressChanged;
        protected virtual void OnReport(T value)
        {
            Action<T> handler = m_handler;
            EventHandler<T> changedEvent = ProgressChanged;
            if (handler != null || changedEvent != null)
            {
                m_synchronizationContext.Post(m_invokeHandlers, value);
            }
        }

        void IProgress<T>.Report(T value)
        {
            OnReport(value);
        }

        private void InvokeHandlers(object state)
        {
            T value = (T)state;
            Action<T> handler = m_handler;
            EventHandler<T> changedEvent = ProgressChanged;
            if (handler != null)
                handler(value);
            if (changedEvent != null)
                changedEvent(this, value);
        }
    }

    internal static class ProgressStatics
    {
        internal static readonly SynchronizationContext DefaultContext = new SynchronizationContext();
    }
}