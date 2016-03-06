using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    public class SEHException : ExternalException
    {
        public SEHException(): base ()
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public SEHException(String message): base (message)
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public SEHException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public virtual bool CanResume()
        {
            return false;
        }
    }
}