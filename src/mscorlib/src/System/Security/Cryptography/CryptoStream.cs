namespace System.Security.Cryptography
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;

    public enum CryptoStreamMode
    {
        Read = 0,
        Write = 1
    }

    public class CryptoStream : Stream, IDisposable
    {
        private Stream _stream;
        private ICryptoTransform _Transform;
        private byte[] _InputBuffer;
        private int _InputBufferIndex = 0;
        private int _InputBlockSize;
        private byte[] _OutputBuffer;
        private int _OutputBufferIndex = 0;
        private int _OutputBlockSize;
        private CryptoStreamMode _transformMode;
        private bool _canRead = false;
        private bool _canWrite = false;
        private bool _finalBlockTransformed = false;
        public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
        {
            _stream = stream;
            _transformMode = mode;
            _Transform = transform;
            switch (_transformMode)
            {
                case CryptoStreamMode.Read:
                    if (!(_stream.CanRead))
                        throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotReadable"), "stream");
                    _canRead = true;
                    break;
                case CryptoStreamMode.Write:
                    if (!(_stream.CanWrite))
                        throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"), "stream");
                    _canWrite = true;
                    break;
                default:
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            }

            InitializeBuffer();
        }

        public override bool CanRead
        {
            [Pure]
            get
            {
                return _canRead;
            }
        }

        public override bool CanSeek
        {
            [Pure]
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            [Pure]
            get
            {
                return _canWrite;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
            }

            set
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
            }
        }

        public bool HasFlushedFinalBlock
        {
            get
            {
                return _finalBlockTransformed;
            }
        }

        public void FlushFinalBlock()
        {
            if (_finalBlockTransformed)
                throw new NotSupportedException(Environment.GetResourceString("Cryptography_CryptoStream_FlushFinalBlockTwice"));
            byte[] finalBytes = _Transform.TransformFinalBlock(_InputBuffer, 0, _InputBufferIndex);
            _finalBlockTransformed = true;
            if (_canWrite && _OutputBufferIndex > 0)
            {
                _stream.Write(_OutputBuffer, 0, _OutputBufferIndex);
                _OutputBufferIndex = 0;
            }

            if (_canWrite)
                _stream.Write(finalBytes, 0, finalBytes.Length);
            CryptoStream innerCryptoStream = _stream as CryptoStream;
            if (innerCryptoStream != null)
            {
                if (!innerCryptoStream.HasFlushedFinalBlock)
                {
                    innerCryptoStream.FlushFinalBlock();
                }
            }
            else
            {
                _stream.Flush();
            }

            if (_InputBuffer != null)
                Array.Clear(_InputBuffer, 0, _InputBuffer.Length);
            if (_OutputBuffer != null)
                Array.Clear(_OutputBuffer, 0, _OutputBuffer.Length);
            return;
        }

        public override void Flush()
        {
            return;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (this.GetType() != typeof (CryptoStream))
                return base.FlushAsync(cancellationToken);
            return cancellationToken.IsCancellationRequested ? Task.FromCancellation(cancellationToken) : Task.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            int bytesToDeliver = count;
            int currentOutputIndex = offset;
            if (_OutputBufferIndex != 0)
            {
                if (_OutputBufferIndex <= count)
                {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, offset, _OutputBufferIndex);
                    bytesToDeliver -= _OutputBufferIndex;
                    currentOutputIndex += _OutputBufferIndex;
                    _OutputBufferIndex = 0;
                }
                else
                {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, offset, count);
                    Buffer.InternalBlockCopy(_OutputBuffer, count, _OutputBuffer, 0, _OutputBufferIndex - count);
                    _OutputBufferIndex -= count;
                    return (count);
                }
            }

            if (_finalBlockTransformed)
            {
                return (count - bytesToDeliver);
            }

            int amountRead = 0;
            int numOutputBytes;
            if (bytesToDeliver > _OutputBlockSize)
            {
                if (_Transform.CanTransformMultipleBlocks)
                {
                    int BlocksToProcess = bytesToDeliver / _OutputBlockSize;
                    int numWholeBlocksInBytes = BlocksToProcess * _InputBlockSize;
                    byte[] tempInputBuffer = new byte[numWholeBlocksInBytes];
                    Buffer.InternalBlockCopy(_InputBuffer, 0, tempInputBuffer, 0, _InputBufferIndex);
                    amountRead = _InputBufferIndex;
                    amountRead += _stream.Read(tempInputBuffer, _InputBufferIndex, numWholeBlocksInBytes - _InputBufferIndex);
                    _InputBufferIndex = 0;
                    if (amountRead <= _InputBlockSize)
                    {
                        _InputBuffer = tempInputBuffer;
                        _InputBufferIndex = amountRead;
                        goto slow;
                    }

                    int numWholeReadBlocksInBytes = (amountRead / _InputBlockSize) * _InputBlockSize;
                    int numIgnoredBytes = amountRead - numWholeReadBlocksInBytes;
                    if (numIgnoredBytes != 0)
                    {
                        _InputBufferIndex = numIgnoredBytes;
                        Buffer.InternalBlockCopy(tempInputBuffer, numWholeReadBlocksInBytes, _InputBuffer, 0, numIgnoredBytes);
                    }

                    byte[] tempOutputBuffer = new byte[(numWholeReadBlocksInBytes / _InputBlockSize) * _OutputBlockSize];
                    numOutputBytes = _Transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, tempOutputBuffer, 0);
                    Buffer.InternalBlockCopy(tempOutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                    Array.Clear(tempInputBuffer, 0, tempInputBuffer.Length);
                    Array.Clear(tempOutputBuffer, 0, tempOutputBuffer.Length);
                    bytesToDeliver -= numOutputBytes;
                    currentOutputIndex += numOutputBytes;
                }
            }

            slow:
                while (bytesToDeliver > 0)
                {
                    while (_InputBufferIndex < _InputBlockSize)
                    {
                        amountRead = _stream.Read(_InputBuffer, _InputBufferIndex, _InputBlockSize - _InputBufferIndex);
                        if (amountRead == 0)
                            goto ProcessFinalBlock;
                        _InputBufferIndex += amountRead;
                    }

                    numOutputBytes = _Transform.TransformBlock(_InputBuffer, 0, _InputBlockSize, _OutputBuffer, 0);
                    _InputBufferIndex = 0;
                    if (bytesToDeliver >= numOutputBytes)
                    {
                        Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                        currentOutputIndex += numOutputBytes;
                        bytesToDeliver -= numOutputBytes;
                    }
                    else
                    {
                        Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                        _OutputBufferIndex = numOutputBytes - bytesToDeliver;
                        Buffer.InternalBlockCopy(_OutputBuffer, bytesToDeliver, _OutputBuffer, 0, _OutputBufferIndex);
                        return count;
                    }
                }

            return count;
            ProcessFinalBlock:
                byte[] finalBytes = _Transform.TransformFinalBlock(_InputBuffer, 0, _InputBufferIndex);
            _OutputBuffer = finalBytes;
            _OutputBufferIndex = finalBytes.Length;
            _finalBlockTransformed = true;
            if (bytesToDeliver < _OutputBufferIndex)
            {
                Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                _OutputBufferIndex -= bytesToDeliver;
                Buffer.InternalBlockCopy(_OutputBuffer, bytesToDeliver, _OutputBuffer, 0, _OutputBufferIndex);
                return (count);
            }
            else
            {
                Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, _OutputBufferIndex);
                bytesToDeliver -= _OutputBufferIndex;
                _OutputBufferIndex = 0;
                return (count - bytesToDeliver);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanRead)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (this.GetType() != typeof (CryptoStream))
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation<int>(cancellationToken);
            return ReadAsyncInternal(buffer, offset, count, cancellationToken);
        }

        private struct HopToThreadPoolAwaitable : INotifyCompletion
        {
            public HopToThreadPoolAwaitable GetAwaiter()
            {
                return this;
            }

            public bool IsCompleted
            {
                get
                {
                    return false;
                }
            }

            public void OnCompleted(Action continuation)
            {
                Task.Run(continuation);
            }

            public void GetResult()
            {
            }
        }

        private async Task<int> ReadAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Contract.Requires(CanRead);
            Contract.Requires(offset >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer.Length - offset >= count);
            await default (HopToThreadPoolAwaitable);
            var sem = base.EnsureAsyncActiveSemaphoreInitialized();
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                int bytesToDeliver = count;
                int currentOutputIndex = offset;
                if (_OutputBufferIndex != 0)
                {
                    if (_OutputBufferIndex <= count)
                    {
                        Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, offset, _OutputBufferIndex);
                        bytesToDeliver -= _OutputBufferIndex;
                        currentOutputIndex += _OutputBufferIndex;
                        _OutputBufferIndex = 0;
                    }
                    else
                    {
                        Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, offset, count);
                        Buffer.InternalBlockCopy(_OutputBuffer, count, _OutputBuffer, 0, _OutputBufferIndex - count);
                        _OutputBufferIndex -= count;
                        return (count);
                    }
                }

                if (_finalBlockTransformed)
                {
                    return (count - bytesToDeliver);
                }

                int amountRead = 0;
                int numOutputBytes;
                if (bytesToDeliver > _OutputBlockSize)
                {
                    if (_Transform.CanTransformMultipleBlocks)
                    {
                        int BlocksToProcess = bytesToDeliver / _OutputBlockSize;
                        int numWholeBlocksInBytes = BlocksToProcess * _InputBlockSize;
                        byte[] tempInputBuffer = new byte[numWholeBlocksInBytes];
                        Buffer.InternalBlockCopy(_InputBuffer, 0, tempInputBuffer, 0, _InputBufferIndex);
                        amountRead = _InputBufferIndex;
                        amountRead += await _stream.ReadAsync(tempInputBuffer, _InputBufferIndex, numWholeBlocksInBytes - _InputBufferIndex, cancellationToken).ConfigureAwait(false);
                        _InputBufferIndex = 0;
                        if (amountRead <= _InputBlockSize)
                        {
                            _InputBuffer = tempInputBuffer;
                            _InputBufferIndex = amountRead;
                            goto slow;
                        }

                        int numWholeReadBlocksInBytes = (amountRead / _InputBlockSize) * _InputBlockSize;
                        int numIgnoredBytes = amountRead - numWholeReadBlocksInBytes;
                        if (numIgnoredBytes != 0)
                        {
                            _InputBufferIndex = numIgnoredBytes;
                            Buffer.InternalBlockCopy(tempInputBuffer, numWholeReadBlocksInBytes, _InputBuffer, 0, numIgnoredBytes);
                        }

                        byte[] tempOutputBuffer = new byte[(numWholeReadBlocksInBytes / _InputBlockSize) * _OutputBlockSize];
                        numOutputBytes = _Transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, tempOutputBuffer, 0);
                        Buffer.InternalBlockCopy(tempOutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                        Array.Clear(tempInputBuffer, 0, tempInputBuffer.Length);
                        Array.Clear(tempOutputBuffer, 0, tempOutputBuffer.Length);
                        bytesToDeliver -= numOutputBytes;
                        currentOutputIndex += numOutputBytes;
                    }
                }

                slow:
                    while (bytesToDeliver > 0)
                    {
                        while (_InputBufferIndex < _InputBlockSize)
                        {
                            amountRead = await _stream.ReadAsync(_InputBuffer, _InputBufferIndex, _InputBlockSize - _InputBufferIndex, cancellationToken).ConfigureAwait(false);
                            if (amountRead == 0)
                                goto ProcessFinalBlock;
                            _InputBufferIndex += amountRead;
                        }

                        numOutputBytes = _Transform.TransformBlock(_InputBuffer, 0, _InputBlockSize, _OutputBuffer, 0);
                        _InputBufferIndex = 0;
                        if (bytesToDeliver >= numOutputBytes)
                        {
                            Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, numOutputBytes);
                            currentOutputIndex += numOutputBytes;
                            bytesToDeliver -= numOutputBytes;
                        }
                        else
                        {
                            Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                            _OutputBufferIndex = numOutputBytes - bytesToDeliver;
                            Buffer.InternalBlockCopy(_OutputBuffer, bytesToDeliver, _OutputBuffer, 0, _OutputBufferIndex);
                            return count;
                        }
                    }

                return count;
                ProcessFinalBlock:
                    byte[] finalBytes = _Transform.TransformFinalBlock(_InputBuffer, 0, _InputBufferIndex);
                _OutputBuffer = finalBytes;
                _OutputBufferIndex = finalBytes.Length;
                _finalBlockTransformed = true;
                if (bytesToDeliver < _OutputBufferIndex)
                {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, bytesToDeliver);
                    _OutputBufferIndex -= bytesToDeliver;
                    Buffer.InternalBlockCopy(_OutputBuffer, bytesToDeliver, _OutputBuffer, 0, _OutputBufferIndex);
                    return (count);
                }
                else
                {
                    Buffer.InternalBlockCopy(_OutputBuffer, 0, buffer, currentOutputIndex, _OutputBufferIndex);
                    bytesToDeliver -= _OutputBufferIndex;
                    _OutputBufferIndex = 0;
                    return (count - bytesToDeliver);
                }
            }
            finally
            {
                sem.Release();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            int bytesToWrite = count;
            int currentInputIndex = offset;
            if (_InputBufferIndex > 0)
            {
                if (count >= _InputBlockSize - _InputBufferIndex)
                {
                    Buffer.InternalBlockCopy(buffer, offset, _InputBuffer, _InputBufferIndex, _InputBlockSize - _InputBufferIndex);
                    currentInputIndex += (_InputBlockSize - _InputBufferIndex);
                    bytesToWrite -= (_InputBlockSize - _InputBufferIndex);
                    _InputBufferIndex = _InputBlockSize;
                }
                else
                {
                    Buffer.InternalBlockCopy(buffer, offset, _InputBuffer, _InputBufferIndex, count);
                    _InputBufferIndex += count;
                    return;
                }
            }

            if (_OutputBufferIndex > 0)
            {
                _stream.Write(_OutputBuffer, 0, _OutputBufferIndex);
                _OutputBufferIndex = 0;
            }

            int numOutputBytes;
            if (_InputBufferIndex == _InputBlockSize)
            {
                numOutputBytes = _Transform.TransformBlock(_InputBuffer, 0, _InputBlockSize, _OutputBuffer, 0);
                _stream.Write(_OutputBuffer, 0, numOutputBytes);
                _InputBufferIndex = 0;
            }

            while (bytesToWrite > 0)
            {
                if (bytesToWrite >= _InputBlockSize)
                {
                    if (_Transform.CanTransformMultipleBlocks)
                    {
                        int numWholeBlocks = bytesToWrite / _InputBlockSize;
                        int numWholeBlocksInBytes = numWholeBlocks * _InputBlockSize;
                        byte[] _tempOutputBuffer = new byte[numWholeBlocks * _OutputBlockSize];
                        numOutputBytes = _Transform.TransformBlock(buffer, currentInputIndex, numWholeBlocksInBytes, _tempOutputBuffer, 0);
                        _stream.Write(_tempOutputBuffer, 0, numOutputBytes);
                        currentInputIndex += numWholeBlocksInBytes;
                        bytesToWrite -= numWholeBlocksInBytes;
                    }
                    else
                    {
                        numOutputBytes = _Transform.TransformBlock(buffer, currentInputIndex, _InputBlockSize, _OutputBuffer, 0);
                        _stream.Write(_OutputBuffer, 0, numOutputBytes);
                        currentInputIndex += _InputBlockSize;
                        bytesToWrite -= _InputBlockSize;
                    }
                }
                else
                {
                    Buffer.InternalBlockCopy(buffer, currentInputIndex, _InputBuffer, 0, bytesToWrite);
                    _InputBufferIndex += bytesToWrite;
                    return;
                }
            }

            return;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!CanWrite)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (this.GetType() != typeof (CryptoStream))
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCancellation(cancellationToken);
            return WriteAsyncInternal(buffer, offset, count, cancellationToken);
        }

        private async Task WriteAsyncInternal(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Contract.Requires(CanWrite);
            Contract.Requires(offset >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer.Length - offset >= count);
            await default (HopToThreadPoolAwaitable);
            var sem = base.EnsureAsyncActiveSemaphoreInitialized();
            await sem.WaitAsync().ConfigureAwait(false);
            try
            {
                int bytesToWrite = count;
                int currentInputIndex = offset;
                if (_InputBufferIndex > 0)
                {
                    if (count >= _InputBlockSize - _InputBufferIndex)
                    {
                        Buffer.InternalBlockCopy(buffer, offset, _InputBuffer, _InputBufferIndex, _InputBlockSize - _InputBufferIndex);
                        currentInputIndex += (_InputBlockSize - _InputBufferIndex);
                        bytesToWrite -= (_InputBlockSize - _InputBufferIndex);
                        _InputBufferIndex = _InputBlockSize;
                    }
                    else
                    {
                        Buffer.InternalBlockCopy(buffer, offset, _InputBuffer, _InputBufferIndex, count);
                        _InputBufferIndex += count;
                        return;
                    }
                }

                if (_OutputBufferIndex > 0)
                {
                    await _stream.WriteAsync(_OutputBuffer, 0, _OutputBufferIndex, cancellationToken).ConfigureAwait(false);
                    _OutputBufferIndex = 0;
                }

                int numOutputBytes;
                if (_InputBufferIndex == _InputBlockSize)
                {
                    numOutputBytes = _Transform.TransformBlock(_InputBuffer, 0, _InputBlockSize, _OutputBuffer, 0);
                    await _stream.WriteAsync(_OutputBuffer, 0, numOutputBytes, cancellationToken).ConfigureAwait(false);
                    _InputBufferIndex = 0;
                }

                while (bytesToWrite > 0)
                {
                    if (bytesToWrite >= _InputBlockSize)
                    {
                        if (_Transform.CanTransformMultipleBlocks)
                        {
                            int numWholeBlocks = bytesToWrite / _InputBlockSize;
                            int numWholeBlocksInBytes = numWholeBlocks * _InputBlockSize;
                            byte[] _tempOutputBuffer = new byte[numWholeBlocks * _OutputBlockSize];
                            numOutputBytes = _Transform.TransformBlock(buffer, currentInputIndex, numWholeBlocksInBytes, _tempOutputBuffer, 0);
                            await _stream.WriteAsync(_tempOutputBuffer, 0, numOutputBytes, cancellationToken).ConfigureAwait(false);
                            currentInputIndex += numWholeBlocksInBytes;
                            bytesToWrite -= numWholeBlocksInBytes;
                        }
                        else
                        {
                            numOutputBytes = _Transform.TransformBlock(buffer, currentInputIndex, _InputBlockSize, _OutputBuffer, 0);
                            await _stream.WriteAsync(_OutputBuffer, 0, numOutputBytes, cancellationToken).ConfigureAwait(false);
                            currentInputIndex += _InputBlockSize;
                            bytesToWrite -= _InputBlockSize;
                        }
                    }
                    else
                    {
                        Buffer.InternalBlockCopy(buffer, currentInputIndex, _InputBuffer, 0, bytesToWrite);
                        _InputBufferIndex += bytesToWrite;
                        return;
                    }
                }

                return;
            }
            finally
            {
                sem.Release();
            }
        }

        public void Clear()
        {
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (!_finalBlockTransformed)
                    {
                        FlushFinalBlock();
                    }

                    _stream.Close();
                }
            }
            finally
            {
                try
                {
                    _finalBlockTransformed = true;
                    if (_InputBuffer != null)
                        Array.Clear(_InputBuffer, 0, _InputBuffer.Length);
                    if (_OutputBuffer != null)
                        Array.Clear(_OutputBuffer, 0, _OutputBuffer.Length);
                    _InputBuffer = null;
                    _OutputBuffer = null;
                    _canRead = false;
                    _canWrite = false;
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        private void InitializeBuffer()
        {
            if (_Transform != null)
            {
                _InputBlockSize = _Transform.InputBlockSize;
                _InputBuffer = new byte[_InputBlockSize];
                _OutputBlockSize = _Transform.OutputBlockSize;
                _OutputBuffer = new byte[_OutputBlockSize];
            }
        }
    }
}