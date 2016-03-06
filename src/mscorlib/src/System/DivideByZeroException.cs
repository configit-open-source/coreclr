using System.Runtime.Serialization;

namespace System
{
    public class DivideByZeroException : ArithmeticException
    {
        public DivideByZeroException(): base (Environment.GetResourceString("Arg_DivideByZero"))
        {
            SetErrorCode(__HResults.COR_E_DIVIDEBYZERO);
        }

        public DivideByZeroException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_DIVIDEBYZERO);
        }

        public DivideByZeroException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_DIVIDEBYZERO);
        }
    }
}