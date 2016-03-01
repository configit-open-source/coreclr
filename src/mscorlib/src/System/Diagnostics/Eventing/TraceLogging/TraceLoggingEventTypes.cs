using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Diagnostics.Tracing
{
    public class TraceLoggingEventTypes
    {
        internal readonly TraceLoggingTypeInfo[] typeInfos;
        internal readonly string name;
        internal readonly EventTags tags;
        internal readonly byte level;
        internal readonly byte opcode;
        internal readonly EventKeywords keywords;
        internal readonly byte[] typeMetadata;
        internal readonly int scratchSize;
        internal readonly int dataCount;
        internal readonly int pinCount;
        private ConcurrentSet<KeyValuePair<string, EventTags>, NameInfo> nameInfos;
        internal TraceLoggingEventTypes(string name, EventTags tags, params Type[] types): this (tags, name, MakeArray(types))
        {
            return;
        }

        internal TraceLoggingEventTypes(string name, EventTags tags, params TraceLoggingTypeInfo[] typeInfos): this (tags, name, MakeArray(typeInfos))
        {
            return;
        }

        internal TraceLoggingEventTypes(string name, EventTags tags, System.Reflection.ParameterInfo[] paramInfos)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            Contract.EndContractBlock();
            this.typeInfos = MakeArray(paramInfos);
            this.name = name;
            this.tags = tags;
            this.level = Statics.DefaultLevel;
            var collector = new TraceLoggingMetadataCollector();
            for (int i = 0; i < typeInfos.Length; ++i)
            {
                var typeInfo = typeInfos[i];
                this.level = Statics.Combine((int)typeInfo.Level, this.level);
                this.opcode = Statics.Combine((int)typeInfo.Opcode, this.opcode);
                this.keywords |= typeInfo.Keywords;
                var paramName = paramInfos[i].Name;
                if (Statics.ShouldOverrideFieldName(paramName))
                {
                    paramName = typeInfo.Name;
                }

                typeInfo.WriteMetadata(collector, paramName, EventFieldFormat.Default);
            }

            this.typeMetadata = collector.GetMetadata();
            this.scratchSize = collector.ScratchSize;
            this.dataCount = collector.DataCount;
            this.pinCount = collector.PinCount;
        }

        private TraceLoggingEventTypes(EventTags tags, string defaultName, TraceLoggingTypeInfo[] typeInfos)
        {
            if (defaultName == null)
            {
                throw new ArgumentNullException("defaultName");
            }

            Contract.EndContractBlock();
            this.typeInfos = typeInfos;
            this.name = defaultName;
            this.tags = tags;
            this.level = Statics.DefaultLevel;
            var collector = new TraceLoggingMetadataCollector();
            foreach (var typeInfo in typeInfos)
            {
                this.level = Statics.Combine((int)typeInfo.Level, this.level);
                this.opcode = Statics.Combine((int)typeInfo.Opcode, this.opcode);
                this.keywords |= typeInfo.Keywords;
                typeInfo.WriteMetadata(collector, null, EventFieldFormat.Default);
            }

            this.typeMetadata = collector.GetMetadata();
            this.scratchSize = collector.ScratchSize;
            this.dataCount = collector.DataCount;
            this.pinCount = collector.PinCount;
        }

        internal string Name
        {
            get
            {
                return this.name;
            }
        }

        internal EventLevel Level
        {
            get
            {
                return (EventLevel)this.level;
            }
        }

        internal EventOpcode Opcode
        {
            get
            {
                return (EventOpcode)this.opcode;
            }
        }

        internal EventKeywords Keywords
        {
            get
            {
                return (EventKeywords)this.keywords;
            }
        }

        internal EventTags Tags
        {
            get
            {
                return this.tags;
            }
        }

        internal NameInfo GetNameInfo(string name, EventTags tags)
        {
            var ret = this.nameInfos.TryGet(new KeyValuePair<string, EventTags>(name, tags));
            if (ret == null)
            {
                ret = this.nameInfos.GetOrAdd(new NameInfo(name, tags, this.typeMetadata.Length));
            }

            return ret;
        }

        private TraceLoggingTypeInfo[] MakeArray(System.Reflection.ParameterInfo[] paramInfos)
        {
            if (paramInfos == null)
            {
                throw new ArgumentNullException("paramInfos");
            }

            Contract.EndContractBlock();
            var recursionCheck = new List<Type>(paramInfos.Length);
            var result = new TraceLoggingTypeInfo[paramInfos.Length];
            for (int i = 0; i < paramInfos.Length; ++i)
            {
                result[i] = TraceLoggingTypeInfo.GetInstance(paramInfos[i].ParameterType, recursionCheck);
            }

            return result;
        }

        private static TraceLoggingTypeInfo[] MakeArray(Type[] types)
        {
            if (types == null)
            {
                throw new ArgumentNullException("types");
            }

            Contract.EndContractBlock();
            var recursionCheck = new List<Type>(types.Length);
            var result = new TraceLoggingTypeInfo[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                result[i] = TraceLoggingTypeInfo.GetInstance(types[i], recursionCheck);
            }

            return result;
        }

        private static TraceLoggingTypeInfo[] MakeArray(TraceLoggingTypeInfo[] typeInfos)
        {
            if (typeInfos == null)
            {
                throw new ArgumentNullException("typeInfos");
            }

            Contract.EndContractBlock();
            return (TraceLoggingTypeInfo[])typeInfos.Clone();
            ;
        }
    }
}