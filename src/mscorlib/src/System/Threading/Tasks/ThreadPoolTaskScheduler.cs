using System.Collections.Generic;

namespace System.Threading.Tasks
{
    internal sealed class ThreadPoolTaskScheduler : TaskScheduler
    {
        internal ThreadPoolTaskScheduler()
        {
            int id = base.Id;
        }

        private static readonly ParameterizedThreadStart s_longRunningThreadWork = new ParameterizedThreadStart(LongRunningThreadWork);
        private static void LongRunningThreadWork(object obj)
        {
                        Task t = obj as Task;
                        t.ExecuteEntry(false);
        }

        protected internal override void QueueTask(Task task)
        {
            if ((task.Options & TaskCreationOptions.LongRunning) != 0)
            {
                Thread thread = new Thread(s_longRunningThreadWork);
                thread.IsBackground = true;
                thread.Start(task);
            }
            else
            {
                bool forceToGlobalQueue = ((task.Options & TaskCreationOptions.PreferFairness) != 0);
                ThreadPool.UnsafeQueueCustomWorkItem(task, forceToGlobalQueue);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (taskWasPreviouslyQueued && !ThreadPool.TryPopCustomWorkItem(task))
                return false;
            bool rval = false;
            try
            {
                rval = task.ExecuteEntry(false);
            }
            finally
            {
                if (taskWasPreviouslyQueued)
                    NotifyWorkItemProgress();
            }

            return rval;
        }

        protected internal override bool TryDequeue(Task task)
        {
            return ThreadPool.TryPopCustomWorkItem(task);
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return FilterTasksFromWorkItems(ThreadPool.GetQueuedWorkItems());
        }

        private IEnumerable<Task> FilterTasksFromWorkItems(IEnumerable<IThreadPoolWorkItem> tpwItems)
        {
            foreach (IThreadPoolWorkItem tpwi in tpwItems)
            {
                if (tpwi is Task)
                {
                    yield return (Task)tpwi;
                }
            }
        }

        internal override void NotifyWorkItemProgress()
        {
            ThreadPool.NotifyWorkItemProgress();
        }

        internal override bool RequiresAtomicStartTransition
        {
            get
            {
                return false;
            }
        }
    }
}