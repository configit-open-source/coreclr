using System.Runtime.Serialization;

namespace System.Threading
{
    public class SynchronizationLockException : SystemException
    {
        public SynchronizationLockException(): base (Environment.GetResourceString("Arg_SynchronizationLockException"))
        {
            SetErrorCode(__HResults.COR_E_SYNCHRONIZATIONLOCK);
        }

        public SynchronizationLockException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_SYNCHRONIZATIONLOCK);
        }

        public SynchronizationLockException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_SYNCHRONIZATIONLOCK);
        }
    }
}