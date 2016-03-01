using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    using System.Diagnostics.Tracing;

    internal sealed class TplEtwProvider : EventSource
    {
        internal bool TasksSetActivityIds;
        internal bool Debug;
        private bool DebugActivityId;
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
                AsyncCausalityTracer.EnableToETW(true);
            else if (command.Command == EventCommand.Disable)
                AsyncCausalityTracer.EnableToETW(false);
            if (IsEnabled(EventLevel.Informational, Keywords.TasksFlowActivityIds))
                ActivityTracker.Instance.Enable();
            else
                TasksSetActivityIds = IsEnabled(EventLevel.Informational, Keywords.TasksSetActivityIds);
            Debug = IsEnabled(EventLevel.Informational, Keywords.Debug);
            DebugActivityId = IsEnabled(EventLevel.Informational, Keywords.DebugActivityId);
        }

        public static TplEtwProvider Log = new TplEtwProvider();
        private TplEtwProvider()
        {
        }

        public enum ForkJoinOperationType
        {
            ParallelInvoke = 1,
            ParallelFor = 2,
            ParallelForEach = 3
        }

        public enum TaskWaitBehavior : int
        {
            Synchronous = 1,
            Asynchronous = 2
        }

        public class Tasks
        {
            public const EventTask Loop = (EventTask)1;
            public const EventTask Invoke = (EventTask)2;
            public const EventTask TaskExecute = (EventTask)3;
            public const EventTask TaskWait = (EventTask)4;
            public const EventTask ForkJoin = (EventTask)5;
            public const EventTask TaskScheduled = (EventTask)6;
            public const EventTask AwaitTaskContinuationScheduled = (EventTask)7;
            public const EventTask TraceOperation = (EventTask)8;
            public const EventTask TraceSynchronousWork = (EventTask)9;
        }

        public class Keywords
        {
            public const EventKeywords TaskTransfer = (EventKeywords)1;
            public const EventKeywords Tasks = (EventKeywords)2;
            public const EventKeywords Parallel = (EventKeywords)4;
            public const EventKeywords AsyncCausalityOperation = (EventKeywords)8;
            public const EventKeywords AsyncCausalityRelation = (EventKeywords)0x10;
            public const EventKeywords AsyncCausalitySynchronousWork = (EventKeywords)0x20;
            public const EventKeywords TaskStops = (EventKeywords)0x40;
            public const EventKeywords TasksFlowActivityIds = (EventKeywords)0x80;
            public const EventKeywords TasksSetActivityIds = (EventKeywords)0x10000;
            public const EventKeywords Debug = (EventKeywords)0x20000;
            public const EventKeywords DebugActivityId = (EventKeywords)0x40000;
        }

        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);
        private const int PARALLELLOOPBEGIN_ID = 1;
        private const int PARALLELLOOPEND_ID = 2;
        private const int PARALLELINVOKEBEGIN_ID = 3;
        private const int PARALLELINVOKEEND_ID = 4;
        private const int PARALLELFORK_ID = 5;
        private const int PARALLELJOIN_ID = 6;
        private const int TASKSCHEDULED_ID = 7;
        private const int TASKSTARTED_ID = 8;
        private const int TASKCOMPLETED_ID = 9;
        private const int TASKWAITBEGIN_ID = 10;
        private const int TASKWAITEND_ID = 11;
        private const int AWAITTASKCONTINUATIONSCHEDULED_ID = 12;
        private const int TASKWAITCONTINUATIONCOMPLETE_ID = 13;
        private const int TASKWAITCONTINUATIONSTARTED_ID = 19;
        private const int TRACEOPERATIONSTART_ID = 14;
        private const int TRACEOPERATIONSTOP_ID = 15;
        private const int TRACEOPERATIONRELATION_ID = 16;
        private const int TRACESYNCHRONOUSWORKSTART_ID = 17;
        private const int TRACESYNCHRONOUSWORKSTOP_ID = 18;
        public void ParallelLoopBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID, ForkJoinOperationType OperationType, long InclusiveFrom, long ExclusiveTo)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Parallel))
            {
                unsafe
                {
                    EventData*eventPayload = stackalloc EventData[6];
                    eventPayload[0].Size = sizeof (int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof (int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[2].Size = sizeof (int);
                    eventPayload[2].DataPointer = ((IntPtr)(&ForkJoinContextID));
                    eventPayload[3].Size = sizeof (int);
                    eventPayload[3].DataPointer = ((IntPtr)(&OperationType));
                    eventPayload[4].Size = sizeof (long);
                    eventPayload[4].DataPointer = ((IntPtr)(&InclusiveFrom));
                    eventPayload[5].Size = sizeof (long);
                    eventPayload[5].DataPointer = ((IntPtr)(&ExclusiveTo));
                    WriteEventCore(PARALLELLOOPBEGIN_ID, 6, eventPayload);
                }
            }
        }

        public void ParallelLoopEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID, long TotalIterations)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Parallel))
            {
                unsafe
                {
                    EventData*eventPayload = stackalloc EventData[4];
                    eventPayload[0].Size = sizeof (int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof (int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[2].Size = sizeof (int);
                    eventPayload[2].DataPointer = ((IntPtr)(&ForkJoinContextID));
                    eventPayload[3].Size = sizeof (long);
                    eventPayload[3].DataPointer = ((IntPtr)(&TotalIterations));
                    WriteEventCore(PARALLELLOOPEND_ID, 4, eventPayload);
                }
            }
        }

        public void ParallelInvokeBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID, ForkJoinOperationType OperationType, int ActionCount)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Parallel))
            {
                unsafe
                {
                    EventData*eventPayload = stackalloc EventData[5];
                    eventPayload[0].Size = sizeof (int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof (int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[2].Size = sizeof (int);
                    eventPayload[2].DataPointer = ((IntPtr)(&ForkJoinContextID));
                    eventPayload[3].Size = sizeof (int);
                    eventPayload[3].DataPointer = ((IntPtr)(&OperationType));
                    eventPayload[4].Size = sizeof (int);
                    eventPayload[4].DataPointer = ((IntPtr)(&ActionCount));
                    WriteEventCore(PARALLELINVOKEBEGIN_ID, 5, eventPayload);
                }
            }
        }

        public void ParallelInvokeEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Parallel))
            {
                WriteEvent(PARALLELINVOKEEND_ID, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
            }
        }

        public void ParallelFork(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Parallel))
            {
                WriteEvent(PARALLELFORK_ID, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
            }
        }

        public void ParallelJoin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ForkJoinContextID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Parallel))
            {
                WriteEvent(PARALLELJOIN_ID, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
            }
        }

        public void TaskScheduled(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID, int CreatingTaskID, int TaskCreationOptions, int appDomain)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData*eventPayload = stackalloc EventData[6];
                    eventPayload[0].Size = sizeof (int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof (int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[2].Size = sizeof (int);
                    eventPayload[2].DataPointer = ((IntPtr)(&TaskID));
                    eventPayload[3].Size = sizeof (int);
                    eventPayload[3].DataPointer = ((IntPtr)(&CreatingTaskID));
                    eventPayload[4].Size = sizeof (int);
                    eventPayload[4].DataPointer = ((IntPtr)(&TaskCreationOptions));
                    eventPayload[5].Size = sizeof (int);
                    eventPayload[5].DataPointer = ((IntPtr)(&appDomain));
                    if (TasksSetActivityIds)
                    {
                        Guid childActivityId = CreateGuidForTaskID(TaskID);
                        WriteEventWithRelatedActivityIdCore(TASKSCHEDULED_ID, &childActivityId, 6, eventPayload);
                    }
                    else
                        WriteEventCore(TASKSCHEDULED_ID, 6, eventPayload);
                }
            }
        }

        public void TaskStarted(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Tasks))
                WriteEvent(TASKSTARTED_ID, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
        }

        public void TaskCompleted(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID, bool IsExceptional)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Tasks))
            {
                unsafe
                {
                    EventData*eventPayload = stackalloc EventData[4];
                    Int32 isExceptionalInt = IsExceptional ? 1 : 0;
                    eventPayload[0].Size = sizeof (int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof (int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[2].Size = sizeof (int);
                    eventPayload[2].DataPointer = ((IntPtr)(&TaskID));
                    eventPayload[3].Size = sizeof (int);
                    eventPayload[3].DataPointer = ((IntPtr)(&isExceptionalInt));
                    WriteEventCore(TASKCOMPLETED_ID, 4, eventPayload);
                }
            }
        }

        public void TaskWaitBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID, TaskWaitBehavior Behavior, int ContinueWithTaskID, int appDomain)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData*eventPayload = stackalloc EventData[5];
                    eventPayload[0].Size = sizeof (int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof (int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[2].Size = sizeof (int);
                    eventPayload[2].DataPointer = ((IntPtr)(&TaskID));
                    eventPayload[3].Size = sizeof (int);
                    eventPayload[3].DataPointer = ((IntPtr)(&Behavior));
                    eventPayload[4].Size = sizeof (int);
                    eventPayload[4].DataPointer = ((IntPtr)(&ContinueWithTaskID));
                    if (TasksSetActivityIds)
                    {
                        Guid childActivityId = CreateGuidForTaskID(TaskID);
                        WriteEventWithRelatedActivityIdCore(TASKWAITBEGIN_ID, &childActivityId, 5, eventPayload);
                    }
                    else
                        WriteEventCore(TASKWAITBEGIN_ID, 5, eventPayload);
                }
            }
        }

        public void TaskWaitEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID, int TaskID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Tasks))
                WriteEvent(TASKWAITEND_ID, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
        }

        public void TaskWaitContinuationComplete(int TaskID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Tasks))
                WriteEvent(TASKWAITCONTINUATIONCOMPLETE_ID, TaskID);
        }

        public void TaskWaitContinuationStarted(int TaskID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Tasks))
                WriteEvent(TASKWAITCONTINUATIONSTARTED_ID, TaskID);
        }

        public void AwaitTaskContinuationScheduled(int OriginatingTaskSchedulerID, int OriginatingTaskID, int ContinuwWithTaskId)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData*eventPayload = stackalloc EventData[3];
                    eventPayload[0].Size = sizeof (int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[1].Size = sizeof (int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[2].Size = sizeof (int);
                    eventPayload[2].DataPointer = ((IntPtr)(&ContinuwWithTaskId));
                    if (TasksSetActivityIds)
                    {
                        Guid continuationActivityId = CreateGuidForTaskID(ContinuwWithTaskId);
                        WriteEventWithRelatedActivityIdCore(AWAITTASKCONTINUATIONSCHEDULED_ID, &continuationActivityId, 3, eventPayload);
                    }
                    else
                        WriteEventCore(AWAITTASKCONTINUATIONSCHEDULED_ID, 3, eventPayload);
                }
            }
        }

        public void TraceOperationBegin(int TaskID, string OperationName, long RelatedContext)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityOperation))
            {
                unsafe
                {
                    fixed (char *operationNamePtr = OperationName)
                    {
                        EventData*eventPayload = stackalloc EventData[3];
                        eventPayload[0].Size = sizeof (int);
                        eventPayload[0].DataPointer = ((IntPtr)(&TaskID));
                        eventPayload[1].Size = ((OperationName.Length + 1) * 2);
                        eventPayload[1].DataPointer = ((IntPtr)operationNamePtr);
                        eventPayload[2].Size = sizeof (long);
                        eventPayload[2].DataPointer = ((IntPtr)(&RelatedContext));
                        WriteEventCore(TRACEOPERATIONSTART_ID, 3, eventPayload);
                    }
                }
            }
        }

        public void TraceOperationRelation(int TaskID, CausalityRelation Relation)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityRelation))
                WriteEvent(TRACEOPERATIONRELATION_ID, TaskID, (int)Relation);
        }

        public void TraceOperationEnd(int TaskID, AsyncCausalityStatus Status)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityOperation))
                WriteEvent(TRACEOPERATIONSTOP_ID, TaskID, (int)Status);
        }

        public void TraceSynchronousWorkBegin(int TaskID, CausalitySynchronousWork Work)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalitySynchronousWork))
                WriteEvent(TRACESYNCHRONOUSWORKSTART_ID, TaskID, (int)Work);
        }

        public void TraceSynchronousWorkEnd(CausalitySynchronousWork Work)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalitySynchronousWork))
            {
                unsafe
                {
                    EventData*eventPayload = stackalloc EventData[1];
                    eventPayload[0].Size = sizeof (int);
                    eventPayload[0].DataPointer = ((IntPtr)(&Work));
                    WriteEventCore(TRACESYNCHRONOUSWORKSTOP_ID, 1, eventPayload);
                }
            }
        }

        unsafe public void RunningContinuation(int TaskID, object Object)
        {
            RunningContinuation(TaskID, (long)*((void **)JitHelpers.UnsafeCastToStackPointer(ref Object)));
        }

        private void RunningContinuation(int TaskID, long Object)
        {
            if (Debug)
                WriteEvent(20, TaskID, Object);
        }

        unsafe public void RunningContinuationList(int TaskID, int Index, object Object)
        {
            RunningContinuationList(TaskID, Index, (long)*((void **)JitHelpers.UnsafeCastToStackPointer(ref Object)));
        }

        public void RunningContinuationList(int TaskID, int Index, long Object)
        {
            if (Debug)
                WriteEvent(21, TaskID, Index, Object);
        }

        public void DebugMessage(string Message)
        {
            WriteEvent(22, Message);
        }

        public void DebugFacilityMessage(string Facility, string Message)
        {
            WriteEvent(23, Facility, Message);
        }

        public void DebugFacilityMessage1(string Facility, string Message, string Value1)
        {
            WriteEvent(24, Facility, Message, Value1);
        }

        public void SetActivityId(Guid NewId)
        {
            if (DebugActivityId)
                WriteEvent(25, NewId);
        }

        public void NewID(int TaskID)
        {
            if (Debug)
                WriteEvent(26, TaskID);
        }

        internal static Guid CreateGuidForTaskID(int taskID)
        {
            uint pid = EventSource.s_currentPid;
            int appDomainID = System.Threading.Thread.GetDomainID();
            return new Guid(taskID, (short)appDomainID, (short)(appDomainID >> 16), (byte)pid, (byte)(pid >> 8), (byte)(pid >> 16), (byte)(pid >> 24), 0xff, 0xdc, 0xd7, 0xb5);
        }
    }
}