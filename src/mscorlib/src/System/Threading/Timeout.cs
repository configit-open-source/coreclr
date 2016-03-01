namespace System.Threading
{
    using System.Threading;
    using System;

    public static class Timeout
    {
        public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, Timeout.Infinite);
        public const int Infinite = -1;
        internal const uint UnsignedInfinite = unchecked ((uint)-1);
    }
}