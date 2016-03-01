using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;

using Microsoft.Win32;

namespace System.Diagnostics.Tracing
{
    internal enum ControllerCommand
    {
        Update = 0,
        SendManifest = -1,
        Enable = -2,
        Disable = -3
    }

    ;
    internal partial class EventProvider : IDisposable
    {
        public struct EventData
        {
            internal unsafe ulong Ptr;
            internal uint Size;
            internal uint Reserved;
        }

        public struct SessionInfo
        {
            internal int sessionIdBit;
            internal int etwSessionId;
            internal SessionInfo(int sessionIdBit_, int etwSessionId_)
            {
                sessionIdBit = sessionIdBit_;
                etwSessionId = etwSessionId_;
            }
        }

        private static bool m_setInformationMissing;
        UnsafeNativeMethods.ManifestEtw.EtwEnableCallback m_etwCallback;
        private long m_regHandle;
        private byte m_level;
        private long m_anyKeywordMask;
        private long m_allKeywordMask;
        private bool m_enabled;
        private Guid m_providerId;
        internal bool m_disposed;
        private static WriteEventErrorCode s_returnCode;
        private const int s_basicTypeAllocationBufferSize = 16;
        private const int s_etwMaxNumberArguments = 32;
        private const int s_etwAPIMaxRefObjCount = 8;
        private const int s_maxEventDataDescriptors = 128;
        private const int s_traceEventMaximumSize = 65482;
        private const int s_traceEventMaximumStringSize = 32724;
        public enum WriteEventErrorCode : int
        {
            NoError = 0,
            NoFreeBuffers = 1,
            EventTooBig = 2,
            NullInput = 3,
            TooManyArgs = 4,
            Other = 5
        }

        ;
        internal EventProvider()
        {
        }

        internal unsafe void Register(Guid providerGuid)
        {
            m_providerId = providerGuid;
            uint status;
            m_etwCallback = new UnsafeNativeMethods.ManifestEtw.EtwEnableCallback(EtwEnableCallBack);
            status = EventRegister(ref m_providerId, m_etwCallback);
            if (status != 0)
            {
                throw new ArgumentException(Win32Native.GetMessage(unchecked ((int)status)));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_disposed)
                return;
            m_enabled = false;
            lock (EventListener.EventListenersLock)
            {
                if (m_disposed)
                    return;
                Deregister();
                m_disposed = true;
            }
        }

        public virtual void Close()
        {
            Dispose();
        }

        ~EventProvider()
        {
            Dispose(false);
        }

        private unsafe void Deregister()
        {
            if (m_regHandle != 0)
            {
                EventUnregister();
                m_regHandle = 0;
            }
        }

        unsafe void EtwEnableCallBack(ref System.Guid sourceId, int controlCode, byte setLevel, long anyKeyword, long allKeyword, UnsafeNativeMethods.ManifestEtw.EVENT_FILTER_DESCRIPTOR*filterData, void *callbackContext)
        {
            try
            {
                ControllerCommand command = ControllerCommand.Update;
                IDictionary<string, string> args = null;
                bool skipFinalOnControllerCommand = false;
                if (controlCode == UnsafeNativeMethods.ManifestEtw.EVENT_CONTROL_CODE_ENABLE_PROVIDER)
                {
                    m_enabled = true;
                    m_level = setLevel;
                    m_anyKeywordMask = anyKeyword;
                    m_allKeywordMask = allKeyword;
                }
                else if (controlCode == UnsafeNativeMethods.ManifestEtw.EVENT_CONTROL_CODE_DISABLE_PROVIDER)
                {
                    m_enabled = false;
                    m_level = 0;
                    m_anyKeywordMask = 0;
                    m_allKeywordMask = 0;
                }
                else if (controlCode == UnsafeNativeMethods.ManifestEtw.EVENT_CONTROL_CODE_CAPTURE_STATE)
                {
                    command = ControllerCommand.SendManifest;
                }
                else
                    return;
                if (!skipFinalOnControllerCommand)
                    OnControllerCommand(command, args, 0, 0);
            }
            catch (Exception)
            {
            }
        }

        protected virtual void OnControllerCommand(ControllerCommand command, IDictionary<string, string> arguments, int sessionId, int etwSessionId)
        {
        }

