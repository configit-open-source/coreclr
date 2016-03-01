namespace System.Reflection
{
    using System;

    [Flags]
    public enum PropertyAttributes
    {
        None = 0x0000,
        SpecialName = 0x0200,
        ReservedMask = 0xf400,
        RTSpecialName = 0x0400,
        HasDefault = 0x1000,
        Reserved2 = 0x2000,
        Reserved3 = 0x4000,
        Reserved4 = 0x8000
    }
}