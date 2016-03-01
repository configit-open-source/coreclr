using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO
{
    public class UnmanagedMemoryAccessor : IDisposable
    {
        private SafeBuffer _buffer;
        private Int64 _offset;
        private Int64 _capacity;
        private FileAccess _access;
        private bool _isOpen;
        private bool _canRead;
        private bool _canWrite;
        protected UnmanagedMemoryAccessor()
        {
            _isOpen = false;
        }

        public UnmanagedMemoryAccessor(SafeBuffer buffer, Int64 offset, Int64 capacity)
        {
            Initialize(buffer, offset, capacity, FileAccess.Read);
        }

        public UnmanagedMemoryAccessor(SafeBuffer buffer, Int64 offset, Int64 capacity, FileAccess access)
        {
            Initialize(buffer, offset, capacity, access);
        }

        protected void Initialize(SafeBuffer buffer, Int64 offset, Int64 capacity, FileAccess access)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (buffer.ByteLength < (UInt64)(offset + capacity))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndCapacityOutOfBounds"));
            }

            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException("access");
            }

            Contract.EndContractBlock();
            if (_isOpen)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CalledTwice"));
            }

            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    buffer.AcquirePointer(ref pointer);
                    if (((byte *)((Int64)pointer + offset + capacity)) < pointer)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_UnmanagedMemAccessorWrapAround"));
                    }
                }
                finally
                {
                    if (pointer != null)
                    {
                        buffer.ReleasePointer();
                    }
                }
            }

            _offset = offset;
            _buffer = buffer;
            _capacity = capacity;
            _access = access;
            _isOpen = true;
            _canRead = (_access & FileAccess.Read) != 0;
            _canWrite = (_access & FileAccess.Write) != 0;
        }

        public Int64 Capacity
        {
            get
            {
                return _capacity;
            }
        }

        public bool CanRead
        {
            get
            {
                return _isOpen && _canRead;
            }
        }

        public bool CanWrite
        {
            get
            {
                return _isOpen && _canWrite;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _isOpen = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsOpen
        {
            get
            {
                return _isOpen;
            }
        }

        public bool ReadBoolean(Int64 position)
        {
            int sizeOfType = sizeof (bool);
            EnsureSafeToRead(position, sizeOfType);
            byte b = InternalReadByte(position);
            return b != 0;
        }

        public byte ReadByte(Int64 position)
        {
            int sizeOfType = sizeof (byte);
            EnsureSafeToRead(position, sizeOfType);
            return InternalReadByte(position);
        }

        public char ReadChar(Int64 position)
        {
            int sizeOfType = sizeof (char);
            EnsureSafeToRead(position, sizeOfType);
            char result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((char *)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public Int16 ReadInt16(Int64 position)
        {
            int sizeOfType = sizeof (Int16);
            EnsureSafeToRead(position, sizeOfType);
            Int16 result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((Int16*)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public Int32 ReadInt32(Int64 position)
        {
            int sizeOfType = sizeof (Int32);
            EnsureSafeToRead(position, sizeOfType);
            Int32 result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((Int32*)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public Int64 ReadInt64(Int64 position)
        {
            int sizeOfType = sizeof (Int64);
            EnsureSafeToRead(position, sizeOfType);
            Int64 result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((Int64*)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public Decimal ReadDecimal(Int64 position)
        {
            int sizeOfType = sizeof (Decimal);
            EnsureSafeToRead(position, sizeOfType);
            int[] decimalArray = new int[4];
            ReadArray<int>(position, decimalArray, 0, decimalArray.Length);
            return new Decimal(decimalArray);
        }

        public Single ReadSingle(Int64 position)
        {
            int sizeOfType = sizeof (Single);
            EnsureSafeToRead(position, sizeOfType);
            Single result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((Single*)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public Double ReadDouble(Int64 position)
        {
            int sizeOfType = sizeof (Double);
            EnsureSafeToRead(position, sizeOfType);
            Double result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((Double*)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public SByte ReadSByte(Int64 position)
        {
            int sizeOfType = sizeof (SByte);
            EnsureSafeToRead(position, sizeOfType);
            SByte result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((SByte*)pointer);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public UInt16 ReadUInt16(Int64 position)
        {
            int sizeOfType = sizeof (UInt16);
            EnsureSafeToRead(position, sizeOfType);
            UInt16 result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((UInt16*)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public UInt32 ReadUInt32(Int64 position)
        {
            int sizeOfType = sizeof (UInt32);
            EnsureSafeToRead(position, sizeOfType);
            UInt32 result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((UInt32*)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public UInt64 ReadUInt64(Int64 position)
        {
            int sizeOfType = sizeof (UInt64);
            EnsureSafeToRead(position, sizeOfType);
            UInt64 result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    result = *((UInt64*)(pointer));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        public void Read<T>(Int64 position, out T structure)where T : struct
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.EndContractBlock();
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }

            if (!CanRead)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
            }

            UInt32 sizeOfT = Marshal.SizeOfType(typeof (T));
            if (position > _capacity - sizeOfT)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToRead", typeof (T).FullName), "position");
                }
            }

            structure = _buffer.Read<T>((UInt64)(_offset + position));
        }

        public int ReadArray<T>(Int64 position, T[] array, Int32 offset, Int32 count)where T : struct
        {
            if (array == null)
            {
                throw new ArgumentNullException("array", "Buffer cannot be null.");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (array.Length - offset < count)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndLengthOutOfBounds"));
            }

            Contract.EndContractBlock();
            if (!CanRead)
            {
                if (!_isOpen)
                {
                    throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
                }
                else
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
                }
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            UInt32 sizeOfT = Marshal.AlignedSizeOf<T>();
            if (position >= _capacity)
            {
                throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
            }

            int n = count;
            long spaceLeft = _capacity - position;
            if (spaceLeft < 0)
            {
                n = 0;
            }
            else
            {
                ulong spaceNeeded = (ulong)(sizeOfT * count);
                if ((ulong)spaceLeft < spaceNeeded)
                {
                    n = (int)(spaceLeft / sizeOfT);
                }
            }

            _buffer.ReadArray<T>((UInt64)(_offset + position), array, offset, n);
            return n;
        }

        public void Write(Int64 position, bool value)
        {
            int sizeOfType = sizeof (bool);
            EnsureSafeToWrite(position, sizeOfType);
            byte b = (byte)(value ? 1 : 0);
            InternalWrite(position, b);
        }

        public void Write(Int64 position, byte value)
        {
            int sizeOfType = sizeof (byte);
            EnsureSafeToWrite(position, sizeOfType);
            InternalWrite(position, value);
        }

        public void Write(Int64 position, char value)
        {
            int sizeOfType = sizeof (char);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((char *)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, Int16 value)
        {
            int sizeOfType = sizeof (Int16);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((Int16*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, Int32 value)
        {
            int sizeOfType = sizeof (Int32);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((Int32*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, Int64 value)
        {
            int sizeOfType = sizeof (Int64);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((Int64*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, Decimal value)
        {
            int sizeOfType = sizeof (Decimal);
            EnsureSafeToWrite(position, sizeOfType);
            byte[] decimalArray = new byte[16];
            Decimal.GetBytes(value, decimalArray);
            int[] bits = new int[4];
            int flags = ((int)decimalArray[12]) | ((int)decimalArray[13] << 8) | ((int)decimalArray[14] << 16) | ((int)decimalArray[15] << 24);
            int lo = ((int)decimalArray[0]) | ((int)decimalArray[1] << 8) | ((int)decimalArray[2] << 16) | ((int)decimalArray[3] << 24);
            int mid = ((int)decimalArray[4]) | ((int)decimalArray[5] << 8) | ((int)decimalArray[6] << 16) | ((int)decimalArray[7] << 24);
            int hi = ((int)decimalArray[8]) | ((int)decimalArray[9] << 8) | ((int)decimalArray[10] << 16) | ((int)decimalArray[11] << 24);
            bits[0] = lo;
            bits[1] = mid;
            bits[2] = hi;
            bits[3] = flags;
            WriteArray<int>(position, bits, 0, bits.Length);
        }

        public void Write(Int64 position, Single value)
        {
            int sizeOfType = sizeof (Single);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((Single*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, Double value)
        {
            int sizeOfType = sizeof (Double);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((Double*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, SByte value)
        {
            int sizeOfType = sizeof (SByte);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((SByte*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, UInt16 value)
        {
            int sizeOfType = sizeof (UInt16);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((UInt16*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, UInt32 value)
        {
            int sizeOfType = sizeof (UInt32);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((UInt32*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(Int64 position, UInt64 value)
        {
            int sizeOfType = sizeof (UInt64);
            EnsureSafeToWrite(position, sizeOfType);
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((UInt64*)pointer) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write<T>(Int64 position, ref T structure)where T : struct
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.EndContractBlock();
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }

            if (!CanWrite)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
            }

            UInt32 sizeOfT = Marshal.SizeOfType(typeof (T));
            if (position > _capacity - sizeOfT)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToWrite", typeof (T).FullName), "position");
                }
            }

            _buffer.Write<T>((UInt64)(_offset + position), structure);
        }

        public void WriteArray<T>(Int64 position, T[] array, Int32 offset, Int32 count)where T : struct
        {
            if (array == null)
            {
                throw new ArgumentNullException("array", "Buffer cannot be null.");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (array.Length - offset < count)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndLengthOutOfBounds"));
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (position >= Capacity)
            {
                throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
            }

            Contract.EndContractBlock();
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }

            if (!CanWrite)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
            }

            _buffer.WriteArray<T>((UInt64)(_offset + position), array, offset, count);
        }

        private byte InternalReadByte(Int64 position)
        {
            Contract.Assert(CanRead, "UMA not readable");
            Contract.Assert(position >= 0, "position less than 0");
            Contract.Assert(position <= _capacity - sizeof (byte), "position is greater than capacity - sizeof(byte)");
            byte result;
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = *((byte *)(pointer + _offset + position));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            return result;
        }

        private void InternalWrite(Int64 position, byte value)
        {
            Contract.Assert(CanWrite, "UMA not writeable");
            Contract.Assert(position >= 0, "position less than 0");
            Contract.Assert(position <= _capacity - sizeof (byte), "position is greater than capacity - sizeof(byte)");
            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    *((byte *)(pointer + _offset + position)) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        private void EnsureSafeToRead(Int64 position, int sizeOfType)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }

            if (!CanRead)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.EndContractBlock();
            if (position > _capacity - sizeOfType)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToRead"), "position");
                }
            }
        }

        private void EnsureSafeToWrite(Int64 position, int sizeOfType)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }

            if (!CanWrite)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.EndContractBlock();
            if (position > _capacity - sizeOfType)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException("position", Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToWrite", "Byte"), "position");
                }
            }
        }
    }
}