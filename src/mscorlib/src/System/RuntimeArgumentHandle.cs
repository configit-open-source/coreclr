namespace System
{
    public struct RuntimeArgumentHandle
    {
        private IntPtr m_ptr;
        internal IntPtr Value
        {
            get
            {
                return m_ptr;
            }
        }
    }
}