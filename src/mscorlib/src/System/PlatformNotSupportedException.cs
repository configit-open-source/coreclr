using System.Runtime.Serialization;

namespace System
{
    public class PlatformNotSupportedException : NotSupportedException
    {
        public PlatformNotSupportedException(): base (Environment.GetResourceString("Arg_PlatformNotSupported"))
        {
            SetErrorCode(__HResults.COR_E_PLATFORMNOTSUPPORTED);
        }

        public PlatformNotSupportedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_PLATFORMNOTSUPPORTED);
        }

        public PlatformNotSupportedException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_PLATFORMNOTSUPPORTED);
        }
    }
}