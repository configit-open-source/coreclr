using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    public class AggregateException : Exception
    {
        private ReadOnlyCollection<Exception> m_innerExceptions;
        public AggregateException(): base (Environment.GetResourceString("AggregateException_ctor_DefaultMessage"))
        {
            m_innerExceptions = new ReadOnlyCollection<Exception>(new Exception[0]);
        }

        public AggregateException(string message): base (message)
        {
            m_innerExceptions = new ReadOnlyCollection<Exception>(new Exception[0]);
        }

        public AggregateException(string message, Exception innerException): base (message, innerException)
        {
            if (innerException == null)
            {
                throw new ArgumentNullException("innerException");
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(new Exception[]{innerException});
        }

        public AggregateException(IEnumerable<Exception> innerExceptions): this (Environment.GetResourceString("AggregateException_ctor_DefaultMessage"), innerExceptions)
        {
        }

        public AggregateException(params Exception[] innerExceptions): this (Environment.GetResourceString("AggregateException_ctor_DefaultMessage"), innerExceptions)
        {
        }

        public AggregateException(string message, IEnumerable<Exception> innerExceptions): this (message, innerExceptions as IList<Exception> ?? (innerExceptions == null ? (List<Exception>)null : new List<Exception>(innerExceptions)))
        {
        }

        public AggregateException(string message, params Exception[] innerExceptions): this (message, (IList<Exception>)innerExceptions)
        {
        }

        private AggregateException(string message, IList<Exception> innerExceptions): base (message, innerExceptions != null && innerExceptions.Count > 0 ? innerExceptions[0] : null)
        {
            if (innerExceptions == null)
            {
                throw new ArgumentNullException("innerExceptions");
            }

            Exception[] exceptionsCopy = new Exception[innerExceptions.Count];
            for (int i = 0; i < exceptionsCopy.Length; i++)
            {
                exceptionsCopy[i] = innerExceptions[i];
                if (exceptionsCopy[i] == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("AggregateException_ctor_InnerExceptionNull"));
                }
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(exceptionsCopy);
        }

        internal AggregateException(IEnumerable<ExceptionDispatchInfo> innerExceptionInfos): this (Environment.GetResourceString("AggregateException_ctor_DefaultMessage"), innerExceptionInfos)
        {
        }

        internal AggregateException(string message, IEnumerable<ExceptionDispatchInfo> innerExceptionInfos): this (message, innerExceptionInfos as IList<ExceptionDispatchInfo> ?? (innerExceptionInfos == null ? (List<ExceptionDispatchInfo>)null : new List<ExceptionDispatchInfo>(innerExceptionInfos)))
        {
        }

        private AggregateException(string message, IList<ExceptionDispatchInfo> innerExceptionInfos): base (message, innerExceptionInfos != null && innerExceptionInfos.Count > 0 && innerExceptionInfos[0] != null ? innerExceptionInfos[0].SourceException : null)
        {
            if (innerExceptionInfos == null)
            {
                throw new ArgumentNullException("innerExceptionInfos");
            }

            Exception[] exceptionsCopy = new Exception[innerExceptionInfos.Count];
            for (int i = 0; i < exceptionsCopy.Length; i++)
            {
                var edi = innerExceptionInfos[i];
                if (edi != null)
                    exceptionsCopy[i] = edi.SourceException;
                if (exceptionsCopy[i] == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("AggregateException_ctor_InnerExceptionNull"));
                }
            }

            m_innerExceptions = new ReadOnlyCollection<Exception>(exceptionsCopy);
        }

        public override Exception GetBaseException()
        {
            Exception back = this;
            AggregateException backAsAggregate = this;
            while (backAsAggregate != null && backAsAggregate.InnerExceptions.Count == 1)
            {
                back = back.InnerException;
                backAsAggregate = back as AggregateException;
            }

            return back;
        }

        public ReadOnlyCollection<Exception> InnerExceptions
        {
            get
            {
                return m_innerExceptions;
            }
        }

        public void Handle(Func<Exception, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }

            List<Exception> unhandledExceptions = null;
            for (int i = 0; i < m_innerExceptions.Count; i++)
            {
                if (!predicate(m_innerExceptions[i]))
                {
                    if (unhandledExceptions == null)
                    {
                        unhandledExceptions = new List<Exception>();
                    }

                    unhandledExceptions.Add(m_innerExceptions[i]);
                }
            }

            if (unhandledExceptions != null)
            {
                throw new AggregateException(Message, unhandledExceptions);
            }
        }

        public AggregateException Flatten()
        {
            List<Exception> flattenedExceptions = new List<Exception>();
            List<AggregateException> exceptionsToFlatten = new List<AggregateException>();
            exceptionsToFlatten.Add(this);
            int nDequeueIndex = 0;
            while (exceptionsToFlatten.Count > nDequeueIndex)
            {
                IList<Exception> currentInnerExceptions = exceptionsToFlatten[nDequeueIndex++].InnerExceptions;
                for (int i = 0; i < currentInnerExceptions.Count; i++)
                {
                    Exception currentInnerException = currentInnerExceptions[i];
                    if (currentInnerException == null)
                    {
                        continue;
                    }

                    AggregateException currentInnerAsAggregate = currentInnerException as AggregateException;
                    if (currentInnerAsAggregate != null)
                    {
                        exceptionsToFlatten.Add(currentInnerAsAggregate);
                    }
                    else
                    {
                        flattenedExceptions.Add(currentInnerException);
                    }
                }
            }

            return new AggregateException(Message, flattenedExceptions);
        }

        public override string Message
        {
            get
            {
                if (m_innerExceptions.Count == 0)
                {
                    return base.Message;
                }

                StringBuilder sb = StringBuilderCache.Acquire();
                sb.Append(base.Message);
                sb.Append(' ');
                for (int i = 0; i < m_innerExceptions.Count; i++)
                {
                    sb.Append('(');
                    sb.Append(m_innerExceptions[i].Message);
                    sb.Append(") ");
                }

                sb.Length -= 1;
                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        public override string ToString()
        {
            string text = base.ToString();
            for (int i = 0; i < m_innerExceptions.Count; i++)
            {
                text = String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("AggregateException_ToString"), text, Environment.NewLine, i, m_innerExceptions[i].ToString(), "<---", Environment.NewLine);
            }

            return text;
        }

        private int InnerExceptionCount
        {
            get
            {
                return InnerExceptions.Count;
            }
        }
    }
}