namespace System.Reflection.Emit
{
    public struct SignatureToken
    {
        public static readonly SignatureToken Empty = new SignatureToken();
        internal int m_signature;
        internal ModuleBuilder m_moduleBuilder;
        internal SignatureToken(int str, ModuleBuilder mod)
        {
            m_signature = str;
            m_moduleBuilder = mod;
        }

        public int Token
        {
            get
            {
                return m_signature;
            }
        }

        public override int GetHashCode()
        {
            return m_signature;
        }

        public override bool Equals(Object obj)
        {
            if (obj is SignatureToken)
                return Equals((SignatureToken)obj);
            else
                return false;
        }

        public bool Equals(SignatureToken obj)
        {
            return obj.m_signature == m_signature;
        }

        public static bool operator ==(SignatureToken a, SignatureToken b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(SignatureToken a, SignatureToken b)
        {
            return !(a == b);
        }
    }
}