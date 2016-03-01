using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{
    public abstract class OrderablePartitioner<TSource> : Partitioner<TSource>
    {
        protected OrderablePartitioner(bool keysOrderedInEachPartition, bool keysOrderedAcrossPartitions, bool keysNormalized)
        {
            KeysOrderedInEachPartition = keysOrderedInEachPartition;
            KeysOrderedAcrossPartitions = keysOrderedAcrossPartitions;
            KeysNormalized = keysNormalized;
        }

        public abstract IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount);
        public virtual IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
        {
            throw new NotSupportedException(Environment.GetResourceString("Partitioner_DynamicPartitionsNotSupported"));
        }

        public bool KeysOrderedInEachPartition
        {
            get;
            private set;
        }

        public bool KeysOrderedAcrossPartitions
        {
            get;
            private set;
        }

        public bool KeysNormalized
        {
            get;
            private set;
        }

        public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
        {
            IList<IEnumerator<KeyValuePair<long, TSource>>> orderablePartitions = GetOrderablePartitions(partitionCount);
            if (orderablePartitions.Count != partitionCount)
            {
                throw new InvalidOperationException("OrderablePartitioner_GetPartitions_WrongNumberOfPartitions");
            }

            IEnumerator<TSource>[] partitions = new IEnumerator<TSource>[partitionCount];
            for (int i = 0; i < partitionCount; i++)
            {
                partitions[i] = new EnumeratorDropIndices(orderablePartitions[i]);
            }

            return partitions;
        }

        public override IEnumerable<TSource> GetDynamicPartitions()
        {
            IEnumerable<KeyValuePair<long, TSource>> orderablePartitions = GetOrderableDynamicPartitions();
            return new EnumerableDropIndices(orderablePartitions);
        }

        private class EnumerableDropIndices : IEnumerable<TSource>, IDisposable
        {
            private readonly IEnumerable<KeyValuePair<long, TSource>> m_source;
            public EnumerableDropIndices(IEnumerable<KeyValuePair<long, TSource>> source)
            {
                m_source = source;
            }

            public IEnumerator<TSource> GetEnumerator()
            {
                return new EnumeratorDropIndices(m_source.GetEnumerator());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((EnumerableDropIndices)this).GetEnumerator();
            }

            public void Dispose()
            {
                IDisposable d = m_source as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
            }
        }

        private class EnumeratorDropIndices : IEnumerator<TSource>
        {
            private readonly IEnumerator<KeyValuePair<long, TSource>> m_source;
            public EnumeratorDropIndices(IEnumerator<KeyValuePair<long, TSource>> source)
            {
                m_source = source;
            }

            public bool MoveNext()
            {
                return m_source.MoveNext();
            }

            public TSource Current
            {
                get
                {
                    return m_source.Current.Value;
                }
            }

            Object IEnumerator.Current
            {
                get
                {
                    return ((EnumeratorDropIndices)this).Current;
                }
            }

            public void Dispose()
            {
                m_source.Dispose();
            }

            public void Reset()
            {
                m_source.Reset();
            }
        }
    }
}