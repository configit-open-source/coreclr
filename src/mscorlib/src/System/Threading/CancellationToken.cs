using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Diagnostics.Contracts;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading
{
    public struct CancellationToken
    {
        private CancellationTokenSource m_source;
        public static CancellationToken None
        {
            get
            {
                return default (CancellationToken);
            }
        }

        public bool IsCancellationRequested
        {
            get
            {
                return m_source != null && m_source.IsCancellationRequested;
            }
        }

        public bool CanBeCanceled
        {
            get
            {
                return m_source != null && m_source.CanBeCanceled;
            }
        }

        public WaitHandle WaitHandle
        {
            get
            {
                if (m_source == null)
                {
                    InitializeDefaultSource();
                }

                return m_source.WaitHandle;
            }
        }

        internal CancellationToken(CancellationTokenSource source)
        {
            m_source = source;
        }

        public CancellationToken(bool canceled): this ()
        {
            if (canceled)
                m_source = CancellationTokenSource.InternalGetStaticSource(canceled);
        }

        private readonly static Action<Object> s_ActionToActionObjShunt = new Action<Object>(ActionToActionObjShunt);
        private static void ActionToActionObjShunt(object obj)
        {
            Action action = obj as Action;
            Contract.Assert(action != null, "Expected an Action here");
            action();
        }

        public CancellationTokenRegistration Register(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            return Register(s_ActionToActionObjShunt, callback, false, true);
        }

        public CancellationTokenRegistration Register(Action callback, bool useSynchronizationContext)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            return Register(s_ActionToActionObjShunt, callback, useSynchronizationContext, true);
        }

        public CancellationTokenRegistration Register(Action<Object> callback, Object state)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            return Register(callback, state, false, true);
        }

        public CancellationTokenRegistration Register(Action<Object> callback, Object state, bool useSynchronizationContext)
        {
            return Register(callback, state, useSynchronizationContext, true);
        }

        internal CancellationTokenRegistration InternalRegisterWithoutEC(Action<object> callback, Object state)
        {
            return Register(callback, state, false, false);
        }

        private CancellationTokenRegistration Register(Action<Object> callback, Object state, bool useSynchronizationContext, bool useExecutionContext)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            if (callback == null)
                throw new ArgumentNullException("callback");
            if (CanBeCanceled == false)
            {
                return new CancellationTokenRegistration();
            }

            SynchronizationContext capturedSyncContext = null;
            ExecutionContext capturedExecutionContext = null;
            if (!IsCancellationRequested)
            {
                if (useSynchronizationContext)
                    capturedSyncContext = SynchronizationContext.Current;
                if (useExecutionContext)
                    capturedExecutionContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.OptimizeDefaultCase);
            }

            return m_source.InternalRegister(callback, state, capturedSyncContext, capturedExecutionContext);
        }

        public bool Equals(CancellationToken other)
        {
            if (m_source == null && other.m_source == null)
            {
                return true;
            }

            if (m_source == null)
            {
                return other.m_source == CancellationTokenSource.InternalGetStaticSource(false);
            }

            if (other.m_source == null)
            {
                return m_source == CancellationTokenSource.InternalGetStaticSource(false);
            }

            return m_source == other.m_source;
        }

        public override bool Equals(Object other)
        {
            if (other is CancellationToken)
            {
                return Equals((CancellationToken)other);
            }

            return false;
        }

        public override Int32 GetHashCode()
        {
            if (m_source == null)
            {
                return CancellationTokenSource.InternalGetStaticSource(false).GetHashCode();
            }

            return m_source.GetHashCode();
        }

        public static bool operator ==(CancellationToken left, CancellationToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CancellationToken left, CancellationToken right)
        {
            return !left.Equals(right);
        }

        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
                ThrowOperationCanceledException();
        }

        internal void ThrowIfSourceDisposed()
        {
            if ((m_source != null) && m_source.IsDisposed)
                ThrowObjectDisposedException();
        }

        private void ThrowOperationCanceledException()
        {
            throw new OperationCanceledException(Environment.GetResourceString("OperationCanceled"), this);
        }

        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(null, Environment.GetResourceString("CancellationToken_SourceDisposed"));
        }

        private void InitializeDefaultSource()
        {
            m_source = CancellationTokenSource.InternalGetStaticSource(false);
        }
    }
}