using System.Runtime;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Runtime.ExceptionServices;

namespace System
{
    internal static class LazyHelpers
    {
        internal static readonly object PUBLICATION_ONLY_SENTINEL = new object ();
    }

    public class Lazy<T>
    {
        class Boxed
        {
            internal Boxed(T value)
            {
                m_value = value;
            }

            internal T m_value;
        }

        class LazyInternalExceptionHolder
        {
            internal ExceptionDispatchInfo m_edi;
            internal LazyInternalExceptionHolder(Exception ex)
            {
                m_edi = ExceptionDispatchInfo.Capture(ex);
            }
        }

        static readonly Func<T> ALREADY_INVOKED_SENTINEL = delegate
        {
            Contract.Assert(false, "ALREADY_INVOKED_SENTINEL should never be invoked.");
            return default (T);
        }

        ;
        private object m_boxed;
        private Func<T> m_valueFactory;
        private object m_threadSafeObj;
        public Lazy(): this (LazyThreadSafetyMode.ExecutionAndPublication)
        {
        }

        public Lazy(Func<T> valueFactory): this (valueFactory, LazyThreadSafetyMode.ExecutionAndPublication)
        {
        }

        public Lazy(bool isThreadSafe): this (isThreadSafe ? LazyThreadSafetyMode.ExecutionAndPublication : LazyThreadSafetyMode.None)
        {
        }

        public Lazy(LazyThreadSafetyMode mode)
        {
            m_threadSafeObj = GetObjectFromMode(mode);
        }

        public Lazy(Func<T> valueFactory, bool isThreadSafe): this (valueFactory, isThreadSafe ? LazyThreadSafetyMode.ExecutionAndPublication : LazyThreadSafetyMode.None)
        {
        }

        public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
        {
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");
            m_threadSafeObj = GetObjectFromMode(mode);
            m_valueFactory = valueFactory;
        }

        private static object GetObjectFromMode(LazyThreadSafetyMode mode)
        {
            if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
                return new object ();
            else if (mode == LazyThreadSafetyMode.PublicationOnly)
                return LazyHelpers.PUBLICATION_ONLY_SENTINEL;
            else if (mode != LazyThreadSafetyMode.None)
                throw new ArgumentOutOfRangeException("mode", Environment.GetResourceString("Lazy_ctor_ModeInvalid"));
            return null;
        }

        private void OnSerializing(StreamingContext context)
        {
            T dummy = Value;
        }

        public override string ToString()
        {
            return IsValueCreated ? Value.ToString() : Environment.GetResourceString("Lazy_ToString_ValueNotCreated");
        }

        internal T ValueForDebugDisplay
        {
            get
            {
                if (!IsValueCreated)
                {
                    return default (T);
                }

                return ((Boxed)m_boxed).m_value;
            }
        }

        internal LazyThreadSafetyMode Mode
        {
            get
            {
                if (m_threadSafeObj == null)
                    return LazyThreadSafetyMode.None;
                if (m_threadSafeObj == (object)LazyHelpers.PUBLICATION_ONLY_SENTINEL)
                    return LazyThreadSafetyMode.PublicationOnly;
                return LazyThreadSafetyMode.ExecutionAndPublication;
            }
        }

        internal bool IsValueFaulted
        {
            get
            {
                return m_boxed is LazyInternalExceptionHolder;
            }
        }

        public bool IsValueCreated
        {
            get
            {
                return m_boxed != null && m_boxed is Boxed;
            }
        }

        public T Value
        {
            get
            {
                Boxed boxed = null;
                if (m_boxed != null)
                {
                    boxed = m_boxed as Boxed;
                    if (boxed != null)
                    {
                        return boxed.m_value;
                    }

                    LazyInternalExceptionHolder exc = m_boxed as LazyInternalExceptionHolder;
                    Contract.Assert(exc != null);
                    exc.m_edi.Throw();
                }

                return LazyInitValue();
            }
        }

