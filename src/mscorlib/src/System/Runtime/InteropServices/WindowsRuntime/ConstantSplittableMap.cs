using System.Collections;
using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class ConstantSplittableMap<TKey, TValue> : IMapView<TKey, TValue>
    {
        private class KeyValuePairComparator : IComparer<KeyValuePair<TKey, TValue>>
        {
            private static readonly IComparer<TKey> keyComparator = Comparer<TKey>.Default;
            public Int32 Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return keyComparator.Compare(x.Key, y.Key);
            }
        }

        private static readonly KeyValuePairComparator keyValuePairComparator = new KeyValuePairComparator();
        private readonly KeyValuePair<TKey, TValue>[] items;
        private readonly int firstItemIndex;
        private readonly int lastItemIndex;
        internal ConstantSplittableMap(IReadOnlyDictionary<TKey, TValue> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
                        this.firstItemIndex = 0;
            this.lastItemIndex = data.Count - 1;
            this.items = CreateKeyValueArray(data.Count, data.GetEnumerator());
        }

        internal ConstantSplittableMap(IMapView<TKey, TValue> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (((UInt32)Int32.MaxValue) < data.Size)
            {
                Exception e = new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingDictionaryTooLarge"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }

            int size = (int)data.Size;
            this.firstItemIndex = 0;
            this.lastItemIndex = size - 1;
            this.items = CreateKeyValueArray(size, data.GetEnumerator());
        }

        private ConstantSplittableMap(KeyValuePair<TKey, TValue>[] items, Int32 firstItemIndex, Int32 lastItemIndex)
        {
            this.items = items;
            this.firstItemIndex = firstItemIndex;
            this.lastItemIndex = lastItemIndex;
        }

        private KeyValuePair<TKey, TValue>[] CreateKeyValueArray(Int32 count, IEnumerator<KeyValuePair<TKey, TValue>> data)
        {
            KeyValuePair<TKey, TValue>[] kvArray = new KeyValuePair<TKey, TValue>[count];
            Int32 i = 0;
            while (data.MoveNext())
                kvArray[i++] = data.Current;
            Array.Sort(kvArray, keyValuePairComparator);
            return kvArray;
        }

        private KeyValuePair<TKey, TValue>[] CreateKeyValueArray(Int32 count, IEnumerator<IKeyValuePair<TKey, TValue>> data)
        {
            KeyValuePair<TKey, TValue>[] kvArray = new KeyValuePair<TKey, TValue>[count];
            Int32 i = 0;
            while (data.MoveNext())
            {
                IKeyValuePair<TKey, TValue> current = data.Current;
                kvArray[i++] = new KeyValuePair<TKey, TValue>(current.Key, current.Value);
            }

            Array.Sort(kvArray, keyValuePairComparator);
            return kvArray;
        }

        public int Count
        {
            get
            {
                return lastItemIndex - firstItemIndex + 1;
            }
        }

        public UInt32 Size
        {
            get
            {
                return (UInt32)(lastItemIndex - firstItemIndex + 1);
            }
        }

        public TValue Lookup(TKey key)
        {
            TValue value;
            bool found = TryGetValue(key, out value);
            if (!found)
            {
                Exception e = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }

            return value;
        }

        public bool HasKey(TKey key)
        {
            TValue value;
            bool hasKey = TryGetValue(key, out value);
            return hasKey;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IKeyValuePair<TKey, TValue>>)this).GetEnumerator();
        }

        public IIterator<IKeyValuePair<TKey, TValue>> First()
        {
            return new EnumeratorToIteratorAdapter<IKeyValuePair<TKey, TValue>>(GetEnumerator());
        }

        public IEnumerator<IKeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new IKeyValuePairEnumerator(items, firstItemIndex, lastItemIndex);
        }

        public void Split(out IMapView<TKey, TValue> firstPartition, out IMapView<TKey, TValue> secondPartition)
        {
            if (Count < 2)
            {
                firstPartition = null;
                secondPartition = null;
                return;
            }

            int pivot = (Int32)(((Int64)firstItemIndex + (Int64)lastItemIndex) / (Int64)2);
            firstPartition = new ConstantSplittableMap<TKey, TValue>(items, firstItemIndex, pivot);
            secondPartition = new ConstantSplittableMap<TKey, TValue>(items, pivot + 1, lastItemIndex);
        }

        public bool ContainsKey(TKey key)
        {
            KeyValuePair<TKey, TValue> searchKey = new KeyValuePair<TKey, TValue>(key, default (TValue));
            int index = Array.BinarySearch(items, firstItemIndex, Count, searchKey, keyValuePairComparator);
            return index >= 0;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            KeyValuePair<TKey, TValue> searchKey = new KeyValuePair<TKey, TValue>(key, default (TValue));
            int index = Array.BinarySearch(items, firstItemIndex, Count, searchKey, keyValuePairComparator);
            if (index < 0)
            {
                value = default (TValue);
                return false;
            }

            value = items[index].Value;
            return true;
        }

        public TValue this[TKey key]
        {
            get
            {
                return Lookup(key);
            }
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                throw new NotImplementedException("NYI");
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                throw new NotImplementedException("NYI");
            }
        }

        internal struct IKeyValuePairEnumerator : IEnumerator<IKeyValuePair<TKey, TValue>>
        {
            private KeyValuePair<TKey, TValue>[] _array;
            private int _start;
            private int _end;
            private int _current;
            internal IKeyValuePairEnumerator(KeyValuePair<TKey, TValue>[] items, int first, int end)
            {
                                                                                                _array = items;
                _start = first;
                _end = end;
                _current = _start - 1;
            }

            public bool MoveNext()
            {
                if (_current < _end)
                {
                    _current++;
                    return true;
                }

                return false;
            }

            public IKeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (_current < _start)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (_current > _end)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    return new CLRIKeyValuePairImpl<TKey, TValue>(ref _array[_current]);
                }
            }

            Object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _current = _start - 1;
            }

            public void Dispose()
            {
            }
        }
    }
}