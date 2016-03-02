using System.Collections.Generic;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    public sealed class EventRegistrationTokenTable<T>
        where T : class
    {
        private Dictionary<EventRegistrationToken, T> m_tokens = new Dictionary<EventRegistrationToken, T>();
        private volatile T m_invokeList;
        public EventRegistrationTokenTable()
        {
            if (!typeof (Delegate).IsAssignableFrom(typeof (T)))
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EventTokenTableRequiresDelegate", typeof (T)));
            }
        }

        public T InvocationList
        {
            get
            {
                return m_invokeList;
            }

            set
            {
                lock (m_tokens)
                {
                    m_tokens.Clear();
                    m_invokeList = null;
                    if (value != null)
                    {
                        AddEventHandlerNoLock(value);
                    }
                }
            }
        }

        public EventRegistrationToken AddEventHandler(T handler)
        {
            if (handler == null)
            {
                return new EventRegistrationToken(0);
            }

            lock (m_tokens)
            {
                return AddEventHandlerNoLock(handler);
            }
        }

        private EventRegistrationToken AddEventHandlerNoLock(T handler)
        {
                        EventRegistrationToken token = GetPreferredToken(handler);
            while (m_tokens.ContainsKey(token))
            {
                token = new EventRegistrationToken(token.Value + 1);
            }

            m_tokens[token] = handler;
            Delegate invokeList = (Delegate)(object)m_invokeList;
            invokeList = MulticastDelegate.Combine(invokeList, (Delegate)(object)handler);
            m_invokeList = (T)(object)invokeList;
            return token;
        }

        internal T ExtractHandler(EventRegistrationToken token)
        {
            T handler = null;
            lock (m_tokens)
            {
                if (m_tokens.TryGetValue(token, out handler))
                {
                    RemoveEventHandlerNoLock(token);
                }
            }

            return handler;
        }

        private static EventRegistrationToken GetPreferredToken(T handler)
        {
                        uint handlerHashCode = 0;
            Delegate[] invocationList = ((Delegate)(object)handler).GetInvocationList();
            if (invocationList.Length == 1)
            {
                handlerHashCode = (uint)invocationList[0].Method.GetHashCode();
            }
            else
            {
                handlerHashCode = (uint)handler.GetHashCode();
            }

            ulong tokenValue = ((ulong)(uint)typeof (T).MetadataToken << 32) | handlerHashCode;
            return new EventRegistrationToken(tokenValue);
        }

        public void RemoveEventHandler(EventRegistrationToken token)
        {
            if (token.Value == 0)
            {
                return;
            }

            lock (m_tokens)
            {
                RemoveEventHandlerNoLock(token);
            }
        }

        public void RemoveEventHandler(T handler)
        {
            if (handler == null)
            {
                return;
            }

            lock (m_tokens)
            {
                EventRegistrationToken preferredToken = GetPreferredToken(handler);
                T registeredHandler;
                if (m_tokens.TryGetValue(preferredToken, out registeredHandler))
                {
                    if (registeredHandler == handler)
                    {
                        RemoveEventHandlerNoLock(preferredToken);
                        return;
                    }
                }

                foreach (KeyValuePair<EventRegistrationToken, T> registration in m_tokens)
                {
                    if (registration.Value == (T)(object)handler)
                    {
                        RemoveEventHandlerNoLock(registration.Key);
                        return;
                    }
                }
            }
        }

        private void RemoveEventHandlerNoLock(EventRegistrationToken token)
        {
            T handler;
            if (m_tokens.TryGetValue(token, out handler))
            {
                m_tokens.Remove(token);
                Delegate invokeList = (Delegate)(object)m_invokeList;
                invokeList = MulticastDelegate.Remove(invokeList, (Delegate)(object)handler);
                m_invokeList = (T)(object)invokeList;
            }
        }

        public static EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref EventRegistrationTokenTable<T> refEventTable)
        {
            if (refEventTable == null)
            {
                Interlocked.CompareExchange(ref refEventTable, new EventRegistrationTokenTable<T>(), null);
            }

            return refEventTable;
        }
    }
}