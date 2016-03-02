namespace System
{
    internal static partial class AppContextDefaultValues
    {
        public static bool TryGetSwitchOverride(string switchName, out bool overrideValue)
        {
            overrideValue = false;
            bool overrideFound = false;
            TryGetSwitchOverridePartial(switchName, ref overrideFound, ref overrideValue);
            return overrideFound;
        }
    }
}