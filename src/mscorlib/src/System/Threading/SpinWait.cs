

namespace System.Threading
{
    public struct SpinWait
    {
        internal const int YIELD_THRESHOLD = 10;
        internal const int SLEEP_0_EVERY_HOW_MANY_TIMES = 5;
        internal const int SLEEP_1_EVERY_HOW_MANY_TIMES = 20;
        private int m_count;
        public int Count
        {
            get
            {
                return m_count;
            }
        }

        public bool NextSpinWillYield
        {
            get
            {
                return m_count > YIELD_THRESHOLD || PlatformHelper.IsSingleProcessor;
            }
        }

        public void SpinOnce()
        {
            if (NextSpinWillYield)
            {
                int yieldsSoFar = (m_count >= YIELD_THRESHOLD ? m_count - YIELD_THRESHOLD : m_count);
                if ((yieldsSoFar % SLEEP_1_EVERY_HOW_MANY_TIMES) == (SLEEP_1_EVERY_HOW_MANY_TIMES - 1))
                {
                    Thread.Sleep(1);
                }
                else if ((yieldsSoFar % SLEEP_0_EVERY_HOW_MANY_TIMES) == (SLEEP_0_EVERY_HOW_MANY_TIMES - 1))
                {
                    Thread.Sleep(0);
                }
                else
                {
                    Thread.Yield();
                }
            }
            else
            {
                Thread.SpinWait(4 << m_count);
            }

            m_count = (m_count == int.MaxValue ? YIELD_THRESHOLD : m_count + 1);
        }

        public void Reset()
        {
            m_count = 0;
        }

        public static void SpinUntil(Func<bool> condition)
        {
            bool result = SpinUntil(condition, Timeout.Infinite);
                    }

        public static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
        {
            Int64 totalMilliseconds = (Int64)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException("timeout", timeout, Environment.GetResourceString("SpinWait_SpinUntil_TimeoutWrong"));
            }

            return SpinUntil(condition, (int)timeout.TotalMilliseconds);
        }

        public static bool SpinUntil(Func<bool> condition, int millisecondsTimeout)
        {
            if (millisecondsTimeout < Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", millisecondsTimeout, Environment.GetResourceString("SpinWait_SpinUntil_TimeoutWrong"));
            }

            if (condition == null)
            {
                throw new ArgumentNullException("condition", Environment.GetResourceString("SpinWait_SpinUntil_ArgumentNull"));
            }

            uint startTime = 0;
            if (millisecondsTimeout != 0 && millisecondsTimeout != Timeout.Infinite)
            {
                startTime = TimeoutHelper.GetTime();
            }

            SpinWait spinner = new SpinWait();
            while (!condition())
            {
                if (millisecondsTimeout == 0)
                {
                    return false;
                }

                spinner.SpinOnce();
                if (millisecondsTimeout != Timeout.Infinite && spinner.NextSpinWillYield)
                {
                    if (millisecondsTimeout <= (TimeoutHelper.GetTime() - startTime))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    internal static class PlatformHelper
    {
        private const int PROCESSOR_COUNT_REFRESH_INTERVAL_MS = 30000;
        private static volatile int s_processorCount;
        private static volatile int s_lastProcessorCountRefreshTicks;
        internal static int ProcessorCount
        {
            get
            {
                int now = Environment.TickCount;
                int procCount = s_processorCount;
                if (procCount == 0 || (now - s_lastProcessorCountRefreshTicks) >= PROCESSOR_COUNT_REFRESH_INTERVAL_MS)
                {
                    s_processorCount = procCount = Environment.ProcessorCount;
                    s_lastProcessorCountRefreshTicks = now;
                }

                                return procCount;
            }
        }

        internal static bool IsSingleProcessor
        {
            get
            {
                return ProcessorCount == 1;
            }
        }
    }

    internal static class TimeoutHelper
    {
        public static uint GetTime()
        {
            return (uint)Environment.TickCount;
        }

        public static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
        {
                        uint elapsedMilliseconds = (GetTime() - startTime);
            if (elapsedMilliseconds > int.MaxValue)
            {
                return 0;
            }

            int currentWaitTimeout = originalWaitMillisecondsTimeout - (int)elapsedMilliseconds;
            ;
            if (currentWaitTimeout <= 0)
            {
                return 0;
            }

            return currentWaitTimeout;
        }
    }
}