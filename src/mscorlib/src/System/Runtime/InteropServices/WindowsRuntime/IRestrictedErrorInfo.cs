using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IRestrictedErrorInfo
    {
        void GetErrorDetails([MarshalAs(UnmanagedType.BStr)] out string description, out int error, [MarshalAs(UnmanagedType.BStr)] out string restrictedDescription, [MarshalAs(UnmanagedType.BStr)] out string capabilitySid);
        void GetReference([MarshalAs(UnmanagedType.BStr)] out string reference);
    }
}