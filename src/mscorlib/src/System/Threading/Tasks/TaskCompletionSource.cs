using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    public class TaskCompletionSource<TResult>
    {
        private readonly Task<TResult> m_task;
        public TaskCompletionSource()
        {
            m_task = new Task<TResult>();
        }

        public TaskCompletionSource(TaskCreationOptions creationOptions): this (null, creationOptions)
        {
        }

        public TaskCompletionSource(object state): this (state, TaskCreationOptions.None)
        {
        }

        public TaskCompletionSource(object state, TaskCreationOptions creationOptions)
        {
            m_task = new Task<TResult>(state, creationOptions);
        }

        public Task<TResult> Task
        {
            get
            {
                return m_task;
            }
        }

        private void SpinUntilCompleted()
        {
            var sw = new SpinWait();
            while (!m_task.IsCompleted)
                sw.SpinOnce();
        }

        public bool TrySetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            bool rval = m_task.TrySetException(exception);
            if (!rval && !m_task.IsCompleted)
                SpinUntilCompleted();
            return rval;
        }

        public bool TrySetException(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null)
                throw new ArgumentNullException("exceptions");
            List<Exception> defensiveCopy = new List<Exception>();
            foreach (Exception e in exceptions)
            {
                if (e == null)
                    throw new ArgumentException(Environment.GetResourceString("TaskCompletionSourceT_TrySetException_NullException"), "exceptions");
                defensiveCopy.Add(e);
            }

            if (defensiveCopy.Count == 0)
                throw new ArgumentException(Environment.GetResourceString("TaskCompletionSourceT_TrySetException_NoExceptions"), "exceptions");
            bool rval = m_task.TrySetException(defensiveCopy);
            if (!rval && !m_task.IsCompleted)
                SpinUntilCompleted();
            return rval;
        }

        internal bool TrySetException(IEnumerable<ExceptionDispatchInfo> exceptions)
        {
            Contract.Assert(exceptions != null);
            foreach (var edi in exceptions)
                Contract.Assert(edi != null, "Contents must be non-null");
            bool rval = m_task.TrySetException(exceptions);
            if (!rval && !m_task.IsCompleted)
                SpinUntilCompleted();
            return rval;
        }

        public void SetException(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");
            if (!TrySetException(exception))
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
            }
        }

        public void SetException(IEnumerable<Exception> exceptions)
        {
            if (!TrySetException(exceptions))
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
            }
        }

        public bool TrySetResult(TResult result)
        {
            bool rval = m_task.TrySetResult(result);
            if (!rval && !m_task.IsCompleted)
                SpinUntilCompleted();
            return rval;
        }

        public void SetResult(TResult result)
        {
            if (!TrySetResult(result))
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
        }

        public bool TrySetCanceled()
        {
            return TrySetCanceled(default (CancellationToken));
        }

        public bool TrySetCanceled(CancellationToken cancellationToken)
        {
            bool rval = m_task.TrySetCanceled(cancellationToken);
            if (!rval && !m_task.IsCompleted)
                SpinUntilCompleted();
            return rval;
        }

        public void SetCanceled()
        {
            if (!TrySetCanceled())
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
        }
    }
}