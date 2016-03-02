
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing
{
    internal class ActivityTracker
    {
        public void OnStart(string providerName, string activityName, int task, ref Guid activityId, ref Guid relatedActivityId, EventActivityOptions options)
        {
            if (m_current == null)
            {
                if (m_checkedForEnable)
                    return;
                m_checkedForEnable = true;
                if (TplEtwProvider.Log.IsEnabled(EventLevel.Informational, TplEtwProvider.Keywords.TasksFlowActivityIds))
                    Enable();
                if (m_current == null)
                    return;
            }

                        var currentActivity = m_current.Value;
            var fullActivityName = NormalizeActivityName(providerName, activityName, task);
            var etwLog = TplEtwProvider.Log;
            if (etwLog.Debug)
            {
                etwLog.DebugFacilityMessage("OnStartEnter", fullActivityName);
                etwLog.DebugFacilityMessage("OnStartEnterActivityState", ActivityInfo.LiveActivities(currentActivity));
            }

            if (currentActivity != null)
            {
                if (currentActivity.m_level >= MAX_ACTIVITY_DEPTH)
                {
                    activityId = Guid.Empty;
                    relatedActivityId = Guid.Empty;
                    if (etwLog.Debug)
                        etwLog.DebugFacilityMessage("OnStartRET", "Fail");
                    return;
                }

                if ((options & EventActivityOptions.Recursive) == 0)
                {
                    ActivityInfo existingActivity = FindActiveActivity(fullActivityName, currentActivity);
                    if (existingActivity != null)
                    {
                        OnStop(providerName, activityName, task, ref activityId);
                        currentActivity = m_current.Value;
                    }
                }
            }

            long id;
            if (currentActivity == null)
                id = Interlocked.Increment(ref m_nextId);
            else
                id = Interlocked.Increment(ref currentActivity.m_lastChildID);
            relatedActivityId = EventSource.CurrentThreadActivityId;
            ActivityInfo newActivity = new ActivityInfo(fullActivityName, id, currentActivity, relatedActivityId, options);
            m_current.Value = newActivity;
            activityId = newActivity.ActivityId;
            if (etwLog.Debug)
            {
                etwLog.DebugFacilityMessage("OnStartRetActivityState", ActivityInfo.LiveActivities(newActivity));
                etwLog.DebugFacilityMessage1("OnStartRet", activityId.ToString(), relatedActivityId.ToString());
            }
        }

        public void OnStop(string providerName, string activityName, int task, ref Guid activityId)
        {
            if (m_current == null)
                return;
            var fullActivityName = NormalizeActivityName(providerName, activityName, task);
            var etwLog = TplEtwProvider.Log;
            if (etwLog.Debug)
            {
                etwLog.DebugFacilityMessage("OnStopEnter", fullActivityName);
                etwLog.DebugFacilityMessage("OnStopEnterActivityState", ActivityInfo.LiveActivities(m_current.Value));
            }

            for (;;)
            {
                ActivityInfo currentActivity = m_current.Value;
                ActivityInfo newCurrentActivity = null;
                ActivityInfo activityToStop = FindActiveActivity(fullActivityName, currentActivity);
                if (activityToStop == null)
                {
                    activityId = Guid.Empty;
                    if (etwLog.Debug)
                        etwLog.DebugFacilityMessage("OnStopRET", "Fail");
                    return;
                }

                activityId = activityToStop.ActivityId;
                ActivityInfo orphan = currentActivity;
                while (orphan != activityToStop && orphan != null)
                {
                    if (orphan.m_stopped != 0)
                    {
                        orphan = orphan.m_creator;
                        continue;
                    }

                    if (orphan.CanBeOrphan())
                    {
                        if (newCurrentActivity == null)
                            newCurrentActivity = orphan;
                    }
                    else
                    {
                        orphan.m_stopped = 1;
                                            }

                    orphan = orphan.m_creator;
                }

                if (Interlocked.CompareExchange(ref activityToStop.m_stopped, 1, 0) == 0)
                {
                    if (newCurrentActivity == null)
                        newCurrentActivity = activityToStop.m_creator;
                    m_current.Value = newCurrentActivity;
                    if (etwLog.Debug)
                    {
                        etwLog.DebugFacilityMessage("OnStopRetActivityState", ActivityInfo.LiveActivities(newCurrentActivity));
                        etwLog.DebugFacilityMessage("OnStopRet", activityId.ToString());
                    }

                    return;
                }
            }
        }

        public void Enable()
        {
            if (m_current == null)
            {
                try
                {
                    m_current = new AsyncLocal<ActivityInfo>(ActivityChanging);
                }
                catch (NotImplementedException)
                {
                    System.Diagnostics.Debugger.Log(0, null, "Activity Enabled() called but AsyncLocals Not Supported (pre V4.6).  Ignoring Enable");
                }
            }
        }

        public static ActivityTracker Instance
        {
            get
            {
                return s_activityTrackerInstance;
            }
        }

        private Guid CurrentActivityId
        {
            get
            {
                return m_current.Value.ActivityId;
            }
        }

        private ActivityInfo FindActiveActivity(string name, ActivityInfo startLocation)
        {
            var activity = startLocation;
            while (activity != null)
            {
                if (name == activity.m_name && activity.m_stopped == 0)
                    return activity;
                activity = activity.m_creator;
            }

            return null;
        }

        private string NormalizeActivityName(string providerName, string activityName, int task)
        {
            if (activityName.EndsWith(EventSource.s_ActivityStartSuffix))
                activityName = activityName.Substring(0, activityName.Length - EventSource.s_ActivityStartSuffix.Length);
            else if (activityName.EndsWith(EventSource.s_ActivityStopSuffix))
                activityName = activityName.Substring(0, activityName.Length - EventSource.s_ActivityStopSuffix.Length);
            else if (task != 0)
                activityName = "task" + task.ToString();
            return providerName + activityName;
        }

        private class ActivityInfo
        {
            public ActivityInfo(string name, long uniqueId, ActivityInfo creator, Guid activityIDToRestore, EventActivityOptions options)
            {
                m_name = name;
                m_eventOptions = options;
                m_creator = creator;
                m_uniqueId = uniqueId;
                m_level = creator != null ? creator.m_level + 1 : 0;
                m_activityIdToRestore = activityIDToRestore;
                CreateActivityPathGuid(out m_guid, out m_activityPathGuidOffset);
            }

            public Guid ActivityId
            {
                get
                {
                    return m_guid;
                }
            }

            public static string Path(ActivityInfo activityInfo)
            {
                if (activityInfo == null)
                    return ("");
                return Path(activityInfo.m_creator) + "/" + activityInfo.m_uniqueId;
            }

            public override string ToString()
            {
                string dead = "";
                if (m_stopped != 0)
                    dead = ",DEAD";
                return m_name + "(" + Path(this) + dead + ")";
            }

            public static string LiveActivities(ActivityInfo list)
            {
                if (list == null)
                    return "";
                return list.ToString() + ";" + LiveActivities(list.m_creator);
            }

            public bool CanBeOrphan()
            {
                if ((m_eventOptions & EventActivityOptions.Detachable) != 0)
                    return true;
                return false;
            }

            private unsafe void CreateActivityPathGuid(out Guid idRet, out int activityPathGuidOffset)
            {
                fixed (Guid*outPtr = &idRet)
                {
                    int activityPathGuidOffsetStart = 0;
                    if (m_creator != null)
                    {
                        activityPathGuidOffsetStart = m_creator.m_activityPathGuidOffset;
                        idRet = m_creator.m_guid;
                    }
                    else
                    {
                        int appDomainID = 0;
                        appDomainID = System.Threading.Thread.GetDomainID();
                        activityPathGuidOffsetStart = AddIdToGuid(outPtr, activityPathGuidOffsetStart, (uint)appDomainID);
                    }

                    activityPathGuidOffset = AddIdToGuid(outPtr, activityPathGuidOffsetStart, (uint)m_uniqueId);
                    if (12 < activityPathGuidOffset)
                        CreateOverflowGuid(outPtr);
                }
            }

            private unsafe void CreateOverflowGuid(Guid*outPtr)
            {
                for (ActivityInfo ancestor = m_creator; ancestor != null; ancestor = ancestor.m_creator)
                {
                    if (ancestor.m_activityPathGuidOffset <= 10)
                    {
                        uint id = unchecked ((uint)Interlocked.Increment(ref ancestor.m_lastChildID));
                        *outPtr = ancestor.m_guid;
                        int endId = AddIdToGuid(outPtr, ancestor.m_activityPathGuidOffset, id, true);
                        if (endId <= 12)
                            break;
                    }
                }
            }

            enum NumberListCodes : byte
            {
                End = 0x0,
                LastImmediateValue = 0xA,
                PrefixCode = 0xB,
                MultiByte1 = 0xC
            }

            private static unsafe int AddIdToGuid(Guid*outPtr, int whereToAddId, uint id, bool overflow = false)
            {
                byte *ptr = (byte *)outPtr;
                byte *endPtr = ptr + 12;
                ptr += whereToAddId;
                if (endPtr <= ptr)
                    return 13;
                if (0 < id && id <= (uint)NumberListCodes.LastImmediateValue && !overflow)
                    WriteNibble(ref ptr, endPtr, id);
                else
                {
                    uint len = 4;
                    if (id <= 0xFF)
                        len = 1;
                    else if (id <= 0xFFFF)
                        len = 2;
                    else if (id <= 0xFFFFFF)
                        len = 3;
                    if (overflow)
                    {
                        if (endPtr <= ptr + 2)
                            return 13;
                        WriteNibble(ref ptr, endPtr, (uint)NumberListCodes.PrefixCode);
                    }

                    WriteNibble(ref ptr, endPtr, (uint)NumberListCodes.MultiByte1 + (len - 1));
                    if (ptr < endPtr && *ptr != 0)
                    {
                        if (id < 4096)
                        {
                            *ptr = (byte)(((uint)NumberListCodes.MultiByte1 << 4) + (id >> 8));
                            id &= 0xFF;
                        }

                        ptr++;
                    }

                    while (0 < len)
                    {
                        if (endPtr <= ptr)
                        {
                            ptr++;
                            break;
                        }

                        *ptr++ = (byte)id;
                        id = (id >> 8);
                        --len;
                    }
                }

                uint *sumPtr = (uint *)outPtr;
                sumPtr[3] = sumPtr[0] + sumPtr[1] + sumPtr[2] + 0x599D99AD;
                return (int)(ptr - ((byte *)outPtr));
            }

            private static unsafe void WriteNibble(ref byte *ptr, byte *endPtr, uint value)
            {
                                                if (*ptr != 0)
                    *ptr++ |= (byte)value;
                else
                    *ptr = (byte)(value << 4);
            }

            readonly internal string m_name;
            readonly long m_uniqueId;
            internal readonly Guid m_guid;
            internal readonly int m_activityPathGuidOffset;
            internal readonly int m_level;
            readonly internal EventActivityOptions m_eventOptions;
            internal long m_lastChildID;
            internal int m_stopped;
            readonly internal ActivityInfo m_creator;
            readonly internal Guid m_activityIdToRestore;
        }

        void ActivityChanging(AsyncLocalValueChangedArgs<ActivityInfo> args)
        {
            ActivityInfo cur = args.CurrentValue;
            ActivityInfo prev = args.PreviousValue;
            if (prev != null && prev.m_creator == cur)
            {
                if (cur == null || prev.m_activityIdToRestore != cur.ActivityId)
                {
                    EventSource.SetCurrentThreadActivityId(prev.m_activityIdToRestore);
                    return;
                }
            }

            while (cur != null)
            {
                if (cur.m_stopped == 0)
                {
                    EventSource.SetCurrentThreadActivityId(cur.ActivityId);
                    return;
                }

                cur = cur.m_creator;
            }
        }

        AsyncLocal<ActivityInfo> m_current;
        bool m_checkedForEnable;
        private static ActivityTracker s_activityTrackerInstance = new ActivityTracker();
        static long m_nextId = 0;
        private const ushort MAX_ACTIVITY_DEPTH = 100;
    }
}