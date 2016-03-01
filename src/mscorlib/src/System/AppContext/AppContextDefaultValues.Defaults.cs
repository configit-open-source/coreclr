using System;

namespace System
{
    internal static partial class AppContextDefaultValues
    {
        internal static readonly string SwitchNoAsyncCurrentCulture = "Switch.System.Globalization.NoAsyncCurrentCulture";
        internal static readonly string SwitchThrowExceptionIfDisposedCancellationTokenSource = "Switch.System.Threading.ThrowExceptionIfDisposedCancellationTokenSource";
        internal static readonly string SwitchPreserveEventListnerObjectIdentity = "Switch.System.Diagnostics.EventSource.PreserveEventListnerObjectIdentity";
        static partial void PopulateOverrideValuesPartial();
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int version)
        {
            switch (platformIdentifier)
            {
                case ".NETCore":
                case ".NETFramework":
                {
                    if (version <= 40502)
                    {
                        AppContext.DefineSwitchDefault(SwitchNoAsyncCurrentCulture, true);
                        AppContext.DefineSwitchDefault(SwitchThrowExceptionIfDisposedCancellationTokenSource, true);
                    }

                    break;
                }

                case "WindowsPhone":
                case "WindowsPhoneApp":
                {
                    if (version <= 80100)
                    {
                        AppContext.DefineSwitchDefault(SwitchNoAsyncCurrentCulture, true);
                        AppContext.DefineSwitchDefault(SwitchThrowExceptionIfDisposedCancellationTokenSource, true);
                    }

                    break;
                }
            }

            PopulateOverrideValuesPartial();
        }
    }
}