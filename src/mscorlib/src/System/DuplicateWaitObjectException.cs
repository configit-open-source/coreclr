using System.Runtime.Serialization;

namespace System
{
    public class DuplicateWaitObjectException : ArgumentException
    {
        private static volatile String _duplicateWaitObjectMessage = null;
        private static String DuplicateWaitObjectMessage
        {
            get
            {
                if (_duplicateWaitObjectMessage == null)
                    _duplicateWaitObjectMessage = Environment.GetResourceString("Arg_DuplicateWaitObjectException");
                return _duplicateWaitObjectMessage;
            }
        }

        public DuplicateWaitObjectException(): base (DuplicateWaitObjectMessage)
        {
            SetErrorCode(__HResults.COR_E_DUPLICATEWAITOBJECT);
        }

        public DuplicateWaitObjectException(String parameterName): base (DuplicateWaitObjectMessage, parameterName)
        {
            SetErrorCode(__HResults.COR_E_DUPLICATEWAITOBJECT);
        }

        public DuplicateWaitObjectException(String parameterName, String message): base (message, parameterName)
        {
            SetErrorCode(__HResults.COR_E_DUPLICATEWAITOBJECT);
        }

        public DuplicateWaitObjectException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_DUPLICATEWAITOBJECT);
        }
    }
}