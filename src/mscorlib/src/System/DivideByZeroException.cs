namespace System
{
    using System;
    using System.Runtime.Serialization;

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

        protected DivideByZeroException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}