
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
                        return ret;
        }

        static private T UnsafeCastInternal<T>(Object o)where T : class
        {
            throw new InvalidOperationException();
        }

        static internal int UnsafeEnumCast<T>(T val)where T : struct
        {
                        return UnsafeEnumCastInternal<T>(val);
        }

        static private int UnsafeEnumCastInternal<T>(T val)where T : struct
        {
            throw new InvalidOperationException();
        }

        static internal long UnsafeEnumCastLong<T>(T val)where T : struct
        {
                        return UnsafeEnumCastLongInternal<T>(val);
        }

        static private long UnsafeEnumCastLongInternal<T>(T val)where T : struct
        {
            throw new InvalidOperationException();
        }

        static internal IntPtr UnsafeCastToStackPointer<T>(ref T val)
        {
            IntPtr p = UnsafeCastToStackPointerInternal<T>(ref val);
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