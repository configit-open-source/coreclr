namespace System.Threading
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.InteropServices;

    public sealed class ThreadStartException : SystemException
    {
        private ThreadStartException(): base (Environment.GetResourceString("Arg_ThreadStartException"))
        {
            SetErrorCode(__HResults.COR_E_THREADSTART);
        }

        private ThreadStartException(Exception reason): base (Environment.GetResourceString("Arg_ThreadStartException"), reason)
        {
            SetErrorCode(__HResults.COR_E_THREADSTART);
        }

        internal ThreadStartException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}