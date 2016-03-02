

namespace System.Collections
{
    public sealed class BitArray : ICollection, ICloneable
    {
        private BitArray()
        {
        }

        public BitArray(int length): this (length, false)
        {
        }

        public BitArray(int length, bool defaultValue)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

                        m_array = new int[GetArrayLength(length, BitsPerInt32)];
            m_length = length;
            int fillValue = defaultValue ? unchecked (((int)0xffffffff)) : 0;
            for (int i = 0; i < m_array.Length; i++)
            {
                m_array[i] = fillValue;
            }

            _version = 0;
        }

        public BitArray(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

                        if (bytes.Length > Int32.MaxValue / BitsPerByte)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ArrayTooLarge", BitsPerByte), "bytes");
            }

            m_array = new int[GetArrayLength(bytes.Length, BytesPerInt32)];
            m_length = bytes.Length * BitsPerByte;
            int i = 0;
            int j = 0;
            while (bytes.Length - j >= 4)
            {
                m_array[i++] = (bytes[j] & 0xff) | ((bytes[j + 1] & 0xff) << 8) | ((bytes[j + 2] & 0xff) << 16) | ((bytes[j + 3] & 0xff) << 24);
                j += 4;
            }

                                    switch (bytes.Length - j)
            {
                case 3:
                    m_array[i] = ((bytes[j + 2] & 0xff) << 16);
                    goto case 2;
                case 2:
                    m_array[i] |= ((bytes[j + 1] & 0xff) << 8);
                    goto case 1;
                case 1:
                    m_array[i] |= (bytes[j] & 0xff);
                    break;
            }

            _version = 0;
        }

        public BitArray(bool[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

                        m_array = new int[GetArrayLength(values.Length, BitsPerInt32)];
            m_length = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i])
                    m_array[i / 32] |= (1 << (i % 32));
            }

            _version = 0;
        }

        public BitArray(int[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

                        if (values.Length > Int32.MaxValue / BitsPerInt32)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ArrayTooLarge", BitsPerInt32), "values");
            }

            m_array = new int[values.Length];
            m_length = values.Length * BitsPerInt32;
            Array.Copy(values, m_array, values.Length);
            _version = 0;
        }

        public BitArray(BitArray bits)
        {
            if (bits == null)
            {
                throw new ArgumentNullException("bits");
            }

                        int arrayLength = GetArrayLength(bits.m_length, BitsPerInt32);
            m_array = new int[arrayLength];
            m_length = bits.m_length;
            Array.Copy(bits.m_array, m_array, arrayLength);
            _version = bits._version;
        }

        public bool this[int index]
        {
            get
            {
                return Get(index);
            }

            set
            {
                Set(index, value);
            }
        }

        public bool Get(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

                        return (m_array[index / 32] & (1 << (index % 32))) != 0;
        }

        public void Set(int index, bool value)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

                        if (value)
            {
                m_array[index / 32] |= (1 << (index % 32));
            }
            else
            {
                m_array[index / 32] &= ~(1 << (index % 32));
            }

            _version++;
        }

        public void SetAll(bool value)
        {
            int fillValue = value ? unchecked (((int)0xffffffff)) : 0;
            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] = fillValue;
            }

            _version++;
        }

        public BitArray And(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (Length != value.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayLengthsDiffer"));
                        int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] &= value.m_array[i];
            }

            _version++;
            return this;
        }

        public BitArray Or(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (Length != value.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayLengthsDiffer"));
                        int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] |= value.m_array[i];
            }

            _version++;
            return this;
        }

        public BitArray Xor(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (Length != value.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayLengthsDiffer"));
                        int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] ^= value.m_array[i];
            }

            _version++;
            return this;
        }

        public BitArray Not()
        {
            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] = ~m_array[i];
            }

            _version++;
            return this;
        }

        public int Length
        {
            get
            {
                                return m_length;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                }

                                int newints = GetArrayLength(value, BitsPerInt32);
                if (newints > m_array.Length || newints + _ShrinkThreshold < m_array.Length)
                {
                    int[] newarray = new int[newints];
                    Array.Copy(m_array, newarray, newints > m_array.Length ? m_array.Length : newints);
                    m_array = newarray;
                }

                if (value > m_length)
                {
                    int last = GetArrayLength(m_length, BitsPerInt32) - 1;
                    int bits = m_length % 32;
                    if (bits > 0)
                    {
                        m_array[last] &= (1 << bits) - 1;
                    }

                    Array.Clear(m_array, last + 1, newints - last - 1);
                }

                m_length = value;
                _version++;
            }
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
                        if (array is int[])
            {
                Array.Copy(m_array, 0, array, index, GetArrayLength(m_length, BitsPerInt32));
            }
            else if (array is byte[])
            {
                int arrayLength = GetArrayLength(m_length, BitsPerByte);
                if ((array.Length - index) < arrayLength)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                byte[] b = (byte[])array;
                for (int i = 0; i < arrayLength; i++)
                    b[index + i] = (byte)((m_array[i / 4] >> ((i % 4) * 8)) & 0x000000FF);
            }
            else if (array is bool[])
            {
                if (array.Length - index < m_length)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                bool[] b = (bool[])array;
                for (int i = 0; i < m_length; i++)
                    b[index + i] = ((m_array[i / 32] >> (i % 32)) & 0x00000001) != 0;
            }
            else
                throw new ArgumentException(Environment.GetResourceString("Arg_BitArrayTypeUnsupported"));
        }

        public int Count
        {
            get
            {
                                return m_length;
            }
        }

        public Object Clone()
        {
                                    return new BitArray(this);
        }

        public Object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }

                return _syncRoot;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new BitArrayEnumeratorSimple(this);
        }

        private const int BitsPerInt32 = 32;
        private const int BytesPerInt32 = 4;
        private const int BitsPerByte = 8;
        private static int GetArrayLength(int n, int div)
        {
                        return n > 0 ? (((n - 1) / div) + 1) : 0;
        }

        private class BitArrayEnumeratorSimple : IEnumerator, ICloneable
        {
            private BitArray bitarray;
            private int index;
            private int version;
            private bool currentElement;
            internal BitArrayEnumeratorSimple(BitArray bitarray)
            {
                this.bitarray = bitarray;
                this.index = -1;
                version = bitarray._version;
            }

            public Object Clone()
            {
                return MemberwiseClone();
            }

            public virtual bool MoveNext()
            {
                if (version != bitarray._version)
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                if (index < (bitarray.Count - 1))
                {
                    index++;
                    currentElement = bitarray.Get(index);
                    return true;
                }
                else
                    index = bitarray.Count;
                return false;
            }

            public virtual Object Current
            {
                get
                {
                    if (index == -1)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (index >= bitarray.Count)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    return currentElement;
                }
            }

            public void Reset()
            {
                if (version != bitarray._version)
                    throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumFailedVersion));
                index = -1;
            }
        }

        private int[] m_array;
        private int m_length;
        private int _version;
        private Object _syncRoot;
        private const int _ShrinkThreshold = 256;
    }
}