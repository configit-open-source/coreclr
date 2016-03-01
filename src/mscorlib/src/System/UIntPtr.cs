namespace System
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Diagnostics.Contracts;

    public struct UIntPtr : ISerializable
    {
        unsafe private void *m_value;
        public static readonly UIntPtr Zero;
        public unsafe UIntPtr(uint value)
        {
            m_value = (void *)value;
        }

        public unsafe UIntPtr(ulong value)
        {
            m_value = (void *)value;
        }

        public unsafe UIntPtr(void *value)
        {
            m_value = value;
        }

        private unsafe UIntPtr(SerializationInfo info, StreamingContext context)
        {
            ulong l = info.GetUInt64("value");
            if (Size == 4 && l > UInt32.MaxValue)
            {
                throw new ArgumentException(Environment.GetResourceString("Serialization_InvalidPtrValue"));
            }

            m_value = (void *)l;
        }

        public unsafe override bool Equals(Object obj)
        {
            if (obj is UIntPtr)
            {
                return (m_value == ((UIntPtr)obj).m_value);
            }

            return false;
        }

        public unsafe override int GetHashCode()
        {
            return unchecked ((int)((long)m_value)) & 0x7fffffff;
        }

        public unsafe uint ToUInt32()
        {
            return checked ((uint)m_value);
        }

        public unsafe ulong ToUInt64()
        {
            return (ulong)m_value;
        }

        public unsafe override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return ((ulong)m_value).ToString(CultureInfo.InvariantCulture);
        }

        public static explicit operator UIntPtr(uint value)
        {
            return new UIntPtr(value);
        }

        public static explicit operator UIntPtr(ulong value)
        {
            return new UIntPtr(value);
        }

        public unsafe static explicit operator uint (UIntPtr value)
        {
            return checked ((uint)value.m_value);
        }

        public unsafe static explicit operator ulong (UIntPtr value)
        {
            return (ulong)value.m_value;
        }

        public static unsafe explicit operator UIntPtr(void *value)
        {
            return new UIntPtr(value);
        }

        public static unsafe explicit operator void *(UIntPtr value)
        {
            return value.m_value;
        }

        public unsafe static bool operator ==(UIntPtr value1, UIntPtr value2)
        {
            return value1.m_value == value2.m_value;
        }

        public unsafe static bool operator !=(UIntPtr value1, UIntPtr value2)
        {
            return value1.m_value != value2.m_value;
        }

        public static UIntPtr Add(UIntPtr pointer, int offset)
        {
            return pointer + offset;
        }

        public static UIntPtr operator +(UIntPtr pointer, int offset)
        {
            return new UIntPtr(pointer.ToUInt64() + (ulong)offset);
        }

        public static UIntPtr Subtract(UIntPtr pointer, int offset)
        {
            return pointer - offset;
        }

        public static UIntPtr operator -(UIntPtr pointer, int offset)
        {
            return new UIntPtr(pointer.ToUInt64() - (ulong)offset);
        }

        public static int Size
        {
            [System.Runtime.Versioning.NonVersionable]
            get
            {
                return 8;
            }
        }

        public unsafe void *ToPointer()
        {
            return m_value;
        }
    }
}