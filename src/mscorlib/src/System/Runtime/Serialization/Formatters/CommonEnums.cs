namespace System.Runtime.Serialization.Formatters
{
    public enum FormatterTypeStyle
    {
        TypesWhenNeeded = 0,
        TypesAlways = 0x1,
        XsdString = 0x2
    }

    public enum FormatterAssemblyStyle
    {
        Simple = 0,
        Full = 1
    }

    public enum TypeFilterLevel
    {
        Low = 0x2,
        Full = 0x3
    }
}