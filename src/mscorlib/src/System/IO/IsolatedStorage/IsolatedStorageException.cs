namespace System.IO.IsolatedStorage
{
    using System;
    using System.Runtime.Serialization;

    public class IsolatedStorageException : Exception
    {
        internal Exception m_UnderlyingException;
        public IsolatedStorageException(): base (Environment.GetResourceString("IsolatedStorage_Exception"))
        {
            SetErrorCode(__HResults.COR_E_ISOSTORE);
        }

        public IsolatedStorageException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ISOSTORE);
        }

        public IsolatedStorageException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_ISOSTORE);
        }

        protected IsolatedStorageException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}