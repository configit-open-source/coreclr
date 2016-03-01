using System.Collections.Generic;

namespace System.Threading
{
    public class CancellationTokenSource : IDisposable
    {
        private static readonly CancellationTokenSource _staticSource_Set = new CancellationTokenSource(true);
        private static readonly CancellationTokenSource _staticSource_NotCancelable = new CancellationTokenSource(false);
        private static readonly int s_nLists = (PlatformHelper.ProcessorCount > 24) ? 24 : PlatformHelper.ProcessorCount;
        private volatile ManualResetEvent m_kernelEvent;
        private volatile SparselyPopulatedArray<CancellationCallbackInfo>[] m_registeredCallbacksLists;
        private const int CANNOT_BE_CANCELED = 0;
        private const int NOT_CANCELED = 1;
        private const int NOTIFYING = 2;
        private const int NOTIFYINGCOMPLETE = 3;
        private volatile int m_state;
        private volatile int m_threadIDExecutingCallbacks = -1;
        private bool m_disposed;
        private volatile CancellationCallbackInfo m_executingCallback;
        private volatile Timer m_timer;
        public bool IsCancellationRequested
        {
            get
            {
                return m_state >= NOTIFYING;
            }
        }

        internal bool IsCancellationCompleted
        {
            get
            {
                return m_state == NOTIFYINGCOMPLETE;
            }
        }

        internal bool IsDisposed
        {
            get
            {
                return m_disposed;
            }
        }

        internal int ThreadIDExecutingCallbacks
        {
            set
            {
                m_threadIDExecutingCallbacks = value;
            }

            get
            {
                return m_threadIDExecutingCallbacks;
            }
        }

        public CancellationToken Token
        {
            get
            {
                ThrowIfDisposed();
                return new CancellationToken(this);
            }
        }

        internal bool CanBeCanceled
        {
            get
            {
                return m_state != CANNOT_BE_CANCELED;
            }
        }

        internal WaitHandle WaitHandle
        {
            get
            {
                ThrowIfDisposed();
                if (m_kernelEvent != null)
                    return m_kernelEvent;
                ManualResetEvent mre = new ManualResetEvent(false);
                if (Interlocked.CompareExchange(ref m_kernelEvent, mre, null) != null)
                {
                    ((IDisposable)mre).Dispose();
                }

                if (IsCancellationRequested)
                    m_kernelEvent.Set();
                return m_kernelEvent;
            }
        }

        internal CancellationCallbackInfo ExecutingCallback
        {
            get
            {
                return m_executingCallback;
            }
        }

        private int CallbackCount
        {
            get
            {
                SparselyPopulatedArray<CancellationCallbackInfo>[] callbackLists = m_registeredCallbacksLists;
                if (callbackLists == null)
                    return 0;
                int count = 0;
                foreach (SparselyPopulatedArray<CancellationCallbackInfo> sparseArray in callbackLists)
                {
                    if (sparseArray != null)
                    {
                        SparselyPopulatedArrayFragment<CancellationCallbackInfo> currCallbacks = sparseArray.Head;
                        while (currCallbacks != null)
                        {
                            for (int i = 0; i < currCallbacks.Length; i++)
                                if (currCallbacks[i] != null)
                                    count++;
                            currCallbacks = currCallbacks.Next;
                        }
                    }
                }

                return count;
            }
        }

        public CancellationTokenSource()
        {
            m_state = NOT_CANCELED;
        }

        private CancellationTokenSource(bool set)
        {
            m_state = set ? NOTIFYINGCOMPLETE : CANNOT_BE_CANCELED;
        }

        public CancellationTokenSource(TimeSpan delay)
        {
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("delay");
            }

            InitializeWithTimer((int)totalMilliseconds);
        }

        public CancellationTokenSource(Int32 millisecondsDelay)
        {
            if (millisecondsDelay < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay");
            }

            InitializeWithTimer(millisecondsDelay);
        }

        private void InitializeWithTimer(Int32 millisecondsDelay)
        {
            m_state = NOT_CANCELED;
            m_timer = new Timer(s_timerCallback, this, millisecondsDelay, -1);
        }

