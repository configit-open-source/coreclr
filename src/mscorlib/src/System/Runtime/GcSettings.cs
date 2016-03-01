namespace System.Runtime
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    public enum GCLargeObjectHeapCompactionMode
    {
        Default = 1,
        CompactOnce = 2
    }

    public enum GCLatencyMode
    {
        Batch = 0,
        Interactive = 1,
        LowLatency = 2,
        SustainedLowLatency = 3,
        NoGCRegion = 4
    }

    public static class GCSettings
    {
        enum SetLatencyModeStatus
        {
            Succeeded = 0,
            NoGCInProgress = 1
        }

        ;
        public static GCLatencyMode LatencyMode
        {
            [System.Security.SecuritySafeCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return (GCLatencyMode)(GC.GetGCLatencyMode());
            }

            [System.Security.SecurityCritical]
            [HostProtection(MayLeakOnAbort = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                if ((value < GCLatencyMode.Batch) || (value > GCLatencyMode.SustainedLowLatency))
                {
                    throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                }

                Contract.EndContractBlock();
                if (GC.SetGCLatencyMode((int)value) == (int)SetLatencyModeStatus.NoGCInProgress)
                    throw new InvalidOperationException("The NoGCRegion mode is in progress. End it and then set a different mode.");
            }
        }

        public static GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode
        {
            [System.Security.SecuritySafeCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return (GCLargeObjectHeapCompactionMode)(GC.GetLOHCompactionMode());
            }

            [System.Security.SecurityCritical]
            [HostProtection(MayLeakOnAbort = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                if ((value < GCLargeObjectHeapCompactionMode.Default) || (value > GCLargeObjectHeapCompactionMode.CompactOnce))
                {
                    throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                }

                Contract.EndContractBlock();
                GC.SetLOHCompactionMode((int)value);
            }
        }

        public static bool IsServerGC
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return GC.IsServerGC();
            }
        }
    }
}