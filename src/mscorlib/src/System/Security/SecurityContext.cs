namespace System.Security
{
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Threading;
    using System.Runtime.Remoting;
    using System.Security.Principal;
    using System.Collections;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    public enum SecurityContextSource
    {
        CurrentAppDomain = 0,
        CurrentAssembly
    }

    internal enum SecurityContextDisableFlow
    {
        Nothing = 0,
        WI = 0x1,
        All = 0x3FFF
    }
}