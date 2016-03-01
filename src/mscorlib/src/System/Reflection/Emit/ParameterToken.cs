namespace System.Reflection.Emit
{
    using System;
    using System.Reflection;
    using System.Security.Permissions;

    public struct ParameterToken
    {
        public static readonly ParameterToken Empty = new ParameterToken();
        internal int m_tkParameter;
        internal ParameterToken(int tkParam)
        {
            m_tkParameter = tkParam;
        }

        public int Token
        {
            get
            {
                return m_tkParameter;
            }
        }

        public override int GetHashCode()
        {
            return m_tkParameter;
        }

        public override bool Equals(Object obj)
        {
            if (obj is ParameterToken)
                return Equals((ParameterToken)obj);
            else
                return false;
        }

        public bool Equals(ParameterToken obj)
        {
            return obj.m_tkParameter == m_tkParameter;
        }

        public static bool operator ==(ParameterToken a, ParameterToken b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ParameterToken a, ParameterToken b)
        {
            return !(a == b);
        }
    }
}