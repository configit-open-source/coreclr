using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace System.Collections.Concurrent
{
    [Flags]
    public enum EnumerablePartitionerOptions
    {
        None = 0x0,
        NoBuffering = 0x1
    }

    public static class Partitioner
    {
        public static OrderablePartitioner<TSource> Create<TSource>(IList<TSource> list, bool loadBalance)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            if (loadBalance)
            {
                return (new DynamicPartitionerForIList<TSource>(list));
            }
            else
            {
                return (new StaticIndexRangePartitionerForIList<TSource>(list));
            }
        }

        public static OrderablePartitioner<TSource> Create<TSource>(TSource[] array, bool loadBalance)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (loadBalance)
            {
                return (new DynamicPartitionerForArray<TSource>(array));
            }
            else
            {
                return (new StaticIndexRangePartitionerForArray<TSource>(array));
            }
        }

        public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source)
        {
            return Create<TSource>(source, EnumerablePartitionerOptions.None);
        }

        public static OrderablePartitioner<TSource> Create<TSource>(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if ((partitionerOptions & (~EnumerablePartitionerOptions.NoBuffering)) != 0)
                throw new ArgumentOutOfRangeException("partitionerOptions");
            return (new DynamicPartitionerForIEnumerable<TSource>(source, partitionerOptions));
        }

        public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive)
        {
            int coreOversubscriptionRate = 3;
            if (toExclusive <= fromInclusive)
                throw new ArgumentOutOfRangeException("toExclusive");
            long rangeSize = (toExclusive - fromInclusive) / (PlatformHelper.ProcessorCount * coreOversubscriptionRate);
            if (rangeSize == 0)
                rangeSize = 1;
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering);
        }

        public static OrderablePartitioner<Tuple<long, long>> Create(long fromInclusive, long toExclusive, long rangeSize)
        {
            if (toExclusive <= fromInclusive)
                throw new ArgumentOutOfRangeException("toExclusive");
            if (rangeSize <= 0)
                throw new ArgumentOutOfRangeException("rangeSize");
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering);
        }

        private static IEnumerable<Tuple<long, long>> CreateRanges(long fromInclusive, long toExclusive, long rangeSize)
        {
            long from, to;
            bool shouldQuit = false;
            for (long i = fromInclusive; (i < toExclusive) && !shouldQuit; i += rangeSize)
            {
                from = i;
                try
                {
                    checked
                    {
                        to = i + rangeSize;
                    }
                }
                catch (OverflowException)
                {
                    to = toExclusive;
                    shouldQuit = true;
                }

                if (to > toExclusive)
                    to = toExclusive;
                yield return new Tuple<long, long>(from, to);
            }
        }

        public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive)
        {
            int coreOversubscriptionRate = 3;
            if (toExclusive <= fromInclusive)
                throw new ArgumentOutOfRangeException("toExclusive");
            int rangeSize = (toExclusive - fromInclusive) / (PlatformHelper.ProcessorCount * coreOversubscriptionRate);
            if (rangeSize == 0)
                rangeSize = 1;
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering);
        }

        public static OrderablePartitioner<Tuple<int, int>> Create(int fromInclusive, int toExclusive, int rangeSize)
        {
            if (toExclusive <= fromInclusive)
                throw new ArgumentOutOfRangeException("toExclusive");
            if (rangeSize <= 0)
                throw new ArgumentOutOfRangeException("rangeSize");
            return Partitioner.Create(CreateRanges(fromInclusive, toExclusive, rangeSize), EnumerablePartitionerOptions.NoBuffering);
        }

        private static IEnumerable<Tuple<int, int>> CreateRanges(int fromInclusive, int toExclusive, int rangeSize)
        {
            int from, to;
            bool shouldQuit = false;
            for (int i = fromInclusive; (i < toExclusive) && !shouldQuit; i += rangeSize)
            {
                from = i;
                try
                {
                    checked
                    {
                        to = i + rangeSize;
                    }
                }
                catch (OverflowException)
                {
                    to = toExclusive;
                    shouldQuit = true;
                }

                if (to > toExclusive)
                    to = toExclusive;
                yield return new Tuple<int, int>(from, to);
            }
        }

        private abstract class DynamicPartitionEnumerator_Abstract<TSource, TSourceReader> : IEnumerator<KeyValuePair<long, TSource>>
        {
            protected readonly TSourceReader m_sharedReader;
            protected static int s_defaultMaxChunkSize = GetDefaultChunkSize<TSource>();
            protected SharedInt m_currentChunkSize;
            protected SharedInt m_localOffset;
            private const int CHUNK_DOUBLING_RATE = 3;
            private int m_doublingCountdown;
            protected readonly int m_maxChunkSize;
            protected readonly SharedLong m_sharedIndex;
            protected DynamicPartitionEnumerator_Abstract(TSourceReader sharedReader, SharedLong sharedIndex): this (sharedReader, sharedIndex, false)
            {
            }

            protected DynamicPartitionEnumerator_Abstract(TSourceReader sharedReader, SharedLong sharedIndex, bool useSingleChunking)
            {
                m_sharedReader = sharedReader;
                m_sharedIndex = sharedIndex;
                m_maxChunkSize = useSingleChunking ? 1 : s_defaultMaxChunkSize;
            }

            protected abstract bool GrabNextChunk(int requestedChunkSize);
            protected abstract bool HasNoElementsLeft
            {
                get;
                set;
            }

            public abstract KeyValuePair<long, TSource> Current
            {
                get;
            }

            public abstract void Dispose();
            public void Reset()
            {
                throw new NotSupportedException();
            }

            Object IEnumerator.Current
            {
                get
                {
                    return ((DynamicPartitionEnumerator_Abstract<TSource, TSourceReader>)this).Current;
                }
            }

            public bool MoveNext()
            {
                if (m_localOffset == null)
                {
                    Contract.Assert(m_currentChunkSize == null);
                    m_localOffset = new SharedInt(-1);
                    m_currentChunkSize = new SharedInt(0);
                    m_doublingCountdown = CHUNK_DOUBLING_RATE;
                }

                if (m_localOffset.Value < m_currentChunkSize.Value - 1)
                {
                    m_localOffset.Value++;
                    return true;
                }
                else
                {
                    Contract.Assert(m_localOffset.Value == m_currentChunkSize.Value - 1 || m_currentChunkSize.Value == 0);
                    int requestedChunkSize;
                    if (m_currentChunkSize.Value == 0)
                    {
                        requestedChunkSize = 1;
                    }
                    else if (m_doublingCountdown > 0)
                    {
                        requestedChunkSize = m_currentChunkSize.Value;
                    }
                    else
                    {
                        requestedChunkSize = Math.Min(m_currentChunkSize.Value * 2, m_maxChunkSize);
                        m_doublingCountdown = CHUNK_DOUBLING_RATE;
                    }

                    m_doublingCountdown--;
                    Contract.Assert(requestedChunkSize > 0 && requestedChunkSize <= m_maxChunkSize);
                    if (GrabNextChunk(requestedChunkSize))
                    {
                        Contract.Assert(m_currentChunkSize.Value <= requestedChunkSize && m_currentChunkSize.Value > 0);
                        m_localOffset.Value = 0;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        private class DynamicPartitionerForIEnumerable<TSource> : OrderablePartitioner<TSource>
        {
            IEnumerable<TSource> m_source;
            readonly bool m_useSingleChunking;
            internal DynamicPartitionerForIEnumerable(IEnumerable<TSource> source, EnumerablePartitionerOptions partitionerOptions): base (true, false, true)
            {
                m_source = source;
                m_useSingleChunking = ((partitionerOptions & EnumerablePartitionerOptions.NoBuffering) != 0);
            }

            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }

                IEnumerator<KeyValuePair<long, TSource>>[] partitions = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = new InternalPartitionEnumerable(m_source.GetEnumerator(), m_useSingleChunking, true);
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = partitionEnumerable.GetEnumerator();
                }

                return partitions;
            }

            override public IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
            {
                return new InternalPartitionEnumerable(m_source.GetEnumerator(), m_useSingleChunking, false);
            }

            override public bool SupportsDynamicPartitions
            {
                get
                {
                    return true;
                }
            }

            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>, IDisposable
            {
                private readonly IEnumerator<TSource> m_sharedReader;
                private SharedLong m_sharedIndex;
                private volatile KeyValuePair<long, TSource>[] m_FillBuffer;
                private volatile int m_FillBufferSize;
                private volatile int m_FillBufferCurrentPosition;
                private volatile int m_activeCopiers;
                private SharedBool m_hasNoElementsLeft;
                private SharedBool m_sourceDepleted;
                private object m_sharedLock;
                private bool m_disposed;
                private SharedInt m_activePartitionCount;
                private readonly bool m_useSingleChunking;
                internal InternalPartitionEnumerable(IEnumerator<TSource> sharedReader, bool useSingleChunking, bool isStaticPartitioning)
                {
                    m_sharedReader = sharedReader;
                    m_sharedIndex = new SharedLong(-1);
                    m_hasNoElementsLeft = new SharedBool(false);
                    m_sourceDepleted = new SharedBool(false);
                    m_sharedLock = new object ();
                    m_useSingleChunking = useSingleChunking;
                    if (!m_useSingleChunking)
                    {
                        int fillBufferMultiplier = (PlatformHelper.ProcessorCount > 4) ? 4 : 1;
                        m_FillBuffer = new KeyValuePair<long, TSource>[fillBufferMultiplier * Partitioner.GetDefaultChunkSize<TSource>()];
                    }

                    if (isStaticPartitioning)
                    {
                        m_activePartitionCount = new SharedInt(0);
                    }
                    else
                    {
                        m_activePartitionCount = null;
                    }
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    if (m_disposed)
                    {
                        throw new ObjectDisposedException(Environment.GetResourceString("PartitionerStatic_CanNotCallGetEnumeratorAfterSourceHasBeenDisposed"));
                    }
                    else
                    {
                        return new InternalPartitionEnumerator(m_sharedReader, m_sharedIndex, m_hasNoElementsLeft, m_sharedLock, m_activePartitionCount, this, m_useSingleChunking);
                    }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }

                private void TryCopyFromFillBuffer(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    actualNumElementsGrabbed = 0;
                    KeyValuePair<long, TSource>[] fillBufferLocalRef = m_FillBuffer;
                    if (fillBufferLocalRef == null)
                        return;
                    if (m_FillBufferCurrentPosition >= m_FillBufferSize)
                    {
                        return;
                    }

                    Interlocked.Increment(ref m_activeCopiers);
                    int endPos = Interlocked.Add(ref m_FillBufferCurrentPosition, requestedChunkSize);
                    int beginPos = endPos - requestedChunkSize;
                    if (beginPos < m_FillBufferSize)
                    {
                        actualNumElementsGrabbed = (endPos < m_FillBufferSize) ? endPos : m_FillBufferSize - beginPos;
                        Array.Copy(fillBufferLocalRef, beginPos, destArray, 0, actualNumElementsGrabbed);
                    }

                    Interlocked.Decrement(ref m_activeCopiers);
                }

                internal bool GrabChunk(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    actualNumElementsGrabbed = 0;
                    if (m_hasNoElementsLeft.Value)
                    {
                        return false;
                    }

                    if (m_useSingleChunking)
                    {
                        return GrabChunk_Single(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
                    }
                    else
                    {
                        return GrabChunk_Buffered(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
                    }
                }

                internal bool GrabChunk_Single(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    Contract.Assert(m_useSingleChunking, "Expected m_useSingleChecking to be true");
                    Contract.Assert(requestedChunkSize == 1, "Got requested chunk size of " + requestedChunkSize + " when single-chunking was on");
                    Contract.Assert(actualNumElementsGrabbed == 0, "Expected actualNumElementsGrabbed == 0, instead it is " + actualNumElementsGrabbed);
                    Contract.Assert(destArray.Length == 1, "Expected destArray to be of length 1, instead its length is " + destArray.Length);
                    lock (m_sharedLock)
                    {
                        if (m_hasNoElementsLeft.Value)
                            return false;
                        try
                        {
                            if (m_sharedReader.MoveNext())
                            {
                                m_sharedIndex.Value = checked (m_sharedIndex.Value + 1);
                                destArray[0] = new KeyValuePair<long, TSource>(m_sharedIndex.Value, m_sharedReader.Current);
                                actualNumElementsGrabbed = 1;
                                return true;
                            }
                            else
                            {
                                m_sourceDepleted.Value = true;
                                m_hasNoElementsLeft.Value = true;
                                return false;
                            }
                        }
                        catch
                        {
                            m_sourceDepleted.Value = true;
                            m_hasNoElementsLeft.Value = true;
                            throw;
                        }
                    }
                }

                internal bool GrabChunk_Buffered(KeyValuePair<long, TSource>[] destArray, int requestedChunkSize, ref int actualNumElementsGrabbed)
                {
                    Contract.Assert(requestedChunkSize > 0);
                    Contract.Assert(!m_useSingleChunking, "Did not expect to be in single-chunking mode");
                    TryCopyFromFillBuffer(destArray, requestedChunkSize, ref actualNumElementsGrabbed);
                    if (actualNumElementsGrabbed == requestedChunkSize)
                    {
                        return true;
                    }
                    else if (m_sourceDepleted.Value)
                    {
                        m_hasNoElementsLeft.Value = true;
                        m_FillBuffer = null;
                        return (actualNumElementsGrabbed > 0);
                    }

                    lock (m_sharedLock)
                    {
                        if (m_sourceDepleted.Value)
                        {
                            return (actualNumElementsGrabbed > 0);
                        }

                        try
                        {
                            if (m_activeCopiers > 0)
                            {
                                SpinWait sw = new SpinWait();
                                while (m_activeCopiers > 0)
                                    sw.SpinOnce();
                            }

                            Contract.Assert(m_sharedIndex != null);
                            for (; actualNumElementsGrabbed < requestedChunkSize; actualNumElementsGrabbed++)
                            {
                                if (m_sharedReader.MoveNext())
                                {
                                    m_sharedIndex.Value = checked (m_sharedIndex.Value + 1);
                                    destArray[actualNumElementsGrabbed] = new KeyValuePair<long, TSource>(m_sharedIndex.Value, m_sharedReader.Current);
                                }
                                else
                                {
                                    m_sourceDepleted.Value = true;
                                    break;
                                }
                            }

                            var localFillBufferRef = m_FillBuffer;
                            if (m_sourceDepleted.Value == false && localFillBufferRef != null && m_FillBufferCurrentPosition >= localFillBufferRef.Length)
                            {
                                for (int i = 0; i < localFillBufferRef.Length; i++)
                                {
                                    if (m_sharedReader.MoveNext())
                                    {
                                        m_sharedIndex.Value = checked (m_sharedIndex.Value + 1);
                                        localFillBufferRef[i] = new KeyValuePair<long, TSource>(m_sharedIndex.Value, m_sharedReader.Current);
                                    }
                                    else
                                    {
                                        m_sourceDepleted.Value = true;
                                        m_FillBufferSize = i;
                                        break;
                                    }
                                }

                                m_FillBufferCurrentPosition = 0;
                            }
                        }
                        catch
                        {
                            m_sourceDepleted.Value = true;
                            m_hasNoElementsLeft.Value = true;
                            throw;
                        }
                    }

                    return (actualNumElementsGrabbed > 0);
                }

                public void Dispose()
                {
                    if (!m_disposed)
                    {
                        m_disposed = true;
                        m_sharedReader.Dispose();
                    }
                }
            }

            private class InternalPartitionEnumerator : DynamicPartitionEnumerator_Abstract<TSource, IEnumerator<TSource>>
            {
                private KeyValuePair<long, TSource>[] m_localList;
                private readonly SharedBool m_hasNoElementsLeft;
                private readonly object m_sharedLock;
                private readonly SharedInt m_activePartitionCount;
                private InternalPartitionEnumerable m_enumerable;
                internal InternalPartitionEnumerator(IEnumerator<TSource> sharedReader, SharedLong sharedIndex, SharedBool hasNoElementsLeft, object sharedLock, SharedInt activePartitionCount, InternalPartitionEnumerable enumerable, bool useSingleChunking): base (sharedReader, sharedIndex, useSingleChunking)
                {
                    m_hasNoElementsLeft = hasNoElementsLeft;
                    m_sharedLock = sharedLock;
                    m_enumerable = enumerable;
                    m_activePartitionCount = activePartitionCount;
                    if (m_activePartitionCount != null)
                    {
                        Interlocked.Increment(ref m_activePartitionCount.Value);
                    }
                }

                override protected bool GrabNextChunk(int requestedChunkSize)
                {
                    Contract.Assert(requestedChunkSize > 0);
                    if (HasNoElementsLeft)
                    {
                        return false;
                    }

                    if (m_localList == null)
                    {
                        m_localList = new KeyValuePair<long, TSource>[m_maxChunkSize];
                    }

                    return m_enumerable.GrabChunk(m_localList, requestedChunkSize, ref m_currentChunkSize.Value);
                }

                override protected bool HasNoElementsLeft
                {
                    get
                    {
                        return m_hasNoElementsLeft.Value;
                    }

                    set
                    {
                        Contract.Assert(value);
                        Contract.Assert(!m_hasNoElementsLeft.Value);
                        m_hasNoElementsLeft.Value = true;
                    }
                }

                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        if (m_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                        }

                        Contract.Assert(m_localList != null);
                        Contract.Assert(m_localOffset.Value >= 0 && m_localOffset.Value < m_currentChunkSize.Value);
                        return (m_localList[m_localOffset.Value]);
                    }
                }

                override public void Dispose()
                {
                    if (m_activePartitionCount != null && Interlocked.Decrement(ref m_activePartitionCount.Value) == 0)
                    {
                        m_enumerable.Dispose();
                    }
                }
            }
        }

        private abstract class DynamicPartitionerForIndexRange_Abstract<TSource, TCollection> : OrderablePartitioner<TSource>
        {
            TCollection m_data;
            protected DynamicPartitionerForIndexRange_Abstract(TCollection data): base (true, false, true)
            {
                m_data = data;
            }

            protected abstract IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(TCollection data);
            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }

                IEnumerator<KeyValuePair<long, TSource>>[] partitions = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
                IEnumerable<KeyValuePair<long, TSource>> partitionEnumerable = GetOrderableDynamicPartitions_Factory(m_data);
                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = partitionEnumerable.GetEnumerator();
                }

                return partitions;
            }

            override public IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
            {
                return GetOrderableDynamicPartitions_Factory(m_data);
            }

            override public bool SupportsDynamicPartitions
            {
                get
                {
                    return true;
                }
            }
        }

        private abstract class DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, TSourceReader> : DynamicPartitionEnumerator_Abstract<TSource, TSourceReader>
        {
            protected int m_startIndex;
            protected DynamicPartitionEnumeratorForIndexRange_Abstract(TSourceReader sharedReader, SharedLong sharedIndex): base (sharedReader, sharedIndex)
            {
            }

            protected abstract int SourceCount
            {
                get;
            }

            override protected bool GrabNextChunk(int requestedChunkSize)
            {
                Contract.Assert(requestedChunkSize > 0);
                while (!HasNoElementsLeft)
                {
                    Contract.Assert(m_sharedIndex != null);
                    long oldSharedIndex = Volatile.Read(ref m_sharedIndex.Value);
                    if (HasNoElementsLeft)
                    {
                        return false;
                    }

                    long newSharedIndex = Math.Min(SourceCount - 1, oldSharedIndex + requestedChunkSize);
                    if (Interlocked.CompareExchange(ref m_sharedIndex.Value, newSharedIndex, oldSharedIndex) == oldSharedIndex)
                    {
                        m_currentChunkSize.Value = (int)(newSharedIndex - oldSharedIndex);
                        m_localOffset.Value = -1;
                        m_startIndex = (int)(oldSharedIndex + 1);
                        return true;
                    }
                }

                return false;
            }

            override protected bool HasNoElementsLeft
            {
                get
                {
                    Contract.Assert(m_sharedIndex != null);
                    return Volatile.Read(ref m_sharedIndex.Value) >= SourceCount - 1;
                }

                set
                {
                    Contract.Assert(false);
                }
            }

            override public void Dispose()
            {
            }
        }

        private class DynamicPartitionerForIList<TSource> : DynamicPartitionerForIndexRange_Abstract<TSource, IList<TSource>>
        {
            internal DynamicPartitionerForIList(IList<TSource> source): base (source)
            {
            }

            override protected IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(IList<TSource> m_data)
            {
                return new InternalPartitionEnumerable(m_data);
            }

            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>
            {
                private readonly IList<TSource> m_sharedReader;
                private SharedLong m_sharedIndex;
                internal InternalPartitionEnumerable(IList<TSource> sharedReader)
                {
                    m_sharedReader = sharedReader;
                    m_sharedIndex = new SharedLong(-1);
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    return new InternalPartitionEnumerator(m_sharedReader, m_sharedIndex);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }
            }

            private class InternalPartitionEnumerator : DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, IList<TSource>>
            {
                internal InternalPartitionEnumerator(IList<TSource> sharedReader, SharedLong sharedIndex): base (sharedReader, sharedIndex)
                {
                }

                override protected int SourceCount
                {
                    get
                    {
                        return m_sharedReader.Count;
                    }
                }

                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        if (m_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                        }

                        Contract.Assert(m_localOffset.Value >= 0 && m_localOffset.Value < m_currentChunkSize.Value);
                        return new KeyValuePair<long, TSource>(m_startIndex + m_localOffset.Value, m_sharedReader[m_startIndex + m_localOffset.Value]);
                    }
                }
            }
        }

        private class DynamicPartitionerForArray<TSource> : DynamicPartitionerForIndexRange_Abstract<TSource, TSource[]>
        {
            internal DynamicPartitionerForArray(TSource[] source): base (source)
            {
            }

            override protected IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions_Factory(TSource[] m_data)
            {
                return new InternalPartitionEnumerable(m_data);
            }

            private class InternalPartitionEnumerable : IEnumerable<KeyValuePair<long, TSource>>
            {
                private readonly TSource[] m_sharedReader;
                private SharedLong m_sharedIndex;
                internal InternalPartitionEnumerable(TSource[] sharedReader)
                {
                    m_sharedReader = sharedReader;
                    m_sharedIndex = new SharedLong(-1);
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((InternalPartitionEnumerable)this).GetEnumerator();
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    return new InternalPartitionEnumerator(m_sharedReader, m_sharedIndex);
                }
            }

            private class InternalPartitionEnumerator : DynamicPartitionEnumeratorForIndexRange_Abstract<TSource, TSource[]>
            {
                internal InternalPartitionEnumerator(TSource[] sharedReader, SharedLong sharedIndex): base (sharedReader, sharedIndex)
                {
                }

                override protected int SourceCount
                {
                    get
                    {
                        return m_sharedReader.Length;
                    }
                }

                override public KeyValuePair<long, TSource> Current
                {
                    get
                    {
                        if (m_currentChunkSize == null)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                        }

                        Contract.Assert(m_localOffset.Value >= 0 && m_localOffset.Value < m_currentChunkSize.Value);
                        return new KeyValuePair<long, TSource>(m_startIndex + m_localOffset.Value, m_sharedReader[m_startIndex + m_localOffset.Value]);
                    }
                }
            }
        }

        private abstract class StaticIndexRangePartitioner<TSource, TCollection> : OrderablePartitioner<TSource>
        {
            protected StaticIndexRangePartitioner(): base (true, true, true)
            {
            }

            protected abstract int SourceCount
            {
                get;
            }

            protected abstract IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex);
            override public IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                if (partitionCount <= 0)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }

                int quotient, remainder;
                quotient = Math.DivRem(SourceCount, partitionCount, out remainder);
                IEnumerator<KeyValuePair<long, TSource>>[] partitions = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];
                int lastEndIndex = -1;
                for (int i = 0; i < partitionCount; i++)
                {
                    int startIndex = lastEndIndex + 1;
                    if (i < remainder)
                    {
                        lastEndIndex = startIndex + quotient;
                    }
                    else
                    {
                        lastEndIndex = startIndex + quotient - 1;
                    }

                    partitions[i] = CreatePartition(startIndex, lastEndIndex);
                }

                return partitions;
            }
        }

        private abstract class StaticIndexRangePartition<TSource> : IEnumerator<KeyValuePair<long, TSource>>
        {
            protected readonly int m_startIndex;
            protected readonly int m_endIndex;
            protected volatile int m_offset;
            protected StaticIndexRangePartition(int startIndex, int endIndex)
            {
                m_startIndex = startIndex;
                m_endIndex = endIndex;
                m_offset = startIndex - 1;
            }

            public abstract KeyValuePair<long, TSource> Current
            {
                get;
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public bool MoveNext()
            {
                if (m_offset < m_endIndex)
                {
                    m_offset++;
                    return true;
                }
                else
                {
                    m_offset = m_endIndex + 1;
                    return false;
                }
            }

            Object IEnumerator.Current
            {
                get
                {
                    return ((StaticIndexRangePartition<TSource>)this).Current;
                }
            }
        }

        private class StaticIndexRangePartitionerForIList<TSource> : StaticIndexRangePartitioner<TSource, IList<TSource>>
        {
            IList<TSource> m_list;
            internal StaticIndexRangePartitionerForIList(IList<TSource> list): base ()
            {
                Contract.Assert(list != null);
                m_list = list;
            }

            override protected int SourceCount
            {
                get
                {
                    return m_list.Count;
                }
            }

            override protected IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex)
            {
                return new StaticIndexRangePartitionForIList<TSource>(m_list, startIndex, endIndex);
            }
        }

        private class StaticIndexRangePartitionForIList<TSource> : StaticIndexRangePartition<TSource>
        {
            private volatile IList<TSource> m_list;
            internal StaticIndexRangePartitionForIList(IList<TSource> list, int startIndex, int endIndex): base (startIndex, endIndex)
            {
                Contract.Assert(startIndex >= 0 && endIndex <= list.Count - 1);
                m_list = list;
            }

            override public KeyValuePair<long, TSource> Current
            {
                get
                {
                    if (m_offset < m_startIndex)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                    }

                    Contract.Assert(m_offset >= m_startIndex && m_offset <= m_endIndex);
                    return (new KeyValuePair<long, TSource>(m_offset, m_list[m_offset]));
                }
            }
        }

        private class StaticIndexRangePartitionerForArray<TSource> : StaticIndexRangePartitioner<TSource, TSource[]>
        {
            TSource[] m_array;
            internal StaticIndexRangePartitionerForArray(TSource[] array): base ()
            {
                Contract.Assert(array != null);
                m_array = array;
            }

            override protected int SourceCount
            {
                get
                {
                    return m_array.Length;
                }
            }

            override protected IEnumerator<KeyValuePair<long, TSource>> CreatePartition(int startIndex, int endIndex)
            {
                return new StaticIndexRangePartitionForArray<TSource>(m_array, startIndex, endIndex);
            }
        }

        private class StaticIndexRangePartitionForArray<TSource> : StaticIndexRangePartition<TSource>
        {
            private volatile TSource[] m_array;
            internal StaticIndexRangePartitionForArray(TSource[] array, int startIndex, int endIndex): base (startIndex, endIndex)
            {
                Contract.Assert(startIndex >= 0 && endIndex <= array.Length - 1);
                m_array = array;
            }

            override public KeyValuePair<long, TSource> Current
            {
                get
                {
                    if (m_offset < m_startIndex)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("PartitionerStatic_CurrentCalledBeforeMoveNext"));
                    }

                    Contract.Assert(m_offset >= m_startIndex && m_offset <= m_endIndex);
                    return (new KeyValuePair<long, TSource>(m_offset, m_array[m_offset]));
                }
            }
        }

        private class SharedInt
        {
            internal volatile int Value;
            internal SharedInt(int value)
            {
                this.Value = value;
            }
        }

        private class SharedBool
        {
            internal volatile bool Value;
            internal SharedBool(bool value)
            {
                this.Value = value;
            }
        }

        private class SharedLong
        {
            internal long Value;
            internal SharedLong(long value)
            {
                this.Value = value;
            }
        }

        private const int DEFAULT_BYTES_PER_CHUNK = 128 * 4;
        private static int GetDefaultChunkSize<TSource>()
        {
            int chunkSize;
            if (typeof (TSource).IsValueType)
            {
                chunkSize = 128;
            }
            else
            {
                Contract.Assert((DEFAULT_BYTES_PER_CHUNK % IntPtr.Size) == 0, "bytes per chunk should be a multiple of pointer size");
                chunkSize = (DEFAULT_BYTES_PER_CHUNK / IntPtr.Size);
            }

            return chunkSize;
        }
    }
}