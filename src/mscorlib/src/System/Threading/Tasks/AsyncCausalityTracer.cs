using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;

using WFD = Windows.Foundation.Diagnostics;

namespace System.Threading.Tasks
{
    internal enum CausalityTraceLevel
    {
        Required = WFD.CausalityTraceLevel.Required,
        Important = WFD.CausalityTraceLevel.Important,
        Verbose = WFD.CausalityTraceLevel.Verbose
    }

    internal enum AsyncCausalityStatus
    {
        Canceled = WFD.AsyncCausalityStatus.Canceled,
        Completed = WFD.AsyncCausalityStatus.Completed,
        Error = WFD.AsyncCausalityStatus.Error,
        Started = WFD.AsyncCausalityStatus.Started
    }

    internal enum CausalityRelation
    {
        AssignDelegate = WFD.CausalityRelation.AssignDelegate,
        Join = WFD.CausalityRelation.Join,
        Choice = WFD.CausalityRelation.Choice,
        Cancel = WFD.CausalityRelation.Cancel,
        Error = WFD.CausalityRelation.Error
    }

    internal enum CausalitySynchronousWork
    {
        CompletionNotification = WFD.CausalitySynchronousWork.CompletionNotification,
        ProgressNotification = WFD.CausalitySynchronousWork.ProgressNotification,
        Execution = WFD.CausalitySynchronousWork.Execution
    }

    internal static class AsyncCausalityTracer
    {
        static internal void EnableToETW(bool enabled)
        {
            if (enabled)
                f_LoggingOn |= Loggers.ETW;
            else
                f_LoggingOn &= ~Loggers.ETW;
        }

        internal static bool LoggingOn
        {
            [FriendAccessAllowed]
            get
            {
                return f_LoggingOn != 0;
            }
        }

        private static readonly Guid s_PlatformId = new Guid(0x4B0171A6, 0xF3D0, 0x41A0, 0x9B, 0x33, 0x02, 0x55, 0x06, 0x52, 0xB9, 0x95);
        private const WFD.CausalitySource s_CausalitySource = WFD.CausalitySource.Library;
        private static WFD.IAsyncCausalityTracerStatics s_TracerFactory;
        [Flags]
        private enum Loggers : byte
        {
            CausalityTracer = 1,
            ETW = 2
        }

        private static Loggers f_LoggingOn;
        static AsyncCausalityTracer()
        {
            if (!Environment.IsWinRTSupported)
                return;
            string ClassId = "Windows.Foundation.Diagnostics.AsyncCausalityTracer";
            Guid guid = new Guid(0x50850B26, 0x267E, 0x451B, 0xA8, 0x90, 0XAB, 0x6A, 0x37, 0x02, 0x45, 0xEE);
            Object factory = null;
            try
            {
                int hresult = Microsoft.Win32.UnsafeNativeMethods.RoGetActivationFactory(ClassId, ref guid, out factory);
                if (hresult < 0 || factory == null)
                    return;
                s_TracerFactory = (WFD.IAsyncCausalityTracerStatics)factory;
                EventRegistrationToken token = s_TracerFactory.add_TracingStatusChanged(new EventHandler<WFD.TracingStatusChangedEventArgs>(TracingStatusChangedHandler));
                Contract.Assert(token != default (EventRegistrationToken), "EventRegistrationToken is null");
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        private static void TracingStatusChangedHandler(Object sender, WFD.TracingStatusChangedEventArgs args)
        {
            if (args.Enabled)
                f_LoggingOn |= Loggers.CausalityTracer;
            else
                f_LoggingOn &= ~Loggers.CausalityTracer;
        }

        internal static void TraceOperationCreation(CausalityTraceLevel traceLevel, int taskId, string operationName, ulong relatedContext)
        {
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceOperationBegin(taskId, operationName, (long)relatedContext);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceOperationCreation((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, s_PlatformId, GetOperationId((uint)taskId), operationName, relatedContext);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        internal static void TraceOperationCompletion(CausalityTraceLevel traceLevel, int taskId, AsyncCausalityStatus status)
        {
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceOperationEnd(taskId, status);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceOperationCompletion((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, s_PlatformId, GetOperationId((uint)taskId), (WFD.AsyncCausalityStatus)status);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        internal static void TraceOperationRelation(CausalityTraceLevel traceLevel, int taskId, CausalityRelation relation)
        {
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceOperationRelation(taskId, relation);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceOperationRelation((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, s_PlatformId, GetOperationId((uint)taskId), (WFD.CausalityRelation)relation);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        internal static void TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, int taskId, CausalitySynchronousWork work)
        {
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceSynchronousWorkBegin(taskId, work);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceSynchronousWorkStart((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, s_PlatformId, GetOperationId((uint)taskId), (WFD.CausalitySynchronousWork)work);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        internal static void TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySynchronousWork work)
        {
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceSynchronousWorkEnd(work);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceSynchronousWorkCompletion((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, (WFD.CausalitySynchronousWork)work);
            }
            catch (Exception ex)
            {
                LogAndDisable(ex);
            }
        }

        private static void LogAndDisable(Exception ex)
        {
            f_LoggingOn = 0;
            Debugger.Log(0, "AsyncCausalityTracer", ex.ToString());
        }

        private static ulong GetOperationId(uint taskId)
        {
            return (((ulong)AppDomain.CurrentDomain.Id) << 32) + taskId;
        }
    }
}