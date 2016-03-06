using System.Runtime.Serialization;

namespace System.IO
{
    public class DirectoryNotFoundException : IOException
    {
        public DirectoryNotFoundException(): base (Environment.GetResourceString("Arg_DirectoryNotFoundException"))
        {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }

        public DirectoryNotFoundException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }

        public DirectoryNotFoundException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }
    }
}