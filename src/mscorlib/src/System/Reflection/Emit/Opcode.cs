using System.Threading;

namespace System.Reflection.Emit
{
    public struct OpCode
    {
        internal const int OperandTypeMask = 0x1F;
        internal const int FlowControlShift = 5;
        internal const int FlowControlMask = 0x0F;
        internal const int OpCodeTypeShift = 9;
        internal const int OpCodeTypeMask = 0x07;
        internal const int StackBehaviourPopShift = 12;
        internal const int StackBehaviourPushShift = 17;
        internal const int StackBehaviourMask = 0x1F;
        internal const int SizeShift = 22;
        internal const int SizeMask = 0x03;
        internal const int EndsUncondJmpBlkFlag = 0x01000000;
        internal const int StackChangeShift = 28;
        private OpCodeValues m_value;
        private int m_flags;
        internal OpCode(OpCodeValues value, int flags)
        {
            m_value = value;
            m_flags = flags;
        }

        internal bool EndsUncondJmpBlk()
        {
            return (m_flags & EndsUncondJmpBlkFlag) != 0;
        }

        internal int StackChange()
        {
            return (m_flags >> StackChangeShift);
        }

        public OperandType OperandType
        {
            get
            {
                return (OperandType)(m_flags & OperandTypeMask);
            }
        }

        public FlowControl FlowControl
        {
            get
            {
                return (FlowControl)((m_flags >> FlowControlShift) & FlowControlMask);
            }
        }

        public OpCodeType OpCodeType
        {
            get
            {
                return (OpCodeType)((m_flags >> OpCodeTypeShift) & OpCodeTypeMask);
            }
        }

        public StackBehaviour StackBehaviourPop
        {
            get
            {
                return (StackBehaviour)((m_flags >> StackBehaviourPopShift) & StackBehaviourMask);
            }
        }

        public StackBehaviour StackBehaviourPush
        {
            get
            {
                return (StackBehaviour)((m_flags >> StackBehaviourPushShift) & StackBehaviourMask);
            }
        }

        public int Size
        {
            get
            {
                return (m_flags >> SizeShift) & SizeMask;
            }
        }

        public short Value
        {
            get
            {
                return (short)m_value;
            }
        }

        private static volatile string[] g_nameCache;
        public String Name
        {
            get
            {
                if (Size == 0)
                    return null;
                string[] nameCache = g_nameCache;
                if (nameCache == null)
                {
                    nameCache = new String[0x11f];
                    g_nameCache = nameCache;
                }

                OpCodeValues opCodeValue = (OpCodeValues)(ushort)Value;
                int idx = (int)opCodeValue;
                if (idx > 0xFF)
                {
                    if (idx >= 0xfe00 && idx <= 0xfe1e)
                    {
                        idx = 0x100 + (idx - 0xfe00);
                    }
                    else
                    {
                        return null;
                    }
                }

                String name = Volatile.Read(ref nameCache[idx]);
                if (name != null)
                    return name;
                name = Enum.GetName(typeof (OpCodeValues), opCodeValue).ToLowerInvariant().Replace("_", ".");
                Volatile.Write(ref nameCache[idx], name);
                return name;
            }
        }

        public override bool Equals(Object obj)
        {
            if (obj is OpCode)
                return Equals((OpCode)obj);
            else
                return false;
        }

        public bool Equals(OpCode obj)
        {
            return obj.Value == Value;
        }

        public static bool operator ==(OpCode a, OpCode b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(OpCode a, OpCode b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override String ToString()
        {
            return Name;
        }
    }
}