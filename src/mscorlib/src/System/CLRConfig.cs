using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
    internal class CLRConfig
    {
        internal static extern bool CheckLegacyManagedDeflateStream();
        internal static extern bool CheckThrowUnobservedTaskExceptions();
    }
}