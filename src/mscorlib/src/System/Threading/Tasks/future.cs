using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    public class Task<TResult> : Task
    {
        internal TResult m_result;
        private static readonly TaskFactory<TResult> s_Factory = new TaskFactory<TResult>();
        internal static readonly Func<Task<Task>, Task<TResult>> TaskWhenAnyCast = completed => (Task<TResult>)completed.Result;
        internal Task(): base ()
        {
        }

        internal Task(object state, TaskCreationOptions options): base (state, options, promiseStyle: true)
        {
        }

        internal Task(TResult result): base (false, TaskCreationOptions.None, default (CancellationToken))
        {
            m_result = result;
        }

        internal Task(bool canceled, TResult result, TaskCreationOptions creationOptions, CancellationToken ct): base (canceled, creationOptions, ct)
        {
            if (!canceled)
            {
                m_result = result;
            }
        }

        public Task(Func<TResult> function): this (function, null, default (CancellationToken), TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Func<TResult> function, CancellationToken cancellationToken): this (function, null, cancellationToken, TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Func<TResult> function, TaskCreationOptions creationOptions): this (function, Task.InternalCurrentIfAttached(creationOptions), default (CancellationToken), creationOptions, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions): this (function, Task.InternalCurrentIfAttached(creationOptions), cancellationToken, creationOptions, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Func<object, TResult> function, object state): this (function, state, null, default (CancellationToken), TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Func<object, TResult> function, object state, CancellationToken cancellationToken): this (function, state, null, cancellationToken, TaskCreationOptions.None, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Func<object, TResult> function, object state, TaskCreationOptions creationOptions): this (function, state, Task.InternalCurrentIfAttached(creationOptions), default (CancellationToken), creationOptions, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        public Task(Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions): this (function, state, Task.InternalCurrentIfAttached(creationOptions), cancellationToken, creationOptions, InternalTaskOptions.None, null)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PossiblyCaptureContext(ref stackMark);
        }

        internal Task(Func<TResult> valueSelector, Task parent, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler, ref StackCrawlMark stackMark): this (valueSelector, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
            PossiblyCaptureContext(ref stackMark);
        }

        internal Task(Func<TResult> valueSelector, Task parent, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler): base (valueSelector, null, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
            if ((internalOptions & InternalTaskOptions.SelfReplicating) != 0)
            {
                throw new ArgumentOutOfRangeException("creationOptions", Environment.GetResourceString("TaskT_ctor_SelfReplicating"));
            }
        }

        internal Task(Func<object, TResult> valueSelector, object state, Task parent, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler, ref StackCrawlMark stackMark): this (valueSelector, state, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
            PossiblyCaptureContext(ref stackMark);
        }

        internal Task(Delegate valueSelector, object state, Task parent, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler): base (valueSelector, state, parent, cancellationToken, creationOptions, internalOptions, scheduler)
        {
            if ((internalOptions & InternalTaskOptions.SelfReplicating) != 0)
            {
                throw new ArgumentOutOfRangeException("creationOptions", Environment.GetResourceString("TaskT_ctor_SelfReplicating"));
            }
        }

        internal static Task<TResult> StartNew(Task parent, Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            if ((internalOptions & InternalTaskOptions.SelfReplicating) != 0)
            {
                throw new ArgumentOutOfRangeException("creationOptions", Environment.GetResourceString("TaskT_ctor_SelfReplicating"));
            }

            Task<TResult> f = new Task<TResult>(function, parent, cancellationToken, creationOptions, internalOptions | InternalTaskOptions.QueuedByRuntime, scheduler, ref stackMark);
            f.ScheduleAndStart(false);
            return f;
        }

        internal static Task<TResult> StartNew(Task parent, Func<object, TResult> function, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            if ((internalOptions & InternalTaskOptions.SelfReplicating) != 0)
            {
                throw new ArgumentOutOfRangeException("creationOptions", Environment.GetResourceString("TaskT_ctor_SelfReplicating"));
            }

            Task<TResult> f = new Task<TResult>(function, state, parent, cancellationToken, creationOptions, internalOptions | InternalTaskOptions.QueuedByRuntime, scheduler, ref stackMark);
            f.ScheduleAndStart(false);
            return f;
        }

        private string DebuggerDisplayResultDescription
        {
            get
            {
                return IsRanToCompletion ? "" + m_result : Environment.GetResourceString("TaskT_DebuggerNoResult");
            }
        }

        private string DebuggerDisplayMethodDescription
        {
            get
            {
                Delegate d = (Delegate)m_action;
                return d != null ? d.Method.ToString() : "{null}";
            }
        }

        internal bool TrySetResult(TResult result)
        {
            if (IsCompleted)
                return false;
                        if (AtomicStateUpdate(TASK_STATE_COMPLETION_RESERVED, TASK_STATE_COMPLETION_RESERVED | TASK_STATE_RAN_TO_COMPLETION | TASK_STATE_FAULTED | TASK_STATE_CANCELED))
            {
                m_result = result;
                Interlocked.Exchange(ref m_stateFlags, m_stateFlags | TASK_STATE_RAN_TO_COMPLETION);
                var cp = m_contingentProperties;
                if (cp != null)
                    cp.SetCompleted();
                FinishStageThree();
                return true;
            }

            return false;
        }

        internal void DangerousSetResult(TResult result)
        {
                        if (m_parent != null)
            {
                bool success = TrySetResult(result);
                            }
            else
            {
                m_result = result;
                m_stateFlags |= TASK_STATE_RAN_TO_COMPLETION;
            }
        }

        public TResult Result
        {
            get
            {
                return IsWaitNotificationEnabledOrNotRanToCompletion ? GetResultCore(waitCompletionNotification: true) : m_result;
            }
        }

        internal TResult ResultOnSuccess
        {
            get
            {
                                return m_result;
            }
        }

        internal TResult GetResultCore(bool waitCompletionNotification)
        {
            if (!IsCompleted)
                InternalWait(Timeout.Infinite, default (CancellationToken));
            if (waitCompletionNotification)
                NotifyDebuggerOfWaitCompletionIfNecessary();
            if (!IsRanToCompletion)
                ThrowIfExceptional(includeTaskCanceledExceptions: true);
                        return m_result;
        }

        internal bool TrySetException(object exceptionObject)
        {
                                                bool returnValue = false;
            EnsureContingentPropertiesInitialized(needsProtection: true);
            if (AtomicStateUpdate(TASK_STATE_COMPLETION_RESERVED, TASK_STATE_COMPLETION_RESERVED | TASK_STATE_RAN_TO_COMPLETION | TASK_STATE_FAULTED | TASK_STATE_CANCELED))
            {
                AddException(exceptionObject);
                Finish(false);
                returnValue = true;
            }

            return returnValue;
        }

        internal bool TrySetCanceled(CancellationToken tokenToRecord)
        {
            return TrySetCanceled(tokenToRecord, null);
        }

        internal bool TrySetCanceled(CancellationToken tokenToRecord, object cancellationException)
        {
                        var ceAsEdi = cancellationException as ExceptionDispatchInfo;
                        bool returnValue = false;
            if (AtomicStateUpdate(Task<object>.TASK_STATE_COMPLETION_RESERVED, Task<object>.TASK_STATE_COMPLETION_RESERVED | Task<object>.TASK_STATE_CANCELED | Task<object>.TASK_STATE_FAULTED | Task<object>.TASK_STATE_RAN_TO_COMPLETION))
            {
                RecordInternalCancellationRequest(tokenToRecord, cancellationException);
                CancellationCleanupLogic();
                returnValue = true;
            }

            return returnValue;
        }

        public new static TaskFactory<TResult> Factory
        {
            get
            {
                return s_Factory;
            }
        }

        internal override void InnerInvoke()
        {
                        var func = m_action as Func<TResult>;
            if (func != null)
            {
                m_result = func();
                return;
            }

            var funcWithState = m_action as Func<object, TResult>;
            if (funcWithState != null)
            {
                m_result = funcWithState(m_stateObject);
                return;
            }

                    }

        public new TaskAwaiter<TResult> GetAwaiter()
        {
            return new TaskAwaiter<TResult>(this);
        }

        public new ConfiguredTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            return new ConfiguredTaskAwaitable<TResult>(this, continueOnCapturedContext);
        }

        public Task ContinueWith(Action<Task<TResult>> continuationAction)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, TaskScheduler.Current, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task<TResult>> continuationAction, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task<TResult>> continuationAction, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, scheduler, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task<TResult>> continuationAction, TaskContinuationOptions continuationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, TaskScheduler.Current, default (CancellationToken), continuationOptions, ref stackMark);
        }

        public Task ContinueWith(Action<Task<TResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
        }

        internal Task ContinueWith(Action<Task<TResult>> continuationAction, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
        {
            if (continuationAction == null)
            {
                throw new ArgumentNullException("continuationAction");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            TaskCreationOptions creationOptions;
            InternalTaskOptions internalOptions;
            CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
            Task continuationTask = new ContinuationTaskFromResultTask<TResult>(this, continuationAction, null, creationOptions, internalOptions, ref stackMark);
            ContinueWithCore(continuationTask, scheduler, cancellationToken, continuationOptions);
            return continuationTask;
        }

        public Task ContinueWith(Action<Task<TResult>, Object> continuationAction, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, TaskScheduler.Current, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task<TResult>, Object> continuationAction, Object state, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task<TResult>, Object> continuationAction, Object state, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, scheduler, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task ContinueWith(Action<Task<TResult>, Object> continuationAction, Object state, TaskContinuationOptions continuationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, TaskScheduler.Current, default (CancellationToken), continuationOptions, ref stackMark);
        }

        public Task ContinueWith(Action<Task<TResult>, Object> continuationAction, Object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith(continuationAction, state, scheduler, cancellationToken, continuationOptions, ref stackMark);
        }

        internal Task ContinueWith(Action<Task<TResult>, Object> continuationAction, Object state, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
        {
            if (continuationAction == null)
            {
                throw new ArgumentNullException("continuationAction");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            TaskCreationOptions creationOptions;
            InternalTaskOptions internalOptions;
            CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
            Task continuationTask = new ContinuationTaskFromResultTask<TResult>(this, continuationAction, state, creationOptions, internalOptions, ref stackMark);
            ContinueWithCore(continuationTask, scheduler, cancellationToken, continuationOptions);
            return continuationTask;
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, TaskScheduler.Current, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, scheduler, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, TaskScheduler.Current, default (CancellationToken), continuationOptions, ref stackMark);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
        }

        internal Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, TNewResult> continuationFunction, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
        {
            if (continuationFunction == null)
            {
                throw new ArgumentNullException("continuationFunction");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            TaskCreationOptions creationOptions;
            InternalTaskOptions internalOptions;
            CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
            Task<TNewResult> continuationFuture = new ContinuationResultTaskFromResultTask<TResult, TNewResult>(this, continuationFunction, null, creationOptions, internalOptions, ref stackMark);
            ContinueWithCore(continuationFuture, scheduler, cancellationToken, continuationOptions);
            return continuationFuture;
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, Object, TNewResult> continuationFunction, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, state, TaskScheduler.Current, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, Object, TNewResult> continuationFunction, Object state, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, state, TaskScheduler.Current, cancellationToken, TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, Object, TNewResult> continuationFunction, Object state, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, state, scheduler, default (CancellationToken), TaskContinuationOptions.None, ref stackMark);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, Object, TNewResult> continuationFunction, Object state, TaskContinuationOptions continuationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, state, TaskScheduler.Current, default (CancellationToken), continuationOptions, ref stackMark);
        }

        public Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, Object, TNewResult> continuationFunction, Object state, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWith<TNewResult>(continuationFunction, state, scheduler, cancellationToken, continuationOptions, ref stackMark);
        }

        internal Task<TNewResult> ContinueWith<TNewResult>(Func<Task<TResult>, Object, TNewResult> continuationFunction, Object state, TaskScheduler scheduler, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, ref StackCrawlMark stackMark)
        {
            if (continuationFunction == null)
            {
                throw new ArgumentNullException("continuationFunction");
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException("scheduler");
            }

            TaskCreationOptions creationOptions;
            InternalTaskOptions internalOptions;
            CreationOptionsFromContinuationOptions(continuationOptions, out creationOptions, out internalOptions);
            Task<TNewResult> continuationFuture = new ContinuationResultTaskFromResultTask<TResult, TNewResult>(this, continuationFunction, state, creationOptions, internalOptions, ref stackMark);
            ContinueWithCore(continuationFuture, scheduler, cancellationToken, continuationOptions);
            return continuationFuture;
        }
    }

    internal class SystemThreadingTasks_FutureDebugView<TResult>
    {
        private Task<TResult> m_task;
        public SystemThreadingTasks_FutureDebugView(Task<TResult> task)
        {
            m_task = task;
        }

        public TResult Result
        {
            get
            {
                return m_task.Status == TaskStatus.RanToCompletion ? m_task.Result : default (TResult);
            }
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
}