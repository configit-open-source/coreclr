namespace System
{
    using System;
    using System.Security.Permissions;
    using System.Reflection;
    using System.Security;
    using System.Threading;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    public enum GCCollectionMode
    {
        Default = 0,
        Forced = 1,
        Optimized = 2
    }

    internal enum InternalGCCollectionMode
    {
        NonBlocking = 0x00000001,
        Blocking = 0x00000002,
        Optimized = 0x00000004,
        Compacting = 0x00000008
    }

    public enum GCNotificationStatus
    {
        Succeeded = 0,
        Failed = 1,
        Canceled = 2,
        Timeout = 3,
        NotApplicable = 4
    }

    public static class GC
    {
        internal static extern int GetGCLatencyMode();
        internal static extern int SetGCLatencyMode(int newLatencyMode);
        internal static extern int _StartNoGCRegion(long totalSize, bool lohSizeKnown, long lohSize, bool disallowFullBlockingGC);
        internal static extern int _EndNoGCRegion();
        internal static extern int GetLOHCompactionMode();
        internal static extern void SetLOHCompactionMode(int newLOHCompactionMode);
        private static extern int GetGenerationWR(IntPtr handle);
        private static extern long GetTotalMemory();
        private static extern void _Collect(int generation, int mode);
        private static extern int GetMaxGeneration();
        private static extern int _CollectionCount(int generation, int getSpecialGCCount);
        internal static extern bool IsServerGC();
        private static extern void _AddMemoryPressure(UInt64 bytesAllocated);
        private static extern void _RemoveMemoryPressure(UInt64 bytesAllocated);
        public static void AddMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated <= 0)
            {
                throw new ArgumentOutOfRangeException("bytesAllocated", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            if ((4 == IntPtr.Size) && (bytesAllocated > Int32.MaxValue))
            {
                throw new ArgumentOutOfRangeException("pressure", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegInt32"));
            }

            Contract.EndContractBlock();
            _AddMemoryPressure((ulong)bytesAllocated);
        }

        public static void RemoveMemoryPressure(long bytesAllocated)
        {
            if (bytesAllocated <= 0)
            {
                throw new ArgumentOutOfRangeException("bytesAllocated", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            if ((4 == IntPtr.Size) && (bytesAllocated > Int32.MaxValue))
            {
                throw new ArgumentOutOfRangeException("bytesAllocated", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegInt32"));
            }

            Contract.EndContractBlock();
            _RemoveMemoryPressure((ulong)bytesAllocated);
        }

        public static extern int GetGeneration(Object obj);
        public static void Collect(int generation)
        {
            Collect(generation, GCCollectionMode.Default);
        }

        public static void Collect()
        {
            _Collect(-1, (int)InternalGCCollectionMode.Blocking);
        }

        public static void Collect(int generation, GCCollectionMode mode)
        {
            Collect(generation, mode, true);
        }

        public static void Collect(int generation, GCCollectionMode mode, bool blocking)
        {
            Collect(generation, mode, blocking, false);
        }

        public static void Collect(int generation, GCCollectionMode mode, bool blocking, bool compacting)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException("generation", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            if ((mode < GCCollectionMode.Default) || (mode > GCCollectionMode.Optimized))
            {
                throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            }

            Contract.EndContractBlock();
            int iInternalModes = 0;
            if (mode == GCCollectionMode.Optimized)
            {
                iInternalModes |= (int)InternalGCCollectionMode.Optimized;
            }

            if (compacting)
                iInternalModes |= (int)InternalGCCollectionMode.Compacting;
            if (blocking)
            {
                iInternalModes |= (int)InternalGCCollectionMode.Blocking;
            }
            else if (!compacting)
            {
                iInternalModes |= (int)InternalGCCollectionMode.NonBlocking;
            }

            _Collect(generation, iInternalModes);
        }

        public static int CollectionCount(int generation)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException("generation", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            Contract.EndContractBlock();
            return _CollectionCount(generation, 0);
        }

        internal static int CollectionCount(int generation, bool getSpecialGCCount)
        {
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException("generation", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            Contract.EndContractBlock();
            return _CollectionCount(generation, (getSpecialGCCount ? 1 : 0));
        }

        public static void KeepAlive(Object obj)
        {
        }

        public static int GetGeneration(WeakReference wo)
        {
            int result = GetGenerationWR(wo.m_handle);
            KeepAlive(wo);
            return result;
        }

        public static int MaxGeneration
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return GetMaxGeneration();
            }
        }

        private static extern void _WaitForPendingFinalizers();
        public static void WaitForPendingFinalizers()
        {
            _WaitForPendingFinalizers();
        }

        private static extern void _SuppressFinalize(Object o);
        public static void SuppressFinalize(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Contract.EndContractBlock();
            _SuppressFinalize(obj);
        }

        private static extern void _ReRegisterForFinalize(Object o);
        public static void ReRegisterForFinalize(Object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            Contract.EndContractBlock();
            _ReRegisterForFinalize(obj);
        }

        public static long GetTotalMemory(bool forceFullCollection)
        {
            long size = GetTotalMemory();
            if (!forceFullCollection)
                return size;
            int reps = 20;
            long newSize = size;
            float diff;
            do
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
                size = newSize;
                newSize = GetTotalMemory();
                diff = ((float)(newSize - size)) / size;
            }
            while (reps-- > 0 && !(-.05 < diff && diff < .05));
            return newSize;
        }

        private static extern bool _RegisterForFullGCNotification(int maxGenerationPercentage, int largeObjectHeapPercentage);
        private static extern bool _CancelFullGCNotification();
        private static extern int _WaitForFullGCApproach(int millisecondsTimeout);
        private static extern int _WaitForFullGCComplete(int millisecondsTimeout);
        public static void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold)
        {
            if ((maxGenerationThreshold <= 0) || (maxGenerationThreshold >= 100))
            {
                throw new ArgumentOutOfRangeException("maxGenerationThreshold", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"), 1, 99));
            }

            if ((largeObjectHeapThreshold <= 0) || (largeObjectHeapThreshold >= 100))
            {
                throw new ArgumentOutOfRangeException("largeObjectHeapThreshold", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"), 1, 99));
            }

            if (!_RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold))
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotWithConcurrentGC"));
            }
        }

        public static void CancelFullGCNotification()
        {
            if (!_CancelFullGCNotification())
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotWithConcurrentGC"));
            }
        }

        public static GCNotificationStatus WaitForFullGCApproach()
        {
            return (GCNotificationStatus)_WaitForFullGCApproach(-1);
        }

        public static GCNotificationStatus WaitForFullGCApproach(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            return (GCNotificationStatus)_WaitForFullGCApproach(millisecondsTimeout);
        }

        public static GCNotificationStatus WaitForFullGCComplete()
        {
            return (GCNotificationStatus)_WaitForFullGCComplete(-1);
        }

        public static GCNotificationStatus WaitForFullGCComplete(int millisecondsTimeout)
        {
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            return (GCNotificationStatus)_WaitForFullGCComplete(millisecondsTimeout);
        }

        enum StartNoGCRegionStatus
        {
            Succeeded = 0,
            NotEnoughMemory = 1,
            AmountTooLarge = 2,
            AlreadyInProgress = 3
        }

        enum EndNoGCRegionStatus
        {
            Succeeded = 0,
            NotInProgress = 1,
            GCInduced = 2,
            AllocationExceeded = 3
        }

        static bool StartNoGCRegionWorker(long totalSize, bool hasLohSize, long lohSize, bool disallowFullBlockingGC)
        {
            StartNoGCRegionStatus status = (StartNoGCRegionStatus)_StartNoGCRegion(totalSize, hasLohSize, lohSize, disallowFullBlockingGC);
            if (status == StartNoGCRegionStatus.AmountTooLarge)
                throw new ArgumentOutOfRangeException("totalSize", "totalSize is too large. For more information about setting the maximum size, see \"Latency Modes\" in http://go.microsoft.com/fwlink/?LinkId=522706");
            else if (status == StartNoGCRegionStatus.AlreadyInProgress)
                throw new InvalidOperationException("The NoGCRegion mode was already in progress");
            else if (status == StartNoGCRegionStatus.NotEnoughMemory)
                return false;
            return true;
        }

        public static bool TryStartNoGCRegion(long totalSize)
        {
            return StartNoGCRegionWorker(totalSize, false, 0, false);
        }

        public static bool TryStartNoGCRegion(long totalSize, long lohSize)
        {
            return StartNoGCRegionWorker(totalSize, true, lohSize, false);
        }

        public static bool TryStartNoGCRegion(long totalSize, bool disallowFullBlockingGC)
        {
            return StartNoGCRegionWorker(totalSize, false, 0, disallowFullBlockingGC);
        }

        public static bool TryStartNoGCRegion(long totalSize, long lohSize, bool disallowFullBlockingGC)
        {
            return StartNoGCRegionWorker(totalSize, true, lohSize, disallowFullBlockingGC);
        }

        static EndNoGCRegionStatus EndNoGCRegionWorker()
        {
            EndNoGCRegionStatus status = (EndNoGCRegionStatus)_EndNoGCRegion();
            if (status == EndNoGCRegionStatus.NotInProgress)
                throw new InvalidOperationException("NoGCRegion mode must be set");
            else if (status == EndNoGCRegionStatus.GCInduced)
                throw new InvalidOperationException("Garbage collection was induced in NoGCRegion mode");
            else if (status == EndNoGCRegionStatus.AllocationExceeded)
                throw new InvalidOperationException("Allocated memory exceeds specified memory for NoGCRegion mode");
            return EndNoGCRegionStatus.Succeeded;
        }

        public static void EndNoGCRegion()
        {
            EndNoGCRegionWorker();
        }
    }
}