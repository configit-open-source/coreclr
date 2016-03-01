using System.Runtime.Serialization;

namespace System.Reflection
{
    public sealed class TargetInvocationException : Exception
    {
        private TargetInvocationException(): base (Environment.GetResourceString("Arg_TargetInvocationException"))
        {
            SetErrorCode(__HResults.COR_E_TARGETINVOCATION);
        }

        private TargetInvocationException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TARGETINVOCATION);
        }

        public TargetInvocationException(System.Exception inner): base (Environment.GetResourceString("Arg_TargetInvocationException"), inner)
        {
            SetErrorCode(__HResults.COR_E_TARGETINVOCATION);
        }

        public TargetInvocationException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_TARGETINVOCATION);
        }

        internal TargetInvocationException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}