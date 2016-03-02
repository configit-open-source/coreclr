

namespace System.Threading.Tasks
{
    public class ParallelLoopState
    {
        private ParallelLoopStateFlags m_flagsBase;
        internal ParallelLoopState(ParallelLoopStateFlags fbase)
        {
            m_flagsBase = fbase;
        }

        internal virtual bool InternalShouldExitCurrentIteration
        {
            get
            {
                                throw new NotSupportedException(Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
            }
        }

        public bool ShouldExitCurrentIteration
        {
            get
            {
                return InternalShouldExitCurrentIteration;
            }
        }

        public bool IsStopped
        {
            get
            {
                return ((m_flagsBase.LoopStateFlags & ParallelLoopStateFlags.PLS_STOPPED) != 0);
            }
        }

        public bool IsExceptional
        {
            get
            {
                return ((m_flagsBase.LoopStateFlags & ParallelLoopStateFlags.PLS_EXCEPTIONAL) != 0);
            }
        }

        internal virtual long ? InternalLowestBreakIteration
        {
            get
            {
                                throw new NotSupportedException(Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
            }
        }

        public long ? LowestBreakIteration
        {
            get
            {
                return InternalLowestBreakIteration;
            }
        }

        public void Stop()
        {
            m_flagsBase.Stop();
        }

        internal virtual void InternalBreak()
        {
                        throw new NotSupportedException(Environment.GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod"));
        }

        public void Break()
        {
            InternalBreak();
        }

        internal static void Break(int iteration, ParallelLoopStateFlags32 pflags)
        {
            int oldValue = ParallelLoopStateFlags.PLS_NONE;
            if (!pflags.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_BROKEN, ParallelLoopStateFlags.PLS_STOPPED | ParallelLoopStateFlags.PLS_EXCEPTIONAL | ParallelLoopStateFlags.PLS_CANCELED, ref oldValue))
            {
                if ((oldValue & ParallelLoopStateFlags.PLS_STOPPED) != 0)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop"));
                }
                else
                {
                    return;
                }
            }

            int oldLBI = pflags.m_lowestBreakIteration;
            if (iteration < oldLBI)
            {
                SpinWait wait = new SpinWait();
                while (Interlocked.CompareExchange(ref pflags.m_lowestBreakIteration, iteration, oldLBI) != oldLBI)
                {
                    wait.SpinOnce();
                    oldLBI = pflags.m_lowestBreakIteration;
                    if (iteration > oldLBI)
                        break;
                }
            }
        }

        internal static void Break(long iteration, ParallelLoopStateFlags64 pflags)
        {
            int oldValue = ParallelLoopStateFlags.PLS_NONE;
            if (!pflags.AtomicLoopStateUpdate(ParallelLoopStateFlags.PLS_BROKEN, ParallelLoopStateFlags.PLS_STOPPED | ParallelLoopStateFlags.PLS_EXCEPTIONAL | ParallelLoopStateFlags.PLS_CANCELED, ref oldValue))
            {
                if ((oldValue & ParallelLoopStateFlags.PLS_STOPPED) != 0)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop"));
                }
                else
                {
                    return;
                }
            }

            long oldLBI = pflags.LowestBreakIteration;
            if (iteration < oldLBI)
            {
                SpinWait wait = new SpinWait();
                while (Interlocked.CompareExchange(ref pflags.m_lowestBreakIteration, iteration, oldLBI) != oldLBI)
                {
                    wait.SpinOnce();
                    oldLBI = pflags.LowestBreakIteration;
                    if (iteration > oldLBI)
                        break;
                }
            }
        }
    }

    internal class ParallelLoopState32 : ParallelLoopState
    {
        private ParallelLoopStateFlags32 m_sharedParallelStateFlags;
        private int m_currentIteration = 0;
        internal ParallelLoopState32(ParallelLoopStateFlags32 sharedParallelStateFlags): base (sharedParallelStateFlags)
        {
            m_sharedParallelStateFlags = sharedParallelStateFlags;
        }

        internal int CurrentIteration
        {
            get
            {
                return m_currentIteration;
            }

            set
            {
                m_currentIteration = value;
            }
        }

        internal override bool InternalShouldExitCurrentIteration
        {
            get
            {
                return m_sharedParallelStateFlags.ShouldExitLoop(CurrentIteration);
            }
        }

        internal override long ? InternalLowestBreakIteration
        {
            get
            {
                return m_sharedParallelStateFlags.NullableLowestBreakIteration;
            }
        }

        internal override void InternalBreak()
        {
            ParallelLoopState.Break(CurrentIteration, m_sharedParallelStateFlags);
        }
    }

    internal class ParallelLoopState64 : ParallelLoopState
    {
        private ParallelLoopStateFlags64 m_sharedParallelStateFlags;
        private long m_currentIteration = 0;
        internal ParallelLoopState64(ParallelLoopStateFlags64 sharedParallelStateFlags): base (sharedParallelStateFlags)
        {
            m_sharedParallelStateFlags = sharedParallelStateFlags;
        }

        internal long CurrentIteration
        {
            get
            {
                return m_currentIteration;
            }

            set
            {
                m_currentIteration = value;
            }
        }

