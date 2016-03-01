namespace System.Security
{
    public enum SecurityContextSource
    {
        CurrentAppDomain = 0,
        CurrentAssembly
    }

    internal enum SecurityContextDisableFlow
    {
        Nothing = 0,
        WI = 0x1,
        All = 0x3FFF
    }
}