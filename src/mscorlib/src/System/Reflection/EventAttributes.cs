namespace System.Reflection
{
    using System;

    [Flags]
    public enum EventAttributes
    {
        None = 0x0000,
        SpecialName = 0x0200,
        ReservedMask = 0x0400,
        RTSpecialName = 0x0400
    }
}