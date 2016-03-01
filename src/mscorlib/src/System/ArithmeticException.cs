namespace System
{
    using System;
    using System.Runtime.Serialization;

    public class ArithmeticException : SystemException
    {
        public ArithmeticException(): base (Environment.GetResourceString("Arg_ArithmeticException"))
        {
            SetErrorCode(__HResults.COR_E_ARITHMETIC);
        }

        public ArithmeticException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ARITHMETIC);
        }

        public ArithmeticException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ARITHMETIC);
        }

        protected ArithmeticException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}