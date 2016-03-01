using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Security.AccessControl
{
    public class EventWaitHandleSecurity
    {
    }

    public enum EventWaitHandleRights
    {
    }
}

namespace System.Threading
{
    public class EventWaitHandle : WaitHandle
    {
        public EventWaitHandle(bool initialState, EventResetMode mode): this (initialState, mode, null)
        {
        }

        public EventWaitHandle(bool initialState, EventResetMode mode, string name)
        {
            if (name != null)
            {
                if (System.IO.Path.MaxPath < name.Length)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
                }
            }

            Contract.EndContractBlock();
            SafeWaitHandle _handle = null;
            switch (mode)
            {
                case EventResetMode.ManualReset:
                    _handle = Win32Native.CreateEvent(null, true, initialState, name);
                    break;
                case EventResetMode.AutoReset:
                    _handle = Win32Native.CreateEvent(null, false, initialState, name);
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag", name));
            }

            ;
            if (_handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                _handle.SetHandleAsInvalid();
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                __Error.WinIOError(errorCode, name);
            }

            SetHandleInternal(_handle);
        }

        public EventWaitHandle(bool initialState, EventResetMode mode, string name, out bool createdNew): this (initialState, mode, name, out createdNew, null)
        {
        }

        public unsafe EventWaitHandle(bool initialState, EventResetMode mode, string name, out bool createdNew, EventWaitHandleSecurity eventSecurity)
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
            SafeWaitHandle _handle = null;
            Boolean isManualReset;
            switch (mode)
            {
                case EventResetMode.ManualReset:
                    isManualReset = true;
                    break;
                case EventResetMode.AutoReset:
                    isManualReset = false;
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag", name));
            }

            ;
            _handle = Win32Native.CreateEvent(secAttrs, isManualReset, initialState, name);
            int errorCode = Marshal.GetLastWin32Error();
            if (_handle.IsInvalid)
            {
                _handle.SetHandleAsInvalid();
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                __Error.WinIOError(errorCode, name);
            }

            createdNew = errorCode != Win32Native.ERROR_ALREADY_EXISTS;
            SetHandleInternal(_handle);
        }

        private EventWaitHandle(SafeWaitHandle handle)
        {
            SetHandleInternal(handle);
        }

        public static EventWaitHandle OpenExisting(string name)
        {
            return OpenExisting(name, (EventWaitHandleRights)0);
        }

        public static EventWaitHandle OpenExisting(string name, EventWaitHandleRights rights)
        {
            EventWaitHandle result;
            switch (OpenExistingWorker(name, rights, out result))
            {
                case OpenExistingResult.NameNotFound:
                    throw new WaitHandleCannotBeOpenedException();
                case OpenExistingResult.NameInvalid:
                    throw new WaitHandleCannotBeOpenedException(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException_InvalidHandle", name));
                case OpenExistingResult.PathNotFound:
                    __Error.WinIOError(Win32Native.ERROR_PATH_NOT_FOUND, "");
                    return result;
                default:
                    return result;
            }
        }

        public static bool TryOpenExisting(string name, out EventWaitHandle result)
        {
            return OpenExistingWorker(name, (EventWaitHandleRights)0, out result) == OpenExistingResult.Success;
        }

        public static bool TryOpenExisting(string name, EventWaitHandleRights rights, out EventWaitHandle result)
        {
            return OpenExistingWorker(name, rights, out result) == OpenExistingResult.Success;
        }

        private static OpenExistingResult OpenExistingWorker(string name, EventWaitHandleRights rights, out EventWaitHandle result)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", Environment.GetResourceString("ArgumentNull_WithParamName"));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            }

            if (null != name && System.IO.Path.MaxPath < name.Length)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WaitHandleNameTooLong", name));
            }

            Contract.EndContractBlock();
            result = null;
            SafeWaitHandle myHandle = Win32Native.OpenEvent(Win32Native.EVENT_MODIFY_STATE | Win32Native.SYNCHRONIZE, false, name);
            if (myHandle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (Win32Native.ERROR_FILE_NOT_FOUND == errorCode || Win32Native.ERROR_INVALID_NAME == errorCode)
                    return OpenExistingResult.NameNotFound;
                if (Win32Native.ERROR_PATH_NOT_FOUND == errorCode)
                    return OpenExistingResult.PathNotFound;
                if (null != name && 0 != name.Length && Win32Native.ERROR_INVALID_HANDLE == errorCode)
                    return OpenExistingResult.NameInvalid;
                __Error.WinIOError(errorCode, "");
            }

            result = new EventWaitHandle(myHandle);
            return OpenExistingResult.Success;
        }

        public bool Reset()
        {
            bool res = Win32Native.ResetEvent(safeWaitHandle);
            if (!res)
                __Error.WinIOError();
            return res;
        }

        public bool Set()
        {
            bool res = Win32Native.SetEvent(safeWaitHandle);
            if (!res)
                __Error.WinIOError();
            return res;
        }
    }
}