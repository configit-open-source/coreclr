using System.Diagnostics.Contracts;

namespace System.Threading
{
    public class ManualResetEventSlim : IDisposable
    {
        private const int DEFAULT_SPIN_SP = 1;
        private const int DEFAULT_SPIN_MP = SpinWait.YIELD_THRESHOLD;
        private volatile object m_lock;
        private volatile ManualResetEvent m_eventObj;
        private volatile int m_combinedState;
        private const int SignalledState_BitMask = unchecked ((int)0x80000000);
        private const int SignalledState_ShiftCount = 31;
        private const int Dispose_BitMask = unchecked ((int)0x40000000);
        private const int SpinCountState_BitMask = unchecked ((int)0x3FF80000);
        private const int SpinCountState_ShiftCount = 19;
        private const int SpinCountState_MaxValue = (1 << 11) - 1;
        private const int NumWaitersState_BitMask = unchecked ((int)0x0007FFFF);
        private const int NumWaitersState_ShiftCount = 0;
        private const int NumWaitersState_MaxValue = (1 << 19) - 1;
        private static int s_nextId;
        private int m_id = Interlocked.Increment(ref s_nextId);
        private long m_lastSetTime;
        private long m_lastResetTime;
        public WaitHandle WaitHandle
        {
            get
            {
                ThrowIfDisposed();
                if (m_eventObj == null)
                {
                    LazyInitializeEvent();
                }

                return m_eventObj;
            }
        }

        public bool IsSet
        {
            get
            {
                return 0 != ExtractStatePortion(m_combinedState, SignalledState_BitMask);
            }

            private set
            {
                UpdateStateAtomically(((value) ? 1 : 0) << SignalledState_ShiftCount, SignalledState_BitMask);
            }
        }

        public int SpinCount
        {
            get
            {
                return ExtractStatePortionAndShiftRight(m_combinedState, SpinCountState_BitMask, SpinCountState_ShiftCount);
            }

            private set
            {
                Contract.Assert(value >= 0, "SpinCount is a restricted-width integer. The value supplied is outside the legal range.");
                Contract.Assert(value <= SpinCountState_MaxValue, "SpinCount is a restricted-width integer. The value supplied is outside the legal range.");
                m_combinedState = (m_combinedState & ~SpinCountState_BitMask) | (value << SpinCountState_ShiftCount);
            }
        }

        private int Waiters
        {
            get
            {
                return ExtractStatePortionAndShiftRight(m_combinedState, NumWaitersState_BitMask, NumWaitersState_ShiftCount);
            }

            set
            {
                Contract.Assert(value >= 0, "NumWaiters should never be less than zero. This indicates an internal error.");
                if (value >= NumWaitersState_MaxValue)
                    throw new InvalidOperationException(String.Format(Environment.GetResourceString("ManualResetEventSlim_ctor_TooManyWaiters"), NumWaitersState_MaxValue));
                UpdateStateAtomically(value << NumWaitersState_ShiftCount, NumWaitersState_BitMask);
            }
        }

        public ManualResetEventSlim(): this (false)
        {
        }

        public ManualResetEventSlim(bool initialState)
        {
            Initialize(initialState, DEFAULT_SPIN_MP);
        }

        public ManualResetEventSlim(bool initialState, int spinCount)
        {
            if (spinCount < 0)
            {
                throw new ArgumentOutOfRangeException("spinCount");
            }

            if (spinCount > SpinCountState_MaxValue)
            {
                throw new ArgumentOutOfRangeException("spinCount", String.Format(Environment.GetResourceString("ManualResetEventSlim_ctor_SpinCountOutOfRange"), SpinCountState_MaxValue));
            }

            Initialize(initialState, spinCount);
        }

        private void Initialize(bool initialState, int spinCount)
        {
            this.m_combinedState = initialState ? (1 << SignalledState_ShiftCount) : 0;
            Contract.Assert(DEFAULT_SPIN_SP >= 0, "Internal error - DEFAULT_SPIN_SP is outside the legal range.");
            Contract.Assert(DEFAULT_SPIN_SP <= SpinCountState_MaxValue, "Internal error - DEFAULT_SPIN_SP is outside the legal range.");
            SpinCount = PlatformHelper.IsSingleProcessor ? DEFAULT_SPIN_SP : spinCount;
        }

        private void EnsureLockObjectCreated()
        {
            Contract.Ensures(m_lock != null);
            if (m_lock != null)
                return;
            object newObj = new object ();
            Interlocked.CompareExchange(ref m_lock, newObj, null);
        }

        private bool LazyInitializeEvent()
        {
            bool preInitializeIsSet = IsSet;
            ManualResetEvent newEventObj = new ManualResetEvent(preInitializeIsSet);
            if (Interlocked.CompareExchange(ref m_eventObj, newEventObj, null) != null)
            {
                newEventObj.Close();
                return false;
            }
            else
            {
                bool currentIsSet = IsSet;
                if (currentIsSet != preInitializeIsSet)
                {
                    Contract.Assert(currentIsSet, "The only safe concurrent transition is from unset->set: detected set->unset.");
                    lock (newEventObj)
                    {
                        if (m_eventObj == newEventObj)
                        {
                            newEventObj.Set();
                        }
                    }
                }

                return true;
            }
        }

        public void Set()
        {
            Set(false);
        }

