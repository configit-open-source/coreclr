namespace System.Threading
{
    using System;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Runtime;

    public static class Interlocked
    {
        public static int Increment(ref int location)
        {
            return Add(ref location, 1);
        }

        public static long Increment(ref long location)
        {
            return Add(ref location, 1);
        }

        public static int Decrement(ref int location)
        {
            return Add(ref location, -1);
        }

        public static long Decrement(ref long location)
        {
            return Add(ref location, -1);
        }

        public static extern int Exchange(ref int location1, int value);
        public static extern long Exchange(ref long location1, long value);
        public static extern float Exchange(ref float location1, float value);
        public static extern double Exchange(ref double location1, double value);
        public static extern Object Exchange(ref Object location1, Object value);
        public static extern IntPtr Exchange(ref IntPtr location1, IntPtr value);
        public static T Exchange<T>(ref T location1, T value)where T : class
        {
            _Exchange(__makeref (location1), __makeref (value));
            return value;
        }

        private static extern void _Exchange(TypedReference location1, TypedReference value);
        public static extern int CompareExchange(ref int location1, int value, int comparand);
        public static extern long CompareExchange(ref long location1, long value, long comparand);
        public static extern float CompareExchange(ref float location1, float value, float comparand);
        public static extern double CompareExchange(ref double location1, double value, double comparand);
        public static extern Object CompareExchange(ref Object location1, Object value, Object comparand);
        public static extern IntPtr CompareExchange(ref IntPtr location1, IntPtr value, IntPtr comparand);
        public static T CompareExchange<T>(ref T location1, T value, T comparand)where T : class
        {
            _CompareExchange(__makeref (location1), __makeref (value), comparand);
            return value;
        }

        private static extern void _CompareExchange(TypedReference location1, TypedReference value, Object comparand);
        internal static extern int CompareExchange(ref int location1, int value, int comparand, ref bool succeeded);
        internal static extern int ExchangeAdd(ref int location1, int value);
        internal static extern long ExchangeAdd(ref long location1, long value);
        public static int Add(ref int location1, int value)
        {
            return ExchangeAdd(ref location1, value) + value;
        }

        public static long Add(ref long location1, long value)
        {
            return ExchangeAdd(ref location1, value) + value;
        }

        public static long Read(ref long location)
        {
            return Interlocked.CompareExchange(ref location, 0, 0);
        }

        public static void MemoryBarrier()
        {
            Thread.MemoryBarrier();
        }
    }
}