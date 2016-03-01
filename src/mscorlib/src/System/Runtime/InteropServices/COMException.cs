namespace System.Runtime.InteropServices
{
    using System;
    using System.Runtime.Serialization;
    using System.Globalization;
    using System.Security;
    using Microsoft.Win32;

    public class COMException : ExternalException
    {
        public COMException(): base (Environment.GetResourceString("Arg_COMException"))
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public COMException(String message): base (message)
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public COMException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.E_FAIL);
        }

        public COMException(String message, int errorCode): base (message)
        {
            SetErrorCode(errorCode);
        }

        internal COMException(int hresult): base (Win32Native.GetMessage(hresult))
        {
            SetErrorCode(hresult);
        }

        internal COMException(String message, int hresult, Exception inner): base (message, inner)
        {
            SetErrorCode(hresult);
        }

        protected COMException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public override String ToString()
        {
            String message = Message;
            String s;
            String _className = GetType().ToString();
            s = _className + " (0x" + HResult.ToString("X8", CultureInfo.InvariantCulture) + ")";
            if (!(message == null || message.Length <= 0))
            {
                s = s + ": " + message;
            }

            Exception _innerException = InnerException;
            if (_innerException != null)
            {
                s = s + " ---> " + _innerException.ToString();
            }

            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;
            return s;
        }
    }
}