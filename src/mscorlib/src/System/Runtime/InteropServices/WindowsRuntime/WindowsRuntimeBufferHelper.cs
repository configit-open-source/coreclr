using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal static class WindowsRuntimeBufferHelper
    {
        private unsafe extern static void StoreOverlappedPtrInCCW(ObjectHandleOnStack windowsRuntimeBuffer, NativeOverlapped*overlapped);
        internal unsafe static void StoreOverlappedInCCW(Object windowsRuntimeBuffer, NativeOverlapped*overlapped)
        {
            StoreOverlappedPtrInCCW(JitHelpers.GetObjectHandleOnStack(ref windowsRuntimeBuffer), overlapped);
        }
    }
}