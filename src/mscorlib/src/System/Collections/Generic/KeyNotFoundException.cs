using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    public class KeyNotFoundException : SystemException, ISerializable
    {
        public KeyNotFoundException(): base (Environment.GetResourceString("Arg_KeyNotFound"))
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }

        public KeyNotFoundException(String message): base (message)
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }

        public KeyNotFoundException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(System.__HResults.COR_E_KEYNOTFOUND);
        }
    }
}