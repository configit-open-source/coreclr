namespace System.Text
{
    using System.Text;
    using System.Runtime;
    using System.Runtime.Serialization;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Threading;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    public sealed class StringBuilder : ISerializable
    {
        internal char[] m_ChunkChars;
        internal StringBuilder m_ChunkPrevious;
        internal int m_ChunkLength;
        internal int m_ChunkOffset;
        internal int m_MaxCapacity = 0;
        internal const int DefaultCapacity = 16;
        private const String CapacityField = "Capacity";
        private const String MaxCapacityField = "m_MaxCapacity";
        private const String StringValueField = "m_StringValue";
        private const String ThreadIDField = "m_currentThread";
        internal const int MaxChunkSize = 8000;
        public StringBuilder(): this (DefaultCapacity)
        {
        }

        public StringBuilder(int capacity): this (String.Empty, capacity)
        {
        }

        public StringBuilder(String value): this (value, DefaultCapacity)
        {
        }

        public StringBuilder(String value, int capacity): this (value, 0, ((value != null) ? value.Length : 0), capacity)
        {
        }

        public StringBuilder(String value, int startIndex, int length, int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", "capacity"));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "length"));
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            Contract.EndContractBlock();
            if (value == null)
            {
                value = String.Empty;
            }

            if (startIndex > value.Length - length)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }

            m_MaxCapacity = Int32.MaxValue;
            if (capacity == 0)
            {
                capacity = DefaultCapacity;
            }

            if (capacity < length)
                capacity = length;
            m_ChunkChars = new char[capacity];
            m_ChunkLength = length;
            unsafe
            {
                fixed (char *sourcePtr = value)
                    ThreadSafeCopy(sourcePtr + startIndex, m_ChunkChars, 0, length);
            }
        }

        public StringBuilder(int capacity, int maxCapacity)
        {
            if (capacity > maxCapacity)
            {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
            }

            if (maxCapacity < 1)
            {
                throw new ArgumentOutOfRangeException("maxCapacity", Environment.GetResourceString("ArgumentOutOfRange_SmallMaxCapacity"));
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", "capacity"));
            }

            Contract.EndContractBlock();
            if (capacity == 0)
            {
                capacity = Math.Min(DefaultCapacity, maxCapacity);
            }

            m_MaxCapacity = maxCapacity;
            m_ChunkChars = new char[capacity];
        }

        private void VerifyClassInvariant()
        {
            BCLDebug.Correctness((uint)(m_ChunkOffset + m_ChunkChars.Length) >= m_ChunkOffset, "Integer Overflow");
            StringBuilder currentBlock = this;
            int maxCapacity = this.m_MaxCapacity;
            for (;;)
            {
                Contract.Assert(currentBlock.m_MaxCapacity == maxCapacity, "Bad maxCapacity");
                Contract.Assert(currentBlock.m_ChunkChars != null, "Empty Buffer");
                Contract.Assert(currentBlock.m_ChunkLength <= currentBlock.m_ChunkChars.Length, "Out of range length");
                Contract.Assert(currentBlock.m_ChunkLength >= 0, "Negative length");
                Contract.Assert(currentBlock.m_ChunkOffset >= 0, "Negative offset");
                StringBuilder prevBlock = currentBlock.m_ChunkPrevious;
                if (prevBlock == null)
                {
                    Contract.Assert(currentBlock.m_ChunkOffset == 0, "First chunk's offset is not 0");
                    break;
                }

                Contract.Assert(currentBlock.m_ChunkOffset == prevBlock.m_ChunkOffset + prevBlock.m_ChunkLength, "There is a gap between chunks!");
                currentBlock = prevBlock;
            }
        }

        public int Capacity
        {
            get
            {
                return m_ChunkChars.Length + m_ChunkOffset;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
                }

                if (value > MaxCapacity)
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_Capacity"));
                }

                if (value < Length)
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }

                Contract.EndContractBlock();
                if (Capacity != value)
                {
                    int newLen = value - m_ChunkOffset;
                    char[] newArray = new char[newLen];
                    Array.Copy(m_ChunkChars, newArray, m_ChunkLength);
                    m_ChunkChars = newArray;
                }
            }
        }

        public int MaxCapacity
        {
            get
            {
                return m_MaxCapacity;
            }
        }

        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
            }

            Contract.EndContractBlock();
            if (Capacity < capacity)
                Capacity = capacity;
            return Capacity;
        }

        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            VerifyClassInvariant();
            if (Length == 0)
                return String.Empty;
            string ret = string.FastAllocateString(Length);
            StringBuilder chunk = this;
            unsafe
            {
                fixed (char *destinationPtr = ret)
                {
                    do
                    {
                        if (chunk.m_ChunkLength > 0)
                        {
                            char[] sourceArray = chunk.m_ChunkChars;
                            int chunkOffset = chunk.m_ChunkOffset;
                            int chunkLength = chunk.m_ChunkLength;
                            if ((uint)(chunkLength + chunkOffset) <= ret.Length && (uint)chunkLength <= (uint)sourceArray.Length)
                            {
                                fixed (char *sourcePtr = sourceArray)
                                    string.wstrcpy(destinationPtr + chunkOffset, sourcePtr, chunkLength);
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException("chunkLength", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                            }
                        }

                        chunk = chunk.m_ChunkPrevious;
                    }
                    while (chunk != null);
                }
            }

            return ret;
        }

        public String ToString(int startIndex, int length)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            int currentLength = this.Length;
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (startIndex > currentLength)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndexLargerThanLength"));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex > (currentLength - length))
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }

            VerifyClassInvariant();
            StringBuilder chunk = this;
            int sourceEndIndex = startIndex + length;
            string ret = string.FastAllocateString(length);
            int curDestIndex = length;
            unsafe
            {
                fixed (char *destinationPtr = ret)
                {
                    while (curDestIndex > 0)
                    {
                        int chunkEndIndex = sourceEndIndex - chunk.m_ChunkOffset;
                        if (chunkEndIndex >= 0)
                        {
                            if (chunkEndIndex > chunk.m_ChunkLength)
                                chunkEndIndex = chunk.m_ChunkLength;
                            int countLeft = curDestIndex;
                            int chunkCount = countLeft;
                            int chunkStartIndex = chunkEndIndex - countLeft;
                            if (chunkStartIndex < 0)
                            {
                                chunkCount += chunkStartIndex;
                                chunkStartIndex = 0;
                            }

                            curDestIndex -= chunkCount;
                            if (chunkCount > 0)
                            {
                                char[] sourceArray = chunk.m_ChunkChars;
                                if ((uint)(chunkCount + curDestIndex) <= length && (uint)(chunkCount + chunkStartIndex) <= (uint)sourceArray.Length)
                                {
                                    fixed (char *sourcePtr = &sourceArray[chunkStartIndex])
                                        string.wstrcpy(destinationPtr + curDestIndex, sourcePtr, chunkCount);
                                }
                                else
                                {
                                    throw new ArgumentOutOfRangeException("chunkCount", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                                }
                            }
                        }

                        chunk = chunk.m_ChunkPrevious;
                    }
                }
            }

            return ret;
        }

        public StringBuilder Clear()
        {
            this.Length = 0;
            return this;
        }

        public int Length
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return m_ChunkOffset + m_ChunkLength;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
                }

                if (value > MaxCapacity)
                {
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                }

                Contract.EndContractBlock();
                int originalCapacity = Capacity;
                if (value == 0 && m_ChunkPrevious == null)
                {
                    m_ChunkLength = 0;
                    m_ChunkOffset = 0;
                    Contract.Assert(Capacity >= originalCapacity, "setting the Length should never decrease the Capacity");
                    return;
                }

                int delta = value - Length;
                if (delta > 0)
                {
                    Append('\0', delta);
                }
                else
                {
                    StringBuilder chunk = FindChunkForIndex(value);
                    if (chunk != this)
                    {
                        int newLen = originalCapacity - chunk.m_ChunkOffset;
                        char[] newArray = new char[newLen];
                        Contract.Assert(newLen > chunk.m_ChunkChars.Length, "the new chunk should be larger than the one it is replacing");
                        Array.Copy(chunk.m_ChunkChars, newArray, chunk.m_ChunkLength);
                        m_ChunkChars = newArray;
                        m_ChunkPrevious = chunk.m_ChunkPrevious;
                        m_ChunkOffset = chunk.m_ChunkOffset;
                    }

                    m_ChunkLength = value - chunk.m_ChunkOffset;
                    VerifyClassInvariant();
                }

                Contract.Assert(Capacity >= originalCapacity, "setting the Length should never decrease the Capacity");
            }
        }

        public char this[int index]
        {
            get
            {
                StringBuilder chunk = this;
                for (;;)
                {
                    int indexInBlock = index - chunk.m_ChunkOffset;
                    if (indexInBlock >= 0)
                    {
                        if (indexInBlock >= chunk.m_ChunkLength)
                            throw new IndexOutOfRangeException();
                        return chunk.m_ChunkChars[indexInBlock];
                    }

                    chunk = chunk.m_ChunkPrevious;
                    if (chunk == null)
                        throw new IndexOutOfRangeException();
                }
            }

            set
            {
                StringBuilder chunk = this;
                for (;;)
                {
                    int indexInBlock = index - chunk.m_ChunkOffset;
                    if (indexInBlock >= 0)
                    {
                        if (indexInBlock >= chunk.m_ChunkLength)
                            throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                        chunk.m_ChunkChars[indexInBlock] = value;
                        return;
                    }

                    chunk = chunk.m_ChunkPrevious;
                    if (chunk == null)
                        throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
        }

        public StringBuilder Append(char value, int repeatCount)
        {
            if (repeatCount < 0)
            {
                throw new ArgumentOutOfRangeException("repeatCount", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }

            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();
            if (repeatCount == 0)
            {
                return this;
            }

            int idx = m_ChunkLength;
            while (repeatCount > 0)
            {
                if (idx < m_ChunkChars.Length)
                {
                    m_ChunkChars[idx++] = value;
                    --repeatCount;
                }
                else
                {
                    m_ChunkLength = idx;
                    ExpandByABlock(repeatCount);
                    Contract.Assert(m_ChunkLength == 0, "Expand should create a new block");
                    idx = 0;
                }
            }

            m_ChunkLength = idx;
            VerifyClassInvariant();
            return this;
        }

        public StringBuilder Append(char[] value, int startIndex, int charCount)
        {
            if (startIndex < 0 && !(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && (charCount == 0)))
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            if (charCount < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && (charCount == 0))
            {
                return this;
            }

            if (value == null)
            {
                if (startIndex == 0 && charCount == 0)
                {
                    return this;
                }

                throw new ArgumentNullException("value");
            }

            if (charCount > value.Length - startIndex)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (charCount == 0)
            {
                return this;
            }

            unsafe
            {
                fixed (char *valueChars = &value[startIndex])
                    Append(valueChars, charCount);
            }

            return this;
        }

        public StringBuilder Append(String value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            if (value != null)
            {
                char[] chunkChars = m_ChunkChars;
                int chunkLength = m_ChunkLength;
                int valueLen = value.Length;
                int newCurrentIndex = chunkLength + valueLen;
                if (newCurrentIndex < chunkChars.Length)
                {
                    if (valueLen <= 2)
                    {
                        if (valueLen > 0)
                            chunkChars[chunkLength] = value[0];
                        if (valueLen > 1)
                            chunkChars[chunkLength + 1] = value[1];
                    }
                    else
                    {
                        unsafe
                        {
                            fixed (char *valuePtr = value)
                                fixed (char *destPtr = &chunkChars[chunkLength])
                                    string.wstrcpy(destPtr, valuePtr, valueLen);
                        }
                    }

                    m_ChunkLength = newCurrentIndex;
                }
                else
                    AppendHelper(value);
            }

            return this;
        }

        private void AppendHelper(string value)
        {
            unsafe
            {
                fixed (char *valueChars = value)
                    Append(valueChars, value.Length);
            }
        }

        internal unsafe extern void ReplaceBufferInternal(char *newBuffer, int newLength);
        internal unsafe extern void ReplaceBufferAnsiInternal(sbyte *newBuffer, int newLength);
        public StringBuilder Append(String value, int startIndex, int count)
        {
            if (startIndex < 0 && !(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && (count == 0)))
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && (count == 0))
            {
                return this;
            }

            if (value == null)
            {
                if (startIndex == 0 && count == 0)
                {
                    return this;
                }

                throw new ArgumentNullException("value");
            }

            if (count == 0)
            {
                return this;
            }

            if (startIndex > value.Length - count)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            unsafe
            {
                fixed (char *valueChars = value)
                    Append(valueChars + startIndex, count);
            }

            return this;
        }

        public StringBuilder AppendLine()
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(Environment.NewLine);
        }

        public StringBuilder AppendLine(string value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Append(value);
            return Append(Environment.NewLine);
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("Arg_NegativeArgCount"));
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_MustBeNonNegNum", "destinationIndex"));
            }

            if (destinationIndex > destination.Length - count)
            {
                throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_OffsetOut"));
            }

            if ((uint)sourceIndex > (uint)Length)
            {
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (sourceIndex > Length - count)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_LongerThanSrcString"));
            }

            Contract.EndContractBlock();
            VerifyClassInvariant();
            StringBuilder chunk = this;
            int sourceEndIndex = sourceIndex + count;
            int curDestIndex = destinationIndex + count;
            while (count > 0)
            {
                int chunkEndIndex = sourceEndIndex - chunk.m_ChunkOffset;
                if (chunkEndIndex >= 0)
                {
                    if (chunkEndIndex > chunk.m_ChunkLength)
                        chunkEndIndex = chunk.m_ChunkLength;
                    int chunkCount = count;
                    int chunkStartIndex = chunkEndIndex - count;
                    if (chunkStartIndex < 0)
                    {
                        chunkCount += chunkStartIndex;
                        chunkStartIndex = 0;
                    }

                    curDestIndex -= chunkCount;
                    count -= chunkCount;
                    ThreadSafeCopy(chunk.m_ChunkChars, chunkStartIndex, destination, curDestIndex, chunkCount);
                }

                chunk = chunk.m_ChunkPrevious;
            }
        }

        public StringBuilder Insert(int index, String value, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();
            int currentLength = Length;
            if ((uint)index > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (value == null || value.Length == 0 || count == 0)
            {
                return this;
            }

            long insertingChars = (long)value.Length * count;
            if (insertingChars > MaxCapacity - this.Length)
            {
                throw new OutOfMemoryException();
            }

            Contract.Assert(insertingChars + this.Length < Int32.MaxValue);
            StringBuilder chunk;
            int indexInChunk;
            MakeRoom(index, (int)insertingChars, out chunk, out indexInChunk, false);
            unsafe
            {
                fixed (char *valuePtr = value)
                {
                    while (count > 0)
                    {
                        ReplaceInPlaceAtChunk(ref chunk, ref indexInChunk, valuePtr, value.Length);
                        --count;
                    }
                }
            }

            return this;
        }

        public StringBuilder Remove(int startIndex, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (length > Length - startIndex)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();
            if (Length == length && startIndex == 0)
            {
                Length = 0;
                return this;
            }

            if (length > 0)
            {
                StringBuilder chunk;
                int indexInChunk;
                Remove(startIndex, length, out chunk, out indexInChunk);
            }

            return this;
        }

        public StringBuilder Append(bool value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString());
        }

        public StringBuilder Append(sbyte value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(byte value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(char value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            if (m_ChunkLength < m_ChunkChars.Length)
                m_ChunkChars[m_ChunkLength++] = value;
            else
                Append(value, 1);
            return this;
        }

        public StringBuilder Append(short value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(int value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(long value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(float value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(double value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(decimal value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(ushort value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(uint value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(ulong value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Append(value.ToString(CultureInfo.CurrentCulture));
        }

        public StringBuilder Append(Object value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            if (null == value)
            {
                return this;
            }

            return Append(value.ToString());
        }

        public StringBuilder Append(char[] value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            if (null != value && value.Length > 0)
            {
                unsafe
                {
                    fixed (char *valueChars = &value[0])
                        Append(valueChars, value.Length);
                }
            }

            return this;
        }

        public StringBuilder Insert(int index, String value)
        {
            if ((uint)index > (uint)Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();
            if (value != null)
            {
                unsafe
                {
                    fixed (char *sourcePtr = value)
                        Insert(index, sourcePtr, value.Length);
                }
            }

            return this;
        }

        public StringBuilder Insert(int index, bool value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(), 1);
        }

        public StringBuilder Insert(int index, sbyte value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, byte value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, short value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, char value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            unsafe
            {
                Insert(index, &value, 1);
            }

            return this;
        }

        public StringBuilder Insert(int index, char[] value)
        {
            if ((uint)index > (uint)Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();
            if (value != null)
                Insert(index, value, 0, value.Length);
            return this;
        }

        public StringBuilder Insert(int index, char[] value, int startIndex, int charCount)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            int currentLength = Length;
            if ((uint)index > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (value == null)
            {
                if (startIndex == 0 && charCount == 0)
                {
                    return this;
                }

                throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_String"));
            }

            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (charCount < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            if (startIndex > value.Length - charCount)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (charCount > 0)
            {
                unsafe
                {
                    fixed (char *sourcePtr = &value[startIndex])
                        Insert(index, sourcePtr, charCount);
                }
            }

            return this;
        }

        public StringBuilder Insert(int index, int value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, long value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, float value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, double value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, decimal value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, ushort value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, uint value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, ulong value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Insert(index, value.ToString(CultureInfo.CurrentCulture), 1);
        }

        public StringBuilder Insert(int index, Object value)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            if (null == value)
            {
                return this;
            }

            return Insert(index, value.ToString(), 1);
        }

        public StringBuilder AppendFormat(String format, Object arg0)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(null, format, new ParamsArray(arg0));
        }

        public StringBuilder AppendFormat(String format, Object arg0, Object arg1)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(null, format, new ParamsArray(arg0, arg1));
        }

        public StringBuilder AppendFormat(String format, Object arg0, Object arg1, Object arg2)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));
        }

        public StringBuilder AppendFormat(String format, params Object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return AppendFormatHelper(null, format, new ParamsArray(args));
        }

        public StringBuilder AppendFormat(IFormatProvider provider, String format, Object arg0)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(provider, format, new ParamsArray(arg0));
        }

        public StringBuilder AppendFormat(IFormatProvider provider, String format, Object arg0, Object arg1)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1));
        }

        public StringBuilder AppendFormat(IFormatProvider provider, String format, Object arg0, Object arg1, Object arg2)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return AppendFormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));
        }

        public StringBuilder AppendFormat(IFormatProvider provider, String format, params Object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }

            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return AppendFormatHelper(provider, format, new ParamsArray(args));
        }

        private static void FormatError()
        {
            throw new FormatException(Environment.GetResourceString("Format_InvalidString"));
        }

        internal StringBuilder AppendFormatHelper(IFormatProvider provider, String format, ParamsArray args)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            Contract.EndContractBlock();
            int pos = 0;
            int len = format.Length;
            char ch = '\x0';
            ICustomFormatter cf = null;
            if (provider != null)
            {
                cf = (ICustomFormatter)provider.GetFormat(typeof (ICustomFormatter));
            }

            while (true)
            {
                while (pos < len)
                {
                    ch = format[pos];
                    pos++;
                    if (ch == '}')
                    {
                        if (pos < len && format[pos] == '}')
                            pos++;
                        else
                            FormatError();
                    }

                    if (ch == '{')
                    {
                        if (pos < len && format[pos] == '{')
                            pos++;
                        else
                        {
                            pos--;
                            break;
                        }
                    }

                    Append(ch);
                }

                if (pos == len)
                    break;
                pos++;
                if (pos == len || (ch = format[pos]) < '0' || ch > '9')
                    FormatError();
                int index = 0;
                do
                {
                    index = index * 10 + ch - '0';
                    pos++;
                    if (pos == len)
                        FormatError();
                    ch = format[pos];
                }
                while (ch >= '0' && ch <= '9' && index < 1000000);
                if (index >= args.Length)
                    throw new FormatException(Environment.GetResourceString("Format_IndexOutOfRange"));
                while (pos < len && (ch = format[pos]) == ' ')
                    pos++;
                bool leftJustify = false;
                int width = 0;
                if (ch == ',')
                {
                    pos++;
                    while (pos < len && format[pos] == ' ')
                        pos++;
                    if (pos == len)
                        FormatError();
                    ch = format[pos];
                    if (ch == '-')
                    {
                        leftJustify = true;
                        pos++;
                        if (pos == len)
                            FormatError();
                        ch = format[pos];
                    }

                    if (ch < '0' || ch > '9')
                        FormatError();
                    do
                    {
                        width = width * 10 + ch - '0';
                        pos++;
                        if (pos == len)
                            FormatError();
                        ch = format[pos];
                    }
                    while (ch >= '0' && ch <= '9' && width < 1000000);
                }

                while (pos < len && (ch = format[pos]) == ' ')
                    pos++;
                Object arg = args[index];
                StringBuilder fmt = null;
                if (ch == ':')
                {
                    pos++;
                    while (true)
                    {
                        if (pos == len)
                            FormatError();
                        ch = format[pos];
                        pos++;
                        if (ch == '{')
                        {
                            if (pos < len && format[pos] == '{')
                                pos++;
                            else
                                FormatError();
                        }
                        else if (ch == '}')
                        {
                            if (pos < len && format[pos] == '}')
                                pos++;
                            else
                            {
                                pos--;
                                break;
                            }
                        }

                        if (fmt == null)
                        {
                            fmt = new StringBuilder();
                        }

                        fmt.Append(ch);
                    }
                }

                if (ch != '}')
                    FormatError();
                pos++;
                String sFmt = null;
                String s = null;
                if (cf != null)
                {
                    if (fmt != null)
                    {
                        sFmt = fmt.ToString();
                    }

                    s = cf.Format(sFmt, arg, provider);
                }

                if (s == null)
                {
                    IFormattable formattableArg = arg as IFormattable;
                    if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    {
                        if (arg is TimeSpan)
                        {
                            formattableArg = null;
                        }
                    }

                    if (formattableArg != null)
                    {
                        if (sFmt == null && fmt != null)
                        {
                            sFmt = fmt.ToString();
                        }

                        s = formattableArg.ToString(sFmt, provider);
                    }
                    else if (arg != null)
                    {
                        s = arg.ToString();
                    }
                }

                if (s == null)
                    s = String.Empty;
                int pad = width - s.Length;
                if (!leftJustify && pad > 0)
                    Append(' ', pad);
                Append(s);
                if (leftJustify && pad > 0)
                    Append(' ', pad);
            }

            return this;
        }

        public StringBuilder Replace(String oldValue, String newValue)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            return Replace(oldValue, newValue, 0, Length);
        }

        public bool Equals(StringBuilder sb)
        {
            if (sb == null)
                return false;
            if (Capacity != sb.Capacity || MaxCapacity != sb.MaxCapacity || Length != sb.Length)
                return false;
            if (sb == this)
                return true;
            StringBuilder thisChunk = this;
            int thisChunkIndex = thisChunk.m_ChunkLength;
            StringBuilder sbChunk = sb;
            int sbChunkIndex = sbChunk.m_ChunkLength;
            for (;;)
            {
                --thisChunkIndex;
                --sbChunkIndex;
                while (thisChunkIndex < 0)
                {
                    thisChunk = thisChunk.m_ChunkPrevious;
                    if (thisChunk == null)
                        break;
                    thisChunkIndex = thisChunk.m_ChunkLength + thisChunkIndex;
                }

                while (sbChunkIndex < 0)
                {
                    sbChunk = sbChunk.m_ChunkPrevious;
                    if (sbChunk == null)
                        break;
                    sbChunkIndex = sbChunk.m_ChunkLength + sbChunkIndex;
                }

                if (thisChunkIndex < 0)
                    return sbChunkIndex < 0;
                if (sbChunkIndex < 0)
                    return false;
                if (thisChunk.m_ChunkChars[thisChunkIndex] != sbChunk.m_ChunkChars[sbChunkIndex])
                    return false;
            }
        }

        public StringBuilder Replace(String oldValue, String newValue, int startIndex, int count)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            int currentLength = Length;
            if ((uint)startIndex > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || startIndex > currentLength - count)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (oldValue == null)
            {
                throw new ArgumentNullException("oldValue");
            }

            if (oldValue.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "oldValue");
            }

            if (newValue == null)
                newValue = "";
            int deltaLength = newValue.Length - oldValue.Length;
            int[] replacements = null;
            int replacementsCount = 0;
            StringBuilder chunk = FindChunkForIndex(startIndex);
            int indexInChunk = startIndex - chunk.m_ChunkOffset;
            while (count > 0)
            {
                if (StartsWith(chunk, indexInChunk, count, oldValue))
                {
                    if (replacements == null)
                        replacements = new int[5];
                    else if (replacementsCount >= replacements.Length)
                    {
                        int[] newArray = new int[replacements.Length * 3 / 2 + 4];
                        Array.Copy(replacements, newArray, replacements.Length);
                        replacements = newArray;
                    }

                    replacements[replacementsCount++] = indexInChunk;
                    indexInChunk += oldValue.Length;
                    count -= oldValue.Length;
                }
                else
                {
                    indexInChunk++;
                    --count;
                }

                if (indexInChunk >= chunk.m_ChunkLength || count == 0)
                {
                    int index = indexInChunk + chunk.m_ChunkOffset;
                    int indexBeforeAdjustment = index;
                    ReplaceAllInChunk(replacements, replacementsCount, chunk, oldValue.Length, newValue);
                    index += ((newValue.Length - oldValue.Length) * replacementsCount);
                    replacementsCount = 0;
                    chunk = FindChunkForIndex(index);
                    indexInChunk = index - chunk.m_ChunkOffset;
                    Contract.Assert(chunk != null || count == 0, "Chunks ended prematurely");
                }
            }

            VerifyClassInvariant();
            return this;
        }

        public StringBuilder Replace(char oldChar, char newChar)
        {
            return Replace(oldChar, newChar, 0, Length);
        }

        public StringBuilder Replace(char oldChar, char newChar, int startIndex, int count)
        {
            Contract.Ensures(Contract.Result<StringBuilder>() != null);
            int currentLength = Length;
            if ((uint)startIndex > (uint)currentLength)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || startIndex > currentLength - count)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            int endIndex = startIndex + count;
            StringBuilder chunk = this;
            for (;;)
            {
                int endIndexInChunk = endIndex - chunk.m_ChunkOffset;
                int startIndexInChunk = startIndex - chunk.m_ChunkOffset;
                if (endIndexInChunk >= 0)
                {
                    int curInChunk = Math.Max(startIndexInChunk, 0);
                    int endInChunk = Math.Min(chunk.m_ChunkLength, endIndexInChunk);
                    while (curInChunk < endInChunk)
                    {
                        if (chunk.m_ChunkChars[curInChunk] == oldChar)
                            chunk.m_ChunkChars[curInChunk] = newChar;
                        curInChunk++;
                    }
                }

                if (startIndexInChunk >= 0)
                    break;
                chunk = chunk.m_ChunkPrevious;
            }

            return this;
        }

        public unsafe StringBuilder Append(char *value, int valueCount)
        {
            if (valueCount < 0)
            {
                throw new ArgumentOutOfRangeException("valueCount", Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }

            int newIndex = valueCount + m_ChunkLength;
            if (newIndex <= m_ChunkChars.Length)
            {
                ThreadSafeCopy(value, m_ChunkChars, m_ChunkLength, valueCount);
                m_ChunkLength = newIndex;
            }
            else
            {
                int firstLength = m_ChunkChars.Length - m_ChunkLength;
                if (firstLength > 0)
                {
                    ThreadSafeCopy(value, m_ChunkChars, m_ChunkLength, firstLength);
                    m_ChunkLength = m_ChunkChars.Length;
                }

                int restLength = valueCount - firstLength;
                ExpandByABlock(restLength);
                Contract.Assert(m_ChunkLength == 0, "Expand did not make a new block");
                ThreadSafeCopy(value + firstLength, m_ChunkChars, 0, restLength);
                m_ChunkLength = restLength;
            }

            VerifyClassInvariant();
            return this;
        }

        unsafe private void Insert(int index, char *value, int valueCount)
        {
            if ((uint)index > (uint)Length)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (valueCount > 0)
            {
                StringBuilder chunk;
                int indexInChunk;
                MakeRoom(index, valueCount, out chunk, out indexInChunk, false);
                ReplaceInPlaceAtChunk(ref chunk, ref indexInChunk, value, valueCount);
            }
        }

        private void ReplaceAllInChunk(int[] replacements, int replacementsCount, StringBuilder sourceChunk, int removeCount, string value)
        {
            if (replacementsCount <= 0)
                return;
            unsafe
            {
                fixed (char *valuePtr = value)
                {
                    int delta = (value.Length - removeCount) * replacementsCount;
                    StringBuilder targetChunk = sourceChunk;
                    int targetIndexInChunk = replacements[0];
                    if (delta > 0)
                        MakeRoom(targetChunk.m_ChunkOffset + targetIndexInChunk, delta, out targetChunk, out targetIndexInChunk, true);
                    int i = 0;
                    for (;;)
                    {
                        ReplaceInPlaceAtChunk(ref targetChunk, ref targetIndexInChunk, valuePtr, value.Length);
                        int gapStart = replacements[i] + removeCount;
                        i++;
                        if (i >= replacementsCount)
                            break;
                        int gapEnd = replacements[i];
                        Contract.Assert(gapStart < sourceChunk.m_ChunkChars.Length, "gap starts at end of buffer.  Should not happen");
                        Contract.Assert(gapStart <= gapEnd, "negative gap size");
                        Contract.Assert(gapEnd <= sourceChunk.m_ChunkLength, "gap too big");
                        if (delta != 0)
                        {
                            fixed (char *sourcePtr = &sourceChunk.m_ChunkChars[gapStart])
                                ReplaceInPlaceAtChunk(ref targetChunk, ref targetIndexInChunk, sourcePtr, gapEnd - gapStart);
                        }
                        else
                        {
                            targetIndexInChunk += gapEnd - gapStart;
                            Contract.Assert(targetIndexInChunk <= targetChunk.m_ChunkLength, "gap not in chunk");
                        }
                    }

                    if (delta < 0)
                        Remove(targetChunk.m_ChunkOffset + targetIndexInChunk, -delta, out targetChunk, out targetIndexInChunk);
                }
            }
        }

        private bool StartsWith(StringBuilder chunk, int indexInChunk, int count, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (count == 0)
                    return false;
                if (indexInChunk >= chunk.m_ChunkLength)
                {
                    chunk = Next(chunk);
                    if (chunk == null)
                        return false;
                    indexInChunk = 0;
                }

                if (value[i] != chunk.m_ChunkChars[indexInChunk])
                    return false;
                indexInChunk++;
                --count;
            }

            return true;
        }

        unsafe private void ReplaceInPlaceAtChunk(ref StringBuilder chunk, ref int indexInChunk, char *value, int count)
        {
            if (count != 0)
            {
                for (;;)
                {
                    int lengthInChunk = chunk.m_ChunkLength - indexInChunk;
                    Contract.Assert(lengthInChunk >= 0, "index not in chunk");
                    int lengthToCopy = Math.Min(lengthInChunk, count);
                    ThreadSafeCopy(value, chunk.m_ChunkChars, indexInChunk, lengthToCopy);
                    indexInChunk += lengthToCopy;
                    if (indexInChunk >= chunk.m_ChunkLength)
                    {
                        chunk = Next(chunk);
                        indexInChunk = 0;
                    }

                    count -= lengthToCopy;
                    if (count == 0)
                        break;
                    value += lengthToCopy;
                }
            }
        }

        unsafe private static void ThreadSafeCopy(char *sourcePtr, char[] destination, int destinationIndex, int count)
        {
            if (count > 0)
            {
                if ((uint)destinationIndex <= (uint)destination.Length && (destinationIndex + count) <= destination.Length)
                {
                    fixed (char *destinationPtr = &destination[destinationIndex])
                        string.wstrcpy(destinationPtr, sourcePtr, count);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
        }

        private static void ThreadSafeCopy(char[] source, int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (count > 0)
            {
                if ((uint)sourceIndex <= (uint)source.Length && (sourceIndex + count) <= source.Length)
                {
                    unsafe
                    {
                        fixed (char *sourcePtr = &source[sourceIndex])
                            ThreadSafeCopy(sourcePtr, destination, destinationIndex, count);
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
        }

        internal unsafe void InternalCopy(IntPtr dest, int len)
        {
            if (len == 0)
                return;
            bool isLastChunk = true;
            byte *dstPtr = (byte *)dest.ToPointer();
            StringBuilder currentSrc = FindChunkForByte(len);
            do
            {
                int chunkOffsetInBytes = currentSrc.m_ChunkOffset * sizeof (char);
                int chunkLengthInBytes = currentSrc.m_ChunkLength * sizeof (char);
                fixed (char *charPtr = &currentSrc.m_ChunkChars[0])
                {
                    byte *srcPtr = (byte *)charPtr;
                    if (isLastChunk)
                    {
                        isLastChunk = false;
                        Buffer.Memcpy(dstPtr + chunkOffsetInBytes, srcPtr, len - chunkOffsetInBytes);
                    }
                    else
                    {
                        Buffer.Memcpy(dstPtr + chunkOffsetInBytes, srcPtr, chunkLengthInBytes);
                    }
                }

                currentSrc = currentSrc.m_ChunkPrevious;
            }
            while (currentSrc != null);
        }

        private StringBuilder FindChunkForIndex(int index)
        {
            Contract.Assert(0 <= index && index <= Length, "index not in string");
            StringBuilder ret = this;
            while (ret.m_ChunkOffset > index)
                ret = ret.m_ChunkPrevious;
            Contract.Assert(ret != null, "index not in string");
            return ret;
        }

        private StringBuilder FindChunkForByte(int byteIndex)
        {
            Contract.Assert(0 <= byteIndex && byteIndex <= Length * sizeof (char), "Byte Index not in string");
            StringBuilder ret = this;
            while (ret.m_ChunkOffset * sizeof (char) > byteIndex)
                ret = ret.m_ChunkPrevious;
            Contract.Assert(ret != null, "Byte Index not in string");
            return ret;
        }

        private StringBuilder Next(StringBuilder chunk)
        {
            if (chunk == this)
                return null;
            return FindChunkForIndex(chunk.m_ChunkOffset + chunk.m_ChunkLength);
        }

        private void ExpandByABlock(int minBlockCharCount)
        {
            Contract.Requires(Capacity == Length, "Expand expect to be called only when there is no space left");
            Contract.Requires(minBlockCharCount > 0, "Expansion request must be positive");
            VerifyClassInvariant();
            if ((minBlockCharCount + Length) > m_MaxCapacity)
                throw new ArgumentOutOfRangeException("requiredLength", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
            int newBlockLength = Math.Max(minBlockCharCount, Math.Min(Length, MaxChunkSize));
            m_ChunkPrevious = new StringBuilder(this);
            m_ChunkOffset += m_ChunkLength;
            m_ChunkLength = 0;
            if (m_ChunkOffset + newBlockLength < newBlockLength)
            {
                m_ChunkChars = null;
                throw new OutOfMemoryException();
            }

            m_ChunkChars = new char[newBlockLength];
            VerifyClassInvariant();
        }

        private StringBuilder(StringBuilder from)
        {
            m_ChunkLength = from.m_ChunkLength;
            m_ChunkOffset = from.m_ChunkOffset;
            m_ChunkChars = from.m_ChunkChars;
            m_ChunkPrevious = from.m_ChunkPrevious;
            m_MaxCapacity = from.m_MaxCapacity;
            VerifyClassInvariant();
        }

        private void MakeRoom(int index, int count, out StringBuilder chunk, out int indexInChunk, bool doneMoveFollowingChars)
        {
            VerifyClassInvariant();
            Contract.Assert(count > 0, "Count must be strictly positive");
            Contract.Assert(index >= 0, "Index can't be negative");
            if (count + Length > m_MaxCapacity)
                throw new ArgumentOutOfRangeException("requiredLength", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
            chunk = this;
            while (chunk.m_ChunkOffset > index)
            {
                chunk.m_ChunkOffset += count;
                chunk = chunk.m_ChunkPrevious;
            }

            indexInChunk = index - chunk.m_ChunkOffset;
            if (!doneMoveFollowingChars && chunk.m_ChunkLength <= DefaultCapacity * 2 && chunk.m_ChunkChars.Length - chunk.m_ChunkLength >= count)
            {
                for (int i = chunk.m_ChunkLength; i > indexInChunk;)
                {
                    --i;
                    chunk.m_ChunkChars[i + count] = chunk.m_ChunkChars[i];
                }

                chunk.m_ChunkLength += count;
                return;
            }

            StringBuilder newChunk = new StringBuilder(Math.Max(count, DefaultCapacity), chunk.m_MaxCapacity, chunk.m_ChunkPrevious);
            newChunk.m_ChunkLength = count;
            int copyCount1 = Math.Min(count, indexInChunk);
            if (copyCount1 > 0)
            {
                unsafe
                {
                    fixed (char *chunkCharsPtr = chunk.m_ChunkChars)
                    {
                        ThreadSafeCopy(chunkCharsPtr, newChunk.m_ChunkChars, 0, copyCount1);
                        int copyCount2 = indexInChunk - copyCount1;
                        if (copyCount2 >= 0)
                        {
                            ThreadSafeCopy(chunkCharsPtr + copyCount1, chunk.m_ChunkChars, 0, copyCount2);
                            indexInChunk = copyCount2;
                        }
                    }
                }
            }

            chunk.m_ChunkPrevious = newChunk;
            chunk.m_ChunkOffset += count;
            if (copyCount1 < count)
            {
                chunk = newChunk;
                indexInChunk = copyCount1;
            }

            VerifyClassInvariant();
        }

        private StringBuilder(int size, int maxCapacity, StringBuilder previousBlock)
        {
            Contract.Assert(size > 0, "size not positive");
            Contract.Assert(maxCapacity > 0, "maxCapacity not positive");
            m_ChunkChars = new char[size];
            m_MaxCapacity = maxCapacity;
            m_ChunkPrevious = previousBlock;
            if (previousBlock != null)
                m_ChunkOffset = previousBlock.m_ChunkOffset + previousBlock.m_ChunkLength;
            VerifyClassInvariant();
        }

        private void Remove(int startIndex, int count, out StringBuilder chunk, out int indexInChunk)
        {
            VerifyClassInvariant();
            Contract.Assert(startIndex >= 0 && startIndex < Length, "startIndex not in string");
            int endIndex = startIndex + count;
            chunk = this;
            StringBuilder endChunk = null;
            int endIndexInChunk = 0;
            for (;;)
            {
                if (endIndex - chunk.m_ChunkOffset >= 0)
                {
                    if (endChunk == null)
                    {
                        endChunk = chunk;
                        endIndexInChunk = endIndex - endChunk.m_ChunkOffset;
                    }

                    if (startIndex - chunk.m_ChunkOffset >= 0)
                    {
                        indexInChunk = startIndex - chunk.m_ChunkOffset;
                        break;
                    }
                }
                else
                {
                    chunk.m_ChunkOffset -= count;
                }

                chunk = chunk.m_ChunkPrevious;
            }

            Contract.Assert(chunk != null, "fell off beginning of string!");
            int copyTargetIndexInChunk = indexInChunk;
            int copyCount = endChunk.m_ChunkLength - endIndexInChunk;
            if (endChunk != chunk)
            {
                copyTargetIndexInChunk = 0;
                chunk.m_ChunkLength = indexInChunk;
                endChunk.m_ChunkPrevious = chunk;
                endChunk.m_ChunkOffset = chunk.m_ChunkOffset + chunk.m_ChunkLength;
                if (indexInChunk == 0)
                {
                    endChunk.m_ChunkPrevious = chunk.m_ChunkPrevious;
                    chunk = endChunk;
                }
            }

            endChunk.m_ChunkLength -= (endIndexInChunk - copyTargetIndexInChunk);
            if (copyTargetIndexInChunk != endIndexInChunk)
                ThreadSafeCopy(endChunk.m_ChunkChars, endIndexInChunk, endChunk.m_ChunkChars, copyTargetIndexInChunk, copyCount);
            Contract.Assert(chunk != null, "fell off beginning of string!");
            VerifyClassInvariant();
        }
    }
}