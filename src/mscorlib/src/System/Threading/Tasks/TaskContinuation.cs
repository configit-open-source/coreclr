
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace System.Threading.Tasks
{
    internal sealed class ContinuationTaskFromTask : Task
    {
        private Task m_antecedent;
        public ContinuationTaskFromTask(Task antecedent, Delegate action, object state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark): base (action, state, Task.InternalCurrentIfAttached(creationOptions), default (CancellationToken), creationOptions, internalOptions, null)
        {
                        m_antecedent = antecedent;
            PossiblyCaptureContext(ref stackMark);
        }

        internal override void InnerInvoke()
        {
            var antecedent = m_antecedent;
                        m_antecedent = null;
            antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();
                        var action = m_action as Action<Task>;
            if (action != null)
            {
                action(antecedent);
                return;
            }

            var actionWithState = m_action as Action<Task, object>;
            if (actionWithState != null)
            {
                actionWithState(antecedent, m_stateObject);
                return;
            }

                    }
    }

    internal sealed class ContinuationResultTaskFromTask<TResult> : Task<TResult>
    {
        private Task m_antecedent;
        public ContinuationResultTaskFromTask(Task antecedent, Delegate function, object state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark): base (function, state, Task.InternalCurrentIfAttached(creationOptions), default (CancellationToken), creationOptions, internalOptions, null)
        {
                        m_antecedent = antecedent;
            PossiblyCaptureContext(ref stackMark);
        }

        internal override void InnerInvoke()
        {
            var antecedent = m_antecedent;
                        m_antecedent = null;
            antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();
                        var func = m_action as Func<Task, TResult>;
            if (func != null)
            {
                m_result = func(antecedent);
                return;
            }

            var funcWithState = m_action as Func<Task, object, TResult>;
            if (funcWithState != null)
            {
                m_result = funcWithState(antecedent, m_stateObject);
                return;
            }

                    }
    }

    internal sealed class ContinuationTaskFromResultTask<TAntecedentResult> : Task
    {
        private Task<TAntecedentResult> m_antecedent;
        public ContinuationTaskFromResultTask(Task<TAntecedentResult> antecedent, Delegate action, object state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark): base (action, state, Task.InternalCurrentIfAttached(creationOptions), default (CancellationToken), creationOptions, internalOptions, null)
        {
                        m_antecedent = antecedent;
            PossiblyCaptureContext(ref stackMark);
        }

        internal override void InnerInvoke()
        {
            var antecedent = m_antecedent;
                        m_antecedent = null;
            antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();
                        var action = m_action as Action<Task<TAntecedentResult>>;
            if (action != null)
            {
                action(antecedent);
                return;
            }

            var actionWithState = m_action as Action<Task<TAntecedentResult>, object>;
            if (actionWithState != null)
            {
                actionWithState(antecedent, m_stateObject);
                return;
            }

                    }
    }

    internal sealed class ContinuationResultTaskFromResultTask<TAntecedentResult, TResult> : Task<TResult>
    {
        private Task<TAntecedentResult> m_antecedent;
        public ContinuationResultTaskFromResultTask(Task<TAntecedentResult> antecedent, Delegate function, object state, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, ref StackCrawlMark stackMark): base (function, state, Task.InternalCurrentIfAttached(creationOptions), default (CancellationToken), creationOptions, internalOptions, null)
        {
                        m_antecedent = antecedent;
            PossiblyCaptureContext(ref stackMark);
        }

        internal override void InnerInvoke()
        {
            var antecedent = m_antecedent;
                        m_antecedent = null;
            antecedent.NotifyDebuggerOfWaitCompletionIfNecessary();
                        var func = m_action as Func<Task<TAntecedentResult>, TResult>;
            if (func != null)
            {
                m_result = func(antecedent);
                return;
            }

            var funcWithState = m_action as Func<Task<TAntecedentResult>, object, TResult>;
            if (funcWithState != null)
            {
                m_result = funcWithState(antecedent, m_stateObject);
                return;
            }

                    }
    }

    internal abstract class TaskContinuation
    {
        internal abstract void Run(Task completedTask, bool bCanInlineContinuationTask);
        protected static void InlineIfPossibleOrElseQueue(Task task, bool needsProtection)
        {
                                    if (needsProtection)
            {
                if (!task.MarkStarted())
                    return;
            }
            else
            {
                task.m_stateFlags |= Task.TASK_STATE_STARTED;
            }

            try
            {
                if (!task.m_taskScheduler.TryRunInline(task, taskWasPreviouslyQueued: false))
                {
                    task.m_taskScheduler.InternalQueueTask(task);
                }
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException && (task.m_stateFlags & Task.TASK_STATE_THREAD_WAS_ABORTED) != 0))
                {
                    TaskSchedulerException tse = new TaskSchedulerException(e);
                    task.AddException(tse);
                    task.Finish(false);
                }
            }
        }

        internal abstract Delegate[] GetDelegateContinuationsForDebugger();
    }

    internal class StandardTaskContinuation : TaskContinuation
    {
        internal readonly Task m_task;
        internal readonly TaskContinuationOptions m_options;
        private readonly TaskScheduler m_taskScheduler;
        internal StandardTaskContinuation(Task task, TaskContinuationOptions options, TaskScheduler scheduler)
        {
                                    m_task = task;
            m_options = options;
            m_taskScheduler = scheduler;
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, m_task.Id, "Task.ContinueWith: " + ((Delegate)task.m_action).Method.Name, 0);
            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(m_task);
            }
        }

        internal override void Run(Task completedTask, bool bCanInlineContinuationTask)
        {
                                    TaskContinuationOptions options = m_options;
            bool isRightKind = completedTask.IsRanToCompletion ? (options & TaskContinuationOptions.NotOnRanToCompletion) == 0 : (completedTask.IsCanceled ? (options & TaskContinuationOptions.NotOnCanceled) == 0 : (options & TaskContinuationOptions.NotOnFaulted) == 0);
            Task continuationTask = m_task;
            if (isRightKind)
            {
                if (!continuationTask.IsCanceled && AsyncCausalityTracer.LoggingOn)
                {
                    AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, continuationTask.Id, CausalityRelation.AssignDelegate);
                }

                continuationTask.m_taskScheduler = m_taskScheduler;
                if (bCanInlineContinuationTask && (options & TaskContinuationOptions.ExecuteSynchronously) != 0)
                {
                    InlineIfPossibleOrElseQueue(continuationTask, needsProtection: true);
                }
                else
                {
                    try
                    {
                        continuationTask.ScheduleAndStart(needsProtection: true);
                    }
                    catch (TaskSchedulerException)
                    {
                    }
                }
            }
            else
                continuationTask.InternalCancel(false);
        }

        internal override Delegate[] GetDelegateContinuationsForDebugger()
        {
            if (m_task.m_action == null)
            {
                return m_task.GetDelegateContinuationsForDebugger();
            }

            return new Delegate[]{m_task.m_action as Delegate};
        }
    }

    internal sealed class SynchronizationContextAwaitTaskContinuation : AwaitTaskContinuation
    {
        private readonly static SendOrPostCallback s_postCallback = state => ((Action)state)();
        private static ContextCallback s_postActionCallback;
        private readonly SynchronizationContext m_syncContext;
        internal SynchronizationContextAwaitTaskContinuation(SynchronizationContext context, Action action, bool flowExecutionContext, ref StackCrawlMark stackMark): base (action, flowExecutionContext, ref stackMark)
        {
                        m_syncContext = context;
        }

        internal sealed override void Run(Task task, bool canInlineContinuationTask)
        {
            if (canInlineContinuationTask && m_syncContext == SynchronizationContext.CurrentNoFlow)
            {
                RunCallback(GetInvokeActionCallback(), m_action, ref Task.t_currentTask);
            }
            else
            {
                TplEtwProvider etwLog = TplEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    m_continuationId = Task.NewId();
                    etwLog.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, m_continuationId);
                }

                RunCallback(GetPostActionCallback(), this, ref Task.t_currentTask);
            }
        }

        private static void PostAction(object state)
        {
            var c = (SynchronizationContextAwaitTaskContinuation)state;
            TplEtwProvider etwLog = TplEtwProvider.Log;
            if (etwLog.TasksSetActivityIds && c.m_continuationId != 0)
            {
                c.m_syncContext.Post(s_postCallback, GetActionLogDelegate(c.m_continuationId, c.m_action));
            }
            else
            {
                c.m_syncContext.Post(s_postCallback, c.m_action);
            }
        }

        private static Action GetActionLogDelegate(int continuationId, Action action)
        {
            return () =>
            {
                Guid savedActivityId;
                Guid activityId = TplEtwProvider.CreateGuidForTaskID(continuationId);
                System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(activityId, out savedActivityId);
                try
                {
                    action();
                }
                finally
                {
                    System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(savedActivityId);
                }
            }

            ;
        }

        private static ContextCallback GetPostActionCallback()
        {
            ContextCallback callback = s_postActionCallback;
            if (callback == null)
            {
                s_postActionCallback = callback = PostAction;
            }

            return callback;
        }
    }

    internal sealed class TaskSchedulerAwaitTaskContinuation : AwaitTaskContinuation
    {
        private readonly TaskScheduler m_scheduler;
        internal TaskSchedulerAwaitTaskContinuation(TaskScheduler scheduler, Action action, bool flowExecutionContext, ref StackCrawlMark stackMark): base (action, flowExecutionContext, ref stackMark)
        {
                        m_scheduler = scheduler;
        }

        internal sealed override void Run(Task ignored, bool canInlineContinuationTask)
        {
            if (m_scheduler == TaskScheduler.Default)
            {
                base.Run(ignored, canInlineContinuationTask);
            }
            else
            {
                bool inlineIfPossible = canInlineContinuationTask && (TaskScheduler.InternalCurrent == m_scheduler || Thread.CurrentThread.IsThreadPoolThread);
                var task = CreateTask(state =>
                {
                    try
                    {
                        ((Action)state)();
                    }
                    catch (Exception exc)
                    {
                        ThrowAsyncIfNecessary(exc);
                    }
                }

                , m_action, m_scheduler);
                if (inlineIfPossible)
                {
                    InlineIfPossibleOrElseQueue(task, needsProtection: false);
                }
                else
                {
                    try
                    {
                        task.ScheduleAndStart(needsProtection: false);
                    }
                    catch (TaskSchedulerException)
                    {
                    }
                }
            }
        }
    }

    internal class AwaitTaskContinuation : TaskContinuation, IThreadPoolWorkItem
    {
        private readonly ExecutionContext m_capturedContext;
        protected readonly Action m_action;
        protected int m_continuationId;
        internal AwaitTaskContinuation(Action action, bool flowExecutionContext, ref StackCrawlMark stackMark)
        {
                        m_action = action;
            if (flowExecutionContext)
            {
                m_capturedContext = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx | ExecutionContext.CaptureOptions.OptimizeDefaultCase);
            }
        }

        internal AwaitTaskContinuation(Action action, bool flowExecutionContext)
        {
                        m_action = action;
            if (flowExecutionContext)
            {
                m_capturedContext = ExecutionContext.FastCapture();
            }
        }

        protected Task CreateTask(Action<object> action, object state, TaskScheduler scheduler)
        {
                                    return new Task(action, state, null, default (CancellationToken), TaskCreationOptions.None, InternalTaskOptions.QueuedByRuntime, scheduler)
            {CapturedContext = m_capturedContext};
        }

        internal override void Run(Task task, bool canInlineContinuationTask)
        {
            if (canInlineContinuationTask && IsValidLocationForInlining)
            {
                RunCallback(GetInvokeActionCallback(), m_action, ref Task.t_currentTask);
            }
            else
            {
                TplEtwProvider etwLog = TplEtwProvider.Log;
                if (etwLog.IsEnabled())
                {
                    m_continuationId = Task.NewId();
                    etwLog.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, m_continuationId);
                }

                ThreadPool.UnsafeQueueCustomWorkItem(this, forceGlobal: false);
            }
        }

        internal static bool IsValidLocationForInlining
        {
            get
            {
                var ctx = SynchronizationContext.CurrentNoFlow;
                if (ctx != null && ctx.GetType() != typeof (SynchronizationContext))
                    return false;
                var sched = TaskScheduler.InternalCurrent;
                return sched == null || sched == TaskScheduler.Default;
            }
        }

        void ExecuteWorkItemHelper()
        {
            var etwLog = TplEtwProvider.Log;
            Guid savedActivityId = Guid.Empty;
            if (etwLog.TasksSetActivityIds && m_continuationId != 0)
            {
                Guid activityId = TplEtwProvider.CreateGuidForTaskID(m_continuationId);
                System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(activityId, out savedActivityId);
            }

            try
            {
                if (m_capturedContext == null)
                {
                    m_action();
                }
                else
                {
                    try
                    {
                        ExecutionContext.Run(m_capturedContext, GetInvokeActionCallback(), m_action, true);
                    }
                    finally
                    {
                        m_capturedContext.Dispose();
                    }
                }
            }
            finally
            {
                if (etwLog.TasksSetActivityIds && m_continuationId != 0)
                {
                    System.Diagnostics.Tracing.EventSource.SetCurrentThreadActivityId(savedActivityId);
                }
            }
        }

        void IThreadPoolWorkItem.ExecuteWorkItem()
        {
            if (m_capturedContext == null && !TplEtwProvider.Log.IsEnabled())
            {
                m_action();
            }
            else
            {
                ExecuteWorkItemHelper();
            }
        }

        void IThreadPoolWorkItem.MarkAborted(ThreadAbortException tae)
        {
        }

        private static ContextCallback s_invokeActionCallback;
        private static void InvokeAction(object state)
        {
            ((Action)state)();
        }

        protected static ContextCallback GetInvokeActionCallback()
        {
            ContextCallback callback = s_invokeActionCallback;
            if (callback == null)
            {
                s_invokeActionCallback = callback = InvokeAction;
            }

            return callback;
        }

        protected void RunCallback(ContextCallback callback, object state, ref Task currentTask)
        {
                                    var prevCurrentTask = currentTask;
            try
            {
                if (prevCurrentTask != null)
                    currentTask = null;
                if (m_capturedContext == null)
                    callback(state);
                else
                    ExecutionContext.Run(m_capturedContext, callback, state, true);
            }
            catch (Exception exc)
            {
                ThrowAsyncIfNecessary(exc);
            }
            finally
            {
                if (prevCurrentTask != null)
                    currentTask = prevCurrentTask;
                if (m_capturedContext != null)
                    m_capturedContext.Dispose();
            }
        }

        internal static void RunOrScheduleAction(Action action, bool allowInlining, ref Task currentTask)
        {
                        if (!allowInlining || !IsValidLocationForInlining)
            {
                UnsafeScheduleAction(action, currentTask);
                return;
            }

            Task prevCurrentTask = currentTask;
            try
            {
                if (prevCurrentTask != null)
                    currentTask = null;
                action();
            }
            catch (Exception exception)
            {
                ThrowAsyncIfNecessary(exception);
            }
            finally
            {
                if (prevCurrentTask != null)
                    currentTask = prevCurrentTask;
            }
        }

        internal static void UnsafeScheduleAction(Action action, Task task)
        {
            AwaitTaskContinuation atc = new AwaitTaskContinuation(action, flowExecutionContext: false);
            var etwLog = TplEtwProvider.Log;
            if (etwLog.IsEnabled() && task != null)
            {
                atc.m_continuationId = Task.NewId();
                etwLog.AwaitTaskContinuationScheduled((task.ExecutingTaskScheduler ?? TaskScheduler.Default).Id, task.Id, atc.m_continuationId);
            }

            ThreadPool.UnsafeQueueCustomWorkItem(atc, forceGlobal: false);
        }

        protected static void ThrowAsyncIfNecessary(Exception exc)
        {
            if (!(exc is ThreadAbortException || exc is AppDomainUnloadedException))
            {
                if (!WindowsRuntimeMarshal.ReportUnhandledError(exc))
                {
                    var edi = ExceptionDispatchInfo.Capture(exc);
                    ThreadPool.QueueUserWorkItem(s => ((ExceptionDispatchInfo)s).Throw(), edi);
                }
            }
        }

        internal override Delegate[] GetDelegateContinuationsForDebugger()
        {
                        return new Delegate[]{AsyncMethodBuilderCore.TryGetStateMachineForDebugger(m_action)};
        }
    }
}