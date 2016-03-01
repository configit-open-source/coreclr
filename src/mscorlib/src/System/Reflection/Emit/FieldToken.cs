namespace System.Reflection.Emit
{
    using System;
    using System.Reflection;
    using System.Security.Permissions;

    public struct FieldToken
    {
        public static readonly FieldToken Empty = new FieldToken();
        internal int m_fieldTok;
        internal Object m_class;
        internal FieldToken(int field, Type fieldClass)
        {
            m_fieldTok = field;
            m_class = fieldClass;
        }

        public int Token
        {
            get
            {
                return m_fieldTok;
            }
        }

        public override int GetHashCode()
        {
            return (m_fieldTok);
        }

        public override bool Equals(Object obj)
        {
            if (obj is FieldToken)
                return Equals((FieldToken)obj);
            else
                return false;
        }

        public bool Equals(FieldToken obj)
        {
            return obj.m_fieldTok == m_fieldTok && obj.m_class == m_class;
        }

        public static bool operator ==(FieldToken a, FieldToken b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(FieldToken a, FieldToken b)
        {
            return !(a == b);
        }
    }
}