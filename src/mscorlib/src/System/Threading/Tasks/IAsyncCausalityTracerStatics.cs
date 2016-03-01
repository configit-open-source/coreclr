using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Windows.Foundation.Diagnostics
{
    internal interface IAsyncCausalityTracerStatics
    {
        void TraceOperationCreation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, string operationName, ulong relatedContext);
        void TraceOperationCompletion(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, AsyncCausalityStatus status);
        void TraceOperationRelation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalityRelation relation);
        void TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalitySynchronousWork work);
        void TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySource source, CausalitySynchronousWork work);
        EventRegistrationToken add_TracingStatusChanged(System.EventHandler<TracingStatusChangedEventArgs> eventHandler);
        void remove_TracingStatusChanged(EventRegistrationToken token);
    }

    internal interface ITracingStatusChangedEventArgs
    {
        bool Enabled
        {
            get;
        }

        CausalityTraceLevel TraceLevel
        {
            get;
        }
    }

    internal sealed class TracingStatusChangedEventArgs : ITracingStatusChangedEventArgs
    {
        public extern bool Enabled
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        public extern CausalityTraceLevel TraceLevel
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }
    }

    internal enum CausalityRelation
    {
        AssignDelegate,
        Join,
        Choice,
        Cancel,
        Error
    }

    internal enum CausalitySource
    {
        Application,
        Library,
        System
    }

    internal enum CausalitySynchronousWork
    {
        CompletionNotification,
        ProgressNotification,
        Execution
    }

    internal enum CausalityTraceLevel
    {
        Required,
        Important,
        Verbose
    }

    internal enum AsyncCausalityStatus
    {
        Canceled = 2,
        Completed = 1,
        Error = 3,
        Started = 0
    }
}