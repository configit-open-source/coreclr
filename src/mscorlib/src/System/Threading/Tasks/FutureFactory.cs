using System;
using System.Security;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Runtime.Versioning;

namespace System.Threading.Tasks
{
    public class TaskFactory<TResult>
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
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            TaskFactory.CheckCreationOptions(creationOptions);
            m_defaultCancellationToken = cancellationToken;
            m_defaultScheduler = scheduler;
            m_defaultCreationOptions = creationOptions;
            m_defaultContinuationOptions = continuationOptions;
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

        public Task<TResult> StartNew(Func<TResult> function)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, m_defaultCancellationToken, m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, cancellationToken, m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew(Func<TResult> function, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, m_defaultCancellationToken, creationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew(Func<TResult> function, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task<TResult>.StartNew(Task.InternalCurrentIfAttached(creationOptions), function, cancellationToken, creationOptions, InternalTaskOptions.None, scheduler, ref stackMark);
        }

        public Task<TResult> StartNew(Func<Object, TResult> function, Object state)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, m_defaultCancellationToken, m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew(Func<Object, TResult> function, Object state, CancellationToken cancellationToken)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, cancellationToken, m_defaultCreationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew(Func<Object, TResult> function, Object state, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Task currTask = Task.InternalCurrent;
            return Task<TResult>.StartNew(currTask, function, state, m_defaultCancellationToken, creationOptions, InternalTaskOptions.None, GetDefaultScheduler(currTask), ref stackMark);
        }

        public Task<TResult> StartNew(Func<Object, TResult> function, Object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return Task<TResult>.StartNew(Task.InternalCurrentIfAttached(creationOptions), function, state, cancellationToken, creationOptions, InternalTaskOptions.None, scheduler, ref stackMark);
        }

        private static void FromAsyncCoreLogic(IAsyncResult iar, Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction, Task<TResult> promise, bool requiresSynchronization)
        {
            Contract.Requires((endFunction != null) != (endAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            Exception ex = null;
            OperationCanceledException oce = null;
            TResult result = default (TResult);
            try
            {
                if (endFunction != null)
                {
                    result = endFunction(iar);
                }
                else
                {
                    endAction(iar);
                }
            }
            catch (OperationCanceledException _oce)
            {
                oce = _oce;
            }
            catch (Exception e)
            {
                ex = e;
            }
            finally
            {
                if (oce != null)
                {
                    promise.TrySetCanceled(oce.CancellationToken, oce);
                }
                else if (ex != null)
                {
                    bool bWonSetException = promise.TrySetException(ex);
                    if (bWonSetException && ex is ThreadAbortException)
                    {
                        promise.m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
                    }
                }
                else
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Completed);
                    if (Task.s_asyncDebuggingEnabled)
                    {
                        Task.RemoveFromActiveTasks(promise.Id);
                    }

                    if (requiresSynchronization)
                    {
                        promise.TrySetResult(result);
                    }
                    else
                    {
                        promise.DangerousSetResult(result);
                    }
                }
            }
        }

        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsyncImpl(asyncResult, endMethod, null, m_defaultCreationOptions, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsyncImpl(asyncResult, endMethod, null, creationOptions, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> FromAsync(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endMethod, TaskCreationOptions creationOptions, TaskScheduler scheduler)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return FromAsyncImpl(asyncResult, endMethod, null, creationOptions, scheduler, ref stackMark);
        }

        internal static Task<TResult> FromAsyncImpl(IAsyncResult asyncResult, Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction, TaskCreationOptions creationOptions, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endMethod");
            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();
            TaskFactory.CheckFromAsyncOptions(creationOptions, false);
            Task<TResult> promise = new Task<TResult>((object)null, creationOptions);
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync", 0);
            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            Task t = new Task(delegate
            {
                FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: true);
            }

            , (object)null, null, default (CancellationToken), TaskCreationOptions.None, InternalTaskOptions.None, null, ref stackMark);
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Verbose, t.Id, "TaskFactory.FromAsync Callback", 0);
            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(t);
            }