        internal override bool InternalShouldExitCurrentIteration
        {
            get
            {
                return m_sharedParallelStateFlags.ShouldExitLoop(CurrentIteration);
            }
        }

        internal override long ? InternalLowestBreakIteration
        {
            get
            {
                return m_sharedParallelStateFlags.NullableLowestBreakIteration;
            }
        }

        internal override void InternalBreak()
        {
            ParallelLoopState.Break(CurrentIteration, m_sharedParallelStateFlags);
        }
    }

    internal class ParallelLoopStateFlags
    {
        internal static int PLS_NONE;
        internal static int PLS_EXCEPTIONAL = 1;
        internal static int PLS_BROKEN = 2;
        internal static int PLS_STOPPED = 4;
        internal static int PLS_CANCELED = 8;
        private volatile int m_LoopStateFlags = PLS_NONE;
        internal int LoopStateFlags
        {
            get
            {
                return m_LoopStateFlags;
            }
        }

        internal bool AtomicLoopStateUpdate(int newState, int illegalStates)
        {
            int oldState = 0;
            return AtomicLoopStateUpdate(newState, illegalStates, ref oldState);
        }

        internal bool AtomicLoopStateUpdate(int newState, int illegalStates, ref int oldState)
        {
            SpinWait sw = new SpinWait();
            do
            {
                oldState = m_LoopStateFlags;
                if ((oldState & illegalStates) != 0)
                    return false;
                if (Interlocked.CompareExchange(ref m_LoopStateFlags, oldState | newState, oldState) == oldState)
                {
                    return true;
                }

                sw.SpinOnce();
            }
            while (true);
        }

        internal void SetExceptional()
        {
            AtomicLoopStateUpdate(PLS_EXCEPTIONAL, PLS_NONE);
        }

        internal void Stop()
        {
            if (!AtomicLoopStateUpdate(PLS_STOPPED, PLS_BROKEN))
            {
                throw new InvalidOperationException(Environment.GetResourceString("ParallelState_Stop_InvalidOperationException_StopAfterBreak"));
            }
        }

        internal bool Cancel()
        {
            return (AtomicLoopStateUpdate(PLS_CANCELED, PLS_NONE));
        }
    }

    internal class ParallelLoopStateFlags32 : ParallelLoopStateFlags
    {
        internal volatile int m_lowestBreakIteration = Int32.MaxValue;
        internal int LowestBreakIteration
        {
            get
            {
                return m_lowestBreakIteration;
            }
        }

        internal long ? NullableLowestBreakIteration
        {
            get
            {
                if (m_lowestBreakIteration == Int32.MaxValue)
                    return null;
                else
                {
                    long rval = m_lowestBreakIteration;
                    if (IntPtr.Size >= 8)
                        return rval;
                    else
                        return Interlocked.Read(ref rval);
                }
            }
        }

        internal bool ShouldExitLoop(int CallerIteration)
        {
            int flags = LoopStateFlags;
            return (flags != PLS_NONE && (((flags & (PLS_EXCEPTIONAL | PLS_STOPPED | PLS_CANCELED)) != 0) || (((flags & PLS_BROKEN) != 0) && (CallerIteration > LowestBreakIteration))));
        }

        internal bool ShouldExitLoop()
        {
            int flags = LoopStateFlags;
            return ((flags != PLS_NONE) && ((flags & (PLS_EXCEPTIONAL | PLS_CANCELED)) != 0));
        }
    }

    internal class ParallelLoopStateFlags64 : ParallelLoopStateFlags
    {
        internal long m_lowestBreakIteration = Int64.MaxValue;
        internal long LowestBreakIteration
        {
            get
            {
                if (IntPtr.Size >= 8)
                    return m_lowestBreakIteration;
                else
                    return Interlocked.Read(ref m_lowestBreakIteration);
            }
        }

        internal long ? NullableLowestBreakIteration
        {
            get
            {
                if (m_lowestBreakIteration == Int64.MaxValue)
                    return null;
                else
                {
                    if (IntPtr.Size >= 8)
                        return m_lowestBreakIteration;
                    else
                        return Interlocked.Read(ref m_lowestBreakIteration);
                }
            }
        }

        internal bool ShouldExitLoop(long CallerIteration)
        {
            int flags = LoopStateFlags;
            return (flags != PLS_NONE && (((flags & (PLS_EXCEPTIONAL | PLS_STOPPED | PLS_CANCELED)) != 0) || (((flags & PLS_BROKEN) != 0) && (CallerIteration > LowestBreakIteration))));
        }

        internal bool ShouldExitLoop()
        {
            int flags = LoopStateFlags;
            return ((flags != PLS_NONE) && ((flags & (PLS_EXCEPTIONAL | PLS_CANCELED)) != 0));
        }
    }

    public struct ParallelLoopResult
    {
        internal bool m_completed;
        internal long ? m_lowestBreakIteration;
        public bool IsCompleted
        {
            get
            {
                return m_completed;
            }
        }

        public long ? LowestBreakIteration
        {
            get
            {
                return m_lowestBreakIteration;
            }
        }
    }
}