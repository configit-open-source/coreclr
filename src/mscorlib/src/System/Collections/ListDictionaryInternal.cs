

namespace System.Collections
{
    internal class ListDictionaryInternal : IDictionary
    {
        DictionaryNode head;
        int version;
        int count;
        private Object _syncRoot;
        public ListDictionaryInternal()
        {
        }

        public Object this[Object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                }

                                DictionaryNode node = head;
                while (node != null)
                {
                    if (node.key.Equals(key))
                    {
                        return node.value;
                    }

                    node = node.next;
                }

                return null;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
                }

                                version++;
                DictionaryNode last = null;
                DictionaryNode node;
                for (node = head; node != null; node = node.next)
                {
                    if (node.key.Equals(key))
                    {
                        break;
                    }

                    last = node;
                }

                if (node != null)
                {
                    node.value = value;
                    return;
                }

                DictionaryNode newNode = new DictionaryNode();
                newNode.key = key;
                newNode.value = value;
                if (last != null)
                {
                    last.next = newNode;
                }
                else
                {
                    head = newNode;
                }

                count++;
            }
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public ICollection Keys
        {
            get
            {
                return new NodeKeyValueCollection(this, true);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public Object SyncRoot
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

        public ICollection Values
        {
            get
            {
                return new NodeKeyValueCollection(this, false);
            }
        }

        public void Add(Object key, Object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            }

                        version++;
            DictionaryNode last = null;
            DictionaryNode node;
            for (node = head; node != null; node = node.next)
            {
                if (node.key.Equals(key))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate__", node.key, key));
                }

                last = node;
            }

            if (node != null)
            {
                node.value = value;
                return;
            }

            DictionaryNode newNode = new DictionaryNode();
            newNode.key = key;
            newNode.value = value;
            if (last != null)
            {
                last.next = newNode;
            }
            else
            {
                head = newNode;
            }

            count++;
        }

        public void Clear()
        {
            count = 0;
            head = null;
            version++;
        }

        public bool Contains(Object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            }

                        for (DictionaryNode node = head; node != null; node = node.next)
            {
                if (node.key.Equals(key))
                {
                    return true;
                }
            }

            return false;
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
                        for (DictionaryNode node = head; node != null; node = node.next)
            {
                array.SetValue(new DictionaryEntry(node.key, node.value), index);
                index++;
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        public void Remove(Object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key", Environment.GetResourceString("ArgumentNull_Key"));
            }

                        version++;
            DictionaryNode last = null;
            DictionaryNode node;
            for (node = head; node != null; node = node.next)
            {
                if (node.key.Equals(key))
                {
                    break;
                }

                last = node;
            }

            if (node == null)
            {
                return;
            }

            if (node == head)
            {
                head = node.next;
            }
            else
            {
                last.next = node.next;
            }

            count--;
        }

        private class NodeEnumerator : IDictionaryEnumerator
        {
            ListDictionaryInternal list;
            DictionaryNode current;
            int version;
            bool start;
            public NodeEnumerator(ListDictionaryInternal list)
            {
                this.list = list;
                version = list.version;
                start = true;
                current = null;
            }

            public Object Current
            {
                get
                {
                    return Entry;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    }

                    return new DictionaryEntry(current.key, current.value);
                }
            }

            public Object Key
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    }

                    return current.key;
                }
            }

            public Object Value
            {
                get
                {
                    if (current == null)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                    }

                    return current.value;
                }
            }

            public bool MoveNext()
            {
                if (version != list.version)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                }

                if (start)
                {
                    current = list.head;
                    start = false;
                }
                else
                {
                    if (current != null)
                    {
                        current = current.next;
                    }
                }

                return (current != null);
            }

            public void Reset()
            {
                if (version != list.version)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                }

                start = true;
                current = null;
            }
        }

        private class NodeKeyValueCollection : ICollection
        {
            ListDictionaryInternal list;
            bool isKeys;
            public NodeKeyValueCollection(ListDictionaryInternal list, bool isKeys)
            {
                this.list = list;
                this.isKeys = isKeys;
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException("array");
                if (array.Rank != 1)
                    throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
                if (index < 0)
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                                if (array.Length - index < list.Count)
                    throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_Index"), "index");
                for (DictionaryNode node = list.head; node != null; node = node.next)
                {
                    array.SetValue(isKeys ? node.key : node.value, index);
                    index++;
                }
            }

            int ICollection.Count
            {
                get
                {
                    int count = 0;
                    for (DictionaryNode node = list.head; node != null; node = node.next)
                    {
                        count++;
                    }

                    return count;
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            Object ICollection.SyncRoot
            {
                get
                {
                    return list.SyncRoot;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new NodeKeyValueEnumerator(list, isKeys);
            }

            private class NodeKeyValueEnumerator : IEnumerator
            {
                ListDictionaryInternal list;
                DictionaryNode current;
                int version;
                bool isKeys;
                bool start;
                public NodeKeyValueEnumerator(ListDictionaryInternal list, bool isKeys)
                {
                    this.list = list;
                    this.isKeys = isKeys;
                    this.version = list.version;
                    this.start = true;
                    this.current = null;
                }

                public Object Current
                {
                    get
                    {
                        if (current == null)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                        }

                        return isKeys ? current.key : current.value;
                    }
                }

                public bool MoveNext()
                {
                    if (version != list.version)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                    }

                    if (start)
                    {
                        current = list.head;
                        start = false;
                    }
                    else
                    {
                        if (current != null)
                        {
                            current = current.next;
                        }
                    }

                    return (current != null);
                }

                public void Reset()
                {
                    if (version != list.version)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumFailedVersion"));
                    }

                    start = true;
                    current = null;
                }
            }
        }

        private class DictionaryNode
        {
            public Object key;
            public Object value;
            public DictionaryNode next;
        }
    }
}