using System.Runtime.Serialization;

namespace System
{
    public class RankException : SystemException
    {
        public RankException(): base (Environment.GetResourceString("Arg_RankException"))
        {
            SetErrorCode(__HResults.COR_E_RANK);
        }

        public RankException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_RANK);
        }

        public RankException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_RANK);
        }

        protected RankException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}