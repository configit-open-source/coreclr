namespace System.Runtime.InteropServices
{
    public sealed class BStrWrapper
    {
        public BStrWrapper(String value)
        {
            m_WrappedObject = value;
        }

        public BStrWrapper(Object value)
        {
            m_WrappedObject = (String)value;
        }

        public String WrappedObject
        {
            get
            {
                return m_WrappedObject;
            }
        }

        private String m_WrappedObject;
    }
}