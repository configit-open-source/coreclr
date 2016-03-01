using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Runtime.Versioning;
using SecurityException = System.Security.SecurityException;

namespace System.IO
{
    public class FileLoadException : IOException
    {
        private String _fileName;
        private String _fusionLog;
        public FileLoadException(): base (Environment.GetResourceString("IO.FileLoad"))
        {
            SetErrorCode(__HResults.COR_E_FILELOAD);
        }

        public FileLoadException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_FILELOAD);
        }

        public FileLoadException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_FILELOAD);
        }

        public FileLoadException(String message, String fileName): base (message)
        {
            SetErrorCode(__HResults.COR_E_FILELOAD);
            _fileName = fileName;
        }

        public FileLoadException(String message, String fileName, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_FILELOAD);
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
                _message = FormatFileLoadExceptionMessage(_fileName, HResult);
        }

        public String FileName
        {
            get
            {
                return _fileName;
            }
        }

        public override System.Collections.IDictionary Data
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                var _data = base.Data;
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && !_data.Contains("HResult"))
                {
                    _data.Add("HResult", HResult);
                }

                return _data;
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

        protected FileLoadException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            _fileName = info.GetString("FileLoad_FileName");
            try
            {
                _fusionLog = info.GetString("FileLoad_FusionLog");
            }
            catch
            {
                _fusionLog = null;
            }
        }

        private FileLoadException(String fileName, String fusionLog, int hResult): base (null)
        {
            SetErrorCode(hResult);
            _fileName = fileName;
            _fusionLog = fusionLog;
            SetMessageField();
        }

        internal static String FormatFileLoadExceptionMessage(String fileName, int hResult)
        {
            string format = null;
            GetFileLoadExceptionMessage(hResult, JitHelpers.GetStringHandleOnStack(ref format));
            string message = null;
            GetMessageForHR(hResult, JitHelpers.GetStringHandleOnStack(ref message));
            return String.Format(CultureInfo.CurrentCulture, format, fileName, message);
        }

        private static extern void GetFileLoadExceptionMessage(int hResult, StringHandleOnStack retString);
        private static extern void GetMessageForHR(int hresult, StringHandleOnStack retString);
    }
}