namespace System.Runtime.ConstrainedExecution
{
    using System.Runtime.InteropServices;
    using System;

    public enum Consistency : int
    {
        MayCorruptProcess = 0,
        MayCorruptAppDomain = 1,
        MayCorruptInstance = 2,
        WillNotCorruptState = 3
    }

    public enum Cer : int
    {
        None = 0,
        MayFail = 1,
        Success = 2
    }

    public sealed class ReliabilityContractAttribute : Attribute
    {
        private Consistency _consistency;
        private Cer _cer;
        public ReliabilityContractAttribute(Consistency consistencyGuarantee, Cer cer)
        {
            _consistency = consistencyGuarantee;
            _cer = cer;
        }

        public Consistency ConsistencyGuarantee
        {
            get
            {
                return _consistency;
            }
        }

        public Cer Cer
        {
            get
            {
                return _cer;
            }
        }
    }
}