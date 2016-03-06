using System.Runtime.Serialization;

namespace System.IO.IsolatedStorage
{
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
    }
}