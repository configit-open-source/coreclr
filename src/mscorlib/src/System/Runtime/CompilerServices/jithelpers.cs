using System.Diagnostics.Contracts;
using System.Threading;

namespace System.Runtime.CompilerServices
{
    internal struct StringHandleOnStack
    {
        private IntPtr m_ptr;
        internal StringHandleOnStack(IntPtr pString)
        {
            m_ptr = pString;
        }
    }

    internal struct ObjectHandleOnStack
    {
        private IntPtr m_ptr;
        internal ObjectHandleOnStack(IntPtr pObject)
        {
            m_ptr = pObject;
        }
    }

    internal struct StackCrawlMarkHandle
    {
        private IntPtr m_ptr;
        internal StackCrawlMarkHandle(IntPtr stackMark)
        {
            m_ptr = stackMark;
        }
    }

    internal class PinningHelper
    {
        public byte m_data;
    }

    internal static class JitHelpers
    {
        internal const string QCall = "QCall";
        static internal StringHandleOnStack GetStringHandleOnStack(ref string s)
        {
            return new StringHandleOnStack(UnsafeCastToStackPointer(ref s));
        }

        static internal ObjectHandleOnStack GetObjectHandleOnStack<T>(ref T o)where T : class
        {
            return new ObjectHandleOnStack(UnsafeCastToStackPointer(ref o));
        }

        static internal StackCrawlMarkHandle GetStackCrawlMarkHandle(ref StackCrawlMark stackMark)
        {
            return new StackCrawlMarkHandle(UnsafeCastToStackPointer(ref stackMark));
        }

        static internal T UnsafeCast<T>(Object o)where T : class
        {
            T ret = UnsafeCastInternal<T>(o);
            Contract.Assert(ret == (o as T), "Invalid use of JitHelpers.UnsafeCast!");
            return ret;
        }

        static private T UnsafeCastInternal<T>(Object o)where T : class
        {
            throw new InvalidOperationException();
        }

        static internal int UnsafeEnumCast<T>(T val)where T : struct
        {
            Contract.Assert(typeof (T).IsEnum && (Enum.GetUnderlyingType(typeof (T)) == typeof (int) || Enum.GetUnderlyingType(typeof (T)) == typeof (uint) || Enum.GetUnderlyingType(typeof (T)) == typeof (short) || Enum.GetUnderlyingType(typeof (T)) == typeof (ushort) || Enum.GetUnderlyingType(typeof (T)) == typeof (byte) || Enum.GetUnderlyingType(typeof (T)) == typeof (sbyte)), "Error, T must be an 4 byte (or less) enum JitHelpers.UnsafeEnumCast!");
            return UnsafeEnumCastInternal<T>(val);
        }

        static private int UnsafeEnumCastInternal<T>(T val)where T : struct
        {
            throw new InvalidOperationException();
        }

        static internal long UnsafeEnumCastLong<T>(T val)where T : struct
        {
            Contract.Assert(typeof (T).IsEnum && (Enum.GetUnderlyingType(typeof (T)) == typeof (long) || Enum.GetUnderlyingType(typeof (T)) == typeof (ulong)), "Error, T must be an 8 byte enum JitHelpers.UnsafeEnumCastLong!");
            return UnsafeEnumCastLongInternal<T>(val);
        }

        static private long UnsafeEnumCastLongInternal<T>(T val)where T : struct
        {
            throw new InvalidOperationException();
        }

        static internal IntPtr UnsafeCastToStackPointer<T>(ref T val)
        {
            IntPtr p = UnsafeCastToStackPointerInternal<T>(ref val);
            Contract.Assert(IsAddressInStack(p), "Pointer not in the stack!");
            return p;
        }

        static private IntPtr UnsafeCastToStackPointerInternal<T>(ref T val)
        {
            throw new InvalidOperationException();
        }

        extern static internal void UnsafeSetArrayElement(Object[] target, int index, Object element);
        static internal PinningHelper GetPinningHelper(Object o)
        {
            return UnsafeCastInternal<PinningHelper>(o);
        }

        extern static bool IsAddressInStack(IntPtr ptr);
    }
}