using System.Diagnostics.Contracts;

namespace System.Threading.Tasks
{
    internal struct IndexRange
    {
        internal long m_nFromInclusive;
        internal long m_nToExclusive;
        internal volatile Shared<long> m_nSharedCurrentIndexOffset;
        internal int m_bRangeFinished;
    }

    internal struct RangeWorker
    {
        internal readonly IndexRange[] m_indexRanges;
        internal int m_nCurrentIndexRange;
        internal long m_nStep;
        internal long m_nIncrementValue;
        internal readonly long m_nMaxIncrementValue;
        internal RangeWorker(IndexRange[] ranges, int nInitialRange, long nStep)
        {
            m_indexRanges = ranges;
            m_nCurrentIndexRange = nInitialRange;
            m_nStep = nStep;
            m_nIncrementValue = nStep;
            m_nMaxIncrementValue = Parallel.DEFAULT_LOOP_STRIDE * nStep;
        }

        internal bool FindNewWork(out long nFromInclusiveLocal, out long nToExclusiveLocal)
        {
            int numIndexRangesToVisit = m_indexRanges.Length;
            do
            {
                IndexRange currentRange = m_indexRanges[m_nCurrentIndexRange];
                if (currentRange.m_bRangeFinished == 0)
                {
                    if (m_indexRanges[m_nCurrentIndexRange].m_nSharedCurrentIndexOffset == null)
                    {
                        Interlocked.CompareExchange(ref m_indexRanges[m_nCurrentIndexRange].m_nSharedCurrentIndexOffset, new Shared<long>(0), null);
                    }

                    long nMyOffset = Interlocked.Add(ref m_indexRanges[m_nCurrentIndexRange].m_nSharedCurrentIndexOffset.Value, m_nIncrementValue) - m_nIncrementValue;
                    if (currentRange.m_nToExclusive - currentRange.m_nFromInclusive > nMyOffset)
                    {
                        nFromInclusiveLocal = currentRange.m_nFromInclusive + nMyOffset;
                        nToExclusiveLocal = nFromInclusiveLocal + m_nIncrementValue;
                        if ((nToExclusiveLocal > currentRange.m_nToExclusive) || (nToExclusiveLocal < currentRange.m_nFromInclusive))
                        {
                            nToExclusiveLocal = currentRange.m_nToExclusive;
                        }

                        if (m_nIncrementValue < m_nMaxIncrementValue)
                        {
                            m_nIncrementValue *= 2;
                            if (m_nIncrementValue > m_nMaxIncrementValue)
                            {
                                m_nIncrementValue = m_nMaxIncrementValue;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        Interlocked.Exchange(ref m_indexRanges[m_nCurrentIndexRange].m_bRangeFinished, 1);
                    }
                }

                m_nCurrentIndexRange = (m_nCurrentIndexRange + 1) % m_indexRanges.Length;
                numIndexRangesToVisit--;
            }
            while (numIndexRangesToVisit > 0);
            nFromInclusiveLocal = 0;
            nToExclusiveLocal = 0;
            return false;
        }

        internal bool FindNewWork32(out int nFromInclusiveLocal32, out int nToExclusiveLocal32)
        {
            long nFromInclusiveLocal;
            long nToExclusiveLocal;
            bool bRetVal = FindNewWork(out nFromInclusiveLocal, out nToExclusiveLocal);
            Contract.Assert((nFromInclusiveLocal <= Int32.MaxValue) && (nFromInclusiveLocal >= Int32.MinValue) && (nToExclusiveLocal <= Int32.MaxValue) && (nToExclusiveLocal >= Int32.MinValue));
            nFromInclusiveLocal32 = (int)nFromInclusiveLocal;
            nToExclusiveLocal32 = (int)nToExclusiveLocal;
            return bRetVal;
        }
    }

    internal class RangeManager
    {
        internal readonly IndexRange[] m_indexRanges;
        internal int m_nCurrentIndexRangeToAssign;
        internal long m_nStep;
        internal RangeManager(long nFromInclusive, long nToExclusive, long nStep, int nNumExpectedWorkers)
        {
            m_nCurrentIndexRangeToAssign = 0;
            m_nStep = nStep;
            if (nNumExpectedWorkers == 1)
                nNumExpectedWorkers = 2;
            ulong uSpan = (ulong)(nToExclusive - nFromInclusive);
            ulong uRangeSize = uSpan / (ulong)nNumExpectedWorkers;
            uRangeSize -= uRangeSize % (ulong)nStep;
            if (uRangeSize == 0)
            {
                uRangeSize = (ulong)nStep;
            }

            Contract.Assert((uSpan / uRangeSize) < Int32.MaxValue);
            int nNumRanges = (int)(uSpan / uRangeSize);
            if (uSpan % uRangeSize != 0)
            {
                nNumRanges++;
            }

            long nRangeSize = (long)uRangeSize;
            m_indexRanges = new IndexRange[nNumRanges];
            long nCurrentIndex = nFromInclusive;
            for (int i = 0; i < nNumRanges; i++)
            {
                m_indexRanges[i].m_nFromInclusive = nCurrentIndex;
                m_indexRanges[i].m_nSharedCurrentIndexOffset = null;
                m_indexRanges[i].m_bRangeFinished = 0;
                nCurrentIndex += nRangeSize;
                if (nCurrentIndex < nCurrentIndex - nRangeSize || nCurrentIndex > nToExclusive)
                {
                    Contract.Assert(i == nNumRanges - 1);
                    nCurrentIndex = nToExclusive;
                }

                m_indexRanges[i].m_nToExclusive = nCurrentIndex;
            }
        }

        internal RangeWorker RegisterNewWorker()
        {
            Contract.Assert(m_indexRanges != null && m_indexRanges.Length != 0);
            int nInitialRange = (Interlocked.Increment(ref m_nCurrentIndexRangeToAssign) - 1) % m_indexRanges.Length;
            return new RangeWorker(m_indexRanges, nInitialRange, m_nStep);
        }
    }
}