        protected EventLevel Level
        {
            get
            {
                return (EventLevel)m_level;
            }

            set
            {
                m_level = (byte)value;
            }
        }

        protected EventKeywords MatchAnyKeyword
        {
            get
            {
                return (EventKeywords)m_anyKeywordMask;
            }

            set
            {
                m_anyKeywordMask = unchecked ((long)value);
            }
        }

        protected EventKeywords MatchAllKeyword
        {
            get
            {
                return (EventKeywords)m_allKeywordMask;
            }

            set
            {
                m_allKeywordMask = unchecked ((long)value);
            }
        }

        static private int FindNull(byte[] buffer, int idx)
        {
            while (idx < buffer.Length && buffer[idx] != 0)
                idx++;
            return idx;
        }

        private unsafe bool GetDataFromController(int etwSessionId, UnsafeNativeMethods.ManifestEtw.EVENT_FILTER_DESCRIPTOR*filterData, out ControllerCommand command, out byte[] data, out int dataStart)
        {
            data = null;
            dataStart = 0;
            if (filterData == null)
            {
                string regKey = @"\Microsoft\Windows\CurrentVersion\Winevt\Publishers\{" + m_providerId + "}";
                if (System.Runtime.InteropServices.Marshal.SizeOf(typeof (IntPtr)) == 8)
                    regKey = @"HKEY_LOCAL_MACHINE\Software" + @"\Wow6432Node" + regKey;
                else
                    regKey = @"HKEY_LOCAL_MACHINE\Software" + regKey;
                string valueName = "ControllerData_Session_" + etwSessionId.ToString(CultureInfo.InvariantCulture);
                (new RegistryPermission(RegistryPermissionAccess.Read, regKey)).Assert();
                data = Microsoft.Win32.Registry.GetValue(regKey, valueName, null) as byte[];
                if (data != null)
                {
                    command = ControllerCommand.Update;
                    return true;
                }
            }
            else
            {
                if (filterData->Ptr != 0 && 0 < filterData->Size && filterData->Size <= 1024)
                {
                    data = new byte[filterData->Size];
                    Marshal.Copy((IntPtr)filterData->Ptr, data, 0, data.Length);
                }

                command = (ControllerCommand)filterData->Type;
                return true;
            }

            command = ControllerCommand.Update;
            return false;
        }

        public bool IsEnabled()
        {
            return m_enabled;
        }

        public bool IsEnabled(byte level, long keywords)
        {
            if (!m_enabled)
            {
                return false;
            }

            if ((level <= m_level) || (m_level == 0))
            {
                if ((keywords == 0) || (((keywords & m_anyKeywordMask) != 0) && ((keywords & m_allKeywordMask) == m_allKeywordMask)))
                {
                    return true;
                }
            }

            return false;
        }

        public static WriteEventErrorCode GetLastWriteEventError()
        {
            return s_returnCode;
        }

        private static void SetLastError(int error)
        {
            switch (error)
            {
                case UnsafeNativeMethods.ManifestEtw.ERROR_ARITHMETIC_OVERFLOW:
                case UnsafeNativeMethods.ManifestEtw.ERROR_MORE_DATA:
                    s_returnCode = WriteEventErrorCode.EventTooBig;
                    break;
                case UnsafeNativeMethods.ManifestEtw.ERROR_NOT_ENOUGH_MEMORY:
                    s_returnCode = WriteEventErrorCode.NoFreeBuffers;
                    break;
            }
        }

