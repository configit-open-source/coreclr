using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{
    public abstract class Partitioner<TSource>
    {
        public abstract IList<IEnumerator<TSource>> GetPartitions(int partitionCount);
        public virtual bool SupportsDynamicPartitions
        {
            get
            {
                return false;
            }
        }

        public virtual IEnumerable<TSource> GetDynamicPartitions()
        {
            throw new NotSupportedException(Environment.GetResourceString("Partitioner_DynamicPartitionsNotSupported"));
        }
    }
}