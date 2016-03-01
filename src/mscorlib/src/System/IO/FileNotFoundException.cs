using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using SecurityException = System.Security.SecurityException;
using System.Globalization;

namespace System.IO
{
    public class FileNotFoundException : IOException
    {
        private String _fileName;
        private String _fusionLog;
        public FileNotFoundException(): base (Environment.GetResourceString("IO.FileNotFound"))
        {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
        }

        public FileNotFoundException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
        }

        public FileNotFoundException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
        }

        public FileNotFoundException(String message, String fileName): base (message)
        {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
            _fileName = fileName;
        }

        public FileNotFoundException(String message, String fileName, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_FILENOTFOUND);
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
                    _message = Environment.GetResourceString("IO.FileNotFound");
                else if (_fileName != null)
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

        protected FileNotFoundException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            _fileName = info.GetString("FileNotFound_FileName");
            try
            {
                _fusionLog = info.GetString("FileNotFound_FusionLog");
            }
            catch
            {
                _fusionLog = null;
            }
        }

        private FileNotFoundException(String fileName, String fusionLog, int hResult): base (null)
        {
            SetErrorCode(hResult);
            _fileName = fileName;
            _fusionLog = fusionLog;
            SetMessageField();
        }
    }
}