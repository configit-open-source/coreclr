namespace System.Diagnostics
{
    using System;

    internal enum AssertFilters
    {
        FailDebug = 0,
        FailIgnore = 1,
        FailTerminate = 2,
        FailContinueFilter = 3
    }
}