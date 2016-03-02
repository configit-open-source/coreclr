namespace System
{
    public sealed class CLSCompliantAttribute : Attribute
    {
        private bool m_compliant;
        public CLSCompliantAttribute(bool isCompliant)
        {
            m_compliant = isCompliant;
        }

        public bool IsCompliant
        {
            get
            {
                return m_compliant;
            }
        }
    }
}