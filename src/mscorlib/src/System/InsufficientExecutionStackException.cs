using System.Runtime.Serialization;

namespace System
{
    public sealed class InsufficientExecutionStackException : SystemException
    {
        public InsufficientExecutionStackException(): base (Environment.GetResourceString("Arg_InsufficientExecutionStackException"))
        {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTEXECUTIONSTACK);
        }

        public InsufficientExecutionStackException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTEXECUTIONSTACK);
        }

        public InsufficientExecutionStackException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_INSUFFICIENTEXECUTIONSTACK);
        }
    }
}