        private static unsafe object EncodeObject(ref object data, ref EventData*dataDescriptor, ref byte *dataBuffer, ref uint totalEventSize)
        {
            Again:
                dataDescriptor->Reserved = 0;
            string sRet = data as string;
            byte[] blobRet = null;
            if (sRet != null)
            {
                dataDescriptor->Size = ((uint)sRet.Length + 1) * 2;
            }
            else if ((blobRet = data as byte[]) != null)
            {
                *(int *)dataBuffer = blobRet.Length;
                dataDescriptor->Ptr = (ulong)dataBuffer;
                dataDescriptor->Size = 4;
                totalEventSize += dataDescriptor->Size;
                dataDescriptor++;
                dataBuffer += s_basicTypeAllocationBufferSize;
                dataDescriptor->Size = (uint)blobRet.Length;
            }
            else if (data is IntPtr)
            {
                dataDescriptor->Size = (uint)sizeof (IntPtr);
                IntPtr*intptrPtr = (IntPtr*)dataBuffer;
                *intptrPtr = (IntPtr)data;
                dataDescriptor->Ptr = (ulong)intptrPtr;
            }
            else if (data is int)
            {
                dataDescriptor->Size = (uint)sizeof (int);
                int *intptr = (int *)dataBuffer;
                *intptr = (int)data;
                dataDescriptor->Ptr = (ulong)intptr;
            }
            else if (data is long)
            {
                dataDescriptor->Size = (uint)sizeof (long);
                long *longptr = (long *)dataBuffer;
                *longptr = (long)data;
                dataDescriptor->Ptr = (ulong)longptr;
            }
            else if (data is uint)
            {
                dataDescriptor->Size = (uint)sizeof (uint);
                uint *uintptr = (uint *)dataBuffer;
                *uintptr = (uint)data;
                dataDescriptor->Ptr = (ulong)uintptr;
            }
            else if (data is UInt64)
            {
                dataDescriptor->Size = (uint)sizeof (ulong);
                UInt64*ulongptr = (ulong *)dataBuffer;
                *ulongptr = (ulong)data;
                dataDescriptor->Ptr = (ulong)ulongptr;
            }
            else if (data is char)
            {
                dataDescriptor->Size = (uint)sizeof (char);
                char *charptr = (char *)dataBuffer;
                *charptr = (char)data;
                dataDescriptor->Ptr = (ulong)charptr;
            }
            else if (data is byte)
            {
                dataDescriptor->Size = (uint)sizeof (byte);
                byte *byteptr = (byte *)dataBuffer;
                *byteptr = (byte)data;
                dataDescriptor->Ptr = (ulong)byteptr;
            }
            else if (data is short)
            {
                dataDescriptor->Size = (uint)sizeof (short);
                short *shortptr = (short *)dataBuffer;
                *shortptr = (short)data;
                dataDescriptor->Ptr = (ulong)shortptr;
            }
            else if (data is sbyte)
            {
                dataDescriptor->Size = (uint)sizeof (sbyte);
                sbyte *sbyteptr = (sbyte *)dataBuffer;
                *sbyteptr = (sbyte)data;
                dataDescriptor->Ptr = (ulong)sbyteptr;
            }
            else if (data is ushort)
            {
                dataDescriptor->Size = (uint)sizeof (ushort);
                ushort *ushortptr = (ushort *)dataBuffer;
                *ushortptr = (ushort)data;
                dataDescriptor->Ptr = (ulong)ushortptr;
            }
            else if (data is float)
            {
                dataDescriptor->Size = (uint)sizeof (float);
                float *floatptr = (float *)dataBuffer;
                *floatptr = (float)data;
                dataDescriptor->Ptr = (ulong)floatptr;
            }
            else if (data is double)
            {
                dataDescriptor->Size = (uint)sizeof (double);
                double *doubleptr = (double *)dataBuffer;
                *doubleptr = (double)data;
                dataDescriptor->Ptr = (ulong)doubleptr;
            }
            else if (data is bool)
            {
                dataDescriptor->Size = 4;
                int *intptr = (int *)dataBuffer;
                if (((bool)data))
                {
                    *intptr = 1;
                }
                else
                {
                    *intptr = 0;
                }

                dataDescriptor->Ptr = (ulong)intptr;
            }
            else if (data is Guid)
            {
                dataDescriptor->Size = (uint)sizeof (Guid);
                Guid*guidptr = (Guid*)dataBuffer;
                *guidptr = (Guid)data;
                dataDescriptor->Ptr = (ulong)guidptr;
            }
            else if (data is decimal)
            {
                dataDescriptor->Size = (uint)sizeof (decimal);
                decimal *decimalptr = (decimal *)dataBuffer;
                *decimalptr = (decimal)data;
                dataDescriptor->Ptr = (ulong)decimalptr;
            }
            else if (data is DateTime)
            {
                const long UTCMinTicks = 504911232000000000;
                long dateTimeTicks = 0;
                if (((DateTime)data).Ticks > UTCMinTicks)
                    dateTimeTicks = ((DateTime)data).ToFileTimeUtc();
                dataDescriptor->Size = (uint)sizeof (long);
                long *longptr = (long *)dataBuffer;
                *longptr = dateTimeTicks;
                dataDescriptor->Ptr = (ulong)longptr;
            }
            else
            {
                if (data is System.Enum)
                {
                    Type underlyingType = Enum.GetUnderlyingType(data.GetType());
                    if (underlyingType == typeof (int))
                    {
                        data = ((IConvertible)data).ToInt32(null);
                        goto Again;
                    }
                    else if (underlyingType == typeof (long))
                    {
                        data = ((IConvertible)data).ToInt64(null);
                        goto Again;
                    }
                }

                if (data == null)
                    sRet = "";
                else
                    sRet = data.ToString();
                dataDescriptor->Size = ((uint)sRet.Length + 1) * 2;
            }

