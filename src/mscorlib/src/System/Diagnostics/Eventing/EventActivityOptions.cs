using System;

namespace System.Diagnostics.Tracing
{
    [Flags]
    public enum EventActivityOptions
    {
        None = 0,
        Disable = 0x2,
        Recursive = 0x4,
        Detachable = 0x8
    }
}