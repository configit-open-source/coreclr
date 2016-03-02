
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

using Microsoft.Win32;

namespace System.IO
{
    public sealed class FileInfo : FileSystemInfo
    {
        private String _name;
        private FileInfo()
        {
        }

        public static FileInfo UnsafeCreateFileInfo(String fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
                        FileInfo fi = new FileInfo();
            fi.Init(fileName, false);
            return fi;
        }

        public FileInfo(String fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
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

            Init(fileName, true);
        }

        private void Init(String fileName, bool checkHost)
        {
            OriginalPath = fileName;
            String fullPath = Path.GetFullPathInternal(fileName);
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, fileName, fullPath);
                state.EnsureState();
            }

            _name = Path.GetFileName(fileName);
            FullPath = fullPath;
            DisplayPath = GetDisplayPath(fileName);
        }

        private String GetDisplayPath(String originalPath)
        {
            return Path.GetFileName(originalPath);
        }

        private FileInfo(SerializationInfo info, StreamingContext context): base (info, context)
        {
            _name = Path.GetFileName(OriginalPath);
            DisplayPath = GetDisplayPath(OriginalPath);
        }

        internal FileInfo(String fullPath, bool ignoreThis)
        {
                        _name = Path.GetFileName(fullPath);
            OriginalPath = _name;
            FullPath = fullPath;
            DisplayPath = _name;
        }

        public override String Name
        {
            get
            {
                return _name;
            }
        }

        public long Length
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_dataInitialised == -1)
                    Refresh();
                if (_dataInitialised != 0)
                    __Error.WinIOError(_dataInitialised, DisplayPath);
                if ((_data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) != 0)
                    __Error.WinIOError(Win32Native.ERROR_FILE_NOT_FOUND, DisplayPath);
                return ((long)_data.fileSizeHigh) << 32 | ((long)_data.fileSizeLow & 0xFFFFFFFFL);
            }
        }

        public String DirectoryName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String directoryName = Path.GetDirectoryName(FullPath);
                if (directoryName != null)
                {
                    FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, DisplayPath, FullPath);
                    state.EnsureState();
                }

                return directoryName;
            }
        }

        public DirectoryInfo Directory
        {
            get
            {
                String dirName = DirectoryName;
                if (dirName == null)
                    return null;
                return new DirectoryInfo(dirName);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return (Attributes & FileAttributes.ReadOnly) != 0;
            }

            set
            {
                if (value)
                    Attributes |= FileAttributes.ReadOnly;
                else
                    Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        public StreamReader OpenText()
        {
            return new StreamReader(FullPath, Encoding.UTF8, true, StreamReader.DefaultBufferSize, false);
        }

        public StreamWriter CreateText()
        {
            return new StreamWriter(FullPath, false);
        }

        public StreamWriter AppendText()
        {
            return new StreamWriter(FullPath, true);
        }

        public FileInfo CopyTo(String destFileName)
        {
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
                        destFileName = File.InternalCopy(FullPath, destFileName, false, true);
            return new FileInfo(destFileName, false);
        }

        public FileInfo CopyTo(String destFileName, bool overwrite)
        {
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
                        destFileName = File.InternalCopy(FullPath, destFileName, overwrite, true);
            return new FileInfo(destFileName, false);
        }

        public FileStream Create()
        {
            return File.Create(FullPath);
        }

        public override void Delete()
        {
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, DisplayPath, FullPath);
            state.EnsureState();
            bool r = Win32Native.DeleteFile(FullPath);
            if (!r)
            {
                int hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_FILE_NOT_FOUND)
                    return;
                else
                    __Error.WinIOError(hr, DisplayPath);
            }
        }

        public void Decrypt()
        {
            File.Decrypt(FullPath);
        }

        public void Encrypt()
        {
            File.Encrypt(FullPath);
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
                    {
                        return false;
                    }

                    return (_data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) == 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public FileStream Open(FileMode mode)
        {
            return Open(mode, FileAccess.ReadWrite, FileShare.None);
        }

        public FileStream Open(FileMode mode, FileAccess access)
        {
            return Open(mode, access, FileShare.None);
        }

        public FileStream Open(FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(FullPath, mode, access, share);
        }

        public FileStream OpenRead()
        {
            return new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
        }

        public FileStream OpenWrite()
        {
            return new FileStream(FullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        public void MoveTo(String destFileName)
        {
            if (destFileName == null)
                throw new ArgumentNullException("destFileName");
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
                        String fullDestFileName = Path.GetFullPathInternal(destFileName);
            FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, DisplayPath, FullPath);
            FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destFileName, fullDestFileName);
            sourceState.EnsureState();
            destState.EnsureState();
            if (!Win32Native.MoveFile(FullPath, fullDestFileName))
                __Error.WinIOError();
            FullPath = fullDestFileName;
            OriginalPath = destFileName;
            _name = Path.GetFileName(fullDestFileName);
            DisplayPath = GetDisplayPath(destFileName);
            _dataInitialised = -1;
        }

        public FileInfo Replace(String destinationFileName, String destinationBackupFileName)
        {
            return Replace(destinationFileName, destinationBackupFileName, false);
        }

        public FileInfo Replace(String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
            File.Replace(FullPath, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
            return new FileInfo(destinationFileName);
        }

        public override String ToString()
        {
            return DisplayPath;
        }
    }
}