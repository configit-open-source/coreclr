using System.Runtime.Serialization;

namespace System.Threading
{
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
    }
}