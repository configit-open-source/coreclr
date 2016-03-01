namespace Microsoft.Win32
{
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Diagnostics.Tracing;

    internal static class UnsafeNativeMethods
    {
        internal static extern int GetTimeZoneInformation(out Win32Native.TimeZoneInformation lpTimeZoneInformation);
        internal static extern int GetDynamicTimeZoneInformation(out Win32Native.DynamicTimeZoneInformation lpDynamicTimeZoneInformation);
        internal static extern bool GetFileMUIPath(int flags, [MarshalAs(UnmanagedType.LPWStr)] String filePath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder language, ref int languageLength, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder fileMuiPath, ref int fileMuiPathLength, ref Int64 enumerator);
        internal static extern int LoadString(SafeLibraryHandle handle, int id, [Out] StringBuilder buffer, int bufferLength);
        internal static extern SafeLibraryHandle LoadLibraryEx(string libFilename, IntPtr reserved, int flags);
        internal static extern bool FreeLibrary(IntPtr hModule);
        internal static unsafe class ManifestEtw
        {
            internal const int ERROR_ARITHMETIC_OVERFLOW = 534;
            internal const int ERROR_NOT_ENOUGH_MEMORY = 8;
            internal const int ERROR_MORE_DATA = 0xEA;
            internal const int ERROR_NOT_SUPPORTED = 50;
            internal const int ERROR_INVALID_PARAMETER = 0x57;
            internal const int EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0;
            internal const int EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;
            internal const int EVENT_CONTROL_CODE_CAPTURE_STATE = 2;
            internal unsafe delegate void EtwEnableCallback([In] ref Guid sourceId, [In] int isEnabled, [In] byte level, [In] long matchAnyKeywords, [In] long matchAllKeywords, [In] EVENT_FILTER_DESCRIPTOR*filterData, [In] void *callbackContext);
            internal static extern unsafe uint EventRegister([In] ref Guid providerId, [In] EtwEnableCallback enableCallback, [In] void *callbackContext, [In][Out] ref long registrationHandle);
            internal static extern uint EventUnregister([In] long registrationHandle);
            internal static extern unsafe int EventWrite([In] long registrationHandle, [In] ref EventDescriptor eventDescriptor, [In] int userDataCount, [In] EventProvider.EventData*userData);
            internal static extern unsafe int EventWriteString([In] long registrationHandle, [In] byte level, [In] long keyword, [In] string msg);
            unsafe internal struct EVENT_FILTER_DESCRIPTOR
            {
                public long Ptr;
                public int Size;
                public int Type;
            }

            ;
            internal static int EventWriteTransferWrapper(long registrationHandle, ref EventDescriptor eventDescriptor, Guid*activityId, Guid*relatedActivityId, int userDataCount, EventProvider.EventData*userData)
            {
                int HResult = EventWriteTransfer(registrationHandle, ref eventDescriptor, activityId, relatedActivityId, userDataCount, userData);
                if (HResult == ERROR_INVALID_PARAMETER && relatedActivityId == null)
                {
                    Guid emptyGuid = Guid.Empty;
                    HResult = EventWriteTransfer(registrationHandle, ref eventDescriptor, activityId, &emptyGuid, userDataCount, userData);
                }

                return HResult;
            }

            private static extern int EventWriteTransfer([In] long registrationHandle, [In] ref EventDescriptor eventDescriptor, [In] Guid*activityId, [In] Guid*relatedActivityId, [In] int userDataCount, [In] EventProvider.EventData*userData);
            internal enum ActivityControl : uint
            {
                EVENT_ACTIVITY_CTRL_GET_ID = 1,
                EVENT_ACTIVITY_CTRL_SET_ID = 2,
                EVENT_ACTIVITY_CTRL_CREATE_ID = 3,
                EVENT_ACTIVITY_CTRL_GET_SET_ID = 4,
                EVENT_ACTIVITY_CTRL_CREATE_SET_ID = 5
            }

            ;
            internal static extern int EventActivityIdControl([In] ActivityControl ControlCode, [In][Out] ref Guid ActivityId);
            internal enum EVENT_INFO_CLASS
            {
                BinaryTrackInfo,
                SetEnableAllKeywords,
                SetTraits
            }

            internal static extern int EventSetInformation([In] long registrationHandle, [In] EVENT_INFO_CLASS informationClass, [In] void *eventInformation, [In] int informationLength);
            internal enum TRACE_QUERY_INFO_CLASS
            {
                TraceGuidQueryList,
                TraceGuidQueryInfo,
                TraceGuidQueryProcess,
                TraceStackTracingInfo,
                MaxTraceSetInfoClass
            }

            ;
            internal struct TRACE_GUID_INFO
            {
                public int InstanceCount;
                public int Reserved;
            }

            ;
            internal struct TRACE_PROVIDER_INSTANCE_INFO
            {
                public int NextOffset;
                public int EnableCount;
                public int Pid;
                public int Flags;
            }

            ;
            internal struct TRACE_ENABLE_INFO
            {
                public int IsEnabled;
                public byte Level;
                public byte Reserved1;
                public ushort LoggerId;
                public int EnableProperty;
                public int Reserved2;
                public long MatchAnyKeyword;
                public long MatchAllKeyword;
            }

            ;
            internal static extern int EnumerateTraceGuidsEx(TRACE_QUERY_INFO_CLASS TraceQueryInfoClass, void *InBuffer, int InBufferSize, void *OutBuffer, int OutBufferSize, ref int ReturnLength);
        }

        internal static extern int RoGetActivationFactory([MarshalAs(UnmanagedType.HString)] string activatableClassId, [In] ref Guid iid, [Out, MarshalAs(UnmanagedType.IInspectable)] out Object factory);
    }
}