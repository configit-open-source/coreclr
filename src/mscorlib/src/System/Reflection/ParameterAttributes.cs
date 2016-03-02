namespace System.Reflection
{
    [Flags]
    public enum ParameterAttributes
    {
        None = 0x0000,
        In = 0x0001,
        Out = 0x0002,
        Lcid = 0x0004,
        Retval = 0x0008,
        Optional = 0x0010,
        ReservedMask = 0xf000,
        HasDefault = 0x1000,
        HasFieldMarshal = 0x2000,
        Reserved3 = 0x4000,
        Reserved4 = 0x8000
    }
}