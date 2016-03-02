using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    internal sealed class PinnableBufferCache
    {
        public PinnableBufferCache(string cacheName, int numberOfElements): this (cacheName, () => new byte[numberOfElements])
        {
        }

        public byte[] AllocateBuffer()
        {
            return (byte[])Allocate();
        }

        public void FreeBuffer(byte[] buffer)
        {
            Free(buffer);
        }

        internal PinnableBufferCache(string cacheName, Func<object> factory)
        {
            m_NotGen2 = new List<object>(DefaultNumberOfBuffers);
            m_factory = factory;
            string envVarName = "PinnableBufferCache_" + cacheName + "_Disabled";
            try
            {
                string envVar = Environment.GetEnvironmentVariable(envVarName);
                if (envVar != null)
                {
                    PinnableBufferCacheEventSource.Log.DebugMessage("Creating " + cacheName + " PinnableBufferCacheDisabled=" + envVar);
                    int index = envVar.IndexOf(cacheName, StringComparison.OrdinalIgnoreCase);
                    if (0 <= index)
                    {
                        PinnableBufferCacheEventSource.Log.DebugMessage("Disabling " + cacheName);
                        return;
                    }
                }
            }
            catch
            {
            }

            string minEnvVarName = "PinnableBufferCache_" + cacheName + "_MinCount";
            try
            {
                string minEnvVar = Environment.GetEnvironmentVariable(minEnvVarName);
                if (minEnvVar != null)
                {
                    if (int.TryParse(minEnvVar, out m_minBufferCount))
                        CreateNewBuffers();
                }
            }
            catch
            {
            }

            PinnableBufferCacheEventSource.Log.Create(cacheName);
            m_CacheName = cacheName;
        }

        internal object Allocate()
        {
            if (m_CacheName == null)
                return m_factory();
            object returnBuffer;
            if (!m_FreeList.TryPop(out returnBuffer))
                Restock(out returnBuffer);
            if (PinnableBufferCacheEventSource.Log.IsEnabled())
            {
                int numAllocCalls = Interlocked.Increment(ref m_numAllocCalls);
                if (numAllocCalls >= 1024)
                {
                    lock (this)
                    {
                        int previousNumAllocCalls = Interlocked.Exchange(ref m_numAllocCalls, 0);
                        if (previousNumAllocCalls >= 1024)
                        {
                            int nonGen2Count = 0;
                            foreach (object o in m_FreeList)
                            {
                                if (GC.GetGeneration(o) < GC.MaxGeneration)
                                {
                                    nonGen2Count++;
                                }
                            }

                            PinnableBufferCacheEventSource.Log.WalkFreeListResult(m_CacheName, m_FreeList.Count, nonGen2Count);
                        }
                    }
                }

                PinnableBufferCacheEventSource.Log.AllocateBuffer(m_CacheName, PinnableBufferCacheEventSource.AddressOf(returnBuffer), returnBuffer.GetHashCode(), GC.GetGeneration(returnBuffer), m_FreeList.Count);
            }

            return returnBuffer;
        }

        internal void Free(object buffer)
        {
            if (m_CacheName == null)
                return;
            if (PinnableBufferCacheEventSource.Log.IsEnabled())
                PinnableBufferCacheEventSource.Log.FreeBuffer(m_CacheName, PinnableBufferCacheEventSource.AddressOf(buffer), buffer.GetHashCode(), m_FreeList.Count);
            if ((m_gen1CountAtLastRestock + 3) > GC.CollectionCount(GC.MaxGeneration - 1))
            {
                lock (this)
                {
                    if (GC.GetGeneration(buffer) < GC.MaxGeneration)
                    {
                        m_moreThanFreeListNeeded = true;
                        PinnableBufferCacheEventSource.Log.FreeBufferStillTooYoung(m_CacheName, m_NotGen2.Count);
                        m_NotGen2.Add(buffer);
                        m_gen1CountAtLastRestock = GC.CollectionCount(GC.MaxGeneration - 1);
                        return;
                    }
                }
            }

            m_FreeList.Push(buffer);
        }

        private void Restock(out object returnBuffer)
        {
            lock (this)
            {
                if (m_FreeList.TryPop(out returnBuffer))
                    return;
                if (m_restockSize == 0)
                    Gen2GcCallback.Register(Gen2GcCallbackFunc, this);
                m_moreThanFreeListNeeded = true;
                PinnableBufferCacheEventSource.Log.AllocateBufferFreeListEmpty(m_CacheName, m_NotGen2.Count);
                if (m_NotGen2.Count == 0)
                    CreateNewBuffers();
                int idx = m_NotGen2.Count - 1;
                if (GC.GetGeneration(m_NotGen2[idx]) < GC.MaxGeneration && GC.GetGeneration(m_NotGen2[0]) == GC.MaxGeneration)
                    idx = 0;
                returnBuffer = m_NotGen2[idx];
                m_NotGen2.RemoveAt(idx);
                if (PinnableBufferCacheEventSource.Log.IsEnabled() && GC.GetGeneration(returnBuffer) < GC.MaxGeneration)
                {
                    PinnableBufferCacheEventSource.Log.AllocateBufferFromNotGen2(m_CacheName, m_NotGen2.Count);
                }

                if (!AgePendingBuffers())
                {
                    if (m_NotGen2.Count == m_restockSize / 2)
                    {
                        PinnableBufferCacheEventSource.Log.DebugMessage("Proactively adding more buffers to aging pool");
                        CreateNewBuffers();
                    }
                }
            }
        }

        private bool AgePendingBuffers()
        {
            if (m_gen1CountAtLastRestock < GC.CollectionCount(GC.MaxGeneration - 1))
            {
                int promotedCount = 0;
                List<object> notInGen2 = new List<object>();
                PinnableBufferCacheEventSource.Log.AllocateBufferAged(m_CacheName, m_NotGen2.Count);
                for (int i = 0; i < m_NotGen2.Count; i++)
                {
                    object currentBuffer = m_NotGen2[i];
                    if (GC.GetGeneration(currentBuffer) >= GC.MaxGeneration)
                    {
                        m_FreeList.Push(currentBuffer);
                        promotedCount++;
                    }
                    else
                    {
                        notInGen2.Add(currentBuffer);
                    }
                }

                PinnableBufferCacheEventSource.Log.AgePendingBuffersResults(m_CacheName, promotedCount, notInGen2.Count);
                m_NotGen2 = notInGen2;
                return true;
            }

            return false;
        }

        private void CreateNewBuffers()
        {
            if (m_restockSize == 0)
                m_restockSize = 4;
            else if (m_restockSize < DefaultNumberOfBuffers)
                m_restockSize = DefaultNumberOfBuffers;
            else if (m_restockSize < 256)
                m_restockSize = m_restockSize * 2;
            else if (m_restockSize < 4096)
                m_restockSize = m_restockSize * 3 / 2;
            else
                m_restockSize = 4096;
            if (m_minBufferCount > m_buffersUnderManagement)
                m_restockSize = Math.Max(m_restockSize, m_minBufferCount - m_buffersUnderManagement);
            PinnableBufferCacheEventSource.Log.AllocateBufferCreatingNewBuffers(m_CacheName, m_buffersUnderManagement, m_restockSize);
            for (int i = 0; i < m_restockSize; i++)
            {
                object newBuffer = m_factory();
                var dummyObject = new object ();
                m_NotGen2.Add(newBuffer);
            }

            m_buffersUnderManagement += m_restockSize;
            m_gen1CountAtLastRestock = GC.CollectionCount(GC.MaxGeneration - 1);
        }

        private static bool Gen2GcCallbackFunc(object targetObj)
        {
            return ((PinnableBufferCache)(targetObj)).TrimFreeListIfNeeded();
        }

        private bool TrimFreeListIfNeeded()
        {
            int curMSec = Environment.TickCount;
            int deltaMSec = curMSec - m_msecNoUseBeyondFreeListSinceThisTime;
            PinnableBufferCacheEventSource.Log.TrimCheck(m_CacheName, m_buffersUnderManagement, m_moreThanFreeListNeeded, deltaMSec);
            if (m_moreThanFreeListNeeded)
            {
                m_moreThanFreeListNeeded = false;
                m_trimmingExperimentInProgress = false;
                m_msecNoUseBeyondFreeListSinceThisTime = curMSec;
                return true;
            }

            if (0 <= deltaMSec && deltaMSec < 10000)
                return true;
            lock (this)
            {
                if (m_moreThanFreeListNeeded)
                {
                    m_moreThanFreeListNeeded = false;
                    m_trimmingExperimentInProgress = false;
                    m_msecNoUseBeyondFreeListSinceThisTime = curMSec;
                    return true;
                }

                var freeCount = m_FreeList.Count;
                if (m_NotGen2.Count > 0)
                {
                    if (!m_trimmingExperimentInProgress)
                    {
                        PinnableBufferCacheEventSource.Log.TrimFlush(m_CacheName, m_buffersUnderManagement, freeCount, m_NotGen2.Count);
                        AgePendingBuffers();
                        m_trimmingExperimentInProgress = true;
                        return true;
                    }

                    PinnableBufferCacheEventSource.Log.TrimFree(m_CacheName, m_buffersUnderManagement, freeCount, m_NotGen2.Count);
                    m_buffersUnderManagement -= m_NotGen2.Count;
                    var newRestockSize = m_buffersUnderManagement / 4;
                    if (newRestockSize < m_restockSize)
                        m_restockSize = Math.Max(newRestockSize, DefaultNumberOfBuffers);
                    m_NotGen2.Clear();
                    m_trimmingExperimentInProgress = false;
                    return true;
                }

                var trimSize = freeCount / 4 + 1;
                if (freeCount * 15 <= m_buffersUnderManagement || m_buffersUnderManagement - trimSize <= m_minBufferCount)
                {
                    PinnableBufferCacheEventSource.Log.TrimFreeSizeOK(m_CacheName, m_buffersUnderManagement, freeCount);
                    return true;
                }

                PinnableBufferCacheEventSource.Log.TrimExperiment(m_CacheName, m_buffersUnderManagement, freeCount, trimSize);
                object buffer;
                for (int i = 0; i < trimSize; i++)
                {
                    if (m_FreeList.TryPop(out buffer))
                        m_NotGen2.Add(buffer);
                }

                m_msecNoUseBeyondFreeListSinceThisTime = curMSec;
                m_trimmingExperimentInProgress = true;
            }

            return true;
        }

        private const int DefaultNumberOfBuffers = 16;
        private string m_CacheName;
        private Func<object> m_factory;
        private ConcurrentStack<object> m_FreeList = new ConcurrentStack<object>();
        private List<object> m_NotGen2;
        private int m_gen1CountAtLastRestock;
        private int m_msecNoUseBeyondFreeListSinceThisTime;
        private bool m_moreThanFreeListNeeded;
        private int m_buffersUnderManagement;
        private int m_restockSize;
        private bool m_trimmingExperimentInProgress;
        private int m_minBufferCount;
        private int m_numAllocCalls;
    }

    internal sealed class Gen2GcCallback : CriticalFinalizerObject
    {
        public Gen2GcCallback(): base ()
        {
        }

        public static void Register(Func<object, bool> callback, object targetObj)
        {
            Gen2GcCallback gcCallback = new Gen2GcCallback();
            gcCallback.Setup(callback, targetObj);
        }

        private Func<object, bool> m_callback;
        private GCHandle m_weakTargetObj;
        private void Setup(Func<object, bool> callback, object targetObj)
        {
            m_callback = callback;
            m_weakTargetObj = GCHandle.Alloc(targetObj, GCHandleType.Weak);
        }

        ~Gen2GcCallback()
        {
            object targetObj = m_weakTargetObj.Target;
            if (targetObj == null)
            {
                m_weakTargetObj.Free();
                return;
            }

            try
            {
                if (!m_callback(targetObj))
                {
                    return;
                }
            }
            catch
            {
            }

            if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
            {
                GC.ReRegisterForFinalize(this);
            }
        }
    }

    internal sealed class PinnableBufferCacheEventSource
    {
        public static readonly PinnableBufferCacheEventSource Log = new PinnableBufferCacheEventSource();
        public bool IsEnabled()
        {
            return false;
        }

        public void DebugMessage(string message)
        {
        }

        public void DebugMessage1(string message, long value)
        {
        }

        public void DebugMessage2(string message, long value1, long value2)
        {
        }

        public void DebugMessage3(string message, long value1, long value2, long value3)
        {
        }

        public void Create(string cacheName)
        {
        }

        public void AllocateBuffer(string cacheName, ulong objectId, int objectHash, int objectGen, int freeCountAfter)
        {
        }

        public void AllocateBufferFromNotGen2(string cacheName, int notGen2CountAfter)
        {
        }

        public void AllocateBufferCreatingNewBuffers(string cacheName, int totalBuffsBefore, int objectCount)
        {
        }

        public void AllocateBufferAged(string cacheName, int agedCount)
        {
        }

        public void AllocateBufferFreeListEmpty(string cacheName, int notGen2CountBefore)
        {
        }

        public void FreeBuffer(string cacheName, ulong objectId, int objectHash, int freeCountBefore)
        {
        }

        public void FreeBufferStillTooYoung(string cacheName, int notGen2CountBefore)
        {
        }

        public void TrimCheck(string cacheName, int totalBuffs, bool neededMoreThanFreeList, int deltaMSec)
        {
        }

        public void TrimFree(string cacheName, int totalBuffs, int freeListCount, int toBeFreed)
        {
        }

        public void TrimExperiment(string cacheName, int totalBuffs, int freeListCount, int numTrimTrial)
        {
        }

        public void TrimFreeSizeOK(string cacheName, int totalBuffs, int freeListCount)
        {
        }

        public void TrimFlush(string cacheName, int totalBuffs, int freeListCount, int notGen2CountBefore)
        {
        }

        public void AgePendingBuffersResults(string cacheName, int promotedToFreeListCount, int heldBackCount)
        {
        }

        public void WalkFreeListResult(string cacheName, int freeListCount, int gen0BuffersInFreeList)
        {
        }

        static internal ulong AddressOf(object obj)
        {
            return 0;
        }

        static internal unsafe long AddressOfObject(byte[] array)
        {
            return 0;
        }
    }
}