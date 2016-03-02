using System.Collections.Generic;
using System.Security;

namespace System.Threading
{
    public delegate void ContextCallback(Object state);
    internal struct ExecutionContextSwitcher
    {
        internal ExecutionContext m_ec;
        internal SynchronizationContext m_sc;
        internal void Undo()
        {
            SynchronizationContext.SetSynchronizationContext(m_sc);
            ExecutionContext.Restore(m_ec);
        }
    }

    public sealed class ExecutionContext : IDisposable
    {
        private static readonly ExecutionContext Default = new ExecutionContext();
        static ExecutionContext t_currentMaybeNull;
        private readonly Dictionary<IAsyncLocal, object> m_localValues;
        private readonly IAsyncLocal[] m_localChangeNotifications;
        private ExecutionContext()
        {
            m_localValues = new Dictionary<IAsyncLocal, object>();
            m_localChangeNotifications = Array.Empty<IAsyncLocal>();
        }

        private ExecutionContext(Dictionary<IAsyncLocal, object> localValues, IAsyncLocal[] localChangeNotifications)
        {
            m_localValues = localValues;
            m_localChangeNotifications = localChangeNotifications;
        }

        public static ExecutionContext Capture()
        {
            return t_currentMaybeNull ?? ExecutionContext.Default;
        }

        public static void Run(ExecutionContext executionContext, ContextCallback callback, Object state)
        {
            ExecutionContextSwitcher ecsw = default (ExecutionContextSwitcher);
            try
            {
                EstablishCopyOnWriteScope(ref ecsw);
                ExecutionContext.Restore(executionContext);
                callback(state);
            }
            catch
            {
                ecsw.Undo();
                throw;
            }

            ecsw.Undo();
        }

        internal static void Restore(ExecutionContext executionContext)
        {
            if (executionContext == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullContext"));
            ExecutionContext previous = t_currentMaybeNull ?? Default;
            t_currentMaybeNull = executionContext;
            if (previous != executionContext)
                OnContextChanged(previous, executionContext);
        }

        static internal void EstablishCopyOnWriteScope(ref ExecutionContextSwitcher ecsw)
        {
            ecsw.m_ec = Capture();
            ecsw.m_sc = SynchronizationContext.CurrentNoFlow;
        }

        private static void OnContextChanged(ExecutionContext previous, ExecutionContext current)
        {
            previous = previous ?? Default;
            foreach (IAsyncLocal local in previous.m_localChangeNotifications)
            {
                object previousValue;
                object currentValue;
                previous.m_localValues.TryGetValue(local, out previousValue);
                current.m_localValues.TryGetValue(local, out currentValue);
                if (previousValue != currentValue)
                    local.OnValueChanged(previousValue, currentValue, true);
            }

            if (current.m_localChangeNotifications != previous.m_localChangeNotifications)
            {
                try
                {
                    foreach (IAsyncLocal local in current.m_localChangeNotifications)
                    {
                        object previousValue;
                        if (!previous.m_localValues.TryGetValue(local, out previousValue))
                        {
                            object currentValue;
                            current.m_localValues.TryGetValue(local, out currentValue);
                            if (previousValue != currentValue)
                                local.OnValueChanged(previousValue, currentValue, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Environment.FailFast(Environment.GetResourceString("ExecutionContext_ExceptionInAsyncLocalNotification"), ex);
                }
            }
        }

        internal static object GetLocalValue(IAsyncLocal local)
        {
            ExecutionContext current = t_currentMaybeNull;
            if (current == null)
                return null;
            object value;
            current.m_localValues.TryGetValue(local, out value);
            return value;
        }

        internal static void SetLocalValue(IAsyncLocal local, object newValue, bool needChangeNotifications)
        {
            ExecutionContext current = t_currentMaybeNull ?? ExecutionContext.Default;
            object previousValue;
            bool hadPreviousValue = current.m_localValues.TryGetValue(local, out previousValue);
            if (previousValue == newValue)
                return;
            Dictionary<IAsyncLocal, object> newValues = new Dictionary<IAsyncLocal, object>(current.m_localValues.Count + (hadPreviousValue ? 0 : 1));
            foreach (KeyValuePair<IAsyncLocal, object> pair in current.m_localValues)
                newValues.Add(pair.Key, pair.Value);
            newValues[local] = newValue;
            IAsyncLocal[] newChangeNotifications = current.m_localChangeNotifications;
            if (needChangeNotifications)
            {
                if (hadPreviousValue)
                {
                                    }
                else
                {
                    int newNotificationIndex = newChangeNotifications.Length;
                    Array.Resize(ref newChangeNotifications, newNotificationIndex + 1);
                    newChangeNotifications[newNotificationIndex] = local;
                }
            }

            t_currentMaybeNull = new ExecutionContext(newValues, newChangeNotifications);
            if (needChangeNotifications)
            {
                local.OnValueChanged(previousValue, newValue, false);
            }
        }

        [Flags]
        internal enum CaptureOptions
        {
            None = 0x00,
            IgnoreSyncCtx = 0x01,
            OptimizeDefaultCase = 0x02
        }

        internal static ExecutionContext Capture(ref StackCrawlMark stackMark, CaptureOptions captureOptions)
        {
            return Capture();
        }

        internal static ExecutionContext FastCapture()
        {
            return Capture();
        }

        internal static void Run(ExecutionContext executionContext, ContextCallback callback, Object state, bool preserveSyncCtx)
        {
            Run(executionContext, callback, state);
        }

        internal bool IsDefaultFTContext(bool ignoreSyncCtx)
        {
            return this == Default;
        }

        public ExecutionContext CreateCopy()
        {
            return this;
        }

        public void Dispose()
        {
        }

        public static bool IsFlowSuppressed()
        {
            return false;
        }

        internal static ExecutionContext PreAllocatedDefault
        {
            [SecuritySafeCritical]
            get
            {
                return ExecutionContext.Default;
            }
        }

        internal bool IsPreAllocatedDefault
        {
            get
            {
                return this == ExecutionContext.Default;
            }
        }
    }
}