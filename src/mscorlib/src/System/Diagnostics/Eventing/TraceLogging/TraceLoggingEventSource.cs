using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Collections.ObjectModel;
using Contract = System.Diagnostics.Contracts.Contract;
using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics.Tracing
{
    public partial class EventSource
    {
        private byte[] providerMetadata;
        public EventSource(string eventSourceName): this (eventSourceName, EventSourceSettings.EtwSelfDescribingEventFormat)
        {
        }

        public EventSource(string eventSourceName, EventSourceSettings config): this (eventSourceName, config, null)
        {
        }

        public EventSource(string eventSourceName, EventSourceSettings config, params string[] traits): this (eventSourceName == null ? new Guid() : GenerateGuidFromName(eventSourceName.ToUpperInvariant()), eventSourceName, config, traits)
        {
            if (eventSourceName == null)
            {
                throw new ArgumentNullException("eventSourceName");
            }

            Contract.EndContractBlock();
        }

        public unsafe void Write(string eventName)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            Contract.EndContractBlock();
            if (!this.IsEnabled())
            {
                return;
            }

            var options = new EventSourceOptions();
            this.WriteImpl(eventName, ref options, null, null, null, SimpleEventTypes<EmptyStruct>.Instance);
        }

        public unsafe void Write(string eventName, EventSourceOptions options)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            Contract.EndContractBlock();
            if (!this.IsEnabled())
            {
                return;
            }

            this.WriteImpl(eventName, ref options, null, null, null, SimpleEventTypes<EmptyStruct>.Instance);
        }

        public unsafe void Write<T>(string eventName, T data)
        {
            if (!this.IsEnabled())
            {
                return;
            }

            var options = new EventSourceOptions();
            this.WriteImpl(eventName, ref options, data, null, null, SimpleEventTypes<T>.Instance);
        }

        public unsafe void Write<T>(string eventName, EventSourceOptions options, T data)
        {
            if (!this.IsEnabled())
            {
                return;
            }

            this.WriteImpl(eventName, ref options, data, null, null, SimpleEventTypes<T>.Instance);
        }

        public unsafe void Write<T>(string eventName, ref EventSourceOptions options, ref T data)
        {
            if (!this.IsEnabled())
            {
                return;
            }

            this.WriteImpl(eventName, ref options, data, null, null, SimpleEventTypes<T>.Instance);
        }

        public unsafe void Write<T>(string eventName, ref EventSourceOptions options, ref Guid activityId, ref Guid relatedActivityId, ref T data)
        {
            if (!this.IsEnabled())
            {
                return;
            }

            fixed (Guid*pActivity = &activityId, pRelated = &relatedActivityId)
            {
                this.WriteImpl(eventName, ref options, data, pActivity, relatedActivityId == Guid.Empty ? null : pRelated, SimpleEventTypes<T>.Instance);
            }
        }

        private unsafe void WriteMultiMerge(string eventName, ref EventSourceOptions options, TraceLoggingEventTypes eventTypes, Guid*activityID, Guid*childActivityID, params object[] values)
        {
            if (!this.IsEnabled())
            {
                return;
            }

            byte level = (options.valuesSet & EventSourceOptions.levelSet) != 0 ? options.level : eventTypes.level;
            EventKeywords keywords = (options.valuesSet & EventSourceOptions.keywordsSet) != 0 ? options.keywords : eventTypes.keywords;
            if (this.IsEnabled((EventLevel)level, keywords))
            {
                WriteMultiMergeInner(eventName, ref options, eventTypes, activityID, childActivityID, values);
            }
        }

        private unsafe void WriteMultiMergeInner(string eventName, ref EventSourceOptions options, TraceLoggingEventTypes eventTypes, Guid*activityID, Guid*childActivityID, params object[] values)
        {
            int identity = 0;
            byte level = (options.valuesSet & EventSourceOptions.levelSet) != 0 ? options.level : eventTypes.level;
            byte opcode = (options.valuesSet & EventSourceOptions.opcodeSet) != 0 ? options.opcode : eventTypes.opcode;
            EventTags tags = (options.valuesSet & EventSourceOptions.tagsSet) != 0 ? options.tags : eventTypes.Tags;
            EventKeywords keywords = (options.valuesSet & EventSourceOptions.keywordsSet) != 0 ? options.keywords : eventTypes.keywords;
            var nameInfo = eventTypes.GetNameInfo(eventName ?? eventTypes.Name, tags);
            if (nameInfo == null)
            {
                return;
            }

            identity = nameInfo.identity;
            EventDescriptor descriptor = new EventDescriptor(identity, level, opcode, (long)keywords);
            var pinCount = eventTypes.pinCount;
            var scratch = stackalloc byte[eventTypes.scratchSize];
            var descriptors = stackalloc EventData[eventTypes.dataCount + 3];
            var pins = stackalloc GCHandle[pinCount];
            fixed (byte *pMetadata0 = this.providerMetadata, pMetadata1 = nameInfo.nameMetadata, pMetadata2 = eventTypes.typeMetadata)
            {
                descriptors[0].SetMetadata(pMetadata0, this.providerMetadata.Length, 2);
                descriptors[1].SetMetadata(pMetadata1, nameInfo.nameMetadata.Length, 1);
                descriptors[2].SetMetadata(pMetadata2, eventTypes.typeMetadata.Length, 1);
                System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    DataCollector.ThreadInstance.Enable(scratch, eventTypes.scratchSize, descriptors + 3, eventTypes.dataCount, pins, pinCount);
                    for (int i = 0; i < eventTypes.typeInfos.Length; i++)
                    {
                        var info = eventTypes.typeInfos[i];
                        info.WriteData(TraceLoggingDataCollector.Instance, info.PropertyValueFactory(values[i]));
                    }

                    this.WriteEventRaw(eventName, ref descriptor, activityID, childActivityID, (int)(DataCollector.ThreadInstance.Finish() - descriptors), (IntPtr)descriptors);
                }
                finally
                {
                    this.WriteCleanup(pins, pinCount);
                }
            }
        }

        internal unsafe void WriteMultiMerge(string eventName, ref EventSourceOptions options, TraceLoggingEventTypes eventTypes, Guid*activityID, Guid*childActivityID, EventData*data)
        {
            if (!this.IsEnabled())
            {
                return;
            }

            fixed (EventSourceOptions*pOptions = &options)
            {
                EventDescriptor descriptor;
                var nameInfo = this.UpdateDescriptor(eventName, eventTypes, ref options, out descriptor);
                if (nameInfo == null)
                {
                    return;
                }

                var descriptors = stackalloc EventData[eventTypes.dataCount + eventTypes.typeInfos.Length * 2 + 3];
                fixed (byte *pMetadata0 = this.providerMetadata, pMetadata1 = nameInfo.nameMetadata, pMetadata2 = eventTypes.typeMetadata)
                {
                    descriptors[0].SetMetadata(pMetadata0, this.providerMetadata.Length, 2);
                    descriptors[1].SetMetadata(pMetadata1, nameInfo.nameMetadata.Length, 1);
                    descriptors[2].SetMetadata(pMetadata2, eventTypes.typeMetadata.Length, 1);
                    int numDescrs = 3;
                    for (int i = 0; i < eventTypes.typeInfos.Length; i++)
                    {
                        if (eventTypes.typeInfos[i].DataType == typeof (string))
                        {
                            descriptors[numDescrs].m_Ptr = (long)&descriptors[numDescrs + 1].m_Size;
                            descriptors[numDescrs].m_Size = 2;
                            numDescrs++;
                            descriptors[numDescrs].m_Ptr = data[i].m_Ptr;
                            descriptors[numDescrs].m_Size = data[i].m_Size - 2;
                            numDescrs++;
                        }
                        else
                        {
                            descriptors[numDescrs].m_Ptr = data[i].m_Ptr;
                            descriptors[numDescrs].m_Size = data[i].m_Size;
                            if (data[i].m_Size == 4 && eventTypes.typeInfos[i].DataType == typeof (bool))
                                descriptors[numDescrs].m_Size = 1;
                            numDescrs++;
                        }
                    }

                    this.WriteEventRaw(eventName, ref descriptor, activityID, childActivityID, numDescrs, (IntPtr)descriptors);
                }
            }
        }

        private unsafe void WriteImpl(string eventName, ref EventSourceOptions options, object data, Guid*pActivityId, Guid*pRelatedActivityId, TraceLoggingEventTypes eventTypes)
        {
            try
            {
                fixed (EventSourceOptions*pOptions = &options)
                {
                    EventDescriptor descriptor;
                    options.Opcode = options.IsOpcodeSet ? options.Opcode : GetOpcodeWithDefault(options.Opcode, eventName);
                    var nameInfo = this.UpdateDescriptor(eventName, eventTypes, ref options, out descriptor);
                    if (nameInfo == null)
                    {
                        return;
                    }

                    var pinCount = eventTypes.pinCount;
                    var scratch = stackalloc byte[eventTypes.scratchSize];
                    var descriptors = stackalloc EventData[eventTypes.dataCount + 3];
                    var pins = stackalloc GCHandle[pinCount];
                    fixed (byte *pMetadata0 = this.providerMetadata, pMetadata1 = nameInfo.nameMetadata, pMetadata2 = eventTypes.typeMetadata)
                    {
                        descriptors[0].SetMetadata(pMetadata0, this.providerMetadata.Length, 2);
                        descriptors[1].SetMetadata(pMetadata1, nameInfo.nameMetadata.Length, 1);
                        descriptors[2].SetMetadata(pMetadata2, eventTypes.typeMetadata.Length, 1);
                        System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
                        EventOpcode opcode = (EventOpcode)descriptor.Opcode;
                        Guid activityId = Guid.Empty;
                        Guid relatedActivityId = Guid.Empty;
                        if (pActivityId == null && pRelatedActivityId == null && ((options.ActivityOptions & EventActivityOptions.Disable) == 0))
                        {
                            if (opcode == EventOpcode.Start)
                            {
                                m_activityTracker.OnStart(m_name, eventName, 0, ref activityId, ref relatedActivityId, options.ActivityOptions);
                            }
                            else if (opcode == EventOpcode.Stop)
                            {
                                m_activityTracker.OnStop(m_name, eventName, 0, ref activityId);
                            }

                            if (activityId != Guid.Empty)
                                pActivityId = &activityId;
                            if (relatedActivityId != Guid.Empty)
                                pRelatedActivityId = &relatedActivityId;
                        }

                        try
                        {
                            DataCollector.ThreadInstance.Enable(scratch, eventTypes.scratchSize, descriptors + 3, eventTypes.dataCount, pins, pinCount);
                            var info = eventTypes.typeInfos[0];
                            info.WriteData(TraceLoggingDataCollector.Instance, info.PropertyValueFactory(data));
                            this.WriteEventRaw(eventName, ref descriptor, pActivityId, pRelatedActivityId, (int)(DataCollector.ThreadInstance.Finish() - descriptors), (IntPtr)descriptors);
                            if (m_Dispatchers != null)
                            {
                                var eventData = (EventPayload)(eventTypes.typeInfos[0].GetData(data));
                                WriteToAllListeners(eventName, ref descriptor, nameInfo.tags, pActivityId, eventData);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is EventSourceException)
                                throw;
                            else
                                ThrowEventSourceException(eventName, ex);
                        }
                        finally
                        {
                            this.WriteCleanup(pins, pinCount);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is EventSourceException)
                    throw;
                else
                    ThrowEventSourceException(eventName, ex);
            }
        }

        private unsafe void WriteToAllListeners(string eventName, ref EventDescriptor eventDescriptor, EventTags tags, Guid*pActivityId, EventPayload payload)
        {
            EventWrittenEventArgs eventCallbackArgs = new EventWrittenEventArgs(this);
            eventCallbackArgs.EventName = eventName;
            eventCallbackArgs.m_level = (EventLevel)eventDescriptor.Level;
            eventCallbackArgs.m_keywords = (EventKeywords)eventDescriptor.Keywords;
            eventCallbackArgs.m_opcode = (EventOpcode)eventDescriptor.Opcode;
            eventCallbackArgs.m_tags = tags;
            eventCallbackArgs.EventId = -1;
            if (pActivityId != null)
                eventCallbackArgs.RelatedActivityId = *pActivityId;
            if (payload != null)
            {
                eventCallbackArgs.Payload = new ReadOnlyCollection<object>((IList<object>)payload.Values);
                eventCallbackArgs.PayloadNames = new ReadOnlyCollection<string>((IList<string>)payload.Keys);
            }

            DispatchToAllListeners(-1, pActivityId, eventCallbackArgs);
        }

        private unsafe void WriteCleanup(GCHandle*pPins, int cPins)
        {
            DataCollector.ThreadInstance.Disable();
            for (int i = 0; i != cPins; i++)
            {
                if (IntPtr.Zero != (IntPtr)pPins[i])
                {
                    pPins[i].Free();
                }
            }
        }

        private void InitializeProviderMetadata()
        {
            if (m_traits != null)
            {
                List<byte> traitMetaData = new List<byte>(100);
                for (int i = 0; i < m_traits.Length - 1; i += 2)
                {
                    if (m_traits[i].StartsWith("ETW_"))
                    {
                        string etwTrait = m_traits[i].Substring(4);
                        byte traitNum;
                        if (!byte.TryParse(etwTrait, out traitNum))
                        {
                            if (etwTrait == "GROUP")
                            {
                                traitNum = 1;
                            }
                            else
                            {
                                throw new ArgumentException(Resources.GetResourceString("UnknownEtwTrait", etwTrait), "traits");
                            }
                        }

                        string value = m_traits[i + 1];
                        int lenPos = traitMetaData.Count;
                        traitMetaData.Add(0);
                        traitMetaData.Add(0);
                        traitMetaData.Add(traitNum);
                        var valueLen = AddValueToMetaData(traitMetaData, value) + 3;
                        traitMetaData[lenPos] = unchecked ((byte)valueLen);
                        traitMetaData[lenPos + 1] = unchecked ((byte)(valueLen >> 8));
                    }
                }

                providerMetadata = Statics.MetadataForString(this.Name, 0, traitMetaData.Count, 0);
                int startPos = providerMetadata.Length - traitMetaData.Count;
                foreach (var b in traitMetaData)
                    providerMetadata[startPos++] = b;
            }
            else
                providerMetadata = Statics.MetadataForString(this.Name, 0, 0, 0);
        }

        private static int AddValueToMetaData(List<byte> metaData, string value)
        {
            if (value.Length == 0)
                return 0;
            int startPos = metaData.Count;
            char firstChar = value[0];
            if (firstChar == '@')
                metaData.AddRange(Encoding.UTF8.GetBytes(value.Substring(1)));
            else if (firstChar == '{')
                metaData.AddRange(new Guid(value).ToByteArray());
            else if (firstChar == '#')
            {
                for (int i = 1; i < value.Length; i++)
                {
                    if (value[i] != ' ')
                    {
                        if (!(i + 1 < value.Length))
                        {
                            throw new ArgumentException(Resources.GetResourceString("EvenHexDigits"), "traits");
                        }

                        metaData.Add((byte)(HexDigit(value[i]) * 16 + HexDigit(value[i + 1])));
                        i++;
                    }
                }
            }
            else if ('A' <= firstChar || ' ' == firstChar)
            {
                metaData.AddRange(Encoding.UTF8.GetBytes(value));
            }
            else
            {
                throw new ArgumentException(Resources.GetResourceString("IllegalValue", value), "traits");
            }

            return metaData.Count - startPos;
        }

        private static int HexDigit(char c)
        {
            if ('0' <= c && c <= '9')
            {
                return (c - '0');
            }

            if ('a' <= c)
            {
                c = unchecked ((char)(c - ('a' - 'A')));
            }

            if ('A' <= c && c <= 'F')
            {
                return (c - 'A' + 10);
            }

            throw new ArgumentException(Resources.GetResourceString("BadHexDigit", c), "traits");
        }

        private NameInfo UpdateDescriptor(string name, TraceLoggingEventTypes eventInfo, ref EventSourceOptions options, out EventDescriptor descriptor)
        {
            NameInfo nameInfo = null;
            int identity = 0;
            byte level = (options.valuesSet & EventSourceOptions.levelSet) != 0 ? options.level : eventInfo.level;
            byte opcode = (options.valuesSet & EventSourceOptions.opcodeSet) != 0 ? options.opcode : eventInfo.opcode;
            EventTags tags = (options.valuesSet & EventSourceOptions.tagsSet) != 0 ? options.tags : eventInfo.Tags;
            EventKeywords keywords = (options.valuesSet & EventSourceOptions.keywordsSet) != 0 ? options.keywords : eventInfo.keywords;
            if (this.IsEnabled((EventLevel)level, keywords))
            {
                nameInfo = eventInfo.GetNameInfo(name ?? eventInfo.Name, tags);
                identity = nameInfo.identity;
            }

            descriptor = new EventDescriptor(identity, level, opcode, (long)keywords);
            return nameInfo;
        }
    }
}