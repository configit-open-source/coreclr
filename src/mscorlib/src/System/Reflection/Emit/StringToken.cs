namespace System.Reflection.Emit
{
    public struct StringToken
    {
        internal int m_string;
        internal StringToken(int str)
        {
            m_string = str;
        }

        public int Token
        {
            get
            {
                return m_string;
            }
        }

        public override int GetHashCode()
        {
            return m_string;
        }

        public override bool Equals(Object obj)
        {
            if (obj is StringToken)
                return Equals((StringToken)obj);
            else
                return false;
        }

        public bool Equals(StringToken obj)
        {
            return obj.m_string == m_string;
        }

        public static bool operator ==(StringToken a, StringToken b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(StringToken a, StringToken b)
        {
            return !(a == b);
        }
    }
}