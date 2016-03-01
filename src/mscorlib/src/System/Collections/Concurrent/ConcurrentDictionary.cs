using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;

namespace System.Collections.Concurrent
{
    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>
    {
        private class Tables
        {
            internal readonly Node[] m_buckets;
            internal readonly object[] m_locks;
            internal volatile int[] m_countPerLock;
            internal readonly IEqualityComparer<TKey> m_comparer;
            internal Tables(Node[] buckets, object[] locks, int[] countPerLock, IEqualityComparer<TKey> comparer)
            {
                m_buckets = buckets;
                m_locks = locks;
                m_countPerLock = countPerLock;
                m_comparer = comparer;
            }
        }

        private volatile Tables m_tables;
        internal IEqualityComparer<TKey> m_comparer;
        private readonly bool m_growLockArray;
        private int m_keyRehashCount;
        private int m_budget;
        private const int DEFAULT_CONCURRENCY_MULTIPLIER = 4;
        private const int DEFAULT_CAPACITY = 31;
        private const int MAX_LOCK_NUMBER = 1024;
        private static readonly bool s_isValueWriteAtomic = IsValueWriteAtomic();
        private static bool IsValueWriteAtomic()
        {
            Type valueType = typeof (TValue);
            bool isAtomic = (valueType.IsClass) || valueType == typeof (Boolean) || valueType == typeof (Char) || valueType == typeof (Byte) || valueType == typeof (SByte) || valueType == typeof (Int16) || valueType == typeof (UInt16) || valueType == typeof (Int32) || valueType == typeof (UInt32) || valueType == typeof (Single);
            if (!isAtomic && IntPtr.Size == 8)
            {
                isAtomic |= valueType == typeof (Double) || valueType == typeof (Int64);
            }

            return isAtomic;
        }

        public ConcurrentDictionary(): this (DefaultConcurrencyLevel, DEFAULT_CAPACITY, true, EqualityComparer<TKey>.Default)
        {
        }

        public ConcurrentDictionary(int concurrencyLevel, int capacity): this (concurrencyLevel, capacity, false, EqualityComparer<TKey>.Default)
        {
        }

        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection): this (collection, EqualityComparer<TKey>.Default)
        {
        }

