using System.IO;
using System.Runtime.Serialization;

namespace System
{
    public class BadImageFormatException : SystemException
    {
        private String _fileName;
        private String _fusionLog;
        public BadImageFormatException(): base (Environment.GetResourceString("Arg_BadImageFormatException"))
        {
            SetErrorCode(__HResults.COR_E_BADIMAGEFORMAT);
        }

        public BadImageFormatException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_BADIMAGEFORMAT);
        }

        public BadImageFormatException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_BADIMAGEFORMAT);
        }

        public BadImageFormatException(String message, String fileName): base (message)
        {
            SetErrorCode(__HResults.COR_E_BADIMAGEFORMAT);
            _fileName = fileName;
        }

        public BadImageFormatException(String message, String fileName, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_BADIMAGEFORMAT);
            _fileName = fileName;
        }

        public override String Message
        {
            get
            {
                SetMessageField();
                return _message;
            }
        }

        private void SetMessageField()
        {
            if (_message == null)
            {
                if ((_fileName == null) && (HResult == System.__HResults.COR_E_EXCEPTION))
                    _message = Environment.GetResourceString("Arg_BadImageFormatException");
                else
                    _message = FileLoadException.FormatFileLoadExceptionMessage(_fileName, HResult);
            }
        }

        public String FileName
        {
            get
            {
                return _fileName;
            }
        }

        public override String ToString()
        {
            String s = GetType().FullName + ": " + Message;
            if (_fileName != null && _fileName.Length != 0)
                s += Environment.NewLine + Environment.GetResourceString("IO.FileName_Name", _fileName);
            if (InnerException != null)
                s = s + " ---> " + InnerException.ToString();
            if (StackTrace != null)
                s += Environment.NewLine + StackTrace;
            return s;
        }

        private BadImageFormatException(String fileName, String fusionLog, int hResult): base (null)
        {
            SetErrorCode(hResult);
            _fileName = fileName;
            _fusionLog = fusionLog;
            SetMessageField();
        }
    }
}