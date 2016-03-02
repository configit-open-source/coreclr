namespace System.Text
{
    internal static class StringBuilderCache
    {
        private const int MAX_BUILDER_SIZE = 360;
        private static StringBuilder CachedInstance;
        public static StringBuilder Acquire(int capacity = StringBuilder.DefaultCapacity)
        {
            if (capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilder sb = StringBuilderCache.CachedInstance;
                if (sb != null)
                {
                    if (capacity <= sb.Capacity)
                    {
                        StringBuilderCache.CachedInstance = null;
                        sb.Clear();
                        return sb;
                    }
                }
            }

            return new StringBuilder(capacity);
        }

        public static void Release(StringBuilder sb)
        {
            if (sb.Capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilderCache.CachedInstance = sb;
            }
        }

        public static string GetStringAndRelease(StringBuilder sb)
        {
            string result = sb.ToString();
            Release(sb);
            return result;
        }
    }
}