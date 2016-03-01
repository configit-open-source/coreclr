namespace System.Runtime.InteropServices
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using System.Runtime.Remoting;
    using System.Runtime.CompilerServices;
    using System.Globalization;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using Win32Native = Microsoft.Win32.Win32Native;
    using Microsoft.Win32.SafeHandles;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices.ComTypes;

    public enum CustomQueryInterfaceMode
    {
        Ignore = 0,
        Allow = 1
    }

    public static partial class Marshal
    {
        private const int LMEM_FIXED = 0;
        private const int LMEM_MOVEABLE = 2;
        private const long HIWORDMASK = unchecked ((long)0xffffffffffff0000L);
        private static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
        private static bool IsWin32Atom(IntPtr ptr)
        {
            long lPtr = (long)ptr;
            return 0 == (lPtr & HIWORDMASK);
        }

        private static bool IsNotWin32Atom(IntPtr ptr)
        {
            long lPtr = (long)ptr;
            return 0 != (lPtr & HIWORDMASK);
        }

        public static readonly int SystemDefaultCharSize = 2;
        public static readonly int SystemMaxDBCSCharSize = GetSystemMaxDBCSCharSize();
        private const String s_strConvertedTypeInfoAssemblyName = "InteropDynamicTypes";
        private const String s_strConvertedTypeInfoAssemblyTitle = "Interop Dynamic Types";
        private const String s_strConvertedTypeInfoAssemblyDesc = "Type dynamically generated from ITypeInfo's";
        private const String s_strConvertedTypeInfoNameSpace = "InteropDynamicTypes";
        private static extern int GetSystemMaxDBCSCharSize();
        unsafe public static String PtrToStringAnsi(IntPtr ptr)
        {
            if (IntPtr.Zero == ptr)
            {
                return null;
            }
            else if (IsWin32Atom(ptr))
            {
                return null;
            }
            else
            {
                int nb = Win32Native.lstrlenA(ptr);
                if (nb == 0)
                {
                    return string.Empty;
                }
                else
                {
                    return new String((sbyte *)ptr);
                }
            }
        }

        unsafe public static String PtrToStringAnsi(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException("ptr");
            if (len < 0)
                throw new ArgumentException("len");
            return new String((sbyte *)ptr, 0, len);
        }

        unsafe public static String PtrToStringUni(IntPtr ptr, int len)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException("ptr");
            if (len < 0)
                throw new ArgumentException("len");
            return new String((char *)ptr, 0, len);
        }

        public static String PtrToStringAuto(IntPtr ptr, int len)
        {
            return PtrToStringUni(ptr, len);
        }

        unsafe public static String PtrToStringUni(IntPtr ptr)
        {
            if (IntPtr.Zero == ptr)
            {
                return null;
            }
            else if (IsWin32Atom(ptr))
            {
                return null;
            }
            else
            {
                return new String((char *)ptr);
            }
        }

        public static String PtrToStringAuto(IntPtr ptr)
        {
            return PtrToStringUni(ptr);
        }

        public static int SizeOf(Object structure)
        {
            if (structure == null)
                throw new ArgumentNullException("structure");
            Contract.EndContractBlock();
            return SizeOfHelper(structure.GetType(), true);
        }

        public static int SizeOf<T>(T structure)
        {
            return SizeOf((object)structure);
        }

        public static int SizeOf(Type t)
        {
            if (t == null)
                throw new ArgumentNullException("t");
            if (!(t is RuntimeType))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "t");
            if (t.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "t");
            Contract.EndContractBlock();
            return SizeOfHelper(t, true);
        }

        public static int SizeOf<T>()
        {
            return SizeOf(typeof (T));
        }

        internal static uint AlignedSizeOf<T>()where T : struct
        {
            uint size = SizeOfType(typeof (T));
            if (size == 1 || size == 2)
            {
                return size;
            }

            if (IntPtr.Size == 8 && size == 4)
            {
                return size;
            }

            return AlignedSizeOfType(typeof (T));
        }

        internal static extern uint SizeOfType(Type type);
        private static extern uint AlignedSizeOfType(Type type);
        internal static extern int SizeOfHelper(Type t, bool throwIfNotMarshalable);
        public static IntPtr OffsetOf(Type t, String fieldName)
        {
            if (t == null)
                throw new ArgumentNullException("t");
            Contract.EndContractBlock();
            FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetOfFieldNotFound", t.FullName), "fieldName");
            RtFieldInfo rtField = f as RtFieldInfo;
            if (rtField == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeFieldInfo"), "fieldName");
            return OffsetOfHelper(rtField);
        }

        public static IntPtr OffsetOf<T>(string fieldName)
        {
            return OffsetOf(typeof (T), fieldName);
        }

        private static extern IntPtr OffsetOfHelper(IRuntimeFieldInfo f);
        public static extern IntPtr UnsafeAddrOfPinnedArrayElement(Array arr, int index);
        public static IntPtr UnsafeAddrOfPinnedArrayElement<T>(T[] arr, int index)
        {
            return UnsafeAddrOfPinnedArrayElement((Array)arr, index);
        }

        public static void Copy(int[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(char[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(short[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(long[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(float[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(double[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(byte[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        public static void Copy(IntPtr[] source, int startIndex, IntPtr destination, int length)
        {
            CopyToNative(source, startIndex, destination, length);
        }

        private static extern void CopyToNative(Object source, int startIndex, IntPtr destination, int length);
        public static void Copy(IntPtr source, int[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, char[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, short[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, long[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, float[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, double[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(IntPtr source, IntPtr[] destination, int startIndex, int length)
        {
            CopyToManaged(source, destination, startIndex, length);
        }

        private static extern void CopyToManaged(IntPtr source, Object destination, int startIndex, int length);
        public static extern byte ReadByte([MarshalAs(UnmanagedType.AsAny), In] Object ptr, int ofs);
        public static unsafe byte ReadByte(IntPtr ptr, int ofs)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                return *addr;
            }
            catch (NullReferenceException)
            {
                throw new AccessViolationException();
            }
        }

        public static byte ReadByte(IntPtr ptr)
        {
            return ReadByte(ptr, 0);
        }

        public static extern short ReadInt16([MarshalAs(UnmanagedType.AsAny), In] Object ptr, int ofs);
        public static unsafe short ReadInt16(IntPtr ptr, int ofs)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked ((int)addr) & 0x1) == 0)
                {
                    return *((short *)addr);
                }
                else
                {
                    short val;
                    byte *valPtr = (byte *)&val;
                    valPtr[0] = addr[0];
                    valPtr[1] = addr[1];
                    return val;
                }
            }
            catch (NullReferenceException)
            {
                throw new AccessViolationException();
            }
        }

        public static short ReadInt16(IntPtr ptr)
        {
            return ReadInt16(ptr, 0);
        }

        public static extern int ReadInt32([MarshalAs(UnmanagedType.AsAny), In] Object ptr, int ofs);
        public static unsafe int ReadInt32(IntPtr ptr, int ofs)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked ((int)addr) & 0x3) == 0)
                {
                    return *((int *)addr);
                }
                else
                {
                    int val;
                    byte *valPtr = (byte *)&val;
                    valPtr[0] = addr[0];
                    valPtr[1] = addr[1];
                    valPtr[2] = addr[2];
                    valPtr[3] = addr[3];
                    return val;
                }
            }
            catch (NullReferenceException)
            {
                throw new AccessViolationException();
            }
        }

        public static int ReadInt32(IntPtr ptr)
        {
            return ReadInt32(ptr, 0);
        }

        public static IntPtr ReadIntPtr([MarshalAs(UnmanagedType.AsAny), In] Object ptr, int ofs)
        {
            return (IntPtr)ReadInt64(ptr, ofs);
        }

        public static IntPtr ReadIntPtr(IntPtr ptr, int ofs)
        {
            return (IntPtr)ReadInt64(ptr, ofs);
        }

        public static IntPtr ReadIntPtr(IntPtr ptr)
        {
            return (IntPtr)ReadInt64(ptr, 0);
        }

        public static extern long ReadInt64([MarshalAs(UnmanagedType.AsAny), In] Object ptr, int ofs);
        public static unsafe long ReadInt64(IntPtr ptr, int ofs)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked ((int)addr) & 0x7) == 0)
                {
                    return *((long *)addr);
                }
                else
                {
                    long val;
                    byte *valPtr = (byte *)&val;
                    valPtr[0] = addr[0];
                    valPtr[1] = addr[1];
                    valPtr[2] = addr[2];
                    valPtr[3] = addr[3];
                    valPtr[4] = addr[4];
                    valPtr[5] = addr[5];
                    valPtr[6] = addr[6];
                    valPtr[7] = addr[7];
                    return val;
                }
            }
            catch (NullReferenceException)
            {
                throw new AccessViolationException();
            }
        }

        public static long ReadInt64(IntPtr ptr)
        {
            return ReadInt64(ptr, 0);
        }

        public static unsafe void WriteByte(IntPtr ptr, int ofs, byte val)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                *addr = val;
            }
            catch (NullReferenceException)
            {
                throw new AccessViolationException();
            }
        }

        public static extern void WriteByte([MarshalAs(UnmanagedType.AsAny), In, Out] Object ptr, int ofs, byte val);
        public static void WriteByte(IntPtr ptr, byte val)
        {
            WriteByte(ptr, 0, val);
        }

        public static unsafe void WriteInt16(IntPtr ptr, int ofs, short val)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked ((int)addr) & 0x1) == 0)
                {
                    *((short *)addr) = val;
                }
                else
                {
                    byte *valPtr = (byte *)&val;
                    addr[0] = valPtr[0];
                    addr[1] = valPtr[1];
                }
            }
            catch (NullReferenceException)
            {
                throw new AccessViolationException();
            }
        }

        public static extern void WriteInt16([MarshalAs(UnmanagedType.AsAny), In, Out] Object ptr, int ofs, short val);
        public static void WriteInt16(IntPtr ptr, short val)
        {
            WriteInt16(ptr, 0, val);
        }

        public static void WriteInt16(IntPtr ptr, int ofs, char val)
        {
            WriteInt16(ptr, ofs, (short)val);
        }

        public static void WriteInt16([In, Out] Object ptr, int ofs, char val)
        {
            WriteInt16(ptr, ofs, (short)val);
        }

        public static void WriteInt16(IntPtr ptr, char val)
        {
            WriteInt16(ptr, 0, (short)val);
        }

        public static unsafe void WriteInt32(IntPtr ptr, int ofs, int val)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked ((int)addr) & 0x3) == 0)
                {
                    *((int *)addr) = val;
                }
                else
                {
                    byte *valPtr = (byte *)&val;
                    addr[0] = valPtr[0];
                    addr[1] = valPtr[1];
                    addr[2] = valPtr[2];
                    addr[3] = valPtr[3];
                }
            }
            catch (NullReferenceException)
            {
                throw new AccessViolationException();
            }
        }

        public static extern void WriteInt32([MarshalAs(UnmanagedType.AsAny), In, Out] Object ptr, int ofs, int val);
        public static void WriteInt32(IntPtr ptr, int val)
        {
            WriteInt32(ptr, 0, val);
        }

        public static void WriteIntPtr(IntPtr ptr, int ofs, IntPtr val)
        {
            WriteInt64(ptr, ofs, (long)val);
        }

        public static void WriteIntPtr([MarshalAs(UnmanagedType.AsAny), In, Out] Object ptr, int ofs, IntPtr val)
        {
            WriteInt64(ptr, ofs, (long)val);
        }

        public static void WriteIntPtr(IntPtr ptr, IntPtr val)
        {
            WriteInt64(ptr, 0, (long)val);
        }

        public static unsafe void WriteInt64(IntPtr ptr, int ofs, long val)
        {
            try
            {
                byte *addr = (byte *)ptr + ofs;
                if ((unchecked ((int)addr) & 0x7) == 0)
                {
                    *((long *)addr) = val;
                }
                else
                {
                    byte *valPtr = (byte *)&val;
                    addr[0] = valPtr[0];
                    addr[1] = valPtr[1];
                    addr[2] = valPtr[2];
                    addr[3] = valPtr[3];
                    addr[4] = valPtr[4];
                    addr[5] = valPtr[5];
                    addr[6] = valPtr[6];
                    addr[7] = valPtr[7];
                }
            }
            catch (NullReferenceException)
            {
                throw new AccessViolationException();
            }
        }

        public static extern void WriteInt64([MarshalAs(UnmanagedType.AsAny), In, Out] Object ptr, int ofs, long val);
        public static void WriteInt64(IntPtr ptr, long val)
        {
            WriteInt64(ptr, 0, val);
        }

        public static extern int GetLastWin32Error();
        internal static extern void SetLastWin32Error(int error);
        public static int GetHRForLastWin32Error()
        {
            int dwLastError = GetLastWin32Error();
            if ((dwLastError & 0x80000000) == 0x80000000)
                return dwLastError;
            else
                return (dwLastError & 0x0000FFFF) | unchecked ((int)0x80070000);
        }

        public static void Prelink(MethodInfo m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            Contract.EndContractBlock();
            RuntimeMethodInfo rmi = m as RuntimeMethodInfo;
            if (rmi == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"));
            InternalPrelink(rmi);
        }

        private static extern void InternalPrelink(IRuntimeMethodInfo m);
        public static void PrelinkAll(Type c)
        {
            if (c == null)
                throw new ArgumentNullException("c");
            Contract.EndContractBlock();
            MethodInfo[] mi = c.GetMethods();
            if (mi != null)
            {
                for (int i = 0; i < mi.Length; i++)
                {
                    Prelink(mi[i]);
                }
            }
        }

        public static int NumParamBytes(MethodInfo m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            Contract.EndContractBlock();
            RuntimeMethodInfo rmi = m as RuntimeMethodInfo;
            if (rmi == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"));
            return InternalNumParamBytes(rmi);
        }

        private static extern int InternalNumParamBytes(IRuntimeMethodInfo m);
        public static extern IntPtr GetExceptionPointers();
        public static extern int GetExceptionCode();
        public static extern void StructureToPtr(Object structure, IntPtr ptr, bool fDeleteOld);
        public static void StructureToPtr<T>(T structure, IntPtr ptr, bool fDeleteOld)
        {
            StructureToPtr((object)structure, ptr, fDeleteOld);
        }

        public static void PtrToStructure(IntPtr ptr, Object structure)
        {
            PtrToStructureHelper(ptr, structure, false);
        }

        public static void PtrToStructure<T>(IntPtr ptr, T structure)
        {
            PtrToStructure(ptr, (object)structure);
        }

        public static Object PtrToStructure(IntPtr ptr, Type structureType)
        {
            if (ptr == IntPtr.Zero)
                return null;
            if (structureType == null)
                throw new ArgumentNullException("structureType");
            if (structureType.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "structureType");
            RuntimeType rt = structureType.UnderlyingSystemType as RuntimeType;
            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "type");
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            Object structure = rt.CreateInstanceDefaultCtor(false, false, false, ref stackMark);
            PtrToStructureHelper(ptr, structure, true);
            return structure;
        }

        public static T PtrToStructure<T>(IntPtr ptr)
        {
            return (T)PtrToStructure(ptr, typeof (T));
        }

        private static extern void PtrToStructureHelper(IntPtr ptr, Object structure, bool allowValueClasses);
        public static extern void DestroyStructure(IntPtr ptr, Type structuretype);
        public static void DestroyStructure<T>(IntPtr ptr)
        {
            DestroyStructure(ptr, typeof (T));
        }

        public static IntPtr GetHINSTANCE(Module m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            Contract.EndContractBlock();
            RuntimeModule rtModule = m as RuntimeModule;
            if (rtModule == null)
            {
                ModuleBuilder mb = m as ModuleBuilder;
                if (mb != null)
                    rtModule = mb.InternalModule;
            }

            if (rtModule == null)
                throw new ArgumentNullException(Environment.GetResourceString("Argument_MustBeRuntimeModule"));
            return GetHINSTANCE(rtModule.GetNativeHandle());
        }

        private extern static IntPtr GetHINSTANCE(RuntimeModule m);
        public static void ThrowExceptionForHR(int errorCode)
        {
            if (errorCode < 0)
                ThrowExceptionForHRInternal(errorCode, IntPtr.Zero);
        }

        public static void ThrowExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode < 0)
                ThrowExceptionForHRInternal(errorCode, errorInfo);
        }

        internal static extern void ThrowExceptionForHRInternal(int errorCode, IntPtr errorInfo);
        public static Exception GetExceptionForHR(int errorCode)
        {
            if (errorCode < 0)
                return GetExceptionForHRInternal(errorCode, IntPtr.Zero);
            else
                return null;
        }

        public static Exception GetExceptionForHR(int errorCode, IntPtr errorInfo)
        {
            if (errorCode < 0)
                return GetExceptionForHRInternal(errorCode, errorInfo);
            else
                return null;
        }

        internal static extern Exception GetExceptionForHRInternal(int errorCode, IntPtr errorInfo);
        public static extern int GetHRForException(Exception e);
        internal static extern int GetHRForException_WinRT(Exception e);
        public static extern IntPtr GetUnmanagedThunkForManagedMethodPtr(IntPtr pfnMethodToWrap, IntPtr pbSignature, int cbSignature);
        public static extern IntPtr GetManagedThunkForUnmanagedMethodPtr(IntPtr pfnMethodToWrap, IntPtr pbSignature, int cbSignature);
        public static Thread GetThreadFromFiberCookie(int cookie)
        {
            if (cookie == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_ArgumentZero"), "cookie");
            Contract.EndContractBlock();
            return InternalGetThreadFromFiberCookie(cookie);
        }

        private static extern Thread InternalGetThreadFromFiberCookie(int cookie);
        public static IntPtr AllocHGlobal(IntPtr cb)
        {
            UIntPtr numBytes;
            numBytes = new UIntPtr(unchecked ((ulong)cb.ToInt64()));
            IntPtr pNewMem = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, unchecked (numBytes));
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        public static IntPtr AllocHGlobal(int cb)
        {
            return AllocHGlobal((IntPtr)cb);
        }

        public static void FreeHGlobal(IntPtr hglobal)
        {
            if (IsNotWin32Atom(hglobal))
            {
                if (IntPtr.Zero != Win32Native.LocalFree(hglobal))
                {
                    ThrowExceptionForHR(GetHRForLastWin32Error());
                }
            }
        }

        public static IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb)
        {
            IntPtr pNewMem = Win32Native.LocalReAlloc(pv, cb, LMEM_MOVEABLE);
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        unsafe public static IntPtr StringToHGlobalAnsi(String s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * SystemMaxDBCSCharSize;
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s");
                UIntPtr len = new UIntPtr((uint)nb);
                IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len);
                if (hglobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    s.ConvertToAnsi((byte *)hglobal, nb, false, false);
                    return hglobal;
                }
            }
        }

        unsafe public static IntPtr StringToHGlobalUni(String s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * 2;
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s");
                UIntPtr len = new UIntPtr((uint)nb);
                IntPtr hglobal = Win32Native.LocalAlloc_NoSafeHandle(LMEM_FIXED, len);
                if (hglobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    fixed (char *firstChar = s)
                    {
                        String.wstrcpy((char *)hglobal, firstChar, s.Length + 1);
                    }

                    return hglobal;
                }
            }
        }

        public static IntPtr StringToHGlobalAuto(String s)
        {
            return StringToHGlobalUni(s);
        }

        internal static readonly Guid ManagedNameGuid = new Guid("{0F21F359-AB84-41E8-9A78-36D110E6D2F9}");
        public static String GetTypeLibName(UCOMITypeLib pTLB)
        {
            return GetTypeLibName((ITypeLib)pTLB);
        }

        public static String GetTypeLibName(ITypeLib typelib)
        {
            if (typelib == null)
                throw new ArgumentNullException("typelib");
            Contract.EndContractBlock();
            String strTypeLibName = null;
            String strDocString = null;
            int dwHelpContext = 0;
            String strHelpFile = null;
            typelib.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile);
            return strTypeLibName;
        }

        internal static String GetTypeLibNameInternal(ITypeLib typelib)
        {
            if (typelib == null)
                throw new ArgumentNullException("typelib");
            Contract.EndContractBlock();
            ITypeLib2 typeLib2 = typelib as ITypeLib2;
            if (typeLib2 != null)
            {
                Guid guid = ManagedNameGuid;
                object val;
                try
                {
                    typeLib2.GetCustData(ref guid, out val);
                }
                catch (Exception)
                {
                    val = null;
                }

                if (val != null && val.GetType() == typeof (string))
                {
                    string customManagedNamespace = (string)val;
                    customManagedNamespace = customManagedNamespace.Trim();
                    if (customManagedNamespace.EndsWith(".DLL", StringComparison.OrdinalIgnoreCase))
                        customManagedNamespace = customManagedNamespace.Substring(0, customManagedNamespace.Length - 4);
                    else if (customManagedNamespace.EndsWith(".EXE", StringComparison.OrdinalIgnoreCase))
                        customManagedNamespace = customManagedNamespace.Substring(0, customManagedNamespace.Length - 4);
                    return customManagedNamespace;
                }
            }

            return GetTypeLibName(typelib);
        }

        public static Guid GetTypeLibGuid(UCOMITypeLib pTLB)
        {
            return GetTypeLibGuid((ITypeLib)pTLB);
        }

        public static Guid GetTypeLibGuid(ITypeLib typelib)
        {
            Guid result = new Guid();
            FCallGetTypeLibGuid(ref result, typelib);
            return result;
        }

        private static extern void FCallGetTypeLibGuid(ref Guid result, ITypeLib pTLB);
        public static int GetTypeLibLcid(UCOMITypeLib pTLB)
        {
            return GetTypeLibLcid((ITypeLib)pTLB);
        }

        public static extern int GetTypeLibLcid(ITypeLib typelib);
        internal static extern void GetTypeLibVersion(ITypeLib typeLibrary, out int major, out int minor);
        internal static Guid GetTypeInfoGuid(ITypeInfo typeInfo)
        {
            Guid result = new Guid();
            FCallGetTypeInfoGuid(ref result, typeInfo);
            return result;
        }

        private static extern void FCallGetTypeInfoGuid(ref Guid result, ITypeInfo typeInfo);
        public static Guid GetTypeLibGuidForAssembly(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");
            Contract.EndContractBlock();
            RuntimeAssembly rtAssembly = asm as RuntimeAssembly;
            if (rtAssembly == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "asm");
            Guid result = new Guid();
            FCallGetTypeLibGuidForAssembly(ref result, rtAssembly);
            return result;
        }

        private static extern void FCallGetTypeLibGuidForAssembly(ref Guid result, RuntimeAssembly asm);
        private static extern void _GetTypeLibVersionForAssembly(RuntimeAssembly inputAssembly, out int majorVersion, out int minorVersion);
        public static void GetTypeLibVersionForAssembly(Assembly inputAssembly, out int majorVersion, out int minorVersion)
        {
            if (inputAssembly == null)
                throw new ArgumentNullException("inputAssembly");
            Contract.EndContractBlock();
            RuntimeAssembly rtAssembly = inputAssembly as RuntimeAssembly;
            if (rtAssembly == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "inputAssembly");
            _GetTypeLibVersionForAssembly(rtAssembly, out majorVersion, out minorVersion);
        }

        public static String GetTypeInfoName(UCOMITypeInfo pTI)
        {
            return GetTypeInfoName((ITypeInfo)pTI);
        }

        public static String GetTypeInfoName(ITypeInfo typeInfo)
        {
            if (typeInfo == null)
                throw new ArgumentNullException("typeInfo");
            Contract.EndContractBlock();
            String strTypeLibName = null;
            String strDocString = null;
            int dwHelpContext = 0;
            String strHelpFile = null;
            typeInfo.GetDocumentation(-1, out strTypeLibName, out strDocString, out dwHelpContext, out strHelpFile);
            return strTypeLibName;
        }

        internal static String GetTypeInfoNameInternal(ITypeInfo typeInfo, out bool hasManagedName)
        {
            if (typeInfo == null)
                throw new ArgumentNullException("typeInfo");
            Contract.EndContractBlock();
            ITypeInfo2 typeInfo2 = typeInfo as ITypeInfo2;
            if (typeInfo2 != null)
            {
                Guid guid = ManagedNameGuid;
                object val;
                try
                {
                    typeInfo2.GetCustData(ref guid, out val);
                }
                catch (Exception)
                {
                    val = null;
                }

                if (val != null && val.GetType() == typeof (string))
                {
                    hasManagedName = true;
                    return (string)val;
                }
            }

            hasManagedName = false;
            return GetTypeInfoName(typeInfo);
        }

        internal static String GetManagedTypeInfoNameInternal(ITypeLib typeLib, ITypeInfo typeInfo)
        {
            bool hasManagedName;
            string name = GetTypeInfoNameInternal(typeInfo, out hasManagedName);
            if (hasManagedName)
                return name;
            else
                return GetTypeLibNameInternal(typeLib) + "." + name;
        }

        private static extern Type GetLoadedTypeForGUID(ref Guid guid);
        public static Type GetTypeFromCLSID(Guid clsid)
        {
            return RuntimeType.GetTypeFromCLSIDImpl(clsid, null, false);
        }

        public static extern IntPtr GetITypeInfoForType(Type t);
        public static IntPtr GetIUnknownForObject(Object o)
        {
            return GetIUnknownForObjectNative(o, false);
        }

        public static IntPtr GetIUnknownForObjectInContext(Object o)
        {
            return GetIUnknownForObjectNative(o, true);
        }

        private static extern IntPtr GetIUnknownForObjectNative(Object o, bool onlyInContext);
        internal static extern IntPtr GetRawIUnknownForComObjectNoAddRef(Object o);
        public static IntPtr GetIDispatchForObject(Object o)
        {
            return GetIDispatchForObjectNative(o, false);
        }

        public static IntPtr GetIDispatchForObjectInContext(Object o)
        {
            return GetIDispatchForObjectNative(o, true);
        }

        private static extern IntPtr GetIDispatchForObjectNative(Object o, bool onlyInContext);
        public static IntPtr GetComInterfaceForObject(Object o, Type T)
        {
            return GetComInterfaceForObjectNative(o, T, false, true);
        }

        public static IntPtr GetComInterfaceForObject<T, TInterface>(T o)
        {
            return GetComInterfaceForObject(o, typeof (TInterface));
        }

        public static IntPtr GetComInterfaceForObject(Object o, Type T, CustomQueryInterfaceMode mode)
        {
            bool bEnableCustomizedQueryInterface = ((mode == CustomQueryInterfaceMode.Allow) ? true : false);
            return GetComInterfaceForObjectNative(o, T, false, bEnableCustomizedQueryInterface);
        }

        public static IntPtr GetComInterfaceForObjectInContext(Object o, Type t)
        {
            return GetComInterfaceForObjectNative(o, t, true, true);
        }

        private static extern IntPtr GetComInterfaceForObjectNative(Object o, Type t, bool onlyInContext, bool fEnalbeCustomizedQueryInterface);
        public static extern Object GetObjectForIUnknown(IntPtr pUnk);
        public static extern Object GetUniqueObjectForIUnknown(IntPtr unknown);
        public static extern Object GetTypedObjectForIUnknown(IntPtr pUnk, Type t);
        public static extern IntPtr CreateAggregatedObject(IntPtr pOuter, Object o);
        public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o)
        {
            return CreateAggregatedObject(pOuter, (object)o);
        }

        public static extern void CleanupUnusedObjectsInCurrentContext();
        public static extern bool AreComObjectsAvailableForCleanup();
        public static extern bool IsComObject(Object o);
        public static IntPtr AllocCoTaskMem(int cb)
        {
            IntPtr pNewMem = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)cb));
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        unsafe public static IntPtr StringToCoTaskMemUni(String s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * 2;
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s");
                IntPtr hglobal = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)nb));
                if (hglobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    fixed (char *firstChar = s)
                    {
                        String.wstrcpy((char *)hglobal, firstChar, s.Length + 1);
                    }

                    return hglobal;
                }
            }
        }

        public static IntPtr StringToCoTaskMemAuto(String s)
        {
            return StringToCoTaskMemUni(s);
        }

        unsafe public static IntPtr StringToCoTaskMemAnsi(String s)
        {
            if (s == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                int nb = (s.Length + 1) * SystemMaxDBCSCharSize;
                if (nb < s.Length)
                    throw new ArgumentOutOfRangeException("s");
                IntPtr hglobal = Win32Native.CoTaskMemAlloc(new UIntPtr((uint)nb));
                if (hglobal == IntPtr.Zero)
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    s.ConvertToAnsi((byte *)hglobal, nb, false, false);
                    return hglobal;
                }
            }
        }

        public static void FreeCoTaskMem(IntPtr ptr)
        {
            if (IsNotWin32Atom(ptr))
            {
                Win32Native.CoTaskMemFree(ptr);
            }
        }

        public static IntPtr ReAllocCoTaskMem(IntPtr pv, int cb)
        {
            IntPtr pNewMem = Win32Native.CoTaskMemRealloc(pv, new UIntPtr((uint)cb));
            if (pNewMem == IntPtr.Zero && cb != 0)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        public static void FreeBSTR(IntPtr ptr)
        {
            if (IsNotWin32Atom(ptr))
            {
                Win32Native.SysFreeString(ptr);
            }
        }

        public static IntPtr StringToBSTR(String s)
        {
            if (s == null)
                return IntPtr.Zero;
            if (s.Length + 1 < s.Length)
                throw new ArgumentOutOfRangeException("s");
            IntPtr bstr = Win32Native.SysAllocStringLen(s, s.Length);
            if (bstr == IntPtr.Zero)
                throw new OutOfMemoryException();
            return bstr;
        }

        public static String PtrToStringBSTR(IntPtr ptr)
        {
            return PtrToStringUni(ptr, (int)Win32Native.SysStringLen(ptr));
        }

        public static int ReleaseComObject(Object o)
        {
            __ComObject co = null;
            try
            {
                co = (__ComObject)o;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o");
            }

            return co.ReleaseSelf();
        }

        internal static extern int InternalReleaseComObject(Object o);
        public static Int32 FinalReleaseComObject(Object o)
        {
            if (o == null)
                throw new ArgumentNullException("o");
            Contract.EndContractBlock();
            __ComObject co = null;
            try
            {
                co = (__ComObject)o;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o");
            }

            co.FinalReleaseSelf();
            return 0;
        }

        internal static extern void InternalFinalReleaseComObject(Object o);
        public static Object GetComObjectData(Object obj, Object key)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            __ComObject comObj = null;
            try
            {
                comObj = (__ComObject)obj;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "obj");
            }

            if (obj.GetType().IsWindowsRuntimeObject)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjIsWinRTObject"), "obj");
            }

            return comObj.GetData(key);
        }

        public static bool SetComObjectData(Object obj, Object key, Object data)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            __ComObject comObj = null;
            try
            {
                comObj = (__ComObject)obj;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "obj");
            }

            if (obj.GetType().IsWindowsRuntimeObject)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjIsWinRTObject"), "obj");
            }

            return comObj.SetData(key, data);
        }

        public static Object CreateWrapperOfType(Object o, Type t)
        {
            if (t == null)
                throw new ArgumentNullException("t");
            if (!t.IsCOMObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeNotComObject"), "t");
            if (t.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "t");
            Contract.EndContractBlock();
            if (t.IsWindowsRuntimeObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeIsWinRTType"), "t");
            if (o == null)
                return null;
            if (!o.GetType().IsCOMObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjNotComObject"), "o");
            if (o.GetType().IsWindowsRuntimeObject)
                throw new ArgumentException(Environment.GetResourceString("Argument_ObjIsWinRTObject"), "o");
            if (o.GetType() == t)
                return o;
            Object Wrapper = GetComObjectData(o, t);
            if (Wrapper == null)
            {
                Wrapper = InternalCreateWrapperOfType(o, t);
                if (!SetComObjectData(o, t, Wrapper))
                {
                    Wrapper = GetComObjectData(o, t);
                }
            }

            return Wrapper;
        }

        public static TWrapper CreateWrapperOfType<T, TWrapper>(T o)
        {
            return (TWrapper)CreateWrapperOfType(o, typeof (TWrapper));
        }

        private static extern Object InternalCreateWrapperOfType(Object o, Type t);
        public static void ReleaseThreadCache()
        {
        }

        public static extern bool IsTypeVisibleFromCom(Type t);
        public static extern int QueryInterface(IntPtr pUnk, ref Guid iid, out IntPtr ppv);
        public static extern int AddRef(IntPtr pUnk);
        public static extern int Release(IntPtr pUnk);
        public static extern void GetNativeVariantForObject(Object obj, IntPtr pDstNativeVariant);
        public static void GetNativeVariantForObject<T>(T obj, IntPtr pDstNativeVariant)
        {
            GetNativeVariantForObject((object)obj, pDstNativeVariant);
        }

        public static extern Object GetObjectForNativeVariant(IntPtr pSrcNativeVariant);
        public static T GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        {
            return (T)GetObjectForNativeVariant(pSrcNativeVariant);
        }

        public static extern Object[] GetObjectsForNativeVariants(IntPtr aSrcNativeVariant, int cVars);
        public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
        {
            object[] objects = GetObjectsForNativeVariants(aSrcNativeVariant, cVars);
            T[] result = null;
            if (objects != null)
            {
                result = new T[objects.Length];
                Array.Copy(objects, result, objects.Length);
            }

            return result;
        }

        public static extern int GetStartComSlot(Type t);
        public static extern int GetEndComSlot(Type t);
        public static extern MemberInfo GetMethodInfoForComSlot(Type t, int slot, ref ComMemberType memberType);
        public static int GetComSlotForMethodInfo(MemberInfo m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            if (!(m is RuntimeMethodInfo))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "m");
            if (!m.DeclaringType.IsInterface)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeInterfaceMethod"), "m");
            if (m.DeclaringType.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "m");
            Contract.EndContractBlock();
            return InternalGetComSlotForMethodInfo((IRuntimeMethodInfo)m);
        }

        private static extern int InternalGetComSlotForMethodInfo(IRuntimeMethodInfo m);
        public static Guid GenerateGuidForType(Type type)
        {
            Guid result = new Guid();
            FCallGenerateGuidForType(ref result, type);
            return result;
        }

        private static extern void FCallGenerateGuidForType(ref Guid result, Type type);
        public static String GenerateProgIdForType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (type.IsImport)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustNotBeComImport"), "type");
            if (type.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type");
            Contract.EndContractBlock();
            if (!RegistrationServices.TypeRequiresRegistrationHelper(type))
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeMustBeComCreatable"), "type");
            IList<CustomAttributeData> cas = CustomAttributeData.GetCustomAttributes(type);
            for (int i = 0; i < cas.Count; i++)
            {
                if (cas[i].Constructor.DeclaringType == typeof (ProgIdAttribute))
                {
                    IList<CustomAttributeTypedArgument> caConstructorArgs = cas[i].ConstructorArguments;
                    Contract.Assert(caConstructorArgs.Count == 1, "caConstructorArgs.Count == 1");
                    CustomAttributeTypedArgument progIdConstructorArg = caConstructorArgs[0];
                    Contract.Assert(progIdConstructorArg.ArgumentType == typeof (String), "progIdConstructorArg.ArgumentType == typeof(String)");
                    String strProgId = (String)progIdConstructorArg.Value;
                    if (strProgId == null)
                        strProgId = String.Empty;
                    return strProgId;
                }
            }

            return type.FullName;
        }

        public static Object BindToMoniker(String monikerName)
        {
            Object obj = null;
            IBindCtx bindctx = null;
            CreateBindCtx(0, out bindctx);
            UInt32 cbEaten;
            IMoniker pmoniker = null;
            MkParseDisplayName(bindctx, monikerName, out cbEaten, out pmoniker);
            BindMoniker(pmoniker, 0, ref IID_IUnknown, out obj);
            return obj;
        }

        public static Object GetActiveObject(String progID)
        {
            Object obj = null;
            Guid clsid;
            try
            {
                CLSIDFromProgIDEx(progID, out clsid);
            }
            catch (Exception)
            {
                CLSIDFromProgID(progID, out clsid);
            }

            GetActiveObject(ref clsid, IntPtr.Zero, out obj);
            return obj;
        }

        private static extern void CLSIDFromProgIDEx([MarshalAs(UnmanagedType.LPWStr)] String progId, out Guid clsid);
        private static extern void CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] String progId, out Guid clsid);
        private static extern void CreateBindCtx(UInt32 reserved, out IBindCtx ppbc);
        private static extern void MkParseDisplayName(IBindCtx pbc, [MarshalAs(UnmanagedType.LPWStr)] String szUserName, out UInt32 pchEaten, out IMoniker ppmk);
        private static extern void BindMoniker(IMoniker pmk, UInt32 grfOpt, ref Guid iidResult, [MarshalAs(UnmanagedType.Interface)] out Object ppvResult);
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.Interface)] out Object ppunk);
        internal static extern bool InternalSwitchCCW(Object oldtp, Object newtp);
        internal static extern Object InternalWrapIUnknownWithComObject(IntPtr i);
        private static IntPtr LoadLicenseManager()
        {
            Assembly sys = Assembly.Load("System, Version=" + ThisAssembly.Version + ", Culture=neutral, PublicKeyToken=" + AssemblyRef.EcmaPublicKeyToken);
            Type t = sys.GetType("System.ComponentModel.LicenseManager");
            if (t == null || !t.IsVisible)
                return IntPtr.Zero;
            return t.TypeHandle.Value;
        }

        public static extern void ChangeWrapperHandleStrength(Object otp, bool fIsWeak);
        internal static extern void InitializeWrapperForWinRT(object o, ref IntPtr pUnk);
        internal static extern void InitializeManagedWinRTFactoryObject(object o, RuntimeType runtimeClassType);
        internal static extern object GetNativeActivationFactory(Type type);
        private static extern void _GetInspectableIids(ObjectHandleOnStack obj, ObjectHandleOnStack guids);
        internal static System.Guid[] GetInspectableIids(object obj)
        {
            System.Guid[] result = null;
            System.__ComObject comObj = obj as System.__ComObject;
            if (comObj != null)
            {
                _GetInspectableIids(JitHelpers.GetObjectHandleOnStack(ref comObj), JitHelpers.GetObjectHandleOnStack(ref result));
            }

            return result;
        }

        private static extern void _GetCachedWinRTTypeByIid(ObjectHandleOnStack appDomainObj, System.Guid iid, out IntPtr rthHandle);
        internal static System.Type GetCachedWinRTTypeByIid(System.AppDomain ad, System.Guid iid)
        {
            IntPtr rthHandle;
            _GetCachedWinRTTypeByIid(JitHelpers.GetObjectHandleOnStack(ref ad), iid, out rthHandle);
            System.Type res = Type.GetTypeFromHandleUnsafe(rthHandle);
            return res;
        }

        private static extern void _GetCachedWinRTTypes(ObjectHandleOnStack appDomainObj, ref int epoch, ObjectHandleOnStack winrtTypes);
        internal static System.Type[] GetCachedWinRTTypes(System.AppDomain ad, ref int epoch)
        {
            System.IntPtr[] res = null;
            _GetCachedWinRTTypes(JitHelpers.GetObjectHandleOnStack(ref ad), ref epoch, JitHelpers.GetObjectHandleOnStack(ref res));
            System.Type[] result = new System.Type[res.Length];
            for (int i = 0; i < res.Length; ++i)
            {
                result[i] = Type.GetTypeFromHandleUnsafe(res[i]);
            }

            return result;
        }

        internal static System.Type[] GetCachedWinRTTypes(System.AppDomain ad)
        {
            int dummyEpoch = 0;
            return GetCachedWinRTTypes(ad, ref dummyEpoch);
        }

        public static Delegate GetDelegateForFunctionPointer(IntPtr ptr, Type t)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException("ptr");
            if (t == null)
                throw new ArgumentNullException("t");
            Contract.EndContractBlock();
            if ((t as RuntimeType) == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "t");
            if (t.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "t");
            Type c = t.BaseType;
            if (c == null || (c != typeof (Delegate) && c != typeof (MulticastDelegate)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "t");
            return GetDelegateForFunctionPointerInternal(ptr, t);
        }

        public static TDelegate GetDelegateForFunctionPointer<TDelegate>(IntPtr ptr)
        {
            return (TDelegate)(object)GetDelegateForFunctionPointer(ptr, typeof (TDelegate));
        }

        internal static extern Delegate GetDelegateForFunctionPointerInternal(IntPtr ptr, Type t);
        public static IntPtr GetFunctionPointerForDelegate(Delegate d)
        {
            if (d == null)
                throw new ArgumentNullException("d");
            Contract.EndContractBlock();
            return GetFunctionPointerForDelegateInternal(d);
        }

        public static IntPtr GetFunctionPointerForDelegate<TDelegate>(TDelegate d)
        {
            return GetFunctionPointerForDelegate((Delegate)(object)d);
        }

        internal static extern IntPtr GetFunctionPointerForDelegateInternal(Delegate d);
        public static IntPtr SecureStringToBSTR(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            Contract.EndContractBlock();
            return s.ToBSTR();
        }

        public static IntPtr SecureStringToCoTaskMemAnsi(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            Contract.EndContractBlock();
            return s.ToAnsiStr(false);
        }

        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            Contract.EndContractBlock();
            return s.ToUniStr(false);
        }

        public static void ZeroFreeBSTR(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.SysStringLen(s) * 2));
            FreeBSTR(s);
        }

        public static void ZeroFreeCoTaskMemAnsi(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.lstrlenA(s)));
            FreeCoTaskMem(s);
        }

        public static void ZeroFreeCoTaskMemUnicode(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.lstrlenW(s) * 2));
            FreeCoTaskMem(s);
        }

        public static IntPtr SecureStringToGlobalAllocAnsi(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            Contract.EndContractBlock();
            return s.ToAnsiStr(true);
        }

        public static IntPtr SecureStringToGlobalAllocUnicode(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            Contract.EndContractBlock();
            return s.ToUniStr(true);
        }

        public static void ZeroFreeGlobalAllocAnsi(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.lstrlenA(s)));
            FreeHGlobal(s);
        }

        public static void ZeroFreeGlobalAllocUnicode(IntPtr s)
        {
            Win32Native.ZeroMemory(s, (UIntPtr)(Win32Native.lstrlenW(s) * 2));
            FreeHGlobal(s);
        }
    }
}