

namespace System.Threading
{
    public class CountdownEvent : IDisposable
    {
        private int m_initialCount;
        private volatile int m_currentCount;
        private ManualResetEventSlim m_event;
        private volatile bool m_disposed;
        public CountdownEvent(int initialCount)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount");
            }

            m_initialCount = initialCount;
            m_currentCount = initialCount;
            m_event = new ManualResetEventSlim();
            if (initialCount == 0)
            {
                m_event.Set();
            }
        }

        public int CurrentCount
        {
            get
            {
                int observedCount = m_currentCount;
                return observedCount < 0 ? 0 : observedCount;
            }
        }

        public int InitialCount
        {
            get
            {
                return m_initialCount;
            }
        }

        public bool IsSet
        {
            get
            {
                return (m_currentCount <= 0);
            }
        }

        public WaitHandle WaitHandle
        {
            get
            {
                ThrowIfDisposed();
                return m_event.WaitHandle;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_event.Dispose();
                m_disposed = true;
            }
        }

        public bool Signal()
        {
            ThrowIfDisposed();
                        if (m_currentCount <= 0)
            {
                throw new InvalidOperationException(Environment.GetResourceString("CountdownEvent_Decrement_BelowZero"));
            }

            int newCount = Interlocked.Decrement(ref m_currentCount);
            if (newCount == 0)
            {
                m_event.Set();
                return true;
            }
            else if (newCount < 0)
            {
                throw new InvalidOperationException(Environment.GetResourceString("CountdownEvent_Decrement_BelowZero"));
            }

            return false;
        }

        public bool Signal(int signalCount)
        {
            if (signalCount <= 0)
            {
                throw new ArgumentOutOfRangeException("signalCount");
            }

            ThrowIfDisposed();
                        int observedCount;
            SpinWait spin = new SpinWait();
            while (true)
            {
                observedCount = m_currentCount;
                if (observedCount < signalCount)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("CountdownEvent_Decrement_BelowZero"));
                }

                if (Interlocked.CompareExchange(ref m_currentCount, observedCount - signalCount, observedCount) == observedCount)
                {
                    break;
                }

                spin.SpinOnce();
            }

            if (observedCount == signalCount)
            {
                m_event.Set();
                return true;
            }

                        return false;
        }

        public void AddCount()
        {
            AddCount(1);
        }

        public bool TryAddCount()
        {
            return TryAddCount(1);
        }

        public void AddCount(int signalCount)
        {
            if (!TryAddCount(signalCount))
            {
                throw new InvalidOperationException(Environment.GetResourceString("CountdownEvent_Increment_AlreadyZero"));
            }
        }

        public bool TryAddCount(int signalCount)
        {
            if (signalCount <= 0)
            {
                throw new ArgumentOutOfRangeException("signalCount");
            }

            ThrowIfDisposed();
            int observedCount;
            SpinWait spin = new SpinWait();
            while (true)
            {
                observedCount = m_currentCount;
                if (observedCount <= 0)
                {
                    return false;
                }
                else if (observedCount > (Int32.MaxValue - signalCount))
                {
                    throw new InvalidOperationException(Environment.GetResourceString("CountdownEvent_Increment_AlreadyMax"));
                }

                if (Interlocked.CompareExchange(ref m_currentCount, observedCount + signalCount, observedCount) == observedCount)
                {
                    break;
                }

                spin.SpinOnce();
            }

            return true;
        }

        public void Reset()
        {
            Reset(m_initialCount);
        }

        public void Reset(int count)
        {
            ThrowIfDisposed();
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            m_currentCount = count;
            m_initialCount = count;
            if (count == 0)
            {
                m_event.Set();
            }
            else
            {
                m_event.Reset();
            }
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
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }

            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            bool returnValue = IsSet;
            if (!returnValue)
            {
                returnValue = m_event.Wait(millisecondsTimeout, cancellationToken);
            }

            return returnValue;
        }

        private void ThrowIfDisposed()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException("CountdownEvent");
            }
        }
    }
}