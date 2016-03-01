using System.Diagnostics.Contracts;
using System.Runtime.ConstrainedExecution;

namespace System.Threading
{
    public struct SpinLock
    {
        private volatile int m_owner;
        private const int SPINNING_FACTOR = 100;
        private const int SLEEP_ONE_FREQUENCY = 40;
        private const int SLEEP_ZERO_FREQUENCY = 10;
        private const int TIMEOUT_CHECK_FREQUENCY = 10;
        private const int LOCK_ID_DISABLE_MASK = unchecked ((int)0x80000000);
        private const int LOCK_ANONYMOUS_OWNED = 0x1;
        private const int WAITERS_MASK = ~(LOCK_ID_DISABLE_MASK | 1);
        private const int ID_DISABLED_AND_ANONYMOUS_OWNED = unchecked ((int)0x80000001);
        private const int LOCK_UNOWNED = 0;
        private static int MAXIMUM_WAITERS = WAITERS_MASK;
        public SpinLock(bool enableThreadOwnerTracking)
        {
            m_owner = LOCK_UNOWNED;
            if (!enableThreadOwnerTracking)
            {
                m_owner |= LOCK_ID_DISABLE_MASK;
                Contract.Assert(!IsThreadOwnerTrackingEnabled, "property should be false by now");
            }
        }

        public void Enter(ref bool lockTaken)
        {
            int observedOwner = m_owner;
            if (lockTaken || (observedOwner & ID_DISABLED_AND_ANONYMOUS_OWNED) != LOCK_ID_DISABLE_MASK || Interlocked.CompareExchange(ref m_owner, observedOwner | LOCK_ANONYMOUS_OWNED, observedOwner, ref lockTaken) != observedOwner)
                ContinueTryEnter(Timeout.Infinite, ref lockTaken);
        }

        public void TryEnter(ref bool lockTaken)
        {
            TryEnter(0, ref lockTaken);
        }

        public void TryEnter(TimeSpan timeout, ref bool lockTaken)
        {
            Int64 totalMilliseconds = (Int64)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("timeout", timeout, Environment.GetResourceString("SpinLock_TryEnter_ArgumentOutOfRange"));
            }

            TryEnter((int)timeout.TotalMilliseconds, ref lockTaken);
        }

        public void TryEnter(int millisecondsTimeout, ref bool lockTaken)
        {
            int observedOwner = m_owner;
            if (millisecondsTimeout < -1 || lockTaken || (observedOwner & ID_DISABLED_AND_ANONYMOUS_OWNED) != LOCK_ID_DISABLE_MASK || Interlocked.CompareExchange(ref m_owner, observedOwner | LOCK_ANONYMOUS_OWNED, observedOwner, ref lockTaken) != observedOwner)
                ContinueTryEnter(millisecondsTimeout, ref lockTaken);
        }

