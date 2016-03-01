using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Semaphore : WaitHandle
    {
        public Semaphore(int initialCount, int maximumCount): this (initialCount, maximumCount, null)
        {
        }

        public Semaphore(int initialCount, int maximumCount, string name)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            }

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_SemaphoreInitialMaximum"));
            }

            SafeWaitHandle myHandle = CreateSemaphone(initialCount, maximumCount, name);
            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                __Error.WinIOError();
            }

            this.SafeWaitHandle = myHandle;
        }

        public Semaphore(int initialCount, int maximumCount, string name, out bool createdNew)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (maximumCount < 1)
            {
                throw new ArgumentOutOfRangeException("maximumCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (initialCount > maximumCount)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_SemaphoreInitialMaximum"));
            }

            SafeWaitHandle myHandle = CreateSemaphone(initialCount, maximumCount, name);
            int errorCode = Marshal.GetLastWin32Error();
            if (myHandle.IsInvalid)
            {
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                __Error.WinIOError();
            }

            createdNew = errorCode != Win32Native.ERROR_ALREADY_EXISTS;
            this.SafeWaitHandle = myHandle;
        }

        private Semaphore(SafeWaitHandle handle)
        {
            this.SafeWaitHandle = handle;
        }

        private static SafeWaitHandle CreateSemaphone(int initialCount, int maximumCount, string name)
        {
            if (name != null)
            {
                if (name.Length > Path.MAX_PATH)
                    throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
            }

            Contract.Assert(initialCount >= 0);
            Contract.Assert(maximumCount >= 1);
            Contract.Assert(initialCount <= maximumCount);
            return Win32Native.CreateSemaphore(null, initialCount, maximumCount, name);
        }

        public static Semaphore OpenExisting(string name)
        {
            Semaphore result;
            switch (OpenExistingWorker(name, out result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                case OpenExistingResult.PathNotFound:
                    throw new IOException(Win32Native.GetMessage(Win32Native.ERROR_PATH_NOT_FOUND));
                default:
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out Semaphore result)
        {
            return OpenExistingWorker(name, out result) == OpenExistingResult.Success;
        }

        private static OpenExistingResult OpenExistingWorker(string name, out Semaphore result)
        {
            if (name == null)
                throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name.Length > Path.MAX_PATH)
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
            const int SYNCHRONIZE = 0x00100000;
            const int SEMAPHORE_MODIFY_STATE = 0x00000002;
            SafeWaitHandle myHandle = Win32Native.OpenSemaphore(SEMAPHORE_MODIFY_STATE | SYNCHRONIZE, false, name);
            if (myHandle.IsInvalid)
            {
                result = null;
                int errorCode = Marshal.GetLastWin32Error();
                if (Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Win32Native.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;
                __Error.WinIOError();
            }

            result = new Semaphore(myHandle);
            return OpenExistingResult.Success;
        }

        public int Release()
        {
            return Release(1);
        }

        public int Release(int releaseCount)
        {
            if (releaseCount < 1)
            {
                throw new ArgumentOutOfRangeException("releaseCount", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            int previousCount;
            if (!Win32Native.ReleaseSemaphore(SafeWaitHandle, releaseCount, out previousCount))
            {
                throw new SemaphoreFullException();
            }

            return previousCount;
        }
    }
}