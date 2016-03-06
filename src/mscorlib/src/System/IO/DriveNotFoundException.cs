using System.Runtime.Serialization;

namespace System.IO
{
    public class DriveNotFoundException : IOException
    {
        public DriveNotFoundException(): base (Environment.GetResourceString("Arg_DriveNotFoundException"))
        {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }

        public DriveNotFoundException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }

        public DriveNotFoundException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_DIRECTORYNOTFOUND);
        }

    }
}