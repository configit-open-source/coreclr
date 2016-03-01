using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Permissions;

namespace System.IO
{
    public class MemoryStream : Stream
    {
        private byte[] _buffer;
        private int _origin;
        private int _position;
        private int _length;
        private int _capacity;
        private bool _expandable;
        private bool _writable;
        private bool _exposable;
        private bool _isOpen;
        private Task<int> _lastReadTask;
        private const int MemStreamMaxLength = Int32.MaxValue;
        public MemoryStream(): this (0)
        {
        }

        public MemoryStream(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity", Environment.GetResourceString("ArgumentOutOfRange_NegativeCapacity"));
            }

            Contract.EndContractBlock();
            _buffer = new byte[capacity];
            _capacity = capacity;
            _expandable = true;
            _writable = true;
            _exposable = true;
            _origin = 0;
            _isOpen = true;
        }

        public MemoryStream(byte[] buffer): this (buffer, true)
        {
        }

        public MemoryStream(byte[] buffer, bool writable)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            Contract.EndContractBlock();
            _buffer = buffer;
            _length = _capacity = buffer.Length;
            _writable = writable;
            _exposable = false;
            _origin = 0;
            _isOpen = true;
        }

        public MemoryStream(byte[] buffer, int index, int count): this (buffer, index, count, true, false)
        {
        }

        public MemoryStream(byte[] buffer, int index, int count, bool writable): this (buffer, index, count, writable, false)
        {
        }

        public MemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            _buffer = buffer;
            _origin = _position = index;
            _length = _capacity = index + count;
            _writable = writable;
            _exposable = publiclyVisible;
            _expandable = false;
            _isOpen = true;
        }

        public override bool CanRead
        {
            [Pure]
            get
            {
                return _isOpen;
            }
        }

        public override bool CanSeek
        {
            [Pure]
            get
            {
                return _isOpen;
            }
        }

        public override bool CanWrite
        {
            [Pure]
            get
            {
                return _writable;
            }
        }

        private void EnsureWriteable()
        {
            if (!CanWrite)
                __Error.WriteNotSupported();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    _isOpen = false;
                    _writable = false;
                    _expandable = false;
                    _lastReadTask = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private bool EnsureCapacity(int value)
        {
            if (value < 0)
                throw new IOException(Environment.GetResourceString("IO.IO_StreamTooLong"));
            if (value > _capacity)
            {
                int newCapacity = value;
                if (newCapacity < 256)
                    newCapacity = 256;
                if (newCapacity < _capacity * 2)
                    newCapacity = _capacity * 2;
                if ((uint)(_capacity * 2) > Array.MaxByteArrayLength)
                    newCapacity = value > Array.MaxByteArrayLength ? value : Array.MaxByteArrayLength;
                Capacity = newCapacity;
                return true;
            }

            return false;
        }

        public override void Flush()
        {
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

        public virtual byte[] GetBuffer()
        {
            if (!_exposable)
                throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_MemStreamBuffer"));
            return _buffer;
        }

        public virtual bool TryGetBuffer(out ArraySegment<byte> buffer)
        {
            if (!_exposable)
            {
                buffer = default (ArraySegment<byte>);
                return false;
            }

            buffer = new ArraySegment<byte>(_buffer, offset: _origin, count: (_length - _origin));
            return true;
        }

        internal byte[] InternalGetBuffer()
        {
            return _buffer;
        }

        internal void InternalGetOriginAndLength(out int origin, out int length)
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            origin = _origin;
            length = _length;
        }

        internal int InternalGetPosition()
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            return _position;
        }

        internal int InternalReadInt32()
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            int pos = (_position += 4);
            if (pos > _length)
            {
                _position = _length;
                __Error.EndOfFile();
            }

            return (int)(_buffer[pos - 4] | _buffer[pos - 3] << 8 | _buffer[pos - 2] << 16 | _buffer[pos - 1] << 24);
        }

        internal int InternalEmulateRead(int count)
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            int n = _length - _position;
            if (n > count)
                n = count;
            if (n < 0)
                n = 0;
            Contract.Assert(_position + n >= 0, "_position + n >= 0");
            _position += n;
            return n;
        }

        public virtual int Capacity
        {
            get
            {
                if (!_isOpen)
                    __Error.StreamIsClosed();
                return _capacity - _origin;
            }

            set
            {
                if (value < Length)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_SmallCapacity"));
                Contract.Ensures(_capacity - _origin == value);
                Contract.EndContractBlock();
                if (!_isOpen)
                    __Error.StreamIsClosed();
                if (!_expandable && (value != Capacity))
                    __Error.MemoryStreamNotExpandable();
                if (_expandable && value != _capacity)
                {
                    if (value > 0)
                    {
                        byte[] newBuffer = new byte[value];
                        if (_length > 0)
                            Buffer.InternalBlockCopy(_buffer, 0, newBuffer, 0, _length);
                        _buffer = newBuffer;
                    }
                    else
                    {
                        _buffer = null;
                    }

                    _capacity = value;
                }
            }
        }

        public override long Length
        {
            get
            {
                if (!_isOpen)
                    __Error.StreamIsClosed();
                return _length - _origin;
            }
        }

        public override long Position
        {
            get
            {
                if (!_isOpen)
                    __Error.StreamIsClosed();
                return _position - _origin;
            }

            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                Contract.Ensures(Position == value);
                Contract.EndContractBlock();
                if (!_isOpen)
                    __Error.StreamIsClosed();
                if (value > MemStreamMaxLength)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_StreamLength"));
                _position = _origin + (int)value;
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
            Contract.EndContractBlock();
            if (!_isOpen)
                __Error.StreamIsClosed();
            int n = _length - _position;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;
            Contract.Assert(_position + n >= 0, "_position + n >= 0");
            if (n <= 8)
            {
                int byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _buffer[_position + byteCount];
            }
            else
                Buffer.InternalBlockCopy(_buffer, _position, buffer, offset, n);
            _position += n;
            return n;
        }

        public override Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<int>(cancellationToken);
            try
            {
                int n = Read(buffer, offset, count);
                var t = _lastReadTask;
                Contract.Assert(t == null || t.Status == TaskStatus.RanToCompletion, "Expected that a stored last task completed successfully");
                return (t != null && t.Result == n) ? t : (_lastReadTask = Task.FromResult<int>(n));
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCancellation<int>(oce);
            }
            catch (Exception exception)
            {
                return Task.FromException<int>(exception);
            }
        }

        public override int ReadByte()
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            if (_position >= _length)
                return -1;
            return _buffer[_position++];
        }

        public override Task CopyToAsync(Stream destination, Int32 bufferSize, CancellationToken cancellationToken)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            if (!CanRead && !CanWrite)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!destination.CanRead && !destination.CanWrite)
                throw new ObjectDisposedException("destination", Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!CanRead)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (!destination.CanWrite)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
            Contract.EndContractBlock();
            if (this.GetType() != typeof (MemoryStream))
                return base.CopyToAsync(destination, bufferSize, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
            Int32 pos = _position;
            Int32 n = InternalEmulateRead(_length - _position);
            MemoryStream memStrDest = destination as MemoryStream;
            if (memStrDest == null)
                return destination.WriteAsync(_buffer, pos, n, cancellationToken);
            try
            {
                memStrDest.Write(_buffer, pos, n);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        public override long Seek(long offset, SeekOrigin loc)
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            if (offset > MemStreamMaxLength)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_StreamLength"));
            switch (loc)
            {
                case SeekOrigin.Begin:
                {
                    int tempPosition = unchecked (_origin + (int)offset);
                    if (offset < 0 || tempPosition < _origin)
                        throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
                    _position = tempPosition;
                    break;
                }

                case SeekOrigin.Current:
                {
                    int tempPosition = unchecked (_position + (int)offset);
                    if (unchecked (_position + offset) < _origin || tempPosition < _origin)
                        throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
                    _position = tempPosition;
                    break;
                }

                case SeekOrigin.End:
                {
                    int tempPosition = unchecked (_length + (int)offset);
                    if (unchecked (_length + offset) < _origin || tempPosition < _origin)
                        throw new IOException(Environment.GetResourceString("IO.IO_SeekBeforeBegin"));
                    _position = tempPosition;
                    break;
                }

                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSeekOrigin"));
            }

            Contract.Assert(_position >= 0, "_position >= 0");
            return _position;
        }

        public override void SetLength(long value)
        {
            if (value < 0 || value > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_StreamLength"));
            }

            Contract.Ensures(_length - _origin == value);
            Contract.EndContractBlock();
            EnsureWriteable();
            Contract.Assert(MemStreamMaxLength == Int32.MaxValue);
            if (value > (Int32.MaxValue - _origin))
            {
                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_StreamLength"));
            }

            int newLength = _origin + (int)value;
            bool allocatedNewArray = EnsureCapacity(newLength);
            if (!allocatedNewArray && newLength > _length)
                Array.Clear(_buffer, _length, newLength - _length);
            _length = newLength;
            if (_position > newLength)
                _position = newLength;
        }

        public virtual byte[] ToArray()
        {
            BCLDebug.Perf(_exposable, "MemoryStream::GetBuffer will let you avoid a copy.");
            byte[] copy = new byte[_length - _origin];
            Buffer.InternalBlockCopy(_buffer, _origin, copy, 0, _length - _origin);
            return copy;
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
            Contract.EndContractBlock();
            if (!_isOpen)
                __Error.StreamIsClosed();
            EnsureWriteable();
            int i = _position + count;
            if (i < 0)
                throw new IOException(Environment.GetResourceString("IO.IO_StreamTooLong"));
            if (i > _length)
            {
                bool mustZero = _position > _length;
                if (i > _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray)
                        mustZero = false;
                }

                if (mustZero)
                    Array.Clear(_buffer, _length, i - _length);
                _length = i;
            }

            if ((count <= 8) && (buffer != _buffer))
            {
                int byteCount = count;
                while (--byteCount >= 0)
                    _buffer[_position + byteCount] = buffer[offset + byteCount];
            }
            else
                Buffer.InternalBlockCopy(buffer, offset, _buffer, _position, count);
            _position = i;
        }

        public override Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
            try
            {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            }
            catch (OperationCanceledException oce)
            {
                return Task.FromCancellation<VoidTaskResult>(oce);
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }

        public override void WriteByte(byte value)
        {
            if (!_isOpen)
                __Error.StreamIsClosed();
            EnsureWriteable();
            if (_position >= _length)
            {
                int newLength = _position + 1;
                bool mustZero = _position > _length;
                if (newLength >= _capacity)
                {
                    bool allocatedNewArray = EnsureCapacity(newLength);
                    if (allocatedNewArray)
                        mustZero = false;
                }

                if (mustZero)
                    Array.Clear(_buffer, _length, _position - _length);
                _length = newLength;
            }

            _buffer[_position++] = value;
        }

        public virtual void WriteTo(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream", Environment.GetResourceString("ArgumentNull_Stream"));
            Contract.EndContractBlock();
            if (!_isOpen)
                __Error.StreamIsClosed();
            stream.Write(_buffer, _origin, _length - _origin);
        }
    }
}