        private T LazyInitValue()
        {
            Boxed boxed = null;
            LazyThreadSafetyMode mode = Mode;
            if (mode == LazyThreadSafetyMode.None)
            {
                boxed = CreateValue();
                m_boxed = boxed;
            }
            else if (mode == LazyThreadSafetyMode.PublicationOnly)
            {
                boxed = CreateValue();
                if (boxed == null || Interlocked.CompareExchange(ref m_boxed, boxed, null) != null)
                {
                    boxed = (Boxed)m_boxed;
                }
                else
                {
                    m_valueFactory = ALREADY_INVOKED_SENTINEL;
                }
            }
            else
            {
                object threadSafeObj = Volatile.Read(ref m_threadSafeObj);
                bool lockTaken = false;
                try
                {
                    if (threadSafeObj != (object)ALREADY_INVOKED_SENTINEL)
                        Monitor.Enter(threadSafeObj, ref lockTaken);
                    else
                        Contract.Assert(m_boxed != null);
                    if (m_boxed == null)
                    {
                        boxed = CreateValue();
                        m_boxed = boxed;
                        Volatile.Write(ref m_threadSafeObj, ALREADY_INVOKED_SENTINEL);
                    }
                    else
                    {
                        boxed = m_boxed as Boxed;
                        if (boxed == null)
                        {
                            LazyInternalExceptionHolder exHolder = m_boxed as LazyInternalExceptionHolder;
                            Contract.Assert(exHolder != null);
                            exHolder.m_edi.Throw();
                        }
                    }
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(threadSafeObj);
                }
            }

            Contract.Assert(boxed != null);
            return boxed.m_value;
        }

        private Boxed CreateValue()
        {
            Boxed boxed = null;
            LazyThreadSafetyMode mode = Mode;
            if (m_valueFactory != null)
            {
                try
                {
                    if (mode != LazyThreadSafetyMode.PublicationOnly && m_valueFactory == ALREADY_INVOKED_SENTINEL)
                        throw new InvalidOperationException(Environment.GetResourceString("Lazy_Value_RecursiveCallsToValue"));
                    Func<T> factory = m_valueFactory;
                    if (mode != LazyThreadSafetyMode.PublicationOnly)
                    {
                        m_valueFactory = ALREADY_INVOKED_SENTINEL;
                    }
                    else if (factory == ALREADY_INVOKED_SENTINEL)
                    {
                        return null;
                    }

                    boxed = new Boxed(factory());
                }
                catch (Exception ex)
                {
                    if (mode != LazyThreadSafetyMode.PublicationOnly)
                        m_boxed = new LazyInternalExceptionHolder(ex);
                    throw;
                }
            }
            else
            {
                try
                {
                    boxed = new Boxed((T)Activator.CreateInstance(typeof (T)));
                }
                catch (System.MissingMethodException)
                {
                    Exception ex = new System.MissingMemberException(Environment.GetResourceString("Lazy_CreateValue_NoParameterlessCtorForT"));
                    if (mode != LazyThreadSafetyMode.PublicationOnly)
                        m_boxed = new LazyInternalExceptionHolder(ex);
                    throw ex;
                }
            }

            return boxed;
        }
    }

    internal sealed class System_LazyDebugView<T>
    {
        private readonly Lazy<T> m_lazy;
        public System_LazyDebugView(Lazy<T> lazy)
        {
            m_lazy = lazy;
        }

        public bool IsValueCreated
        {
            get
            {
                return m_lazy.IsValueCreated;
            }
        }

        public T Value
        {
            get
            {
                return m_lazy.ValueForDebugDisplay;
            }
        }

        public LazyThreadSafetyMode Mode
        {
            get
            {
                return m_lazy.Mode;
            }
        }

        public bool IsValueFaulted
        {
            get
            {
                return m_lazy.IsValueFaulted;
            }
        }
    }
}