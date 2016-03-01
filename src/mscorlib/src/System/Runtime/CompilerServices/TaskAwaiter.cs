using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Permissions;
using System.Diagnostics.Tracing;

namespace System.Runtime.CompilerServices
{
    public struct TaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly Task m_task;
        internal TaskAwaiter(Task task)
        {
            Contract.Requires(task != null, "Constructing an awaiter requires a task to await.");
            m_task = task;
        }

        public bool IsCompleted
        {
            get
            {
                return m_task.IsCompleted;
            }
        }

        public void OnCompleted(Action continuation)
        {
            OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: true);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: false);
        }

        public void GetResult()
        {
            ValidateEnd(m_task);
        }

        internal static void ValidateEnd(Task task)
        {
            if (task.IsWaitNotificationEnabledOrNotRanToCompletion)
            {
                HandleNonSuccessAndDebuggerNotification(task);
            }
        }

        private static void HandleNonSuccessAndDebuggerNotification(Task task)
        {
            if (!task.IsCompleted)
            {
                bool taskCompleted = task.InternalWait(Timeout.Infinite, default (CancellationToken));
                Contract.Assert(taskCompleted, "With an infinite timeout, the task should have always completed.");
            }

            task.NotifyDebuggerOfWaitCompletionIfNecessary();
            if (!task.IsRanToCompletion)
                ThrowForNonSuccess(task);
        }

        private static void ThrowForNonSuccess(Task task)
        {
            Contract.Requires(task.IsCompleted, "Task must have been completed by now.");
            Contract.Requires(task.Status != TaskStatus.RanToCompletion, "Task should not be completed successfully.");
            switch (task.Status)
            {
                case TaskStatus.Canceled:
                    var oceEdi = task.GetCancellationExceptionDispatchInfo();
                    if (oceEdi != null)
                    {
                        oceEdi.Throw();
                        Contract.Assert(false, "Throw() should have thrown");
                    }

                    throw new TaskCanceledException(task);
                case TaskStatus.Faulted:
                    var edis = task.GetExceptionDispatchInfos();
                    if (edis.Count > 0)
                    {
                        edis[0].Throw();
                        Contract.Assert(false, "Throw() should have thrown");
                        break;
                    }
                    else
                    {
                        Contract.Assert(false, "There should be exceptions if we're Faulted.");
                        throw task.Exception;
                    }
            }
        }

        internal static void OnCompletedInternal(Task task, Action continuation, bool continueOnCapturedContext, bool flowExecutionContext)
        {
            if (continuation == null)
                throw new ArgumentNullException("continuation");
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            if (TplEtwProvider.Log.IsEnabled() || Task.s_asyncDebuggingEnabled)
            {
                continuation = OutputWaitEtwEvents(task, continuation);
            }

            task.SetContinuationForAwait(continuation, continueOnCapturedContext, flowExecutionContext, ref stackMark);
        }

        private static Action OutputWaitEtwEvents(Task task, Action continuation)
        {
            Contract.Requires(task != null, "Need a task to wait on");
            Contract.Requires(continuation != null, "Need a continuation to invoke when the wait completes");
            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(task);
            }

            var etwLog = TplEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                var currentTaskAtBegin = Task.InternalCurrent;
                var continuationTask = AsyncMethodBuilderCore.TryGetContinuationTask(continuation);
                etwLog.TaskWaitBegin((currentTaskAtBegin != null ? currentTaskAtBegin.m_taskScheduler.Id : TaskScheduler.Default.Id), (currentTaskAtBegin != null ? currentTaskAtBegin.Id : 0), task.Id, TplEtwProvider.TaskWaitBehavior.Asynchronous, (continuationTask != null ? continuationTask.Id : 0), System.Threading.Thread.GetDomainID());
            }

            return AsyncMethodBuilderCore.CreateContinuationWrapper(continuation, () =>
            {
                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(task.Id);
                }

                Guid prevActivityId = new Guid();
                bool bEtwLogEnabled = etwLog.IsEnabled();
                if (bEtwLogEnabled)
                {
                    var currentTaskAtEnd = Task.InternalCurrent;
                    etwLog.TaskWaitEnd((currentTaskAtEnd != null ? currentTaskAtEnd.m_taskScheduler.Id : TaskScheduler.Default.Id), (currentTaskAtEnd != null ? currentTaskAtEnd.Id : 0), task.Id);
                    if (etwLog.TasksSetActivityIds && (task.Options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0)
                        EventSource.SetCurrentThreadActivityId(TplEtwProvider.CreateGuidForTaskID(task.Id), out prevActivityId);
                }

                continuation();
                if (bEtwLogEnabled)
                {
                    etwLog.TaskWaitContinuationComplete(task.Id);
                    if (etwLog.TasksSetActivityIds && (task.Options & (TaskCreationOptions)InternalTaskOptions.PromiseTask) != 0)
                        EventSource.SetCurrentThreadActivityId(prevActivityId);
                }
            }

            );
        }
    }

    public struct TaskAwaiter<TResult> : ICriticalNotifyCompletion
    {
        private readonly Task<TResult> m_task;
        internal TaskAwaiter(Task<TResult> task)
        {
            Contract.Requires(task != null, "Constructing an awaiter requires a task to await.");
            m_task = task;
        }

        public bool IsCompleted
        {
            get
            {
                return m_task.IsCompleted;
            }
        }

        public void OnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: true);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            TaskAwaiter.OnCompletedInternal(m_task, continuation, continueOnCapturedContext: true, flowExecutionContext: false);
        }

        public TResult GetResult()
        {
            TaskAwaiter.ValidateEnd(m_task);
            return m_task.ResultOnSuccess;
        }
    }

    public struct ConfiguredTaskAwaitable
    {
        private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter m_configuredTaskAwaiter;
        internal ConfiguredTaskAwaitable(Task task, bool continueOnCapturedContext)
        {
            Contract.Requires(task != null, "Constructing an awaitable requires a task to await.");
            m_configuredTaskAwaiter = new ConfiguredTaskAwaitable.ConfiguredTaskAwaiter(task, continueOnCapturedContext);
        }

        public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter()
        {
            return m_configuredTaskAwaiter;
        }

        public struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion
        {
            private readonly Task m_task;
            private readonly bool m_continueOnCapturedContext;
            internal ConfiguredTaskAwaiter(Task task, bool continueOnCapturedContext)
            {
                Contract.Requires(task != null, "Constructing an awaiter requires a task to await.");
                m_task = task;
                m_continueOnCapturedContext = continueOnCapturedContext;
            }

            public bool IsCompleted
            {
                get
                {
                    return m_task.IsCompleted;
                }
            }

            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, m_continueOnCapturedContext, flowExecutionContext: true);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, m_continueOnCapturedContext, flowExecutionContext: false);
            }

            public void GetResult()
            {
                TaskAwaiter.ValidateEnd(m_task);
            }
        }
    }

    public struct ConfiguredTaskAwaitable<TResult>
    {
        private readonly ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter m_configuredTaskAwaiter;
        internal ConfiguredTaskAwaitable(Task<TResult> task, bool continueOnCapturedContext)
        {
            m_configuredTaskAwaiter = new ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter(task, continueOnCapturedContext);
        }

        public ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter GetAwaiter()
        {
            return m_configuredTaskAwaiter;
        }

        public struct ConfiguredTaskAwaiter : ICriticalNotifyCompletion
        {
            private readonly Task<TResult> m_task;
            private readonly bool m_continueOnCapturedContext;
            internal ConfiguredTaskAwaiter(Task<TResult> task, bool continueOnCapturedContext)
            {
                Contract.Requires(task != null, "Constructing an awaiter requires a task to await.");
                m_task = task;
                m_continueOnCapturedContext = continueOnCapturedContext;
            }

            public bool IsCompleted
            {
                get
                {
                    return m_task.IsCompleted;
                }
            }

            public void OnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, m_continueOnCapturedContext, flowExecutionContext: true);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                TaskAwaiter.OnCompletedInternal(m_task, continuation, m_continueOnCapturedContext, flowExecutionContext: false);
            }

            public TResult GetResult()
            {
                TaskAwaiter.ValidateEnd(m_task);
                return m_task.ResultOnSuccess;
            }
        }
    }
}