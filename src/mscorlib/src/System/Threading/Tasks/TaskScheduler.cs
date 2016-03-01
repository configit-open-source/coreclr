using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading.Tasks
{
    public abstract class TaskScheduler
    {
        protected internal abstract void QueueTask(Task task);
        protected abstract bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued);
        protected abstract IEnumerable<Task> GetScheduledTasks();
        public virtual Int32 MaximumConcurrencyLevel
        {
            get
            {
                return Int32.MaxValue;
            }
        }

        internal bool TryRunInline(Task task, bool taskWasPreviouslyQueued)
        {
            TaskScheduler ets = task.ExecutingTaskScheduler;
            if (ets != this && ets != null)
                return ets.TryRunInline(task, taskWasPreviouslyQueued);
            StackGuard currentStackGuard;
            if ((ets == null) || (task.m_action == null) || task.IsDelegateInvoked || task.IsCanceled || (currentStackGuard = Task.CurrentStackGuard).TryBeginInliningScope() == false)
            {
                return false;
            }

            bool bInlined = false;
            try
            {
                task.FireTaskScheduledIfNeeded(this);
                bInlined = TryExecuteTaskInline(task, taskWasPreviouslyQueued);
            }
            finally
            {
                currentStackGuard.EndInliningScope();
            }

            if (bInlined && !(task.IsDelegateInvoked || task.IsCanceled))
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskScheduler_InconsistentStateAfterTryExecuteTaskInline"));
            }

            return bInlined;
        }

        protected internal virtual bool TryDequeue(Task task)
        {
            return false;
        }

        internal virtual void NotifyWorkItemProgress()
        {
        }

        internal virtual bool RequiresAtomicStartTransition
        {
            get
            {
                return true;
            }
        }

        internal void InternalQueueTask(Task task)
        {
                        task.FireTaskScheduledIfNeeded(this);
            this.QueueTask(task);
        }

        private static ConditionalWeakTable<TaskScheduler, object> s_activeTaskSchedulers;
        private static readonly TaskScheduler s_defaultTaskScheduler = new ThreadPoolTaskScheduler();
        internal static int s_taskSchedulerIdCounter;
        private volatile int m_taskSchedulerId;
        protected TaskScheduler()
        {
            if (Debugger.IsAttached)
            {
                AddToActiveTaskSchedulers();
            }
        }

        private void AddToActiveTaskSchedulers()
        {
            ConditionalWeakTable<TaskScheduler, object> activeTaskSchedulers = s_activeTaskSchedulers;
            if (activeTaskSchedulers == null)
            {
                Interlocked.CompareExchange(ref s_activeTaskSchedulers, new ConditionalWeakTable<TaskScheduler, object>(), null);
                activeTaskSchedulers = s_activeTaskSchedulers;
            }

            activeTaskSchedulers.Add(this, null);
        }

        public static TaskScheduler Default
        {
            get
            {
                return s_defaultTaskScheduler;
            }
        }

        public static TaskScheduler Current
        {
            get
            {
                TaskScheduler current = InternalCurrent;
                return current ?? TaskScheduler.Default;
            }
        }

        internal static TaskScheduler InternalCurrent
        {
            get
            {
                Task currentTask = Task.InternalCurrent;
                return ((currentTask != null) && ((currentTask.CreationOptions & TaskCreationOptions.HideScheduler) == 0)) ? currentTask.ExecutingTaskScheduler : null;
            }
        }

        public static TaskScheduler FromCurrentSynchronizationContext()
        {
            return new SynchronizationContextTaskScheduler();
        }

        public Int32 Id
        {
            get
            {
                if (m_taskSchedulerId == 0)
                {
                    int newId = 0;
                    do
                    {
                        newId = Interlocked.Increment(ref s_taskSchedulerIdCounter);
                    }
                    while (newId == 0);
                    Interlocked.CompareExchange(ref m_taskSchedulerId, newId, 0);
                }

                return m_taskSchedulerId;
            }
        }

        protected bool TryExecuteTask(Task task)
        {
            if (task.ExecutingTaskScheduler != this)
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskScheduler_ExecuteTask_WrongTaskScheduler"));
            }

            return task.ExecuteEntry(true);
        }

        private static EventHandler<UnobservedTaskExceptionEventArgs> _unobservedTaskException;
        private static readonly object _unobservedTaskExceptionLockObject = new object ();
        public static event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException
        {
            [System.Security.SecurityCritical]
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (_unobservedTaskExceptionLockObject)
                        _unobservedTaskException += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (_unobservedTaskExceptionLockObject)
                    _unobservedTaskException -= value;
            }
        }

        internal static void PublishUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs ueea)
        {
            lock (_unobservedTaskExceptionLockObject)
            {
                EventHandler<UnobservedTaskExceptionEventArgs> handler = _unobservedTaskException;
                if (handler != null)
                {
                    handler(sender, ueea);
                }
            }
        }

        internal Task[] GetScheduledTasksForDebugger()
        {
            IEnumerable<Task> activeTasksSource = GetScheduledTasks();
            if (activeTasksSource == null)
                return null;
            Task[] activeTasksArray = activeTasksSource as Task[];
            if (activeTasksArray == null)
            {
                activeTasksArray = (new List<Task>(activeTasksSource)).ToArray();
            }

            foreach (Task t in activeTasksArray)
            {
                int tmp = t.Id;
            }

            return activeTasksArray;
        }

        internal static TaskScheduler[] GetTaskSchedulersForDebugger()
        {
            if (s_activeTaskSchedulers == null)
            {
                return new TaskScheduler[]{s_defaultTaskScheduler};
            }

            ICollection<TaskScheduler> schedulers = s_activeTaskSchedulers.Keys;
            if (!schedulers.Contains(s_defaultTaskScheduler))
            {
                schedulers.Add(s_defaultTaskScheduler);
            }

            var arr = new TaskScheduler[schedulers.Count];
            schedulers.CopyTo(arr, 0);
            foreach (var scheduler in arr)
            {
                                int tmp = scheduler.Id;
            }

            return arr;
        }

        internal sealed class SystemThreadingTasks_TaskSchedulerDebugView
        {
            private readonly TaskScheduler m_taskScheduler;
            public SystemThreadingTasks_TaskSchedulerDebugView(TaskScheduler scheduler)
            {
                m_taskScheduler = scheduler;
            }

            public Int32 Id
            {
                get
                {
                    return m_taskScheduler.Id;
                }
            }

            public IEnumerable<Task> ScheduledTasks
            {
                [SecurityCritical]
                get
                {
                    return m_taskScheduler.GetScheduledTasks();
                }
            }
        }
    }

    internal sealed class SynchronizationContextTaskScheduler : TaskScheduler
    {
        private SynchronizationContext m_synchronizationContext;
        internal SynchronizationContextTaskScheduler()
        {
            SynchronizationContext synContext = SynchronizationContext.Current;
            if (synContext == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskScheduler_FromCurrentSynchronizationContext_NoCurrent"));
            }

            m_synchronizationContext = synContext;
        }

        protected internal override void QueueTask(Task task)
        {
            m_synchronizationContext.Post(s_postCallback, (object)task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (SynchronizationContext.Current == m_synchronizationContext)
            {
                return TryExecuteTask(task);
            }
            else
                return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return null;
        }

        public override Int32 MaximumConcurrencyLevel
        {
            get
            {
                return 1;
            }
        }

        private static SendOrPostCallback s_postCallback = new SendOrPostCallback(PostCallback);
        private static void PostCallback(object obj)
        {
            Task task = (Task)obj;
            task.ExecuteEntry(true);
        }
    }

    public class UnobservedTaskExceptionEventArgs : EventArgs
    {
        private AggregateException m_exception;
        internal bool m_observed = false;
        public UnobservedTaskExceptionEventArgs(AggregateException exception)
        {
            m_exception = exception;
        }

        public void SetObserved()
        {
            m_observed = true;
        }

        public bool Observed
        {
            get
            {
                return m_observed;
            }
        }

        public AggregateException Exception
        {
            get
            {
                return m_exception;
            }
        }
    }
}