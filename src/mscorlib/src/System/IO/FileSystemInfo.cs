
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Microsoft.Win32;

namespace System.IO
{
    public abstract class FileSystemInfo : ISerializable
    {
        internal Win32Native.WIN32_FILE_ATTRIBUTE_DATA _data;
        internal int _dataInitialised = -1;
        private const int ERROR_INVALID_PARAMETER = 87;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        protected String FullPath;
        protected String OriginalPath;
        private String _displayPath = "";
        protected FileSystemInfo()
        {
        }

        protected FileSystemInfo(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
                        FullPath = Path.GetFullPathInternal(info.GetString("FullPath"));
            OriginalPath = info.GetString("OriginalPath");
            _dataInitialised = -1;
        }

        internal void InitializeFrom(Win32Native.WIN32_FIND_DATA findData)
        {
            _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            _data.PopulateFrom(findData);
            _dataInitialised = 0;
        }

        public virtual String FullName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String demandDir;
                if (this is DirectoryInfo)
                    demandDir = Directory.GetDemandDir(FullPath, true);
                else
                    demandDir = FullPath;
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandDir);
                sourceState.EnsureState();
                return FullPath;
            }
        }

        internal virtual String UnsafeGetFullName
        {
            [System.Security.SecurityCritical]
            get
            {
                String demandDir;
                if (this is DirectoryInfo)
                    demandDir = Directory.GetDemandDir(FullPath, true);
                else
                    demandDir = FullPath;
                return FullPath;
            }
        }

        public String Extension
        {
            get
            {
                int length = FullPath.Length;
                for (int i = length; --i >= 0;)
                {
                    char ch = FullPath[i];
                    if (ch == '.')
                        return FullPath.Substring(i, length - i);
                    if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar || ch == Path.VolumeSeparatorChar)
                        break;
                }

                return String.Empty;
            }
        }

        public abstract String Name
        {
            get;
        }

        public abstract bool Exists
        {
            get;
        }

        public abstract void Delete();
        public DateTime CreationTime
        {
            get
            {
                return CreationTimeUtc.ToLocalTime();
            }

            set
            {
                CreationTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime CreationTimeUtc
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, FullPath);
                sourceState.EnsureState();
                if (_dataInitialised == -1)
                {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0)
                    __Error.WinIOError(_dataInitialised, DisplayPath);
                long fileTime = ((long)_data.ftCreationTimeHigh << 32) | _data.ftCreationTimeLow;
                return DateTime.FromFileTimeUtc(fileTime);
            }

            set
            {
                if (this is DirectoryInfo)
                    Directory.SetCreationTimeUtc(FullPath, value);
                else
                    File.SetCreationTimeUtc(FullPath, value);
                _dataInitialised = -1;
            }
        }

        public DateTime LastAccessTime
        {
            get
            {
                return LastAccessTimeUtc.ToLocalTime();
            }

            set
            {
                LastAccessTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime LastAccessTimeUtc
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, FullPath);
                sourceState.EnsureState();
                if (_dataInitialised == -1)
                {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0)
                    __Error.WinIOError(_dataInitialised, DisplayPath);
                long fileTime = ((long)_data.ftLastAccessTimeHigh << 32) | _data.ftLastAccessTimeLow;
                return DateTime.FromFileTimeUtc(fileTime);
            }

            set
            {
                if (this is DirectoryInfo)
                    Directory.SetLastAccessTimeUtc(FullPath, value);
                else
                    File.SetLastAccessTimeUtc(FullPath, value);
                _dataInitialised = -1;
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return LastWriteTimeUtc.ToLocalTime();
            }

            set
            {
                LastWriteTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime LastWriteTimeUtc
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, FullPath);
                sourceState.EnsureState();
                if (_dataInitialised == -1)
                {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0)
                    __Error.WinIOError(_dataInitialised, DisplayPath);
                long fileTime = ((long)_data.ftLastWriteTimeHigh << 32) | _data.ftLastWriteTimeLow;
                return DateTime.FromFileTimeUtc(fileTime);
            }

            set
            {
                if (this is DirectoryInfo)
                    Directory.SetLastWriteTimeUtc(FullPath, value);
                else
                    File.SetLastWriteTimeUtc(FullPath, value);
                _dataInitialised = -1;
            }
        }

        public void Refresh()
        {
            _dataInitialised = File.FillAttributeInfo(FullPath, ref _data, false, false);
        }

        public FileAttributes Attributes
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, FullPath);
                sourceState.EnsureState();
                if (_dataInitialised == -1)
                {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0)
                    __Error.WinIOError(_dataInitialised, DisplayPath);
                return (FileAttributes)_data.fileAttributes;
            }

            [System.Security.SecurityCritical]
            set
            {
                bool r = Win32Native.SetFileAttributes(FullPath, (int)value);
                if (!r)
                {
                    int hr = Marshal.GetLastWin32Error();
                    if (hr == ERROR_INVALID_PARAMETER)
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileAttrs"));
                    if (hr == ERROR_ACCESS_DENIED)
                        throw new ArgumentException(Environment.GetResourceString("UnauthorizedAccess_IODenied_NoPathName"));
                    __Error.WinIOError(hr, DisplayPath);
                }

                _dataInitialised = -1;
            }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("OriginalPath", OriginalPath, typeof (String));
            info.AddValue("FullPath", FullPath, typeof (String));
        }

        internal String DisplayPath
        {
            get
            {
                return _displayPath;
            }

            set
            {
                _displayPath = value;
            }
        }
    }
}