        public ConcurrentDictionary(IEqualityComparer<TKey> comparer): this (DefaultConcurrencyLevel, DEFAULT_CAPACITY, true, comparer)
        {
        }

        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer): this (comparer)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            InitializeFromCollection(collection);
        }

        public ConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer): this (concurrencyLevel, DEFAULT_CAPACITY, false, comparer)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            InitializeFromCollection(collection);
        }

        private void InitializeFromCollection(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            TValue dummy;
            foreach (KeyValuePair<TKey, TValue> pair in collection)
            {
                if (pair.Key == null)
                    throw new ArgumentNullException("key");
                if (!TryAddInternal(pair.Key, pair.Value, false, false, out dummy))
                {
                    throw new ArgumentException(GetResource("ConcurrentDictionary_SourceContainsDuplicateKeys"));
                }
            }

            if (m_budget == 0)
            {
                m_budget = m_tables.m_buckets.Length / m_tables.m_locks.Length;
            }
        }

        public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer): this (concurrencyLevel, capacity, false, comparer)
        {
        }

        internal ConcurrentDictionary(int concurrencyLevel, int capacity, bool growLockArray, IEqualityComparer<TKey> comparer)
        {
            if (concurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException("concurrencyLevel", GetResource("ConcurrentDictionary_ConcurrencyLevelMustBePositive"));
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", GetResource("ConcurrentDictionary_CapacityMustNotBeNegative"));
            }

            if (comparer == null)
                throw new ArgumentNullException("comparer");
            if (capacity < concurrencyLevel)
            {
                capacity = concurrencyLevel;
            }

            object[] locks = new object[concurrencyLevel];
            for (int i = 0; i < locks.Length; i++)
            {
                locks[i] = new object ();
            }

            int[] countPerLock = new int[locks.Length];
            Node[] buckets = new Node[capacity];
            m_tables = new Tables(buckets, locks, countPerLock, comparer);
            m_growLockArray = growLockArray;
            m_budget = buckets.Length / locks.Length;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            TValue dummy;
            return TryAddInternal(key, value, false, true, out dummy);
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            TValue throwAwayValue;
            return TryGetValue(key, out throwAwayValue);
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return TryRemoveInternal(key, out value, false, default (TValue));
        }

        private bool TryRemoveInternal(TKey key, out TValue value, bool matchValue, TValue oldValue)
        {
            while (true)
            {
                Tables tables = m_tables;
                IEqualityComparer<TKey> comparer = tables.m_comparer;
                int bucketNo, lockNo;
                GetBucketAndLockNo(comparer.GetHashCode(key), out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);
                lock (tables.m_locks[lockNo])
                {
                    if (tables != m_tables)
                    {
                        continue;
                    }

                    Node prev = null;
                    for (Node curr = tables.m_buckets[bucketNo]; curr != null; curr = curr.m_next)
                    {
                        Assert((prev == null && curr == tables.m_buckets[bucketNo]) || prev.m_next == curr);
                        if (comparer.Equals(curr.m_key, key))
                        {
                            if (matchValue)
                            {
                                bool valuesMatch = EqualityComparer<TValue>.Default.Equals(oldValue, curr.m_value);
                                if (!valuesMatch)
                                {
                                    value = default (TValue);
                                    return false;
                                }
                            }

                            if (prev == null)
                            {
                                Volatile.Write<Node>(ref tables.m_buckets[bucketNo], curr.m_next);
                            }
                            else
                            {
                                prev.m_next = curr.m_next;
                            }

                            value = curr.m_value;
                            tables.m_countPerLock[lockNo]--;
                            return true;
                        }

                        prev = curr;
                    }
                }

                value = default (TValue);
                return false;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            int bucketNo, lockNoUnused;
            Tables tables = m_tables;
            IEqualityComparer<TKey> comparer = tables.m_comparer;
            GetBucketAndLockNo(comparer.GetHashCode(key), out bucketNo, out lockNoUnused, tables.m_buckets.Length, tables.m_locks.Length);
            Node n = Volatile.Read<Node>(ref tables.m_buckets[bucketNo]);
            while (n != null)
            {
                if (comparer.Equals(n.m_key, key))
                {
                    value = n.m_value;
                    return true;
                }

                n = n.m_next;
            }

            value = default (TValue);
            return false;
        }

        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            IEqualityComparer<TValue> valueComparer = EqualityComparer<TValue>.Default;
            while (true)
            {
                int bucketNo;
                int lockNo;
                int hashcode;
                Tables tables = m_tables;
                IEqualityComparer<TKey> comparer = tables.m_comparer;
                hashcode = comparer.GetHashCode(key);
                GetBucketAndLockNo(hashcode, out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);
                lock (tables.m_locks[lockNo])
                {
                    if (tables != m_tables)
                    {
                        continue;
                    }

                    Node prev = null;
                    for (Node node = tables.m_buckets[bucketNo]; node != null; node = node.m_next)
                    {
                        Assert((prev == null && node == tables.m_buckets[bucketNo]) || prev.m_next == node);
                        if (comparer.Equals(node.m_key, key))
                        {
                            if (valueComparer.Equals(node.m_value, comparisonValue))
                            {
                                if (s_isValueWriteAtomic)
                                {
                                    node.m_value = newValue;
                                }
                                else
                                {
                                    Node newNode = new Node(node.m_key, newValue, hashcode, node.m_next);
                                    if (prev == null)
                                    {
                                        tables.m_buckets[bucketNo] = newNode;
                                    }
                                    else
                                    {
                                        prev.m_next = newNode;
                                    }
                                }

                                return true;
                            }

                            return false;
                        }

                        prev = node;
                    }

                    return false;
                }
            }
        }

        public void Clear()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                Tables newTables = new Tables(new Node[DEFAULT_CAPACITY], m_tables.m_locks, new int[m_tables.m_countPerLock.Length], m_tables.m_comparer);
                m_tables = newTables;
                m_budget = Math.Max(1, newTables.m_buckets.Length / newTables.m_locks.Length);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", GetResource("ConcurrentDictionary_IndexIsNegative"));
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                int count = 0;
                for (int i = 0; i < m_tables.m_locks.Length && count >= 0; i++)
                {
                    count += m_tables.m_countPerLock[i];
                }

                if (array.Length - count < index || count < 0)
                {
                    throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayNotLargeEnough"));
                }

                CopyToPairs(array, index);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                int count = 0;
                checked
                {
                    for (int i = 0; i < m_tables.m_locks.Length; i++)
                    {
                        count += m_tables.m_countPerLock[i];
                    }
                }

                KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[count];
                CopyToPairs(array, 0);
                return array;
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        private void CopyToPairs(KeyValuePair<TKey, TValue>[] array, int index)
        {
            Node[] buckets = m_tables.m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current.m_next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    index++;
                }
            }
        }

        private void CopyToEntries(DictionaryEntry[] array, int index)
        {
            Node[] buckets = m_tables.m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current.m_next)
                {
                    array[index] = new DictionaryEntry(current.m_key, current.m_value);
                    index++;
                }
            }
        }

        private void CopyToObjects(object[] array, int index)
        {
            Node[] buckets = m_tables.m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                for (Node current = buckets[i]; current != null; current = current.m_next)
                {
                    array[index] = new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    index++;
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Node[] buckets = m_tables.m_buckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                Node current = Volatile.Read<Node>(ref buckets[i]);
                while (current != null)
                {
                    yield return new KeyValuePair<TKey, TValue>(current.m_key, current.m_value);
                    current = current.m_next;
                }
            }
        }

        private bool TryAddInternal(TKey key, TValue value, bool updateIfExists, bool acquireLock, out TValue resultingValue)
        {
            while (true)
            {
                int bucketNo, lockNo;
                int hashcode;
                Tables tables = m_tables;
                IEqualityComparer<TKey> comparer = tables.m_comparer;
                hashcode = comparer.GetHashCode(key);
                GetBucketAndLockNo(hashcode, out bucketNo, out lockNo, tables.m_buckets.Length, tables.m_locks.Length);
                bool resizeDesired = false;
                bool lockTaken = false;
                try
                {
                    if (acquireLock)
                        Monitor.Enter(tables.m_locks[lockNo], ref lockTaken);
                    if (tables != m_tables)
                    {
                        continue;
                    }

                    Node prev = null;
                    for (Node node = tables.m_buckets[bucketNo]; node != null; node = node.m_next)
                    {
                        Assert((prev == null && node == tables.m_buckets[bucketNo]) || prev.m_next == node);
                        if (comparer.Equals(node.m_key, key))
                        {
                            if (updateIfExists)
                            {
                                if (s_isValueWriteAtomic)
                                {
                                    node.m_value = value;
                                }
                                else
                                {
                                    Node newNode = new Node(node.m_key, value, hashcode, node.m_next);
                                    if (prev == null)
                                    {
                                        tables.m_buckets[bucketNo] = newNode;
                                    }
                                    else
                                    {
                                        prev.m_next = newNode;
                                    }
                                }

                                resultingValue = value;
                            }
                            else
                            {
                                resultingValue = node.m_value;
                            }

                            return false;
                        }

                        prev = node;
                    }

                    Volatile.Write<Node>(ref tables.m_buckets[bucketNo], new Node(key, value, hashcode, tables.m_buckets[bucketNo]));
                    checked
                    {
                        tables.m_countPerLock[lockNo]++;
                    }

                    if (tables.m_countPerLock[lockNo] > m_budget)
                    {
                        resizeDesired = true;
                    }
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(tables.m_locks[lockNo]);
                }

                if (resizeDesired)
                {
                    {
                        GrowTable(tables, tables.m_comparer, false, m_keyRehashCount);
                    }
                }

                resultingValue = value;
                return true;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                {
                    throw new KeyNotFoundException();
                }

                return value;
            }

            set
            {
                if (key == null)
                    throw new ArgumentNullException("key");
                TValue dummy;
                TryAddInternal(key, value, true, true, out dummy);
            }
        }

        public int Count
        {
            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
            get
            {
                int count = 0;
                int acquiredLocks = 0;
                try
                {
                    AcquireAllLocks(ref acquiredLocks);
                    for (int i = 0; i < m_tables.m_countPerLock.Length; i++)
                    {
                        count += m_tables.m_countPerLock[i];
                    }
                }
                finally
                {
                    ReleaseLocks(0, acquiredLocks);
                }

                return count;
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");
            TValue resultingValue;
            if (TryGetValue(key, out resultingValue))
            {
                return resultingValue;
            }

            TryAddInternal(key, valueFactory(key), false, true, out resultingValue);
            return resultingValue;
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            TValue resultingValue;
            TryAddInternal(key, value, false, true, out resultingValue);
            return resultingValue;
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (addValueFactory == null)
                throw new ArgumentNullException("addValueFactory");
            if (updateValueFactory == null)
                throw new ArgumentNullException("updateValueFactory");
            TValue newValue, resultingValue;
            while (true)
            {
                TValue oldValue;
                if (TryGetValue(key, out oldValue))
                {
                    newValue = updateValueFactory(key, oldValue);
                    if (TryUpdate(key, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else
                {
                    newValue = addValueFactory(key);
                    if (TryAddInternal(key, newValue, false, true, out resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }

        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (updateValueFactory == null)
                throw new ArgumentNullException("updateValueFactory");
            TValue newValue, resultingValue;
            while (true)
            {
                TValue oldValue;
                if (TryGetValue(key, out oldValue))
                {
                    newValue = updateValueFactory(key, oldValue);
                    if (TryUpdate(key, newValue, oldValue))
                    {
                        return newValue;
                    }
                }
                else
                {
                    if (TryAddInternal(key, addValue, false, true, out resultingValue))
                    {
                        return resultingValue;
                    }
                }
            }
        }

        public bool IsEmpty
        {
            [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "ConcurrencyCop just doesn't know about these locks")]
            get
            {
                int acquiredLocks = 0;
                try
                {
                    AcquireAllLocks(ref acquiredLocks);
                    for (int i = 0; i < m_tables.m_countPerLock.Length; i++)
                    {
                        if (m_tables.m_countPerLock[i] != 0)
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    ReleaseLocks(0, acquiredLocks);
                }

                return true;
            }
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
            {
                throw new ArgumentException(GetResource("ConcurrentDictionary_KeyAlreadyExisted"));
            }
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            TValue throwAwayValue;
            return TryRemove(key, out throwAwayValue);
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return GetKeys();
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                return GetKeys();
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return GetValues();
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                return GetValues();
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            ((IDictionary<TKey, TValue>)this).Add(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            TValue value;
            if (!TryGetValue(keyValuePair.Key, out value))
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(value, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (keyValuePair.Key == null)
                throw new ArgumentNullException(GetResource("ConcurrentDictionary_ItemKeyIsNull"));
            TValue throwAwayValue;
            return TryRemoveInternal(keyValuePair.Key, out throwAwayValue, true, keyValuePair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ConcurrentDictionary<TKey, TValue>)this).GetEnumerator();
        }

        void IDictionary.Add(object key, object value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (!(key is TKey))
                throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfKeyIncorrect"));
            TValue typedValue;
            try
            {
                typedValue = (TValue)value;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfValueIncorrect"));
            }

            ((IDictionary<TKey, TValue>)this).Add((TKey)key, typedValue);
        }

        bool IDictionary.Contains(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return (key is TKey) && ((ConcurrentDictionary<TKey, TValue>)this).ContainsKey((TKey)key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new DictionaryEnumerator(this);
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return GetKeys();
            }
        }

        void IDictionary.Remove(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            TValue throwAwayValue;
            if (key is TKey)
            {
                this.TryRemove((TKey)key, out throwAwayValue);
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                return GetValues();
            }
        }

        object IDictionary.this[object key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException("key");
                TValue value;
                if (key is TKey && this.TryGetValue((TKey)key, out value))
                {
                    return value;
                }

                return null;
            }

            set
            {
                if (key == null)
                    throw new ArgumentNullException("key");
                if (!(key is TKey))
                    throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfKeyIncorrect"));
                if (!(value is TValue))
                    throw new ArgumentException(GetResource("ConcurrentDictionary_TypeOfValueIncorrect"));
                ((ConcurrentDictionary<TKey, TValue>)this)[(TKey)key] = (TValue)value;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", GetResource("ConcurrentDictionary_IndexIsNegative"));
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                Tables tables = m_tables;
                int count = 0;
                for (int i = 0; i < tables.m_locks.Length && count >= 0; i++)
                {
                    count += tables.m_countPerLock[i];
                }

                if (array.Length - count < index || count < 0)
                {
                    throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayNotLargeEnough"));
                }

                KeyValuePair<TKey, TValue>[] pairs = array as KeyValuePair<TKey, TValue>[];
                if (pairs != null)
                {
                    CopyToPairs(pairs, index);
                    return;
                }

                DictionaryEntry[] entries = array as DictionaryEntry[];
                if (entries != null)
                {
                    CopyToEntries(entries, index);
                    return;
                }

                object[] objects = array as object[];
                if (objects != null)
                {
                    CopyToObjects(objects, index);
                    return;
                }

                throw new ArgumentException(GetResource("ConcurrentDictionary_ArrayIncorrectType"), "array");
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("ConcurrentCollection_SyncRoot_NotSupported"));
            }
        }

        private void GrowTable(Tables tables, IEqualityComparer<TKey> newComparer, bool regenerateHashKeys, int rehashCount)
        {
            int locksAcquired = 0;
            try
            {
                AcquireLocks(0, 1, ref locksAcquired);
                if (regenerateHashKeys && rehashCount == m_keyRehashCount)
                {
                    tables = m_tables;
                }
                else
                {
                    if (tables != m_tables)
                    {
                        return;
                    }

                    long approxCount = 0;
                    for (int i = 0; i < tables.m_countPerLock.Length; i++)
                    {
                        approxCount += tables.m_countPerLock[i];
                    }

                    if (approxCount < tables.m_buckets.Length / 4)
                    {
                        m_budget = 2 * m_budget;
                        if (m_budget < 0)
                        {
                            m_budget = int.MaxValue;
                        }

                        return;
                    }
                }

                int newLength = 0;
                bool maximizeTableSize = false;
                try
                {
                    checked
                    {
                        newLength = tables.m_buckets.Length * 2 + 1;
                        while (newLength % 3 == 0 || newLength % 5 == 0 || newLength % 7 == 0)
                        {
                            newLength += 2;
                        }

                        Assert(newLength % 2 != 0);
                        if (newLength > Array.MaxArrayLength)
                        {
                            maximizeTableSize = true;
                        }
                    }
                }
                catch (OverflowException)
                {
                    maximizeTableSize = true;
                }

                if (maximizeTableSize)
                {
                    newLength = Array.MaxArrayLength;
                    m_budget = int.MaxValue;
                }

                AcquireLocks(1, tables.m_locks.Length, ref locksAcquired);
                object[] newLocks = tables.m_locks;
                if (m_growLockArray && tables.m_locks.Length < MAX_LOCK_NUMBER)
                {
                    newLocks = new object[tables.m_locks.Length * 2];
                    Array.Copy(tables.m_locks, newLocks, tables.m_locks.Length);
                    for (int i = tables.m_locks.Length; i < newLocks.Length; i++)
                    {
                        newLocks[i] = new object ();
                    }
                }

                Node[] newBuckets = new Node[newLength];
                int[] newCountPerLock = new int[newLocks.Length];
                for (int i = 0; i < tables.m_buckets.Length; i++)
                {
                    Node current = tables.m_buckets[i];
                    while (current != null)
                    {
                        Node next = current.m_next;
                        int newBucketNo, newLockNo;
                        int nodeHashCode = current.m_hashcode;
                        if (regenerateHashKeys)
                        {
                            nodeHashCode = newComparer.GetHashCode(current.m_key);
                        }

                        GetBucketAndLockNo(nodeHashCode, out newBucketNo, out newLockNo, newBuckets.Length, newLocks.Length);
                        newBuckets[newBucketNo] = new Node(current.m_key, current.m_value, nodeHashCode, newBuckets[newBucketNo]);
                        checked
                        {
                            newCountPerLock[newLockNo]++;
                        }

                        current = next;
                    }
                }

                if (regenerateHashKeys)
                {
                    unchecked
                    {
                        m_keyRehashCount++;
                    }
                }

                m_budget = Math.Max(1, newBuckets.Length / newLocks.Length);
                m_tables = new Tables(newBuckets, newLocks, newCountPerLock, newComparer);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        private void GetBucketAndLockNo(int hashcode, out int bucketNo, out int lockNo, int bucketCount, int lockCount)
        {
            bucketNo = (hashcode & 0x7fffffff) % bucketCount;
            lockNo = bucketNo % lockCount;
            Assert(bucketNo >= 0 && bucketNo < bucketCount);
            Assert(lockNo >= 0 && lockNo < lockCount);
        }

        private static int DefaultConcurrencyLevel
        {
            get
            {
                return DEFAULT_CONCURRENCY_MULTIPLIER * PlatformHelper.ProcessorCount;
            }
        }

        private void AcquireAllLocks(ref int locksAcquired)
        {
            AcquireLocks(0, 1, ref locksAcquired);
            AcquireLocks(1, m_tables.m_locks.Length, ref locksAcquired);
            Assert(locksAcquired == m_tables.m_locks.Length);
        }

        private void AcquireLocks(int fromInclusive, int toExclusive, ref int locksAcquired)
        {
            Assert(fromInclusive <= toExclusive);
            object[] locks = m_tables.m_locks;
            for (int i = fromInclusive; i < toExclusive; i++)
            {
                bool lockTaken = false;
                try
                {
                    Monitor.Enter(locks[i], ref lockTaken);
                }
                finally
                {
                    if (lockTaken)
                    {
                        locksAcquired++;
                    }
                }
            }
        }

        private void ReleaseLocks(int fromInclusive, int toExclusive)
        {
            Assert(fromInclusive <= toExclusive);
            for (int i = fromInclusive; i < toExclusive; i++)
            {
                Monitor.Exit(m_tables.m_locks[i]);
            }
        }

        private ReadOnlyCollection<TKey> GetKeys()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                List<TKey> keys = new List<TKey>();
                for (int i = 0; i < m_tables.m_buckets.Length; i++)
                {
                    Node current = m_tables.m_buckets[i];
                    while (current != null)
                    {
                        keys.Add(current.m_key);
                        current = current.m_next;
                    }
                }

                return new ReadOnlyCollection<TKey>(keys);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        private ReadOnlyCollection<TValue> GetValues()
        {
            int locksAcquired = 0;
            try
            {
                AcquireAllLocks(ref locksAcquired);
                List<TValue> values = new List<TValue>();
                for (int i = 0; i < m_tables.m_buckets.Length; i++)
                {
                    Node current = m_tables.m_buckets[i];
                    while (current != null)
                    {
                        values.Add(current.m_value);
                        current = current.m_next;
                    }
                }

                return new ReadOnlyCollection<TValue>(values);
            }
            finally
            {
                ReleaseLocks(0, locksAcquired);
            }
        }

        private void Assert(bool condition)
        {
            Contract.Assert(condition);
        }

        private string GetResource(string key)
        {
            Assert(key != null);
            return Environment.GetResourceString(key);
        }

        private class Node
        {
            internal TKey m_key;
            internal TValue m_value;
            internal volatile Node m_next;
            internal int m_hashcode;
            internal Node(TKey key, TValue value, int hashcode, Node next)
            {
                m_key = key;
                m_value = value;
                m_next = next;
                m_hashcode = hashcode;
            }
        }

        private class DictionaryEnumerator : IDictionaryEnumerator
        {
            IEnumerator<KeyValuePair<TKey, TValue>> m_enumerator;
            internal DictionaryEnumerator(ConcurrentDictionary<TKey, TValue> dictionary)
            {
                m_enumerator = dictionary.GetEnumerator();
            }

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(m_enumerator.Current.Key, m_enumerator.Current.Value);
                }
            }

            public object Key
            {
                get
                {
                    return m_enumerator.Current.Key;
                }
            }

            public object Value
            {
                get
                {
                    return m_enumerator.Current.Value;
                }
            }

            public object Current
            {
                get
                {
                    return this.Entry;
                }
            }

            public bool MoveNext()
            {
                return m_enumerator.MoveNext();
            }

            public void Reset()
            {
                m_enumerator.Reset();
            }
        }
    }
}