namespace Microsoft.Win32
{
    public enum RegistryValueKind
    {
        String = Win32Native.REG_SZ,
        ExpandString = Win32Native.REG_EXPAND_SZ,
        Binary = Win32Native.REG_BINARY,
        DWord = Win32Native.REG_DWORD,
        MultiString = Win32Native.REG_MULTI_SZ,
        QWord = Win32Native.REG_QWORD,
        Unknown = 0,
        None = unchecked ((int)0xFFFFFFFF)}
}