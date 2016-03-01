using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Concurrent
{
    public interface IProducerConsumerCollection<T> : IEnumerable<T>, ICollection
    {
        void CopyTo(T[] array, int index);
        bool TryAdd(T item);
        bool TryTake(out T item);
        T[] ToArray();
    }

    internal sealed class SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<T>
    {
        private IProducerConsumerCollection<T> m_collection;
        public SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView(IProducerConsumerCollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            m_collection = collection;
        }

        public T[] Items
        {
            get
            {
                return m_collection.ToArray();
            }
        }
    }
}