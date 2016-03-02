using System.Collections.Generic;
using System.Reflection;
using System.Security;

namespace System.Runtime.Serialization
{
    public sealed class SerializationInfo
    {
        private const int defaultSize = 4;
        private const string s_mscorlibAssemblySimpleName = "mscorlib";
        private const string s_mscorlibFileName = s_mscorlibAssemblySimpleName + ".dll";
        internal String[] m_members;
        internal Object[] m_data;
        internal Type[] m_types;
        private Dictionary<string, int> m_nameToIndex;
        internal int m_currMember;
        internal IFormatterConverter m_converter;
        private String m_fullTypeName;
        private String m_assemName;
        private Type objectType;
        private bool isFullTypeNameSetExplicit;
        private bool isAssemblyNameSetExplicit;
        private bool requireSameTokenInPartialTrust;
        public SerializationInfo(Type type, IFormatterConverter converter): this (type, converter, false)
        {
        }

        public SerializationInfo(Type type, IFormatterConverter converter, bool requireSameTokenInPartialTrust)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (converter == null)
            {
                throw new ArgumentNullException("converter");
            }

                        objectType = type;
            m_fullTypeName = type.FullName;
            m_assemName = type.Module.Assembly.FullName;
            m_members = new String[defaultSize];
            m_data = new Object[defaultSize];
            m_types = new Type[defaultSize];
            m_nameToIndex = new Dictionary<string, int>();
            m_converter = converter;
            this.requireSameTokenInPartialTrust = requireSameTokenInPartialTrust;
        }

        public String FullTypeName
        {
            get
            {
                return m_fullTypeName;
            }

            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }

