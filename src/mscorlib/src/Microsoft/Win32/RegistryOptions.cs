using System;

namespace Microsoft.Win32
{
    [Flags]
    public enum RegistryOptions
    {
        None = Win32Native.REG_OPTION_NON_VOLATILE,
        Volatile = Win32Native.REG_OPTION_VOLATILE
    }

    ;
}