using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal static class FileSystemEnumerableFactory
    {
        internal static IEnumerable<String> CreateFileNameIterator(String path, String originalUserPath, String searchPattern, bool includeFiles, bool includeDirs, SearchOption searchOption, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);
            SearchResultHandler<String> handler = new StringResultHandler(includeFiles, includeDirs);
            return new FileSystemEnumerableIterator<String>(path, originalUserPath, searchPattern, searchOption, handler, checkHost);
        }

        internal static IEnumerable<FileInfo> CreateFileInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);
            SearchResultHandler<FileInfo> handler = new FileInfoResultHandler();
            return new FileSystemEnumerableIterator<FileInfo>(path, originalUserPath, searchPattern, searchOption, handler, true);
        }

        internal static IEnumerable<DirectoryInfo> CreateDirectoryInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);
            SearchResultHandler<DirectoryInfo> handler = new DirectoryInfoResultHandler();
            return new FileSystemEnumerableIterator<DirectoryInfo>(path, originalUserPath, searchPattern, searchOption, handler, true);
        }

        internal static IEnumerable<FileSystemInfo> CreateFileSystemInfoIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);
            SearchResultHandler<FileSystemInfo> handler = new FileSystemInfoResultHandler();
            return new FileSystemEnumerableIterator<FileSystemInfo>(path, originalUserPath, searchPattern, searchOption, handler, true);
        }
    }

    abstract internal class Iterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
    {
        int threadId;
        internal int state;
        internal TSource current;
        public Iterator()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
        }

        public TSource Current
        {
            get
            {
                return current;
            }
        }

        protected abstract Iterator<TSource> Clone();
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            current = default (TSource);
            state = -1;
        }

        public IEnumerator<TSource> GetEnumerator()
        {
            if (threadId == Thread.CurrentThread.ManagedThreadId && state == 0)
            {
                state = 1;
                return this;
            }

            Iterator<TSource> duplicate = Clone();
            duplicate.state = 1;
            return duplicate;
        }

        public abstract bool MoveNext();
        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }

    internal class FileSystemEnumerableIterator<TSource> : Iterator<TSource>
    {
        private const int STATE_INIT = 1;
        private const int STATE_SEARCH_NEXT_DIR = 2;
        private const int STATE_FIND_NEXT_FILE = 3;
        private const int STATE_FINISH = 4;
        private SearchResultHandler<TSource> _resultHandler;
        private List<Directory.SearchData> searchStack;
        private Directory.SearchData searchData;
        private String searchCriteria;
        SafeFindHandle _hnd = null;
        bool needsParentPathDiscoveryDemand;
        bool empty;
        private String userPath;
        private SearchOption searchOption;
        private String fullPath;
        private String normalizedSearchPath;
        private int oldMode;
        private bool _checkHost;
        internal FileSystemEnumerableIterator(String path, String originalUserPath, String searchPattern, SearchOption searchOption, SearchResultHandler<TSource> resultHandler, bool checkHost)
        {
            Contract.Requires(path != null);
            Contract.Requires(originalUserPath != null);
            Contract.Requires(searchPattern != null);
            Contract.Requires(searchOption == SearchOption.AllDirectories || searchOption == SearchOption.TopDirectoryOnly);
            Contract.Requires(resultHandler != null);
            oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
            searchStack = new List<Directory.SearchData>();
            String normalizedSearchPattern = NormalizeSearchPattern(searchPattern);
            if (normalizedSearchPattern.Length == 0)
            {
                empty = true;
            }
            else
            {
                _resultHandler = resultHandler;
                this.searchOption = searchOption;
                fullPath = Path.GetFullPathInternal(path);
                String fullSearchString = GetFullSearchString(fullPath, normalizedSearchPattern);
                normalizedSearchPath = Path.GetDirectoryName(fullSearchString);
                String[] demandPaths = new String[2];
                demandPaths[0] = Directory.GetDemandDir(fullPath, true);
                demandPaths[1] = Directory.GetDemandDir(normalizedSearchPath, true);
                _checkHost = checkHost;
                if (checkHost)
                {
                    FileSecurityState state1 = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPaths[0]);
                    state1.EnsureState();
                    FileSecurityState state2 = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPaths[1]);
                    state2.EnsureState();
                }

                searchCriteria = GetNormalizedSearchCriteria(fullSearchString, normalizedSearchPath);
                String searchPatternDirName = Path.GetDirectoryName(normalizedSearchPattern);
                String userPathTemp = originalUserPath;
                if (searchPatternDirName != null && searchPatternDirName.Length != 0)
                {
                    userPathTemp = Path.Combine(userPathTemp, searchPatternDirName);
                }

                this.userPath = userPathTemp;
                searchData = new Directory.SearchData(normalizedSearchPath, this.userPath, searchOption);
                CommonInit();
            }
        }

        private void CommonInit()
        {
            Contract.Assert(searchCriteria != null && searchData != null, "searchCriteria and searchData should be initialized");
            String searchPath = Path.InternalCombine(searchData.fullPath, searchCriteria);
            Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();
            _hnd = Win32Native.FindFirstFile(searchPath, data);
            if (_hnd.IsInvalid)
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr != Win32Native.ERROR_FILE_NOT_FOUND && hr != Win32Native.ERROR_NO_MORE_FILES)
                {
                    HandleError(hr, searchData.fullPath);
                }
                else
                {
                    empty = searchData.searchOption == SearchOption.TopDirectoryOnly;
                }
            }

            if (searchData.searchOption == SearchOption.TopDirectoryOnly)
            {
                if (empty)
                {
                    _hnd.Dispose();
                }
                else
                {
                    SearchResult searchResult = CreateSearchResult(searchData, data);
                    if (_resultHandler.IsResultIncluded(searchResult))
                    {
                        current = _resultHandler.CreateObject(searchResult);
                    }
                }
            }
            else
            {
                _hnd.Dispose();
                searchStack.Add(searchData);
            }
        }

        private FileSystemEnumerableIterator(String fullPath, String normalizedSearchPath, String searchCriteria, String userPath, SearchOption searchOption, SearchResultHandler<TSource> resultHandler, bool checkHost)
        {
            this.fullPath = fullPath;
            this.normalizedSearchPath = normalizedSearchPath;
            this.searchCriteria = searchCriteria;
            this._resultHandler = resultHandler;
            this.userPath = userPath;
            this.searchOption = searchOption;
            this._checkHost = checkHost;
            searchStack = new List<Directory.SearchData>();
            if (searchCriteria != null)
            {
                String[] demandPaths = new String[2];
                demandPaths[0] = Directory.GetDemandDir(fullPath, true);
                demandPaths[1] = Directory.GetDemandDir(normalizedSearchPath, true);
                if (checkHost)
                {
                    FileSecurityState state1 = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPaths[0]);
                    state1.EnsureState();
                    FileSecurityState state2 = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandPaths[1]);
                    state2.EnsureState();
                }

                searchData = new Directory.SearchData(normalizedSearchPath, userPath, searchOption);
                CommonInit();
            }
            else
            {
                empty = true;
            }
        }

        protected override Iterator<TSource> Clone()
        {
            return new FileSystemEnumerableIterator<TSource>(fullPath, normalizedSearchPath, searchCriteria, userPath, searchOption, _resultHandler, _checkHost);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_hnd != null)
                {
                    _hnd.Dispose();
                }
            }
            finally
            {
                Win32Native.SetErrorMode(oldMode);
                base.Dispose(disposing);
            }
        }

        public override bool MoveNext()
        {
            Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();
            switch (state)
            {
                case STATE_INIT:
                {
                    if (empty)
                    {
                        state = STATE_FINISH;
                        goto case STATE_FINISH;
                    }

                    if (searchData.searchOption == SearchOption.TopDirectoryOnly)
                    {
                        state = STATE_FIND_NEXT_FILE;
                        if (current != null)
                        {
                            return true;
                        }
                        else
                        {
                            goto case STATE_FIND_NEXT_FILE;
                        }
                    }
                    else
                    {
                        state = STATE_SEARCH_NEXT_DIR;
                        goto case STATE_SEARCH_NEXT_DIR;
                    }
                }

                case STATE_SEARCH_NEXT_DIR:
                {
                    Contract.Assert(searchData.searchOption != SearchOption.TopDirectoryOnly, "should not reach this code path if searchOption == TopDirectoryOnly");
                    while (searchStack.Count > 0)
                    {
                        searchData = searchStack[0];
                        Contract.Assert((searchData.fullPath != null), "fullpath can't be null!");
                        searchStack.RemoveAt(0);
                        AddSearchableDirsToStack(searchData);
                        String searchPath = Path.InternalCombine(searchData.fullPath, searchCriteria);
                        _hnd = Win32Native.FindFirstFile(searchPath, data);
                        if (_hnd.IsInvalid)
                        {
                            int hr = Marshal.GetLastWin32Error();
                            if (hr == Win32Native.ERROR_FILE_NOT_FOUND || hr == Win32Native.ERROR_NO_MORE_FILES || hr == Win32Native.ERROR_PATH_NOT_FOUND)
                                continue;
                            _hnd.Dispose();
                            HandleError(hr, searchData.fullPath);
                        }

                        state = STATE_FIND_NEXT_FILE;
                        needsParentPathDiscoveryDemand = true;
                        SearchResult searchResult = CreateSearchResult(searchData, data);
                        if (_resultHandler.IsResultIncluded(searchResult))
                        {
                            if (needsParentPathDiscoveryDemand)
                            {
                                DoDemand(searchData.fullPath);
                                needsParentPathDiscoveryDemand = false;
                            }

                            current = _resultHandler.CreateObject(searchResult);
                            return true;
                        }
                        else
                        {
                            goto case STATE_FIND_NEXT_FILE;
                        }
                    }

                    state = STATE_FINISH;
                    goto case STATE_FINISH;
                }

                case STATE_FIND_NEXT_FILE:
                {
                    if (searchData != null && _hnd != null)
                    {
                        while (Win32Native.FindNextFile(_hnd, data))
                        {
                            SearchResult searchResult = CreateSearchResult(searchData, data);
                            if (_resultHandler.IsResultIncluded(searchResult))
                            {
                                if (needsParentPathDiscoveryDemand)
                                {
                                    DoDemand(searchData.fullPath);
                                    needsParentPathDiscoveryDemand = false;
                                }

                                current = _resultHandler.CreateObject(searchResult);
                                return true;
                            }
                        }

                        int hr = Marshal.GetLastWin32Error();
                        if (_hnd != null)
                            _hnd.Dispose();
                        if ((hr != 0) && (hr != Win32Native.ERROR_NO_MORE_FILES) && (hr != Win32Native.ERROR_FILE_NOT_FOUND))
                        {
                            HandleError(hr, searchData.fullPath);
                        }
                    }

                    if (searchData.searchOption == SearchOption.TopDirectoryOnly)
                    {
                        state = STATE_FINISH;
                        goto case STATE_FINISH;
                    }
                    else
                    {
                        state = STATE_SEARCH_NEXT_DIR;
                        goto case STATE_SEARCH_NEXT_DIR;
                    }
                }

                case STATE_FINISH:
                {
                    Dispose();
                    break;
                }
            }

            return false;
        }

        private SearchResult CreateSearchResult(Directory.SearchData localSearchData, Win32Native.WIN32_FIND_DATA findData)
        {
            String userPathFinal = Path.InternalCombine(localSearchData.userPath, findData.cFileName);
            String fullPathFinal = Path.InternalCombine(localSearchData.fullPath, findData.cFileName);
            return new SearchResult(fullPathFinal, userPathFinal, findData);
        }

        private void HandleError(int hr, String path)
        {
            Dispose();
            __Error.WinIOError(hr, path);
        }

        private void AddSearchableDirsToStack(Directory.SearchData localSearchData)
        {
            Contract.Requires(localSearchData != null);
            String searchPath = Path.InternalCombine(localSearchData.fullPath, "*");
            SafeFindHandle hnd = null;
            Win32Native.WIN32_FIND_DATA data = new Win32Native.WIN32_FIND_DATA();
            try
            {
                hnd = Win32Native.FindFirstFile(searchPath, data);
                if (hnd.IsInvalid)
                {
                    int hr = Marshal.GetLastWin32Error();
                    if (hr == Win32Native.ERROR_FILE_NOT_FOUND || hr == Win32Native.ERROR_NO_MORE_FILES || hr == Win32Native.ERROR_PATH_NOT_FOUND)
                        return;
                    HandleError(hr, localSearchData.fullPath);
                }

                int incr = 0;
                do
                {
                    if (FileSystemEnumerableHelpers.IsDir(data))
                    {
                        String tempFullPath = Path.InternalCombine(localSearchData.fullPath, data.cFileName);
                        String tempUserPath = Path.InternalCombine(localSearchData.userPath, data.cFileName);
                        SearchOption option = localSearchData.searchOption;
                        Directory.SearchData searchDataSubDir = new Directory.SearchData(tempFullPath, tempUserPath, option);
                        searchStack.Insert(incr++, searchDataSubDir);
                    }
                }
                while (Win32Native.FindNextFile(hnd, data));
            }
            finally
            {
                if (hnd != null)
                    hnd.Dispose();
            }
        }

        internal void DoDemand(String fullPathToDemand)
        {
            if (_checkHost)
            {
                String demandDir = Directory.GetDemandDir(fullPathToDemand, true);
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandDir);
                state.EnsureState();
            }
        }

        private static String NormalizeSearchPattern(String searchPattern)
        {
            Contract.Requires(searchPattern != null);
            String tempSearchPattern = searchPattern.TrimEnd(Path.TrimEndChars);
            if (tempSearchPattern.Equals("."))
            {
                tempSearchPattern = "*";
            }

            Path.CheckSearchPattern(tempSearchPattern);
            return tempSearchPattern;
        }

        private static String GetNormalizedSearchCriteria(String fullSearchString, String fullPathMod)
        {
            Contract.Requires(fullSearchString != null);
            Contract.Requires(fullPathMod != null);
            Contract.Requires(fullSearchString.Length >= fullPathMod.Length);
            String searchCriteria = null;
            char lastChar = fullPathMod[fullPathMod.Length - 1];
            if (Path.IsDirectorySeparator(lastChar))
            {
                searchCriteria = fullSearchString.Substring(fullPathMod.Length);
            }
            else
            {
                Contract.Assert(fullSearchString.Length > fullPathMod.Length);
                searchCriteria = fullSearchString.Substring(fullPathMod.Length + 1);
            }

            return searchCriteria;
        }

        private static String GetFullSearchString(String fullPath, String searchPattern)
        {
            Contract.Requires(fullPath != null);
            Contract.Requires(searchPattern != null);
            String tempStr = Path.InternalCombine(fullPath, searchPattern);
            char lastChar = tempStr[tempStr.Length - 1];
            if (Path.IsDirectorySeparator(lastChar) || lastChar == Path.VolumeSeparatorChar)
            {
                tempStr = tempStr + '*';
            }

            return tempStr;
        }
    }

    internal abstract class SearchResultHandler<TSource>
    {
        internal abstract bool IsResultIncluded(SearchResult result);
        internal abstract TSource CreateObject(SearchResult result);
    }

    internal class StringResultHandler : SearchResultHandler<String>
    {
        private bool _includeFiles;
        private bool _includeDirs;
        internal StringResultHandler(bool includeFiles, bool includeDirs)
        {
            _includeFiles = includeFiles;
            _includeDirs = includeDirs;
        }

        internal override bool IsResultIncluded(SearchResult result)
        {
            bool includeFile = _includeFiles && FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool includeDir = _includeDirs && FileSystemEnumerableHelpers.IsDir(result.FindData);
            Contract.Assert(!(includeFile && includeDir), result.FindData.cFileName + ": current item can't be both file and dir!");
            return (includeFile || includeDir);
        }

        internal override String CreateObject(SearchResult result)
        {
            return result.UserPath;
        }
    }

    internal class FileInfoResultHandler : SearchResultHandler<FileInfo>
    {
        internal override bool IsResultIncluded(SearchResult result)
        {
            return FileSystemEnumerableHelpers.IsFile(result.FindData);
        }

        internal override FileInfo CreateObject(SearchResult result)
        {
            String name = result.FullPath;
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, name);
            state.EnsureState();
            FileInfo fi = new FileInfo(name, false);
            fi.InitializeFrom(result.FindData);
            return fi;
        }
    }

    internal class DirectoryInfoResultHandler : SearchResultHandler<DirectoryInfo>
    {
        internal override bool IsResultIncluded(SearchResult result)
        {
            return FileSystemEnumerableHelpers.IsDir(result.FindData);
        }

        internal override DirectoryInfo CreateObject(SearchResult result)
        {
            String name = result.FullPath;
            String permissionName = name + "\\.";
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, permissionName);
            state.EnsureState();
            DirectoryInfo di = new DirectoryInfo(name, false);
            di.InitializeFrom(result.FindData);
            return di;
        }
    }

    internal class FileSystemInfoResultHandler : SearchResultHandler<FileSystemInfo>
    {
        internal override bool IsResultIncluded(SearchResult result)
        {
            bool includeFile = FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool includeDir = FileSystemEnumerableHelpers.IsDir(result.FindData);
            Contract.Assert(!(includeFile && includeDir), result.FindData.cFileName + ": current item can't be both file and dir!");
            return (includeDir || includeFile);
        }

        internal override FileSystemInfo CreateObject(SearchResult result)
        {
            bool isFile = FileSystemEnumerableHelpers.IsFile(result.FindData);
            bool isDir = FileSystemEnumerableHelpers.IsDir(result.FindData);
            if (isDir)
            {
                String name = result.FullPath;
                String permissionName = name + "\\.";
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, permissionName);
                state.EnsureState();
                DirectoryInfo di = new DirectoryInfo(name, false);
                di.InitializeFrom(result.FindData);
                return di;
            }
            else
            {
                Contract.Assert(isFile);
                String name = result.FullPath;
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, name);
                state.EnsureState();
                FileInfo fi = new FileInfo(name, false);
                fi.InitializeFrom(result.FindData);
                return fi;
            }
        }
    }

    internal sealed class SearchResult
    {
        private String fullPath;
        private String userPath;
        private Win32Native.WIN32_FIND_DATA findData;
        internal SearchResult(String fullPath, String userPath, Win32Native.WIN32_FIND_DATA findData)
        {
            Contract.Requires(fullPath != null);
            Contract.Requires(userPath != null);
            this.fullPath = fullPath;
            this.userPath = userPath;
            this.findData = findData;
        }

        internal String FullPath
        {
            get
            {
                return fullPath;
            }
        }

        internal String UserPath
        {
            get
            {
                return userPath;
            }
        }

        internal Win32Native.WIN32_FIND_DATA FindData
        {
            [System.Security.SecurityCritical]
            get
            {
                return findData;
            }
        }
    }

    internal static class FileSystemEnumerableHelpers
    {
        internal static bool IsDir(Win32Native.WIN32_FIND_DATA data)
        {
            return (0 != (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY)) && !data.cFileName.Equals(".") && !data.cFileName.Equals("..");
        }

        internal static bool IsFile(Win32Native.WIN32_FIND_DATA data)
        {
            return 0 == (data.dwFileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY);
        }
    }
}