
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    unsafe internal sealed class FileStreamAsyncResult : IAsyncResult
    {
        private AsyncCallback _userCallback;
        private Object _userStateObject;
        private ManualResetEvent _waitHandle;
        private SafeFileHandle _handle;
        private NativeOverlapped*_overlapped;
        internal NativeOverlapped*OverLapped
        {
            [SecurityCritical]
            get
            {
                return _overlapped;
            }
        }

        internal bool IsAsync
        {
            [SecuritySafeCritical]
            get
            {
                return _overlapped != null;
            }
        }

        internal int _EndXxxCalled;
        private int _numBytes;
        internal int NumBytes
        {
            get
            {
                return _numBytes;
            }
        }

        private int _errorCode;
        internal int ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

        private int _numBufferedBytes;
        internal int NumBufferedBytes
        {
            get
            {
                return _numBufferedBytes;
            }
        }

        internal int NumBytesRead
        {
            get
            {
                return _numBytes + _numBufferedBytes;
            }
        }

        private bool _isWrite;
        internal bool IsWrite
        {
            get
            {
                return _isWrite;
            }
        }

        private bool _isComplete;
        private bool _completedSynchronously;
        private unsafe static IOCompletionCallback s_IOCallback;
        internal FileStreamAsyncResult(int numBufferedBytes, byte[] bytes, SafeFileHandle handle, AsyncCallback userCallback, Object userStateObject, bool isWrite)
        {
            _userCallback = userCallback;
            _userStateObject = userStateObject;
            _isWrite = isWrite;
            _numBufferedBytes = numBufferedBytes;
            _handle = handle;
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            _waitHandle = waitHandle;
            Overlapped overlapped = new Overlapped(0, 0, IntPtr.Zero, this);
            if (userCallback != null)
            {
                var ioCallback = s_IOCallback;
                if (ioCallback == null)
                    s_IOCallback = ioCallback = new IOCompletionCallback(AsyncFSCallback);
                _overlapped = overlapped.Pack(ioCallback, bytes);
            }
            else
            {
                _overlapped = overlapped.UnsafePack(null, bytes);
            }

                    }

        internal static FileStreamAsyncResult CreateBufferedReadResult(int numBufferedBytes, AsyncCallback userCallback, Object userStateObject, bool isWrite)
        {
            FileStreamAsyncResult asyncResult = new FileStreamAsyncResult(numBufferedBytes, userCallback, userStateObject, isWrite);
            asyncResult.CallUserCallback();
            return asyncResult;
        }

        private FileStreamAsyncResult(int numBufferedBytes, AsyncCallback userCallback, Object userStateObject, bool isWrite)
        {
            _userCallback = userCallback;
            _userStateObject = userStateObject;
            _isWrite = isWrite;
            _numBufferedBytes = numBufferedBytes;
        }

        public Object AsyncState
        {
            get
            {
                return _userStateObject;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return _isComplete;
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_waitHandle == null)
                {
                    ManualResetEvent mre = new ManualResetEvent(false);
                    if (_overlapped != null && _overlapped->EventHandle != IntPtr.Zero)
                    {
                        mre.SafeWaitHandle = new SafeWaitHandle(_overlapped->EventHandle, true);
                    }

                    if (Interlocked.CompareExchange<ManualResetEvent>(ref _waitHandle, mre, null) == null)
                    {
                        if (_isComplete)
                            _waitHandle.Set();
                    }
                    else
                    {
                        mre.Close();
                    }
                }

                return _waitHandle;
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                return _completedSynchronously;
            }
        }

        private void CallUserCallbackWorker()
        {
            _isComplete = true;
            Thread.MemoryBarrier();
            if (_waitHandle != null)
                _waitHandle.Set();
            _userCallback(this);
        }

        internal void CallUserCallback()
        {
            if (_userCallback != null)
            {
                _completedSynchronously = false;
                ThreadPool.QueueUserWorkItem(state => ((FileStreamAsyncResult)state).CallUserCallbackWorker(), this);
            }
            else
            {
                _isComplete = true;
                Thread.MemoryBarrier();
                if (_waitHandle != null)
                    _waitHandle.Set();
            }
        }

        internal void ReleaseNativeResource()
        {
            if (this._overlapped != null)
                Overlapped.Free(_overlapped);
        }

        internal void Wait()
        {
            if (_waitHandle != null)
            {
                try
                {
                    _waitHandle.WaitOne();
                                    }
                finally
                {
                    _waitHandle.Close();
                }
            }
        }

        unsafe private static void AsyncFSCallback(uint errorCode, uint numBytes, NativeOverlapped*pOverlapped)
        {
            BCLDebug.Log(String.Format("AsyncFSCallback called.  errorCode: " + errorCode + "  numBytes: " + numBytes));
            Overlapped overlapped = Overlapped.Unpack(pOverlapped);
            FileStreamAsyncResult asyncResult = (FileStreamAsyncResult)overlapped.AsyncResult;
            asyncResult._numBytes = (int)numBytes;
            if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                FrameworkEventSource.Log.ThreadTransferReceive((long)(asyncResult.OverLapped), 2, string.Empty);
            if (errorCode == FileStream.ERROR_BROKEN_PIPE || errorCode == FileStream.ERROR_NO_DATA)
                errorCode = 0;
            asyncResult._errorCode = (int)errorCode;
            asyncResult._completedSynchronously = false;
            asyncResult._isComplete = true;
            Thread.MemoryBarrier();
            ManualResetEvent wh = asyncResult._waitHandle;
            if (wh != null)
            {
                                bool r = wh.Set();
                                if (!r)
                    __Error.WinIOError();
            }

            AsyncCallback userCallback = asyncResult._userCallback;
            if (userCallback != null)
                userCallback(asyncResult);
        }

        internal void Cancel()
        {
                                    if (IsCompleted)
                return;
            if (_handle.IsInvalid)
                return;
            bool r = Win32Native.CancelIoEx(_handle, _overlapped);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != Win32Native.ERROR_NOT_FOUND)
                    __Error.WinIOError(errorCode, String.Empty);
            }
        }
    }

    public class FileStream : Stream
    {
        internal const int DefaultBufferSize = 4096;
        private static readonly bool _canUseAsync = !CompatibilitySwitches.IsAppEarlierThanWindowsPhone8;
        private byte[] _buffer;
        private String _fileName;
        private bool _isAsync;
        private bool _canRead;
        private bool _canWrite;
        private bool _canSeek;
        private bool _exposedHandle;
        private bool _isPipe;
        private int _readPos;
        private int _readLen;
        private int _writePos;
        private int _bufferSize;
        private SafeFileHandle _handle;
        private long _pos;
        private long _appendStart;
        private static AsyncCallback s_endReadTask;
        private static AsyncCallback s_endWriteTask;
        private static Action<object> s_cancelReadHandler;
        private static Action<object> s_cancelWriteHandler;
        internal FileStream()
        {
        }

        public FileStream(String path, FileMode mode): this (path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.Read, DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false, true)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if (callingAssembly != null && !callingAssembly.IsProfileAssembly)
                {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Arg_MethodAccessException_WithCaller"), caller, callee));
                }
            }
        }

        public FileStream(String path, FileMode mode, FileAccess access): this (path, mode, access, FileShare.Read, DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false, true)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if (callingAssembly != null && !callingAssembly.IsProfileAssembly)
                {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Arg_MethodAccessException_WithCaller"), caller, callee));
                }
            }
        }

        public FileStream(String path, FileMode mode, FileAccess access, FileShare share): this (path, mode, access, share, DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false, true)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if (callingAssembly != null && !callingAssembly.IsProfileAssembly)
                {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Arg_MethodAccessException_WithCaller"), caller, callee));
                }
            }
        }

        public FileStream(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize): this (path, mode, access, share, bufferSize, FileOptions.None, Path.GetFileName(path), false, false, true)
        {
        }

        public FileStream(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options): this (path, mode, access, share, bufferSize, options, Path.GetFileName(path), false)
        {
        }

        public FileStream(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync): this (path, mode, access, share, bufferSize, (useAsync ? FileOptions.Asynchronous : FileOptions.None), Path.GetFileName(path), false)
        {
        }

        internal FileStream(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, String msgPath, bool bFromProxy)
        {
            Win32Native.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);
            Init(path, mode, access, 0, false, share, bufferSize, options, secAttrs, msgPath, bFromProxy, false, false);
        }

        internal FileStream(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, String msgPath, bool bFromProxy, bool useLongPath)
        {
            Win32Native.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);
            Init(path, mode, access, 0, false, share, bufferSize, options, secAttrs, msgPath, bFromProxy, useLongPath, false);
        }

        internal FileStream(String path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, String msgPath, bool bFromProxy, bool useLongPath, bool checkHost)
        {
            Win32Native.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);
            Init(path, mode, access, 0, false, share, bufferSize, options, secAttrs, msgPath, bFromProxy, useLongPath, checkHost);
        }

        private void Init(String path, FileMode mode, FileAccess access, int rights, bool useRights, FileShare share, int bufferSize, FileOptions options, Win32Native.SECURITY_ATTRIBUTES secAttrs, String msgPath, bool bFromProxy, bool useLongPath, bool checkHost)
        {
            if (path == null)
                throw new ArgumentNullException("path", Environment.GetResourceString("ArgumentNull_Path"));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        _fileName = msgPath;
            _exposedHandle = false;
            FileShare tempshare = share & ~FileShare.Inheritable;
            String badArg = null;
            if (mode < FileMode.CreateNew || mode > FileMode.Append)
                badArg = "mode";
            else if (!useRights && (access < FileAccess.Read || access > FileAccess.ReadWrite))
                badArg = "access";
            else if (tempshare < FileShare.None || tempshare > (FileShare.ReadWrite | FileShare.Delete))
                badArg = "share";
            if (badArg != null)
                throw new ArgumentOutOfRangeException(badArg, Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            if (options != FileOptions.None && (options & ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Encrypted | (FileOptions)0x20000000)) != 0)
                throw new ArgumentOutOfRangeException("options", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            if (!useRights && (access & FileAccess.Write) == 0)
            {
                if (mode == FileMode.Truncate || mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.Append)
                {
                    if (!useRights)
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFileMode&AccessCombo", mode, access));
                }
            }

            int fAccess;
            if (!useRights)
            {
                fAccess = access == FileAccess.Read ? GENERIC_READ : access == FileAccess.Write ? GENERIC_WRITE : GENERIC_READ | GENERIC_WRITE;
            }
            else
            {
                fAccess = rights;
            }

            int maxPath = useLongPath ? Path.MaxLongPath : Path.MaxPath;
            String filePath = Path.NormalizePath(path, true, maxPath);
            _fileName = filePath;
            if (filePath.StartsWith("\\\\.\\", StringComparison.Ordinal))
                throw new ArgumentException(Environment.GetResourceString("Arg_DevicesNotSupported"));
            Path.CheckInvalidPathChars(filePath, true);
            if (filePath.IndexOf(':', 2) != -1)
                throw new NotSupportedException(Environment.GetResourceString("Argument_PathFormatNotSupported"));
            bool read = false;
            if (!useRights && (access & FileAccess.Read) != 0)
            {
                if (mode == FileMode.Append)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidAppendMode"));
                else
                    read = true;
            }

            {
                FileIOPermissionAccess secAccess = FileIOPermissionAccess.NoAccess;
                if (read)
                {
                                        secAccess = secAccess | FileIOPermissionAccess.Read;
                }

                if (!useRights && (access & FileAccess.Write) != 0)
                {
                    if (mode == FileMode.Append)
                        secAccess = secAccess | FileIOPermissionAccess.Append;
                    else
                        secAccess = secAccess | FileIOPermissionAccess.Write;
                }

                if (checkHost)
                {
                    FileSecurityState state = new FileSecurityState(FileSecurityState.ToFileSecurityState(secAccess), path, filePath);
                    state.EnsureState();
                }
            }

            share &= ~FileShare.Inheritable;
            bool seekToEnd = (mode == FileMode.Append);
            if (mode == FileMode.Append)
                mode = FileMode.OpenOrCreate;
            if (_canUseAsync && (options & FileOptions.Asynchronous) != 0)
                _isAsync = true;
            else
                options &= ~FileOptions.Asynchronous;
            int flagsAndAttributes = (int)options;
            flagsAndAttributes |= (Win32Native.SECURITY_SQOS_PRESENT | Win32Native.SECURITY_ANONYMOUS);
            int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
            try
            {
                String tempPath = filePath;
                if (useLongPath)
                    tempPath = Path.AddLongPathPrefix(tempPath);
                _handle = Win32Native.SafeCreateFile(tempPath, fAccess, share, secAttrs, mode, flagsAndAttributes, IntPtr.Zero);
                if (_handle.IsInvalid)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == __Error.ERROR_PATH_NOT_FOUND && filePath.Equals(Directory.InternalGetDirectoryRoot(filePath)))
                        errorCode = __Error.ERROR_ACCESS_DENIED;
                    bool canGiveFullPath = false;
                    if (!bFromProxy)
                    {
                        try
                        {
                            canGiveFullPath = true;
                        }
                        catch (SecurityException)
                        {
                        }
                    }

                    if (canGiveFullPath)
                        __Error.WinIOError(errorCode, _fileName);
                    else
                        __Error.WinIOError(errorCode, msgPath);
                }
            }
            finally
            {
                Win32Native.SetErrorMode(oldMode);
            }

            int fileType = Win32Native.GetFileType(_handle);
            if (fileType != Win32Native.FILE_TYPE_DISK)
            {
                _handle.Close();
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_FileStreamOnNonFiles"));
            }

            if (_isAsync)
            {
                bool b = false;
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                try
                {
                    b = ThreadPool.BindHandle(_handle);
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                    if (!b)
                    {
                                                _handle.Close();
                    }
                }

                if (!b)
                    throw new IOException(Environment.GetResourceString("IO.IO_BindHandleFailed"));
            }

            if (!useRights)
            {
                _canRead = (access & FileAccess.Read) != 0;
                _canWrite = (access & FileAccess.Write) != 0;
            }

            _canSeek = true;
            _isPipe = false;
            _pos = 0;
            _bufferSize = bufferSize;
            _readPos = 0;
            _readLen = 0;
            _writePos = 0;
            if (seekToEnd)
            {
                _appendStart = SeekCore(0, SeekOrigin.End);
            }
            else
            {
                _appendStart = -1;
            }
        }

        public FileStream(IntPtr handle, FileAccess access): this (handle, access, true, DefaultBufferSize, false)
        {
        }

        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle): this (handle, access, ownsHandle, DefaultBufferSize, false)
        {
        }

        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize): this (handle, access, ownsHandle, bufferSize, false)
        {
        }

        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, bool isAsync): this (new SafeFileHandle(handle, ownsHandle), access, bufferSize, isAsync)
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access): this (handle, access, DefaultBufferSize, false)
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize): this (handle, access, bufferSize, false)
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            if (handle.IsInvalid)
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidHandle"), "handle");
                        _handle = handle;
            _exposedHandle = true;
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException("access", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            int handleType = Win32Native.GetFileType(_handle);
                        _isAsync = isAsync && _canUseAsync;
            _canRead = 0 != (access & FileAccess.Read);
            _canWrite = 0 != (access & FileAccess.Write);
            _canSeek = handleType == Win32Native.FILE_TYPE_DISK;
            _bufferSize = bufferSize;
            _readPos = 0;
            _readLen = 0;
            _writePos = 0;
            _fileName = null;
            _isPipe = handleType == Win32Native.FILE_TYPE_PIPE;
            if (handleType != Win32Native.FILE_TYPE_PIPE)
                VerifyHandleIsSync();
            if (_canSeek)
                SeekCore(0, SeekOrigin.Current);
            else
                _pos = 0;
        }

        private static Win32Native.SECURITY_ATTRIBUTES GetSecAttrs(FileShare share)
        {
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
            if ((share & FileShare.Inheritable) != 0)
            {
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);
                secAttrs.bInheritHandle = 1;
            }

            return secAttrs;
        }

        private unsafe void VerifyHandleIsSync()
        {
            byte[] bytes = new byte[1];
            int hr = 0;
            int r = 0;
            if (CanRead)
            {
                r = ReadFileNative(_handle, bytes, 0, 0, null, out hr);
            }
            else if (CanWrite)
            {
                r = WriteFileNative(_handle, bytes, 0, 0, null, out hr);
            }

            if (hr == ERROR_INVALID_PARAMETER)
                throw new ArgumentException(Environment.GetResourceString("Arg_HandleNotSync"));
            if (hr == Win32Native.ERROR_INVALID_HANDLE)
                __Error.WinIOError(hr, "<OS handle>");
        }

        public override bool CanRead
        {
            
            get
            {
                return _canRead;
            }
        }

        public override bool CanWrite
        {
            
            get
            {
                return _canWrite;
            }
        }

        public override bool CanSeek
        {
            
            get
            {
                return _canSeek;
            }
        }

        public virtual bool IsAsync
        {
            get
            {
                return _isAsync;
            }
        }

        public override long Length
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_handle.IsClosed)
                    __Error.FileNotOpen();
                if (!CanSeek)
                    __Error.SeekNotSupported();
                int hi = 0, lo = 0;
                lo = Win32Native.GetFileSize(_handle, out hi);
                if (lo == -1)
                {
                    int hr = Marshal.GetLastWin32Error();
                    if (hr != 0)
                        __Error.WinIOError(hr, String.Empty);
                }

                long len = (((long)hi) << 32) | ((uint)lo);
                if (_writePos > 0 && _pos + _writePos > len)
                    len = _writePos + _pos;
                return len;
            }
        }

        public String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_fileName == null)
                    return Environment.GetResourceString("IO_UnknownFileName");
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, _fileName);
                sourceState.EnsureState();
                return _fileName;
            }
        }

        internal String NameInternal
        {
            get
            {
                if (_fileName == null)
                    return "<UnknownFileName>";
                return _fileName;
            }
        }

        public override long Position
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_handle.IsClosed)
                    __Error.FileNotOpen();
                if (!CanSeek)
                    __Error.SeekNotSupported();
                                if (_exposedHandle)
                    VerifyOSHandlePosition();
                return _pos + (_readPos - _readLen + _writePos);
            }

            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                                if (_writePos > 0)
                    FlushWrite(false);
                _readPos = 0;
                _readLen = 0;
                Seek(value, SeekOrigin.Begin);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_handle != null && !_handle.IsClosed)
                {
                    if (_writePos > 0)
                    {
                        FlushWrite(!disposing);
                    }
                }
            }
            finally
            {
                if (_handle != null && !_handle.IsClosed)
                    _handle.Dispose();
                _canRead = false;
                _canWrite = false;
                _canSeek = false;
                base.Dispose(disposing);
            }
        }

        ~FileStream()
        {
            if (_handle != null)
            {
                BCLDebug.Correctness(_handle.IsClosed, "You didn't close a FileStream & it got finalized.  Name: \"" + _fileName + "\"");
                Dispose(false);
            }
        }

        public override void Flush()
        {
            Flush(false);
        }

        public virtual void Flush(Boolean flushToDisk)
        {
            if (_handle.IsClosed)
                __Error.FileNotOpen();
            FlushInternalBuffer();
            if (flushToDisk && CanWrite)
            {
                FlushOSBuffer();
            }
        }

        private void FlushInternalBuffer()
        {
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            else if (_readPos < _readLen && CanSeek)
            {
                FlushRead();
            }
        }

        private void FlushOSBuffer()
        {
            if (!Win32Native.FlushFileBuffers(_handle))
            {
                __Error.WinIOError();
            }
        }

        private void FlushRead()
        {
                        if (_readPos - _readLen != 0)
            {
                                SeekCore(_readPos - _readLen, SeekOrigin.Current);
            }

            _readPos = 0;
            _readLen = 0;
        }

        private void FlushWrite(bool calledFromFinalizer)
        {
                        if (_isAsync)
            {
                IAsyncResult asyncResult = BeginWriteCore(_buffer, 0, _writePos, null, null);
                if (!calledFromFinalizer)
                    EndWrite(asyncResult);
            }
            else
                WriteCore(_buffer, 0, _writePos);
            _writePos = 0;
        }

        public virtual IntPtr Handle
        {
            [System.Security.SecurityCritical]
            get
            {
                Flush();
                _readPos = 0;
                _readLen = 0;
                _writePos = 0;
                _exposedHandle = true;
                return _handle.DangerousGetHandle();
            }
        }

        public virtual SafeFileHandle SafeFileHandle
        {
            [System.Security.SecurityCritical]
            get
            {
                Flush();
                _readPos = 0;
                _readLen = 0;
                _writePos = 0;
                _exposedHandle = true;
                return _handle;
            }
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (!CanSeek)
                __Error.SeekNotSupported();
            if (!CanWrite)
                __Error.WriteNotSupported();
            if (_writePos > 0)
            {
                FlushWrite(false);
            }
            else if (_readPos < _readLen)
            {
                FlushRead();
            }

            _readPos = 0;
            _readLen = 0;
            if (_appendStart != -1 && value < _appendStart)
                throw new IOException(Environment.GetResourceString("IO.IO_SetLengthAppendTruncate"));
            SetLengthCore(value);
        }

        private void SetLengthCore(long value)
        {
                        long origPos = _pos;
            if (_exposedHandle)
                VerifyOSHandlePosition();
            if (_pos != value)
                SeekCore(value, SeekOrigin.Begin);
            if (!Win32Native.SetEndOfFile(_handle))
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr == __Error.ERROR_INVALID_PARAMETER)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_FileLengthTooBig"));
                __Error.WinIOError(hr, String.Empty);
            }

            if (origPos != value)
            {
                if (origPos < value)
                    SeekCore(origPos, SeekOrigin.Begin);
                else
                    SeekCore(0, SeekOrigin.End);
            }
        }

        public override int Read([In, Out] byte[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (_handle.IsClosed)
                __Error.FileNotOpen();
                        bool isBlocked = false;
            int n = _readLen - _readPos;
            if (n == 0)
            {
                if (!CanRead)
                    __Error.ReadNotSupported();
                if (_writePos > 0)
                    FlushWrite(false);
                if (!CanSeek || (count >= _bufferSize))
                {
                    n = ReadCore(array, offset, count);
                    _readPos = 0;
                    _readLen = 0;
                    return n;
                }

                if (_buffer == null)
                    _buffer = new byte[_bufferSize];
                n = ReadCore(_buffer, 0, _bufferSize);
                if (n == 0)
                    return 0;
                isBlocked = n < _bufferSize;
                _readPos = 0;
                _readLen = n;
            }

            if (n > count)
                n = count;
            Buffer.InternalBlockCopy(_buffer, _readPos, array, offset, n);
            _readPos += n;
            if (!_isPipe)
            {
                if (n < count && !isBlocked)
                {
                                        int moreBytesRead = ReadCore(array, offset + n, count - n);
                    n += moreBytesRead;
                    _readPos = 0;
                    _readLen = 0;
                }
            }

            return n;
        }

        private unsafe int ReadCore(byte[] buffer, int offset, int count)
        {
                                                                                    if (_isAsync)
            {
                IAsyncResult result = BeginReadCore(buffer, offset, count, null, null, 0);
                return EndRead(result);
            }

            if (_exposedHandle)
                VerifyOSHandlePosition();
            int hr = 0;
            int r = ReadFileNative(_handle, buffer, offset, count, null, out hr);
            if (r == -1)
            {
                if (hr == ERROR_BROKEN_PIPE)
                {
                    r = 0;
                }
                else
                {
                    if (hr == ERROR_INVALID_PARAMETER)
                        throw new ArgumentException(Environment.GetResourceString("Arg_HandleNotSync"));
                    __Error.WinIOError(hr, String.Empty);
                }
            }

                        _pos += r;
            return r;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin < SeekOrigin.Begin || origin > SeekOrigin.End)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSeekOrigin"));
                        if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (!CanSeek)
                __Error.SeekNotSupported();
                        if (_writePos > 0)
            {
                FlushWrite(false);
            }
            else if (origin == SeekOrigin.Current)
            {
                offset -= (_readLen - _readPos);
            }

            if (_exposedHandle)
                VerifyOSHandlePosition();
            long oldPos = _pos + (_readPos - _readLen);
            long pos = SeekCore(offset, origin);
            if (_appendStart != -1 && pos < _appendStart)
            {
                SeekCore(oldPos, SeekOrigin.Begin);
                throw new IOException(Environment.GetResourceString("IO.IO_SeekAppendOverwrite"));
            }

            if (_readLen > 0)
            {
                if (oldPos == pos)
                {
                    if (_readPos > 0)
                    {
                        Buffer.InternalBlockCopy(_buffer, _readPos, _buffer, 0, _readLen - _readPos);
                        _readLen -= _readPos;
                        _readPos = 0;
                    }

                    if (_readLen > 0)
                        SeekCore(_readLen, SeekOrigin.Current);
                }
                else if (oldPos - _readPos < pos && pos < oldPos + _readLen - _readPos)
                {
                    int diff = (int)(pos - oldPos);
                    Buffer.InternalBlockCopy(_buffer, _readPos + diff, _buffer, 0, _readLen - (_readPos + diff));
                    _readLen -= (_readPos + diff);
                    _readPos = 0;
                    if (_readLen > 0)
                        SeekCore(_readLen, SeekOrigin.Current);
                }
                else
                {
                    _readPos = 0;
                    _readLen = 0;
                }

                                            }

            return pos;
        }

        private long SeekCore(long offset, SeekOrigin origin)
        {
                                    int hr = 0;
            long ret = 0;
            ret = Win32Native.SetFilePointer(_handle, offset, origin, out hr);
            if (ret == -1)
            {
                if (hr == Win32Native.ERROR_INVALID_HANDLE)
                    _handle.Dispose();
                __Error.WinIOError(hr, String.Empty);
            }

            _pos = ret;
            return ret;
        }

        private void VerifyOSHandlePosition()
        {
            if (!CanSeek)
                return;
            long oldPos = _pos;
            long curPos = SeekCore(0, SeekOrigin.Current);
            if (curPos != oldPos)
            {
                _readPos = 0;
                _readLen = 0;
                if (_writePos > 0)
                {
                    _writePos = 0;
                    throw new IOException(Environment.GetResourceString("IO.IO_FileStreamHandlePosition"));
                }
            }
        }

        public override void Write(byte[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (_writePos == 0)
            {
                if (!CanWrite)
                    __Error.WriteNotSupported();
                if (_readPos < _readLen)
                    FlushRead();
                _readPos = 0;
                _readLen = 0;
            }

            if (_writePos > 0)
            {
                int numBytes = _bufferSize - _writePos;
                if (numBytes > 0)
                {
                    if (numBytes > count)
                        numBytes = count;
                    Buffer.InternalBlockCopy(array, offset, _buffer, _writePos, numBytes);
                    _writePos += numBytes;
                    if (count == numBytes)
                        return;
                    offset += numBytes;
                    count -= numBytes;
                }

                if (_isAsync)
                {
                    IAsyncResult result = BeginWriteCore(_buffer, 0, _writePos, null, null);
                    EndWrite(result);
                }
                else
                {
                    WriteCore(_buffer, 0, _writePos);
                }

                _writePos = 0;
            }

            if (count >= _bufferSize)
            {
                                WriteCore(array, offset, count);
                return;
            }
            else if (count == 0)
                return;
            if (_buffer == null)
                _buffer = new byte[_bufferSize];
            Buffer.InternalBlockCopy(array, offset, _buffer, _writePos, count);
            _writePos = count;
            return;
        }

        private unsafe void WriteCore(byte[] buffer, int offset, int count)
        {
                                                                                    if (_isAsync)
            {
                IAsyncResult result = BeginWriteCore(buffer, offset, count, null, null);
                EndWrite(result);
                return;
            }

            if (_exposedHandle)
                VerifyOSHandlePosition();
            int hr = 0;
            int r = WriteFileNative(_handle, buffer, offset, count, null, out hr);
            if (r == -1)
            {
                if (hr == ERROR_NO_DATA)
                {
                    r = 0;
                }
                else
                {
                    if (hr == ERROR_INVALID_PARAMETER)
                        throw new IOException(Environment.GetResourceString("IO.IO_FileTooLongOrHandleNotSync"));
                    __Error.WinIOError(hr, String.Empty);
                }
            }

                        _pos += r;
            return;
        }

        public override IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (numBytes < 0)
                throw new ArgumentOutOfRangeException("numBytes", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - offset < numBytes)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (!_isAsync)
                return base.BeginRead(array, offset, numBytes, userCallback, stateObject);
            else
                return BeginReadAsync(array, offset, numBytes, userCallback, stateObject);
        }

        private FileStreamAsyncResult BeginReadAsync(byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
                        if (!CanRead)
                __Error.ReadNotSupported();
                        if (_isPipe)
            {
                if (_readPos < _readLen)
                {
                    int n = _readLen - _readPos;
                    if (n > numBytes)
                        n = numBytes;
                    Buffer.InternalBlockCopy(_buffer, _readPos, array, offset, n);
                    _readPos += n;
                    return FileStreamAsyncResult.CreateBufferedReadResult(n, userCallback, stateObject, false);
                }
                else
                {
                                        return BeginReadCore(array, offset, numBytes, userCallback, stateObject, 0);
                }
            }

                        if (_writePos > 0)
                FlushWrite(false);
            if (_readPos == _readLen)
            {
                if (numBytes < _bufferSize)
                {
                    if (_buffer == null)
                        _buffer = new byte[_bufferSize];
                    IAsyncResult bufferRead = BeginReadCore(_buffer, 0, _bufferSize, null, null, 0);
                    _readLen = EndRead(bufferRead);
                    int n = _readLen;
                    if (n > numBytes)
                        n = numBytes;
                    Buffer.InternalBlockCopy(_buffer, 0, array, offset, n);
                    _readPos = n;
                    return FileStreamAsyncResult.CreateBufferedReadResult(n, userCallback, stateObject, false);
                }
                else
                {
                    _readPos = 0;
                    _readLen = 0;
                    return BeginReadCore(array, offset, numBytes, userCallback, stateObject, 0);
                }
            }
            else
            {
                int n = _readLen - _readPos;
                if (n > numBytes)
                    n = numBytes;
                Buffer.InternalBlockCopy(_buffer, _readPos, array, offset, n);
                _readPos += n;
                if (n >= numBytes)
                {
                    return FileStreamAsyncResult.CreateBufferedReadResult(n, userCallback, stateObject, false);
                }
                else
                {
                    _readPos = 0;
                    _readLen = 0;
                    return BeginReadCore(array, offset + n, numBytes - n, userCallback, stateObject, n);
                }
            }
        }

        unsafe private FileStreamAsyncResult BeginReadCore(byte[] bytes, int offset, int numBytes, AsyncCallback userCallback, Object stateObject, int numBufferedBytesRead)
        {
                                                                                                FileStreamAsyncResult asyncResult = new FileStreamAsyncResult(numBufferedBytesRead, bytes, _handle, userCallback, stateObject, false);
            NativeOverlapped*intOverlapped = asyncResult.OverLapped;
            if (CanSeek)
            {
                long len = Length;
                if (_exposedHandle)
                    VerifyOSHandlePosition();
                if (_pos + numBytes > len)
                {
                    if (_pos <= len)
                        numBytes = (int)(len - _pos);
                    else
                        numBytes = 0;
                }

                intOverlapped->OffsetLow = unchecked ((int)_pos);
                intOverlapped->OffsetHigh = (int)(_pos >> 32);
                SeekCore(numBytes, SeekOrigin.Current);
            }

            if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                FrameworkEventSource.Log.ThreadTransferSend((long)(asyncResult.OverLapped), 2, string.Empty, false);
            int hr = 0;
            int r = ReadFileNative(_handle, bytes, offset, numBytes, intOverlapped, out hr);
            if (r == -1 && numBytes != -1)
            {
                if (hr == ERROR_BROKEN_PIPE)
                {
                    intOverlapped->InternalLow = IntPtr.Zero;
                    asyncResult.CallUserCallback();
                }
                else if (hr != ERROR_IO_PENDING)
                {
                    if (!_handle.IsClosed && CanSeek)
                        SeekCore(0, SeekOrigin.Current);
                    if (hr == ERROR_HANDLE_EOF)
                        __Error.EndOfFile();
                    else
                        __Error.WinIOError(hr, String.Empty);
                }
            }
            else
            {
            }

            return asyncResult;
        }

        public unsafe override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
                        if (!_isAsync)
                return base.EndRead(asyncResult);
            FileStreamAsyncResult afsar = asyncResult as FileStreamAsyncResult;
            if (afsar == null || afsar.IsWrite)
                __Error.WrongAsyncResult();
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
                __Error.EndReadCalledTwice();
            afsar.Wait();
            afsar.ReleaseNativeResource();
            if (afsar.ErrorCode != 0)
                __Error.WinIOError(afsar.ErrorCode, String.Empty);
            return afsar.NumBytesRead;
        }

        public override int ReadByte()
        {
            if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (_readLen == 0 && !CanRead)
                __Error.ReadNotSupported();
                        if (_readPos == _readLen)
            {
                if (_writePos > 0)
                    FlushWrite(false);
                                if (_buffer == null)
                    _buffer = new byte[_bufferSize];
                _readLen = ReadCore(_buffer, 0, _bufferSize);
                _readPos = 0;
            }

            if (_readPos == _readLen)
                return -1;
            int result = _buffer[_readPos];
            _readPos++;
            return result;
        }

        public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (numBytes < 0)
                throw new ArgumentOutOfRangeException("numBytes", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - offset < numBytes)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (!_isAsync)
                return base.BeginWrite(array, offset, numBytes, userCallback, stateObject);
            else
                return BeginWriteAsync(array, offset, numBytes, userCallback, stateObject);
        }

        private FileStreamAsyncResult BeginWriteAsync(byte[] array, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
                        if (!CanWrite)
                __Error.WriteNotSupported();
                        if (_isPipe)
            {
                                if (_writePos > 0)
                    FlushWrite(false);
                return BeginWriteCore(array, offset, numBytes, userCallback, stateObject);
            }

            if (_writePos == 0)
            {
                if (_readPos < _readLen)
                    FlushRead();
                _readPos = 0;
                _readLen = 0;
            }

            int n = _bufferSize - _writePos;
            if (numBytes <= n)
            {
                if (_writePos == 0)
                    _buffer = new byte[_bufferSize];
                Buffer.InternalBlockCopy(array, offset, _buffer, _writePos, numBytes);
                _writePos += numBytes;
                return FileStreamAsyncResult.CreateBufferedReadResult(numBytes, userCallback, stateObject, true);
            }

            if (_writePos > 0)
                FlushWrite(false);
            return BeginWriteCore(array, offset, numBytes, userCallback, stateObject);
        }

        unsafe private FileStreamAsyncResult BeginWriteCore(byte[] bytes, int offset, int numBytes, AsyncCallback userCallback, Object stateObject)
        {
                                                                                                FileStreamAsyncResult asyncResult = new FileStreamAsyncResult(0, bytes, _handle, userCallback, stateObject, true);
            NativeOverlapped*intOverlapped = asyncResult.OverLapped;
            if (CanSeek)
            {
                long len = Length;
                if (_exposedHandle)
                    VerifyOSHandlePosition();
                if (_pos + numBytes > len)
                {
                    SetLengthCore(_pos + numBytes);
                }

                intOverlapped->OffsetLow = (int)_pos;
                intOverlapped->OffsetHigh = (int)(_pos >> 32);
                SeekCore(numBytes, SeekOrigin.Current);
            }

            if (FrameworkEventSource.IsInitialized && FrameworkEventSource.Log.IsEnabled(EventLevel.Informational, FrameworkEventSource.Keywords.ThreadTransfer))
                FrameworkEventSource.Log.ThreadTransferSend((long)(asyncResult.OverLapped), 2, string.Empty, false);
            int hr = 0;
            int r = WriteFileNative(_handle, bytes, offset, numBytes, intOverlapped, out hr);
            if (r == -1 && numBytes != -1)
            {
                if (hr == ERROR_NO_DATA)
                {
                    asyncResult.CallUserCallback();
                }
                else if (hr != ERROR_IO_PENDING)
                {
                    if (!_handle.IsClosed && CanSeek)
                        SeekCore(0, SeekOrigin.Current);
                    if (hr == ERROR_HANDLE_EOF)
                        __Error.EndOfFile();
                    else
                        __Error.WinIOError(hr, String.Empty);
                }
            }
            else
            {
            }

            return asyncResult;
        }

        public unsafe override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
                        if (!_isAsync)
            {
                base.EndWrite(asyncResult);
                return;
            }

            FileStreamAsyncResult afsar = asyncResult as FileStreamAsyncResult;
            if (afsar == null || !afsar.IsWrite)
                __Error.WrongAsyncResult();
            if (1 == Interlocked.CompareExchange(ref afsar._EndXxxCalled, 1, 0))
                __Error.EndWriteCalledTwice();
            afsar.Wait();
            afsar.ReleaseNativeResource();
            if (afsar.ErrorCode != 0)
                __Error.WinIOError(afsar.ErrorCode, String.Empty);
            return;
        }

        public override void WriteByte(byte value)
        {
            if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (_writePos == 0)
            {
                if (!CanWrite)
                    __Error.WriteNotSupported();
                if (_readPos < _readLen)
                    FlushRead();
                _readPos = 0;
                _readLen = 0;
                                if (_buffer == null)
                    _buffer = new byte[_bufferSize];
            }

            if (_writePos == _bufferSize)
                FlushWrite(false);
            _buffer[_writePos] = value;
            _writePos++;
        }

        public virtual void Lock(long position, long length)
        {
            if (position < 0 || length < 0)
                throw new ArgumentOutOfRangeException((position < 0 ? "position" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        if (_handle.IsClosed)
                __Error.FileNotOpen();
            int positionLow = unchecked ((int)(position));
            int positionHigh = unchecked ((int)(position >> 32));
            int lengthLow = unchecked ((int)(length));
            int lengthHigh = unchecked ((int)(length >> 32));
            if (!Win32Native.LockFile(_handle, positionLow, positionHigh, lengthLow, lengthHigh))
                __Error.WinIOError();
        }

        public virtual void Unlock(long position, long length)
        {
            if (position < 0 || length < 0)
                throw new ArgumentOutOfRangeException((position < 0 ? "position" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        if (_handle.IsClosed)
                __Error.FileNotOpen();
            int positionLow = unchecked ((int)(position));
            int positionHigh = unchecked ((int)(position >> 32));
            int lengthLow = unchecked ((int)(length));
            int lengthHigh = unchecked ((int)(length >> 32));
            if (!Win32Native.UnlockFile(_handle, positionLow, positionHigh, lengthLow, lengthHigh))
                __Error.WinIOError();
        }

        private const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const int FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
        private const int FILE_FLAG_OVERLAPPED = 0x40000000;
        internal const int GENERIC_READ = unchecked ((int)0x80000000);
        private const int GENERIC_WRITE = 0x40000000;
        private const int FILE_BEGIN = 0;
        private const int FILE_CURRENT = 1;
        private const int FILE_END = 2;
        internal const int ERROR_BROKEN_PIPE = 109;
        internal const int ERROR_NO_DATA = 232;
        private const int ERROR_HANDLE_EOF = 38;
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_IO_PENDING = 997;
        private unsafe int ReadFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, NativeOverlapped*overlapped, out int hr)
        {
                                                            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(Environment.GetResourceString("IndexOutOfRange_IORaceCondition"));
                                    if (bytes.Length == 0)
            {
                hr = 0;
                return 0;
            }

            int r = 0;
            int numBytesRead = 0;
            fixed (byte *p = bytes)
            {
                if (_isAsync)
                    r = Win32Native.ReadFile(handle, p + offset, count, IntPtr.Zero, overlapped);
                else
                    r = Win32Native.ReadFile(handle, p + offset, count, out numBytesRead, IntPtr.Zero);
            }

            if (r == 0)
            {
                hr = Marshal.GetLastWin32Error();
                if (hr == ERROR_BROKEN_PIPE || hr == Win32Native.ERROR_PIPE_NOT_CONNECTED)
                {
                    return -1;
                }

                if (hr == Win32Native.ERROR_INVALID_HANDLE)
                    _handle.Dispose();
                return -1;
            }
            else
                hr = 0;
            return numBytesRead;
        }

        private unsafe int WriteFileNative(SafeFileHandle handle, byte[] bytes, int offset, int count, NativeOverlapped*overlapped, out int hr)
        {
                                                            if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(Environment.GetResourceString("IndexOutOfRange_IORaceCondition"));
                                    if (bytes.Length == 0)
            {
                hr = 0;
                return 0;
            }

            int numBytesWritten = 0;
            int r = 0;
            fixed (byte *p = bytes)
            {
                if (_isAsync)
                    r = Win32Native.WriteFile(handle, p + offset, count, IntPtr.Zero, overlapped);
                else
                    r = Win32Native.WriteFile(handle, p + offset, count, out numBytesWritten, IntPtr.Zero);
            }

            if (r == 0)
            {
                hr = Marshal.GetLastWin32Error();
                if (hr == ERROR_NO_DATA)
                {
                    return -1;
                }

                if (hr == Win32Native.ERROR_INVALID_HANDLE)
                    _handle.Dispose();
                return -1;
            }
            else
                hr = 0;
            return numBytesWritten;
        }

        public override Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (this.GetType() != typeof (FileStream))
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<int>(cancellationToken);
            if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (!_isAsync)
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            var readTask = new FileStreamReadWriteTask<int>(cancellationToken);
            var endReadTask = s_endReadTask;
            if (endReadTask == null)
                s_endReadTask = endReadTask = EndReadTask;
            readTask._asyncResult = BeginReadAsync(buffer, offset, count, endReadTask, readTask);
            if (readTask._asyncResult.IsAsync && cancellationToken.CanBeCanceled)
            {
                var cancelReadHandler = s_cancelReadHandler;
                if (cancelReadHandler == null)
                    s_cancelReadHandler = cancelReadHandler = CancelTask<int>;
                readTask._registration = cancellationToken.Register(cancelReadHandler, readTask);
                if (readTask._asyncResult.IsCompleted)
                    readTask._registration.Dispose();
            }

            return readTask;
        }

        public override Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (this.GetType() != typeof (FileStream))
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
            if (_handle.IsClosed)
                __Error.FileNotOpen();
            if (!_isAsync)
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            var writeTask = new FileStreamReadWriteTask<VoidTaskResult>(cancellationToken);
            var endWriteTask = s_endWriteTask;
            if (endWriteTask == null)
                s_endWriteTask = endWriteTask = EndWriteTask;
            writeTask._asyncResult = BeginWriteAsync(buffer, offset, count, endWriteTask, writeTask);
            if (writeTask._asyncResult.IsAsync && cancellationToken.CanBeCanceled)
            {
                var cancelWriteHandler = s_cancelWriteHandler;
                if (cancelWriteHandler == null)
                    s_cancelWriteHandler = cancelWriteHandler = CancelTask<VoidTaskResult>;
                writeTask._registration = cancellationToken.Register(cancelWriteHandler, writeTask);
                if (writeTask._asyncResult.IsCompleted)
                    writeTask._registration.Dispose();
            }

            return writeTask;
        }

        private sealed class FileStreamReadWriteTask<T> : Task<T>
        {
            internal CancellationToken _cancellationToken;
            internal CancellationTokenRegistration _registration;
            internal FileStreamAsyncResult _asyncResult;
            internal FileStreamReadWriteTask(CancellationToken cancellationToken): base ()
            {
                _cancellationToken = cancellationToken;
            }
        }

        private static void CancelTask<T>(object state)
        {
            var task = state as FileStreamReadWriteTask<T>;
                        FileStreamAsyncResult asyncResult = task._asyncResult;
                                                try
            {
                if (!asyncResult.IsCompleted)
                    asyncResult.Cancel();
            }
            catch (Exception ex)
            {
                task.TrySetException(ex);
            }
        }

        private static void EndReadTask(IAsyncResult iar)
        {
            FileStreamAsyncResult asyncResult = iar as FileStreamAsyncResult;
                                    var readTask = asyncResult.AsyncState as FileStreamReadWriteTask<int>;
                        try
            {
                if (asyncResult.IsAsync)
                {
                    asyncResult.ReleaseNativeResource();
                    readTask._registration.Dispose();
                }

                if (asyncResult.ErrorCode == Win32Native.ERROR_OPERATION_ABORTED)
                {
                    var cancellationToken = readTask._cancellationToken;
                                        readTask.TrySetCanceled(cancellationToken);
                }
                else
                    readTask.TrySetResult(asyncResult.NumBytesRead);
            }
            catch (Exception ex)
            {
                readTask.TrySetException(ex);
            }
        }

        private static void EndWriteTask(IAsyncResult iar)
        {
            var asyncResult = iar as FileStreamAsyncResult;
                                    var writeTask = iar.AsyncState as FileStreamReadWriteTask<VoidTaskResult>;
                        try
            {
                if (asyncResult.IsAsync)
                {
                    asyncResult.ReleaseNativeResource();
                    writeTask._registration.Dispose();
                }

                if (asyncResult.ErrorCode == Win32Native.ERROR_OPERATION_ABORTED)
                {
                    var cancellationToken = writeTask._cancellationToken;
                                        writeTask.TrySetCanceled(cancellationToken);
                }
                else
                    writeTask.TrySetResult(default (VoidTaskResult));
            }
            catch (Exception ex)
            {
                writeTask.TrySetException(ex);
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (this.GetType() != typeof (FileStream))
                return base.FlushAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
            if (_handle.IsClosed)
                __Error.FileNotOpen();
            try
            {
                FlushInternalBuffer();
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }

            if (CanWrite)
                return Task.Factory.StartNew(state => ((FileStream)state).FlushOSBuffer(), this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            else
                return Task.CompletedTask;
        }
    }
}