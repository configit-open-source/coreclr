namespace System.Globalization
{
    [Flags]
    public enum DateTimeStyles
    {
        None = 0x00000000,
        AllowLeadingWhite = 0x00000001,
        AllowTrailingWhite = 0x00000002,
        AllowInnerWhite = 0x00000004,
        AllowWhiteSpaces = AllowLeadingWhite | AllowInnerWhite | AllowTrailingWhite,
        NoCurrentDateDefault = 0x00000008,
        AdjustToUniversal = 0x00000010,
        AssumeLocal = 0x00000020,
        AssumeUniversal = 0x00000040,
        RoundtripKind = 0x00000080
    }
}