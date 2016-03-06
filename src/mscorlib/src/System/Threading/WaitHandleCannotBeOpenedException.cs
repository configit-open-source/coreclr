using System.Runtime.Serialization;

namespace System.Threading
{
    public class WaitHandleCannotBeOpenedException : Exception
    {
        public WaitHandleCannotBeOpenedException(): base (Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException"))
        {
            SetErrorCode(__HResults.COR_E_WAITHANDLECANNOTBEOPENED);
        }

        public WaitHandleCannotBeOpenedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_WAITHANDLECANNOTBEOPENED);
        }

        public WaitHandleCannotBeOpenedException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_WAITHANDLECANNOTBEOPENED);
        }
    }
}