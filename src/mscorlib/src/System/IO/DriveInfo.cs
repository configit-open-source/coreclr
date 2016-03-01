using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

using Microsoft.Win32;

namespace System.IO
{
    public enum DriveType
    {
        Unknown = 0,
        NoRootDirectory = 1,
        Removable = 2,
        Fixed = 3,
        Network = 4,
        CDRom = 5,
        Ram = 6
    }

    public sealed class DriveInfo : ISerializable
    {
        private String _name;
        private const String NameField = "_name";
        public DriveInfo(String driveName)
        {
            if (driveName == null)
                throw new ArgumentNullException("driveName");
            Contract.EndContractBlock();
            if (driveName.Length == 1)
                _name = driveName + ":\\";
            else
            {
                Path.CheckInvalidPathChars(driveName);
                _name = Path.GetPathRoot(driveName);
                if (_name == null || _name.Length == 0 || _name.StartsWith("\\\\", StringComparison.Ordinal))
                    throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDriveLetterOrRootDir"));
            }

            if (_name.Length == 2 && _name[1] == ':')
            {
                _name = _name + "\\";
            }

            char letter = driveName[0];
            if (!((letter >= 'A' && letter <= 'Z') || (letter >= 'a' && letter <= 'z')))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDriveLetterOrRootDir"));
            String demandPath = _name + '.';
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPath).Demand();
        }

        private DriveInfo(SerializationInfo info, StreamingContext context)
        {
            _name = (String)info.GetValue(NameField, typeof (String));
            String demandPath = _name + '.';
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPath).Demand();
        }

        public String Name
        {
            get
            {
                return _name;
            }
        }

        public DriveType DriveType
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (DriveType)Win32Native.GetDriveType(Name);
            }
        }

        public String DriveFormat
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Win32Native.GetVolumeInformation(Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        __Error.WinIODriveError(Name, errorCode);
                    }
                }
                finally
                {
                    Win32Native.SetErrorMode(oldMode);
                }

                return fileSystemName.ToString();
            }
        }

        public bool IsReady
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return Directory.InternalExists(Name);
            }
        }

        public long AvailableFreeSpace
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                long userBytes, totalBytes, freeBytes;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Win32Native.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        __Error.WinIODriveError(Name);
                }
                finally
                {
                    Win32Native.SetErrorMode(oldMode);
                }

                return userBytes;
            }
        }

        public long TotalFreeSpace
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                long userBytes, totalBytes, freeBytes;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Win32Native.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        __Error.WinIODriveError(Name);
                }
                finally
                {
                    Win32Native.SetErrorMode(oldMode);
                }

                return freeBytes;
            }
        }

        public long TotalSize
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                long userBytes, totalBytes, freeBytes;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Win32Native.GetDiskFreeSpaceEx(Name, out userBytes, out totalBytes, out freeBytes);
                    if (!r)
                        __Error.WinIODriveError(Name);
                }
                finally
                {
                    Win32Native.SetErrorMode(oldMode);
                }

                return totalBytes;
            }
        }

        public static DriveInfo[] GetDrives()
        {
            String[] drives = Directory.GetLogicalDrives();
            DriveInfo[] di = new DriveInfo[drives.Length];
            for (int i = 0; i < drives.Length; i++)
                di[i] = new DriveInfo(drives[i]);
            return di;
        }

        public DirectoryInfo RootDirectory
        {
            get
            {
                return new DirectoryInfo(Name);
            }
        }

        public String VolumeLabel
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Win32Native.GetVolumeInformation(Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        if (errorCode == Win32Native.ERROR_INVALID_DATA)
                            errorCode = Win32Native.ERROR_INVALID_DRIVE;
                        __Error.WinIODriveError(Name, errorCode);
                    }
                }
                finally
                {
                    Win32Native.SetErrorMode(oldMode);
                }

                return volumeName.ToString();
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                String demandPath = _name + '.';
                new FileIOPermission(FileIOPermissionAccess.Write, demandPath).Demand();
                int oldMode = Win32Native.SetErrorMode(Win32Native.SEM_FAILCRITICALERRORS);
                try
                {
                    bool r = Win32Native.SetVolumeLabel(Name, value);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        if (errorCode == Win32Native.ERROR_ACCESS_DENIED)
                            throw new UnauthorizedAccessException(Environment.GetResourceString("InvalidOperation_SetVolumeLabelFailed"));
                        __Error.WinIODriveError(Name, errorCode);
                    }
                }
                finally
                {
                    Win32Native.SetErrorMode(oldMode);
                }
            }
        }

        public override String ToString()
        {
            return Name;
        }
    }
}