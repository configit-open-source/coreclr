namespace Microsoft.Win32
{
    using System;

    [Flags]
    public enum RegistryOptions
    {
        None = Win32Native.REG_OPTION_NON_VOLATILE,
        Volatile = Win32Native.REG_OPTION_VOLATILE
    }

    ;
}