using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Security.Permissions;
using System.Text;
using System.Threading;

using Microsoft.Reflection;
using Microsoft.Win32;

namespace System.Diagnostics.Tracing
{
    public partial class EventSource : IDisposable
    {
        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public Guid Guid
        {
            get
            {
                return m_guid;
            }
        }

        public bool IsEnabled()
        {
            return m_eventSourceEnabled;
        }

        public bool IsEnabled(EventLevel level, EventKeywords keywords)
        {
            return IsEnabled(level, keywords, EventChannel.None);
        }

        public bool IsEnabled(EventLevel level, EventKeywords keywords, EventChannel channel)
        {
            if (!m_eventSourceEnabled)
                return false;
            if (!IsEnabledCommon(m_eventSourceEnabled, m_level, m_matchAnyKeyword, level, keywords, channel))
                return false;
            return true;
        }

        public EventSourceSettings Settings
        {
            get
            {
                return m_config;
            }
        }

        public static Guid GetGuid(Type eventSourceType)
        {
            if (eventSourceType == null)
                throw new ArgumentNullException("eventSourceType");
            Contract.EndContractBlock();
            EventSourceAttribute attrib = (EventSourceAttribute)GetCustomAttributeHelper(eventSourceType, typeof (EventSourceAttribute));
            string name = eventSourceType.Name;
            if (attrib != null)
            {
                if (attrib.Guid != null)
                {
                    Guid g = Guid.Empty;
                    if (Guid.TryParse(attrib.Guid, out g))
                        return g;
                }

                if (attrib.Name != null)
                    name = attrib.Name;
            }

            if (name == null)
            {
                throw new ArgumentException(Resources.GetResourceString("Argument_InvalidTypeName"), "eventSourceType");
            }

            return GenerateGuidFromName(name.ToUpperInvariant());
        }

        public static string GetName(Type eventSourceType)
        {
            return GetName(eventSourceType, EventManifestOptions.None);
        }

        public static string GenerateManifest(Type eventSourceType, string assemblyPathToIncludeInManifest)
        {
            return GenerateManifest(eventSourceType, assemblyPathToIncludeInManifest, EventManifestOptions.None);
        }

        public static string GenerateManifest(Type eventSourceType, string assemblyPathToIncludeInManifest, EventManifestOptions flags)
        {
            if (eventSourceType == null)
                throw new ArgumentNullException("eventSourceType");
            Contract.EndContractBlock();
            byte[] manifestBytes = EventSource.CreateManifestAndDescriptors(eventSourceType, assemblyPathToIncludeInManifest, null, flags);
            return (manifestBytes == null) ? null : Encoding.UTF8.GetString(manifestBytes, 0, manifestBytes.Length);
        }

        public static IEnumerable<EventSource> GetSources()
        {
            var ret = new List<EventSource>();
            lock (EventListener.EventListenersLock)
            {
                foreach (WeakReference eventSourceRef in EventListener.s_EventSources)
                {
                    EventSource eventSource = eventSourceRef.Target as EventSource;
                    if (eventSource != null && !eventSource.IsDisposed)
                        ret.Add(eventSource);
                }
            }

            return ret;
        }

        public static void SendCommand(EventSource eventSource, EventCommand command, IDictionary<string, string> commandArguments)
        {
            if (eventSource == null)
                throw new ArgumentNullException("eventSource");
            if ((int)command <= (int)EventCommand.Update && (int)command != (int)EventCommand.SendManifest)
            {
                throw new ArgumentException(Resources.GetResourceString("EventSource_InvalidCommand"), "command");
            }

            eventSource.SendCommand(null, 0, 0, command, true, EventLevel.LogAlways, EventKeywords.None, commandArguments);
        }

        internal static Guid InternalCurrentThreadActivityId
        {
            [System.Security.SecurityCritical]
            get
            {
                Guid retval = CurrentThreadActivityId;
                if (retval == Guid.Empty)
                {
                    retval = FallbackActivityId;
                }

                return retval;
            }
        }

        internal static Guid FallbackActivityId
        {
            [System.Security.SecurityCritical]
            get
            {
                int threadID = AppDomain.GetCurrentThreadId();
                return new Guid(unchecked ((uint)threadID), unchecked ((ushort)s_currentPid), unchecked ((ushort)(s_currentPid >> 16)), 0x94, 0x1b, 0x87, 0xd5, 0xa6, 0x5c, 0x36, 0x64);
            }
        }

        public Exception ConstructionException
        {
            get
            {
                return m_constructionException;
            }
        }

        public string GetTrait(string key)
        {
            if (m_traits != null)
            {
                for (int i = 0; i < m_traits.Length - 1; i += 2)
                {
                    if (m_traits[i] == key)
                        return m_traits[i + 1];
                }
            }

            return null;
        }

        public override string ToString()
        {
            return Resources.GetResourceString("EventSource_ToString", Name, Guid);
        }

        public event EventHandler<EventCommandEventArgs> EventCommandExecuted
        {
            add
            {
                m_eventCommandExecuted += value;
                EventCommandEventArgs deferredCommands = m_deferredCommands;
                while (deferredCommands != null)
                {
                    value(this, deferredCommands);
                    deferredCommands = deferredCommands.nextCommand;
                }
            }

            remove
            {
                m_eventCommandExecuted -= value;
            }
        }

        protected EventSource(): this (EventSourceSettings.EtwManifestEventFormat)
        {
        }

        protected EventSource(bool throwOnEventWriteErrors): this (EventSourceSettings.EtwManifestEventFormat | (throwOnEventWriteErrors ? EventSourceSettings.ThrowOnEventWriteErrors : 0))
        {
        }

        protected EventSource(EventSourceSettings settings): this (settings, null)
        {
        }

        protected EventSource(EventSourceSettings settings, params string[] traits)
        {
            m_config = ValidateSettings(settings);
            Guid eventSourceGuid;
            string eventSourceName;
            EventMetadata[] eventDescriptors;
            byte[] manifest;
            GetMetadata(out eventSourceGuid, out eventSourceName, out eventDescriptors, out manifest);
            if (eventSourceGuid.Equals(Guid.Empty) || eventSourceName == null)
            {
                var myType = this.GetType();
                eventSourceGuid = GetGuid(myType);
                eventSourceName = GetName(myType);
            }

            Initialize(eventSourceGuid, eventSourceName, traits);
        }

        internal virtual void GetMetadata(out Guid eventSourceGuid, out string eventSourceName, out EventMetadata[] eventData, out byte[] manifestBytes)
        {
            eventSourceGuid = Guid.Empty;
            eventSourceName = null;
            eventData = null;
            manifestBytes = null;
            return;
        }

        protected virtual void OnEventCommand(EventCommandEventArgs command)
        {
        }

        protected unsafe void WriteEvent(int eventId)
        {
            WriteEventCore(eventId, 0, null);
        }

