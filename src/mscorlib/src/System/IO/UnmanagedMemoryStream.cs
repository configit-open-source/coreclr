
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public class UnmanagedMemoryStream : Stream
    {
        private const long UnmanagedMemStreamMaxLength = Int64.MaxValue;
        private SafeBuffer _buffer;
        private unsafe byte *_mem;
        private long _length;
        private long _capacity;
        private long _position;
        private long _offset;
        private FileAccess _access;
        internal bool _isOpen;
        private Task<Int32> _lastReadTask;
        protected UnmanagedMemoryStream()
        {
            unsafe
            {
                _mem = null;
            }

            _isOpen = false;
        }

        public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length)
        {
            Initialize(buffer, offset, length, FileAccess.Read, false);
        }

        public UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length, FileAccess access)
        {
            Initialize(buffer, offset, length, access, false);
        }

        internal UnmanagedMemoryStream(SafeBuffer buffer, long offset, long length, FileAccess access, bool skipSecurityCheck)
        {
            Initialize(buffer, offset, length, access, skipSecurityCheck);
        }

        protected void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access)
        {
            Initialize(buffer, offset, length, access, false);
        }

        internal void Initialize(SafeBuffer buffer, long offset, long length, FileAccess access, bool skipSecurityCheck)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (buffer.ByteLength < (ulong)(offset + length))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSafeBufferOffLen"));
            }

            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException("access");
            }

                        if (_isOpen)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CalledTwice"));
            }

            if (!skipSecurityCheck)
            {
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            }

            unsafe
            {
                byte *pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    buffer.AcquirePointer(ref pointer);
                    if ((pointer + offset + length) < pointer)
                    {
                        throw new ArgumentException(Environment.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround"));
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
            _length = length;
            _capacity = length;
            _access = access;
            _isOpen = true;
        }

        public unsafe UnmanagedMemoryStream(byte *pointer, long length)
        {
            Initialize(pointer, length, length, FileAccess.Read, false);
        }

        public unsafe UnmanagedMemoryStream(byte *pointer, long length, long capacity, FileAccess access)
        {
            Initialize(pointer, length, capacity, access, false);
        }

        internal unsafe UnmanagedMemoryStream(byte *pointer, long length, long capacity, FileAccess access, bool skipSecurityCheck)
        {
            Initialize(pointer, length, capacity, access, skipSecurityCheck);
        }

        protected unsafe void Initialize(byte *pointer, long length, long capacity, FileAccess access)
        {
            Initialize(pointer, length, capacity, access, false);
        }

        internal unsafe void Initialize(byte *pointer, long length, long capacity, FileAccess access, bool skipSecurityCheck)
        {
            if (pointer == null)
                throw new ArgumentNullException("pointer");
            if (length < 0 || capacity < 0)
                throw new ArgumentOutOfRangeException((length < 0) ? "length" : "capacity", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (length > capacity)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_LengthGreaterThanCapacity"));
                        if (((byte *)((long)pointer + capacity)) < pointer)
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamWrapAround"));
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException("access", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            if (_isOpen)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CalledTwice"));
            if (!skipSecurityCheck)
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            _mem = pointer;
            _offset = 0;
            _length = length;
            _capacity = capacity;
            _access = access;
            _isOpen = true;
        }

        public override bool CanRead
        {
            
            get
            {
                return _isOpen && (_access & FileAccess.Read) != 0;
            }
        }

        public override bool CanSeek
        {
            
            get
            {
                return _isOpen;
            }
        }

        public override bool CanWrite
        {
            
            get
            {
                return _isOpen && (_access & FileAccess.Write) != 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _isOpen = false;
            unsafe
            {
                _mem = null;
            }

            base.Dispose(disposing);
        }

        public override void Flush()
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
            try
            {
                Flush();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public override long Length
        {
            get
            {
                if (!_isOpen)
                    __Error.StreamIsClosed();
                return Interlocked.Read(ref _length);
            }
        }

        public long Capacity
        {
            get
            {
                if (!_isOpen)
                    __Error.StreamIsClosed();
                return _capacity;
            }
        }

        public override long Position
        {
            get
            {
                if (!CanSeek)
                    __Error.StreamIsClosed();
                                return Interlocked.Read(ref _position);
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                                if (!CanSeek)
                    __Error.StreamIsClosed();
                Interlocked.Exchange(ref _position, value);
            }
        }

        public unsafe byte *PositionPointer
        {
            [System.Security.SecurityCritical]
            get
            {
                if (_buffer != null)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_UmsSafeBuffer"));
                }

                long pos = Interlocked.Read(ref _position);
                if (pos > _capacity)
                    throw new IndexOutOfRangeException(Environment.GetResourceString("IndexOutOfRange_UMSPosition"));
                byte *ptr = _mem + pos;
                if (!_isOpen)
                    __Error.StreamIsClosed();
                return ptr;
            }

            [System.Security.SecurityCritical]
            set
            {
                if (_buffer != null)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_UmsSafeBuffer"));
                if (!_isOpen)
                    __Error.StreamIsClosed();
                if (new IntPtr(value - _mem).ToInt64() > UnmanagedMemStreamMaxLength)
                    throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength"));
                if (value < _mem)
                    throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
                Interlocked.Exchange(ref _position, value - _mem);
            }
        }

        internal unsafe byte *Pointer
        {
            [System.Security.SecurityCritical]
            get
            {
                if (_buffer != null)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_UmsSafeBuffer"));
                return _mem;
            }
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (!_isOpen)
                __Error.StreamIsClosed();
            if (!CanRead)
                __Error.ReadNotSupported();
            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            long n = len - pos;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;
            int nInt = (int)n;
            if (nInt < 0)
                nInt = 0;
                        if (_buffer != null)
            {
                unsafe
                {
                    byte *pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        Buffer.Memcpy(buffer, offset, pointer + pos + _offset, 0, nInt);
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
            else
            {
                unsafe
                {
                    Buffer.Memcpy(buffer, offset, _mem + pos, 0, nInt);
                }
            }

            Interlocked.Exchange(ref _position, pos + n);
            return nInt;
        }

        public override Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<Int32>(cancellationToken);
            try
            {
                Int32 n = Read(buffer, offset, count);
                Task<Int32> t = _lastReadTask;
                return (t != null && t.Result == n) ? t : (_lastReadTask = Task.FromResult<Int32>(n));
            }
            catch (Exception ex)
            {
                                return Task.FromException<Int32>(ex);
            }
        }

        public override int ReadByte()
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            if (!CanRead)
                __Error.ReadNotSupported();
            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            if (pos >= len)
                return -1;
            Interlocked.Exchange(ref _position, pos + 1);
            int result;
            if (_buffer != null)
            {
                unsafe
                {
                    byte *pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        result = *(pointer + pos + _offset);
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
            else
            {
                unsafe
                {
                    result = _mem[pos];
                }
            }

            return result;
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            if (offset > UnmanagedMemStreamMaxLength)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_UnmanagedMemStreamLength"));
            switch (loc)
            {
                case SeekOrigin.Begin:
                    if (offset < 0)
                        throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
                    Interlocked.Exchange(ref _position, offset);
                    break;
                case SeekOrigin.Current:
                    long pos = Interlocked.Read(ref _position);
                    if (offset + pos < 0)
                        throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
                    Interlocked.Exchange(ref _position, offset + pos);
                    break;
                case SeekOrigin.End:
                    long len = Interlocked.Read(ref _length);
                    if (len + offset < 0)
                        throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
                    Interlocked.Exchange(ref _position, len + offset);
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSeekOrigin"));
            }

            long finalPos = Interlocked.Read(ref _position);
                        return finalPos;
        }

        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        if (_buffer != null)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UmsSafeBuffer"));
            if (!_isOpen)
                __Error.StreamIsClosed();
            if (!CanWrite)
                __Error.WriteNotSupported();
            if (value > _capacity)
                throw new IOException(Environment.GetResourceString("IO.IO_FixedCapacity"));
            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            if (value > len)
            {
                unsafe
                {
                    Buffer.ZeroMemory(_mem + len, value - len);
                }
            }

            Interlocked.Exchange(ref _length, value);
            if (pos > value)
            {
                Interlocked.Exchange(ref _position, value);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (!_isOpen)
                __Error.StreamIsClosed();
            if (!CanWrite)
                __Error.WriteNotSupported();
            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            long n = pos + count;
            if (n < 0)
                throw new IOException(Environment.GetResourceString("IO.IO_StreamTooLong"));
            if (n > _capacity)
            {
                throw new NotSupportedException(Environment.GetResourceString("IO.IO_FixedCapacity"));
            }

            if (_buffer == null)
            {
                if (pos > len)
                {
                    unsafe
                    {
                        Buffer.ZeroMemory(_mem + len, pos - len);
                    }
                }

                if (n > len)
                {
                    Interlocked.Exchange(ref _length, n);
                }
            }

            if (_buffer != null)
            {
                long bytesLeft = _capacity - pos;
                if (bytesLeft < count)
                {
                    throw new ArgumentException(Environment.GetResourceString("Arg_BufferTooSmall"));
                }

                unsafe
                {
                    byte *pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        Buffer.Memcpy(pointer + pos + _offset, 0, buffer, offset, count);
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
            else
            {
                unsafe
                {
                    Buffer.Memcpy(_mem + pos, 0, buffer, offset, count);
                }
            }

            Interlocked.Exchange(ref _position, n);
            return;
        }

        public override Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
            try
            {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                                return Task.FromException<Int32>(ex);
            }
        }

        public override void WriteByte(byte value)
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            if (!CanWrite)
                __Error.WriteNotSupported();
            long pos = Interlocked.Read(ref _position);
            long len = Interlocked.Read(ref _length);
            long n = pos + 1;
            if (pos >= len)
            {
                if (n < 0)
                    throw new IOException(Environment.GetResourceString("IO.IO_StreamTooLong"));
                if (n > _capacity)
                    throw new NotSupportedException(Environment.GetResourceString("IO.IO_FixedCapacity"));
                if (_buffer == null)
                {
                    if (pos > len)
                    {
                        unsafe
                        {
                            Buffer.ZeroMemory(_mem + len, pos - len);
                        }
                    }

                    Interlocked.Exchange(ref _length, n);
                }
            }

            if (_buffer != null)
            {
                unsafe
                {
                    byte *pointer = null;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        _buffer.AcquirePointer(ref pointer);
                        *(pointer + pos + _offset) = value;
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
            else
            {
                unsafe
                {
                    _mem[pos] = value;
                }
            }

            Interlocked.Exchange(ref _position, n);
        }
    }
}