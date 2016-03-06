using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

using Microsoft.Win32;

namespace System.StubHelpers
{
    internal static class AnsiCharMarshaler
    {
        unsafe static internal byte[] DoAnsiConversion(string str, bool fBestFit, bool fThrowOnUnmappableChar, out int cbLength)
        {
            byte[] buffer = new byte[(str.Length + 1) * Marshal.SystemMaxDBCSCharSize];
            fixed (byte *bufferPtr = buffer)
            {
                cbLength = str.ConvertToAnsi(bufferPtr, buffer.Length, fBestFit, fThrowOnUnmappableChar);
            }

            return buffer;
        }

        unsafe static internal byte ConvertToNative(char managedChar, bool fBestFit, bool fThrowOnUnmappableChar)
        {
            int cbAllocLength = (1 + 1) * Marshal.SystemMaxDBCSCharSize;
            byte *bufferPtr = stackalloc byte[cbAllocLength];
            int cbLength = managedChar.ToString().ConvertToAnsi(bufferPtr, cbAllocLength, fBestFit, fThrowOnUnmappableChar);
            BCLDebug.Assert(cbLength > 0, "Zero bytes returned from DoAnsiConversion in AnsiCharMarshaler.ConvertToNative");
            return bufferPtr[0];
        }

        static internal char ConvertToManaged(byte nativeChar)
        {
            byte[] bytes = new byte[1]{nativeChar};
            string str = Encoding.Default.GetString(bytes);
            return str[0];
        }
    }

    internal static class CSTRMarshaler
    {
        static internal unsafe IntPtr ConvertToNative(int flags, string strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            StubHelpers.CheckStringLength(strManaged.Length);
            int nb;
            byte *pbNativeBuffer = (byte *)pNativeBuffer;
            if (pbNativeBuffer != null || Marshal.SystemMaxDBCSCharSize == 1)
            {
                nb = (strManaged.Length + 1) * Marshal.SystemMaxDBCSCharSize;
                if (pbNativeBuffer == null)
                {
                    pbNativeBuffer = (byte *)Marshal.AllocCoTaskMem(nb + 1);
                }

                nb = strManaged.ConvertToAnsi(pbNativeBuffer, nb + 1, 0 != (flags & 0xFF), 0 != (flags >> 8));
            }
            else
            {
                byte[] bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, 0 != (flags & 0xFF), 0 != (flags >> 8), out nb);
                pbNativeBuffer = (byte *)Marshal.AllocCoTaskMem(nb + 2);
                Buffer.Memcpy(pbNativeBuffer, 0, bytes, 0, nb);
            }

            pbNativeBuffer[nb] = 0x00;
            pbNativeBuffer[nb + 1] = 0x00;
            return (IntPtr)pbNativeBuffer;
        }

        static internal unsafe string ConvertToManaged(IntPtr cstr)
        {
            if (IntPtr.Zero == cstr)
                return null;
            else
                return new String((sbyte *)cstr);
        }

