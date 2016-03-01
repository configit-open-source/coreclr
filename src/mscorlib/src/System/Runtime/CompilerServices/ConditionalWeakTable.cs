using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    public sealed class ConditionalWeakTable<TKey, TValue>
        where TKey : class where TValue : class
    {
        public ConditionalWeakTable()
        {
            _buckets = Array.Empty<int>();
            _entries = Array.Empty<Entry>();
            _freeList = -1;
            _lock = new Object();
            Resize();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            lock (_lock)
            {
                VerifyIntegrity();
                return TryGetValueWorker(key, out value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            lock (_lock)
            {
                VerifyIntegrity();
                _invalid = true;
                int entryIndex = FindEntry(key);
                if (entryIndex != -1)
                {
                    _invalid = false;
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_AddingDuplicate);
                }

                CreateEntry(key, value);
                _invalid = false;
            }
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.key);
            }

            lock (_lock)
            {
                VerifyIntegrity();
                _invalid = true;
                int hashCode = RuntimeHelpers.GetHashCode(key) & Int32.MaxValue;
                int bucket = hashCode % _buckets.Length;
                int last = -1;
                for (int entriesIndex = _buckets[bucket]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next)
                {
                    if (_entries[entriesIndex].hashCode == hashCode && _entries[entriesIndex].depHnd.GetPrimary() == key)
                    {
                        if (last == -1)
                        {
                            _buckets[bucket] = _entries[entriesIndex].next;
                        }
                        else
                        {
                            _entries[last].next = _entries[entriesIndex].next;
                        }

                        _entries[entriesIndex].depHnd.Free();
                        _entries[entriesIndex].next = _freeList;
                        _freeList = entriesIndex;
                        _invalid = false;
                        return true;
                    }

                    last = entriesIndex;
                }

                _invalid = false;
                return false;
            }
        }

        public TValue GetValue(TKey key, CreateValueCallback createValueCallback)
        {
            if (createValueCallback == null)
            {
                throw new ArgumentNullException("createValueCallback");
            }

            TValue existingValue;
            if (TryGetValue(key, out existingValue))
            {
                return existingValue;
            }

            TValue newValue = createValueCallback(key);
            lock (_lock)
            {
                VerifyIntegrity();
                _invalid = true;
                if (TryGetValueWorker(key, out existingValue))
                {
                    _invalid = false;
                    return existingValue;
                }
                else
                {
                    CreateEntry(key, newValue);
                    _invalid = false;
                    return newValue;
                }
            }
        }

        public TValue GetOrCreateValue(TKey key)
        {
            return GetValue(key, k => Activator.CreateInstance<TValue>());
        }

        public delegate TValue CreateValueCallback(TKey key);
        internal TKey FindEquivalentKeyUnsafe(TKey key, out TValue value)
        {
            lock (_lock)
            {
                for (int bucket = 0; bucket < _buckets.Length; ++bucket)
                {
                    for (int entriesIndex = _buckets[bucket]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next)
                    {
                        object thisKey, thisValue;
                        _entries[entriesIndex].depHnd.GetPrimaryAndSecondary(out thisKey, out thisValue);
                        if (Object.Equals(thisKey, key))
                        {
                            value = (TValue)thisValue;
                            return (TKey)thisKey;
                        }
                    }
                }
            }

            value = default (TValue);
            return null;
        }

        internal ICollection<TKey> Keys
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                List<TKey> list = new List<TKey>();
                lock (_lock)
                {
                    for (int bucket = 0; bucket < _buckets.Length; ++bucket)
                    {
                        for (int entriesIndex = _buckets[bucket]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next)
                        {
                            TKey thisKey = (TKey)_entries[entriesIndex].depHnd.GetPrimary();
                            if (thisKey != null)
                            {
                                list.Add(thisKey);
                            }
                        }
                    }
                }

                return list;
            }
        }

        internal ICollection<TValue> Values
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                List<TValue> list = new List<TValue>();
                lock (_lock)
                {
                    for (int bucket = 0; bucket < _buckets.Length; ++bucket)
                    {
                        for (int entriesIndex = _buckets[bucket]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next)
                        {
                            Object primary = null;
                            Object secondary = null;
                            _entries[entriesIndex].depHnd.GetPrimaryAndSecondary(out primary, out secondary);
                            if (primary != null)
                            {
                                list.Add((TValue)secondary);
                            }
                        }
                    }
                }

                return list;
            }
        }

        internal void Clear()
        {
            lock (_lock)
            {
                for (int bucketIndex = 0; bucketIndex < _buckets.Length; bucketIndex++)
                {
                    _buckets[bucketIndex] = -1;
                }

                int entriesIndex;
                for (entriesIndex = 0; entriesIndex < _entries.Length; entriesIndex++)
                {
                    if (_entries[entriesIndex].depHnd.IsAllocated)
                    {
                        _entries[entriesIndex].depHnd.Free();
                    }

                    _entries[entriesIndex].next = entriesIndex - 1;
                }

                _freeList = entriesIndex - 1;
            }
        }

        private bool TryGetValueWorker(TKey key, out TValue value)
        {
            int entryIndex = FindEntry(key);
            if (entryIndex != -1)
            {
                Object primary = null;
                Object secondary = null;
                _entries[entryIndex].depHnd.GetPrimaryAndSecondary(out primary, out secondary);
                if (primary != null)
                {
                    value = (TValue)secondary;
                    return true;
                }
            }

            value = default (TValue);
            return false;
        }

        private void CreateEntry(TKey key, TValue value)
        {
            if (_freeList == -1)
            {
                Resize();
            }

            int hashCode = RuntimeHelpers.GetHashCode(key) & Int32.MaxValue;
            int bucket = hashCode % _buckets.Length;
            int newEntry = _freeList;
            _freeList = _entries[newEntry].next;
            _entries[newEntry].hashCode = hashCode;
            _entries[newEntry].depHnd = new DependentHandle(key, value);
            _entries[newEntry].next = _buckets[bucket];
            _buckets[bucket] = newEntry;
        }

        private void Resize()
        {
            int newSize = _buckets.Length;
            bool hasExpiredEntries = false;
            int entriesIndex;
            for (entriesIndex = 0; entriesIndex < _entries.Length; entriesIndex++)
            {
                if (_entries[entriesIndex].depHnd.IsAllocated && _entries[entriesIndex].depHnd.GetPrimary() == null)
                {
                    hasExpiredEntries = true;
                    break;
                }
            }

            if (!hasExpiredEntries)
            {
                newSize = System.Collections.HashHelpers.GetPrime(_buckets.Length == 0 ? _initialCapacity + 1 : _buckets.Length * 2);
            }

            int newFreeList = -1;
            int[] newBuckets = new int[newSize];
            for (int bucketIndex = 0; bucketIndex < newSize; bucketIndex++)
            {
                newBuckets[bucketIndex] = -1;
            }

            Entry[] newEntries = new Entry[newSize];
            for (entriesIndex = 0; entriesIndex < _entries.Length; entriesIndex++)
            {
                DependentHandle depHnd = _entries[entriesIndex].depHnd;
                if (depHnd.IsAllocated && depHnd.GetPrimary() != null)
                {
                    int bucket = _entries[entriesIndex].hashCode % newSize;
                    newEntries[entriesIndex].depHnd = depHnd;
                    newEntries[entriesIndex].hashCode = _entries[entriesIndex].hashCode;
                    newEntries[entriesIndex].next = newBuckets[bucket];
                    newBuckets[bucket] = entriesIndex;
                }
                else
                {
                    _entries[entriesIndex].depHnd.Free();
                    newEntries[entriesIndex].depHnd = new DependentHandle();
                    newEntries[entriesIndex].next = newFreeList;
                    newFreeList = entriesIndex;
                }
            }

            while (entriesIndex != newEntries.Length)
            {
                newEntries[entriesIndex].depHnd = new DependentHandle();
                newEntries[entriesIndex].next = newFreeList;
                newFreeList = entriesIndex;
                entriesIndex++;
            }

            _buckets = newBuckets;
            _entries = newEntries;
            _freeList = newFreeList;
        }

        private int FindEntry(TKey key)
        {
            int hashCode = RuntimeHelpers.GetHashCode(key) & Int32.MaxValue;
            for (int entriesIndex = _buckets[hashCode % _buckets.Length]; entriesIndex != -1; entriesIndex = _entries[entriesIndex].next)
            {
                if (_entries[entriesIndex].hashCode == hashCode && _entries[entriesIndex].depHnd.GetPrimary() == key)
                {
                    return entriesIndex;
                }
            }

            return -1;
        }

        private void VerifyIntegrity()
        {
            if (_invalid)
            {
                throw new InvalidOperationException(Environment.GetResourceString("CollectionCorrupted"));
            }
        }

        ~ConditionalWeakTable()
        {
            if (Environment.HasShutdownStarted)
            {
                return;
            }

            if (_lock != null)
            {
                lock (_lock)
                {
                    if (_invalid)
                    {
                        return;
                    }

                    Entry[] entries = _entries;
                    _invalid = true;
                    _entries = null;
                    _buckets = null;
                    for (int entriesIndex = 0; entriesIndex < entries.Length; entriesIndex++)
                    {
                        entries[entriesIndex].depHnd.Free();
                    }
                }
            }
        }

        private struct Entry
        {
            public DependentHandle depHnd;
            public int hashCode;
            public int next;
        }

        private int[] _buckets;
        private Entry[] _entries;
        private int _freeList;
        private const int _initialCapacity = 5;
        private readonly Object _lock;
        private bool _invalid;
    }

    struct DependentHandle
    {
        public DependentHandle(Object primary, Object secondary)
        {
            IntPtr handle = (IntPtr)0;
            nInitialize(primary, secondary, out handle);
            _handle = handle;
        }

        public bool IsAllocated
        {
            get
            {
                return _handle != (IntPtr)0;
            }
        }

        public Object GetPrimary()
        {
            Object primary;
            nGetPrimary(_handle, out primary);
            return primary;
        }

        public void GetPrimaryAndSecondary(out Object primary, out Object secondary)
        {
            nGetPrimaryAndSecondary(_handle, out primary, out secondary);
        }

        public void Free()
        {
            if (_handle != (IntPtr)0)
            {
                IntPtr handle = _handle;
                _handle = (IntPtr)0;
                nFree(handle);
            }
        }

        private static extern void nInitialize(Object primary, Object secondary, out IntPtr dependentHandle);
        private static extern void nGetPrimary(IntPtr dependentHandle, out Object primary);
        private static extern void nGetPrimaryAndSecondary(IntPtr dependentHandle, out Object primary, out Object secondary);
        private static extern void nFree(IntPtr dependentHandle);
        private IntPtr _handle;
    }
}