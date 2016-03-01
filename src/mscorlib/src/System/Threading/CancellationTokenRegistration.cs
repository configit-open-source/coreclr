namespace System.Threading
{
    public struct CancellationTokenRegistration : IEquatable<CancellationTokenRegistration>, IDisposable
    {
        private readonly CancellationCallbackInfo m_callbackInfo;
        private readonly SparselyPopulatedArrayAddInfo<CancellationCallbackInfo> m_registrationInfo;
        internal CancellationTokenRegistration(CancellationCallbackInfo callbackInfo, SparselyPopulatedArrayAddInfo<CancellationCallbackInfo> registrationInfo)
        {
            m_callbackInfo = callbackInfo;
            m_registrationInfo = registrationInfo;
        }

        internal bool TryDeregister()
        {
            if (m_registrationInfo.Source == null)
                return false;
            CancellationCallbackInfo prevailingCallbackInfoInSlot = m_registrationInfo.Source.SafeAtomicRemove(m_registrationInfo.Index, m_callbackInfo);
            if (prevailingCallbackInfoInSlot != m_callbackInfo)
                return false;
            return true;
        }

        public void Dispose()
        {
            bool deregisterOccurred = TryDeregister();
            var callbackInfo = m_callbackInfo;
            if (callbackInfo != null)
            {
                var tokenSource = callbackInfo.CancellationTokenSource;
                if (tokenSource.IsCancellationRequested && !tokenSource.IsCancellationCompleted && !deregisterOccurred && tokenSource.ThreadIDExecutingCallbacks != Thread.CurrentThread.ManagedThreadId)
                {
                    tokenSource.WaitForCallbackToComplete(m_callbackInfo);
                }
            }
        }

        public static bool operator ==(CancellationTokenRegistration left, CancellationTokenRegistration right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CancellationTokenRegistration left, CancellationTokenRegistration right)
        {
            return !left.Equals(right);
        }

        public override bool Equals(object obj)
        {
            return ((obj is CancellationTokenRegistration) && Equals((CancellationTokenRegistration)obj));
        }

        public bool Equals(CancellationTokenRegistration other)
        {
            return m_callbackInfo == other.m_callbackInfo && m_registrationInfo.Source == other.m_registrationInfo.Source && m_registrationInfo.Index == other.m_registrationInfo.Index;
        }

        public override int GetHashCode()
        {
            if (m_registrationInfo.Source != null)
                return m_registrationInfo.Source.GetHashCode() ^ m_registrationInfo.Index.GetHashCode();
            return m_registrationInfo.Index.GetHashCode();
        }
    }
}