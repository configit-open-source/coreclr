using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics.Contracts;

namespace System.Threading.Tasks
{
    public class TaskFactory
    {
        private CancellationToken m_defaultCancellationToken;
        private TaskScheduler m_defaultScheduler;
        private TaskCreationOptions m_defaultCreationOptions;
        private TaskContinuationOptions m_defaultContinuationOptions;
        private TaskScheduler DefaultScheduler
        {
            get
            {
                if (m_defaultScheduler == null)
                    return TaskScheduler.Current;
                else
                    return m_defaultScheduler;
            }
        }

        private TaskScheduler GetDefaultScheduler(Task currTask)
        {
            if (m_defaultScheduler != null)
                return m_defaultScheduler;
            else if ((currTask != null) && ((currTask.CreationOptions & TaskCreationOptions.HideScheduler) == 0))
                return currTask.ExecutingTaskScheduler;
            else
                return TaskScheduler.Default;
        }

        public TaskFactory(): this (default (CancellationToken), TaskCreationOptions.None, TaskContinuationOptions.None, null)
        {
        }

        public TaskFactory(CancellationToken cancellationToken): this (cancellationToken, TaskCreationOptions.None, TaskContinuationOptions.None, null)
        {
        }

        public TaskFactory(TaskScheduler scheduler): this (default (CancellationToken), TaskCreationOptions.None, TaskContinuationOptions.None, scheduler)
        {
        }

        public TaskFactory(TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions): this (default (CancellationToken), creationOptions, continuationOptions, null)
        {
        }

        public TaskFactory(CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            CheckMultiTaskContinuationOptions(continuationOptions);
            CheckCreationOptions(creationOptions);
            m_defaultCancellationToken = cancellationToken;
            m_defaultScheduler = scheduler;
            m_defaultCreationOptions = creationOptions;
            m_defaultContinuationOptions = continuationOptions;
        }

        internal static void CheckCreationOptions(TaskCreationOptions creationOptions)
        {
            if ((creationOptions & ~(TaskCreationOptions.AttachedToParent | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler | TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness | TaskCreationOptions.RunContinuationsAsynchronously)) != 0)
            {
                throw new ArgumentOutOfRangeException("creationOptions");
            }

            Contract.EndContractBlock();
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return m_defaultCancellationToken;
            }
        }

        public TaskScheduler Scheduler
        {
            get
            {
                return m_defaultScheduler;
            }
        }

        public TaskCreationOptions CreationOptions
        {
            get
            {
                return m_defaultCreationOptions;
            }
        }

        public TaskContinuationOptions ContinuationOptions
        {
            get
            {
                return m_defaultContinuationOptions;
            }
        }

