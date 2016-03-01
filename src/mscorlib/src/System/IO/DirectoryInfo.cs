using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.IO
{
    public sealed class DirectoryInfo : FileSystemInfo
    {
        private String[] demandDir;
        private DirectoryInfo()
        {
        }

        public static DirectoryInfo UnsafeCreateDirectoryInfo(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            DirectoryInfo di = new DirectoryInfo();
            di.Init(path, false);
            return di;
        }

        public DirectoryInfo(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            Init(path, true);
        }

        private void Init(String path, bool checkHost)
        {
            if ((path.Length == 2) && (path[1] == ':'))
            {
                OriginalPath = ".";
            }
            else
            {
                OriginalPath = path;
            }

            String fullPath = Path.GetFullPathInternal(path);
            demandDir = new String[]{Directory.GetDemandDir(fullPath, true)};
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, OriginalPath, fullPath);
                state.EnsureState();
            }

            FullPath = fullPath;
            DisplayPath = GetDisplayName(OriginalPath, FullPath);
        }

        internal DirectoryInfo(String fullPath, bool junk)
        {
            Contract.Assert(Path.GetRootLength(fullPath) > 0, "fullPath must be fully qualified!");
            OriginalPath = Path.GetFileName(fullPath);
            FullPath = fullPath;
            DisplayPath = GetDisplayName(OriginalPath, FullPath);
            demandDir = new String[]{Directory.GetDemandDir(fullPath, true)};
        }

        private DirectoryInfo(SerializationInfo info, StreamingContext context): base (info, context)
        {
            DisplayPath = GetDisplayName(OriginalPath, FullPath);
        }

        public override String Name
        {
            get
            {
                return DisplayPath;
            }
        }

        public DirectoryInfo Parent
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String parentName;
                String s = FullPath;
                if (s.Length > 3 && s.EndsWith(Path.DirectorySeparatorChar))
                    s = FullPath.Substring(0, FullPath.Length - 1);
                parentName = Path.GetDirectoryName(s);
                if (parentName == null)
                    return null;
                DirectoryInfo dir = new DirectoryInfo(parentName, false);
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery | FileSecurityStateAccess.Read, String.Empty, dir.demandDir[0]);
                state.EnsureState();
                return dir;
            }
        }

        public DirectoryInfo CreateSubdirectory(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            return CreateSubdirectory(path, null);
        }

        public DirectoryInfo CreateSubdirectory(String path, Object directorySecurity)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            return CreateSubdirectoryHelper(path, directorySecurity);
        }

        private DirectoryInfo CreateSubdirectoryHelper(String path, Object directorySecurity)
        {
            Contract.Requires(path != null);
            String newDirs = Path.InternalCombine(FullPath, path);
            String fullPath = Path.GetFullPathInternal(newDirs);
            if (0 != String.Compare(FullPath, 0, fullPath, 0, FullPath.Length, StringComparison.OrdinalIgnoreCase))
            {
                String displayPath = __Error.GetDisplayablePath(DisplayPath, false);
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSubPath", path, displayPath));
            }

            String demandDirForCreation = Directory.GetDemandDir(fullPath, true);
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, OriginalPath, demandDirForCreation);
            state.EnsureState();
            Directory.InternalCreateDirectory(fullPath, path, directorySecurity);
            return new DirectoryInfo(fullPath);
        }

        public void Create()
        {
            Directory.InternalCreateDirectory(FullPath, OriginalPath, null, true);
        }

        public override bool Exists
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                try
                {
                    if (_dataInitialised == -1)
                        Refresh();
                    if (_dataInitialised != 0)
                        return false;
                    return _data.fileAttributes != -1 && (_data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) != 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public FileInfo[] GetFiles(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();
            return InternalGetFiles(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public FileInfo[] GetFiles(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();
            return InternalGetFiles(searchPattern, searchOption);
        }

        private FileInfo[] InternalGetFiles(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            IEnumerable<FileInfo> enble = FileSystemEnumerableFactory.CreateFileInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
            List<FileInfo> fileList = new List<FileInfo>(enble);
            return fileList.ToArray();
        }

        public FileInfo[] GetFiles()
        {
            return InternalGetFiles("*", SearchOption.TopDirectoryOnly);
        }

        public DirectoryInfo[] GetDirectories()
        {
            return InternalGetDirectories("*", SearchOption.TopDirectoryOnly);
        }

        public FileSystemInfo[] GetFileSystemInfos(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();
            return InternalGetFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public FileSystemInfo[] GetFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();
            return InternalGetFileSystemInfos(searchPattern, searchOption);
        }

        private FileSystemInfo[] InternalGetFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            IEnumerable<FileSystemInfo> enble = FileSystemEnumerableFactory.CreateFileSystemInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
            List<FileSystemInfo> fileList = new List<FileSystemInfo>(enble);
            return fileList.ToArray();
        }

        public FileSystemInfo[] GetFileSystemInfos()
        {
            return InternalGetFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        public DirectoryInfo[] GetDirectories(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();
            return InternalGetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public DirectoryInfo[] GetDirectories(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();
            return InternalGetDirectories(searchPattern, searchOption);
        }

        private DirectoryInfo[] InternalGetDirectories(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            IEnumerable<DirectoryInfo> enble = FileSystemEnumerableFactory.CreateDirectoryInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
            List<DirectoryInfo> fileList = new List<DirectoryInfo>(enble);
            return fileList.ToArray();
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories()
        {
            return InternalEnumerateDirectories("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();
            return InternalEnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<DirectoryInfo> EnumerateDirectories(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();
            return InternalEnumerateDirectories(searchPattern, searchOption);
        }

        private IEnumerable<DirectoryInfo> InternalEnumerateDirectories(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            return FileSystemEnumerableFactory.CreateDirectoryInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
        }

        public IEnumerable<FileInfo> EnumerateFiles()
        {
            return InternalEnumerateFiles("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileInfo> EnumerateFiles(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();
            return InternalEnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileInfo> EnumerateFiles(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();
            return InternalEnumerateFiles(searchPattern, searchOption);
        }

        private IEnumerable<FileInfo> InternalEnumerateFiles(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            return FileSystemEnumerableFactory.CreateFileInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos()
        {
            return InternalEnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(String searchPattern)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            Contract.EndContractBlock();
            return InternalEnumerateFileSystemInfos(searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            if (searchPattern == null)
                throw new ArgumentNullException("searchPattern");
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
                throw new ArgumentOutOfRangeException("searchOption", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();
            return InternalEnumerateFileSystemInfos(searchPattern, searchOption);
        }

        private IEnumerable<FileSystemInfo> InternalEnumerateFileSystemInfos(String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            return FileSystemEnumerableFactory.CreateFileSystemInfoIterator(FullPath, OriginalPath, searchPattern, searchOption);
        }

        public DirectoryInfo Root
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String demandPath;
                int rootLength = Path.GetRootLength(FullPath);
                String rootPath = FullPath.Substring(0, rootLength);
                demandPath = Directory.GetDemandDir(rootPath, true);
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPath);
                sourceState.EnsureState();
                return new DirectoryInfo(rootPath);
            }
        }

        public void MoveTo(String destDirName)
        {
            if (destDirName == null)
                throw new ArgumentNullException("destDirName");
            if (destDirName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destDirName");
            Contract.EndContractBlock();
            FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, DisplayPath, Directory.GetDemandDir(FullPath, true));
            sourceState.EnsureState();
            String fullDestDirName = Path.GetFullPathInternal(destDirName);
            String demandPath;
            if (!fullDestDirName.EndsWith(Path.DirectorySeparatorChar))
                fullDestDirName = fullDestDirName + Path.DirectorySeparatorChar;
            demandPath = fullDestDirName + '.';
            FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destDirName, demandPath);
            destState.EnsureState();
            String fullSourcePath;
            if (FullPath.EndsWith(Path.DirectorySeparatorChar))
                fullSourcePath = FullPath;
            else
                fullSourcePath = FullPath + Path.DirectorySeparatorChar;
            if (String.Compare(fullSourcePath, fullDestDirName, StringComparison.OrdinalIgnoreCase) == 0)
                throw new IOException(Environment.GetResourceString("IO.IO_SourceDestMustBeDifferent"));
            String sourceRoot = Path.GetPathRoot(fullSourcePath);
            String destinationRoot = Path.GetPathRoot(fullDestDirName);
            if (String.Compare(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase) != 0)
                throw new IOException(Environment.GetResourceString("IO.IO_SourceDestMustHaveSameRoot"));
            if (!Win32Native.MoveFile(FullPath, destDirName))
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND)
                {
                    hr = Win32Native.ERROR_PATH_NOT_FOUND;
                    __Error.WinIOError(hr, DisplayPath);
                }

                if (hr == Win32Native.ERROR_ACCESS_DENIED)
                    throw new IOException(Environment.GetResourceString("UnauthorizedAccess_IODenied_Path", DisplayPath));
                __Error.WinIOError(hr, String.Empty);
            }

            FullPath = fullDestDirName;
            OriginalPath = destDirName;
            DisplayPath = GetDisplayName(OriginalPath, FullPath);
            demandDir = new String[]{Directory.GetDemandDir(FullPath, true)};
            _dataInitialised = -1;
        }

        public override void Delete()
        {
            Directory.Delete(FullPath, OriginalPath, false, true);
        }

        public void Delete(bool recursive)
        {
            Directory.Delete(FullPath, OriginalPath, recursive, true);
        }

        public override String ToString()
        {
            return DisplayPath;
        }

        private static String GetDisplayName(String originalPath, String fullPath)
        {
            Contract.Assert(originalPath != null);
            Contract.Assert(fullPath != null);
            String displayName = "";
            if ((originalPath.Length == 2) && (originalPath[1] == ':'))
            {
                displayName = ".";
            }
            else
            {
                displayName = GetDirName(fullPath);
            }

            return displayName;
        }

        private static String GetDirName(String fullPath)
        {
            Contract.Assert(fullPath != null);
            String dirName = null;
            if (fullPath.Length > 3)
            {
                String s = fullPath;
                if (fullPath.EndsWith(Path.DirectorySeparatorChar))
                {
                    s = fullPath.Substring(0, fullPath.Length - 1);
                }

                dirName = Path.GetFileName(s);
            }
            else
            {
                dirName = fullPath;
            }

            return dirName;
        }
    }
}