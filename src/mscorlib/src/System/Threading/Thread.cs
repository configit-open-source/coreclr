using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;

namespace System.Threading
{
    internal delegate Object InternalCrossContextDelegate(Object[] args);
    internal class ThreadHelper
    {
        static ThreadHelper()
        {
        }

        Delegate _start;
        Object _startArg = null;
        ExecutionContext _executionContext = null;
        internal ThreadHelper(Delegate start)
        {
            _start = start;
        }

        internal void SetExecutionContextHelper(ExecutionContext ec)
        {
            _executionContext = ec;
        }

        static internal ContextCallback _ccb = new ContextCallback(ThreadStart_Context);
        static private void ThreadStart_Context(Object state)
        {
            ThreadHelper t = (ThreadHelper)state;
            if (t._start is ThreadStart)
            {
                ((ThreadStart)t._start)();
            }
            else
            {
                ((ParameterizedThreadStart)t._start)(t._startArg);
            }
        }

        internal void ThreadStart(object obj)
        {
            _startArg = obj;
            if (_executionContext != null)
            {
                ExecutionContext.Run(_executionContext, _ccb, (Object)this);
            }
            else
            {
                ((ParameterizedThreadStart)_start)(obj);
            }
        }

        internal void ThreadStart()
        {
            if (_executionContext != null)
            {
                ExecutionContext.Run(_executionContext, _ccb, (Object)this);
            }
            else
            {
                ((ThreadStart)_start)();
            }
        }
    }

    internal struct ThreadHandle
    {
        private IntPtr m_ptr;
        internal ThreadHandle(IntPtr pThread)
        {
            m_ptr = pThread;
        }
    }

    public sealed class Thread : CriticalFinalizerObject, _Thread
    {
        private String m_Name;
        private Delegate m_Delegate;
        private Object m_ThreadStartArg;
        private IntPtr DONT_USE_InternalThread;
        private int m_Priority;
        private int m_ManagedThreadId;
        private bool m_ExecutionContextBelongsToOuterScope;
        private bool m_ForbidExecutionContextMutation;
        static private LocalDataStoreMgr s_LocalDataStoreMgr;
        static private LocalDataStoreHolder s_LocalDataStore;
        internal static CultureInfo m_CurrentCulture;
        internal static CultureInfo m_CurrentUICulture;
        static AsyncLocal<CultureInfo> s_asyncLocalCurrentCulture;
        static AsyncLocal<CultureInfo> s_asyncLocalCurrentUICulture;
        static void AsyncLocalSetCurrentCulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            m_CurrentCulture = args.CurrentValue;
        }

        static void AsyncLocalSetCurrentUICulture(AsyncLocalValueChangedArgs<CultureInfo> args)
        {
            m_CurrentUICulture = args.CurrentValue;
        }

        internal Thread()
        {
        }

        public Thread(ThreadStart start)
        {
            if (start == null)
            {
                throw new ArgumentNullException("start");
            }

                        SetStartHelper((Delegate)start, 0);
        }

        public Thread(ThreadStart start, int maxStackSize)
        {
            if (start == null)
            {
                throw new ArgumentNullException("start");
            }

            if (0 > maxStackSize)
                throw new ArgumentOutOfRangeException("maxStackSize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        SetStartHelper((Delegate)start, maxStackSize);
        }

        public Thread(ParameterizedThreadStart start)
        {
            if (start == null)
            {
                throw new ArgumentNullException("start");
            }

                        SetStartHelper((Delegate)start, 0);
        }

        public Thread(ParameterizedThreadStart start, int maxStackSize)
        {
            if (start == null)
            {
                throw new ArgumentNullException("start");
            }

            if (0 > maxStackSize)
                throw new ArgumentOutOfRangeException("maxStackSize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        SetStartHelper((Delegate)start, maxStackSize);
        }

        public override int GetHashCode()
        {
            return m_ManagedThreadId;
        }

        extern public int ManagedThreadId
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [MethodImpl(MethodImplOptions.InternalCall)]
            [System.Security.SecuritySafeCritical]
            get;
        }

        internal unsafe ThreadHandle GetNativeHandle()
        {
            IntPtr thread = DONT_USE_InternalThread;
            if (thread.IsNull())
                throw new ArgumentException(null, Environment.GetResourceString("Argument_InvalidHandle"));
            return new ThreadHandle(thread);
        }

        public void Start()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Start(ref stackMark);
        }

