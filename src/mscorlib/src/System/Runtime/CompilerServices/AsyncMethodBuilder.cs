using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public struct AsyncVoidMethodBuilder
    {
        private SynchronizationContext m_synchronizationContext;
        private AsyncMethodBuilderCore m_coreState;
        private Task m_task;
        public static AsyncVoidMethodBuilder Create()
        {
            SynchronizationContext sc = SynchronizationContext.CurrentNoFlow;
            if (sc != null)
                sc.OperationStarted();
            return new AsyncVoidMethodBuilder()
            {m_synchronizationContext = sc};
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)where TStateMachine : IAsyncStateMachine
        {
            if (stateMachine == null)
                throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();
            ExecutionContextSwitcher ecs = default (ExecutionContextSwitcher);
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                ExecutionContext.EstablishCopyOnWriteScope(ref ecs);
                stateMachine.MoveNext();
            }
            finally
            {
                ecs.Undo();
            }
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            m_coreState.SetStateMachine(stateMachine);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                AsyncMethodBuilderCore.MoveNextRunner runnerToInitialize = null;
                var continuation = m_coreState.GetCompletionAction(AsyncCausalityTracer.LoggingOn ? this.Task : null, ref runnerToInitialize);
                Contract.Assert(continuation != null, "GetCompletionAction should always return a valid action.");
                if (m_coreState.m_stateMachine == null)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Task.Id, "Async: " + stateMachine.GetType().Name, 0);
                    m_coreState.PostBoxInitialization(stateMachine, runnerToInitialize, null);
                }

                awaiter.OnCompleted(continuation);
            }
            catch (Exception exc)
            {
                AsyncMethodBuilderCore.ThrowAsync(exc, targetContext: null);
            }
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                AsyncMethodBuilderCore.MoveNextRunner runnerToInitialize = null;
                var continuation = m_coreState.GetCompletionAction(AsyncCausalityTracer.LoggingOn ? this.Task : null, ref runnerToInitialize);
                Contract.Assert(continuation != null, "GetCompletionAction should always return a valid action.");
                if (m_coreState.m_stateMachine == null)
                {
                    if (AsyncCausalityTracer.LoggingOn)
                        AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, this.Task.Id, "Async: " + stateMachine.GetType().Name, 0);
                    m_coreState.PostBoxInitialization(stateMachine, runnerToInitialize, null);
                }

                awaiter.UnsafeOnCompleted(continuation);
            }
            catch (Exception e)
            {
                AsyncMethodBuilderCore.ThrowAsync(e, targetContext: null);
            }
        }

        public void SetResult()
        {
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Task.Id, AsyncCausalityStatus.Completed);
            if (m_synchronizationContext != null)
            {
                NotifySynchronizationContextOfCompletion();
            }
        }

        public void SetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            Contract.EndContractBlock();
            if (AsyncCausalityTracer.LoggingOn)
                AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, this.Task.Id, AsyncCausalityStatus.Error);
            if (m_synchronizationContext != null)
            {
                try
                {
                    AsyncMethodBuilderCore.ThrowAsync(exception, targetContext: m_synchronizationContext);
                }
                finally
                {
                    NotifySynchronizationContextOfCompletion();
                }
            }
            else
            {
                AsyncMethodBuilderCore.ThrowAsync(exception, targetContext: null);
            }
        }

        private void NotifySynchronizationContextOfCompletion()
        {
            Contract.Assert(m_synchronizationContext != null, "Must only be used with a non-null context.");
            try
            {
                m_synchronizationContext.OperationCompleted();
            }
            catch (Exception exc)
            {
                AsyncMethodBuilderCore.ThrowAsync(exc, targetContext: null);
            }
        }

        private Task Task
        {
            get
            {
                if (m_task == null)
                    m_task = new Task();
                return m_task;
            }
        }

        private object ObjectIdForDebugger
        {
            get
            {
                return this.Task;
            }
        }
    }

    public struct AsyncTaskMethodBuilder
    {
        private readonly static Task<VoidTaskResult> s_cachedCompleted = AsyncTaskMethodBuilder<VoidTaskResult>.s_defaultResultTask;
        private AsyncTaskMethodBuilder<VoidTaskResult> m_builder;
        public static AsyncTaskMethodBuilder Create()
        {
            return default (AsyncTaskMethodBuilder);
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)where TStateMachine : IAsyncStateMachine
        {
            if (stateMachine == null)
                throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();
            ExecutionContextSwitcher ecs = default (ExecutionContextSwitcher);
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                ExecutionContext.EstablishCopyOnWriteScope(ref ecs);
                stateMachine.MoveNext();
            }
            finally
            {
                ecs.Undo();
            }
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            m_builder.SetStateMachine(stateMachine);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            m_builder.AwaitOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            m_builder.AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref awaiter, ref stateMachine);
        }

        public Task Task
        {
            get
            {
                return m_builder.Task;
            }
        }

        public void SetResult()
        {
            m_builder.SetResult(s_cachedCompleted);
        }

        public void SetException(Exception exception)
        {
            m_builder.SetException(exception);
        }

        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            m_builder.SetNotificationForWaitCompletion(enabled);
        }

        private object ObjectIdForDebugger
        {
            get
            {
                return this.Task;
            }
        }
    }

    public struct AsyncTaskMethodBuilder<TResult>
    {
        internal readonly static Task<TResult> s_defaultResultTask = AsyncTaskCache.CreateCacheableTask(default (TResult));
        private AsyncMethodBuilderCore m_coreState;
        private Task<TResult> m_task;
        public static AsyncTaskMethodBuilder<TResult> Create()
        {
            return default (AsyncTaskMethodBuilder<TResult>);
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)where TStateMachine : IAsyncStateMachine
        {
            if (stateMachine == null)
                throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();
            ExecutionContextSwitcher ecs = default (ExecutionContextSwitcher);
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                ExecutionContext.EstablishCopyOnWriteScope(ref ecs);
                stateMachine.MoveNext();
            }
            finally
            {
                ecs.Undo();
            }
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            m_coreState.SetStateMachine(stateMachine);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                AsyncMethodBuilderCore.MoveNextRunner runnerToInitialize = null;
                var continuation = m_coreState.GetCompletionAction(AsyncCausalityTracer.LoggingOn ? this.Task : null, ref runnerToInitialize);
                Contract.Assert(continuation != null, "GetCompletionAction should always return a valid action.");
                if (m_coreState.m_stateMachine == null)
                {
                    var builtTask = this.Task;
                    m_coreState.PostBoxInitialization(stateMachine, runnerToInitialize, builtTask);
                }

                awaiter.OnCompleted(continuation);
            }
            catch (Exception e)
            {
                AsyncMethodBuilderCore.ThrowAsync(e, targetContext: null);
            }
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                AsyncMethodBuilderCore.MoveNextRunner runnerToInitialize = null;
                var continuation = m_coreState.GetCompletionAction(AsyncCausalityTracer.LoggingOn ? this.Task : null, ref runnerToInitialize);
                Contract.Assert(continuation != null, "GetCompletionAction should always return a valid action.");
                if (m_coreState.m_stateMachine == null)
                {
                    var builtTask = this.Task;
                    m_coreState.PostBoxInitialization(stateMachine, runnerToInitialize, builtTask);
                }

                awaiter.UnsafeOnCompleted(continuation);
            }
            catch (Exception e)
            {
                AsyncMethodBuilderCore.ThrowAsync(e, targetContext: null);
            }
        }

        public Task<TResult> Task
        {
            get
            {
                var task = m_task;
                if (task == null)
                {
                    m_task = task = new Task<TResult>();
                }

                return task;
            }
        }

        public void SetResult(TResult result)
        {
            var task = m_task;
            if (task == null)
            {
                m_task = GetTaskForResult(result);
                Contract.Assert(m_task != null, "GetTaskForResult should never return null");
            }
            else
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel.Required, task.Id, AsyncCausalityStatus.Completed);
                if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
                {
                    System.Threading.Tasks.Task.RemoveFromActiveTasks(task.Id);
                }

                if (!task.TrySetResult(result))
                {
                    throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
                }
            }
        }

        internal void SetResult(Task<TResult> completedTask)
        {
            Contract.Requires(completedTask != null, "Expected non-null task");
            Contract.Requires(completedTask.Status == TaskStatus.RanToCompletion, "Expected a successfully completed task");
            var task = m_task;
            if (task == null)
            {
                m_task = completedTask;
            }
            else
            {
                SetResult(default (TResult));
            }
        }

        public void SetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            Contract.EndContractBlock();
            var task = m_task;
            if (task == null)
            {
                task = this.Task;
            }

            var oce = exception as OperationCanceledException;
            bool successfullySet = oce != null ? task.TrySetCanceled(oce.CancellationToken, oce) : task.TrySetException(exception);
            if (!successfullySet)
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
            }
        }

        internal void SetNotificationForWaitCompletion(bool enabled)
        {
            this.Task.SetNotificationForWaitCompletion(enabled);
        }

        private object ObjectIdForDebugger
        {
            get
            {
                return this.Task;
            }
        }

        private Task<TResult> GetTaskForResult(TResult result)
        {
            Contract.Ensures(EqualityComparer<TResult>.Default.Equals(result, Contract.Result<Task<TResult>>().Result), "The returned task's Result must return the same value as the specified result value.");
            if (null != (object)default (TResult))
            {
                if (typeof (TResult) == typeof (Boolean))
                {
                    Boolean value = (Boolean)(object)result;
                    Task<Boolean> task = value ? AsyncTaskCache.TrueTask : AsyncTaskCache.FalseTask;
                    return JitHelpers.UnsafeCast<Task<TResult>>(task);
                }
                else if (typeof (TResult) == typeof (Int32))
                {
                    Int32 value = (Int32)(object)result;
                    if (value < AsyncTaskCache.EXCLUSIVE_INT32_MAX && value >= AsyncTaskCache.INCLUSIVE_INT32_MIN)
                    {
                        Task<Int32> task = AsyncTaskCache.Int32Tasks[value - AsyncTaskCache.INCLUSIVE_INT32_MIN];
                        return JitHelpers.UnsafeCast<Task<TResult>>(task);
                    }
                }
                else if ((typeof (TResult) == typeof (UInt32) && default (UInt32) == (UInt32)(object)result) || (typeof (TResult) == typeof (Byte) && default (Byte) == (Byte)(object)result) || (typeof (TResult) == typeof (SByte) && default (SByte) == (SByte)(object)result) || (typeof (TResult) == typeof (Char) && default (Char) == (Char)(object)result) || (typeof (TResult) == typeof (Decimal) && default (Decimal) == (Decimal)(object)result) || (typeof (TResult) == typeof (Int64) && default (Int64) == (Int64)(object)result) || (typeof (TResult) == typeof (UInt64) && default (UInt64) == (UInt64)(object)result) || (typeof (TResult) == typeof (Int16) && default (Int16) == (Int16)(object)result) || (typeof (TResult) == typeof (UInt16) && default (UInt16) == (UInt16)(object)result) || (typeof (TResult) == typeof (IntPtr) && default (IntPtr) == (IntPtr)(object)result) || (typeof (TResult) == typeof (UIntPtr) && default (UIntPtr) == (UIntPtr)(object)result))
                {
                    return s_defaultResultTask;
                }
            }
            else if (result == null)
            {
                return s_defaultResultTask;
            }

            return new Task<TResult>(result);
        }
    }

    internal static class AsyncTaskCache
    {
        internal readonly static Task<Boolean> TrueTask = CreateCacheableTask(true);
        internal readonly static Task<Boolean> FalseTask = CreateCacheableTask(false);
        internal readonly static Task<Int32>[] Int32Tasks = CreateInt32Tasks();
        internal const Int32 INCLUSIVE_INT32_MIN = -1;
        internal const Int32 EXCLUSIVE_INT32_MAX = 9;
        private static Task<Int32>[] CreateInt32Tasks()
        {
            Contract.Assert(EXCLUSIVE_INT32_MAX >= INCLUSIVE_INT32_MIN, "Expected max to be at least min");
            var tasks = new Task<Int32>[EXCLUSIVE_INT32_MAX - INCLUSIVE_INT32_MIN];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = CreateCacheableTask(i + INCLUSIVE_INT32_MIN);
            }

            return tasks;
        }

        internal static Task<TResult> CreateCacheableTask<TResult>(TResult result)
        {
            return new Task<TResult>(false, result, (TaskCreationOptions)InternalTaskOptions.DoNotDispose, default (CancellationToken));
        }
    }

    internal struct AsyncMethodBuilderCore
    {
        internal IAsyncStateMachine m_stateMachine;
        internal Action m_defaultContextAction;
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            if (stateMachine == null)
                throw new ArgumentNullException("stateMachine");
            Contract.EndContractBlock();
            if (m_stateMachine != null)
                throw new InvalidOperationException(Environment.GetResourceString("AsyncMethodBuilder_InstanceNotInitialized"));
            m_stateMachine = stateMachine;
        }

        internal Action GetCompletionAction(Task taskForTracing, ref MoveNextRunner runnerToInitialize)
        {
            Contract.Assert(m_defaultContextAction == null || m_stateMachine != null, "Expected non-null m_stateMachine on non-null m_defaultContextAction");
            Debugger.NotifyOfCrossThreadDependency();
            var capturedContext = ExecutionContext.FastCapture();
            Action action;
            MoveNextRunner runner;
            if (capturedContext != null && capturedContext.IsPreAllocatedDefault)
            {
                action = m_defaultContextAction;
                if (action != null)
                {
                    Contract.Assert(m_stateMachine != null, "If the delegate was set, the state machine should have been as well.");
                    return action;
                }

                runner = new MoveNextRunner(capturedContext, m_stateMachine);
                action = new Action(runner.Run);
                if (taskForTracing != null)
                {
                    m_defaultContextAction = action = OutputAsyncCausalityEvents(taskForTracing, action);
                }
                else
                {
                    m_defaultContextAction = action;
                }
            }
            else
            {
                runner = new MoveNextRunner(capturedContext, m_stateMachine);
                action = new Action(runner.Run);
                if (taskForTracing != null)
                {
                    action = OutputAsyncCausalityEvents(taskForTracing, action);
                }
            }

            if (m_stateMachine == null)
                runnerToInitialize = runner;
            return action;
        }

        private Action OutputAsyncCausalityEvents(Task innerTask, Action continuation)
        {
            return CreateContinuationWrapper(continuation, () =>
            {
                AsyncCausalityTracer.TraceSynchronousWorkStart(CausalityTraceLevel.Required, innerTask.Id, CausalitySynchronousWork.Execution);
                continuation.Invoke();
                AsyncCausalityTracer.TraceSynchronousWorkCompletion(CausalityTraceLevel.Required, CausalitySynchronousWork.Execution);
            }

            , innerTask);
        }

        internal void PostBoxInitialization(IAsyncStateMachine stateMachine, MoveNextRunner runner, Task builtTask)
        {
            if (builtTask != null)
            {
                if (AsyncCausalityTracer.LoggingOn)
                    AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel.Required, builtTask.Id, "Async: " + stateMachine.GetType().Name, 0);
                if (System.Threading.Tasks.Task.s_asyncDebuggingEnabled)
                    System.Threading.Tasks.Task.AddToActiveTasks(builtTask);
            }

            m_stateMachine = stateMachine;
            m_stateMachine.SetStateMachine(m_stateMachine);
            Contract.Assert(runner.m_stateMachine == null, "The runner's state machine should not yet have been populated.");
            Contract.Assert(m_stateMachine != null, "The builder's state machine field should have been initialized.");
            runner.m_stateMachine = m_stateMachine;
        }

        internal static void ThrowAsync(Exception exception, SynchronizationContext targetContext)
        {
            var edi = ExceptionDispatchInfo.Capture(exception);
            if (targetContext != null)
            {
                try
                {
                    targetContext.Post(state => ((ExceptionDispatchInfo)state).Throw(), edi);
                    return;
                }
                catch (Exception postException)
                {
                    edi = ExceptionDispatchInfo.Capture(new AggregateException(exception, postException));
                }
            }

            if (!WindowsRuntimeMarshal.ReportUnhandledError(edi.SourceException))
            {
                ThreadPool.QueueUserWorkItem(state => ((ExceptionDispatchInfo)state).Throw(), edi);
            }
        }

        internal sealed class MoveNextRunner
        {
            private readonly ExecutionContext m_context;
            internal IAsyncStateMachine m_stateMachine;
            internal MoveNextRunner(ExecutionContext context, IAsyncStateMachine stateMachine)
            {
                m_context = context;
                m_stateMachine = stateMachine;
            }

            internal void Run()
            {
                Contract.Assert(m_stateMachine != null, "The state machine must have been set before calling Run.");
                if (m_context != null)
                {
                    try
                    {
                        ContextCallback callback = s_invokeMoveNext;
                        if (callback == null)
                        {
                            s_invokeMoveNext = callback = InvokeMoveNext;
                        }

                        ExecutionContext.Run(m_context, callback, m_stateMachine, preserveSyncCtx: true);
                    }
                    finally
                    {
                        m_context.Dispose();
                    }
                }
                else
                {
                    m_stateMachine.MoveNext();
                }
            }

            private static ContextCallback s_invokeMoveNext;
            private static void InvokeMoveNext(object stateMachine)
            {
                ((IAsyncStateMachine)stateMachine).MoveNext();
            }
        }

        private class ContinuationWrapper
        {
            internal readonly Action m_continuation;
            private readonly Action m_invokeAction;
            internal readonly Task m_innerTask;
            internal ContinuationWrapper(Action continuation, Action invokeAction, Task innerTask)
            {
                Contract.Requires(continuation != null, "Expected non-null continuation");
                if (innerTask == null)
                    innerTask = TryGetContinuationTask(continuation);
                m_continuation = continuation;
                m_innerTask = innerTask;
                m_invokeAction = invokeAction;
            }

            internal void Invoke()
            {
                m_invokeAction();
            }
        }

        internal static Action CreateContinuationWrapper(Action continuation, Action invokeAction, Task innerTask = null)
        {
            return new ContinuationWrapper(continuation, invokeAction, innerTask).Invoke;
        }

        internal static Action TryGetStateMachineForDebugger(Action action)
        {
            object target = action.Target;
            var runner = target as AsyncMethodBuilderCore.MoveNextRunner;
            if (runner != null)
            {
                return new Action(runner.m_stateMachine.MoveNext);
            }

            var continuationWrapper = target as ContinuationWrapper;
            if (continuationWrapper != null)
            {
                return TryGetStateMachineForDebugger(continuationWrapper.m_continuation);
            }

            return action;
        }

        internal static Task TryGetContinuationTask(Action action)
        {
            if (action != null)
            {
                var asWrapper = action.Target as ContinuationWrapper;
                if (asWrapper != null)
                    return asWrapper.m_innerTask;
            }

            return null;
        }
    }
}