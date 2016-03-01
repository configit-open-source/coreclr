using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;

namespace System
{
    public struct IntPtr : ISerializable
    {
        unsafe private void *m_value;
        public static readonly IntPtr Zero;
        internal unsafe bool IsNull()
        {
            return (this.m_value == null);
        }

        public unsafe IntPtr(int value)
        {
            m_value = (void *)(long)value;
        }

        public unsafe IntPtr(long value)
        {
            m_value = (void *)value;
        }

        public unsafe IntPtr(void *value)
        {
            m_value = value;
        }

        private unsafe IntPtr(SerializationInfo info, StreamingContext context)
        {
            long l = info.GetInt64("value");
            if (Size == 4 && (l > Int32.MaxValue || l < Int32.MinValue))
            {
                throw new ArgumentException(Environment.GetResourceString("Serialization_InvalidPtrValue"));
            }

            m_value = (void *)l;
        }

        public unsafe override bool Equals(Object obj)
        {
            if (obj is IntPtr)
            {
                return (m_value == ((IntPtr)obj).m_value);
            }

            return false;
        }

        public unsafe override int GetHashCode()
        {
            return unchecked ((int)((long)m_value));
        }

        public unsafe int ToInt32()
        {
            long l = (long)m_value;
            return checked ((int)l);
        }

        public unsafe long ToInt64()
        {
            return (long)m_value;
        }

        public unsafe override String ToString()
        {
            return ((long)m_value).ToString(CultureInfo.InvariantCulture);
        }

        public unsafe String ToString(String format)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return ((long)m_value).ToString(format, CultureInfo.InvariantCulture);
        }

        public static explicit operator IntPtr(int value)
        {
            return new IntPtr(value);
        }

        public static explicit operator IntPtr(long value)
        {
            return new IntPtr(value);
        }

        public static unsafe explicit operator IntPtr(void *value)
        {
            return new IntPtr(value);
        }

        public static unsafe explicit operator void *(IntPtr value)
        {
            return value.m_value;
        }

        public unsafe static explicit operator int (IntPtr value)
        {
            long l = (long)value.m_value;
            return checked ((int)l);
        }

        public unsafe static explicit operator long (IntPtr value)
        {
            return (long)value.m_value;
        }

        public unsafe static bool operator ==(IntPtr value1, IntPtr value2)
        {
            return value1.m_value == value2.m_value;
        }

        public unsafe static bool operator !=(IntPtr value1, IntPtr value2)
        {
            return value1.m_value != value2.m_value;
        }

        public static IntPtr Add(IntPtr pointer, int offset)
        {
            return pointer + offset;
        }

        public static IntPtr operator +(IntPtr pointer, int offset)
        {
            return new IntPtr(pointer.ToInt64() + offset);
        }

        public static IntPtr Subtract(IntPtr pointer, int offset)
        {
            return pointer - offset;
        }

        public static IntPtr operator -(IntPtr pointer, int offset)
        {
            return new IntPtr(pointer.ToInt64() - offset);
        }

        public static int Size
        {
            [Pure]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
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