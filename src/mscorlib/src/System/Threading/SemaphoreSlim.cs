
using System.Threading.Tasks;

namespace System.Threading
{
    public class SemaphoreSlim : IDisposable
    {
        private volatile int m_currentCount;
        private readonly int m_maxCount;
        private volatile int m_waitCount;
        private object m_lockObj;
        private volatile ManualResetEvent m_waitHandle;
        private TaskNode m_asyncHead;
        private TaskNode m_asyncTail;
        private readonly static Task<bool> s_trueTask = new Task<bool>(false, true, (TaskCreationOptions)InternalTaskOptions.DoNotDispose, default (CancellationToken));
        private const int NO_MAXIMUM = Int32.MaxValue;
        private sealed class TaskNode : Task<bool>, IThreadPoolWorkItem
        {
            internal TaskNode Prev, Next;
            internal TaskNode(): base ()
            {
            }

            void IThreadPoolWorkItem.ExecuteWorkItem()
            {
                bool setSuccessfully = TrySetResult(true);
                            }

            void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
            {
            }
        }

        public int CurrentCount
        {
            get
            {
                return m_currentCount;
            }
        }

        public WaitHandle AvailableWaitHandle
        {
            get
            {
                CheckDispose();
                if (m_waitHandle != null)
                    return m_waitHandle;
                lock (m_lockObj)
                {
                    if (m_waitHandle == null)
                    {
                        m_waitHandle = new ManualResetEvent(m_currentCount != 0);
                    }
                }

                return m_waitHandle;
            }
        }

        public SemaphoreSlim(int initialCount): this (initialCount, NO_MAXIMUM)
        {
        }

        public SemaphoreSlim(int initialCount, int maxCount)
        {
            if (initialCount < 0 || initialCount > maxCount)
            {
                throw new ArgumentOutOfRangeException("initialCount", initialCount, GetResourceString("SemaphoreSlim_ctor_InitialCountWrong"));
            }

            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException("maxCount", maxCount, GetResourceString("SemaphoreSlim_ctor_MaxCountWrong"));
            }

            m_maxCount = maxCount;
            m_lockObj = new object ();
            m_currentCount = initialCount;
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
            Int64 totalMilliseconds = (Int64)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("timeout", timeout, GetResourceString("SemaphoreSlim_Wait_TimeoutWrong"));
            }

