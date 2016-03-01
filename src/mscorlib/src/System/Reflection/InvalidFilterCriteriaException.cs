namespace System.Reflection
{
    using System;
    using System.Runtime.Serialization;
    using ApplicationException = System.ApplicationException;

    public class InvalidFilterCriteriaException : Exception
    {
        public InvalidFilterCriteriaException(): base (Environment.GetResourceString("Arg_InvalidFilterCriteriaException"))
        {
            SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA);
        }

        public InvalidFilterCriteriaException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA);
        }

        public InvalidFilterCriteriaException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_INVALIDFILTERCRITERIA);
        }

        protected InvalidFilterCriteriaException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}