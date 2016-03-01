using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public class StreamReader : TextReader
    {
        public new static readonly StreamReader Null = new NullStreamReader();
        private static byte[] s_utf8Preamble;
        internal static int DefaultBufferSize
        {
            get
            {
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    return 4096;
                }

                return 1024;
            }
        }

        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;
        private const int MaxSharedBuilderCapacity = 360;
        private Stream stream;
        private Encoding encoding;
        private Decoder decoder;
        private byte[] byteBuffer;
        private char[] charBuffer;
        private byte[] _preamble;
        private int charPos;
        private int charLen;
        private int byteLen;
        private int bytePos;
        private StringBuilder _builder;
        private int _maxCharsPerBuffer;
        private bool _detectEncoding;
        private bool _checkPreamble;
        private bool _isBlocked;
        private bool _closable;
        private volatile Task _asyncReadTask;
        private void CheckAsyncTaskInProgress()
        {
            Task t = _asyncReadTask;
            if (t != null && !t.IsCompleted)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsyncIOInProgress"));
        }

        internal StreamReader()
        {
        }

        public StreamReader(Stream stream): this (stream, true)
        {
        }

        public StreamReader(Stream stream, bool detectEncodingFromByteOrderMarks): this (stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize, false)
        {
        }

        public StreamReader(Stream stream, Encoding encoding): this (stream, encoding, true, DefaultBufferSize, false)
        {
        }

        public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks): this (stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, false)
        {
        }

        public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize): this (stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false)
        {
        }

        public StreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            if (stream == null || encoding == null)
                throw new ArgumentNullException((stream == null ? "stream" : "encoding"));
            if (!stream.CanRead)
                throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotReadable"));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            Contract.EndContractBlock();
            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen);
        }

        public StreamReader(String path): this (path, true)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if (callingAssembly != null && !callingAssembly.IsProfileAssembly)
                {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(System.Globalization.CultureInfo.CurrentCulture, Environment.GetResourceString("Arg_MethodAccessException_WithCaller"), caller, callee));
                }
            }
        }

        public StreamReader(String path, bool detectEncodingFromByteOrderMarks): this (path, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        public StreamReader(String path, Encoding encoding): this (path, encoding, true, DefaultBufferSize)
        {
        }

        public StreamReader(String path, Encoding encoding, bool detectEncodingFromByteOrderMarks): this (path, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        public StreamReader(String path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize): this (path, encoding, detectEncodingFromByteOrderMarks, bufferSize, true)
        {
        }

        internal StreamReader(String path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool checkHost)
        {
            if (path == null || encoding == null)
                throw new ArgumentNullException((path == null ? "path" : "encoding"));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyPath"));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            Contract.EndContractBlock();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.SequentialScan, Path.GetFileName(path), false, false, checkHost);
            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false);
        }

        private void Init(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            this.stream = stream;
            this.encoding = encoding;
            decoder = encoding.GetDecoder();
            if (bufferSize < MinBufferSize)
                bufferSize = MinBufferSize;
            byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            charBuffer = new char[_maxCharsPerBuffer];
            byteLen = 0;
            bytePos = 0;
            _detectEncoding = detectEncodingFromByteOrderMarks;
            _preamble = object.ReferenceEquals(encoding, Encoding.UTF8) ? (s_utf8Preamble ?? (s_utf8Preamble = encoding.GetPreamble())) : encoding.GetPreamble();
            _checkPreamble = (_preamble.Length > 0);
            _isBlocked = false;
            _closable = !leaveOpen;
        }

        internal void Init(Stream stream)
        {
            this.stream = stream;
            _closable = true;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!LeaveOpen && disposing && (stream != null))
                    stream.Close();
            }
            finally
            {
                if (!LeaveOpen && (stream != null))
                {
                    stream = null;
                    encoding = null;
                    decoder = null;
                    byteBuffer = null;
                    charBuffer = null;
                    charPos = 0;
                    charLen = 0;
                    _builder = null;
                    base.Dispose(disposing);
                }
            }
        }

        public virtual Encoding CurrentEncoding
        {
            get
            {
                return encoding;
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
                return !_closable;
            }
        }

        public void DiscardBufferedData()
        {
            CheckAsyncTaskInProgress();
            byteLen = 0;
            charLen = 0;
            charPos = 0;
            if (encoding != null)
            {
                decoder = encoding.GetDecoder();
            }

            _isBlocked = false;
        }

        public bool EndOfStream
        {
            get
            {
                if (stream == null)
                    __Error.ReaderClosed();
                CheckAsyncTaskInProgress();
                if (charPos < charLen)
                    return false;
                int numRead = ReadBuffer();
                return numRead == 0;
            }
        }

        public override int Peek()
        {
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            if (charPos == charLen)
            {
                if (_isBlocked || ReadBuffer() == 0)
                    return -1;
            }

            return charBuffer[charPos];
        }

        public override int Read()
        {
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            if (charPos == charLen)
            {
                if (ReadBuffer() == 0)
                    return -1;
            }

            int result = charBuffer[charPos];
            charPos++;
            return result;
        }

        public override int Read([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            int charsRead = 0;
            bool readToUserBuffer = false;
            while (count > 0)
            {
                int n = charLen - charPos;
                if (n == 0)
                    n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);
                if (n == 0)
                    break;
                if (n > count)
                    n = count;
                if (!readToUserBuffer)
                {
                    Buffer.InternalBlockCopy(charBuffer, charPos * 2, buffer, (index + charsRead) * 2, n * 2);
                    charPos += n;
                }

                charsRead += n;
                count -= n;
                if (_isBlocked)
                    break;
            }

            return charsRead;
        }

        public override String ReadToEnd()
        {
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            StringBuilder sb = AcquireSharedStringBuilder(charLen - charPos);
            do
            {
                sb.Append(charBuffer, charPos, charLen - charPos);
                charPos = charLen;
                ReadBuffer();
            }
            while (charLen > 0);
            return GetStringAndReleaseSharedStringBuilder(sb);
        }

        public override int ReadBlock([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            return base.ReadBlock(buffer, index, count);
        }

        private void CompressBuffer(int n)
        {
            Contract.Assert(byteLen >= n, "CompressBuffer was called with a number of bytes greater than the current buffer length.  Are two threads using this StreamReader at the same time?");
            Buffer.InternalBlockCopy(byteBuffer, n, byteBuffer, 0, byteLen - n);
            byteLen -= n;
        }

        private void DetectEncoding()
        {
            if (byteLen < 2)
                return;
            _detectEncoding = false;
            bool changedEncoding = false;
            if (byteBuffer[0] == 0xFE && byteBuffer[1] == 0xFF)
            {
                encoding = Encoding.BigEndianUnicode;
                CompressBuffer(2);
                changedEncoding = true;
            }
            else if (byteBuffer[0] == 0xFF && byteBuffer[1] == 0xFE)
            {
                if (byteLen < 4 || byteBuffer[2] != 0 || byteBuffer[3] != 0)
                {
                    encoding = Encoding.Unicode;
                    CompressBuffer(2);
                    changedEncoding = true;
                }
                else
                {
                    encoding = Encoding.UTF32;
                    CompressBuffer(4);
                    changedEncoding = true;
                }
            }
            else if (byteLen >= 3 && byteBuffer[0] == 0xEF && byteBuffer[1] == 0xBB && byteBuffer[2] == 0xBF)
            {
                encoding = Encoding.UTF8;
                CompressBuffer(3);
                changedEncoding = true;
            }
            else if (byteLen >= 4 && byteBuffer[0] == 0 && byteBuffer[1] == 0 && byteBuffer[2] == 0xFE && byteBuffer[3] == 0xFF)
            {
                encoding = new UTF32Encoding(true, true);
                CompressBuffer(4);
                changedEncoding = true;
            }
            else if (byteLen == 2)
                _detectEncoding = true;
            if (changedEncoding)
            {
                decoder = encoding.GetDecoder();
                _maxCharsPerBuffer = encoding.GetMaxCharCount(byteBuffer.Length);
                charBuffer = new char[_maxCharsPerBuffer];
            }
        }

        private bool IsPreamble()
        {
            if (!_checkPreamble)
                return _checkPreamble;
            Contract.Assert(bytePos <= _preamble.Length, "_compressPreamble was called with the current bytePos greater than the preamble buffer length.  Are two threads using this StreamReader at the same time?");
            int len = (byteLen >= (_preamble.Length)) ? (_preamble.Length - bytePos) : (byteLen - bytePos);
            for (int i = 0; i < len; i++, bytePos++)
            {
                if (byteBuffer[bytePos] != _preamble[bytePos])
                {
                    bytePos = 0;
                    _checkPreamble = false;
                    break;
                }
            }

            Contract.Assert(bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
            if (_checkPreamble)
            {
                if (bytePos == _preamble.Length)
                {
                    CompressBuffer(_preamble.Length);
                    bytePos = 0;
                    _checkPreamble = false;
                    _detectEncoding = false;
                }
            }

            return _checkPreamble;
        }

        private StringBuilder AcquireSharedStringBuilder(int capacity)
        {
            if (capacity > MaxSharedBuilderCapacity)
                return new StringBuilder(capacity);
            StringBuilder sb = _builder;
            if (sb == null)
                return _builder = new StringBuilder(capacity);
            sb.Length = 0;
            if (sb.Capacity < capacity)
                sb.Capacity = capacity;
            return sb;
        }

        private string GetStringAndReleaseSharedStringBuilder(StringBuilder sb)
        {
            if (sb == _builder && sb.Capacity > MaxSharedBuilderCapacity)
                _builder = null;
            return sb.ToString();
        }

        internal virtual int ReadBuffer()
        {
            charLen = 0;
            charPos = 0;
            if (!_checkPreamble)
                byteLen = 0;
            do
            {
                if (_checkPreamble)
                {
                    Contract.Assert(bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                    int len = stream.Read(byteBuffer, bytePos, byteBuffer.Length - bytePos);
                    Contract.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");
                    if (len == 0)
                    {
                        if (byteLen > 0)
                        {
                            charLen += decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charLen);
                            bytePos = byteLen = 0;
                        }

                        return charLen;
                    }

                    byteLen += len;
                }
                else
                {
                    Contract.Assert(bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");
                    byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);
                    Contract.Assert(byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");
                    if (byteLen == 0)
                        return charLen;
                }

                _isBlocked = (byteLen < byteBuffer.Length);
                if (IsPreamble())
                    continue;
                if (_detectEncoding && byteLen >= 2)
                    DetectEncoding();
                charLen += decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charLen);
            }
            while (charLen == 0);
            return charLen;
        }

        private int ReadBuffer(char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer)
        {
            charLen = 0;
            charPos = 0;
            if (!_checkPreamble)
                byteLen = 0;
            int charsRead = 0;
            readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
            do
            {
                Contract.Assert(charsRead == 0);
                if (_checkPreamble)
                {
                    Contract.Assert(bytePos <= _preamble.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                    int len = stream.Read(byteBuffer, bytePos, byteBuffer.Length - bytePos);
                    Contract.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");
                    if (len == 0)
                    {
                        if (byteLen > 0)
                        {
                            if (readToUserBuffer)
                            {
                                charsRead = decoder.GetChars(byteBuffer, 0, byteLen, userBuffer, userOffset + charsRead);
                                charLen = 0;
                            }
                            else
                            {
                                charsRead = decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charsRead);
                                charLen += charsRead;
                            }
                        }

                        return charsRead;
                    }

                    byteLen += len;
                }
                else
                {
                    Contract.Assert(bytePos == 0, "bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");
                    byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);
                    Contract.Assert(byteLen >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");
                    if (byteLen == 0)
                        break;
                }

                _isBlocked = (byteLen < byteBuffer.Length);
                if (IsPreamble())
                    continue;
                if (_detectEncoding && byteLen >= 2)
                {
                    DetectEncoding();
                    readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
                }

                charPos = 0;
                if (readToUserBuffer)
                {
                    charsRead += decoder.GetChars(byteBuffer, 0, byteLen, userBuffer, userOffset + charsRead);
                    charLen = 0;
                }
                else
                {
                    charsRead = decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charsRead);
                    charLen += charsRead;
                }
            }
            while (charsRead == 0);
            _isBlocked &= charsRead < desiredChars;
            return charsRead;
        }

        public override String ReadLine()
        {
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            if (charPos == charLen)
            {
                if (ReadBuffer() == 0)
                    return null;
            }

            StringBuilder sb = null;
            do
            {
                int i = charPos;
                do
                {
                    char ch = charBuffer[i];
                    if (ch == '\r' || ch == '\n')
                    {
                        String s;
                        if (sb != null)
                        {
                            sb.Append(charBuffer, charPos, i - charPos);
                            s = GetStringAndReleaseSharedStringBuilder(sb);
                        }
                        else
                        {
                            s = new String(charBuffer, charPos, i - charPos);
                        }

                        charPos = i + 1;
                        if (ch == '\r' && (charPos < charLen || ReadBuffer() > 0))
                        {
                            if (charBuffer[charPos] == '\n')
                                charPos++;
                        }

                        return s;
                    }

                    i++;
                }
                while (i < charLen);
                i = charLen - charPos;
                if (sb == null)
                    sb = AcquireSharedStringBuilder(i + 80);
                sb.Append(charBuffer, charPos, i);
            }
            while (ReadBuffer() > 0);
            return GetStringAndReleaseSharedStringBuilder(sb);
        }

        public override Task<String> ReadLineAsync()
        {
            if (this.GetType() != typeof (StreamReader))
                return base.ReadLineAsync();
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            Task<String> task = ReadLineAsyncInternal();
            _asyncReadTask = task;
            return task;
        }

        private async Task<String> ReadLineAsyncInternal()
        {
            if (CharPos_Prop == CharLen_Prop && (await ReadBufferAsync().ConfigureAwait(false)) == 0)
                return null;
            StringBuilder sb = null;
            do
            {
                char[] tmpCharBuffer = CharBuffer_Prop;
                int tmpCharLen = CharLen_Prop;
                int tmpCharPos = CharPos_Prop;
                int i = tmpCharPos;
                do
                {
                    char ch = tmpCharBuffer[i];
                    if (ch == '\r' || ch == '\n')
                    {
                        String s;
                        if (sb != null)
                        {
                            sb.Append(tmpCharBuffer, tmpCharPos, i - tmpCharPos);
                            s = GetStringAndReleaseSharedStringBuilder(sb);
                        }
                        else
                        {
                            s = new String(tmpCharBuffer, tmpCharPos, i - tmpCharPos);
                        }

                        CharPos_Prop = tmpCharPos = i + 1;
                        if (ch == '\r' && (tmpCharPos < tmpCharLen || (await ReadBufferAsync().ConfigureAwait(false)) > 0))
                        {
                            tmpCharPos = CharPos_Prop;
                            if (CharBuffer_Prop[tmpCharPos] == '\n')
                                CharPos_Prop = ++tmpCharPos;
                        }

                        return s;
                    }

                    i++;
                }
                while (i < tmpCharLen);
                i = tmpCharLen - tmpCharPos;
                if (sb == null)
                    sb = AcquireSharedStringBuilder(i + 80);
                sb.Append(tmpCharBuffer, tmpCharPos, i);
            }
            while (await ReadBufferAsync().ConfigureAwait(false) > 0);
            return GetStringAndReleaseSharedStringBuilder(sb);
        }

        public override Task<String> ReadToEndAsync()
        {
            if (this.GetType() != typeof (StreamReader))
                return base.ReadToEndAsync();
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            Task<String> task = ReadToEndAsyncInternal();
            _asyncReadTask = task;
            return task;
        }

        private async Task<String> ReadToEndAsyncInternal()
        {
            StringBuilder sb = AcquireSharedStringBuilder(CharLen_Prop - CharPos_Prop);
            do
            {
                int tmpCharPos = CharPos_Prop;
                sb.Append(CharBuffer_Prop, tmpCharPos, CharLen_Prop - tmpCharPos);
                CharPos_Prop = CharLen_Prop;
                await ReadBufferAsync().ConfigureAwait(false);
            }
            while (CharLen_Prop > 0);
            return GetStringAndReleaseSharedStringBuilder(sb);
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (this.GetType() != typeof (StreamReader))
                return base.ReadAsync(buffer, index, count);
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            Task<int> task = ReadAsyncInternal(buffer, index, count);
            _asyncReadTask = task;
            return task;
        }

        internal override async Task<int> ReadAsyncInternal(char[] buffer, int index, int count)
        {
            if (CharPos_Prop == CharLen_Prop && (await ReadBufferAsync().ConfigureAwait(false)) == 0)
                return 0;
            int charsRead = 0;
            bool readToUserBuffer = false;
            Byte[] tmpByteBuffer = ByteBuffer_Prop;
            Stream tmpStream = Stream_Prop;
            while (count > 0)
            {
                int n = CharLen_Prop - CharPos_Prop;
                if (n == 0)
                {
                    CharLen_Prop = 0;
                    CharPos_Prop = 0;
                    if (!CheckPreamble_Prop)
                        ByteLen_Prop = 0;
                    readToUserBuffer = count >= MaxCharsPerBuffer_Prop;
                    do
                    {
                        Contract.Assert(n == 0);
                        if (CheckPreamble_Prop)
                        {
                            Contract.Assert(BytePos_Prop <= Preamble_Prop.Length, "possible bug in _compressPreamble.  Are two threads using this StreamReader at the same time?");
                            int tmpBytePos = BytePos_Prop;
                            int len = await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos).ConfigureAwait(false);
                            Contract.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");
                            if (len == 0)
                            {
                                if (ByteLen_Prop > 0)
                                {
                                    if (readToUserBuffer)
                                    {
                                        n = Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, buffer, index + charsRead);
                                        CharLen_Prop = 0;
                                    }
                                    else
                                    {
                                        n = Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, 0);
                                        CharLen_Prop += n;
                                    }
                                }

                                Contract.Assert(n == 0);
                                IsBlocked_Prop = true;
                                break;
                            }
                            else
                            {
                                ByteLen_Prop += len;
                            }
                        }
                        else
                        {
                            Contract.Assert(BytePos_Prop == 0, "_bytePos can be non zero only when we are trying to _checkPreamble.  Are two threads using this StreamReader at the same time?");
                            ByteLen_Prop = await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length).ConfigureAwait(false);
                            Contract.Assert(ByteLen_Prop >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");
                            if (ByteLen_Prop == 0)
                            {
                                IsBlocked_Prop = true;
                                break;
                            }
                        }

                        IsBlocked_Prop = (ByteLen_Prop < tmpByteBuffer.Length);
                        if (IsPreamble())
                            continue;
                        if (DetectEncoding_Prop && ByteLen_Prop >= 2)
                        {
                            DetectEncoding();
                            readToUserBuffer = count >= MaxCharsPerBuffer_Prop;
                        }

                        Contract.Assert(n == 0);
                        CharPos_Prop = 0;
                        if (readToUserBuffer)
                        {
                            n += Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, buffer, index + charsRead);
                            Contract.Assert(n > 0);
                            CharLen_Prop = 0;
                        }
                        else
                        {
                            n = Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, 0);
                            Contract.Assert(n > 0);
                            CharLen_Prop += n;
                        }
                    }
                    while (n == 0);
                    if (n == 0)
                        break;
                }

                if (n > count)
                    n = count;
                if (!readToUserBuffer)
                {
                    Buffer.InternalBlockCopy(CharBuffer_Prop, CharPos_Prop * 2, buffer, (index + charsRead) * 2, n * 2);
                    CharPos_Prop += n;
                }

                charsRead += n;
                count -= n;
                if (IsBlocked_Prop)
                    break;
            }

            return charsRead;
        }

        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();
            if (this.GetType() != typeof (StreamReader))
                return base.ReadBlockAsync(buffer, index, count);
            if (stream == null)
                __Error.ReaderClosed();
            CheckAsyncTaskInProgress();
            Task<int> task = base.ReadBlockAsync(buffer, index, count);
            _asyncReadTask = task;
            return task;
        }

        private Int32 CharLen_Prop
        {
            get
            {
                return charLen;
            }

            set
            {
                charLen = value;
            }
        }

        private Int32 CharPos_Prop
        {
            get
            {
                return charPos;
            }

            set
            {
                charPos = value;
            }
        }

        private Int32 ByteLen_Prop
        {
            get
            {
                return byteLen;
            }

            set
            {
                byteLen = value;
            }
        }

        private Int32 BytePos_Prop
        {
            get
            {
                return bytePos;
            }

            set
            {
                bytePos = value;
            }
        }

        private Byte[] Preamble_Prop
        {
            get
            {
                return _preamble;
            }
        }

        private bool CheckPreamble_Prop
        {
            get
            {
                return _checkPreamble;
            }
        }

        private Decoder Decoder_Prop
        {
            get
            {
                return decoder;
            }
        }

        private bool DetectEncoding_Prop
        {
            get
            {
                return _detectEncoding;
            }
        }

        private Char[] CharBuffer_Prop
        {
            get
            {
                return charBuffer;
            }
        }

        private Byte[] ByteBuffer_Prop
        {
            get
            {
                return byteBuffer;
            }
        }

        private bool IsBlocked_Prop
        {
            get
            {
                return _isBlocked;
            }

            set
            {
                _isBlocked = value;
            }
        }

        private Stream Stream_Prop
        {
            get
            {
                return stream;
            }
        }

        private Int32 MaxCharsPerBuffer_Prop
        {
            get
            {
                return _maxCharsPerBuffer;
            }
        }

        private async Task<int> ReadBufferAsync()
        {
            CharLen_Prop = 0;
            CharPos_Prop = 0;
            Byte[] tmpByteBuffer = ByteBuffer_Prop;
            Stream tmpStream = Stream_Prop;
            if (!CheckPreamble_Prop)
                ByteLen_Prop = 0;
            do
            {
                if (CheckPreamble_Prop)
                {
                    Contract.Assert(BytePos_Prop <= Preamble_Prop.Length, "possible bug in _compressPreamble. Are two threads using this StreamReader at the same time?");
                    int tmpBytePos = BytePos_Prop;
                    int len = await tmpStream.ReadAsync(tmpByteBuffer, tmpBytePos, tmpByteBuffer.Length - tmpBytePos).ConfigureAwait(false);
                    Contract.Assert(len >= 0, "Stream.Read returned a negative number!  This is a bug in your stream class.");
                    if (len == 0)
                    {
                        if (ByteLen_Prop > 0)
                        {
                            CharLen_Prop += Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, CharLen_Prop);
                            BytePos_Prop = 0;
                            ByteLen_Prop = 0;
                        }

                        return CharLen_Prop;
                    }

                    ByteLen_Prop += len;
                }
                else
                {
                    Contract.Assert(BytePos_Prop == 0, "_bytePos can be non zero only when we are trying to _checkPreamble. Are two threads using this StreamReader at the same time?");
                    ByteLen_Prop = await tmpStream.ReadAsync(tmpByteBuffer, 0, tmpByteBuffer.Length).ConfigureAwait(false);
                    Contract.Assert(ByteLen_Prop >= 0, "Stream.Read returned a negative number!  Bug in stream class.");
                    if (ByteLen_Prop == 0)
                        return CharLen_Prop;
                }

                IsBlocked_Prop = (ByteLen_Prop < tmpByteBuffer.Length);
                if (IsPreamble())
                    continue;
                if (DetectEncoding_Prop && ByteLen_Prop >= 2)
                    DetectEncoding();
                CharLen_Prop += Decoder_Prop.GetChars(tmpByteBuffer, 0, ByteLen_Prop, CharBuffer_Prop, CharLen_Prop);
            }
            while (CharLen_Prop == 0);
            return CharLen_Prop;
        }

        private class NullStreamReader : StreamReader
        {
            internal NullStreamReader()
            {
                Init(Stream.Null);
            }

            public override Stream BaseStream
            {
                get
                {
                    return Stream.Null;
                }
            }

            public override Encoding CurrentEncoding
            {
                get
                {
                    return Encoding.Unicode;
                }
            }

            protected override void Dispose(bool disposing)
            {
            }

            public override int Peek()
            {
                return -1;
            }

            public override int Read()
            {
                return -1;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                return 0;
            }

            public override String ReadLine()
            {
                return null;
            }

            public override String ReadToEnd()
            {
                return String.Empty;
            }

            internal override int ReadBuffer()
            {
                return 0;
            }
        }
    }
}