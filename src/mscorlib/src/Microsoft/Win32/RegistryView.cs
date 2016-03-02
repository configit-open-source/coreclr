namespace Microsoft.Win32
{
    public enum RegistryView
    {
        Default = 0,
        Registry64 = Win32Native.KEY_WOW64_64KEY,
        Registry32 = Win32Native.KEY_WOW64_32KEY
    }

    ;
}