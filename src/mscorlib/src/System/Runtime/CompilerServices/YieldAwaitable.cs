using System.Diagnostics.Contracts;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public struct YieldAwaitable
    {
        public YieldAwaiter GetAwaiter()
        {
            return new YieldAwaiter();
        }

        public struct YieldAwaiter : ICriticalNotifyCompletion
        {
            public bool IsCompleted
            {
                get
                {
                    return false;
                }
            }

            public void OnCompleted(Action continuation)
            {
                QueueContinuation(continuation, flowContext: true);
            }

            public void UnsafeOnCompleted(Action continuation)
            {
                QueueContinuation(continuation, flowContext: false);
            }

            private static void QueueContinuation(Action continuation, bool flowContext)
            {
                if (continuation == null)
                    throw new ArgumentNullException("continuation");
                Contract.EndContractBlock();
                if (TplEtwProvider.Log.IsEnabled())
                {
                    continuation = OutputCorrelationEtwEvent(continuation);
                }

                var syncCtx = SynchronizationContext.CurrentNoFlow;
                if (syncCtx != null && syncCtx.GetType() != typeof (SynchronizationContext))
                {
                    syncCtx.Post(s_sendOrPostCallbackRunAction, continuation);
                }
                else
                {
                    TaskScheduler scheduler = TaskScheduler.Current;
                    if (scheduler == TaskScheduler.Default)
                    {
                        if (flowContext)
                        {
                            ThreadPool.QueueUserWorkItem(s_waitCallbackRunAction, continuation);
                        }
                        else
                        {
                            ThreadPool.UnsafeQueueUserWorkItem(s_waitCallbackRunAction, continuation);
                        }
                    }
                    else
                    {
                        Task.Factory.StartNew(continuation, default (CancellationToken), TaskCreationOptions.PreferFairness, scheduler);
                    }
                }
            }

            private static Action OutputCorrelationEtwEvent(Action continuation)
            {
                int continuationId = Task.NewId();
                Task currentTask = Task.InternalCurrent;
                TplEtwProvider.Log.AwaitTaskContinuationScheduled(TaskScheduler.Current.Id, (currentTask != null) ? currentTask.Id : 0, continuationId);
                return AsyncMethodBuilderCore.CreateContinuationWrapper(continuation, () =>
                {
                    var etwLog = TplEtwProvider.Log;
                    etwLog.TaskWaitContinuationStarted(continuationId);
                    Guid prevActivityId = new Guid();
                    if (etwLog.TasksSetActivityIds)
                        EventSource.SetCurrentThreadActivityId(TplEtwProvider.CreateGuidForTaskID(continuationId), out prevActivityId);
                    continuation();
                    if (etwLog.TasksSetActivityIds)
                        EventSource.SetCurrentThreadActivityId(prevActivityId);
                    etwLog.TaskWaitContinuationComplete(continuationId);
                }

                );
            }

            private static readonly WaitCallback s_waitCallbackRunAction = RunAction;
            private static readonly SendOrPostCallback s_sendOrPostCallbackRunAction = RunAction;
            private static void RunAction(object state)
            {
                ((Action)state)();
            }

            public void GetResult()
            {
            }
        }
    }
}