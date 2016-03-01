using System;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.Runtime
{
    public sealed class MemoryFailPoint : CriticalFinalizerObject, IDisposable
    {
        private static readonly ulong TopOfMemory;
        private static long hiddenLastKnownFreeAddressSpace = 0;
        private static long hiddenLastTimeCheckingAddressSpace = 0;
        private const int CheckThreshold = 10 * 1000;
        private static long LastKnownFreeAddressSpace
        {
            get
            {
                return Volatile.Read(ref hiddenLastKnownFreeAddressSpace);
            }

            set
            {
                Volatile.Write(ref hiddenLastKnownFreeAddressSpace, value);
            }
        }

        private static long AddToLastKnownFreeAddressSpace(long addend)
        {
            return Interlocked.Add(ref hiddenLastKnownFreeAddressSpace, addend);
        }

        private static long LastTimeCheckingAddressSpace
        {
            get
            {
                return Volatile.Read(ref hiddenLastTimeCheckingAddressSpace);
            }

            set
            {
                Volatile.Write(ref hiddenLastTimeCheckingAddressSpace, value);
            }
        }

        private const int LowMemoryFudgeFactor = 16 << 20;
        private const int MemoryCheckGranularity = 16;
        private static readonly ulong GCSegmentSize;
        private ulong _reservedMemory;
        private bool _mustSubtractReservation;
        static MemoryFailPoint()
        {
            GetMemorySettings(out GCSegmentSize, out TopOfMemory);
        }

        public MemoryFailPoint(int sizeInMegabytes)
        {
            if (sizeInMegabytes <= 0)
                throw new ArgumentOutOfRangeException("sizeInMegabytes", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            ulong size = ((ulong)sizeInMegabytes) << 20;
            _reservedMemory = size;
            ulong segmentSize = (ulong)(Math.Ceiling((double)size / GCSegmentSize) * GCSegmentSize);
            if (segmentSize >= TopOfMemory)
                throw new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_TooBig"));
            ulong requestedSizeRounded = (ulong)(Math.Ceiling((double)sizeInMegabytes / MemoryCheckGranularity) * MemoryCheckGranularity);
            requestedSizeRounded <<= 20;
            ulong availPageFile = 0;
            ulong totalAddressSpaceFree = 0;
            for (int stage = 0; stage < 3; stage++)
            {
                CheckForAvailableMemory(out availPageFile, out totalAddressSpaceFree);
                ulong reserved = SharedStatics.MemoryFailPointReservedMemory;
                ulong segPlusReserved = segmentSize + reserved;
                bool overflow = segPlusReserved < segmentSize || segPlusReserved < reserved;
                bool needPageFile = availPageFile < (requestedSizeRounded + reserved + LowMemoryFudgeFactor) || overflow;
                bool needAddressSpace = totalAddressSpaceFree < segPlusReserved || overflow;
                long now = Environment.TickCount;
                if ((now > LastTimeCheckingAddressSpace + CheckThreshold || now < LastTimeCheckingAddressSpace) || LastKnownFreeAddressSpace < (long)segmentSize)
                {
                    CheckForFreeAddressSpace(segmentSize, false);
                }

                bool needContiguousVASpace = (ulong)LastKnownFreeAddressSpace < segmentSize;
                BCLDebug.Trace("MEMORYFAILPOINT", "MemoryFailPoint: Checking for {0} MB, for allocation size of {1} MB, stage {9}.  Need page file? {2}  Need Address Space? {3}  Need Contiguous address space? {4}  Avail page file: {5} MB  Total free VA space: {6} MB  Contiguous free address space (found): {7} MB  Space reserved via process's MemoryFailPoints: {8} MB", segmentSize >> 20, sizeInMegabytes, needPageFile, needAddressSpace, needContiguousVASpace, availPageFile >> 20, totalAddressSpaceFree >> 20, LastKnownFreeAddressSpace >> 20, reserved, stage);
                if (!needPageFile && !needAddressSpace && !needContiguousVASpace)
                    break;
                switch (stage)
                {
                    case 0:
                        GC.Collect();
                        continue;
                    case 1:
                        if (!needPageFile)
                            continue;
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try
                        {
                        }
                        finally
                        {
                            UIntPtr numBytes = new UIntPtr(segmentSize);
                            unsafe
                            {
                                void *pMemory = Win32Native.VirtualAlloc(null, numBytes, Win32Native.MEM_COMMIT, Win32Native.PAGE_READWRITE);
                                if (pMemory != null)
                                {
                                    bool r = Win32Native.VirtualFree(pMemory, UIntPtr.Zero, Win32Native.MEM_RELEASE);
                                    if (!r)
                                        __Error.WinIOError();
                                }
                            }
                        }

                        continue;
                    case 2:
                        if (needPageFile || needAddressSpace)
                        {
                            InsufficientMemoryException e = new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint"));
                            e.Data["MemFailPointState"] = new MemoryFailPointState(sizeInMegabytes, segmentSize, needPageFile, needAddressSpace, needContiguousVASpace, availPageFile >> 20, totalAddressSpaceFree >> 20, LastKnownFreeAddressSpace >> 20, reserved);
                            throw e;
                        }

                        if (needContiguousVASpace)
                        {
                            InsufficientMemoryException e = new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_VAFrag"));
                            e.Data["MemFailPointState"] = new MemoryFailPointState(sizeInMegabytes, segmentSize, needPageFile, needAddressSpace, needContiguousVASpace, availPageFile >> 20, totalAddressSpaceFree >> 20, LastKnownFreeAddressSpace >> 20, reserved);
                            throw e;
                        }

                        break;
                    default:
                        Contract.Assert(false, "Fell through switch statement!");
                        break;
                }
            }

            AddToLastKnownFreeAddressSpace(-((long)size));
            if (LastKnownFreeAddressSpace < 0)
                CheckForFreeAddressSpace(segmentSize, true);
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                SharedStatics.AddMemoryFailPointReservation((long)size);
                _mustSubtractReservation = true;
            }
        }

        private static void CheckForAvailableMemory(out ulong availPageFile, out ulong totalAddressSpaceFree)
        {
            bool r;
            Win32Native.MEMORYSTATUSEX memory = new Win32Native.MEMORYSTATUSEX();
            r = Win32Native.GlobalMemoryStatusEx(ref memory);
            if (!r)
                __Error.WinIOError();
            availPageFile = memory.availPageFile;
            totalAddressSpaceFree = memory.availVirtual;
        }

        private static unsafe bool CheckForFreeAddressSpace(ulong size, bool shouldThrow)
        {
            ulong freeSpaceAfterGCHeap = MemFreeAfterAddress(null, size);
            BCLDebug.Trace("MEMORYFAILPOINT", "MemoryFailPoint: Checked for free VA space.  Found enough? {0}  Asked for: {1}  Found: {2}", (freeSpaceAfterGCHeap >= size), size, freeSpaceAfterGCHeap);
            LastKnownFreeAddressSpace = (long)freeSpaceAfterGCHeap;
            LastTimeCheckingAddressSpace = Environment.TickCount;
            if (freeSpaceAfterGCHeap < size && shouldThrow)
                throw new InsufficientMemoryException(Environment.GetResourceString("InsufficientMemory_MemFailPoint_VAFrag"));
            return freeSpaceAfterGCHeap >= size;
        }

        private static unsafe ulong MemFreeAfterAddress(void *address, ulong size)
        {
            if (size >= TopOfMemory)
                return 0;
            ulong largestFreeRegion = 0;
            Win32Native.MEMORY_BASIC_INFORMATION memInfo = new Win32Native.MEMORY_BASIC_INFORMATION();
            UIntPtr sizeOfMemInfo = (UIntPtr)Marshal.SizeOf(memInfo);
            while (((ulong)address) + size < TopOfMemory)
            {
                UIntPtr r = Win32Native.VirtualQuery(address, ref memInfo, sizeOfMemInfo);
                if (r == UIntPtr.Zero)
                    __Error.WinIOError();
                ulong regionSize = memInfo.RegionSize.ToUInt64();
                if (memInfo.State == Win32Native.MEM_FREE)
                {
                    if (regionSize >= size)
                        return regionSize;
                    else
                        largestFreeRegion = Math.Max(largestFreeRegion, regionSize);
                }

                address = (void *)((ulong)address + regionSize);
            }

            return largestFreeRegion;
        }

        private static extern void GetMemorySettings(out ulong maxGCSegmentSize, out ulong topOfMemory);
        ~MemoryFailPoint()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_mustSubtractReservation)
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    SharedStatics.AddMemoryFailPointReservation(-((long)_reservedMemory));
                    _mustSubtractReservation = false;
                }
            }
        }

        internal sealed class MemoryFailPointState
        {
            private ulong _segmentSize;
            private int _allocationSizeInMB;
            private bool _needPageFile;
            private bool _needAddressSpace;
            private bool _needContiguousVASpace;
            private ulong _availPageFile;
            private ulong _totalFreeAddressSpace;
            private long _lastKnownFreeAddressSpace;
            private ulong _reservedMem;
            private String _stackTrace;
            internal MemoryFailPointState(int allocationSizeInMB, ulong segmentSize, bool needPageFile, bool needAddressSpace, bool needContiguousVASpace, ulong availPageFile, ulong totalFreeAddressSpace, long lastKnownFreeAddressSpace, ulong reservedMem)
            {
                _allocationSizeInMB = allocationSizeInMB;
                _segmentSize = segmentSize;
                _needPageFile = needPageFile;
                _needAddressSpace = needAddressSpace;
                _needContiguousVASpace = needContiguousVASpace;
                _availPageFile = availPageFile;
                _totalFreeAddressSpace = totalFreeAddressSpace;
                _lastKnownFreeAddressSpace = lastKnownFreeAddressSpace;
                _reservedMem = reservedMem;
                try
                {
                    _stackTrace = Environment.StackTrace;
                }
                catch (System.Security.SecurityException)
                {
                    _stackTrace = "no permission";
                }
                catch (OutOfMemoryException)
                {
                    _stackTrace = "out of memory";
                }
            }

            public override String ToString()
            {
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "MemoryFailPoint detected insufficient memory to guarantee an operation could complete.  Checked for {0} MB, for allocation size of {1} MB.  Need page file? {2}  Need Address Space? {3}  Need Contiguous address space? {4}  Avail page file: {5} MB  Total free VA space: {6} MB  Contiguous free address space (found): {7} MB  Space reserved by process's MemoryFailPoints: {8} MB", _segmentSize >> 20, _allocationSizeInMB, _needPageFile, _needAddressSpace, _needContiguousVASpace, _availPageFile >> 20, _totalFreeAddressSpace >> 20, _lastKnownFreeAddressSpace >> 20, _reservedMem);
            }

            public String StackTrace
            {
                get
                {
                    return _stackTrace;
                }
            }
        }
    }
}