namespace System
{
    using System;
    using System.Runtime.Serialization;

    public class ArrayTypeMismatchException : SystemException
    {
        public ArrayTypeMismatchException(): base (Environment.GetResourceString("Arg_ArrayTypeMismatchException"))
        {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }

        public ArrayTypeMismatchException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }

        public ArrayTypeMismatchException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }

        protected ArrayTypeMismatchException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}