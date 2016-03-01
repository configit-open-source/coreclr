
using System.Runtime.Versioning;

namespace System.Collections.Generic
{
    internal interface IArraySortHelper<TKey>
    {
        void Sort(TKey[] keys, int index, int length, IComparer<TKey> comparer);
        int BinarySearch(TKey[] keys, int index, int length, TKey value, IComparer<TKey> comparer);
    }

    internal static class IntrospectiveSortUtilities
    {
        internal const int IntrosortSizeThreshold = 16;
        internal const int QuickSortDepthThreshold = 32;
        internal static int FloorLog2(int n)
        {
            int result = 0;
            while (n >= 1)
            {
                result++;
                n = n / 2;
            }

            return result;
        }

        internal static void ThrowOrIgnoreBadComparer(Object comparer)
        {
            if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_BogusIComparer", comparer));
            }
        }
    }

    internal class ArraySortHelper<T> : IArraySortHelper<T>
    {
        static volatile IArraySortHelper<T> defaultArraySortHelper;
        public static IArraySortHelper<T> Default
        {
            get
            {
                IArraySortHelper<T> sorter = defaultArraySortHelper;
                if (sorter == null)
                    sorter = CreateArraySortHelper();
                return sorter;
            }
        }

        private static IArraySortHelper<T> CreateArraySortHelper()
        {
            if (typeof (IComparable<T>).IsAssignableFrom(typeof (T)))
            {
                defaultArraySortHelper = (IArraySortHelper<T>)RuntimeTypeHandle.Allocate(typeof (GenericArraySortHelper<string>).TypeHandle.Instantiate(new Type[]{typeof (T)}));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelper<T>();
            }

            return defaultArraySortHelper;
        }

        public void Sort(T[] keys, int index, int length, IComparer<T> comparer)
        {
                                    try
            {
                if (comparer == null)
                {
                    comparer = Comparer<T>.Default;
                }

                IntrospectiveSort(keys, index, length, comparer);
            }
            catch (IndexOutOfRangeException)
            {
                IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        public int BinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
            try
            {
                if (comparer == null)
                {
                    comparer = Comparer<T>.Default;
                }

                return InternalBinarySearch(array, index, length, value, comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        internal static int InternalBinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
                                    int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer.Compare(array[i], value);
                if (order == 0)
                    return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }

        private static void SwapIfGreater(T[] keys, IComparer<T> comparer, int a, int b)
        {
            if (a != b)
            {
                if (comparer.Compare(keys[a], keys[b]) > 0)
                {
                    T key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                }
            }
        }

        private static void Swap(T[] a, int i, int j)
        {
            if (i != j)
            {
                T t = a[i];
                a[i] = a[j];
                a[j] = t;
            }
        }

        internal static void DepthLimitedQuickSort(T[] keys, int left, int right, IComparer<T> comparer, int depthLimit)
        {
            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, left, right, comparer);
                    return;
                }

                int i = left;
                int j = right;
                int middle = i + ((j - i) >> 1);
                SwapIfGreater(keys, comparer, i, middle);
                SwapIfGreater(keys, comparer, i, j);
                SwapIfGreater(keys, comparer, middle, j);
                T x = keys[middle];
                do
                {
                    while (comparer.Compare(keys[i], x) < 0)
                        i++;
                    while (comparer.Compare(x, keys[j]) < 0)
                        j--;
                                        if (i > j)
                        break;
                    if (i < j)
                    {
                        T key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                    }

                    i++;
                    j--;
                }
                while (i <= j);
                depthLimit--;
                if (j - left <= right - i)
                {
                    if (left < j)
                        DepthLimitedQuickSort(keys, left, j, comparer, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right)
                        DepthLimitedQuickSort(keys, i, right, comparer, depthLimit);
                    right = j;
                }
            }
            while (left < right);
        }

        internal static void IntrospectiveSort(T[] keys, int left, int length, IComparer<T> comparer)
        {
                                                                                    if (length < 2)
                return;
            IntroSort(keys, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length), comparer);
        }

        private static void IntroSort(T[] keys, int lo, int hi, int depthLimit, IComparer<T> comparer)
        {
                                                            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }

                    if (partitionSize == 2)
                    {
                        SwapIfGreater(keys, comparer, lo, hi);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreater(keys, comparer, lo, hi - 1);
                        SwapIfGreater(keys, comparer, lo, hi);
                        SwapIfGreater(keys, comparer, hi - 1, hi);
                        return;
                    }

                    InsertionSort(keys, lo, hi, comparer);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, lo, hi, comparer);
                    return;
                }

                depthLimit--;
                int p = PickPivotAndPartition(keys, lo, hi, comparer);
                IntroSort(keys, p + 1, hi, depthLimit, comparer);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(T[] keys, int lo, int hi, IComparer<T> comparer)
        {
                                                                                    int middle = lo + ((hi - lo) / 2);
            SwapIfGreater(keys, comparer, lo, middle);
            SwapIfGreater(keys, comparer, lo, hi);
            SwapIfGreater(keys, comparer, middle, hi);
            T pivot = keys[middle];
            Swap(keys, middle, hi - 1);
            int left = lo, right = hi - 1;
            while (left < right)
            {
                while (comparer.Compare(keys[++left], pivot) < 0)
                    ;
                while (comparer.Compare(pivot, keys[--right]) < 0)
                    ;
                if (left >= right)
                    break;
                Swap(keys, left, right);
            }

            Swap(keys, left, (hi - 1));
            return left;
        }

        private static void Heapsort(T[] keys, int lo, int hi, IComparer<T> comparer)
        {
                                                                        int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, i, n, lo, comparer);
            }

            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, lo, lo + i - 1);
                DownHeap(keys, 1, i - 1, lo, comparer);
            }
        }

        private static void DownHeap(T[] keys, int i, int n, int lo, IComparer<T> comparer)
        {
                                                            T d = keys[lo + i - 1];
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && comparer.Compare(keys[lo + child - 1], keys[lo + child]) < 0)
                {
                    child++;
                }

                if (!(comparer.Compare(d, keys[lo + child - 1]) < 0))
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                i = child;
            }

            keys[lo + i - 1] = d;
        }

        private static void InsertionSort(T[] keys, int lo, int hi, IComparer<T> comparer)
        {
                                                            int i, j;
            T t;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                while (j >= lo && comparer.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    j--;
                }

                keys[j + 1] = t;
            }
        }
    }

    internal class GenericArraySortHelper<T> : IArraySortHelper<T> where T : IComparable<T>
    {
        public void Sort(T[] keys, int index, int length, IComparer<T> comparer)
        {
                                    try
            {
                if (comparer == null && CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    comparer = Comparer<T>.Default;
                if (comparer == null || (comparer == Comparer<T>.Default && !CompatibilitySwitches.IsAppEarlierThanWindowsPhone8))
                {
                    IntrospectiveSort(keys, index, length);
                }
                else
                {
                    ArraySortHelper<T>.IntrospectiveSort(keys, index, length, comparer);
                }
            }
            catch (IndexOutOfRangeException)
            {
                IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        public int BinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
                                    try
            {
                if (comparer == null || comparer == Comparer<T>.Default)
                {
                    return BinarySearch(array, index, length, value);
                }
                else
                {
                    return ArraySortHelper<T>.InternalBinarySearch(array, index, length, value, comparer);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        private static int BinarySearch(T[] array, int index, int length, T value)
        {
            int lo = index;
            int hi = index + length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order;
                if (array[i] == null)
                {
                    order = (value == null) ? 0 : -1;
                }
                else
                {
                    order = array[i].CompareTo(value);
                }

                if (order == 0)
                {
                    return i;
                }

                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }

        private static void SwapIfGreaterWithItems(T[] keys, int a, int b)
        {
                                                if (a != b)
            {
                if (keys[a] != null && keys[a].CompareTo(keys[b]) > 0)
                {
                    T key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                }
            }
        }

        private static void Swap(T[] a, int i, int j)
        {
            if (i != j)
            {
                T t = a[i];
                a[i] = a[j];
                a[j] = t;
            }
        }

        private static void DepthLimitedQuickSort(T[] keys, int left, int right, int depthLimit)
        {
                                                do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, left, right);
                    return;
                }

                int i = left;
                int j = right;
                int middle = i + ((j - i) >> 1);
                SwapIfGreaterWithItems(keys, i, middle);
                SwapIfGreaterWithItems(keys, i, j);
                SwapIfGreaterWithItems(keys, middle, j);
                T x = keys[middle];
                do
                {
                    if (x == null)
                    {
                        while (keys[j] != null)
                            j--;
                    }
                    else
                    {
                        while (x.CompareTo(keys[i]) > 0)
                            i++;
                        while (x.CompareTo(keys[j]) < 0)
                            j--;
                    }

                                        if (i > j)
                        break;
                    if (i < j)
                    {
                        T key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                    }

                    i++;
                    j--;
                }
                while (i <= j);
                depthLimit--;
                if (j - left <= right - i)
                {
                    if (left < j)
                        DepthLimitedQuickSort(keys, left, j, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right)
                        DepthLimitedQuickSort(keys, i, right, depthLimit);
                    right = j;
                }
            }
            while (left < right);
        }

        internal static void IntrospectiveSort(T[] keys, int left, int length)
        {
                                                                        if (length < 2)
                return;
            IntroSort(keys, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length));
        }

        private static void IntroSort(T[] keys, int lo, int hi, int depthLimit)
        {
                                                while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }

                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems(keys, lo, hi);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems(keys, lo, hi - 1);
                        SwapIfGreaterWithItems(keys, lo, hi);
                        SwapIfGreaterWithItems(keys, hi - 1, hi);
                        return;
                    }

                    InsertionSort(keys, lo, hi);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, lo, hi);
                    return;
                }

                depthLimit--;
                int p = PickPivotAndPartition(keys, lo, hi);
                IntroSort(keys, p + 1, hi, depthLimit);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(T[] keys, int lo, int hi)
        {
                                                                        int middle = lo + ((hi - lo) / 2);
            SwapIfGreaterWithItems(keys, lo, middle);
            SwapIfGreaterWithItems(keys, lo, hi);
            SwapIfGreaterWithItems(keys, middle, hi);
            T pivot = keys[middle];
            Swap(keys, middle, hi - 1);
            int left = lo, right = hi - 1;
            while (left < right)
            {
                if (pivot == null)
                {
                    while (left < (hi - 1) && keys[++left] == null)
                        ;
                    while (right > lo && keys[--right] != null)
                        ;
                }
                else
                {
                    while (pivot.CompareTo(keys[++left]) > 0)
                        ;
                    while (pivot.CompareTo(keys[--right]) < 0)
                        ;
                }

                if (left >= right)
                    break;
                Swap(keys, left, right);
            }

            Swap(keys, left, (hi - 1));
            return left;
        }

        private static void Heapsort(T[] keys, int lo, int hi)
        {
                                                            int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, i, n, lo);
            }

            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, lo, lo + i - 1);
                DownHeap(keys, 1, i - 1, lo);
            }
        }

        private static void DownHeap(T[] keys, int i, int n, int lo)
        {
                                                T d = keys[lo + i - 1];
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && (keys[lo + child - 1] == null || keys[lo + child - 1].CompareTo(keys[lo + child]) < 0))
                {
                    child++;
                }

                if (keys[lo + child - 1] == null || keys[lo + child - 1].CompareTo(d) < 0)
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                i = child;
            }

            keys[lo + i - 1] = d;
        }

        private static void InsertionSort(T[] keys, int lo, int hi)
        {
                                                            int i, j;
            T t;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                while (j >= lo && (t == null || t.CompareTo(keys[j]) < 0))
                {
                    keys[j + 1] = keys[j];
                    j--;
                }

                keys[j + 1] = t;
            }
        }
    }

    internal interface IArraySortHelper<TKey, TValue>
    {
        void Sort(TKey[] keys, TValue[] values, int index, int length, IComparer<TKey> comparer);
    }

    internal class ArraySortHelper<TKey, TValue> : IArraySortHelper<TKey, TValue>
    {
        static volatile IArraySortHelper<TKey, TValue> defaultArraySortHelper;
        public static IArraySortHelper<TKey, TValue> Default
        {
            get
            {
                IArraySortHelper<TKey, TValue> sorter = defaultArraySortHelper;
                if (sorter == null)
                    sorter = CreateArraySortHelper();
                return sorter;
            }
        }

        private static IArraySortHelper<TKey, TValue> CreateArraySortHelper()
        {
            if (typeof (IComparable<TKey>).IsAssignableFrom(typeof (TKey)))
            {
                defaultArraySortHelper = (IArraySortHelper<TKey, TValue>)RuntimeTypeHandle.Allocate(typeof (GenericArraySortHelper<string, string>).TypeHandle.Instantiate(new Type[]{typeof (TKey), typeof (TValue)}));
            }
            else
            {
                defaultArraySortHelper = new ArraySortHelper<TKey, TValue>();
            }

            return defaultArraySortHelper;
        }

        public void Sort(TKey[] keys, TValue[] values, int index, int length, IComparer<TKey> comparer)
        {
                                    try
            {
                if (comparer == null || comparer == Comparer<TKey>.Default)
                {
                    comparer = Comparer<TKey>.Default;
                }

                IntrospectiveSort(keys, values, index, length, comparer);
            }
            catch (IndexOutOfRangeException)
            {
                IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        private static void SwapIfGreaterWithItems(TKey[] keys, TValue[] values, IComparer<TKey> comparer, int a, int b)
        {
                                                                        if (a != b)
            {
                if (comparer.Compare(keys[a], keys[b]) > 0)
                {
                    TKey key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                    if (values != null)
                    {
                        TValue value = values[a];
                        values[a] = values[b];
                        values[b] = value;
                    }
                }
            }
        }

        private static void Swap(TKey[] keys, TValue[] values, int i, int j)
        {
            if (i != j)
            {
                TKey k = keys[i];
                keys[i] = keys[j];
                keys[j] = k;
                if (values != null)
                {
                    TValue v = values[i];
                    values[i] = values[j];
                    values[j] = v;
                }
            }
        }

        internal static void DepthLimitedQuickSort(TKey[] keys, TValue[] values, int left, int right, IComparer<TKey> comparer, int depthLimit)
        {
            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, values, left, right, comparer);
                    return;
                }

                int i = left;
                int j = right;
                int middle = i + ((j - i) >> 1);
                SwapIfGreaterWithItems(keys, values, comparer, i, middle);
                SwapIfGreaterWithItems(keys, values, comparer, i, j);
                SwapIfGreaterWithItems(keys, values, comparer, middle, j);
                TKey x = keys[middle];
                do
                {
                    while (comparer.Compare(keys[i], x) < 0)
                        i++;
                    while (comparer.Compare(x, keys[j]) < 0)
                        j--;
                                        if (i > j)
                        break;
                    if (i < j)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                        if (values != null)
                        {
                            TValue value = values[i];
                            values[i] = values[j];
                            values[j] = value;
                        }
                    }

                    i++;
                    j--;
                }
                while (i <= j);
                depthLimit--;
                if (j - left <= right - i)
                {
                    if (left < j)
                        DepthLimitedQuickSort(keys, values, left, j, comparer, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right)
                        DepthLimitedQuickSort(keys, values, i, right, comparer, depthLimit);
                    right = j;
                }
            }
            while (left < right);
        }

        internal static void IntrospectiveSort(TKey[] keys, TValue[] values, int left, int length, IComparer<TKey> comparer)
        {
                                                                                                            if (length < 2)
                return;
            IntroSort(keys, values, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length), comparer);
        }

        private static void IntroSort(TKey[] keys, TValue[] values, int lo, int hi, int depthLimit, IComparer<TKey> comparer)
        {
                                                                        while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }

                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi - 1);
                        SwapIfGreaterWithItems(keys, values, comparer, lo, hi);
                        SwapIfGreaterWithItems(keys, values, comparer, hi - 1, hi);
                        return;
                    }

                    InsertionSort(keys, values, lo, hi, comparer);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, values, lo, hi, comparer);
                    return;
                }

                depthLimit--;
                int p = PickPivotAndPartition(keys, values, lo, hi, comparer);
                IntroSort(keys, values, p + 1, hi, depthLimit, comparer);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(TKey[] keys, TValue[] values, int lo, int hi, IComparer<TKey> comparer)
        {
                                                                                                int middle = lo + ((hi - lo) / 2);
            SwapIfGreaterWithItems(keys, values, comparer, lo, middle);
            SwapIfGreaterWithItems(keys, values, comparer, lo, hi);
            SwapIfGreaterWithItems(keys, values, comparer, middle, hi);
            TKey pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            int left = lo, right = hi - 1;
            while (left < right)
            {
                while (comparer.Compare(keys[++left], pivot) < 0)
                    ;
                while (comparer.Compare(pivot, keys[--right]) < 0)
                    ;
                if (left >= right)
                    break;
                Swap(keys, values, left, right);
            }

            Swap(keys, values, left, (hi - 1));
            return left;
        }

        private static void Heapsort(TKey[] keys, TValue[] values, int lo, int hi, IComparer<TKey> comparer)
        {
                                                                                    int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, values, i, n, lo, comparer);
            }

            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, values, lo, lo + i - 1);
                DownHeap(keys, values, 1, i - 1, lo, comparer);
            }
        }

        private static void DownHeap(TKey[] keys, TValue[] values, int i, int n, int lo, IComparer<TKey> comparer)
        {
                                                            TKey d = keys[lo + i - 1];
            TValue dValue = (values != null) ? values[lo + i - 1] : default (TValue);
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && comparer.Compare(keys[lo + child - 1], keys[lo + child]) < 0)
                {
                    child++;
                }

                if (!(comparer.Compare(d, keys[lo + child - 1]) < 0))
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                if (values != null)
                    values[lo + i - 1] = values[lo + child - 1];
                i = child;
            }

            keys[lo + i - 1] = d;
            if (values != null)
                values[lo + i - 1] = dValue;
        }

        private static void InsertionSort(TKey[] keys, TValue[] values, int lo, int hi, IComparer<TKey> comparer)
        {
                                                                                    int i, j;
            TKey t;
            TValue tValue;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                tValue = (values != null) ? values[i + 1] : default (TValue);
                while (j >= lo && comparer.Compare(t, keys[j]) < 0)
                {
                    keys[j + 1] = keys[j];
                    if (values != null)
                        values[j + 1] = values[j];
                    j--;
                }

                keys[j + 1] = t;
                if (values != null)
                    values[j + 1] = tValue;
            }
        }
    }

    internal class GenericArraySortHelper<TKey, TValue> : IArraySortHelper<TKey, TValue> where TKey : IComparable<TKey>
    {
        public void Sort(TKey[] keys, TValue[] values, int index, int length, IComparer<TKey> comparer)
        {
                                    try
            {
                if (comparer == null || comparer == Comparer<TKey>.Default)
                {
                    IntrospectiveSort(keys, values, index, length);
                }
                else
                {
                    ArraySortHelper<TKey, TValue>.IntrospectiveSort(keys, values, index, length, comparer);
                }
            }
            catch (IndexOutOfRangeException)
            {
                IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
            }
        }

        private static void SwapIfGreaterWithItems(TKey[] keys, TValue[] values, int a, int b)
        {
            if (a != b)
            {
                if (keys[a] != null && keys[a].CompareTo(keys[b]) > 0)
                {
                    TKey key = keys[a];
                    keys[a] = keys[b];
                    keys[b] = key;
                    if (values != null)
                    {
                        TValue value = values[a];
                        values[a] = values[b];
                        values[b] = value;
                    }
                }
            }
        }

        private static void Swap(TKey[] keys, TValue[] values, int i, int j)
        {
            if (i != j)
            {
                TKey k = keys[i];
                keys[i] = keys[j];
                keys[j] = k;
                if (values != null)
                {
                    TValue v = values[i];
                    values[i] = values[j];
                    values[j] = v;
                }
            }
        }

        private static void DepthLimitedQuickSort(TKey[] keys, TValue[] values, int left, int right, int depthLimit)
        {
            do
            {
                if (depthLimit == 0)
                {
                    Heapsort(keys, values, left, right);
                    return;
                }

                int i = left;
                int j = right;
                int middle = i + ((j - i) >> 1);
                SwapIfGreaterWithItems(keys, values, i, middle);
                SwapIfGreaterWithItems(keys, values, i, j);
                SwapIfGreaterWithItems(keys, values, middle, j);
                TKey x = keys[middle];
                do
                {
                    if (x == null)
                    {
                        while (keys[j] != null)
                            j--;
                    }
                    else
                    {
                        while (x.CompareTo(keys[i]) > 0)
                            i++;
                        while (x.CompareTo(keys[j]) < 0)
                            j--;
                    }

                                        if (i > j)
                        break;
                    if (i < j)
                    {
                        TKey key = keys[i];
                        keys[i] = keys[j];
                        keys[j] = key;
                        if (values != null)
                        {
                            TValue value = values[i];
                            values[i] = values[j];
                            values[j] = value;
                        }
                    }

                    i++;
                    j--;
                }
                while (i <= j);
                depthLimit--;
                if (j - left <= right - i)
                {
                    if (left < j)
                        DepthLimitedQuickSort(keys, values, left, j, depthLimit);
                    left = i;
                }
                else
                {
                    if (i < right)
                        DepthLimitedQuickSort(keys, values, i, right, depthLimit);
                    right = j;
                }
            }
            while (left < right);
        }

        internal static void IntrospectiveSort(TKey[] keys, TValue[] values, int left, int length)
        {
                                                                                                if (length < 2)
                return;
            IntroSort(keys, values, left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length));
        }

        private static void IntroSort(TKey[] keys, TValue[] values, int lo, int hi, int depthLimit)
        {
                                                            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }

                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems(keys, values, lo, hi);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems(keys, values, lo, hi - 1);
                        SwapIfGreaterWithItems(keys, values, lo, hi);
                        SwapIfGreaterWithItems(keys, values, hi - 1, hi);
                        return;
                    }

                    InsertionSort(keys, values, lo, hi);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(keys, values, lo, hi);
                    return;
                }

                depthLimit--;
                int p = PickPivotAndPartition(keys, values, lo, hi);
                IntroSort(keys, values, p + 1, hi, depthLimit);
                hi = p - 1;
            }
        }

        private static int PickPivotAndPartition(TKey[] keys, TValue[] values, int lo, int hi)
        {
                                                                                    int middle = lo + ((hi - lo) / 2);
            SwapIfGreaterWithItems(keys, values, lo, middle);
            SwapIfGreaterWithItems(keys, values, lo, hi);
            SwapIfGreaterWithItems(keys, values, middle, hi);
            TKey pivot = keys[middle];
            Swap(keys, values, middle, hi - 1);
            int left = lo, right = hi - 1;
            while (left < right)
            {
                if (pivot == null)
                {
                    while (left < (hi - 1) && keys[++left] == null)
                        ;
                    while (right > lo && keys[--right] != null)
                        ;
                }
                else
                {
                    while (pivot.CompareTo(keys[++left]) > 0)
                        ;
                    while (pivot.CompareTo(keys[--right]) < 0)
                        ;
                }

                if (left >= right)
                    break;
                Swap(keys, values, left, right);
            }

            Swap(keys, values, left, (hi - 1));
            return left;
        }

        private static void Heapsort(TKey[] keys, TValue[] values, int lo, int hi)
        {
                                                                        int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i = i - 1)
            {
                DownHeap(keys, values, i, n, lo);
            }

            for (int i = n; i > 1; i = i - 1)
            {
                Swap(keys, values, lo, lo + i - 1);
                DownHeap(keys, values, 1, i - 1, lo);
            }
        }

        private static void DownHeap(TKey[] keys, TValue[] values, int i, int n, int lo)
        {
                                                TKey d = keys[lo + i - 1];
            TValue dValue = (values != null) ? values[lo + i - 1] : default (TValue);
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && (keys[lo + child - 1] == null || keys[lo + child - 1].CompareTo(keys[lo + child]) < 0))
                {
                    child++;
                }

                if (keys[lo + child - 1] == null || keys[lo + child - 1].CompareTo(d) < 0)
                    break;
                keys[lo + i - 1] = keys[lo + child - 1];
                if (values != null)
                    values[lo + i - 1] = values[lo + child - 1];
                i = child;
            }

            keys[lo + i - 1] = d;
            if (values != null)
                values[lo + i - 1] = dValue;
        }

        private static void InsertionSort(TKey[] keys, TValue[] values, int lo, int hi)
        {
                                                                        int i, j;
            TKey t;
            TValue tValue;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = keys[i + 1];
                tValue = (values != null) ? values[i + 1] : default (TValue);
                while (j >= lo && (t == null || t.CompareTo(keys[j]) < 0))
                {
                    keys[j + 1] = keys[j];
                    if (values != null)
                        values[j + 1] = values[j];
                    j--;
                }

                keys[j + 1] = t;
                if (values != null)
                    values[j + 1] = tValue;
            }
        }
    }
}