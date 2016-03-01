using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Security;
using System.Security.Permissions;

namespace System.Threading.Tasks
{
    public class ConcurrentExclusiveSchedulerPair
    {
        private readonly ConcurrentDictionary<int, ProcessingMode> m_threadProcessingMapping = new ConcurrentDictionary<int, ProcessingMode>();
        private readonly ConcurrentExclusiveTaskScheduler m_concurrentTaskScheduler;
        private readonly ConcurrentExclusiveTaskScheduler m_exclusiveTaskScheduler;
        private readonly TaskScheduler m_underlyingTaskScheduler;
        private readonly int m_maxConcurrencyLevel;
        private readonly int m_maxItemsPerTask;
        private int m_processingCount;
        private CompletionState m_completionState;
        private const int UNLIMITED_PROCESSING = -1;
        private const int EXCLUSIVE_PROCESSING_SENTINEL = -1;
        private const int DEFAULT_MAXITEMSPERTASK = UNLIMITED_PROCESSING;
        private static Int32 DefaultMaxConcurrencyLevel
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }

        private object ValueLock
        {
            get
            {
                return m_threadProcessingMapping;
            }
        }

        public ConcurrentExclusiveSchedulerPair(): this (TaskScheduler.Default, DefaultMaxConcurrencyLevel, DEFAULT_MAXITEMSPERTASK)
        {
        }

        public ConcurrentExclusiveSchedulerPair(TaskScheduler taskScheduler): this (taskScheduler, DefaultMaxConcurrencyLevel, DEFAULT_MAXITEMSPERTASK)
        {
        }

        public ConcurrentExclusiveSchedulerPair(TaskScheduler taskScheduler, int maxConcurrencyLevel): this (taskScheduler, maxConcurrencyLevel, DEFAULT_MAXITEMSPERTASK)
        {
        }

        public ConcurrentExclusiveSchedulerPair(TaskScheduler taskScheduler, int maxConcurrencyLevel, int maxItemsPerTask)
        {
            if (taskScheduler == null)
                throw new ArgumentNullException("taskScheduler");
            if (maxConcurrencyLevel == 0 || maxConcurrencyLevel < -1)
                throw new ArgumentOutOfRangeException("maxConcurrencyLevel");
            if (maxItemsPerTask == 0 || maxItemsPerTask < -1)
                throw new ArgumentOutOfRangeException("maxItemsPerTask");
            Contract.EndContractBlock();
            m_underlyingTaskScheduler = taskScheduler;
            m_maxConcurrencyLevel = maxConcurrencyLevel;
            m_maxItemsPerTask = maxItemsPerTask;
            int mcl = taskScheduler.MaximumConcurrencyLevel;
            if (mcl > 0 && mcl < m_maxConcurrencyLevel)
                m_maxConcurrencyLevel = mcl;
            if (m_maxConcurrencyLevel == UNLIMITED_PROCESSING)
                m_maxConcurrencyLevel = Int32.MaxValue;
            if (m_maxItemsPerTask == UNLIMITED_PROCESSING)
                m_maxItemsPerTask = Int32.MaxValue;
            m_exclusiveTaskScheduler = new ConcurrentExclusiveTaskScheduler(this, 1, ProcessingMode.ProcessingExclusiveTask);
            m_concurrentTaskScheduler = new ConcurrentExclusiveTaskScheduler(this, m_maxConcurrencyLevel, ProcessingMode.ProcessingConcurrentTasks);
        }

        public void Complete()
        {
            lock (ValueLock)
            {
                if (!CompletionRequested)
                {
                    RequestCompletion();
                    CleanupStateIfCompletingAndQuiesced();
                }
            }
        }

        public Task Completion
        {
            get
            {
                return EnsureCompletionStateInitialized().Task;
            }
        }

        private CompletionState EnsureCompletionStateInitialized()
        {
            return LazyInitializer.EnsureInitialized(ref m_completionState, () => new CompletionState());
        }

