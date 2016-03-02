
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Security;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public delegate void TimerCallback(Object state);
    class TimerQueue
    {
        static TimerQueue s_queue = new TimerQueue();
        public static TimerQueue Instance
        {
            get
            {
                return s_queue;
            }
        }

        private TimerQueue()
        {
        }

        private static int TickCount
        {
            [SecuritySafeCritical]
            get
            {
                if (Environment.IsWindows8OrAbove)
                {
                    ulong time100ns;
                    bool result = Win32Native.QueryUnbiasedInterruptTime(out time100ns);
                    if (!result)
                        throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
                    return (int)(uint)(time100ns / 10000);
                }
                else
                {
                    return Environment.TickCount;
                }
            }
        }

        class AppDomainTimerSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public AppDomainTimerSafeHandle(): base (true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return DeleteAppDomainTimer(handle);
            }
        }

        AppDomainTimerSafeHandle m_appDomainTimer;
        bool m_isAppDomainTimerScheduled;
        int m_currentAppDomainTimerStartTicks;
        uint m_currentAppDomainTimerDuration;
        private bool EnsureAppDomainTimerFiresBy(uint requestedDuration)
        {
            const uint maxPossibleDuration = 0x0fffffff;
            uint actualDuration = Math.Min(requestedDuration, maxPossibleDuration);
            if (m_isAppDomainTimerScheduled)
            {
                uint elapsed = (uint)(TickCount - m_currentAppDomainTimerStartTicks);
                if (elapsed >= m_currentAppDomainTimerDuration)
                    return true;
                uint remainingDuration = m_currentAppDomainTimerDuration - elapsed;
                if (actualDuration >= remainingDuration)
                    return true;
            }

            if (m_pauseTicks != 0)
            {
                                                return true;
            }

            if (m_appDomainTimer == null || m_appDomainTimer.IsInvalid)
            {
                                m_appDomainTimer = CreateAppDomainTimer(actualDuration);
                if (!m_appDomainTimer.IsInvalid)
                {
                    m_isAppDomainTimerScheduled = true;
                    m_currentAppDomainTimerStartTicks = TickCount;
                    m_currentAppDomainTimerDuration = actualDuration;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (ChangeAppDomainTimer(m_appDomainTimer, actualDuration))
                {
                    m_isAppDomainTimerScheduled = true;
                    m_currentAppDomainTimerStartTicks = TickCount;
                    m_currentAppDomainTimerDuration = actualDuration;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal static void AppDomainTimerCallback()
        {
            Instance.FireNextTimers();
        }

        static extern AppDomainTimerSafeHandle CreateAppDomainTimer(uint dueTime);
        static extern bool ChangeAppDomainTimer(AppDomainTimerSafeHandle handle, uint dueTime);
        static extern bool DeleteAppDomainTimer(IntPtr handle);
        TimerQueueTimer m_timers;
        volatile int m_pauseTicks = 0;
        internal void Pause()
        {
            lock (this)
            {
                if (m_appDomainTimer != null && !m_appDomainTimer.IsInvalid)
                {
                    m_appDomainTimer.Dispose();
                    m_appDomainTimer = null;
                    m_isAppDomainTimerScheduled = false;
                    m_pauseTicks = TickCount;
                }
            }
        }

        internal void Resume()
        {
            lock (this)
            {
                try
                {
                }
                finally
                {
                    int pauseTicks = m_pauseTicks;
                    m_pauseTicks = 0;
                    int resumedTicks = TickCount;
                    int pauseDuration = resumedTicks - pauseTicks;
                    bool haveTimerToSchedule = false;
                    uint nextAppDomainTimerDuration = uint.MaxValue;
                    TimerQueueTimer timer = m_timers;
                    while (timer != null)
                    {
                                                                        uint elapsed;
                        if (timer.m_startTicks <= pauseTicks)
                            elapsed = (uint)(pauseTicks - timer.m_startTicks);
                        else
                            elapsed = (uint)(resumedTicks - timer.m_startTicks);
                        timer.m_dueTime = (timer.m_dueTime > elapsed) ? timer.m_dueTime - elapsed : 0;
                        ;
                        timer.m_startTicks = resumedTicks;
                        if (timer.m_dueTime < nextAppDomainTimerDuration)
                        {
                            haveTimerToSchedule = true;
                            nextAppDomainTimerDuration = timer.m_dueTime;
                        }

                        timer = timer.m_next;
                    }

                    if (haveTimerToSchedule)
                    {
                        EnsureAppDomainTimerFiresBy(nextAppDomainTimerDuration);
                    }
                }
            }
        }

        private void FireNextTimers()
        {
            TimerQueueTimer timerToFireOnThisThread = null;
            lock (this)
            {
                try
                {
                }
                finally
                {
                    m_isAppDomainTimerScheduled = false;
                    bool haveTimerToSchedule = false;
                    uint nextAppDomainTimerDuration = uint.MaxValue;
                    int nowTicks = TickCount;
                    TimerQueueTimer timer = m_timers;
                    while (timer != null)
                    {
                                                uint elapsed = (uint)(nowTicks - timer.m_startTicks);
                        if (elapsed >= timer.m_dueTime)
                        {
                            TimerQueueTimer nextTimer = timer.m_next;
                            if (timer.m_period != Timeout.UnsignedInfinite)
                            {
                                timer.m_startTicks = nowTicks;
                                timer.m_dueTime = timer.m_period;
                                if (timer.m_dueTime < nextAppDomainTimerDuration)
                                {
                                    haveTimerToSchedule = true;
                                    nextAppDomainTimerDuration = timer.m_dueTime;
                                }
                            }
                            else
                            {
                                DeleteTimer(timer);
                            }

                            if (timerToFireOnThisThread == null)
                                timerToFireOnThisThread = timer;
                            else
                                QueueTimerCompletion(timer);
                            timer = nextTimer;
                        }
                        else
                        {
                            uint remaining = timer.m_dueTime - elapsed;
                            if (remaining < nextAppDomainTimerDuration)
                            {
                                haveTimerToSchedule = true;
                                nextAppDomainTimerDuration = remaining;
                            }

                            timer = timer.m_next;
                        }
                    }

                    if (haveTimerToSchedule)
                        EnsureAppDomainTimerFiresBy(nextAppDomainTimerDuration);
                }
            }

            if (timerToFireOnThisThread != null)
                timerToFireOnThisThread.Fire();
        }

        private static void QueueTimerCompletion(TimerQueueTimer timer)
        {
            WaitCallback callback = s_fireQueuedTimerCompletion;
            if (callback == null)
                s_fireQueuedTimerCompletion = callback = new WaitCallback(FireQueuedTimerCompletion);
            ThreadPool.UnsafeQueueUserWorkItem(callback, timer);
        }

        private static WaitCallback s_fireQueuedTimerCompletion;
        private static void FireQueuedTimerCompletion(object state)
        {
            ((TimerQueueTimer)state).Fire();
        }

        public bool UpdateTimer(TimerQueueTimer timer, uint dueTime, uint period)
        {
            if (timer.m_dueTime == Timeout.UnsignedInfinite)
            {
                timer.m_next = m_timers;
                timer.m_prev = null;
                if (timer.m_next != null)
                    timer.m_next.m_prev = timer;
                m_timers = timer;
            }

            timer.m_dueTime = dueTime;
            timer.m_period = (period == 0) ? Timeout.UnsignedInfinite : period;
            timer.m_startTicks = TickCount;
            return EnsureAppDomainTimerFiresBy(dueTime);
        }

        public void DeleteTimer(TimerQueueTimer timer)
        {
            if (timer.m_dueTime != Timeout.UnsignedInfinite)
            {
                if (timer.m_next != null)
                    timer.m_next.m_prev = timer.m_prev;
                if (timer.m_prev != null)
                    timer.m_prev.m_next = timer.m_next;
                if (m_timers == timer)
                    m_timers = timer.m_next;
                timer.m_dueTime = Timeout.UnsignedInfinite;
                timer.m_period = Timeout.UnsignedInfinite;
                timer.m_startTicks = 0;
                timer.m_prev = null;
                timer.m_next = null;
            }
        }
    }

    sealed class TimerQueueTimer
    {
        internal TimerQueueTimer m_next;
        internal TimerQueueTimer m_prev;
        internal int m_startTicks;
        internal uint m_dueTime;
        internal uint m_period;
        readonly TimerCallback m_timerCallback;
        readonly Object m_state;
        readonly ExecutionContext m_executionContext;
        int m_callbacksRunning;
        volatile bool m_canceled;
        volatile WaitHandle m_notifyWhenNoCallbacksRunning;
        internal TimerQueueTimer(TimerCallback timerCallback, object state, uint dueTime, uint period, ref StackCrawlMark stackMark)
        {
            m_timerCallback = timerCallback;
            m_state = state;
            m_dueTime = Timeout.UnsignedInfinite;
            m_period = Timeout.UnsignedInfinite;
            if (!ExecutionContext.IsFlowSuppressed())
            {
                m_executionContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
            }

            if (dueTime != Timeout.UnsignedInfinite)
                Change(dueTime, period);
        }

        internal bool Change(uint dueTime, uint period)
        {
            bool success;
            lock (TimerQueue.Instance)
            {
                if (m_canceled)
                    throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
                try
                {
                }
                finally
                {
                    m_period = period;
                    if (dueTime == Timeout.UnsignedInfinite)
                    {
                        TimerQueue.Instance.DeleteTimer(this);
                        success = true;
                    }
                    else
                    {
                        if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                            FrameworkEventSource.Log.ThreadTransferSendObj(this, 1, string.Empty, true);
                        success = TimerQueue.Instance.UpdateTimer(this, dueTime, period);
                    }
                }
            }

            return success;
        }

        public void Close()
        {
            lock (TimerQueue.Instance)
            {
                try
                {
                }
                finally
                {
                    if (!m_canceled)
                    {
                        m_canceled = true;
                        TimerQueue.Instance.DeleteTimer(this);
                    }
                }
            }
        }

        public bool Close(WaitHandle toSignal)
        {
            bool success;
            bool shouldSignal = false;
            lock (TimerQueue.Instance)
            {
                try
                {
                }
                finally
                {
                    if (m_canceled)
                    {
                        success = false;
                    }
                    else
                    {
                        m_canceled = true;
                        m_notifyWhenNoCallbacksRunning = toSignal;
                        TimerQueue.Instance.DeleteTimer(this);
                        if (m_callbacksRunning == 0)
                            shouldSignal = true;
                        success = true;
                    }
                }
            }

            if (shouldSignal)
                SignalNoCallbacksRunning();
            return success;
        }

        internal void Fire()
        {
            bool canceled = false;
            lock (TimerQueue.Instance)
            {
                try
                {
                }
                finally
                {
                    canceled = m_canceled;
                    if (!canceled)
                        m_callbacksRunning++;
                }
            }

            if (canceled)
                return;
            CallCallback();
            bool shouldSignal = false;
            lock (TimerQueue.Instance)
            {
                try
                {
                }
                finally
                {
                    m_callbacksRunning--;
                    if (m_canceled && m_callbacksRunning == 0 && m_notifyWhenNoCallbacksRunning != null)
                        shouldSignal = true;
                }
            }

            if (shouldSignal)
                SignalNoCallbacksRunning();
        }

        internal void SignalNoCallbacksRunning()
        {
            Win32Native.SetEvent(m_notifyWhenNoCallbacksRunning.SafeWaitHandle);
        }

        internal void CallCallback()
        {
            if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                FrameworkEventSource.Log.ThreadTransferReceiveObj(this, 1, string.Empty);
            if (m_executionContext == null)
            {
                m_timerCallback(m_state);
            }
            else
            {
                using (ExecutionContext executionContext = m_executionContext.IsPreAllocatedDefault ? m_executionContext : m_executionContext.CreateCopy())
                {
                    ContextCallback callback = s_callCallbackInContext;
                    if (callback == null)
                        s_callCallbackInContext = callback = new ContextCallback(CallCallbackInContext);
                    ExecutionContext.Run(executionContext, callback, this, true);
                }
            }
        }

        private static ContextCallback s_callCallbackInContext;
        private static void CallCallbackInContext(object state)
        {
            TimerQueueTimer t = (TimerQueueTimer)state;
            t.m_timerCallback(t.m_state);
        }
    }

    sealed class TimerHolder
    {
        internal TimerQueueTimer m_timer;
        public TimerHolder(TimerQueueTimer timer)
        {
            m_timer = timer;
        }

        ~TimerHolder()
        {
            if (Environment.HasShutdownStarted || AppDomain.CurrentDomain.IsFinalizingForUnload())
                return;
            m_timer.Close();
        }

        public void Close()
        {
            m_timer.Close();
            GC.SuppressFinalize(this);
        }

        public bool Close(WaitHandle notifyObject)
        {
            bool result = m_timer.Close(notifyObject);
            GC.SuppressFinalize(this);
            return result;
        }
    }

    public sealed class Timer : IDisposable
    {
        private const UInt32 MAX_SUPPORTED_TIMEOUT = (uint)0xfffffffe;
        private TimerHolder m_timer;
        public Timer(TimerCallback callback, Object state, int dueTime, int period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (period < -1)
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback, state, (UInt32)dueTime, (UInt32)period, ref stackMark);
        }

        public Timer(TimerCallback callback, Object state, TimeSpan dueTime, TimeSpan period)
        {
            long dueTm = (long)dueTime.TotalMilliseconds;
            if (dueTm < -1)
                throw new ArgumentOutOfRangeException("dueTm", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (dueTm > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("dueTm", Environment.GetResourceString("ArgumentOutOfRange_TimeoutTooLarge"));
            long periodTm = (long)period.TotalMilliseconds;
            if (periodTm < -1)
                throw new ArgumentOutOfRangeException("periodTm", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (periodTm > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("periodTm", Environment.GetResourceString("ArgumentOutOfRange_PeriodTooLarge"));
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback, state, (UInt32)dueTm, (UInt32)periodTm, ref stackMark);
        }

        public Timer(TimerCallback callback, Object state, UInt32 dueTime, UInt32 period)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback, state, dueTime, period, ref stackMark);
        }

        public Timer(TimerCallback callback, Object state, long dueTime, long period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (period < -1)
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (dueTime > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_TimeoutTooLarge"));
            if (period > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_PeriodTooLarge"));
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback, state, (UInt32)dueTime, (UInt32)period, ref stackMark);
        }

        public Timer(TimerCallback callback)
        {
            int dueTime = -1;
            int period = -1;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            TimerSetup(callback, this, (UInt32)dueTime, (UInt32)period, ref stackMark);
        }

        private void TimerSetup(TimerCallback callback, Object state, UInt32 dueTime, UInt32 period, ref StackCrawlMark stackMark)
        {
            if (callback == null)
                throw new ArgumentNullException("TimerCallback");
                        m_timer = new TimerHolder(new TimerQueueTimer(callback, state, dueTime, period, ref stackMark));
        }

        internal static void Pause()
        {
            TimerQueue.Instance.Pause();
        }

        internal static void Resume()
        {
            TimerQueue.Instance.Resume();
        }

        public bool Change(int dueTime, int period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (period < -1)
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
                        return m_timer.m_timer.Change((UInt32)dueTime, (UInt32)period);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            return Change((long)dueTime.TotalMilliseconds, (long)period.TotalMilliseconds);
        }

        public bool Change(UInt32 dueTime, UInt32 period)
        {
            return m_timer.m_timer.Change(dueTime, period);
        }

        public bool Change(long dueTime, long period)
        {
            if (dueTime < -1)
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (period < -1)
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            if (dueTime > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("dueTime", Environment.GetResourceString("ArgumentOutOfRange_TimeoutTooLarge"));
            if (period > MAX_SUPPORTED_TIMEOUT)
                throw new ArgumentOutOfRangeException("period", Environment.GetResourceString("ArgumentOutOfRange_PeriodTooLarge"));
                        return m_timer.m_timer.Change((UInt32)dueTime, (UInt32)period);
        }

        public bool Dispose(WaitHandle notifyObject)
        {
            if (notifyObject == null)
                throw new ArgumentNullException("notifyObject");
                        return m_timer.Close(notifyObject);
        }

        public void Dispose()
        {
            m_timer.Close();
        }

        internal void KeepRootedWhileScheduled()
        {
            GC.SuppressFinalize(m_timer);
        }
    }
}