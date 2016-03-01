namespace System.Runtime.InteropServices
{
    public sealed class UnknownWrapper
    {
        public UnknownWrapper(Object obj)
        {
            m_WrappedObject = obj;
        }

        public Object WrappedObject
        {
            get
            {
                return m_WrappedObject;
            }
        }

        private Object m_WrappedObject;
    }
}