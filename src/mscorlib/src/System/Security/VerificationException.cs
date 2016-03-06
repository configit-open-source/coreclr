using System.Runtime.Serialization;

namespace System.Security
{
    public class VerificationException : SystemException
    {
        public VerificationException(): base (Environment.GetResourceString("Verification_Exception"))
        {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }

        public VerificationException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }

        public VerificationException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_VERIFICATION);
        }
    }
}