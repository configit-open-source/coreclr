

namespace System.Threading
{
    public static class Monitor
    {
        public static extern void Enter(Object obj);
        public static void Enter(Object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();
            ReliableEnter(obj, ref lockTaken);
                    }

        private static void ThrowLockTakenException()
        {
            throw new ArgumentException(Environment.GetResourceString("Argument_MustBeFalse"), "lockTaken");
        }

        private static extern void ReliableEnter(Object obj, ref bool lockTaken);
        public static extern void Exit(Object obj);
        public static bool TryEnter(Object obj)
        {
            bool lockTaken = false;
            TryEnter(obj, 0, ref lockTaken);
            return lockTaken;
        }

        public static void TryEnter(Object obj, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();
            ReliableEnterTimeout(obj, 0, ref lockTaken);
        }

        public static bool TryEnter(Object obj, int millisecondsTimeout)
        {
            bool lockTaken = false;
            TryEnter(obj, millisecondsTimeout, ref lockTaken);
            return lockTaken;
        }

        private static int MillisecondsTimeoutFromTimeSpan(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long)Int32.MaxValue)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            return (int)tm;
        }

        public static bool TryEnter(Object obj, TimeSpan timeout)
        {
            return TryEnter(obj, MillisecondsTimeoutFromTimeSpan(timeout));
        }

        public static void TryEnter(Object obj, int millisecondsTimeout, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();
            ReliableEnterTimeout(obj, millisecondsTimeout, ref lockTaken);
        }

        public static void TryEnter(Object obj, TimeSpan timeout, ref bool lockTaken)
        {
            if (lockTaken)
                ThrowLockTakenException();
            ReliableEnterTimeout(obj, MillisecondsTimeoutFromTimeSpan(timeout), ref lockTaken);
        }

        private static extern void ReliableEnterTimeout(Object obj, int timeout, ref bool lockTaken);
        public static bool IsEntered(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            return IsEnteredNative(obj);
        }

        private static extern bool IsEnteredNative(Object obj);
        private static extern bool ObjWait(bool exitContext, int millisecondsTimeout, Object obj);
        public static bool Wait(Object obj, int millisecondsTimeout, bool exitContext)
        {
            if (obj == null)
                throw (new ArgumentNullException("obj"));
            return ObjWait(exitContext, millisecondsTimeout, obj);
        }

        public static bool Wait(Object obj, TimeSpan timeout, bool exitContext)
        {
            return Wait(obj, MillisecondsTimeoutFromTimeSpan(timeout), exitContext);
        }

        public static bool Wait(Object obj, int millisecondsTimeout)
        {
            return Wait(obj, millisecondsTimeout, false);
        }

        public static bool Wait(Object obj, TimeSpan timeout)
        {
            return Wait(obj, MillisecondsTimeoutFromTimeSpan(timeout), false);
        }

        public static bool Wait(Object obj)
        {
            return Wait(obj, Timeout.Infinite, false);
        }

        private static extern void ObjPulse(Object obj);
        public static void Pulse(Object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

                        ObjPulse(obj);
        }

        private static extern void ObjPulseAll(Object obj);
        public static void PulseAll(Object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

                        ObjPulseAll(obj);
        }
    }
}