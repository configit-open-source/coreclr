using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Win32;
using System.Diagnostics.Tracing;

namespace System.Threading.Tasks
{
    internal class Shared<T>
    {
        internal T Value;
        internal Shared(T value)
        {
            this.Value = value;
        }
    }

    public enum TaskStatus
    {
        Created,
        WaitingForActivation,
        WaitingToRun,
        Running,
        WaitingForChildrenToComplete,
        RanToCompletion,
        Canceled,
        Faulted
    }

    public class Task : IThreadPoolWorkItem, IAsyncResult, IDisposable
    {
        internal static Task t_currentTask;
        private static StackGuard t_stackGuard;
        internal static int s_taskIdCounter;
        private readonly static TaskFactory s_factory = new TaskFactory();
        private volatile int m_taskId;
        internal object m_action;
        internal object m_stateObject;
        internal TaskScheduler m_taskScheduler;
        internal readonly Task m_parent;
        internal volatile int m_stateFlags;
        private const int OptionsMask = 0xFFFF;
        internal const int TASK_STATE_STARTED = 0x10000;
        internal const int TASK_STATE_DELEGATE_INVOKED = 0x20000;
        internal const int TASK_STATE_DISPOSED = 0x40000;
        internal const int TASK_STATE_EXCEPTIONOBSERVEDBYPARENT = 0x80000;
        internal const int TASK_STATE_CANCELLATIONACKNOWLEDGED = 0x100000;
        internal const int TASK_STATE_FAULTED = 0x200000;
        internal const int TASK_STATE_CANCELED = 0x400000;
        internal const int TASK_STATE_WAITING_ON_CHILDREN = 0x800000;
        internal const int TASK_STATE_RAN_TO_COMPLETION = 0x1000000;
        internal const int TASK_STATE_WAITINGFORACTIVATION = 0x2000000;
        internal const int TASK_STATE_COMPLETION_RESERVED = 0x4000000;
        internal const int TASK_STATE_THREAD_WAS_ABORTED = 0x8000000;
        internal const int TASK_STATE_WAIT_COMPLETION_NOTIFICATION = 0x10000000;
        internal const int TASK_STATE_EXECUTIONCONTEXT_IS_NULL = 0x20000000;
        internal const int TASK_STATE_TASKSCHEDULED_WAS_FIRED = 0x40000000;
        private const int TASK_STATE_COMPLETED_MASK = TASK_STATE_CANCELED | TASK_STATE_FAULTED | TASK_STATE_RAN_TO_COMPLETION;
        private const int CANCELLATION_REQUESTED = 0x1;
        private volatile object m_continuationObject = null;
        private static readonly object s_taskCompletionSentinel = new object ();
        internal static bool s_asyncDebuggingEnabled;
        private static readonly Dictionary<int, Task> s_currentActiveTasks = new Dictionary<int, Task>();
        private static readonly Object s_activeTasksLock = new Object();
        internal static bool AddToActiveTasks(Task task)
        {
            Contract.Requires(task != null, "Null Task objects can't be added to the ActiveTasks collection");
            lock (s_activeTasksLock)
            {
                s_currentActiveTasks[task.Id] = task;
            }

            return true;
        }

        internal static void RemoveFromActiveTasks(int taskId)
        {
            lock (s_activeTasksLock)
            {
                s_currentActiveTasks.Remove(taskId);
            }
        }

        internal class ContingentProperties
        {
            internal ExecutionContext m_capturedContext;
            internal volatile ManualResetEventSlim m_completionEvent;
            internal volatile TaskExceptionHolder m_exceptionsHolder;
            internal CancellationToken m_cancellationToken;
            internal Shared<CancellationTokenRegistration> m_cancellationRegistration;
            internal volatile int m_internalCancellationRequested;
            internal volatile int m_completionCountdown = 1;
            internal volatile List<Task> m_exceptionalChildren;
            internal void SetCompleted()
            {
                var mres = m_completionEvent;
                if (mres != null)
                    mres.Set();
            }

            internal void DeregisterCancellationCallback()
            {
                if (m_cancellationRegistration != null)
                {
                    try
                    {
                        m_cancellationRegistration.Value.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }

                    m_cancellationRegistration = null;
                }
            }
        }

        internal volatile ContingentProperties m_contingentProperties;
        internal Task(bool canceled, TaskCreationOptions creationOptions, CancellationToken ct)
        {
            int optionFlags = (int)creationOptions;
            if (canceled)
            {
                m_stateFlags = TASK_STATE_CANCELED | TASK_STATE_CANCELLATIONACKNOWLEDGED | optionFlags;
                ContingentProperties props;
                m_contingentProperties = props = new ContingentProperties();
                props.m_cancellationToken = ct;
                props.m_internalCancellationRequested = CANCELLATION_REQUESTED;
            }
            else
                m_stateFlags = TASK_STATE_RAN_TO_COMPLETION | optionFlags;
        }

        internal Task()
        {
            m_stateFlags = TASK_STATE_WAITINGFORACTIVATION | (int)InternalTaskOptions.PromiseTask;
        }

        internal Task(object state, TaskCreationOptions creationOptions, bool promiseStyle)
        {
            Contract.Assert(promiseStyle, "Promise CTOR: promiseStyle was false");
            if ((creationOptions & ~(TaskCreationOptions.AttachedToParent | TaskCreationOptions.RunContinuationsAsynchronously)) != 0)
            {
                throw new ArgumentOutOfRangeException("creationOptions");
            }

            if ((creationOptions & TaskCreationOptions.AttachedToParent) != 0)
                m_parent = Task.InternalCurrent;
            TaskConstructorCore(null, state, default (CancellationToken), creationOptions, InternalTaskOptions.PromiseTask, null);
        }

        public Task(Action action): this (action, null, null, default (CancellationToken), TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Action action, CancellationToken cancellationToken): this (action, null, null, cancellationToken, TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Action action, TaskCreationOptions creationOptions): this (action, null, Task.InternalCurrentIfAttached(creationOptions), default (CancellationToken), creationOptions, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions): this (action, null, Task.InternalCurrentIfAttached(creationOptions), cancellationToken, creationOptions, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Action<object> action, object state): this (action, state, null, default (CancellationToken), TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Action<object> action, object state, CancellationToken cancellationToken): this (action, state, null, cancellationToken, TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Action<object> action, object state, TaskCreationOptions creationOptions): this (action, state, Task.InternalCurrentIfAttached(creationOptions), default (CancellationToken), creationOptions, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions): this (action, state, Task.InternalCurrentIfAttached(creationOptions), cancellationToken, creationOptions, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        internal Task(Action<object> action, object state, Task parent, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler, ref StackCrawlMark stackMark): this (action, state, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
            PossiblyCaptureContext(ref stackMark);
        }

        internal Task(Delegate action, object state, Task parent, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            Contract.EndContractBlock();
            if (((creationOptions & TaskCreationOptions.AttachedToParent) != 0) || ((internalOptions & InternalTaskOptions.SelfReplicating) != 0))
            {
                m_parent = parent;
            }

            TaskConstructorCore(action, state, cancellationToken, creationOptions, internalOptions, scheduler);
        }

        internal void TaskConstructorCore(object action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler)
        {
            m_action = action;
            m_stateObject = state;
            m_taskScheduler = scheduler;
            if ((creationOptions & ~(TaskCreationOptions.AttachedToParent | TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler | TaskCreationOptions.PreferFairness | TaskCreationOptions.RunContinuationsAsynchronously)) != 0)
            {
                throw new ArgumentOutOfRangeException("creationOptions");
            }

            int illegalInternalOptions = (int)(internalOptions & ~(InternalTaskOptions.SelfReplicating | InternalTaskOptions.ChildReplica | InternalTaskOptions.PromiseTask | InternalTaskOptions.ContinuationTask | InternalTaskOptions.LazyCancellation | InternalTaskOptions.QueuedByRuntime));
            Contract.Assert(illegalInternalOptions == 0, "TaskConstructorCore: Illegal internal options");
            if (((creationOptions & TaskCreationOptions.LongRunning) != 0) && ((internalOptions & InternalTaskOptions.SelfReplicating) != 0))
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_ctor_LRandSR"));
            }

            Contract.Assert(m_stateFlags == 0, "TaskConstructorCore: non-zero m_stateFlags");
            Contract.Assert((((int)creationOptions) | OptionsMask) == OptionsMask, "TaskConstructorCore: options take too many bits");
            var tmpFlags = (int)creationOptions | (int)internalOptions;
            if ((m_action == null) || ((internalOptions & InternalTaskOptions.ContinuationTask) != 0))
            {
                tmpFlags |= TASK_STATE_WAITINGFORACTIVATION;
            }

            m_stateFlags = tmpFlags;
            if (m_parent != null && ((creationOptions & TaskCreationOptions.AttachedToParent) != 0) && ((m_parent.CreationOptions & TaskCreationOptions.DenyChildAttach) == 0))
            {
                m_parent.AddNewChild();
            }

            if (cancellationToken.CanBeCanceled)
            {
                Contract.Assert((internalOptions & (InternalTaskOptions.ChildReplica | InternalTaskOptions.SelfReplicating | InternalTaskOptions.ContinuationTask)) == 0, "TaskConstructorCore: Did not expect to see cancelable token for replica/replicating or continuation task.");
                AssignCancellationToken(cancellationToken, null, null);
            }
        }

