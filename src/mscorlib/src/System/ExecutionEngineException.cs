namespace System
{
    using System;
    using System.Runtime.Serialization;

    public sealed class ExecutionEngineException : SystemException
    {
        public ExecutionEngineException(): base (Environment.GetResourceString("Arg_ExecutionEngineException"))
        {
            SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        }

        public ExecutionEngineException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        }

        public ExecutionEngineException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_EXECUTIONENGINE);
        }

        internal ExecutionEngineException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}