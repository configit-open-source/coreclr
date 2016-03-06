using System.Runtime.Serialization;

namespace System
{
    public sealed class DataMisalignedException : SystemException
    {
        public DataMisalignedException(): base (Environment.GetResourceString("Arg_DataMisalignedException"))
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }

        public DataMisalignedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }

        public DataMisalignedException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }
    }
}