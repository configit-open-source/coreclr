namespace System.Threading
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.IO;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Security.Principal;
    using System.Security;
    using System.Diagnostics.Contracts;

    public sealed class Mutex : WaitHandle
    {
        static bool dummyBool;
        public class MutexSecurity
        {
        }

        public Mutex(bool initiallyOwned, String name, out bool createdNew): this (initiallyOwned, name, out createdNew, (MutexSecurity)null)
        {
        }

        public unsafe Mutex(bool initiallyOwned, String name, out bool createdNew, MutexSecurity mutexSecurity)
        {
            if (name != null)
            {
                if (System.IO.Path.MaxPath < name.Length)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
                }
            }

            Contract.EndContractBlock();
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
            CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, secAttrs);
        }

        internal Mutex(bool initiallyOwned, String name, out bool createdNew, Win32Native.SECURITY_ATTRIBUTES secAttrs)
        {
            if (name != null)
            {
                if (System.IO.Path.MaxPath < name.Length)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
                }
            }

            Contract.EndContractBlock();
            CreateMutexWithGuaranteedCleanup(initiallyOwned, name, out createdNew, secAttrs);
        }

        static string WinCEObjectNameQuirk(string name)
        {
            if (name == null)
                return null;
            return name.Replace('\\', '\u2044');
        }

        internal void CreateMutexWithGuaranteedCleanup(bool initiallyOwned, String name, out bool createdNew, Win32Native.SECURITY_ATTRIBUTES secAttrs)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                name = WinCEObjectNameQuirk(name);
            RuntimeHelpers.CleanupCode cleanupCode = new RuntimeHelpers.CleanupCode(MutexCleanupCode);
            MutexCleanupInfo cleanupInfo = new MutexCleanupInfo(null, false);
            MutexTryCodeHelper tryCodeHelper = new MutexTryCodeHelper(initiallyOwned, cleanupInfo, name, secAttrs, this);
            RuntimeHelpers.TryCode tryCode = new RuntimeHelpers.TryCode(tryCodeHelper.MutexTryCode);
            RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(tryCode, cleanupCode, cleanupInfo);
            createdNew = tryCodeHelper.m_newMutex;
        }

        internal class MutexTryCodeHelper
        {
            bool m_initiallyOwned;
            MutexCleanupInfo m_cleanupInfo;
            internal bool m_newMutex;
            String m_name;
            Win32Native.SECURITY_ATTRIBUTES m_secAttrs;
            Mutex m_mutex;
            internal MutexTryCodeHelper(bool initiallyOwned, MutexCleanupInfo cleanupInfo, String name, Win32Native.SECURITY_ATTRIBUTES secAttrs, Mutex mutex)
            {
                m_initiallyOwned = initiallyOwned;
                m_cleanupInfo = cleanupInfo;
                m_name = name;
                m_secAttrs = secAttrs;
                m_mutex = mutex;
            }

            internal void MutexTryCode(object userData)
            {
                SafeWaitHandle mutexHandle = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    if (m_initiallyOwned)
                    {
                        m_cleanupInfo.inCriticalRegion = true;
                    }
                }

                int errorCode = 0;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    errorCode = CreateMutexHandle(m_initiallyOwned, m_name, m_secAttrs, out mutexHandle);
                }

                if (mutexHandle.IsInvalid)
                {
                    mutexHandle.SetHandleAsInvalid();
                    if (null != m_name && 0 != m_name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                        throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", m_name));
                    __Error.WinIOError(errorCode, m_name);
                }

                m_newMutex = errorCode != Win32Native.ERROR_ALREADY_EXISTS;
                m_mutex.SetHandleInternal(mutexHandle);
                m_mutex.hasThreadAffinity = true;
            }
        }

        private void MutexCleanupCode(Object userData, bool exceptionThrown)
        {
            MutexCleanupInfo cleanupInfo = (MutexCleanupInfo)userData;
            if (!hasThreadAffinity)
            {
                if (cleanupInfo.mutexHandle != null && !cleanupInfo.mutexHandle.IsInvalid)
                {
                    if (cleanupInfo.inCriticalRegion)
                    {
                        Win32Native.ReleaseMutex(cleanupInfo.mutexHandle);
                    }

                    cleanupInfo.mutexHandle.Dispose();
                }

                if (cleanupInfo.inCriticalRegion)
                {
                }
            }
        }

        internal class MutexCleanupInfo
        {
            internal SafeWaitHandle mutexHandle;
            internal bool inCriticalRegion;
            internal MutexCleanupInfo(SafeWaitHandle mutexHandle, bool inCriticalRegion)
            {
                this.mutexHandle = mutexHandle;
                this.inCriticalRegion = inCriticalRegion;
            }
        }

        public Mutex(bool initiallyOwned, String name): this (initiallyOwned, name, out dummyBool)
        {
        }

        public Mutex(bool initiallyOwned): this (initiallyOwned, null, out dummyBool)
        {
        }

        public Mutex(): this (false, null, out dummyBool)
        {
        }

        private Mutex(SafeWaitHandle handle)
        {
            SetHandleInternal(handle);
            hasThreadAffinity = true;
        }

        public static Mutex OpenExisting(string name)
        {
            return OpenExisting(name, (MutexRights)0);
        }

        public enum MutexRights
        {
        }

        public static Mutex OpenExisting(string name, MutexRights rights)
        {
            Mutex result;
            switch (OpenExistingWorker(name, rights, out result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                case OpenExistingResult.PathNotFound:
                    __Error.WinIOError(Win32Native.ERROR_PATH_NOT_FOUND, name);
                    return result;
                default:
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out Mutex result)
        {
            return OpenExistingWorker(name, (MutexRights)0, out result) == OpenExistingResult.Success;
        }

        public static bool TryOpenExisting(string name, MutexRights rights, out Mutex result)
        {
            return OpenExistingWorker(name, rights, out result) == OpenExistingResult.Success;
        }

        private static OpenExistingResult OpenExistingWorker(string name, MutexRights rights, out Mutex result)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            }

            if (System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
            }

            Contract.EndContractBlock();
            result = null;
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                name = WinCEObjectNameQuirk(name);
            SafeWaitHandle myHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
            int errorCode = 0;
            if (myHandle.IsInvalid)
            {
                errorCode = Marshal.GetLastWin32Error();
                if (Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Win32Native.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;
                __Error.WinIOError(errorCode, name);
            }

            result = new Mutex(myHandle);
            return OpenExistingResult.Success;
        }

        public void ReleaseMutex()
        {
            if (Win32Native.ReleaseMutex(safeWaitHandle))
            {
            }
            else
            {
                throw new Exception(Environment.GetResourceString("Arg_SynchronizationLockException"));
            }
        }

        static int CreateMutexHandle(bool initiallyOwned, String name, Win32Native.SECURITY_ATTRIBUTES securityAttribute, out SafeWaitHandle mutexHandle)
        {
            int errorCode;
            bool fAffinity = false;
            while (true)
            {
                mutexHandle = Win32Native.CreateMutex(securityAttribute, initiallyOwned, name);
                errorCode = Marshal.GetLastWin32Error();
                if (!mutexHandle.IsInvalid)
                {
                    break;
                }

                if (errorCode == Win32Native.ERROR_ACCESS_DENIED)
                {
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        try
                        {
                        }
                        finally
                        {
                            fAffinity = true;
                        }

                        mutexHandle = Win32Native.OpenMutex(Win32Native.MUTEX_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
                        if (!mutexHandle.IsInvalid)
                        {
                            errorCode = Win32Native.ERROR_ALREADY_EXISTS;
                        }
                        else
                        {
                            errorCode = Marshal.GetLastWin32Error();
                        }
                    }
                    finally
                    {
                        if (fAffinity)
                        {
                        }
                    }

                    if (errorCode != Win32Native.ERROR_FILE_NOT_FOUND)
                    {
                        if (errorCode == Win32Native.ERROR_SUCCESS)
                        {
                            errorCode = Win32Native.ERROR_ALREADY_EXISTS;
                        }

                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return errorCode;
        }
    }
}