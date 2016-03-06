using System.Runtime.Serialization;

namespace System
{
    public sealed class StackOverflowException : SystemException
    {
        public StackOverflowException(): base (Environment.GetResourceString("Arg_StackOverflowException"))
        {
            SetErrorCode(__HResults.COR_E_STACKOVERFLOW);
        }

        public StackOverflowException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_STACKOVERFLOW);
        }

        public StackOverflowException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_STACKOVERFLOW);
        }
    }
}