        protected unsafe void WriteEvent(int eventId, int arg1)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[1];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                WriteEventCore(eventId, 1, descrs);
            }
        }

        protected unsafe void WriteEvent(int eventId, int arg1, int arg2)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[2];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                WriteEventCore(eventId, 2, descrs);
            }
        }

        protected unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 4;
                WriteEventCore(eventId, 3, descrs);
            }
        }

        protected unsafe void WriteEvent(int eventId, long arg1)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[1];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                WriteEventCore(eventId, 1, descrs);
            }
        }

        protected unsafe void WriteEvent(int eventId, long arg1, long arg2)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[2];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 8;
                WriteEventCore(eventId, 2, descrs);
            }
        }

        protected unsafe void WriteEvent(int eventId, long arg1, long arg2, long arg3)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 8;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 8;
                WriteEventCore(eventId, 3, descrs);
            }
        }

        protected unsafe void WriteEvent(int eventId, string arg1)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null)
                    arg1 = "";
                fixed (char *string1Bytes = arg1)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[1];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    WriteEventCore(eventId, 1, descrs);
                }
            }
        }

        protected unsafe void WriteEvent(int eventId, string arg1, string arg2)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null)
                    arg1 = "";
                if (arg2 == null)
                    arg2 = "";
                fixed (char *string1Bytes = arg1)
                    fixed (char *string2Bytes = arg2)
                    {
                        EventSource.EventData*descrs = stackalloc EventSource.EventData[2];
                        descrs[0].DataPointer = (IntPtr)string1Bytes;
                        descrs[0].Size = ((arg1.Length + 1) * 2);
                        descrs[1].DataPointer = (IntPtr)string2Bytes;
                        descrs[1].Size = ((arg2.Length + 1) * 2);
                        WriteEventCore(eventId, 2, descrs);
                    }
            }
        }

        protected unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null)
                    arg1 = "";
                if (arg2 == null)
                    arg2 = "";
                if (arg3 == null)
                    arg3 = "";
                fixed (char *string1Bytes = arg1)
                    fixed (char *string2Bytes = arg2)
                        fixed (char *string3Bytes = arg3)
                        {
                            EventSource.EventData*descrs = stackalloc EventSource.EventData[3];
                            descrs[0].DataPointer = (IntPtr)string1Bytes;
                            descrs[0].Size = ((arg1.Length + 1) * 2);
                            descrs[1].DataPointer = (IntPtr)string2Bytes;
                            descrs[1].Size = ((arg2.Length + 1) * 2);
                            descrs[2].DataPointer = (IntPtr)string3Bytes;
                            descrs[2].Size = ((arg3.Length + 1) * 2);
                            WriteEventCore(eventId, 3, descrs);
                        }
            }
        }

        protected unsafe void WriteEvent(int eventId, string arg1, int arg2)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null)
                    arg1 = "";
                fixed (char *string1Bytes = arg1)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[2];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        protected unsafe void WriteEvent(int eventId, string arg1, int arg2, int arg3)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null)
                    arg1 = "";
                fixed (char *string1Bytes = arg1)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[3];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = 4;
                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }

        protected unsafe void WriteEvent(int eventId, string arg1, long arg2)
        {
            if (m_eventSourceEnabled)
            {
                if (arg1 == null)
                    arg1 = "";
                fixed (char *string1Bytes = arg1)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[2];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 8;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        protected unsafe void WriteEvent(int eventId, long arg1, string arg2)
        {
            if (m_eventSourceEnabled)
            {
                if (arg2 == null)
                    arg2 = "";
                fixed (char *string2Bytes = arg2)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[2];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        protected unsafe void WriteEvent(int eventId, int arg1, string arg2)
        {
            if (m_eventSourceEnabled)
            {
                if (arg2 == null)
                    arg2 = "";
                fixed (char *string2Bytes = arg2)
                {
                    EventSource.EventData*descrs = stackalloc EventSource.EventData[2];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 4;
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        protected unsafe void WriteEvent(int eventId, byte[] arg1)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[2];
                if (arg1 == null || arg1.Length == 0)
                {
                    int blobSize = 0;
                    descrs[0].DataPointer = (IntPtr)(&blobSize);
                    descrs[0].Size = 4;
                    descrs[1].DataPointer = (IntPtr)(&blobSize);
                    descrs[1].Size = 0;
                    WriteEventCore(eventId, 2, descrs);
                }
                else
                {
                    int blobSize = arg1.Length;
                    fixed (byte *blob = &arg1[0])
                    {
                        descrs[0].DataPointer = (IntPtr)(&blobSize);
                        descrs[0].Size = 4;
                        descrs[1].DataPointer = (IntPtr)blob;
                        descrs[1].Size = blobSize;
                        WriteEventCore(eventId, 2, descrs);
                    }
                }
            }
        }

        protected unsafe void WriteEvent(int eventId, long arg1, byte[] arg2)
        {
            if (m_eventSourceEnabled)
            {
                EventSource.EventData*descrs = stackalloc EventSource.EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                if (arg2 == null || arg2.Length == 0)
                {
                    int blobSize = 0;
                    descrs[1].DataPointer = (IntPtr)(&blobSize);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)(&blobSize);
                    descrs[2].Size = 0;
                    WriteEventCore(eventId, 3, descrs);
                }
                else
                {
                    int blobSize = arg2.Length;
                    fixed (byte *blob = &arg2[0])
                    {
                        descrs[1].DataPointer = (IntPtr)(&blobSize);
                        descrs[1].Size = 4;
                        descrs[2].DataPointer = (IntPtr)blob;
                        descrs[2].Size = blobSize;
                        WriteEventCore(eventId, 3, descrs);
                    }
                }
            }
        }

        protected internal struct EventData
        {
            public IntPtr DataPointer
            {
                get
                {
                    return (IntPtr)m_Ptr;
                }

                set
                {
                    m_Ptr = unchecked ((long)value);
                }
            }

            public int Size
            {
                get
                {
                    return m_Size;
                }

                set
                {
                    m_Size = value;
                }
            }

            internal unsafe void SetMetadata(byte *pointer, int size, int reserved)
            {
                this.m_Ptr = (long)(ulong)(UIntPtr)pointer;
                this.m_Size = size;
                this.m_Reserved = reserved;
            }

            internal long m_Ptr;
            internal int m_Size;
            internal int m_Reserved;
        }

        protected unsafe void WriteEventCore(int eventId, int eventDataCount, EventSource.EventData*data)
        {
            WriteEventWithRelatedActivityIdCore(eventId, null, eventDataCount, data);
        }

        protected unsafe void WriteEventWithRelatedActivityIdCore(int eventId, Guid*relatedActivityId, int eventDataCount, EventSource.EventData*data)
        {
            if (m_eventSourceEnabled)
            {
                try
                {
                    Contract.Assert(m_eventData != null);
                    if (relatedActivityId != null)
                        ValidateEventOpcodeForTransfer(ref m_eventData[eventId], m_eventData[eventId].Name);
                    if (m_eventData[eventId].EnabledForETW)
                    {
                        EventOpcode opcode = (EventOpcode)m_eventData[eventId].Descriptor.Opcode;
                        EventActivityOptions activityOptions = m_eventData[eventId].ActivityOptions;
                        Guid*pActivityId = null;
                        Guid activityId = Guid.Empty;
                        Guid relActivityId = Guid.Empty;
                        if (opcode != EventOpcode.Info && relatedActivityId == null && ((activityOptions & EventActivityOptions.Disable) == 0))
                        {
                            if (opcode == EventOpcode.Start)
                            {
                                m_activityTracker.OnStart(m_name, m_eventData[eventId].Name, m_eventData[eventId].Descriptor.Task, ref activityId, ref relActivityId, m_eventData[eventId].ActivityOptions);
                            }
                            else if (opcode == EventOpcode.Stop)
                            {
                                m_activityTracker.OnStop(m_name, m_eventData[eventId].Name, m_eventData[eventId].Descriptor.Task, ref activityId);
                            }

                            if (activityId != Guid.Empty)
                                pActivityId = &activityId;
                            if (relActivityId != Guid.Empty)
                                relatedActivityId = &relActivityId;
                        }

                        SessionMask etwSessions = SessionMask.All;
                        if ((ulong)m_curLiveSessions != 0)
                            etwSessions = GetEtwSessionMask(eventId, relatedActivityId);
                        if ((ulong)etwSessions != 0 || m_legacySessions != null && m_legacySessions.Count > 0)
                        {
                            if (!SelfDescribingEvents)
                            {
                                if (etwSessions.IsEqualOrSupersetOf(m_curLiveSessions))
                                {
                                    if (!m_provider.WriteEvent(ref m_eventData[eventId].Descriptor, pActivityId, relatedActivityId, eventDataCount, (IntPtr)data))
                                        ThrowEventSourceException(m_eventData[eventId].Name);
                                }
                                else
                                {
                                    long origKwd = unchecked ((long)((ulong)m_eventData[eventId].Descriptor.Keywords & ~(SessionMask.All.ToEventKeywords())));
                                    var desc = new EventDescriptor(m_eventData[eventId].Descriptor.EventId, m_eventData[eventId].Descriptor.Version, m_eventData[eventId].Descriptor.Channel, m_eventData[eventId].Descriptor.Level, m_eventData[eventId].Descriptor.Opcode, m_eventData[eventId].Descriptor.Task, unchecked ((long)etwSessions.ToEventKeywords() | origKwd));
                                    if (!m_provider.WriteEvent(ref desc, pActivityId, relatedActivityId, eventDataCount, (IntPtr)data))
                                        ThrowEventSourceException(m_eventData[eventId].Name);
                                }
                            }
                            else
                            {
                                TraceLoggingEventTypes tlet = m_eventData[eventId].TraceLoggingEventTypes;
                                if (tlet == null)
                                {
                                    tlet = new TraceLoggingEventTypes(m_eventData[eventId].Name, EventTags.None, m_eventData[eventId].Parameters);
                                    Interlocked.CompareExchange(ref m_eventData[eventId].TraceLoggingEventTypes, tlet, null);
                                }

                                long origKwd = unchecked ((long)((ulong)m_eventData[eventId].Descriptor.Keywords & ~(SessionMask.All.ToEventKeywords())));
                                EventSourceOptions opt = new EventSourceOptions{Keywords = (EventKeywords)unchecked ((long)etwSessions.ToEventKeywords() | origKwd), Level = (EventLevel)m_eventData[eventId].Descriptor.Level, Opcode = (EventOpcode)m_eventData[eventId].Descriptor.Opcode};
                                WriteMultiMerge(m_eventData[eventId].Name, ref opt, tlet, pActivityId, relatedActivityId, data);
                            }
                        }
                    }

                    if (m_Dispatchers != null && m_eventData[eventId].EnabledForAnyListener)
                        WriteToAllListeners(eventId, relatedActivityId, eventDataCount, data);
                }
                catch (Exception ex)
                {
                    if (ex is EventSourceException)
                        throw;
                    else
                        ThrowEventSourceException(m_eventData[eventId].Name, ex);
                }
            }
        }

        protected unsafe void WriteEvent(int eventId, params object[] args)
        {
            WriteEventVarargs(eventId, null, args);
        }

        protected unsafe void WriteEventWithRelatedActivityId(int eventId, Guid relatedActivityId, params object[] args)
        {
            WriteEventVarargs(eventId, &relatedActivityId, args);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_eventSourceEnabled)
                {
                    try
                    {
                        SendManifest(m_rawManifest);
                    }
                    catch (Exception)
                    {
                    }

                    m_eventSourceEnabled = false;
                }

                if (m_provider != null)
                {
                    m_provider.Dispose();
                    m_provider = null;
                }
            }

            m_eventSourceEnabled = false;
            m_eventSourceDisposed = true;
        }

        ~EventSource()
        {
            this.Dispose(false);
        }

        internal void WriteStringToListener(EventListener listener, string msg, SessionMask m)
        {
            Contract.Assert(listener == null || (uint)m == (uint)SessionMask.FromId(0));
            if (m_eventSourceEnabled)
            {
                if (listener == null)
                {
                    WriteEventString(0, unchecked ((long)m.ToEventKeywords()), msg);
                }
                else
                {
                    List<object> arg = new List<object>();
                    arg.Add(msg);
                    EventWrittenEventArgs eventCallbackArgs = new EventWrittenEventArgs(this);
                    eventCallbackArgs.EventId = 0;
                    eventCallbackArgs.Payload = new ReadOnlyCollection<object>(arg);
                    listener.OnEventWritten(eventCallbackArgs);
                }
            }
        }

        private unsafe void WriteEventRaw(string eventName, ref EventDescriptor eventDescriptor, Guid*activityID, Guid*relatedActivityID, int dataCount, IntPtr data)
        {
            if (m_provider == null)
            {
                ThrowEventSourceException(eventName);
            }
            else
            {
                if (!m_provider.WriteEventRaw(ref eventDescriptor, activityID, relatedActivityID, dataCount, data))
                    ThrowEventSourceException(eventName);
            }
        }

        internal EventSource(Guid eventSourceGuid, string eventSourceName): this (eventSourceGuid, eventSourceName, EventSourceSettings.EtwManifestEventFormat)
        {
        }

        internal EventSource(Guid eventSourceGuid, string eventSourceName, EventSourceSettings settings, string[] traits = null)
        {
            m_config = ValidateSettings(settings);
            Initialize(eventSourceGuid, eventSourceName, traits);
        }

        private unsafe void Initialize(Guid eventSourceGuid, string eventSourceName, string[] traits)
        {
            try
            {
                m_traits = traits;
                if (m_traits != null && m_traits.Length % 2 != 0)
                {
                    throw new ArgumentException(Resources.GetResourceString("TraitEven"), "traits");
                }

                if (eventSourceGuid == Guid.Empty)
                {
                    throw new ArgumentException(Resources.GetResourceString("EventSource_NeedGuid"));
                }

                if (eventSourceName == null)
                {
                    throw new ArgumentException(Resources.GetResourceString("EventSource_NeedName"));
                }

                m_name = eventSourceName;
                m_guid = eventSourceGuid;
                m_curLiveSessions = new SessionMask(0);
                m_etwSessionIdMap = new EtwSession[SessionMask.MAX];
                m_activityTracker = ActivityTracker.Instance;
                this.InitializeProviderMetadata();
                var provider = new OverideEventProvider(this);
                provider.Register(eventSourceGuid);
                EventListener.AddEventSource(this);
                m_provider = provider;
                var osVer = Environment.OSVersion.Version.Major * 10 + Environment.OSVersion.Version.Minor;
                if (this.Name != "System.Diagnostics.Eventing.FrameworkEventSource" || osVer >= 62)
                {
                    int setInformationResult;
                    System.Runtime.InteropServices.GCHandle metadataHandle = System.Runtime.InteropServices.GCHandle.Alloc(this.providerMetadata, System.Runtime.InteropServices.GCHandleType.Pinned);
                    IntPtr providerMetadata = metadataHandle.AddrOfPinnedObject();
                    setInformationResult = m_provider.SetInformation(UnsafeNativeMethods.ManifestEtw.EVENT_INFO_CLASS.SetTraits, providerMetadata, (uint)this.providerMetadata.Length);
                    metadataHandle.Free();
                }

                Contract.Assert(!m_eventSourceEnabled);
                m_completelyInited = true;
            }
            catch (Exception e)
            {
                if (m_constructionException == null)
                    m_constructionException = e;
                ReportOutOfBandMessage("ERROR: Exception during construction of EventSource " + Name + ": " + e.Message, true);
            }

            lock (EventListener.EventListenersLock)
            {
                EventCommandEventArgs deferredCommands = m_deferredCommands;
                while (deferredCommands != null)
                {
                    DoCommand(deferredCommands);
                    deferredCommands = deferredCommands.nextCommand;
                }
            }
        }

        private static string GetName(Type eventSourceType, EventManifestOptions flags)
        {
            if (eventSourceType == null)
                throw new ArgumentNullException("eventSourceType");
            Contract.EndContractBlock();
            EventSourceAttribute attrib = (EventSourceAttribute)GetCustomAttributeHelper(eventSourceType, typeof (EventSourceAttribute), flags);
            if (attrib != null && attrib.Name != null)
                return attrib.Name;
            return eventSourceType.Name;
        }

        private struct Sha1ForNonSecretPurposes
        {
            private long length;
            private uint[] w;
            private int pos;
            public void Start()
            {
                if (this.w == null)
                {
                    this.w = new uint[85];
                }

                this.length = 0;
                this.pos = 0;
                this.w[80] = 0x67452301;
                this.w[81] = 0xEFCDAB89;
                this.w[82] = 0x98BADCFE;
                this.w[83] = 0x10325476;
                this.w[84] = 0xC3D2E1F0;
            }

            public void Append(byte input)
            {
                this.w[this.pos / 4] = (this.w[this.pos / 4] << 8) | input;
                if (64 == ++this.pos)
                {
                    this.Drain();
                }
            }

            public void Append(byte[] input)
            {
                foreach (var b in input)
                {
                    this.Append(b);
                }
            }

            public void Finish(byte[] output)
            {
                long l = this.length + 8 * this.pos;
                this.Append(0x80);
                while (this.pos != 56)
                {
                    this.Append(0x00);
                }

                unchecked
                {
                    this.Append((byte)(l >> 56));
                    this.Append((byte)(l >> 48));
                    this.Append((byte)(l >> 40));
                    this.Append((byte)(l >> 32));
                    this.Append((byte)(l >> 24));
                    this.Append((byte)(l >> 16));
                    this.Append((byte)(l >> 8));
                    this.Append((byte)l);
                    int end = output.Length < 20 ? output.Length : 20;
                    for (int i = 0; i != end; i++)
                    {
                        uint temp = this.w[80 + i / 4];
                        output[i] = (byte)(temp >> 24);
                        this.w[80 + i / 4] = temp << 8;
                    }
                }
            }

            private void Drain()
            {
                for (int i = 16; i != 80; i++)
                {
                    this.w[i] = Rol1((this.w[i - 3] ^ this.w[i - 8] ^ this.w[i - 14] ^ this.w[i - 16]));
                }

                unchecked
                {
                    uint a = this.w[80];
                    uint b = this.w[81];
                    uint c = this.w[82];
                    uint d = this.w[83];
                    uint e = this.w[84];
                    for (int i = 0; i != 20; i++)
                    {
                        const uint k = 0x5A827999;
                        uint f = (b & c) | ((~b) & d);
                        uint temp = Rol5(a) + f + e + k + this.w[i];
                        e = d;
                        d = c;
                        c = Rol30(b);
                        b = a;
                        a = temp;
                    }

                    for (int i = 20; i != 40; i++)
                    {
                        uint f = b ^ c ^ d;
                        const uint k = 0x6ED9EBA1;
                        uint temp = Rol5(a) + f + e + k + this.w[i];
                        e = d;
                        d = c;
                        c = Rol30(b);
                        b = a;
                        a = temp;
                    }

                    for (int i = 40; i != 60; i++)
                    {
                        uint f = (b & c) | (b & d) | (c & d);
                        const uint k = 0x8F1BBCDC;
                        uint temp = Rol5(a) + f + e + k + this.w[i];
                        e = d;
                        d = c;
                        c = Rol30(b);
                        b = a;
                        a = temp;
                    }

                    for (int i = 60; i != 80; i++)
                    {
                        uint f = b ^ c ^ d;
                        const uint k = 0xCA62C1D6;
                        uint temp = Rol5(a) + f + e + k + this.w[i];
                        e = d;
                        d = c;
                        c = Rol30(b);
                        b = a;
                        a = temp;
                    }

                    this.w[80] += a;
                    this.w[81] += b;
                    this.w[82] += c;
                    this.w[83] += d;
                    this.w[84] += e;
                }

                this.length += 512;
                this.pos = 0;
            }

            private static uint Rol1(uint input)
            {
                return (input << 1) | (input >> 31);
            }

            private static uint Rol5(uint input)
            {
                return (input << 5) | (input >> 27);
            }

            private static uint Rol30(uint input)
            {
                return (input << 30) | (input >> 2);
            }
        }

        private static Guid GenerateGuidFromName(string name)
        {
            byte[] bytes = Encoding.BigEndianUnicode.GetBytes(name);
            var hash = new Sha1ForNonSecretPurposes();
            hash.Start();
            hash.Append(namespaceBytes);
            hash.Append(bytes);
            Array.Resize(ref bytes, 16);
            hash.Finish(bytes);
            bytes[7] = unchecked ((byte)((bytes[7] & 0x0F) | 0x50));
            return new Guid(bytes);
        }

        private unsafe object DecodeObject(int eventId, int parameterId, ref EventSource.EventData*data)
        {
            IntPtr dataPointer = data->DataPointer;
            ++data;
            Type dataType = GetDataType(m_eventData[eventId], parameterId);
            Again:
                if (dataType == typeof (IntPtr))
                {
                    return *((IntPtr*)dataPointer);
                }
                else if (dataType == typeof (int))
                {
                    return *((int *)dataPointer);
                }
                else if (dataType == typeof (uint))
                {
                    return *((uint *)dataPointer);
                }
                else if (dataType == typeof (long))
                {
                    return *((long *)dataPointer);
                }
                else if (dataType == typeof (ulong))
                {
                    return *((ulong *)dataPointer);
                }
                else if (dataType == typeof (byte))
                {
                    return *((byte *)dataPointer);
                }
                else if (dataType == typeof (sbyte))
                {
                    return *((sbyte *)dataPointer);
                }
                else if (dataType == typeof (short))
                {
                    return *((short *)dataPointer);
                }
                else if (dataType == typeof (ushort))
                {
                    return *((ushort *)dataPointer);
                }
                else if (dataType == typeof (float))
                {
                    return *((float *)dataPointer);
                }
                else if (dataType == typeof (double))
                {
                    return *((double *)dataPointer);
                }
                else if (dataType == typeof (decimal))
                {
                    return *((decimal *)dataPointer);
                }
                else if (dataType == typeof (bool))
                {
                    if (*((int *)dataPointer) == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (dataType == typeof (Guid))
                {
                    return *((Guid*)dataPointer);
                }
                else if (dataType == typeof (char))
                {
                    return *((char *)dataPointer);
                }
                else if (dataType == typeof (DateTime))
                {
                    long dateTimeTicks = *((long *)dataPointer);
                    return DateTime.FromFileTimeUtc(dateTimeTicks);
                }
                else if (dataType == typeof (byte[]))
                {
                    int cbSize = *((int *)dataPointer);
                    byte[] blob = new byte[cbSize];
                    dataPointer = data->DataPointer;
                    data++;
                    for (int i = 0; i < cbSize; ++i)
                        blob[i] = *((byte *)dataPointer);
                    return blob;
                }
                else if (dataType == typeof (byte *))
                {
                    return null;
                }
                else
                {
                    if (m_EventSourcePreventRecursion && m_EventSourceInDecodeObject)
                    {
                        return null;
                    }

                    try
                    {
                        m_EventSourceInDecodeObject = true;
                        if (dataType.IsEnum())
                        {
                            dataType = Enum.GetUnderlyingType(dataType);
                            goto Again;
                        }

                        return System.Runtime.InteropServices.Marshal.PtrToStringUni(dataPointer);
                    }
                    finally
                    {
                        m_EventSourceInDecodeObject = false;
                    }
                }
        }

        private EventDispatcher GetDispatcher(EventListener listener)
        {
            EventDispatcher dispatcher = m_Dispatchers;
            while (dispatcher != null)
            {
                if (dispatcher.m_Listener == listener)
                    return dispatcher;
                dispatcher = dispatcher.m_Next;
            }

            return dispatcher;
        }

        private unsafe void WriteEventVarargs(int eventId, Guid*childActivityID, object[] args)
        {
            if (m_eventSourceEnabled)
            {
                try
                {
                    Contract.Assert(m_eventData != null);
                    if (childActivityID != null)
                    {
                        ValidateEventOpcodeForTransfer(ref m_eventData[eventId], m_eventData[eventId].Name);
                        if (!m_eventData[eventId].HasRelatedActivityID)
                        {
                            throw new ArgumentException(Resources.GetResourceString("EventSource_NoRelatedActivityId"));
                        }
                    }

                    LogEventArgsMismatches(m_eventData[eventId].Parameters, args);
                    if (m_eventData[eventId].EnabledForETW)
                    {
                        Guid*pActivityId = null;
                        Guid activityId = Guid.Empty;
                        Guid relatedActivityId = Guid.Empty;
                        EventOpcode opcode = (EventOpcode)m_eventData[eventId].Descriptor.Opcode;
                        EventActivityOptions activityOptions = m_eventData[eventId].ActivityOptions;
                        if (childActivityID == null && ((activityOptions & EventActivityOptions.Disable) == 0))
                        {
                            if (opcode == EventOpcode.Start)
                            {
                                m_activityTracker.OnStart(m_name, m_eventData[eventId].Name, m_eventData[eventId].Descriptor.Task, ref activityId, ref relatedActivityId, m_eventData[eventId].ActivityOptions);
                            }
                            else if (opcode == EventOpcode.Stop)
                            {
                                m_activityTracker.OnStop(m_name, m_eventData[eventId].Name, m_eventData[eventId].Descriptor.Task, ref activityId);
                            }

                            if (activityId != Guid.Empty)
                                pActivityId = &activityId;
                            if (relatedActivityId != Guid.Empty)
                                childActivityID = &relatedActivityId;
                        }

                        SessionMask etwSessions = SessionMask.All;
                        if ((ulong)m_curLiveSessions != 0)
                            etwSessions = GetEtwSessionMask(eventId, childActivityID);
                        if ((ulong)etwSessions != 0 || m_legacySessions != null && m_legacySessions.Count > 0)
                        {
                            if (!SelfDescribingEvents)
                            {
                                if (etwSessions.IsEqualOrSupersetOf(m_curLiveSessions))
                                {
                                    if (!m_provider.WriteEvent(ref m_eventData[eventId].Descriptor, pActivityId, childActivityID, args))
                                        ThrowEventSourceException(m_eventData[eventId].Name);
                                }
                                else
                                {
                                    long origKwd = unchecked ((long)((ulong)m_eventData[eventId].Descriptor.Keywords & ~(SessionMask.All.ToEventKeywords())));
                                    var desc = new EventDescriptor(m_eventData[eventId].Descriptor.EventId, m_eventData[eventId].Descriptor.Version, m_eventData[eventId].Descriptor.Channel, m_eventData[eventId].Descriptor.Level, m_eventData[eventId].Descriptor.Opcode, m_eventData[eventId].Descriptor.Task, unchecked ((long)(ulong)etwSessions | origKwd));
                                    if (!m_provider.WriteEvent(ref desc, pActivityId, childActivityID, args))
                                        ThrowEventSourceException(m_eventData[eventId].Name);
                                }
                            }
                            else
                            {
                                TraceLoggingEventTypes tlet = m_eventData[eventId].TraceLoggingEventTypes;
                                if (tlet == null)
                                {
                                    tlet = new TraceLoggingEventTypes(m_eventData[eventId].Name, EventTags.None, m_eventData[eventId].Parameters);
                                    Interlocked.CompareExchange(ref m_eventData[eventId].TraceLoggingEventTypes, tlet, null);
                                }

                                long origKwd = unchecked ((long)((ulong)m_eventData[eventId].Descriptor.Keywords & ~(SessionMask.All.ToEventKeywords())));
                                EventSourceOptions opt = new EventSourceOptions{Keywords = (EventKeywords)unchecked ((long)(ulong)etwSessions | origKwd), Level = (EventLevel)m_eventData[eventId].Descriptor.Level, Opcode = (EventOpcode)m_eventData[eventId].Descriptor.Opcode};
                                WriteMultiMerge(m_eventData[eventId].Name, ref opt, tlet, pActivityId, childActivityID, args);
                            }
                        }
                    }

                    if (m_Dispatchers != null && m_eventData[eventId].EnabledForAnyListener)
                    {
                        if (AppContextSwitches.PreserveEventListnerObjectIdentity)
                        {
                            WriteToAllListeners(eventId, childActivityID, args);
                        }
                        else
                        {
                            object[] serializedArgs = SerializeEventArgs(eventId, args);
                            WriteToAllListeners(eventId, childActivityID, serializedArgs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is EventSourceException)
                        throw;
                    else
                        ThrowEventSourceException(m_eventData[eventId].Name, ex);
                }
            }
        }

        unsafe private object[] SerializeEventArgs(int eventId, object[] args)
        {
            TraceLoggingEventTypes eventTypes = m_eventData[eventId].TraceLoggingEventTypes;
            if (eventTypes == null)
            {
                eventTypes = new TraceLoggingEventTypes(m_eventData[eventId].Name, EventTags.None, m_eventData[eventId].Parameters);
                Interlocked.CompareExchange(ref m_eventData[eventId].TraceLoggingEventTypes, eventTypes, null);
            }

            var eventData = new object[eventTypes.typeInfos.Length];
            for (int i = 0; i < eventTypes.typeInfos.Length; i++)
            {
                eventData[i] = eventTypes.typeInfos[i].GetData(args[i]);
            }

            return eventData;
        }

        private void LogEventArgsMismatches(ParameterInfo[] infos, object[] args)
        {
            bool typesMatch = args.Length == infos.Length;
            int i = 0;
            while (typesMatch && i < args.Length)
            {
                Type pType = infos[i].ParameterType;
                if ((args[i] != null && (args[i].GetType() != pType)) || (args[i] == null && (!(pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof (Nullable<>)))))
                {
                    typesMatch = false;
                    break;
                }

                ++i;
            }

            if (!typesMatch)
            {
                System.Diagnostics.Debugger.Log(0, null, Resources.GetResourceString("EventSource_VarArgsParameterMismatch") + "\r\n");
            }
        }

        unsafe private void WriteToAllListeners(int eventId, Guid*childActivityID, int eventDataCount, EventSource.EventData*data)
        {
            int paramCount = GetParameterCount(m_eventData[eventId]);
            if (eventDataCount != paramCount)
            {
                ReportOutOfBandMessage(Resources.GetResourceString("EventSource_EventParametersMismatch", eventId, eventDataCount, paramCount), true);
                paramCount = Math.Min(paramCount, eventDataCount);
            }

            object[] args = new object[paramCount];
            EventSource.EventData*dataPtr = data;
            for (int i = 0; i < paramCount; i++)
                args[i] = DecodeObject(eventId, i, ref dataPtr);
            WriteToAllListeners(eventId, childActivityID, args);
        }

        unsafe private void WriteToAllListeners(int eventId, Guid*childActivityID, params object[] args)
        {
            EventWrittenEventArgs eventCallbackArgs = new EventWrittenEventArgs(this);
            eventCallbackArgs.EventId = eventId;
            if (childActivityID != null)
                eventCallbackArgs.RelatedActivityId = *childActivityID;
            eventCallbackArgs.EventName = m_eventData[eventId].Name;
            eventCallbackArgs.Message = m_eventData[eventId].Message;
            eventCallbackArgs.Payload = new ReadOnlyCollection<object>(args);
            DispatchToAllListeners(eventId, childActivityID, eventCallbackArgs);
        }

        private unsafe void DispatchToAllListeners(int eventId, Guid*childActivityID, EventWrittenEventArgs eventCallbackArgs)
        {
            Exception lastThrownException = null;
            for (EventDispatcher dispatcher = m_Dispatchers; dispatcher != null; dispatcher = dispatcher.m_Next)
            {
                Contract.Assert(dispatcher.m_EventEnabled != null);
                if (eventId == -1 || dispatcher.m_EventEnabled[eventId])
                {
                    var activityFilter = dispatcher.m_Listener.m_activityFilter;
                    if (activityFilter == null || ActivityFilter.PassesActivityFilter(activityFilter, childActivityID, m_eventData[eventId].TriggersActivityTracking > 0, this, eventId) || !dispatcher.m_activityFilteringEnabled)
                    {
                        try
                        {
                            dispatcher.m_Listener.OnEventWritten(eventCallbackArgs);
                        }
                        catch (Exception e)
                        {
                            ReportOutOfBandMessage("ERROR: Exception during EventSource.OnEventWritten: " + e.Message, false);
                            lastThrownException = e;
                        }
                    }
                }
            }

            if (lastThrownException != null)
            {
                throw new EventSourceException(lastThrownException);
            }
        }

        private unsafe void WriteEventString(EventLevel level, long keywords, string msgString)
        {
            if (m_provider != null)
            {
                string eventName = "EventSourceMessage";
                if (SelfDescribingEvents)
                {
                    EventSourceOptions opt = new EventSourceOptions{Keywords = (EventKeywords)unchecked (keywords), Level = level};
                    var msg = new
                    {
                    message = msgString
                    }

                    ;
                    var tlet = new TraceLoggingEventTypes(eventName, EventTags.None, new Type[]{msg.GetType()});
                    WriteMultiMergeInner(eventName, ref opt, tlet, null, null, msg);
                }
                else
                {
                    if (m_rawManifest == null && m_outOfBandMessageCount == 1)
                    {
                        ManifestBuilder manifestBuilder = new ManifestBuilder(Name, Guid, Name, null, EventManifestOptions.None);
                        manifestBuilder.StartEvent(eventName, new EventAttribute(0)
                        {Level = EventLevel.LogAlways, Task = (EventTask)0xFFFE});
                        manifestBuilder.AddEventParameter(typeof (string), "message");
                        manifestBuilder.EndEvent();
                        SendManifest(manifestBuilder.CreateManifest());
                    }

                    fixed (char *msgStringPtr = msgString)
                    {
                        EventDescriptor descr = new EventDescriptor(0, 0, 0, (byte)level, 0, 0, keywords);
                        EventProvider.EventData data = new EventProvider.EventData();
                        data.Ptr = (ulong)msgStringPtr;
                        data.Size = (uint)(2 * (msgString.Length + 1));
                        data.Reserved = 0;
                        m_provider.WriteEvent(ref descr, null, null, 1, (IntPtr)((void *)&data));
                    }
                }
            }
        }

        private void WriteStringToAllListeners(string eventName, string msg)
        {
            EventWrittenEventArgs eventCallbackArgs = new EventWrittenEventArgs(this);
            eventCallbackArgs.EventId = 0;
            eventCallbackArgs.Message = msg;
            eventCallbackArgs.Payload = new ReadOnlyCollection<object>(new List<object>()
            {msg});
            eventCallbackArgs.PayloadNames = new ReadOnlyCollection<string>(new List<string>{"message"});
            eventCallbackArgs.EventName = eventName;
            for (EventDispatcher dispatcher = m_Dispatchers; dispatcher != null; dispatcher = dispatcher.m_Next)
            {
                bool dispatcherEnabled = false;
                if (dispatcher.m_EventEnabled == null)
                {
                    dispatcherEnabled = true;
                }
                else
                {
                    for (int evtId = 0; evtId < dispatcher.m_EventEnabled.Length; ++evtId)
                    {
                        if (dispatcher.m_EventEnabled[evtId])
                        {
                            dispatcherEnabled = true;
                            break;
                        }
                    }
                }

                try
                {
                    if (dispatcherEnabled)
                        dispatcher.m_Listener.OnEventWritten(eventCallbackArgs);
                }
                catch
                {
                }
            }
        }

        unsafe private SessionMask GetEtwSessionMask(int eventId, Guid*childActivityID)
        {
            SessionMask etwSessions = new SessionMask();
            for (int i = 0; i < SessionMask.MAX; ++i)
            {
                EtwSession etwSession = m_etwSessionIdMap[i];
                if (etwSession != null)
                {
                    ActivityFilter activityFilter = etwSession.m_activityFilter;
                    if (activityFilter == null && !m_activityFilteringForETWEnabled[i] || activityFilter != null && ActivityFilter.PassesActivityFilter(activityFilter, childActivityID, m_eventData[eventId].TriggersActivityTracking > 0, this, eventId) || !m_activityFilteringForETWEnabled[i])
                    {
                        etwSessions[i] = true;
                    }
                }
            }

            if (m_legacySessions != null && m_legacySessions.Count > 0 && (EventOpcode)m_eventData[eventId].Descriptor.Opcode == EventOpcode.Send)
            {
                Guid*pCurrentActivityId = null;
                Guid currentActivityId;
                foreach (var legacyEtwSession in m_legacySessions)
                {
                    if (legacyEtwSession == null)
                        continue;
                    ActivityFilter activityFilter = legacyEtwSession.m_activityFilter;
                    if (activityFilter != null)
                    {
                        if (pCurrentActivityId == null)
                        {
                            currentActivityId = InternalCurrentThreadActivityId;
                            pCurrentActivityId = &currentActivityId;
                        }

                        ActivityFilter.FlowActivityIfNeeded(activityFilter, pCurrentActivityId, childActivityID);
                    }
                }
            }

            return etwSessions;
        }

        private bool IsEnabledByDefault(int eventNum, bool enable, EventLevel currentLevel, EventKeywords currentMatchAnyKeyword)
        {
            if (!enable)
                return false;
            EventLevel eventLevel = (EventLevel)m_eventData[eventNum].Descriptor.Level;
            EventKeywords eventKeywords = unchecked ((EventKeywords)((ulong)m_eventData[eventNum].Descriptor.Keywords & (~(SessionMask.All.ToEventKeywords()))));
            EventChannel channel = unchecked ((EventChannel)m_eventData[eventNum].Descriptor.Channel);
            return IsEnabledCommon(enable, currentLevel, currentMatchAnyKeyword, eventLevel, eventKeywords, channel);
        }

        private bool IsEnabledCommon(bool enabled, EventLevel currentLevel, EventKeywords currentMatchAnyKeyword, EventLevel eventLevel, EventKeywords eventKeywords, EventChannel eventChannel)
        {
            if (!enabled)
                return false;
            if ((currentLevel != 0) && (currentLevel < eventLevel))
                return false;
            if (currentMatchAnyKeyword != 0 && eventKeywords != 0)
            {
                if (eventChannel != EventChannel.None && this.m_channelData != null && this.m_channelData.Length > (int)eventChannel)
                {
                    EventKeywords channel_keywords = unchecked ((EventKeywords)(m_channelData[(int)eventChannel] | (ulong)eventKeywords));
                    if (channel_keywords != 0 && (channel_keywords & currentMatchAnyKeyword) == 0)
                        return false;
                }
                else
                {
                    if ((unchecked ((ulong)eventKeywords & (ulong)currentMatchAnyKeyword)) == 0)
                        return false;
                }
            }

            return true;
        }

        private void ThrowEventSourceException(string eventName, Exception innerEx = null)
        {
            if (m_EventSourceExceptionRecurenceCount > 0)
                return;
            try
            {
                m_EventSourceExceptionRecurenceCount++;
                string errorPrefix = "EventSourceException";
                if (eventName != null)
                {
                    errorPrefix += " while processing event \"" + eventName + "\"";
                }

                switch (EventProvider.GetLastWriteEventError())
                {
                    case EventProvider.WriteEventErrorCode.EventTooBig:
                        ReportOutOfBandMessage(errorPrefix + ": " + Resources.GetResourceString("EventSource_EventTooBig"), true);
                        if (ThrowOnEventWriteErrors)
                            throw new EventSourceException(Resources.GetResourceString("EventSource_EventTooBig"), innerEx);
                        break;
                    case EventProvider.WriteEventErrorCode.NoFreeBuffers:
                        ReportOutOfBandMessage(errorPrefix + ": " + Resources.GetResourceString("EventSource_NoFreeBuffers"), true);
                        if (ThrowOnEventWriteErrors)
                            throw new EventSourceException(Resources.GetResourceString("EventSource_NoFreeBuffers"), innerEx);
                        break;
                    case EventProvider.WriteEventErrorCode.NullInput:
                        ReportOutOfBandMessage(errorPrefix + ": " + Resources.GetResourceString("EventSource_NullInput"), true);
                        if (ThrowOnEventWriteErrors)
                            throw new EventSourceException(Resources.GetResourceString("EventSource_NullInput"), innerEx);
                        break;
                    case EventProvider.WriteEventErrorCode.TooManyArgs:
                        ReportOutOfBandMessage(errorPrefix + ": " + Resources.GetResourceString("EventSource_TooManyArgs"), true);
                        if (ThrowOnEventWriteErrors)
                            throw new EventSourceException(Resources.GetResourceString("EventSource_TooManyArgs"), innerEx);
                        break;
                    default:
                        if (innerEx != null)
                            ReportOutOfBandMessage(errorPrefix + ": " + innerEx.GetType() + ":" + innerEx.Message, true);
                        else
                            ReportOutOfBandMessage(errorPrefix, true);
                        if (ThrowOnEventWriteErrors)
                            throw new EventSourceException(innerEx);
                        break;
                }
            }
            finally
            {
                m_EventSourceExceptionRecurenceCount--;
            }
        }

        private void ValidateEventOpcodeForTransfer(ref EventMetadata eventData, string eventName)
        {
            if ((EventOpcode)eventData.Descriptor.Opcode != EventOpcode.Send && (EventOpcode)eventData.Descriptor.Opcode != EventOpcode.Receive && (EventOpcode)eventData.Descriptor.Opcode != EventOpcode.Start)
            {
                ThrowEventSourceException(eventName);
            }
        }

        internal static EventOpcode GetOpcodeWithDefault(EventOpcode opcode, string eventName)
        {
            if (opcode == EventOpcode.Info && eventName != null)
            {
                if (eventName.EndsWith(s_ActivityStartSuffix, StringComparison.Ordinal))
                {
                    return EventOpcode.Start;
                }
                else if (eventName.EndsWith(s_ActivityStopSuffix, StringComparison.Ordinal))
                {
                    return EventOpcode.Stop;
                }
            }

            return opcode;
        }

        private class OverideEventProvider : EventProvider
        {
            public OverideEventProvider(EventSource eventSource)
            {
                this.m_eventSource = eventSource;
            }

            protected override void OnControllerCommand(ControllerCommand command, IDictionary<string, string> arguments, int perEventSourceSessionId, int etwSessionId)
            {
                EventListener listener = null;
                m_eventSource.SendCommand(listener, perEventSourceSessionId, etwSessionId, (EventCommand)command, IsEnabled(), Level, MatchAnyKeyword, arguments);
            }

            private EventSource m_eventSource;
        }

        internal partial struct EventMetadata
        {
            public EventDescriptor Descriptor;
            public EventTags Tags;
            public bool EnabledForAnyListener;
            public bool EnabledForETW;
            public bool HasRelatedActivityID;
            public byte TriggersActivityTracking;
            public string Name;
            public string Message;
            public ParameterInfo[] Parameters;
            public TraceLoggingEventTypes TraceLoggingEventTypes;
            public EventActivityOptions ActivityOptions;
        }

        ;
        internal void SendCommand(EventListener listener, int perEventSourceSessionId, int etwSessionId, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> commandArguments)
        {
            var commandArgs = new EventCommandEventArgs(command, commandArguments, this, listener, perEventSourceSessionId, etwSessionId, enable, level, matchAnyKeyword);
            lock (EventListener.EventListenersLock)
            {
                if (m_completelyInited)
                {
                    this.m_deferredCommands = null;
                    DoCommand(commandArgs);
                }
                else
                {
                    commandArgs.nextCommand = m_deferredCommands;
                    m_deferredCommands = commandArgs;
                }
            }
        }

        internal void DoCommand(EventCommandEventArgs commandArgs)
        {
            Contract.Assert(m_completelyInited);
            if (m_provider == null)
                return;
            m_outOfBandMessageCount = 0;
            bool shouldReport = (commandArgs.perEventSourceSessionId > 0) && (commandArgs.perEventSourceSessionId <= SessionMask.MAX);
            try
            {
                EnsureDescriptorsInitialized();
                Contract.Assert(m_eventData != null);
                commandArgs.dispatcher = GetDispatcher(commandArgs.listener);
                if (commandArgs.dispatcher == null && commandArgs.listener != null)
                {
                    throw new ArgumentException(Resources.GetResourceString("EventSource_ListenerNotFound"));
                }

                if (commandArgs.Arguments == null)
                    commandArgs.Arguments = new Dictionary<string, string>();
                if (commandArgs.Command == EventCommand.Update)
                {
                    for (int i = 0; i < m_eventData.Length; i++)
                        EnableEventForDispatcher(commandArgs.dispatcher, i, IsEnabledByDefault(i, commandArgs.enable, commandArgs.level, commandArgs.matchAnyKeyword));
                    if (commandArgs.enable)
                    {
                        if (!m_eventSourceEnabled)
                        {
                            m_level = commandArgs.level;
                            m_matchAnyKeyword = commandArgs.matchAnyKeyword;
                        }
                        else
                        {
                            if (commandArgs.level > m_level)
                                m_level = commandArgs.level;
                            if (commandArgs.matchAnyKeyword == 0)
                                m_matchAnyKeyword = 0;
                            else if (m_matchAnyKeyword != 0)
                                m_matchAnyKeyword = unchecked (m_matchAnyKeyword | commandArgs.matchAnyKeyword);
                        }
                    }

                    bool bSessionEnable = (commandArgs.perEventSourceSessionId >= 0);
                    if (commandArgs.perEventSourceSessionId == 0 && commandArgs.enable == false)
                        bSessionEnable = false;
                    if (commandArgs.listener == null)
                    {
                        if (!bSessionEnable)
                            commandArgs.perEventSourceSessionId = -commandArgs.perEventSourceSessionId;
                        --commandArgs.perEventSourceSessionId;
                    }

                    commandArgs.Command = bSessionEnable ? EventCommand.Enable : EventCommand.Disable;
                    Contract.Assert(commandArgs.perEventSourceSessionId >= -1 && commandArgs.perEventSourceSessionId <= SessionMask.MAX);
                    if (bSessionEnable && commandArgs.dispatcher == null)
                    {
                        if (!SelfDescribingEvents)
                            SendManifest(m_rawManifest);
                    }

                    if (bSessionEnable && commandArgs.perEventSourceSessionId != -1)
                    {
                        bool participateInSampling = false;
                        string activityFilters;
                        int sessionIdBit;
                        ParseCommandArgs(commandArgs.Arguments, out participateInSampling, out activityFilters, out sessionIdBit);
                        if (commandArgs.listener == null && commandArgs.Arguments.Count > 0 && commandArgs.perEventSourceSessionId != sessionIdBit)
                        {
                            throw new ArgumentException(Resources.GetResourceString("EventSource_SessionIdError", commandArgs.perEventSourceSessionId + SessionMask.SHIFT_SESSION_TO_KEYWORD, sessionIdBit + SessionMask.SHIFT_SESSION_TO_KEYWORD));
                        }

                        if (commandArgs.listener == null)
                        {
                            UpdateEtwSession(commandArgs.perEventSourceSessionId, commandArgs.etwSessionId, true, activityFilters, participateInSampling);
                        }
                        else
                        {
                            ActivityFilter.UpdateFilter(ref commandArgs.listener.m_activityFilter, this, 0, activityFilters);
                            commandArgs.dispatcher.m_activityFilteringEnabled = participateInSampling;
                        }
                    }
                    else if (!bSessionEnable && commandArgs.listener == null)
                    {
                        if (commandArgs.perEventSourceSessionId >= 0 && commandArgs.perEventSourceSessionId < SessionMask.MAX)
                        {
                            commandArgs.Arguments["EtwSessionKeyword"] = (commandArgs.perEventSourceSessionId + SessionMask.SHIFT_SESSION_TO_KEYWORD).ToString(CultureInfo.InvariantCulture);
                        }
                    }

                    if (commandArgs.enable)
                    {
                        Contract.Assert(m_eventData != null);
                        m_eventSourceEnabled = true;
                    }

                    this.OnEventCommand(commandArgs);
                    var eventCommandCallback = this.m_eventCommandExecuted;
                    if (eventCommandCallback != null)
                        eventCommandCallback(this, commandArgs);
                    if (commandArgs.listener == null && !bSessionEnable && commandArgs.perEventSourceSessionId != -1)
                    {
                        UpdateEtwSession(commandArgs.perEventSourceSessionId, commandArgs.etwSessionId, false, null, false);
                    }

                    if (!commandArgs.enable)
                    {
                        if (commandArgs.listener == null)
                        {
                            for (int i = 0; i < SessionMask.MAX; ++i)
                            {
                                EtwSession etwSession = m_etwSessionIdMap[i];
                                if (etwSession != null)
                                    ActivityFilter.DisableFilter(ref etwSession.m_activityFilter, this);
                            }

                            m_activityFilteringForETWEnabled = new SessionMask(0);
                            m_curLiveSessions = new SessionMask(0);
                            if (m_etwSessionIdMap != null)
                                for (int i = 0; i < SessionMask.MAX; ++i)
                                    m_etwSessionIdMap[i] = null;
                            if (m_legacySessions != null)
                                m_legacySessions.Clear();
                        }
                        else
                        {
                            ActivityFilter.DisableFilter(ref commandArgs.listener.m_activityFilter, this);
                            commandArgs.dispatcher.m_activityFilteringEnabled = false;
                        }

                        for (int i = 0; i < m_eventData.Length; i++)
                        {
                            bool isEnabledForAnyListener = false;
                            for (EventDispatcher dispatcher = m_Dispatchers; dispatcher != null; dispatcher = dispatcher.m_Next)
                            {
                                if (dispatcher.m_EventEnabled[i])
                                {
                                    isEnabledForAnyListener = true;
                                    break;
                                }
                            }

                            m_eventData[i].EnabledForAnyListener = isEnabledForAnyListener;
                        }

                        if (!AnyEventEnabled())
                        {
                            m_level = 0;
                            m_matchAnyKeyword = 0;
                            m_eventSourceEnabled = false;
                        }
                    }

                    UpdateKwdTriggers(commandArgs.enable);
                }
                else
                {
                    if (commandArgs.Command == EventCommand.SendManifest)
                    {
                        if (m_rawManifest != null)
                            SendManifest(m_rawManifest);
                    }

                    this.OnEventCommand(commandArgs);
                    var eventCommandCallback = m_eventCommandExecuted;
                    if (eventCommandCallback != null)
                        eventCommandCallback(this, commandArgs);
                }

                if (m_completelyInited && (commandArgs.listener != null || shouldReport))
                {
                    SessionMask m = SessionMask.FromId(commandArgs.perEventSourceSessionId);
                    ReportActivitySamplingInfo(commandArgs.listener, m);
                }
            }
            catch (Exception e)
            {
                ReportOutOfBandMessage("ERROR: Exception in Command Processing for EventSource " + Name + ": " + e.Message, true);
            }
        }

        internal void UpdateEtwSession(int sessionIdBit, int etwSessionId, bool bEnable, string activityFilters, bool participateInSampling)
        {
            if (sessionIdBit < SessionMask.MAX)
            {
                if (bEnable)
                {
                    var etwSession = EtwSession.GetEtwSession(etwSessionId, true);
                    ActivityFilter.UpdateFilter(ref etwSession.m_activityFilter, this, sessionIdBit, activityFilters);
                    m_etwSessionIdMap[sessionIdBit] = etwSession;
                    m_activityFilteringForETWEnabled[sessionIdBit] = participateInSampling;
                }
                else
                {
                    var etwSession = EtwSession.GetEtwSession(etwSessionId);
                    m_etwSessionIdMap[sessionIdBit] = null;
                    m_activityFilteringForETWEnabled[sessionIdBit] = false;
                    if (etwSession != null)
                    {
                        ActivityFilter.DisableFilter(ref etwSession.m_activityFilter, this);
                        EtwSession.RemoveEtwSession(etwSession);
                    }
                }

                m_curLiveSessions[sessionIdBit] = bEnable;
            }
            else
            {
                if (bEnable)
                {
                    if (m_legacySessions == null)
                        m_legacySessions = new List<EtwSession>(8);
                    var etwSession = EtwSession.GetEtwSession(etwSessionId, true);
                    if (!m_legacySessions.Contains(etwSession))
                        m_legacySessions.Add(etwSession);
                }
                else
                {
                    var etwSession = EtwSession.GetEtwSession(etwSessionId);
                    if (etwSession != null)
                    {
                        if (m_legacySessions != null)
                            m_legacySessions.Remove(etwSession);
                        EtwSession.RemoveEtwSession(etwSession);
                    }
                }
            }
        }

        internal static bool ParseCommandArgs(IDictionary<string, string> commandArguments, out bool participateInSampling, out string activityFilters, out int sessionIdBit)
        {
            bool res = true;
            participateInSampling = false;
            string activityFilterString;
            if (commandArguments.TryGetValue("ActivitySamplingStartEvent", out activityFilters))
            {
                participateInSampling = true;
            }

            if (commandArguments.TryGetValue("ActivitySampling", out activityFilterString))
            {
                if (string.Compare(activityFilterString, "false", StringComparison.OrdinalIgnoreCase) == 0 || activityFilterString == "0")
                    participateInSampling = false;
                else
                    participateInSampling = true;
            }

            string sSessionKwd;
            int sessionKwd = -1;
            if (!commandArguments.TryGetValue("EtwSessionKeyword", out sSessionKwd) || !int.TryParse(sSessionKwd, out sessionKwd) || sessionKwd < SessionMask.SHIFT_SESSION_TO_KEYWORD || sessionKwd >= SessionMask.SHIFT_SESSION_TO_KEYWORD + SessionMask.MAX)
            {
                sessionIdBit = -1;
                res = false;
            }
            else
            {
                sessionIdBit = sessionKwd - SessionMask.SHIFT_SESSION_TO_KEYWORD;
            }

            return res;
        }

        internal void UpdateKwdTriggers(bool enable)
        {
            if (enable)
            {
                ulong gKeywords = unchecked ((ulong)m_matchAnyKeyword);
                if (gKeywords == 0)
                    gKeywords = 0xFFFFffffFFFFffff;
                m_keywordTriggers = 0;
                for (int sessId = 0; sessId < SessionMask.MAX; ++sessId)
                {
                    EtwSession etwSession = m_etwSessionIdMap[sessId];
                    if (etwSession == null)
                        continue;
                    ActivityFilter activityFilter = etwSession.m_activityFilter;
                    ActivityFilter.UpdateKwdTriggers(activityFilter, m_guid, this, unchecked ((EventKeywords)gKeywords));
                }
            }
            else
            {
                m_keywordTriggers = 0;
            }
        }

        internal bool EnableEventForDispatcher(EventDispatcher dispatcher, int eventId, bool value)
        {
            if (dispatcher == null)
            {
                if (eventId >= m_eventData.Length)
                    return false;
                if (m_provider != null)
                    m_eventData[eventId].EnabledForETW = value;
            }
            else
            {
                if (eventId >= dispatcher.m_EventEnabled.Length)
                    return false;
                dispatcher.m_EventEnabled[eventId] = value;
                if (value)
                    m_eventData[eventId].EnabledForAnyListener = true;
            }

            return true;
        }

        private bool AnyEventEnabled()
        {
            for (int i = 0; i < m_eventData.Length; i++)
                if (m_eventData[i].EnabledForETW || m_eventData[i].EnabledForAnyListener)
                    return true;
            return false;
        }

        private bool IsDisposed
        {
            get
            {
                return m_eventSourceDisposed;
            }
        }

        private void EnsureDescriptorsInitialized()
        {
            Contract.Assert(Monitor.IsEntered(EventListener.EventListenersLock));
            if (m_eventData == null)
            {
                Guid eventSourceGuid = Guid.Empty;
                string eventSourceName = null;
                EventMetadata[] eventData = null;
                byte[] manifest = null;
                GetMetadata(out eventSourceGuid, out eventSourceName, out eventData, out manifest);
                if (eventSourceGuid.Equals(Guid.Empty) || eventSourceName == null || eventData == null || manifest == null)
                {
                    Contract.Assert(m_rawManifest == null);
                    m_rawManifest = CreateManifestAndDescriptors(this.GetType(), Name, this);
                    Contract.Assert(m_eventData != null);
                }
                else
                {
                    m_name = eventSourceName;
                    m_guid = eventSourceGuid;
                    m_eventData = eventData;
                    m_rawManifest = manifest;
                }

                foreach (WeakReference eventSourceRef in EventListener.s_EventSources)
                {
                    EventSource eventSource = eventSourceRef.Target as EventSource;
                    if (eventSource != null && eventSource.Guid == m_guid && !eventSource.IsDisposed)
                    {
                        if (eventSource != this)
                        {
                            throw new ArgumentException(Resources.GetResourceString("EventSource_EventSourceGuidInUse", m_guid));
                        }
                    }
                }

                EventDispatcher dispatcher = m_Dispatchers;
                while (dispatcher != null)
                {
                    if (dispatcher.m_EventEnabled == null)
                        dispatcher.m_EventEnabled = new bool[m_eventData.Length];
                    dispatcher = dispatcher.m_Next;
                }
            }

            if (s_currentPid == 0)
            {
                s_currentPid = Win32Native.GetCurrentProcessId();
            }
        }

        private unsafe bool SendManifest(byte[] rawManifest)
        {
            bool success = true;
            if (rawManifest == null)
                return false;
            Contract.Assert(!SelfDescribingEvents);
            fixed (byte *dataPtr = rawManifest)
            {
                var manifestDescr = new EventDescriptor(0xFFFE, 1, 0, 0, 0xFE, 0xFFFE, 0x00ffFFFFffffFFFF);
                ManifestEnvelope envelope = new ManifestEnvelope();
                envelope.Format = ManifestEnvelope.ManifestFormats.SimpleXmlFormat;
                envelope.MajorVersion = 1;
                envelope.MinorVersion = 0;
                envelope.Magic = 0x5B;
                int dataLeft = rawManifest.Length;
                envelope.ChunkNumber = 0;
                EventProvider.EventData*dataDescrs = stackalloc EventProvider.EventData[2];
                dataDescrs[0].Ptr = (ulong)&envelope;
                dataDescrs[0].Size = (uint)sizeof (ManifestEnvelope);
                dataDescrs[0].Reserved = 0;
                dataDescrs[1].Ptr = (ulong)dataPtr;
                dataDescrs[1].Reserved = 0;
                int chunkSize = ManifestEnvelope.MaxChunkSize;
                TRY_AGAIN_WITH_SMALLER_CHUNK_SIZE:
                    envelope.TotalChunks = (ushort)((dataLeft + (chunkSize - 1)) / chunkSize);
                while (dataLeft > 0)
                {
                    dataDescrs[1].Size = (uint)Math.Min(dataLeft, chunkSize);
                    if (m_provider != null)
                    {
                        if (!m_provider.WriteEvent(ref manifestDescr, null, null, 2, (IntPtr)dataDescrs))
                        {
                            if (EventProvider.GetLastWriteEventError() == EventProvider.WriteEventErrorCode.EventTooBig)
                            {
                                if (envelope.ChunkNumber == 0 && chunkSize > 256)
                                {
                                    chunkSize = chunkSize / 2;
                                    goto TRY_AGAIN_WITH_SMALLER_CHUNK_SIZE;
                                }
                            }

                            success = false;
                            if (ThrowOnEventWriteErrors)
                                ThrowEventSourceException("SendManifest");
                            break;
                        }
                    }

                    dataLeft -= chunkSize;
                    dataDescrs[1].Ptr += (uint)chunkSize;
                    envelope.ChunkNumber++;
                }
            }

            return success;
        }

        internal static Attribute GetCustomAttributeHelper(MemberInfo member, Type attributeType, EventManifestOptions flags = EventManifestOptions.None)
        {
            if (!member.Module.Assembly.ReflectionOnly() && (flags & EventManifestOptions.AllowEventSourceOverride) == 0)
            {
                Attribute firstAttribute = null;
                foreach (var attribute in member.GetCustomAttributes(attributeType, false))
                {
                    firstAttribute = (Attribute)attribute;
                    break;
                }

                return firstAttribute;
            }

            string fullTypeNameToFind = attributeType.FullName;
            foreach (CustomAttributeData data in CustomAttributeData.GetCustomAttributes(member))
            {
                if (AttributeTypeNamesMatch(attributeType, data.Constructor.ReflectedType))
                {
                    Attribute attr = null;
                    Contract.Assert(data.ConstructorArguments.Count <= 1);
                    if (data.ConstructorArguments.Count == 1)
                    {
                        attr = (Attribute)Activator.CreateInstance(attributeType, new object[]{data.ConstructorArguments[0].Value});
                    }
                    else if (data.ConstructorArguments.Count == 0)
                    {
                        attr = (Attribute)Activator.CreateInstance(attributeType);
                    }

                    if (attr != null)
                    {
                        Type t = attr.GetType();
                        foreach (CustomAttributeNamedArgument namedArgument in data.NamedArguments)
                        {
                            PropertyInfo p = t.GetProperty(namedArgument.MemberInfo.Name, BindingFlags.Public | BindingFlags.Instance);
                            object value = namedArgument.TypedValue.Value;
                            if (p.PropertyType.IsEnum)
                            {
                                value = Enum.Parse(p.PropertyType, value.ToString());
                            }

                            p.SetValue(attr, value, null);
                        }

                        return attr;
                    }
                }
            }

            return null;
        }

        private static bool AttributeTypeNamesMatch(Type attributeType, Type reflectedAttributeType)
        {
            return attributeType == reflectedAttributeType || string.Equals(attributeType.FullName, reflectedAttributeType.FullName, StringComparison.Ordinal) || string.Equals(attributeType.Name, reflectedAttributeType.Name, StringComparison.Ordinal) && attributeType.Namespace.EndsWith("Diagnostics.Tracing") && (reflectedAttributeType.Namespace.EndsWith("Diagnostics.Tracing"));
        }

        private static Type GetEventSourceBaseType(Type eventSourceType, bool allowEventSourceOverride, bool reflectionOnly)
        {
            if (eventSourceType.BaseType() == null)
                return null;
            do
            {
                eventSourceType = eventSourceType.BaseType();
            }
            while (eventSourceType != null && eventSourceType.IsAbstract());
            if (eventSourceType != null)
            {
                if (!allowEventSourceOverride)
                {
                    if (reflectionOnly && eventSourceType.FullName != typeof (EventSource).FullName || !reflectionOnly && eventSourceType != typeof (EventSource))
                        return null;
                }
                else
                {
                    if (eventSourceType.Name != "EventSource")
                        return null;
                }
            }

            return eventSourceType;
        }

        private static byte[] CreateManifestAndDescriptors(Type eventSourceType, string eventSourceDllName, EventSource source, EventManifestOptions flags = EventManifestOptions.None)
        {
            ManifestBuilder manifest = null;
            bool bNeedsManifest = source != null ? !source.SelfDescribingEvents : true;
            Exception exception = null;
            byte[] res = null;
            if (eventSourceType.IsAbstract() && (flags & EventManifestOptions.Strict) == 0)
                return null;
            try
            {
                MethodInfo[] methods = eventSourceType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                EventAttribute defaultEventAttribute;
                int eventId = 1;
                EventMetadata[] eventData = null;
                Dictionary<string, string> eventsByName = null;
                if (source != null || (flags & EventManifestOptions.Strict) != 0)
                {
                    eventData = new EventMetadata[methods.Length + 1];
                    eventData[0].Name = "";
                }

                ResourceManager resources = null;
                EventSourceAttribute eventSourceAttrib = (EventSourceAttribute)GetCustomAttributeHelper(eventSourceType, typeof (EventSourceAttribute), flags);
                if (eventSourceAttrib != null && eventSourceAttrib.LocalizationResources != null)
                    resources = new ResourceManager(eventSourceAttrib.LocalizationResources, eventSourceType.Assembly());
                manifest = new ManifestBuilder(GetName(eventSourceType, flags), GetGuid(eventSourceType), eventSourceDllName, resources, flags);
                manifest.StartEvent("EventSourceMessage", new EventAttribute(0)
                {Level = EventLevel.LogAlways, Task = (EventTask)0xFFFE});
                manifest.AddEventParameter(typeof (string), "message");
                manifest.EndEvent();
                if ((flags & EventManifestOptions.Strict) != 0)
                {
                    bool typeMatch = GetEventSourceBaseType(eventSourceType, (flags & EventManifestOptions.AllowEventSourceOverride) != 0, eventSourceType.Assembly().ReflectionOnly()) != null;
                    if (!typeMatch)
                    {
                        manifest.ManifestError(Resources.GetResourceString("EventSource_TypeMustDeriveFromEventSource"));
                    }

                    if (!eventSourceType.IsAbstract() && !eventSourceType.IsSealed())
                    {
                        manifest.ManifestError(Resources.GetResourceString("EventSource_TypeMustBeSealedOrAbstract"));
                    }
                }

                foreach (var providerEnumKind in new string[]{"Keywords", "Tasks", "Opcodes"})
                {
                    Type nestedType = eventSourceType.GetNestedType(providerEnumKind);
                    if (nestedType != null)
                    {
                        if (eventSourceType.IsAbstract())
                        {
                            manifest.ManifestError(Resources.GetResourceString("EventSource_AbstractMustNotDeclareKTOC", nestedType.Name));
                        }
                        else
                        {
                            foreach (FieldInfo staticField in nestedType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                            {
                                AddProviderEnumKind(manifest, staticField, providerEnumKind);
                            }
                        }
                    }
                }

                {
                    manifest.AddKeyword("Session3", (long)0x1000 << 32);
                    manifest.AddKeyword("Session2", (long)0x2000 << 32);
                    manifest.AddKeyword("Session1", (long)0x4000 << 32);
                    manifest.AddKeyword("Session0", (long)0x8000 << 32);
                }

                if (eventSourceType != typeof (EventSource))
                {
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        ParameterInfo[] args = method.GetParameters();
                        EventAttribute eventAttribute = (EventAttribute)GetCustomAttributeHelper(method, typeof (EventAttribute), flags);
                        if (method.IsStatic)
                        {
                            continue;
                        }

                        if (eventSourceType.IsAbstract())
                        {
                            if (eventAttribute != null)
                            {
                                manifest.ManifestError(Resources.GetResourceString("EventSource_AbstractMustNotDeclareEventMethods", method.Name, eventAttribute.EventId));
                            }

                            continue;
                        }
                        else if (eventAttribute == null)
                        {
                            if (method.ReturnType != typeof (void))
                            {
                                continue;
                            }

                            if (method.IsVirtual)
                            {
                                continue;
                            }

                            if (GetCustomAttributeHelper(method, typeof (NonEventAttribute), flags) != null)
                                continue;
                            defaultEventAttribute = new EventAttribute(eventId);
                            eventAttribute = defaultEventAttribute;
                        }
                        else if (eventAttribute.EventId <= 0)
                        {
                            manifest.ManifestError(Resources.GetResourceString("EventSource_NeedPositiveId", method.Name), true);
                            continue;
                        }

                        if (method.Name.LastIndexOf('.') >= 0)
                        {
                            manifest.ManifestError(Resources.GetResourceString("EventSource_EventMustNotBeExplicitImplementation", method.Name, eventAttribute.EventId));
                        }

                        eventId++;
                        string eventName = method.Name;
                        if (eventAttribute.Opcode == EventOpcode.Info)
                        {
                            bool noTask = (eventAttribute.Task == EventTask.None);
                            if (noTask)
                                eventAttribute.Task = (EventTask)(0xFFFE - eventAttribute.EventId);
                            if (!eventAttribute.IsOpcodeSet)
                                eventAttribute.Opcode = GetOpcodeWithDefault(EventOpcode.Info, eventName);
                            if (noTask)
                            {
                                if (eventAttribute.Opcode == EventOpcode.Start)
                                {
                                    string taskName = eventName.Substring(0, eventName.Length - s_ActivityStartSuffix.Length);
                                    if (string.Compare(eventName, 0, taskName, 0, taskName.Length) == 0 && string.Compare(eventName, taskName.Length, s_ActivityStartSuffix, 0, Math.Max(eventName.Length - taskName.Length, s_ActivityStartSuffix.Length)) == 0)
                                    {
                                        manifest.AddTask(taskName, (int)eventAttribute.Task);
                                    }
                                }
                                else if (eventAttribute.Opcode == EventOpcode.Stop)
                                {
                                    int startEventId = eventAttribute.EventId - 1;
                                    if (eventData != null && startEventId < eventData.Length)
                                    {
                                        Contract.Assert(0 <= startEventId);
                                        EventMetadata startEventMetadata = eventData[startEventId];
                                        string taskName = eventName.Substring(0, eventName.Length - s_ActivityStopSuffix.Length);
                                        if (startEventMetadata.Descriptor.Opcode == (byte)EventOpcode.Start && string.Compare(startEventMetadata.Name, 0, taskName, 0, taskName.Length) == 0 && string.Compare(startEventMetadata.Name, taskName.Length, s_ActivityStartSuffix, 0, Math.Max(startEventMetadata.Name.Length - taskName.Length, s_ActivityStartSuffix.Length)) == 0)
                                        {
                                            eventAttribute.Task = (EventTask)startEventMetadata.Descriptor.Task;
                                            noTask = false;
                                        }
                                    }

                                    if (noTask && (flags & EventManifestOptions.Strict) != 0)
                                    {
                                        throw new ArgumentException(Resources.GetResourceString("EventSource_StopsFollowStarts"));
                                    }
                                }
                            }
                        }

                        bool hasRelatedActivityID = RemoveFirstArgIfRelatedActivityId(ref args);
                        if (!(source != null && source.SelfDescribingEvents))
                        {
                            manifest.StartEvent(eventName, eventAttribute);
                            for (int fieldIdx = 0; fieldIdx < args.Length; fieldIdx++)
                            {
                                manifest.AddEventParameter(args[fieldIdx].ParameterType, args[fieldIdx].Name);
                            }

                            manifest.EndEvent();
                        }

                        if (source != null || (flags & EventManifestOptions.Strict) != 0)
                        {
                            DebugCheckEvent(ref eventsByName, eventData, method, eventAttribute, manifest, flags);
                            if (eventAttribute.Channel != EventChannel.None)
                            {
                                unchecked
                                {
                                    eventAttribute.Keywords |= (EventKeywords)manifest.GetChannelKeyword(eventAttribute.Channel);
                                }
                            }

                            string eventKey = "event_" + eventName;
                            string msg = manifest.GetLocalizedMessage(eventKey, CultureInfo.CurrentUICulture, etwFormat: false);
                            if (msg != null)
                                eventAttribute.Message = msg;
                            AddEventDescriptor(ref eventData, eventName, eventAttribute, args, hasRelatedActivityID);
                        }
                    }
                }

                NameInfo.ReserveEventIDsBelow(eventId);
                if (source != null)
                {
                    TrimEventDescriptors(ref eventData);
                    source.m_eventData = eventData;
                    source.m_channelData = manifest.GetChannelData();
                }

                if (!eventSourceType.IsAbstract() && (source == null || !source.SelfDescribingEvents))
                {
                    bNeedsManifest = (flags & EventManifestOptions.OnlyIfNeededForRegistration) == 0 || manifest.GetChannelData().Length > 0;
                    if (!bNeedsManifest && (flags & EventManifestOptions.Strict) == 0)
                        return null;
                    res = manifest.CreateManifest();
                }
            }
            catch (Exception e)
            {
                if ((flags & EventManifestOptions.Strict) == 0)
                    throw;
                exception = e;
            }

            if ((flags & EventManifestOptions.Strict) != 0 && (manifest.Errors.Count > 0 || exception != null))
            {
                string msg = String.Empty;
                if (manifest.Errors.Count > 0)
                {
                    bool firstError = true;
                    foreach (string error in manifest.Errors)
                    {
                        if (!firstError)
                            msg += Environment.NewLine;
                        firstError = false;
                        msg += error;
                    }
                }
                else
                    msg = "Unexpected error: " + exception.Message;
                throw new ArgumentException(msg, exception);
            }

            return bNeedsManifest ? res : null;
        }

        private static bool RemoveFirstArgIfRelatedActivityId(ref ParameterInfo[] args)
        {
            if (args.Length > 0 && args[0].ParameterType == typeof (Guid) && string.Compare(args[0].Name, "relatedActivityId", StringComparison.OrdinalIgnoreCase) == 0)
            {
                var newargs = new ParameterInfo[args.Length - 1];
                Array.Copy(args, 1, newargs, 0, args.Length - 1);
                args = newargs;
                return true;
            }

            return false;
        }

        private static void AddProviderEnumKind(ManifestBuilder manifest, FieldInfo staticField, string providerEnumKind)
        {
            bool reflectionOnly = staticField.Module.Assembly.ReflectionOnly();
            Type staticFieldType = staticField.FieldType;
            if (!reflectionOnly && (staticFieldType == typeof (EventOpcode)) || AttributeTypeNamesMatch(staticFieldType, typeof (EventOpcode)))
            {
                if (providerEnumKind != "Opcodes")
                    goto Error;
                int value = (int)staticField.GetRawConstantValue();
                manifest.AddOpcode(staticField.Name, value);
            }
            else if (!reflectionOnly && (staticFieldType == typeof (EventTask)) || AttributeTypeNamesMatch(staticFieldType, typeof (EventTask)))
            {
                if (providerEnumKind != "Tasks")
                    goto Error;
                int value = (int)staticField.GetRawConstantValue();
                manifest.AddTask(staticField.Name, value);
            }
            else if (!reflectionOnly && (staticFieldType == typeof (EventKeywords)) || AttributeTypeNamesMatch(staticFieldType, typeof (EventKeywords)))
            {
                if (providerEnumKind != "Keywords")
                    goto Error;
                ulong value = unchecked ((ulong)(long)staticField.GetRawConstantValue());
                manifest.AddKeyword(staticField.Name, value);
            }

            return;
            Error:
                manifest.ManifestError(Resources.GetResourceString("EventSource_EnumKindMismatch", staticField.Name, staticField.FieldType.Name, providerEnumKind));
        }

        private static void AddEventDescriptor(ref EventMetadata[] eventData, string eventName, EventAttribute eventAttribute, ParameterInfo[] eventParameters, bool hasRelatedActivityID)
        {
            if (eventData == null || eventData.Length <= eventAttribute.EventId)
            {
                EventMetadata[] newValues = new EventMetadata[Math.Max(eventData.Length + 16, eventAttribute.EventId + 1)];
                Array.Copy(eventData, newValues, eventData.Length);
                eventData = newValues;
            }

            eventData[eventAttribute.EventId].Descriptor = new EventDescriptor(eventAttribute.EventId, eventAttribute.Version, (byte)eventAttribute.Channel, (byte)eventAttribute.Level, (byte)eventAttribute.Opcode, (int)eventAttribute.Task, unchecked ((long)((ulong)eventAttribute.Keywords | SessionMask.All.ToEventKeywords())));
            eventData[eventAttribute.EventId].Tags = eventAttribute.Tags;
            eventData[eventAttribute.EventId].Name = eventName;
            eventData[eventAttribute.EventId].Parameters = eventParameters;
            eventData[eventAttribute.EventId].Message = eventAttribute.Message;
            eventData[eventAttribute.EventId].ActivityOptions = eventAttribute.ActivityOptions;
            eventData[eventAttribute.EventId].HasRelatedActivityID = hasRelatedActivityID;
        }

        private static void TrimEventDescriptors(ref EventMetadata[] eventData)
        {
            int idx = eventData.Length;
            while (0 < idx)
            {
                --idx;
                if (eventData[idx].Descriptor.EventId != 0)
                    break;
            }

            if (eventData.Length - idx > 2)
            {
                EventMetadata[] newValues = new EventMetadata[idx + 1];
                Array.Copy(eventData, newValues, newValues.Length);
                eventData = newValues;
            }
        }

        internal void AddListener(EventListener listener)
        {
            lock (EventListener.EventListenersLock)
            {
                bool[] enabledArray = null;
                if (m_eventData != null)
                    enabledArray = new bool[m_eventData.Length];
                m_Dispatchers = new EventDispatcher(m_Dispatchers, enabledArray, listener);
                listener.OnEventSourceCreated(this);
            }
        }

        private static void DebugCheckEvent(ref Dictionary<string, string> eventsByName, EventMetadata[] eventData, MethodInfo method, EventAttribute eventAttribute, ManifestBuilder manifest, EventManifestOptions options)
        {
            int evtId = eventAttribute.EventId;
            string evtName = method.Name;
            int eventArg = GetHelperCallFirstArg(method);
            if (eventArg >= 0 && evtId != eventArg)
            {
                manifest.ManifestError(Resources.GetResourceString("EventSource_MismatchIdToWriteEvent", evtName, evtId, eventArg), true);
            }

            if (evtId < eventData.Length && eventData[evtId].Descriptor.EventId != 0)
            {
                manifest.ManifestError(Resources.GetResourceString("EventSource_EventIdReused", evtName, evtId, eventData[evtId].Name), true);
            }

            Contract.Assert(eventAttribute.Task != EventTask.None || eventAttribute.Opcode != EventOpcode.Info);
            for (int idx = 0; idx < eventData.Length; ++idx)
            {
                if (eventData[idx].Name == null)
                    continue;
                if (eventData[idx].Descriptor.Task == (int)eventAttribute.Task && eventData[idx].Descriptor.Opcode == (int)eventAttribute.Opcode)
                {
                    manifest.ManifestError(Resources.GetResourceString("EventSource_TaskOpcodePairReused", evtName, evtId, eventData[idx].Name, idx));
                    if ((options & EventManifestOptions.Strict) == 0)
                        break;
                }
            }

            if (eventAttribute.Opcode != EventOpcode.Info)
            {
                bool failure = false;
                if (eventAttribute.Task == EventTask.None)
                    failure = true;
                else
                {
                    var autoAssignedTask = (EventTask)(0xFFFE - evtId);
                    if ((eventAttribute.Opcode != EventOpcode.Start && eventAttribute.Opcode != EventOpcode.Stop) && eventAttribute.Task == autoAssignedTask)
                        failure = true;
                }

                if (failure)
                {
                    manifest.ManifestError(Resources.GetResourceString("EventSource_EventMustHaveTaskIfNonDefaultOpcode", evtName, evtId));
                }
            }

            if (eventsByName == null)
                eventsByName = new Dictionary<string, string>();
            if (eventsByName.ContainsKey(evtName))
            {
                manifest.ManifestError(Resources.GetResourceString("EventSource_EventNameReused", evtName), true);
            }

            eventsByName[evtName] = evtName;
        }

        static private int GetHelperCallFirstArg(MethodInfo method)
        {
            (new ReflectionPermission(ReflectionPermissionFlag.MemberAccess)).Assert();
            byte[] instrs = method.GetMethodBody().GetILAsByteArray();
            int retVal = -1;
            for (int idx = 0; idx < instrs.Length;)
            {
                switch (instrs[idx])
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                        break;
                    case 14:
                    case 16:
                        idx++;
                        break;
                    case 20:
                        break;
                    case 21:
                    case 22:
                    case 23:
                    case 24:
                    case 25:
                    case 26:
                    case 27:
                    case 28:
                    case 29:
                    case 30:
                        if (idx > 0 && instrs[idx - 1] == 2)
                            retVal = instrs[idx] - 22;
                        break;
                    case 31:
                        if (idx > 0 && instrs[idx - 1] == 2)
                            retVal = instrs[idx + 1];
                        idx++;
                        break;
                    case 32:
                        idx += 4;
                        break;
                    case 37:
                        break;
                    case 40:
                        idx += 4;
                        if (retVal >= 0)
                        {
                            for (int search = idx + 1; search < instrs.Length; search++)
                            {
                                if (instrs[search] == 42)
                                    return retVal;
                                if (instrs[search] != 0)
                                    break;
                            }
                        }

                        retVal = -1;
                        break;
                    case 44:
                    case 45:
                        retVal = -1;
                        idx++;
                        break;
                    case 57:
                    case 58:
                        retVal = -1;
                        idx += 4;
                        break;
                    case 103:
                    case 104:
                    case 105:
                    case 106:
                    case 109:
                    case 110:
                        break;
                    case 140:
                    case 141:
                        idx += 4;
                        break;
                    case 162:
                        break;
                    case 254:
                        idx++;
                        if (idx >= instrs.Length || instrs[idx] >= 6)
                            goto default;
                        break;
                    default:
                        return -1;
                }

                idx++;
            }

            return -1;
        }

        internal void ReportOutOfBandMessage(string msg, bool flush)
        {
            try
            {
                System.Diagnostics.Debugger.Log(0, null, String.Format("EventSource Error: {0}{1}", msg, Environment.NewLine));
                if (m_outOfBandMessageCount < 16 - 1)
                    m_outOfBandMessageCount++;
                else
                {
                    if (m_outOfBandMessageCount == 16)
                        return;
                    m_outOfBandMessageCount = 16;
                    msg = "Reached message limit.   End of EventSource error messages.";
                }

                WriteEventString(EventLevel.LogAlways, -1, msg);
                WriteStringToAllListeners("EventSourceMessage", msg);
            }
            catch (Exception)
            {
            }
        }

        private EventSourceSettings ValidateSettings(EventSourceSettings settings)
        {
            var evtFormatMask = EventSourceSettings.EtwManifestEventFormat | EventSourceSettings.EtwSelfDescribingEventFormat;
            if ((settings & evtFormatMask) == evtFormatMask)
            {
                throw new ArgumentException(Resources.GetResourceString("EventSource_InvalidEventFormat"), "settings");
            }

            if ((settings & evtFormatMask) == 0)
                settings |= EventSourceSettings.EtwSelfDescribingEventFormat;
            return settings;
        }

        private bool ThrowOnEventWriteErrors
        {
            get
            {
                return (m_config & EventSourceSettings.ThrowOnEventWriteErrors) != 0;
            }

            set
            {
                if (value)
                    m_config |= EventSourceSettings.ThrowOnEventWriteErrors;
                else
                    m_config &= ~EventSourceSettings.ThrowOnEventWriteErrors;
            }
        }

        private bool SelfDescribingEvents
        {
            get
            {
                Contract.Assert(((m_config & EventSourceSettings.EtwManifestEventFormat) != 0) != ((m_config & EventSourceSettings.EtwSelfDescribingEventFormat) != 0));
                return (m_config & EventSourceSettings.EtwSelfDescribingEventFormat) != 0;
            }

            set
            {
                if (!value)
                {
                    m_config |= EventSourceSettings.EtwManifestEventFormat;
                    m_config &= ~EventSourceSettings.EtwSelfDescribingEventFormat;
                }
                else
                {
                    m_config |= EventSourceSettings.EtwSelfDescribingEventFormat;
                    m_config &= ~EventSourceSettings.EtwManifestEventFormat;
                }
            }
        }

        private void ReportActivitySamplingInfo(EventListener listener, SessionMask sessions)
        {
            Contract.Assert(listener == null || (uint)sessions == (uint)SessionMask.FromId(0));
            for (int perEventSourceSessionId = 0; perEventSourceSessionId < SessionMask.MAX; ++perEventSourceSessionId)
            {
                if (!sessions[perEventSourceSessionId])
                    continue;
                ActivityFilter af;
                if (listener == null)
                {
                    EtwSession etwSession = m_etwSessionIdMap[perEventSourceSessionId];
                    Contract.Assert(etwSession != null);
                    af = etwSession.m_activityFilter;
                }
                else
                {
                    af = listener.m_activityFilter;
                }

                if (af == null)
                    continue;
                SessionMask m = new SessionMask();
                m[perEventSourceSessionId] = true;
                foreach (var t in af.GetFilterAsTuple(m_guid))
                {
                    WriteStringToListener(listener, string.Format(CultureInfo.InvariantCulture, "Session {0}: {1} = {2}", perEventSourceSessionId, t.Item1, t.Item2), m);
                }

                bool participateInSampling = (listener == null) ? m_activityFilteringForETWEnabled[perEventSourceSessionId] : GetDispatcher(listener).m_activityFilteringEnabled;
                WriteStringToListener(listener, string.Format(CultureInfo.InvariantCulture, "Session {0}: Activity Sampling support: {1}", perEventSourceSessionId, participateInSampling ? "enabled" : "disabled"), m);
            }
        }

        private string m_name;
        internal int m_id;
        private Guid m_guid;
        internal volatile EventMetadata[] m_eventData;
        private volatile byte[] m_rawManifest;
        private EventHandler<EventCommandEventArgs> m_eventCommandExecuted;
        private EventSourceSettings m_config;
        private bool m_eventSourceDisposed;
        private bool m_eventSourceEnabled;
        internal EventLevel m_level;
        internal EventKeywords m_matchAnyKeyword;
        internal volatile EventDispatcher m_Dispatchers;
        private volatile OverideEventProvider m_provider;
        private bool m_completelyInited;
        private Exception m_constructionException;
        private byte m_outOfBandMessageCount;
        private EventCommandEventArgs m_deferredCommands;
        private string[] m_traits;
        internal static uint s_currentPid;
        private static byte m_EventSourceExceptionRecurenceCount = 0;
        private static bool m_EventSourceInDecodeObject = false;
        internal volatile ulong[] m_channelData;
        private SessionMask m_curLiveSessions;
        private EtwSession[] m_etwSessionIdMap;
        private List<EtwSession> m_legacySessions;
        internal long m_keywordTriggers;
        internal SessionMask m_activityFilteringForETWEnabled;
        static internal Action<Guid> s_activityDying;
        ActivityTracker m_activityTracker;
        internal const string s_ActivityStartSuffix = "Start";
        internal const string s_ActivityStopSuffix = "Stop";
        private static readonly byte[] namespaceBytes = new byte[]{0x48, 0x2C, 0x2D, 0xB2, 0xC3, 0x90, 0x47, 0xC8, 0x87, 0xF8, 0x1A, 0x15, 0xBF, 0xC1, 0x30, 0xFB, };
    }

    [Flags]
    public enum EventSourceSettings
    {
        Default = 0,
        ThrowOnEventWriteErrors = 1,
        EtwManifestEventFormat = 4,
        EtwSelfDescribingEventFormat = 8
    }

    public class EventListener : IDisposable
    {
        private event EventHandler<EventSourceCreatedEventArgs> _EventSourceCreated;
        public event EventHandler<EventSourceCreatedEventArgs> EventSourceCreated
        {
            add
            {
                CallBackForExistingEventSources(false, value);
                this._EventSourceCreated = (EventHandler<EventSourceCreatedEventArgs>)Delegate.Combine(_EventSourceCreated, value);
            }

            remove
            {
                this._EventSourceCreated = (EventHandler<EventSourceCreatedEventArgs>)Delegate.Remove(_EventSourceCreated, value);
            }
        }

        public event EventHandler<EventWrittenEventArgs> EventWritten;
        public EventListener()
        {
            CallBackForExistingEventSources(true, (obj, args) => args.EventSource.AddListener(this));
        }

        public virtual void Dispose()
        {
            lock (EventListenersLock)
            {
                Contract.Assert(s_Listeners != null);
                if (s_Listeners != null)
                {
                    if (this == s_Listeners)
                    {
                        EventListener cur = s_Listeners;
                        s_Listeners = this.m_Next;
                        RemoveReferencesToListenerInEventSources(cur);
                    }
                    else
                    {
                        EventListener prev = s_Listeners;
                        for (;;)
                        {
                            EventListener cur = prev.m_Next;
                            if (cur == null)
                                break;
                            if (cur == this)
                            {
                                prev.m_Next = cur.m_Next;
                                RemoveReferencesToListenerInEventSources(cur);
                                break;
                            }

                            prev = cur;
                        }
                    }
                }

                Validate();
            }
        }

        public void EnableEvents(EventSource eventSource, EventLevel level)
        {
            EnableEvents(eventSource, level, EventKeywords.None);
        }

        public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword)
        {
            EnableEvents(eventSource, level, matchAnyKeyword, null);
        }

        public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> arguments)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException("eventSource");
            }

            Contract.EndContractBlock();
            eventSource.SendCommand(this, 0, 0, EventCommand.Update, true, level, matchAnyKeyword, arguments);
        }

        public void DisableEvents(EventSource eventSource)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException("eventSource");
            }

            Contract.EndContractBlock();
            eventSource.SendCommand(this, 0, 0, EventCommand.Update, false, EventLevel.LogAlways, EventKeywords.None, null);
        }

        public static int EventSourceIndex(EventSource eventSource)
        {
            return eventSource.m_id;
        }

        internal protected virtual void OnEventSourceCreated(EventSource eventSource)
        {
            EventHandler<EventSourceCreatedEventArgs> callBack = this._EventSourceCreated;
            if (callBack != null)
            {
                EventSourceCreatedEventArgs args = new EventSourceCreatedEventArgs();
                args.EventSource = eventSource;
                callBack(this, args);
            }
        }

        internal protected virtual void OnEventWritten(EventWrittenEventArgs eventData)
        {
            EventHandler<EventWrittenEventArgs> callBack = this.EventWritten;
            if (callBack != null)
            {
                callBack(this, eventData);
            }
        }

        internal static void AddEventSource(EventSource newEventSource)
        {
            lock (EventListenersLock)
            {
                if (s_EventSources == null)
                    s_EventSources = new List<WeakReference>(2);
                if (!s_EventSourceShutdownRegistered)
                {
                    s_EventSourceShutdownRegistered = true;
                }

                int newIndex = -1;
                if (s_EventSources.Count % 64 == 63)
                {
                    int i = s_EventSources.Count;
                    while (0 < i)
                    {
                        --i;
                        WeakReference weakRef = s_EventSources[i];
                        if (!weakRef.IsAlive)
                        {
                            newIndex = i;
                            weakRef.Target = newEventSource;
                            break;
                        }
                    }
                }

                if (newIndex < 0)
                {
                    newIndex = s_EventSources.Count;
                    s_EventSources.Add(new WeakReference(newEventSource));
                }

                newEventSource.m_id = newIndex;
                for (EventListener listener = s_Listeners; listener != null; listener = listener.m_Next)
                    newEventSource.AddListener(listener);
                Validate();
            }
        }

        private static void DisposeOnShutdown(object sender, EventArgs e)
        {
            lock (EventListenersLock)
            {
                foreach (var esRef in s_EventSources)
                {
                    EventSource es = esRef.Target as EventSource;
                    if (es != null)
                        es.Dispose();
                }
            }
        }

        private static void RemoveReferencesToListenerInEventSources(EventListener listenerToRemove)
        {
            Contract.Assert(Monitor.IsEntered(EventListener.EventListenersLock));
            foreach (WeakReference eventSourceRef in s_EventSources)
            {
                EventSource eventSource = eventSourceRef.Target as EventSource;
                if (eventSource != null)
                {
                    if (eventSource.m_Dispatchers.m_Listener == listenerToRemove)
                        eventSource.m_Dispatchers = eventSource.m_Dispatchers.m_Next;
                    else
                    {
                        EventDispatcher prev = eventSource.m_Dispatchers;
                        for (;;)
                        {
                            EventDispatcher cur = prev.m_Next;
                            if (cur == null)
                            {
                                Contract.Assert(false, "EventSource did not have a registered EventListener!");
                                break;
                            }

                            if (cur.m_Listener == listenerToRemove)
                            {
                                prev.m_Next = cur.m_Next;
                                break;
                            }

                            prev = cur;
                        }
                    }
                }
            }
        }

        internal static void Validate()
        {
            lock (EventListenersLock)
            {
                Dictionary<EventListener, bool> allListeners = new Dictionary<EventListener, bool>();
                EventListener cur = s_Listeners;
                while (cur != null)
                {
                    allListeners.Add(cur, true);
                    cur = cur.m_Next;
                }

                int id = -1;
                foreach (WeakReference eventSourceRef in s_EventSources)
                {
                    id++;
                    EventSource eventSource = eventSourceRef.Target as EventSource;
                    if (eventSource == null)
                        continue;
                    Contract.Assert(eventSource.m_id == id, "Unexpected event source ID.");
                    EventDispatcher dispatcher = eventSource.m_Dispatchers;
                    while (dispatcher != null)
                    {
                        Contract.Assert(allListeners.ContainsKey(dispatcher.m_Listener), "EventSource has a listener not on the global list.");
                        dispatcher = dispatcher.m_Next;
                    }

                    foreach (EventListener listener in allListeners.Keys)
                    {
                        dispatcher = eventSource.m_Dispatchers;
                        for (;;)
                        {
                            Contract.Assert(dispatcher != null, "Listener is not on all eventSources.");
                            if (dispatcher.m_Listener == listener)
                                break;
                            dispatcher = dispatcher.m_Next;
                        }
                    }
                }
            }
        }

        internal static object EventListenersLock
        {
            get
            {
                if (s_EventSources == null)
                    Interlocked.CompareExchange(ref s_EventSources, new List<WeakReference>(2), null);
                return s_EventSources;
            }
        }

        private void CallBackForExistingEventSources(bool addToListenersList, EventHandler<EventSourceCreatedEventArgs> callback)
        {
            lock (EventListenersLock)
            {
                if (s_CreatingListener)
                {
                    throw new InvalidOperationException(Resources.GetResourceString("EventSource_ListenerCreatedInsideCallback"));
                }

                try
                {
                    s_CreatingListener = true;
                    if (addToListenersList)
                    {
                        this.m_Next = s_Listeners;
                        s_Listeners = this;
                    }

                    WeakReference[] eventSourcesSnapshot = s_EventSources.ToArray();
                    for (int i = 0; i < eventSourcesSnapshot.Length; i++)
                    {
                        WeakReference eventSourceRef = eventSourcesSnapshot[i];
                        EventSource eventSource = eventSourceRef.Target as EventSource;
                        if (eventSource != null)
                        {
                            EventSourceCreatedEventArgs args = new EventSourceCreatedEventArgs();
                            args.EventSource = eventSource;
                            callback(this, args);
                        }
                    }

                    Validate();
                }
                finally
                {
                    s_CreatingListener = false;
                }
            }
        }

        internal volatile EventListener m_Next;
        internal ActivityFilter m_activityFilter;
        internal static EventListener s_Listeners;
        internal static List<WeakReference> s_EventSources;
        private static bool s_CreatingListener = false;
        private static bool s_EventSourceShutdownRegistered = false;
    }

    public class EventCommandEventArgs : EventArgs
    {
        public EventCommand Command
        {
            get;
            internal set;
        }

        public IDictionary<String, String> Arguments
        {
            get;
            internal set;
        }

        public bool EnableEvent(int eventId)
        {
            if (Command != EventCommand.Enable && Command != EventCommand.Disable)
                throw new InvalidOperationException();
            return eventSource.EnableEventForDispatcher(dispatcher, eventId, true);
        }

        public bool DisableEvent(int eventId)
        {
            if (Command != EventCommand.Enable && Command != EventCommand.Disable)
                throw new InvalidOperationException();
            return eventSource.EnableEventForDispatcher(dispatcher, eventId, false);
        }

        internal EventCommandEventArgs(EventCommand command, IDictionary<string, string> arguments, EventSource eventSource, EventListener listener, int perEventSourceSessionId, int etwSessionId, bool enable, EventLevel level, EventKeywords matchAnyKeyword)
        {
            this.Command = command;
            this.Arguments = arguments;
            this.eventSource = eventSource;
            this.listener = listener;
            this.perEventSourceSessionId = perEventSourceSessionId;
            this.etwSessionId = etwSessionId;
            this.enable = enable;
            this.level = level;
            this.matchAnyKeyword = matchAnyKeyword;
        }

        internal EventSource eventSource;
        internal EventDispatcher dispatcher;
        internal EventListener listener;
        internal int perEventSourceSessionId;
        internal int etwSessionId;
        internal bool enable;
        internal EventLevel level;
        internal EventKeywords matchAnyKeyword;
        internal EventCommandEventArgs nextCommand;
    }

    public class EventSourceCreatedEventArgs : EventArgs
    {
        public EventSource EventSource
        {
            get;
            internal set;
        }
    }

    public class EventWrittenEventArgs : EventArgs
    {
        public string EventName
        {
            get
            {
                if (m_eventName != null || EventId < 0)
                {
                    return m_eventName;
                }
                else
                    return m_eventSource.m_eventData[EventId].Name;
            }

            internal set
            {
                m_eventName = value;
            }
        }

        public int EventId
        {
            get;
            internal set;
        }

        public Guid ActivityId
        {
            [System.Security.SecurityCritical]
            get
            {
                return EventSource.CurrentThreadActivityId;
            }
        }

        public Guid RelatedActivityId
        {
            [System.Security.SecurityCritical]
            get;
            internal set;
        }

        public ReadOnlyCollection<Object> Payload
        {
            get;
            internal set;
        }

        public ReadOnlyCollection<string> PayloadNames
        {
            get
            {
                if (m_payloadNames == null)
                {
                    Contract.Assert(EventId != -1);
                    var names = new List<string>();
                    foreach (var parameter in m_eventSource.m_eventData[EventId].Parameters)
                    {
                        names.Add(parameter.Name);
                    }

                    m_payloadNames = new ReadOnlyCollection<string>(names);
                }

                return m_payloadNames;
            }

            internal set
            {
                m_payloadNames = value;
            }
        }

        public EventSource EventSource
        {
            get
            {
                return m_eventSource;
            }
        }

        public EventKeywords Keywords
        {
            get
            {
                if (EventId < 0)
                    return m_keywords;
                return (EventKeywords)m_eventSource.m_eventData[EventId].Descriptor.Keywords;
            }
        }

        public EventOpcode Opcode
        {
            get
            {
                if (EventId < 0)
                    return m_opcode;
                return (EventOpcode)m_eventSource.m_eventData[EventId].Descriptor.Opcode;
            }
        }

        public EventTask Task
        {
            get
            {
                if (EventId < 0)
                    return EventTask.None;
                return (EventTask)m_eventSource.m_eventData[EventId].Descriptor.Task;
            }
        }

        public EventTags Tags
        {
            get
            {
                if (EventId < 0)
                    return m_tags;
                return m_eventSource.m_eventData[EventId].Tags;
            }
        }

        public string Message
        {
            get
            {
                if (EventId < 0)
                    return m_message;
                else
                    return m_eventSource.m_eventData[EventId].Message;
            }

            internal set
            {
                m_message = value;
            }
        }

        public EventChannel Channel
        {
            get
            {
                if (EventId < 0)
                    return EventChannel.None;
                return (EventChannel)m_eventSource.m_eventData[EventId].Descriptor.Channel;
            }
        }

        public byte Version
        {
            get
            {
                if (EventId < 0)
                    return 0;
                return m_eventSource.m_eventData[EventId].Descriptor.Version;
            }
        }

        public EventLevel Level
        {
            get
            {
                if (EventId < 0)
                    return m_level;
                return (EventLevel)m_eventSource.m_eventData[EventId].Descriptor.Level;
            }
        }

        internal EventWrittenEventArgs(EventSource eventSource)
        {
            m_eventSource = eventSource;
        }

        private string m_message;
        private string m_eventName;
        private EventSource m_eventSource;
        private ReadOnlyCollection<string> m_payloadNames;
        internal EventTags m_tags;
        internal EventOpcode m_opcode;
        internal EventLevel m_level;
        internal EventKeywords m_keywords;
    }

    public sealed class EventSourceAttribute : Attribute
    {
        public string Name
        {
            get;
            set;
        }

        public string Guid
        {
            get;
            set;
        }

        public string LocalizationResources
        {
            get;
            set;
        }
    }

    public sealed class EventAttribute : Attribute
    {
        public EventAttribute(int eventId)
        {
            this.EventId = eventId;
            Level = EventLevel.Informational;
            this.m_opcodeSet = false;
        }

        public int EventId
        {
            get;
            private set;
        }

        public EventLevel Level
        {
            get;
            set;
        }

        public EventKeywords Keywords
        {
            get;
            set;
        }

        public EventOpcode Opcode
        {
            get
            {
                return m_opcode;
            }

            set
            {
                this.m_opcode = value;
                this.m_opcodeSet = true;
            }
        }

        internal bool IsOpcodeSet
        {
            get
            {
                return m_opcodeSet;
            }
        }

        public EventTask Task
        {
            get;
            set;
        }

        public EventChannel Channel
        {
            get;
            set;
        }

        public byte Version
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public EventTags Tags
        {
            get;
            set;
        }

        public EventActivityOptions ActivityOptions
        {
            get;
            set;
        }

        EventOpcode m_opcode;
        private bool m_opcodeSet;
    }

    public sealed class NonEventAttribute : Attribute
    {
        public NonEventAttribute()
        {
        }
    }

    class EventChannelAttribute : Attribute
    {
        public bool Enabled
        {
            get;
            set;
        }

        public EventChannelType EventChannelType
        {
            get;
            set;
        }
    }

    enum EventChannelType
    {
        Admin = 1,
        Operational,
        Analytic,
        Debug
    }

    public enum EventCommand
    {
        Update = 0,
        SendManifest = -1,
        Enable = -2,
        Disable = -3
    }

    ;
    internal sealed class ActivityFilter : IDisposable
    {
        public static void DisableFilter(ref ActivityFilter filterList, EventSource source)
        {
            Contract.Assert(Monitor.IsEntered(EventListener.EventListenersLock));
            if (filterList == null)
                return;
            ActivityFilter cur;
            ActivityFilter prev = filterList;
            cur = prev.m_next;
            while (cur != null)
            {
                if (cur.m_providerGuid == source.Guid)
                {
                    if (cur.m_eventId >= 0 && cur.m_eventId < source.m_eventData.Length)
                        --source.m_eventData[cur.m_eventId].TriggersActivityTracking;
                    prev.m_next = cur.m_next;
                    cur.Dispose();
                    cur = prev.m_next;
                }
                else
                {
                    prev = cur;
                    cur = prev.m_next;
                }
            }

            if (filterList.m_providerGuid == source.Guid)
            {
                if (filterList.m_eventId >= 0 && filterList.m_eventId < source.m_eventData.Length)
                    --source.m_eventData[filterList.m_eventId].TriggersActivityTracking;
                var first = filterList;
                filterList = first.m_next;
                first.Dispose();
            }

            if (filterList != null)
            {
                EnsureActivityCleanupDelegate(filterList);
            }
        }

        public static void UpdateFilter(ref ActivityFilter filterList, EventSource source, int perEventSourceSessionId, string startEvents)
        {
            Contract.Assert(Monitor.IsEntered(EventListener.EventListenersLock));
            DisableFilter(ref filterList, source);
            if (!string.IsNullOrEmpty(startEvents))
            {
                string[] activityFilterStrings = startEvents.Split(' ');
                for (int i = 0; i < activityFilterStrings.Length; ++i)
                {
                    string activityFilterString = activityFilterStrings[i];
                    int sampleFreq = 1;
                    int eventId = -1;
                    int colonIdx = activityFilterString.IndexOf(':');
                    if (colonIdx < 0)
                    {
                        source.ReportOutOfBandMessage("ERROR: Invalid ActivitySamplingStartEvent specification: " + activityFilterString, false);
                        continue;
                    }

                    string sFreq = activityFilterString.Substring(colonIdx + 1);
                    if (!int.TryParse(sFreq, out sampleFreq))
                    {
                        source.ReportOutOfBandMessage("ERROR: Invalid sampling frequency specification: " + sFreq, false);
                        continue;
                    }

                    activityFilterString = activityFilterString.Substring(0, colonIdx);
                    if (!int.TryParse(activityFilterString, out eventId))
                    {
                        eventId = -1;
                        for (int j = 0; j < source.m_eventData.Length; j++)
                        {
                            EventSource.EventMetadata[] ed = source.m_eventData;
                            if (ed[j].Name != null && ed[j].Name.Length == activityFilterString.Length && string.Compare(ed[j].Name, activityFilterString, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                eventId = ed[j].Descriptor.EventId;
                                break;
                            }
                        }
                    }

                    if (eventId < 0 || eventId >= source.m_eventData.Length)
                    {
                        source.ReportOutOfBandMessage("ERROR: Invalid eventId specification: " + activityFilterString, false);
                        continue;
                    }

                    EnableFilter(ref filterList, source, perEventSourceSessionId, eventId, sampleFreq);
                }
            }
        }

        public static ActivityFilter GetFilter(ActivityFilter filterList, EventSource source)
        {
            for (var af = filterList; af != null; af = af.m_next)
            {
                if (af.m_providerGuid == source.Guid && af.m_samplingFreq != -1)
                    return af;
            }

            return null;
        }

        unsafe public static bool PassesActivityFilter(ActivityFilter filterList, Guid*childActivityID, bool triggeringEvent, EventSource source, int eventId)
        {
            Contract.Assert(filterList != null && filterList.m_activeActivities != null);
            bool shouldBeLogged = false;
            if (triggeringEvent)
            {
                for (ActivityFilter af = filterList; af != null; af = af.m_next)
                {
                    if (eventId == af.m_eventId && source.Guid == af.m_providerGuid)
                    {
                        int curSampleCount, newSampleCount;
                        do
                        {
                            curSampleCount = af.m_curSampleCount;
                            if (curSampleCount <= 1)
                                newSampleCount = af.m_samplingFreq;
                            else
                                newSampleCount = curSampleCount - 1;
                        }
                        while (Interlocked.CompareExchange(ref af.m_curSampleCount, newSampleCount, curSampleCount) != curSampleCount);
                        if (curSampleCount <= 1)
                        {
                            Guid currentActivityId = EventSource.InternalCurrentThreadActivityId;
                            Tuple<Guid, int> startId;
                            if (!af.m_rootActiveActivities.TryGetValue(currentActivityId, out startId))
                            {
                                shouldBeLogged = true;
                                af.m_activeActivities[currentActivityId] = Environment.TickCount;
                                af.m_rootActiveActivities[currentActivityId] = Tuple.Create(source.Guid, eventId);
                            }
                        }
                        else
                        {
                            Guid currentActivityId = EventSource.InternalCurrentThreadActivityId;
                            Tuple<Guid, int> startId;
                            if (af.m_rootActiveActivities.TryGetValue(currentActivityId, out startId) && startId.Item1 == source.Guid && startId.Item2 == eventId)
                            {
                                int dummy;
                                af.m_activeActivities.TryRemove(currentActivityId, out dummy);
                            }
                        }

                        break;
                    }
                }
            }

            var activeActivities = GetActiveActivities(filterList);
            if (activeActivities != null)
            {
                if (!shouldBeLogged)
                {
                    shouldBeLogged = !activeActivities.IsEmpty && activeActivities.ContainsKey(EventSource.InternalCurrentThreadActivityId);
                }

                if (shouldBeLogged && childActivityID != null && ((EventOpcode)source.m_eventData[eventId].Descriptor.Opcode == EventOpcode.Send))
                {
                    FlowActivityIfNeeded(filterList, null, childActivityID);
                }
            }

            return shouldBeLogged;
        }

        public static bool IsCurrentActivityActive(ActivityFilter filterList)
        {
            var activeActivities = GetActiveActivities(filterList);
            if (activeActivities != null && activeActivities.ContainsKey(EventSource.InternalCurrentThreadActivityId))
                return true;
            return false;
        }

        unsafe public static void FlowActivityIfNeeded(ActivityFilter filterList, Guid*currentActivityId, Guid*childActivityID)
        {
            Contract.Assert(childActivityID != null);
            var activeActivities = GetActiveActivities(filterList);
            Contract.Assert(activeActivities != null);
            if (currentActivityId != null && !activeActivities.ContainsKey(*currentActivityId))
                return;
            if (activeActivities.Count > MaxActivityTrackCount)
            {
                TrimActiveActivityStore(activeActivities);
                activeActivities[EventSource.InternalCurrentThreadActivityId] = Environment.TickCount;
            }

            activeActivities[*childActivityID] = Environment.TickCount;
        }

        public static void UpdateKwdTriggers(ActivityFilter activityFilter, Guid sourceGuid, EventSource source, EventKeywords sessKeywords)
        {
            for (var af = activityFilter; af != null; af = af.m_next)
            {
                if ((sourceGuid == af.m_providerGuid) && (source.m_eventData[af.m_eventId].TriggersActivityTracking > 0 || ((EventOpcode)source.m_eventData[af.m_eventId].Descriptor.Opcode == EventOpcode.Send)))
                {
                    unchecked
                    {
                        source.m_keywordTriggers |= (source.m_eventData[af.m_eventId].Descriptor.Keywords & (long)sessKeywords);
                    }
                }
            }
        }

        public IEnumerable<Tuple<int, int>> GetFilterAsTuple(Guid sourceGuid)
        {
            for (ActivityFilter af = this; af != null; af = af.m_next)
            {
                if (af.m_providerGuid == sourceGuid)
                    yield return Tuple.Create(af.m_eventId, af.m_samplingFreq);
            }
        }

        public void Dispose()
        {
            Contract.Assert(Monitor.IsEntered(EventListener.EventListenersLock));
            if (m_myActivityDelegate != null)
            {
                EventSource.s_activityDying = (Action<Guid>)Delegate.Remove(EventSource.s_activityDying, m_myActivityDelegate);
                m_myActivityDelegate = null;
            }
        }

        private ActivityFilter(EventSource source, int perEventSourceSessionId, int eventId, int samplingFreq, ActivityFilter existingFilter = null)
        {
            m_providerGuid = source.Guid;
            m_perEventSourceSessionId = perEventSourceSessionId;
            m_eventId = eventId;
            m_samplingFreq = samplingFreq;
            m_next = existingFilter;
            Contract.Assert(existingFilter == null || (existingFilter.m_activeActivities == null) == (existingFilter.m_rootActiveActivities == null));
            ConcurrentDictionary<Guid, int> activeActivities = null;
            if (existingFilter == null || (activeActivities = GetActiveActivities(existingFilter)) == null)
            {
                m_activeActivities = new ConcurrentDictionary<Guid, int>();
                m_rootActiveActivities = new ConcurrentDictionary<Guid, Tuple<Guid, int>>();
                m_myActivityDelegate = GetActivityDyingDelegate(this);
                EventSource.s_activityDying = (Action<Guid>)Delegate.Combine(EventSource.s_activityDying, m_myActivityDelegate);
            }
            else
            {
                m_activeActivities = activeActivities;
                m_rootActiveActivities = existingFilter.m_rootActiveActivities;
            }
        }

        private static void EnsureActivityCleanupDelegate(ActivityFilter filterList)
        {
            if (filterList == null)
                return;
            for (ActivityFilter af = filterList; af != null; af = af.m_next)
            {
                if (af.m_myActivityDelegate != null)
                    return;
            }

            filterList.m_myActivityDelegate = GetActivityDyingDelegate(filterList);
            EventSource.s_activityDying = (Action<Guid>)Delegate.Combine(EventSource.s_activityDying, filterList.m_myActivityDelegate);
        }

        private static Action<Guid> GetActivityDyingDelegate(ActivityFilter filterList)
        {
            return (Guid oldActivity) =>
            {
                int dummy;
                filterList.m_activeActivities.TryRemove(oldActivity, out dummy);
                Tuple<Guid, int> dummyTuple;
                filterList.m_rootActiveActivities.TryRemove(oldActivity, out dummyTuple);
            }

            ;
        }

        private static bool EnableFilter(ref ActivityFilter filterList, EventSource source, int perEventSourceSessionId, int eventId, int samplingFreq)
        {
            Contract.Assert(Monitor.IsEntered(EventListener.EventListenersLock));
            Contract.Assert(samplingFreq > 0);
            Contract.Assert(eventId >= 0);
            filterList = new ActivityFilter(source, perEventSourceSessionId, eventId, samplingFreq, filterList);
            if (0 <= eventId && eventId < source.m_eventData.Length)
                ++source.m_eventData[eventId].TriggersActivityTracking;
            return true;
        }

        private static void TrimActiveActivityStore(ConcurrentDictionary<Guid, int> activities)
        {
            if (activities.Count > MaxActivityTrackCount)
            {
                var keyValues = activities.ToArray();
                var tickNow = Environment.TickCount;
                Array.Sort(keyValues, (x, y) => (0x7FFFFFFF & (tickNow - y.Value)) - (0x7FFFFFFF & (tickNow - x.Value)));
                for (int i = 0; i < keyValues.Length / 2; i++)
                {
                    int dummy;
                    activities.TryRemove(keyValues[i].Key, out dummy);
                }
            }
        }

        private static ConcurrentDictionary<Guid, int> GetActiveActivities(ActivityFilter filterList)
        {
            for (ActivityFilter af = filterList; af != null; af = af.m_next)
            {
                if (af.m_activeActivities != null)
                    return af.m_activeActivities;
            }

            return null;
        }

        ConcurrentDictionary<Guid, int> m_activeActivities;
        ConcurrentDictionary<Guid, Tuple<Guid, int>> m_rootActiveActivities;
        Guid m_providerGuid;
        int m_eventId;
        int m_samplingFreq;
        int m_curSampleCount;
        int m_perEventSourceSessionId;
        const int MaxActivityTrackCount = 100000;
        ActivityFilter m_next;
        Action<Guid> m_myActivityDelegate;
    }

    ;
    internal class EtwSession
    {
        public static EtwSession GetEtwSession(int etwSessionId, bool bCreateIfNeeded = false)
        {
            if (etwSessionId < 0)
                return null;
            EtwSession etwSession;
            foreach (var wrEtwSession in s_etwSessions)
            {
                if (wrEtwSession.TryGetTarget(out etwSession) && etwSession.m_etwSessionId == etwSessionId)
                    return etwSession;
            }

            if (!bCreateIfNeeded)
                return null;
            if (s_etwSessions == null)
                s_etwSessions = new List<WeakReference<EtwSession>>();
            etwSession = new EtwSession(etwSessionId);
            s_etwSessions.Add(new WeakReference<EtwSession>(etwSession));
            if (s_etwSessions.Count > s_thrSessionCount)
                TrimGlobalList();
            return etwSession;
        }

        public static void RemoveEtwSession(EtwSession etwSession)
        {
            Contract.Assert(etwSession != null);
            if (s_etwSessions == null || etwSession == null)
                return;
            s_etwSessions.RemoveAll((wrEtwSession) =>
            {
                EtwSession session;
                return wrEtwSession.TryGetTarget(out session) && (session.m_etwSessionId == etwSession.m_etwSessionId);
            }

            );
            if (s_etwSessions.Count > s_thrSessionCount)
                TrimGlobalList();
        }

        private static void TrimGlobalList()
        {
            if (s_etwSessions == null)
                return;
            s_etwSessions.RemoveAll((wrEtwSession) =>
            {
                EtwSession session;
                return !wrEtwSession.TryGetTarget(out session);
            }

            );
        }

        private EtwSession(int etwSessionId)
        {
            m_etwSessionId = etwSessionId;
        }

        public readonly int m_etwSessionId;
        public ActivityFilter m_activityFilter;
        private static List<WeakReference<EtwSession>> s_etwSessions = new List<WeakReference<EtwSession>>();
        private const int s_thrSessionCount = 16;
    }

    internal struct SessionMask
    {
        public SessionMask(SessionMask m)
        {
            m_mask = m.m_mask;
        }

        public SessionMask(uint mask = 0)
        {
            m_mask = mask & MASK;
        }

        public bool IsEqualOrSupersetOf(SessionMask m)
        {
            return (this.m_mask | m.m_mask) == this.m_mask;
        }

        public static SessionMask All
        {
            get
            {
                return new SessionMask(MASK);
            }
        }

        public static SessionMask FromId(int perEventSourceSessionId)
        {
            Contract.Assert(perEventSourceSessionId < MAX);
            return new SessionMask((uint)1 << perEventSourceSessionId);
        }

        public ulong ToEventKeywords()
        {
            return (ulong)m_mask << SHIFT_SESSION_TO_KEYWORD;
        }

        public static SessionMask FromEventKeywords(ulong m)
        {
            return new SessionMask((uint)(m >> SHIFT_SESSION_TO_KEYWORD));
        }

        public bool this[int perEventSourceSessionId]
        {
            get
            {
                Contract.Assert(perEventSourceSessionId < MAX);
                return (m_mask & (1 << perEventSourceSessionId)) != 0;
            }

            set
            {
                Contract.Assert(perEventSourceSessionId < MAX);
                if (value)
                    m_mask |= ((uint)1 << perEventSourceSessionId);
                else
                    m_mask &= ~((uint)1 << perEventSourceSessionId);
            }
        }

        public static SessionMask operator |(SessionMask m1, SessionMask m2)
        {
            return new SessionMask(m1.m_mask | m2.m_mask);
        }

        public static SessionMask operator &(SessionMask m1, SessionMask m2)
        {
            return new SessionMask(m1.m_mask & m2.m_mask);
        }

        public static SessionMask operator ^(SessionMask m1, SessionMask m2)
        {
            return new SessionMask(m1.m_mask ^ m2.m_mask);
        }

        public static SessionMask operator ~(SessionMask m)
        {
            return new SessionMask(MASK & ~(m.m_mask));
        }

        public static explicit operator ulong (SessionMask m)
        {
            return m.m_mask;
        }

        public static explicit operator uint (SessionMask m)
        {
            return m.m_mask;
        }

        private uint m_mask;
        internal const int SHIFT_SESSION_TO_KEYWORD = 44;
        internal const uint MASK = 0x0fU;
        internal const uint MAX = 4;
    }

    internal class EventDispatcher
    {
        internal EventDispatcher(EventDispatcher next, bool[] eventEnabled, EventListener listener)
        {
            m_Next = next;
            m_EventEnabled = eventEnabled;
            m_Listener = listener;
        }

        readonly internal EventListener m_Listener;
        internal bool[] m_EventEnabled;
        internal bool m_activityFilteringEnabled;
        internal EventDispatcher m_Next;
    }

    [Flags]
    public enum EventManifestOptions
    {
        None = 0x0,
        Strict = 0x1,
        AllCultures = 0x2,
        OnlyIfNeededForRegistration = 0x4,
        AllowEventSourceOverride = 0x8
    }

    internal partial class ManifestBuilder
    {
        public ManifestBuilder(string providerName, Guid providerGuid, string dllName, ResourceManager resources, EventManifestOptions flags)
        {
            this.providerName = providerName;
            this.flags = flags;
            this.resources = resources;
            sb = new StringBuilder();
            events = new StringBuilder();
            templates = new StringBuilder();
            opcodeTab = new Dictionary<int, string>();
            stringTab = new Dictionary<string, string>();
            errors = new List<string>();
            perEventByteArrayArgIndices = new Dictionary<string, List<int>>();
            sb.AppendLine("<instrumentationManifest xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
            sb.AppendLine(" <instrumentation xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:win=\"http://manifests.microsoft.com/win/2004/08/windows/events\">");
            sb.AppendLine("  <events xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
            sb.Append("<provider name=\"").Append(providerName).Append("\" guid=\"{").Append(providerGuid.ToString()).Append("}");
            if (dllName != null)
                sb.Append("\" resourceFileName=\"").Append(dllName).Append("\" messageFileName=\"").Append(dllName);
            var symbolsName = providerName.Replace("-", "").Replace(".", "_");
            sb.Append("\" symbol=\"").Append(symbolsName);
            sb.Append("\">").AppendLine();
        }

        public void AddOpcode(string name, int value)
        {
            if ((flags & EventManifestOptions.Strict) != 0)
            {
                if (value <= 10 || value >= 239)
                {
                    ManifestError(Resources.GetResourceString("EventSource_IllegalOpcodeValue", name, value));
                }

                string prevName;
                if (opcodeTab.TryGetValue(value, out prevName) && !name.Equals(prevName, StringComparison.Ordinal))
                {
                    ManifestError(Resources.GetResourceString("EventSource_OpcodeCollision", name, prevName, value));
                }
            }

            opcodeTab[value] = name;
        }

        public void AddTask(string name, int value)
        {
            if ((flags & EventManifestOptions.Strict) != 0)
            {
                if (value <= 0 || value >= 65535)
                {
                    ManifestError(Resources.GetResourceString("EventSource_IllegalTaskValue", name, value));
                }

                string prevName;
                if (taskTab != null && taskTab.TryGetValue(value, out prevName) && !name.Equals(prevName, StringComparison.Ordinal))
                {
                    ManifestError(Resources.GetResourceString("EventSource_TaskCollision", name, prevName, value));
                }
            }

            if (taskTab == null)
                taskTab = new Dictionary<int, string>();
            taskTab[value] = name;
        }

        public void AddKeyword(string name, ulong value)
        {
            if ((value & (value - 1)) != 0)
            {
                ManifestError(Resources.GetResourceString("EventSource_KeywordNeedPowerOfTwo", "0x" + value.ToString("x", CultureInfo.CurrentCulture), name), true);
            }

            if ((flags & EventManifestOptions.Strict) != 0)
            {
                if (value >= 0x0000100000000000UL && !name.StartsWith("Session", StringComparison.Ordinal))
                {
                    ManifestError(Resources.GetResourceString("EventSource_IllegalKeywordsValue", name, "0x" + value.ToString("x", CultureInfo.CurrentCulture)));
                }

                string prevName;
                if (keywordTab != null && keywordTab.TryGetValue(value, out prevName) && !name.Equals(prevName, StringComparison.Ordinal))
                {
                    ManifestError(Resources.GetResourceString("EventSource_KeywordCollision", name, prevName, "0x" + value.ToString("x", CultureInfo.CurrentCulture)));
                }
            }

            if (keywordTab == null)
                keywordTab = new Dictionary<ulong, string>();
            keywordTab[value] = name;
        }

        public void AddChannel(string name, int value, EventChannelAttribute channelAttribute)
        {
            EventChannel chValue = (EventChannel)value;
            if (value < (int)EventChannel.Admin || value > 255)
                ManifestError(Resources.GetResourceString("EventSource_EventChannelOutOfRange", name, value));
            else if (chValue >= EventChannel.Admin && chValue <= EventChannel.Debug && channelAttribute != null && EventChannelToChannelType(chValue) != channelAttribute.EventChannelType)
            {
                ManifestError(Resources.GetResourceString("EventSource_ChannelTypeDoesNotMatchEventChannelValue", name, ((EventChannel)value).ToString()));
            }

            ulong kwd = GetChannelKeyword(chValue);
            if (channelTab == null)
                channelTab = new Dictionary<int, ChannelInfo>(4);
            channelTab[value] = new ChannelInfo{Name = name, Keywords = kwd, Attribs = channelAttribute};
        }

        private EventChannelType EventChannelToChannelType(EventChannel channel)
        {
            Contract.Assert(channel >= EventChannel.Admin && channel <= EventChannel.Debug);
            return (EventChannelType)((int)channel - (int)EventChannel.Admin + (int)EventChannelType.Admin);
        }

        private EventChannelAttribute GetDefaultChannelAttribute(EventChannel channel)
        {
            EventChannelAttribute attrib = new EventChannelAttribute();
            attrib.EventChannelType = EventChannelToChannelType(channel);
            if (attrib.EventChannelType <= EventChannelType.Operational)
                attrib.Enabled = true;
            return attrib;
        }

        public ulong[] GetChannelData()
        {
            if (this.channelTab == null)
            {
                return new ulong[0];
            }

            int maxkey = -1;
            foreach (var item in this.channelTab.Keys)
            {
                if (item > maxkey)
                {
                    maxkey = item;
                }
            }

            ulong[] channelMask = new ulong[maxkey + 1];
            foreach (var item in this.channelTab)
            {
                channelMask[item.Key] = item.Value.Keywords;
            }

            return channelMask;
        }

        public void StartEvent(string eventName, EventAttribute eventAttribute)
        {
            Contract.Assert(numParams == 0);
            Contract.Assert(this.eventName == null);
            this.eventName = eventName;
            numParams = 0;
            byteArrArgIndices = null;
            events.Append("  <event").Append(" value=\"").Append(eventAttribute.EventId).Append("\"").Append(" version=\"").Append(eventAttribute.Version).Append("\"").Append(" level=\"").Append(GetLevelName(eventAttribute.Level)).Append("\"").Append(" symbol=\"").Append(eventName).Append("\"");
            WriteMessageAttrib(events, "event", eventName, eventAttribute.Message);
            if (eventAttribute.Keywords != 0)
                events.Append(" keywords=\"").Append(GetKeywords((ulong)eventAttribute.Keywords, eventName)).Append("\"");
            if (eventAttribute.Opcode != 0)
                events.Append(" opcode=\"").Append(GetOpcodeName(eventAttribute.Opcode, eventName)).Append("\"");
            if (eventAttribute.Task != 0)
                events.Append(" task=\"").Append(GetTaskName(eventAttribute.Task, eventName)).Append("\"");
            if (eventAttribute.Channel != 0)
            {
                events.Append(" channel=\"").Append(GetChannelName(eventAttribute.Channel, eventName, eventAttribute.Message)).Append("\"");
            }
        }

        public void AddEventParameter(Type type, string name)
        {
            if (numParams == 0)
                templates.Append("  <template tid=\"").Append(eventName).Append("Args\">").AppendLine();
            if (type == typeof (byte[]))
            {
                if (byteArrArgIndices == null)
                    byteArrArgIndices = new List<int>(4);
                byteArrArgIndices.Add(numParams);
                numParams++;
                templates.Append("   <data name=\"").Append(name).Append("Size\" inType=\"win:UInt32\"/>").AppendLine();
            }

            numParams++;
            templates.Append("   <data name=\"").Append(name).Append("\" inType=\"").Append(GetTypeName(type)).Append("\"");
            if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof (byte))
            {
                templates.Append(" length=\"").Append(name).Append("Size\"");
            }

            if (type.IsEnum() && Enum.GetUnderlyingType(type) != typeof (UInt64) && Enum.GetUnderlyingType(type) != typeof (Int64))
            {
                templates.Append(" map=\"").Append(type.Name).Append("\"");
                if (mapsTab == null)
                    mapsTab = new Dictionary<string, Type>();
                if (!mapsTab.ContainsKey(type.Name))
                    mapsTab.Add(type.Name, type);
            }

            templates.Append("/>").AppendLine();
        }

        public void EndEvent()
        {
            if (numParams > 0)
            {
                templates.Append("  </template>").AppendLine();
                events.Append(" template=\"").Append(eventName).Append("Args\"");
            }

            events.Append("/>").AppendLine();
            if (byteArrArgIndices != null)
                perEventByteArrayArgIndices[eventName] = byteArrArgIndices;
            string msg;
            if (stringTab.TryGetValue("event_" + eventName, out msg))
            {
                msg = TranslateToManifestConvention(msg, eventName);
                stringTab["event_" + eventName] = msg;
            }

            eventName = null;
            numParams = 0;
            byteArrArgIndices = null;
        }

        public ulong GetChannelKeyword(EventChannel channel)
        {
            if (channelTab == null)
            {
                channelTab = new Dictionary<int, ChannelInfo>(4);
            }

            if (channelTab.Count == MaxCountChannels)
                ManifestError(Resources.GetResourceString("EventSource_MaxChannelExceeded"));
            ulong channelKeyword;
            ChannelInfo info;
            if (!channelTab.TryGetValue((int)channel, out info))
            {
                channelKeyword = nextChannelKeywordBit;
                nextChannelKeywordBit >>= 1;
            }
            else
            {
                channelKeyword = info.Keywords;
            }

            return channelKeyword;
        }

        public byte[] CreateManifest()
        {
            string str = CreateManifestString();
            return Encoding.UTF8.GetBytes(str);
        }

        public IList<string> Errors
        {
            get
            {
                return errors;
            }
        }

        public void ManifestError(string msg, bool runtimeCritical = false)
        {
            if ((flags & EventManifestOptions.Strict) != 0)
                errors.Add(msg);
            else if (runtimeCritical)
                throw new ArgumentException(msg);
        }

        private string CreateManifestString()
        {
            if (channelTab != null)
            {
                sb.Append(" <channels>").AppendLine();
                var sortedChannels = new List<KeyValuePair<int, ChannelInfo>>();
                foreach (KeyValuePair<int, ChannelInfo> p in channelTab)
                {
                    sortedChannels.Add(p);
                }

                sortedChannels.Sort((p1, p2) => -Comparer<ulong>.Default.Compare(p1.Value.Keywords, p2.Value.Keywords));
                foreach (var kvpair in sortedChannels)
                {
                    int channel = kvpair.Key;
                    ChannelInfo channelInfo = kvpair.Value;
                    string channelType = null;
                    string elementName = "channel";
                    bool enabled = false;
                    string fullName = null;
                    if (channelInfo.Attribs != null)
                    {
                        var attribs = channelInfo.Attribs;
                        if (Enum.IsDefined(typeof (EventChannelType), attribs.EventChannelType))
                            channelType = attribs.EventChannelType.ToString();
                        enabled = attribs.Enabled;
                    }

                    if (fullName == null)
                        fullName = providerName + "/" + channelInfo.Name;
                    sb.Append("  <").Append(elementName);
                    sb.Append(" chid=\"").Append(channelInfo.Name).Append("\"");
                    sb.Append(" name=\"").Append(fullName).Append("\"");
                    if (elementName == "channel")
                    {
                        WriteMessageAttrib(sb, "channel", channelInfo.Name, null);
                        sb.Append(" value=\"").Append(channel).Append("\"");
                        if (channelType != null)
                            sb.Append(" type=\"").Append(channelType).Append("\"");
                        sb.Append(" enabled=\"").Append(enabled.ToString().ToLower()).Append("\"");
                    }

                    sb.Append("/>").AppendLine();
                }

                sb.Append(" </channels>").AppendLine();
            }

            if (taskTab != null)
            {
                sb.Append(" <tasks>").AppendLine();
                var sortedTasks = new List<int>(taskTab.Keys);
                sortedTasks.Sort();
                foreach (int task in sortedTasks)
                {
                    sb.Append("  <task");
                    WriteNameAndMessageAttribs(sb, "task", taskTab[task]);
                    sb.Append(" value=\"").Append(task).Append("\"/>").AppendLine();
                }

                sb.Append(" </tasks>").AppendLine();
            }

            if (mapsTab != null)
            {
                sb.Append(" <maps>").AppendLine();
                foreach (Type enumType in mapsTab.Values)
                {
                    bool isbitmap = EventSource.GetCustomAttributeHelper(enumType, typeof (FlagsAttribute), flags) != null;
                    string mapKind = isbitmap ? "bitMap" : "valueMap";
                    sb.Append("  <").Append(mapKind).Append(" name=\"").Append(enumType.Name).Append("\">").AppendLine();
                    FieldInfo[] staticFields = enumType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
                    foreach (FieldInfo staticField in staticFields)
                    {
                        object constantValObj = staticField.GetRawConstantValue();
                        if (constantValObj != null)
                        {
                            long hexValue;
                            if (constantValObj is int)
                                hexValue = ((int)constantValObj);
                            else if (constantValObj is long)
                                hexValue = ((long)constantValObj);
                            else
                                continue;
                            if (isbitmap && ((hexValue & (hexValue - 1)) != 0 || hexValue == 0))
                                continue;
                            sb.Append("   <map value=\"0x").Append(hexValue.ToString("x", CultureInfo.InvariantCulture)).Append("\"");
                            WriteMessageAttrib(sb, "map", enumType.Name + "." + staticField.Name, staticField.Name);
                            sb.Append("/>").AppendLine();
                        }
                    }

                    sb.Append("  </").Append(mapKind).Append(">").AppendLine();
                }

                sb.Append(" </maps>").AppendLine();
            }

            sb.Append(" <opcodes>").AppendLine();
            var sortedOpcodes = new List<int>(opcodeTab.Keys);
            sortedOpcodes.Sort();
            foreach (int opcode in sortedOpcodes)
            {
                sb.Append("  <opcode");
                WriteNameAndMessageAttribs(sb, "opcode", opcodeTab[opcode]);
                sb.Append(" value=\"").Append(opcode).Append("\"/>").AppendLine();
            }

            sb.Append(" </opcodes>").AppendLine();
            if (keywordTab != null)
            {
                sb.Append(" <keywords>").AppendLine();
                var sortedKeywords = new List<ulong>(keywordTab.Keys);
                sortedKeywords.Sort();
                foreach (ulong keyword in sortedKeywords)
                {
                    sb.Append("  <keyword");
                    WriteNameAndMessageAttribs(sb, "keyword", keywordTab[keyword]);
                    sb.Append(" mask=\"0x").Append(keyword.ToString("x", CultureInfo.InvariantCulture)).Append("\"/>").AppendLine();
                }

                sb.Append(" </keywords>").AppendLine();
            }

            sb.Append(" <events>").AppendLine();
            sb.Append(events);
            sb.Append(" </events>").AppendLine();
            sb.Append(" <templates>").AppendLine();
            if (templates.Length > 0)
            {
                sb.Append(templates);
            }
            else
            {
                sb.Append("    <template tid=\"_empty\"></template>").AppendLine();
            }

            sb.Append(" </templates>").AppendLine();
            sb.Append("</provider>").AppendLine();
            sb.Append("</events>").AppendLine();
            sb.Append("</instrumentation>").AppendLine();
            sb.Append("<localization>").AppendLine();
            List<CultureInfo> cultures = null;
            if (resources != null && (flags & EventManifestOptions.AllCultures) != 0)
            {
                cultures = GetSupportedCultures(resources);
            }
            else
            {
                cultures = new List<CultureInfo>();
                cultures.Add(CultureInfo.CurrentUICulture);
            }

            var sortedStrings = new string[stringTab.Keys.Count];
            stringTab.Keys.CopyTo(sortedStrings, 0);
            ArraySortHelper<string>.IntrospectiveSort(sortedStrings, 0, sortedStrings.Length, Comparer<string>.Default);
            foreach (var ci in cultures)
            {
                sb.Append(" <resources culture=\"").Append(ci.Name).Append("\">").AppendLine();
                sb.Append("  <stringTable>").AppendLine();
                foreach (var stringKey in sortedStrings)
                {
                    string val = GetLocalizedMessage(stringKey, ci, etwFormat: true);
                    sb.Append("   <string id=\"").Append(stringKey).Append("\" value=\"").Append(val).Append("\"/>").AppendLine();
                }

                sb.Append("  </stringTable>").AppendLine();
                sb.Append(" </resources>").AppendLine();
            }

            sb.Append("</localization>").AppendLine();
            sb.AppendLine("</instrumentationManifest>");
            return sb.ToString();
        }

        private void WriteNameAndMessageAttribs(StringBuilder stringBuilder, string elementName, string name)
        {
            stringBuilder.Append(" name=\"").Append(name).Append("\"");
            WriteMessageAttrib(sb, elementName, name, name);
        }

        private void WriteMessageAttrib(StringBuilder stringBuilder, string elementName, string name, string value)
        {
            string key = elementName + "_" + name;
            if (resources != null)
            {
                string localizedString = resources.GetString(key, CultureInfo.InvariantCulture);
                if (localizedString != null)
                    value = localizedString;
            }

            if (value == null)
                return;
            stringBuilder.Append(" message=\"$(string.").Append(key).Append(")\"");
            string prevValue;
            if (stringTab.TryGetValue(key, out prevValue) && !prevValue.Equals(value))
            {
                ManifestError(Resources.GetResourceString("EventSource_DuplicateStringKey", key), true);
                return;
            }

            stringTab[key] = value;
        }

        internal string GetLocalizedMessage(string key, CultureInfo ci, bool etwFormat)
        {
            string value = null;
            if (resources != null)
            {
                string localizedString = resources.GetString(key, ci);
                if (localizedString != null)
                {
                    value = localizedString;
                    if (etwFormat && key.StartsWith("event_"))
                    {
                        var evtName = key.Substring("event_".Length);
                        value = TranslateToManifestConvention(value, evtName);
                    }
                }
            }

            if (etwFormat && value == null)
                stringTab.TryGetValue(key, out value);
            return value;
        }

        private static List<CultureInfo> GetSupportedCultures(ResourceManager resources)
        {
            var cultures = new List<CultureInfo>();
            if (!cultures.Contains(CultureInfo.CurrentUICulture))
                cultures.Insert(0, CultureInfo.CurrentUICulture);
            return cultures;
        }

        private static string GetLevelName(EventLevel level)
        {
            return (((int)level >= 16) ? "" : "win:") + level.ToString();
        }

        private string GetChannelName(EventChannel channel, string eventName, string eventMessage)
        {
            ChannelInfo info = null;
            if (channelTab == null || !channelTab.TryGetValue((int)channel, out info))
            {
                if (channel < EventChannel.Admin)
                    ManifestError(Resources.GetResourceString("EventSource_UndefinedChannel", channel, eventName));
                if (channelTab == null)
                    channelTab = new Dictionary<int, ChannelInfo>(4);
                string channelName = channel.ToString();
                if (EventChannel.Debug < channel)
                    channelName = "Channel" + channelName;
                AddChannel(channelName, (int)channel, GetDefaultChannelAttribute(channel));
                if (!channelTab.TryGetValue((int)channel, out info))
                    ManifestError(Resources.GetResourceString("EventSource_UndefinedChannel", channel, eventName));
            }

            if (resources != null && eventMessage == null)
                eventMessage = resources.GetString("event_" + eventName, CultureInfo.InvariantCulture);
            if (info.Attribs.EventChannelType == EventChannelType.Admin && eventMessage == null)
                ManifestError(Resources.GetResourceString("EventSource_EventWithAdminChannelMustHaveMessage", eventName, info.Name));
            return info.Name;
        }

        private string GetTaskName(EventTask task, string eventName)
        {
            if (task == EventTask.None)
                return "";
            string ret;
            if (taskTab == null)
                taskTab = new Dictionary<int, string>();
            if (!taskTab.TryGetValue((int)task, out ret))
                ret = taskTab[(int)task] = eventName;
            return ret;
        }

        private string GetOpcodeName(EventOpcode opcode, string eventName)
        {
            switch (opcode)
            {
                case EventOpcode.Info:
                    return "win:Info";
                case EventOpcode.Start:
                    return "win:Start";
                case EventOpcode.Stop:
                    return "win:Stop";
                case EventOpcode.DataCollectionStart:
                    return "win:DC_Start";
                case EventOpcode.DataCollectionStop:
                    return "win:DC_Stop";
                case EventOpcode.Extension:
                    return "win:Extension";
                case EventOpcode.Reply:
                    return "win:Reply";
                case EventOpcode.Resume:
                    return "win:Resume";
                case EventOpcode.Suspend:
                    return "win:Suspend";
                case EventOpcode.Send:
                    return "win:Send";
                case EventOpcode.Receive:
                    return "win:Receive";
            }

            string ret;
            if (opcodeTab == null || !opcodeTab.TryGetValue((int)opcode, out ret))
            {
                ManifestError(Resources.GetResourceString("EventSource_UndefinedOpcode", opcode, eventName), true);
                ret = null;
            }

            return ret;
        }

        private string GetKeywords(ulong keywords, string eventName)
        {
            string ret = "";
            for (ulong bit = 1; bit != 0; bit <<= 1)
            {
                if ((keywords & bit) != 0)
                {
                    string keyword = null;
                    if ((keywordTab == null || !keywordTab.TryGetValue(bit, out keyword)) && (bit >= (ulong)0x1000000000000))
                    {
                        keyword = string.Empty;
                    }

                    if (keyword == null)
                    {
                        ManifestError(Resources.GetResourceString("EventSource_UndefinedKeyword", "0x" + bit.ToString("x", CultureInfo.CurrentCulture), eventName), true);
                        keyword = string.Empty;
                    }

                    if (ret.Length != 0 && keyword.Length != 0)
                        ret = ret + " ";
                    ret = ret + keyword;
                }
            }

            return ret;
        }

        private string GetTypeName(Type type)
        {
            if (type.IsEnum())
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var typeName = GetTypeName(fields[0].FieldType);
                return typeName.Replace("win:Int", "win:UInt");
            }

            return GetTypeNameHelper(type);
        }

        private static void UpdateStringBuilder(ref StringBuilder stringBuilder, string eventMessage, int startIndex, int count)
        {
            if (stringBuilder == null)
                stringBuilder = new StringBuilder();
            stringBuilder.Append(eventMessage, startIndex, count);
        }

        private string TranslateToManifestConvention(string eventMessage, string evtName)
        {
            StringBuilder stringBuilder = null;
            int writtenSoFar = 0;
            int chIdx = -1;
            for (int i = 0;;)
            {
                if (i >= eventMessage.Length)
                {
                    if (stringBuilder == null)
                        return eventMessage;
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    return stringBuilder.ToString();
                }

                if (eventMessage[i] == '%')
                {
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    stringBuilder.Append("%%");
                    i++;
                    writtenSoFar = i;
                }
                else if (i < eventMessage.Length - 1 && (eventMessage[i] == '{' && eventMessage[i + 1] == '{' || eventMessage[i] == '}' && eventMessage[i + 1] == '}'))
                {
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    stringBuilder.Append(eventMessage[i]);
                    i++;
                    i++;
                    writtenSoFar = i;
                }
                else if (eventMessage[i] == '{')
                {
                    int leftBracket = i;
                    i++;
                    int argNum = 0;
                    while (i < eventMessage.Length && Char.IsDigit(eventMessage[i]))
                    {
                        argNum = argNum * 10 + eventMessage[i] - '0';
                        i++;
                    }

                    if (i < eventMessage.Length && eventMessage[i] == '}')
                    {
                        i++;
                        UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, leftBracket - writtenSoFar);
                        int manIndex = TranslateIndexToManifestConvention(argNum, evtName);
                        stringBuilder.Append('%').Append(manIndex);
                        if (i < eventMessage.Length && eventMessage[i] == '!')
                        {
                            i++;
                            stringBuilder.Append("%!");
                        }

                        writtenSoFar = i;
                    }
                    else
                    {
                        ManifestError(Resources.GetResourceString("EventSource_UnsupportedMessageProperty", evtName, eventMessage));
                    }
                }
                else if ((chIdx = "&<>'\"\r\n\t".IndexOf(eventMessage[i])) >= 0)
                {
                    string[] escapes = {"&amp;", "&lt;", "&gt;", "&apos;", "&quot;", "%r", "%n", "%t"};
                    var update = new Action<char, string>((ch, escape) =>
                    {
                        UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                        i++;
                        stringBuilder.Append(escape);
                        writtenSoFar = i;
                    }

                    );
                    update(eventMessage[i], escapes[chIdx]);
                }
                else
                    i++;
            }
        }

        private int TranslateIndexToManifestConvention(int idx, string evtName)
        {
            List<int> byteArrArgIndices;
            if (perEventByteArrayArgIndices.TryGetValue(evtName, out byteArrArgIndices))
            {
                foreach (var byArrIdx in byteArrArgIndices)
                {
                    if (idx >= byArrIdx)
                        ++idx;
                    else
                        break;
                }
            }

            return idx + 1;
        }

        class ChannelInfo
        {
            public string Name;
            public ulong Keywords;
            public EventChannelAttribute Attribs;
        }

        Dictionary<int, string> opcodeTab;
        Dictionary<int, string> taskTab;
        Dictionary<int, ChannelInfo> channelTab;
        Dictionary<ulong, string> keywordTab;
        Dictionary<string, Type> mapsTab;
        Dictionary<string, string> stringTab;
        ulong nextChannelKeywordBit = 0x8000000000000000;
        const int MaxCountChannels = 8;
        StringBuilder sb;
        StringBuilder events;
        StringBuilder templates;
        string providerName;
        ResourceManager resources;
        EventManifestOptions flags;
        IList<string> errors;
        Dictionary<string, List<int>> perEventByteArrayArgIndices;
        string eventName;
        int numParams;
        List<int> byteArrArgIndices;
    }

    internal struct ManifestEnvelope
    {
        public const int MaxChunkSize = 0xFF00;
        public enum ManifestFormats : byte
        {
            SimpleXmlFormat = 1
        }

        public ManifestFormats Format;
        public byte MajorVersion;
        public byte MinorVersion;
        public byte Magic;
        public ushort TotalChunks;
        public ushort ChunkNumber;
    }

    ;
}