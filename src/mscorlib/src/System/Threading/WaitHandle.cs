
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public abstract class WaitHandle : IDisposable
    {
        public const int WaitTimeout = 0x102;
        private const int MAX_WAITHANDLES = 64;
        private IntPtr waitHandle;
        internal volatile SafeWaitHandle safeWaitHandle;
        internal bool hasThreadAffinity;
        private static IntPtr GetInvalidHandle()
        {
            return Win32Native.INVALID_HANDLE_VALUE;
        }

        protected static readonly IntPtr InvalidHandle = GetInvalidHandle();
        private const int WAIT_OBJECT_0 = 0;
        private const int WAIT_ABANDONED = 0x80;
        private const int WAIT_FAILED = 0x7FFFFFFF;
        private const int ERROR_TOO_MANY_POSTS = 0x12A;
        internal enum OpenExistingResult
        {
            Success,
            NameNotFound,
            PathNotFound,
            NameInvalid
        }

        protected WaitHandle()
        {
            Init();
        }

        private void Init()
        {
            safeWaitHandle = null;
            waitHandle = InvalidHandle;
            hasThreadAffinity = false;
        }

        public virtual IntPtr Handle
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return safeWaitHandle == null ? InvalidHandle : safeWaitHandle.DangerousGetHandle();
            }

            [System.Security.SecurityCritical]
            set
            {
                if (value == InvalidHandle)
                {
                    if (safeWaitHandle != null)
                    {
                        safeWaitHandle.SetHandleAsInvalid();
                        safeWaitHandle = null;
                    }
                }
                else
                {
                    safeWaitHandle = new SafeWaitHandle(value, true);
                }

                waitHandle = value;
            }
        }

        public SafeWaitHandle SafeWaitHandle
        {
            [System.Security.SecurityCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            get
            {
                if (safeWaitHandle == null)
                {
                    safeWaitHandle = new SafeWaitHandle(InvalidHandle, false);
                }

                return safeWaitHandle;
            }

            [System.Security.SecurityCritical]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            set
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    if (value == null)
                    {
                        safeWaitHandle = null;
                        waitHandle = InvalidHandle;
                    }
                    else
                    {
                        safeWaitHandle = value;
                        waitHandle = safeWaitHandle.DangerousGetHandle();
                    }
                }
            }
        }

        internal void SetHandleInternal(SafeWaitHandle handle)
        {
            safeWaitHandle = handle;
            waitHandle = handle.DangerousGetHandle();
        }

        public virtual bool WaitOne(int millisecondsTimeout, bool exitContext)
        {
            if (millisecondsTimeout < -1)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }

                        return WaitOne((long)millisecondsTimeout, exitContext);
        }

        public virtual bool WaitOne(TimeSpan timeout, bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long)Int32.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }

            return WaitOne(tm, exitContext);
        }

        public virtual bool WaitOne()
        {
            return WaitOne(-1, false);
        }

        public virtual bool WaitOne(int millisecondsTimeout)
        {
            return WaitOne(millisecondsTimeout, false);
        }

        public virtual bool WaitOne(TimeSpan timeout)
        {
            return WaitOne(timeout, false);
        }

        private bool WaitOne(long timeout, bool exitContext)
        {
            return InternalWaitOne(safeWaitHandle, timeout, hasThreadAffinity, exitContext);
        }

        internal static bool InternalWaitOne(SafeHandle waitableSafeHandle, long millisecondsTimeout, bool hasThreadAffinity, bool exitContext)
        {
            if (waitableSafeHandle == null)
            {
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
            }

                        int ret = WaitOneNative(waitableSafeHandle, (uint)millisecondsTimeout, hasThreadAffinity, exitContext);
            if (AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
            if (ret == WAIT_ABANDONED)
            {
                ThrowAbandonedMutexException();
            }

            return (ret != WaitTimeout);
        }

        internal bool WaitOneWithoutFAS()
        {
            if (safeWaitHandle == null)
            {
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));
            }

                        long timeout = -1;
            int ret = WaitOneNative(safeWaitHandle, (uint)timeout, hasThreadAffinity, false);
            if (ret == WAIT_ABANDONED)
            {
                ThrowAbandonedMutexException();
            }

            return (ret != WaitTimeout);
        }

        private static extern int WaitOneNative(SafeHandle waitableSafeHandle, uint millisecondsTimeout, bool hasThreadAffinity, bool exitContext);
        private static extern int WaitMultiple(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext, bool WaitAll);
        public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            if (waitHandles == null)
            {
                throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_Waithandles"));
            }

            if (waitHandles.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyWaithandleArray"));
            }

            if (waitHandles.Length > MAX_WAITHANDLES)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_MaxWaitHandles"));
            }

            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }

                        WaitHandle[] internalWaitHandles = new WaitHandle[waitHandles.Length];
            for (int i = 0; i < waitHandles.Length; i++)
            {
                WaitHandle waitHandle = waitHandles[i];
                if (waitHandle == null)
                    throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_ArrayElement"));
                internalWaitHandles[i] = waitHandle;
            }

            waitHandles = null;
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                return true;
            int ret = WaitMultiple(internalWaitHandles, millisecondsTimeout, exitContext, true);
            if (AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
            if ((WAIT_ABANDONED <= ret) && (WAIT_ABANDONED + internalWaitHandles.Length > ret))
            {
                ThrowAbandonedMutexException();
            }

            GC.KeepAlive(internalWaitHandles);
            return (ret != WaitTimeout);
        }

        public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long)Int32.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }

            return WaitAll(waitHandles, (int)tm, exitContext);
        }

        public static bool WaitAll(WaitHandle[] waitHandles)
        {
            return WaitAll(waitHandles, Timeout.Infinite, true);
        }

        public static bool WaitAll(WaitHandle[] waitHandles, int millisecondsTimeout)
        {
            return WaitAll(waitHandles, millisecondsTimeout, true);
        }

        public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            return WaitAll(waitHandles, timeout, true);
        }

        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout, bool exitContext)
        {
            if (waitHandles == null)
            {
                throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_Waithandles"));
            }

            if (waitHandles.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyWaithandleArray"));
            }

            if (MAX_WAITHANDLES < waitHandles.Length)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_MaxWaitHandles"));
            }

            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }

                        WaitHandle[] internalWaitHandles = new WaitHandle[waitHandles.Length];
            for (int i = 0; i < waitHandles.Length; i++)
            {
                WaitHandle waitHandle = waitHandles[i];
                if (waitHandle == null)
                    throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_ArrayElement"));
                internalWaitHandles[i] = waitHandle;
            }

            waitHandles = null;
            int ret = WaitMultiple(internalWaitHandles, millisecondsTimeout, exitContext, false);
            if (AppDomainPauseManager.IsPaused)
                AppDomainPauseManager.ResumeEvent.WaitOneWithoutFAS();
            if ((WAIT_ABANDONED <= ret) && (WAIT_ABANDONED + internalWaitHandles.Length > ret))
            {
                int mutexIndex = ret - WAIT_ABANDONED;
                if (0 <= mutexIndex && mutexIndex < internalWaitHandles.Length)
                {
                    ThrowAbandonedMutexException(mutexIndex, internalWaitHandles[mutexIndex]);
                }
                else
                {
                    ThrowAbandonedMutexException();
                }
            }

            GC.KeepAlive(internalWaitHandles);
            return ret;
        }

        public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout, bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long)Int32.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }

            return WaitAny(waitHandles, (int)tm, exitContext);
        }

        public static int WaitAny(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            return WaitAny(waitHandles, timeout, true);
        }

        public static int WaitAny(WaitHandle[] waitHandles)
        {
            return WaitAny(waitHandles, Timeout.Infinite, true);
        }

        public static int WaitAny(WaitHandle[] waitHandles, int millisecondsTimeout)
        {
            return WaitAny(waitHandles, millisecondsTimeout, true);
        }

        private static extern int SignalAndWaitOne(SafeWaitHandle waitHandleToSignal, SafeWaitHandle waitHandleToWaitOn, int millisecondsTimeout, bool hasThreadAffinity, bool exitContext);
        public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn)
        {
            return SignalAndWait(toSignal, toWaitOn, -1, false);
        }

        public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, TimeSpan timeout, bool exitContext)
        {
            long tm = (long)timeout.TotalMilliseconds;
            if (-1 > tm || (long)Int32.MaxValue < tm)
            {
                throw new ArgumentOutOfRangeException("timeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }

            return SignalAndWait(toSignal, toWaitOn, (int)tm, exitContext);
        }

        public static bool SignalAndWait(WaitHandle toSignal, WaitHandle toWaitOn, int millisecondsTimeout, bool exitContext)
        {
            if (null == toSignal)
            {
                throw new ArgumentNullException("toSignal");
            }

            if (null == toWaitOn)
            {
                throw new ArgumentNullException("toWaitOn");
            }

            if (-1 > millisecondsTimeout)
            {
                throw new ArgumentOutOfRangeException("millisecondsTimeout", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1"));
            }

                        int ret = SignalAndWaitOne(toSignal.safeWaitHandle, toWaitOn.safeWaitHandle, millisecondsTimeout, toWaitOn.hasThreadAffinity, exitContext);
            if (WAIT_ABANDONED == ret)
            {
                ThrowAbandonedMutexException();
            }

            if (ERROR_TOO_MANY_POSTS == ret)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Threading.WaitHandleTooManyPosts"));
            }

            if (WAIT_OBJECT_0 == ret)
            {
                return true;
            }

            return false;
        }

        private static void ThrowAbandonedMutexException()
        {
            throw new AbandonedMutexException();
        }

        private static void ThrowAbandonedMutexException(int location, WaitHandle handle)
        {
            throw new AbandonedMutexException(location, handle);
        }

        public virtual void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool explicitDisposing)
        {
            if (safeWaitHandle != null)
            {
                safeWaitHandle.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}