        private void AssignCancellationToken(CancellationToken cancellationToken, Task antecedent, TaskContinuation continuation)
        {
            ContingentProperties props = EnsureContingentPropertiesInitialized(needsProtection: false);
            props.m_cancellationToken = cancellationToken;
            try
            {
                if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
                {
                    cancellationToken.ThrowIfSourceDisposed();
                }

                if ((((InternalTaskOptions)Options & (InternalTaskOptions.QueuedByRuntime | InternalTaskOptions.PromiseTask | InternalTaskOptions.LazyCancellation)) == 0))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        this.InternalCancel(false);
                    }
                    else
                    {
                        CancellationTokenRegistration ctr;
                        if (antecedent == null)
                        {
                            ctr = cancellationToken.InternalRegisterWithoutEC(s_taskCancelCallback, this);
                        }
                        else
                        {
                            ctr = cancellationToken.InternalRegisterWithoutEC(s_taskCancelCallback, new Tuple<Task, Task, TaskContinuation>(this, antecedent, continuation));
                        }

                        props.m_cancellationRegistration = new Shared<CancellationTokenRegistration>(ctr);
                    }
                }
            }
            catch
            {
                if ((m_parent != null) && ((Options & TaskCreationOptions.AttachedToParent) != 0) && ((m_parent.Options & TaskCreationOptions.DenyChildAttach) == 0))
                {
                    m_parent.DisregardChild();
                }

                throw;
            }
        }

        private readonly static Action<Object> s_taskCancelCallback = new Action<Object>(TaskCancelCallback);
        private static void TaskCancelCallback(Object o)
        {
            var targetTask = o as Task;
            if (targetTask == null)
            {
                var tuple = o as Tuple<Task, Task, TaskContinuation>;
                if (tuple != null)
                {
                    targetTask = tuple.Item1;
                    Task antecedentTask = tuple.Item2;
                    TaskContinuation continuation = tuple.Item3;
                    antecedentTask.RemoveContinuation(continuation);
                }
            }

            Contract.Assert(targetTask != null, "targetTask should have been non-null, with the supplied argument being a task or a tuple containing one");
            targetTask.InternalCancel(false);
        }

        private string DebuggerDisplayMethodDescription
        {
            get
            {
                Delegate d = (Delegate)m_action;
                return d != null ? d.Method.ToString() : "{null}";
            }
        }

        internal void PossiblyCaptureContext(ref StackCrawlMark stackMark)
        {
            Contract.Assert(m_contingentProperties == null || m_contingentProperties.m_capturedContext == null, "Captured an ExecutionContext when one was already captured.");
            CapturedContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
        }

        internal TaskCreationOptions Options
        {
            get
            {
                int stateFlags = m_stateFlags;
                return OptionsMethod(stateFlags);
            }
        }

        internal static TaskCreationOptions OptionsMethod(int flags)
        {
            Contract.Assert((OptionsMask & 1) == 1, "OptionsMask needs a shift in Options.get");
            return (TaskCreationOptions)(flags & OptionsMask);
        }

        internal bool AtomicStateUpdate(int newBits, int illegalBits)
        {
            SpinWait sw = new SpinWait();
            do
            {
                int oldFlags = m_stateFlags;
                if ((oldFlags & illegalBits) != 0)
                    return false;
                if (Interlocked.CompareExchange(ref m_stateFlags, oldFlags | newBits, oldFlags) == oldFlags)
                {
                    return true;
                }

                sw.SpinOnce();
            }
            while (true);
        }

        internal bool AtomicStateUpdate(int newBits, int illegalBits, ref int oldFlags)
        {
            SpinWait sw = new SpinWait();
            do
            {
                oldFlags = m_stateFlags;
                if ((oldFlags & illegalBits) != 0)
                    return false;
                if (Interlocked.CompareExchange(ref m_stateFlags, oldFlags | newBits, oldFlags) == oldFlags)
                {
                    return true;
                }

                sw.SpinOnce();
            }
            while (true);
        }

        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            Contract.Assert((Options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0, "Should only be used for promise-style tasks");
            if (enabled)
            {
                bool success = AtomicStateUpdate(TASK_STATE_WAIT_COMPLETION_NOTIFICATION, TASK_STATE_COMPLETED_MASK | TASK_STATE_COMPLETION_RESERVED);
                Contract.Assert(success, "Tried to set enabled on completed Task");
            }
            else
            {
                SpinWait sw = new SpinWait();
                while (true)
                {
                    int oldFlags = m_stateFlags;
                    int newFlags = oldFlags & (~TASK_STATE_WAIT_COMPLETION_NOTIFICATION);
                    if (Interlocked.CompareExchange(ref m_stateFlags, newFlags, oldFlags) == oldFlags)
                        break;
                    sw.SpinOnce();
                }
            }
        }

        internal bool NotifyDebuggerOfWaitCompletionIfNecessary()
        {
            if (IsWaitNotificationEnabled && ShouldNotifyDebuggerOfWaitCompletion)
            {
                NotifyDebuggerOfWaitCompletion();
                return true;
            }

            return false;
        }

        internal static bool AnyTaskRequiresNotifyDebuggerOfWaitCompletion(Task[] tasks)
        {
            Contract.Assert(tasks != null, "Expected non-null array of tasks");
            foreach (var task in tasks)
            {
                if (task != null && task.IsWaitNotificationEnabled && task.ShouldNotifyDebuggerOfWaitCompletion)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsWaitNotificationEnabledOrNotRanToCompletion
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (m_stateFlags & (Task.TASK_STATE_WAIT_COMPLETION_NOTIFICATION | Task.TASK_STATE_RAN_TO_COMPLETION)) != Task.TASK_STATE_RAN_TO_COMPLETION;
            }
        }

        internal virtual bool ShouldNotifyDebuggerOfWaitCompletion
        {
            get
            {
                bool isWaitNotificationEnabled = IsWaitNotificationEnabled;
                Contract.Assert(isWaitNotificationEnabled, "Should only be called if the wait completion bit is set.");
                return isWaitNotificationEnabled;
            }
        }

        internal bool IsWaitNotificationEnabled
        {
            get
            {
                return (m_stateFlags & TASK_STATE_WAIT_COMPLETION_NOTIFICATION) != 0;
            }
        }

        private void NotifyDebuggerOfWaitCompletion()
        {
            Contract.Assert(IsWaitNotificationEnabled, "Should only be called if the wait completion bit is set.");
            SetNotificationForWaitCompletion(enabled: false);
        }

        internal bool MarkStarted()
        {
            return AtomicStateUpdate(TASK_STATE_STARTED, TASK_STATE_CANCELED | TASK_STATE_STARTED);
        }

        internal bool FireTaskScheduledIfNeeded(TaskScheduler ts)
        {
            var etwLog = TplEtwProvider.Log;
            if (etwLog.IsEnabled() && (m_stateFlags & Task.TASK_STATE_TASKSCHEDULED_WAS_FIRED) == 0)
            {
                m_stateFlags |= Task.TASK_STATE_TASKSCHEDULED_WAS_FIRED;
                Task currentTask = Task.InternalCurrent;
                Task parentTask = this.m_parent;
                etwLog.TaskScheduled(ts.Id, currentTask == null ? 0 : currentTask.Id, this.Id, parentTask == null ? 0 : parentTask.Id, (int)this.Options, System.Threading.Thread.GetDomainID());
                return true;
            }
            else
                return false;
        }

        internal void AddNewChild()
        {
            Contract.Assert(Task.InternalCurrent == this || this.IsSelfReplicatingRoot, "Task.AddNewChild(): Called from an external context");
            var props = EnsureContingentPropertiesInitialized(needsProtection: true);
            if (props.m_completionCountdown == 1 && !IsSelfReplicatingRoot)
            {
                props.m_completionCountdown++;
            }
            else
            {
                Interlocked.Increment(ref props.m_completionCountdown);
            }
        }

        internal void DisregardChild()
        {
            Contract.Assert(Task.InternalCurrent == this, "Task.DisregardChild(): Called from an external context");
            var props = EnsureContingentPropertiesInitialized(needsProtection: true);
            Contract.Assert(props.m_completionCountdown >= 2, "Task.DisregardChild(): Expected parent count to be >= 2");
            Interlocked.Decrement(ref props.m_completionCountdown);
        }

        public void Start()
        {
            Start(TaskScheduler.Current);
        }

        public void Start(TaskScheduler scheduler)
        {
            int flags = m_stateFlags;
            if (IsCompletedMethod(flags))
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_Start_TaskCompleted"));
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            var options = OptionsMethod(flags);
            if ((options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_Start_Promise"));
            }

            if ((options & (TaskCreationOptions)InternalTaskOptions.ContinuationTask) != 0)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_Start_ContinuationTask"));
            }

            if (Interlocked.CompareExchange(ref m_taskScheduler, scheduler, null) != null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_Start_AlreadyStarted"));
            }

            ScheduleAndStart(true);
        }

        public void RunSynchronously()
        {
            InternalRunSynchronously(TaskScheduler.Current, waitForCompletion: true);
        }

        public void RunSynchronously(TaskScheduler scheduler)
        {
            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            Contract.EndContractBlock();
            InternalRunSynchronously(scheduler, waitForCompletion: true);
        }

        internal void InternalRunSynchronously(TaskScheduler scheduler, bool waitForCompletion)
        {
            Contract.Requires(scheduler != null, "Task.InternalRunSynchronously(): null TaskScheduler");
            int flags = m_stateFlags;
            var options = OptionsMethod(flags);
            if ((options & (TaskCreationOptions)InternalTaskOptions.ContinuationTask) != 0)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_Continuation"));
            }

            if ((options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_Promise"));
            }

            if (IsCompletedMethod(flags))
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_TaskCompleted"));
            }

            if (Interlocked.CompareExchange(ref m_taskScheduler, scheduler, null) != null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_AlreadyStarted"));
            }

            if (MarkStarted())
            {
                bool taskQueued = false;
                try
                {
                    if (!scheduler.TryRunInline(this, false))
                    {
                        scheduler.InternalQueueTask(this);
                        taskQueued = true;
                    }

                    if (waitForCompletion && !IsCompleted)
                    {
                        SpinThenBlockingWait(Timeout.Infinite, default (CancellationToken));
                    }
                }
                catch (Exception e)
                {
                    if (!taskQueued && !(e is ThreadAbortException))
                    {
                        TaskSchedulerException tse = new TaskSchedulerException(e);
                        AddException(tse);
                        Finish(false);
                        Contract.Assert((m_contingentProperties != null) && (m_contingentProperties.m_exceptionsHolder != null) && (m_contingentProperties.m_exceptionsHolder.ContainsFaultList), "Task.InternalRunSynchronously(): Expected m_contingentProperties.m_exceptionsHolder to exist " + "and to have faults recorded.");
                        m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
                        throw tse;
                    }
                    else
                        throw;
                }
            }
            else
            {
                Contract.Assert((m_stateFlags & TASK_STATE_CANCELED) != 0, "Task.RunSynchronously: expected TASK_STATE_CANCELED to be set");
                throw new InvalidOperationException(Environment.GetResourceString("Task_RunSynchronously_TaskCompleted"));
            }
        }

        internal static Task InternalStartNew(Task creatingTask, Delegate action, object state, CancellationToken cancellationToken, TaskScheduler scheduler, TaskCreationOptions options, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark)
        {
            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            Contract.EndContractBlock();
            Task t = new Task(action, state, creatingTask, cancellationToken, options, internalOptions | InternalTaskOptions.QueuedByRuntime, scheduler);
            t.PossiblyCaptureContext(ref stackMark);
            t.ScheduleAndStart(false);
            return t;
        }

        internal static int NewId()
        {
            int newId = 0;
            do
            {
                newId = Interlocked.Increment(ref s_taskIdCounter);
            }
            while (newId == 0);
            TplEtwProvider.Log.NewID(newId);
            return newId;
        }

        public int Id
        {
            get
            {
                if (m_taskId == 0)
                {
                    int newId = NewId();
                    Interlocked.CompareExchange(ref m_taskId, newId, 0);
                }

                return m_taskId;
            }
        }

        public static int ? CurrentId
        {
            get
            {
                Task currentTask = InternalCurrent;
                if (currentTask != null)
                    return currentTask.Id;
                else
                    return null;
            }
        }

        internal static Task InternalCurrent
        {
            get
            {
                return t_currentTask;
            }
        }

        internal static Task InternalCurrentIfAttached(TaskCreationOptions creationOptions)
        {
            return (creationOptions & TaskCreationOptions.AttachedToParent) != 0 ? InternalCurrent : null;
        }

        internal static StackGuard CurrentStackGuard
        {
            get
            {
                StackGuard sg = t_stackGuard;
                if (sg == null)
                {
                    t_stackGuard = sg = new StackGuard();
                }

                return sg;
            }
        }

        public AggregateException Exception
        {
            get
            {
                AggregateException e = null;
                if (IsFaulted)
                    e = GetExceptions(false);
                Contract.Assert((e == null) || IsFaulted, "Task.Exception_get(): returning non-null value when not Faulted");
                return e;
            }
        }

        public TaskStatus Status
        {
            get
            {
                TaskStatus rval;
                int sf = m_stateFlags;
                if ((sf & TASK_STATE_FAULTED) != 0)
                    rval = TaskStatus.Faulted;
                else if ((sf & TASK_STATE_CANCELED) != 0)
                    rval = TaskStatus.Canceled;
                else if ((sf & TASK_STATE_RAN_TO_COMPLETION) != 0)
                    rval = TaskStatus.RanToCompletion;
                else if ((sf & TASK_STATE_WAITING_ON_CHILDREN) != 0)
                    rval = TaskStatus.WaitingForChildrenToComplete;
                else if ((sf & TASK_STATE_DELEGATE_INVOKED) != 0)
                    rval = TaskStatus.Running;
                else if ((sf & TASK_STATE_STARTED) != 0)
                    rval = TaskStatus.WaitingToRun;
                else if ((sf & TASK_STATE_WAITINGFORACTIVATION) != 0)
                    rval = TaskStatus.WaitingForActivation;
                else
                    rval = TaskStatus.Created;
                return rval;
            }
        }

        public bool IsCanceled
        {
            get
            {
                return (m_stateFlags & (TASK_STATE_CANCELED | TASK_STATE_FAULTED)) == TASK_STATE_CANCELED;
            }
        }

        internal bool IsCancellationRequested
        {
            get
            {
                var props = m_contingentProperties;
                return props != null && (props.m_internalCancellationRequested == CANCELLATION_REQUESTED || props.m_cancellationToken.IsCancellationRequested);
            }
        }

        internal ContingentProperties EnsureContingentPropertiesInitialized(bool needsProtection)
        {
            var props = m_contingentProperties;
            return props != null ? props : EnsureContingentPropertiesInitializedCore(needsProtection);
        }

        private ContingentProperties EnsureContingentPropertiesInitializedCore(bool needsProtection)
        {
            if (needsProtection)
            {
                return LazyInitializer.EnsureInitialized<ContingentProperties>(ref m_contingentProperties, s_createContingentProperties);
            }
            else
            {
                Contract.Assert(m_contingentProperties == null, "Expected props to be null after checking and with needsProtection == false");
                return m_contingentProperties = new ContingentProperties();
            }
        }

        private static readonly Func<ContingentProperties> s_createContingentProperties = () => new ContingentProperties();
        internal CancellationToken CancellationToken
        {
            get
            {
                var props = m_contingentProperties;
                return (props == null) ? default (CancellationToken) : props.m_cancellationToken;
            }
        }

        internal bool IsCancellationAcknowledged
        {
            get
            {
                return (m_stateFlags & TASK_STATE_CANCELLATIONACKNOWLEDGED) != 0;
            }
        }

        public bool IsCompleted
        {
            get
            {
                int stateFlags = m_stateFlags;
                return IsCompletedMethod(stateFlags);
            }
        }

        private static bool IsCompletedMethod(int flags)
        {
            return (flags & TASK_STATE_COMPLETED_MASK) != 0;
        }

        internal bool IsRanToCompletion
        {
            get
            {
                return (m_stateFlags & TASK_STATE_COMPLETED_MASK) == TASK_STATE_RAN_TO_COMPLETION;
            }
        }

        public TaskCreationOptions CreationOptions
        {
            get
            {
                return Options & (TaskCreationOptions)(~InternalTaskOptions.InternalOptionsMask);
            }
        }

        WaitHandle IAsyncResult.AsyncWaitHandle
        {
            get
            {
                bool isDisposed = (m_stateFlags & TASK_STATE_DISPOSED) != 0;
                if (isDisposed)
                {
                    throw new ObjectDisposedException(null, Environment.GetResourceString("Task_ThrowIfDisposed"));
                }

                return CompletedEvent.WaitHandle;
            }
        }

        public object AsyncState
        {
            get
            {
                return m_stateObject;
            }
        }

        bool IAsyncResult.CompletedSynchronously
        {
            get
            {
                return false;
            }
        }

        internal TaskScheduler ExecutingTaskScheduler
        {
            get
            {
                return m_taskScheduler;
            }
        }

        public static TaskFactory Factory
        {
            get
            {
                return s_factory;
            }
        }

        private static Task s_completedTask;
        public static Task CompletedTask
        {
            get
            {
                var completedTask = s_completedTask;
                if (completedTask == null)
                    s_completedTask = completedTask = new Task(false, (TaskCreationOptions)InternalTaskOptions.DoNotDispose, default (CancellationToken));
                return completedTask;
            }
        }

        internal ManualResetEventSlim CompletedEvent
        {
            get
            {
                var contingentProps = EnsureContingentPropertiesInitialized(needsProtection: true);
                if (contingentProps.m_completionEvent == null)
                {
                    bool wasCompleted = IsCompleted;
                    ManualResetEventSlim newMre = new ManualResetEventSlim(wasCompleted);
                    if (Interlocked.CompareExchange(ref contingentProps.m_completionEvent, newMre, null) != null)
                    {
                        newMre.Dispose();
                    }
                    else if (!wasCompleted && IsCompleted)
                    {
                        newMre.Set();
                    }
                }

                return contingentProps.m_completionEvent;
            }
        }

        internal bool IsSelfReplicatingRoot
        {
            get
            {
                return (Options & (TaskCreationOptions)(InternalTaskOptions.SelfReplicating | InternalTaskOptions.ChildReplica)) == (TaskCreationOptions)InternalTaskOptions.SelfReplicating;
            }
        }

        internal bool IsChildReplica
        {
            get
            {
                return (Options & (TaskCreationOptions)InternalTaskOptions.ChildReplica) != 0;
            }
        }

        internal int ActiveChildCount
        {
            get
            {
                var props = m_contingentProperties;
                return props != null ? props.m_completionCountdown - 1 : 0;
            }
        }

        internal bool ExceptionRecorded
        {
            get
            {
                var props = m_contingentProperties;
                return (props != null) && (props.m_exceptionsHolder != null) && (props.m_exceptionsHolder.ContainsFaultList);
            }
        }

        public bool IsFaulted
        {
            get
            {
                return ((m_stateFlags & TASK_STATE_FAULTED) != 0);
            }
        }

        internal ExecutionContext CapturedContext
        {
            get
            {
                if ((m_stateFlags & TASK_STATE_EXECUTIONCONTEXT_IS_NULL) == TASK_STATE_EXECUTIONCONTEXT_IS_NULL)
                {
                    return null;
                }
                else
                {
                    var props = m_contingentProperties;
                    if (props != null && props.m_capturedContext != null)
                        return props.m_capturedContext;
                    else
                        return ExecutionContext.PreAllocatedDefault;
                }
            }

            set
            {
                if (value == null)
                {
                    m_stateFlags |= TASK_STATE_EXECUTIONCONTEXT_IS_NULL;
                }
                else if (!value.IsPreAllocatedDefault)
                {
                    EnsureContingentPropertiesInitialized(needsProtection: false).m_capturedContext = value;
                }
            }
        }

        private static ExecutionContext CopyExecutionContext(ExecutionContext capturedContext)
        {
            if (capturedContext == null)
                return null;
            if (capturedContext.IsPreAllocatedDefault)
                return ExecutionContext.PreAllocatedDefault;
            return capturedContext.CreateCopy();
        }

        internal int InternalId
        {
            get
            {
                return GetHashCode();
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
                if ((Options & (TaskCreationOptions)InternalTaskOptions.DoNotDispose) != 0)
                {
                    return;
                }

                if (!IsCompleted)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("Task_Dispose_NotCompleted"));
                }

                var cp = m_contingentProperties;
                if (cp != null)
                {
                    var ev = cp.m_completionEvent;
                    if (ev != null)
                    {
                        cp.m_completionEvent = null;
                        if (!ev.IsSet)
                            ev.Set();
                        ev.Dispose();
                    }
                }
            }

            m_stateFlags |= TASK_STATE_DISPOSED;
        }

        internal void ScheduleAndStart(bool needsProtection)
        {
            Contract.Assert(m_taskScheduler != null, "expected a task scheduler to have been selected");
            Contract.Assert((m_stateFlags & TASK_STATE_STARTED) == 0, "task has already started");
            if (needsProtection)
            {
                if (!MarkStarted())
                {
                    return;
                }
            }
            else
            {
                m_stateFlags |= TASK_STATE_STARTED;
            }

            if (s_asyncDebuggingEnabled)
            {
                AddToActiveTasks(this);
            }

            if (AsyncCausalityTracer.LoggingOn && (Options & (TaskCreationOptions)InternalTaskOptions.ContinuationTask) == 0)
            {
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "Task: " + ((Delegate)m_action).Method.Name, 0);
            }

            try
            {
                m_taskScheduler.InternalQueueTask(this);
            }
            catch (ThreadAbortException tae)
            {
                AddException(tae);
                FinishThreadAbortedTask(true, false);
            }
            catch (Exception e)
            {
                TaskSchedulerException tse = new TaskSchedulerException(e);
                AddException(tse);
                Finish(false);
                if ((Options & (TaskCreationOptions)InternalTaskOptions.ContinuationTask) == 0)
                {
                    Contract.Assert((m_contingentProperties != null) && (m_contingentProperties.m_exceptionsHolder != null) && (m_contingentProperties.m_exceptionsHolder.ContainsFaultList), "Task.ScheduleAndStart(): Expected m_contingentProperties.m_exceptionsHolder to exist " + "and to have faults recorded.");
                    m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
                }

                throw tse;
            }
        }

        internal void AddException(object exceptionObject)
        {
            Contract.Requires(exceptionObject != null, "Task.AddException: Expected a non-null exception object");
            AddException(exceptionObject, representsCancellation: false);
        }

        internal void AddException(object exceptionObject, bool representsCancellation)
        {
            Contract.Requires(exceptionObject != null, "Task.AddException: Expected a non-null exception object");
            var eoAsException = exceptionObject as Exception;
            var eoAsEnumerableException = exceptionObject as IEnumerable<Exception>;
            var eoAsEdi = exceptionObject as ExceptionDispatchInfo;
            var eoAsEnumerableEdi = exceptionObject as IEnumerable<ExceptionDispatchInfo>;
            Contract.Assert(eoAsException != null || eoAsEnumerableException != null || eoAsEdi != null || eoAsEnumerableEdi != null, "Task.AddException: Expected an Exception, ExceptionDispatchInfo, or an IEnumerable<> of one of those");
            var eoAsOce = exceptionObject as OperationCanceledException;
            Contract.Assert(!representsCancellation || eoAsOce != null || (eoAsEdi != null && eoAsEdi.SourceException is OperationCanceledException), "representsCancellation should be true only if an OCE was provided.");
            var props = EnsureContingentPropertiesInitialized(needsProtection: true);
            if (props.m_exceptionsHolder == null)
            {
                TaskExceptionHolder holder = new TaskExceptionHolder(this);
                if (Interlocked.CompareExchange(ref props.m_exceptionsHolder, holder, null) != null)
                {
                    holder.MarkAsHandled(false);
                }
            }

            lock (props)
            {
                props.m_exceptionsHolder.Add(exceptionObject, representsCancellation);
            }
        }

        private AggregateException GetExceptions(bool includeTaskCanceledExceptions)
        {
            Exception canceledException = null;
            if (includeTaskCanceledExceptions && IsCanceled)
            {
                canceledException = new TaskCanceledException(this);
            }

            if (ExceptionRecorded)
            {
                Contract.Assert(m_contingentProperties != null);
                return m_contingentProperties.m_exceptionsHolder.CreateExceptionObject(false, canceledException);
            }
            else if (canceledException != null)
            {
                return new AggregateException(canceledException);
            }

            return null;
        }

        internal ReadOnlyCollection<ExceptionDispatchInfo> GetExceptionDispatchInfos()
        {
            bool exceptionsAvailable = IsFaulted && ExceptionRecorded;
            Contract.Assert(exceptionsAvailable, "Must only be used when the task has faulted with exceptions.");
            return exceptionsAvailable ? m_contingentProperties.m_exceptionsHolder.GetExceptionDispatchInfos() : new ReadOnlyCollection<ExceptionDispatchInfo>(new ExceptionDispatchInfo[0]);
        }

        internal ExceptionDispatchInfo GetCancellationExceptionDispatchInfo()
        {
            Contract.Assert(IsCanceled, "Must only be used when the task has canceled.");
            var props = m_contingentProperties;
            if (props == null)
                return null;
            var holder = props.m_exceptionsHolder;
            if (holder == null)
                return null;
            return holder.GetCancellationExceptionDispatchInfo();
        }

        internal void ThrowIfExceptional(bool includeTaskCanceledExceptions)
        {
            Contract.Requires(IsCompleted, "ThrowIfExceptional(): Expected IsCompleted == true");
            Exception exception = GetExceptions(includeTaskCanceledExceptions);
            if (exception != null)
            {
                UpdateExceptionObservedStatus();
                throw exception;
            }
        }

        internal void UpdateExceptionObservedStatus()
        {
            if ((m_parent != null) && ((Options & TaskCreationOptions.AttachedToParent) != 0) && ((m_parent.CreationOptions & TaskCreationOptions.DenyChildAttach) == 0) && Task.InternalCurrent == m_parent)
            {
                m_stateFlags |= TASK_STATE_EXCEPTIONOBSERVEDBYPARENT;
            }
        }

        internal bool IsExceptionObservedByParent
        {
            get
            {
                return (m_stateFlags & TASK_STATE_EXCEPTIONOBSERVEDBYPARENT) != 0;
            }
        }

        internal bool IsDelegateInvoked
        {
            get
            {
                return (m_stateFlags & TASK_STATE_DELEGATE_INVOKED) != 0;
            }
        }

        internal void Finish(bool bUserDelegateExecuted)
        {
            if (!bUserDelegateExecuted)
            {
                FinishStageTwo();
            }
            else
            {
                var props = m_contingentProperties;
                if (props == null || (props.m_completionCountdown == 1 && !IsSelfReplicatingRoot) || Interlocked.Decrement(ref props.m_completionCountdown) == 0)
                {
                    FinishStageTwo();
                }
                else
                {
                    AtomicStateUpdate(TASK_STATE_WAITING_ON_CHILDREN, TASK_STATE_FAULTED | TASK_STATE_CANCELED | TASK_STATE_RAN_TO_COMPLETION);
                }

                List<Task> exceptionalChildren = props != null ? props.m_exceptionalChildren : null;
                if (exceptionalChildren != null)
                {
                    lock (exceptionalChildren)
                    {
                        exceptionalChildren.RemoveAll(s_IsExceptionObservedByParentPredicate);
                    }
                }
            }
        }

        private readonly static Predicate<Task> s_IsExceptionObservedByParentPredicate = new Predicate<Task>((t) =>
        {
            return t.IsExceptionObservedByParent;
        }

        );
        internal void FinishStageTwo()
        {
            AddExceptionsFromChildren();
            int completionState;
            if (ExceptionRecorded)
            {
                completionState = TASK_STATE_FAULTED;
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Error);
                if (Task.s_asyncDebuggingEnabled)
                {
                    RemoveFromActiveTasks(this.Id);
                }
            }
            else if (IsCancellationRequested && IsCancellationAcknowledged)
            {
                completionState = TASK_STATE_CANCELED;
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Canceled);
                if (Task.s_asyncDebuggingEnabled)
                {
                    RemoveFromActiveTasks(this.Id);
                }
            }
            else
            {
                completionState = TASK_STATE_RAN_TO_COMPLETION;
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
                if (Task.s_asyncDebuggingEnabled)
                {
                    RemoveFromActiveTasks(this.Id);
                }
            }

            Interlocked.Exchange(ref m_stateFlags, m_stateFlags | completionState);
            var cp = m_contingentProperties;
            if (cp != null)
            {
                cp.SetCompleted();
                cp.DeregisterCancellationCallback();
            }

            FinishStageThree();
        }

        internal void FinishStageThree()
        {
            m_action = null;
            if (m_parent != null && ((m_parent.CreationOptions & TaskCreationOptions.DenyChildAttach) == 0) && (((TaskCreationOptions)(m_stateFlags & OptionsMask)) & TaskCreationOptions.AttachedToParent) != 0)
            {
                m_parent.ProcessChildCompletion(this);
            }

            FinishContinuations();
        }

        internal void ProcessChildCompletion(Task childTask)
        {
            Contract.Requires(childTask != null);
            Contract.Requires(childTask.IsCompleted, "ProcessChildCompletion was called for an uncompleted task");
            Contract.Assert(childTask.m_parent == this, "ProcessChildCompletion should only be called for a child of this task");
            var props = m_contingentProperties;
            if (childTask.IsFaulted && !childTask.IsExceptionObservedByParent)
            {
                if (props.m_exceptionalChildren == null)
                {
                    Interlocked.CompareExchange(ref props.m_exceptionalChildren, new List<Task>(), null);
                }

                List<Task> tmp = props.m_exceptionalChildren;
                if (tmp != null)
                {
                    lock (tmp)
                    {
                        tmp.Add(childTask);
                    }
                }
            }

            if (Interlocked.Decrement(ref props.m_completionCountdown) == 0)
            {
                FinishStageTwo();
            }
        }

        internal void AddExceptionsFromChildren()
        {
            var props = m_contingentProperties;
            List<Task> tmp = (props != null) ? props.m_exceptionalChildren : null;
            if (tmp != null)
            {
                lock (tmp)
                {
                    foreach (Task task in tmp)
                    {
                        Contract.Assert(task.IsCompleted, "Expected all tasks in list to be completed");
                        if (task.IsFaulted && !task.IsExceptionObservedByParent)
                        {
                            TaskExceptionHolder exceptionHolder = task.m_contingentProperties.m_exceptionsHolder;
                            Contract.Assert(exceptionHolder != null);
                            AddException(exceptionHolder.CreateExceptionObject(false, null));
                        }
                    }
                }

                props.m_exceptionalChildren = null;
            }
        }

        internal void FinishThreadAbortedTask(bool bTAEAddedToExceptionHolder, bool delegateRan)
        {
            Contract.Assert(!bTAEAddedToExceptionHolder || (m_contingentProperties != null && m_contingentProperties.m_exceptionsHolder != null), "FinishThreadAbortedTask() called on a task whose exception holder wasn't initialized");
            if (bTAEAddedToExceptionHolder)
                m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
            if (!AtomicStateUpdate(TASK_STATE_THREAD_WAS_ABORTED, TASK_STATE_THREAD_WAS_ABORTED | TASK_STATE_RAN_TO_COMPLETION | TASK_STATE_FAULTED | TASK_STATE_CANCELED))
            {
                return;
            }

            Finish(delegateRan);
        }

        private void Execute()
        {
            if (IsSelfReplicatingRoot)
            {
                ExecuteSelfReplicating(this);
            }
            else
            {
                try
                {
                    InnerInvoke();
                }
                catch (ThreadAbortException tae)
                {
                    if (!IsChildReplica)
                    {
                        HandleException(tae);
                        FinishThreadAbortedTask(true, true);
                    }
                }
                catch (Exception exn)
                {
                    HandleException(exn);
                }
            }
        }

        internal virtual bool ShouldReplicate()
        {
            return true;
        }

        internal virtual Task CreateReplicaTask(Action<object> taskReplicaDelegate, Object stateObject, Task parentTask, TaskScheduler taskScheduler, TaskCreationOptions creationOptionsForReplica, InternalTaskOptions internalOptionsForReplica)
        {
            return new Task(taskReplicaDelegate, stateObject, parentTask, default (CancellationToken), creationOptionsForReplica, internalOptionsForReplica, parentTask.ExecutingTaskScheduler);
        }

        internal virtual Object SavedStateForNextReplica
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        internal virtual Object SavedStateFromPreviousReplica
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        internal virtual Task HandedOverChildReplica
        {
            get
            {
                return null;
            }

            set
            {
            }
        }

        private static void ExecuteSelfReplicating(Task root)
        {
            TaskCreationOptions creationOptionsForReplicas = root.CreationOptions | TaskCreationOptions.AttachedToParent;
            InternalTaskOptions internalOptionsForReplicas = InternalTaskOptions.ChildReplica | InternalTaskOptions.SelfReplicating | InternalTaskOptions.QueuedByRuntime;
            bool replicasAreQuitting = false;
            Action<object> taskReplicaDelegate = null;
            taskReplicaDelegate = delegate
            {
                Task currentTask = Task.InternalCurrent;
                Task childTask = currentTask.HandedOverChildReplica;
                if (childTask == null)
                {
                    if (!root.ShouldReplicate())
                        return;
                    if (Volatile.Read(ref replicasAreQuitting))
                    {
                        return;
                    }

                    ExecutionContext creatorContext = root.CapturedContext;
                    childTask = root.CreateReplicaTask(taskReplicaDelegate, root.m_stateObject, root, root.ExecutingTaskScheduler, creationOptionsForReplicas, internalOptionsForReplicas);
                    childTask.CapturedContext = CopyExecutionContext(creatorContext);
                    childTask.ScheduleAndStart(false);
                }

                try
                {
                    root.InnerInvokeWithArg(currentTask);
                }
                catch (Exception exn)
                {
                    root.HandleException(exn);
                    if (exn is ThreadAbortException)
                    {
                        currentTask.FinishThreadAbortedTask(false, true);
                    }
                }

                Object savedState = currentTask.SavedStateForNextReplica;
                if (savedState != null)
                {
                    Task replacementReplica = root.CreateReplicaTask(taskReplicaDelegate, root.m_stateObject, root, root.ExecutingTaskScheduler, creationOptionsForReplicas, internalOptionsForReplicas);
                    ExecutionContext creatorContext = root.CapturedContext;
                    replacementReplica.CapturedContext = CopyExecutionContext(creatorContext);
                    replacementReplica.HandedOverChildReplica = childTask;
                    replacementReplica.SavedStateFromPreviousReplica = savedState;
                    replacementReplica.ScheduleAndStart(false);
                }
                else
                {
                    replicasAreQuitting = true;
                    try
                    {
                        childTask.InternalCancel(true);
                    }
                    catch (Exception e)
                    {
                        root.HandleException(e);
                    }
                }
            }

            ;
            taskReplicaDelegate(null);
        }

        void IThreadPoolWorkItem.ExecuteWorkItem()
        {
            ExecuteEntry(false);
        }

        void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
        {
            if (!IsCompleted)
            {
                HandleException(tae);
                FinishThreadAbortedTask(true, false);
            }
        }

        internal bool ExecuteEntry(bool bPreventDoubleExecution)
        {
            if (bPreventDoubleExecution || ((Options & (TaskCreationOptions)InternalTaskOptions.SelfReplicating) != 0))
            {
                int previousState = 0;
                if (!AtomicStateUpdate(TASK_STATE_DELEGATE_INVOKED, TASK_STATE_DELEGATE_INVOKED | TASK_STATE_COMPLETED_MASK, ref previousState) && (previousState & TASK_STATE_CANCELED) == 0)
                {
                    return false;
                }
            }
            else
            {
                m_stateFlags |= TASK_STATE_DELEGATE_INVOKED;
            }

            if (!IsCancellationRequested && !IsCanceled)
            {
                ExecuteWithThreadLocal(ref t_currentTask);
            }
            else if (!IsCanceled)
            {
                int prevState = Interlocked.Exchange(ref m_stateFlags, m_stateFlags | TASK_STATE_CANCELED);
                if ((prevState & TASK_STATE_CANCELED) == 0)
                {
                    CancellationCleanupLogic();
                }
            }

            return true;
        }

        private void ExecuteWithThreadLocal(ref Task currentTaskSlot)
        {
            Task previousTask = currentTaskSlot;
            var etwLog = TplEtwProvider.Log;
            Guid savedActivityID = new Guid();
            bool etwIsEnabled = etwLog.IsEnabled();
            if (etwIsEnabled)
            {
                if (etwLog.TasksSetActivityIds)
                    EventSource.SetCurrentThreadActivityId(TplEtwProvider.CreateGuidForTaskID(this.Id), out savedActivityID);
                if (previousTask != null)
                    etwLog.TaskStarted(previousTask.m_taskScheduler.Id, previousTask.Id, this.Id);
                else
                    etwLog.TaskStarted(TaskScheduler.Current.Id, 0, this.Id);
            }

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceSynchronousWorkStart(CausalityTraceLevel.Required, this.Id, CausalitySynchronousWork.Execution);
            try
            {
                currentTaskSlot = this;
                ExecutionContext ec = CapturedContext;
                if (ec == null)
                {
                    Execute();
                }
                else
                {
                    if (IsSelfReplicatingRoot || IsChildReplica)
                    {
                        CapturedContext = CopyExecutionContext(ec);
                    }

                    var callback = s_ecCallback;
                    if (callback == null)
                        s_ecCallback = callback = new ContextCallback(ExecutionContextCallback);
                    ExecutionContext.Run(ec, callback, this, true);
                }

                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceSynchronousWorkCompletion(CausalityTraceLevel.Required, CausalitySynchronousWork.Execution);
                Finish(true);
            }
            finally
            {
                currentTaskSlot = previousTask;
                if (etwIsEnabled)
                {
                    if (previousTask != null)
                        etwLog.TaskCompleted(previousTask.m_taskScheduler.Id, previousTask.Id, this.Id, IsFaulted);
                    else
                        etwLog.TaskCompleted(TaskScheduler.Current.Id, 0, this.Id, IsFaulted);
                    if (etwLog.TasksSetActivityIds)
                        EventSource.SetCurrentThreadActivityId(savedActivityID);
                }
            }
        }

        private static ContextCallback s_ecCallback;
        private static void ExecutionContextCallback(object obj)
        {
            Task task = obj as Task;
            Contract.Assert(task != null, "expected a task object");
            task.Execute();
        }

        internal virtual void InnerInvoke()
        {
            Contract.Assert(m_action != null, "Null action in InnerInvoke()");
            var action = m_action as Action;
            if (action != null)
            {
                action();
                return;
            }

            var actionWithState = m_action as Action<object>;
            if (actionWithState != null)
            {
                actionWithState(m_stateObject);
                return;
            }

            Contract.Assert(false, "Invalid m_action in Task");
        }

        internal void InnerInvokeWithArg(Task childTask)
        {
            InnerInvoke();
        }

        private void HandleException(Exception unhandledException)
        {
            Contract.Requires(unhandledException != null);
            OperationCanceledException exceptionAsOce = unhandledException as OperationCanceledException;
            if (exceptionAsOce != null && IsCancellationRequested && m_contingentProperties.m_cancellationToken == exceptionAsOce.CancellationToken)
            {
                SetCancellationAcknowledged();
                AddException(exceptionAsOce, representsCancellation: true);
            }
            else
            {
                AddException(unhandledException);
            }
        }

        public TaskAwaiter GetAwaiter()
        {
            return new TaskAwaiter(this);
        }

        public ConfiguredTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
        {
            return new ConfiguredTaskAwaitable(this, continueOnCapturedContext);
        }

        internal void SetContinuationForAwait(Action continuationAction, bool continueOnCapturedContext, bool flowExecutionContext, ref StackCrawlMark stackMark)
        {
            Contract.Requires(continuationAction != null);
            TaskContinuation tc = null;
            if (continueOnCapturedContext)
            {
                var syncCtx = SynchronizationContext.CurrentNoFlow;
                if (syncCtx != null && syncCtx.GetType() != typeof (SynchronizationContext))
                {
                    tc = new SynchronizationContextAwaitTaskContinuation(syncCtx, continuationAction, flowExecutionContext, ref stackMark);
                }
                else
                {
                    var scheduler = TaskScheduler.InternalCurrent;
                    if (scheduler != null && scheduler != TaskScheduler.Default)
                    {
                        tc = new TaskSchedulerAwaitTaskContinuation(scheduler, continuationAction, flowExecutionContext, ref stackMark);
                    }
                }
            }

            if (tc == null && flowExecutionContext)
            {
                tc = new AwaitTaskContinuation(continuationAction, flowExecutionContext: true, stackMark: ref stackMark);
            }

            if (tc != null)
            {
                if (!AddTaskContinuation(tc, addBeforeOthers: false))
                    tc.Run(this, bCanInlineContinuationTask: false);
            }
            else
            {
                Contract.Assert(!flowExecutionContext, "We already determined we're not required to flow context.");
                if (!AddTaskContinuation(continuationAction, addBeforeOthers: false))
                    AwaitTaskContinuation.UnsafeScheduleAction(continuationAction, this);
            }
        }

        public static YieldAwaitable Yield()
        {
            return new YieldAwaitable();
        }

        public void Wait()
        {
            bool waitResult = Wait(Timeout.Infinite, default (CancellationToken));
            Contract.Assert(waitResult, "expected wait to succeed");
        }

        public bool Wait(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return Wait((int)totalMilliseconds, default (CancellationToken));
        }

        public void Wait(CancellationToken cancellationToken)
        {
            Wait(Timeout.Infinite, cancellationToken);
        }

        public bool Wait(int millisecondsTimeout)
        {
            return Wait(millisecondsTimeout, default (CancellationToken));
        }

        public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }

            Contract.EndContractBlock();
            if (!IsWaitNotificationEnabledOrNotRanToCompletion)
                return true;
            if (!InternalWait(millisecondsTimeout, cancellationToken))
                return false;
            if (IsWaitNotificationEnabledOrNotRanToCompletion)
            {
                NotifyDebuggerOfWaitCompletionIfNecessary();
                if (IsCanceled)
                    cancellationToken.ThrowIfCancellationRequested();
                ThrowIfExceptional(true);
            }

            Contract.Assert((m_stateFlags & TASK_STATE_FAULTED) == 0, "Task.Wait() completing when in Faulted state.");
            return true;
        }

        private bool WrappedTryRunInline()
        {
            if (m_taskScheduler == null)
                return false;
            try
            {
                return m_taskScheduler.TryRunInline(this, true);
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    TaskSchedulerException tse = new TaskSchedulerException(e);
                    throw tse;
                }
                else
                {
                    throw;
                }
            }
        }

        internal bool InternalWait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            var etwLog = TplEtwProvider.Log;
            bool etwIsEnabled = etwLog.IsEnabled();
            if (etwIsEnabled)
            {
                Task currentTask = Task.InternalCurrent;
                etwLog.TaskWaitBegin((currentTask != null ? currentTask.m_taskScheduler.Id : TaskScheduler.Default.Id), (currentTask != null ? currentTask.Id : 0), this.Id, TplEtwProvider.TaskWaitBehavior.Synchronous, 0, System.Threading.Thread.GetDomainID());
            }

            bool returnValue = IsCompleted;
            if (!returnValue)
            {
                Debugger.NotifyOfCrossThreadDependency();
                if (millisecondsTimeout == Timeout.Infinite && !cancellationToken.CanBeCanceled && WrappedTryRunInline() && IsCompleted)
                {
                    returnValue = true;
                }
                else
                {
                    returnValue = SpinThenBlockingWait(millisecondsTimeout, cancellationToken);
                }
            }

            Contract.Assert(IsCompleted || millisecondsTimeout != Timeout.Infinite);
            if (etwIsEnabled)
            {
                Task currentTask = Task.InternalCurrent;
                if (currentTask != null)
                {
                    etwLog.TaskWaitEnd(currentTask.m_taskScheduler.Id, currentTask.Id, this.Id);
                }
                else
                {
                    etwLog.TaskWaitEnd(TaskScheduler.Default.Id, 0, this.Id);
                }

                etwLog.TaskWaitContinuationComplete(this.Id);
            }

            return returnValue;
        }

        private sealed class SetOnInvokeMres : ManualResetEventSlim, ITaskCompletionAction
        {
            internal SetOnInvokeMres(): base (false, 0)
            {
            }

            public void Invoke(Task completingTask)
            {
                Set();
            }

            public bool InvokeMayRunArbitraryCode
            {
                get
                {
                    return false;
                }
            }
        }

        private bool SpinThenBlockingWait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            bool infiniteWait = millisecondsTimeout == Timeout.Infinite;
            uint startTimeTicks = infiniteWait ? 0 : (uint)Environment.TickCount;
            bool returnValue = SpinWait(millisecondsTimeout);
            if (!returnValue)
            {
                var mres = new SetOnInvokeMres();
                try
                {
                    AddCompletionAction(mres, addBeforeOthers: true);
                    if (infiniteWait)
                    {
                        returnValue = mres.Wait(Timeout.Infinite, cancellationToken);
                    }
                    else
                    {
                        uint elapsedTimeTicks = ((uint)Environment.TickCount) - startTimeTicks;
                        if (elapsedTimeTicks < millisecondsTimeout)
                        {
                            returnValue = mres.Wait((int)(millisecondsTimeout - elapsedTimeTicks), cancellationToken);
                        }
                    }
                }
                finally
                {
                    if (!IsCompleted)
                        RemoveContinuation(mres);
                }
            }

            return returnValue;
        }

        private bool SpinWait(int millisecondsTimeout)
        {
            if (IsCompleted)
                return true;
            if (millisecondsTimeout == 0)
            {
                return false;
            }

            int spinCount = PlatformHelper.IsSingleProcessor ? 1 : System.Threading.SpinWait.YIELD_THRESHOLD;
            for (int i = 0; i < spinCount; i++)
            {
                if (IsCompleted)
                {
                    return true;
                }

                if (i == spinCount / 2)
                {
                    Thread.Yield();
                }
                else
                {
                    Thread.SpinWait(PlatformHelper.ProcessorCount * (4 << i));
                }
            }

            return IsCompleted;
        }

        internal bool InternalCancel(bool bCancelNonExecutingOnly)
        {
            Contract.Requires((Options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) == 0, "Task.InternalCancel() did not expect promise-style task");
            bool bPopSucceeded = false;
            bool mustCleanup = false;
            TaskSchedulerException tse = null;
            if ((m_stateFlags & TASK_STATE_STARTED) != 0)
            {
                TaskScheduler ts = m_taskScheduler;
                try
                {
                    bPopSucceeded = (ts != null) && ts.TryDequeue(this);
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        tse = new TaskSchedulerException(e);
                    }
                }

                bool bRequiresAtomicStartTransition = (ts != null && ts.RequiresAtomicStartTransition) || ((Options & (TaskCreationOptions)InternalTaskOptions.SelfReplicating) != 0);
                if (!bPopSucceeded && bCancelNonExecutingOnly && bRequiresAtomicStartTransition)
                {
                    mustCleanup = AtomicStateUpdate(TASK_STATE_CANCELED, TASK_STATE_DELEGATE_INVOKED | TASK_STATE_CANCELED);
                }
            }

            if (!bCancelNonExecutingOnly || bPopSucceeded || mustCleanup)
            {
                RecordInternalCancellationRequest();
                if (bPopSucceeded)
                {
                    Contract.Assert(!mustCleanup, "Possibly an invalid state transition call was made in InternalCancel()");
                    mustCleanup = AtomicStateUpdate(TASK_STATE_CANCELED, TASK_STATE_CANCELED | TASK_STATE_DELEGATE_INVOKED);
                }
                else if (!mustCleanup && (m_stateFlags & TASK_STATE_STARTED) == 0)
                {
                    mustCleanup = AtomicStateUpdate(TASK_STATE_CANCELED, TASK_STATE_CANCELED | TASK_STATE_STARTED | TASK_STATE_RAN_TO_COMPLETION | TASK_STATE_FAULTED | TASK_STATE_DELEGATE_INVOKED);
                }

                if (mustCleanup)
                {
                    CancellationCleanupLogic();
                }
            }

            if (tse != null)
                throw tse;
            else
                return (mustCleanup);
        }

        internal void RecordInternalCancellationRequest()
        {
            var props = EnsureContingentPropertiesInitialized(needsProtection: true);
            props.m_internalCancellationRequested = CANCELLATION_REQUESTED;
        }

        internal void RecordInternalCancellationRequest(CancellationToken tokenToRecord)
        {
            RecordInternalCancellationRequest();
            Contract.Assert((Options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0, "Task.RecordInternalCancellationRequest(CancellationToken) only valid for promise-style task");
            Contract.Assert(m_contingentProperties.m_cancellationToken == default (CancellationToken));
            if (tokenToRecord != default (CancellationToken))
            {
                m_contingentProperties.m_cancellationToken = tokenToRecord;
            }
        }

        internal void RecordInternalCancellationRequest(CancellationToken tokenToRecord, object cancellationException)
        {
            RecordInternalCancellationRequest(tokenToRecord);
            if (cancellationException != null)
            {
                var oce = cancellationException as OperationCanceledException;
                if (oce == null)
                {
                    var edi = cancellationException as ExceptionDispatchInfo;
                    Contract.Assert(edi != null, "Expected either an OCE or an EDI");
                    oce = edi.SourceException as OperationCanceledException;
                    Contract.Assert(oce != null, "Expected EDI to contain an OCE");
                }

                Contract.Assert(oce.CancellationToken == tokenToRecord, "Expected OCE's token to match the provided token.");
                AddException(cancellationException, representsCancellation: true);
            }
        }

        internal void CancellationCleanupLogic()
        {
            Contract.Assert((m_stateFlags & (TASK_STATE_CANCELED | TASK_STATE_COMPLETION_RESERVED)) != 0, "Task.CancellationCleanupLogic(): Task not canceled or reserved.");
            Interlocked.Exchange(ref m_stateFlags, m_stateFlags | TASK_STATE_CANCELED);
            var cp = m_contingentProperties;
            if (cp != null)
            {
                cp.SetCompleted();
                cp.DeregisterCancellationCallback();
            }

            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Canceled);
            if (Task.s_asyncDebuggingEnabled)
            {
                RemoveFromActiveTasks(this.Id);
            }

            FinishStageThree();
        }

        private void SetCancellationAcknowledged()
        {
            Contract.Assert(this == Task.InternalCurrent, "SetCancellationAcknowledged() should only be called while this is still the current task");
            Contract.Assert(IsCancellationRequested, "SetCancellationAcknowledged() should not be called if the task's CT wasn't signaled");
            m_stateFlags |= TASK_STATE_CANCELLATIONACKNOWLEDGED;
        }

        internal void FinishContinuations()
        {
            object continuationObject = Interlocked.Exchange(ref m_continuationObject, s_taskCompletionSentinel);
            TplEtwProvider etw = TplEtwProvider.Log;
            bool tplEtwProviderLoggingEnabled = etw.IsEnabled();
            if (tplEtwProviderLoggingEnabled)
            {
                etw.RunningContinuation(Id, continuationObject);
            }

            if (continuationObject != null)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceSynchronousWorkStart(CausalityTraceLevel.Required, this.Id, CausalitySynchronousWork.CompletionNotification);
                bool bCanInlineContinuations = !(((m_stateFlags & TASK_STATE_THREAD_WAS_ABORTED) != 0) || (Thread.CurrentThread.ThreadState == ThreadState.AbortRequested) || ((m_stateFlags & (int)TaskCreationOptions.RunContinuationsAsynchronously) != 0));
                Action singleAction = continuationObject as Action;
                if (singleAction != null)
                {
                    AwaitTaskContinuation.RunOrScheduleAction(singleAction, bCanInlineContinuations, ref t_currentTask);
                    LogFinishCompletionNotification();
                    return;
                }

                ITaskCompletionAction singleTaskCompletionAction = continuationObject as ITaskCompletionAction;
                if (singleTaskCompletionAction != null)
                {
                    if (bCanInlineContinuations || !singleTaskCompletionAction.InvokeMayRunArbitraryCode)
                    {
                        singleTaskCompletionAction.Invoke(this);
                    }
                    else
                    {
                        ThreadPool.UnsafeQueueCustomWorkItem(new CompletionActionInvoker(singleTaskCompletionAction, this), forceGlobal: false);
                    }

                    LogFinishCompletionNotification();
                    return;
                }

                TaskContinuation singleTaskContinuation = continuationObject as TaskContinuation;
                if (singleTaskContinuation != null)
                {
                    singleTaskContinuation.Run(this, bCanInlineContinuations);
                    LogFinishCompletionNotification();
                    return;
                }

                List<object> continuations = continuationObject as List<object>;
                if (continuations == null)
                {
                    LogFinishCompletionNotification();
                    return;
                }

                lock (continuations)
                {
                }

                int continuationCount = continuations.Count;
                for (int i = 0; i < continuationCount; i++)
                {
                    var tc = continuations[i] as StandardTaskContinuation;
                    if (tc != null && (tc.m_options & TaskContinuationOptions.ExecuteSynchronously) == 0)
                    {
                        if (tplEtwProviderLoggingEnabled)
                        {
                            etw.RunningContinuationList(Id, i, tc);
                        }

                        continuations[i] = null;
                        tc.Run(this, bCanInlineContinuations);
                    }
                }

                for (int i = 0; i < continuationCount; i++)
                {
                    object currentContinuation = continuations[i];
                    if (currentContinuation == null)
                        continue;
                    continuations[i] = null;
                    if (tplEtwProviderLoggingEnabled)
                    {
                        etw.RunningContinuationList(Id, i, currentContinuation);
                    }

                    Action ad = currentContinuation as Action;
                    if (ad != null)
                    {
                        AwaitTaskContinuation.RunOrScheduleAction(ad, bCanInlineContinuations, ref t_currentTask);
                    }
                    else
                    {
                        TaskContinuation tc = currentContinuation as TaskContinuation;
                        if (tc != null)
                        {
                            tc.Run(this, bCanInlineContinuations);
                        }
                        else
                        {
                            Contract.Assert(currentContinuation is ITaskCompletionAction, "Expected continuation element to be Action, TaskContinuation, or ITaskContinuationAction");
                            var action = (ITaskCompletionAction)currentContinuation;
                            if (bCanInlineContinuations || !action.InvokeMayRunArbitraryCode)
                            {
                                action.Invoke(this);
                            }
                            else
                            {
                                ThreadPool.UnsafeQueueCustomWorkItem(new CompletionActionInvoker(action, this), forceGlobal: false);
                            }
                        }
                    }
                }

                LogFinishCompletionNotification();
            }
        }

        private void LogFinishCompletionNotification()
        {
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceSynchronousWorkCompletion(CausalityTraceLevel.Required, CausalitySynchronousWork.CompletionNotification);
        }

        public Task ContinueWith(Action<Task> continuationAction)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, TaskScheduler.Current, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task> continuationAction, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, scheduler, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, TaskScheduler.Current, default (CancellationToken), continuationOptions, ref stackMark);
        }

        public Task ContinueWith(Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
        }

        private Task ContinueWith(Action<Task> continuationAction, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
        {
            if (continuationAction == null)
            {
                throw new ArgumentNullException("continuationAction");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            Contract.EndContractBlock();
            TaskCreationOptions creationOptions;
            InternalTaskOptions internalOptions;
            CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
            Task continuationTask = new ContinuationTaskFromTask(this, continuationAction, null, creationOptions, internalOptions, ref stackMark);
            ContinueWithCore(continuationTask, scheduler, cancellationToken, continuationOptions);
            return continuationTask;
        }

        public Task ContinueWith(Action<Task, Object> continuationAction, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, TaskScheduler.Current, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task, Object> continuationAction, Object state, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task, Object> continuationAction, Object state, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, scheduler, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task, Object> continuationAction, Object state, TaskContinuationOptions continuationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, TaskScheduler.Current, default (CancellationToken), continuationOptions, ref stackMark);
        }

        public Task ContinueWith(Action<Task, Object> continuationAction, Object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, scheduler, cancellationToken, continuationOptions, ref stackMark);
        }

        private Task ContinueWith(Action<Task, Object> continuationAction, Object state, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
        {
            if (continuationAction == null)
            {
                throw new ArgumentNullException("continuationAction");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            Contract.EndContractBlock();
            TaskCreationOptions creationOptions;
            InternalTaskOptions internalOptions;
            CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
            Task continuationTask = new ContinuationTaskFromTask(this, continuationAction, state, creationOptions, internalOptions, ref stackMark);
            ContinueWithCore(continuationTask, scheduler, cancellationToken, continuationOptions);
            return continuationTask;
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, TaskScheduler.Current, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, scheduler, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, TaskScheduler.Current, default (CancellationToken), continuationOptions, ref stackMark);
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
        }

        private Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuationFunction, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
        {
            if (continuationFunction == null)
            {
                throw new ArgumentNullException("continuationFunction");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            Contract.EndContractBlock();
            TaskCreationOptions creationOptions;
            InternalTaskOptions internalOptions;
            CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
            Task<TResult> continuationTask = new ContinuationResultTaskFromTask<TResult>(this, continuationFunction, null, creationOptions, internalOptions, ref stackMark);
            ContinueWithCore(continuationTask, scheduler, cancellationToken, continuationOptions);
            return continuationTask;
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, Object, TResult> continuationFunction, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, state, TaskScheduler.Current, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, Object, TResult> continuationFunction, Object state, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, state, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, Object, TResult> continuationFunction, Object state, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, state, scheduler, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, Object, TResult> continuationFunction, Object state, TaskContinuationOptions continuationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, state, TaskScheduler.Current, default (CancellationToken), continuationOptions, ref stackMark);
        }

        public Task<TResult> ContinueWith<TResult>(Func<Task, Object, TResult> continuationFunction, Object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TResult>(continuationFunction, state, scheduler, cancellationToken, continuationOptions, ref stackMark);
        }

        private Task<TResult> ContinueWith<TResult>(Func<Task, Object, TResult> continuationFunction, Object state, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
        {
            if (continuationFunction == null)
            {
                throw new ArgumentNullException("continuationFunction");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            Contract.EndContractBlock();
            TaskCreationOptions creationOptions;
            InternalTaskOptions internalOptions;
            CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
            Task<TResult> continuationTask = new ContinuationResultTaskFromTask<TResult>(this, continuationFunction, state, creationOptions, internalOptions, ref stackMark);
            ContinueWithCore(continuationTask, scheduler, cancellationToken, continuationOptions);
            return continuationTask;
        }

        internal static void CreationOptionsFromContinuationOptions(TaskContinuationOptions continuationOptions, out TaskCreationOptions creationOptions, out InternalTaskOptions internalOptions)
        {
            TaskContinuationOptions NotOnAnything = TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnRanToCompletion;
            TaskContinuationOptions creationOptionsMask = TaskContinuationOptions.PreferFairness | TaskContinuationOptions.LongRunning | TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.HideScheduler | TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.RunContinuationsAsynchronously;
            TaskContinuationOptions illegalMask = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.LongRunning;
            if ((continuationOptions & illegalMask) == illegalMask)
            {
                throw new ArgumentOutOfRangeException("continuationOptions", Environment.GetResourceString("Task_ContinueWith_ESandLR"));
            }

            if ((continuationOptions & ~(creationOptionsMask | NotOnAnything | TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously)) != 0)
            {
                throw new ArgumentOutOfRangeException("continuationOptions");
            }

            if ((continuationOptions & NotOnAnything) == NotOnAnything)
            {
                throw new ArgumentOutOfRangeException("continuationOptions", Environment.GetResourceString("Task_ContinueWith_NotOnAnything"));
            }

            creationOptions = (TaskCreationOptions)(continuationOptions & creationOptionsMask);
            internalOptions = InternalTaskOptions.ContinuationTask;
            if ((continuationOptions & TaskContinuationOptions.LazyCancellation) != 0)
                internalOptions |= InternalTaskOptions.LazyCancellation;
        }

        internal void ContinueWithCore(Task continuationTask, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions options)
        {
            Contract.Requires(continuationTask != null, "Task.ContinueWithCore(): null continuationTask");
            Contract.Requires(scheduler != null, "Task.ContinueWithCore(): null scheduler");
            Contract.Requires(!continuationTask.IsCompleted, "Did not expect continuationTask to be completed");
            TaskContinuation continuation = new StandardTaskContinuation(continuationTask, options, scheduler);
            if (cancellationToken.CanBeCanceled)
            {
                if (IsCompleted || cancellationToken.IsCancellationRequested)
                {
                    continuationTask.AssignCancellationToken(cancellationToken, null, null);
                }
                else
                {
                    continuationTask.AssignCancellationToken(cancellationToken, this, continuation);
                }
            }

            if (!continuationTask.IsCompleted)
            {
                if ((this.Options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0 && !(this is ITaskCompletionAction))
                {
                    var etwLog = TplEtwProvider.Log;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.AwaitTaskContinuationScheduled(TaskScheduler.Current.Id, Task.CurrentId ?? 0, continuationTask.Id);
                    }
                }

                bool continuationQueued = AddTaskContinuation(continuation, addBeforeOthers: false);
                if (!continuationQueued)
                    continuation.Run(this, bCanInlineContinuationTask: true);
            }
        }

        internal void AddCompletionAction(ITaskCompletionAction action)
        {
            AddCompletionAction(action, addBeforeOthers: false);
        }

        private void AddCompletionAction(ITaskCompletionAction action, bool addBeforeOthers)
        {
            if (!AddTaskContinuation(action, addBeforeOthers))
                action.Invoke(this);
        }

        private bool AddTaskContinuationComplex(object tc, bool addBeforeOthers)
        {
            Contract.Requires(tc != null, "Expected non-null tc object in AddTaskContinuationComplex");
            object oldValue = m_continuationObject;
            if ((oldValue != s_taskCompletionSentinel) && (!(oldValue is List<object>)))
            {
                List<object> newList = new List<object>();
                newList.Add(oldValue);
                Interlocked.CompareExchange(ref m_continuationObject, newList, oldValue);
            }

            List<object> list = m_continuationObject as List<object>;
            Contract.Assert((list != null) || (m_continuationObject == s_taskCompletionSentinel), "Expected m_continuationObject to be list or sentinel");
            if (list != null)
            {
                lock (list)
                {
                    if (m_continuationObject != s_taskCompletionSentinel)
                    {
                        if (list.Count == list.Capacity)
                        {
                            list.RemoveAll(s_IsTaskContinuationNullPredicate);
                        }

                        if (addBeforeOthers)
                            list.Insert(0, tc);
                        else
                            list.Add(tc);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool AddTaskContinuation(object tc, bool addBeforeOthers)
        {
            Contract.Requires(tc != null);
            if (IsCompleted)
                return false;
            if ((m_continuationObject != null) || (Interlocked.CompareExchange(ref m_continuationObject, tc, null) != null))
            {
                return AddTaskContinuationComplex(tc, addBeforeOthers);
            }
            else
                return true;
        }

        internal void RemoveContinuation(object continuationObject)
        {
            object continuationsLocalRef = m_continuationObject;
            if (continuationsLocalRef == s_taskCompletionSentinel)
                return;
            List<object> continuationsLocalListRef = continuationsLocalRef as List<object>;
            if (continuationsLocalListRef == null)
            {
                if (Interlocked.CompareExchange(ref m_continuationObject, new List<object>(), continuationObject) != continuationObject)
                {
                    continuationsLocalListRef = m_continuationObject as List<object>;
                }
                else
                {
                    return;
                }
            }

            if (continuationsLocalListRef != null)
            {
                lock (continuationsLocalListRef)
                {
                    if (m_continuationObject == s_taskCompletionSentinel)
                        return;
                    int index = continuationsLocalListRef.IndexOf(continuationObject);
                    if (index != -1)
                    {
                        continuationsLocalListRef[index] = null;
                    }
                }
            }
        }

        private readonly static Predicate<object> s_IsTaskContinuationNullPredicate = new Predicate<object>((tc) =>
        {
            return (tc == null);
        }

        );
        public static void WaitAll(params Task[] tasks)
        {
            bool waitResult = WaitAll(tasks, Timeout.Infinite);
            Contract.Assert(waitResult, "expected wait to succeed");
        }

        public static bool WaitAll(Task[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return WaitAll(tasks, (int)totalMilliseconds);
        }

        public static bool WaitAll(Task[] tasks, int millisecondsTimeout)
        {
            return WaitAll(tasks, millisecondsTimeout, default (CancellationToken));
        }

        public static void WaitAll(Task[] tasks, CancellationToken cancellationToken)
        {
            WaitAll(tasks, Timeout.Infinite, cancellationToken);
        }

        public static bool WaitAll(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException("tasks");
            }

            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }

            Contract.EndContractBlock();
            cancellationToken.ThrowIfCancellationRequested();
            List<Exception> exceptions = null;
            List<Task> waitedOnTaskList = null;
            List<Task> notificationTasks = null;
            bool exceptionSeen = false, cancellationSeen = false;
            bool returnValue = true;
            for (int i = tasks.Length - 1; i >= 0; i--)
            {
                Task task = tasks[i];
                if (task == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Task_WaitMulti_NullTask"), "tasks");
                }

                bool taskIsCompleted = task.IsCompleted;
                if (!taskIsCompleted)
                {
                    if (millisecondsTimeout != Timeout.Infinite || cancellationToken.CanBeCanceled)
                    {
                        AddToList(task, ref waitedOnTaskList, initSize: tasks.Length);
                    }
                    else
                    {
                        taskIsCompleted = task.WrappedTryRunInline() && task.IsCompleted;
                        if (!taskIsCompleted)
                            AddToList(task, ref waitedOnTaskList, initSize: tasks.Length);
                    }
                }

                if (taskIsCompleted)
                {
                    if (task.IsFaulted)
                        exceptionSeen = true;
                    else if (task.IsCanceled)
                        cancellationSeen = true;
                    if (task.IsWaitNotificationEnabled)
                        AddToList(task, ref notificationTasks, initSize: 1);
                }
            }

            if (waitedOnTaskList != null)
            {
                returnValue = WaitAllBlockingCore(waitedOnTaskList, millisecondsTimeout, cancellationToken);
                if (returnValue)
                {
                    foreach (var task in waitedOnTaskList)
                    {
                        if (task.IsFaulted)
                            exceptionSeen = true;
                        else if (task.IsCanceled)
                            cancellationSeen = true;
                        if (task.IsWaitNotificationEnabled)
                            AddToList(task, ref notificationTasks, initSize: 1);
                    }
                }

                GC.KeepAlive(tasks);
            }

            if (returnValue && notificationTasks != null)
            {
                foreach (var task in notificationTasks)
                {
                    if (task.NotifyDebuggerOfWaitCompletionIfNecessary())
                        break;
                }
            }

            if (returnValue && (exceptionSeen || cancellationSeen))
            {
                if (!exceptionSeen)
                    cancellationToken.ThrowIfCancellationRequested();
                foreach (var task in tasks)
                    AddExceptionsForCompletedTask(ref exceptions, task);
                Contract.Assert(exceptions != null, "Should have seen at least one exception");
                throw new AggregateException(exceptions);
            }

            return returnValue;
        }

        private static void AddToList<T>(T item, ref List<T> list, int initSize)
        {
            if (list == null)
                list = new List<T>(initSize);
            list.Add(item);
        }

        private static bool WaitAllBlockingCore(List<Task> tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            Contract.Assert(tasks != null, "Expected a non-null list of tasks");
            Contract.Assert(tasks.Count > 0, "Expected at least one task");
            bool waitCompleted = false;
            var mres = new SetOnCountdownMres(tasks.Count);
            try
            {
                foreach (var task in tasks)
                {
                    task.AddCompletionAction(mres, addBeforeOthers: true);
                }

                waitCompleted = mres.Wait(millisecondsTimeout, cancellationToken);
            }
            finally
            {
                if (!waitCompleted)
                {
                    foreach (var task in tasks)
                    {
                        if (!task.IsCompleted)
                            task.RemoveContinuation(mres);
                    }
                }
            }

            return waitCompleted;
        }

        private sealed class SetOnCountdownMres : ManualResetEventSlim, ITaskCompletionAction
        {
            private int _count;
            internal SetOnCountdownMres(int count)
            {
                Contract.Assert(count > 0, "Expected count > 0");
                _count = count;
            }

            public void Invoke(Task completingTask)
            {
                if (Interlocked.Decrement(ref _count) == 0)
                    Set();
                Contract.Assert(_count >= 0, "Count should never go below 0");
            }

            public bool InvokeMayRunArbitraryCode
            {
                get
                {
                    return false;
                }
            }
        }

        internal static void FastWaitAll(Task[] tasks)
        {
            Contract.Requires(tasks != null);
            List<Exception> exceptions = null;
            for (int i = tasks.Length - 1; i >= 0; i--)
            {
                if (!tasks[i].IsCompleted)
                {
                    tasks[i].WrappedTryRunInline();
                }
            }

            for (int i = tasks.Length - 1; i >= 0; i--)
            {
                var task = tasks[i];
                task.SpinThenBlockingWait(Timeout.Infinite, default (CancellationToken));
                AddExceptionsForCompletedTask(ref exceptions, task);
            }

            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

        internal static void AddExceptionsForCompletedTask(ref List<Exception> exceptions, Task t)
        {
            AggregateException ex = t.GetExceptions(true);
            if (ex != null)
            {
                t.UpdateExceptionObservedStatus();
                if (exceptions == null)
                {
                    exceptions = new List<Exception>(ex.InnerExceptions.Count);
                }

                exceptions.AddRange(ex.InnerExceptions);
            }
        }

        public static int WaitAny(params Task[] tasks)
        {
            int waitResult = WaitAny(tasks, Timeout.Infinite);
            Contract.Assert(tasks.Length == 0 || waitResult != -1, "expected wait to succeed");
            return waitResult;
        }

        public static int WaitAny(Task[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            return WaitAny(tasks, (int)totalMilliseconds);
        }

        public static int WaitAny(Task[] tasks, CancellationToken cancellationToken)
        {
            return WaitAny(tasks, Timeout.Infinite, cancellationToken);
        }

        public static int WaitAny(Task[] tasks, int millisecondsTimeout)
        {
            return WaitAny(tasks, millisecondsTimeout, default (CancellationToken));
        }

        public static int WaitAny(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (tasks == null)
            {
                throw new ArgumentNullException("tasks");
            }

            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout");
            }

            Contract.EndContractBlock();
            cancellationToken.ThrowIfCancellationRequested();
            int signaledTaskIndex = -1;
            for (int taskIndex = 0; taskIndex < tasks.Length; taskIndex++)
            {
                Task task = tasks[taskIndex];
                if (task == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Task_WaitMulti_NullTask"), "tasks");
                }

                if (signaledTaskIndex == -1 && task.IsCompleted)
                {
                    signaledTaskIndex = taskIndex;
                }
            }

            if (signaledTaskIndex == -1 && tasks.Length != 0)
            {
                Task<Task> firstCompleted = TaskFactory.CommonCWAnyLogic(tasks);
                bool waitCompleted = firstCompleted.Wait(millisecondsTimeout, cancellationToken);
                if (waitCompleted)
                {
                    Contract.Assert(firstCompleted.Status == TaskStatus.RanToCompletion);
                    signaledTaskIndex = Array.IndexOf(tasks, firstCompleted.Result);
                    Contract.Assert(signaledTaskIndex >= 0);
                }
            }

            GC.KeepAlive(tasks);
            return signaledTaskIndex;
        }

        public static Task<TResult> FromResult<TResult>(TResult result)
        {
            return new Task<TResult>(result);
        }

        public static Task FromException(Exception exception)
        {
            return FromException<VoidTaskResult>(exception);
        }

        public static Task<TResult> FromException<TResult>(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            Contract.EndContractBlock();
            var task = new Task<TResult>();
            bool succeeded = task.TrySetException(exception);
            Contract.Assert(succeeded, "This should always succeed on a new task.");
            return task;
        }

        internal static Task FromCancellation(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                throw new ArgumentOutOfRangeException("cancellationToken");
            Contract.EndContractBlock();
            return new Task(true, TaskCreationOptions.None, cancellationToken);
        }

        public static Task FromCanceled(CancellationToken cancellationToken)
        {
            return FromCancellation(cancellationToken);
        }

        internal static Task<TResult> FromCancellation<TResult>(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                throw new ArgumentOutOfRangeException("cancellationToken");
            Contract.EndContractBlock();
            return new Task<TResult>(true, default (TResult), TaskCreationOptions.None, cancellationToken);
        }

        public static Task<TResult> FromCanceled<TResult>(CancellationToken cancellationToken)
        {
            return FromCancellation<TResult>(cancellationToken);
        }

        internal static Task<TResult> FromCancellation<TResult>(OperationCanceledException exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            Contract.EndContractBlock();
            var task = new Task<TResult>();
            bool succeeded = task.TrySetCanceled(exception.CancellationToken, exception);
            Contract.Assert(succeeded, "This should always succeed on a new task.");
            return task;
        }

        public static Task Run(Action action)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task.InternalStartNew(null, action, null, default (CancellationToken), TaskScheduler.Default, TaskCreationOptions.DenyChildAttach, InternalTaskOptions.None, ref stackMark);
        }

        public static Task Run(Action action, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task.InternalStartNew(null, action, null, cancellationToken, TaskScheduler.Default, TaskCreationOptions.DenyChildAttach, InternalTaskOptions.None, ref stackMark);
        }

        public static Task<TResult> Run<TResult>(Func<TResult> function)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task<TResult>.StartNew(null, function, default (CancellationToken), TaskCreationOptions.DenyChildAttach, InternalTaskOptions.None, TaskScheduler.Default, ref stackMark);
        }

        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task<TResult>.StartNew(null, function, cancellationToken, TaskCreationOptions.DenyChildAttach, InternalTaskOptions.None, TaskScheduler.Default, ref stackMark);
        }

        public static Task Run(Func<Task> function)
        {
            return Run(function, default (CancellationToken));
        }

        public static Task Run(Func<Task> function, CancellationToken cancellationToken)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            Contract.EndContractBlock();
            if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
            {
                cancellationToken.ThrowIfSourceDisposed();
            }

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
            Task<Task> task1 = Task<Task>.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            UnwrapPromise<VoidTaskResult> promise = new UnwrapPromise<VoidTaskResult>(task1, lookForOce: true);
            return promise;
        }

        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function)
        {
            return Run(function, default (CancellationToken));
        }

        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            if (function == null)
                throw new ArgumentNullException("function");
            Contract.EndContractBlock();
            if (AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
            {
                cancellationToken.ThrowIfSourceDisposed();
            }

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<TResult>(cancellationToken);
            Task<Task<TResult>> task1 = Task<Task<TResult>>.Factory.StartNew(function, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            UnwrapPromise<TResult> promise = new UnwrapPromise<TResult>(task1, lookForOce: true);
            return promise;
        }

        public static Task Delay(TimeSpan delay)
        {
            return Delay(delay, default (CancellationToken));
        }

        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            long totalMilliseconds = (long)delay.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("delay", Environment.GetResourceString("Task_Delay_InvalidDelay"));
            }

            return Delay((int)totalMilliseconds, cancellationToken);
        }

        public static Task Delay(int millisecondsDelay)
        {
            return Delay(millisecondsDelay, default (CancellationToken));
        }

        public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken)
        {
            if (millisecondsDelay < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsDelay", Environment.GetResourceString("Task_Delay_InvalidMillisecondsDelay"));
            }

            Contract.EndContractBlock();
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCancellation(cancellationToken);
            }
            else if (millisecondsDelay == 0)
            {
                return Task.CompletedTask;
            }

            var promise = new DelayPromise(cancellationToken);
            if (cancellationToken.CanBeCanceled)
            {
                promise.Registration = cancellationToken.InternalRegisterWithoutEC(state => ((DelayPromise)state).Complete(), promise);
            }

            if (millisecondsDelay != Timeout.Infinite)
            {
                promise.Timer = new Timer(state => ((DelayPromise)state).Complete(), promise, millisecondsDelay, Timeout.Infinite);
                promise.Timer.KeepRootedWhileScheduled();
            }

            return promise;
        }

        private sealed class DelayPromise : Task<VoidTaskResult>
        {
            internal DelayPromise(CancellationToken token): base ()
            {
                this.Token = token;
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "Task.Delay", 0);
                if (Task.s_asyncDebuggingEnabled)
                {
                    AddToActiveTasks(this);
                }
            }

            internal readonly CancellationToken Token;
            internal CancellationTokenRegistration Registration;
            internal Timer Timer;
            internal void Complete()
            {
                bool setSucceeded;
                if (Token.IsCancellationRequested)
                {
                    setSucceeded = TrySetCanceled(Token);
                }
                else
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
                    if (Task.s_asyncDebuggingEnabled)
                    {
                        RemoveFromActiveTasks(this.Id);
                    }

                    setSucceeded = TrySetResult(default (VoidTaskResult));
                }

                if (setSucceeded)
                {
                    if (Timer != null)
                        Timer.Dispose();
                    Registration.Dispose();
                }
            }
        }

        public static Task WhenAll(IEnumerable<Task> tasks)
        {
            Task[] taskArray = tasks as Task[];
            if (taskArray != null)
            {
                return WhenAll(taskArray);
            }

            ICollection<Task> taskCollection = tasks as ICollection<Task>;
            if (taskCollection != null)
            {
                int index = 0;
                taskArray = new Task[taskCollection.Count];
                foreach (var task in tasks)
                {
                    if (task == null)
                        throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                    taskArray[index++] = task;
                }

                return InternalWhenAll(taskArray);
            }

            if (tasks == null)
                throw new ArgumentNullException("tasks");
            List<Task> taskList = new List<Task>();
            foreach (Task task in tasks)
            {
                if (task == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                taskList.Add(task);
            }

            return InternalWhenAll(taskList.ToArray());
        }

        public static Task WhenAll(params Task[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            Contract.EndContractBlock();
            int taskCount = tasks.Length;
            if (taskCount == 0)
                return InternalWhenAll(tasks);
            Task[] tasksCopy = new Task[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                Task task = tasks[i];
                if (task == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                tasksCopy[i] = task;
            }

            return InternalWhenAll(tasksCopy);
        }

        private static Task InternalWhenAll(Task[] tasks)
        {
            Contract.Requires(tasks != null, "Expected a non-null tasks array");
            return (tasks.Length == 0) ? Task.CompletedTask : new WhenAllPromise(tasks);
        }

        private sealed class WhenAllPromise : Task<VoidTaskResult>, ITaskCompletionAction
        {
            private readonly Task[] m_tasks;
            private int m_count;
            internal WhenAllPromise(Task[] tasks): base ()
            {
                Contract.Requires(tasks != null, "Expected a non-null task array");
                Contract.Requires(tasks.Length > 0, "Expected a non-zero length task array");
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "Task.WhenAll", 0);
                if (s_asyncDebuggingEnabled)
                {
                    AddToActiveTasks(this);
                }

                m_tasks = tasks;
                m_count = tasks.Length;
                foreach (var task in tasks)
                {
                    if (task.IsCompleted)
                        this.Invoke(task);
                    else
                        task.AddCompletionAction(this);
                }
            }

            public void Invoke(Task completedTask)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, this.Id, CausalityRelation.Join);
                if (Interlocked.Decrement(ref m_count) == 0)
                {
                    List<ExceptionDispatchInfo> observedExceptions = null;
                    Task canceledTask = null;
                    for (int i = 0; i < m_tasks.Length; i++)
                    {
                        var task = m_tasks[i];
                        Contract.Assert(task != null, "Constituent task in WhenAll should never be null");
                        if (task.IsFaulted)
                        {
                            if (observedExceptions == null)
                                observedExceptions = new List<ExceptionDispatchInfo>();
                            observedExceptions.AddRange(task.GetExceptionDispatchInfos());
                        }
                        else if (task.IsCanceled)
                        {
                            if (canceledTask == null)
                                canceledTask = task;
                        }

                        if (task.IsWaitNotificationEnabled)
                            this.SetNotificationForWaitCompletion(enabled: true);
                        else
                            m_tasks[i] = null;
                    }

                    if (observedExceptions != null)
                    {
                        Contract.Assert(observedExceptions.Count > 0, "Expected at least one exception");
                        TrySetException(observedExceptions);
                    }
                    else if (canceledTask != null)
                    {
                        TrySetCanceled(canceledTask.CancellationToken, canceledTask.GetCancellationExceptionDispatchInfo());
                    }
                    else
                    {
                        if (AsyncCausalityTracer.LoggingOn)
                            AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
                        if (Task.s_asyncDebuggingEnabled)
                        {
                            RemoveFromActiveTasks(this.Id);
                        }

                        TrySetResult(default (VoidTaskResult));
                    }
                }

                Contract.Assert(m_count >= 0, "Count should never go below 0");
            }

            public bool InvokeMayRunArbitraryCode
            {
                get
                {
                    return true;
                }
            }

            internal override bool ShouldNotifyDebuggerOfWaitCompletion
            {
                get
                {
                    return base.ShouldNotifyDebuggerOfWaitCompletion && Task.AnyTaskRequiresNotifyDebuggerOfWaitCompletion(m_tasks);
                }
            }
        }

        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            Task<TResult>[] taskArray = tasks as Task<TResult>[];
            if (taskArray != null)
            {
                return WhenAll<TResult>(taskArray);
            }

            ICollection<Task<TResult>> taskCollection = tasks as ICollection<Task<TResult>>;
            if (taskCollection != null)
            {
                int index = 0;
                taskArray = new Task<TResult>[taskCollection.Count];
                foreach (var task in tasks)
                {
                    if (task == null)
                        throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                    taskArray[index++] = task;
                }

                return InternalWhenAll<TResult>(taskArray);
            }

            if (tasks == null)
                throw new ArgumentNullException("tasks");
            List<Task<TResult>> taskList = new List<Task<TResult>>();
            foreach (Task<TResult> task in tasks)
            {
                if (task == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                taskList.Add(task);
            }

            return InternalWhenAll<TResult>(taskList.ToArray());
        }

        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            Contract.EndContractBlock();
            int taskCount = tasks.Length;
            if (taskCount == 0)
                return InternalWhenAll<TResult>(tasks);
            Task<TResult>[] tasksCopy = new Task<TResult>[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                Task<TResult> task = tasks[i];
                if (task == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                tasksCopy[i] = task;
            }

            return InternalWhenAll<TResult>(tasksCopy);
        }

        private static Task<TResult[]> InternalWhenAll<TResult>(Task<TResult>[] tasks)
        {
            Contract.Requires(tasks != null, "Expected a non-null tasks array");
            return (tasks.Length == 0) ? new Task<TResult[]>(false, new TResult[0], TaskCreationOptions.None, default (CancellationToken)) : new WhenAllPromise<TResult>(tasks);
        }

        private sealed class WhenAllPromise<T> : Task<T[]>, ITaskCompletionAction
        {
            private readonly Task<T>[] m_tasks;
            private int m_count;
            internal WhenAllPromise(Task<T>[] tasks): base ()
            {
                Contract.Requires(tasks != null, "Expected a non-null task array");
                Contract.Requires(tasks.Length > 0, "Expected a non-zero length task array");
                m_tasks = tasks;
                m_count = tasks.Length;
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "Task.WhenAll", 0);
                if (s_asyncDebuggingEnabled)
                {
                    AddToActiveTasks(this);
                }

                foreach (var task in tasks)
                {
                    if (task.IsCompleted)
                        this.Invoke(task);
                    else
                        task.AddCompletionAction(this);
                }
            }

            public void Invoke(Task ignored)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, this.Id, CausalityRelation.Join);
                if (Interlocked.Decrement(ref m_count) == 0)
                {
                    T[] results = new T[m_tasks.Length];
                    List<ExceptionDispatchInfo> observedExceptions = null;
                    Task canceledTask = null;
                    for (int i = 0; i < m_tasks.Length; i++)
                    {
                        Task<T> task = m_tasks[i];
                        Contract.Assert(task != null, "Constituent task in WhenAll should never be null");
                        if (task.IsFaulted)
                        {
                            if (observedExceptions == null)
                                observedExceptions = new List<ExceptionDispatchInfo>();
                            observedExceptions.AddRange(task.GetExceptionDispatchInfos());
                        }
                        else if (task.IsCanceled)
                        {
                            if (canceledTask == null)
                                canceledTask = task;
                        }
                        else
                        {
                            Contract.Assert(task.Status == TaskStatus.RanToCompletion);
                            results[i] = task.GetResultCore(waitCompletionNotification: false);
                        }

                        if (task.IsWaitNotificationEnabled)
                            this.SetNotificationForWaitCompletion(enabled: true);
                        else
                            m_tasks[i] = null;
                    }

                    if (observedExceptions != null)
                    {
                        Contract.Assert(observedExceptions.Count > 0, "Expected at least one exception");
                        TrySetException(observedExceptions);
                    }
                    else if (canceledTask != null)
                    {
                        TrySetCanceled(canceledTask.CancellationToken, canceledTask.GetCancellationExceptionDispatchInfo());
                    }
                    else
                    {
                        if (AsyncCausalityTracer.LoggingOn)
                            AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
                        if (Task.s_asyncDebuggingEnabled)
                        {
                            RemoveFromActiveTasks(this.Id);
                        }

                        TrySetResult(results);
                    }
                }

                Contract.Assert(m_count >= 0, "Count should never go below 0");
            }

            public bool InvokeMayRunArbitraryCode
            {
                get
                {
                    return true;
                }
            }

            internal override bool ShouldNotifyDebuggerOfWaitCompletion
            {
                get
                {
                    return base.ShouldNotifyDebuggerOfWaitCompletion && Task.AnyTaskRequiresNotifyDebuggerOfWaitCompletion(m_tasks);
                }
            }
        }

        public static Task<Task> WhenAny(params Task[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
            }

            Contract.EndContractBlock();
            int taskCount = tasks.Length;
            Task[] tasksCopy = new Task[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                Task task = tasks[i];
                if (task == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                tasksCopy[i] = task;
            }

            return TaskFactory.CommonCWAnyLogic(tasksCopy);
        }

        public static Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            Contract.EndContractBlock();
            List<Task> taskList = new List<Task>();
            foreach (Task task in tasks)
            {
                if (task == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                taskList.Add(task);
            }

            if (taskList.Count == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
            }

            return TaskFactory.CommonCWAnyLogic(taskList);
        }

        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
            Task<Task> intermediate = WhenAny((Task[])tasks);
            return intermediate.ContinueWith(Task<TResult>.TaskWhenAnyCast, default (CancellationToken), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            Task<Task> intermediate = WhenAny((IEnumerable<Task>)tasks);
            return intermediate.ContinueWith(Task<TResult>.TaskWhenAnyCast, default (CancellationToken), TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        internal static Task<TResult> CreateUnwrapPromise<TResult>(Task outerTask, bool lookForOce)
        {
            Contract.Requires(outerTask != null);
            return new UnwrapPromise<TResult>(outerTask, lookForOce);
        }

        internal virtual Delegate[] GetDelegateContinuationsForDebugger()
        {
            if (this.m_continuationObject != this)
                return GetDelegatesFromContinuationObject(this.m_continuationObject);
            else
                return null;
        }

        internal static Delegate[] GetDelegatesFromContinuationObject(object continuationObject)
        {
            if (continuationObject != null)
            {
                Action singleAction = continuationObject as Action;
                if (singleAction != null)
                {
                    return new Delegate[]{AsyncMethodBuilderCore.TryGetStateMachineForDebugger(singleAction)};
                }

                TaskContinuation taskContinuation = continuationObject as TaskContinuation;
                if (taskContinuation != null)
                {
                    return taskContinuation.GetDelegateContinuationsForDebugger();
                }

                Task continuationTask = continuationObject as Task;
                if (continuationTask != null)
                {
                    Contract.Assert(continuationTask.m_action == null);
                    Delegate[] delegates = continuationTask.GetDelegateContinuationsForDebugger();
                    if (delegates != null)
                        return delegates;
                }

                ITaskCompletionAction singleCompletionAction = continuationObject as ITaskCompletionAction;
                if (singleCompletionAction != null)
                {
                    return new Delegate[]{new Action<Task>(singleCompletionAction.Invoke)};
                }

                List<object> continuationList = continuationObject as List<object>;
                if (continuationList != null)
                {
                    List<Delegate> result = new List<Delegate>();
                    foreach (object obj in continuationList)
                    {
                        var innerDelegates = GetDelegatesFromContinuationObject(obj);
                        if (innerDelegates != null)
                        {
                            foreach (var del in innerDelegates)
                            {
                                if (del != null)
                                    result.Add(del);
                            }
                        }
                    }

                    return result.ToArray();
                }
            }

            return null;
        }

        private static Task GetActiveTaskFromId(int taskId)
        {
            Task task = null;
            s_currentActiveTasks.TryGetValue(taskId, out task);
            return task;
        }

        private static Task[] GetActiveTasks()
        {
            return new List<Task>(s_currentActiveTasks.Values).ToArray();
        }
    }

    internal sealed class CompletionActionInvoker : IThreadPoolWorkItem
    {
        private readonly ITaskCompletionAction m_action;
        private readonly Task m_completingTask;
        internal CompletionActionInvoker(ITaskCompletionAction action, Task completingTask)
        {
            m_action = action;
            m_completingTask = completingTask;
        }

        void IThreadPoolWorkItem.ExecuteWorkItem()
        {
            m_action.Invoke(m_completingTask);
        }

        void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
        {
        }
    }

    internal class SystemThreadingTasks_TaskDebugView
    {
        private Task m_task;
        public SystemThreadingTasks_TaskDebugView(Task task)
        {
            m_task = task;
        }

        public object AsyncState
        {
            get
            {
                return m_task.AsyncState;
            }
        }

        public TaskCreationOptions CreationOptions
        {
            get
            {
                return m_task.CreationOptions;
            }
        }

        public Exception Exception
        {
            get
            {
                return m_task.Exception;
            }
        }

        public int Id
        {
            get
            {
                return m_task.Id;
            }
        }

        public bool CancellationPending
        {
            get
            {
                return (m_task.Status == TaskStatus.WaitingToRun) && m_task.CancellationToken.IsCancellationRequested;
            }
        }

        public TaskStatus Status
        {
            get
            {
                return m_task.Status;
            }
        }
    }

    internal class ParallelForReplicatingTask : Task
    {
        private int m_replicationDownCount;
        internal ParallelForReplicatingTask(ParallelOptions parallelOptions, Action action, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions): base (action, null, Task.InternalCurrent, default (CancellationToken), creationOptions, internalOptions | InternalTaskOptions.SelfReplicating, null)
        {
            m_replicationDownCount = parallelOptions.EffectiveMaxConcurrencyLevel;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        internal override bool ShouldReplicate()
        {
            if (m_replicationDownCount == -1)
                return true;
            if (m_replicationDownCount > 0)
            {
                m_replicationDownCount--;
                return true;
            }

            return false;
        }

        internal override Task CreateReplicaTask(Action<object> taskReplicaDelegate, Object stateObject, Task parentTask, TaskScheduler taskScheduler, TaskCreationOptions creationOptionsForReplica, InternalTaskOptions internalOptionsForReplica)
        {
            return new ParallelForReplicaTask(taskReplicaDelegate, stateObject, parentTask, taskScheduler, creationOptionsForReplica, internalOptionsForReplica);
        }
    }

    internal class ParallelForReplicaTask : Task
    {
        internal object m_stateForNextReplica;
        internal object m_stateFromPreviousReplica;
        internal Task m_handedOverChildReplica;
        internal ParallelForReplicaTask(Action<object> taskReplicaDelegate, Object stateObject, Task parentTask, TaskScheduler taskScheduler, TaskCreationOptions creationOptionsForReplica, InternalTaskOptions internalOptionsForReplica): base (taskReplicaDelegate, stateObject, parentTask, default (CancellationToken), creationOptionsForReplica, internalOptionsForReplica, taskScheduler)
        {
        }

        internal override Object SavedStateForNextReplica
        {
            get
            {
                return m_stateForNextReplica;
            }

            set
            {
                m_stateForNextReplica = value;
            }
        }

        internal override Object SavedStateFromPreviousReplica
        {
            get
            {
                return m_stateFromPreviousReplica;
            }

            set
            {
                m_stateFromPreviousReplica = value;
            }
        }

        internal override Task HandedOverChildReplica
        {
            get
            {
                return m_handedOverChildReplica;
            }

            set
            {
                m_handedOverChildReplica = value;
            }
        }
    }

    [Flags]
    public enum TaskCreationOptions
    {
        None = 0x0,
        PreferFairness = 0x01,
        LongRunning = 0x02,
        AttachedToParent = 0x04,
        DenyChildAttach = 0x08,
        HideScheduler = 0x10,
        RunContinuationsAsynchronously = 0x40
    }

    [Flags]
    internal enum InternalTaskOptions
    {
        None,
        InternalOptionsMask = 0x0000FF00,
        ChildReplica = 0x0100,
        ContinuationTask = 0x0200,
        PromiseTask = 0x0400,
        SelfReplicating = 0x0800,
        LazyCancellation = 0x1000,
        QueuedByRuntime = 0x2000,
        DoNotDispose = 0x4000
    }

    [Flags]
    public enum TaskContinuationOptions
    {
        None = 0,
        PreferFairness = 0x01,
        LongRunning = 0x02,
        AttachedToParent = 0x04,
        DenyChildAttach = 0x08,
        HideScheduler = 0x10,
        LazyCancellation = 0x20,
        RunContinuationsAsynchronously = 0x40,
        NotOnRanToCompletion = 0x10000,
        NotOnFaulted = 0x20000,
        NotOnCanceled = 0x40000,
        OnlyOnRanToCompletion = NotOnFaulted | NotOnCanceled,
        OnlyOnFaulted = NotOnRanToCompletion | NotOnCanceled,
        OnlyOnCanceled = NotOnRanToCompletion | NotOnFaulted,
        ExecuteSynchronously = 0x80000
    }

    internal class StackGuard
    {
        private int m_inliningDepth = 0;
        private const int MAX_UNCHECKED_INLINING_DEPTH = 20;
        internal bool TryBeginInliningScope()
        {
            if (m_inliningDepth < MAX_UNCHECKED_INLINING_DEPTH || CheckForSufficientStack())
            {
                m_inliningDepth++;
                return true;
            }
            else
                return false;
        }

        internal void EndInliningScope()
        {
            m_inliningDepth--;
            Contract.Assert(m_inliningDepth >= 0, "Inlining depth count should never go negative.");
            if (m_inliningDepth < 0)
                m_inliningDepth = 0;
        }

        private unsafe bool CheckForSufficientStack()
        {
            return RuntimeHelpers.TryEnsureSufficientExecutionStack();
        }
    }

    internal struct VoidTaskResult
    {
    }

    internal interface ITaskCompletionAction
    {
        void Invoke(Task completingTask);
        bool InvokeMayRunArbitraryCode
        {
            get;
        }
    }

    internal sealed class UnwrapPromise<TResult> : Task<TResult>, ITaskCompletionAction
    {
        private const byte STATE_WAITING_ON_OUTER_TASK = 0;
        private const byte STATE_WAITING_ON_INNER_TASK = 1;
        private const byte STATE_DONE = 2;
        private byte _state;
        private readonly bool _lookForOce;
        public UnwrapPromise(Task outerTask, bool lookForOce): base ((object)null, outerTask.CreationOptions & TaskCreationOptions.AttachedToParent)
        {
            Contract.Requires(outerTask != null, "Expected non-null outerTask");
            _lookForOce = lookForOce;
            _state = STATE_WAITING_ON_OUTER_TASK;
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "Task.Unwrap", 0);
            if (Task.s_asyncDebuggingEnabled)
            {
                AddToActiveTasks(this);
            }

            if (outerTask.IsCompleted)
            {
                ProcessCompletedOuterTask(outerTask);
            }
            else
            {
                outerTask.AddCompletionAction(this);
            }
        }

        public void Invoke(Task completingTask)
        {
            var sg = Task.CurrentStackGuard;
            if (sg.TryBeginInliningScope())
            {
                try
                {
                    InvokeCore(completingTask);
                }
                finally
                {
                    sg.EndInliningScope();
                }
            }
            else
                InvokeCoreAsync(completingTask);
        }

        private void InvokeCore(Task completingTask)
        {
            switch (_state)
            {
                case STATE_WAITING_ON_OUTER_TASK:
                    ProcessCompletedOuterTask(completingTask);
                    break;
                case STATE_WAITING_ON_INNER_TASK:
                    bool result = TrySetFromTask(completingTask, lookForOce: false);
                    _state = STATE_DONE;
                    Contract.Assert(result, "Expected TrySetFromTask from inner task to succeed");
                    break;
                default:
                    Contract.Assert(false, "UnwrapPromise in illegal state");
                    break;
            }
        }

        private void InvokeCoreAsync(Task completingTask)
        {
            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                var tuple = (Tuple<UnwrapPromise<TResult>, Task>)state;
                tuple.Item1.InvokeCore(tuple.Item2);
            }

            , Tuple.Create<UnwrapPromise<TResult>, Task>(this, completingTask));
        }

        private void ProcessCompletedOuterTask(Task task)
        {
            Contract.Requires(task != null && task.IsCompleted, "Expected non-null, completed outer task");
            Contract.Assert(_state == STATE_WAITING_ON_OUTER_TASK, "We're in the wrong state!");
            _state = STATE_WAITING_ON_INNER_TASK;
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    bool result = TrySetFromTask(task, _lookForOce);
                    Contract.Assert(result, "Expected TrySetFromTask from outer task to succeed");
                    break;
                case TaskStatus.RanToCompletion:
                    var taskOfTaskOfTResult = task as Task<Task<TResult>>;
                    ProcessInnerTask(taskOfTaskOfTResult != null ? taskOfTaskOfTResult.Result : ((Task<Task>)task).Result);
                    break;
            }
        }

        private bool TrySetFromTask(Task task, bool lookForOce)
        {
            Contract.Requires(task != null && task.IsCompleted, "TrySetFromTask: Expected task to have completed.");
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, this.Id, CausalityRelation.Join);
            bool result = false;
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    result = TrySetCanceled(task.CancellationToken, task.GetCancellationExceptionDispatchInfo());
                    break;
                case TaskStatus.Faulted:
                    var edis = task.GetExceptionDispatchInfos();
                    ExceptionDispatchInfo oceEdi;
                    OperationCanceledException oce;
                    if (lookForOce && edis.Count > 0 && (oceEdi = edis[0]) != null && (oce = oceEdi.SourceException as OperationCanceledException) != null)
                    {
                        result = TrySetCanceled(oce.CancellationToken, oceEdi);
                    }
                    else
                    {
                        result = TrySetException(edis);
                    }

                    break;
                case TaskStatus.RanToCompletion:
                    var taskTResult = task as Task<TResult>;
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
                    if (Task.s_asyncDebuggingEnabled)
                    {
                        RemoveFromActiveTasks(this.Id);
                    }

                    result = TrySetResult(taskTResult != null ? taskTResult.Result : default (TResult));
                    break;
            }

            return result;
        }

        private void ProcessInnerTask(Task task)
        {
            if (task == null)
            {
                TrySetCanceled(default (CancellationToken));
                _state = STATE_DONE;
            }
            else if (task.IsCompleted)
            {
                TrySetFromTask(task, lookForOce: false);
                _state = STATE_DONE;
            }
            else
            {
                task.AddCompletionAction(this);
            }
        }

        public bool InvokeMayRunArbitraryCode
        {
            get
            {
                return true;
            }
        }
    }
}