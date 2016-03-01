using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks
{
    internal interface IProducerConsumerQueue<T> : IEnumerable<T>
    {
        void Enqueue(T item);
        bool TryDequeue(out T result);
        bool IsEmpty
        {
            get;
        }

        int Count
        {
            get;
        }

        int GetCountSafe(object syncObj);
    }

    internal sealed class MultiProducerMultiConsumerQueue<T> : ConcurrentQueue<T>, IProducerConsumerQueue<T>
    {
        void IProducerConsumerQueue<T>.Enqueue(T item)
        {
            base.Enqueue(item);
        }

        bool IProducerConsumerQueue<T>.TryDequeue(out T result)
        {
            return base.TryDequeue(out result);
        }

        bool IProducerConsumerQueue<T>.IsEmpty
        {
            get
            {
                return base.IsEmpty;
            }
        }

        int IProducerConsumerQueue<T>.Count
        {
            get
            {
                return base.Count;
            }
        }

        int IProducerConsumerQueue<T>.GetCountSafe(object syncObj)
        {
            return base.Count;
        }
    }

    internal sealed class SingleProducerSingleConsumerQueue<T> : IProducerConsumerQueue<T>
    {
        private const int INIT_SEGMENT_SIZE = 32;
        private const int MAX_SEGMENT_SIZE = 0x1000000;
        private volatile Segment m_head;
        private volatile Segment m_tail;
        internal SingleProducerSingleConsumerQueue()
        {
            Contract.Assert(INIT_SEGMENT_SIZE > 0, "Initial segment size must be > 0.");
            Contract.Assert((INIT_SEGMENT_SIZE & (INIT_SEGMENT_SIZE - 1)) == 0, "Initial segment size must be a power of 2");
            Contract.Assert(INIT_SEGMENT_SIZE <= MAX_SEGMENT_SIZE, "Initial segment size should be <= maximum.");
            Contract.Assert(MAX_SEGMENT_SIZE < Int32.MaxValue / 2, "Max segment size * 2 must be < Int32.MaxValue, or else overflow could occur.");
            m_head = m_tail = new Segment(INIT_SEGMENT_SIZE);
        }

        public void Enqueue(T item)
        {
            Segment segment = m_tail;
            var array = segment.m_array;
            int last = segment.m_state.m_last;
            int tail2 = (last + 1) & (array.Length - 1);
            if (tail2 != segment.m_state.m_firstCopy)
            {
                array[last] = item;
                segment.m_state.m_last = tail2;
            }
            else
                EnqueueSlow(item, ref segment);
        }

        private void EnqueueSlow(T item, ref Segment segment)
        {
            Contract.Requires(segment != null, "Expected a non-null segment.");
            if (segment.m_state.m_firstCopy != segment.m_state.m_first)
            {
                segment.m_state.m_firstCopy = segment.m_state.m_first;
                Enqueue(item);
                return;
            }

            int newSegmentSize = m_tail.m_array.Length << 1;
            Contract.Assert(newSegmentSize > 0, "The max size should always be small enough that we don't overflow.");
            if (newSegmentSize > MAX_SEGMENT_SIZE)
                newSegmentSize = MAX_SEGMENT_SIZE;
            var newSegment = new Segment(newSegmentSize);
            newSegment.m_array[0] = item;
            newSegment.m_state.m_last = 1;
            newSegment.m_state.m_lastCopy = 1;
            try
            {
            }
            finally
            {
                Volatile.Write(ref m_tail.m_next, newSegment);
                m_tail = newSegment;
            }
        }

        public bool TryDequeue(out T result)
        {
            Segment segment = m_head;
            var array = segment.m_array;
            int first = segment.m_state.m_first;
            if (first != segment.m_state.m_lastCopy)
            {
                result = array[first];
                array[first] = default (T);
                segment.m_state.m_first = (first + 1) & (array.Length - 1);
                return true;
            }
            else
                return TryDequeueSlow(ref segment, ref array, out result);
        }

        private bool TryDequeueSlow(ref Segment segment, ref T[] array, out T result)
        {
            Contract.Requires(segment != null, "Expected a non-null segment.");
            Contract.Requires(array != null, "Expected a non-null item array.");
            if (segment.m_state.m_last != segment.m_state.m_lastCopy)
            {
                segment.m_state.m_lastCopy = segment.m_state.m_last;
                return TryDequeue(out result);
            }

            if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
            {
                segment = segment.m_next;
                array = segment.m_array;
                m_head = segment;
            }

            var first = segment.m_state.m_first;
            if (first == segment.m_state.m_last)
            {
                result = default (T);
                return false;
            }

            result = array[first];
            array[first] = default (T);
            segment.m_state.m_first = (first + 1) & (segment.m_array.Length - 1);
            segment.m_state.m_lastCopy = segment.m_state.m_last;
            return true;
        }

        public bool TryPeek(out T result)
        {
            Segment segment = m_head;
            var array = segment.m_array;
            int first = segment.m_state.m_first;
            if (first != segment.m_state.m_lastCopy)
            {
                result = array[first];
                return true;
            }
            else
                return TryPeekSlow(ref segment, ref array, out result);
        }

        private bool TryPeekSlow(ref Segment segment, ref T[] array, out T result)
        {
            Contract.Requires(segment != null, "Expected a non-null segment.");
            Contract.Requires(array != null, "Expected a non-null item array.");
            if (segment.m_state.m_last != segment.m_state.m_lastCopy)
            {
                segment.m_state.m_lastCopy = segment.m_state.m_last;
                return TryPeek(out result);
            }

            if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
            {
                segment = segment.m_next;
                array = segment.m_array;
                m_head = segment;
            }

            var first = segment.m_state.m_first;
            if (first == segment.m_state.m_last)
            {
                result = default (T);
                return false;
            }

            result = array[first];
            return true;
        }

        public bool TryDequeueIf(Predicate<T> predicate, out T result)
        {
            Segment segment = m_head;
            var array = segment.m_array;
            int first = segment.m_state.m_first;
            if (first != segment.m_state.m_lastCopy)
            {
                result = array[first];
                if (predicate == null || predicate(result))
                {
                    array[first] = default (T);
                    segment.m_state.m_first = (first + 1) & (array.Length - 1);
                    return true;
                }
                else
                {
                    result = default (T);
                    return false;
                }
            }
            else
                return TryDequeueIfSlow(predicate, ref segment, ref array, out result);
        }

        private bool TryDequeueIfSlow(Predicate<T> predicate, ref Segment segment, ref T[] array, out T result)
        {
            Contract.Requires(segment != null, "Expected a non-null segment.");
            Contract.Requires(array != null, "Expected a non-null item array.");
            if (segment.m_state.m_last != segment.m_state.m_lastCopy)
            {
                segment.m_state.m_lastCopy = segment.m_state.m_last;
                return TryDequeueIf(predicate, out result);
            }

            if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
            {
                segment = segment.m_next;
                array = segment.m_array;
                m_head = segment;
            }

            var first = segment.m_state.m_first;
            if (first == segment.m_state.m_last)
            {
                result = default (T);
                return false;
            }

            result = array[first];
            if (predicate == null || predicate(result))
            {
                array[first] = default (T);
                segment.m_state.m_first = (first + 1) & (segment.m_array.Length - 1);
                segment.m_state.m_lastCopy = segment.m_state.m_last;
                return true;
            }
            else
            {
                result = default (T);
                return false;
            }
        }

        public void Clear()
        {
            T ignored;
            while (TryDequeue(out ignored))
                ;
        }

        public bool IsEmpty
        {
            get
            {
                var head = m_head;
                if (head.m_state.m_first != head.m_state.m_lastCopy)
                    return false;
                if (head.m_state.m_first != head.m_state.m_last)
                    return false;
                return head.m_next == null;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (Segment segment = m_head; segment != null; segment = segment.m_next)
            {
                for (int pt = segment.m_state.m_first; pt != segment.m_state.m_last; pt = (pt + 1) & (segment.m_array.Length - 1))
                {
                    yield return segment.m_array[pt];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get
            {
                int count = 0;
                for (Segment segment = m_head; segment != null; segment = segment.m_next)
                {
                    int arraySize = segment.m_array.Length;
                    int first, last;
                    while (true)
                    {
                        first = segment.m_state.m_first;
                        last = segment.m_state.m_last;
                        if (first == segment.m_state.m_first)
                            break;
                    }

                    count += (last - first) & (arraySize - 1);
                }

                return count;
            }
        }

        int IProducerConsumerQueue<T>.GetCountSafe(object syncObj)
        {
            Contract.Assert(syncObj != null, "The syncObj parameter is null.");
            lock (syncObj)
            {
                return Count;
            }
        }

        private sealed class Segment
        {
            internal Segment m_next;
            internal readonly T[] m_array;
            internal SegmentState m_state;
            internal Segment(int size)
            {
                Contract.Requires((size & (size - 1)) == 0, "Size must be a power of 2");
                m_array = new T[size];
            }
        }

        private struct SegmentState
        {
            internal PaddingFor32 m_pad0;
            internal volatile int m_first;
            internal int m_lastCopy;
            internal PaddingFor32 m_pad1;
            internal int m_firstCopy;
            internal volatile int m_last;
            internal PaddingFor32 m_pad2;
        }

        private sealed class SingleProducerSingleConsumerQueue_DebugView
        {
            private readonly SingleProducerSingleConsumerQueue<T> m_queue;
            public SingleProducerSingleConsumerQueue_DebugView(SingleProducerSingleConsumerQueue<T> queue)
            {
                Contract.Requires(queue != null, "Expected a non-null queue.");
                m_queue = queue;
            }

            public T[] Items
            {
                get
                {
                    List<T> list = new List<T>();
                    foreach (T item in m_queue)
                        list.Add(item);
                    return list.ToArray();
                }
            }
        }
    }

    static class PaddingHelpers
    {
        internal const int CACHE_LINE_SIZE = 128;
    }

    struct PaddingFor32
    {
    }
}