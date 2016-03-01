namespace System.Security
{
    using System;
    using System.Runtime.InteropServices;

    public enum SecurityZone
    {
        MyComputer = 0,
        Intranet = 1,
        Trusted = 2,
        Internet = 3,
        Untrusted = 4,
        NoZone = -1
    }
}