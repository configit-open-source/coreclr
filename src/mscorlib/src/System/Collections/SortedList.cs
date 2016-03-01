namespace System.Collections
{
    using System;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    public class SortedList : IDictionary, ICloneable
    {
        private Object[] keys;
        private Object[] values;
        private int _size;
        private int version;
        private IComparer comparer;
        private KeyList keyList;
        private ValueList valueList;
        private Object _syncRoot;
        private const int _defaultCapacity = 16;
        private static Object[] emptyArray = EmptyArray<Object>.Value;
        public SortedList()
        {
            Init();
        }

        private void Init()
        {
            keys = emptyArray;
            values = emptyArray;
            _size = 0;
            comparer = new Comparer(CultureInfo.CurrentCulture);
        }

        public SortedList(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException("initialCapacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            keys = new Object[initialCapacity];
            values = new Object[initialCapacity];
            comparer = new Comparer(CultureInfo.CurrentCulture);
        }

        public SortedList(IComparer comparer): this ()
        {
            if (comparer != null)
                this.comparer = comparer;
        }

        public SortedList(IComparer comparer, int capacity): this (comparer)
        {
            Capacity = capacity;
        }

        public SortedList(IDictionary d): this (d, null)
        {
        }

        public SortedList(IDictionary d, IComparer comparer): this (comparer, (d != null ? d.Count : 0))
        {
            if (d == null)
                throw new ArgumentNullException("d", Environment.GetResourceString("ArgumentNull_Dictionary"));
            Contract.EndContractBlock();
            d.Keys.CopyTo(keys, 0);
            d.Values.CopyTo(values, 0);
            Array.Sort(keys, values, comparer);
            _size = d.Count;
        }

        public virtual void Add(Object key, Object value)
        {
            if (key == null)
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            Contract.EndContractBlock();
            int i = Array.BinarySearch(keys, 0, _size, key, comparer);
            if (i >= 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate__", GetKey(i), key));
            Insert(~i, key, value);
        }

        public virtual int Capacity
        {
            get
            {
                return keys.Length;
            }

            set
            {
                if (value < Count)
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }

                Contract.EndContractBlock();
                if (value != keys.Length)
                {
                    if (value > 0)
                    {
                        Object[] newKeys = new Object[value];
                        Object[] newValues = new Object[value];
                        if (_size > 0)
                        {
                            Array.Copy(keys, 0, newKeys, 0, _size);
                            Array.Copy(values, 0, newValues, 0, _size);
                        }

                        keys = newKeys;
                        values = newValues;
                    }
                    else
                    {
                        Contract.Assert(_size == 0, "Size is not zero");
                        keys = emptyArray;
                        values = emptyArray;
                    }
                }
            }
        }

        public virtual int Count
        {
            get
            {
                return _size;
            }
        }

        public virtual ICollection Keys
        {
            get
            {
                return GetKeyList();
            }
        }

        public virtual ICollection Values
        {
            get
            {
                return GetValueList();
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public virtual Object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }

                return _syncRoot;
            }
        }

        public virtual void Clear()
        {
            version++;
            Array.Clear(keys, 0, _size);
            Array.Clear(values, 0, _size);
            _size = 0;
        }

        public virtual Object Clone()
        {
            SortedList sl = new SortedList(_size);
            Array.Copy(keys, 0, sl.keys, 0, _size);
            Array.Copy(values, 0, sl.values, 0, _size);
            sl._size = _size;
            sl.version = version;
            sl.comparer = comparer;
            return sl;
        }

        public virtual bool Contains(Object key)
        {
            return IndexOfKey(key) >= 0;
        }

        public virtual bool ContainsKey(Object key)
        {
            return IndexOfKey(key) >= 0;
        }

        public virtual bool ContainsValue(Object value)
        {
            return IndexOfValue(value) >= 0;
        }

        public virtual void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Array"));
            if (array.Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayPlusOffTooSmall"));
            Contract.EndContractBlock();
            for (int i = 0; i < Count; i++)
            {
                DictionaryEntry entry = new DictionaryEntry(keys[i], values[i]);
                array.SetValue(entry, i + arrayIndex);
            }
        }

        internal virtual KeyValuePairs[] ToKeyValuePairsArray()
        {
            KeyValuePairs[] array = new KeyValuePairs[Count];
            for (int i = 0; i < Count; i++)
            {
                array[i] = new KeyValuePairs(keys[i], values[i]);
            }

            return array;
        }

        private void EnsureCapacity(int min)
        {
            int newCapacity = keys.Length == 0 ? 16 : keys.Length * 2;
            if ((uint)newCapacity > Array.MaxArrayLength)
                newCapacity = Array.MaxArrayLength;
            if (newCapacity < min)
                newCapacity = min;
            Capacity = newCapacity;
        }

        public virtual Object GetByIndex(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            return values[index];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SortedListEnumerator(this, 0, _size, SortedListEnumerator.DictEntry);
        }

        public virtual IDictionaryEnumerator GetEnumerator()
        {
            return new SortedListEnumerator(this, 0, _size, SortedListEnumerator.DictEntry);
        }

        public virtual Object GetKey(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            return keys[index];
        }

        public virtual IList GetKeyList()
        {
            if (keyList == null)
                keyList = new KeyList(this);
            return keyList;
        }

        public virtual IList GetValueList()
        {
            if (valueList == null)
                valueList = new ValueList(this);
            return valueList;
        }

        public virtual Object this[Object key]
        {
            get
            {
                int i = IndexOfKey(key);
                if (i >= 0)
                    return values[i];
                return null;
            }

            set
            {
                if (key == null)
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                Contract.EndContractBlock();
                int i = Array.BinarySearch(keys, 0, _size, key, comparer);
                if (i >= 0)
                {
                    values[i] = value;
                    version++;
                    return;
                }

                Insert(~i, key, value);
            }
        }

        public virtual int IndexOfKey(Object key)
        {
            if (key == null)
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            Contract.EndContractBlock();
            int ret = Array.BinarySearch(keys, 0, _size, key, comparer);
            return ret >= 0 ? ret : -1;
        }

        public virtual int IndexOfValue(Object value)
        {
            return Array.IndexOf(values, value, 0, _size);
        }

        private void Insert(int index, Object key, Object value)
        {
            if (_size == keys.Length)
                EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(keys, index, keys, index + 1, _size - index);
                Array.Copy(values, index, values, index + 1, _size - index);
            }

            keys[index] = key;
            values[index] = value;
            _size++;
            version++;
        }

        public virtual void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            _size--;
            if (index < _size)
            {
                Array.Copy(keys, index + 1, keys, index, _size - index);
                Array.Copy(values, index + 1, values, index, _size - index);
            }

            keys[_size] = null;
            values[_size] = null;
            version++;
        }

        public virtual void Remove(Object key)
        {
            int i = IndexOfKey(key);
            if (i >= 0)
                RemoveAt(i);
        }

        public virtual void SetByIndex(int index, Object value)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            Contract.EndContractBlock();
            values[index] = value;
            version++;
        }

        public static SortedList Synchronized(SortedList list)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.EndContractBlock();
            return new SyncSortedList(list);
        }

        public virtual void TrimToSize()
        {
            Capacity = _size;
        }

        private class SyncSortedList : SortedList
        {
            private SortedList _list;
            private Object _root;
            internal SyncSortedList(SortedList list)
            {
                _list = list;
                _root = list.SyncRoot;
            }

            public override int Count
            {
                get
                {
                    lock (_root)
                    {
                        return _list.Count;
                    }
                }
            }

            public override Object SyncRoot
            {
                get
                {
                    return _root;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return _list.IsReadOnly;
                }
            }

            public override bool IsFixedSize
            {
                get
                {
                    return _list.IsFixedSize;
                }
            }

            public override bool IsSynchronized
            {
                get
                {
                    return true;
                }
            }

            public override Object this[Object key]
            {
                get
                {
                    lock (_root)
                    {
                        return _list[key];
                    }
                }

                set
                {
                    lock (_root)
                    {
                        _list[key] = value;
                    }
                }
            }

            public override void Add(Object key, Object value)
            {
                lock (_root)
                {
                    _list.Add(key, value);
                }
            }

            public override int Capacity
            {
                get
                {
                    lock (_root)
                    {
                        return _list.Capacity;
                    }
                }
            }

            public override void Clear()
            {
                lock (_root)
                {
                    _list.Clear();
                }
            }

            public override Object Clone()
            {
                lock (_root)
                {
                    return _list.Clone();
                }
            }

            public override bool Contains(Object key)
            {
                lock (_root)
                {
                    return _list.Contains(key);
                }
            }

            public override bool ContainsKey(Object key)
            {
                lock (_root)
                {
                    return _list.ContainsKey(key);
                }
            }

            public override bool ContainsValue(Object key)
            {
                lock (_root)
                {
                    return _list.ContainsValue(key);
                }
            }

            public override void CopyTo(Array array, int index)
            {
                lock (_root)
                {
                    _list.CopyTo(array, index);
                }
            }

            public override Object GetByIndex(int index)
            {
                lock (_root)
                {
                    return _list.GetByIndex(index);
                }
            }

            public override IDictionaryEnumerator GetEnumerator()
            {
                lock (_root)
                {
                    return _list.GetEnumerator();
                }
            }

            public override Object GetKey(int index)
            {
                lock (_root)
                {
                    return _list.GetKey(index);
                }
            }

            public override IList GetKeyList()
            {
                lock (_root)
                {
                    return _list.GetKeyList();
                }
            }

            public override IList GetValueList()
            {
                lock (_root)
                {
                    return _list.GetValueList();
                }
            }

            public override int IndexOfKey(Object key)
            {
                if (key == null)
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                Contract.EndContractBlock();
                lock (_root)
                {
                    return _list.IndexOfKey(key);
                }
            }

            public override int IndexOfValue(Object value)
            {
                lock (_root)
                {
                    return _list.IndexOfValue(value);
                }
            }

            public override void RemoveAt(int index)
            {
                lock (_root)
                {
                    _list.RemoveAt(index);
                }
            }

            public override void Remove(Object key)
            {
                lock (_root)
                {
                    _list.Remove(key);
                }
            }

            public override void SetByIndex(int index, Object value)
            {
                lock (_root)
                {
                    _list.SetByIndex(index, value);
                }
            }

            internal override KeyValuePairs[] ToKeyValuePairsArray()
            {
                return _list.ToKeyValuePairsArray();
            }

            public override void TrimToSize()
            {
                lock (_root)
                {
                    _list.TrimToSize();
                }
            }
        }

        private class SortedListEnumerator : IDictionaryEnumerator, ICloneable
        {
            private SortedList sortedList;
            private Object key;
            private Object value;
            private int index;
            private int startIndex;
            private int endIndex;
            private int version;
            private bool current;
            private int getObjectRetType;
            internal const int Keys = 1;
            internal const int Values = 2;
            internal const int DictEntry = 3;
            internal SortedListEnumerator(SortedList sortedList, int index, int count, int getObjRetType)
            {
                this.sortedList = sortedList;
                this.index = index;
                startIndex = index;
                endIndex = index + count;
                version = sortedList.version;
                getObjectRetType = getObjRetType;
                current = false;
            }

            public Object Clone()
            {
                return MemberwiseClone();
            }

            public virtual Object Key
            {
                get
                {
                    if (version != sortedList.version)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                    if (current == false)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    return key;
                }
            }

            public virtual bool MoveNext()
            {
                if (version != sortedList.version)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                if (index < endIndex)
                {
                    key = sortedList.keys[index];
                    value = sortedList.values[index];
                    index++;
                    current = true;
                    return true;
                }

                key = null;
                value = null;
                current = false;
                return false;
            }

            public virtual DictionaryEntry Entry
            {
                get
                {
                    if (version != sortedList.version)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                    if (current == false)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    return new DictionaryEntry(key, value);
                }
            }

            public virtual Object Current
            {
                get
                {
                    if (current == false)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    if (getObjectRetType == Keys)
                        return key;
                    else if (getObjectRetType == Values)
                        return value;
                    else
                        return new DictionaryEntry(key, value);
                }
            }

            public virtual Object Value
            {
                get
                {
                    if (version != sortedList.version)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                    if (current == false)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    return value;
                }
            }

            public virtual void Reset()
            {
                if (version != sortedList.version)
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                index = startIndex;
                current = false;
                key = null;
                value = null;
            }
        }

        private class KeyList : IList
        {
            private SortedList sortedList;
            internal KeyList(SortedList sortedList)
            {
                this.sortedList = sortedList;
            }

            public virtual int Count
            {
                get
                {
                    return sortedList._size;
                }
            }

            public virtual bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public virtual bool IsFixedSize
            {
                get
                {
                    return true;
                }
            }

            public virtual bool IsSynchronized
            {
                get
                {
                    return sortedList.IsSynchronized;
                }
            }

            public virtual Object SyncRoot
            {
                get
                {
                    return sortedList.SyncRoot;
                }
            }

            public virtual int Add(Object key)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }

            public virtual void Clear()
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }

            public virtual bool Contains(Object key)
            {
                return sortedList.Contains(key);
            }

            public virtual void CopyTo(Array array, int arrayIndex)
            {
                if (array != null && array.Rank != 1)
                    throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
                Contract.EndContractBlock();
                Array.Copy(sortedList.keys, 0, array, arrayIndex, sortedList.Count);
            }

            public virtual void Insert(int index, Object value)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }

            public virtual Object this[int index]
            {
                get
                {
                    return sortedList.GetKey(index);
                }

                set
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_KeyCollectionSet"));
                }
            }

            public virtual IEnumerator GetEnumerator()
            {
                return new SortedListEnumerator(sortedList, 0, sortedList.Count, SortedListEnumerator.Keys);
            }

            public virtual int IndexOf(Object key)
            {
                if (key == null)
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                Contract.EndContractBlock();
                int i = Array.BinarySearch(sortedList.keys, 0, sortedList.Count, key, sortedList.comparer);
                if (i >= 0)
                    return i;
                return -1;
            }

            public virtual void Remove(Object key)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }

            public virtual void RemoveAt(int index)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }
        }

        private class ValueList : IList
        {
            private SortedList sortedList;
            internal ValueList(SortedList sortedList)
            {
                this.sortedList = sortedList;
            }

            public virtual int Count
            {
                get
                {
                    return sortedList._size;
                }
            }

            public virtual bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public virtual bool IsFixedSize
            {
                get
                {
                    return true;
                }
            }

            public virtual bool IsSynchronized
            {
                get
                {
                    return sortedList.IsSynchronized;
                }
            }

            public virtual Object SyncRoot
            {
                get
                {
                    return sortedList.SyncRoot;
                }
            }

            public virtual int Add(Object key)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }

            public virtual void Clear()
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }

            public virtual bool Contains(Object value)
            {
                return sortedList.ContainsValue(value);
            }

            public virtual void CopyTo(Array array, int arrayIndex)
            {
                if (array != null && array.Rank != 1)
                    throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
                Contract.EndContractBlock();
                Array.Copy(sortedList.values, 0, array, arrayIndex, sortedList.Count);
            }

            public virtual void Insert(int index, Object value)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }

            public virtual Object this[int index]
            {
                get
                {
                    return sortedList.GetByIndex(index);
                }

                set
                {
                    throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
                }
            }

            public virtual IEnumerator GetEnumerator()
            {
                return new SortedListEnumerator(sortedList, 0, sortedList.Count, SortedListEnumerator.Values);
            }

            public virtual int IndexOf(Object value)
            {
                return Array.IndexOf(sortedList.values, value, 0, sortedList.Count);
            }

            public virtual void Remove(Object value)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }

            public virtual void RemoveAt(int index)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_SortedListNestedWrite));
            }
        }

        internal class SortedListDebugView
        {
            private SortedList sortedList;
            public SortedListDebugView(SortedList sortedList)
            {
                if (sortedList == null)
                {
                    throw new ArgumentNullException("sortedList");
                }

                Contract.EndContractBlock();
                this.sortedList = sortedList;
            }

            public KeyValuePairs[] Items
            {
                get
                {
                    return sortedList.ToKeyValuePairsArray();
                }
            }
        }
    }
}