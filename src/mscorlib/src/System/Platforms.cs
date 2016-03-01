namespace System
{
    [Flags]
    public enum Platforms
    {
        Win32S = 1 << PlatformID.Win32S,
        Win32Windows = 1 << PlatformID.Win32Windows,
        Win32NT = 1 << PlatformID.Win32NT,
        WinCE = 1 << PlatformID.WinCE,
        Unix = 1 << PlatformID.Unix,
        Xbox = 1 << PlatformID.Xbox,
        NokiaS60 = 1 << PlatformID.NokiaS60,
        All = Win32S | Win32Windows | Win32NT | WinCE | Unix | Xbox | NokiaS60
    }
}