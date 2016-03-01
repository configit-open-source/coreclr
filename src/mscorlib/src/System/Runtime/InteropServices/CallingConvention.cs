namespace System.Runtime.InteropServices
{
    using System;

    public enum CallingConvention
    {
        Winapi = 1,
        Cdecl = 2,
        StdCall = 3,
        ThisCall = 4,
        FastCall = 5
    }
}