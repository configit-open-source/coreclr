using System.Runtime.Serialization;

namespace System.Security.Policy
{
    public class PolicyException : SystemException
    {
        public PolicyException(): base (Environment.GetResourceString("Policy_Default"))
        {
            HResult = __HResults.CORSEC_E_POLICY_EXCEPTION;
        }

        public PolicyException(String message): base (message)
        {
            HResult = __HResults.CORSEC_E_POLICY_EXCEPTION;
        }

        public PolicyException(String message, Exception exception): base (message, exception)
        {
            HResult = __HResults.CORSEC_E_POLICY_EXCEPTION;
        }

        internal PolicyException(String message, int hresult): base (message)
        {
            HResult = hresult;
        }

        internal PolicyException(String message, int hresult, Exception exception): base (message, exception)
        {
            HResult = hresult;
        }
    }
}