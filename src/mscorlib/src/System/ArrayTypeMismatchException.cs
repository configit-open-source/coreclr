using System.Runtime.Serialization;

namespace System
{
    public class ArrayTypeMismatchException : SystemException
    {
        public ArrayTypeMismatchException(): base (Environment.GetResourceString("Arg_ArrayTypeMismatchException"))
        {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }

        public ArrayTypeMismatchException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }

        public ArrayTypeMismatchException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ARRAYTYPEMISMATCH);
        }
    }
}