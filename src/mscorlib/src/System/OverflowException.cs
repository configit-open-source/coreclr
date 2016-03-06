using System.Runtime.Serialization;

namespace System
{
    public class OverflowException : ArithmeticException
    {
        public OverflowException(): base (Environment.GetResourceString("Arg_OverflowException"))
        {
            SetErrorCode(__HResults.COR_E_OVERFLOW);
        }

        public OverflowException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_OVERFLOW);
        }

        public OverflowException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_OVERFLOW);
        }
    }
}