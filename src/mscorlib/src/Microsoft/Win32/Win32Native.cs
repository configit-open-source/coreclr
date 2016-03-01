namespace Microsoft.Win32
{
    using System;
    using System.Security;
    using System.Security.Principal;
    using System.Text;
    using System.Configuration.Assemblies;
    using System.Runtime.Remoting;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using BOOL = System.Int32;
    using DWORD = System.UInt32;
    using ULONG = System.UInt32;

    internal static class Win32Native
    {
        internal const int KEY_QUERY_VALUE = 0x0001;
        internal const int KEY_SET_VALUE = 0x0002;
        internal const int KEY_CREATE_SUB_KEY = 0x0004;
        internal const int KEY_ENUMERATE_SUB_KEYS = 0x0008;
        internal const int KEY_NOTIFY = 0x0010;
        internal const int KEY_CREATE_LINK = 0x0020;
        internal const int KEY_READ = ((STANDARD_RIGHTS_READ | KEY_QUERY_VALUE | KEY_ENUMERATE_SUB_KEYS | KEY_NOTIFY) & (~SYNCHRONIZE));
        internal const int KEY_WRITE = ((STANDARD_RIGHTS_WRITE | KEY_SET_VALUE | KEY_CREATE_SUB_KEY) & (~SYNCHRONIZE));
        internal const int KEY_WOW64_64KEY = 0x0100;
        internal const int KEY_WOW64_32KEY = 0x0200;
        internal const int REG_OPTION_NON_VOLATILE = 0x0000;
        internal const int REG_OPTION_VOLATILE = 0x0001;
        internal const int REG_OPTION_CREATE_LINK = 0x0002;
        internal const int REG_OPTION_BACKUP_RESTORE = 0x0004;
        internal const int REG_NONE = 0;
        internal const int REG_SZ = 1;
        internal const int REG_EXPAND_SZ = 2;
        internal const int REG_BINARY = 3;
        internal const int REG_DWORD = 4;
        internal const int REG_DWORD_LITTLE_ENDIAN = 4;
        internal const int REG_DWORD_BIG_ENDIAN = 5;
        internal const int REG_LINK = 6;
        internal const int REG_MULTI_SZ = 7;
        internal const int REG_RESOURCE_LIST = 8;
        internal const int REG_FULL_RESOURCE_DESCRIPTOR = 9;
        internal const int REG_RESOURCE_REQUIREMENTS_LIST = 10;
        internal const int REG_QWORD = 11;
        internal const int HWND_BROADCAST = 0xffff;
        internal const int WM_SETTINGCHANGE = 0x001A;
        internal const uint CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;
        internal const uint CRYPTPROTECTMEMORY_SAME_PROCESS = 0x00;
        internal const uint CRYPTPROTECTMEMORY_CROSS_PROCESS = 0x01;
        internal const uint CRYPTPROTECTMEMORY_SAME_LOGON = 0x02;
        internal const int SECURITY_ANONYMOUS = ((int)SECURITY_IMPERSONATION_LEVEL.Anonymous << 16);
        internal const int SECURITY_SQOS_PRESENT = 0x00100000;
        internal const string MICROSOFT_KERBEROS_NAME = "Kerberos";
        internal const uint ANONYMOUS_LOGON_LUID = 0x3e6;
        internal const int SECURITY_ANONYMOUS_LOGON_RID = 0x00000007;
        internal const int SECURITY_AUTHENTICATED_USER_RID = 0x0000000B;
        internal const int SECURITY_LOCAL_SYSTEM_RID = 0x00000012;
        internal const int SECURITY_BUILTIN_DOMAIN_RID = 0x00000020;
        internal const uint SE_PRIVILEGE_DISABLED = 0x00000000;
        internal const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
        internal const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        internal const uint SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;
        internal const uint SE_GROUP_MANDATORY = 0x00000001;
        internal const uint SE_GROUP_ENABLED_BY_DEFAULT = 0x00000002;
        internal const uint SE_GROUP_ENABLED = 0x00000004;
        internal const uint SE_GROUP_OWNER = 0x00000008;
        internal const uint SE_GROUP_USE_FOR_DENY_ONLY = 0x00000010;
        internal const uint SE_GROUP_LOGON_ID = 0xC0000000;
        internal const uint SE_GROUP_RESOURCE = 0x20000000;
        internal const uint DUPLICATE_CLOSE_SOURCE = 0x00000001;
        internal const uint DUPLICATE_SAME_ACCESS = 0x00000002;
        internal const uint DUPLICATE_SAME_ATTRIBUTES = 0x00000004;
        internal const int TIME_ZONE_ID_INVALID = -1;
        internal const int TIME_ZONE_ID_UNKNOWN = 0;
        internal const int TIME_ZONE_ID_STANDARD = 1;
        internal const int TIME_ZONE_ID_DAYLIGHT = 2;
        internal const int MAX_PATH = 260;
        internal const int MUI_LANGUAGE_ID = 0x4;
        internal const int MUI_LANGUAGE_NAME = 0x8;
        internal const int MUI_PREFERRED_UI_LANGUAGES = 0x10;
        internal const int MUI_INSTALLED_LANGUAGES = 0x20;
        internal const int MUI_ALL_LANGUAGES = 0x40;
        internal const int MUI_LANG_NEUTRAL_PE_FILE = 0x100;
        internal const int MUI_NON_LANG_NEUTRAL_FILE = 0x200;
        internal const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        internal const int LOAD_STRING_MAX_LENGTH = 500;
        internal struct SystemTime
        {
            public short Year;
            public short Month;
            public short DayOfWeek;
            public short Day;
            public short Hour;
            public short Minute;
            public short Second;
            public short Milliseconds;
        }

        internal struct TimeZoneInformation
        {
            public Int32 Bias;
            public string StandardName;
            public SystemTime StandardDate;
            public Int32 StandardBias;
            public string DaylightName;
            public SystemTime DaylightDate;
            public Int32 DaylightBias;
            public TimeZoneInformation(Win32Native.DynamicTimeZoneInformation dtzi)
            {
                Bias = dtzi.Bias;
                StandardName = dtzi.StandardName;
                StandardDate = dtzi.StandardDate;
                StandardBias = dtzi.StandardBias;
                DaylightName = dtzi.DaylightName;
                DaylightDate = dtzi.DaylightDate;
                DaylightBias = dtzi.DaylightBias;
            }
        }

        internal struct DynamicTimeZoneInformation
        {
            public Int32 Bias;
            public string StandardName;
            public SystemTime StandardDate;
            public Int32 StandardBias;
            public string DaylightName;
            public SystemTime DaylightDate;
            public Int32 DaylightBias;
            public string TimeZoneKeyName;
            public bool DynamicDaylightTimeDisabled;
        }

        internal struct RegistryTimeZoneInformation
        {
            public Int32 Bias;
            public Int32 StandardBias;
            public Int32 DaylightBias;
            public SystemTime StandardDate;
            public SystemTime DaylightDate;
            public RegistryTimeZoneInformation(Win32Native.TimeZoneInformation tzi)
            {
                Bias = tzi.Bias;
                StandardDate = tzi.StandardDate;
                StandardBias = tzi.StandardBias;
                DaylightDate = tzi.DaylightDate;
                DaylightBias = tzi.DaylightBias;
            }

            public RegistryTimeZoneInformation(Byte[] bytes)
            {
                if (bytes == null || bytes.Length != 44)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidREG_TZI_FORMAT"), "bytes");
                }

                Bias = BitConverter.ToInt32(bytes, 0);
                StandardBias = BitConverter.ToInt32(bytes, 4);
                DaylightBias = BitConverter.ToInt32(bytes, 8);
                StandardDate.Year = BitConverter.ToInt16(bytes, 12);
                StandardDate.Month = BitConverter.ToInt16(bytes, 14);
                StandardDate.DayOfWeek = BitConverter.ToInt16(bytes, 16);
                StandardDate.Day = BitConverter.ToInt16(bytes, 18);
                StandardDate.Hour = BitConverter.ToInt16(bytes, 20);
                StandardDate.Minute = BitConverter.ToInt16(bytes, 22);
                StandardDate.Second = BitConverter.ToInt16(bytes, 24);
                StandardDate.Milliseconds = BitConverter.ToInt16(bytes, 26);
                DaylightDate.Year = BitConverter.ToInt16(bytes, 28);
                DaylightDate.Month = BitConverter.ToInt16(bytes, 30);
                DaylightDate.DayOfWeek = BitConverter.ToInt16(bytes, 32);
                DaylightDate.Day = BitConverter.ToInt16(bytes, 34);
                DaylightDate.Hour = BitConverter.ToInt16(bytes, 36);
                DaylightDate.Minute = BitConverter.ToInt16(bytes, 38);
                DaylightDate.Second = BitConverter.ToInt16(bytes, 40);
                DaylightDate.Milliseconds = BitConverter.ToInt16(bytes, 42);
            }
        }

        internal const int READ_CONTROL = 0x00020000;
        internal const int SYNCHRONIZE = 0x00100000;
        internal const int STANDARD_RIGHTS_READ = READ_CONTROL;
        internal const int STANDARD_RIGHTS_WRITE = READ_CONTROL;
        internal const int SEMAPHORE_MODIFY_STATE = 0x00000002;
        internal const int EVENT_MODIFY_STATE = 0x00000002;
        internal const int MUTEX_MODIFY_STATE = 0x00000001;
        internal const int MUTEX_ALL_ACCESS = 0x001F0001;
        internal const int LMEM_FIXED = 0x0000;
        internal const int LMEM_ZEROINIT = 0x0040;
        internal const int LPTR = (LMEM_FIXED | LMEM_ZEROINIT);
        internal class OSVERSIONINFO
        {
            internal OSVERSIONINFO()
            {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            }

            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0;
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            internal String CSDVersion = null;
        }

        internal class OSVERSIONINFOEX
        {
            public OSVERSIONINFOEX()
            {
                OSVersionInfoSize = (int)Marshal.SizeOf(this);
            }

            internal int OSVersionInfoSize = 0;
            internal int MajorVersion = 0;
            internal int MinorVersion = 0;
            internal int BuildNumber = 0;
            internal int PlatformId = 0;
            internal string CSDVersion = null;
            internal ushort ServicePackMajor = 0;
            internal ushort ServicePackMinor = 0;
            internal short SuiteMask = 0;
            internal byte ProductType = 0;
            internal byte Reserved = 0;
        }

        internal struct SYSTEM_INFO
        {
            internal int dwOemId;
            internal int dwPageSize;
            internal IntPtr lpMinimumApplicationAddress;
            internal IntPtr lpMaximumApplicationAddress;
            internal IntPtr dwActiveProcessorMask;
            internal int dwNumberOfProcessors;
            internal int dwProcessorType;
            internal int dwAllocationGranularity;
            internal short wProcessorLevel;
            internal short wProcessorRevision;
        }

        internal class SECURITY_ATTRIBUTES
        {
            internal int nLength = 0;
            internal unsafe byte *pSecurityDescriptor = null;
            internal int bInheritHandle = 0;
        }

        internal struct WIN32_FILE_ATTRIBUTE_DATA
        {
            internal int fileAttributes;
            internal uint ftCreationTimeLow;
            internal uint ftCreationTimeHigh;
            internal uint ftLastAccessTimeLow;
            internal uint ftLastAccessTimeHigh;
            internal uint ftLastWriteTimeLow;
            internal uint ftLastWriteTimeHigh;
            internal int fileSizeHigh;
            internal int fileSizeLow;
            internal void PopulateFrom(WIN32_FIND_DATA findData)
            {
                fileAttributes = findData.dwFileAttributes;
                ftCreationTimeLow = findData.ftCreationTime_dwLowDateTime;
                ftCreationTimeHigh = findData.ftCreationTime_dwHighDateTime;
                ftLastAccessTimeLow = findData.ftLastAccessTime_dwLowDateTime;
                ftLastAccessTimeHigh = findData.ftLastAccessTime_dwHighDateTime;
                ftLastWriteTimeLow = findData.ftLastWriteTime_dwLowDateTime;
                ftLastWriteTimeHigh = findData.ftLastWriteTime_dwHighDateTime;
                fileSizeHigh = findData.nFileSizeHigh;
                fileSizeLow = findData.nFileSizeLow;
            }
        }

        internal struct FILE_TIME
        {
            public FILE_TIME(long fileTime)
            {
                ftTimeLow = (uint)fileTime;
                ftTimeHigh = (uint)(fileTime >> 32);
            }

            public long ToTicks()
            {
                return ((long)ftTimeHigh << 32) + ftTimeLow;
            }

            internal uint ftTimeLow;
            internal uint ftTimeHigh;
        }

        internal struct KERB_S4U_LOGON
        {
            internal uint MessageType;
            internal uint Flags;
            internal UNICODE_INTPTR_STRING ClientUpn;
            internal UNICODE_INTPTR_STRING ClientRealm;
        }

        internal struct LSA_OBJECT_ATTRIBUTES
        {
            internal int Length;
            internal IntPtr RootDirectory;
            internal IntPtr ObjectName;
            internal int Attributes;
            internal IntPtr SecurityDescriptor;
            internal IntPtr SecurityQualityOfService;
        }

        internal struct UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            internal string Buffer;
        }

        internal struct UNICODE_INTPTR_STRING
        {
            internal UNICODE_INTPTR_STRING(int stringBytes, SafeLocalAllocHandle buffer)
            {
                BCLDebug.Assert(buffer == null || (stringBytes >= 0 && (ulong)stringBytes <= buffer.ByteLength), "buffer == null || (stringBytes >= 0 && stringBytes <= buffer.ByteLength)");
                this.Length = (ushort)stringBytes;
                this.MaxLength = (ushort)buffer.ByteLength;
                this.Buffer = buffer.DangerousGetHandle();
            }

            internal UNICODE_INTPTR_STRING(int stringBytes, IntPtr buffer)
            {
                BCLDebug.Assert((stringBytes == 0 && buffer == IntPtr.Zero) || (stringBytes > 0 && stringBytes <= UInt16.MaxValue && buffer != IntPtr.Zero), "(stringBytes == 0 && buffer == IntPtr.Zero) || (stringBytes > 0 && stringBytes <= UInt16.MaxValue && buffer != IntPtr.Zero)");
                this.Length = (ushort)stringBytes;
                this.MaxLength = (ushort)stringBytes;
                this.Buffer = buffer;
            }

            internal ushort Length;
            internal ushort MaxLength;
            internal IntPtr Buffer;
        }

        internal struct LSA_TRANSLATED_NAME
        {
            internal int Use;
            internal UNICODE_INTPTR_STRING Name;
            internal int DomainIndex;
        }

        internal struct LSA_TRANSLATED_SID
        {
            internal int Use;
            internal uint Rid;
            internal int DomainIndex;
        }

        internal struct LSA_TRANSLATED_SID2
        {
            internal int Use;
            internal IntPtr Sid;
            internal int DomainIndex;
            uint Flags;
        }

        internal struct LSA_TRUST_INFORMATION
        {
            internal UNICODE_INTPTR_STRING Name;
            internal IntPtr Sid;
        }

        internal struct LSA_REFERENCED_DOMAIN_LIST
        {
            internal int Entries;
            internal IntPtr Domains;
        }

        internal struct LUID
        {
            internal uint LowPart;
            internal uint HighPart;
        }

        internal struct LUID_AND_ATTRIBUTES
        {
            internal LUID Luid;
            internal uint Attributes;
        }

        internal struct QUOTA_LIMITS
        {
            internal IntPtr PagedPoolLimit;
            internal IntPtr NonPagedPoolLimit;
            internal IntPtr MinimumWorkingSetSize;
            internal IntPtr MaximumWorkingSetSize;
            internal IntPtr PagefileLimit;
            internal IntPtr TimeLimit;
        }

        internal struct SECURITY_LOGON_SESSION_DATA
        {
            internal uint Size;
            internal LUID LogonId;
            internal UNICODE_INTPTR_STRING UserName;
            internal UNICODE_INTPTR_STRING LogonDomain;
            internal UNICODE_INTPTR_STRING AuthenticationPackage;
            internal uint LogonType;
            internal uint Session;
            internal IntPtr Sid;
            internal long LogonTime;
        }

        internal struct SID_AND_ATTRIBUTES
        {
            internal IntPtr Sid;
            internal uint Attributes;
            internal static readonly long SizeOf = (long)Marshal.SizeOf(typeof (SID_AND_ATTRIBUTES));
        }

        internal struct TOKEN_GROUPS
        {
            internal uint GroupCount;
            internal SID_AND_ATTRIBUTES Groups;
        }

        internal struct TOKEN_PRIMARY_GROUP
        {
            internal IntPtr PrimaryGroup;
        }

        internal struct TOKEN_PRIVILEGE
        {
            internal uint PrivilegeCount;
            internal LUID_AND_ATTRIBUTES Privilege;
        }

        internal struct TOKEN_SOURCE
        {
            private const int TOKEN_SOURCE_LENGTH = 8;
            internal char[] Name;
            internal LUID SourceIdentifier;
        }

        internal struct TOKEN_STATISTICS
        {
            internal LUID TokenId;
            internal LUID AuthenticationId;
            internal long ExpirationTime;
            internal uint TokenType;
            internal uint ImpersonationLevel;
            internal uint DynamicCharged;
            internal uint DynamicAvailable;
            internal uint GroupCount;
            internal uint PrivilegeCount;
            internal LUID ModifiedId;
        }

        internal struct TOKEN_USER
        {
            internal SID_AND_ATTRIBUTES User;
        }

        internal struct MEMORYSTATUSEX
        {
            internal int length;
            internal int memoryLoad;
            internal ulong totalPhys;
            internal ulong availPhys;
            internal ulong totalPageFile;
            internal ulong availPageFile;
            internal ulong totalVirtual;
            internal ulong availVirtual;
            internal ulong availExtendedVirtual;
        }

        internal unsafe struct MEMORY_BASIC_INFORMATION
        {
            internal void *BaseAddress;
            internal void *AllocationBase;
            internal uint AllocationProtect;
            internal UIntPtr RegionSize;
            internal uint State;
            internal uint Protect;
            internal uint Type;
        }

        internal const String KERNEL32 = "kernel32.dll";
        internal const String USER32 = "user32.dll";
        internal const String OLE32 = "ole32.dll";
        internal const String OLEAUT32 = "oleaut32.dll";
        internal const String ADVAPI32 = "advapi32.dll";
        internal const String SHELL32 = "shell32.dll";
        internal const String SHIM = "mscoree.dll";
        internal const String CRYPT32 = "crypt32.dll";
        internal const String SECUR32 = "secur32.dll";
        internal const String NTDLL = "ntdll.dll";
        internal const String MSCORWKS = "coreclr.dll";
        internal const int SEM_FAILCRITICALERRORS = 1;
        internal static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);
        internal static extern int FormatMessage(int dwFlags, IntPtr lpSource, int dwMessageId, int dwLanguageId, [Out] StringBuilder lpBuffer, int nSize, IntPtr va_list_arguments);
        internal static String GetMessage(int errorCode)
        {
            StringBuilder sb = StringBuilderCache.Acquire(512);
            int result = Win32Native.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY, IntPtr.Zero, errorCode, 0, sb, sb.Capacity, IntPtr.Zero);
            if (result != 0)
            {
                return StringBuilderCache.GetStringAndRelease(sb);
            }
            else
            {
                StringBuilderCache.Release(sb);
                return Environment.GetResourceString("UnknownError_Num", errorCode);
            }
        }

        internal static extern IntPtr LocalAlloc_NoSafeHandle(int uFlags, UIntPtr sizetdwBytes);
        internal static extern SafeLocalAllocHandle LocalAlloc([In] int uFlags, [In] UIntPtr sizetdwBytes);
        internal static extern IntPtr LocalFree(IntPtr handle);
        internal static extern void ZeroMemory(IntPtr address, UIntPtr length);
        internal static bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer)
        {
            buffer.length = Marshal.SizeOf(typeof (MEMORYSTATUSEX));
            return GlobalMemoryStatusExNative(ref buffer);
        }

        private static extern bool GlobalMemoryStatusExNative([In, Out] ref MEMORYSTATUSEX buffer);
        unsafe internal static extern UIntPtr VirtualQuery(void *address, ref MEMORY_BASIC_INFORMATION buffer, UIntPtr sizeOfBuffer);
        unsafe internal static extern void *VirtualAlloc(void *address, UIntPtr numBytes, int commitOrReserve, int pageProtectionMode);
        unsafe internal static extern bool VirtualFree(void *address, UIntPtr numBytes, int pageFreeMode);
        private static extern IntPtr GetProcAddress(IntPtr hModule, String methodName);
        private static extern IntPtr GetModuleHandle(String moduleName);
        internal static bool DoesWin32MethodExist(String moduleName, String methodName)
        {
            IntPtr hModule = Win32Native.GetModuleHandle(moduleName);
            if (hModule == IntPtr.Zero)
            {
                BCLDebug.Assert(hModule != IntPtr.Zero, "GetModuleHandle failed.  Dll isn't loaded?");
                return false;
            }

            IntPtr functionPointer = Win32Native.GetProcAddress(hModule, methodName);
            return (functionPointer != IntPtr.Zero);
        }

        internal static extern bool IsWow64Process([In] IntPtr hSourceProcessHandle, [Out, MarshalAs(UnmanagedType.Bool)] out bool isWow64);
        internal static extern uint GetTempPath(int bufferLen, [Out] StringBuilder buffer);
        internal static extern int lstrlenA(IntPtr ptr);
        internal static extern int lstrlenW(IntPtr ptr);
        internal static extern IntPtr SysAllocStringLen(String src, int len);
        internal static extern uint SysStringLen(IntPtr bstr);
        internal static extern void SysFreeString(IntPtr bstr);
        internal static extern IntPtr SysAllocStringByteLen(byte[] str, uint len);
        internal static extern uint SysStringByteLen(IntPtr bstr);
        internal static extern uint SysStringLen(SafeBSTRHandle bstr);
        internal static extern int GetACP();
        internal static extern bool SetEvent(SafeWaitHandle handle);
        internal static extern bool ResetEvent(SafeWaitHandle handle);
        internal static extern SafeWaitHandle CreateEvent(SECURITY_ATTRIBUTES lpSecurityAttributes, bool isManualReset, bool initialState, String name);
        internal static extern SafeWaitHandle OpenEvent(int desiredAccess, bool inheritHandle, String name);
        internal static extern SafeWaitHandle CreateMutex(SECURITY_ATTRIBUTES lpSecurityAttributes, bool initialOwner, String name);
        internal static extern SafeWaitHandle OpenMutex(int desiredAccess, bool inheritHandle, String name);
        internal static extern bool ReleaseMutex(SafeWaitHandle handle);
        internal unsafe static extern int GetFullPathName(char *path, int numBufferChars, char *buffer, IntPtr mustBeZero);
        internal unsafe static extern int GetFullPathName(String path, int numBufferChars, [Out] StringBuilder buffer, IntPtr mustBeZero);
        internal unsafe static extern int GetLongPathName(char *path, char *longPathBuffer, int bufferLength);
        internal static extern int GetLongPathName(String path, [Out] StringBuilder longPathBuffer, int bufferLength);
        internal static SafeFileHandle SafeCreateFile(String lpFileName, int dwDesiredAccess, System.IO.FileShare dwShareMode, SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile)
        {
            SafeFileHandle handle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
            if (!handle.IsInvalid)
            {
                int fileType = Win32Native.GetFileType(handle);
                if (fileType != Win32Native.FILE_TYPE_DISK)
                {
                    handle.Dispose();
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_FileStreamOnNonFiles"));
                }
            }

            return handle;
        }

        internal static SafeFileHandle UnsafeCreateFile(String lpFileName, int dwDesiredAccess, System.IO.FileShare dwShareMode, SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile)
        {
            SafeFileHandle handle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
            return handle;
        }

        private static extern SafeFileHandle CreateFile(String lpFileName, int dwDesiredAccess, System.IO.FileShare dwShareMode, SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);
        internal static extern SafeFileMappingHandle CreateFileMapping(SafeFileHandle hFile, IntPtr lpAttributes, uint fProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, String lpName);
        internal static extern IntPtr MapViewOfFile(SafeFileMappingHandle handle, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumerOfBytesToMap);
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);
        internal static extern bool CloseHandle(IntPtr handle);
        internal static extern int GetFileType(SafeFileHandle handle);
        internal static extern bool SetEndOfFile(SafeFileHandle hFile);
        internal static extern bool FlushFileBuffers(SafeFileHandle hFile);
        private unsafe static extern int SetFilePointerWin32(SafeFileHandle handle, int lo, int *hi, int origin);
        internal unsafe static long SetFilePointer(SafeFileHandle handle, long offset, System.IO.SeekOrigin origin, out int hr)
        {
            hr = 0;
            int lo = (int)offset;
            int hi = (int)(offset >> 32);
            lo = SetFilePointerWin32(handle, lo, &hi, (int)origin);
            if (lo == -1 && ((hr = Marshal.GetLastWin32Error()) != 0))
                return -1;
            return (long)(((ulong)((uint)hi)) << 32) | ((uint)lo);
        }

        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte *bytes, int numBytesToRead, IntPtr numBytesRead_mustBeZero, NativeOverlapped*overlapped);
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte *bytes, int numBytesToRead, out int numBytesRead, IntPtr mustBeZero);
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte *bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped*lpOverlapped);
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte *bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);
        internal static unsafe extern bool CancelIoEx(SafeFileHandle handle, NativeOverlapped*lpOverlapped);
        internal static extern bool GetDiskFreeSpaceEx(String drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);
        internal static extern int GetDriveType(String drive);
        internal static extern bool GetVolumeInformation(String drive, [Out] StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, [Out] StringBuilder fileSystemName, int fileSystemNameBufLen);
        internal static extern bool SetVolumeLabel(String driveLetter, String volumeName);
        internal static extern bool QueryPerformanceCounter(out long value);
        internal static extern bool QueryPerformanceFrequency(out long value);
        internal static extern SafeWaitHandle CreateSemaphore(SECURITY_ATTRIBUTES lpSecurityAttributes, int initialCount, int maximumCount, String name);
        internal static extern bool ReleaseSemaphore(SafeWaitHandle handle, int releaseCount, out int previousCount);
        internal static extern SafeWaitHandle OpenSemaphore(int desiredAccess, bool inheritHandle, String name);
        internal const int FIND_STARTSWITH = 0x00100000;
        internal const int FIND_ENDSWITH = 0x00200000;
        internal const int FIND_FROMSTART = 0x00400000;
        internal const int FIND_FROMEND = 0x00800000;
        internal static extern int GetWindowsDirectory([Out] StringBuilder sb, int length);
        internal static extern int GetSystemDirectory([Out] StringBuilder sb, int length);
        internal unsafe static extern bool SetFileTime(SafeFileHandle hFile, FILE_TIME*creationTime, FILE_TIME*lastAccessTime, FILE_TIME*lastWriteTime);
        internal static extern int GetFileSize(SafeFileHandle hFile, out int highSize);
        internal static extern bool LockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);
        internal static extern bool UnlockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh);
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        internal const int STD_INPUT_HANDLE = -10;
        internal const int STD_OUTPUT_HANDLE = -11;
        internal const int STD_ERROR_HANDLE = -12;
        internal static extern IntPtr GetStdHandle(int nStdHandle);
        internal const int CTRL_C_EVENT = 0;
        internal const int CTRL_BREAK_EVENT = 1;
        internal const int CTRL_CLOSE_EVENT = 2;
        internal const int CTRL_LOGOFF_EVENT = 5;
        internal const int CTRL_SHUTDOWN_EVENT = 6;
        internal const short KEY_EVENT = 1;
        internal const int FILE_TYPE_DISK = 0x0001;
        internal const int FILE_TYPE_CHAR = 0x0002;
        internal const int FILE_TYPE_PIPE = 0x0003;
        internal const int REPLACEFILE_WRITE_THROUGH = 0x1;
        internal const int REPLACEFILE_IGNORE_MERGE_ERRORS = 0x2;
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
        internal const uint FILE_MAP_WRITE = 0x0002;
        internal const uint FILE_MAP_READ = 0x0004;
        internal const int FILE_ATTRIBUTE_READONLY = 0x00000001;
        internal const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        internal const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        internal const int IO_REPARSE_TAG_MOUNT_POINT = unchecked ((int)0xA0000003);
        internal const int PAGE_READWRITE = 0x04;
        internal const int MEM_COMMIT = 0x1000;
        internal const int MEM_RESERVE = 0x2000;
        internal const int MEM_RELEASE = 0x8000;
        internal const int MEM_FREE = 0x10000;
        internal const int ERROR_SUCCESS = 0x0;
        internal const int ERROR_INVALID_FUNCTION = 0x1;
        internal const int ERROR_FILE_NOT_FOUND = 0x2;
        internal const int ERROR_PATH_NOT_FOUND = 0x3;
        internal const int ERROR_ACCESS_DENIED = 0x5;
        internal const int ERROR_INVALID_HANDLE = 0x6;
        internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
        internal const int ERROR_INVALID_DATA = 0xd;
        internal const int ERROR_INVALID_DRIVE = 0xf;
        internal const int ERROR_NO_MORE_FILES = 0x12;
        internal const int ERROR_NOT_READY = 0x15;
        internal const int ERROR_BAD_LENGTH = 0x18;
        internal const int ERROR_SHARING_VIOLATION = 0x20;
        internal const int ERROR_NOT_SUPPORTED = 0x32;
        internal const int ERROR_FILE_EXISTS = 0x50;
        internal const int ERROR_INVALID_PARAMETER = 0x57;
        internal const int ERROR_BROKEN_PIPE = 0x6D;
        internal const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;
        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
        internal const int ERROR_INVALID_NAME = 0x7B;
        internal const int ERROR_BAD_PATHNAME = 0xA1;
        internal const int ERROR_ALREADY_EXISTS = 0xB7;
        internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
        internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;
        internal const int ERROR_NO_DATA = 0xE8;
        internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
        internal const int ERROR_MORE_DATA = 0xEA;
        internal const int ERROR_DIRECTORY = 0x10B;
        internal const int ERROR_OPERATION_ABORTED = 0x3E3;
        internal const int ERROR_NOT_FOUND = 0x490;
        internal const int ERROR_NO_TOKEN = 0x3f0;
        internal const int ERROR_DLL_INIT_FAILED = 0x45A;
        internal const int ERROR_NON_ACCOUNT_SID = 0x4E9;
        internal const int ERROR_NOT_ALL_ASSIGNED = 0x514;
        internal const int ERROR_UNKNOWN_REVISION = 0x519;
        internal const int ERROR_INVALID_OWNER = 0x51B;
        internal const int ERROR_INVALID_PRIMARY_GROUP = 0x51C;
        internal const int ERROR_NO_SUCH_PRIVILEGE = 0x521;
        internal const int ERROR_PRIVILEGE_NOT_HELD = 0x522;
        internal const int ERROR_NONE_MAPPED = 0x534;
        internal const int ERROR_INVALID_ACL = 0x538;
        internal const int ERROR_INVALID_SID = 0x539;
        internal const int ERROR_INVALID_SECURITY_DESCR = 0x53A;
        internal const int ERROR_BAD_IMPERSONATION_LEVEL = 0x542;
        internal const int ERROR_CANT_OPEN_ANONYMOUS = 0x543;
        internal const int ERROR_NO_SECURITY_ON_OBJECT = 0x546;
        internal const int ERROR_TRUSTED_RELATIONSHIP_FAILURE = 0x6FD;
        internal const uint STATUS_SUCCESS = 0x00000000;
        internal const uint STATUS_SOME_NOT_MAPPED = 0x00000107;
        internal const uint STATUS_NO_MEMORY = 0xC0000017;
        internal const uint STATUS_OBJECT_NAME_NOT_FOUND = 0xC0000034;
        internal const uint STATUS_NONE_MAPPED = 0xC0000073;
        internal const uint STATUS_INSUFFICIENT_RESOURCES = 0xC000009A;
        internal const uint STATUS_ACCESS_DENIED = 0xC0000022;
        internal const int INVALID_FILE_SIZE = -1;
        internal const int STATUS_ACCOUNT_RESTRICTION = unchecked ((int)0xC000006E);
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            BCLDebug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!");
            return unchecked (((int)0x80070000) | errorCode);
        }

        internal class WIN32_FIND_DATA
        {
            internal int dwFileAttributes = 0;
            internal uint ftCreationTime_dwLowDateTime = 0;
            internal uint ftCreationTime_dwHighDateTime = 0;
            internal uint ftLastAccessTime_dwLowDateTime = 0;
            internal uint ftLastAccessTime_dwHighDateTime = 0;
            internal uint ftLastWriteTime_dwLowDateTime = 0;
            internal uint ftLastWriteTime_dwHighDateTime = 0;
            internal int nFileSizeHigh = 0;
            internal int nFileSizeLow = 0;
            internal int dwReserved0 = 0;
            internal int dwReserved1 = 0;
            internal String cFileName = null;
            internal String cAlternateFileName = null;
        }

        private static extern bool CopyFileEx(String src, String dst, IntPtr progressRoutine, IntPtr progressData, ref uint cancel, uint flags);
        internal static bool CopyFile(String src, String dst, bool failIfExists)
        {
            uint cancel = 0;
            return CopyFileEx(src, dst, IntPtr.Zero, IntPtr.Zero, ref cancel, failIfExists ? 1U : 0U);
        }

        internal static extern bool CreateDirectory(String path, SECURITY_ATTRIBUTES lpSecurityAttributes);
        internal static extern bool DeleteFile(String path);
        internal static extern bool ReplaceFile(String replacedFileName, String replacementFileName, String backupFileName, int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved);
        internal static extern bool DecryptFile(String path, int reservedMustBeZero);
        internal static extern bool EncryptFile(String path);
        internal static extern SafeFindHandle FindFirstFile(String fileName, [In, Out] Win32Native.WIN32_FIND_DATA data);
        internal static extern bool FindNextFile(SafeFindHandle hndFindFile, [In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_DATA lpFindFileData);
        internal static extern bool FindClose(IntPtr handle);
        internal static extern int GetCurrentDirectory(int nBufferLength, [Out] StringBuilder lpBuffer);
        internal static extern bool GetFileAttributesEx(String name, int fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);
        internal static extern bool SetFileAttributes(String name, int attr);
        internal static extern int GetLogicalDrives();
        internal static extern uint GetTempFileName(String tmpPath, String prefix, uint uniqueIdOrZero, [Out] StringBuilder tmpFileName);
        private static extern bool MoveFileEx(String src, String dst, uint flags);
        internal static bool MoveFile(String src, String dst)
        {
            return MoveFileEx(src, dst, 2);
        }

        internal static extern bool DeleteVolumeMountPoint(String mountPoint);
        internal static extern bool RemoveDirectory(String path);
        internal static extern bool SetCurrentDirectory(String path);
        private static extern int SetErrorMode_VistaAndOlder(int newMode);
        private static extern bool SetErrorMode_Win7AndNewer(int newMode, out int oldMode);
        private static readonly Version ThreadErrorModeMinOsVersion = new Version(6, 1, 7600);
        internal static int SetErrorMode(int newMode)
        {
            return SetErrorMode_VistaAndOlder(newMode);
        }

        internal const int LCID_SUPPORTED = 0x00000002;
        internal static extern unsafe int WideCharToMultiByte(uint cp, uint flags, char *pwzSource, int cchSource, byte *pbDestBuffer, int cbDestBuffer, IntPtr null1, IntPtr null2);
        internal delegate bool ConsoleCtrlHandlerRoutine(int controlType);
        internal static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerRoutine handler, bool addOrRemove);
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);
        internal static extern int GetEnvironmentVariable(string lpName, [Out] StringBuilder lpValue, int size);
        internal static unsafe extern char *GetEnvironmentStrings();
        internal static unsafe extern bool FreeEnvironmentStrings(char *pStrings);
        internal static extern uint GetCurrentProcessId();
        internal static extern bool GetUserName([Out] StringBuilder lpBuffer, ref int nSize);
        internal extern static int GetComputerName([Out] StringBuilder nameBuffer, ref int bufferSize);
        internal extern static int CoCreateGuid(out Guid guid);
        internal static extern IntPtr CoTaskMemAlloc(UIntPtr cb);
        internal static extern void CoTaskMemFree(IntPtr ptr);
        internal static extern IntPtr CoTaskMemRealloc(IntPtr pv, UIntPtr cb);
        internal struct COORD
        {
            internal short X;
            internal short Y;
        }

        internal struct SMALL_RECT
        {
            internal short Left;
            internal short Top;
            internal short Right;
            internal short Bottom;
        }

        internal struct CONSOLE_SCREEN_BUFFER_INFO
        {
            internal COORD dwSize;
            internal COORD dwCursorPosition;
            internal short wAttributes;
            internal SMALL_RECT srWindow;
            internal COORD dwMaximumWindowSize;
        }

        internal struct CONSOLE_CURSOR_INFO
        {
            internal int dwSize;
            internal bool bVisible;
        }

        internal struct KeyEventRecord
        {
            internal bool keyDown;
            internal short repeatCount;
            internal short virtualKeyCode;
            internal short virtualScanCode;
            internal char uChar;
            internal int controlKeyState;
        }

        internal struct InputRecord
        {
            internal short eventType;
            internal KeyEventRecord keyEvent;
        }

        [Flags]
        internal enum Color : short
        {
            Black = 0,
            ForegroundBlue = 0x1,
            ForegroundGreen = 0x2,
            ForegroundRed = 0x4,
            ForegroundYellow = 0x6,
            ForegroundIntensity = 0x8,
            BackgroundBlue = 0x10,
            BackgroundGreen = 0x20,
            BackgroundRed = 0x40,
            BackgroundYellow = 0x60,
            BackgroundIntensity = 0x80,
            ForegroundMask = 0xf,
            BackgroundMask = 0xf0,
            ColorMask = 0xff
        }

        internal struct CHAR_INFO
        {
            ushort charData;
            short attributes;
        }

        internal const int ENABLE_PROCESSED_INPUT = 0x0001;
        internal const int ENABLE_LINE_INPUT = 0x0002;
        internal const int ENABLE_ECHO_INPUT = 0x0004;
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int mode);
        internal static extern bool Beep(int frequency, int duration);
        internal static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);
        internal static extern bool SetConsoleScreenBufferSize(IntPtr hConsoleOutput, COORD size);
        internal static extern COORD GetLargestConsoleWindowSize(IntPtr hConsoleOutput);
        internal static extern bool FillConsoleOutputCharacter(IntPtr hConsoleOutput, char character, int nLength, COORD dwWriteCoord, out int pNumCharsWritten);
        internal static extern bool FillConsoleOutputAttribute(IntPtr hConsoleOutput, short wColorAttribute, int numCells, COORD startCoord, out int pNumBytesWritten);
        internal static unsafe extern bool SetConsoleWindowInfo(IntPtr hConsoleOutput, bool absolute, SMALL_RECT*consoleWindow);
        internal static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, short attributes);
        internal static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, COORD cursorPosition);
        internal static extern bool GetConsoleCursorInfo(IntPtr hConsoleOutput, out CONSOLE_CURSOR_INFO cci);
        internal static extern bool SetConsoleCursorInfo(IntPtr hConsoleOutput, ref CONSOLE_CURSOR_INFO cci);
        internal static extern bool SetConsoleTitle(String title);
        internal static extern bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);
        internal static extern bool PeekConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);
        internal static unsafe extern bool ReadConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO*pBuffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT readRegion);
        internal static unsafe extern bool ReadConsoleW(SafeFileHandle hConsoleInput, Byte*lpBuffer, Int32 nNumberOfCharsToRead, out Int32 lpNumberOfCharsRead, IntPtr pInputControl);
        internal static unsafe extern bool WriteConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO*buffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT writeRegion);
        internal static unsafe extern bool WriteConsoleW(SafeFileHandle hConsoleOutput, Byte*lpBuffer, Int32 nNumberOfCharsToWrite, out Int32 lpNumberOfCharsWritten, IntPtr lpReservedMustBeNull);
        internal static extern short GetKeyState(int virtualKeyCode);
        internal static extern uint GetConsoleCP();
        internal static extern bool SetConsoleCP(uint codePage);
        internal static extern uint GetConsoleOutputCP();
        internal static extern bool SetConsoleOutputCP(uint codePage);
        internal static extern int RegConnectRegistry(String machineName, SafeRegistryHandle key, out SafeRegistryHandle result);
        internal static extern int RegCreateKeyEx(SafeRegistryHandle hKey, String lpSubKey, int Reserved, String lpClass, int dwOptions, int samDesired, SECURITY_ATTRIBUTES lpSecurityAttributes, out SafeRegistryHandle hkResult, out int lpdwDisposition);
        internal static extern int RegDeleteKey(SafeRegistryHandle hKey, String lpSubKey);
        internal static extern int RegDeleteKeyEx(SafeRegistryHandle hKey, String lpSubKey, int samDesired, int Reserved);
        internal static extern int RegDeleteValue(SafeRegistryHandle hKey, String lpValueName);
        internal unsafe static extern int RegEnumKeyEx(SafeRegistryHandle hKey, int dwIndex, char *lpName, ref int lpcbName, int[] lpReserved, [Out] StringBuilder lpClass, int[] lpcbClass, long[] lpftLastWriteTime);
        internal unsafe static extern int RegEnumValue(SafeRegistryHandle hKey, int dwIndex, char *lpValueName, ref int lpcbValueName, IntPtr lpReserved_MustBeZero, int[] lpType, byte[] lpData, int[] lpcbData);
        internal static extern int RegFlushKey(SafeRegistryHandle hKey);
        internal static extern int RegOpenKeyEx(SafeRegistryHandle hKey, String lpSubKey, int ulOptions, int samDesired, out SafeRegistryHandle hkResult);
        internal static extern int RegOpenKeyEx(IntPtr hKey, String lpSubKey, int ulOptions, int samDesired, out SafeRegistryHandle hkResult);
        internal static extern int RegQueryInfoKey(SafeRegistryHandle hKey, [Out] StringBuilder lpClass, int[] lpcbClass, IntPtr lpReserved_MustBeZero, ref int lpcSubKeys, int[] lpcbMaxSubKeyLen, int[] lpcbMaxClassLen, ref int lpcValues, int[] lpcbMaxValueNameLen, int[] lpcbMaxValueLen, int[] lpcbSecurityDescriptor, int[] lpftLastWriteTime);
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, int[] lpReserved, ref int lpType, [Out] byte[] lpData, ref int lpcbData);
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, int[] lpReserved, ref int lpType, ref int lpData, ref int lpcbData);
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, int[] lpReserved, ref int lpType, ref long lpData, ref int lpcbData);
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, int[] lpReserved, ref int lpType, [Out] char[] lpData, ref int lpcbData);
        internal static extern int RegQueryValueEx(SafeRegistryHandle hKey, String lpValueName, int[] lpReserved, ref int lpType, [Out] StringBuilder lpData, ref int lpcbData);
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName, int Reserved, RegistryValueKind dwType, byte[] lpData, int cbData);
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName, int Reserved, RegistryValueKind dwType, ref int lpData, int cbData);
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName, int Reserved, RegistryValueKind dwType, ref long lpData, int cbData);
        internal static extern int RegSetValueEx(SafeRegistryHandle hKey, String lpValueName, int Reserved, RegistryValueKind dwType, String lpData, int cbData);
        internal static extern int ExpandEnvironmentStrings(String lpSrc, [Out] StringBuilder lpDst, int nSize);
        internal static extern IntPtr LocalReAlloc(IntPtr handle, IntPtr sizetcbBytes, int uFlags);
        internal const int SHGFP_TYPE_CURRENT = 0;
        internal const int UOI_FLAGS = 1;
        internal const int WSF_VISIBLE = 1;
        internal const int CSIDL_FLAG_CREATE = 0x8000;
        internal const int CSIDL_FLAG_DONT_VERIFY = 0x4000;
        internal const int CSIDL_ADMINTOOLS = 0x0030;
        internal const int CSIDL_CDBURN_AREA = 0x003b;
        internal const int CSIDL_COMMON_ADMINTOOLS = 0x002f;
        internal const int CSIDL_COMMON_DOCUMENTS = 0x002e;
        internal const int CSIDL_COMMON_MUSIC = 0x0035;
        internal const int CSIDL_COMMON_OEM_LINKS = 0x003a;
        internal const int CSIDL_COMMON_PICTURES = 0x0036;
        internal const int CSIDL_COMMON_STARTMENU = 0x0016;
        internal const int CSIDL_COMMON_PROGRAMS = 0X0017;
        internal const int CSIDL_COMMON_STARTUP = 0x0018;
        internal const int CSIDL_COMMON_DESKTOPDIRECTORY = 0x0019;
        internal const int CSIDL_COMMON_TEMPLATES = 0x002d;
        internal const int CSIDL_COMMON_VIDEO = 0x0037;
        internal const int CSIDL_FONTS = 0x0014;
        internal const int CSIDL_MYVIDEO = 0x000e;
        internal const int CSIDL_NETHOOD = 0x0013;
        internal const int CSIDL_PRINTHOOD = 0x001b;
        internal const int CSIDL_PROFILE = 0x0028;
        internal const int CSIDL_PROGRAM_FILES_COMMONX86 = 0x002c;
        internal const int CSIDL_PROGRAM_FILESX86 = 0x002a;
        internal const int CSIDL_RESOURCES = 0x0038;
        internal const int CSIDL_RESOURCES_LOCALIZED = 0x0039;
        internal const int CSIDL_SYSTEMX86 = 0x0029;
        internal const int CSIDL_WINDOWS = 0x0024;
        internal const int CSIDL_APPDATA = 0x001a;
        internal const int CSIDL_COMMON_APPDATA = 0x0023;
        internal const int CSIDL_LOCAL_APPDATA = 0x001c;
        internal const int CSIDL_COOKIES = 0x0021;
        internal const int CSIDL_FAVORITES = 0x0006;
        internal const int CSIDL_HISTORY = 0x0022;
        internal const int CSIDL_INTERNET_CACHE = 0x0020;
        internal const int CSIDL_PROGRAMS = 0x0002;
        internal const int CSIDL_RECENT = 0x0008;
        internal const int CSIDL_SENDTO = 0x0009;
        internal const int CSIDL_STARTMENU = 0x000b;
        internal const int CSIDL_STARTUP = 0x0007;
        internal const int CSIDL_SYSTEM = 0x0025;
        internal const int CSIDL_TEMPLATES = 0x0015;
        internal const int CSIDL_DESKTOPDIRECTORY = 0x0010;
        internal const int CSIDL_PERSONAL = 0x0005;
        internal const int CSIDL_PROGRAM_FILES = 0x0026;
        internal const int CSIDL_PROGRAM_FILES_COMMON = 0x002b;
        internal const int CSIDL_DESKTOP = 0x0000;
        internal const int CSIDL_DRIVES = 0x0011;
        internal const int CSIDL_MYMUSIC = 0x000d;
        internal const int CSIDL_MYPICTURES = 0x0027;
        internal static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, [Out] StringBuilder lpszPath);
        internal const int NameSamCompatible = 2;
        internal static extern byte GetUserNameEx(int format, [Out] StringBuilder domainName, ref uint domainNameLen);
        internal static extern bool LookupAccountName(string machineName, string accountName, byte[] sid, ref int sidLen, [Out] StringBuilder domainName, ref uint domainNameLen, out int peUse);
        internal static extern IntPtr GetProcessWindowStation();
        internal static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, [MarshalAs(UnmanagedType.LPStruct)] USEROBJECTFLAGS pvBuffer, int nLength, ref int lpnLengthNeeded);
        internal static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam, String lParam, uint fuFlags, uint uTimeout, IntPtr lpdwResult);
        internal class USEROBJECTFLAGS
        {
            internal int fInherit = 0;
            internal int fReserved = 0;
            internal int dwFlags = 0;
        }

        internal static extern int SystemFunction040([In, Out] SafeBSTRHandle pDataIn, [In] uint cbDataIn, [In] uint dwFlags);
        internal static extern int SystemFunction041([In, Out] SafeBSTRHandle pDataIn, [In] uint cbDataIn, [In] uint dwFlags);
        internal static extern int RtlNtStatusToDosError([In] int status);
        internal static extern uint BCryptGetFipsAlgorithmMode([MarshalAs(UnmanagedType.U1), Out] out bool pfEnabled);
        internal static extern bool AdjustTokenPrivileges([In] SafeAccessTokenHandle TokenHandle, [In] bool DisableAllPrivileges, [In] ref TOKEN_PRIVILEGE NewState, [In] uint BufferLength, [In, Out] ref TOKEN_PRIVILEGE PreviousState, [In, Out] ref uint ReturnLength);
        internal static extern bool AllocateLocallyUniqueId([In, Out] ref LUID Luid);
        internal static extern bool CheckTokenMembership([In] SafeAccessTokenHandle TokenHandle, [In] byte[] SidToCheck, [In, Out] ref bool IsMember);
        internal static extern BOOL ConvertSdToStringSd(byte[] securityDescriptor, uint requestedRevision, ULONG securityInformation, out IntPtr resultString, ref ULONG resultStringLength);
        internal static extern BOOL ConvertStringSdToSd(string stringSd, uint stringSdRevision, out IntPtr resultSd, ref ULONG resultSdLength);
        internal static extern BOOL ConvertStringSidToSid(string stringSid, out IntPtr ByteArray);
        internal static extern bool ConvertSidToStringSid(IntPtr Sid, ref IntPtr StringSid);
        internal static extern BOOL CreateWellKnownSid(int sidType, byte[] domainSid, [Out] byte[] resultSid, ref uint resultSidLength);
        internal static extern bool DuplicateHandle([In] IntPtr hSourceProcessHandle, [In] IntPtr hSourceHandle, [In] IntPtr hTargetProcessHandle, [In, Out] ref SafeAccessTokenHandle lpTargetHandle, [In] uint dwDesiredAccess, [In] bool bInheritHandle, [In] uint dwOptions);
        internal static extern bool DuplicateHandle([In] IntPtr hSourceProcessHandle, [In] SafeAccessTokenHandle hSourceHandle, [In] IntPtr hTargetProcessHandle, [In, Out] ref SafeAccessTokenHandle lpTargetHandle, [In] uint dwDesiredAccess, [In] bool bInheritHandle, [In] uint dwOptions);
        internal static extern BOOL IsEqualDomainSid(byte[] sid1, byte[] sid2, out bool result);
        internal static extern IntPtr GetCurrentProcess();
        internal static extern IntPtr GetCurrentThread();
        internal static extern uint GetSecurityDescriptorLength(IntPtr byteArray);
        internal static extern uint GetSecurityInfoByHandle(SafeHandle handle, uint objectType, uint securityInformation, out IntPtr sidOwner, out IntPtr sidGroup, out IntPtr dacl, out IntPtr sacl, out IntPtr securityDescriptor);
        internal static extern uint GetSecurityInfoByName(string name, uint objectType, uint securityInformation, out IntPtr sidOwner, out IntPtr sidGroup, out IntPtr dacl, out IntPtr sacl, out IntPtr securityDescriptor);
        internal static extern bool GetTokenInformation([In] IntPtr TokenHandle, [In] uint TokenInformationClass, [In] SafeLocalAllocHandle TokenInformation, [In] uint TokenInformationLength, [Out] out uint ReturnLength);
        internal static extern bool GetTokenInformation([In] SafeAccessTokenHandle TokenHandle, [In] uint TokenInformationClass, [In] SafeLocalAllocHandle TokenInformation, [In] uint TokenInformationLength, [Out] out uint ReturnLength);
        internal static extern BOOL GetWindowsAccountDomainSid(byte[] sid, [Out] byte[] resultSid, ref uint resultSidLength);
        internal enum SECURITY_IMPERSONATION_LEVEL
        {
            Anonymous = 0,
            Identification = 1,
            Impersonation = 2,
            Delegation = 3
        }

        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_INVALID = 0;
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_INT64 = 1;
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_UINT64 = 2;
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_STRING = 3;
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_FQBN = 4;
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_SID = 5;
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_BOOLEAN = 6;
        internal const int CLAIM_SECURITY_ATTRIBUTE_TYPE_OCTET_STRING = 16;
        internal const int CLAIM_SECURITY_ATTRIBUTE_NON_INHERITABLE = 1;
        internal const int CLAIM_SECURITY_ATTRIBUTE_VALUE_CASE_SENSITIVE = 2;
        internal const int CLAIM_SECURITY_ATTRIBUTE_USE_FOR_DENY_ONLY = 4;
        internal const int CLAIM_SECURITY_ATTRIBUTE_DISABLED_BY_DEFAULT = 8;
        internal const int CLAIM_SECURITY_ATTRIBUTE_DISABLED = 16;
        internal const int CLAIM_SECURITY_ATTRIBUTE_MANDATORY = 32;
        internal const int CLAIM_SECURITY_ATTRIBUTE_VALID_FLAGS = CLAIM_SECURITY_ATTRIBUTE_NON_INHERITABLE | CLAIM_SECURITY_ATTRIBUTE_VALUE_CASE_SENSITIVE | CLAIM_SECURITY_ATTRIBUTE_USE_FOR_DENY_ONLY | CLAIM_SECURITY_ATTRIBUTE_DISABLED_BY_DEFAULT | CLAIM_SECURITY_ATTRIBUTE_DISABLED | CLAIM_SECURITY_ATTRIBUTE_MANDATORY;
        internal struct CLAIM_SECURITY_ATTRIBUTE_INFORMATION_V1
        {
            public IntPtr pAttributeV1;
        }

        internal struct CLAIM_SECURITY_ATTRIBUTES_INFORMATION
        {
            public ushort Version;
            public ushort Reserved;
            public uint AttributeCount;
            public CLAIM_SECURITY_ATTRIBUTE_INFORMATION_V1 Attribute;
        }

        internal struct CLAIM_SECURITY_ATTRIBUTE_FQBN_VALUE
        {
            public ulong Version;
            public string Name;
        }

        internal struct CLAIM_SECURITY_ATTRIBUTE_OCTET_STRING_VALUE
        {
            public IntPtr pValue;
            public uint ValueLength;
        }

        internal struct CLAIM_VALUES_ATTRIBUTE_V1
        {
            public IntPtr pInt64;
            public IntPtr pUint64;
            public IntPtr ppString;
            public IntPtr pFqbn;
            public IntPtr pOctetString;
        }

        internal struct CLAIM_SECURITY_ATTRIBUTE_V1
        {
            public string Name;
            public ushort ValueType;
            public ushort Reserved;
            public uint Flags;
            public uint ValueCount;
            public CLAIM_VALUES_ATTRIBUTE_V1 Values;
        }

        internal static extern BOOL IsWellKnownSid(byte[] sid, int type);
        internal static extern uint LsaOpenPolicy(string systemName, ref LSA_OBJECT_ATTRIBUTES attributes, int accessMask, out SafeLsaPolicyHandle handle);
        internal static extern bool LookupPrivilegeValue([In] string lpSystemName, [In] string lpName, [In, Out] ref LUID Luid);
        internal static extern uint LsaLookupSids(SafeLsaPolicyHandle handle, int count, IntPtr[] sids, ref SafeLsaMemoryHandle referencedDomains, ref SafeLsaMemoryHandle names);
        internal static extern int LsaFreeMemory(IntPtr handle);
        internal static extern uint LsaLookupNames(SafeLsaPolicyHandle handle, int count, UNICODE_STRING[] names, ref SafeLsaMemoryHandle referencedDomains, ref SafeLsaMemoryHandle sids);
        internal static extern uint LsaLookupNames2(SafeLsaPolicyHandle handle, int flags, int count, UNICODE_STRING[] names, ref SafeLsaMemoryHandle referencedDomains, ref SafeLsaMemoryHandle sids);
        internal static extern int LsaConnectUntrusted([In, Out] ref SafeLsaLogonProcessHandle LsaHandle);
        internal static extern int LsaGetLogonSessionData([In] ref LUID LogonId, [In, Out] ref SafeLsaReturnBufferHandle ppLogonSessionData);
        internal static extern int LsaLogonUser([In] SafeLsaLogonProcessHandle LsaHandle, [In] ref UNICODE_INTPTR_STRING OriginName, [In] uint LogonType, [In] uint AuthenticationPackage, [In] IntPtr AuthenticationInformation, [In] uint AuthenticationInformationLength, [In] IntPtr LocalGroups, [In] ref TOKEN_SOURCE SourceContext, [In, Out] ref SafeLsaReturnBufferHandle ProfileBuffer, [In, Out] ref uint ProfileBufferLength, [In, Out] ref LUID LogonId, [In, Out] ref SafeAccessTokenHandle Token, [In, Out] ref QUOTA_LIMITS Quotas, [In, Out] ref int SubStatus);
        internal static extern int LsaLookupAuthenticationPackage([In] SafeLsaLogonProcessHandle LsaHandle, [In] ref UNICODE_INTPTR_STRING PackageName, [In, Out] ref uint AuthenticationPackage);
        internal static extern int LsaRegisterLogonProcess([In] ref UNICODE_INTPTR_STRING LogonProcessName, [In, Out] ref SafeLsaLogonProcessHandle LsaHandle, [In, Out] ref IntPtr SecurityMode);
        internal static extern int LsaDeregisterLogonProcess(IntPtr handle);
        internal static extern int LsaClose(IntPtr handle);
        internal static extern int LsaFreeReturnBuffer(IntPtr handle);
        internal static extern bool OpenProcessToken([In] IntPtr ProcessToken, [In] TokenAccessLevels DesiredAccess, [Out] out SafeAccessTokenHandle TokenHandle);
        internal static extern bool OpenThreadToken([In] IntPtr ThreadHandle, [In] TokenAccessLevels DesiredAccess, [In, MarshalAs(UnmanagedType.Bool)] bool OpenAsSelf, [Out] out SafeAccessTokenHandle TokenHandle);
        internal static extern uint SetSecurityInfoByName(string name, uint objectType, uint securityInformation, byte[] owner, byte[] group, byte[] dacl, byte[] sacl);
        internal static extern uint SetSecurityInfoByHandle(SafeHandle handle, uint objectType, uint securityInformation, byte[] owner, byte[] group, byte[] dacl, byte[] sacl);
        internal unsafe static extern int WideCharToMultiByte(int CodePage, UInt32 dwFlags, char *lpWideCharStr, int cchWideChar, byte *lpMultiByteStr, int cchMultiByte, char *lpDefaultChar, bool *lpUsedDefaultChar);
        internal unsafe static extern int MultiByteToWideChar(int CodePage, UInt32 dwFlags, byte *lpMultiByteStr, int cchMultiByte, char *lpWideCharStr, int cchWideChar);
        internal extern static bool QueryUnbiasedInterruptTime(out ulong UnbiasedTime);
    }
}