        public void Start(object parameter)
        {
            if (m_Delegate is ThreadStart)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ThreadWrongThreadStart"));
            }

            m_ThreadStartArg = parameter;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Start(ref stackMark);
        }

        private void Start(ref StackCrawlMark stackMark)
        {
            StartupSetApartmentStateInternal();
            if (m_Delegate != null)
            {
                ThreadHelper t = (ThreadHelper)(m_Delegate.Target);
                ExecutionContext ec = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.IgnoreSyncCtx);
                t.SetExecutionContextHelper(ec);
            }

            IPrincipal principal = null;
            StartInternal(principal, ref stackMark);
        }

        private extern void StartInternal(IPrincipal principal, ref StackCrawlMark stackMark);
        internal extern static IntPtr InternalGetCurrentThread();
        public void Abort()
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if (callingAssembly != null && !callingAssembly.IsProfileAssembly)
                {
                    string caller = new StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Arg_MethodAccessException_WithCaller"), caller, callee));
                }
            }

            AbortInternal();
        }

        private extern void AbortInternal();
        public ThreadPriority Priority
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (ThreadPriority)GetPriorityNative();
            }

            [System.Security.SecuritySafeCritical]
            [HostProtection(SelfAffectingThreading = true)]
            set
            {
                SetPriorityNative((int)value);
            }
        }

        private extern int GetPriorityNative();
        private extern void SetPriorityNative(int priority);
        public extern bool IsAlive
        {
            [System.Security.SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        public extern bool IsThreadPoolThread
        {
            [System.Security.SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        private extern bool JoinInternal(int millisecondsTimeout);
        public void Join()
        {
            JoinInternal(Timeout.Infinite);
        }

        public bool Join(int millisecondsTimeout)
        {
            return JoinInternal(millisecondsTimeout);
        }

        public bool Join(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long)Int32.MaxValue)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            return Join((int)tm);
        }

        private static extern void SleepInternal(int millisecondsTimeout);
        public static void Sleep(int millisecondsTimeout)
        {
            SleepInternal(millisecondsTimeout);
            if (AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
        }

        public static void Sleep(TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (tm < -1 || tm > (long)Int32.MaxValue)
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            Sleep((int)tm);
        }

        private static extern void SpinWaitInternal(int iterations);
        public static void SpinWait(int iterations)
        {
            SpinWaitInternal(iterations);
        }

        private static extern bool YieldInternal();
        public static bool Yield()
        {
            return YieldInternal();
        }

        public static Thread CurrentThread
        {
            [System.Security.SecuritySafeCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get
            {
                                return GetCurrentThreadNative();
            }
        }

        private static extern Thread GetCurrentThreadNative();
        private void SetStartHelper(Delegate start, int maxStackSize)
        {
                        ThreadHelper threadStartCallBack = new ThreadHelper(start);
            if (start is ThreadStart)
            {
                SetStart(new ThreadStart(threadStartCallBack.ThreadStart), maxStackSize);
            }
            else
            {
                SetStart(new ParameterizedThreadStart(threadStartCallBack.ThreadStart), maxStackSize);
            }
        }

        private static extern ulong GetProcessDefaultStackSize();
        private extern void SetStart(Delegate start, int maxStackSize);
        ~Thread()
        {
            InternalFinalize();
        }

        private extern void InternalFinalize();
        public extern void DisableComObjectEagerCleanup();
        public bool IsBackground
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return IsBackgroundNative();
            }

            [System.Security.SecuritySafeCritical]
            [HostProtection(SelfAffectingThreading = true)]
            set
            {
                SetBackgroundNative(value);
            }
        }

        private extern bool IsBackgroundNative();
        private extern void SetBackgroundNative(bool isBackground);
        public ThreadState ThreadState
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (ThreadState)GetThreadStateNative();
            }
        }

        private extern int GetThreadStateNative();
        public ApartmentState ApartmentState
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (ApartmentState)GetApartmentStateNative();
            }

            [System.Security.SecuritySafeCritical]
            [HostProtection(Synchronization = true, SelfAffectingThreading = true)]
            set
            {
                SetApartmentStateNative((int)value, true);
            }
        }

        public ApartmentState GetApartmentState()
        {
            return (ApartmentState)GetApartmentStateNative();
        }

        public bool TrySetApartmentState(ApartmentState state)
        {
            return SetApartmentStateHelper(state, false);
        }

        public void SetApartmentState(ApartmentState state)
        {
            bool result = SetApartmentStateHelper(state, true);
            if (!result)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ApartmentStateSwitchFailed"));
        }

        private bool SetApartmentStateHelper(ApartmentState state, bool fireMDAOnMismatch)
        {
            ApartmentState retState = (ApartmentState)SetApartmentStateNative((int)state, fireMDAOnMismatch);
            if ((state == System.Threading.ApartmentState.Unknown) && (retState == System.Threading.ApartmentState.MTA))
                return true;
            if (retState != state)
                return false;
            return true;
        }

        private extern int GetApartmentStateNative();
        private extern int SetApartmentStateNative(int state, bool fireMDAOnMismatch);
        private extern void StartupSetApartmentStateInternal();
        public static LocalDataStoreSlot AllocateDataSlot()
        {
            return LocalDataStoreManager.AllocateDataSlot();
        }

        public static LocalDataStoreSlot AllocateNamedDataSlot(String name)
        {
            return LocalDataStoreManager.AllocateNamedDataSlot(name);
        }

        public static LocalDataStoreSlot GetNamedDataSlot(String name)
        {
            return LocalDataStoreManager.GetNamedDataSlot(name);
        }

        public static void FreeNamedDataSlot(String name)
        {
            LocalDataStoreManager.FreeNamedDataSlot(name);
        }

        public static Object GetData(LocalDataStoreSlot slot)
        {
            LocalDataStoreHolder dls = s_LocalDataStore;
            if (dls == null)
            {
                LocalDataStoreManager.ValidateSlot(slot);
                return null;
            }

            return dls.Store.GetData(slot);
        }

        public static void SetData(LocalDataStoreSlot slot, Object data)
        {
            LocalDataStoreHolder dls = s_LocalDataStore;
            if (dls == null)
            {
                dls = LocalDataStoreManager.CreateLocalDataStore();
                s_LocalDataStore = dls;
            }

            dls.Store.SetData(slot, data);
        }

        public CultureInfo CurrentUICulture
        {
            get
            {
                                if (AppDomain.IsAppXModel())
                {
                    return CultureInfo.GetCultureInfoForUserPreferredLanguageInAppX() ?? GetCurrentUICultureNoAppX();
                }
                else
                {
                    return GetCurrentUICultureNoAppX();
                }
            }

            [System.Security.SecuritySafeCritical]
            [HostProtection(ExternalThreading = true)]
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                                CultureInfo.VerifyCultureName(value, true);
                if (m_CurrentUICulture == null && m_CurrentCulture == null)
                    nativeInitCultureAccessors();
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    CultureInfo.SetCurrentUICultureQuirk(value);
                    return;
                }

                if (!AppContextSwitches.NoAsyncCurrentCulture)
                {
                    if (s_asyncLocalCurrentUICulture == null)
                    {
                        Interlocked.CompareExchange(ref s_asyncLocalCurrentUICulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentUICulture), null);
                    }

                    s_asyncLocalCurrentUICulture.Value = value;
                }
                else
                {
                    m_CurrentUICulture = value;
                }
            }
        }

        internal CultureInfo GetCurrentUICultureNoAppX()
        {
                        if (m_CurrentUICulture == null)
            {
                CultureInfo appDomainDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
                return (appDomainDefaultUICulture != null ? appDomainDefaultUICulture : CultureInfo.UserDefaultUICulture);
            }

            return m_CurrentUICulture;
        }

        public CultureInfo CurrentCulture
        {
            get
            {
                                if (AppDomain.IsAppXModel())
                {
                    return CultureInfo.GetCultureInfoForUserPreferredLanguageInAppX() ?? GetCurrentCultureNoAppX();
                }
                else
                {
                    return GetCurrentCultureNoAppX();
                }
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }

                                if (m_CurrentCulture == null && m_CurrentUICulture == null)
                    nativeInitCultureAccessors();
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    CultureInfo.SetCurrentCultureQuirk(value);
                    return;
                }

                if (!AppContextSwitches.NoAsyncCurrentCulture)
                {
                    if (s_asyncLocalCurrentCulture == null)
                    {
                        Interlocked.CompareExchange(ref s_asyncLocalCurrentCulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentCulture), null);
                    }

                    s_asyncLocalCurrentCulture.Value = value;
                }
                else
                {
                    m_CurrentCulture = value;
                }
            }
        }

        private CultureInfo GetCurrentCultureNoAppX()
        {
                        if (m_CurrentCulture == null)
            {
                CultureInfo appDomainDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
                return (appDomainDefaultCulture != null ? appDomainDefaultCulture : CultureInfo.UserDefaultCulture);
            }

            return m_CurrentCulture;
        }

        private static extern void nativeInitCultureAccessors();
        private static extern AppDomain GetDomainInternal();
        private static extern AppDomain GetFastDomainInternal();
        public static AppDomain GetDomain()
        {
                        AppDomain ad;
            ad = GetFastDomainInternal();
            if (ad == null)
                ad = GetDomainInternal();
            return ad;
        }

        public static int GetDomainID()
        {
            return GetDomain().GetId();
        }

        public String Name
        {
            get
            {
                return m_Name;
            }

            [System.Security.SecuritySafeCritical]
            [HostProtection(ExternalThreading = true)]
            set
            {
                lock (this)
                {
                    if (m_Name != null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WriteOnce"));
                    m_Name = value;
                    InformThreadNameChange(GetNativeHandle(), value, (value != null) ? value.Length : 0);
                }
            }
        }

        private static extern void InformThreadNameChange(ThreadHandle t, String name, int len);
        internal Object AbortReason
        {
            [System.Security.SecurityCritical]
            get
            {
                object result = null;
                try
                {
                    result = GetAbortReason();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ExceptionStateCrossAppDomain"), e);
                }

                return result;
            }

            [System.Security.SecurityCritical]
            set
            {
                SetAbortReason(value);
            }
        }

        public static byte VolatileRead(ref byte address)
        {
            byte ret = address;
            MemoryBarrier();
            return ret;
        }

        public static short VolatileRead(ref short address)
        {
            short ret = address;
            MemoryBarrier();
            return ret;
        }

        public static int VolatileRead(ref int address)
        {
            int ret = address;
            MemoryBarrier();
            return ret;
        }

        public static long VolatileRead(ref long address)
        {
            long ret = address;
            MemoryBarrier();
            return ret;
        }

        public static sbyte VolatileRead(ref sbyte address)
        {
            sbyte ret = address;
            MemoryBarrier();
            return ret;
        }

        public static ushort VolatileRead(ref ushort address)
        {
            ushort ret = address;
            MemoryBarrier();
            return ret;
        }

        public static uint VolatileRead(ref uint address)
        {
            uint ret = address;
            MemoryBarrier();
            return ret;
        }

        public static IntPtr VolatileRead(ref IntPtr address)
        {
            IntPtr ret = address;
            MemoryBarrier();
            return ret;
        }

        public static UIntPtr VolatileRead(ref UIntPtr address)
        {
            UIntPtr ret = address;
            MemoryBarrier();
            return ret;
        }

        public static ulong VolatileRead(ref ulong address)
        {
            ulong ret = address;
            MemoryBarrier();
            return ret;
        }

        public static float VolatileRead(ref float address)
        {
            float ret = address;
            MemoryBarrier();
            return ret;
        }

        public static double VolatileRead(ref double address)
        {
            double ret = address;
            MemoryBarrier();
            return ret;
        }

        public static Object VolatileRead(ref Object address)
        {
            Object ret = address;
            MemoryBarrier();
            return ret;
        }

        public static void VolatileWrite(ref byte address, byte value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref short address, short value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref int address, int value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref long address, long value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref sbyte address, sbyte value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref ushort address, ushort value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref uint address, uint value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref IntPtr address, IntPtr value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref UIntPtr address, UIntPtr value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref ulong address, ulong value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref float address, float value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref double address, double value)
        {
            MemoryBarrier();
            address = value;
        }

        public static void VolatileWrite(ref Object address, Object value)
        {
            MemoryBarrier();
            address = value;
        }

        public static extern void MemoryBarrier();
        private static LocalDataStoreMgr LocalDataStoreManager
        {
            get
            {
                if (s_LocalDataStoreMgr == null)
                {
                    Interlocked.CompareExchange(ref s_LocalDataStoreMgr, new LocalDataStoreMgr(), null);
                }

                return s_LocalDataStoreMgr;
            }
        }

        internal extern void SetAbortReason(Object o);
        internal extern Object GetAbortReason();
        internal extern void ClearAbortReason();
    }

    internal enum StackCrawlMark
    {
        LookForMe = 0,
        LookForMyCaller = 1,
        LookForMyCallersCaller = 2,
        LookForThread = 3
    }
}