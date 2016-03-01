using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Diagnostics.Tracing
{
    internal abstract class TraceLoggingTypeInfo
    {
        private readonly string name;
        private readonly EventKeywords keywords;
        private readonly EventLevel level = (EventLevel)(-1);
        private readonly EventOpcode opcode = (EventOpcode)(-1);
        private readonly EventTags tags;
        private readonly Type dataType;
        private readonly Func<object, PropertyValue> propertyValueFactory;
        internal TraceLoggingTypeInfo(Type dataType)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException("dataType");
            }

            Contract.EndContractBlock();
            this.name = dataType.Name;
            this.dataType = dataType;
            this.propertyValueFactory = PropertyValue.GetFactory(dataType);
        }

        internal TraceLoggingTypeInfo(Type dataType, string name, EventLevel level, EventOpcode opcode, EventKeywords keywords, EventTags tags)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException("dataType");
            }

            if (name == null)
            {
                throw new ArgumentNullException("eventName");
            }

            Contract.EndContractBlock();
            Statics.CheckName(name);
            this.name = name;
            this.keywords = keywords;
            this.level = level;
            this.opcode = opcode;
            this.tags = tags;
            this.dataType = dataType;
            this.propertyValueFactory = PropertyValue.GetFactory(dataType);
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public EventLevel Level
        {
            get
            {
                return this.level;
            }
        }

        public EventOpcode Opcode
        {
            get
            {
                return this.opcode;
            }
        }

        public EventKeywords Keywords
        {
            get
            {
                return this.keywords;
            }
        }

        public EventTags Tags
        {
            get
            {
                return this.tags;
            }
        }

        internal Type DataType
        {
            get
            {
                return this.dataType;
            }
        }

        internal Func<object, PropertyValue> PropertyValueFactory
        {
            get
            {
                return this.propertyValueFactory;
            }
        }

        public abstract void WriteMetadata(TraceLoggingMetadataCollector collector, string name, EventFieldFormat format);
        public abstract void WriteData(TraceLoggingDataCollector collector, PropertyValue value);
        public virtual object GetData(object value)
        {
            return value;
        }

        private static Dictionary<Type, TraceLoggingTypeInfo> threadCache;
        public static TraceLoggingTypeInfo GetInstance(Type type, List<Type> recursionCheck)
        {
            var cache = threadCache ?? (threadCache = new Dictionary<Type, TraceLoggingTypeInfo>());
            TraceLoggingTypeInfo instance;
            if (!cache.TryGetValue(type, out instance))
            {
                if (recursionCheck == null)
                    recursionCheck = new List<Type>();
                var recursionCheckCount = recursionCheck.Count;
                instance = Statics.CreateDefaultTypeInfo(type, recursionCheck);
                cache[type] = instance;
                recursionCheck.RemoveRange(recursionCheckCount, recursionCheck.Count - recursionCheckCount);
            }

            return instance;
        }
    }
}