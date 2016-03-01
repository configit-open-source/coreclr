namespace System.Globalization
{
    [Flags]
    public enum CultureTypes
    {
        NeutralCultures = 0x0001,
        SpecificCultures = 0x0002,
        InstalledWin32Cultures = 0x0004,
        AllCultures = NeutralCultures | SpecificCultures | InstalledWin32Cultures,
        UserCustomCulture = 0x0008,
        ReplacementCultures = 0x0010,
        WindowsOnlyCultures = 0x0020,
        FrameworkCultures = 0x0040
    }
}