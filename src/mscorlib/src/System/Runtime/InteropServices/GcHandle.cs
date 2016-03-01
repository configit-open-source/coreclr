using System.Diagnostics.Contracts;
using System.Threading;

namespace System.Runtime.InteropServices
{
    public enum GCHandleType
    {
        Weak = 0,
        WeakTrackResurrection = 1,
        Normal = 2,
        Pinned = 3
    }

    public struct GCHandle
    {
        private const GCHandleType MaxHandleType = GCHandleType.Pinned;
        internal GCHandle(Object value, GCHandleType type)
        {
            if ((uint)type > (uint)MaxHandleType)
                throw new ArgumentOutOfRangeException("type", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            Contract.EndContractBlock();
            m_handle = InternalAlloc(value, type);
            if (type == GCHandleType.Pinned)
                SetIsPinned();
        }

        internal GCHandle(IntPtr handle)
        {
            InternalCheckDomain(handle);
            m_handle = handle;
        }

        public static GCHandle Alloc(Object value)
        {
            return new GCHandle(value, GCHandleType.Normal);
        }

        public static GCHandle Alloc(Object value, GCHandleType type)
        {
            return new GCHandle(value, type);
        }

        public void Free()
        {
            IntPtr handle = m_handle;
            if (handle != IntPtr.Zero && Interlocked.CompareExchange(ref m_handle, IntPtr.Zero, handle) == handle)
            {
                InternalFree((IntPtr)(((long)handle) & ~1L));
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));
            }
        }

        public Object Target
        {
            [System.Security.SecurityCritical]
            get
            {
                if (m_handle == IntPtr.Zero)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));
                return InternalGet(GetHandleValue());
            }

            [System.Security.SecurityCritical]
            set
            {
                if (m_handle == IntPtr.Zero)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));
                InternalSet(GetHandleValue(), value, IsPinned());
            }
        }

        public IntPtr AddrOfPinnedObject()
        {
            if (!IsPinned())
            {
                if (m_handle == IntPtr.Zero)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotPinned"));
            }

            return InternalAddrOfPinnedObject(GetHandleValue());
        }

        public bool IsAllocated
        {
            get
            {
                return m_handle != IntPtr.Zero;
            }
        }

        public static explicit operator GCHandle(IntPtr value)
        {
            return FromIntPtr(value);
        }

        public static GCHandle FromIntPtr(IntPtr value)
        {
            if (value == IntPtr.Zero)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_HandleIsNotInitialized"));
            Contract.EndContractBlock();
            IntPtr handle = value;
            return new GCHandle(handle);
        }

        public static explicit operator IntPtr(GCHandle value)
        {
            return ToIntPtr(value);
        }

        public static IntPtr ToIntPtr(GCHandle value)
        {
            return value.m_handle;
        }

        public override int GetHashCode()
        {
            return m_handle.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            GCHandle hnd;
            if (o == null || !(o is GCHandle))
                return false;
            else
                hnd = (GCHandle)o;
            return m_handle == hnd.m_handle;
        }

        public static bool operator ==(GCHandle a, GCHandle b)
        {
            return a.m_handle == b.m_handle;
        }

        public static bool operator !=(GCHandle a, GCHandle b)
        {
            return a.m_handle != b.m_handle;
        }

        internal IntPtr GetHandleValue()
        {
            return new IntPtr(((long)m_handle) & ~1L);
        }

        internal bool IsPinned()
        {
            return (((long)m_handle) & 1) != 0;
        }

        internal void SetIsPinned()
        {
            m_handle = new IntPtr(((long)m_handle) | 1L);
        }

        internal static extern IntPtr InternalAlloc(Object value, GCHandleType type);
        internal static extern void InternalFree(IntPtr handle);
        internal static extern Object InternalGet(IntPtr handle);
        internal static extern void InternalSet(IntPtr handle, Object value, bool isPinned);
        internal static extern Object InternalCompareExchange(IntPtr handle, Object value, Object oldValue, bool isPinned);
        internal static extern IntPtr InternalAddrOfPinnedObject(IntPtr handle);
        internal static extern void InternalCheckDomain(IntPtr handle);
        internal static extern GCHandleType InternalGetHandleType(IntPtr handle);
        private IntPtr m_handle;
    }
}