        private bool CompletionRequested
        {
            get
            {
                return m_completionState != null && Volatile.Read(ref m_completionState.m_completionRequested);
            }
        }

        private void RequestCompletion()
        {
            ContractAssertMonitorStatus(ValueLock, held: true);
            EnsureCompletionStateInitialized().m_completionRequested = true;
        }

        private void CleanupStateIfCompletingAndQuiesced()
        {
            ContractAssertMonitorStatus(ValueLock, held: true);
            if (ReadyToComplete)
                CompleteTaskAsync();
        }

        private bool ReadyToComplete
        {
            get
            {
                ContractAssertMonitorStatus(ValueLock, held: true);
                if (!CompletionRequested || m_processingCount != 0)
                    return false;
                var cs = EnsureCompletionStateInitialized();
                return (cs.m_exceptions != null && cs.m_exceptions.Count > 0) || (m_concurrentTaskScheduler.m_tasks.IsEmpty && m_exclusiveTaskScheduler.m_tasks.IsEmpty);
            }
        }

        private void CompleteTaskAsync()
        {
            Contract.Requires(ReadyToComplete, "The block must be ready to complete to be here.");
            ContractAssertMonitorStatus(ValueLock, held: true);
            var cs = EnsureCompletionStateInitialized();
            if (!cs.m_completionQueued)
            {
                cs.m_completionQueued = true;
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var localCs = (CompletionState)state;
                    Contract.Assert(!localCs.Task.IsCompleted, "Completion should only happen once.");
                    var exceptions = localCs.m_exceptions;
                    bool success = (exceptions != null && exceptions.Count > 0) ? localCs.TrySetException(exceptions) : localCs.TrySetResult(default (VoidTaskResult));
                    Contract.Assert(success, "Expected to complete completion task.");
                }

                , cs);
            }
        }

        private void FaultWithTask(Task faultedTask)
        {
            Contract.Requires(faultedTask != null && faultedTask.IsFaulted && faultedTask.Exception.InnerExceptions.Count > 0, "Needs a task in the faulted state and thus with exceptions.");
            ContractAssertMonitorStatus(ValueLock, held: true);
            var cs = EnsureCompletionStateInitialized();
            if (cs.m_exceptions == null)
                cs.m_exceptions = new List<Exception>();
            cs.m_exceptions.AddRange(faultedTask.Exception.InnerExceptions);
            RequestCompletion();
        }

        public TaskScheduler ConcurrentScheduler
        {
            get
            {
                return m_concurrentTaskScheduler;
            }
        }

        public TaskScheduler ExclusiveScheduler
        {
            get
            {
                return m_exclusiveTaskScheduler;
            }
        }

        private int ConcurrentTaskCountForDebugger
        {
            get
            {
                return m_concurrentTaskScheduler.m_tasks.Count;
            }
        }

        private int ExclusiveTaskCountForDebugger
        {
            get
            {
                return m_exclusiveTaskScheduler.m_tasks.Count;
            }
        }

        private void ProcessAsyncIfNecessary(bool fairly = false)
        {
            ContractAssertMonitorStatus(ValueLock, held: true);
            if (m_processingCount >= 0)
            {
                bool exclusiveTasksAreWaiting = !m_exclusiveTaskScheduler.m_tasks.IsEmpty;
                Task processingTask = null;
                if (m_processingCount == 0 && exclusiveTasksAreWaiting)
                {
                    m_processingCount = EXCLUSIVE_PROCESSING_SENTINEL;
                    try
                    {
                        processingTask = new Task(thisPair => ((ConcurrentExclusiveSchedulerPair)thisPair).ProcessExclusiveTasks(), this, default (CancellationToken), GetCreationOptionsForTask(fairly));
                        processingTask.Start(m_underlyingTaskScheduler);
                    }
                    catch
                    {
                        m_processingCount = 0;
                        FaultWithTask(processingTask);
                    }
                }
                else
                {
                    int concurrentTasksWaitingCount = m_concurrentTaskScheduler.m_tasks.Count;
                    if (concurrentTasksWaitingCount > 0 && !exclusiveTasksAreWaiting && m_processingCount < m_maxConcurrencyLevel)
                    {
                        for (int i = 0; i < concurrentTasksWaitingCount && m_processingCount < m_maxConcurrencyLevel; ++i)
                        {
                            ++m_processingCount;
                            try
                            {
                                processingTask = new Task(thisPair => ((ConcurrentExclusiveSchedulerPair)thisPair).ProcessConcurrentTasks(), this, default (CancellationToken), GetCreationOptionsForTask(fairly));
                                processingTask.Start(m_underlyingTaskScheduler);
                            }
                            catch
                            {
                                --m_processingCount;
                                FaultWithTask(processingTask);
                            }
                        }
                    }
                }

                CleanupStateIfCompletingAndQuiesced();
            }
            else
                Contract.Assert(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "The processing count must be the sentinel if it's not >= 0.");
        }

        private void ProcessExclusiveTasks()
        {
            Contract.Requires(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "Processing exclusive tasks requires being in exclusive mode.");
            Contract.Requires(!m_exclusiveTaskScheduler.m_tasks.IsEmpty, "Processing exclusive tasks requires tasks to be processed.");
            ContractAssertMonitorStatus(ValueLock, held: false);
            try
            {
                Contract.Assert(!m_threadProcessingMapping.ContainsKey(Thread.CurrentThread.ManagedThreadId), "This thread should not yet be involved in this pair's processing.");
                m_threadProcessingMapping[Thread.CurrentThread.ManagedThreadId] = ProcessingMode.ProcessingExclusiveTask;
                for (int i = 0; i < m_maxItemsPerTask; i++)
                {
                    Task exclusiveTask;
                    if (!m_exclusiveTaskScheduler.m_tasks.TryDequeue(out exclusiveTask))
                        break;
                    if (!exclusiveTask.IsFaulted)
                        m_exclusiveTaskScheduler.ExecuteTask(exclusiveTask);
                }
            }
            finally
            {
                ProcessingMode currentMode;
                m_threadProcessingMapping.TryRemove(Thread.CurrentThread.ManagedThreadId, out currentMode);
                Contract.Assert(currentMode == ProcessingMode.ProcessingExclusiveTask, "Somehow we ended up escaping exclusive mode.");
                lock (ValueLock)
                {
                    Contract.Assert(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "The processing mode should not have deviated from exclusive.");
                    m_processingCount = 0;
                    ProcessAsyncIfNecessary(true);
                }
            }
        }

        private void ProcessConcurrentTasks()
        {
            Contract.Requires(m_processingCount > 0, "Processing concurrent tasks requires us to be in concurrent mode.");
            ContractAssertMonitorStatus(ValueLock, held: false);
            try
            {
                Contract.Assert(!m_threadProcessingMapping.ContainsKey(Thread.CurrentThread.ManagedThreadId), "This thread should not yet be involved in this pair's processing.");
                m_threadProcessingMapping[Thread.CurrentThread.ManagedThreadId] = ProcessingMode.ProcessingConcurrentTasks;
                for (int i = 0; i < m_maxItemsPerTask; i++)
                {
                    Task concurrentTask;
                    if (!m_concurrentTaskScheduler.m_tasks.TryDequeue(out concurrentTask))
                        break;
                    if (!concurrentTask.IsFaulted)
                        m_concurrentTaskScheduler.ExecuteTask(concurrentTask);
                    if (!m_exclusiveTaskScheduler.m_tasks.IsEmpty)
                        break;
                }
            }
            finally
            {
                ProcessingMode currentMode;
                m_threadProcessingMapping.TryRemove(Thread.CurrentThread.ManagedThreadId, out currentMode);
                Contract.Assert(currentMode == ProcessingMode.ProcessingConcurrentTasks, "Somehow we ended up escaping concurrent mode.");
                lock (ValueLock)
                {
                    Contract.Assert(m_processingCount > 0, "The procesing mode should not have deviated from concurrent.");
                    if (m_processingCount > 0)
                        --m_processingCount;
                    ProcessAsyncIfNecessary(true);
                }
            }
        }

        private sealed class CompletionState : TaskCompletionSource<VoidTaskResult>
        {
            internal bool m_completionRequested;
            internal bool m_completionQueued;
            internal List<Exception> m_exceptions;
        }

        private sealed class ConcurrentExclusiveTaskScheduler : TaskScheduler
        {
            private static readonly Func<object, bool> s_tryExecuteTaskShim = new Func<object, bool>(TryExecuteTaskShim);
            private readonly ConcurrentExclusiveSchedulerPair m_pair;
            private readonly int m_maxConcurrencyLevel;
            private readonly ProcessingMode m_processingMode;
            internal readonly IProducerConsumerQueue<Task> m_tasks;
            internal ConcurrentExclusiveTaskScheduler(ConcurrentExclusiveSchedulerPair pair, int maxConcurrencyLevel, ProcessingMode processingMode)
            {
                Contract.Requires(pair != null, "Scheduler must be associated with a valid pair.");
                Contract.Requires(processingMode == ProcessingMode.ProcessingConcurrentTasks || processingMode == ProcessingMode.ProcessingExclusiveTask, "Scheduler must be for concurrent or exclusive processing.");
                Contract.Requires((processingMode == ProcessingMode.ProcessingConcurrentTasks && (maxConcurrencyLevel >= 1 || maxConcurrencyLevel == UNLIMITED_PROCESSING)) || (processingMode == ProcessingMode.ProcessingExclusiveTask && maxConcurrencyLevel == 1), "If we're in concurrent mode, our concurrency level should be positive or unlimited.  If exclusive, it should be 1.");
                m_pair = pair;
                m_maxConcurrencyLevel = maxConcurrencyLevel;
                m_processingMode = processingMode;
                m_tasks = (processingMode == ProcessingMode.ProcessingExclusiveTask) ? (IProducerConsumerQueue<Task>)new SingleProducerSingleConsumerQueue<Task>() : (IProducerConsumerQueue<Task>)new MultiProducerMultiConsumerQueue<Task>();
            }

            public override int MaximumConcurrencyLevel
            {
                get
                {
                    return m_maxConcurrencyLevel;
                }
            }

            protected internal override void QueueTask(Task task)
            {
                Contract.Assert(task != null, "Infrastructure should have provided a non-null task.");
                lock (m_pair.ValueLock)
                {
                    if (m_pair.CompletionRequested)
                        throw new InvalidOperationException(GetType().Name);
                    m_tasks.Enqueue(task);
                    m_pair.ProcessAsyncIfNecessary();
                }
            }

            internal void ExecuteTask(Task task)
            {
                Contract.Assert(task != null, "Infrastructure should have provided a non-null task.");
                base.TryExecuteTask(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                Contract.Assert(task != null, "Infrastructure should have provided a non-null task.");
                if (!taskWasPreviouslyQueued && m_pair.CompletionRequested)
                    return false;
                bool isDefaultScheduler = m_pair.m_underlyingTaskScheduler == TaskScheduler.Default;
                if (isDefaultScheduler && taskWasPreviouslyQueued && !Thread.CurrentThread.IsThreadPoolThread)
                {
                    return false;
                }
                else
                {
                    ProcessingMode currentThreadMode;
                    if (m_pair.m_threadProcessingMapping.TryGetValue(Thread.CurrentThread.ManagedThreadId, out currentThreadMode) && currentThreadMode == m_processingMode)
                    {
                        return (isDefaultScheduler && !taskWasPreviouslyQueued) ? TryExecuteTask(task) : TryExecuteTaskInlineOnTargetScheduler(task);
                    }
                }

                return false;
            }

            private bool TryExecuteTaskInlineOnTargetScheduler(Task task)
            {
                var t = new Task<bool>(s_tryExecuteTaskShim, Tuple.Create(this, task));
                try
                {
                    t.RunSynchronously(m_pair.m_underlyingTaskScheduler);
                    return t.Result;
                }
                catch
                {
                    Contract.Assert(t.IsFaulted, "Task should be faulted due to the scheduler faulting it and throwing the exception.");
                    var ignored = t.Exception;
                    throw;
                }
                finally
                {
                    t.Dispose();
                }
            }

            private static bool TryExecuteTaskShim(object state)
            {
                var tuple = (Tuple<ConcurrentExclusiveTaskScheduler, Task>)state;
                return tuple.Item1.TryExecuteTask(tuple.Item2);
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return m_tasks;
            }

            private int CountForDebugger
            {
                get
                {
                    return m_tasks.Count;
                }
            }

            private sealed class DebugView
            {
                private readonly ConcurrentExclusiveTaskScheduler m_taskScheduler;
                public DebugView(ConcurrentExclusiveTaskScheduler scheduler)
                {
                    Contract.Requires(scheduler != null, "Need a scheduler with which to construct the debug view.");
                    m_taskScheduler = scheduler;
                }

                public int MaximumConcurrencyLevel
                {
                    get
                    {
                        return m_taskScheduler.m_maxConcurrencyLevel;
                    }
                }

                public IEnumerable<Task> ScheduledTasks
                {
                    get
                    {
                        return m_taskScheduler.m_tasks;
                    }
                }

                public ConcurrentExclusiveSchedulerPair SchedulerPair
                {
                    get
                    {
                        return m_taskScheduler.m_pair;
                    }
                }
            }
        }

        private sealed class DebugView
        {
            private readonly ConcurrentExclusiveSchedulerPair m_pair;
            public DebugView(ConcurrentExclusiveSchedulerPair pair)
            {
                Contract.Requires(pair != null, "Need a pair with which to construct the debug view.");
                m_pair = pair;
            }

            public ProcessingMode Mode
            {
                get
                {
                    return m_pair.ModeForDebugger;
                }
            }

            public IEnumerable<Task> ScheduledExclusive
            {
                get
                {
                    return m_pair.m_exclusiveTaskScheduler.m_tasks;
                }
            }

            public IEnumerable<Task> ScheduledConcurrent
            {
                get
                {
                    return m_pair.m_concurrentTaskScheduler.m_tasks;
                }
            }

            public int CurrentlyExecutingTaskCount
            {
                get
                {
                    return (m_pair.m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL) ? 1 : m_pair.m_processingCount;
                }
            }

            public TaskScheduler TargetScheduler
            {
                get
                {
                    return m_pair.m_underlyingTaskScheduler;
                }
            }
        }

        private ProcessingMode ModeForDebugger
        {
            get
            {
                if (m_completionState != null && m_completionState.Task.IsCompleted)
                    return ProcessingMode.Completed;
                var mode = ProcessingMode.NotCurrentlyProcessing;
                if (m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL)
                    mode |= ProcessingMode.ProcessingExclusiveTask;
                if (m_processingCount >= 1)
                    mode |= ProcessingMode.ProcessingConcurrentTasks;
                if (CompletionRequested)
                    mode |= ProcessingMode.Completing;
                return mode;
            }
        }

        internal static void ContractAssertMonitorStatus(object syncObj, bool held)
        {
            Contract.Requires(syncObj != null, "The monitor object to check must be provided.");
            Contract.Assert(Monitor.IsEntered(syncObj) == held, "The locking scheme was not correctly followed.");
        }

        internal static TaskCreationOptions GetCreationOptionsForTask(bool isReplacementReplica = false)
        {
            TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;
            if (isReplacementReplica)
                options |= TaskCreationOptions.PreferFairness;
            return options;
        }

        [Flags]
        private enum ProcessingMode : byte
        {
            NotCurrentlyProcessing = 0x0,
            ProcessingExclusiveTask = 0x1,
            ProcessingConcurrentTasks = 0x2,
            Completing = 0x4,
            Completed = 0x8
        }
    }
}