            return Wait((int)timeout.TotalMilliseconds, new CancellationToken());
        }

        public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Int64 totalMilliseconds = (Int64)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("timeout", timeout, GetResourceString("SemaphoreSlim_Wait_TimeoutWrong"));
            }

            return Wait((int)timeout.TotalMilliseconds, cancellationToken);
        }

        public bool Wait(int millisecondsTimeout)
        {
            return Wait(millisecondsTimeout, new CancellationToken());
        }

        public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDispose();
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("totalMilliSeconds", millisecondsTimeout, GetResourceString("SemaphoreSlim_Wait_TimeoutWrong"));
            }

            cancellationToken.ThrowIfCancellationRequested();
            uint startTime = 0;
            if (millisecondsTimeout != Timeout.Infinite && millisecondsTimeout > 0)
            {
                startTime = TimeoutHelper.GetTime();
            }

            bool waitSuccessful = false;
            Task<bool> asyncWaitTask = null;
            bool lockTaken = false;
            CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.InternalRegisterWithoutEC(s_cancellationTokenCanceledEventHandler, this);
            try
            {
                SpinWait spin = new SpinWait();
                while (m_currentCount == 0 && !spin.NextSpinWillYield)
                {
                    spin.SpinOnce();
                }

                try
                {
                }
                finally
                {
                    Monitor.Enter(m_lockObj, ref lockTaken);
                    if (lockTaken)
                    {
                        m_waitCount++;
                    }
                }

                if (m_asyncHead != null)
                {
                                        asyncWaitTask = WaitAsync(millisecondsTimeout, cancellationToken);
                }
                else
                {
                    OperationCanceledException oce = null;
                    if (m_currentCount == 0)
                    {
                        if (millisecondsTimeout == 0)
                        {
                            return false;
                        }

                        try
                        {
                            waitSuccessful = WaitUntilCountOrTimeout(millisecondsTimeout, startTime, cancellationToken);
                        }
                        catch (OperationCanceledException e)
                        {
                            oce = e;
                        }
                    }

                                        if (m_currentCount > 0)
                    {
                        waitSuccessful = true;
                        m_currentCount--;
                    }
                    else if (oce != null)
                    {
                        throw oce;
                    }

                    if (m_waitHandle != null && m_currentCount == 0)
                    {
                        m_waitHandle.Reset();
                    }
                }
            }
            finally
            {
                if (lockTaken)
                {
                    m_waitCount--;
                    Monitor.Exit(m_lockObj);
                }

                cancellationTokenRegistration.Dispose();
            }

            return (asyncWaitTask != null) ? asyncWaitTask.GetAwaiter().GetResult() : waitSuccessful;
        }

        private bool WaitUntilCountOrTimeout(int millisecondsTimeout, uint startTime, CancellationToken cancellationToken)
        {
            int remainingWaitMilliseconds = Timeout.Infinite;
            while (m_currentCount == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (millisecondsTimeout != Timeout.Infinite)
                {
                    remainingWaitMilliseconds = TimeoutHelper.UpdateTimeOut(startTime, millisecondsTimeout);
                    if (remainingWaitMilliseconds <= 0)
                    {
                        return false;
                    }
                }

                if (!Monitor.Wait(m_lockObj, remainingWaitMilliseconds))
                {
                    return false;
                }
            }

            return true;
        }

        public Task WaitAsync()
        {
            return WaitAsync(Timeout.Infinite, default (CancellationToken));
        }

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            return WaitAsync(Timeout.Infinite, cancellationToken);
        }

        public Task<bool> WaitAsync(int millisecondsTimeout)
        {
            return WaitAsync(millisecondsTimeout, default (CancellationToken));
        }

        public Task<bool> WaitAsync(TimeSpan timeout)
        {
            return WaitAsync(timeout, default (CancellationToken));
        }

        public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Int64 totalMilliseconds = (Int64)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("timeout", timeout, GetResourceString("SemaphoreSlim_Wait_TimeoutWrong"));
            }

            return WaitAsync((int)timeout.TotalMilliseconds, cancellationToken);
        }

        public Task<bool> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            CheckDispose();
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("totalMilliSeconds", millisecondsTimeout, GetResourceString("SemaphoreSlim_Wait_TimeoutWrong"));
            }

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<bool>(cancellationToken);
            lock (m_lockObj)
            {
                if (m_currentCount > 0)
                {
                    --m_currentCount;
                    if (m_waitHandle != null && m_currentCount == 0)
                        m_waitHandle.Reset();
                    return s_trueTask;
                }
                else
                {
                                        var asyncWaiter = CreateAndAddAsyncWaiter();
                    return (millisecondsTimeout == Timeout.Infinite && !cancellationToken.CanBeCanceled) ? asyncWaiter : WaitUntilCountOrTimeoutAsync(asyncWaiter, millisecondsTimeout, cancellationToken);
                }
            }
        }

        private TaskNode CreateAndAddAsyncWaiter()
        {
                        var task = new TaskNode();
            if (m_asyncHead == null)
            {
                                m_asyncHead = task;
                m_asyncTail = task;
            }
            else
            {
                                m_asyncTail.Next = task;
                task.Prev = m_asyncTail;
                m_asyncTail = task;
            }

            return task;
        }

        private bool RemoveAsyncWaiter(TaskNode task)
        {
                                    bool wasInList = m_asyncHead == task || task.Prev != null;
            if (task.Next != null)
                task.Next.Prev = task.Prev;
            if (task.Prev != null)
                task.Prev.Next = task.Next;
            if (m_asyncHead == task)
                m_asyncHead = task.Next;
            if (m_asyncTail == task)
                m_asyncTail = task.Prev;
                        task.Next = task.Prev = null;
            return wasInList;
        }

        private async Task<bool> WaitUntilCountOrTimeoutAsync(TaskNode asyncWaiter, int millisecondsTimeout, CancellationToken cancellationToken)
        {
                                    using (var cts = cancellationToken.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, default (CancellationToken)) : new CancellationTokenSource())
            {
                var waitCompleted = Task.WhenAny(asyncWaiter, Task.Delay(millisecondsTimeout, cts.Token));
                if (asyncWaiter == await waitCompleted.ConfigureAwait(false))
                {
                    cts.Cancel();
                    return true;
                }
            }

            lock (m_lockObj)
            {
                if (RemoveAsyncWaiter(asyncWaiter))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return false;
                }
            }

            return await asyncWaiter.ConfigureAwait(false);
        }

        public int Release()
        {
            return Release(1);
        }

        public int Release(int releaseCount)
        {
            CheckDispose();
            if (releaseCount < 1)
            {
                throw new ArgumentOutOfRangeException("releaseCount", releaseCount, GetResourceString("SemaphoreSlim_Release_CountWrong"));
            }

            int returnCount;
            lock (m_lockObj)
            {
                int currentCount = m_currentCount;
                returnCount = currentCount;
                if (m_maxCount - currentCount < releaseCount)
                {
                    throw new SemaphoreFullException();
                }

                currentCount += releaseCount;
                int waitCount = m_waitCount;
                if (currentCount == 1 || waitCount == 1)
                {
                    Monitor.Pulse(m_lockObj);
                }
                else if (waitCount > 1)
                {
                    Monitor.PulseAll(m_lockObj);
                }

                if (m_asyncHead != null)
                {
                                        int maxAsyncToRelease = currentCount - waitCount;
                    while (maxAsyncToRelease > 0 && m_asyncHead != null)
                    {
                        --currentCount;
                        --maxAsyncToRelease;
                        var waiterTask = m_asyncHead;
                        RemoveAsyncWaiter(waiterTask);
                        QueueWaiterTask(waiterTask);
                    }
                }

                m_currentCount = currentCount;
                if (m_waitHandle != null && returnCount == 0 && currentCount > 0)
                {
                    m_waitHandle.Set();
                }
            }

            return returnCount;
        }

        private static void QueueWaiterTask(TaskNode waiterTask)
        {
            ThreadPool.UnsafeQueueCustomWorkItem(waiterTask, forceGlobal: false);
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
                if (m_waitHandle != null)
                {
                    m_waitHandle.Close();
                    m_waitHandle = null;
                }

                m_lockObj = null;
                m_asyncHead = null;
                m_asyncTail = null;
            }
        }

        private static Action<object> s_cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            SemaphoreSlim semaphore = obj as SemaphoreSlim;
                        lock (semaphore.m_lockObj)
            {
                Monitor.PulseAll(semaphore.m_lockObj);
            }
        }

        private void CheckDispose()
        {
            if (m_lockObj == null)
            {
                throw new ObjectDisposedException(null, GetResourceString("SemaphoreSlim_Disposed"));
            }
        }

        private static string GetResourceString(string str)
        {
            return Environment.GetResourceString(str);
        }
    }
}