

namespace System.Collections
{
    internal sealed class EmptyReadOnlyDictionaryInternal : IDictionary
    {
        public EmptyReadOnlyDictionaryInternal()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new NodeEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < this.Count)
                throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_Index"), "index");
                    }

        public int Count
        {
            get
            {
                return 0;
            }
        }

        public Object SyncRoot
        {
            get
            {
                return this;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public Object this[Object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                }

                                return null;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                }

                if (!key.GetType().IsSerializable)
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "key");
                if ((value != null) && (!value.GetType().IsSerializable))
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "value");
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }
        }

        public ICollection Keys
        {
            get
            {
                return EmptyArray<Object>.Value;
            }
        }

        public ICollection Values
        {
            get
            {
                return EmptyArray<Object>.Value;
            }
        }

        public bool Contains(Object key)
        {
            return false;
        }

        public void Add(Object key, Object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            }

            if (!key.GetType().IsSerializable)
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "key");
            if ((value != null) && (!value.GetType().IsSerializable))
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSerializable"), "value");
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
        }

        public void Clear()
        {
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return true;
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new NodeEnumerator();
        }

        public void Remove(Object key)
        {
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
        }

        private sealed class NodeEnumerator : IDictionaryEnumerator
        {
            public NodeEnumerator()
            {
            }

            public bool MoveNext()
            {
                return false;
            }

            public Object Current
            {
                get
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
            }

            public void Reset()
            {
            }

            public Object Key
            {
                get
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
            }

            public Object Value
            {
                get
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }
            }
        }
    }
}