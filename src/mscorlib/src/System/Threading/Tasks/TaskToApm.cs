
using System.IO;

namespace System.Threading.Tasks
{
    internal static class TaskToApm
    {
        public static IAsyncResult Begin(Task task, AsyncCallback callback, object state)
        {
                        IAsyncResult asyncResult;
            if (task.IsCompleted)
            {
                asyncResult = new TaskWrapperAsyncResult(task, state, completedSynchronously: true);
                if (callback != null)
                    callback(asyncResult);
            }
            else
            {
                asyncResult = task.AsyncState == state ? (IAsyncResult)task : new TaskWrapperAsyncResult(task, state, completedSynchronously: false);
                if (callback != null)
                    InvokeCallbackWhenTaskCompletes(task, callback, asyncResult);
            }

            return asyncResult;
        }

        public static void End(IAsyncResult asyncResult)
        {
            Task task;
            var twar = asyncResult as TaskWrapperAsyncResult;
            if (twar != null)
            {
                task = twar.Task;
                            }
            else
            {
                task = asyncResult as Task;
            }

            if (task == null)
                __Error.WrongAsyncResult();
            task.GetAwaiter().GetResult();
        }

        public static TResult End<TResult>(IAsyncResult asyncResult)
        {
            Task<TResult> task;
            var twar = asyncResult as TaskWrapperAsyncResult;
            if (twar != null)
            {
                task = twar.Task as Task<TResult>;
                            }
            else
            {
                task = asyncResult as Task<TResult>;
            }

            if (task == null)
                __Error.WrongAsyncResult();
            return task.GetAwaiter().GetResult();
        }

        private static void InvokeCallbackWhenTaskCompletes(Task antecedent, AsyncCallback callback, IAsyncResult asyncResult)
        {
                                                antecedent.ConfigureAwait(continueOnCapturedContext: false).GetAwaiter().OnCompleted(() => callback(asyncResult));
        }

        private sealed class TaskWrapperAsyncResult : IAsyncResult
        {
            internal readonly Task Task;
            private readonly object m_state;
            private readonly bool m_completedSynchronously;
            internal TaskWrapperAsyncResult(Task task, object state, bool completedSynchronously)
            {
                                                this.Task = task;
                m_state = state;
                m_completedSynchronously = completedSynchronously;
            }

            object IAsyncResult.AsyncState
            {
                get
                {
                    return m_state;
                }
            }

            bool IAsyncResult.CompletedSynchronously
            {
                get
                {
                    return m_completedSynchronously;
                }
            }

            bool IAsyncResult.IsCompleted
            {
                get
                {
                    return this.Task.IsCompleted;
                }
            }

            WaitHandle IAsyncResult.AsyncWaitHandle
            {
                get
                {
                    return ((IAsyncResult)this.Task).AsyncWaitHandle;
                }
            }
        }
    }
}