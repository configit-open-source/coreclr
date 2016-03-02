namespace System.Diagnostics.Tracing
{
    public struct EventSourceOptions
    {
        internal EventKeywords keywords;
        internal EventTags tags;
        internal EventActivityOptions activityOptions;
        internal byte level;
        internal byte opcode;
        internal byte valuesSet;
        internal const byte keywordsSet = 0x1;
        internal const byte tagsSet = 0x2;
        internal const byte levelSet = 0x4;
        internal const byte opcodeSet = 0x8;
        internal const byte activityOptionsSet = 0x10;
        public EventLevel Level
        {
            get
            {
                return (EventLevel)this.level;
            }

            set
            {
                this.level = checked ((byte)value);
                this.valuesSet |= levelSet;
            }
        }

        public EventOpcode Opcode
        {
            get
            {
                return (EventOpcode)this.opcode;
            }

            set
            {
                this.opcode = checked ((byte)value);
                this.valuesSet |= opcodeSet;
            }
        }

        internal bool IsOpcodeSet
        {
            get
            {
                return (this.valuesSet & opcodeSet) != 0;
            }
        }

        public EventKeywords Keywords
        {
            get
            {
                return this.keywords;
            }

            set
            {
                this.keywords = value;
                this.valuesSet |= keywordsSet;
            }
        }

        public EventTags Tags
        {
            get
            {
                return this.tags;
            }

            set
            {
                this.tags = value;
                this.valuesSet |= tagsSet;
            }
        }

        public EventActivityOptions ActivityOptions
        {
            get
            {
                return this.activityOptions;
            }

            set
            {
                this.activityOptions = value;
                this.valuesSet |= activityOptionsSet;
            }
        }
    }
}