using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System
{
    public class Exception : _Exception
    {

        public Exception()
        {

        }

        public Exception(String message)
        {
            _message = message;
        }

        public Exception(String message, Exception innerException)
        {
            _message = message;
            _innerException = innerException;
        }

        public virtual String Message
        {
            get
            {
                if (_message == null)
                {
                    if (_className == null)
                    {
                        _className = GetClassName();
                    }

                    return Environment.GetResourceString("Exception_WasThrown", _className);
                }
                else
                {
                    return _message;
                }
            }
        }

        private static extern bool IsImmutableAgileException(Exception e);
        internal class __RestrictedErrorObject
        {
            private object _realErrorObject;
            internal __RestrictedErrorObject(object errorObject)
            {
                _realErrorObject = errorObject;
            }

            public object RealErrorObject
            {
                get
                {
                    return _realErrorObject;
                }
            }
        }


        private string GetClassName()
        {
            if (_className == null)
                _className = GetType().ToString();
            return _className;
        }

        public virtual Exception GetBaseException()
        {
            Exception inner = InnerException;
            Exception back = this;
            while (inner != null)
            {
                back = inner;
                inner = inner.InnerException;
            }

            return back;
        }

        public Exception InnerException
        {
            get
            {
                return _innerException;
            }
        }

        public MethodBase TargetSite {
      get {
        return null;
      }
    }

        public virtual String StackTrace
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return GetStackTrace(true);
            }
        }

        private string GetStackTrace(bool needFileInfo)
        {
            string stackTraceString = _stackTraceString;
            string remoteStackTraceString = _remoteStackTraceString;
            if (stackTraceString != null)
            {
                return remoteStackTraceString + stackTraceString;
            }

            if (_stackTrace == null)
            {
                return remoteStackTraceString;
            }

            String tempStackTraceString = Environment.GetStackTrace(this, needFileInfo);
            return remoteStackTraceString + tempStackTraceString;
        }

        internal void SetErrorCode(int hr)
        {
            HResult = hr;
        }

        public virtual String HelpLink
        {
            get
            {
                return _helpURL;
            }

            set
            {
                _helpURL = value;
            }
        }

        public virtual String Source
        {
            get
            {
               
                return _source;
            }
            set
            {
                _source = value;
            }
        }

        public override String ToString()
        {
            return ToString(true, true);
        }

        private String ToString(bool needFileLineInfo, bool needMessage)
        {
            String message = (needMessage ? Message : null);
            String s;
            if (message == null || message.Length <= 0)
            {
                s = GetClassName();
            }
            else
            {
                s = GetClassName() + ": " + message;
            }

            if (_innerException != null)
            {
                s = s + " ---> " + _innerException.ToString(needFileLineInfo, needMessage) + Environment.NewLine + "   " + Environment.GetResourceString("Exception_EndOfInnerExceptionStack");
            }

            string stackTrace = GetStackTrace(needFileLineInfo);
            if (stackTrace != null)
            {
                s += Environment.NewLine + stackTrace;
            }

            return s;
        }



        private String _className;
        private MethodBase _exceptionMethod;
        private String _exceptionMethodString;
        internal String _message;
        private IDictionary _data;
        private Exception _innerException;
        private String _helpURL;
        private Object _stackTrace;
        private Object _watsonBuckets;
        private String _stackTraceString;
        private String _remoteStackTraceString;
        private int _remoteStackIndex;
        private Object _dynamicMethods;
        internal int _HResult;
        public int HResult
        {
            get
            {
                return _HResult;
            }

            protected set
            {
                _HResult = value;
            }
        }

        private String _source;
        
    }

    internal sealed class CrossAppDomainMarshaledException : SystemException
    {
        public CrossAppDomainMarshaledException(String message, int errorCode): base (message)
        {
            SetErrorCode(errorCode);
        }
    }
}