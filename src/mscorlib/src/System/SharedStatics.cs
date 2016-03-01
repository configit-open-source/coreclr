namespace System
{
    using System.Threading;
    using System.Runtime.Remoting;
    using System.Security;
    using System.Security.Util;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;
    using StringMaker = System.Security.Util.Tokenizer.StringMaker;

    internal sealed class SharedStatics
    {
        private static SharedStatics _sharedStatics;
        private SharedStatics()
        {
            BCLDebug.Assert(false, "SharedStatics..ctor() is never called.");
        }

        private volatile String _Remoting_Identity_IDGuid;
        public static String Remoting_Identity_IDGuid
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_sharedStatics._Remoting_Identity_IDGuid == null)
                {
                    bool tookLock = false;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        Monitor.Enter(_sharedStatics, ref tookLock);
                        if (_sharedStatics._Remoting_Identity_IDGuid == null)
                        {
                            _sharedStatics._Remoting_Identity_IDGuid = Guid.NewGuid().ToString().Replace('-', '_');
                        }
                    }
                    finally
                    {
                        if (tookLock)
                            Monitor.Exit(_sharedStatics);
                    }
                }

                Contract.Assert(_sharedStatics._Remoting_Identity_IDGuid != null, "_sharedStatics._Remoting_Identity_IDGuid != null");
                return _sharedStatics._Remoting_Identity_IDGuid;
            }
        }

        private StringMaker _maker;
        static public StringMaker GetSharedStringMaker()
        {
            StringMaker maker = null;
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(_sharedStatics, ref tookLock);
                if (_sharedStatics._maker != null)
                {
                    maker = _sharedStatics._maker;
                    _sharedStatics._maker = null;
                }
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(_sharedStatics);
            }

            if (maker == null)
            {
                maker = new StringMaker();
            }

            return maker;
        }

        static public void ReleaseSharedStringMaker(ref StringMaker maker)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(_sharedStatics, ref tookLock);
                _sharedStatics._maker = maker;
                maker = null;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(_sharedStatics);
            }
        }

        private int _Remoting_Identity_IDSeqNum;
        internal static int Remoting_Identity_GetNextSeqNum()
        {
            return Interlocked.Increment(ref _sharedStatics._Remoting_Identity_IDSeqNum);
        }

        private long _memFailPointReservedMemory;
        internal static long AddMemoryFailPointReservation(long size)
        {
            return Interlocked.Add(ref _sharedStatics._memFailPointReservedMemory, (long)size);
        }

        internal static ulong MemoryFailPointReservedMemory
        {
            get
            {
                Contract.Assert(Volatile.Read(ref _sharedStatics._memFailPointReservedMemory) >= 0, "Process-wide MemoryFailPoint reserved memory was negative!");
                return (ulong)Volatile.Read(ref _sharedStatics._memFailPointReservedMemory);
            }
        }
    }
}