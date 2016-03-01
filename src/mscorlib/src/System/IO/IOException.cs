using System;
using System.Runtime.Serialization;

namespace System.IO
{
    public class IOException : SystemException
    {
        private String _maybeFullPath;
        public IOException(): base (Environment.GetResourceString("Arg_IOException"))
        {
            SetErrorCode(__HResults.COR_E_IO);
        }

        public IOException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_IO);
        }

        public IOException(String message, int hresult): base (message)
        {
            SetErrorCode(hresult);
        }

        internal IOException(String message, int hresult, String maybeFullPath): base (message)
        {
            SetErrorCode(hresult);
            _maybeFullPath = maybeFullPath;
        }

        public IOException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_IO);
        }

        protected IOException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}