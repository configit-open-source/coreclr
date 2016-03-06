using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    public class MarshalDirectiveException : SystemException
    {
        public MarshalDirectiveException(): base (Environment.GetResourceString("Arg_MarshalDirectiveException"))
        {
            SetErrorCode(__HResults.COR_E_MARSHALDIRECTIVE);
        }

        public MarshalDirectiveException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_MARSHALDIRECTIVE);
        }

        public MarshalDirectiveException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_MARSHALDIRECTIVE);
        }
    }
}