            if (asyncResult.IsCompleted)
            {
                try
                {
                    t.InternalRunSynchronously(scheduler, waitForCompletion: false);
                }
                catch (Exception e)
                {
                    promise.TrySetException(e);
                }
            }
            else
            {
                ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, delegate
                {
                    try
                    {
                        t.InternalRunSynchronously(scheduler, waitForCompletion: false);
                    }
                    catch (Exception e)
                    {
                        promise.TrySetException(e);
                    }
                }

                , null, Timeout.Infinite, true);
            }

            return promise;
        }

        public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, state, m_defaultCreationOptions);
        }

        public Task<TResult> FromAsync(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, object state, TaskCreationOptions creationOptions)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, state, creationOptions);
        }

        internal static Task<TResult> FromAsyncImpl(Func<AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction, object state, TaskCreationOptions creationOptions)
        {
            if (beginMethod == null)
                throw new ArgumentNullException("beginMethod");
            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endMethod");
            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");
            TaskFactory.CheckFromAsyncOptions(creationOptions, true);
            Task<TResult> promise = new Task<TResult>(state, creationOptions);
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync: " + beginMethod.Method.Name, 0);
            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            try
            {
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    var asyncResult = beginMethod(iar =>
                    {
                        if (!iar.CompletedSynchronously)
                            FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }

                    , state);
                    if (asyncResult.CompletedSynchronously)
                    {
                        Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                        FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: false);
                    }
                }
                else
                {
                    var asyncResult = beginMethod(iar =>
                    {
                        FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }

                    , state);
                }
            }
            catch
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Error);
                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(promise.Id);
                }

                promise.TrySetResult(default (TResult));
                throw;
            }

            return promise;
        }

        public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, state, m_defaultCreationOptions);
        }

        public Task<TResult> FromAsync<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, object state, TaskCreationOptions creationOptions)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, state, creationOptions);
        }

        internal static Task<TResult> FromAsyncImpl<TArg1>(Func<TArg1, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction, TArg1 arg1, object state, TaskCreationOptions creationOptions)
        {
            if (beginMethod == null)
                throw new ArgumentNullException("beginMethod");
            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endFunction");
            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");
            TaskFactory.CheckFromAsyncOptions(creationOptions, true);
            Task<TResult> promise = new Task<TResult>(state, creationOptions);
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync: " + beginMethod.Method.Name, 0);
            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            try
            {
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    var asyncResult = beginMethod(arg1, iar =>
                    {
                        if (!iar.CompletedSynchronously)
                            FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }

                    , state);
                    if (asyncResult.CompletedSynchronously)
                    {
                        Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                        FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: false);
                    }
                }
                else
                {
                    var asyncResult = beginMethod(arg1, iar =>
                    {
                        FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }

                    , state);
                }
            }
            catch
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Error);
                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(promise.Id);
                }

                promise.TrySetResult(default (TResult));
                throw;
            }

            return promise;
        }

        public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, state, m_defaultCreationOptions);
        }

        public Task<TResult> FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, state, creationOptions);
        }

        internal static Task<TResult> FromAsyncImpl<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction, TArg1 arg1, TArg2 arg2, object state, TaskCreationOptions creationOptions)
        {
            if (beginMethod == null)
                throw new ArgumentNullException("beginMethod");
            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endMethod");
            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");
            TaskFactory.CheckFromAsyncOptions(creationOptions, true);
            Task<TResult> promise = new Task<TResult>(state, creationOptions);
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync: " + beginMethod.Method.Name, 0);
            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            try
            {
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    var asyncResult = beginMethod(arg1, arg2, iar =>
                    {
                        if (!iar.CompletedSynchronously)
                            FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }

                    , state);
                    if (asyncResult.CompletedSynchronously)
                    {
                        Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                        FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: false);
                    }
                }
                else
                {
                    var asyncResult = beginMethod(arg1, arg2, iar =>
                    {
                        FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }

                    , state);
                }
            }
            catch
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Error);
                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(promise.Id);
                }

                promise.TrySetResult(default (TResult));
                throw;
            }

            return promise;
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, arg3, state, m_defaultCreationOptions);
        }

        public Task<TResult> FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endMethod, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
        {
            return FromAsyncImpl(beginMethod, endMethod, null, arg1, arg2, arg3, state, creationOptions);
        }

        internal static Task<TResult> FromAsyncImpl<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, object, IAsyncResult> beginMethod, Func<IAsyncResult, TResult> endFunction, Action<IAsyncResult> endAction, TArg1 arg1, TArg2 arg2, TArg3 arg3, object state, TaskCreationOptions creationOptions)
        {
            if (beginMethod == null)
                throw new ArgumentNullException("beginMethod");
            if (endFunction == null && endAction == null)
                throw new ArgumentNullException("endMethod");
            Contract.Requires((endFunction != null) != (endAction != null), "Both endFunction and endAction were non-null");
            TaskFactory.CheckFromAsyncOptions(creationOptions, true);
            Task<TResult> promise = new Task<TResult>(state, creationOptions);
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, promise.Id, "TaskFactory.FromAsync: " + beginMethod.Method.Name, 0);
            if (Task.s_asyncDebuggingEnabled)
            {
                Task.AddToActiveTasks(promise);
            }

            try
            {
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    var asyncResult = beginMethod(arg1, arg2, arg3, iar =>
                    {
                        if (!iar.CompletedSynchronously)
                            FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }

                    , state);
                    if (asyncResult.CompletedSynchronously)
                    {
                        Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                        FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: false);
                    }
                }
                else
                {
                    var asyncResult = beginMethod(arg1, arg2, arg3, iar =>
                    {
                        FromAsyncCoreLogic(iar, endFunction, endAction, promise, requiresSynchronization: true);
                    }

                    , state);
                }
            }
            catch
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, promise.Id, AsyncCausalityStatus.Error);
                if (Task.s_asyncDebuggingEnabled)
                {
                    Task.RemoveFromActiveTasks(promise.Id);
                }

                promise.TrySetResult(default (TResult));
                throw;
            }

            return promise;
        }

        internal static Task<TResult> FromAsyncTrim<TInstance, TArgs>(TInstance thisRef, TArgs args, Func<TInstance, TArgs, AsyncCallback, object, IAsyncResult> beginMethod, Func<TInstance, IAsyncResult, TResult> endMethod)where TInstance : class
        {
            Contract.Assert(thisRef != null, "Expected a non-null thisRef");
            Contract.Assert(beginMethod != null, "Expected a non-null beginMethod");
            Contract.Assert(endMethod != null, "Expected a non-null endMethod");
            var promise = new FromAsyncTrimPromise<TInstance>(thisRef, endMethod);
            var asyncResult = beginMethod(thisRef, args, FromAsyncTrimPromise<TInstance>.s_completeFromAsyncResult, promise);
            if (asyncResult.CompletedSynchronously)
            {
                Contract.Assert(asyncResult.IsCompleted, "If the operation completed synchronously, it must be completed.");
                promise.Complete(thisRef, endMethod, asyncResult, requiresSynchronization: false);
            }

            return promise;
        }

        private sealed class FromAsyncTrimPromise<TInstance> : Task<TResult> where TInstance : class
        {
            internal readonly static AsyncCallback s_completeFromAsyncResult = CompleteFromAsyncResult;
            private TInstance m_thisRef;
            private Func<TInstance, IAsyncResult, TResult> m_endMethod;
            internal FromAsyncTrimPromise(TInstance thisRef, Func<TInstance, IAsyncResult, TResult> endMethod): base ()
            {
                Contract.Requires(thisRef != null, "Expected a non-null thisRef");
                Contract.Requires(endMethod != null, "Expected a non-null endMethod");
                m_thisRef = thisRef;
                m_endMethod = endMethod;
            }

            internal static void CompleteFromAsyncResult(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                Contract.EndContractBlock();
                var promise = asyncResult.AsyncState as FromAsyncTrimPromise<TInstance>;
                if (promise == null)
                    throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndCalledMultiple"), "asyncResult");
                var thisRef = promise.m_thisRef;
                var endMethod = promise.m_endMethod;
                promise.m_thisRef = default (TInstance);
                promise.m_endMethod = null;
                if (endMethod == null)
                    throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndCalledMultiple"), "asyncResult");
                if (!asyncResult.CompletedSynchronously)
                {
                    promise.Complete(thisRef, endMethod, asyncResult, requiresSynchronization: true);
                }
            }

            internal void Complete(TInstance thisRef, Func<TInstance, IAsyncResult, TResult> endMethod, IAsyncResult asyncResult, bool requiresSynchronization)
            {
                Contract.Assert(!IsCompleted, "The task should not have been completed yet.");
                bool successfullySet = false;
                try
                {
                    var result = endMethod(thisRef, asyncResult);
                    if (requiresSynchronization)
                    {
                        successfullySet = TrySetResult(result);
                    }
                    else
                    {
                        DangerousSetResult(result);
                        successfullySet = true;
                    }
                }
                catch (OperationCanceledException oce)
                {
                    successfullySet = TrySetCanceled(oce.CancellationToken, oce);
                }
                catch (Exception exc)
                {
                    successfullySet = TrySetException(exc);
                }

                Contract.Assert(successfullySet, "Expected the task to not yet be completed");
            }
        }

        private static Task<TResult> CreateCanceledTask(TaskContinuationOptions continuationOptions, CancellationToken ct)
        {
            TaskCreationOptions tco;
            InternalTaskOptions dontcare;
            Task.CreationOptionsFromContinuationOptions(continuationOptions, out tco, out dontcare);
            return new Task<TResult>(true, default (TResult), tco, ct);
        }

        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll(Task[] tasks, Func<Task[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAll<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAllImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        internal static Task<TResult> ContinueWhenAllImpl<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>[], TResult> continuationFunction, Action<Task<TAntecedentResult>[]> continuationAction, TaskContinuationOptions continuationOptions, CancellationToken cancellationToken, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            Contract.Requires((continuationFunction != null) != (continuationAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();
            Task<TAntecedentResult>[] tasksCopy = TaskFactory.CheckMultiContinuationTasksAndCopy<TAntecedentResult>(tasks);
            if (cancellationToken.IsCancellationRequested && ((continuationOptions & TaskContinuationOptions.LazyCancellation) == 0))
            {
                return CreateCanceledTask(continuationOptions, cancellationToken);
            }

            var starter = TaskFactory.CommonCWAllLogic(tasksCopy);
            if (continuationFunction != null)
            {
                return starter.ContinueWith<TResult>(GenericDelegateCache<TAntecedentResult, TResult>.CWAllFuncDelegate, continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
            else
            {
                Contract.Assert(continuationAction != null);
                return starter.ContinueWith<TResult>(GenericDelegateCache<TAntecedentResult, TResult>.CWAllActionDelegate, continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
        }

        internal static Task<TResult> ContinueWhenAllImpl(Task[] tasks, Func<Task[], TResult> continuationFunction, Action<Task[]> continuationAction, TaskContinuationOptions continuationOptions, CancellationToken cancellationToken, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            Contract.Requires((continuationFunction != null) != (continuationAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();
            Task[] tasksCopy = TaskFactory.CheckMultiContinuationTasksAndCopy(tasks);
            if (cancellationToken.IsCancellationRequested && ((continuationOptions & TaskContinuationOptions.LazyCancellation) == 0))
            {
                return CreateCanceledTask(continuationOptions, cancellationToken);
            }

            var starter = TaskFactory.CommonCWAllLogic(tasksCopy);
            if (continuationFunction != null)
            {
                return starter.ContinueWith((completedTasks, state) =>
                {
                    completedTasks.NotifyDebuggerOfWaitCompletionIfNecessary();
                    return ((Func<Task[], TResult>)state)(completedTasks.Result);
                }

                , continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
            else
            {
                Contract.Assert(continuationAction != null);
                return starter.ContinueWith<TResult>((completedTasks, state) =>
                {
                    completedTasks.NotifyDebuggerOfWaitCompletionIfNecessary();
                    ((Action<Task[]>)state)(completedTasks.Result);
                    return default (TResult);
                }

                , continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
        }

        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny(Task[] tasks, Func<Task, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, m_defaultContinuationOptions, cancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, TaskContinuationOptions continuationOptions)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, m_defaultCancellationToken, DefaultScheduler, ref stackMark);
        }

        public Task<TResult> ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, CancellationToken cancellationToken, TaskContinuationOptions continuationOptions, TaskScheduler scheduler)
        {
            if (continuationFunction == null)
                throw new ArgumentNullException("continuationFunction");
            Contract.EndContractBlock();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return ContinueWhenAnyImpl<TAntecedentResult>(tasks, continuationFunction, null, continuationOptions, cancellationToken, scheduler, ref stackMark);
        }

        internal static Task<TResult> ContinueWhenAnyImpl(Task[] tasks, Func<Task, TResult> continuationFunction, Action<Task> continuationAction, TaskContinuationOptions continuationOptions, CancellationToken cancellationToken, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
            Contract.Requires((continuationFunction != null) != (continuationAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();
            Task<Task> starter = TaskFactory.CommonCWAnyLogic(tasks);
            if (cancellationToken.IsCancellationRequested && ((continuationOptions & TaskContinuationOptions.LazyCancellation) == 0))
            {
                return CreateCanceledTask(continuationOptions, cancellationToken);
            }

            if (continuationFunction != null)
            {
                return starter.ContinueWith((completedTask, state) =>
                {
                    return ((Func<Task, TResult>)state)(completedTask.Result);
                }

                , continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
            else
            {
                Contract.Assert(continuationAction != null);
                return starter.ContinueWith<TResult>((completedTask, state) =>
                {
                    ((Action<Task>)state)(completedTask.Result);
                    return default (TResult);
                }

                , continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
        }

        internal static Task<TResult> ContinueWhenAnyImpl<TAntecedentResult>(Task<TAntecedentResult>[] tasks, Func<Task<TAntecedentResult>, TResult> continuationFunction, Action<Task<TAntecedentResult>> continuationAction, TaskContinuationOptions continuationOptions, CancellationToken cancellationToken, TaskScheduler scheduler, ref StackCrawlMark stackMark)
        {
            TaskFactory.CheckMultiTaskContinuationOptions(continuationOptions);
            if (tasks == null)
                throw new ArgumentNullException("tasks");
            if (tasks.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Task_MultiTaskContinuation_EmptyTaskList"), "tasks");
            Contract.Requires((continuationFunction != null) != (continuationAction != null), "Expected exactly one of endFunction/endAction to be non-null");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            Contract.EndContractBlock();
            var starter = TaskFactory.CommonCWAnyLogic(tasks);
            if (cancellationToken.IsCancellationRequested && ((continuationOptions & TaskContinuationOptions.LazyCancellation) == 0))
            {
                return CreateCanceledTask(continuationOptions, cancellationToken);
            }

            if (continuationFunction != null)
            {
                return starter.ContinueWith<TResult>(GenericDelegateCache<TAntecedentResult, TResult>.CWAnyFuncDelegate, continuationFunction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
            else
            {
                Contract.Assert(continuationAction != null);
                return starter.ContinueWith<TResult>(GenericDelegateCache<TAntecedentResult, TResult>.CWAnyActionDelegate, continuationAction, scheduler, cancellationToken, continuationOptions, ref stackMark);
            }
        }
    }

    internal static class GenericDelegateCache<TAntecedentResult, TResult>
    {
        internal static Func<Task<Task>, object, TResult> CWAnyFuncDelegate = (Task<Task> wrappedWinner, object state) =>
        {
            var func = (Func<Task<TAntecedentResult>, TResult>)state;
            var arg = (Task<TAntecedentResult>)wrappedWinner.Result;
            return func(arg);
        }

        ;
        internal static Func<Task<Task>, object, TResult> CWAnyActionDelegate = (Task<Task> wrappedWinner, object state) =>
        {
            var action = (Action<Task<TAntecedentResult>>)state;
            var arg = (Task<TAntecedentResult>)wrappedWinner.Result;
            action(arg);
            return default (TResult);
        }

        ;
        internal static Func<Task<Task<TAntecedentResult>[]>, object, TResult> CWAllFuncDelegate = (Task<Task<TAntecedentResult>[]> wrappedAntecedents, object state) =>
        {
            wrappedAntecedents.NotifyDebuggerOfWaitCompletionIfNecessary();
            var func = (Func<Task<TAntecedentResult>[], TResult>)state;
            return func(wrappedAntecedents.Result);
        }

        ;
        internal static Func<Task<Task<TAntecedentResult>[]>, object, TResult> CWAllActionDelegate = (Task<Task<TAntecedentResult>[]> wrappedAntecedents, object state) =>
        {
            wrappedAntecedents.NotifyDebuggerOfWaitCompletionIfNecessary();
            var action = (Action<Task<TAntecedentResult>[]>)state;
            action(wrappedAntecedents.Result);
            return default (TResult);
        }

        ;
    }
}