                                m_fullTypeName = value;
                isFullTypeNameSetExplicit = true;
            }
        }

        public String AssemblyName
        {
            get
            {
                return m_assemName;
            }

            [SecuritySafeCritical]
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }

                                if (this.requireSameTokenInPartialTrust)
                {
                    DemandForUnsafeAssemblyNameAssignments(this.m_assemName, value);
                }

                m_assemName = value;
                isAssemblyNameSetExplicit = true;
            }
        }

        public void SetType(Type type)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

                        if (this.requireSameTokenInPartialTrust)
            {
                DemandForUnsafeAssemblyNameAssignments(this.ObjectType.Assembly.FullName, type.Assembly.FullName);
            }

            if (!Object.ReferenceEquals(objectType, type))
            {
                objectType = type;
                m_fullTypeName = type.FullName;
                m_assemName = type.Module.Assembly.FullName;
                isFullTypeNameSetExplicit = false;
                isAssemblyNameSetExplicit = false;
            }
        }

        private static bool Compare(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                        return false;
                }

                return true;
            }
        }

        internal static void DemandForUnsafeAssemblyNameAssignments(string originalAssemblyName, string newAssemblyName)
        {
            if (!IsAssemblyNameAssignmentSafe(originalAssemblyName, newAssemblyName))
            {
                CodeAccessPermission.Demand(PermissionType.SecuritySerialization);
            }
        }

        internal static bool IsAssemblyNameAssignmentSafe(string originalAssemblyName, string newAssemblyName)
        {
            if (originalAssemblyName == newAssemblyName)
            {
                return true;
            }

            AssemblyName originalAssembly = new AssemblyName(originalAssemblyName);
            AssemblyName newAssembly = new AssemblyName(newAssemblyName);
            if (string.Equals(newAssembly.Name, s_mscorlibAssemblySimpleName, StringComparison.OrdinalIgnoreCase) || string.Equals(newAssembly.Name, s_mscorlibFileName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Compare(originalAssembly.GetPublicKeyToken(), newAssembly.GetPublicKeyToken());
        }

        public int MemberCount
        {
            get
            {
                return m_currMember;
            }
        }

        public Type ObjectType
        {
            get
            {
                return objectType;
            }
        }

        public bool IsFullTypeNameSetExplicit
        {
            get
            {
                return isFullTypeNameSetExplicit;
            }
        }

        public bool IsAssemblyNameSetExplicit
        {
            get
            {
                return isAssemblyNameSetExplicit;
            }
        }

        public SerializationInfoEnumerator GetEnumerator()
        {
            return new SerializationInfoEnumerator(m_members, m_data, m_types, m_currMember);
        }

        private void ExpandArrays()
        {
            int newSize;
                        newSize = (m_currMember * 2);
            if (newSize < m_currMember)
            {
                if (Int32.MaxValue > m_currMember)
                {
                    newSize = Int32.MaxValue;
                }
            }

            String[] newMembers = new String[newSize];
            Object[] newData = new Object[newSize];
            Type[] newTypes = new Type[newSize];
            Array.Copy(m_members, newMembers, m_currMember);
            Array.Copy(m_data, newData, m_currMember);
            Array.Copy(m_types, newTypes, m_currMember);
            m_members = newMembers;
            m_data = newData;
            m_types = newTypes;
        }

        public void AddValue(String name, Object value, Type type)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

                        AddValueInternal(name, value, type);
        }

        public void AddValue(String name, Object value)
        {
            if (null == value)
            {
                AddValue(name, value, typeof (Object));
            }
            else
            {
                AddValue(name, value, value.GetType());
            }
        }

        public void AddValue(String name, bool value)
        {
            AddValue(name, (Object)value, typeof (bool));
        }

        public void AddValue(String name, char value)
        {
            AddValue(name, (Object)value, typeof (char));
        }

        public void AddValue(String name, sbyte value)
        {
            AddValue(name, (Object)value, typeof (sbyte));
        }

        public void AddValue(String name, byte value)
        {
            AddValue(name, (Object)value, typeof (byte));
        }

        public void AddValue(String name, short value)
        {
            AddValue(name, (Object)value, typeof (short));
        }

        public void AddValue(String name, ushort value)
        {
            AddValue(name, (Object)value, typeof (ushort));
        }

        public void AddValue(String name, int value)
        {
            AddValue(name, (Object)value, typeof (int));
        }

        public void AddValue(String name, uint value)
        {
            AddValue(name, (Object)value, typeof (uint));
        }

        public void AddValue(String name, long value)
        {
            AddValue(name, (Object)value, typeof (long));
        }

        public void AddValue(String name, ulong value)
        {
            AddValue(name, (Object)value, typeof (ulong));
        }

        public void AddValue(String name, float value)
        {
            AddValue(name, (Object)value, typeof (float));
        }

        public void AddValue(String name, double value)
        {
            AddValue(name, (Object)value, typeof (double));
        }

        public void AddValue(String name, decimal value)
        {
            AddValue(name, (Object)value, typeof (decimal));
        }

        public void AddValue(String name, DateTime value)
        {
            AddValue(name, (Object)value, typeof (DateTime));
        }

        internal void AddValueInternal(String name, Object value, Type type)
        {
            if (m_nameToIndex.ContainsKey(name))
            {
                BCLDebug.Trace("SER", "[SerializationInfo.AddValue]Tried to add ", name, " twice to the SI.");
                throw new SerializationException(Environment.GetResourceString("Serialization_SameNameTwice"));
            }

            m_nameToIndex.Add(name, m_currMember);
            if (m_currMember >= m_members.Length)
            {
                ExpandArrays();
            }

            m_members[m_currMember] = name;
            m_data[m_currMember] = value;
            m_types[m_currMember] = type;
            m_currMember++;
        }

        internal void UpdateValue(String name, Object value, Type type)
        {
                                                int index = FindElement(name);
            if (index < 0)
            {
                AddValueInternal(name, value, type);
            }
            else
            {
                m_data[index] = value;
                m_types[index] = type;
            }
        }

        private int FindElement(String name)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

                        BCLDebug.Trace("SER", "[SerializationInfo.FindElement]Looking for ", name, " CurrMember is: ", m_currMember);
            int index;
            if (m_nameToIndex.TryGetValue(name, out index))
            {
                return index;
            }

            return -1;
        }

        private Object GetElement(String name, out Type foundType)
        {
            int index = FindElement(name);
            if (index == -1)
            {
                throw new SerializationException(Environment.GetResourceString("Serialization_NotFound", name));
            }

                                    foundType = m_types[index];
                        return m_data[index];
        }

        private Object GetElementNoThrow(String name, out Type foundType)
        {
            int index = FindElement(name);
            if (index == -1)
            {
                foundType = null;
                return null;
            }

                                    foundType = m_types[index];
                        return m_data[index];
        }

        public Object GetValue(String name, Type type)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

                        RuntimeType rt = type as RuntimeType;
            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, type) || type.IsAssignableFrom(foundType) || value == null)
            {
                return value;
            }

                        return m_converter.Convert(value, type);
        }

        internal Object GetValueNoThrow(String name, Type type)
        {
            Type foundType;
            Object value;
                                    value = GetElementNoThrow(name, out foundType);
            if (value == null)
                return null;
            if (Object.ReferenceEquals(foundType, type) || type.IsAssignableFrom(foundType) || value == null)
            {
                return value;
            }

                        return m_converter.Convert(value, type);
        }

        public bool GetBoolean(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (bool)))
            {
                return (bool)value;
            }

            return m_converter.ToBoolean(value);
        }

        public char GetChar(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (char)))
            {
                return (char)value;
            }

            return m_converter.ToChar(value);
        }

        public sbyte GetSByte(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (sbyte)))
            {
                return (sbyte)value;
            }

            return m_converter.ToSByte(value);
        }

        public byte GetByte(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (byte)))
            {
                return (byte)value;
            }

            return m_converter.ToByte(value);
        }

        public short GetInt16(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (short)))
            {
                return (short)value;
            }

            return m_converter.ToInt16(value);
        }

        public ushort GetUInt16(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (ushort)))
            {
                return (ushort)value;
            }

            return m_converter.ToUInt16(value);
        }

        public int GetInt32(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (int)))
            {
                return (int)value;
            }

            return m_converter.ToInt32(value);
        }

        public uint GetUInt32(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (uint)))
            {
                return (uint)value;
            }

            return m_converter.ToUInt32(value);
        }

        public long GetInt64(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (long)))
            {
                return (long)value;
            }

            return m_converter.ToInt64(value);
        }

        public ulong GetUInt64(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (ulong)))
            {
                return (ulong)value;
            }

            return m_converter.ToUInt64(value);
        }

        public float GetSingle(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (float)))
            {
                return (float)value;
            }

            return m_converter.ToSingle(value);
        }

        public double GetDouble(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (double)))
            {
                return (double)value;
            }

            return m_converter.ToDouble(value);
        }

        public decimal GetDecimal(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (decimal)))
            {
                return (decimal)value;
            }

            return m_converter.ToDecimal(value);
        }

        public DateTime GetDateTime(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (DateTime)))
            {
                return (DateTime)value;
            }

            return m_converter.ToDateTime(value);
        }

        public String GetString(String name)
        {
            Type foundType;
            Object value;
            value = GetElement(name, out foundType);
            if (Object.ReferenceEquals(foundType, typeof (String)) || value == null)
            {
                return (String)value;
            }

            return m_converter.ToString(value);
        }

        internal string[] MemberNames
        {
            get
            {
                return m_members;
            }
        }

        internal object[] MemberValues
        {
            get
            {
                return m_data;
            }
        }
    }
}