        public void Cancel()
        {
            Cancel(false);
        }

        public void Cancel(bool throwOnFirstException)
        {
            ThrowIfDisposed();
            NotifyCancellation(throwOnFirstException);
        }

        public void CancelAfter(TimeSpan delay)
        {
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("delay");
            }

            CancelAfter((int)totalMilliseconds);
        }

        public void CancelAfter(Int32 millisecondsDelay)
        {
            ThrowIfDisposed();
            if (millisecondsDelay < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay");
            }

            if (IsCancellationRequested)
                return;
            if (m_timer == null)
            {
                Timer newTimer = new Timer(s_timerCallback, this, -1, -1);
                if (Interlocked.CompareExchange(ref m_timer, newTimer, null) != null)
                {
                    newTimer.Dispose();
                }
            }

            try
            {
                m_timer.Change(millisecondsDelay, -1);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static readonly TimerCallback s_timerCallback = new TimerCallback(TimerCallbackLogic);
        private static void TimerCallbackLogic(object obj)
        {
            CancellationTokenSource cts = (CancellationTokenSource)obj;
            if (!cts.IsDisposed)
            {
                try
                {
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    if (!cts.IsDisposed)
                        throw;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !m_disposed)
            {
                if (m_timer != null)
                    m_timer.Dispose();
                m_registeredCallbacksLists = null;
                if (m_kernelEvent != null)
                {
                    m_kernelEvent.Close();
                    m_kernelEvent = null;
                }

                m_disposed = true;
            }
        }

        internal void ThrowIfDisposed()
        {
            if (m_disposed)
                ThrowObjectDisposedException();
        }

        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(null, Environment.GetResourceString("CancellationTokenSource_Disposed"));
        }

        internal static CancellationTokenSource InternalGetStaticSource(bool set)
        {
            return set ? _staticSource_Set : _staticSource_NotCancelable;
        }

        internal CancellationTokenRegistration InternalRegister(Action<object> callback, object stateForCallback, SynchronizationContext targetSyncContext, ExecutionContext executionContext)
        {
            if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
            {
                ThrowIfDisposed();
            }

                        if (!IsCancellationRequested)
            {
                if (m_disposed && !AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
                    return new CancellationTokenRegistration();
                int myIndex = Thread.CurrentThread.ManagedThreadId % s_nLists;
                CancellationCallbackInfo callbackInfo = targetSyncContext != null ? new CancellationCallbackInfo.WithSyncContext(callback, stateForCallback, executionContext, this, targetSyncContext) : new CancellationCallbackInfo(callback, stateForCallback, executionContext, this);
                var registeredCallbacksLists = m_registeredCallbacksLists;
                if (registeredCallbacksLists == null)
                {
                    SparselyPopulatedArray<CancellationCallbackInfo>[] list = new SparselyPopulatedArray<CancellationCallbackInfo>[s_nLists];
                    registeredCallbacksLists = Interlocked.CompareExchange(ref m_registeredCallbacksLists, list, null);
                    if (registeredCallbacksLists == null)
                        registeredCallbacksLists = list;
                }

                var callbacks = Volatile.Read<SparselyPopulatedArray<CancellationCallbackInfo>>(ref registeredCallbacksLists[myIndex]);
                if (callbacks == null)
                {
                    SparselyPopulatedArray<CancellationCallbackInfo> callBackArray = new SparselyPopulatedArray<CancellationCallbackInfo>(4);
                    Interlocked.CompareExchange(ref (registeredCallbacksLists[myIndex]), callBackArray, null);
                    callbacks = registeredCallbacksLists[myIndex];
                }

                SparselyPopulatedArrayAddInfo<CancellationCallbackInfo> addInfo = callbacks.Add(callbackInfo);
                CancellationTokenRegistration registration = new CancellationTokenRegistration(callbackInfo, addInfo);
                if (!IsCancellationRequested)
                    return registration;
                bool deregisterOccurred = registration.TryDeregister();
                if (!deregisterOccurred)
                {
                    return registration;
                }
            }

            callback(stateForCallback);
            return new CancellationTokenRegistration();
        }

        private void NotifyCancellation(bool throwOnFirstException)
        {
            if (IsCancellationRequested)
                return;
            if (Interlocked.CompareExchange(ref m_state, NOTIFYING, NOT_CANCELED) == NOT_CANCELED)
            {
                Timer timer = m_timer;
                if (timer != null)
                    timer.Dispose();
                ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
                if (m_kernelEvent != null)
                    m_kernelEvent.Set();
                ExecuteCallbackHandlers(throwOnFirstException);
                            }
        }

        private void ExecuteCallbackHandlers(bool throwOnFirstException)
        {
                                    List<Exception> exceptionList = null;
            SparselyPopulatedArray<CancellationCallbackInfo>[] callbackLists = m_registeredCallbacksLists;
            if (callbackLists == null)
            {
                Interlocked.Exchange(ref m_state, NOTIFYINGCOMPLETE);
                return;
            }

            try
            {
                for (int index = 0; index < callbackLists.Length; index++)
                {
                    SparselyPopulatedArray<CancellationCallbackInfo> list = Volatile.Read<SparselyPopulatedArray<CancellationCallbackInfo>>(ref callbackLists[index]);
                    if (list != null)
                    {
                        SparselyPopulatedArrayFragment<CancellationCallbackInfo> currArrayFragment = list.Tail;
                        while (currArrayFragment != null)
                        {
                            for (int i = currArrayFragment.Length - 1; i >= 0; i--)
                            {
                                m_executingCallback = currArrayFragment[i];
                                if (m_executingCallback != null)
                                {
                                    CancellationCallbackCoreWorkArguments args = new CancellationCallbackCoreWorkArguments(currArrayFragment, i);
                                    try
                                    {
                                        var wsc = m_executingCallback as CancellationCallbackInfo.WithSyncContext;
                                        if (wsc != null)
                                        {
                                                                                        wsc.TargetSyncContext.Send(CancellationCallbackCoreWork_OnSyncContext, args);
                                            ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
                                        }
                                        else
                                        {
                                            CancellationCallbackCoreWork(args);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        if (throwOnFirstException)
                                            throw;
                                        if (exceptionList == null)
                                            exceptionList = new List<Exception>();
                                        exceptionList.Add(ex);
                                    }
                                }
                            }

                            currArrayFragment = currArrayFragment.Prev;
                        }
                    }
                }
            }
            finally
            {
                m_state = NOTIFYINGCOMPLETE;
                m_executingCallback = null;
                Thread.MemoryBarrier();
            }

            if (exceptionList != null)
            {
                                throw new AggregateException(exceptionList);
            }
        }

        private void CancellationCallbackCoreWork_OnSyncContext(object obj)
        {
            CancellationCallbackCoreWork((CancellationCallbackCoreWorkArguments)obj);
        }

        private void CancellationCallbackCoreWork(CancellationCallbackCoreWorkArguments args)
        {
            CancellationCallbackInfo callback = args.m_currArrayFragment.SafeAtomicRemove(args.m_currArrayIndex, m_executingCallback);
            if (callback == m_executingCallback)
            {
                if (callback.TargetExecutionContext != null)
                {
                    callback.CancellationTokenSource.ThreadIDExecutingCallbacks = Thread.CurrentThread.ManagedThreadId;
                }

                callback.ExecuteCallback();
            }
        }

        public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken token1, CancellationToken token2)
        {
            return token1.CanBeCanceled || token2.CanBeCanceled ? new LinkedCancellationTokenSource(token1, token2) : new CancellationTokenSource();
        }

        public static CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException("tokens");
            if (tokens.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("CancellationToken_CreateLinkedToken_TokensIsEmpty"));
                        return new LinkedCancellationTokenSource(tokens);
        }

        internal void WaitForCallbackToComplete(CancellationCallbackInfo callbackInfo)
        {
            SpinWait sw = new SpinWait();
            while (ExecutingCallback == callbackInfo)
            {
                sw.SpinOnce();
            }
        }

        private sealed class LinkedCancellationTokenSource : CancellationTokenSource
        {
            private static readonly Action<object> s_linkedTokenCancelDelegate = s => ((CancellationTokenSource)s).Cancel();
            private CancellationTokenRegistration[] m_linkingRegistrations;
            internal LinkedCancellationTokenSource(CancellationToken token1, CancellationToken token2)
            {
                bool token2CanBeCanceled = token2.CanBeCanceled;
                if (token1.CanBeCanceled)
                {
                    m_linkingRegistrations = new CancellationTokenRegistration[token2CanBeCanceled ? 2 : 1];
                    m_linkingRegistrations[0] = token1.InternalRegisterWithoutEC(s_linkedTokenCancelDelegate, this);
                }

                if (token2CanBeCanceled)
                {
                    int index = 1;
                    if (m_linkingRegistrations == null)
                    {
                        m_linkingRegistrations = new CancellationTokenRegistration[1];
                        index = 0;
                    }

                    m_linkingRegistrations[index] = token2.InternalRegisterWithoutEC(s_linkedTokenCancelDelegate, this);
                }
            }

            internal LinkedCancellationTokenSource(params CancellationToken[] tokens)
            {
                m_linkingRegistrations = new CancellationTokenRegistration[tokens.Length];
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (tokens[i].CanBeCanceled)
                    {
                        m_linkingRegistrations[i] = tokens[i].InternalRegisterWithoutEC(s_linkedTokenCancelDelegate, this);
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing || m_disposed)
                    return;
                CancellationTokenRegistration[] linkingRegistrations = m_linkingRegistrations;
                if (linkingRegistrations != null)
                {
                    m_linkingRegistrations = null;
                    for (int i = 0; i < linkingRegistrations.Length; i++)
                    {
                        linkingRegistrations[i].Dispose();
                    }
                }

                base.Dispose(disposing);
            }
        }
    }

    internal struct CancellationCallbackCoreWorkArguments
    {
        internal SparselyPopulatedArrayFragment<CancellationCallbackInfo> m_currArrayFragment;
        internal int m_currArrayIndex;
        public CancellationCallbackCoreWorkArguments(SparselyPopulatedArrayFragment<CancellationCallbackInfo> currArrayFragment, int currArrayIndex)
        {
            m_currArrayFragment = currArrayFragment;
            m_currArrayIndex = currArrayIndex;
        }
    }

    internal class CancellationCallbackInfo
    {
        internal readonly Action<object> Callback;
        internal readonly object StateForCallback;
        internal readonly ExecutionContext TargetExecutionContext;
        internal readonly CancellationTokenSource CancellationTokenSource;
        internal sealed class WithSyncContext : CancellationCallbackInfo
        {
            internal readonly SynchronizationContext TargetSyncContext;
            internal WithSyncContext(Action<object> callback, object stateForCallback, ExecutionContext targetExecutionContext, CancellationTokenSource cancellationTokenSource, SynchronizationContext targetSyncContext): base (callback, stateForCallback, targetExecutionContext, cancellationTokenSource)
            {
                TargetSyncContext = targetSyncContext;
            }
        }

        internal CancellationCallbackInfo(Action<object> callback, object stateForCallback, ExecutionContext targetExecutionContext, CancellationTokenSource cancellationTokenSource)
        {
            Callback = callback;
            StateForCallback = stateForCallback;
            TargetExecutionContext = targetExecutionContext;
            CancellationTokenSource = cancellationTokenSource;
        }

        private static ContextCallback s_executionContextCallback;
        internal void ExecuteCallback()
        {
            if (TargetExecutionContext != null)
            {
                var callback = s_executionContextCallback;
                if (callback == null)
                    s_executionContextCallback = callback = new ContextCallback(ExecutionContextCallback);
                ExecutionContext.Run(TargetExecutionContext, callback, this);
            }
            else
            {
                ExecutionContextCallback(this);
            }
        }

        private static void ExecutionContextCallback(object obj)
        {
            CancellationCallbackInfo callbackInfo = obj as CancellationCallbackInfo;
                        callbackInfo.Callback(callbackInfo.StateForCallback);
        }
    }

    internal class SparselyPopulatedArray<T>
        where T : class
    {
        private readonly SparselyPopulatedArrayFragment<T> m_head;
        private volatile SparselyPopulatedArrayFragment<T> m_tail;
        internal SparselyPopulatedArray(int initialSize)
        {
            m_head = m_tail = new SparselyPopulatedArrayFragment<T>(initialSize);
        }

        internal SparselyPopulatedArrayFragment<T> Head
        {
            get
            {
                return m_head;
            }
        }

        internal SparselyPopulatedArrayFragment<T> Tail
        {
            get
            {
                return m_tail;
            }
        }

        internal SparselyPopulatedArrayAddInfo<T> Add(T element)
        {
            while (true)
            {
                SparselyPopulatedArrayFragment<T> tail = m_tail;
                while (tail.m_next != null)
                    m_tail = (tail = tail.m_next);
                SparselyPopulatedArrayFragment<T> curr = tail;
                while (curr != null)
                {
                    const int RE_SEARCH_THRESHOLD = -10;
                    if (curr.m_freeCount < 1)
                        --curr.m_freeCount;
                    if (curr.m_freeCount > 0 || curr.m_freeCount < RE_SEARCH_THRESHOLD)
                    {
                        int c = curr.Length;
                        int start = ((c - curr.m_freeCount) % c);
                        if (start < 0)
                        {
                            start = 0;
                            curr.m_freeCount--;
                        }

                                                for (int i = 0; i < c; i++)
                        {
                            int tryIndex = (start + i) % c;
                                                        if (curr.m_elements[tryIndex] == null && Interlocked.CompareExchange(ref curr.m_elements[tryIndex], element, null) == null)
                            {
                                int newFreeCount = curr.m_freeCount - 1;
                                curr.m_freeCount = newFreeCount > 0 ? newFreeCount : 0;
                                return new SparselyPopulatedArrayAddInfo<T>(curr, tryIndex);
                            }
                        }
                    }

                    curr = curr.m_prev;
                }

                SparselyPopulatedArrayFragment<T> newTail = new SparselyPopulatedArrayFragment<T>(tail.m_elements.Length == 4096 ? 4096 : tail.m_elements.Length * 2, tail);
                if (Interlocked.CompareExchange(ref tail.m_next, newTail, null) == null)
                {
                    m_tail = newTail;
                }
            }
        }
    }

    internal struct SparselyPopulatedArrayAddInfo<T>
        where T : class
    {
        private SparselyPopulatedArrayFragment<T> m_source;
        private int m_index;
        internal SparselyPopulatedArrayAddInfo(SparselyPopulatedArrayFragment<T> source, int index)
        {
                                    m_source = source;
            m_index = index;
        }

        internal SparselyPopulatedArrayFragment<T> Source
        {
            get
            {
                return m_source;
            }
        }

        internal int Index
        {
            get
            {
                return m_index;
            }
        }
    }

    internal class SparselyPopulatedArrayFragment<T>
        where T : class
    {
        internal readonly T[] m_elements;
        internal volatile int m_freeCount;
        internal volatile SparselyPopulatedArrayFragment<T> m_next;
        internal volatile SparselyPopulatedArrayFragment<T> m_prev;
        internal SparselyPopulatedArrayFragment(int size): this (size, null)
        {
        }

        internal SparselyPopulatedArrayFragment(int size, SparselyPopulatedArrayFragment<T> prev)
        {
            m_elements = new T[size];
            m_freeCount = size;
            m_prev = prev;
        }

        internal T this[int index]
        {
            get
            {
                return Volatile.Read<T>(ref m_elements[index]);
            }
        }

        internal int Length
        {
            get
            {
                return m_elements.Length;
            }
        }

        internal SparselyPopulatedArrayFragment<T> Next
        {
            get
            {
                return m_next;
            }
        }

        internal SparselyPopulatedArrayFragment<T> Prev
        {
            get
            {
                return m_prev;
            }
        }

        internal T SafeAtomicRemove(int index, T expectedElement)
        {
            T prevailingValue = Interlocked.CompareExchange(ref m_elements[index], null, expectedElement);
            if (prevailingValue != null)
                ++m_freeCount;
            return prevailingValue;
        }
    }
}