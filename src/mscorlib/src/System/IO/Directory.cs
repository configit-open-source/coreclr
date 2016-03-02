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
    public static class Directory
    {
        public static DirectoryInfo GetParent(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"), "path");
                        String fullPath = Path.GetFullPathInternal(path);
            String s = Path.GetDirectoryName(fullPath);
            if (s == null)
                return null;
            return new DirectoryInfo(s);
        }

        public static DirectoryInfo CreateDirectory(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
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

            return InternalCreateDirectoryHelper(path, true);
        }

        internal static DirectoryInfo UnsafeCreateDirectory(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
                        return InternalCreateDirectoryHelper(path, false);
        }

        internal static DirectoryInfo InternalCreateDirectoryHelper(String path, bool checkHost)
        {
                                    String fullPath = Path.GetFullPathInternal(path);
            String demandDir = GetDemandDir(fullPath, true);
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, demandDir);
                state.EnsureState();
            }

            InternalCreateDirectory(fullPath, path, null, checkHost);
            return new DirectoryInfo(fullPath, false);
        }

        internal static String GetDemandDir(string fullPath, bool thisDirOnly)
        {
            String demandPath;
            if (thisDirOnly)
            {
                if (fullPath.EndsWith(Path.DirectorySeparatorChar) || fullPath.EndsWith(Path.AltDirectorySeparatorChar))
                    demandPath = fullPath + ".";
                else
                    demandPath = fullPath + Path.DirectorySeparatorCharAsString + ".";
            }
            else
            {
                if (!(fullPath.EndsWith(Path.DirectorySeparatorChar) || fullPath.EndsWith(Path.AltDirectorySeparatorChar)))
                    demandPath = fullPath + Path.DirectorySeparatorCharAsString;
                else
                    demandPath = fullPath;
            }

            return demandPath;
        }

        internal static void InternalCreateDirectory(String fullPath, String path, Object dirSecurityObj)
        {
            InternalCreateDirectory(fullPath, path, dirSecurityObj, false);
        }

        internal unsafe static void InternalCreateDirectory(String fullPath, String path, Object dirSecurityObj, bool checkHost)
        {
            int length = fullPath.Length;
            if (length >= 2 && Path.IsDirectorySeparator(fullPath[length - 1]))
                length--;
            int lengthRoot = Path.GetRootLength(fullPath);
            if (length == 2 && Path.IsDirectorySeparator(fullPath[1]))
                throw new IOException(Environment.GetResourceString("IO.IO_CannotCreateDirectory", path));
            if (InternalExists(fullPath))
            {
                return;
            }

            List<string> stackDir = new List<string>();
            bool somepathexists = false;
            if (length > lengthRoot)
            {
                int i = length - 1;
                while (i >= lengthRoot && !somepathexists)
                {
                    String dir = fullPath.Substring(0, i + 1);
                    if (!InternalExists(dir))
                        stackDir.Add(dir);
                    else
                        somepathexists = true;
                    while (i > lengthRoot && fullPath[i] != Path.DirectorySeparatorChar && fullPath[i] != Path.AltDirectorySeparatorChar)
                        i--;
                    i--;
                }
            }

            int count = stackDir.Count;
            if (stackDir.Count != 0)
            {
                String[] securityList = new String[stackDir.Count];
                stackDir.CopyTo(securityList, 0);
                for (int j = 0; j < securityList.Length; j++)
                    securityList[j] += "\\.";
                if (checkHost)
                {
                    foreach (String demandPath in securityList)
                    {
                        FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, String.Empty, demandPath);
                        state.EnsureState();
                    }
                }
            }

            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
            bool r = true;
            int firstError = 0;
            String errorString = path;
            while (stackDir.Count > 0)
            {
                String name = stackDir[stackDir.Count - 1];
                stackDir.RemoveAt(stackDir.Count - 1);
                if (name.Length >= Path.MAX_DIRECTORY_PATH)
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                r = Win32Native.CreateDirectory(name, secAttrs);
                if (!r && (firstError == 0))
                {
                    int currentError = Marshal.GetLastWin32Error();
                    if (currentError != Win32Native.ERROR_ALREADY_EXISTS)
                        firstError = currentError;
                    else
                    {
                        if (File.InternalExists(name) || (!InternalExists(name, out currentError) && currentError == Win32Native.ERROR_ACCESS_DENIED))
                        {
                            firstError = currentError;
                            try
                            {
                                if (checkHost)
                                {
                                    FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, GetDemandDir(name, true));
                                    state.EnsureState();
                                }

                                errorString = name;
                            }
                            catch (SecurityException)
                            {
                            }
                        }
                    }
                }
            }

            if ((count == 0) && !somepathexists)
            {
                String root = InternalGetDirectoryRoot(fullPath);
                if (!InternalExists(root))
                {
                    __Error.WinIOError(Win32Native.ERROR_PATH_NOT_FOUND, InternalGetDirectoryRoot(path));
                }

                return;
            }

            if (!r && (firstError != 0))
            {
                __Error.WinIOError(firstError, errorString);
            }
        }

        public static bool Exists(String path)
        {
            return InternalExistsHelper(path, true);
        }

        internal static bool UnsafeExists(String path)
        {
            return InternalExistsHelper(path, false);
        }

        internal static bool InternalExistsHelper(String path, bool checkHost)
        {
            try
            {
                if (path == null)
                    return false;
                if (path.Length == 0)
                    return false;
                String fullPath = Path.GetFullPathInternal(path);
                String demandPath = GetDemandDir(fullPath, true);
                if (checkHost)
                {
                    FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, path, demandPath);
                    state.EnsureState();
                }

                return InternalExists(fullPath);
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
            int lastError = Win32Native.ERROR_SUCCESS;
            return InternalExists(path, out lastError);
        }

        internal static bool InternalExists(String path, out int lastError)
        {
            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            lastError = File.FillAttributeInfo(path, ref data, false, true);
            return (lastError == 0) && (data.fileAttributes != -1) && ((data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) != 0);
        }

        public static void SetCreationTime(String path, DateTime creationTime)
        {
            SetCreationTimeUtc(path, creationTime.ToUniversalTime());
        }

        public unsafe static void SetCreationTimeUtc(String path, DateTime creationTimeUtc)
        {
            using (SafeFileHandle handle = Directory.OpenHandle(path))
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
            return File.GetCreationTime(path);
        }

        public static DateTime GetCreationTimeUtc(String path)
        {
            return File.GetCreationTimeUtc(path);
        }

        public static void SetLastWriteTime(String path, DateTime lastWriteTime)
        {
            SetLastWriteTimeUtc(path, lastWriteTime.ToUniversalTime());
        }

        public unsafe static void SetLastWriteTimeUtc(String path, DateTime lastWriteTimeUtc)
        {
            using (SafeFileHandle handle = Directory.OpenHandle(path))
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
            return File.GetLastWriteTime(path);
        }

        public static DateTime GetLastWriteTimeUtc(String path)
        {
            return File.GetLastWriteTimeUtc(path);
        }

        public static void SetLastAccessTime(String path, DateTime lastAccessTime)
        {
            SetLastAccessTimeUtc(path, lastAccessTime.ToUniversalTime());
        }

        public unsafe static void SetLastAccessTimeUtc(String path, DateTime lastAccessTimeUtc)
        {
            using (SafeFileHandle handle = Directory.OpenHandle(path))
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
            return File.GetLastAccessTime(path);
        }

        public static DateTime GetLastAccessTimeUtc(String path)
        {
            return File.GetLastAccessTimeUtc(path);
        }

        public static String[] GetFiles(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                                    return InternalGetFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static String[] GetFiles(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
                                    return InternalGetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static String[] GetFiles(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                                    return InternalGetFiles(path, searchPattern, searchOption);
        }

        private static String[] InternalGetFiles(String path, String searchPattern, SearchOption searchOption)
        {
                                                return InternalGetFileDirectoryNames(path, path, searchPattern, true, false, searchOption, true);
        }

        internal static String[] UnsafeGetFiles(String path, String searchPattern, SearchOption searchOption)
        {
                                                return InternalGetFileDirectoryNames(path, path, searchPattern, true, false, searchOption, false);
        }

        public static String[] GetDirectories(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                                    return InternalGetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static String[] GetDirectories(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
                                    return InternalGetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static String[] GetDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                                    return InternalGetDirectories(path, searchPattern, searchOption);
        }

        private static String[] InternalGetDirectories(String path, String searchPattern, SearchOption searchOption)
        {
                                                            return InternalGetFileDirectoryNames(path, path, searchPattern, false, true, searchOption, true);
        }

        internal static String[] UnsafeGetDirectories(String path, String searchPattern, SearchOption searchOption)
        {
                                                            return InternalGetFileDirectoryNames(path, path, searchPattern, false, true, searchOption, false);
        }

        public static String[] GetFileSystemEntries(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                                    return InternalGetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static String[] GetFileSystemEntries(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
                                    return InternalGetFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static String[] GetFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                                    return InternalGetFileSystemEntries(path, searchPattern, searchOption);
        }

        private static String[] InternalGetFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
                                                return InternalGetFileDirectoryNames(path, path, searchPattern, true, true, searchOption, true);
        }

        internal sealed class SearchData
        {
            public SearchData(String fullPath, String userPath, SearchOption searchOption)
            {
                                                                this.fullPath = fullPath;
                this.userPath = userPath;
                this.searchOption = searchOption;
            }

            public readonly string fullPath;
            public readonly string userPath;
            public readonly SearchOption searchOption;
        }

        internal static String[] InternalGetFileDirectoryNames(String path, String userPathOriginal, String searchPattern, bool includeFiles, bool includeDirs, SearchOption searchOption, bool checkHost)
        {
                                                            IEnumerable<String> enble = FileSystemEnumerableFactory.CreateFileNameIterator(path, userPathOriginal, searchPattern, includeFiles, includeDirs, searchOption, checkHost);
            List<String> fileList = new List<String>(enble);
            return fileList.ToArray();
        }

        public static IEnumerable<String> EnumerateDirectories(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        return InternalEnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateDirectories(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
                        return InternalEnumerateDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateDirectories(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                        return InternalEnumerateDirectories(path, searchPattern, searchOption);
        }

        private static IEnumerable<String> InternalEnumerateDirectories(String path, String searchPattern, SearchOption searchOption)
        {
                                                return EnumerateFileSystemNames(path, searchPattern, searchOption, false, true);
        }

        public static IEnumerable<String> EnumerateFiles(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                                    return InternalEnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFiles(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
                                    return InternalEnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFiles(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                                    return InternalEnumerateFiles(path, searchPattern, searchOption);
        }

        private static IEnumerable<String> InternalEnumerateFiles(String path, String searchPattern, SearchOption searchOption)
        {
                                                            return EnumerateFileSystemNames(path, searchPattern, searchOption, true, false);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                                    return InternalEnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(String path, String searchPattern)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
                                    return InternalEnumerateFileSystemEntries(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public static IEnumerable<String> EnumerateFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
                                    return InternalEnumerateFileSystemEntries(path, searchPattern, searchOption);
        }

        private static IEnumerable<String> InternalEnumerateFileSystemEntries(String path, String searchPattern, SearchOption searchOption)
        {
                                                            return EnumerateFileSystemNames(path, searchPattern, searchOption, true, true);
        }

        private static IEnumerable<String> EnumerateFileSystemNames(String path, String searchPattern, SearchOption searchOption, bool includeFiles, bool includeDirs)
        {
                                                            return FileSystemEnumerableFactory.CreateFileNameIterator(path, path, searchPattern, includeFiles, includeDirs, searchOption, true);
        }

        public static String[] GetLogicalDrives()
        {
                        new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            int drives = Win32Native.GetLogicalDrives();
            if (drives == 0)
                __Error.WinIOError();
            uint d = (uint)drives;
            int count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0)
                    count++;
                d >>= 1;
            }

            String[] result = new String[count];
            char[] root = new char[]{'A', ':', '\\'};
            d = (uint)drives;
            count = 0;
            while (d != 0)
            {
                if (((int)d & 1) != 0)
                {
                    result[count++] = new String(root);
                }

                d >>= 1;
                root[0]++;
            }

            return result;
        }

        public static String GetDirectoryRoot(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        String fullPath = Path.GetFullPathInternal(path);
            String root = fullPath.Substring(0, Path.GetRootLength(fullPath));
            String demandPath = GetDemandDir(root, true);
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, path, demandPath);
            state.EnsureState();
            return root;
        }

        internal static String InternalGetDirectoryRoot(String path)
        {
            if (path == null)
                return null;
            return path.Substring(0, Path.GetRootLength(path));
        }

        public static String GetCurrentDirectory()
        {
            return InternalGetCurrentDirectory(true);
        }

        internal static String UnsafeGetCurrentDirectory()
        {
            return InternalGetCurrentDirectory(false);
        }

        private static String InternalGetCurrentDirectory(bool checkHost)
        {
            StringBuilder sb = StringBuilderCache.Acquire(Path.MaxPath + 1);
            if (Win32Native.GetCurrentDirectory(sb.Capacity, sb) == 0)
                __Error.WinIOError();
            String currentDirectory = sb.ToString();
            if (currentDirectory.IndexOf('~') >= 0)
            {
                int r = Win32Native.GetLongPathName(currentDirectory, sb, sb.Capacity);
                if (r == 0 || r >= Path.MaxPath)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (r >= Path.MaxPath)
                        errorCode = Win32Native.ERROR_FILENAME_EXCED_RANGE;
                    if (errorCode != Win32Native.ERROR_FILE_NOT_FOUND && errorCode != Win32Native.ERROR_PATH_NOT_FOUND && errorCode != Win32Native.ERROR_INVALID_FUNCTION && errorCode != Win32Native.ERROR_ACCESS_DENIED)
                        __Error.WinIOError(errorCode, String.Empty);
                }

                currentDirectory = sb.ToString();
            }

            StringBuilderCache.Release(sb);
            String demandPath = GetDemandDir(currentDirectory, true);
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPath);
                state.EnsureState();
            }

            return currentDirectory;
        }

        public static void SetCurrentDirectory(String path)
        {
            if (path == null)
                throw new ArgumentNullException("value");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"));
                        if (path.Length >= Path.MaxPath)
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            String fulldestDirName = Path.GetFullPathInternal(path);
            if (!Win32Native.SetCurrentDirectory(fulldestDirName))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Win32Native.ERROR_FILE_NOT_FOUND)
                    errorCode = Win32Native.ERROR_PATH_NOT_FOUND;
                __Error.WinIOError(errorCode, fulldestDirName);
            }
        }

        public static void Move(String sourceDirName, String destDirName)
        {
            InternalMove(sourceDirName, destDirName, true);
        }

        internal static void UnsafeMove(String sourceDirName, String destDirName)
        {
            InternalMove(sourceDirName, destDirName, false);
        }

        private static void InternalMove(String sourceDirName, String destDirName, bool checkHost)
        {
            if (sourceDirName == null)
                throw new ArgumentNullException("sourceDirName");
            if (sourceDirName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "sourceDirName");
            if (destDirName == null)
                throw new ArgumentNullException("destDirName");
            if (destDirName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destDirName");
                        String fullsourceDirName = Path.GetFullPathInternal(sourceDirName);
            String sourcePath = GetDemandDir(fullsourceDirName, false);
            if (sourcePath.Length >= Path.MAX_DIRECTORY_PATH)
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            String fulldestDirName = Path.GetFullPathInternal(destDirName);
            String destPath = GetDemandDir(fulldestDirName, false);
            if (destPath.Length >= Path.MAX_DIRECTORY_PATH)
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            if (checkHost)
            {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, sourceDirName, sourcePath);
                FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destDirName, destPath);
                sourceState.EnsureState();
                destState.EnsureState();
            }

            if (String.Compare(sourcePath, destPath, StringComparison.OrdinalIgnoreCase) == 0)
                throw new IOException(Environment.GetResourceString("IO.IO_SourceDestMustBeDifferent"));
            String sourceRoot = Path.GetPathRoot(sourcePath);
            String destinationRoot = Path.GetPathRoot(destPath);
            if (String.Compare(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase) != 0)
                throw new IOException(Environment.GetResourceString("IO.IO_SourceDestMustHaveSameRoot"));
            if (!Win32Native.MoveFile(sourceDirName, destDirName))
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND)
                {
                    hr = Win32Native.ERROR_PATH_NOT_FOUND;
                    __Error.WinIOError(hr, fullsourceDirName);
                }

                if (hr == Win32Native.ERROR_ACCESS_DENIED)
                    throw new IOException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", sourceDirName), Win32Native.MakeHRFromErrorCode(hr));
                __Error.WinIOError(hr, String.Empty);
            }
        }

        public static void Delete(String path)
        {
            String fullPath = Path.GetFullPathInternal(path);
            Delete(fullPath, path, false, true);
        }

        public static void Delete(String path, bool recursive)
        {
            String fullPath = Path.GetFullPathInternal(path);
            Delete(fullPath, path, recursive, true);
        }

        internal static void UnsafeDelete(String path, bool recursive)
        {
            String fullPath = Path.GetFullPathInternal(path);
            Delete(fullPath, path, recursive, false);
        }

        internal static void Delete(String fullPath, String userPath, bool recursive, bool checkHost)
        {
            String demandPath;
            demandPath = GetDemandDir(fullPath, !recursive);
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, userPath, demandPath);
                state.EnsureState();
            }

            Win32Native.WIN32_FILE_ATTRIBUTE_DATA data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            int dataInitialised = File.FillAttributeInfo(fullPath, ref data, false, true);
            if (dataInitialised != 0)
            {
                if (dataInitialised == Win32Native.ERROR_FILE_NOT_FOUND)
                    dataInitialised = Win32Native.ERROR_PATH_NOT_FOUND;
                __Error.WinIOError(dataInitialised, fullPath);
            }

            if (((FileAttributes)data.fileAttributes & FileAttributes.ReparsePoint) != 0)
                recursive = false;
            DeleteHelper(fullPath, userPath, recursive, true);
        }

        private static void DeleteHelper(String fullPath, String userPath, bool recursive, bool throwOnTopLevelDirectoryNotFound)
        {
            bool r;
            int hr;
            Exception ex = null;
            if (recursive)
            {
                Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();
                using (SafeFindHandle hnd = Win32Native.FindFirstFile(fullPath + Path.DirectorySeparatorCharAsString + "*", data))
                {
                    if (hnd.IsInvalid)
                    {
                        hr = Marshal.GetLastWin32Error();
                        __Error.WinIOError(hr, fullPath);
                    }

                    do
                    {
                        bool isDir = (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY));
                        if (isDir)
                        {
                            if (data.cFileName.Equals(".") || data.cFileName.Equals(".."))
                                continue;
                            bool shouldRecurse = (0 == (data.dwFileAttributes & (int)FileAttributes.ReparsePoint));
                            if (shouldRecurse)
                            {
                                String newFullPath = Path.InternalCombine(fullPath, data.cFileName);
                                String newUserPath = Path.InternalCombine(userPath, data.cFileName);
                                try
                                {
                                    DeleteHelper(newFullPath, newUserPath, recursive, false);
                                }
                                catch (Exception e)
                                {
                                    if (ex == null)
                                    {
                                        ex = e;
                                    }
                                }
                            }
                            else
                            {
                                if (data.dwReserved0 == Win32Native.IO_REPARSE_TAG_MOUNT_POINT)
                                {
                                    String mountPoint = Path.InternalCombine(fullPath, data.cFileName + Path.DirectorySeparatorChar);
                                    r = Win32Native.DeleteVolumeMountPoint(mountPoint);
                                    if (!r)
                                    {
                                        hr = Marshal.GetLastWin32Error();
                                        if (hr != Win32Native.ERROR_PATH_NOT_FOUND)
                                        {
                                            try
                                            {
                                                __Error.WinIOError(hr, data.cFileName);
                                            }
                                            catch (Exception e)
                                            {
                                                if (ex == null)
                                                {
                                                    ex = e;
                                                }
                                            }
                                        }
                                    }
                                }

                                String reparsePoint = Path.InternalCombine(fullPath, data.cFileName);
                                r = Win32Native.RemoveDirectory(reparsePoint);
                                if (!r)
                                {
                                    hr = Marshal.GetLastWin32Error();
                                    if (hr != Win32Native.ERROR_PATH_NOT_FOUND)
                                    {
                                        try
                                        {
                                            __Error.WinIOError(hr, data.cFileName);
                                        }
                                        catch (Exception e)
                                        {
                                            if (ex == null)
                                            {
                                                ex = e;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            String fileName = Path.InternalCombine(fullPath, data.cFileName);
                            r = Win32Native.DeleteFile(fileName);
                            if (!r)
                            {
                                hr = Marshal.GetLastWin32Error();
                                if (hr != Win32Native.ERROR_FILE_NOT_FOUND)
                                {
                                    try
                                    {
                                        __Error.WinIOError(hr, data.cFileName);
                                    }
                                    catch (Exception e)
                                    {
                                        if (ex == null)
                                        {
                                            ex = e;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    while (Win32Native.FindNextFile(hnd, data));
                    hr = Marshal.GetLastWin32Error();
                }

                if (ex != null)
                    throw ex;
                if (hr != 0 && hr != Win32Native.ERROR_NO_MORE_FILES)
                    __Error.WinIOError(hr, userPath);
            }

            r = Win32Native.RemoveDirectory(fullPath);
            if (!r)
            {
                hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND)
                    hr = Win32Native.ERROR_PATH_NOT_FOUND;
                if (hr == Win32Native.ERROR_ACCESS_DENIED)
                    throw new IOException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", userPath));
                if (hr == Win32Native.ERROR_PATH_NOT_FOUND && !throwOnTopLevelDirectoryNotFound)
                    return;
                __Error.WinIOError(hr, fullPath);
            }
        }

        private static SafeFileHandle OpenHandle(String path)
        {
            String fullPath = Path.GetFullPathInternal(path);
            String root = Path.GetPathRoot(fullPath);
            if (root == fullPath && root[1] == Path.VolumeSeparatorChar)
                throw new ArgumentException(Environment.GetResourceString("Arg_PathIsVolume"));
            SafeFileHandle handle = Win32Native.SafeCreateFile(fullPath, GENERIC_WRITE, (FileShare)(FILE_SHARE_WRITE | FILE_SHARE_DELETE), null, FileMode.Open, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                int hr = Marshal.GetLastWin32Error();
                __Error.WinIOError(hr, fullPath);
            }

            return handle;
        }

        private const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const int GENERIC_WRITE = unchecked ((int)0x40000000);
        private const int FILE_SHARE_WRITE = 0x00000002;
        private const int FILE_SHARE_DELETE = 0x00000004;
        private const int OPEN_EXISTING = 0x00000003;
        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    }
}