        private void Set(bool duringCancellation)
        {
            IsSet = true;
            if (Waiters > 0)
            {
                Contract.Assert(m_lock != null);
                lock (m_lock)
                {
                    Monitor.PulseAll(m_lock);
                }
            }

            ManualResetEvent eventObj = m_eventObj;
            if (eventObj != null && !duringCancellation)
            {
                lock (eventObj)
                {
                    if (m_eventObj != null)
                    {
                        m_eventObj.Set();
                    }
                }
            }

            m_lastSetTime = DateTime.UtcNow.Ticks;
        }

        public void Reset()
        {
            ThrowIfDisposed();
            if (m_eventObj != null)
            {
                m_eventObj.Reset();
            }

            IsSet = false;
            m_lastResetTime = DateTime.UtcNow.Ticks;
        }

        public void Wait()
        {
            Wait(Timeout.Infinite, new CancellationToken());
        }

        public void Wait(CancellationToken cancellationToken)
        {
            Wait(Timeout.Infinite, cancellationToken);
        }

        public bool Wait(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return Wait((int)totalMilliseconds, new CancellationToken());
        }

        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return Wait((int)totalMilliseconds, cancellationToken);
        }

        public bool Wait(int millisecondsTimeout)
        {
            return Wait(millisecondsTimeout, new CancellationToken());
        }

        public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }

            if (!IsSet)
            {
                if (millisecondsTimeout == 0)
                {
                    return false;
                }

                uint startTime = 0;
                bool bNeedTimeoutAdjustment = false;
                int realMillisecondsTimeout = millisecondsTimeout;
                if (millisecondsTimeout != Timeout.Infinite)
                {
                    startTime = TimeoutHelper.GetTime();
                    bNeedTimeoutAdjustment = true;
                }

                int HOW_MANY_SPIN_BEFORE_YIELD = 10;
                int HOW_MANY_YIELD_EVERY_SLEEP_0 = 5;
                int HOW_MANY_YIELD_EVERY_SLEEP_1 = 20;
                int spinCount = SpinCount;
                for (int i = 0; i < spinCount; i++)
                {
                    if (IsSet)
                    {
                        return true;
                    }
                    else if (i < HOW_MANY_SPIN_BEFORE_YIELD)
                    {
                        if (i == HOW_MANY_SPIN_BEFORE_YIELD / 2)
                        {
                            Thread.Yield();
                        }
                        else
                        {
                            Thread.SpinWait(PlatformHelper.ProcessorCount * (4 << i));
                        }
                    }
                    else if (i % HOW_MANY_YIELD_EVERY_SLEEP_1 == 0)
                    {
                        Thread.Sleep(1);
                    }
                    else if (i % HOW_MANY_YIELD_EVERY_SLEEP_0 == 0)
                    {
                        Thread.Sleep(0);
                    }
                    else
                    {
                        Thread.Yield();
                    }

                    if (i >= 100 && i % 10 == 0)
                        cancellationToken.ThrowIfCancellationRequested();
                }

                EnsureLockObjectCreated();
                using (cancellationToken.InternalRegisterWithoutEC(s_cancellationTokenCallback, this))
                {
                    lock (m_lock)
                    {
                        while (!IsSet)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (bNeedTimeoutAdjustment)
                            {
                                realMillisecondsTimeout = TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
                                if (realMillisecondsTimeout <= 0)
                                    return false;
                            }

                            Waiters = Waiters + 1;
                            if (IsSet)
                            {
                                Waiters--;
                                return true;
                            }

                            try
                            {
                                if (!Monitor.Wait(m_lock, realMillisecondsTimeout))
                                    return false;
                            }
                            finally
                            {
                                Waiters = Waiters - 1;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if ((m_combinedState & Dispose_BitMask) != 0)
                return;
            m_combinedState |= Dispose_BitMask;
            if (disposing)
            {
                ManualResetEvent eventObj = m_eventObj;
                if (eventObj != null)
                {
                    lock (eventObj)
                    {
                        eventObj.Close();
                        m_eventObj = null;
                    }
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if ((m_combinedState & Dispose_BitMask) != 0)
                throw new ObjectDisposedException(Environment.GetResourceString("ManualResetEventSlim_Disposed"));
        }

        private static Action<object> s_cancellationTokenCallback = new Action<object>(CancellationTokenCallback);
        private static void CancellationTokenCallback(object obj)
        {
            ManualResetEventSlim mre = obj as ManualResetEventSlim;
            Contract.Assert(mre != null, "Expected a ManualResetEventSlim");
            Contract.Assert(mre.m_lock != null);
            lock (mre.m_lock)
            {
                Monitor.PulseAll(mre.m_lock);
            }
        }

        private void UpdateStateAtomically(int newBits, int updateBitsMask)
        {
            SpinWait sw = new SpinWait();
            Contract.Assert((newBits | updateBitsMask) == updateBitsMask, "newBits do not fall within the updateBitsMask.");
            do
            {
                int oldState = m_combinedState;
                int newState = (oldState & ~updateBitsMask) | newBits;
                if (Interlocked.CompareExchange(ref m_combinedState, newState, oldState) == oldState)
                {
                    return;
                }

                sw.SpinOnce();
            }
            while (true);
        }

        private static int ExtractStatePortionAndShiftRight(int state, int mask, int rightBitShiftCount)
        {
            return unchecked ((int)(((uint)(state & mask)) >> rightBitShiftCount));
        }

        private static int ExtractStatePortion(int state, int mask)
        {
            return state & mask;
        }
    }
}