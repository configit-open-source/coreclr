using System.Runtime.Serialization;

namespace System
{
    public class TypeUnloadedException : SystemException
    {
        public TypeUnloadedException(): base (Environment.GetResourceString("Arg_TypeUnloadedException"))
        {
            SetErrorCode(__HResults.COR_E_TYPEUNLOADED);
        }

        public TypeUnloadedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TYPEUNLOADED);
        }

        public TypeUnloadedException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_TYPEUNLOADED);
        }
    }
}