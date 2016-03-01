namespace System.Runtime.ExceptionServices
{
    public sealed class ExceptionDispatchInfo
    {
        private Exception m_Exception;
        private string m_remoteStackTrace;
        private object m_stackTrace;
        private object m_dynamicMethods;
        private UIntPtr m_IPForWatsonBuckets;
        private Object m_WatsonBuckets;
        private ExceptionDispatchInfo(Exception exception)
        {
            m_Exception = exception;
            m_remoteStackTrace = exception.RemoteStackTrace;
            object stackTrace;
            object dynamicMethods;
            m_Exception.GetStackTracesDeepCopy(out stackTrace, out dynamicMethods);
            m_stackTrace = stackTrace;
            m_dynamicMethods = dynamicMethods;
            m_IPForWatsonBuckets = exception.IPForWatsonBuckets;
            m_WatsonBuckets = exception.WatsonBuckets;
        }

        internal UIntPtr IPForWatsonBuckets
        {
            get
            {
                return m_IPForWatsonBuckets;
            }
        }

        internal object WatsonBuckets
        {
            get
            {
                return m_WatsonBuckets;
            }
        }

        internal object BinaryStackTraceArray
        {
            get
            {
                return m_stackTrace;
            }
        }

        internal object DynamicMethodArray
        {
            get
            {
                return m_dynamicMethods;
            }
        }

        internal string RemoteStackTrace
        {
            get
            {
                return m_remoteStackTrace;
            }
        }

        public static ExceptionDispatchInfo Capture(Exception source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source", Environment.GetResourceString("ArgumentNull_Obj"));
            }

            return new ExceptionDispatchInfo(source);
        }

        public Exception SourceException
        {
            get
            {
                return m_Exception;
            }
        }

        public void Throw()
        {
            m_Exception.RestoreExceptionDispatchInfo(this);
            throw m_Exception;
        }
    }
}