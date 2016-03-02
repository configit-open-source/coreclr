using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static class File
    {
        private const int GetFileExInfoStandard = 0;
        public static StreamReader OpenText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        return new StreamReader(path);
        }

        public static StreamWriter CreateText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        return new StreamWriter(path, false);
        }

        public static StreamWriter AppendText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        return new StreamWriter(path, true);
        }

        public static void Copy(String sourceFileName, String destFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
                        InternalCopy(sourceFileName, destFileName, false, true);
        }

        public static void Copy(String sourceFileName, String destFileName, bool overwrite)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
                        InternalCopy(sourceFileName, destFileName, overwrite, true);
        }

        internal static void UnsafeCopy(String sourceFileName, String destFileName, bool overwrite)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
                        InternalCopy(sourceFileName, destFileName, overwrite, false);
        }

        internal static String InternalCopy(String sourceFileName, String destFileName, bool overwrite, bool checkHost)
        {
                                                            String fullSourceFileName = Path.GetFullPathInternal(sourceFileName);
            String fullDestFileName = Path.GetFullPathInternal(destFileName);
            if (checkHost)
            {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, sourceFileName, fullSourceFileName);
                FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destFileName, fullDestFileName);
                sourceState.EnsureState();
                destState.EnsureState();
            }

            bool r = Win32Native.CopyFile(fullSourceFileName, fullDestFileName, !overwrite);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                String fileName = destFileName;
                if (errorCode != Win32Native.ERROR_FILE_EXISTS)
                {
                    using (SafeFileHandle handle = Win32Native.UnsafeCreateFile(fullSourceFileName, FileStream.GENERIC_READ, FileShare.Read, null, FileMode.Open, 0, IntPtr.Zero))
                    {
                        if (handle.IsInvalid)
                            fileName = sourceFileName;
                    }

                    if (errorCode == Win32Native.ERROR_ACCESS_DENIED)
                    {
                        if (Directory.InternalExists(fullDestFileName))
                            throw new IOException(Environment.GetResourceString("Arg_FileIsDirectory_Name", destFileName), Win32Native.ERROR_ACCESS_DENIED, fullDestFileName);
                    }
                }

                __Error.WinIOError(errorCode, fileName);
            }

            return fullDestFileName;
        }

        public static FileStream Create(String path)
        {
            return Create(path, FileStream.DefaultBufferSize);
        }

        public static FileStream Create(String path, int bufferSize)
        {
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize);
        }

        public static FileStream Create(String path, int bufferSize, FileOptions options)
        {
            return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize, options);
        }

        public static void Delete(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
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

            InternalDelete(path, true);
        }

        internal static void UnsafeDelete(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        InternalDelete(path, false);
        }

        internal static void InternalDelete(String path, bool checkHost)
        {
            String fullPath = Path.GetFullPathInternal(path);
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, path, fullPath);
                state.EnsureState();
            }

            bool r = Win32Native.DeleteFile(fullPath);
            if (!r)
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND)
                    return;
                else
                    __Error.WinIOError(hr, fullPath);
            }
        }

        public static void Decrypt(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        String fullPath = Path.GetFullPathInternal(path);
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, fullPath, false, false);
            bool r = Win32Native.DecryptFile(fullPath, 0);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Win32Native.ERROR_ACCESS_DENIED)
                {
                    DriveInfo di = new DriveInfo(Path.GetPathRoot(fullPath));
                    if (!String.Equals("NTFS", di.DriveFormat))
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_EncryptionNeedsNTFS"));
                }

                __Error.WinIOError(errorCode, fullPath);
            }
        }

        public static void Encrypt(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        String fullPath = Path.GetFullPathInternal(path);
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read | FileIOPermissionAccess.Write, fullPath, false, false);
            bool r = Win32Native.EncryptFile(fullPath);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Win32Native.ERROR_ACCESS_DENIED)
                {
                    DriveInfo di = new DriveInfo(Path.GetPathRoot(fullPath));
                    if (!String.Equals("NTFS", di.DriveFormat))
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_EncryptionNeedsNTFS"));
                }

                __Error.WinIOError(errorCode, fullPath);
            }
        }

        public static bool Exists(String path)
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

            return InternalExistsHelper(path, true);
        }

        internal static bool UnsafeExists(String path)
        {
            return InternalExistsHelper(path, false);
        }

        private static bool InternalExistsHelper(String path, bool checkHost)
        {
            try
            {
                if (path == null)
                    return false;
                if (path.Length == 0)
                    return false;
                path = Path.GetFullPathInternal(path);
                                if (path.Length > 0 && Path.IsDirectorySeparator(path[path.Length - 1]))
                {
                    return false;
                }

                if (checkHost)
                {
                    FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, path);
                    state.EnsureState();
                }

                return InternalExists(path);
            }
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return false;
        }

        internal static bool InternalExists(String path)
        {
            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(path, ref data, false, true);
            return (dataInitialised == 0) && (data.fileAttributes != -1) && ((data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) == 0);
        }

        public static FileStream Open(String path, FileMode mode)
        {
            return Open(path, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None);
        }

        public static FileStream Open(String path, FileMode mode, FileAccess access)
        {
            return Open(path, mode, access, FileShare.None);
        }

        public static FileStream Open(String path, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(path, mode, access, share);
        }

        public static void SetCreationTime(String path, DateTime creationTime)
        {
            SetCreationTimeUtc(path, creationTime.ToUniversalTime());
        }

        public unsafe static void SetCreationTimeUtc(String path, DateTime creationTimeUtc)
        {
            SafeFileHandle handle;
            using (OpenFile(path, FileAccess.Write, out handle))
            {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(creationTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle, &fileTime, null, null);
                if (!r)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        public static DateTime GetCreationTime(String path)
        {
            return InternalGetCreationTimeUtc(path, true).ToLocalTime();
        }

        public static DateTime GetCreationTimeUtc(String path)
        {
            return InternalGetCreationTimeUtc(path, false);
        }

        private static DateTime InternalGetCreationTimeUtc(String path, bool checkHost)
        {
            String fullPath = Path.GetFullPathInternal(path);
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, fullPath);
                state.EnsureState();
            }

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);
            long dt = ((long)(data.ftCreationTimeHigh) << 32) | ((long)data.ftCreationTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        public static void SetLastAccessTime(String path, DateTime lastAccessTime)
        {
            SetLastAccessTimeUtc(path, lastAccessTime.ToUniversalTime());
        }

        public unsafe static void SetLastAccessTimeUtc(String path, DateTime lastAccessTimeUtc)
        {
            SafeFileHandle handle;
            using (OpenFile(path, FileAccess.Write, out handle))
            {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(lastAccessTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle, null, &fileTime, null);
                if (!r)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        public static DateTime GetLastAccessTime(String path)
        {
            return InternalGetLastAccessTimeUtc(path, true).ToLocalTime();
        }

        public static DateTime GetLastAccessTimeUtc(String path)
        {
            return InternalGetLastAccessTimeUtc(path, false);
        }

        private static DateTime InternalGetLastAccessTimeUtc(String path, bool checkHost)
        {
            String fullPath = Path.GetFullPathInternal(path);
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, fullPath);
                state.EnsureState();
            }

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);
            long dt = ((long)(data.ftLastAccessTimeHigh) << 32) | ((long)data.ftLastAccessTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        public static void SetLastWriteTime(String path, DateTime lastWriteTime)
        {
            SetLastWriteTimeUtc(path, lastWriteTime.ToUniversalTime());
        }

        public unsafe static void SetLastWriteTimeUtc(String path, DateTime lastWriteTimeUtc)
        {
            SafeFileHandle handle;
            using (OpenFile(path, FileAccess.Write, out handle))
            {
                Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME(lastWriteTimeUtc.ToFileTimeUtc());
                bool r = Win32Native.SetFileTime(handle, null, null, &fileTime);
                if (!r)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    __Error.WinIOError(errorCode, path);
                }
            }
        }

        public static DateTime GetLastWriteTime(String path)
        {
            return InternalGetLastWriteTimeUtc(path, true).ToLocalTime();
        }

        public static DateTime GetLastWriteTimeUtc(String path)
        {
            return InternalGetLastWriteTimeUtc(path, false);
        }

        private static DateTime InternalGetLastWriteTimeUtc(String path, bool checkHost)
        {
            String fullPath = Path.GetFullPathInternal(path);
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, fullPath);
                state.EnsureState();
            }

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, false);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);
            long dt = ((long)data.ftLastWriteTimeHigh << 32) | ((long)data.ftLastWriteTimeLow);
            return DateTime.FromFileTimeUtc(dt);
        }

        public static FileAttributes GetAttributes(String path)
        {
            String fullPath = Path.GetFullPathInternal(path);
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, fullPath);
            state.EnsureState();
            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = FillAttributeInfo(fullPath, ref data, false, true);
            if (dataInitialised != 0)
                __Error.WinIOError(dataInitialised, fullPath);
            return (FileAttributes)data.fileAttributes;
        }

        public static void SetAttributes(String path, FileAttributes fileAttributes)
        {
            String fullPath = Path.GetFullPathInternal(path);
            bool r = Win32Native.SetFileAttributes(fullPath, (int)fileAttributes);
            if (!r)
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr == ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileAttrs"));
                __Error.WinIOError(hr, fullPath);
            }
        }

        public static FileStream OpenRead(String path)
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

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public static FileStream OpenWrite(String path)
        {
            return new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        public static String ReadAllText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        return InternalReadAllText(path, Encoding.UTF8, true);
        }

        public static String ReadAllText(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        return InternalReadAllText(path, encoding, true);
        }

        internal static String UnsafeReadAllText(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        return InternalReadAllText(path, Encoding.UTF8, false);
        }

        private static String InternalReadAllText(String path, Encoding encoding, bool checkHost)
        {
                                                using (StreamReader sr = new StreamReader(path, encoding, true, StreamReader.DefaultBufferSize, checkHost))
                return sr.ReadToEnd();
        }

        public static void WriteAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllText(path, contents, StreamWriter.UTF8NoBOM, true);
        }

        public static void WriteAllText(String path, String contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllText(path, contents, encoding, true);
        }

        internal static void UnsafeWriteAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllText(path, contents, StreamWriter.UTF8NoBOM, false);
        }

        private static void InternalWriteAllText(String path, String contents, Encoding encoding, bool checkHost)
        {
                                                using (StreamWriter sw = new StreamWriter(path, false, encoding, StreamWriter.DefaultBufferSize, checkHost))
                sw.Write(contents);
        }

        public static byte[] ReadAllBytes(String path)
        {
            return InternalReadAllBytes(path, true);
        }

        internal static byte[] UnsafeReadAllBytes(String path)
        {
            return InternalReadAllBytes(path, false);
        }

        private static byte[] InternalReadAllBytes(String path, bool checkHost)
        {
            byte[] bytes;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileStream.DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false, checkHost))
            {
                int index = 0;
                long fileLength = fs.Length;
                if (fileLength > Int32.MaxValue)
                    throw new IOException(Environment.GetResourceString("IO.IO_FileTooLong2GB"));
                int count = (int)fileLength;
                bytes = new byte[count];
                while (count > 0)
                {
                    int n = fs.Read(bytes, index, count);
                    if (n == 0)
                        __Error.EndOfFile();
                    index += n;
                    count -= n;
                }
            }

            return bytes;
        }

        public static void WriteAllBytes(String path, byte[] bytes)
        {
            if (path == null)
                throw new ArgumentNullException("path", Environment.GetResourceString("ArgumentNull_Path"));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            if (bytes == null)
                throw new ArgumentNullException("bytes");
                        InternalWriteAllBytes(path, bytes, true);
        }

        internal static void UnsafeWriteAllBytes(String path, byte[] bytes)
        {
            if (path == null)
                throw new ArgumentNullException("path", Environment.GetResourceString("ArgumentNull_Path"));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            if (bytes == null)
                throw new ArgumentNullException("bytes");
                        InternalWriteAllBytes(path, bytes, false);
        }

        private static void InternalWriteAllBytes(String path, byte[] bytes, bool checkHost)
        {
                                                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, FileStream.DefaultBufferSize, FileOptions.None, Path.GetFileName(path), false, false, checkHost))
            {
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        public static String[] ReadAllLines(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        return InternalReadAllLines(path, Encoding.UTF8);
        }

        public static String[] ReadAllLines(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        return InternalReadAllLines(path, encoding);
        }

        private static String[] InternalReadAllLines(String path, Encoding encoding)
        {
                                                String line;
            List<String> lines = new List<String>();
            using (StreamReader sr = new StreamReader(path, encoding))
                while ((line = sr.ReadLine()) != null)
                    lines.Add(line);
            return lines.ToArray();
        }

        public static IEnumerable<String> ReadLines(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
                        return ReadLinesIterator.CreateIterator(path, Encoding.UTF8);
        }

        public static IEnumerable<String> ReadLines(String path, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
                        return ReadLinesIterator.CreateIterator(path, encoding);
        }

        public static void WriteAllLines(String path, String[] contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllLines(new StreamWriter(path, false, StreamWriter.UTF8NoBOM), contents);
        }

        public static void WriteAllLines(String path, String[] contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllLines(new StreamWriter(path, false, encoding), contents);
        }

        public static void WriteAllLines(String path, IEnumerable<String> contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllLines(new StreamWriter(path, false, StreamWriter.UTF8NoBOM), contents);
        }

        public static void WriteAllLines(String path, IEnumerable<String> contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllLines(new StreamWriter(path, false, encoding), contents);
        }

        private static void InternalWriteAllLines(TextWriter writer, IEnumerable<String> contents)
        {
                                    using (writer)
            {
                foreach (String line in contents)
                {
                    writer.WriteLine(line);
                }
            }
        }

        public static void AppendAllText(String path, String contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalAppendAllText(path, contents, StreamWriter.UTF8NoBOM);
        }

        public static void AppendAllText(String path, String contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalAppendAllText(path, contents, encoding);
        }

        private static void InternalAppendAllText(String path, String contents, Encoding encoding)
        {
                                                using (StreamWriter sw = new StreamWriter(path, true, encoding))
                sw.Write(contents);
        }

        public static void AppendAllLines(String path, IEnumerable<String> contents)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllLines(new StreamWriter(path, true, StreamWriter.UTF8NoBOM), contents);
        }

        public static void AppendAllLines(String path, IEnumerable<String> contents, Encoding encoding)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (contents == null)
                throw new ArgumentNullException("contents");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
                        InternalWriteAllLines(new StreamWriter(path, true, encoding), contents);
        }

        public static void Move(String sourceFileName, String destFileName)
        {
            InternalMove(sourceFileName, destFileName, true);
        }

        internal static void UnsafeMove(String sourceFileName, String destFileName)
        {
            InternalMove(sourceFileName, destFileName, false);
        }

        private static void InternalMove(String sourceFileName, String destFileName, bool checkHost)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (sourceFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
                        String fullSourceFileName = Path.GetFullPathInternal(sourceFileName);
            String fullDestFileName = Path.GetFullPathInternal(destFileName);
            if (checkHost)
            {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, sourceFileName, fullSourceFileName);
                FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destFileName, fullDestFileName);
                sourceState.EnsureState();
                destState.EnsureState();
            }

            if (!InternalExists(fullSourceFileName))
                __Error.WinIOError(Win32Native.ERROR_FILE_NOT_FOUND, fullSourceFileName);
            if (!Win32Native.MoveFile(fullSourceFileName, fullDestFileName))
            {
                __Error.WinIOError();
            }
        }

        public static void Replace(String sourceFileName, String destinationFileName, String destinationBackupFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");
            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");
                        InternalReplace(sourceFileName, destinationFileName, destinationBackupFileName, false);
        }

        public static void Replace(String sourceFileName, String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");
            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");
                        InternalReplace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
        }

        private static void InternalReplace(String sourceFileName, String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
                                    String fullSrcPath = Path.GetFullPathInternal(sourceFileName);
            String fullDestPath = Path.GetFullPathInternal(destinationFileName);
            String fullBackupPath = null;
            if (destinationBackupFileName != null)
                fullBackupPath = Path.GetFullPathInternal(destinationBackupFileName);
            FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read | FileSecurityStateAccess.Write, sourceFileName, fullSrcPath);
            FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Read | FileSecurityStateAccess.Write, destinationFileName, fullDestPath);
            FileSecurityState backupState = new FileSecurityState(FileSecurityStateAccess.Read | FileSecurityStateAccess.Write, destinationBackupFileName, fullBackupPath);
            sourceState.EnsureState();
            destState.EnsureState();
            backupState.EnsureState();
            int flags = Win32Native.REPLACEFILE_WRITE_THROUGH;
            if (ignoreMetadataErrors)
                flags |= Win32Native.REPLACEFILE_IGNORE_MERGE_ERRORS;
            bool r = Win32Native.ReplaceFile(fullDestPath, fullSrcPath, fullBackupPath, flags, IntPtr.Zero, IntPtr.Zero);
            if (!r)
                __Error.WinIOError();
        }

        internal static int FillAttributeInfo(String path, ref Win32Native.WIN32_FILE_ATTRIBUTE_DATA data, bool tryagain, bool returnErrorOnNotFound)
        {
            int dataInitialised = 0;
            if (tryagain)
            {
                Win32Native.WIN32_FIND_DATA findData;
                findData = new Win32Native.WIN32_FIND_DATA();
                String tempPath = path.TrimEnd(new char[]{Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar});
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try
                {
                    bool error = false;
                    SafeFindHandle handle = Win32Native.FindFirstFile(tempPath, findData);
                    try
                    {
                        if (handle.IsInvalid)
                        {
                            error = true;
                            dataInitialised = Marshal.GetLastWin32Error();
                            if (dataInitialised == Win32Native.ERROR_FILE_NOT_FOUND || dataInitialised == Win32Native.ERROR_PATH_NOT_FOUND || dataInitialised == Win32Native.ERROR_NOT_READY)
                            {
                                if (!returnErrorOnNotFound)
                                {
                                    dataInitialised = 0;
                                    data.fileAttributes = -1;
                                }
                            }

                            return dataInitialised;
                        }
                    }
                    finally
                    {
                        try
                        {
                            handle.Close();
                        }
                        catch
                        {
                            if (!error)
                            {
                                                                __Error.WinIOError();
                            }
                        }
                    }
                }
                finally
                {
                    Win32Native.SetErrorMode(oldMode);
                }

                data.PopulateFrom(findData);
            }
            else
            {
                bool success = false;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try
                {
                    success = Win32Native.GetFileAttributesEx(path, GetFileExInfoStandard, ref data);
                }
                finally
                {
                    Win32Native.SetErrorMode(oldMode);
                }

                if (!success)
                {
                    dataInitialised = Marshal.GetLastWin32Error();
                    if (dataInitialised != Win32Native.ERROR_FILE_NOT_FOUND && dataInitialised != Win32Native.ERROR_PATH_NOT_FOUND && dataInitialised != Win32Native.ERROR_NOT_READY)
                    {
                        return FillAttributeInfo(path, ref data, true, returnErrorOnNotFound);
                    }
                    else
                    {
                        if (!returnErrorOnNotFound)
                        {
                            dataInitialised = 0;
                            data.fileAttributes = -1;
                        }
                    }
                }
            }

            return dataInitialised;
        }

        private static FileStream OpenFile(String path, FileAccess access, out SafeFileHandle handle)
        {
            FileStream fs = new FileStream(path, FileMode.Open, access, FileShare.ReadWrite, 1);
            handle = fs.SafeFileHandle;
            if (handle.IsInvalid)
            {
                int hr = Marshal.GetLastWin32Error();
                String FullPath = Path.GetFullPathInternal(path);
                if (hr == __Error.ERROR_PATH_NOT_FOUND && FullPath.Equals(Directory.GetDirectoryRoot(FullPath)))
                    hr = __Error.ERROR_ACCESS_DENIED;
                __Error.WinIOError(hr, path);
            }

            return fs;
        }

        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_ACCESS_DENIED = 0x5;
    }
}