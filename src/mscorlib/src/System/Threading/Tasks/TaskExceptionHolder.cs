using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    internal class TaskExceptionHolder
    {
        private readonly static bool s_failFastOnUnobservedException = ShouldFailFastOnUnobservedException();
        private static volatile bool s_domainUnloadStarted;
        private static volatile EventHandler s_adUnloadEventHandler;
        private readonly Task m_task;
        private volatile List<ExceptionDispatchInfo> m_faultExceptions;
        private ExceptionDispatchInfo m_cancellationException;
        private volatile bool m_isHandled;
        internal TaskExceptionHolder(Task task)
        {
            Contract.Requires(task != null, "Expected a non-null task.");
            m_task = task;
            EnsureADUnloadCallbackRegistered();
        }

        private static bool ShouldFailFastOnUnobservedException()
        {
            bool shouldFailFast = false;
            return shouldFailFast;
        }

        private static void EnsureADUnloadCallbackRegistered()
        {
            if (s_adUnloadEventHandler == null && Interlocked.CompareExchange(ref s_adUnloadEventHandler, AppDomainUnloadCallback, null) == null)
            {
                AppDomain.CurrentDomain.DomainUnload += s_adUnloadEventHandler;
            }
        }

        private static void AppDomainUnloadCallback(object sender, EventArgs e)
        {
            s_domainUnloadStarted = true;
        }

        ~TaskExceptionHolder()
        {
            if (m_faultExceptions != null && !m_isHandled && !Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload() && !s_domainUnloadStarted)
            {
                foreach (ExceptionDispatchInfo edi in m_faultExceptions)
                {
                    var exp = edi.SourceException;
                    AggregateException aggExp = exp as AggregateException;
                    if (aggExp != null)
                    {
                        AggregateException flattenedAggExp = aggExp.Flatten();
                        foreach (Exception innerExp in flattenedAggExp.InnerExceptions)
                        {
                            if (innerExp is ThreadAbortException)
                                return;
                        }
                    }
                    else if (exp is ThreadAbortException)
                    {
                        return;
                    }
                }

                AggregateException exceptionToThrow = new AggregateException(Environment.GetResourceString("TaskExceptionHolder_UnhandledException"), m_faultExceptions);
                UnobservedTaskExceptionEventArgs ueea = new UnobservedTaskExceptionEventArgs(exceptionToThrow);
                TaskScheduler.PublishUnobservedTaskException(m_task, ueea);
                if (s_failFastOnUnobservedException && !ueea.m_observed)
                {
                    throw exceptionToThrow;
                }
            }
        }

        internal bool ContainsFaultList
        {
            get
            {
                return m_faultExceptions != null;
            }
        }

        internal void Add(object exceptionObject)
        {
            Add(exceptionObject, representsCancellation: false);
        }

        internal void Add(object exceptionObject, bool representsCancellation)
        {
            Contract.Requires(exceptionObject != null, "TaskExceptionHolder.Add(): Expected a non-null exceptionObject");
            Contract.Requires(exceptionObject is Exception || exceptionObject is IEnumerable<Exception> || exceptionObject is ExceptionDispatchInfo || exceptionObject is IEnumerable<ExceptionDispatchInfo>, "TaskExceptionHolder.Add(): Expected Exception, IEnumerable<Exception>, ExceptionDispatchInfo, or IEnumerable<ExceptionDispatchInfo>");
            if (representsCancellation)
                SetCancellationException(exceptionObject);
            else
                AddFaultException(exceptionObject);
        }

        private void SetCancellationException(object exceptionObject)
        {
            Contract.Requires(exceptionObject != null, "Expected exceptionObject to be non-null.");
            Contract.Assert(m_cancellationException == null, "Expected SetCancellationException to be called only once.");
            Contract.Assert(m_faultExceptions == null, "Expected SetCancellationException to be called before any faults were added.");
            var oce = exceptionObject as OperationCanceledException;
            if (oce != null)
            {
                m_cancellationException = ExceptionDispatchInfo.Capture(oce);
            }
            else
            {
                var edi = exceptionObject as ExceptionDispatchInfo;
                Contract.Assert(edi != null && edi.SourceException is OperationCanceledException, "Expected an OCE or an EDI that contained an OCE");
                m_cancellationException = edi;
            }

            MarkAsHandled(false);
        }

        private void AddFaultException(object exceptionObject)
        {
            Contract.Requires(exceptionObject != null, "AddFaultException(): Expected a non-null exceptionObject");
            var exceptions = m_faultExceptions;
            if (exceptions == null)
                m_faultExceptions = exceptions = new List<ExceptionDispatchInfo>(1);
            else
                Contract.Assert(exceptions.Count > 0, "Expected existing exceptions list to have > 0 exceptions.");
            var exception = exceptionObject as Exception;
            if (exception != null)
            {
                exceptions.Add(ExceptionDispatchInfo.Capture(exception));
            }
            else
            {
                var edi = exceptionObject as ExceptionDispatchInfo;
                if (edi != null)
                {
                    exceptions.Add(edi);
                }
                else
                {
                    var exColl = exceptionObject as IEnumerable<Exception>;
                    if (exColl != null)
                    {
                        int numExceptions = 0;
                        foreach (var exc in exColl)
                        {
                            Contract.Assert(exc != null, "No exceptions should be null");
                            numExceptions++;
                            exceptions.Add(ExceptionDispatchInfo.Capture(exc));
                        }

                        Contract.Assert(numExceptions > 0, "Collection should contain at least one exception.");
                    }
                    else
                    {
                        var ediColl = exceptionObject as IEnumerable<ExceptionDispatchInfo>;
                        if (ediColl != null)
                        {
                            exceptions.AddRange(ediColl);
                            Contract.Assert(exceptions.Count > 0, "There should be at least one dispatch info.");
                            foreach (var tmp in exceptions)
                            {
                                Contract.Assert(tmp != null, "No dispatch infos should be null");
                            }
                        }
                        else
                        {
                            throw new ArgumentException(Environment.GetResourceString("TaskExceptionHolder_UnknownExceptionType"), "exceptionObject");
                        }
                    }
                }
            }

            for (int i = 0; i < exceptions.Count; i++)
            {
                var t = exceptions[i].SourceException.GetType();
                if (t != typeof (ThreadAbortException) && t != typeof (AppDomainUnloadedException))
                {
                    MarkAsUnhandled();
                    break;
                }
                else if (i == exceptions.Count - 1)
                {
                    MarkAsHandled(false);
                }
            }
        }

        private void MarkAsUnhandled()
        {
            if (m_isHandled)
            {
                GC.ReRegisterForFinalize(this);
                m_isHandled = false;
            }
        }

        internal void MarkAsHandled(bool calledFromFinalizer)
        {
            if (!m_isHandled)
            {
                if (!calledFromFinalizer)
                {
                    GC.SuppressFinalize(this);
                }

                m_isHandled = true;
            }
        }

        internal AggregateException CreateExceptionObject(bool calledFromFinalizer, Exception includeThisException)
        {
            var exceptions = m_faultExceptions;
            Contract.Assert(exceptions != null, "Expected an initialized list.");
            Contract.Assert(exceptions.Count > 0, "Expected at least one exception.");
            MarkAsHandled(calledFromFinalizer);
            if (includeThisException == null)
                return new AggregateException(exceptions);
            Exception[] combinedExceptions = new Exception[exceptions.Count + 1];
            for (int i = 0; i < combinedExceptions.Length - 1; i++)
            {
                combinedExceptions[i] = exceptions[i].SourceException;
            }

            combinedExceptions[combinedExceptions.Length - 1] = includeThisException;
            return new AggregateException(combinedExceptions);
        }

        internal ReadOnlyCollection<ExceptionDispatchInfo> GetExceptionDispatchInfos()
        {
            var exceptions = m_faultExceptions;
            Contract.Assert(exceptions != null, "Expected an initialized list.");
            Contract.Assert(exceptions.Count > 0, "Expected at least one exception.");
            MarkAsHandled(false);
            return new ReadOnlyCollection<ExceptionDispatchInfo>(exceptions);
        }

        internal ExceptionDispatchInfo GetCancellationExceptionDispatchInfo()
        {
            var edi = m_cancellationException;
            Contract.Assert(edi == null || edi.SourceException is OperationCanceledException, "Expected the EDI to be for an OperationCanceledException");
            return edi;
        }
    }
}