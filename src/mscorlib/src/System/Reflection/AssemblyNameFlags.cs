namespace System.Reflection
{
    using System;

    public enum AssemblyNameFlags
    {
        None = 0x0000,
        PublicKey = 0x0001,
        EnableJITcompileOptimizer = 0x4000,
        EnableJITcompileTracking = 0x8000,
        Retargetable = 0x0100
    }

    public enum AssemblyContentType
    {
        Default = 0x0000,
        WindowsRuntime = 0x0001
    }

    public enum ProcessorArchitecture
    {
        None = 0x0000,
        MSIL = 0x0001,
        X86 = 0x0002,
        IA64 = 0x0003,
        Amd64 = 0x0004,
        Arm = 0x0005
    }
}