namespace System.Reflection
{
    using System;
    using SystemException = System.SystemException;
    using System.Runtime.Serialization;

    public sealed class TargetParameterCountException : Exception
    {
        public TargetParameterCountException(): base (Environment.GetResourceString("Arg_TargetParameterCountException"))
        {
            SetErrorCode(__HResults.COR_E_TARGETPARAMCOUNT);
        }

        public TargetParameterCountException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TARGETPARAMCOUNT);
        }

        public TargetParameterCountException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_TARGETPARAMCOUNT);
        }

        internal TargetParameterCountException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}