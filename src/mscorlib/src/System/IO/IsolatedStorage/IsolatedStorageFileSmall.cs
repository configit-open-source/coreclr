using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Util;
using System.Text;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO.IsolatedStorage
{
    public sealed class IsolatedStorageFile : IDisposable
    {
        private const int s_BlockSize = 1024;
        private const int s_DirSize = s_BlockSize;
        internal const string c_DisabledFileName = "disabled.dat";
        private string m_AppFilesPath;
        private string m_GroupName;
        private FileIOAccess m_AppFilesPathAccess;
        private bool m_bDisposed;
        private bool m_closed;
        private object m_internalLock = new object ();
        private static string s_RootFromHost;
        private static string s_IsolatedStorageRoot;
        private static Lazy<IsolatedStorageFileIOHelperBase> s_IsoStoreFileIOHelper;
        static IsolatedStorageFile()
        {
            (new ReflectionPermission(PermissionState.Unrestricted)).Assert();
            s_IsoStoreFileIOHelper = new Lazy<IsolatedStorageFileIOHelperBase>(GetIsolatedStorageFileIOHelper);
        }

        internal IsolatedStorageFile()
        {
        }

        public static Int64 DefaultQuota
        {
            get
            {
                return Int64.MaxValue;
            }
        }

        public static Boolean IsEnabled
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return IsolatedStorageGroup.Enabled;
            }
        }

        public Int64 UsedSize
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return 0;
            }
        }

        public Int64 Quota
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                EnsureStoreIsValid();
                return Int64.MaxValue;
            }
        }

        public Int64 AvailableFreeSpace
        {
            [SecuritySafeCritical]
            get
            {
                IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToGetAvailableFreeSpace();
                if (s.IsStateAvailable())
                {
                    if (s.AvailableFreeSpaceComputed)
                    {
                        return s.AvailableFreeSpace;
                    }
                    else
                    {
                        return Quota - UsedSize;
                    }
                }
                else
                {
                    return Quota - UsedSize;
                }
            }
        }

        internal string RootDirectory
        {
            get
            {
                return m_AppFilesPath;
            }
        }

        internal bool Disposed
        {
            get
            {
                return m_bDisposed;
            }
        }

        internal string GroupName
        {
            get
            {
                return m_GroupName;
            }
        }

        internal static string IsolatedStorageRoot
        {
            [System.Security.SecurityCritical]
            get
            {
                if (s_IsolatedStorageRoot == null)
                {
                    s_IsolatedStorageRoot = FetchOrCreateRoot();
                }

                return s_IsolatedStorageRoot;
            }

            private set
            {
                s_IsolatedStorageRoot = value;
            }
        }

        internal bool IsDeleted
        {
            [System.Security.SecurityCritical]
            get
            {
                try
                {
                    return !Directory.UnsafeExists(IsolatedStorageRoot);
                }
                catch (IOException)
                {
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    return true;
                }
            }
        }

        public void Remove()
        {
            CleanDirectoryNoUnreserve(m_AppFilesPath);
        }

        private static bool CleanDirectoryNoUnreserve(string targetDirectory)
        {
            bool noErrors = true;
            foreach (string f in Directory.UnsafeGetFiles(targetDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.UnsafeDelete(Path.Combine(targetDirectory, f));
                }
                catch (IOException)
                {
                    noErrors = false;
                }
                catch (UnauthorizedAccessException)
                {
                    noErrors = false;
                }
            }

            foreach (string d in Directory.UnsafeGetDirectories(targetDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                if (CleanDirectoryNoUnreserve(d))
                {
                    try
                    {
                        Directory.UnsafeDelete(d, false);
                    }
                    catch (IOException)
                    {
                        noErrors = false;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        noErrors = false;
                    }
                }
                else
                {
                    noErrors = false;
                }
            }

            return noErrors;
        }

        public void Close()
        {
            lock (m_internalLock)
            {
                if (!m_closed)
                {
                    m_closed = true;
                    GC.SuppressFinalize(this);
                }
            }
        }

        public bool IncreaseQuotaTo(Int64 newQuotaSize)
        {
            if (newQuotaSize <= Quota)
            {
                throw new ArgumentException(Environment.GetResourceString("IsolatedStorage_OldQuotaLarger"));
            }

                        EnsureStoreIsValid();
            IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToIncreaseQuotaForGroup(m_GroupName, newQuotaSize, UsedSize);
            if (!s.IsStateAvailable())
            {
                return false;
            }

            return true;
        }

        public void DeleteFile(String file)
        {
            if (file == null)
                throw new ArgumentNullException("file");
                        EnsureStoreIsValid();
            try
            {
                String fullPath = GetFullPath(file);
                Demand(fullPath);
                File.UnsafeDelete(fullPath);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_DeleteFile", e);
            }
        }

        public bool FileExists(string path)
        {
            if (path == null)
            {
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    return false;
                }

                throw new ArgumentNullException("path");
            }

            EnsureStoreIsValid();
            String isPath = GetFullPath(path);
            String fullPath = Path.GetFullPathInternal(isPath);
            try
            {
                Demand(fullPath);
            }
            catch
            {
                return false;
            }

            bool ret = File.UnsafeExists(fullPath);
            return ret;
        }

        public bool DirectoryExists(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
                        EnsureStoreIsValid();
            String isPath = GetFullPath(path);
            String fullPath = Path.GetFullPathInternal(isPath);
            if (isPath.EndsWith(Path.DirectorySeparatorChar + ".", StringComparison.Ordinal))
            {
                if (fullPath.EndsWith(Path.DirectorySeparatorChar))
                {
                    fullPath += ".";
                }
                else
                {
                    fullPath += Path.DirectorySeparatorChar + ".";
                }
            }

            try
            {
                Demand(fullPath);
            }
            catch
            {
                return false;
            }

            bool ret = Directory.UnsafeExists(fullPath);
            return ret;
        }

        public void CreateDirectory(String dir)
        {
            if (dir == null)
                throw new ArgumentNullException("dir");
                        EnsureStoreIsValid();
            String isPath = GetFullPath(dir);
            String fullPath = Path.GetFullPathInternal(isPath);
            try
            {
                Demand(fullPath);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_CreateDirectory", e);
            }

            if (Directory.InternalExists(fullPath))
            {
                return;
            }

            String[] dirList = DirectoriesToCreate(fullPath);
            if (dirList == null)
            {
                return;
            }

            try
            {
                Directory.UnsafeCreateDirectory(dirList[dirList.Length - 1]);
            }
            catch (Exception e)
            {
                try
                {
                    Directory.UnsafeDelete(dirList[0], true);
                }
                catch
                {
                }

                throw GetIsolatedStorageException("IsolatedStorage_CreateDirectory", e);
            }
        }

        public void DeleteDirectory(String dir)
        {
            if (dir == null)
                throw new ArgumentNullException("dir");
                        EnsureStoreIsValid();
            try
            {
                string fullPath = GetFullPath(dir);
                Demand(fullPath);
                Directory.UnsafeDelete(fullPath, false);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_DeleteDirectory", e);
            }
        }

        public String[] GetFileNames()
        {
            return GetFileNames("*");
        }

        public String[] GetFileNames(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
                        EnsureStoreIsValid();
            String[] retVal = GetFileDirectoryNames(GetFullPath(searchPattern), searchPattern, true, this);
            return retVal;
        }

        public String[] GetDirectoryNames()
        {
            return GetDirectoryNames("*");
        }

        public String[] GetDirectoryNames(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
                        EnsureStoreIsValid();
            String[] retVal = GetFileDirectoryNames(GetFullPath(searchPattern), searchPattern, false, this);
            return retVal;
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, this);
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, access, this);
        }

        public IsolatedStorageFileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, mode, access, share, this);
        }

        public IsolatedStorageFileStream CreateFile(string path)
        {
            EnsureStoreIsValid();
            return new IsolatedStorageFileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, this);
        }

        public DateTimeOffset GetCreationTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
            }

                        EnsureStoreIsValid();
            String isPath = GetFullPath(path);
            String fullPath = Path.GetFullPathInternal(isPath);
            try
            {
                Demand(fullPath);
            }
            catch
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }

            DateTimeOffset ret = new DateTimeOffset(File.GetCreationTimeUtc(fullPath)).ToLocalTime();
            return ret;
        }

        public DateTimeOffset GetLastAccessTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
            }

                        EnsureStoreIsValid();
            String isPath = GetFullPath(path);
            String fullPath = Path.GetFullPathInternal(isPath);
            try
            {
                Demand(fullPath);
            }
            catch
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }

            DateTimeOffset ret = new DateTimeOffset(File.GetLastAccessTimeUtc(fullPath)).ToLocalTime();
            return ret;
        }

        public DateTimeOffset GetLastWriteTime(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (path == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "path");
            }

                        EnsureStoreIsValid();
            String isPath = GetFullPath(path);
            String fullPath = Path.GetFullPathInternal(isPath);
            try
            {
                Demand(fullPath);
            }
            catch
            {
                return new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero).ToLocalTime();
            }

            DateTimeOffset ret = new DateTimeOffset(File.GetLastWriteTimeUtc(fullPath)).ToLocalTime();
            return ret;
        }

        public void CopyFile(string sourceFileName, string destinationFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");
            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");
            if (sourceFileName == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
            }

            if (destinationFileName == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
            }

                        CopyFile(sourceFileName, destinationFileName, false);
        }

        public void CopyFile(string sourceFileName, string destinationFileName, bool overwrite)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");
            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");
            if (sourceFileName == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
            }

            if (destinationFileName == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
            }

                        EnsureStoreIsValid();
            String sourceFileNameFullPath = Path.GetFullPathInternal(GetFullPath(sourceFileName));
            String destinationFileNameFullPath = Path.GetFullPathInternal(GetFullPath(destinationFileName));
            try
            {
                Demand(sourceFileNameFullPath);
                Demand(destinationFileNameFullPath);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }

            try
            {
                File.UnsafeCopy(sourceFileNameFullPath, destinationFileNameFullPath, overwrite);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", sourceFileName));
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }
        }

        public void MoveFile(string sourceFileName, string destinationFileName)
        {
            if (sourceFileName == null)
                throw new ArgumentNullException("sourceFileName");
            if (destinationFileName == null)
                throw new ArgumentNullException("destinationFileName");
            if (sourceFileName == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceFileName");
            }

            if (destinationFileName == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationFileName");
            }

                        EnsureStoreIsValid();
            String sourceFileNameFullPath = Path.GetFullPathInternal(GetFullPath(sourceFileName));
            String destinationFileNameFullPath = Path.GetFullPathInternal(GetFullPath(destinationFileName));
            try
            {
                Demand(sourceFileNameFullPath);
                Demand(destinationFileNameFullPath);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }

            try
            {
                s_IsoStoreFileIOHelper.Value.UnsafeMoveFile(sourceFileNameFullPath, destinationFileNameFullPath);
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", sourceFileName));
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }
        }

        public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName)
        {
            if (sourceDirectoryName == null)
                throw new ArgumentNullException("sourceDirectoryName");
            if (destinationDirectoryName == null)
                throw new ArgumentNullException("destinationDirectoryName");
            if (sourceDirectoryName == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "sourceDirectoryName");
            }

            if (destinationDirectoryName == String.Empty)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"), "destinationDirectoryName");
            }

                        EnsureStoreIsValid();
            String sourceDirectoryNameFullPath = Path.GetFullPathInternal(GetFullPath(sourceDirectoryName));
            String destinationDirectoryNameFullPath = Path.GetFullPathInternal(GetFullPath(destinationDirectoryName));
            try
            {
                Demand(sourceDirectoryNameFullPath);
                Demand(destinationDirectoryNameFullPath);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }

            try
            {
                Directory.UnsafeMove(sourceDirectoryNameFullPath, destinationDirectoryNameFullPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new DirectoryNotFoundException(Environment.GetResourceString("IO.PathNotFound_Path", sourceDirectoryName));
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }
        }

        public static IsolatedStorageFile GetUserStoreForApplication()
        {
            IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToGetGroupAndIdForApplication();
            s.EnsureState();
            return GetUserStore(s.Group, s.Id);
        }

        internal static IsolatedStorageFile GetUserStore(string group, string id)
        {
            IsolatedStorageRoot = FetchOrCreateRoot();
            if (!IsolatedStorageGroup.Enabled)
            {
                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Init"));
            }

            IsolatedStorageFile isf = new IsolatedStorageFile();
            isf.m_GroupName = group;
            isf.m_AppFilesPath = IsolatedStorageRoot;
            isf.m_AppFilesPathAccess = FileIOAccessFromPath(isf.m_AppFilesPath);
            return isf;
        }

        public static IsolatedStorageFile GetUserStoreForSite()
        {
            return GetUserStoreForApplication();
        }

        internal string GetFullPath(string partialPath)
        {
                        int i;
            for (i = 0; i < partialPath.Length; i++)
            {
                if (partialPath[i] != Path.DirectorySeparatorChar && partialPath[i] != Path.AltDirectorySeparatorChar)
                {
                    break;
                }
            }

            partialPath = partialPath.Substring(i);
            return Path.Combine(m_AppFilesPath, partialPath);
        }

        private static void CreatePathPrefixIfNeeded(string path)
        {
            string root = Path.GetPathRoot(path);
                        try
            {
                if (!Directory.UnsafeExists(path))
                {
                    Directory.UnsafeCreateDirectory(path);
                }
            }
            catch (IOException e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }
            catch (UnauthorizedAccessException e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
            }
        }

        internal static string FetchOrCreateRoot()
        {
            string rootFromHost = s_RootFromHost;
            if (s_RootFromHost == null)
            {
                IsolatedStorageSecurityState s = IsolatedStorageSecurityState.CreateStateToGetRootUserDirectory();
                s.EnsureState();
                string root = s.RootUserDirectory;
                IsolatedStorageSecurityState s2 = IsolatedStorageSecurityState.CreateStateForIsolatedStorageFolderName();
                if (s2.IsStateAvailable())
                {
                    if (s2.IsolatedStorageFolderName != null)
                    {
                        root = Path.Combine(root, s2.IsolatedStorageFolderName);
                    }
                    else
                    {
                        root = Path.Combine(root, "IsolatedStore");
                    }
                }
                else
                {
                    root = Path.Combine(root, "IsolatedStore");
                }

                s_RootFromHost = root;
            }

            CreatePathPrefixIfNeeded(s_RootFromHost);
            return s_RootFromHost;
        }

        internal static string CreateRandomDirectory(String rootDir)
        {
            string rndName;
            string dirToCreate;
            do
            {
                rndName = Path.Combine(Path.GetRandomFileName(), Path.GetRandomFileName());
                dirToCreate = Path.Combine(rootDir, rndName);
            }
            while (Directory.UnsafeExists(dirToCreate));
            try
            {
                Directory.UnsafeCreateDirectory(dirToCreate);
            }
            catch (Exception e)
            {
                throw GetIsolatedStorageException("IsolatedStorage_Init", e);
            }

            return rndName;
        }

        internal static string GetRandomDirectory(String rootDir)
        {
            String[] nodes1 = GetFileDirectoryNames(Path.Combine(rootDir, "*"), "*", false, null);
            for (int i = 0; i < nodes1.Length; ++i)
            {
                if (nodes1[i].Length == 12)
                {
                    String[] nodes2 = GetFileDirectoryNames(Path.Combine(Path.Combine(rootDir, nodes1[i]), "*"), "*", false, null);
                    for (int j = 0; j < nodes2.Length; ++j)
                    {
                        if (nodes2[j].Length == 12)
                        {
                            return (Path.Combine(nodes1[i], nodes2[j]));
                        }
                    }
                }
            }

            return null;
        }

        internal static void TouchFile(string pathToFile)
        {
            using (FileStream fs = new FileStream(pathToFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, FileStream.DefaultBufferSize, false))
            {
            }
        }

        internal void EnsureStoreIsValid()
        {
            if (Disposed)
                throw new ObjectDisposedException(null, Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
                        if (IsDeleted)
            {
                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
            }

            if (m_closed)
                throw new InvalidOperationException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
            if (!IsolatedStorageGroup.Enabled)
            {
                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_StoreNotOpen"));
            }
        }

        internal static ulong RoundToBlockSize(ulong num)
        {
            if (num < s_BlockSize)
                return s_BlockSize;
            ulong rem = (num % s_BlockSize);
            if (rem != 0)
                num += (s_BlockSize - rem);
            return num;
        }

        internal static ulong RoundToBlockSizeFloor(ulong num)
        {
            if (num < s_BlockSize)
                return 0;
            ulong rem = (num % s_BlockSize);
            num -= rem;
            return num;
        }

        private String[] DirectoriesToCreate(String fullPath)
        {
                        List<String> list = new List<String>();
            int length = fullPath.Length;
            if (length >= 2 && fullPath[length - 1] == Path.DirectorySeparatorChar)
                length--;
            int i = Path.GetRootLength(fullPath);
            while (i < length)
            {
                i++;
                while (i < length && fullPath[i] != Path.DirectorySeparatorChar)
                    i++;
                String currDir = fullPath.Substring(0, i);
                if (!Directory.InternalExists(currDir))
                {
                    list.Add(currDir);
                }
            }

            if (list.Count != 0)
            {
                return list.ToArray();
            }

            return null;
        }

        private static FileIOAccess FileIOAccessFromPath(string fullPath)
        {
            FileIOAccess access = new FileIOAccess();
            fullPath = fullPath.Replace("~", "");
            ArrayList expressions = StringExpressionSet.CreateListFromExpressions(new string[]{fullPath}, true);
            access.AddExpressions(expressions, false);
            return access;
        }

        internal void Demand(String pathToDemand)
        {
            try
            {
                FileIOAccess target = FileIOAccessFromPath(pathToDemand);
                if (!target.IsSubsetOf(m_AppFilesPathAccess))
                {
                    throw new SecurityException();
                }
            }
            catch (Exception)
            {
                throw new SecurityException();
            }
        }

        internal static String GetHash(String s)
        {
            byte[] preHash = (new System.Security.Cryptography.SHA256Managed()).ComputeHash(Encoding.Unicode.GetBytes(s));
            if (preHash.Length % 5 != 0)
            {
                byte[] b = new byte[preHash.Length + (5 - (preHash.Length % 5))];
                for (int i = 0; i < preHash.Length; i++)
                {
                    b[i] = preHash[i];
                }

                preHash = b;
            }

            return Path.ToBase32StringSuitableForDirName(preHash);
        }

        private static String[] GetFileDirectoryNames(String path, String msg, bool file, IsolatedStorageFile isf)
        {
            int hr;
            if (path == null)
                throw new ArgumentNullException("path", Environment.GetResourceString("ArgumentNull_Path"));
                        bool fEndsWithDirectory = false;
            char lastChar = path[path.Length - 1];
            if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar || lastChar == '.')
                fEndsWithDirectory = true;
            String fullPath = Path.GetFullPathInternal(path);
            if ((fEndsWithDirectory) && (fullPath[fullPath.Length - 1] != lastChar))
                fullPath += "\\*";
            String dir = Path.GetDirectoryName(fullPath);
            if (dir != null)
                dir += "\\";
            if (isf != null)
            {
                try
                {
                    isf.Demand(dir == null ? fullPath : dir);
                }
                catch (Exception e)
                {
                    throw GetIsolatedStorageException("IsolatedStorage_Operation", e);
                }
            }

            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhoneMango)
            {
                fullPath = Path.Combine(Path.GetDirectoryName(fullPath), "*");
            }

            String[] list = new String[10];
            int listSize = 0;
            Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();
            SafeFindHandle hnd = Win32Native.FindFirstFile(fullPath, data);
            if (hnd.IsInvalid)
            {
                hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND)
                    return new String[0];
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && hr == Win32Native.ERROR_PATH_NOT_FOUND)
                    __Error.WinIOError(hr, msg);
                throw GetIsolatedStorageException("IsolatedStorage_Operation", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1)));
            }

            int numEntries = 0;
            do
            {
                bool includeThis;
                if (file)
                    includeThis = (0 == (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY));
                else
                {
                    includeThis = (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY));
                    if (includeThis && (data.cFileName.Equals(".") || data.cFileName.Equals("..")))
                        includeThis = false;
                }

                if (includeThis)
                {
                    numEntries++;
                    if (listSize == list.Length)
                    {
                        String[] newList = new String[list.Length * 2];
                        Array.Copy(list, 0, newList, 0, listSize);
                        list = newList;
                    }

                    list[listSize++] = data.cFileName;
                }
            }
            while (Win32Native.FindNextFile(hnd, data));
            hr = Marshal.GetLastWin32Error();
            hnd.Close();
            if (hr != 0 && hr != Win32Native.ERROR_NO_MORE_FILES)
                __Error.WinIOError(hr, msg);
            if (!file && numEntries == 1 && (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY)))
            {
                String[] sa = new String[1];
                sa[0] = data.cFileName;
                return sa;
            }

            if (listSize == list.Length)
                return list;
            String[] items = new String[listSize];
            Array.Copy(list, 0, items, 0, listSize);
            return items;
        }

        public void Dispose()
        {
            Close();
            m_bDisposed = true;
        }

        internal static Exception GetIsolatedStorageException(string exceptionKey, Exception rootCause)
        {
            IsolatedStorageException e = new IsolatedStorageException(Environment.GetResourceString(exceptionKey), rootCause);
            e.m_UnderlyingException = rootCause;
            return e;
        }

        internal static IsolatedStorageFileIOHelperBase GetIsolatedStorageFileIOHelper()
        {
            Type WinRTResourceManagerType = Type.GetType("System.IO.IsolatedStorage.IsolatedStorageFileIOHelper, " + AssemblyRef.SystemRuntimeWindowsRuntime, true);
            return (IsolatedStorageFileIOHelperBase)Activator.CreateInstance(WinRTResourceManagerType, true);
        }
    }

    public class IsolatedStorageFileIOHelperBase
    {
        public virtual void UnsafeMoveFile(string sourceFileName, string destinationFileName)
        {
        }
    }
}