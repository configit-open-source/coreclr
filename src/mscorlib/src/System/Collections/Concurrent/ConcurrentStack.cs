using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{
    public class ConcurrentStack<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        private class Node
        {
            internal readonly T m_value;
            internal Node m_next;
            internal Node(T value)
            {
                m_value = value;
                m_next = null;
            }
        }

        private volatile Node m_head;
        private const int BACKOFF_MAX_YIELDS = 8;
        public ConcurrentStack()
        {
        }

        public ConcurrentStack(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            InitializeFromCollection(collection);
        }

        private void InitializeFromCollection(IEnumerable<T> collection)
        {
            Node lastNode = null;
            foreach (T element in collection)
            {
                Node newNode = new Node(element);
                newNode.m_next = lastNode;
                lastNode = newNode;
            }

            m_head = lastNode;
        }

        public bool IsEmpty
        {
            get
            {
                return m_head == null;
            }
        }

        public int Count
        {
            get
            {
                int count = 0;
                for (Node curr = m_head; curr != null; curr = curr.m_next)
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

        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("ConcurrentCollection_SyncRoot_NotSupported"));
            }
        }

        public void Clear()
        {
            m_head = null;
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            ((ICollection)ToList()).CopyTo(array, index);
        }

        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            ToList().CopyTo(array, index);
        }

        public void Push(T item)
        {
            Node newNode = new Node(item);
            newNode.m_next = m_head;
            if (Interlocked.CompareExchange(ref m_head, newNode, newNode.m_next) == newNode.m_next)
            {
                return;
            }

            PushCore(newNode, newNode);
        }

        public void PushRange(T[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            PushRange(items, 0, items.Length);
        }

        public void PushRange(T[] items, int startIndex, int count)
        {
            ValidatePushPopRangeInput(items, startIndex, count);
            if (count == 0)
                return;
            Node head, tail;
            head = tail = new Node(items[startIndex]);
            for (int i = startIndex + 1; i < startIndex + count; i++)
            {
                Node node = new Node(items[i]);
                node.m_next = head;
                head = node;
            }

            tail.m_next = m_head;
            if (Interlocked.CompareExchange(ref m_head, head, tail.m_next) == tail.m_next)
            {
                return;
            }

            PushCore(head, tail);
        }

        private void PushCore(Node head, Node tail)
        {
            SpinWait spin = new SpinWait();
            do
            {
                spin.SpinOnce();
                tail.m_next = m_head;
            }
            while (Interlocked.CompareExchange(ref m_head, head, tail.m_next) != tail.m_next);
        }

        private void ValidatePushPopRangeInput(T[] items, int startIndex, int count)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ConcurrentStack_PushPopRange_CountOutOfRange"));
            }

            int length = items.Length;
            if (startIndex >= length || startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ConcurrentStack_PushPopRange_StartOutOfRange"));
            }

            if (length - count < startIndex)
            {
                throw new ArgumentException(Environment.GetResourceString("ConcurrentStack_PushPopRange_InvalidCount"));
            }
        }

        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            Push(item);
            return true;
        }

        public bool TryPeek(out T result)
        {
            Node head = m_head;
            if (head == null)
            {
                result = default (T);
                return false;
            }
            else
            {
                result = head.m_value;
                return true;
            }
        }

        public bool TryPop(out T result)
        {
            Node head = m_head;
            if (head == null)
            {
                result = default (T);
                return false;
            }

            if (Interlocked.CompareExchange(ref m_head, head.m_next, head) == head)
            {
                result = head.m_value;
                return true;
            }

            return TryPopCore(out result);
        }

        public int TryPopRange(T[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            return TryPopRange(items, 0, items.Length);
        }

        public int TryPopRange(T[] items, int startIndex, int count)
        {
            ValidatePushPopRangeInput(items, startIndex, count);
            if (count == 0)
                return 0;
            Node poppedHead;
            int nodesCount = TryPopCore(count, out poppedHead);
            if (nodesCount > 0)
            {
                CopyRemovedItems(poppedHead, items, startIndex, nodesCount);
            }

            return nodesCount;
        }

        private bool TryPopCore(out T result)
        {
            Node poppedNode;
            if (TryPopCore(1, out poppedNode) == 1)
            {
                result = poppedNode.m_value;
                return true;
            }

            result = default (T);
            return false;
        }

        private int TryPopCore(int count, out Node poppedHead)
        {
            SpinWait spin = new SpinWait();
            Node head;
            Node next;
            int backoff = 1;
            Random r = new Random(Environment.TickCount & Int32.MaxValue);
            while (true)
            {
                head = m_head;
                if (head == null)
                {
                    poppedHead = null;
                    return 0;
                }

                next = head;
                int nodesCount = 1;
                for (; nodesCount < count && next.m_next != null; nodesCount++)
                {
                    next = next.m_next;
                }

                if (Interlocked.CompareExchange(ref m_head, next.m_next, head) == head)
                {
                    poppedHead = head;
                    return nodesCount;
                }

                for (int i = 0; i < backoff; i++)
                {
                    spin.SpinOnce();
                }

                backoff = spin.NextSpinWillYield ? r.Next(1, BACKOFF_MAX_YIELDS) : backoff * 2;
            }
        }

        private void CopyRemovedItems(Node head, T[] collection, int startIndex, int nodesCount)
        {
            Node current = head;
            for (int i = startIndex; i < startIndex + nodesCount; i++)
            {
                collection[i] = current.m_value;
                current = current.m_next;
            }
        }

        bool IProducerConsumerCollection<T>.TryTake(out T item)
        {
            return TryPop(out item);
        }

        public T[] ToArray()
        {
            return ToList().ToArray();
        }

        private List<T> ToList()
        {
            List<T> list = new List<T>();
            Node curr = m_head;
            while (curr != null)
            {
                list.Add(curr.m_value);
                curr = curr.m_next;
            }

            return list;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetEnumerator(m_head);
        }

        private IEnumerator<T> GetEnumerator(Node head)
        {
            Node current = head;
            while (current != null)
            {
                yield return current.m_value;
                current = current.m_next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}