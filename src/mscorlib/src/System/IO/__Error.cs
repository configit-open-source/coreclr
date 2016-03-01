using System;
using System.Runtime.InteropServices;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Text;
using System.Globalization;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics.Contracts;

namespace System.IO
{
    internal static class __Error
    {
        internal static void EndOfFile()
        {
            throw new EndOfStreamException(Environment.GetResourceString("IO.EOF_ReadBeyondEOF"));
        }

        internal static void FileNotOpen()
        {
            throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_FileClosed"));
        }

        internal static void StreamIsClosed()
        {
            throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_StreamClosed"));
        }

        internal static void MemoryStreamNotExpandable()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_MemStreamNotExpandable"));
        }

        internal static void ReaderClosed()
        {
            throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ReaderClosed"));
        }

        internal static void ReadNotSupported()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
        }

        internal static void SeekNotSupported()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
        }

        internal static void WrongAsyncResult()
        {
            throw new ArgumentException(Environment.GetResourceString("Arg_WrongAsyncResult"));
        }

        internal static void EndReadCalledTwice()
        {
            throw new ArgumentException(Environment.GetResourceString("InvalidOperation_EndReadCalledMultiple"));
        }

        internal static void EndWriteCalledTwice()
        {
            throw new ArgumentException(Environment.GetResourceString("InvalidOperation_EndWriteCalledMultiple"));
        }

        internal static String GetDisplayablePath(String path, bool isInvalidPath)
        {
            if (String.IsNullOrEmpty(path))
                return String.Empty;
            bool isFullyQualified = false;
            if (path.Length < 2)
                return path;
            if (Path.IsDirectorySeparator(path[0]) && Path.IsDirectorySeparator(path[1]))
                isFullyQualified = true;
            else if (path[1] == Path.VolumeSeparatorChar)
            {
                isFullyQualified = true;
            }

            if (!isFullyQualified && !isInvalidPath)
                return path;
            bool safeToReturn = false;
            try
            {
                if (!isInvalidPath)
                {
                    safeToReturn = true;
                }
            }
            catch (SecurityException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }

            if (!safeToReturn)
            {
                if (Path.IsDirectorySeparator(path[path.Length - 1]))
                    path = Environment.GetResourceString("IO.IO_NoPermissionToDirectoryName");
                else
                    path = Path.GetFileName(path);
            }

            return path;
        }

        internal static void WinIOError()
        {
            int errorCode = Marshal.GetLastWin32Error();
            WinIOError(errorCode, String.Empty);
        }

        internal static void WinIOError(int errorCode, String maybeFullPath)
        {
            bool isInvalidPath = errorCode == Win32Native.ERROR_INVALID_NAME || errorCode == Win32Native.ERROR_BAD_PATHNAME;
            String str = GetDisplayablePath(maybeFullPath, isInvalidPath);
            switch (errorCode)
            {
                case Win32Native.ERROR_FILE_NOT_FOUND:
                    if (str.Length == 0)
                        throw new FileNotFoundException(Environment.GetResourceString("IO.FileNotFound"));
                    else
                        throw new FileNotFoundException(Environment.GetResourceString("IO.FileNotFound_FileName", str), str);
                case Win32Native.ERROR_PATH_NOT_FOUND:
                    if (str.Length == 0)
                        throw new DirectoryNotFoundException(Environment.GetResourceString("IO.PathNotFound_NoPathName"));
                    else
                        throw new DirectoryNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", str));
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str.Length == 0)
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_IODenied_NoPathName"));
                    else
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", str));
                case Win32Native.ERROR_ALREADY_EXISTS:
                    if (str.Length == 0)
                        goto default;
                    throw new IOException(Environment.GetResourceString("IO.IO_AlreadyExists_Name", str), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
                case Win32Native.ERROR_FILENAME_EXCED_RANGE:
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                case Win32Native.ERROR_INVALID_DRIVE:
                    throw new DriveNotFoundException(Environment.GetResourceString("IO.DriveNotFound_Drive", str));
                case Win32Native.ERROR_INVALID_PARAMETER:
                    throw new IOException(Win32Native.GetMessage(errorCode), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
                case Win32Native.ERROR_SHARING_VIOLATION:
                    if (str.Length == 0)
                        throw new IOException(Environment.GetResourceString("IO.IO_SharingViolation_NoFileName"), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
                    else
                        throw new IOException(Environment.GetResourceString("IO.IO_SharingViolation_File", str), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
                case Win32Native.ERROR_FILE_EXISTS:
                    if (str.Length == 0)
                        goto default;
                    throw new IOException(Environment.GetResourceString("IO.IO_FileExists_Name", str), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
                case Win32Native.ERROR_OPERATION_ABORTED:
                    throw new OperationCanceledException();
                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
            }
        }

        internal static void WinIODriveError(String driveName)
        {
            int errorCode = Marshal.GetLastWin32Error();
            WinIODriveError(driveName, errorCode);
        }

        internal static void WinIODriveError(String driveName, int errorCode)
        {
            switch (errorCode)
            {
                case Win32Native.ERROR_PATH_NOT_FOUND:
                case Win32Native.ERROR_INVALID_DRIVE:
                    throw new DriveNotFoundException(Environment.GetResourceString("IO.DriveNotFound_Drive", driveName));
                default:
                    WinIOError(errorCode, driveName);
                    break;
            }
        }

        internal static void WriteNotSupported()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
        }

        internal static void WriterClosed()
        {
            throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_WriterClosed"));
        }

        internal const int ERROR_FILE_NOT_FOUND = Win32Native.ERROR_FILE_NOT_FOUND;
        internal const int ERROR_PATH_NOT_FOUND = Win32Native.ERROR_PATH_NOT_FOUND;
        internal const int ERROR_ACCESS_DENIED = Win32Native.ERROR_ACCESS_DENIED;
        internal const int ERROR_INVALID_PARAMETER = Win32Native.ERROR_INVALID_PARAMETER;
    }
}