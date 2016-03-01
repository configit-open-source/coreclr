namespace System.Runtime.InteropServices
{
    using System;
    using System.Security;
    using System.Security.Permissions;

    public sealed class DispatchWrapper
    {
        public DispatchWrapper(Object obj)
        {
            if (obj != null)
            {
                IntPtr pdisp = Marshal.GetIDispatchForObject(obj);
                Marshal.Release(pdisp);
            }

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