using System.Collections;

namespace System.Runtime.Serialization
{
    public struct SerializationEntry
    {
        private Type m_type;
        private Object m_value;
        private String m_name;
        public Object Value
        {
            get
            {
                return m_value;
            }
        }

        public String Name
        {
            get
            {
                return m_name;
            }
        }

        public Type ObjectType
        {
            get
            {
                return m_type;
            }
        }

        internal SerializationEntry(String entryName, Object entryValue, Type entryType)
        {
            m_value = entryValue;
            m_name = entryName;
            m_type = entryType;
        }
    }

    public sealed class SerializationInfoEnumerator : IEnumerator
    {
        String[] m_members;
        Object[] m_data;
        Type[] m_types;
        int m_numItems;
        int m_currItem;
        bool m_current;
        internal SerializationInfoEnumerator(String[] members, Object[] info, Type[] types, int numItems)
        {
                                                                                                m_members = members;
            m_data = info;
            m_types = types;
            m_numItems = numItems - 1;
            m_currItem = -1;
            m_current = false;
        }

        public bool MoveNext()
        {
            if (m_currItem < m_numItems)
            {
                m_currItem++;
                m_current = true;
            }
            else
            {
                m_current = false;
            }

            return m_current;
        }

        Object IEnumerator.Current
        {
            get
            {
                if (m_current == false)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }

                return (Object)(new SerializationEntry(m_members[m_currItem], m_data[m_currItem], m_types[m_currItem]));
            }
        }

        public SerializationEntry Current
        {
            get
            {
                if (m_current == false)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }

                return (new SerializationEntry(m_members[m_currItem], m_data[m_currItem], m_types[m_currItem]));
            }
        }

        public void Reset()
        {
            m_currItem = -1;
            m_current = false;
        }

        public String Name
        {
            get
            {
                if (m_current == false)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }

                return m_members[m_currItem];
            }
        }

        public Object Value
        {
            get
            {
                if (m_current == false)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }

                return m_data[m_currItem];
            }
        }

        public Type ObjectType
        {
            get
            {
                if (m_current == false)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EnumOpCantHappen"));
                }

                return m_types[m_currItem];
            }
        }
    }
}