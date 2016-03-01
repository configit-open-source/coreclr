using System.Runtime.Serialization;

namespace System.Threading
{
    public class AbandonedMutexException : SystemException
    {
        private int m_MutexIndex = -1;
        private Mutex m_Mutex = null;
        public AbandonedMutexException(): base (Environment.GetResourceString("Threading.AbandonedMutexException"))
        {
            SetErrorCode(__HResults.COR_E_ABANDONEDMUTEX);
        }

        public AbandonedMutexException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ABANDONEDMUTEX);
        }

        public AbandonedMutexException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_ABANDONEDMUTEX);
        }

        public AbandonedMutexException(int location, WaitHandle handle): base (Environment.GetResourceString("Threading.AbandonedMutexException"))
        {
            SetErrorCode(__HResults.COR_E_ABANDONEDMUTEX);
            SetupException(location, handle);
        }

        public AbandonedMutexException(String message, int location, WaitHandle handle): base (message)
        {
            SetErrorCode(__HResults.COR_E_ABANDONEDMUTEX);
            SetupException(location, handle);
        }

        public AbandonedMutexException(String message, Exception inner, int location, WaitHandle handle): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_ABANDONEDMUTEX);
            SetupException(location, handle);
        }

        private void SetupException(int location, WaitHandle handle)
        {
            m_MutexIndex = location;
            if (handle != null)
                m_Mutex = handle as Mutex;
        }

        protected AbandonedMutexException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public Mutex Mutex
        {
            get
            {
                return m_Mutex;
            }
        }

        public int MutexIndex
        {
            get
            {
                return m_MutexIndex;
            }
        }
    }
}