namespace Microsoft.Win32
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using System.IO;
    using System.Runtime.Remoting;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.Versioning;
    using System.Globalization;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;

    public enum RegistryHive
    {
        ClassesRoot = unchecked ((int)0x80000000),
        CurrentUser = unchecked ((int)0x80000001),
        LocalMachine = unchecked ((int)0x80000002),
        Users = unchecked ((int)0x80000003),
        PerformanceData = unchecked ((int)0x80000004),
        CurrentConfig = unchecked ((int)0x80000005),
        DynData = unchecked ((int)0x80000006)}

    public sealed class RegistryKey : IDisposable
    {
        internal static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked ((int)0x80000000));
        internal static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked ((int)0x80000001));
        internal static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked ((int)0x80000002));
        internal static readonly IntPtr HKEY_USERS = new IntPtr(unchecked ((int)0x80000003));
        internal static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked ((int)0x80000004));
        internal static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked ((int)0x80000005));
        internal static readonly IntPtr HKEY_DYN_DATA = new IntPtr(unchecked ((int)0x80000006));
        private const int STATE_DIRTY = 0x0001;
        private const int STATE_SYSTEMKEY = 0x0002;
        private const int STATE_WRITEACCESS = 0x0004;
        private const int STATE_PERF_DATA = 0x0008;
        private static readonly String[] hkeyNames = new String[]{"HKEY_CLASSES_ROOT", "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE", "HKEY_USERS", "HKEY_PERFORMANCE_DATA", "HKEY_CURRENT_CONFIG", "HKEY_DYN_DATA"};
        private const int MaxKeyLength = 255;
        private const int MaxValueLength = 16383;
        private volatile SafeRegistryHandle hkey = null;
        private volatile int state = 0;
        private volatile String keyName;
        private volatile bool remoteKey = false;
        private volatile RegistryKeyPermissionCheck checkMode;
        private volatile RegistryView regView = RegistryView.Default;
        private enum RegistryInternalCheck
        {
            CheckSubKeyWritePermission = 0,
            CheckSubKeyReadPermission = 1,
            CheckSubKeyCreatePermission = 2,
            CheckSubTreeReadPermission = 3,
            CheckSubTreeWritePermission = 4,
            CheckSubTreeReadWritePermission = 5,
            CheckValueWritePermission = 6,
            CheckValueCreatePermission = 7,
            CheckValueReadPermission = 8,
            CheckKeyReadPermission = 9,
            CheckSubTreePermission = 10,
            CheckOpenSubKeyWithWritablePermission = 11,
            CheckOpenSubKeyPermission = 12
        }

        ;
        private RegistryKey(SafeRegistryHandle hkey, bool writable, RegistryView view): this (hkey, writable, false, false, false, view)
        {
        }

        private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey, bool remoteKey, bool isPerfData, RegistryView view)
        {
            this.hkey = hkey;
            this.keyName = "";
            this.remoteKey = remoteKey;
            this.regView = view;
            if (systemkey)
            {
                this.state |= STATE_SYSTEMKEY;
            }

            if (writable)
            {
                this.state |= STATE_WRITEACCESS;
            }

            if (isPerfData)
                this.state |= STATE_PERF_DATA;
            ValidateKeyView(view);
        }

        public void Close()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (hkey != null)
            {
                if (!IsSystemKey())
                {
                    try
                    {
                        hkey.Dispose();
                    }
                    catch (IOException)
                    {
                    }
                    finally
                    {
                        hkey = null;
                    }
                }
                else if (disposing && IsPerfDataKey())
                {
                    SafeRegistryHandle.RegCloseKey(RegistryKey.HKEY_PERFORMANCE_DATA);
                }
            }
        }

        public void Flush()
        {
            if (hkey != null)
            {
                if (IsDirty())
                {
                    Win32Native.RegFlushKey(hkey);
                }
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        public RegistryKey CreateSubKey(String subkey)
        {
            return CreateSubKey(subkey, checkMode);
        }

        public RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck)
        {
            return CreateSubKeyInternal(subkey, permissionCheck, null, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions options)
        {
            return CreateSubKeyInternal(subkey, permissionCheck, null, options);
        }

        public RegistryKey CreateSubKey(String subkey, bool writable)
        {
            return CreateSubKeyInternal(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, null, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(String subkey, bool writable, RegistryOptions options)
        {
            return CreateSubKeyInternal(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, null, options);
        }

        private unsafe RegistryKey CreateSubKeyInternal(String subkey, RegistryKeyPermissionCheck permissionCheck, object registrySecurityObj, RegistryOptions registryOptions)
        {
            ValidateKeyOptions(registryOptions);
            ValidateKeyName(subkey);
            ValidateKeyMode(permissionCheck);
            EnsureWriteable();
            subkey = FixupName(subkey);
            if (!remoteKey)
            {
                RegistryKey key = InternalOpenSubKey(subkey, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree));
                if (key != null)
                {
                    CheckPermission(RegistryInternalCheck.CheckSubKeyWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);
                    CheckPermission(RegistryInternalCheck.CheckSubTreePermission, subkey, false, permissionCheck);
                    key.checkMode = permissionCheck;
                    return key;
                }
            }

            CheckPermission(RegistryInternalCheck.CheckSubKeyCreatePermission, subkey, false, RegistryKeyPermissionCheck.Default);
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
            int disposition = 0;
            SafeRegistryHandle result = null;
            int ret = Win32Native.RegCreateKeyEx(hkey, subkey, 0, null, (int)registryOptions, GetRegistryKeyAccess(permissionCheck != RegistryKeyPermissionCheck.ReadSubTree) | (int)regView, secAttrs, out result, out disposition);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree), false, remoteKey, false, regView);
                CheckPermission(RegistryInternalCheck.CheckSubTreePermission, subkey, false, permissionCheck);
                key.checkMode = permissionCheck;
                if (subkey.Length == 0)
                    key.keyName = keyName;
                else
                    key.keyName = keyName + "\\" + subkey;
                return key;
            }
            else if (ret != 0)
                Win32Error(ret, keyName + "\\" + subkey);
            BCLDebug.Assert(false, "Unexpected code path in RegistryKey::CreateSubKey");
            return null;
        }

        public void DeleteSubKey(String subkey)
        {
            DeleteSubKey(subkey, true);
        }

        public void DeleteSubKey(String subkey, bool throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);
            EnsureWriteable();
            subkey = FixupName(subkey);
            CheckPermission(RegistryInternalCheck.CheckSubKeyWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);
            RegistryKey key = InternalOpenSubKey(subkey, false);
            if (key != null)
            {
                try
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_RegRemoveSubKey);
                    }
                }
                finally
                {
                    key.Close();
                }

                int ret;
                try
                {
                    ret = Win32Native.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);
                }
                catch (EntryPointNotFoundException)
                {
                    ret = Win32Native.RegDeleteKey(hkey, subkey);
                }

                if (ret != 0)
                {
                    if (ret == Win32Native.ERROR_FILE_NOT_FOUND)
                    {
                        if (throwOnMissingSubKey)
                            ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
                    }
                    else
                        Win32Error(ret, null);
                }
            }
            else
            {
                if (throwOnMissingSubKey)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        }

        public void DeleteSubKeyTree(String subkey)
        {
            DeleteSubKeyTree(subkey, true);
        }

        public void DeleteSubKeyTree(String subkey, Boolean throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);
            if (subkey.Length == 0 && IsSystemKey())
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyDelHive);
            }

            EnsureWriteable();
            subkey = FixupName(subkey);
            CheckPermission(RegistryInternalCheck.CheckSubTreeWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);
            RegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null)
            {
                try
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        String[] keys = key.InternalGetSubKeyNames();
                        for (int i = 0; i < keys.Length; i++)
                        {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }
                finally
                {
                    key.Close();
                }

                int ret;
                try
                {
                    ret = Win32Native.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);
                }
                catch (EntryPointNotFoundException)
                {
                    ret = Win32Native.RegDeleteKey(hkey, subkey);
                }

                if (ret != 0)
                    Win32Error(ret, null);
            }
            else if (throwOnMissingSubKey)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        }

        private void DeleteSubKeyTreeInternal(string subkey)
        {
            RegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null)
            {
                try
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        String[] keys = key.InternalGetSubKeyNames();
                        for (int i = 0; i < keys.Length; i++)
                        {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }
                finally
                {
                    key.Close();
                }

                int ret;
                try
                {
                    ret = Win32Native.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);
                }
                catch (EntryPointNotFoundException)
                {
                    ret = Win32Native.RegDeleteKey(hkey, subkey);
                }

                if (ret != 0)
                    Win32Error(ret, null);
            }
            else
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        }

        public void DeleteValue(String name)
        {
            DeleteValue(name, true);
        }

        public void DeleteValue(String name, bool throwOnMissingValue)
        {
            EnsureWriteable();
            CheckPermission(RegistryInternalCheck.CheckValueWritePermission, name, false, RegistryKeyPermissionCheck.Default);
            int errorCode = Win32Native.RegDeleteValue(hkey, name);
            if (errorCode == Win32Native.ERROR_FILE_NOT_FOUND || errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE)
            {
                if (throwOnMissingValue)
                {
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyValueAbsent);
                }
            }

            BCLDebug.Correctness(errorCode == 0, "RegDeleteValue failed.  Here's your error code: " + errorCode);
        }

        internal static RegistryKey GetBaseKey(IntPtr hKey)
        {
            return GetBaseKey(hKey, RegistryView.Default);
        }

        internal static RegistryKey GetBaseKey(IntPtr hKey, RegistryView view)
        {
            int index = ((int)hKey) & 0x0FFFFFFF;
            BCLDebug.Assert(index >= 0 && index < hkeyNames.Length, "index is out of range!");
            BCLDebug.Assert((((int)hKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!");
            bool isPerf = hKey == HKEY_PERFORMANCE_DATA;
            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, isPerf);
            RegistryKey key = new RegistryKey(srh, true, true, false, isPerf, view);
            key.checkMode = RegistryKeyPermissionCheck.Default;
            key.keyName = hkeyNames[index];
            return key;
        }

        public static RegistryKey OpenBaseKey(RegistryHive hKey, RegistryView view)
        {
            ValidateKeyView(view);
            CheckUnmanagedCodePermission();
            return GetBaseKey((IntPtr)((int)hKey), view);
        }

        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, String machineName)
        {
            return OpenRemoteBaseKey(hKey, machineName, RegistryView.Default);
        }

        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, String machineName, RegistryView view)
        {
            if (machineName == null)
                throw new ArgumentNullException("machineName");
            int index = (int)hKey & 0x0FFFFFFF;
            if (index < 0 || index >= hkeyNames.Length || ((int)hKey & 0xFFFFFFF0) != 0x80000000)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyOutOfRange"));
            }

            ValidateKeyView(view);
            CheckUnmanagedCodePermission();
            SafeRegistryHandle foreignHKey = null;
            int ret = Win32Native.RegConnectRegistry(machineName, new SafeRegistryHandle(new IntPtr((int)hKey), false), out foreignHKey);
            if (ret == Win32Native.ERROR_DLL_INIT_FAILED)
                throw new ArgumentException(Environment.GetResourceString("Arg_DllInitFailure"));
            if (ret != 0)
                Win32ErrorStatic(ret, null);
            if (foreignHKey.IsInvalid)
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyNoRemoteConnect", machineName));
            RegistryKey key = new RegistryKey(foreignHKey, true, false, true, ((IntPtr)hKey) == HKEY_PERFORMANCE_DATA, view);
            key.checkMode = RegistryKeyPermissionCheck.Default;
            key.keyName = hkeyNames[index];
            return key;
        }

        public RegistryKey OpenSubKey(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();
            name = FixupName(name);
            CheckPermission(RegistryInternalCheck.CheckOpenSubKeyWithWritablePermission, name, writable, RegistryKeyPermissionCheck.Default);
            SafeRegistryHandle result = null;
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, GetRegistryKeyAccess(writable) | (int)regView, out result);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false, regView);
                key.checkMode = GetSubKeyPermissonCheck(writable);
                key.keyName = keyName + "\\" + name;
                return key;
            }

            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            }

            return null;
        }

        internal RegistryKey InternalOpenSubKey(String name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();
            SafeRegistryHandle result = null;
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, GetRegistryKeyAccess(writable) | (int)regView, out result);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false, regView);
                key.keyName = keyName + "\\" + name;
                return key;
            }

            return null;
        }

        public RegistryKey OpenSubKey(String name)
        {
            return OpenSubKey(name, false);
        }

        public int SubKeyCount
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                CheckPermission(RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
                return InternalSubKeyCount();
            }
        }

        public RegistryView View
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                EnsureNotDisposed();
                return regView;
            }
        }

        internal int InternalSubKeyCount()
        {
            EnsureNotDisposed();
            int subkeys = 0;
            int junk = 0;
            int ret = Win32Native.RegQueryInfoKey(hkey, null, null, IntPtr.Zero, ref subkeys, null, null, ref junk, null, null, null, null);
            if (ret != 0)
                Win32Error(ret, null);
            return subkeys;
        }

        public String[] GetSubKeyNames()
        {
            CheckPermission(RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
            return InternalGetSubKeyNames();
        }

        internal unsafe String[] InternalGetSubKeyNames()
        {
            EnsureNotDisposed();
            int subkeys = InternalSubKeyCount();
            String[] names = new String[subkeys];
            if (subkeys > 0)
            {
                char[] name = new char[MaxKeyLength + 1];
                int namelen;
                fixed (char *namePtr = &name[0])
                {
                    for (int i = 0; i < subkeys; i++)
                    {
                        namelen = name.Length;
                        int ret = Win32Native.RegEnumKeyEx(hkey, i, namePtr, ref namelen, null, null, null, null);
                        if (ret != 0)
                            Win32Error(ret, null);
                        names[i] = new String(namePtr);
                    }
                }
            }

            return names;
        }

        public int ValueCount
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                CheckPermission(RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
                return InternalValueCount();
            }
        }

        internal int InternalValueCount()
        {
            EnsureNotDisposed();
            int values = 0;
            int junk = 0;
            int ret = Win32Native.RegQueryInfoKey(hkey, null, null, IntPtr.Zero, ref junk, null, null, ref values, null, null, null, null);
            if (ret != 0)
                Win32Error(ret, null);
            return values;
        }

        public unsafe String[] GetValueNames()
        {
            CheckPermission(RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
            EnsureNotDisposed();
            int values = InternalValueCount();
            String[] names = new String[values];
            if (values > 0)
            {
                char[] name = new char[MaxValueLength + 1];
                int namelen;
                fixed (char *namePtr = &name[0])
                {
                    for (int i = 0; i < values; i++)
                    {
                        namelen = name.Length;
                        int ret = Win32Native.RegEnumValue(hkey, i, namePtr, ref namelen, IntPtr.Zero, null, null, null);
                        if (ret != 0)
                        {
                            if (!(IsPerfDataKey() && ret == Win32Native.ERROR_MORE_DATA))
                                Win32Error(ret, null);
                        }

                        names[i] = new String(namePtr);
                    }
                }
            }

            return names;
        }

        public Object GetValue(String name)
        {
            CheckPermission(RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
            return InternalGetValue(name, null, false, true);
        }

        public Object GetValue(String name, Object defaultValue)
        {
            CheckPermission(RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
            return InternalGetValue(name, defaultValue, false, true);
        }

        public Object GetValue(String name, Object defaultValue, RegistryValueOptions options)
        {
            if (options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options), "options");
            }

            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            CheckPermission(RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
            return InternalGetValue(name, defaultValue, doNotExpand, true);
        }

        internal Object InternalGetValue(String name, Object defaultValue, bool doNotExpand, bool checkSecurity)
        {
            if (checkSecurity)
            {
                EnsureNotDisposed();
            }

            Object data = defaultValue;
            int type = 0;
            int datasize = 0;
            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
            if (ret != 0)
            {
                if (IsPerfDataKey())
                {
                    int size = 65000;
                    int sizeInput = size;
                    int r;
                    byte[] blob = new byte[size];
                    while (Win32Native.ERROR_MORE_DATA == (r = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref sizeInput)))
                    {
                        if (size == Int32.MaxValue)
                        {
                            Win32Error(r, name);
                        }
                        else if (size > (Int32.MaxValue / 2))
                        {
                            size = Int32.MaxValue;
                        }
                        else
                        {
                            size *= 2;
                        }

                        sizeInput = size;
                        blob = new byte[size];
                    }

                    if (r != 0)
                        Win32Error(r, name);
                    return blob;
                }
                else
                {
                    if (ret != Win32Native.ERROR_MORE_DATA)
                        return data;
                }
            }

            if (datasize < 0)
            {
                BCLDebug.Assert(false, "[InternalGetValue] RegQueryValue returned ERROR_SUCCESS but gave a negative datasize");
                datasize = 0;
            }

            switch (type)
            {
                case Win32Native.REG_NONE:
                case Win32Native.REG_DWORD_BIG_ENDIAN:
                case Win32Native.REG_BINARY:
                {
                    byte[] blob = new byte[datasize];
                    ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                    data = blob;
                }

                    break;
                case Win32Native.REG_QWORD:
                {
                    if (datasize > 8)
                    {
                        goto case Win32Native.REG_BINARY;
                    }

                    long blob = 0;
                    BCLDebug.Assert(datasize == 8, "datasize==8");
                    ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);
                    data = blob;
                }

                    break;
                case Win32Native.REG_DWORD:
                {
                    if (datasize > 4)
                    {
                        goto case Win32Native.REG_QWORD;
                    }

                    int blob = 0;
                    BCLDebug.Assert(datasize == 4, "datasize==4");
                    ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);
                    data = blob;
                }

                    break;
                case Win32Native.REG_SZ:
                {
                    if (datasize % 2 == 1)
                    {
                        try
                        {
                            datasize = checked (datasize + 1);
                        }
                        catch (OverflowException e)
                        {
                            throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), e);
                        }
                    }

                    char[] blob = new char[datasize / 2];
                    ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                    if (blob.Length > 0 && blob[blob.Length - 1] == (char)0)
                    {
                        data = new String(blob, 0, blob.Length - 1);
                    }
                    else
                    {
                        data = new String(blob);
                    }
                }

                    break;
                case Win32Native.REG_EXPAND_SZ:
                {
                    if (datasize % 2 == 1)
                    {
                        try
                        {
                            datasize = checked (datasize + 1);
                        }
                        catch (OverflowException e)
                        {
                            throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), e);
                        }
                    }

                    char[] blob = new char[datasize / 2];
                    ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                    if (blob.Length > 0 && blob[blob.Length - 1] == (char)0)
                    {
                        data = new String(blob, 0, blob.Length - 1);
                    }
                    else
                    {
                        data = new String(blob);
                    }

                    if (!doNotExpand)
                        data = Environment.ExpandEnvironmentVariables((String)data);
                }

                    break;
                case Win32Native.REG_MULTI_SZ:
                {
                    if (datasize % 2 == 1)
                    {
                        try
                        {
                            datasize = checked (datasize + 1);
                        }
                        catch (OverflowException e)
                        {
                            throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), e);
                        }
                    }

                    char[] blob = new char[datasize / 2];
                    ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                    if (blob.Length > 0 && blob[blob.Length - 1] != (char)0)
                    {
                        try
                        {
                            char[] newBlob = new char[checked (blob.Length + 1)];
                            for (int i = 0; i < blob.Length; i++)
                            {
                                newBlob[i] = blob[i];
                            }

                            newBlob[newBlob.Length - 1] = (char)0;
                            blob = newBlob;
                        }
                        catch (OverflowException e)
                        {
                            throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), e);
                        }

                        blob[blob.Length - 1] = (char)0;
                    }

                    IList<String> strings = new List<String>();
                    int cur = 0;
                    int len = blob.Length;
                    while (ret == 0 && cur < len)
                    {
                        int nextNull = cur;
                        while (nextNull < len && blob[nextNull] != (char)0)
                        {
                            nextNull++;
                        }

                        if (nextNull < len)
                        {
                            BCLDebug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                            if (nextNull - cur > 0)
                            {
                                strings.Add(new String(blob, cur, nextNull - cur));
                            }
                            else
                            {
                                if (nextNull != len - 1)
                                    strings.Add(String.Empty);
                            }
                        }
                        else
                        {
                            strings.Add(new String(blob, cur, len - cur));
                        }

                        cur = nextNull + 1;
                    }

                    data = new String[strings.Count];
                    strings.CopyTo((String[])data, 0);
                }

                    break;
                case Win32Native.REG_LINK:
                default:
                    break;
            }

            return data;
        }

        public RegistryValueKind GetValueKind(string name)
        {
            CheckPermission(RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
            EnsureNotDisposed();
            int type = 0;
            int datasize = 0;
            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
            if (ret != 0)
                Win32Error(ret, null);
            if (type == Win32Native.REG_NONE)
                return RegistryValueKind.None;
            else if (!Enum.IsDefined(typeof (RegistryValueKind), type))
                return RegistryValueKind.Unknown;
            else
                return (RegistryValueKind)type;
        }

        private bool IsDirty()
        {
            return (this.state & STATE_DIRTY) != 0;
        }

        private bool IsSystemKey()
        {
            return (this.state & STATE_SYSTEMKEY) != 0;
        }

        private bool IsWritable()
        {
            return (this.state & STATE_WRITEACCESS) != 0;
        }

        private bool IsPerfDataKey()
        {
            return (this.state & STATE_PERF_DATA) != 0;
        }

        public String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                EnsureNotDisposed();
                return keyName;
            }
        }

        private void SetDirty()
        {
            this.state |= STATE_DIRTY;
        }

        public void SetValue(String name, Object value)
        {
            SetValue(name, value, RegistryValueKind.Unknown);
        }

        public unsafe void SetValue(String name, Object value, RegistryValueKind valueKind)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if (name != null && name.Length > MaxValueLength)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_RegValStrLenBug"));
            }

            if (!Enum.IsDefined(typeof (RegistryValueKind), valueKind))
                throw new ArgumentException(Environment.GetResourceString("Arg_RegBadKeyKind"), "valueKind");
            EnsureWriteable();
            if (!remoteKey && ContainsRegistryValue(name))
            {
                CheckPermission(RegistryInternalCheck.CheckValueWritePermission, name, false, RegistryKeyPermissionCheck.Default);
            }
            else
            {
                CheckPermission(RegistryInternalCheck.CheckValueCreatePermission, name, false, RegistryKeyPermissionCheck.Default);
            }

            if (valueKind == RegistryValueKind.Unknown)
            {
                valueKind = CalculateValueKind(value);
            }

            int ret = 0;
            try
            {
                switch (valueKind)
                {
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                    {
                        String data = value.ToString();
                        ret = Win32Native.RegSetValueEx(hkey, name, 0, valueKind, data, checked (data.Length * 2 + 2));
                        break;
                    }

                    case RegistryValueKind.MultiString:
                    {
                        string[] dataStrings = (string[])(((string[])value).Clone());
                        int sizeInBytes = 0;
                        for (int i = 0; i < dataStrings.Length; i++)
                        {
                            if (dataStrings[i] == null)
                            {
                                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetStrArrNull);
                            }

                            sizeInBytes = checked (sizeInBytes + (dataStrings[i].Length + 1) * 2);
                        }

                        sizeInBytes = checked (sizeInBytes + 2);
                        byte[] basePtr = new byte[sizeInBytes];
                        fixed (byte *b = basePtr)
                        {
                            IntPtr currentPtr = new IntPtr((void *)b);
                            for (int i = 0; i < dataStrings.Length; i++)
                            {
                                String.InternalCopy(dataStrings[i], currentPtr, (checked (dataStrings[i].Length * 2)));
                                currentPtr = new IntPtr((long)currentPtr + (checked (dataStrings[i].Length * 2)));
                                *(char *)(currentPtr.ToPointer()) = '\0';
                                currentPtr = new IntPtr((long)currentPtr + 2);
                            }

                            *(char *)(currentPtr.ToPointer()) = '\0';
                            currentPtr = new IntPtr((long)currentPtr + 2);
                            ret = Win32Native.RegSetValueEx(hkey, name, 0, RegistryValueKind.MultiString, basePtr, sizeInBytes);
                        }

                        break;
                    }

                    case RegistryValueKind.None:
                    case RegistryValueKind.Binary:
                        byte[] dataBytes = (byte[])value;
                        ret = Win32Native.RegSetValueEx(hkey, name, 0, (valueKind == RegistryValueKind.None ? Win32Native.REG_NONE : RegistryValueKind.Binary), dataBytes, dataBytes.Length);
                        break;
                    case RegistryValueKind.DWord:
                    {
                        int data = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
                        ret = Win32Native.RegSetValueEx(hkey, name, 0, RegistryValueKind.DWord, ref data, 4);
                        break;
                    }

                    case RegistryValueKind.QWord:
                    {
                        long data = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
                        ret = Win32Native.RegSetValueEx(hkey, name, 0, RegistryValueKind.QWord, ref data, 8);
                        break;
                    }
                }
            }
            catch (OverflowException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (InvalidOperationException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (FormatException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }

            if (ret == 0)
            {
                SetDirty();
            }
            else
                Win32Error(ret, null);
        }

        private RegistryValueKind CalculateValueKind(Object value)
        {
            if (value is Int32)
                return RegistryValueKind.DWord;
            else if (value is Array)
            {
                if (value is byte[])
                    return RegistryValueKind.Binary;
                else if (value is String[])
                    return RegistryValueKind.MultiString;
                else
                    throw new ArgumentException(Environment.GetResourceString("Arg_RegSetBadArrType", value.GetType().Name));
            }
            else
                return RegistryValueKind.String;
        }

        public override String ToString()
        {
            EnsureNotDisposed();
            return keyName;
        }

        internal void Win32Error(int errorCode, String str)
        {
            switch (errorCode)
            {
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", str));
                    else
                        throw new UnauthorizedAccessException();
                case Win32Native.ERROR_INVALID_HANDLE:
                    if (!IsPerfDataKey())
                    {
                        this.hkey.SetHandleAsInvalid();
                        this.hkey = null;
                    }

                    goto default;
                case Win32Native.ERROR_FILE_NOT_FOUND:
                    throw new IOException(Environment.GetResourceString("Arg_RegKeyNotFound"), errorCode);
                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
            }
        }

        internal static void Win32ErrorStatic(int errorCode, String str)
        {
            switch (errorCode)
            {
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", str));
                    else
                        throw new UnauthorizedAccessException();
                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
            }
        }

        internal static String FixupName(String name)
        {
            BCLDebug.Assert(name != null, "[FixupName]name!=null");
            if (name.IndexOf('\\') == -1)
                return name;
            StringBuilder sb = new StringBuilder(name);
            FixupPath(sb);
            int temp = sb.Length - 1;
            if (temp >= 0 && sb[temp] == '\\')
                sb.Length = temp;
            return sb.ToString();
        }

        private static void FixupPath(StringBuilder path)
        {
            Contract.Requires(path != null);
            int length = path.Length;
            bool fixup = false;
            char markerChar = (char)0xFFFF;
            int i = 1;
            while (i < length - 1)
            {
                if (path[i] == '\\')
                {
                    i++;
                    while (i < length)
                    {
                        if (path[i] == '\\')
                        {
                            path[i] = markerChar;
                            i++;
                            fixup = true;
                        }
                        else
                            break;
                    }
                }

                i++;
            }

            if (fixup)
            {
                i = 0;
                int j = 0;
                while (i < length)
                {
                    if (path[i] == markerChar)
                    {
                        i++;
                        continue;
                    }

                    path[j] = path[i];
                    i++;
                    j++;
                }

                path.Length += j - i;
            }
        }

        private void GetSubKeyReadPermission(string subkeyName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Read;
            path = keyName + "\\" + subkeyName + "\\.";
        }

        private void GetSubKeyWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Write;
            path = keyName + "\\" + subkeyName + "\\.";
        }

        private void GetSubKeyCreatePermission(string subkeyName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Create;
            path = keyName + "\\" + subkeyName + "\\.";
        }

        private void GetSubTreeReadPermission(string subkeyName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Read;
            path = keyName + "\\" + subkeyName + "\\";
        }

        private void GetSubTreeWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Write;
            path = keyName + "\\" + subkeyName + "\\";
        }

        private void GetSubTreeReadWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Write | RegistryPermissionAccess.Read;
            path = keyName + "\\" + subkeyName;
        }

        private void GetValueReadPermission(string valueName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Read;
            path = keyName + "\\" + valueName;
        }

        private void GetValueWritePermission(string valueName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Write;
            path = keyName + "\\" + valueName;
        }

        private void GetValueCreatePermission(string valueName, out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Create;
            path = keyName + "\\" + valueName;
        }

        private void GetKeyReadPermission(out RegistryPermissionAccess access, out string path)
        {
            access = RegistryPermissionAccess.Read;
            path = keyName + "\\.";
        }

        private void CheckPermission(RegistryInternalCheck check, string item, bool subKeyWritable, RegistryKeyPermissionCheck subKeyCheck)
        {
            bool demand = false;
            RegistryPermissionAccess access = RegistryPermissionAccess.NoAccess;
            string path = null;
            switch (check)
            {
                case RegistryInternalCheck.CheckSubKeyReadPermission:
                    if (remoteKey)
                    {
                        CheckUnmanagedCodePermission();
                    }
                    else
                    {
                        BCLDebug.Assert(checkMode == RegistryKeyPermissionCheck.Default, "Should be called from a key opened under default mode only!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        demand = true;
                        GetSubKeyReadPermission(item, out access, out path);
                    }

                    break;
                case RegistryInternalCheck.CheckSubKeyWritePermission:
                    if (remoteKey)
                    {
                        CheckUnmanagedCodePermission();
                    }
                    else
                    {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        if (checkMode == RegistryKeyPermissionCheck.Default)
                        {
                            demand = true;
                            GetSubKeyWritePermission(item, out access, out path);
                        }
                    }

                    break;
                case RegistryInternalCheck.CheckSubKeyCreatePermission:
                    if (remoteKey)
                    {
                        CheckUnmanagedCodePermission();
                    }
                    else
                    {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        if (checkMode == RegistryKeyPermissionCheck.Default)
                        {
                            demand = true;
                            GetSubKeyCreatePermission(item, out access, out path);
                        }
                    }

                    break;
                case RegistryInternalCheck.CheckSubTreeReadPermission:
                    if (remoteKey)
                    {
                        CheckUnmanagedCodePermission();
                    }
                    else
                    {
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        if (checkMode == RegistryKeyPermissionCheck.Default)
                        {
                            demand = true;
                            GetSubTreeReadPermission(item, out access, out path);
                        }
                    }

                    break;
                case RegistryInternalCheck.CheckSubTreeWritePermission:
                    if (remoteKey)
                    {
                        CheckUnmanagedCodePermission();
                    }
                    else
                    {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        if (checkMode == RegistryKeyPermissionCheck.Default)
                        {
                            demand = true;
                            GetSubTreeWritePermission(item, out access, out path);
                        }
                    }

                    break;
                case RegistryInternalCheck.CheckSubTreeReadWritePermission:
                    if (remoteKey)
                    {
                        CheckUnmanagedCodePermission();
                    }
                    else
                    {
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        demand = true;
                        GetSubTreeReadWritePermission(item, out access, out path);
                    }

                    break;
                case RegistryInternalCheck.CheckValueReadPermission:
                    BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                    BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                    if (checkMode == RegistryKeyPermissionCheck.Default)
                    {
                        demand = true;
                        GetValueReadPermission(item, out access, out path);
                    }

                    break;
                case RegistryInternalCheck.CheckValueWritePermission:
                    if (remoteKey)
                    {
                        CheckUnmanagedCodePermission();
                    }
                    else
                    {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        if (checkMode == RegistryKeyPermissionCheck.Default)
                        {
                            demand = true;
                            GetValueWritePermission(item, out access, out path);
                        }
                    }

                    break;
                case RegistryInternalCheck.CheckValueCreatePermission:
                    if (remoteKey)
                    {
                        CheckUnmanagedCodePermission();
                    }
                    else
                    {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating value under read-only key!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        if (checkMode == RegistryKeyPermissionCheck.Default)
                        {
                            demand = true;
                            GetValueCreatePermission(item, out access, out path);
                        }
                    }

                    break;
                case RegistryInternalCheck.CheckKeyReadPermission:
                    if (checkMode == RegistryKeyPermissionCheck.Default)
                    {
                        BCLDebug.Assert(item == null, "CheckKeyReadPermission should never have a non-null item parameter!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        demand = true;
                        GetKeyReadPermission(out access, out path);
                    }

                    break;
                case RegistryInternalCheck.CheckSubTreePermission:
                    BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                    if (subKeyCheck == RegistryKeyPermissionCheck.ReadSubTree)
                    {
                        if (checkMode == RegistryKeyPermissionCheck.Default)
                        {
                            if (remoteKey)
                            {
                                CheckUnmanagedCodePermission();
                            }
                            else
                            {
                                demand = true;
                                GetSubTreeReadPermission(item, out access, out path);
                            }
                        }
                    }
                    else if (subKeyCheck == RegistryKeyPermissionCheck.ReadWriteSubTree)
                    {
                        if (checkMode != RegistryKeyPermissionCheck.ReadWriteSubTree)
                        {
                            if (remoteKey)
                            {
                                CheckUnmanagedCodePermission();
                            }
                            else
                            {
                                demand = true;
                                GetSubTreeReadWritePermission(item, out access, out path);
                            }
                        }
                    }

                    break;
                case RegistryInternalCheck.CheckOpenSubKeyWithWritablePermission:
                    BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                    if (checkMode == RegistryKeyPermissionCheck.Default)
                    {
                        if (remoteKey)
                        {
                            CheckUnmanagedCodePermission();
                        }
                        else
                        {
                            demand = true;
                            GetSubKeyReadPermission(item, out access, out path);
                        }

                        break;
                    }

                    if (subKeyWritable && (checkMode == RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        if (remoteKey)
                        {
                            CheckUnmanagedCodePermission();
                        }
                        else
                        {
                            demand = true;
                            GetSubTreeReadWritePermission(item, out access, out path);
                        }

                        break;
                    }

                    break;
                case RegistryInternalCheck.CheckOpenSubKeyPermission:
                    BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                    if (subKeyCheck == RegistryKeyPermissionCheck.Default)
                    {
                        if (checkMode == RegistryKeyPermissionCheck.Default)
                        {
                            if (remoteKey)
                            {
                                CheckUnmanagedCodePermission();
                            }
                            else
                            {
                                demand = true;
                                GetSubKeyReadPermission(item, out access, out path);
                            }
                        }
                    }

                    break;
                default:
                    BCLDebug.Assert(false, "CheckPermission default switch case should never be hit!");
                    break;
            }

            if (demand)
            {
                new RegistryPermission(access, path).Demand();
            }
        }

        static private void CheckUnmanagedCodePermission()
        {
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
        }

        private bool ContainsRegistryValue(string name)
        {
            int type = 0;
            int datasize = 0;
            int retval = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
            return retval == 0;
        }

        private void EnsureNotDisposed()
        {
            if (hkey == null)
            {
                ThrowHelper.ThrowObjectDisposedException(keyName, ExceptionResource.ObjectDisposed_RegKeyClosed);
            }
        }

        private void EnsureWriteable()
        {
            EnsureNotDisposed();
            if (!IsWritable())
            {
                ThrowHelper.ThrowUnauthorizedAccessException(ExceptionResource.UnauthorizedAccess_RegistryNoWrite);
            }
        }

        static int GetRegistryKeyAccess(bool isWritable)
        {
            int winAccess;
            if (!isWritable)
            {
                winAccess = Win32Native.KEY_READ;
            }
            else
            {
                winAccess = Win32Native.KEY_READ | Win32Native.KEY_WRITE;
            }

            return winAccess;
        }

        static int GetRegistryKeyAccess(RegistryKeyPermissionCheck mode)
        {
            int winAccess = 0;
            switch (mode)
            {
                case RegistryKeyPermissionCheck.ReadSubTree:
                case RegistryKeyPermissionCheck.Default:
                    winAccess = Win32Native.KEY_READ;
                    break;
                case RegistryKeyPermissionCheck.ReadWriteSubTree:
                    winAccess = Win32Native.KEY_READ | Win32Native.KEY_WRITE;
                    break;
                default:
                    BCLDebug.Assert(false, "unexpected code path");
                    break;
            }

            return winAccess;
        }

        private RegistryKeyPermissionCheck GetSubKeyPermissonCheck(bool subkeyWritable)
        {
            if (checkMode == RegistryKeyPermissionCheck.Default)
            {
                return checkMode;
            }

            if (subkeyWritable)
            {
                return RegistryKeyPermissionCheck.ReadWriteSubTree;
            }
            else
            {
                return RegistryKeyPermissionCheck.ReadSubTree;
            }
        }

        static private void ValidateKeyName(string name)
        {
            Contract.Ensures(name != null);
            if (name == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
            }

            int nextSlash = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
            int current = 0;
            while (nextSlash != -1)
            {
                if ((nextSlash - current) > MaxKeyLength)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);
                current = nextSlash + 1;
                nextSlash = name.IndexOf("\\", current, StringComparison.OrdinalIgnoreCase);
            }

            if ((name.Length - current) > MaxKeyLength)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);
        }

        static private void ValidateKeyMode(RegistryKeyPermissionCheck mode)
        {
            if (mode < RegistryKeyPermissionCheck.Default || mode > RegistryKeyPermissionCheck.ReadWriteSubTree)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryKeyPermissionCheck, ExceptionArgument.mode);
            }
        }

        static private void ValidateKeyOptions(RegistryOptions options)
        {
            if (options < RegistryOptions.None || options > RegistryOptions.Volatile)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryOptionsCheck, ExceptionArgument.options);
            }
        }

        static private void ValidateKeyView(RegistryView view)
        {
            if (view != RegistryView.Default && view != RegistryView.Registry32 && view != RegistryView.Registry64)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryViewCheck, ExceptionArgument.view);
            }
        }

        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    }

    [Flags]
    public enum RegistryValueOptions
    {
        None = 0,
        DoNotExpandEnvironmentNames = 1
    }

    public enum RegistryKeyPermissionCheck
    {
        Default = 0,
        ReadSubTree = 1,
        ReadWriteSubTree = 2
    }
}