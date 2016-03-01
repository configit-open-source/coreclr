using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.Versioning
{
    internal enum TargetFrameworkId
    {
        NotYetChecked = 0,
        Unrecognized = 1,
        Unspecified = 2,
        NetFramework = 3,
        Portable = 4,
        NetCore = 5,
        Silverlight = 6,
        Phone = 7
    }
}