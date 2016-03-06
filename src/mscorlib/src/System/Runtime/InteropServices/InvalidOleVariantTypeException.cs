using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    public class InvalidOleVariantTypeException : SystemException
    {
        public InvalidOleVariantTypeException(): base (Environment.GetResourceString("Arg_InvalidOleVariantTypeException"))
        {
            SetErrorCode(__HResults.COR_E_INVALIDOLEVARIANTTYPE);
        }

        public InvalidOleVariantTypeException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INVALIDOLEVARIANTTYPE);
        }

        public InvalidOleVariantTypeException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_INVALIDOLEVARIANTTYPE);
        }
    }
}