using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public sealed class BufferedStream : Stream
    {
        private const Int32 _DefaultBufferSize = 4096;
        private Stream _stream;
        private Byte[] _buffer;
        private readonly Int32 _bufferSize;
        private Int32 _readPos;
        private Int32 _readLen;
        private Int32 _writePos;
        private BeginEndAwaitableAdapter _beginEndAwaitable;
        private Task<Int32> _lastSyncCompletedReadTask;
        private BufferedStream()
        {
        }

        public BufferedStream(Stream stream): this (stream, _DefaultBufferSize)
        {
        }

        public BufferedStream(Stream stream, Int32 bufferSize)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_MustBePositive", "bufferSize"));
            Contract.EndContractBlock();
            BCLDebug.Perf(!(stream is FileStream), "FileStream is buffered - don't wrap it in a BufferedStream");
            BCLDebug.Perf(!(stream is MemoryStream), "MemoryStream shouldn't be wrapped in a BufferedStream!");
            BCLDebug.Perf(!(stream is BufferedStream), "BufferedStream shouldn't be wrapped in another BufferedStream!");
            _stream = stream;
            _bufferSize = bufferSize;
            if (!_stream.CanRead && !_stream.CanWrite)
                __Error.StreamIsClosed();
        }

        private void EnsureNotClosed()
        {
            if (_stream == null)
                __Error.StreamIsClosed();
        }

        private void EnsureCanSeek()
        {
            Contract.Requires(_stream != null);
            if (!_stream.CanSeek)
                __Error.SeekNotSupported();
        }

        private void EnsureCanRead()
        {
            Contract.Requires(_stream != null);
            if (!_stream.CanRead)
                __Error.ReadNotSupported();
        }

        private void EnsureCanWrite()
        {
            Contract.Requires(_stream != null);
            if (!_stream.CanWrite)
                __Error.WriteNotSupported();
        }

        private void EnsureBeginEndAwaitableAllocated()
        {
            if (_beginEndAwaitable == null)
                _beginEndAwaitable = new BeginEndAwaitableAdapter();
        }

        private const Int32 MaxShadowBufferSize = 81920;
        private void EnsureShadowBufferAllocated()
        {
            Contract.Assert(_buffer != null);
            Contract.Assert(_bufferSize > 0);
            if (_buffer.Length != _bufferSize || _bufferSize >= MaxShadowBufferSize)
                return;
            Byte[] shadowBuffer = new Byte[Math.Min(_bufferSize + _bufferSize, MaxShadowBufferSize)];
            Buffer.InternalBlockCopy(_buffer, 0, shadowBuffer, 0, _writePos);
            _buffer = shadowBuffer;
        }

        private void EnsureBufferAllocated()
        {
            Contract.Assert(_bufferSize > 0);
            if (_buffer == null)
                _buffer = new Byte[_bufferSize];
        }

        internal Stream UnderlyingStream
        {
            [FriendAccessAllowed]
            [Pure]
            get
            {
                return _stream;
            }
        }

        internal Int32 BufferSize
        {
            [FriendAccessAllowed]
            [Pure]
            get
            {
                return _bufferSize;
            }
        }

        public override bool CanRead
        {
            [Pure]
            get
            {
                return _stream != null && _stream.CanRead;
            }
        }

        public override bool CanWrite
        {
            [Pure]
            get
            {
                return _stream != null && _stream.CanWrite;
            }
        }

        public override bool CanSeek
        {
            [Pure]
            get
            {
                return _stream != null && _stream.CanSeek;
            }
        }

        public override Int64 Length
        {
            get
            {
                EnsureNotClosed();
                if (_writePos > 0)
                    FlushWrite();
                return _stream.Length;
            }
        }

        public override Int64 Position
        {
            get
            {
                EnsureNotClosed();
                EnsureCanSeek();
                Contract.Assert(!(_writePos > 0 && _readPos != _readLen), "Read and Write buffers cannot both have data in them at the same time.");
                return _stream.Position + (_readPos - _readLen + _writePos);
            }

            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                Contract.EndContractBlock();
                EnsureNotClosed();
                EnsureCanSeek();
                if (_writePos > 0)
                    FlushWrite();
                _readPos = 0;
                _readLen = 0;
                _stream.Seek(value, SeekOrigin.Begin);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _stream != null)
                {
                    try
                    {
                        Flush();
                    }
                    finally
                    {
                        _stream.Close();
                    }
                }
            }
            finally
            {
                _stream = null;
                _buffer = null;
                _lastSyncCompletedReadTask = null;
                base.Dispose(disposing);
            }
        }

        public override void Flush()
        {
            EnsureNotClosed();
            if (_writePos > 0)
            {
                FlushWrite();
                Contract.Assert(_writePos == 0 && _readPos == 0 && _readLen == 0);
                return;
            }

            if (_readPos < _readLen)
            {
                if (!_stream.CanSeek)
                    return;
                FlushRead();
                if (_stream.CanWrite || _stream is BufferedStream)
                    _stream.Flush();
                Contract.Assert(_writePos == 0 && _readPos == 0 && _readLen == 0);
                return;
            }

            if (_stream.CanWrite || _stream is BufferedStream)
                _stream.Flush();
            _writePos = _readPos = _readLen = 0;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<Int32>(cancellationToken);
            EnsureNotClosed();
            return FlushAsyncInternal(cancellationToken, this, _stream, _writePos, _readPos, _readLen);
        }

        private static async Task FlushAsyncInternal(CancellationToken cancellationToken, BufferedStream _this, Stream stream, Int32 writePos, Int32 readPos, Int32 readLen)
        {
            Contract.Assert(stream != null);
            SemaphoreSlim sem = _this.EnsureAsyncActiveSemaphoreInitialized();
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                if (writePos > 0)
                {
                    await _this.FlushWriteAsync(cancellationToken).ConfigureAwait(false);
                    Contract.Assert(_this._writePos == 0 && _this._readPos == 0 && _this._readLen == 0);
                    return;
                }

                if (readPos < readLen)
                {
                    if (!stream.CanSeek)
                        return;
                    _this.FlushRead();
                    if (stream.CanRead || stream is BufferedStream)
                        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    Contract.Assert(_this._writePos == 0 && _this._readPos == 0 && _this._readLen == 0);
                    return;
                }

                if (stream.CanWrite || stream is BufferedStream)
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                Contract.Assert(_this._writePos == 0 && _this._readPos == _this._readLen);
            }
            finally
            {
                sem.Release();
            }
        }

        private void FlushRead()
        {
            Contract.Assert(_writePos == 0, "BufferedStream: Write buffer must be empty in FlushRead!");
            if (_readPos - _readLen != 0)
                _stream.Seek(_readPos - _readLen, SeekOrigin.Current);
            _readPos = 0;
            _readLen = 0;
        }

        private void ClearReadBufferBeforeWrite()
        {
            Contract.Assert(_readPos <= _readLen, "_readPos <= _readLen [" + _readPos + " <= " + _readLen + "]");
            if (_readPos == _readLen)
            {
                _readPos = _readLen = 0;
                return;
            }

            Contract.Assert(_readPos < _readLen);
            if (!_stream.CanSeek)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CannotWriteToBufferedStreamIfReadBufferCannotBeFlushed"));
            FlushRead();
        }

        private void FlushWrite()
        {
            Contract.Assert(_readPos == 0 && _readLen == 0, "BufferedStream: Read buffer must be empty in FlushWrite!");
            Contract.Assert(_buffer != null && _bufferSize >= _writePos, "BufferedStream: Write buffer must be allocated and write position must be in the bounds of the buffer in FlushWrite!");
            _stream.Write(_buffer, 0, _writePos);
            _writePos = 0;
            _stream.Flush();
        }

        private async Task FlushWriteAsync(CancellationToken cancellationToken)
        {
            Contract.Assert(_readPos == 0 && _readLen == 0, "BufferedStream: Read buffer must be empty in FlushWrite!");
            Contract.Assert(_buffer != null && _bufferSize >= _writePos, "BufferedStream: Write buffer must be allocated and write position must be in the bounds of the buffer in FlushWrite!");
            await _stream.WriteAsync(_buffer, 0, _writePos, cancellationToken).ConfigureAwait(false);
            _writePos = 0;
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private Int32 ReadFromBuffer(Byte[] array, Int32 offset, Int32 count)
        {
            Int32 readBytes = _readLen - _readPos;
            Contract.Assert(readBytes >= 0);
            if (readBytes == 0)
                return 0;
            Contract.Assert(readBytes > 0);
            if (readBytes > count)
                readBytes = count;
            Buffer.InternalBlockCopy(_buffer, _readPos, array, offset, readBytes);
            _readPos += readBytes;
            return readBytes;
        }

        private Int32 ReadFromBuffer(Byte[] array, Int32 offset, Int32 count, out Exception error)
        {
            try
            {
                error = null;
                return ReadFromBuffer(array, offset, count);
            }
            catch (Exception ex)
            {
                error = ex;
                return 0;
            }
        }

        public override int Read([In, Out] Byte[] array, Int32 offset, Int32 count)
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            EnsureNotClosed();
            EnsureCanRead();
            Int32 bytesFromBuffer = ReadFromBuffer(array, offset, count);
            if (bytesFromBuffer == count)
                return bytesFromBuffer;
            Int32 alreadySatisfied = bytesFromBuffer;
            if (bytesFromBuffer > 0)
            {
                count -= bytesFromBuffer;
                offset += bytesFromBuffer;
            }

            Contract.Assert(_readLen == _readPos);
            _readPos = _readLen = 0;
            if (_writePos > 0)
                FlushWrite();
            if (count >= _bufferSize)
            {
                return _stream.Read(array, offset, count) + alreadySatisfied;
            }

            EnsureBufferAllocated();
            _readLen = _stream.Read(_buffer, 0, _bufferSize);
            bytesFromBuffer = ReadFromBuffer(array, offset, count);
            return bytesFromBuffer + alreadySatisfied;
        }

        public override IAsyncResult BeginRead(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
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
            if (_stream == null)
                __Error.ReadNotSupported();
            EnsureCanRead();
            Int32 bytesFromBuffer = 0;
            SemaphoreSlim sem = base.EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreLockTask = sem.WaitAsync();
            if (semaphoreLockTask.Status == TaskStatus.RanToCompletion)
            {
                bool completeSynchronously = true;
                try
                {
                    Exception error;
                    bytesFromBuffer = ReadFromBuffer(buffer, offset, count, out error);
                    completeSynchronously = (bytesFromBuffer == count || error != null);
                    if (completeSynchronously)
                    {
                        SynchronousAsyncResult asyncResult = (error == null) ? new SynchronousAsyncResult(bytesFromBuffer, state) : new SynchronousAsyncResult(error, state, isWrite: false);
                        if (callback != null)
                            callback(asyncResult);
                        return asyncResult;
                    }
                }
                finally
                {
                    if (completeSynchronously)
                        sem.Release();
                }
            }

            return BeginReadFromUnderlyingStream(buffer, offset + bytesFromBuffer, count - bytesFromBuffer, callback, state, bytesFromBuffer, semaphoreLockTask);
        }

        private IAsyncResult BeginReadFromUnderlyingStream(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state, Int32 bytesAlreadySatisfied, Task semaphoreLockTask)
        {
            Task<Int32> readOp = ReadFromUnderlyingStreamAsync(buffer, offset, count, CancellationToken.None, bytesAlreadySatisfied, semaphoreLockTask, useApmPattern: true);
            return TaskToApm.Begin(readOp, callback, state);
        }

        public override Int32 EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            Contract.Ensures(Contract.Result<Int32>() >= 0);
            Contract.EndContractBlock();
            var sAR = asyncResult as SynchronousAsyncResult;
            if (sAR != null)
                return SynchronousAsyncResult.EndRead(asyncResult);
            return TaskToApm.End<Int32>(asyncResult);
        }

        private Task<Int32> LastSyncCompletedReadTask(Int32 val)
        {
            Task<Int32> t = _lastSyncCompletedReadTask;
            Contract.Assert(t == null || t.Status == TaskStatus.RanToCompletion);
            if (t != null && t.Result == val)
                return t;
            t = Task.FromResult<Int32>(val);
            _lastSyncCompletedReadTask = t;
            return t;
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
            Contract.EndContractBlock();
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<Int32>(cancellationToken);
            EnsureNotClosed();
            EnsureCanRead();
            Int32 bytesFromBuffer = 0;
            SemaphoreSlim sem = base.EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreLockTask = sem.WaitAsync();
            if (semaphoreLockTask.Status == TaskStatus.RanToCompletion)
            {
                bool completeSynchronously = true;
                try
                {
                    Exception error;
                    bytesFromBuffer = ReadFromBuffer(buffer, offset, count, out error);
                    completeSynchronously = (bytesFromBuffer == count || error != null);
                    if (completeSynchronously)
                    {
                        return (error == null) ? LastSyncCompletedReadTask(bytesFromBuffer) : Task.FromException<Int32>(error);
                    }
                }
                finally
                {
                    if (completeSynchronously)
                        sem.Release();
                }
            }

            return ReadFromUnderlyingStreamAsync(buffer, offset + bytesFromBuffer, count - bytesFromBuffer, cancellationToken, bytesFromBuffer, semaphoreLockTask, useApmPattern: false);
        }

        private async Task<Int32> ReadFromUnderlyingStreamAsync(Byte[] array, Int32 offset, Int32 count, CancellationToken cancellationToken, Int32 bytesAlreadySatisfied, Task semaphoreLockTask, bool useApmPattern)
        {
            Contract.Assert(array != null);
            Contract.Assert(offset >= 0);
            Contract.Assert(count >= 0);
            Contract.Assert(array.Length - offset >= count);
            Contract.Assert(_stream != null);
            Contract.Assert(_stream.CanRead);
            Contract.Assert(_bufferSize > 0);
            Contract.Assert(semaphoreLockTask != null);
            await semaphoreLockTask.ConfigureAwait(false);
            try
            {
                Int32 bytesFromBuffer = ReadFromBuffer(array, offset, count);
                if (bytesFromBuffer == count)
                    return bytesAlreadySatisfied + bytesFromBuffer;
                if (bytesFromBuffer > 0)
                {
                    count -= bytesFromBuffer;
                    offset += bytesFromBuffer;
                    bytesAlreadySatisfied += bytesFromBuffer;
                }

                Contract.Assert(_readLen == _readPos);
                _readPos = _readLen = 0;
                if (_writePos > 0)
                    await FlushWriteAsync(cancellationToken).ConfigureAwait(false);
                if (count >= _bufferSize)
                {
                    if (useApmPattern)
                    {
                        EnsureBeginEndAwaitableAllocated();
                        _stream.BeginRead(array, offset, count, BeginEndAwaitableAdapter.Callback, _beginEndAwaitable);
                        return bytesAlreadySatisfied + _stream.EndRead(await _beginEndAwaitable);
                    }
                    else
                    {
                        return bytesAlreadySatisfied + await _stream.ReadAsync(array, offset, count, cancellationToken).ConfigureAwait(false);
                    }
                }

                EnsureBufferAllocated();
                if (useApmPattern)
                {
                    EnsureBeginEndAwaitableAllocated();
                    _stream.BeginRead(_buffer, 0, _bufferSize, BeginEndAwaitableAdapter.Callback, _beginEndAwaitable);
                    _readLen = _stream.EndRead(await _beginEndAwaitable);
                }
                else
                {
                    _readLen = await _stream.ReadAsync(_buffer, 0, _bufferSize, cancellationToken).ConfigureAwait(false);
                }

                bytesFromBuffer = ReadFromBuffer(array, offset, count);
                return bytesAlreadySatisfied + bytesFromBuffer;
            }
            finally
            {
                SemaphoreSlim sem = base.EnsureAsyncActiveSemaphoreInitialized();
                sem.Release();
            }
        }

        public override Int32 ReadByte()
        {
            EnsureNotClosed();
            EnsureCanRead();
            if (_readPos == _readLen)
            {
                if (_writePos > 0)
                    FlushWrite();
                EnsureBufferAllocated();
                _readLen = _stream.Read(_buffer, 0, _bufferSize);
                _readPos = 0;
            }

            if (_readPos == _readLen)
                return -1;
            Int32 b = _buffer[_readPos++];
            return b;
        }

        private void WriteToBuffer(Byte[] array, ref Int32 offset, ref Int32 count)
        {
            Int32 bytesToWrite = Math.Min(_bufferSize - _writePos, count);
            if (bytesToWrite <= 0)
                return;
            EnsureBufferAllocated();
            Buffer.InternalBlockCopy(array, offset, _buffer, _writePos, bytesToWrite);
            _writePos += bytesToWrite;
            count -= bytesToWrite;
            offset += bytesToWrite;
        }

        private void WriteToBuffer(Byte[] array, ref Int32 offset, ref Int32 count, out Exception error)
        {
            try
            {
                error = null;
                WriteToBuffer(array, ref offset, ref count);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }

        public override void Write(Byte[] array, Int32 offset, Int32 count)
        {
            if (array == null)
                throw new ArgumentNullException("array", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            EnsureNotClosed();
            EnsureCanWrite();
            if (_writePos == 0)
                ClearReadBufferBeforeWrite();
            Contract.Assert(_writePos < _bufferSize);
            Int32 totalUserBytes;
            bool useBuffer;
            checked
            {
                totalUserBytes = _writePos + count;
                useBuffer = (totalUserBytes + count < (_bufferSize + _bufferSize));
            }

            if (useBuffer)
            {
                WriteToBuffer(array, ref offset, ref count);
                if (_writePos < _bufferSize)
                {
                    Contract.Assert(count == 0);
                    return;
                }

                Contract.Assert(count >= 0);
                Contract.Assert(_writePos == _bufferSize);
                Contract.Assert(_buffer != null);
                _stream.Write(_buffer, 0, _writePos);
                _writePos = 0;
                WriteToBuffer(array, ref offset, ref count);
                Contract.Assert(count == 0);
                Contract.Assert(_writePos < _bufferSize);
            }
            else
            {
                if (_writePos > 0)
                {
                    Contract.Assert(_buffer != null);
                    Contract.Assert(totalUserBytes >= _bufferSize);
                    if (totalUserBytes <= (_bufferSize + _bufferSize) && totalUserBytes <= MaxShadowBufferSize)
                    {
                        EnsureShadowBufferAllocated();
                        Buffer.InternalBlockCopy(array, offset, _buffer, _writePos, count);
                        _stream.Write(_buffer, 0, totalUserBytes);
                        _writePos = 0;
                        return;
                    }

                    _stream.Write(_buffer, 0, _writePos);
                    _writePos = 0;
                }

                _stream.Write(array, offset, count);
            }
        }

        public override IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state)
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
            if (_stream == null)
                __Error.ReadNotSupported();
            EnsureCanWrite();
            SemaphoreSlim sem = base.EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreLockTask = sem.WaitAsync();
            if (semaphoreLockTask.Status == TaskStatus.RanToCompletion)
            {
                bool completeSynchronously = true;
                try
                {
                    if (_writePos == 0)
                        ClearReadBufferBeforeWrite();
                    Contract.Assert(_writePos < _bufferSize);
                    completeSynchronously = (count < _bufferSize - _writePos);
                    if (completeSynchronously)
                    {
                        Exception error;
                        WriteToBuffer(buffer, ref offset, ref count, out error);
                        Contract.Assert(count == 0);
                        SynchronousAsyncResult asyncResult = (error == null) ? new SynchronousAsyncResult(state) : new SynchronousAsyncResult(error, state, isWrite: true);
                        if (callback != null)
                            callback(asyncResult);
                        return asyncResult;
                    }
                }
                finally
                {
                    if (completeSynchronously)
                        sem.Release();
                }
            }

            return BeginWriteToUnderlyingStream(buffer, offset, count, callback, state, semaphoreLockTask);
        }

        private IAsyncResult BeginWriteToUnderlyingStream(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback callback, Object state, Task semaphoreLockTask)
        {
            Task writeOp = WriteToUnderlyingStreamAsync(buffer, offset, count, CancellationToken.None, semaphoreLockTask, useApmPattern: true);
            return TaskToApm.Begin(writeOp, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
            Contract.EndContractBlock();
            var sAR = asyncResult as SynchronousAsyncResult;
            if (sAR != null)
            {
                SynchronousAsyncResult.EndWrite(asyncResult);
                return;
            }

            TaskToApm.End(asyncResult);
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
            Contract.EndContractBlock();
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<Int32>(cancellationToken);
            EnsureNotClosed();
            EnsureCanWrite();
            SemaphoreSlim sem = base.EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreLockTask = sem.WaitAsync();
            if (semaphoreLockTask.Status == TaskStatus.RanToCompletion)
            {
                bool completeSynchronously = true;
                try
                {
                    if (_writePos == 0)
                        ClearReadBufferBeforeWrite();
                    Contract.Assert(_writePos < _bufferSize);
                    completeSynchronously = (count < _bufferSize - _writePos);
                    if (completeSynchronously)
                    {
                        Exception error;
                        WriteToBuffer(buffer, ref offset, ref count, out error);
                        Contract.Assert(count == 0);
                        return (error == null) ? Task.CompletedTask : Task.FromException(error);
                    }
                }
                finally
                {
                    if (completeSynchronously)
                        sem.Release();
                }
            }

            return WriteToUnderlyingStreamAsync(buffer, offset, count, cancellationToken, semaphoreLockTask, useApmPattern: false);
        }

        private async Task WriteToUnderlyingStreamAsync(Byte[] array, Int32 offset, Int32 count, CancellationToken cancellationToken, Task semaphoreLockTask, bool useApmPattern)
        {
            Contract.Assert(array != null);
            Contract.Assert(offset >= 0);
            Contract.Assert(count >= 0);
            Contract.Assert(array.Length - offset >= count);
            Contract.Assert(_stream != null);
            Contract.Assert(_stream.CanWrite);
            Contract.Assert(_bufferSize > 0);
            Contract.Assert(semaphoreLockTask != null);
            await semaphoreLockTask.ConfigureAwait(false);
            try
            {
                if (_writePos == 0)
                    ClearReadBufferBeforeWrite();
                Int32 totalUserBytes;
                bool useBuffer;
                checked
                {
                    totalUserBytes = _writePos + count;
                    useBuffer = (totalUserBytes + count < (_bufferSize + _bufferSize));
                }

                if (useBuffer)
                {
                    WriteToBuffer(array, ref offset, ref count);
                    if (_writePos < _bufferSize)
                    {
                        Contract.Assert(count == 0);
                        return;
                    }

                    Contract.Assert(count >= 0);
                    Contract.Assert(_writePos == _bufferSize);
                    Contract.Assert(_buffer != null);
                    if (useApmPattern)
                    {
                        EnsureBeginEndAwaitableAllocated();
                        _stream.BeginWrite(_buffer, 0, _writePos, BeginEndAwaitableAdapter.Callback, _beginEndAwaitable);
                        _stream.EndWrite(await _beginEndAwaitable);
                    }
                    else
                    {
                        await _stream.WriteAsync(_buffer, 0, _writePos, cancellationToken).ConfigureAwait(false);
                    }

                    _writePos = 0;
                    WriteToBuffer(array, ref offset, ref count);
                    Contract.Assert(count == 0);
                    Contract.Assert(_writePos < _bufferSize);
                }
                else
                {
                    if (_writePos > 0)
                    {
                        Contract.Assert(_buffer != null);
                        Contract.Assert(totalUserBytes >= _bufferSize);
                        if (totalUserBytes <= (_bufferSize + _bufferSize) && totalUserBytes <= MaxShadowBufferSize)
                        {
                            EnsureShadowBufferAllocated();
                            Buffer.InternalBlockCopy(array, offset, _buffer, _writePos, count);
                            if (useApmPattern)
                            {
                                EnsureBeginEndAwaitableAllocated();
                                _stream.BeginWrite(_buffer, 0, totalUserBytes, BeginEndAwaitableAdapter.Callback, _beginEndAwaitable);
                                _stream.EndWrite(await _beginEndAwaitable);
                            }
                            else
                            {
                                await _stream.WriteAsync(_buffer, 0, totalUserBytes, cancellationToken).ConfigureAwait(false);
                            }

                            _writePos = 0;
                            return;
                        }

                        if (useApmPattern)
                        {
                            EnsureBeginEndAwaitableAllocated();
                            _stream.BeginWrite(_buffer, 0, _writePos, BeginEndAwaitableAdapter.Callback, _beginEndAwaitable);
                            _stream.EndWrite(await _beginEndAwaitable);
                        }
                        else
                        {
                            await _stream.WriteAsync(_buffer, 0, _writePos, cancellationToken).ConfigureAwait(false);
                        }

                        _writePos = 0;
                    }

                    if (useApmPattern)
                    {
                        EnsureBeginEndAwaitableAllocated();
                        _stream.BeginWrite(array, offset, count, BeginEndAwaitableAdapter.Callback, _beginEndAwaitable);
                        _stream.EndWrite(await _beginEndAwaitable);
                    }
                    else
                    {
                        await _stream.WriteAsync(array, offset, count, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                SemaphoreSlim sem = base.EnsureAsyncActiveSemaphoreInitialized();
                sem.Release();
            }
        }

        public override void WriteByte(Byte value)
        {
            EnsureNotClosed();
            if (_writePos == 0)
            {
                EnsureCanWrite();
                ClearReadBufferBeforeWrite();
                EnsureBufferAllocated();
            }

            if (_writePos >= _bufferSize - 1)
                FlushWrite();
            _buffer[_writePos++] = value;
            Contract.Assert(_writePos < _bufferSize);
        }

        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            EnsureNotClosed();
            EnsureCanSeek();
            if (_writePos > 0)
            {
                FlushWrite();
                return _stream.Seek(offset, origin);
            }

            if (_readLen - _readPos > 0 && origin == SeekOrigin.Current)
            {
                offset -= (_readLen - _readPos);
            }

            Int64 oldPos = Position;
            Contract.Assert(oldPos == _stream.Position + (_readPos - _readLen));
            Int64 newPos = _stream.Seek(offset, origin);
            _readPos = (Int32)(newPos - (oldPos - _readPos));
            if (0 <= _readPos && _readPos < _readLen)
            {
                _stream.Seek(_readLen - _readPos, SeekOrigin.Current);
            }
            else
            {
                _readPos = _readLen = 0;
            }

            Contract.Assert(newPos == Position, "newPos (=" + newPos + ") == Position (=" + Position + ")");
            return newPos;
        }

        public override void SetLength(Int64 value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NegFileSize"));
            Contract.EndContractBlock();
            EnsureNotClosed();
            EnsureCanSeek();
            EnsureCanWrite();
            Flush();
            _stream.SetLength(value);
        }
    }
}