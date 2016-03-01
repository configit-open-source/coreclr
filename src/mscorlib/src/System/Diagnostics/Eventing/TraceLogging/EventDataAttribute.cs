using System;

namespace System.Diagnostics.Tracing
{
    public class EventDataAttribute : Attribute
    {
        private EventLevel level = (EventLevel)(-1);
        private EventOpcode opcode = (EventOpcode)(-1);
        public string Name
        {
            get;
            set;
        }

        internal EventLevel Level
        {
            get
            {
                return this.level;
            }

            set
            {
                this.level = value;
            }
        }

        internal EventOpcode Opcode
        {
            get
            {
                return this.opcode;
            }

            set
            {
                this.opcode = value;
            }
        }

        internal EventKeywords Keywords
        {
            get;
            set;
        }

        internal EventTags Tags
        {
            get;
            set;
        }
    }
}