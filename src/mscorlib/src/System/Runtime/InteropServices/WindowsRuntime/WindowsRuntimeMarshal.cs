using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    public static class WindowsRuntimeMarshal
    {
        public static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod, Action<EventRegistrationToken> removeMethod, T handler)
        {
            if (addMethod == null)
                throw new ArgumentNullException("addMethod");
            if (removeMethod == null)
                throw new ArgumentNullException("removeMethod");
            Contract.EndContractBlock();
            if (handler == null)
            {
                return;
            }

            object target = removeMethod.Target;
            if (target == null || Marshal.IsComObject(target))
                NativeOrStaticEventRegistrationImpl.AddEventHandler<T>(addMethod, removeMethod, handler);
            else
                ManagedEventRegistrationImpl.AddEventHandler<T>(addMethod, removeMethod, handler);
        }

        public static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler)
        {
            if (removeMethod == null)
                throw new ArgumentNullException("removeMethod");
            Contract.EndContractBlock();
            if (handler == null)
            {
                return;
            }

            object target = removeMethod.Target;
            if (target == null || Marshal.IsComObject(target))
                NativeOrStaticEventRegistrationImpl.RemoveEventHandler<T>(removeMethod, handler);
            else
                ManagedEventRegistrationImpl.RemoveEventHandler<T>(removeMethod, handler);
        }

        public static void RemoveAllEventHandlers(Action<EventRegistrationToken> removeMethod)
        {
            if (removeMethod == null)
                throw new ArgumentNullException("removeMethod");
            Contract.EndContractBlock();
            object target = removeMethod.Target;
            if (target == null || Marshal.IsComObject(target))
                NativeOrStaticEventRegistrationImpl.RemoveAllEventHandlers(removeMethod);
            else
                ManagedEventRegistrationImpl.RemoveAllEventHandlers(removeMethod);
        }

        internal static int GetRegistrationTokenCacheSize()
        {
            int count = 0;
            if (ManagedEventRegistrationImpl.s_eventRegistrations != null)
            {
                lock (ManagedEventRegistrationImpl.s_eventRegistrations)
                {
                    count += ManagedEventRegistrationImpl.s_eventRegistrations.Keys.Count;
                }
            }

            if (NativeOrStaticEventRegistrationImpl.s_eventRegistrations != null)
            {
                lock (NativeOrStaticEventRegistrationImpl.s_eventRegistrations)
                {
                    count += NativeOrStaticEventRegistrationImpl.s_eventRegistrations.Count;
                }
            }

            return count;
        }

        internal struct EventRegistrationTokenList
        {
            private EventRegistrationToken firstToken;
            private List<EventRegistrationToken> restTokens;
            internal EventRegistrationTokenList(EventRegistrationToken token)
            {
                firstToken = token;
                restTokens = null;
            }

            internal EventRegistrationTokenList(EventRegistrationTokenList list)
            {
                firstToken = list.firstToken;
                restTokens = list.restTokens;
            }

            public bool Push(EventRegistrationToken token)
            {
                bool needCopy = false;
                if (restTokens == null)
                {
                    restTokens = new List<EventRegistrationToken>();
                    needCopy = true;
                }

                restTokens.Add(token);
                return needCopy;
            }

            public bool Pop(out EventRegistrationToken token)
            {
                if (restTokens == null || restTokens.Count == 0)
                {
                    token = firstToken;
                    return false;
                }

                int last = restTokens.Count - 1;
                token = restTokens[last];
                restTokens.RemoveAt(last);
                return true;
            }

            public void CopyTo(List<EventRegistrationToken> tokens)
            {
                tokens.Add(firstToken);
                if (restTokens != null)
                    tokens.AddRange(restTokens);
            }
        }

        internal static class ManagedEventRegistrationImpl
        {
            internal volatile static ConditionalWeakTable<object, Dictionary<MethodInfo, Dictionary<object, EventRegistrationTokenList>>> s_eventRegistrations = new ConditionalWeakTable<object, Dictionary<MethodInfo, Dictionary<object, EventRegistrationTokenList>>>();
            internal static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod, Action<EventRegistrationToken> removeMethod, T handler)
            {
                Contract.Requires(addMethod != null);
                Contract.Requires(removeMethod != null);
                object instance = removeMethod.Target;
                Dictionary<object, EventRegistrationTokenList> registrationTokens = GetEventRegistrationTokenTable(instance, removeMethod);
                EventRegistrationToken token = addMethod(handler);
                lock (registrationTokens)
                {
                    EventRegistrationTokenList tokens;
                    if (!registrationTokens.TryGetValue(handler, out tokens))
                    {
                        tokens = new EventRegistrationTokenList(token);
                        registrationTokens[handler] = tokens;
                    }
                    else
                    {
                        bool needCopy = tokens.Push(token);
                        if (needCopy)
                            registrationTokens[handler] = tokens;
                    }

                    BCLDebug.Log("INTEROP", "[WinRT_Eventing] Event subscribed for managed instance = " + instance + ", handler = " + handler + "\n");
                }
            }

            private static Dictionary<object, EventRegistrationTokenList> GetEventRegistrationTokenTable(object instance, Action<EventRegistrationToken> removeMethod)
            {
                Contract.Requires(instance != null);
                Contract.Requires(removeMethod != null);
                Contract.Requires(s_eventRegistrations != null);
                lock (s_eventRegistrations)
                {
                    Dictionary<MethodInfo, Dictionary<object, EventRegistrationTokenList>> instanceMap = null;
                    if (!s_eventRegistrations.TryGetValue(instance, out instanceMap))
                    {
                        instanceMap = new Dictionary<MethodInfo, Dictionary<object, EventRegistrationTokenList>>();
                        s_eventRegistrations.Add(instance, instanceMap);
                    }

                    Dictionary<object, EventRegistrationTokenList> tokens = null;
                    if (!instanceMap.TryGetValue(removeMethod.Method, out tokens))
                    {
                        tokens = new Dictionary<object, EventRegistrationTokenList>();
                        instanceMap.Add(removeMethod.Method, tokens);
                    }

                    return tokens;
                }
            }

            internal static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler)
            {
                Contract.Requires(removeMethod != null);
                object instance = removeMethod.Target;
                Dictionary<object, EventRegistrationTokenList> registrationTokens = GetEventRegistrationTokenTable(instance, removeMethod);
                EventRegistrationToken token;
                lock (registrationTokens)
                {
                    EventRegistrationTokenList tokens;
                    if (!registrationTokens.TryGetValue(handler, out tokens))
                    {
                        BCLDebug.Log("INTEROP", "[WinRT_Eventing] no registrationTokens found for instance=" + instance + ", handler= " + handler + "\n");
                        return;
                    }

                    bool moreItems = tokens.Pop(out token);
                    if (!moreItems)
                    {
                        registrationTokens.Remove(handler);
                    }
                }

                removeMethod(token);
                BCLDebug.Log("INTEROP", "[WinRT_Eventing] Event unsubscribed for managed instance = " + instance + ", handler = " + handler + ", token = " + token.m_value + "\n");
            }

            internal static void RemoveAllEventHandlers(Action<EventRegistrationToken> removeMethod)
            {
                Contract.Requires(removeMethod != null);
                object instance = removeMethod.Target;
                Dictionary<object, EventRegistrationTokenList> registrationTokens = GetEventRegistrationTokenTable(instance, removeMethod);
                List<EventRegistrationToken> tokensToRemove = new List<EventRegistrationToken>();
                lock (registrationTokens)
                {
                    foreach (EventRegistrationTokenList tokens in registrationTokens.Values)
                    {
                        tokens.CopyTo(tokensToRemove);
                    }

                    registrationTokens.Clear();
                    BCLDebug.Log("INTEROP", "[WinRT_Eventing] Cache cleared for managed instance = " + instance + "\n");
                }

                BCLDebug.Log("INTEROP", "[WinRT_Eventing] Start removing all events for instance = " + instance + "\n");
                CallRemoveMethods(removeMethod, tokensToRemove);
                BCLDebug.Log("INTEROP", "[WinRT_Eventing] Finished removing all events for instance = " + instance + "\n");
            }
        }

        internal static class NativeOrStaticEventRegistrationImpl
        {
            internal struct EventCacheKey
            {
                internal object target;
                internal MethodInfo method;
                public override string ToString()
                {
                    return "(" + target + ", " + method + ")";
                }
            }

            internal class EventCacheKeyEqualityComparer : IEqualityComparer<EventCacheKey>
            {
                public bool Equals(EventCacheKey lhs, EventCacheKey rhs)
                {
                    return (Object.Equals(lhs.target, rhs.target) && Object.Equals(lhs.method, rhs.method));
                }

                public int GetHashCode(EventCacheKey key)
                {
                    return key.target.GetHashCode() ^ key.method.GetHashCode();
                }
            }

            internal class EventRegistrationTokenListWithCount
            {
                private TokenListCount _tokenListCount;
                EventRegistrationTokenList _tokenList;
                internal EventRegistrationTokenListWithCount(TokenListCount tokenListCount, EventRegistrationToken token)
                {
                    _tokenListCount = tokenListCount;
                    _tokenListCount.Inc();
                    _tokenList = new EventRegistrationTokenList(token);
                }

                ~EventRegistrationTokenListWithCount()
                {
                    BCLDebug.Log("INTEROP", "[WinRT_Eventing] Finalizing EventRegistrationTokenList for " + _tokenListCount.Key + "\n");
                    _tokenListCount.Dec();
                }

                public void Push(EventRegistrationToken token)
                {
                    _tokenList.Push(token);
                }

                public bool Pop(out EventRegistrationToken token)
                {
                    return _tokenList.Pop(out token);
                }

                public void CopyTo(List<EventRegistrationToken> tokens)
                {
                    _tokenList.CopyTo(tokens);
                }
            }

            internal class TokenListCount
            {
                private int _count;
                private EventCacheKey _key;
                internal TokenListCount(EventCacheKey key)
                {
                    _key = key;
                }

                internal EventCacheKey Key
                {
                    get
                    {
                        return _key;
                    }
                }

                internal void Inc()
                {
                    int newCount = Interlocked.Increment(ref _count);
                    BCLDebug.Log("INTEROP", "[WinRT_Eventing] Incremented TokenListCount for " + _key + ", Value = " + newCount + "\n");
                }

                internal void Dec()
                {
                    s_eventCacheRWLock.AcquireWriterLock(Timeout.Infinite);
                    try
                    {
                        int newCount = Interlocked.Decrement(ref _count);
                        BCLDebug.Log("INTEROP", "[WinRT_Eventing] Decremented TokenListCount for " + _key + ", Value = " + newCount + "\n");
                        if (newCount == 0)
                            CleanupCache();
                    }
                    finally
                    {
                        s_eventCacheRWLock.ReleaseWriterLock();
                    }
                }

                private void CleanupCache()
                {
                    Contract.Requires(s_eventRegistrations != null);
                    BCLDebug.Log("INTEROP", "[WinRT_Eventing] Removing " + _key + " from cache" + "\n");
                    s_eventRegistrations.Remove(_key);
                    BCLDebug.Log("INTEROP", "[WinRT_Eventing] s_eventRegistrations size = " + s_eventRegistrations.Count + "\n");
                }
            }

            internal struct EventCacheEntry
            {
                internal ConditionalWeakTable<object, EventRegistrationTokenListWithCount> registrationTable;
                internal TokenListCount tokenListCount;
            }

            internal volatile static Dictionary<EventCacheKey, EventCacheEntry> s_eventRegistrations = new Dictionary<EventCacheKey, EventCacheEntry>(new EventCacheKeyEqualityComparer());
            private volatile static MyReaderWriterLock s_eventCacheRWLock = new MyReaderWriterLock();
            private static object GetInstanceKey(Action<EventRegistrationToken> removeMethod)
            {
                object target = removeMethod.Target;
                Contract.Assert(target == null || Marshal.IsComObject(target), "Must be null or a RCW");
                if (target == null)
                    return removeMethod.Method.DeclaringType;
                return (object)Marshal.GetRawIUnknownForComObjectNoAddRef(target);
            }

            internal static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod, Action<EventRegistrationToken> removeMethod, T handler)
            {
                object instanceKey = GetInstanceKey(removeMethod);
                EventRegistrationToken token = addMethod(handler);
                bool tokenAdded = false;
                try
                {
                    EventRegistrationTokenListWithCount tokens;
                    s_eventCacheRWLock.AcquireReaderLock(Timeout.Infinite);
                    try
                    {
                        TokenListCount tokenListCount;
                        ConditionalWeakTable<object, EventRegistrationTokenListWithCount> registrationTokens = GetOrCreateEventRegistrationTokenTable(instanceKey, removeMethod, out tokenListCount);
                        lock (registrationTokens)
                        {
                            object key = registrationTokens.FindEquivalentKeyUnsafe(handler, out tokens);
                            if (key == null)
                            {
                                tokens = new EventRegistrationTokenListWithCount(tokenListCount, token);
                                registrationTokens.Add(handler, tokens);
                            }
                            else
                            {
                                tokens.Push(token);
                            }

                            tokenAdded = true;
                        }
                    }
                    finally
                    {
                        s_eventCacheRWLock.ReleaseReaderLock();
                    }

                    BCLDebug.Log("INTEROP", "[WinRT_Eventing] Event subscribed for instance = " + instanceKey + ", handler = " + handler + "\n");
                }
                catch (Exception)
                {
                    if (!tokenAdded)
                    {
                        removeMethod(token);
                    }

                    throw;
                }
            }

            private static ConditionalWeakTable<object, EventRegistrationTokenListWithCount> GetEventRegistrationTokenTableNoCreate(object instance, Action<EventRegistrationToken> removeMethod, out TokenListCount tokenListCount)
            {
                Contract.Requires(instance != null);
                Contract.Requires(removeMethod != null);
                return GetEventRegistrationTokenTableInternal(instance, removeMethod, out tokenListCount, false);
            }

            private static ConditionalWeakTable<object, EventRegistrationTokenListWithCount> GetOrCreateEventRegistrationTokenTable(object instance, Action<EventRegistrationToken> removeMethod, out TokenListCount tokenListCount)
            {
                Contract.Requires(instance != null);
                Contract.Requires(removeMethod != null);
                return GetEventRegistrationTokenTableInternal(instance, removeMethod, out tokenListCount, true);
            }

            private static ConditionalWeakTable<object, EventRegistrationTokenListWithCount> GetEventRegistrationTokenTableInternal(object instance, Action<EventRegistrationToken> removeMethod, out TokenListCount tokenListCount, bool createIfNotFound)
            {
                Contract.Requires(instance != null);
                Contract.Requires(removeMethod != null);
                Contract.Requires(s_eventRegistrations != null);
                EventCacheKey eventCacheKey;
                eventCacheKey.target = instance;
                eventCacheKey.method = removeMethod.Method;
                lock (s_eventRegistrations)
                {
                    EventCacheEntry eventCacheEntry;
                    if (!s_eventRegistrations.TryGetValue(eventCacheKey, out eventCacheEntry))
                    {
                        if (!createIfNotFound)
                        {
                            tokenListCount = null;
                            return null;
                        }

                        BCLDebug.Log("INTEROP", "[WinRT_Eventing] Adding (" + instance + "," + removeMethod.Method + ") into cache" + "\n");
                        eventCacheEntry = new EventCacheEntry();
                        eventCacheEntry.registrationTable = new ConditionalWeakTable<object, EventRegistrationTokenListWithCount>();
                        eventCacheEntry.tokenListCount = new TokenListCount(eventCacheKey);
                        s_eventRegistrations.Add(eventCacheKey, eventCacheEntry);
                    }

                    tokenListCount = eventCacheEntry.tokenListCount;
                    return eventCacheEntry.registrationTable;
                }
            }

            internal static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler)
            {
                object instanceKey = GetInstanceKey(removeMethod);
                EventRegistrationToken token;
                s_eventCacheRWLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    TokenListCount tokenListCount;
                    ConditionalWeakTable<object, EventRegistrationTokenListWithCount> registrationTokens = GetEventRegistrationTokenTableNoCreate(instanceKey, removeMethod, out tokenListCount);
                    if (registrationTokens == null)
                    {
                        BCLDebug.Log("INTEROP", "[WinRT_Eventing] no registrationTokens found for instance=" + instanceKey + ", handler= " + handler + "\n");
                        return;
                    }

                    lock (registrationTokens)
                    {
                        EventRegistrationTokenListWithCount tokens;
                        object key = registrationTokens.FindEquivalentKeyUnsafe(handler, out tokens);
                        Contract.Assert((key != null && tokens != null) || (key == null && tokens == null), "key and tokens must be both null or non-null");
                        if (tokens == null)
                        {
                            BCLDebug.Log("INTEROP", "[WinRT_Eventing] no token list found for instance=" + instanceKey + ", handler= " + handler + "\n");
                            return;
                        }

                        bool moreItems = tokens.Pop(out token);
                        if (!moreItems)
                        {
                            registrationTokens.Remove(key);
                        }

                        BCLDebug.Log("INTEROP", "[WinRT_Eventing] Event unsubscribed for managed instance = " + instanceKey + ", handler = " + handler + ", token = " + token.m_value + "\n");
                    }
                }
                finally
                {
                    s_eventCacheRWLock.ReleaseReaderLock();
                }

                removeMethod(token);
            }

            internal static void RemoveAllEventHandlers(Action<EventRegistrationToken> removeMethod)
            {
                object instanceKey = GetInstanceKey(removeMethod);
                List<EventRegistrationToken> tokensToRemove = new List<EventRegistrationToken>();
                s_eventCacheRWLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    TokenListCount tokenListCount;
                    ConditionalWeakTable<object, EventRegistrationTokenListWithCount> registrationTokens = GetEventRegistrationTokenTableNoCreate(instanceKey, removeMethod, out tokenListCount);
                    if (registrationTokens == null)
                    {
                        return;
                    }

                    lock (registrationTokens)
                    {
                        foreach (EventRegistrationTokenListWithCount tokens in registrationTokens.Values)
                        {
                            tokens.CopyTo(tokensToRemove);
                        }

                        registrationTokens.Clear();
                        BCLDebug.Log("INTEROP", "[WinRT_Eventing] Cache cleared for managed instance = " + instanceKey + "\n");
                    }
                }
                finally
                {
                    s_eventCacheRWLock.ReleaseReaderLock();
                }

                BCLDebug.Log("INTEROP", "[WinRT_Eventing] Start removing all events for instance = " + instanceKey + "\n");
                CallRemoveMethods(removeMethod, tokensToRemove);
                BCLDebug.Log("INTEROP", "[WinRT_Eventing] Finished removing all events for instance = " + instanceKey + "\n");
            }

            internal class ReaderWriterLockTimedOutException : ApplicationException
            {
            }

            internal class MyReaderWriterLock
            {
                int myLock;
                int owners;
                uint numWriteWaiters;
                uint numReadWaiters;
                EventWaitHandle writeEvent;
                EventWaitHandle readEvent;
                internal MyReaderWriterLock()
                {
                }

                internal void AcquireReaderLock(int millisecondsTimeout)
                {
                    EnterMyLock();
                    for (;;)
                    {
                        if (owners >= 0 && numWriteWaiters == 0)
                        {
                            owners++;
                            break;
                        }

                        if (readEvent == null)
                        {
                            LazyCreateEvent(ref readEvent, false);
                            continue;
                        }

                        WaitOnEvent(readEvent, ref numReadWaiters, millisecondsTimeout);
                    }

                    ExitMyLock();
                }

                internal void AcquireWriterLock(int millisecondsTimeout)
                {
                    EnterMyLock();
                    for (;;)
                    {
                        if (owners == 0)
                        {
                            owners = -1;
                            break;
                        }

                        if (writeEvent == null)
                        {
                            LazyCreateEvent(ref writeEvent, true);
                            continue;
                        }

                        WaitOnEvent(writeEvent, ref numWriteWaiters, millisecondsTimeout);
                    }

                    ExitMyLock();
                }

                internal void ReleaseReaderLock()
                {
                    EnterMyLock();
                    Contract.Assert(owners > 0, "ReleasingReaderLock: releasing lock and no read lock taken");
                    --owners;
                    ExitAndWakeUpAppropriateWaiters();
                }

                internal void ReleaseWriterLock()
                {
                    EnterMyLock();
                    Contract.Assert(owners == -1, "Calling ReleaseWriterLock when no write lock is held");
                    owners++;
                    ExitAndWakeUpAppropriateWaiters();
                }

                private void LazyCreateEvent(ref EventWaitHandle waitEvent, bool makeAutoResetEvent)
                {
                    Contract.Assert(myLock != 0, "Lock must be held");
                    Contract.Assert(waitEvent == null, "Wait event must be null");
                    ExitMyLock();
                    EventWaitHandle newEvent;
                    if (makeAutoResetEvent)
                        newEvent = new AutoResetEvent(false);
                    else
                        newEvent = new ManualResetEvent(false);
                    EnterMyLock();
                    if (waitEvent == null)
                        waitEvent = newEvent;
                }

                private void WaitOnEvent(EventWaitHandle waitEvent, ref uint numWaiters, int millisecondsTimeout)
                {
                    Contract.Assert(myLock != 0, "Lock must be held");
                    waitEvent.Reset();
                    numWaiters++;
                    bool waitSuccessful = false;
                    ExitMyLock();
                    try
                    {
                        if (!waitEvent.WaitOne(millisecondsTimeout, false))
                            throw new ReaderWriterLockTimedOutException();
                        waitSuccessful = true;
                    }
                    finally
                    {
                        EnterMyLock();
                        --numWaiters;
                        if (!waitSuccessful)
                            ExitMyLock();
                    }
                }

                private void ExitAndWakeUpAppropriateWaiters()
                {
                    Contract.Assert(myLock != 0, "Lock must be held");
                    if (owners == 0 && numWriteWaiters > 0)
                    {
                        ExitMyLock();
                        writeEvent.Set();
                    }
                    else if (owners >= 0 && numReadWaiters != 0)
                    {
                        ExitMyLock();
                        readEvent.Set();
                    }
                    else
                        ExitMyLock();
                }

                private void EnterMyLock()
                {
                    if (Interlocked.CompareExchange(ref myLock, 1, 0) != 0)
                        EnterMyLockSpin();
                }

                private void EnterMyLockSpin()
                {
                    for (int i = 0;; i++)
                    {
                        if (i < 3 && Environment.ProcessorCount > 1)
                            Thread.SpinWait(20);
                        else
                            Thread.Sleep(0);
                        if (Interlocked.CompareExchange(ref myLock, 1, 0) == 0)
                            return;
                    }
                }

                private void ExitMyLock()
                {
                    Contract.Assert(myLock != 0, "Exiting spin lock that is not held");
                    myLock = 0;
                }
            }

            ;
        }

        internal static void CallRemoveMethods(Action<EventRegistrationToken> removeMethod, List<EventRegistrationToken> tokensToRemove)
        {
            List<Exception> exceptions = new List<Exception>();
            foreach (EventRegistrationToken token in tokensToRemove)
            {
                try
                {
                    removeMethod(token);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                BCLDebug.Log("INTEROP", "[WinRT_Eventing] Event unsubscribed for token = " + token.m_value + "\n");
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions.ToArray());
        }

        internal static unsafe string HStringToString(IntPtr hstring)
        {
            Contract.Requires(Environment.IsWinRTSupported);
            if (hstring == IntPtr.Zero)
            {
                return String.Empty;
            }

            unsafe
            {
                uint length;
                char *rawBuffer = UnsafeNativeMethods.WindowsGetStringRawBuffer(hstring, &length);
                return new String(rawBuffer, 0, checked ((int)length));
            }
        }

        internal static Exception GetExceptionForHR(int hresult, Exception innerException, string messageResource)
        {
            Exception e = null;
            if (innerException != null)
            {
                string message = innerException.Message;
                if (message == null && messageResource != null)
                {
                    message = Environment.GetResourceString(messageResource);
                }

                e = new Exception(message, innerException);
            }
            else
            {
                string message = (messageResource != null ? Environment.GetResourceString(messageResource) : null);
                e = new Exception(message);
            }

            e.SetErrorCode(hresult);
            return e;
        }

        internal static Exception GetExceptionForHR(int hresult, Exception innerException)
        {
            return GetExceptionForHR(hresult, innerException, null);
        }

        private static bool s_haveBlueErrorApis = true;
        private static bool RoOriginateLanguageException(int error, string message, IntPtr languageException)
        {
            if (s_haveBlueErrorApis)
            {
                try
                {
                    return UnsafeNativeMethods.RoOriginateLanguageException(error, message, languageException);
                }
                catch (EntryPointNotFoundException)
                {
                    s_haveBlueErrorApis = false;
                }
            }

            return false;
        }

        private static void RoReportUnhandledError(IRestrictedErrorInfo error)
        {
            if (s_haveBlueErrorApis)
            {
                try
                {
                    UnsafeNativeMethods.RoReportUnhandledError(error);
                }
                catch (EntryPointNotFoundException)
                {
                    s_haveBlueErrorApis = false;
                }
            }
        }

        private static Guid s_iidIErrorInfo = new Guid(0x1CF2B120, 0x547D, 0x101B, 0x8E, 0x65, 0x08, 0x00, 0x2B, 0x2B, 0xD1, 0x19);
        internal static bool ReportUnhandledError(Exception e)
        {
            if (!AppDomain.IsAppXModel())
            {
                return false;
            }

            if (!s_haveBlueErrorApis)
            {
                return false;
            }

            if (e != null)
            {
                IntPtr exceptionIUnknown = IntPtr.Zero;
                IntPtr exceptionIErrorInfo = IntPtr.Zero;
                try
                {
                    exceptionIUnknown = Marshal.GetIUnknownForObject(e);
                    if (exceptionIUnknown != IntPtr.Zero)
                    {
                        Marshal.QueryInterface(exceptionIUnknown, ref s_iidIErrorInfo, out exceptionIErrorInfo);
                        if (exceptionIErrorInfo != IntPtr.Zero)
                        {
                            if (RoOriginateLanguageException(Marshal.GetHRForException_WinRT(e), e.Message, exceptionIErrorInfo))
                            {
                                IRestrictedErrorInfo restrictedError = UnsafeNativeMethods.GetRestrictedErrorInfo();
                                if (restrictedError != null)
                                {
                                    RoReportUnhandledError(restrictedError);
                                    return true;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (exceptionIErrorInfo != IntPtr.Zero)
                    {
                        Marshal.Release(exceptionIErrorInfo);
                    }

                    if (exceptionIUnknown != IntPtr.Zero)
                    {
                        Marshal.Release(exceptionIUnknown);
                    }
                }
            }

            return false;
        }

        internal static IntPtr GetActivationFactoryForType(Type type)
        {
            ManagedActivationFactory activationFactory = GetManagedActivationFactory(type);
            return Marshal.GetComInterfaceForObject(activationFactory, typeof (IActivationFactory));
        }

        internal static ManagedActivationFactory GetManagedActivationFactory(Type type)
        {
            ManagedActivationFactory activationFactory = new ManagedActivationFactory(type);
            Marshal.InitializeManagedWinRTFactoryObject(activationFactory, (RuntimeType)type);
            return activationFactory;
        }

        public static IActivationFactory GetActivationFactory(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (type.IsWindowsRuntimeObject && type.IsImport)
            {
                return (IActivationFactory)Marshal.GetNativeActivationFactory(type);
            }
            else
            {
                return GetManagedActivationFactory(type);
            }
        }

        public static IntPtr StringToHString(String s)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            if (s == null)
                throw new ArgumentNullException("s");
            unsafe
            {
                IntPtr hstring;
                int hrCreate = UnsafeNativeMethods.WindowsCreateString(s, s.Length, &hstring);
                Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
                return hstring;
            }
        }

        public static String PtrToStringHString(IntPtr ptr)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            return HStringToString(ptr);
        }

        public static void FreeHString(IntPtr ptr)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            if (ptr != IntPtr.Zero)
            {
                UnsafeNativeMethods.WindowsDeleteString(ptr);
            }
        }
    }
}