        public Task StartNew(Action action)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, null, m_defaultCancellationToken, GetDefaultScheduler(currTask), m_defaultCreationOptions, InternalTaskOptions.None, ref stackMark);
        }

        public Task StartNew(Action action, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, null, cancellationToken, GetDefaultScheduler(currTask), m_defaultCreationOptions, InternalTaskOptions.None, ref stackMark);
        }

        public Task StartNew(Action action, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, null, m_defaultCancellationToken, GetDefaultScheduler(currTask), creationOptions, InternalTaskOptions.None, ref stackMark);
        }

        public Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task.InternalStartNew(Task.InternalCurrentIfAttached(creationOptions), action, null, cancellationToken, scheduler, creationOptions, InternalTaskOptions.None, ref stackMark);
        }

        internal Task StartNew(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions, InternalTaskOptions internalOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task.InternalStartNew(Task.InternalCurrentIfAttached(creationOptions), action, null, cancellationToken, scheduler, creationOptions, internalOptions, ref stackMark);
        }

        public Task StartNew(Action<Object> action, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, state, m_defaultCancellationToken, GetDefaultScheduler(currTask), m_defaultCreationOptions, InternalTaskOptions.None, ref stackMark);
        }

        public Task StartNew(Action<Object> action, Object state, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, state, cancellationToken, GetDefaultScheduler(currTask), m_defaultCreationOptions, InternalTaskOptions.None, ref stackMark);
        }

        public Task StartNew(Action<Object> action, Object state, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task.InternalStartNew(currTask, action, state, m_defaultCancellationToken, GetDefaultScheduler(currTask), creationOptions, InternalTaskOptions.None, ref stackMark);
        }

        public Task StartNew(Action<Object> action, Object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task.InternalStartNew(Task.InternalCurrentIfAttached(creationOptions), action, state, cancellationToken, scheduler, creationOptions, InternalTaskOptions.None, ref stackMark);
        }

        public Task<TResult> StartNew<TResult>(Func<TResult> function)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, m_defaultCancellationToken, m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, cancellationToken, m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew<TResult>(Func<TResult> function, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, m_defaultCancellationToken, creationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew<TResult>(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task<TResult>.StartNew(Task.InternalCurrentIfAttached(creationOptions), function, cancellationToken, creationOptions, InternalTaskOptions.None, scheduler, ref stackMark);
        }

        public Task<TResult> StartNew<TResult>(Func<Object, TResult> function, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, m_defaultCancellationToken, m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew<TResult>(Func<Object, TResult> function, Object state, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, cancellationToken, m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew<TResult>(Func<Object, TResult> function, Object state, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, m_defaultCancellationToken, creationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew<TResult>(Func<Object, TResult> function, Object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task<TResult>.StartNew(Task.InternalCurrentIfAttached(creationOptions), function, state, cancellationToken, creationOptions, InternalTaskOptions.None, scheduler, ref stackMark);
        }

        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsync(asyncResult, endMethod, m_defaultCreationOptions, DefaultScheduler, ref stackMark);
        }

        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsync(asyncResult, endMethod, creationOptions, DefaultScheduler, ref stackMark);
        }

        public Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsync(asyncResult, endMethod, creationOptions, scheduler, ref stackMark);
        }

        private Task FromAsync(IAsyncResult asyncResult, Action<IAsyncResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl(asyncResult, null, endMethod, creationOptions, scheduler, ref stackMark);
        }

        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state)
        {
            return FromAsync(beginMethod, endMethod, state, m_defaultCreationOptions);
        }

        public Task FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, object state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl(beginMethod, null, endMethod, state, creationOptions);
        }

        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state)
        {
            return FromAsync(beginMethod, endMethod, arg1, state, m_defaultCreationOptions);
        }

        public Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl(beginMethod, null, endMethod, arg1, state, creationOptions);
        }

        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state)
        {
            return FromAsync(beginMethod, endMethod, arg1, arg2, state, m_defaultCreationOptions);
        }

        public Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl(beginMethod, null, endMethod, arg1, arg2, state, creationOptions);
        }

        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state)
        {
            return FromAsync(beginMethod, endMethod, arg1, arg2, arg3, state, m_defaultCreationOptions);
        }

        public Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Action<IAsyncResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<VoidTaskResult>.FromAsyncImpl<TArg1, TArg2, TArg3>(beginMethod, null, endMethod, arg1, arg2, arg3, state, creationOptions);
        }

        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.FromAsyncImpl(asyncResult, endMethod, null, m_defaultCreationOptions, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.FromAsyncImpl(asyncResult, endMethod, null, creationOptions, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> FromAsync<TResult>(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.FromAsyncImpl(asyncResult, endMethod, null, creationOptions, scheduler, ref stackMark);
        }

        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, state, m_defaultCreationOptions);
        }

        public Task<TResult> FromAsync<TResult>(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, state, creationOptions);
        }

        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, state, m_defaultCreationOptions);
        }

        public Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, state, creationOptions);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, state, m_defaultCreationOptions);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, state, creationOptions);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, arg3, state, m_defaultCreationOptions);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
        {
            return TaskFactory<TResult>.FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, arg3, state, creationOptions);
        }

        internal static void CheckFromAsyncOptions(TaskCreationOptions creationOptions, bool hasBeginMethod)
        {
            if (hasBeginMethod)
            {
                if ((creationOptions & TaskCreationOptions.LongRunning) != 0)
                    throw new ArgumentOutOfRangeException("creationOptions", Environment.GetResourceString("Task_FromAsync_LongRunning"));
                if ((creationOptions & TaskCreationOptions.PreferFairness) != 0)
                    throw new ArgumentOutOfRangeException("creationOptions", Environment.GetResourceString("Task_FromAsync_PreferFairness"));
            }

            if ((creationOptions & ~(TaskCreationOptions.AttachedToParent | TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler | TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning)) != 0)
            {
                throw new ArgumentOutOfRangeException("creationOptions");
            }
        }

        private sealed class CompleteOnCountdownPromise : Task<Task[]>, ITaskCompletionAction
        {
            private readonly Task[] _tasks;
            private int _count;
            internal CompleteOnCountdownPromise(Task[] tasksCopy): base ()
            {
                Contract.Requires((tasksCopy != null) && (tasksCopy.Length > 0), "Expected non-null task array with at least one element in it");
                _tasks = tasksCopy;
                _count = tasksCopy.Length;
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "TaskFactory.ContinueWhenAll", 0);
                if (Task.s_asyncDebuggingEnabled)
                {
                    AddToActiveTasks(this);
                }
            }

            public void Invoke(Task completingTask)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, this.Id, CausalityRelation.Join);
                if (completingTask.IsWaitNotificationEnabled)
                    this.SetNotificationForWaitCompletion(enabled: true);
                if (Interlocked.Decrement(ref _count) == 0)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
                    if (Task.s_asyncDebuggingEnabled)
                    {
                        RemoveFromActiveTasks(this.Id);
                    }

                    TrySetResult(_tasks);
                }

                Contract.Assert(_count >= 0, "Count should never go below 0");
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
                    return base.ShouldNotifyDebuggerOfWaitCompletion && Task.AnyTaskRequiresNotifyDebuggerOfWaitCompletion(_tasks);
                }
            }
        }

        internal static Task<Task[]> CommonCWAllLogic(Task[] tasksCopy)
        {
            Contract.Requires(tasksCopy != null);
            CompleteOnCountdownPromise promise = new CompleteOnCountdownPromise(tasksCopy);
            for (int i = 0; i < tasksCopy.Length; i++)
            {
                if (tasksCopy[i].IsCompleted)
                    promise.Invoke(tasksCopy[i]);
                else
                    tasksCopy[i].AddCompletionAction(promise);
            }

            return promise;
        }

        private sealed class CompleteOnCountdownPromise<T> : Task<Task<T>[]>, ITaskCompletionAction
        {
            private readonly Task<T>[] _tasks;
            private int _count;
            internal CompleteOnCountdownPromise(Task<T>[] tasksCopy): base ()
            {
                Contract.Requires((tasksCopy != null) && (tasksCopy.Length > 0), "Expected non-null task array with at least one element in it");
                _tasks = tasksCopy;
                _count = tasksCopy.Length;
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "TaskFactory.ContinueWhenAll<>", 0);
                if (Task.s_asyncDebuggingEnabled)
                {
                    AddToActiveTasks(this);
                }
            }

            public void Invoke(Task completingTask)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, this.Id, CausalityRelation.Join);
                if (completingTask.IsWaitNotificationEnabled)
                    this.SetNotificationForWaitCompletion(enabled: true);
                if (Interlocked.Decrement(ref _count) == 0)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
                    if (Task.s_asyncDebuggingEnabled)
                    {
                        RemoveFromActiveTasks(this.Id);
                    }

                    TrySetResult(_tasks);
                }

                Contract.Assert(_count >= 0, "Count should never go below 0");
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
                    return base.ShouldNotifyDebuggerOfWaitCompletion && Task.AnyTaskRequiresNotifyDebuggerOfWaitCompletion(_tasks);
                }
            }
        }

        internal static Task<Task<T>[]> CommonCWAllLogic<T>(Task<T>[] tasksCopy)
        {
            Contract.Requires(tasksCopy != null);
            CompleteOnCountdownPromise<T> promise = new CompleteOnCountdownPromise<T>(tasksCopy);
            for (int i = 0; i < tasksCopy.Length; i++)
            {
                if (tasksCopy[i].IsCompleted)
                    promise.Invoke(tasksCopy[i]);
                else
                    tasksCopy[i].AddCompletionAction(promise);
            }

            return promise;
        }

        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl(tasks, null, continuationAction, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl(tasks, null, continuationAction, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl(tasks, null, continuationAction, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAll(Task[] tasks, Action<Task[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl(tasks, null, continuationAction, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, null, continuationAction, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, null, continuationAction, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, null, continuationAction, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>[]> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, null, continuationAction, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAllImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAllImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAllImpl(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TResult>(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAllImpl(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        internal sealed class CompleteOnInvokePromise : Task<Task>, ITaskCompletionAction
        {
            private IList<Task> _tasks;
            private int m_firstTaskAlreadyCompleted;
            public CompleteOnInvokePromise(IList<Task> tasks): base ()
            {
                Contract.Requires(tasks != null, "Expected non-null collection of tasks");
                _tasks = tasks;
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Id, "TaskFactory.ContinueWhenAny", 0);
                if (Task.s_asyncDebuggingEnabled)
                {
                    AddToActiveTasks(this);
                }
            }

            public void Invoke(Task completingTask)
            {
                if (Interlocked.CompareExchange(ref m_firstTaskAlreadyCompleted, 1, 0) == 0)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                    {
                        AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel.Important, this.Id, CausalityRelation.Choice);
                        AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Id, AsyncCausalityStatus.Completed);
                    }

                    if (Task.s_asyncDebuggingEnabled)
                    {
                        RemoveFromActiveTasks(this.Id);
                    }

                    bool success = TrySetResult(completingTask);
                    Contract.Assert(success, "Only one task should have gotten to this point, and thus this must be successful.");
                    var tasks = _tasks;
                    int numTasks = tasks.Count;
                    for (int i = 0; i < numTasks; i++)
                    {
                        var task = tasks[i];
                        if (task != null && !task.IsCompleted)
                            task.RemoveContinuation(this);
                    }

                    _tasks = null;
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

        internal static Task<Task> CommonCWAnyLogic(IList<Task> tasks)
        {
            Contract.Requires(tasks != null);
            var promise = new CompleteOnInvokePromise(tasks);
            bool checkArgsOnly = false;
            int numTasks = tasks.Count;
            for (int i = 0; i < numTasks; i++)
            {
                var task = tasks[i];
                if (task == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
                if (checkArgsOnly)
                    continue;
                if (promise.IsCompleted)
                {
                    checkArgsOnly = true;
                }
                else if (task.IsCompleted)
                {
                    promise.Invoke(task);
                    checkArgsOnly = true;
                }
                else
                    task.AddCompletionAction(promise);
            }

            return promise;
        }

        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl(tasks, null, continuationAction, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl(tasks, null, continuationAction, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl(tasks, null, continuationAction, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAny(Task[] tasks, Action<Task> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl(tasks, null, continuationAction, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAnyImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAnyImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAnyImpl(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TResult>(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAnyImpl(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            return TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<TResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, null, continuationAction, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, null, continuationAction, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction, TaskContinuationOptions continuationOptions)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, null, continuationAction, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Action<Task<TAntecedentResult>> continuationAction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationAction == null)
                throw new ArgumentNullException("continuationAction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return TaskFactory<VoidTaskResult>.ContinueWhenAnyImpl<TAntecedentResult>(tasks, null, continuationAction, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        internal static Task[] CheckMultiContinuationTasksAndCopy(Task[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
            Contract.EndContractBlock();
            Task[] tasksCopy = new Task[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasksCopy[i] = tasks[i];
                if (tasksCopy[i] == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
            }

            return tasksCopy;
        }

        internal static Task<TResult>[] CheckMultiContinuationTasksAndCopy<TResult>(Task<TResult>[] tasks)
        {
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
            Contract.EndContractBlock();
            Task<TResult>[] tasksCopy = new Task<TResult>[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasksCopy[i] = tasks[i];
                if (tasksCopy[i] == null)
                    throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_NullTask"), "tasks");
            }

            return tasksCopy;
        }

        internal static void CheckMultiTaskContinuationOptions(TaskContinuationOptions continuationOptions)
        {
            const TaskContinuationOptions NotOnAny = TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnRanToCompletion;
            const TaskContinuationOptions illegalMask = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.LongRunning;
            if ((continuationOptions & illegalMask) == illegalMask)
            {
                throw new ArgumentOutOfRangeException("continuationOptions", Environment.GetResourceString("Task_ContinueWith_ESandLR"));
            }

            if ((continuationOptions & ~(TaskContinuationOptions.LongRunning | TaskContinuationOptions.PreferFairness | TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.HideScheduler | TaskContinuationOptions.LazyCancellation | NotOnAny | TaskContinuationOptions.ExecuteSynchronously)) != 0)
            {
                throw new ArgumentOutOfRangeException("continuationOptions");
            }

            if ((continuationOptions & NotOnAny) != 0)
                throw new ArgumentOutOfRangeException("continuationOptions", Environment.GetResourceString("Task_MultiTaskContinuation_FireOptions"));
            Contract.EndContractBlock();
        }
    }
}