            totalEventSize += dataDescriptor->Size;
            dataDescriptor++;
            dataBuffer += s_basicTypeAllocationBufferSize;
            return (object)sRet ?? (object)blobRet;
        }

        internal unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, Guid*activityID, Guid*childActivityID, params object[] eventPayload)
        {
            int status = 0;
            if (IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
            {
                int argCount = 0;
                unsafe
                {
                    argCount = eventPayload.Length;
                    if (argCount > s_etwMaxNumberArguments)
                    {
                        s_returnCode = WriteEventErrorCode.TooManyArgs;
                        return false;
                    }

                    uint totalEventSize = 0;
                    int index;
                    int refObjIndex = 0;
                    List<int> refObjPosition = new List<int>(s_etwAPIMaxRefObjCount);
                    List<object> dataRefObj = new List<object>(s_etwAPIMaxRefObjCount);
                    EventData*userData = stackalloc EventData[2 * argCount];
                    EventData*userDataPtr = (EventData*)userData;
                    byte *dataBuffer = stackalloc byte[s_basicTypeAllocationBufferSize * 2 * argCount];
                    byte *currentBuffer = dataBuffer;
                    bool hasNonStringRefArgs = false;
                    for (index = 0; index < eventPayload.Length; index++)
                    {
                        if (eventPayload[index] != null)
                        {
                            object supportedRefObj;
                            supportedRefObj = EncodeObject(ref eventPayload[index], ref userDataPtr, ref currentBuffer, ref totalEventSize);
                            if (supportedRefObj != null)
                            {
                                int idx = (int)(userDataPtr - userData - 1);
                                if (!(supportedRefObj is string))
                                {
                                    if (eventPayload.Length + idx + 1 - index > s_etwMaxNumberArguments)
                                    {
                                        s_returnCode = WriteEventErrorCode.TooManyArgs;
                                        return false;
                                    }

                                    hasNonStringRefArgs = true;
                                }

                                dataRefObj.Add(supportedRefObj);
                                refObjPosition.Add(idx);
                                refObjIndex++;
                            }
                        }
                        else
                        {
                            s_returnCode = WriteEventErrorCode.NullInput;
                            return false;
                        }
                    }

                    argCount = (int)(userDataPtr - userData);
                    if (totalEventSize > s_traceEventMaximumSize)
                    {
                        s_returnCode = WriteEventErrorCode.EventTooBig;
                        return false;
                    }

                    if (!hasNonStringRefArgs && (refObjIndex < s_etwAPIMaxRefObjCount))
                    {
                        while (refObjIndex < s_etwAPIMaxRefObjCount)
                        {
                            dataRefObj.Add(null);
                            ++refObjIndex;
                        }

                        fixed (char *v0 = (string)dataRefObj[0], v1 = (string)dataRefObj[1], v2 = (string)dataRefObj[2], v3 = (string)dataRefObj[3], v4 = (string)dataRefObj[4], v5 = (string)dataRefObj[5], v6 = (string)dataRefObj[6], v7 = (string)dataRefObj[7])
                        {
                            userDataPtr = (EventData*)userData;
                            if (dataRefObj[0] != null)
                            {
                                userDataPtr[refObjPosition[0]].Ptr = (ulong)v0;
                            }

                            if (dataRefObj[1] != null)
                            {
                                userDataPtr[refObjPosition[1]].Ptr = (ulong)v1;
                            }

                            if (dataRefObj[2] != null)
                            {
                                userDataPtr[refObjPosition[2]].Ptr = (ulong)v2;
                            }

                            if (dataRefObj[3] != null)
                            {
                                userDataPtr[refObjPosition[3]].Ptr = (ulong)v3;
                            }

                            if (dataRefObj[4] != null)
                            {
                                userDataPtr[refObjPosition[4]].Ptr = (ulong)v4;
                            }

                            if (dataRefObj[5] != null)
                            {
                                userDataPtr[refObjPosition[5]].Ptr = (ulong)v5;
                            }

                            if (dataRefObj[6] != null)
                            {
                                userDataPtr[refObjPosition[6]].Ptr = (ulong)v6;
                            }

                            if (dataRefObj[7] != null)
                            {
                                userDataPtr[refObjPosition[7]].Ptr = (ulong)v7;
                            }

                            status = UnsafeNativeMethods.ManifestEtw.EventWriteTransferWrapper(m_regHandle, ref eventDescriptor, activityID, childActivityID, argCount, userData);
                        }
                    }
                    else
                    {
                        userDataPtr = (EventData*)userData;
                        GCHandle[] rgGCHandle = new GCHandle[refObjIndex];
                        for (int i = 0; i < refObjIndex; ++i)
                        {
                            rgGCHandle[i] = GCHandle.Alloc(dataRefObj[i], GCHandleType.Pinned);
                            if (dataRefObj[i] is string)
                            {
                                fixed (char *p = (string)dataRefObj[i])
                                    userDataPtr[refObjPosition[i]].Ptr = (ulong)p;
                            }
                            else
                            {
                                fixed (byte *p = (byte[])dataRefObj[i])
                                    userDataPtr[refObjPosition[i]].Ptr = (ulong)p;
                            }
                        }

                        status = UnsafeNativeMethods.ManifestEtw.EventWriteTransferWrapper(m_regHandle, ref eventDescriptor, activityID, childActivityID, argCount, userData);
                        for (int i = 0; i < refObjIndex; ++i)
                        {
                            rgGCHandle[i].Free();
                        }
                    }
                }
            }

            if (status != 0)
            {
                SetLastError((int)status);
                return false;
            }

            return true;
        }

        internal unsafe protected bool WriteEvent(ref EventDescriptor eventDescriptor, Guid*activityID, Guid*childActivityID, int dataCount, IntPtr data)
        {
            if (childActivityID != null)
            {
                Contract.Assert((EventOpcode)eventDescriptor.Opcode == EventOpcode.Send || (EventOpcode)eventDescriptor.Opcode == EventOpcode.Receive || (EventOpcode)eventDescriptor.Opcode == EventOpcode.Start || (EventOpcode)eventDescriptor.Opcode == EventOpcode.Stop);
            }

            int status = UnsafeNativeMethods.ManifestEtw.EventWriteTransferWrapper(m_regHandle, ref eventDescriptor, activityID, childActivityID, dataCount, (EventData*)data);
            if (status != 0)
            {
                SetLastError(status);
                return false;
            }

            return true;
        }

        internal unsafe bool WriteEventRaw(ref EventDescriptor eventDescriptor, Guid*activityID, Guid*relatedActivityID, int dataCount, IntPtr data)
        {
            int status = UnsafeNativeMethods.ManifestEtw.EventWriteTransferWrapper(m_regHandle, ref eventDescriptor, activityID, relatedActivityID, dataCount, (EventData*)data);
            if (status != 0)
            {
                SetLastError(status);
                return false;
            }

            return true;
        }

        private unsafe uint EventRegister(ref Guid providerId, UnsafeNativeMethods.ManifestEtw.EtwEnableCallback enableCallback)
        {
            m_providerId = providerId;
            m_etwCallback = enableCallback;
            return UnsafeNativeMethods.ManifestEtw.EventRegister(ref providerId, enableCallback, null, ref m_regHandle);
        }

        private uint EventUnregister()
        {
            uint status = UnsafeNativeMethods.ManifestEtw.EventUnregister(m_regHandle);
            m_regHandle = 0;
            return status;
        }

        static int[] nibblebits = {0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4};
        private static int bitcount(uint n)
        {
            int count = 0;
            for (; n != 0; n = n >> 4)
                count += nibblebits[n & 0x0f];
            return count;
        }

        private static int bitindex(uint n)
        {
            Contract.Assert(bitcount(n) == 1);
            int idx = 0;
            while ((n & (1 << idx)) == 0)
                idx++;
            return idx;
        }
    }
}