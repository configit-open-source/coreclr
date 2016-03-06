using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    public class SafeArrayTypeMismatchException : SystemException
    {
        public SafeArrayTypeMismatchException(): base (Environment.GetResourceString("Arg_SafeArrayTypeMismatchException"))
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYTYPEMISMATCH);
        }

        public SafeArrayTypeMismatchException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYTYPEMISMATCH);
        }

        public SafeArrayTypeMismatchException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_SAFEARRAYTYPEMISMATCH);
        }
    }
}