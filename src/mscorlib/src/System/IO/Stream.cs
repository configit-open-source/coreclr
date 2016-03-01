
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    public abstract class Stream : IDisposable
    {
        public static readonly Stream Null = new NullStream();
        private const int _DefaultCopyBufferSize = 81920;
        private ReadWriteTask _activeReadWriteTask;
        private SemaphoreSlim _asyncActiveSemaphore;
        internal SemaphoreSlim EnsureAsyncActiveSemaphoreInitialized()
        {
            return LazyInitializer.EnsureInitialized(ref _asyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
        }

        public abstract bool CanRead
        {
            
            get;
        }

        public abstract bool CanSeek
        {
            
            get;
        }

        public virtual bool CanTimeout
        {
            
            get
            {
                return false;
            }
        }

        public abstract bool CanWrite
        {
            
            get;
        }

        public abstract long Length
        {
            get;
        }

        public abstract long Position
        {
            get;
            set;
        }

        public virtual int ReadTimeout
        {
            get
            {
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
            }

            set
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
            }
        }

        public virtual int WriteTimeout
        {
            get
            {
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
            }

            set
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TimeoutsNotSupported"));
            }
        }

        public Task CopyToAsync(Stream destination)
        {
            return CopyToAsync(destination, _DefaultCopyBufferSize);
        }

        public Task CopyToAsync(Stream destination, Int32 bufferSize)
        {
            return CopyToAsync(destination, bufferSize, CancellationToken.None);
        }

        public virtual Task CopyToAsync(Stream destination, Int32 bufferSize, CancellationToken cancellationToken)
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
                        return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
        }

        private async Task CopyToAsyncInternal(Stream destination, Int32 bufferSize, CancellationToken cancellationToken)
        {
                                                            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
        }

        public void CopyTo(Stream destination)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (!CanRead && !CanWrite)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!destination.CanRead && !destination.CanWrite)
                throw new ObjectDisposedException("destination", Environment.GetResourceString("ObjectDisposed_StreamClosed"));
            if (!CanRead)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
            if (!destination.CanWrite)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnwritableStream"));
                        InternalCopyTo(destination, _DefaultCopyBufferSize);
        }

        public void CopyTo(Stream destination, int bufferSize)
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
                        InternalCopyTo(destination, bufferSize);
        }

        private void InternalCopyTo(Stream destination, int bufferSize)
        {
                                                            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = Read(buffer, 0, buffer.Length)) != 0)
                destination.Write(buffer, 0, read);
        }

        public virtual void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Close();
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract void Flush();
        public Task FlushAsync()
        {
            return FlushAsync(CancellationToken.None);
        }

        public virtual Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(state => ((Stream)state).Flush(), this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        protected virtual WaitHandle CreateWaitHandle()
        {
                        return new ManualResetEvent(false);
        }

        public virtual IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
                        return BeginReadInternal(buffer, offset, count, callback, state, serializeAsynchronously: false, apm: true);
        }

        internal IAsyncResult BeginReadInternal(byte[] buffer, int offset, int count, AsyncCallback callback, Object state, bool serializeAsynchronously, bool apm)
        {
                        if (!CanRead)
                __Error.ReadNotSupported();
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                return BlockingBeginRead(buffer, offset, count, callback, state);
            }

            var semaphore = EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreTask = null;
            if (serializeAsynchronously)
            {
                semaphoreTask = semaphore.WaitAsync();
            }
            else
            {
                semaphore.Wait();
            }

            var asyncResult = new ReadWriteTask(true, apm, delegate
            {
                var thisTask = Task.InternalCurrent as ReadWriteTask;
                                try
                {
                    return thisTask._stream.Read(thisTask._buffer, thisTask._offset, thisTask._count);
                }
                finally
                {
                    if (!thisTask._apm)
                    {
                        thisTask._stream.FinishTrackingAsyncOperation();
                    }

                    thisTask.ClearBeginState();
                }
            }

            , state, this, buffer, offset, count, callback);
            if (semaphoreTask != null)
                RunReadWriteTaskWhenReady(semaphoreTask, asyncResult);
            else
                RunReadWriteTask(asyncResult);
            return asyncResult;
        }

        public virtual int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
                                    if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                return BlockingEndRead(asyncResult);
            }

            var readTask = _activeReadWriteTask;
            if (readTask == null)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
            }
            else if (readTask != asyncResult)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
            }
            else if (!readTask._isRead)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndReadCalledMultiple"));
            }

            try
            {
                return readTask.GetAwaiter().GetResult();
            }
            finally
            {
                FinishTrackingAsyncOperation();
            }
        }

        public Task<int> ReadAsync(Byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None);
        }

        public virtual Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ? Task.FromCancellation<int>(cancellationToken) : BeginEndReadAsync(buffer, offset, count);
        }

        private extern bool HasOverriddenBeginEndRead();
        private Task<Int32> BeginEndReadAsync(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (!HasOverriddenBeginEndRead())
            {
                return (Task<Int32>)BeginReadInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
            }

            return TaskFactory<Int32>.FromAsyncTrim(this, new ReadWriteParameters{Buffer = buffer, Offset = offset, Count = count}, (stream, args, callback, state) => stream.BeginRead(args.Buffer, args.Offset, args.Count, callback, state), (stream, asyncResult) => stream.EndRead(asyncResult));
        }

        private struct ReadWriteParameters
        {
            internal byte[] Buffer;
            internal int Offset;
            internal int Count;
        }

        public virtual IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
                        return BeginWriteInternal(buffer, offset, count, callback, state, serializeAsynchronously: false, apm: true);
        }

        internal IAsyncResult BeginWriteInternal(byte[] buffer, int offset, int count, AsyncCallback callback, Object state, bool serializeAsynchronously, bool apm)
        {
                        if (!CanWrite)
                __Error.WriteNotSupported();
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                return BlockingBeginWrite(buffer, offset, count, callback, state);
            }

            var semaphore = EnsureAsyncActiveSemaphoreInitialized();
            Task semaphoreTask = null;
            if (serializeAsynchronously)
            {
                semaphoreTask = semaphore.WaitAsync();
            }
            else
            {
                semaphore.Wait();
            }

            var asyncResult = new ReadWriteTask(false, apm, delegate
            {
                var thisTask = Task.InternalCurrent as ReadWriteTask;
                                try
                {
                    thisTask._stream.Write(thisTask._buffer, thisTask._offset, thisTask._count);
                    return 0;
                }
                finally
                {
                    if (!thisTask._apm)
                    {
                        thisTask._stream.FinishTrackingAsyncOperation();
                    }

                    thisTask.ClearBeginState();
                }
            }

            , state, this, buffer, offset, count, callback);
            if (semaphoreTask != null)
                RunReadWriteTaskWhenReady(semaphoreTask, asyncResult);
            else
                RunReadWriteTask(asyncResult);
            return asyncResult;
        }

        private void RunReadWriteTaskWhenReady(Task asyncWaiter, ReadWriteTask readWriteTask)
        {
                                    if (asyncWaiter.IsCompleted)
            {
                                RunReadWriteTask(readWriteTask);
            }
            else
            {
                asyncWaiter.ContinueWith((t, state) =>
                {
                                        var rwt = (ReadWriteTask)state;
                    rwt._stream.RunReadWriteTask(rwt);
                }

                , readWriteTask, default (CancellationToken), TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private void RunReadWriteTask(ReadWriteTask readWriteTask)
        {
                                    _activeReadWriteTask = readWriteTask;
            readWriteTask.m_taskScheduler = TaskScheduler.Default;
            readWriteTask.ScheduleAndStart(needsProtection: false);
        }

        private void FinishTrackingAsyncOperation()
        {
            _activeReadWriteTask = null;
                        _asyncActiveSemaphore.Release();
        }

        public virtual void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");
                        if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                BlockingEndWrite(asyncResult);
                return;
            }

            var writeTask = _activeReadWriteTask;
            if (writeTask == null)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
            }
            else if (writeTask != asyncResult)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
            }
            else if (writeTask._isRead)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_WrongAsyncResultOrEndWriteCalledMultiple"));
            }

            try
            {
                writeTask.GetAwaiter().GetResult();
                            }
            finally
            {
                FinishTrackingAsyncOperation();
            }
        }

        private sealed class ReadWriteTask : Task<int>, ITaskCompletionAction
        {
            internal readonly bool _isRead;
            internal readonly bool _apm;
            internal Stream _stream;
            internal byte[] _buffer;
            internal readonly int _offset;
            internal readonly int _count;
            private AsyncCallback _callback;
            private ExecutionContext _context;
            internal void ClearBeginState()
            {
                _stream = null;
                _buffer = null;
            }

            public ReadWriteTask(bool isRead, bool apm, Func<object, int> function, object state, Stream stream, byte[] buffer, int offset, int count, AsyncCallback callback): base (function, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach)
            {
                                                                                StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
                _isRead = isRead;
                _apm = apm;
                _stream = stream;
                _buffer = buffer;
                _offset = offset;
                _count = count;
                if (callback != null)
                {
                    _callback = callback;
                    _context = ExecutionContext.Capture(ref stackMark, ExecutionContext.CaptureOptions.OptimizeDefaultCase | ExecutionContext.CaptureOptions.IgnoreSyncCtx);
                    base.AddCompletionAction(this);
                }
            }

            private static void InvokeAsyncCallback(object completedTask)
            {
                var rwc = (ReadWriteTask)completedTask;
                var callback = rwc._callback;
                rwc._callback = null;
                callback(rwc);
            }

            private static ContextCallback s_invokeAsyncCallback;
            void ITaskCompletionAction.Invoke(Task completingTask)
            {
                var context = _context;
                if (context == null)
                {
                    var callback = _callback;
                    _callback = null;
                    callback(completingTask);
                }
                else
                {
                    _context = null;
                    var invokeAsyncCallback = s_invokeAsyncCallback;
                    if (invokeAsyncCallback == null)
                        s_invokeAsyncCallback = invokeAsyncCallback = InvokeAsyncCallback;
                    using (context)
                        ExecutionContext.Run(context, invokeAsyncCallback, this, true);
                }
            }

            bool ITaskCompletionAction.InvokeMayRunArbitraryCode
            {
                get
                {
                    return true;
                }
            }
        }

        public Task WriteAsync(Byte[] buffer, int offset, int count)
        {
            return WriteAsync(buffer, offset, count, CancellationToken.None);
        }

        public virtual Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ? Task.FromCancellation(cancellationToken) : BeginEndWriteAsync(buffer, offset, count);
        }

        private extern bool HasOverriddenBeginEndWrite();
        private Task BeginEndWriteAsync(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (!HasOverriddenBeginEndWrite())
            {
                return (Task)BeginWriteInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
            }

            return TaskFactory<VoidTaskResult>.FromAsyncTrim(this, new ReadWriteParameters{Buffer = buffer, Offset = offset, Count = count}, (stream, args, callback, state) => stream.BeginWrite(args.Buffer, args.Offset, args.Count, callback, state), (stream, asyncResult) =>
            {
                stream.EndWrite(asyncResult);
                return default (VoidTaskResult);
            }

            );
        }

        public abstract long Seek(long offset, SeekOrigin origin);
        public abstract void SetLength(long value);
        public abstract int Read([In, Out] byte[] buffer, int offset, int count);
        public virtual int ReadByte()
        {
                                    byte[] oneByteArray = new byte[1];
            int r = Read(oneByteArray, 0, 1);
            if (r == 0)
                return -1;
            return oneByteArray[0];
        }

        public abstract void Write(byte[] buffer, int offset, int count);
        public virtual void WriteByte(byte value)
        {
            byte[] oneByteArray = new byte[1];
            oneByteArray[0] = value;
            Write(oneByteArray, 0, 1);
        }

        public static Stream Synchronized(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
                                    if (stream is SyncStream)
                return stream;
            return new SyncStream(stream);
        }

        protected virtual void ObjectInvariant()
        {
        }

        internal IAsyncResult BlockingBeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
                        SynchronousAsyncResult asyncResult;
            try
            {
                int numRead = Read(buffer, offset, count);
                asyncResult = new SynchronousAsyncResult(numRead, state);
            }
            catch (IOException ex)
            {
                asyncResult = new SynchronousAsyncResult(ex, state, isWrite: false);
            }

            if (callback != null)
            {
                callback(asyncResult);
            }

            return asyncResult;
        }

        internal static int BlockingEndRead(IAsyncResult asyncResult)
        {
                        return SynchronousAsyncResult.EndRead(asyncResult);
        }

        internal IAsyncResult BlockingBeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
        {
                        SynchronousAsyncResult asyncResult;
            try
            {
                Write(buffer, offset, count);
                asyncResult = new SynchronousAsyncResult(state);
            }
            catch (IOException ex)
            {
                asyncResult = new SynchronousAsyncResult(ex, state, isWrite: true);
            }

            if (callback != null)
            {
                callback(asyncResult);
            }

            return asyncResult;
        }

        internal static void BlockingEndWrite(IAsyncResult asyncResult)
        {
            SynchronousAsyncResult.EndWrite(asyncResult);
        }

        private sealed class NullStream : Stream
        {
            internal NullStream()
            {
            }

            public override bool CanRead
            {
                
                get
                {
                    return true;
                }
            }

            public override bool CanWrite
            {
                
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                
                get
                {
                    return true;
                }
            }

            public override long Length
            {
                get
                {
                    return 0;
                }
            }

            public override long Position
            {
                get
                {
                    return 0;
                }

                set
                {
                }
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override void Flush()
            {
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return cancellationToken.IsCancellationRequested ? Task.FromCancellation(cancellationToken) : Task.CompletedTask;
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
            {
                if (!CanRead)
                    __Error.ReadNotSupported();
                return BlockingBeginRead(buffer, offset, count, callback, state);
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                                return BlockingEndRead(asyncResult);
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
            {
                if (!CanWrite)
                    __Error.WriteNotSupported();
                return BlockingBeginWrite(buffer, offset, count, callback, state);
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                                BlockingEndWrite(asyncResult);
            }

            public override int Read([In, Out] byte[] buffer, int offset, int count)
            {
                return 0;
            }

            public override Task<int> ReadAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var nullReadTask = s_nullReadTask;
                if (nullReadTask == null)
                    s_nullReadTask = nullReadTask = new Task<int>(false, 0, (TaskCreationOptions)InternalTaskOptions.DoNotDispose, CancellationToken.None);
                return nullReadTask;
            }

            private static Task<int> s_nullReadTask;
            public override int ReadByte()
            {
                return -1;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }

            public override Task WriteAsync(Byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return cancellationToken.IsCancellationRequested ? Task.FromCancellation(cancellationToken) : Task.CompletedTask;
            }

            public override void WriteByte(byte value)
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return 0;
            }

            public override void SetLength(long length)
            {
            }
        }

        internal sealed class SynchronousAsyncResult : IAsyncResult
        {
            private readonly Object _stateObject;
            private readonly bool _isWrite;
            private ManualResetEvent _waitHandle;
            private ExceptionDispatchInfo _exceptionInfo;
            private bool _endXxxCalled;
            private Int32 _bytesRead;
            internal SynchronousAsyncResult(Int32 bytesRead, Object asyncStateObject)
            {
                _bytesRead = bytesRead;
                _stateObject = asyncStateObject;
            }

            internal SynchronousAsyncResult(Object asyncStateObject)
            {
                _stateObject = asyncStateObject;
                _isWrite = true;
            }

            internal SynchronousAsyncResult(Exception ex, Object asyncStateObject, bool isWrite)
            {
                _exceptionInfo = ExceptionDispatchInfo.Capture(ex);
                _stateObject = asyncStateObject;
                _isWrite = isWrite;
            }

            public bool IsCompleted
            {
                get
                {
                    return true;
                }
            }

            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    return LazyInitializer.EnsureInitialized(ref _waitHandle, () => new ManualResetEvent(true));
                }
            }

            public Object AsyncState
            {
                get
                {
                    return _stateObject;
                }
            }

            public bool CompletedSynchronously
            {
                get
                {
                    return true;
                }
            }

            internal void ThrowIfError()
            {
                if (_exceptionInfo != null)
                    _exceptionInfo.Throw();
            }

            internal static Int32 EndRead(IAsyncResult asyncResult)
            {
                SynchronousAsyncResult ar = asyncResult as SynchronousAsyncResult;
                if (ar == null || ar._isWrite)
                    __Error.WrongAsyncResult();
                if (ar._endXxxCalled)
                    __Error.EndReadCalledTwice();
                ar._endXxxCalled = true;
                ar.ThrowIfError();
                return ar._bytesRead;
            }

            internal static void EndWrite(IAsyncResult asyncResult)
            {
                SynchronousAsyncResult ar = asyncResult as SynchronousAsyncResult;
                if (ar == null || !ar._isWrite)
                    __Error.WrongAsyncResult();
                if (ar._endXxxCalled)
                    __Error.EndWriteCalledTwice();
                ar._endXxxCalled = true;
                ar.ThrowIfError();
            }
        }

        internal sealed class SyncStream : Stream, IDisposable
        {
            private Stream _stream;
            internal SyncStream(Stream stream)
            {
                if (stream == null)
                    throw new ArgumentNullException("stream");
                                _stream = stream;
            }

            public override bool CanRead
            {
                
                get
                {
                    return _stream.CanRead;
                }
            }

            public override bool CanWrite
            {
                
                get
                {
                    return _stream.CanWrite;
                }
            }

            public override bool CanSeek
            {
                
                get
                {
                    return _stream.CanSeek;
                }
            }

            public override bool CanTimeout
            {
                
                get
                {
                    return _stream.CanTimeout;
                }
            }

            public override long Length
            {
                get
                {
                    lock (_stream)
                    {
                        return _stream.Length;
                    }
                }
            }

            public override long Position
            {
                get
                {
                    lock (_stream)
                    {
                        return _stream.Position;
                    }
                }

                set
                {
                    lock (_stream)
                    {
                        _stream.Position = value;
                    }
                }
            }

            public override int ReadTimeout
            {
                get
                {
                    return _stream.ReadTimeout;
                }

                set
                {
                    _stream.ReadTimeout = value;
                }
            }

            public override int WriteTimeout
            {
                get
                {
                    return _stream.WriteTimeout;
                }

                set
                {
                    _stream.WriteTimeout = value;
                }
            }

            public override void Close()
            {
                lock (_stream)
                {
                    try
                    {
                        _stream.Close();
                    }
                    finally
                    {
                        base.Dispose(true);
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                lock (_stream)
                {
                    try
                    {
                        if (disposing)
                            ((IDisposable)_stream).Dispose();
                    }
                    finally
                    {
                        base.Dispose(disposing);
                    }
                }
            }

            public override void Flush()
            {
                lock (_stream)
                    _stream.Flush();
            }

            public override int Read([In, Out] byte[] bytes, int offset, int count)
            {
                lock (_stream)
                    return _stream.Read(bytes, offset, count);
            }

            public override int ReadByte()
            {
                lock (_stream)
                    return _stream.ReadByte();
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
            {
                bool overridesBeginRead = _stream.HasOverriddenBeginEndRead();
                lock (_stream)
                {
                    return overridesBeginRead ? _stream.BeginRead(buffer, offset, count, callback, state) : _stream.BeginReadInternal(buffer, offset, count, callback, state, serializeAsynchronously: true, apm: true);
                }
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                                                lock (_stream)
                    return _stream.EndRead(asyncResult);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                lock (_stream)
                    return _stream.Seek(offset, origin);
            }

            public override void SetLength(long length)
            {
                lock (_stream)
                    _stream.SetLength(length);
            }

            public override void Write(byte[] bytes, int offset, int count)
            {
                lock (_stream)
                    _stream.Write(bytes, offset, count);
            }

            public override void WriteByte(byte b)
            {
                lock (_stream)
                    _stream.WriteByte(b);
            }

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, Object state)
            {
                bool overridesBeginWrite = _stream.HasOverriddenBeginEndWrite();
                lock (_stream)
                {
                    return overridesBeginWrite ? _stream.BeginWrite(buffer, offset, count, callback, state) : _stream.BeginWriteInternal(buffer, offset, count, callback, state, serializeAsynchronously: true, apm: true);
                }
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                if (asyncResult == null)
                    throw new ArgumentNullException("asyncResult");
                                lock (_stream)
                    _stream.EndWrite(asyncResult);
            }
        }
    }
}