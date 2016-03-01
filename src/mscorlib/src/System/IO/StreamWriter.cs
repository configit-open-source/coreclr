using System;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Permissions;
using System.Runtime.Serialization;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.IO
{
    public class StreamWriter : TextWriter
    {
        internal const int DefaultBufferSize = 1024;
        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;
        private const Int32 DontCopyOnWriteLineThreshold = 512;
        public new static readonly StreamWriter Null = new StreamWriter(Stream.Null, new UTF8Encoding(false, true), MinBufferSize, true);
        private Stream stream;
        private Encoding encoding;
        private Encoder encoder;
        private byte[] byteBuffer;
        private char[] charBuffer;
        private int charPos;
        private int charLen;
        private bool autoFlush;
        private bool haveWrittenPreamble;
        private bool closable;
        private volatile Task _asyncWriteTask;
        private void CheckAsyncTaskInProgress()
        {
            Task t = _asyncWriteTask;
            if (t != null && !t.IsCompleted)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsyncIOInProgress"));
        }

        private static volatile Encoding _UTF8NoBOM;
        internal static Encoding UTF8NoBOM
        {
            [FriendAccessAllowed]
            get
            {
                if (_UTF8NoBOM == null)
                {
                    UTF8Encoding noBOM = new UTF8Encoding(false, true);
                    Thread.MemoryBarrier();
                    _UTF8NoBOM = noBOM;
                }

                return _UTF8NoBOM;
            }
        }

        internal StreamWriter(): base (null)
        {
        }

        public StreamWriter(Stream stream): this (stream, UTF8NoBOM, DefaultBufferSize, false)
        {
        }

        public StreamWriter(Stream stream, Encoding encoding): this (stream, encoding, DefaultBufferSize, false)
        {
        }

        public StreamWriter(Stream stream, Encoding encoding, int bufferSize): this (stream, encoding, bufferSize, false)
        {
        }

        public StreamWriter(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen): base (null)
        {
            if (stream == null || encoding == null)
                throw new ArgumentNullException((stream == null ? "stream" : "encoding"));
            if (!stream.CanWrite)
                throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            Contract.EndContractBlock();
            Init(stream, encoding, bufferSize, leaveOpen);
        }

        public StreamWriter(String path): this (path, false, UTF8NoBOM, DefaultBufferSize)
        {
        }

        public StreamWriter(String path, bool append): this (path, append, UTF8NoBOM, DefaultBufferSize)
        {
        }

        public StreamWriter(String path, bool append, Encoding encoding): this (path, append, encoding, DefaultBufferSize)
        {
        }

        public StreamWriter(String path, bool append, Encoding encoding, int bufferSize): this (path, append, encoding, bufferSize, true)
        {
        }

        internal StreamWriter(String path, bool append, Encoding encoding, int bufferSize, bool checkHost): base (null)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            Contract.EndContractBlock();
            Stream stream = CreateFile(path, append, checkHost);
            Init(stream, encoding, bufferSize, false);
        }

        private void Init(Stream streamArg, Encoding encodingArg, int bufferSize, bool shouldLeaveOpen)
        {
            this.stream = streamArg;
            this.encoding = encodingArg;
            this.encoder = encoding.GetEncoder();
            if (bufferSize < MinBufferSize)
                bufferSize = MinBufferSize;
            charBuffer = new char[bufferSize];
            byteBuffer = new byte[encoding.GetMaxByteCount(bufferSize)];
            charLen = bufferSize;
            if (stream.CanSeek && stream.Position > 0)
                haveWrittenPreamble = true;
            closable = !shouldLeaveOpen;
        }

        private static Stream CreateFile(String path, bool append, bool checkHost)
        {
            FileMode mode = append ? FileMode.Append : FileMode.Create;
            FileStream f = new FileStream(path, mode, FileAccess.Write, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.SequentialScan, Path.GetFileName(path), false, false, checkHost);
            return f;
        }

        public override void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (stream != null)
                {
                    if (disposing || (LeaveOpen && stream is __ConsoleStream))
                    {
                        CheckAsyncTaskInProgress();
                        Flush(true, true);
                    }
                }
            }
            finally
            {
                if (!LeaveOpen && stream != null)
                {
                    try
                    {
                        if (disposing)
                            stream.Close();
                    }
                    finally
                    {
                        stream = null;
                        byteBuffer = null;
                        charBuffer = null;
                        encoding = null;
                        encoder = null;
                        charLen = 0;
                        base.Dispose(disposing);
                    }
                }
            }
        }

        public override void Flush()
        {
            CheckAsyncTaskInProgress();
            Flush(true, true);
        }

        private void Flush(bool flushStream, bool flushEncoder)
        {
            if (stream == null)
                __Error.WriterClosed();
            if (charPos == 0 && ((!flushStream && !flushEncoder) || CompatibilitySwitches.IsAppEarlierThanWindowsPhone8))
                return;
            if (!haveWrittenPreamble)
            {
                haveWrittenPreamble = true;
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length > 0)
                    stream.Write(preamble, 0, preamble.Length);
            }

            int count = encoder.GetBytes(charBuffer, 0, charPos, byteBuffer, 0, flushEncoder);
            charPos = 0;
            if (count > 0)
                stream.Write(byteBuffer, 0, count);
            if (flushStream)
                stream.Flush();
        }

        public virtual bool AutoFlush
        {
            get
            {
                return autoFlush;
            }

            set
            {
                CheckAsyncTaskInProgress();
                autoFlush = value;
                if (value)
                    Flush(true, false);
            }
        }

        public virtual Stream BaseStream
        {
            get
            {
                return stream;
            }
        }

        internal bool LeaveOpen
        {
            get
            {
                return !closable;
            }
        }

        internal bool HaveWrittenPreamble
        {
            set
            {
                haveWrittenPreamble = value;
            }
        }

        public override Encoding Encoding
        {
            get
            {
                return encoding;
            }
        }

        public override void Write(char value)
        {
            CheckAsyncTaskInProgress();
            if (charPos == charLen)
                Flush(false, false);
            charBuffer[charPos] = value;
            charPos++;
            if (autoFlush)
                Flush(true, false);
        }

        public override void Write(char[] buffer)
        {
            if (buffer == null)
                return;
            CheckAsyncTaskInProgress();
            int index = 0;
            int count = buffer.Length;
            while (count > 0)
            {
                if (charPos == charLen)
                    Flush(false, false);
                int n = charLen - charPos;
                if (n > count)
                    n = count;
                Contract.Assert(n > 0, "StreamWriter::Write(char[]) isn't making progress!  This is most likely a race condition in user code.");
                Buffer.InternalBlockCopy(buffer, index * sizeof (char), charBuffer, charPos * sizeof (char), n * sizeof (char));
                charPos += n;
                index += n;
                count -= n;
            }

            if (autoFlush)
                Flush(true, false);
        }

        public override void Write(char[] buffer, int index, int count)
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
            CheckAsyncTaskInProgress();
            while (count > 0)
            {
                if (charPos == charLen)
                    Flush(false, false);
                int n = charLen - charPos;
                if (n > count)
                    n = count;
                Contract.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");
                Buffer.InternalBlockCopy(buffer, index * sizeof (char), charBuffer, charPos * sizeof (char), n * sizeof (char));
                charPos += n;
                index += n;
                count -= n;
            }

            if (autoFlush)
                Flush(true, false);
        }

        public override void Write(String value)
        {
            if (value != null)
            {
                CheckAsyncTaskInProgress();
                int count = value.Length;
                int index = 0;
                while (count > 0)
                {
                    if (charPos == charLen)
                        Flush(false, false);
                    int n = charLen - charPos;
                    if (n > count)
                        n = count;
                    Contract.Assert(n > 0, "StreamWriter::Write(String) isn't making progress!  This is most likely a race condition in user code.");
                    value.CopyTo(index, charBuffer, charPos, n);
                    charPos += n;
                    index += n;
                    count -= n;
                }

                if (autoFlush)
                    Flush(true, false);
            }
        }

        public override Task WriteAsync(char value)
        {
            if (this.GetType() != typeof (StreamWriter))
                return base.WriteAsync(value);
            if (stream == null)
                __Error.WriterClosed();
            CheckAsyncTaskInProgress();
            Task task = WriteAsyncInternal(this, value, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: false);
            _asyncWriteTask = task;
            return task;
        }

        private static async Task WriteAsyncInternal(StreamWriter _this, Char value, Char[] charBuffer, Int32 charPos, Int32 charLen, Char[] coreNewLine, bool autoFlush, bool appendNewLine)
        {
            if (charPos == charLen)
            {
                await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                Contract.Assert(_this.charPos == 0);
                charPos = 0;
            }

            charBuffer[charPos] = value;
            charPos++;
            if (appendNewLine)
            {
                for (Int32 i = 0; i < coreNewLine.Length; i++)
                {
                    if (charPos == charLen)
                    {
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                        Contract.Assert(_this.charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }

            if (autoFlush)
            {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
                Contract.Assert(_this.charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        public override Task WriteAsync(String value)
        {
            if (this.GetType() != typeof (StreamWriter))
                return base.WriteAsync(value);
            if (value != null)
            {
                if (stream == null)
                    __Error.WriterClosed();
                CheckAsyncTaskInProgress();
                Task task = WriteAsyncInternal(this, value, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: false);
                _asyncWriteTask = task;
                return task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private static async Task WriteAsyncInternal(StreamWriter _this, String value, Char[] charBuffer, Int32 charPos, Int32 charLen, Char[] coreNewLine, bool autoFlush, bool appendNewLine)
        {
            Contract.Requires(value != null);
            int count = value.Length;
            int index = 0;
            while (count > 0)
            {
                if (charPos == charLen)
                {
                    await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                    Contract.Assert(_this.charPos == 0);
                    charPos = 0;
                }

                int n = charLen - charPos;
                if (n > count)
                    n = count;
                Contract.Assert(n > 0, "StreamWriter::Write(String) isn't making progress!  This is most likely a race condition in user code.");
                value.CopyTo(index, charBuffer, charPos, n);
                charPos += n;
                index += n;
                count -= n;
            }

            if (appendNewLine)
            {
                for (Int32 i = 0; i < coreNewLine.Length; i++)
                {
                    if (charPos == charLen)
                    {
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                        Contract.Assert(_this.charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }

            if (autoFlush)
            {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
                Contract.Assert(_this.charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
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
            if (this.GetType() != typeof (StreamWriter))
                return base.WriteAsync(buffer, index, count);
            if (stream == null)
                __Error.WriterClosed();
            CheckAsyncTaskInProgress();
            Task task = WriteAsyncInternal(this, buffer, index, count, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: false);
            _asyncWriteTask = task;
            return task;
        }

        private static async Task WriteAsyncInternal(StreamWriter _this, Char[] buffer, Int32 index, Int32 count, Char[] charBuffer, Int32 charPos, Int32 charLen, Char[] coreNewLine, bool autoFlush, bool appendNewLine)
        {
            Contract.Requires(count == 0 || (count > 0 && buffer != null));
            Contract.Requires(index >= 0);
            Contract.Requires(count >= 0);
            Contract.Requires(buffer == null || (buffer != null && buffer.Length - index >= count));
            while (count > 0)
            {
                if (charPos == charLen)
                {
                    await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                    Contract.Assert(_this.charPos == 0);
                    charPos = 0;
                }

                int n = charLen - charPos;
                if (n > count)
                    n = count;
                Contract.Assert(n > 0, "StreamWriter::Write(char[], int, int) isn't making progress!  This is most likely a race condition in user code.");
                Buffer.InternalBlockCopy(buffer, index * sizeof (char), charBuffer, charPos * sizeof (char), n * sizeof (char));
                charPos += n;
                index += n;
                count -= n;
            }

            if (appendNewLine)
            {
                for (Int32 i = 0; i < coreNewLine.Length; i++)
                {
                    if (charPos == charLen)
                    {
                        await _this.FlushAsyncInternal(false, false, charBuffer, charPos).ConfigureAwait(false);
                        Contract.Assert(_this.charPos == 0);
                        charPos = 0;
                    }

                    charBuffer[charPos] = coreNewLine[i];
                    charPos++;
                }
            }

            if (autoFlush)
            {
                await _this.FlushAsyncInternal(true, false, charBuffer, charPos).ConfigureAwait(false);
                Contract.Assert(_this.charPos == 0);
                charPos = 0;
            }

            _this.CharPos_Prop = charPos;
        }

        public override Task WriteLineAsync()
        {
            if (this.GetType() != typeof (StreamWriter))
                return base.WriteLineAsync();
            if (stream == null)
                __Error.WriterClosed();
            CheckAsyncTaskInProgress();
            Task task = WriteAsyncInternal(this, null, 0, 0, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: true);
            _asyncWriteTask = task;
            return task;
        }

        public override Task WriteLineAsync(char value)
        {
            if (this.GetType() != typeof (StreamWriter))
                return base.WriteLineAsync(value);
            if (stream == null)
                __Error.WriterClosed();
            CheckAsyncTaskInProgress();
            Task task = WriteAsyncInternal(this, value, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: true);
            _asyncWriteTask = task;
            return task;
        }

        public override Task WriteLineAsync(String value)
        {
            if (this.GetType() != typeof (StreamWriter))
                return base.WriteLineAsync(value);
            if (stream == null)
                __Error.WriterClosed();
            CheckAsyncTaskInProgress();
            Task task = WriteAsyncInternal(this, value, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: true);
            _asyncWriteTask = task;
            return task;
        }

        public override Task WriteLineAsync(char[] buffer, int index, int count)
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
            if (this.GetType() != typeof (StreamWriter))
                return base.WriteLineAsync(buffer, index, count);
            if (stream == null)
                __Error.WriterClosed();
            CheckAsyncTaskInProgress();
            Task task = WriteAsyncInternal(this, buffer, index, count, charBuffer, charPos, charLen, CoreNewLine, autoFlush, appendNewLine: true);
            _asyncWriteTask = task;
            return task;
        }

        public override Task FlushAsync()
        {
            if (this.GetType() != typeof (StreamWriter))
                return base.FlushAsync();
            if (stream == null)
                __Error.WriterClosed();
            CheckAsyncTaskInProgress();
            Task task = FlushAsyncInternal(true, true, charBuffer, charPos);
            _asyncWriteTask = task;
            return task;
        }

        private Int32 CharPos_Prop
        {
            set
            {
                this.charPos = value;
            }
        }

        private bool HaveWrittenPreamble_Prop
        {
            set
            {
                this.haveWrittenPreamble = value;
            }
        }

        private Task FlushAsyncInternal(bool flushStream, bool flushEncoder, Char[] sCharBuffer, Int32 sCharPos)
        {
            if (sCharPos == 0 && !flushStream && !flushEncoder)
                return Task.CompletedTask;
            Task flushTask = FlushAsyncInternal(this, flushStream, flushEncoder, sCharBuffer, sCharPos, this.haveWrittenPreamble, this.encoding, this.encoder, this.byteBuffer, this.stream);
            this.charPos = 0;
            return flushTask;
        }

        private static async Task FlushAsyncInternal(StreamWriter _this, bool flushStream, bool flushEncoder, Char[] charBuffer, Int32 charPos, bool haveWrittenPreamble, Encoding encoding, Encoder encoder, Byte[] byteBuffer, Stream stream)
        {
            if (!haveWrittenPreamble)
            {
                _this.HaveWrittenPreamble_Prop = true;
                byte[] preamble = encoding.GetPreamble();
                if (preamble.Length > 0)
                    await stream.WriteAsync(preamble, 0, preamble.Length).ConfigureAwait(false);
            }

            int count = encoder.GetBytes(charBuffer, 0, charPos, byteBuffer, 0, flushEncoder);
            if (count > 0)
                await stream.WriteAsync(byteBuffer, 0, count).ConfigureAwait(false);
            if (flushStream)
                await stream.FlushAsync().ConfigureAwait(false);
        }
    }
}