        static internal void ClearNative(IntPtr pNative)
        {
            Win32Native.CoTaskMemFree(pNative);
        }
    }

    internal static class BSTRMarshaler
    {
        static internal unsafe IntPtr ConvertToNative(string strManaged, IntPtr pNativeBuffer)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }
            else
            {
                StubHelpers.CheckStringLength(strManaged.Length);
                byte trailByte;
                bool hasTrailByte = strManaged.TryGetTrailByte(out trailByte);
                uint lengthInBytes = (uint)strManaged.Length * 2;
                if (hasTrailByte)
                {
                    lengthInBytes++;
                }

                byte *ptrToFirstChar;
                if (pNativeBuffer != IntPtr.Zero)
                {
                    uint length = *((uint *)pNativeBuffer.ToPointer());
                    BCLDebug.Assert(length >= lengthInBytes + 6, "BSTR localloc'ed buffer is too small");
                    *((uint *)pNativeBuffer.ToPointer()) = lengthInBytes;
                    ptrToFirstChar = (byte *)pNativeBuffer.ToPointer() + 4;
                }
                else
                {
                    ptrToFirstChar = (byte *)Win32Native.SysAllocStringByteLen(null, lengthInBytes).ToPointer();
                    if (ptrToFirstChar == null)
                    {
                        throw new OutOfMemoryException();
                    }
                }

                fixed (char *ch = strManaged)
                {
                    Buffer.Memcpy(ptrToFirstChar, (byte *)ch, (strManaged.Length + 1) * 2);
                }

                if (hasTrailByte)
                {
                    ptrToFirstChar[lengthInBytes - 1] = trailByte;
                }

                return (IntPtr)ptrToFirstChar;
            }
        }

        static internal unsafe string ConvertToManaged(IntPtr bstr)
        {
            if (IntPtr.Zero == bstr)
            {
                return null;
            }
            else
            {
                uint length = Win32Native.SysStringByteLen(bstr);
                StubHelpers.CheckStringLength(length);
                string ret;
                if (length == 1)
                {
                    ret = String.FastAllocateString(0);
                }
                else
                {
                    ret = new String((char *)bstr, 0, (int)(length / 2));
                }

                if ((length & 1) == 1)
                {
                    ret.SetTrailByte(((byte *)bstr.ToPointer())[length - 1]);
                }

                return ret;
            }
        }

        static internal void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Win32Native.SysFreeString(pNative);
            }
        }
    }

    internal static class VBByValStrMarshaler
    {
        static internal unsafe IntPtr ConvertToNative(string strManaged, bool fBestFit, bool fThrowOnUnmappableChar, ref int cch)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            byte *pNative;
            cch = strManaged.Length;
            StubHelpers.CheckStringLength(cch);
            int nbytes = sizeof (uint) + ((cch + 1) * Marshal.SystemMaxDBCSCharSize);
            pNative = (byte *)Marshal.AllocCoTaskMem(nbytes);
            int *pLength = (int *)pNative;
            pNative = pNative + sizeof (uint);
            if (0 == cch)
            {
                *pNative = 0;
                *pLength = 0;
            }
            else
            {
                int nbytesused;
                byte[] bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, fBestFit, fThrowOnUnmappableChar, out nbytesused);
                BCLDebug.Assert(nbytesused < nbytes, "Insufficient buffer allocated in VBByValStrMarshaler.ConvertToNative");
                Buffer.Memcpy(pNative, 0, bytes, 0, nbytesused);
                pNative[nbytesused] = 0;
                *pLength = nbytesused;
            }

            return new IntPtr(pNative);
        }

        static internal unsafe string ConvertToManaged(IntPtr pNative, int cch)
        {
            if (IntPtr.Zero == pNative)
            {
                return null;
            }

            return new String((sbyte *)pNative, 0, cch);
        }

        static internal unsafe void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Win32Native.CoTaskMemFree((IntPtr)(((long)pNative) - sizeof (uint)));
            }
        }
    }

    internal static class AnsiBSTRMarshaler
    {
        static internal unsafe IntPtr ConvertToNative(int flags, string strManaged)
        {
            if (null == strManaged)
            {
                return IntPtr.Zero;
            }

            int length = strManaged.Length;
            StubHelpers.CheckStringLength(length);
            byte[] bytes = null;
            int nb = 0;
            if (length > 0)
            {
                bytes = AnsiCharMarshaler.DoAnsiConversion(strManaged, 0 != (flags & 0xFF), 0 != (flags >> 8), out nb);
            }

            return Win32Native.SysAllocStringByteLen(bytes, (uint)nb);
        }

        static internal unsafe string ConvertToManaged(IntPtr bstr)
        {
            if (IntPtr.Zero == bstr)
            {
                return null;
            }
            else
            {
                return new String((sbyte *)bstr);
            }
        }

        static internal unsafe void ClearNative(IntPtr pNative)
        {
            if (IntPtr.Zero != pNative)
            {
                Win32Native.SysFreeString(pNative);
            }
        }
    }

    internal static class WSTRBufferMarshaler
    {
        static internal IntPtr ConvertToNative(string strManaged)
        {
                        return IntPtr.Zero;
        }

        static internal unsafe string ConvertToManaged(IntPtr bstr)
        {
                        return null;
        }

        static internal void ClearNative(IntPtr pNative)
        {
                    }
    }

    internal struct DateTimeNative
    {
        public Int64 UniversalTime;
    }

    ;
    internal static class DateTimeOffsetMarshaler
    {
        private const Int64 ManagedUtcTicksAtNativeZero = 504911232000000000;
        internal static void ConvertToNative(ref DateTimeOffset managedDTO, out DateTimeNative dateTime)
        {
            Int64 managedUtcTicks = managedDTO.UtcTicks;
            dateTime.UniversalTime = managedUtcTicks - ManagedUtcTicksAtNativeZero;
        }

        internal static void ConvertToManaged(out DateTimeOffset managedLocalDTO, ref DateTimeNative nativeTicks)
        {
            Int64 managedUtcTicks = ManagedUtcTicksAtNativeZero + nativeTicks.UniversalTime;
            DateTimeOffset managedUtcDTO = new DateTimeOffset(managedUtcTicks, TimeSpan.Zero);
            managedLocalDTO = managedUtcDTO.ToLocalTime(true);
        }
    }

    internal static class HStringMarshaler
    {
        internal static unsafe IntPtr ConvertToNative(string managed)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            if (managed == null)
                throw new ArgumentNullException();
            IntPtr hstring;
            int hrCreate = System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateString(managed, managed.Length, &hstring);
            Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
            return hstring;
        }

        internal static unsafe IntPtr ConvertToNativeReference(string managed, [Out] HSTRING_HEADER*hstringHeader)
        {
            if (!Environment.IsWinRTSupported)
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            if (managed == null)
                throw new ArgumentNullException();
            fixed (char *pManaged = managed)
            {
                IntPtr hstring;
                int hrCreate = System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateStringReference(pManaged, managed.Length, hstringHeader, &hstring);
                Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
                return hstring;
            }
        }

        internal static string ConvertToManaged(IntPtr hstring)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            return WindowsRuntimeMarshal.HStringToString(hstring);
        }

        internal static void ClearNative(IntPtr hstring)
        {
                        if (hstring != IntPtr.Zero)
            {
                System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsDeleteString(hstring);
            }
        }
    }

    internal static class ObjectMarshaler
    {
        static internal extern void ConvertToNative(object objSrc, IntPtr pDstVariant);
        static internal extern object ConvertToManaged(IntPtr pSrcVariant);
        static internal extern void ClearNative(IntPtr pVariant);
    }

    internal static class ValueClassMarshaler
    {
        static internal extern void ConvertToNative(IntPtr dst, IntPtr src, IntPtr pMT, ref CleanupWorkList pCleanupWorkList);
        static internal extern void ConvertToManaged(IntPtr dst, IntPtr src, IntPtr pMT);
        static internal extern void ClearNative(IntPtr dst, IntPtr pMT);
    }

    internal static class DateMarshaler
    {
        static internal extern double ConvertToNative(DateTime managedDate);
        static internal extern long ConvertToManaged(double nativeDate);
    }

    internal static class InterfaceMarshaler
    {
        static internal extern IntPtr ConvertToNative(object objSrc, IntPtr itfMT, IntPtr classMT, int flags);
        static internal extern object ConvertToManaged(IntPtr pUnk, IntPtr itfMT, IntPtr classMT, int flags);
        static internal extern void ClearNative(IntPtr pUnk);
        static internal extern object ConvertToManagedWithoutUnboxing(IntPtr pNative);
    }

    internal static class UriMarshaler
    {
        static internal extern string GetRawUriFromNative(IntPtr pUri);
        static unsafe internal extern IntPtr CreateNativeUriInstanceHelper(char *rawUri, int strLen);
        static unsafe internal IntPtr CreateNativeUriInstance(string rawUri)
        {
            fixed (char *pManaged = rawUri)
            {
                return CreateNativeUriInstanceHelper(pManaged, rawUri.Length);
            }
        }
    }

    internal static class EventArgsMarshaler
    {
        static internal IntPtr CreateNativeNCCEventArgsInstance(int action, object newItems, object oldItems, int newIndex, int oldIndex)
        {
            IntPtr newItemsIP = IntPtr.Zero;
            IntPtr oldItemsIP = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                if (newItems != null)
                    newItemsIP = Marshal.GetComInterfaceForObject(newItems, typeof (IBindableVector));
                if (oldItems != null)
                    oldItemsIP = Marshal.GetComInterfaceForObject(oldItems, typeof (IBindableVector));
                return CreateNativeNCCEventArgsInstanceHelper(action, newItemsIP, oldItemsIP, newIndex, oldIndex);
            }
            finally
            {
                if (!oldItemsIP.IsNull())
                    Marshal.Release(oldItemsIP);
                if (!newItemsIP.IsNull())
                    Marshal.Release(newItemsIP);
            }
        }

        static extern internal IntPtr CreateNativePCEventArgsInstance([MarshalAs(UnmanagedType.HString)] string name);
        static extern internal IntPtr CreateNativeNCCEventArgsInstanceHelper(int action, IntPtr newItem, IntPtr oldItem, int newIndex, int oldIndex);
    }

    internal static class MngdNativeArrayMarshaler
    {
        static internal extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags);
        static internal extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int cElements);
        static internal extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ClearNative(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);
        static internal extern void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);
    }

    internal static class MngdSafeArrayMarshaler
    {
        static internal extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int iRank, int dwFlags);
        static internal extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, object pOriginalManaged);
        static internal extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
    }

    internal static class MngdHiddenLengthArrayMarshaler
    {
        static internal extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, IntPtr cbElementSize, ushort vt);
        internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        internal static unsafe void ConvertContentsToNative_DateTime(ref DateTimeOffset[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                DateTimeNative*nativeBuffer = *(DateTimeNative**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    DateTimeOffsetMarshaler.ConvertToNative(ref managedArray[i], out nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToNative_Type(ref System.Type[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                TypeNameNative*nativeBuffer = *(TypeNameNative**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    SystemTypeMarshaler.ConvertToNative(managedArray[i], &nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToNative_Exception(ref Exception[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                Int32*nativeBuffer = *(Int32**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = HResultExceptionMarshaler.ConvertToNative(managedArray[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToNative_Nullable<T>(ref Nullable<T>[] managedArray, IntPtr pNativeHome)where T : struct
        {
            if (managedArray != null)
            {
                IntPtr*nativeBuffer = *(IntPtr**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = NullableMarshaler.ConvertToNative<T>(ref managedArray[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToNative_KeyValuePair<K, V>(ref KeyValuePair<K, V>[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                IntPtr*nativeBuffer = *(IntPtr**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    nativeBuffer[i] = KeyValuePairMarshaler.ConvertToNative<K, V>(ref managedArray[i]);
                }
            }
        }

        internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int elementCount);
        internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        internal static unsafe void ConvertContentsToManaged_DateTime(ref DateTimeOffset[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                DateTimeNative*nativeBuffer = *(DateTimeNative**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    DateTimeOffsetMarshaler.ConvertToManaged(out managedArray[i], ref nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToManaged_Type(ref System.Type[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                TypeNameNative*nativeBuffer = *(TypeNameNative**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    SystemTypeMarshaler.ConvertToManaged(&nativeBuffer[i], ref managedArray[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToManaged_Exception(ref Exception[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                Int32*nativeBuffer = *(Int32**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = HResultExceptionMarshaler.ConvertToManaged(nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToManaged_Nullable<T>(ref Nullable<T>[] managedArray, IntPtr pNativeHome)where T : struct
        {
            if (managedArray != null)
            {
                IntPtr*nativeBuffer = *(IntPtr**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = NullableMarshaler.ConvertToManaged<T>(nativeBuffer[i]);
                }
            }
        }

        internal static unsafe void ConvertContentsToManaged_KeyValuePair<K, V>(ref KeyValuePair<K, V>[] managedArray, IntPtr pNativeHome)
        {
            if (managedArray != null)
            {
                IntPtr*nativeBuffer = *(IntPtr**)pNativeHome;
                for (int i = 0; i < managedArray.Length; i++)
                {
                    managedArray[i] = KeyValuePairMarshaler.ConvertToManaged<K, V>(nativeBuffer[i]);
                }
            }
        }

        internal static extern void ClearNativeContents(IntPtr pMarshalState, IntPtr pNativeHome, int cElements);
        internal static unsafe void ClearNativeContents_Type(IntPtr pNativeHome, int cElements)
        {
                        TypeNameNative*pNativeTypeArray = *(TypeNameNative**)pNativeHome;
            if (pNativeTypeArray != null)
            {
                for (int i = 0; i < cElements; ++i)
                {
                    SystemTypeMarshaler.ClearNative(pNativeTypeArray);
                    pNativeTypeArray++;
                }
            }
        }
    }

    internal static class MngdRefCustomMarshaler
    {
        static internal extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pCMHelper);
        static internal extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
        static internal extern void ClearManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
    }

    internal struct AsAnyMarshaler
    {
        private const ushort VTHACK_ANSICHAR = 253;
        private const ushort VTHACK_WINBOOL = 254;
        private enum BackPropAction
        {
            None,
            Array,
            Layout,
            StringBuilderAnsi,
            StringBuilderUnicode
        }

        private IntPtr pvArrayMarshaler;
        private BackPropAction backPropAction;
        private Type layoutType;
        private CleanupWorkList cleanupWorkList;
        private static bool IsIn(int dwFlags)
        {
            return ((dwFlags & 0x10000000) != 0);
        }

        private static bool IsOut(int dwFlags)
        {
            return ((dwFlags & 0x20000000) != 0);
        }

        private static bool IsAnsi(int dwFlags)
        {
            return ((dwFlags & 0x00FF0000) != 0);
        }

        private static bool IsThrowOn(int dwFlags)
        {
            return ((dwFlags & 0x0000FF00) != 0);
        }

        private static bool IsBestFit(int dwFlags)
        {
            return ((dwFlags & 0x000000FF) != 0);
        }

        internal AsAnyMarshaler(IntPtr pvArrayMarshaler)
        {
            BCLDebug.Assert(pvArrayMarshaler != IntPtr.Zero, "pvArrayMarshaler must not be null");
            this.pvArrayMarshaler = pvArrayMarshaler;
            this.backPropAction = BackPropAction.None;
            this.layoutType = null;
            this.cleanupWorkList = null;
        }

        private unsafe IntPtr ConvertArrayToNative(object pManagedHome, int dwFlags)
        {
            Type elementType = pManagedHome.GetType().GetElementType();
            VarEnum vt = VarEnum.VT_EMPTY;
            switch (Type.GetTypeCode(elementType))
            {
                case TypeCode.SByte:
                    vt = VarEnum.VT_I1;
                    break;
                case TypeCode.Byte:
                    vt = VarEnum.VT_UI1;
                    break;
                case TypeCode.Int16:
                    vt = VarEnum.VT_I2;
                    break;
                case TypeCode.UInt16:
                    vt = VarEnum.VT_UI2;
                    break;
                case TypeCode.Int32:
                    vt = VarEnum.VT_I4;
                    break;
                case TypeCode.UInt32:
                    vt = VarEnum.VT_UI4;
                    break;
                case TypeCode.Int64:
                    vt = VarEnum.VT_I8;
                    break;
                case TypeCode.UInt64:
                    vt = VarEnum.VT_UI8;
                    break;
                case TypeCode.Single:
                    vt = VarEnum.VT_R4;
                    break;
                case TypeCode.Double:
                    vt = VarEnum.VT_R8;
                    break;
                case TypeCode.Char:
                    vt = (IsAnsi(dwFlags) ? (VarEnum)VTHACK_ANSICHAR : VarEnum.VT_UI2);
                    break;
                case TypeCode.Boolean:
                    vt = (VarEnum)VTHACK_WINBOOL;
                    break;
                case TypeCode.Object:
                {
                    if (elementType == typeof (IntPtr))
                    {
                        vt = (IntPtr.Size == 4 ? VarEnum.VT_I4 : VarEnum.VT_I8);
                    }
                    else if (elementType == typeof (UIntPtr))
                    {
                        vt = (IntPtr.Size == 4 ? VarEnum.VT_UI4 : VarEnum.VT_UI8);
                    }
                    else
                        goto default;
                    break;
                }

                default:
                    throw new ArgumentException(Environment.GetResourceString("Arg_NDirectBadObject"));
            }

            int dwArrayMarshalerFlags = (int)vt;
            if (IsBestFit(dwFlags))
                dwArrayMarshalerFlags |= (1 << 16);
            if (IsThrowOn(dwFlags))
                dwArrayMarshalerFlags |= (1 << 24);
            MngdNativeArrayMarshaler.CreateMarshaler(pvArrayMarshaler, IntPtr.Zero, dwArrayMarshalerFlags);
            IntPtr pNativeHome;
            IntPtr pNativeHomeAddr = new IntPtr(&pNativeHome);
            MngdNativeArrayMarshaler.ConvertSpaceToNative(pvArrayMarshaler, ref pManagedHome, pNativeHomeAddr);
            if (IsIn(dwFlags))
            {
                MngdNativeArrayMarshaler.ConvertContentsToNative(pvArrayMarshaler, ref pManagedHome, pNativeHomeAddr);
            }

            if (IsOut(dwFlags))
            {
                backPropAction = BackPropAction.Array;
            }

            return pNativeHome;
        }

        private static IntPtr ConvertStringToNative(string pManagedHome, int dwFlags)
        {
            IntPtr pNativeHome;
            if (IsAnsi(dwFlags))
            {
                pNativeHome = CSTRMarshaler.ConvertToNative(dwFlags & 0xFFFF, pManagedHome, IntPtr.Zero);
            }
            else
            {
                StubHelpers.CheckStringLength(pManagedHome.Length);
                int allocSize = (pManagedHome.Length + 1) * 2;
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);
                String.InternalCopy(pManagedHome, pNativeHome, allocSize);
            }

            return pNativeHome;
        }

        private unsafe IntPtr ConvertStringBuilderToNative(StringBuilder pManagedHome, int dwFlags)
        {
            IntPtr pNativeHome;
            if (IsAnsi(dwFlags))
            {
                StubHelpers.CheckStringLength(pManagedHome.Capacity);
                int allocSize = (pManagedHome.Capacity * Marshal.SystemMaxDBCSCharSize) + 4;
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);
                byte *ptr = (byte *)pNativeHome;
                *(ptr + allocSize - 3) = 0;
                *(ptr + allocSize - 2) = 0;
                *(ptr + allocSize - 1) = 0;
                if (IsIn(dwFlags))
                {
                    int length = pManagedHome.ToString().ConvertToAnsi(ptr, allocSize, IsBestFit(dwFlags), IsThrowOn(dwFlags));
                                    }

                if (IsOut(dwFlags))
                {
                    backPropAction = BackPropAction.StringBuilderAnsi;
                }
            }
            else
            {
                int allocSize = (pManagedHome.Capacity * 2) + 4;
                pNativeHome = Marshal.AllocCoTaskMem(allocSize);
                byte *ptr = (byte *)pNativeHome;
                *(ptr + allocSize - 1) = 0;
                *(ptr + allocSize - 2) = 0;
                if (IsIn(dwFlags))
                {
                    int length = pManagedHome.Length * 2;
                    pManagedHome.InternalCopy(pNativeHome, length);
                    *(ptr + length + 0) = 0;
                    *(ptr + length + 1) = 0;
                }

                if (IsOut(dwFlags))
                {
                    backPropAction = BackPropAction.StringBuilderUnicode;
                }
            }

            return pNativeHome;
        }

        private unsafe IntPtr ConvertLayoutToNative(object pManagedHome, int dwFlags)
        {
            int allocSize = Marshal.SizeOfHelper(pManagedHome.GetType(), false);
            IntPtr pNativeHome = Marshal.AllocCoTaskMem(allocSize);
            if (IsIn(dwFlags))
            {
                StubHelpers.FmtClassUpdateNativeInternal(pManagedHome, (byte *)pNativeHome.ToPointer(), ref cleanupWorkList);
            }

            if (IsOut(dwFlags))
            {
                backPropAction = BackPropAction.Layout;
            }

            layoutType = pManagedHome.GetType();
            return pNativeHome;
        }

        internal IntPtr ConvertToNative(object pManagedHome, int dwFlags)
        {
            if (pManagedHome == null)
                return IntPtr.Zero;
            if (pManagedHome is ArrayWithOffset)
                throw new ArgumentException(Environment.GetResourceString("Arg_MarshalAsAnyRestriction"));
            IntPtr pNativeHome;
            if (pManagedHome.GetType().IsArray)
            {
                pNativeHome = ConvertArrayToNative(pManagedHome, dwFlags);
            }
            else
            {
                string strValue;
                StringBuilder sbValue;
                if ((strValue = pManagedHome as string) != null)
                {
                    pNativeHome = ConvertStringToNative(strValue, dwFlags);
                }
                else if ((sbValue = pManagedHome as StringBuilder) != null)
                {
                    pNativeHome = ConvertStringBuilderToNative(sbValue, dwFlags);
                }
                else if (pManagedHome.GetType().IsLayoutSequential || pManagedHome.GetType().IsExplicitLayout)
                {
                    pNativeHome = ConvertLayoutToNative(pManagedHome, dwFlags);
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Arg_NDirectBadObject"));
                }
            }

            return pNativeHome;
        }

        internal unsafe void ConvertToManaged(object pManagedHome, IntPtr pNativeHome)
        {
            switch (backPropAction)
            {
                case BackPropAction.Array:
                {
                    MngdNativeArrayMarshaler.ConvertContentsToManaged(pvArrayMarshaler, ref pManagedHome, new IntPtr(&pNativeHome));
                    break;
                }

                case BackPropAction.Layout:
                {
                    StubHelpers.FmtClassUpdateCLRInternal(pManagedHome, (byte *)pNativeHome.ToPointer());
                    break;
                }

                case BackPropAction.StringBuilderAnsi:
                {
                    sbyte *ptr = (sbyte *)pNativeHome.ToPointer();
                    ((StringBuilder)pManagedHome).ReplaceBufferAnsiInternal(ptr, Win32Native.lstrlenA(pNativeHome));
                    break;
                }

                case BackPropAction.StringBuilderUnicode:
                {
                    char *ptr = (char *)pNativeHome.ToPointer();
                    ((StringBuilder)pManagedHome).ReplaceBufferInternal(ptr, Win32Native.lstrlenW(pNativeHome));
                    break;
                }
            }
        }

        internal void ClearNative(IntPtr pNativeHome)
        {
            if (pNativeHome != IntPtr.Zero)
            {
                if (layoutType != null)
                {
                    Marshal.DestroyStructure(pNativeHome, layoutType);
                }

                Win32Native.CoTaskMemFree(pNativeHome);
            }

            StubHelpers.DestroyCleanupList(ref cleanupWorkList);
        }
    }

    internal static class NullableMarshaler
    {
        static internal IntPtr ConvertToNative<T>(ref Nullable<T> pManaged)where T : struct
        {
            if (pManaged.HasValue)
            {
                object impl = IReferenceFactory.CreateIReference(pManaged);
                return Marshal.GetComInterfaceForObject(impl, typeof (IReference<T>));
            }
            else
            {
                return IntPtr.Zero;
            }
        }

        static internal void ConvertToManagedRetVoid<T>(IntPtr pNative, ref Nullable<T> retObj)where T : struct
        {
            retObj = ConvertToManaged<T>(pNative);
        }

        static internal Nullable<T> ConvertToManaged<T>(IntPtr pNative)where T : struct
        {
            if (pNative != IntPtr.Zero)
            {
                object wrapper = InterfaceMarshaler.ConvertToManagedWithoutUnboxing(pNative);
                return (Nullable<T>)CLRIReferenceImpl<T>.UnboxHelper(wrapper);
            }
            else
            {
                return new Nullable<T>();
            }
        }
    }

    internal struct TypeNameNative
    {
        internal IntPtr typeName;
        internal TypeKind typeKind;
    }

    internal enum TypeKind
    {
        Primitive,
        Metadata,
        Projection
    }

    ;
    internal static class WinRTTypeNameConverter
    {
        internal static extern string ConvertToWinRTTypeName(System.Type managedType, out bool isPrimitive);
        internal static extern System.Type GetTypeFromWinRTTypeName(string typeName, out bool isPrimitive);
    }

    internal static class SystemTypeMarshaler
    {
        internal static unsafe void ConvertToNative(System.Type managedType, TypeNameNative*pNativeType)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            string typeName;
            if (managedType != null)
            {
                if (managedType.GetType() != typeof (System.RuntimeType))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_WinRTSystemRuntimeType", managedType.GetType().ToString()));
                }

                bool isPrimitive;
                string winrtTypeName = WinRTTypeNameConverter.ConvertToWinRTTypeName(managedType, out isPrimitive);
                if (winrtTypeName != null)
                {
                    typeName = winrtTypeName;
                    if (isPrimitive)
                        pNativeType->typeKind = TypeKind.Primitive;
                    else
                        pNativeType->typeKind = TypeKind.Metadata;
                }
                else
                {
                    typeName = managedType.AssemblyQualifiedName;
                    pNativeType->typeKind = TypeKind.Projection;
                }
            }
            else
            {
                typeName = "";
                pNativeType->typeKind = TypeKind.Projection;
            }

            int hrCreate = System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateString(typeName, typeName.Length, &pNativeType->typeName);
            Marshal.ThrowExceptionForHR(hrCreate, new IntPtr(-1));
        }

        internal static unsafe void ConvertToManaged(TypeNameNative*pNativeType, ref System.Type managedType)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            string typeName = WindowsRuntimeMarshal.HStringToString(pNativeType->typeName);
            if (String.IsNullOrEmpty(typeName))
            {
                managedType = null;
                return;
            }

            if (pNativeType->typeKind == TypeKind.Projection)
            {
                managedType = Type.GetType(typeName, true);
            }
            else
            {
                bool isPrimitive;
                managedType = WinRTTypeNameConverter.GetTypeFromWinRTTypeName(typeName, out isPrimitive);
                if (isPrimitive != (pNativeType->typeKind == TypeKind.Primitive))
                    throw new ArgumentException(Environment.GetResourceString("Argument_Unexpected_TypeSource"));
            }
        }

        internal static unsafe void ClearNative(TypeNameNative*pNativeType)
        {
                        if (pNativeType->typeName != IntPtr.Zero)
            {
                System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsDeleteString(pNativeType->typeName);
            }
        }
    }

    internal static class HResultExceptionMarshaler
    {
        static internal unsafe int ConvertToNative(Exception ex)
        {
            if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            if (ex == null)
                return 0;
            return ex._HResult;
        }

        static internal unsafe Exception ConvertToManaged(int hr)
        {
                        if (!Environment.IsWinRTSupported)
            {
                throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
            }

            Exception e = null;
            if (hr < 0)
            {
                e = StubHelpers.InternalGetCOMHRExceptionObject(hr, IntPtr.Zero, null, true);
            }

                        return e;
        }
    }

    internal static class KeyValuePairMarshaler
    {
        internal static IntPtr ConvertToNative<K, V>([In] ref KeyValuePair<K, V> pair)
        {
            IKeyValuePair<K, V> impl = new CLRIKeyValuePairImpl<K, V>(ref pair);
            return Marshal.GetComInterfaceForObject(impl, typeof (IKeyValuePair<K, V>));
        }

        internal static KeyValuePair<K, V> ConvertToManaged<K, V>(IntPtr pInsp)
        {
            object obj = InterfaceMarshaler.ConvertToManagedWithoutUnboxing(pInsp);
            IKeyValuePair<K, V> pair = (IKeyValuePair<K, V>)obj;
            return new KeyValuePair<K, V>(pair.Key, pair.Value);
        }

        internal static object ConvertToManagedBox<K, V>(IntPtr pInsp)
        {
            return (object)ConvertToManaged<K, V>(pInsp);
        }
    }

    internal struct NativeVariant
    {
        ushort vt;
        ushort wReserved1;
        ushort wReserved2;
        ushort wReserved3;
        IntPtr data1;
        IntPtr data2;
    }

    internal sealed class CleanupWorkListElement
    {
        public CleanupWorkListElement(SafeHandle handle)
        {
            m_handle = handle;
        }

        public SafeHandle m_handle;
        public bool m_owned;
    }

    internal sealed class CleanupWorkList
    {
        private List<CleanupWorkListElement> m_list = new List<CleanupWorkListElement>();
        public void Add(CleanupWorkListElement elem)
        {
            BCLDebug.Assert(elem.m_owned == false, "m_owned is supposed to be false and set later by DangerousAddRef");
            m_list.Add(elem);
        }

        public void Destroy()
        {
            for (int i = m_list.Count - 1; i >= 0; i--)
            {
                if (m_list[i].m_owned)
                    StubHelpers.SafeHandleRelease(m_list[i].m_handle);
            }
        }
    }

    internal static class StubHelpers
    {
        static internal extern bool IsQCall(IntPtr pMD);
        static internal extern void InitDeclaringType(IntPtr pMD);
        static internal extern IntPtr GetNDirectTarget(IntPtr pMD);
        static internal extern IntPtr GetDelegateTarget(Delegate pThis, ref IntPtr pStubArg);
        static internal extern void ClearLastError();
        static internal extern void SetLastError();
        static internal extern void ThrowInteropParamException(int resID, int paramIdx);
        static internal IntPtr AddToCleanupList(ref CleanupWorkList pCleanupWorkList, SafeHandle handle)
        {
            if (pCleanupWorkList == null)
                pCleanupWorkList = new CleanupWorkList();
            CleanupWorkListElement element = new CleanupWorkListElement(handle);
            pCleanupWorkList.Add(element);
            return SafeHandleAddRef(handle, ref element.m_owned);
        }

        static internal void DestroyCleanupList(ref CleanupWorkList pCleanupWorkList)
        {
            if (pCleanupWorkList != null)
            {
                pCleanupWorkList.Destroy();
                pCleanupWorkList = null;
            }
        }

        static internal Exception GetHRExceptionObject(int hr)
        {
            Exception ex = InternalGetHRExceptionObject(hr);
            return ex;
        }

        static internal extern Exception InternalGetHRExceptionObject(int hr);
        static internal Exception GetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object pThis)
        {
            Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis, false);
            return ex;
        }

        static internal Exception GetCOMHRExceptionObject_WinRT(int hr, IntPtr pCPCMD, object pThis)
        {
            Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis, true);
            return ex;
        }

        static internal extern Exception InternalGetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object pThis, bool fForWinRT);
        static internal extern IntPtr CreateCustomMarshalerHelper(IntPtr pMD, int paramToken, IntPtr hndManagedType);
        static internal IntPtr SafeHandleAddRef(SafeHandle pHandle, ref bool success)
        {
            if (pHandle == null)
            {
                throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_SafeHandle"));
            }

                        pHandle.DangerousAddRef(ref success);
            return (success ? pHandle.DangerousGetHandle() : IntPtr.Zero);
        }

        static internal void SafeHandleRelease(SafeHandle pHandle)
        {
            if (pHandle == null)
            {
                throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_SafeHandle"));
            }

                        try
            {
                pHandle.DangerousRelease();
            }
            catch (Exception)
            {
            }
        }

        static internal extern IntPtr GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease);
        static internal extern IntPtr GetCOMIPFromRCW_WinRT(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);
        static internal extern IntPtr GetCOMIPFromRCW_WinRTSharedGeneric(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);
        static internal extern IntPtr GetCOMIPFromRCW_WinRTDelegate(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget);
        static internal extern bool ShouldCallWinRTInterface(object objSrc, IntPtr pCPCMD);
        static internal extern Delegate GetTargetForAmbiguousVariantCall(object objSrc, IntPtr pMT, out bool fUseString);
        static internal extern void StubRegisterRCW(object pThis);
        static internal extern void StubUnregisterRCW(object pThis);
        static internal extern IntPtr GetDelegateInvokeMethod(Delegate pThis);
        static internal extern object GetWinRTFactoryObject(IntPtr pCPCMD);
        static internal extern IntPtr GetWinRTFactoryReturnValue(object pThis, IntPtr pCtorEntry);
        static internal extern IntPtr GetOuterInspectable(object pThis, IntPtr pCtorMD);
        static internal extern IntPtr ProfilerBeginTransitionCallback(IntPtr pSecretParam, IntPtr pThread, object pThis);
        static internal extern void ProfilerEndTransitionCallback(IntPtr pMD, IntPtr pThread);
        static internal void CheckStringLength(int length)
        {
            CheckStringLength((uint)length);
        }

        static internal void CheckStringLength(uint length)
        {
            if (length > 0x7ffffff0)
            {
                throw new MarshalDirectiveException(Environment.GetResourceString("Marshaler_StringTooLong"));
            }
        }

        static internal unsafe extern int strlen(sbyte *ptr);
        static internal extern void DecimalCanonicalizeInternal(ref Decimal dec);
        static internal unsafe extern void FmtClassUpdateNativeInternal(object obj, byte *pNative, ref CleanupWorkList pCleanupWorkList);
        static internal unsafe extern void FmtClassUpdateCLRInternal(object obj, byte *pNative);
        static internal unsafe extern void LayoutDestroyNativeInternal(byte *pNative, IntPtr pMT);
        static internal extern object AllocateInternal(IntPtr typeHandle);
        static internal extern void MarshalToUnmanagedVaListInternal(IntPtr va_list, uint vaListSize, IntPtr pArgIterator);
        static internal extern void MarshalToManagedVaListInternal(IntPtr va_list, IntPtr pArgIterator);
        static internal extern uint CalcVaListSize(IntPtr va_list);
        static internal extern void ValidateObject(object obj, IntPtr pMD, object pThis);
        static internal extern void LogPinnedArgument(IntPtr localDesc, IntPtr nativeArg);
        static internal extern void ValidateByref(IntPtr byref, IntPtr pMD, object pThis);
        static internal extern IntPtr GetStubContext();
        static internal extern IntPtr GetStubContextAddr();
        internal static extern void ArrayTypeCheck(object o, Object[] arr);
    }
}