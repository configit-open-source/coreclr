using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Collections.Concurrent
{
    public class ConcurrentQueue<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        private volatile Segment m_head;
        private volatile Segment m_tail;
        private T[] m_serializationArray;
        private const int SEGMENT_SIZE = 32;
        internal volatile int m_numSnapshotTakers = 0;
        public ConcurrentQueue()
        {
            m_head = m_tail = new Segment(0, this);
        }

        private void InitializeFromCollection(IEnumerable<T> collection)
        {
            Segment localTail = new Segment(0, this);
            m_head = localTail;
            int index = 0;
            foreach (T element in collection)
            {
                Contract.Assert(index >= 0 && index < SEGMENT_SIZE);
                localTail.UnsafeAdd(element);
                index++;
                if (index >= SEGMENT_SIZE)
                {
                    localTail = localTail.UnsafeGrow();
                    index = 0;
                }
            }

            m_tail = localTail;
        }

        public ConcurrentQueue(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            InitializeFromCollection(collection);
        }

        private void OnSerializing(StreamingContext context)
        {
            m_serializationArray = ToArray();
        }

        private void OnDeserialized(StreamingContext context)
        {
            Contract.Assert(m_serializationArray != null);
            InitializeFromCollection(m_serializationArray);
            m_serializationArray = null;
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            ((ICollection)ToList()).CopyTo(array, index);
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            Enqueue(item);
            return true;
        }

        bool IProducerConsumerCollection<T>.TryTake(out T item)
        {
            return TryDequeue(out item);
        }

        public bool IsEmpty
        {
            get
            {
                Segment head = m_head;
                if (!head.IsEmpty)
                    return false;
                else if (head.Next == null)
                    return true;
                else
                {
                    SpinWait spin = new SpinWait();
                    while (head.IsEmpty)
                    {
                        if (head.Next == null)
                            return true;
                        spin.SpinOnce();
                        head = m_head;
                    }

                    return false;
                }
            }
        }

        public T[] ToArray()
        {
            return ToList().ToArray();
        }

        private List<T> ToList()
        {
            Interlocked.Increment(ref m_numSnapshotTakers);
            List<T> list = new List<T>();
            try
            {
                Segment head, tail;
                int headLow, tailHigh;
                GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);
                if (head == tail)
                {
                    head.AddToList(list, headLow, tailHigh);
                }
                else
                {
                    head.AddToList(list, headLow, SEGMENT_SIZE - 1);
                    Segment curr = head.Next;
                    while (curr != tail)
                    {
                        curr.AddToList(list, 0, SEGMENT_SIZE - 1);
                        curr = curr.Next;
                    }

                    tail.AddToList(list, 0, tailHigh);
                }
            }
            finally
            {
                Interlocked.Decrement(ref m_numSnapshotTakers);
            }

            return list;
        }

        private void GetHeadTailPositions(out Segment head, out Segment tail, out int headLow, out int tailHigh)
        {
            head = m_head;
            tail = m_tail;
            headLow = head.Low;
            tailHigh = tail.High;
            SpinWait spin = new SpinWait();
            while (head != m_head || tail != m_tail || headLow != head.Low || tailHigh != tail.High || head.m_index > tail.m_index)
            {
                spin.SpinOnce();
                head = m_head;
                tail = m_tail;
                headLow = head.Low;
                tailHigh = tail.High;
            }
        }

        public int Count
        {
            get
            {
                Segment head, tail;
                int headLow, tailHigh;
                GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);
                if (head == tail)
                {
                    return tailHigh - headLow + 1;
                }

                int count = SEGMENT_SIZE - headLow;
                count += SEGMENT_SIZE * ((int)(tail.m_index - head.m_index - 1));
                count += tailHigh + 1;
                return count;
            }
        }

        public void CopyTo(T[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            ToList().CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            Interlocked.Increment(ref m_numSnapshotTakers);
            Segment head, tail;
            int headLow, tailHigh;
            GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);
            return GetEnumerator(head, tail, headLow, tailHigh);
        }

        private IEnumerator<T> GetEnumerator(Segment head, Segment tail, int headLow, int tailHigh)
        {
            try
            {
                SpinWait spin = new SpinWait();
                if (head == tail)
                {
                    for (int i = headLow; i <= tailHigh; i++)
                    {
                        spin.Reset();
                        while (!head.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }

                        yield return head.m_array[i];
                    }
                }
                else
                {
                    for (int i = headLow; i < SEGMENT_SIZE; i++)
                    {
                        spin.Reset();
                        while (!head.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }

                        yield return head.m_array[i];
                    }

                    Segment curr = head.Next;
                    while (curr != tail)
                    {
                        for (int i = 0; i < SEGMENT_SIZE; i++)
                        {
                            spin.Reset();
                            while (!curr.m_state[i].m_value)
                            {
                                spin.SpinOnce();
                            }

                            yield return curr.m_array[i];
                        }

                        curr = curr.Next;
                    }

                    for (int i = 0; i <= tailHigh; i++)
                    {
                        spin.Reset();
                        while (!tail.m_state[i].m_value)
                        {
                            spin.SpinOnce();
                        }

                        yield return tail.m_array[i];
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref m_numSnapshotTakers);
            }
        }

        public void Enqueue(T item)
        {
            SpinWait spin = new SpinWait();
            while (true)
            {
                Segment tail = m_tail;
                if (tail.TryAppend(item))
                    return;
                spin.SpinOnce();
            }
        }

        public bool TryDequeue(out T result)
        {
            while (!IsEmpty)
            {
                Segment head = m_head;
                if (head.TryRemove(out result))
                    return true;
            }

            result = default (T);
            return false;
        }

        public bool TryPeek(out T result)
        {
            Interlocked.Increment(ref m_numSnapshotTakers);
            while (!IsEmpty)
            {
                Segment head = m_head;
                if (head.TryPeek(out result))
                {
                    Interlocked.Decrement(ref m_numSnapshotTakers);
                    return true;
                }
            }

            result = default (T);
            Interlocked.Decrement(ref m_numSnapshotTakers);
            return false;
        }

        private class Segment
        {
            internal volatile T[] m_array;
            internal volatile VolatileBool[] m_state;
            private volatile Segment m_next;
            internal readonly long m_index;
            private volatile int m_low;
            private volatile int m_high;
            private volatile ConcurrentQueue<T> m_source;
            internal Segment(long index, ConcurrentQueue<T> source)
            {
                m_array = new T[SEGMENT_SIZE];
                m_state = new VolatileBool[SEGMENT_SIZE];
                m_high = -1;
                Contract.Assert(index >= 0);
                m_index = index;
                m_source = source;
            }

            internal Segment Next
            {
                get
                {
                    return m_next;
                }
            }

            internal bool IsEmpty
            {
                get
                {
                    return (Low > High);
                }
            }

            internal void UnsafeAdd(T value)
            {
                Contract.Assert(m_high < SEGMENT_SIZE - 1);
                m_high++;
                m_array[m_high] = value;
                m_state[m_high].m_value = true;
            }

            internal Segment UnsafeGrow()
            {
                Contract.Assert(m_high >= SEGMENT_SIZE - 1);
                Segment newSegment = new Segment(m_index + 1, m_source);
                m_next = newSegment;
                return newSegment;
            }

            internal void Grow()
            {
                Segment newSegment = new Segment(m_index + 1, m_source);
                m_next = newSegment;
                Contract.Assert(m_source.m_tail == this);
                m_source.m_tail = m_next;
            }

            internal bool TryAppend(T value)
            {
                if (m_high >= SEGMENT_SIZE - 1)
                {
                    return false;
                }

                int newhigh = SEGMENT_SIZE;
                try
                {
                }
                finally
                {
                    newhigh = Interlocked.Increment(ref m_high);
                    if (newhigh <= SEGMENT_SIZE - 1)
                    {
                        m_array[newhigh] = value;
                        m_state[newhigh].m_value = true;
                    }

                    if (newhigh == SEGMENT_SIZE - 1)
                    {
                        Grow();
                    }
                }

                return newhigh <= SEGMENT_SIZE - 1;
            }

            internal bool TryRemove(out T result)
            {
                SpinWait spin = new SpinWait();
                int lowLocal = Low, highLocal = High;
                while (lowLocal <= highLocal)
                {
                    if (Interlocked.CompareExchange(ref m_low, lowLocal + 1, lowLocal) == lowLocal)
                    {
                        SpinWait spinLocal = new SpinWait();
                        while (!m_state[lowLocal].m_value)
                        {
                            spinLocal.SpinOnce();
                        }

                        result = m_array[lowLocal];
                        if (m_source.m_numSnapshotTakers <= 0)
                        {
                            m_array[lowLocal] = default (T);
                        }

                        if (lowLocal + 1 >= SEGMENT_SIZE)
                        {
                            spinLocal = new SpinWait();
                            while (m_next == null)
                            {
                                spinLocal.SpinOnce();
                            }

                            Contract.Assert(m_source.m_head == this);
                            m_source.m_head = m_next;
                        }

                        return true;
                    }
                    else
                    {
                        spin.SpinOnce();
                        lowLocal = Low;
                        highLocal = High;
                    }
                }

                result = default (T);
                return false;
            }

            internal bool TryPeek(out T result)
            {
                result = default (T);
                int lowLocal = Low;
                if (lowLocal > High)
                    return false;
                SpinWait spin = new SpinWait();
                while (!m_state[lowLocal].m_value)
                {
                    spin.SpinOnce();
                }

                result = m_array[lowLocal];
                return true;
            }

            internal void AddToList(List<T> list, int start, int end)
            {
                for (int i = start; i <= end; i++)
                {
                    SpinWait spin = new SpinWait();
                    while (!m_state[i].m_value)
                    {
                        spin.SpinOnce();
                    }

                    list.Add(m_array[i]);
                }
            }

            internal int Low
            {
                get
                {
                    return Math.Min(m_low, SEGMENT_SIZE);
                }
            }

            internal int High
            {
                get
                {
                    return Math.Min(m_high, SEGMENT_SIZE - 1);
                }
            }
        }
    }

    struct VolatileBool
    {
        public VolatileBool(bool value)
        {
            m_value = value;
        }

        public volatile bool m_value;
    }
}