using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Threading.Tasks
{
    public class ParallelOptions
    {
        private TaskScheduler m_scheduler;
        private int m_maxDegreeOfParallelism;
        private CancellationToken m_cancellationToken;
        public ParallelOptions()
        {
            m_scheduler = TaskScheduler.Default;
            m_maxDegreeOfParallelism = -1;
            m_cancellationToken = CancellationToken.None;
        }

        public TaskScheduler TaskScheduler
        {
            get
            {
                return m_scheduler;
            }

            set
            {
                m_scheduler = value;
            }
        }

        internal TaskScheduler EffectiveTaskScheduler
        {
            get
            {
                if (m_scheduler == null)
                    return TaskScheduler.Current;
                else
                    return m_scheduler;
            }
        }

        public int MaxDegreeOfParallelism
        {
            get
            {
                return m_maxDegreeOfParallelism;
            }

            set
            {
                if ((value == 0) || (value < -1))
                    throw new ArgumentOutOfRangeException("MaxDegreeOfParallelism");
                m_maxDegreeOfParallelism = value;
            }
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return m_cancellationToken;
            }

            set
            {
                m_cancellationToken = value;
            }
        }

        internal int EffectiveMaxConcurrencyLevel
        {
            get
            {
                int rval = MaxDegreeOfParallelism;
                int schedulerMax = EffectiveTaskScheduler.MaximumConcurrencyLevel;
                if ((schedulerMax > 0) && (schedulerMax != Int32.MaxValue))
                {
                    rval = (rval == -1) ? schedulerMax : Math.Min(schedulerMax, rval);
                }

                return rval;
            }
        }
    }

    public static class Parallel
    {
        internal static int s_forkJoinContextID;
        internal const int DEFAULT_LOOP_STRIDE = 16;
        internal static ParallelOptions s_defaultParallelOptions = new ParallelOptions();
        public static void Invoke(params Action[] actions)
        {
            Invoke(s_defaultParallelOptions, actions);
        }

        public static void Invoke(ParallelOptions parallelOptions, params Action[] actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            if (parallelOptions.CancellationToken.CanBeCanceled && AppContextSwitches.ThrowExceptionIfDisposedCancellationTokenSource)
            {
                parallelOptions.CancellationToken.ThrowIfSourceDisposed();
            }

            if (parallelOptions.CancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(parallelOptions.CancellationToken);
            Action[] actionsCopy = new Action[actions.Length];
            for (int i = 0; i < actionsCopy.Length; i++)
            {
                actionsCopy[i] = actions[i];
                if (actionsCopy[i] == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Parallel_Invoke_ActionNull"));
                }
            }

            int forkJoinContextID = 0;
            Task callerTask = null;
            if (TplEtwProvider.Log.IsEnabled())
            {
                forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
                callerTask = Task.InternalCurrent;
                TplEtwProvider.Log.ParallelInvokeBegin((callerTask != null ? callerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (callerTask != null ? callerTask.Id : 0), forkJoinContextID, TplEtwProvider.ForkJoinOperationType.ParallelInvoke, actionsCopy.Length);
            }

            actions = null;
            if (actionsCopy.Length < 1)
                return;
            const int SMALL_ACTIONCOUNT_LIMIT = 10;
            try
            {
                if ((actionsCopy.Length > SMALL_ACTIONCOUNT_LIMIT) || (parallelOptions.MaxDegreeOfParallelism != -1 && parallelOptions.MaxDegreeOfParallelism < actionsCopy.Length))
                {
                    ConcurrentQueue<Exception> exceptionQ = null;
                    try
                    {
                        int actionIndex = 0;
                        ParallelForReplicatingTask rootTask = new ParallelForReplicatingTask(parallelOptions, delegate
                        {
                            int myIndex = Interlocked.Increment(ref actionIndex);
                            while (myIndex <= actionsCopy.Length)
                            {
                                try
                                {
                                    actionsCopy[myIndex - 1]();
                                }
                                catch (Exception e)
                                {
                                    LazyInitializer.EnsureInitialized<ConcurrentQueue<Exception>>(ref exceptionQ, () =>
                                    {
                                        return new ConcurrentQueue<Exception>();
                                    }

                                    );
                                    exceptionQ.Enqueue(e);
                                }

                                if (parallelOptions.CancellationToken.IsCancellationRequested)
                                    throw new OperationCanceledException(parallelOptions.CancellationToken);
                                myIndex = Interlocked.Increment(ref actionIndex);
                            }
                        }

                        , TaskCreationOptions.None, InternalTaskOptions.SelfReplicating);
                        rootTask.RunSynchronously(parallelOptions.EffectiveTaskScheduler);
                        rootTask.Wait();
                    }
                    catch (Exception e)
                    {
                        LazyInitializer.EnsureInitialized<ConcurrentQueue<Exception>>(ref exceptionQ, () =>
                        {
                            return new ConcurrentQueue<Exception>();
                        }

                        );
                        AggregateException ae = e as AggregateException;
                        if (ae != null)
                        {
                            foreach (Exception exc in ae.InnerExceptions)
                                exceptionQ.Enqueue(exc);
                        }
                        else
                        {
                            exceptionQ.Enqueue(e);
                        }
                    }

                    if ((exceptionQ != null) && (exceptionQ.Count > 0))
                    {
                        ThrowIfReducableToSingleOCE(exceptionQ, parallelOptions.CancellationToken);
                        throw new AggregateException(exceptionQ);
                    }
                }
                else
                {
                    Task[] tasks = new Task[actionsCopy.Length];
                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(parallelOptions.CancellationToken);
                    for (int i = 1; i < tasks.Length; i++)
                    {
                        tasks[i] = Task.Factory.StartNew(actionsCopy[i], parallelOptions.CancellationToken, TaskCreationOptions.None, InternalTaskOptions.None, parallelOptions.EffectiveTaskScheduler);
                    }

                    tasks[0] = new Task(actionsCopy[0]);
                    tasks[0].RunSynchronously(parallelOptions.EffectiveTaskScheduler);
                    try
                    {
                        if (tasks.Length <= 4)
                        {
                            Task.FastWaitAll(tasks);
                        }
                        else
                        {
                            Task.WaitAll(tasks);
                        }
                    }
                    catch (AggregateException aggExp)
                    {
                        ThrowIfReducableToSingleOCE(aggExp.InnerExceptions, parallelOptions.CancellationToken);
                        throw;
                    }
                    finally
                    {
                        for (int i = 0; i < tasks.Length; i++)
                        {
                            if (tasks[i].IsCompleted)
                                tasks[i].Dispose();
                        }
                    }
                }
            }
            finally
            {
                if (TplEtwProvider.Log.IsEnabled())
                {
                    TplEtwProvider.Log.ParallelInvokeEnd((callerTask != null ? callerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (callerTask != null ? callerTask.Id : 0), forkJoinContextID);
                }
            }
        }

        public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return ForWorker<object>(fromInclusive, toExclusive, s_defaultParallelOptions, body, null, null, null, null);
        }

        public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return ForWorker64<object>(fromInclusive, toExclusive, s_defaultParallelOptions, body, null, null, null, null);
        }

        public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForWorker<object>(fromInclusive, toExclusive, parallelOptions, body, null, null, null, null);
        }

        public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForWorker64<object>(fromInclusive, toExclusive, parallelOptions, body, null, null, null, null);
        }

        public static ParallelLoopResult For(int fromInclusive, int toExclusive, Action<int, ParallelLoopState> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return ForWorker<object>(fromInclusive, toExclusive, s_defaultParallelOptions, null, body, null, null, null);
        }

        public static ParallelLoopResult For(long fromInclusive, long toExclusive, Action<long, ParallelLoopState> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return ForWorker64<object>(fromInclusive, toExclusive, s_defaultParallelOptions, null, body, null, null, null);
        }

        public static ParallelLoopResult For(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int, ParallelLoopState> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForWorker<object>(fromInclusive, toExclusive, parallelOptions, null, body, null, null, null);
        }

        public static ParallelLoopResult For(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long, ParallelLoopState> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForWorker64<object>(fromInclusive, toExclusive, parallelOptions, null, body, null, null, null);
        }

        public static ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, Func<TLocal> localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            return ForWorker(fromInclusive, toExclusive, s_defaultParallelOptions, null, null, body, localInit, localFinally);
        }

        public static ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, Func<TLocal> localInit, Func<long, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            return ForWorker64(fromInclusive, toExclusive, s_defaultParallelOptions, null, null, body, localInit, localFinally);
        }

        public static ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<int, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForWorker(fromInclusive, toExclusive, parallelOptions, null, null, body, localInit, localFinally);
        }

        public static ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<long, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForWorker64(fromInclusive, toExclusive, parallelOptions, null, null, body, localInit, localFinally);
        }

        private static ParallelLoopResult ForWorker<TLocal>(int fromInclusive, int toExclusive, ParallelOptions parallelOptions, Action<int> body, Action<int, ParallelLoopState> bodyWithState, Func<int, ParallelLoopState, TLocal, TLocal> bodyWithLocal, Func<TLocal> localInit, Action<TLocal> localFinally)
        {
                                    ParallelLoopResult result = new ParallelLoopResult();
            if (toExclusive <= fromInclusive)
            {
                result.m_completed = true;
                return result;
            }

            ParallelLoopStateFlags32 sharedPStateFlags = new ParallelLoopStateFlags32();
            TaskCreationOptions creationOptions = TaskCreationOptions.None;
            InternalTaskOptions internalOptions = InternalTaskOptions.SelfReplicating;
            if (parallelOptions.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(parallelOptions.CancellationToken);
            }

            int numExpectedWorkers = (parallelOptions.EffectiveMaxConcurrencyLevel == -1) ? PlatformHelper.ProcessorCount : parallelOptions.EffectiveMaxConcurrencyLevel;
            RangeManager rangeManager = new RangeManager(fromInclusive, toExclusive, 1, numExpectedWorkers);
            OperationCanceledException oce = null;
            CancellationTokenRegistration ctr = new CancellationTokenRegistration();
            if (parallelOptions.CancellationToken.CanBeCanceled)
            {
                ctr = parallelOptions.CancellationToken.InternalRegisterWithoutEC((o) =>
                {
                    sharedPStateFlags.Cancel();
                    oce = new OperationCanceledException(parallelOptions.CancellationToken);
                }

                , null);
            }

            int forkJoinContextID = 0;
            Task callingTask = null;
            if (TplEtwProvider.Log.IsEnabled())
            {
                forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
                callingTask = Task.InternalCurrent;
                TplEtwProvider.Log.ParallelLoopBegin((callingTask != null ? callingTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (callingTask != null ? callingTask.Id : 0), forkJoinContextID, TplEtwProvider.ForkJoinOperationType.ParallelFor, fromInclusive, toExclusive);
            }

            ParallelForReplicatingTask rootTask = null;
            try
            {
                rootTask = new ParallelForReplicatingTask(parallelOptions, delegate
                {
                    Task currentWorkerTask = Task.InternalCurrent;
                    bool bIsRootTask = (currentWorkerTask == rootTask);
                    RangeWorker currentWorker = new RangeWorker();
                    Object savedStateFromPreviousReplica = currentWorkerTask.SavedStateFromPreviousReplica;
                    if (savedStateFromPreviousReplica is RangeWorker)
                        currentWorker = (RangeWorker)savedStateFromPreviousReplica;
                    else
                        currentWorker = rangeManager.RegisterNewWorker();
                    int nFromInclusiveLocal;
                    int nToExclusiveLocal;
                    if (currentWorker.FindNewWork32(out nFromInclusiveLocal, out nToExclusiveLocal) == false || sharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal) == true)
                    {
                        return;
                    }

                    if (TplEtwProvider.Log.IsEnabled())
                    {
                        TplEtwProvider.Log.ParallelFork((currentWorkerTask != null ? currentWorkerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (currentWorkerTask != null ? currentWorkerTask.Id : 0), forkJoinContextID);
                    }

                    TLocal localValue = default (TLocal);
                    bool bLocalValueInitialized = false;
                    try
                    {
                        ParallelLoopState32 state = null;
                        if (bodyWithState != null)
                        {
                                                        state = new ParallelLoopState32(sharedPStateFlags);
                        }
                        else if (bodyWithLocal != null)
                        {
                                                        state = new ParallelLoopState32(sharedPStateFlags);
                            if (localInit != null)
                            {
                                localValue = localInit();
                                bLocalValueInitialized = true;
                            }
                        }

                        LoopTimer loopTimer = new LoopTimer(rootTask.ActiveChildCount);
                        do
                        {
                            if (body != null)
                            {
                                for (int j = nFromInclusiveLocal; j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.PLS_NONE || !sharedPStateFlags.ShouldExitLoop()); j += 1)
                                {
                                    body(j);
                                }
                            }
                            else if (bodyWithState != null)
                            {
                                for (int j = nFromInclusiveLocal; j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.PLS_NONE || !sharedPStateFlags.ShouldExitLoop(j)); j += 1)
                                {
                                    state.CurrentIteration = j;
                                    bodyWithState(j, state);
                                }
                            }
                            else
                            {
                                for (int j = nFromInclusiveLocal; j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.PLS_NONE || !sharedPStateFlags.ShouldExitLoop(j)); j += 1)
                                {
                                    state.CurrentIteration = j;
                                    localValue = bodyWithLocal(j, state, localValue);
                                }
                            }

                            if (!bIsRootTask && loopTimer.LimitExceeded())
                            {
                                currentWorkerTask.SavedStateForNextReplica = (object)currentWorker;
                                break;
                            }
                        }
                        while (currentWorker.FindNewWork32(out nFromInclusiveLocal, out nToExclusiveLocal) && ((sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.PLS_NONE) || !sharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal)));
                    }
                    catch
                    {
                        sharedPStateFlags.SetExceptional();
                        throw;
                    }
                    finally
                    {
                        if (localFinally != null && bLocalValueInitialized)
                        {
                            localFinally(localValue);
                        }

                        if (TplEtwProvider.Log.IsEnabled())
                        {
                            TplEtwProvider.Log.ParallelJoin((currentWorkerTask != null ? currentWorkerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (currentWorkerTask != null ? currentWorkerTask.Id : 0), forkJoinContextID);
                        }
                    }
                }

                , creationOptions, internalOptions);
                rootTask.RunSynchronously(parallelOptions.EffectiveTaskScheduler);
                rootTask.Wait();
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                if (oce != null)
                    throw oce;
            }
            catch (AggregateException aggExp)
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                ThrowIfReducableToSingleOCE(aggExp.InnerExceptions, parallelOptions.CancellationToken);
                throw;
            }
            catch (TaskSchedulerException)
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                throw;
            }
            finally
            {
                int sb_status = sharedPStateFlags.LoopStateFlags;
                result.m_completed = (sb_status == ParallelLoopStateFlags.PLS_NONE);
                if ((sb_status & ParallelLoopStateFlags.PLS_BROKEN) != 0)
                {
                    result.m_lowestBreakIteration = sharedPStateFlags.LowestBreakIteration;
                }

                if ((rootTask != null) && rootTask.IsCompleted)
                    rootTask.Dispose();
                if (TplEtwProvider.Log.IsEnabled())
                {
                    int nTotalIterations = 0;
                    if (sb_status == ParallelLoopStateFlags.PLS_NONE)
                        nTotalIterations = toExclusive - fromInclusive;
                    else if ((sb_status & ParallelLoopStateFlags.PLS_BROKEN) != 0)
                        nTotalIterations = sharedPStateFlags.LowestBreakIteration - fromInclusive;
                    else
                        nTotalIterations = -1;
                    TplEtwProvider.Log.ParallelLoopEnd((callingTask != null ? callingTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (callingTask != null ? callingTask.Id : 0), forkJoinContextID, nTotalIterations);
                }
            }

            return result;
        }

        private static ParallelLoopResult ForWorker64<TLocal>(long fromInclusive, long toExclusive, ParallelOptions parallelOptions, Action<long> body, Action<long, ParallelLoopState> bodyWithState, Func<long, ParallelLoopState, TLocal, TLocal> bodyWithLocal, Func<TLocal> localInit, Action<TLocal> localFinally)
        {
                                    ParallelLoopResult result = new ParallelLoopResult();
            if (toExclusive <= fromInclusive)
            {
                result.m_completed = true;
                return result;
            }

            ParallelLoopStateFlags64 sharedPStateFlags = new ParallelLoopStateFlags64();
            TaskCreationOptions creationOptions = TaskCreationOptions.None;
            InternalTaskOptions internalOptions = InternalTaskOptions.SelfReplicating;
            if (parallelOptions.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(parallelOptions.CancellationToken);
            }

            int numExpectedWorkers = (parallelOptions.EffectiveMaxConcurrencyLevel == -1) ? PlatformHelper.ProcessorCount : parallelOptions.EffectiveMaxConcurrencyLevel;
            RangeManager rangeManager = new RangeManager(fromInclusive, toExclusive, 1, numExpectedWorkers);
            OperationCanceledException oce = null;
            CancellationTokenRegistration ctr = new CancellationTokenRegistration();
            if (parallelOptions.CancellationToken.CanBeCanceled)
            {
                ctr = parallelOptions.CancellationToken.InternalRegisterWithoutEC((o) =>
                {
                    sharedPStateFlags.Cancel();
                    oce = new OperationCanceledException(parallelOptions.CancellationToken);
                }

                , null);
            }

            Task callerTask = null;
            int forkJoinContextID = 0;
            if (TplEtwProvider.Log.IsEnabled())
            {
                forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
                callerTask = Task.InternalCurrent;
                TplEtwProvider.Log.ParallelLoopBegin((callerTask != null ? callerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (callerTask != null ? callerTask.Id : 0), forkJoinContextID, TplEtwProvider.ForkJoinOperationType.ParallelFor, fromInclusive, toExclusive);
            }

            ParallelForReplicatingTask rootTask = null;
            try
            {
                rootTask = new ParallelForReplicatingTask(parallelOptions, delegate
                {
                    Task currentWorkerTask = Task.InternalCurrent;
                    bool bIsRootTask = (currentWorkerTask == rootTask);
                    RangeWorker currentWorker = new RangeWorker();
                    Object savedStateFromPreviousReplica = currentWorkerTask.SavedStateFromPreviousReplica;
                    if (savedStateFromPreviousReplica is RangeWorker)
                        currentWorker = (RangeWorker)savedStateFromPreviousReplica;
                    else
                        currentWorker = rangeManager.RegisterNewWorker();
                    long nFromInclusiveLocal;
                    long nToExclusiveLocal;
                    if (currentWorker.FindNewWork(out nFromInclusiveLocal, out nToExclusiveLocal) == false || sharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal) == true)
                    {
                        return;
                    }

                    if (TplEtwProvider.Log.IsEnabled())
                    {
                        TplEtwProvider.Log.ParallelFork((currentWorkerTask != null ? currentWorkerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (currentWorkerTask != null ? currentWorkerTask.Id : 0), forkJoinContextID);
                    }

                    TLocal localValue = default (TLocal);
                    bool bLocalValueInitialized = false;
                    try
                    {
                        ParallelLoopState64 state = null;
                        if (bodyWithState != null)
                        {
                                                        state = new ParallelLoopState64(sharedPStateFlags);
                        }
                        else if (bodyWithLocal != null)
                        {
                                                        state = new ParallelLoopState64(sharedPStateFlags);
                            if (localInit != null)
                            {
                                localValue = localInit();
                                bLocalValueInitialized = true;
                            }
                        }

                        LoopTimer loopTimer = new LoopTimer(rootTask.ActiveChildCount);
                        do
                        {
                            if (body != null)
                            {
                                for (long j = nFromInclusiveLocal; j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.PLS_NONE || !sharedPStateFlags.ShouldExitLoop()); j += 1)
                                {
                                    body(j);
                                }
                            }
                            else if (bodyWithState != null)
                            {
                                for (long j = nFromInclusiveLocal; j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.PLS_NONE || !sharedPStateFlags.ShouldExitLoop(j)); j += 1)
                                {
                                    state.CurrentIteration = j;
                                    bodyWithState(j, state);
                                }
                            }
                            else
                            {
                                for (long j = nFromInclusiveLocal; j < nToExclusiveLocal && (sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.PLS_NONE || !sharedPStateFlags.ShouldExitLoop(j)); j += 1)
                                {
                                    state.CurrentIteration = j;
                                    localValue = bodyWithLocal(j, state, localValue);
                                }
                            }

                            if (!bIsRootTask && loopTimer.LimitExceeded())
                            {
                                currentWorkerTask.SavedStateForNextReplica = (object)currentWorker;
                                break;
                            }
                        }
                        while (currentWorker.FindNewWork(out nFromInclusiveLocal, out nToExclusiveLocal) && ((sharedPStateFlags.LoopStateFlags == ParallelLoopStateFlags.PLS_NONE) || !sharedPStateFlags.ShouldExitLoop(nFromInclusiveLocal)));
                    }
                    catch
                    {
                        sharedPStateFlags.SetExceptional();
                        throw;
                    }
                    finally
                    {
                        if (localFinally != null && bLocalValueInitialized)
                        {
                            localFinally(localValue);
                        }

                        if (TplEtwProvider.Log.IsEnabled())
                        {
                            TplEtwProvider.Log.ParallelJoin((currentWorkerTask != null ? currentWorkerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (currentWorkerTask != null ? currentWorkerTask.Id : 0), forkJoinContextID);
                        }
                    }
                }

                , creationOptions, internalOptions);
                rootTask.RunSynchronously(parallelOptions.EffectiveTaskScheduler);
                rootTask.Wait();
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                if (oce != null)
                    throw oce;
            }
            catch (AggregateException aggExp)
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                ThrowIfReducableToSingleOCE(aggExp.InnerExceptions, parallelOptions.CancellationToken);
                throw;
            }
            catch (TaskSchedulerException)
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                throw;
            }
            finally
            {
                int sb_status = sharedPStateFlags.LoopStateFlags;
                result.m_completed = (sb_status == ParallelLoopStateFlags.PLS_NONE);
                if ((sb_status & ParallelLoopStateFlags.PLS_BROKEN) != 0)
                {
                    result.m_lowestBreakIteration = sharedPStateFlags.LowestBreakIteration;
                }

                if ((rootTask != null) && rootTask.IsCompleted)
                    rootTask.Dispose();
                if (TplEtwProvider.Log.IsEnabled())
                {
                    long nTotalIterations = 0;
                    if (sb_status == ParallelLoopStateFlags.PLS_NONE)
                        nTotalIterations = toExclusive - fromInclusive;
                    else if ((sb_status & ParallelLoopStateFlags.PLS_BROKEN) != 0)
                        nTotalIterations = sharedPStateFlags.LowestBreakIteration - fromInclusive;
                    else
                        nTotalIterations = -1;
                    TplEtwProvider.Log.ParallelLoopEnd((callerTask != null ? callerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (callerTask != null ? callerTask.Id : 0), forkJoinContextID, nTotalIterations);
                }
            }

            return result;
        }

        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return ForEachWorker<TSource, object>(source, s_defaultParallelOptions, body, null, null, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForEachWorker<TSource, object>(source, parallelOptions, body, null, null, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return ForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, body, null, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForEachWorker<TSource, object>(source, parallelOptions, null, body, null, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, Action<TSource, ParallelLoopState, long> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return ForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, null, body, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState, long> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForEachWorker<TSource, object>(source, parallelOptions, null, null, body, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            return ForEachWorker<TSource, TLocal>(source, s_defaultParallelOptions, null, null, null, body, null, localInit, localFinally);
        }

        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForEachWorker<TSource, TLocal>(source, parallelOptions, null, null, null, body, null, localInit, localFinally);
        }

        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            return ForEachWorker<TSource, TLocal>(source, s_defaultParallelOptions, null, null, null, null, body, localInit, localFinally);
        }

        public static ParallelLoopResult ForEach<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return ForEachWorker<TSource, TLocal>(source, parallelOptions, null, null, null, null, body, localInit, localFinally);
        }

        private static ParallelLoopResult ForEachWorker<TSource, TLocal>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Action<TSource> body, Action<TSource, ParallelLoopState> bodyWithState, Action<TSource, ParallelLoopState, long> bodyWithStateAndIndex, Func<TSource, ParallelLoopState, TLocal, TLocal> bodyWithStateAndLocal, Func<TSource, ParallelLoopState, long, TLocal, TLocal> bodyWithEverything, Func<TLocal> localInit, Action<TLocal> localFinally)
        {
                                    if (parallelOptions.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(parallelOptions.CancellationToken);
            }

            TSource[] sourceAsArray = source as TSource[];
            if (sourceAsArray != null)
            {
                return ForEachWorker<TSource, TLocal>(sourceAsArray, parallelOptions, body, bodyWithState, bodyWithStateAndIndex, bodyWithStateAndLocal, bodyWithEverything, localInit, localFinally);
            }

            IList<TSource> sourceAsList = source as IList<TSource>;
            if (sourceAsList != null)
            {
                return ForEachWorker<TSource, TLocal>(sourceAsList, parallelOptions, body, bodyWithState, bodyWithStateAndIndex, bodyWithStateAndLocal, bodyWithEverything, localInit, localFinally);
            }

            return PartitionerForEachWorker<TSource, TLocal>(Partitioner.Create(source), parallelOptions, body, bodyWithState, bodyWithStateAndIndex, bodyWithStateAndLocal, bodyWithEverything, localInit, localFinally);
        }

        private static ParallelLoopResult ForEachWorker<TSource, TLocal>(TSource[] array, ParallelOptions parallelOptions, Action<TSource> body, Action<TSource, ParallelLoopState> bodyWithState, Action<TSource, ParallelLoopState, long> bodyWithStateAndIndex, Func<TSource, ParallelLoopState, TLocal, TLocal> bodyWithStateAndLocal, Func<TSource, ParallelLoopState, long, TLocal, TLocal> bodyWithEverything, Func<TLocal> localInit, Action<TLocal> localFinally)
        {
                                    int from = array.GetLowerBound(0);
            int to = array.GetUpperBound(0) + 1;
            if (body != null)
            {
                return ForWorker<object>(from, to, parallelOptions, (i) => body(array[i]), null, null, null, null);
            }
            else if (bodyWithState != null)
            {
                return ForWorker<object>(from, to, parallelOptions, null, (i, state) => bodyWithState(array[i], state), null, null, null);
            }
            else if (bodyWithStateAndIndex != null)
            {
                return ForWorker<object>(from, to, parallelOptions, null, (i, state) => bodyWithStateAndIndex(array[i], state, i), null, null, null);
            }
            else if (bodyWithStateAndLocal != null)
            {
                return ForWorker<TLocal>(from, to, parallelOptions, null, null, (i, state, local) => bodyWithStateAndLocal(array[i], state, local), localInit, localFinally);
            }
            else
            {
                return ForWorker<TLocal>(from, to, parallelOptions, null, null, (i, state, local) => bodyWithEverything(array[i], state, i, local), localInit, localFinally);
            }
        }

        private static ParallelLoopResult ForEachWorker<TSource, TLocal>(IList<TSource> list, ParallelOptions parallelOptions, Action<TSource> body, Action<TSource, ParallelLoopState> bodyWithState, Action<TSource, ParallelLoopState, long> bodyWithStateAndIndex, Func<TSource, ParallelLoopState, TLocal, TLocal> bodyWithStateAndLocal, Func<TSource, ParallelLoopState, long, TLocal, TLocal> bodyWithEverything, Func<TLocal> localInit, Action<TLocal> localFinally)
        {
                                    if (body != null)
            {
                return ForWorker<object>(0, list.Count, parallelOptions, (i) => body(list[i]), null, null, null, null);
            }
            else if (bodyWithState != null)
            {
                return ForWorker<object>(0, list.Count, parallelOptions, null, (i, state) => bodyWithState(list[i], state), null, null, null);
            }
            else if (bodyWithStateAndIndex != null)
            {
                return ForWorker<object>(0, list.Count, parallelOptions, null, (i, state) => bodyWithStateAndIndex(list[i], state, i), null, null, null);
            }
            else if (bodyWithStateAndLocal != null)
            {
                return ForWorker<TLocal>(0, list.Count, parallelOptions, null, null, (i, state, local) => bodyWithStateAndLocal(list[i], state, local), localInit, localFinally);
            }
            else
            {
                return ForWorker<TLocal>(0, list.Count, parallelOptions, null, null, (i, state, local) => bodyWithEverything(list[i], state, i, local), localInit, localFinally);
            }
        }

        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, body, null, null, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource, ParallelLoopState> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, body, null, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, Action<TSource, ParallelLoopState, long> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_OrderedPartitionerKeysNotNormalized"));
            }

            return PartitionerForEachWorker<TSource, object>(source, s_defaultParallelOptions, null, null, body, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            return PartitionerForEachWorker<TSource, TLocal>(source, s_defaultParallelOptions, null, null, null, body, null, localInit, localFinally);
        }

        public static ParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_OrderedPartitionerKeysNotNormalized"));
            }

            return PartitionerForEachWorker<TSource, TLocal>(source, s_defaultParallelOptions, null, null, null, null, body, localInit, localFinally);
        }

        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return PartitionerForEachWorker<TSource, object>(source, parallelOptions, body, null, null, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return PartitionerForEachWorker<TSource, object>(source, parallelOptions, null, body, null, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource, ParallelLoopState, long> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_OrderedPartitionerKeysNotNormalized"));
            }

            return PartitionerForEachWorker<TSource, object>(source, parallelOptions, null, null, body, null, null, null, null);
        }

        public static ParallelLoopResult ForEach<TSource, TLocal>(Partitioner<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            return PartitionerForEachWorker<TSource, TLocal>(source, parallelOptions, null, null, null, body, null, localInit, localFinally);
        }

        public static ParallelLoopResult ForEach<TSource, TLocal>(OrderablePartitioner<TSource> source, ParallelOptions parallelOptions, Func<TLocal> localInit, Func<TSource, ParallelLoopState, long, TLocal, TLocal> body, Action<TLocal> localFinally)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            if (localInit == null)
            {
                throw new ArgumentNullException("localInit");
            }

            if (localFinally == null)
            {
                throw new ArgumentNullException("localFinally");
            }

            if (parallelOptions == null)
            {
                throw new ArgumentNullException("parallelOptions");
            }

            if (!source.KeysNormalized)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_OrderedPartitionerKeysNotNormalized"));
            }

            return PartitionerForEachWorker<TSource, TLocal>(source, parallelOptions, null, null, null, null, body, localInit, localFinally);
        }

        private static ParallelLoopResult PartitionerForEachWorker<TSource, TLocal>(Partitioner<TSource> source, ParallelOptions parallelOptions, Action<TSource> simpleBody, Action<TSource, ParallelLoopState> bodyWithState, Action<TSource, ParallelLoopState, long> bodyWithStateAndIndex, Func<TSource, ParallelLoopState, TLocal, TLocal> bodyWithStateAndLocal, Func<TSource, ParallelLoopState, long, TLocal, TLocal> bodyWithEverything, Func<TLocal> localInit, Action<TLocal> localFinally)
        {
                                    OrderablePartitioner<TSource> orderedSource = source as OrderablePartitioner<TSource>;
                        if (!source.SupportsDynamicPartitions)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_PartitionerNotDynamic"));
            }

            if (parallelOptions.CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(parallelOptions.CancellationToken);
            }

            int forkJoinContextID = 0;
            Task callerTask = null;
            if (TplEtwProvider.Log.IsEnabled())
            {
                forkJoinContextID = Interlocked.Increment(ref s_forkJoinContextID);
                callerTask = Task.InternalCurrent;
                TplEtwProvider.Log.ParallelLoopBegin((callerTask != null ? callerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (callerTask != null ? callerTask.Id : 0), forkJoinContextID, TplEtwProvider.ForkJoinOperationType.ParallelForEach, 0, 0);
            }

            ParallelLoopStateFlags64 sharedPStateFlags = new ParallelLoopStateFlags64();
            ParallelLoopResult result = new ParallelLoopResult();
            OperationCanceledException oce = null;
            CancellationTokenRegistration ctr = new CancellationTokenRegistration();
            if (parallelOptions.CancellationToken.CanBeCanceled)
            {
                ctr = parallelOptions.CancellationToken.InternalRegisterWithoutEC((o) =>
                {
                    sharedPStateFlags.Cancel();
                    oce = new OperationCanceledException(parallelOptions.CancellationToken);
                }

                , null);
            }

            IEnumerable<TSource> partitionerSource = null;
            IEnumerable<KeyValuePair<long, TSource>> orderablePartitionerSource = null;
            if (orderedSource != null)
            {
                orderablePartitionerSource = orderedSource.GetOrderableDynamicPartitions();
                if (orderablePartitionerSource == null)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_PartitionerReturnedNull"));
                }
            }
            else
            {
                partitionerSource = source.GetDynamicPartitions();
                if (partitionerSource == null)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_PartitionerReturnedNull"));
                }
            }

            ParallelForReplicatingTask rootTask = null;
            Action partitionAction = delegate
            {
                Task currentWorkerTask = Task.InternalCurrent;
                if (TplEtwProvider.Log.IsEnabled())
                {
                    TplEtwProvider.Log.ParallelFork((currentWorkerTask != null ? currentWorkerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (currentWorkerTask != null ? currentWorkerTask.Id : 0), forkJoinContextID);
                }

                TLocal localValue = default (TLocal);
                bool bLocalValueInitialized = false;
                IDisposable myPartitionToDispose = null;
                try
                {
                    ParallelLoopState64 state = null;
                    if (bodyWithState != null || bodyWithStateAndIndex != null)
                    {
                        state = new ParallelLoopState64(sharedPStateFlags);
                    }
                    else if (bodyWithStateAndLocal != null || bodyWithEverything != null)
                    {
                        state = new ParallelLoopState64(sharedPStateFlags);
                        if (localInit != null)
                        {
                            localValue = localInit();
                            bLocalValueInitialized = true;
                        }
                    }

                    bool bIsRootTask = (rootTask == currentWorkerTask);
                    LoopTimer loopTimer = new LoopTimer(rootTask.ActiveChildCount);
                    if (orderedSource != null)
                    {
                        IEnumerator<KeyValuePair<long, TSource>> myPartition = currentWorkerTask.SavedStateFromPreviousReplica as IEnumerator<KeyValuePair<long, TSource>>;
                        if (myPartition == null)
                        {
                            myPartition = orderablePartitionerSource.GetEnumerator();
                            if (myPartition == null)
                            {
                                throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_NullEnumerator"));
                            }
                        }

                        myPartitionToDispose = myPartition;
                        while (myPartition.MoveNext())
                        {
                            KeyValuePair<long, TSource> kvp = myPartition.Current;
                            long index = kvp.Key;
                            TSource value = kvp.Value;
                            if (state != null)
                                state.CurrentIteration = index;
                            if (simpleBody != null)
                                simpleBody(value);
                            else if (bodyWithState != null)
                                bodyWithState(value, state);
                            else if (bodyWithStateAndIndex != null)
                                bodyWithStateAndIndex(value, state, index);
                            else if (bodyWithStateAndLocal != null)
                                localValue = bodyWithStateAndLocal(value, state, localValue);
                            else
                                localValue = bodyWithEverything(value, state, index, localValue);
                            if (sharedPStateFlags.ShouldExitLoop(index))
                                break;
                            if (!bIsRootTask && loopTimer.LimitExceeded())
                            {
                                currentWorkerTask.SavedStateForNextReplica = myPartition;
                                myPartitionToDispose = null;
                                break;
                            }
                        }
                    }
                    else
                    {
                        IEnumerator<TSource> myPartition = currentWorkerTask.SavedStateFromPreviousReplica as IEnumerator<TSource>;
                        if (myPartition == null)
                        {
                            myPartition = partitionerSource.GetEnumerator();
                            if (myPartition == null)
                            {
                                throw new InvalidOperationException(Environment.GetResourceString("Parallel_ForEach_NullEnumerator"));
                            }
                        }

                        myPartitionToDispose = myPartition;
                        if (state != null)
                            state.CurrentIteration = 0;
                        while (myPartition.MoveNext())
                        {
                            TSource t = myPartition.Current;
                            if (simpleBody != null)
                                simpleBody(t);
                            else if (bodyWithState != null)
                                bodyWithState(t, state);
                            else if (bodyWithStateAndLocal != null)
                                localValue = bodyWithStateAndLocal(t, state, localValue);
                            else
                                                            if (sharedPStateFlags.LoopStateFlags != ParallelLoopStateFlags.PLS_NONE)
                                break;
                            if (!bIsRootTask && loopTimer.LimitExceeded())
                            {
                                currentWorkerTask.SavedStateForNextReplica = myPartition;
                                myPartitionToDispose = null;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    sharedPStateFlags.SetExceptional();
                    throw;
                }
                finally
                {
                    if (localFinally != null && bLocalValueInitialized)
                    {
                        localFinally(localValue);
                    }

                    if (myPartitionToDispose != null)
                    {
                        myPartitionToDispose.Dispose();
                    }

                    if (TplEtwProvider.Log.IsEnabled())
                    {
                        TplEtwProvider.Log.ParallelJoin((currentWorkerTask != null ? currentWorkerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (currentWorkerTask != null ? currentWorkerTask.Id : 0), forkJoinContextID);
                    }
                }
            }

            ;
            try
            {
                rootTask = new ParallelForReplicatingTask(parallelOptions, partitionAction, TaskCreationOptions.None, InternalTaskOptions.SelfReplicating);
                rootTask.RunSynchronously(parallelOptions.EffectiveTaskScheduler);
                rootTask.Wait();
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                if (oce != null)
                    throw oce;
            }
            catch (AggregateException aggExp)
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                ThrowIfReducableToSingleOCE(aggExp.InnerExceptions, parallelOptions.CancellationToken);
                throw;
            }
            catch (TaskSchedulerException)
            {
                if (parallelOptions.CancellationToken.CanBeCanceled)
                {
                    ctr.Dispose();
                }

                throw;
            }
            finally
            {
                int sb_status = sharedPStateFlags.LoopStateFlags;
                result.m_completed = (sb_status == ParallelLoopStateFlags.PLS_NONE);
                if ((sb_status & ParallelLoopStateFlags.PLS_BROKEN) != 0)
                {
                    result.m_lowestBreakIteration = sharedPStateFlags.LowestBreakIteration;
                }

                if ((rootTask != null) && rootTask.IsCompleted)
                    rootTask.Dispose();
                IDisposable d = null;
                if (orderablePartitionerSource != null)
                {
                    d = orderablePartitionerSource as IDisposable;
                }
                else
                {
                    d = partitionerSource as IDisposable;
                }

                if (d != null)
                {
                    d.Dispose();
                }

                if (TplEtwProvider.Log.IsEnabled())
                {
                    TplEtwProvider.Log.ParallelLoopEnd((callerTask != null ? callerTask.m_taskScheduler.Id : TaskScheduler.Current.Id), (callerTask != null ? callerTask.Id : 0), forkJoinContextID, 0);
                }
            }

            return result;
        }

        internal static void ThrowIfReducableToSingleOCE(IEnumerable<Exception> excCollection, CancellationToken ct)
        {
            bool bCollectionNotZeroLength = false;
            if (ct.IsCancellationRequested)
            {
                foreach (Exception e in excCollection)
                {
                    bCollectionNotZeroLength = true;
                    OperationCanceledException oce = e as OperationCanceledException;
                    if (oce == null || oce.CancellationToken != ct)
                    {
                        return;
                    }
                }

                if (bCollectionNotZeroLength)
                    throw new OperationCanceledException(ct);
            }
        }

        internal struct LoopTimer
        {
            public LoopTimer(int nWorkerTaskIndex)
            {
                int timeOut = s_BaseNotifyPeriodMS + (nWorkerTaskIndex % PlatformHelper.ProcessorCount) * s_NotifyPeriodIncrementMS;
                m_timeLimit = Environment.TickCount + timeOut;
            }

            public bool LimitExceeded()
            {
                                return (Environment.TickCount > m_timeLimit);
            }

            const int s_BaseNotifyPeriodMS = 100;
            const int s_NotifyPeriodIncrementMS = 50;
            private int m_timeLimit;
        }
    }
}