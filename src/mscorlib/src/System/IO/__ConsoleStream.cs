
using System.Runtime.InteropServices;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal sealed class __ConsoleStream : Stream
    {
        const int BytesPerWChar = 2;
        private SafeFileHandle _handle;
        private bool _canRead;
        private bool _canWrite;
        private bool _useFileAPIs;
        private bool _isPipe;
        internal __ConsoleStream(SafeFileHandle handle, FileAccess access, bool useFileAPIs)
        {
                        _handle = handle;
            _canRead = ((access & FileAccess.Read) == FileAccess.Read);
            _canWrite = ((access & FileAccess.Write) == FileAccess.Write);
            _useFileAPIs = useFileAPIs;
            _isPipe = Win32Native.GetFileType(handle) == Win32Native.FILE_TYPE_PIPE;
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
                return false;
            }
        }

        public override long Length
        {
            get
            {
                __Error.SeekNotSupported();
                return 0;
            }
        }

        public override long Position
        {
            get
            {
                __Error.SeekNotSupported();
                return 0;
            }

            set
            {
                __Error.SeekNotSupported();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_handle != null)
            {
                _handle = null;
            }

            _canRead = false;
            _canWrite = false;
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            if (_handle == null)
                __Error.FileNotOpen();
            if (!CanWrite)
                __Error.WriteNotSupported();
        }

        public override void SetLength(long value)
        {
            __Error.SeekNotSupported();
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException((offset < 0 ? "offset" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (!_canRead)
                __Error.ReadNotSupported();
            int bytesRead;
            int errCode = ReadFileNative(_handle, buffer, offset, count, _useFileAPIs, _isPipe, out bytesRead);
            if (Win32Native.ERROR_SUCCESS != errCode)
                __Error.WinIOError(errCode, String.Empty);
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            __Error.SeekNotSupported();
            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException((offset < 0 ? "offset" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (!_canWrite)
                __Error.WriteNotSupported();
            int errCode = WriteFileNative(_handle, buffer, offset, count, _useFileAPIs);
            if (Win32Native.ERROR_SUCCESS != errCode)
                __Error.WinIOError(errCode, String.Empty);
            return;
        }

        private unsafe static int ReadFileNative(SafeFileHandle hFile, byte[] bytes, int offset, int count, bool useFileAPIs, bool isPipe, out int bytesRead)
        {
                                                if (bytes.Length - offset < count)
                throw new IndexOutOfRangeException(Environment.GetResourceString("IndexOutOfRange_IORaceCondition"));
                        if (bytes.Length == 0)
            {
                bytesRead = 0;
                return Win32Native.ERROR_SUCCESS;
            }

            bool readSuccess;
            if (useFileAPIs)
            {
                fixed (byte *p = bytes)
                {
                    readSuccess = (0 != Win32Native.ReadFile(hFile, p + offset, count, out bytesRead, IntPtr.Zero));
                }
            }
            else
            {
                fixed (byte *p = bytes)
                {
                    int charsRead;
                    readSuccess = Win32Native.ReadConsoleW(hFile, p + offset, count / BytesPerWChar, out charsRead, IntPtr.Zero);
                    bytesRead = charsRead * BytesPerWChar;
                }
            }

            if (readSuccess)
                return Win32Native.ERROR_SUCCESS;
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == Win32Native.ERROR_NO_DATA || errorCode == Win32Native.ERROR_BROKEN_PIPE)
                return Win32Native.ERROR_SUCCESS;
            return errorCode;
        }

        private static unsafe int WriteFileNative(SafeFileHandle hFile, byte[] bytes, int offset, int count, bool useFileAPIs)
        {
                                                            if (bytes.Length == 0)
                return Win32Native.ERROR_SUCCESS;
            bool writeSuccess;
            if (useFileAPIs)
            {
                fixed (byte *p = bytes)
                {
                    int numBytesWritten;
                    writeSuccess = (0 != Win32Native.WriteFile(hFile, p + offset, count, out numBytesWritten, IntPtr.Zero));
                                    }
            }
            else
            {
                fixed (byte *p = bytes)
                {
                    Int32 charsWritten;
                    writeSuccess = Win32Native.WriteConsoleW(hFile, p + offset, count / BytesPerWChar, out charsWritten, IntPtr.Zero);
                                    }
            }

            if (writeSuccess)
                return Win32Native.ERROR_SUCCESS;
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == Win32Native.ERROR_NO_DATA || errorCode == Win32Native.ERROR_BROKEN_PIPE)
                return Win32Native.ERROR_SUCCESS;
            return errorCode;
        }

        private static extern void WaitForAvailableConsoleInput(SafeFileHandle file, bool isPipe);
    }
}