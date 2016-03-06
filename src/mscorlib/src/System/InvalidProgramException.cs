using System.Runtime.Serialization;

namespace System
{
    public sealed class InvalidProgramException : SystemException
    {
        public InvalidProgramException(): base (Environment.GetResourceString("InvalidProgram_Default"))
        {
            SetErrorCode(__HResults.COR_E_INVALIDPROGRAM);
        }

        public InvalidProgramException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_INVALIDPROGRAM);
        }

        public InvalidProgramException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_INVALIDPROGRAM);
        }
    }
}