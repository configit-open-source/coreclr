using System;

namespace System.Reflection.Emit
{
    [Flags]
    public enum AssemblyBuilderAccess
    {
        Run = 1,
        RunAndCollect = 8 | Run
    }
}