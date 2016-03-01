namespace System.Runtime.InteropServices
{
    using System;
    using System.Security.Permissions;

    public sealed class ErrorWrapper
    {
        public ErrorWrapper(int errorCode)
        {
            m_ErrorCode = errorCode;
        }

        public ErrorWrapper(Object errorCode)
        {
            if (!(errorCode is int))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeInt32"), "errorCode");
            m_ErrorCode = (int)errorCode;
        }

        public ErrorWrapper(Exception e)
        {
            m_ErrorCode = Marshal.GetHRForException(e);
        }

        public int ErrorCode
        {
            get
            {
                return m_ErrorCode;
            }
        }

        private int m_ErrorCode;
    }
}