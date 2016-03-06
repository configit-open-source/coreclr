using System.Runtime.Serialization;

namespace System
{
    public class CannotUnloadAppDomainException : SystemException
    {
        public CannotUnloadAppDomainException(): base (Environment.GetResourceString("Arg_CannotUnloadAppDomainException"))
        {
            SetErrorCode(__HResults.COR_E_CANNOTUNLOADAPPDOMAIN);
        }

        public CannotUnloadAppDomainException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_CANNOTUNLOADAPPDOMAIN);
        }

        public CannotUnloadAppDomainException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_CANNOTUNLOADAPPDOMAIN);
        }
    }
}