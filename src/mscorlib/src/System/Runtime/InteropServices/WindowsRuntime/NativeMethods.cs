using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal unsafe struct HSTRING_HEADER
    {
    }

    internal static class UnsafeNativeMethods
    {
        internal static extern IRestrictedErrorInfo GetRestrictedErrorInfo();
        internal static extern bool RoOriginateLanguageException(int error, [MarshalAs(UnmanagedType.HString)] string message, IntPtr languageException);
        internal static extern void RoReportUnhandledError(IRestrictedErrorInfo error);
        internal static unsafe extern int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, [Out] IntPtr*hstring);
        internal static unsafe extern int WindowsCreateStringReference(char *sourceString, int length, [Out] HSTRING_HEADER*hstringHeader, [Out] IntPtr*hstring);
        internal static extern int WindowsDeleteString(IntPtr hstring);
        internal static unsafe extern char *WindowsGetStringRawBuffer(IntPtr hstring, [Out] uint *length);
    }
}