        private void ContinueTryEnter(int millisecondsTimeout, ref bool lockTaken)
        {
            if (lockTaken)
            {
                lockTaken = false;
                throw new System.ArgumentException(Environment.GetResourceString("SpinLock_TryReliableEnter_ArgumentException"));
            }

            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, Environment.GetResourceString("SpinLock_TryEnter_ArgumentOutOfRange"));
            }

            uint startTime = 0;
            if (millisecondsTimeout != Timeout.Infinite && millisecondsTimeout != 0)
            {
                startTime = TimeoutHelper.GetTime();
            }

            if (IsThreadOwnerTrackingEnabled)
            {
                ContinueTryEnterWithThreadTracking(millisecondsTimeout, startTime, ref lockTaken);
                return;
            }

            int observedOwner;
            int turn = int.MaxValue;
            observedOwner = m_owner;
            if ((observedOwner & LOCK_ANONYMOUS_OWNED) == LOCK_UNOWNED)
            {
                if (Interlocked.CompareExchange(ref m_owner, observedOwner | 1, observedOwner, ref lockTaken) == observedOwner)
                {
                    return;
                }
            }
            else
            {
                if ((observedOwner & WAITERS_MASK) != MAXIMUM_WAITERS)
                    turn = (Interlocked.Add(ref m_owner, 2) & WAITERS_MASK) >> 1;
            }

            if (millisecondsTimeout == 0 || (millisecondsTimeout != Timeout.Infinite && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0))
            {
                DecrementWaiters();
                return;
            }

            int processorCount = PlatformHelper.ProcessorCount;
            if (turn < processorCount)
            {
                int processFactor = 1;
                for (int i = 1; i <= turn * SPINNING_FACTOR; i++)
                {
                    Thread.SpinWait((turn + i) * SPINNING_FACTOR * processFactor);
                    if (processFactor < processorCount)
                        processFactor++;
                    observedOwner = m_owner;
                    if ((observedOwner & LOCK_ANONYMOUS_OWNED) == LOCK_UNOWNED)
                    {
                        int newOwner = (observedOwner & WAITERS_MASK) == 0 ? observedOwner | 1 : (observedOwner - 2) | 1;
                        Contract.Assert((newOwner & WAITERS_MASK) >= 0);
                        if (Interlocked.CompareExchange(ref m_owner, newOwner, observedOwner, ref lockTaken) == observedOwner)
                        {
                            return;
                        }
                    }
                }
            }

            if (millisecondsTimeout != Timeout.Infinite && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0)
            {
                DecrementWaiters();
                return;
            }

            int yieldsoFar = 0;
            while (true)
            {
                observedOwner = m_owner;
                if ((observedOwner & LOCK_ANONYMOUS_OWNED) == LOCK_UNOWNED)
                {
                    int newOwner = (observedOwner & WAITERS_MASK) == 0 ? observedOwner | 1 : (observedOwner - 2) | 1;
                    Contract.Assert((newOwner & WAITERS_MASK) >= 0);
                    if (Interlocked.CompareExchange(ref m_owner, newOwner, observedOwner, ref lockTaken) == observedOwner)
                    {
                        return;
                    }
                }

                if (yieldsoFar % SLEEP_ONE_FREQUENCY == 0)
                {
                    Thread.Sleep(1);
                }
                else if (yieldsoFar % SLEEP_ZERO_FREQUENCY == 0)
                {
                    Thread.Sleep(0);
                }
                else
                {
                    Thread.Yield();
                }

                if (yieldsoFar % TIMEOUT_CHECK_FREQUENCY == 0)
                {
                    if (millisecondsTimeout != Timeout.Infinite && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0)
                    {
                        DecrementWaiters();
                        return;
                    }
                }

                yieldsoFar++;
            }
        }

        private void DecrementWaiters()
        {
            SpinWait spinner = new SpinWait();
            while (true)
            {
                int observedOwner = m_owner;
                if ((observedOwner & WAITERS_MASK) == 0)
                    return;
                if (Interlocked.CompareExchange(ref m_owner, observedOwner - 2, observedOwner) == observedOwner)
                {
                    Contract.Assert(!IsThreadOwnerTrackingEnabled);
                    break;
                }

                spinner.SpinOnce();
            }
        }

        private void ContinueTryEnterWithThreadTracking(int millisecondsTimeout, uint startTime, ref bool lockTaken)
        {
            Contract.Assert(IsThreadOwnerTrackingEnabled);
            int lockUnowned = 0;
            int m_newOwner = Thread.CurrentThread.ManagedThreadId;
            if (m_owner == m_newOwner)
            {
                throw new LockRecursionException(Environment.GetResourceString("SpinLock_TryEnter_LockRecursionException"));
            }

            SpinWait spinner = new SpinWait();
            do
            {
                spinner.SpinOnce();
                if (m_owner == lockUnowned)
                {
                    if (Interlocked.CompareExchange(ref m_owner, m_newOwner, lockUnowned, ref lockTaken) == lockUnowned)
                    {
                        return;
                    }
                }

                if (millisecondsTimeout == 0 || (millisecondsTimeout != Timeout.Infinite && spinner.NextSpinWillYield && TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout) <= 0))
                {
                    return;
                }
            }
            while (true);
        }

        public void Exit()
        {
            if ((m_owner & LOCK_ID_DISABLE_MASK) == 0)
                ExitSlowPath(true);
            else
                Interlocked.Decrement(ref m_owner);
        }

        public void Exit(bool useMemoryBarrier)
        {
            if ((m_owner & LOCK_ID_DISABLE_MASK) != 0 && !useMemoryBarrier)
            {
                int tmpOwner = m_owner;
                m_owner = tmpOwner & (~LOCK_ANONYMOUS_OWNED);
            }
            else
                ExitSlowPath(useMemoryBarrier);
        }

        private void ExitSlowPath(bool useMemoryBarrier)
        {
            bool threadTrackingEnabled = (m_owner & LOCK_ID_DISABLE_MASK) == 0;
            if (threadTrackingEnabled && !IsHeldByCurrentThread)
            {
                throw new System.Threading.SynchronizationLockException(Environment.GetResourceString("SpinLock_Exit_SynchronizationLockException"));
            }

            if (useMemoryBarrier)
            {
                if (threadTrackingEnabled)
                    Interlocked.Exchange(ref m_owner, LOCK_UNOWNED);
                else
                    Interlocked.Decrement(ref m_owner);
            }
            else
            {
                if (threadTrackingEnabled)
                    m_owner = LOCK_UNOWNED;
                else
                {
                    int tmpOwner = m_owner;
                    m_owner = tmpOwner & (~LOCK_ANONYMOUS_OWNED);
                }
            }
        }

        public bool IsHeld
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                if (IsThreadOwnerTrackingEnabled)
                    return m_owner != LOCK_UNOWNED;
                return (m_owner & LOCK_ANONYMOUS_OWNED) != LOCK_UNOWNED;
            }
        }

        public bool IsHeldByCurrentThread
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                if (!IsThreadOwnerTrackingEnabled)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("SpinLock_IsHeldByCurrentThread"));
                }

                return ((m_owner & (~LOCK_ID_DISABLE_MASK)) == Thread.CurrentThread.ManagedThreadId);
            }
        }

        public bool IsThreadOwnerTrackingEnabled
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return (m_owner & LOCK_ID_DISABLE_MASK) == 0;
            }
        }

        internal class SystemThreading_SpinLockDebugView
        {
            private SpinLock m_spinLock;
            public SystemThreading_SpinLockDebugView(SpinLock spinLock)
            {
                m_spinLock = spinLock;
            }

            public bool ? IsHeldByCurrentThread
            {
                get
                {
                    try
                    {
                        return m_spinLock.IsHeldByCurrentThread;
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                }
            }

            public int ? OwnerThreadID
            {
                get
                {
                    if (m_spinLock.IsThreadOwnerTrackingEnabled)
                    {
                        return m_spinLock.m_owner;
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public bool IsHeld
            {
                get
                {
                    return m_spinLock.IsHeld;
                }
            }
        }
    }
}