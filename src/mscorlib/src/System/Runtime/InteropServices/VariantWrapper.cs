namespace System.Runtime.InteropServices
{
    public sealed class VariantWrapper
    {
        public VariantWrapper(Object obj)
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