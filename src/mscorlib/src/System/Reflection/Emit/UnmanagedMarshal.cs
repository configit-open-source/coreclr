namespace System.Reflection.Emit
{
    using System.Runtime.InteropServices;
    using System;
    using System.Security.Permissions;

    public sealed class UnmanagedMarshal
    {
        public static UnmanagedMarshal DefineUnmanagedMarshal(UnmanagedType unmanagedType)
        {
            if (unmanagedType == UnmanagedType.ByValTStr || unmanagedType == UnmanagedType.SafeArray || unmanagedType == UnmanagedType.CustomMarshaler || unmanagedType == UnmanagedType.ByValArray || unmanagedType == UnmanagedType.LPArray)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_NotASimpleNativeType"));
            }

            return new UnmanagedMarshal(unmanagedType, Guid.Empty, 0, (UnmanagedType)0);
        }

        public static UnmanagedMarshal DefineByValTStr(int elemCount)
        {
            return new UnmanagedMarshal(UnmanagedType.ByValTStr, Guid.Empty, elemCount, (UnmanagedType)0);
        }

        public static UnmanagedMarshal DefineSafeArray(UnmanagedType elemType)
        {
            return new UnmanagedMarshal(UnmanagedType.SafeArray, Guid.Empty, 0, elemType);
        }

        public static UnmanagedMarshal DefineByValArray(int elemCount)
        {
            return new UnmanagedMarshal(UnmanagedType.ByValArray, Guid.Empty, elemCount, (UnmanagedType)0);
        }

        public static UnmanagedMarshal DefineLPArray(UnmanagedType elemType)
        {
            return new UnmanagedMarshal(UnmanagedType.LPArray, Guid.Empty, 0, elemType);
        }

        public UnmanagedType GetUnmanagedType
        {
            get
            {
                return m_unmanagedType;
            }
        }

        public Guid IIDGuid
        {
            get
            {
                if (m_unmanagedType == UnmanagedType.CustomMarshaler)
                    return m_guid;
                throw new ArgumentException(Environment.GetResourceString("Argument_NotACustomMarshaler"));
            }
        }

        public int ElementCount
        {
            get
            {
                if (m_unmanagedType != UnmanagedType.ByValArray && m_unmanagedType != UnmanagedType.ByValTStr)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NoUnmanagedElementCount"));
                }

                return m_numElem;
            }
        }

        public UnmanagedType BaseType
        {
            get
            {
                if (m_unmanagedType != UnmanagedType.LPArray && m_unmanagedType != UnmanagedType.SafeArray)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NoNestedMarshal"));
                }

                return m_baseType;
            }
        }

        private UnmanagedMarshal(UnmanagedType unmanagedType, Guid guid, int numElem, UnmanagedType type)
        {
            m_unmanagedType = unmanagedType;
            m_guid = guid;
            m_numElem = numElem;
            m_baseType = type;
        }

        internal UnmanagedType m_unmanagedType;
        internal Guid m_guid;
        internal int m_numElem;
        internal UnmanagedType m_baseType;
        internal byte[] InternalGetBytes()
        {
            byte[] buf;
            if (m_unmanagedType == UnmanagedType.SafeArray || m_unmanagedType == UnmanagedType.LPArray)
            {
                int cBuf = 2;
                buf = new byte[cBuf];
                buf[0] = (byte)(m_unmanagedType);
                buf[1] = (byte)(m_baseType);
                return buf;
            }
            else if (m_unmanagedType == UnmanagedType.ByValArray || m_unmanagedType == UnmanagedType.ByValTStr)
            {
                int cBuf;
                int iBuf = 0;
                if (m_numElem <= 0x7f)
                    cBuf = 1;
                else if (m_numElem <= 0x3FFF)
                    cBuf = 2;
                else
                    cBuf = 4;
                cBuf = cBuf + 1;
                buf = new byte[cBuf];
                buf[iBuf++] = (byte)(m_unmanagedType);
                if (m_numElem <= 0x7F)
                {
                    buf[iBuf++] = (byte)(m_numElem & 0xFF);
                }
                else if (m_numElem <= 0x3FFF)
                {
                    buf[iBuf++] = (byte)((m_numElem >> 8) | 0x80);
                    buf[iBuf++] = (byte)(m_numElem & 0xFF);
                }
                else if (m_numElem <= 0x1FFFFFFF)
                {
                    buf[iBuf++] = (byte)((m_numElem >> 24) | 0xC0);
                    buf[iBuf++] = (byte)((m_numElem >> 16) & 0xFF);
                    buf[iBuf++] = (byte)((m_numElem >> 8) & 0xFF);
                    buf[iBuf++] = (byte)((m_numElem) & 0xFF);
                }

                return buf;
            }

            buf = new byte[1];
            buf[0] = (byte)(m_unmanagedType);
            return buf;
        }
    }
}