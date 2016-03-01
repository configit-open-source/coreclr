using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace System.Threading
{
    internal static class ThreadPoolGlobals
    {
        public static uint tpQuantum = 30U;
        public static int processorCount = Environment.ProcessorCount;
        public static bool tpHosted = ThreadPool.IsThreadPoolHosted();
        public static volatile bool vmTpInitialized;
        public static bool enableWorkerTracking;
        public static ThreadPoolWorkQueue workQueue = new ThreadPoolWorkQueue();
        static ThreadPoolGlobals()
        {
        }
    }

    internal sealed class ThreadPoolWorkQueue
    {
        internal class SparseArray<T>
            where T : class
        {
            private volatile T[] m_array;
            internal SparseArray(int initialSize)
            {
                m_array = new T[initialSize];
            }

            internal T[] Current
            {
                get
                {
                    return m_array;
                }
            }

            internal int Add(T e)
            {
                while (true)
                {
                    T[] array = m_array;
                    lock (array)
                    {
                        for (int i = 0; i < array.Length; i++)
                        {
                            if (array[i] == null)
                            {
                                Volatile.Write(ref array[i], e);
                                return i;
                            }
                            else if (i == array.Length - 1)
                            {
                                if (array != m_array)
                                    continue;
                                T[] newArray = new T[array.Length * 2];
                                Array.Copy(array, newArray, i + 1);
                                newArray[i + 1] = e;
                                m_array = newArray;
                                return i + 1;
                            }
                        }
                    }
                }
            }

            internal void Remove(T e)
            {
                T[] array = m_array;
                lock (array)
                {
                    for (int i = 0; i < m_array.Length; i++)
                    {
                        if (m_array[i] == e)
                        {
                            Volatile.Write(ref m_array[i], null);
                            break;
                        }
                    }
                }
            }
        }

        internal class WorkStealingQueue
        {
            private const int INITIAL_SIZE = 32;
            internal volatile IThreadPoolWorkItem[] m_array = new IThreadPoolWorkItem[INITIAL_SIZE];
            private volatile int m_mask = INITIAL_SIZE - 1;
            private const int START_INDEX = int.MaxValue;
            private volatile int m_headIndex = START_INDEX;
            private volatile int m_tailIndex = START_INDEX;
            private SpinLock m_foreignLock = new SpinLock(false);
            public void LocalPush(IThreadPoolWorkItem obj)
            {
                int tail = m_tailIndex;
                if (tail == int.MaxValue)
                {
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);
                        if (m_tailIndex == int.MaxValue)
                        {
                            m_headIndex = m_headIndex & m_mask;
                            m_tailIndex = tail = m_tailIndex & m_mask;
                                                    }
                    }
                    finally
                    {
                        if (lockTaken)
                            m_foreignLock.Exit(true);
                    }
                }

                if (tail < m_headIndex + m_mask)
                {
                    Volatile.Write(ref m_array[tail & m_mask], obj);
                    m_tailIndex = tail + 1;
                }
                else
                {
                    bool lockTaken = false;
                    try
                    {
                        m_foreignLock.Enter(ref lockTaken);
                        int head = m_headIndex;
                        int count = m_tailIndex - m_headIndex;
                        if (count >= m_mask)
                        {
                            IThreadPoolWorkItem[] newArray = new IThreadPoolWorkItem[m_array.Length << 1];
                            for (int i = 0; i < m_array.Length; i++)
                                newArray[i] = m_array[(i + head) & m_mask];
                            m_array = newArray;
                            m_headIndex = 0;
                            m_tailIndex = tail = count;
                            m_mask = (m_mask << 1) | 1;
                        }

                        Volatile.Write(ref m_array[tail & m_mask], obj);
                        m_tailIndex = tail + 1;
                    }
                    finally
                    {
                        if (lockTaken)
                            m_foreignLock.Exit(false);
                    }
                }
            }

            public bool LocalFindAndPop(IThreadPoolWorkItem obj)
            {
                if (m_array[(m_tailIndex - 1) & m_mask] == obj)
                {
                    IThreadPoolWorkItem unused;
                    if (LocalPop(out unused))
                    {
                                                return true;
                    }

                    return false;
                }

                for (int i = m_tailIndex - 2; i >= m_headIndex; i--)
                {
                    if (m_array[i & m_mask] == obj)
                    {
                        bool lockTaken = false;
                        try
                        {
                            m_foreignLock.Enter(ref lockTaken);
                            if (m_array[i & m_mask] == null)
                                return false;
                            Volatile.Write(ref m_array[i & m_mask], null);
                            if (i == m_tailIndex)
                                m_tailIndex -= 1;
                            else if (i == m_headIndex)
                                m_headIndex += 1;
                            return true;
                        }
                        finally
                        {
                            if (lockTaken)
                                m_foreignLock.Exit(false);
                        }
                    }
                }

                return false;
            }

            public bool LocalPop(out IThreadPoolWorkItem obj)
            {
                while (true)
                {
                    int tail = m_tailIndex;
                    if (m_headIndex >= tail)
                    {
                        obj = null;
                        return false;
                    }

                    tail -= 1;
                    Interlocked.Exchange(ref m_tailIndex, tail);
                    if (m_headIndex <= tail)
                    {
                        int idx = tail & m_mask;
                        obj = Volatile.Read(ref m_array[idx]);
                        if (obj == null)
                            continue;
                        m_array[idx] = null;
                        return true;
                    }
                    else
                    {
                        bool lockTaken = false;
                        try
                        {
                            m_foreignLock.Enter(ref lockTaken);
                            if (m_headIndex <= tail)
                            {
                                int idx = tail & m_mask;
                                obj = Volatile.Read(ref m_array[idx]);
                                if (obj == null)
                                    continue;
                                m_array[idx] = null;
                                return true;
                            }
                            else
                            {
                                m_tailIndex = tail + 1;
                                obj = null;
                                return false;
                            }
                        }
                        finally
                        {
                            if (lockTaken)
                                m_foreignLock.Exit(false);
                        }
                    }
                }
            }

            public bool TrySteal(out IThreadPoolWorkItem obj, ref bool missedSteal)
            {
                return TrySteal(out obj, ref missedSteal, 0);
            }

            private bool TrySteal(out IThreadPoolWorkItem obj, ref bool missedSteal, int millisecondsTimeout)
            {
                obj = null;
                while (true)
                {
                    if (m_headIndex >= m_tailIndex)
                        return false;
                    bool taken = false;
                    try
                    {
                        m_foreignLock.TryEnter(millisecondsTimeout, ref taken);
                        if (taken)
                        {
                            int head = m_headIndex;
                            Interlocked.Exchange(ref m_headIndex, head + 1);
                            if (head < m_tailIndex)
                            {
                                int idx = head & m_mask;
                                obj = Volatile.Read(ref m_array[idx]);
                                if (obj == null)
                                    continue;
                                m_array[idx] = null;
                                return true;
                            }
                            else
                            {
                                m_headIndex = head;
                                obj = null;
                                missedSteal = true;
                            }
                        }
                        else
                        {
                            missedSteal = true;
                        }
                    }
                    finally
                    {
                        if (taken)
                            m_foreignLock.Exit(false);
                    }

                    return false;
                }
            }
        }

        internal class QueueSegment
        {
            internal readonly IThreadPoolWorkItem[] nodes;
            private const int QueueSegmentLength = 256;
            private volatile int indexes;
            public volatile QueueSegment Next;
            const int SixteenBits = 0xffff;
            void GetIndexes(out int upper, out int lower)
            {
                int i = indexes;
                upper = (i >> 16) & SixteenBits;
                lower = i & SixteenBits;
                                                                                            }

            bool CompareExchangeIndexes(ref int prevUpper, int newUpper, ref int prevLower, int newLower)
            {
                                                                                                                                                int oldIndexes = (prevUpper << 16) | (prevLower & SixteenBits);
                int newIndexes = (newUpper << 16) | (newLower & SixteenBits);
                int prevIndexes = Interlocked.CompareExchange(ref indexes, newIndexes, oldIndexes);
                prevUpper = (prevIndexes >> 16) & SixteenBits;
                prevLower = prevIndexes & SixteenBits;
                return prevIndexes == oldIndexes;
            }

            public QueueSegment()
            {
                                nodes = new IThreadPoolWorkItem[QueueSegmentLength];
            }

            public bool IsUsedUp()
            {
                int upper, lower;
                GetIndexes(out upper, out lower);
                return (upper == nodes.Length) && (lower == nodes.Length);
            }

            public bool TryEnqueue(IThreadPoolWorkItem node)
            {
                                int upper, lower;
                GetIndexes(out upper, out lower);
                while (true)
                {
                    if (upper == nodes.Length)
                        return false;
                    if (CompareExchangeIndexes(ref upper, upper + 1, ref lower, lower))
                    {
                                                Volatile.Write(ref nodes[upper], node);
                        return true;
                    }
                }
            }

            public bool TryDequeue(out IThreadPoolWorkItem node)
            {
                int upper, lower;
                GetIndexes(out upper, out lower);
                while (true)
                {
                    if (lower == upper)
                    {
                        node = null;
                        return false;
                    }

                    if (CompareExchangeIndexes(ref upper, upper, ref lower, lower + 1))
                    {
                        SpinWait spinner = new SpinWait();
                        while ((node = Volatile.Read(ref nodes[lower])) == null)
                            spinner.SpinOnce();
                        nodes[lower] = null;
                        return true;
                    }
                }
            }
        }

        internal volatile QueueSegment queueHead;
        internal volatile QueueSegment queueTail;
        internal bool loggingEnabled;
        internal static SparseArray<WorkStealingQueue> allThreadQueues = new SparseArray<WorkStealingQueue>(16);
        private volatile int numOutstandingThreadRequests = 0;
        public ThreadPoolWorkQueue()
        {
            queueTail = queueHead = new QueueSegment();
            loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, FrameworkEventSource.Keywords.ThreadPool | FrameworkEventSource.Keywords.ThreadTransfer);
        }

        public ThreadPoolWorkQueueThreadLocals EnsureCurrentThreadHasQueue()
        {
            if (null == ThreadPoolWorkQueueThreadLocals.threadLocals)
                ThreadPoolWorkQueueThreadLocals.threadLocals = new ThreadPoolWorkQueueThreadLocals(this);
            return ThreadPoolWorkQueueThreadLocals.threadLocals;
        }

        internal void EnsureThreadRequested()
        {
            int count = numOutstandingThreadRequests;
            while (count < ThreadPoolGlobals.processorCount)
            {
                int prev = Interlocked.CompareExchange(ref numOutstandingThreadRequests, count + 1, count);
                if (prev == count)
                {
                    ThreadPool.RequestWorkerThread();
                    break;
                }

                count = prev;
            }
        }

        internal void MarkThreadRequestSatisfied()
        {
            int count = numOutstandingThreadRequests;
            while (count > 0)
            {
                int prev = Interlocked.CompareExchange(ref numOutstandingThreadRequests, count - 1, count);
                if (prev == count)
                {
                    break;
                }

                count = prev;
            }
        }

        public void Enqueue(IThreadPoolWorkItem callback, bool forceGlobal)
        {
            ThreadPoolWorkQueueThreadLocals tl = null;
            if (!forceGlobal)
                tl = ThreadPoolWorkQueueThreadLocals.threadLocals;
            if (loggingEnabled)
                System.Diagnostics.Tracing.FrameworkEventSource.Log.ThreadPoolEnqueueWorkObject(callback);
            if (null != tl)
            {
                tl.workStealingQueue.LocalPush(callback);
            }
            else
            {
                QueueSegment head = queueHead;
                while (!head.TryEnqueue(callback))
                {
                    Interlocked.CompareExchange(ref head.Next, new QueueSegment(), null);
                    while (head.Next != null)
                    {
                        Interlocked.CompareExchange(ref queueHead, head.Next, head);
                        head = queueHead;
                    }
                }
            }

            EnsureThreadRequested();
        }

        internal bool LocalFindAndPop(IThreadPoolWorkItem callback)
        {
            ThreadPoolWorkQueueThreadLocals tl = ThreadPoolWorkQueueThreadLocals.threadLocals;
            if (null == tl)
                return false;
            return tl.workStealingQueue.LocalFindAndPop(callback);
        }

        public void Dequeue(ThreadPoolWorkQueueThreadLocals tl, out IThreadPoolWorkItem callback, out bool missedSteal)
        {
            callback = null;
            missedSteal = false;
            WorkStealingQueue wsq = tl.workStealingQueue;
            if (wsq.LocalPop(out callback))
                            if (null == callback)
            {
                QueueSegment tail = queueTail;
                while (true)
                {
                    if (tail.TryDequeue(out callback))
                    {
                                                break;
                    }

                    if (null == tail.Next || !tail.IsUsedUp())
                    {
                        break;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref queueTail, tail.Next, tail);
                        tail = queueTail;
                    }
                }
            }

            if (null == callback)
            {
                WorkStealingQueue[] otherQueues = allThreadQueues.Current;
                int i = tl.random.Next(otherQueues.Length);
                int c = otherQueues.Length;
                while (c > 0)
                {
                    WorkStealingQueue otherQueue = Volatile.Read(ref otherQueues[i % otherQueues.Length]);
                    if (otherQueue != null && otherQueue != wsq && otherQueue.TrySteal(out callback, ref missedSteal))
                    {
                                                break;
                    }

                    i++;
                    c--;
                }
            }
        }

        static internal bool Dispatch()
        {
            var workQueue = ThreadPoolGlobals.workQueue;
            int quantumStartTime = Environment.TickCount;
            workQueue.MarkThreadRequestSatisfied();
            workQueue.loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, FrameworkEventSource.Keywords.ThreadPool | FrameworkEventSource.Keywords.ThreadTransfer);
            bool needAnotherThread = true;
            IThreadPoolWorkItem workItem = null;
            try
            {
                ThreadPoolWorkQueueThreadLocals tl = workQueue.EnsureCurrentThreadHasQueue();
                while ((Environment.TickCount - quantumStartTime) < ThreadPoolGlobals.tpQuantum)
                {
                    try
                    {
                    }
                    finally
                    {
                        bool missedSteal = false;
                        workQueue.Dequeue(tl, out workItem, out missedSteal);
                        if (workItem == null)
                        {
                            needAnotherThread = missedSteal;
                        }
                        else
                        {
                            workQueue.EnsureThreadRequested();
                        }
                    }

                    if (workItem == null)
                    {
                        return true;
                    }
                    else
                    {
                        if (workQueue.loggingEnabled)
                            System.Diagnostics.Tracing.FrameworkEventSource.Log.ThreadPoolDequeueWorkObject(workItem);
                        if (ThreadPoolGlobals.enableWorkerTracking)
                        {
                            bool reportedStatus = false;
                            try
                            {
                                try
                                {
                                }
                                finally
                                {
                                    ThreadPool.ReportThreadStatus(true);
                                    reportedStatus = true;
                                }

                                workItem.ExecuteWorkItem();
                                workItem = null;
                            }
                            finally
                            {
                                if (reportedStatus)
                                    ThreadPool.ReportThreadStatus(false);
                            }
                        }
                        else
                        {
                            workItem.ExecuteWorkItem();
                            workItem = null;
                        }

                        if (!ThreadPool.NotifyWorkItemComplete())
                            return false;
                    }
                }

                return true;
            }
            catch (ThreadAbortException tae)
            {
                if (workItem != null)
                    workItem.MarkAborted(tae);
                needAnotherThread = false;
            }
            finally
            {
                if (needAnotherThread)
                    workQueue.EnsureThreadRequested();
            }

                        return true;
        }
    }

    internal sealed class ThreadPoolWorkQueueThreadLocals
    {
        public static ThreadPoolWorkQueueThreadLocals threadLocals;
        public readonly ThreadPoolWorkQueue workQueue;
        public readonly ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue;
        public readonly Random random = new Random(Thread.CurrentThread.ManagedThreadId);
        public ThreadPoolWorkQueueThreadLocals(ThreadPoolWorkQueue tpq)
        {
            workQueue = tpq;
            workStealingQueue = new ThreadPoolWorkQueue.WorkStealingQueue();
            ThreadPoolWorkQueue.allThreadQueues.Add(workStealingQueue);
        }

        private void CleanUp()
        {
            if (null != workStealingQueue)
            {
                if (null != workQueue)
                {
                    bool done = false;
                    while (!done)
                    {
                        try
                        {
                        }
                        finally
                        {
                            IThreadPoolWorkItem cb = null;
                            if (workStealingQueue.LocalPop(out cb))
                            {
                                                                workQueue.Enqueue(cb, true);
                            }
                            else
                            {
                                done = true;
                            }
                        }
                    }
                }

                ThreadPoolWorkQueue.allThreadQueues.Remove(workStealingQueue);
            }
        }

        ~ThreadPoolWorkQueueThreadLocals()
        {
            if (!(Environment.HasShutdownStarted || AppDomain.CurrentDomain.IsFinalizingForUnload()))
                CleanUp();
        }
    }

    internal sealed class RegisteredWaitHandleSafe : CriticalFinalizerObject
    {
        private static IntPtr InvalidHandle
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return Win32Native.INVALID_HANDLE_VALUE;
            }
        }

        private IntPtr registeredWaitHandle;
        private WaitHandle m_internalWaitObject;
        private bool bReleaseNeeded = false;
        private volatile int m_lock = 0;
        internal RegisteredWaitHandleSafe()
        {
            registeredWaitHandle = InvalidHandle;
        }

        internal IntPtr GetHandle()
        {
            return registeredWaitHandle;
        }

        internal void SetHandle(IntPtr handle)
        {
            registeredWaitHandle = handle;
        }

        internal void SetWaitObject(WaitHandle waitObject)
        {
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                m_internalWaitObject = waitObject;
                if (waitObject != null)
                {
                    m_internalWaitObject.SafeWaitHandle.DangerousAddRef(ref bReleaseNeeded);
                }
            }
        }

        internal bool Unregister(WaitHandle waitObject)
        {
            bool result = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                bool bLockTaken = false;
                do
                {
                    if (Interlocked.CompareExchange(ref m_lock, 1, 0) == 0)
                    {
                        bLockTaken = true;
                        try
                        {
                            if (ValidHandle())
                            {
                                result = UnregisterWaitNative(GetHandle(), waitObject == null ? null : waitObject.SafeWaitHandle);
                                if (result == true)
                                {
                                    if (bReleaseNeeded)
                                    {
                                        m_internalWaitObject.SafeWaitHandle.DangerousRelease();
                                        bReleaseNeeded = false;
                                    }

                                    SetHandle(InvalidHandle);
                                    m_internalWaitObject = null;
                                    GC.SuppressFinalize(this);
                                }
                            }
                        }
                        finally
                        {
                            m_lock = 0;
                        }
                    }

                    Thread.SpinWait(1);
                }
                while (!bLockTaken);
            }

            return result;
        }

        private bool ValidHandle()
        {
            return (registeredWaitHandle != InvalidHandle && registeredWaitHandle != IntPtr.Zero);
        }

        ~RegisteredWaitHandleSafe()
        {
            if (Interlocked.CompareExchange(ref m_lock, 1, 0) == 0)
            {
                try
                {
                    if (ValidHandle())
                    {
                        WaitHandleCleanupNative(registeredWaitHandle);
                        if (bReleaseNeeded)
                        {
                            m_internalWaitObject.SafeWaitHandle.DangerousRelease();
                            bReleaseNeeded = false;
                        }

                        SetHandle(InvalidHandle);
                        m_internalWaitObject = null;
                    }
                }
                finally
                {
                    m_lock = 0;
                }
            }
        }

        private static extern void WaitHandleCleanupNative(IntPtr handle);
        private static extern bool UnregisterWaitNative(IntPtr handle, SafeHandle waitObject);
    }

    public sealed class RegisteredWaitHandle
    {
        private RegisteredWaitHandleSafe internalRegisteredWait;
        internal RegisteredWaitHandle()
        {
            internalRegisteredWait = new RegisteredWaitHandleSafe();
        }

        internal void SetHandle(IntPtr handle)
        {
            internalRegisteredWait.SetHandle(handle);
        }

        internal void SetWaitObject(WaitHandle waitObject)
        {
            internalRegisteredWait.SetWaitObject(waitObject);
        }

        public bool Unregister(WaitHandle waitObject)
        {
            return internalRegisteredWait.Unregister(waitObject);
        }
    }

    public delegate void WaitCallback(Object state);
    public delegate void WaitOrTimerCallback(Object state, bool timedOut);
    internal static class _ThreadPoolWaitCallback
    {
        static internal bool PerformWaitCallback()
        {
            return ThreadPoolWorkQueue.Dispatch();
        }
    }

    internal interface IThreadPoolWorkItem
    {
        void ExecuteWorkItem();
        void MarkAborted(ThreadAbortException tae);
    }

    internal sealed class QueueUserWorkItemCallback : IThreadPoolWorkItem
    {
        static QueueUserWorkItemCallback()
        {
        }

        private WaitCallback callback;
        private ExecutionContext context;
        private Object state;
        volatile int executed;
        ~QueueUserWorkItemCallback()
        {
                    }

        void MarkExecuted(bool aborted)
        {
            GC.SuppressFinalize(this);
                    }

        internal QueueUserWorkItemCallback(WaitCallback waitCallback, Object stateObj, ExecutionContext ec)
        {
            callback = waitCallback;
            state = stateObj;
            context = ec;
        }

        void IThreadPoolWorkItem.ExecuteWorkItem()
        {
            MarkExecuted(false);
            if (context == null)
            {
                WaitCallback cb = callback;
                callback = null;
                cb(state);
            }
            else
            {
                ExecutionContext.Run(context, ccb, this, true);
            }
        }

        void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
        {
            MarkExecuted(true);
        }

        static internal ContextCallback ccb = new ContextCallback(WaitCallback_Context);
        static private void WaitCallback_Context(Object state)
        {
            QueueUserWorkItemCallback obj = (QueueUserWorkItemCallback)state;
            WaitCallback wc = obj.callback as WaitCallback;
                        wc(obj.state);
        }
    }

    internal sealed class QueueUserWorkItemCallbackDefaultContext : IThreadPoolWorkItem
    {
        static QueueUserWorkItemCallbackDefaultContext()
        {
        }

        private WaitCallback callback;
        private Object state;
        private volatile int executed;
        ~QueueUserWorkItemCallbackDefaultContext()
        {
                    }

        void MarkExecuted(bool aborted)
        {
            GC.SuppressFinalize(this);
                    }

        internal QueueUserWorkItemCallbackDefaultContext(WaitCallback waitCallback, Object stateObj)
        {
            callback = waitCallback;
            state = stateObj;
        }

        void IThreadPoolWorkItem.ExecuteWorkItem()
        {
            MarkExecuted(false);
            ExecutionContext.Run(ExecutionContext.PreAllocatedDefault, ccb, this, true);
        }

        void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
        {
            MarkExecuted(true);
        }

        static internal ContextCallback ccb = new ContextCallback(WaitCallback_Context);
        static private void WaitCallback_Context(Object state)
        {
            QueueUserWorkItemCallbackDefaultContext obj = (QueueUserWorkItemCallbackDefaultContext)state;
            WaitCallback wc = obj.callback as WaitCallback;
                        obj.callback = null;
            wc(obj.state);
        }
    }

    internal class _ThreadPoolWaitOrTimerCallback
    {
        static _ThreadPoolWaitOrTimerCallback()
        {
        }

        WaitOrTimerCallback _waitOrTimerCallback;
        ExecutionContext _executionContext;
        Object _state;
        static private ContextCallback _ccbt = new ContextCallback(WaitOrTimerCallback_Context_t);
        static private ContextCallback _ccbf = new ContextCallback(WaitOrTimerCallback_Context_f);
        internal _ThreadPoolWaitOrTimerCallback(WaitOrTimerCallback waitOrTimerCallback, Object state, bool compressStack, ref StackCrawlMark stackMark)
        {
            _waitOrTimerCallback = waitOrTimerCallback;
            _state = state;
            if (compressStack && !ExecutionContext.IsFlowSuppressed())
            {
                _executionContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
            }
        }

        static private void WaitOrTimerCallback_Context_t(Object state)
        {
            WaitOrTimerCallback_Context(state, true);
        }

        static private void WaitOrTimerCallback_Context_f(Object state)
        {
            WaitOrTimerCallback_Context(state, false);
        }

        static private void WaitOrTimerCallback_Context(Object state, bool timedOut)
        {
            _ThreadPoolWaitOrTimerCallback helper = (_ThreadPoolWaitOrTimerCallback)state;
            helper._waitOrTimerCallback(helper._state, timedOut);
        }

        static internal void PerformWaitOrTimerCallback(Object state, bool timedOut)
        {
            _ThreadPoolWaitOrTimerCallback helper = (_ThreadPoolWaitOrTimerCallback)state;
                        if (helper._executionContext == null)
            {
                WaitOrTimerCallback callback = helper._waitOrTimerCallback;
                callback(helper._state, timedOut);
            }
            else
            {
                using (ExecutionContext executionContext = helper._executionContext.CreateCopy())
                {
                    if (timedOut)
                        ExecutionContext.Run(executionContext, _ccbt, helper, true);
                    else
                        ExecutionContext.Run(executionContext, _ccbf, helper, true);
                }
            }
        }
    }

    unsafe public delegate void IOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped*pOVERLAP);
    public static class ThreadPool
    {
        public static bool SetMaxThreads(int workerThreads, int completionPortThreads)
        {
            return SetMaxThreadsNative(workerThreads, completionPortThreads);
        }

        public static void GetMaxThreads(out int workerThreads, out int completionPortThreads)
        {
            GetMaxThreadsNative(out workerThreads, out completionPortThreads);
        }

        public static bool SetMinThreads(int workerThreads, int completionPortThreads)
        {
            return SetMinThreadsNative(workerThreads, completionPortThreads);
        }

        public static void GetMinThreads(out int workerThreads, out int completionPortThreads)
        {
            GetMinThreadsNative(out workerThreads, out completionPortThreads);
        }

        public static void GetAvailableThreads(out int workerThreads, out int completionPortThreads)
        {
            GetAvailableThreadsNative(out workerThreads, out completionPortThreads);
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, ref stackMark, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RegisterWaitForSingleObject(waitObject, callBack, state, millisecondsTimeOutInterval, executeOnlyOnce, ref stackMark, false);
        }

        private static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, uint millisecondsTimeOutInterval, bool executeOnlyOnce, ref StackCrawlMark stackMark, bool compressStack)
        {
            RegisteredWaitHandle registeredWaitHandle = new RegisteredWaitHandle();
            if (callBack != null)
            {
                _ThreadPoolWaitOrTimerCallback callBackHelper = new _ThreadPoolWaitOrTimerCallback(callBack, state, compressStack, ref stackMark);
                state = (Object)callBackHelper;
                registeredWaitHandle.SetWaitObject(waitObject);
                IntPtr nativeRegisteredWaitHandle = RegisterWaitForSingleObjectNative(waitObject, state, millisecondsTimeOutInterval, executeOnlyOnce, registeredWaitHandle, ref stackMark, compressStack);
                registeredWaitHandle.SetHandle(nativeRegisteredWaitHandle);
            }
            else
            {
                throw new ArgumentNullException("WaitOrTimerCallback");
            }

            return registeredWaitHandle;
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RegisterWaitForSingleObject(waitObject, callBack, state, (UInt32)millisecondsTimeOutInterval, executeOnlyOnce, ref stackMark, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, int millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RegisterWaitForSingleObject(waitObject, callBack, state, (UInt32)millisecondsTimeOutInterval, executeOnlyOnce, ref stackMark, false);
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RegisterWaitForSingleObject(waitObject, callBack, state, (UInt32)millisecondsTimeOutInterval, executeOnlyOnce, ref stackMark, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, long millisecondsTimeOutInterval, bool executeOnlyOnce)
        {
            if (millisecondsTimeOutInterval < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeOutInterval", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RegisterWaitForSingleObject(waitObject, callBack, state, (UInt32)millisecondsTimeOutInterval, executeOnlyOnce, ref stackMark, false);
        }

        public static RegisteredWaitHandle RegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, TimeSpan timeout, bool executeOnlyOnce)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (tm > (long)Int32.MaxValue)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_LessEqualToIntegerMaxVal"));
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RegisterWaitForSingleObject(waitObject, callBack, state, (UInt32)tm, executeOnlyOnce, ref stackMark, true);
        }

        public static RegisteredWaitHandle UnsafeRegisterWaitForSingleObject(WaitHandle waitObject, WaitOrTimerCallback callBack, Object state, TimeSpan timeout, bool executeOnlyOnce)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (tm > (long)Int32.MaxValue)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_LessEqualToIntegerMaxVal"));
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RegisterWaitForSingleObject(waitObject, callBack, state, (UInt32)tm, executeOnlyOnce, ref stackMark, false);
        }

        public static bool QueueUserWorkItem(WaitCallback callBack, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return QueueUserWorkItemHelper(callBack, state, ref stackMark, true);
        }

        public static bool QueueUserWorkItem(WaitCallback callBack)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return QueueUserWorkItemHelper(callBack, null, ref stackMark, true);
        }

        public static bool UnsafeQueueUserWorkItem(WaitCallback callBack, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return QueueUserWorkItemHelper(callBack, state, ref stackMark, false);
        }

        private static bool QueueUserWorkItemHelper(WaitCallback callBack, Object state, ref StackCrawlMark stackMark, bool compressStack)
        {
            bool success = true;
            if (callBack != null)
            {
                EnsureVMInitialized();
                try
                {
                }
                finally
                {
                    ExecutionContext context = compressStack && !ExecutionContext.IsFlowSuppressed() ? ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase) : null;
                    IThreadPoolWorkItem tpcallBack = context == ExecutionContext.PreAllocatedDefault ? new QueueUserWorkItemCallbackDefaultContext(callBack, state) : (IThreadPoolWorkItem)new QueueUserWorkItemCallback(callBack, state, context);
                    ThreadPoolGlobals.workQueue.Enqueue(tpcallBack, true);
                    success = true;
                }
            }
            else
            {
                throw new ArgumentNullException("WaitCallback");
            }

            return success;
        }

        internal static void UnsafeQueueCustomWorkItem(IThreadPoolWorkItem workItem, bool forceGlobal)
        {
                        EnsureVMInitialized();
            try
            {
            }
            finally
            {
                ThreadPoolGlobals.workQueue.Enqueue(workItem, forceGlobal);
            }
        }

        internal static bool TryPopCustomWorkItem(IThreadPoolWorkItem workItem)
        {
                        if (!ThreadPoolGlobals.vmTpInitialized)
                return false;
            return ThreadPoolGlobals.workQueue.LocalFindAndPop(workItem);
        }

        internal static IEnumerable<IThreadPoolWorkItem> GetQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(ThreadPoolWorkQueue.allThreadQueues.Current, ThreadPoolGlobals.workQueue.queueTail);
        }

        internal static IEnumerable<IThreadPoolWorkItem> EnumerateQueuedWorkItems(ThreadPoolWorkQueue.WorkStealingQueue[] wsQueues, ThreadPoolWorkQueue.QueueSegment globalQueueTail)
        {
            if (wsQueues != null)
            {
                foreach (ThreadPoolWorkQueue.WorkStealingQueue wsq in wsQueues)
                {
                    if (wsq != null && wsq.m_array != null)
                    {
                        IThreadPoolWorkItem[] items = wsq.m_array;
                        for (int i = 0; i < items.Length; i++)
                        {
                            IThreadPoolWorkItem item = items[i];
                            if (item != null)
                                yield return item;
                        }
                    }
                }
            }

            if (globalQueueTail != null)
            {
                for (ThreadPoolWorkQueue.QueueSegment segment = globalQueueTail; segment != null; segment = segment.Next)
                {
                    IThreadPoolWorkItem[] items = segment.nodes;
                    for (int i = 0; i < items.Length; i++)
                    {
                        IThreadPoolWorkItem item = items[i];
                        if (item != null)
                            yield return item;
                    }
                }
            }
        }

        internal static IEnumerable<IThreadPoolWorkItem> GetLocallyQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(new ThreadPoolWorkQueue.WorkStealingQueue[]{ThreadPoolWorkQueueThreadLocals.threadLocals.workStealingQueue}, null);
        }

        internal static IEnumerable<IThreadPoolWorkItem> GetGloballyQueuedWorkItems()
        {
            return EnumerateQueuedWorkItems(null, ThreadPoolGlobals.workQueue.queueTail);
        }

        private static object[] ToObjectArray(IEnumerable<IThreadPoolWorkItem> workitems)
        {
            int i = 0;
            foreach (IThreadPoolWorkItem item in workitems)
            {
                i++;
            }

            object[] result = new object[i];
            i = 0;
            foreach (IThreadPoolWorkItem item in workitems)
            {
                if (i < result.Length)
                    result[i] = item;
                i++;
            }

            return result;
        }

        internal static object[] GetQueuedWorkItemsForDebugger()
        {
            return ToObjectArray(GetQueuedWorkItems());
        }

        internal static object[] GetGloballyQueuedWorkItemsForDebugger()
        {
            return ToObjectArray(GetGloballyQueuedWorkItems());
        }

        internal static object[] GetLocallyQueuedWorkItemsForDebugger()
        {
            return ToObjectArray(GetLocallyQueuedWorkItems());
        }

        internal static extern bool RequestWorkerThread();
        unsafe private static extern bool PostQueuedCompletionStatus(NativeOverlapped*overlapped);
        unsafe public static bool UnsafeQueueNativeOverlapped(NativeOverlapped*overlapped)
        {
            return PostQueuedCompletionStatus(overlapped);
        }

        private static void EnsureVMInitialized()
        {
            if (!ThreadPoolGlobals.vmTpInitialized)
            {
                ThreadPool.InitializeVMTp(ref ThreadPoolGlobals.enableWorkerTracking);
                ThreadPoolGlobals.vmTpInitialized = true;
            }
        }

        private static extern bool SetMinThreadsNative(int workerThreads, int completionPortThreads);
        private static extern bool SetMaxThreadsNative(int workerThreads, int completionPortThreads);
        private static extern void GetMinThreadsNative(out int workerThreads, out int completionPortThreads);
        private static extern void GetMaxThreadsNative(out int workerThreads, out int completionPortThreads);
        private static extern void GetAvailableThreadsNative(out int workerThreads, out int completionPortThreads);
        internal static extern bool NotifyWorkItemComplete();
        internal static extern void ReportThreadStatus(bool isWorking);
        internal static void NotifyWorkItemProgress()
        {
            if (!ThreadPoolGlobals.vmTpInitialized)
                ThreadPool.InitializeVMTp(ref ThreadPoolGlobals.enableWorkerTracking);
            NotifyWorkItemProgressNative();
        }

        internal static extern void NotifyWorkItemProgressNative();
        internal static extern bool IsThreadPoolHosted();
        private static extern void InitializeVMTp(ref bool enableWorkerTracking);
        private static extern IntPtr RegisterWaitForSingleObjectNative(WaitHandle waitHandle, Object state, uint timeOutInterval, bool executeOnlyOnce, RegisteredWaitHandle registeredWaitHandle, ref StackCrawlMark stackMark, bool compressStack);
        public static bool BindHandle(SafeHandle osHandle)
        {
            if (osHandle == null)
                throw new ArgumentNullException("osHandle");
            bool ret = false;
            bool mustReleaseSafeHandle = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                osHandle.DangerousAddRef(ref mustReleaseSafeHandle);
                ret = BindIOCompletionCallbackNative(osHandle.DangerousGetHandle());
            }
            finally
            {
                if (mustReleaseSafeHandle)
                    osHandle.DangerousRelease();
            }

            return ret;
        }

        private static extern bool BindIOCompletionCallbackNative(IntPtr fileHandle);
    }
}