namespace System.IO
{
    using System;
    using System.Runtime;
    using System.Text;
    using System.Globalization;
    using System.Diagnostics.Contracts;
    using System.Security;

    public class BinaryReader : IDisposable
    {
        private const int MaxCharBytesSize = 128;
        private Stream m_stream;
        private byte[] m_buffer;
        private Decoder m_decoder;
        private byte[] m_charBytes;
        private char[] m_singleChar;
        private char[] m_charBuffer;
        private int m_maxCharsSize;
        private bool m_2BytesPerChar;
        private bool m_isMemoryStream;
        private bool m_leaveOpen;
        public BinaryReader(Stream input): this (input, new UTF8Encoding(), false)
        {
        }

        public BinaryReader(Stream input, Encoding encoding): this (input, encoding, false)
        {
        }

        public BinaryReader(Stream input, Encoding encoding, bool leaveOpen)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            if (!input.CanRead)
                throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotReadable"));
            Contract.EndContractBlock();
            m_stream = input;
            m_decoder = encoding.GetDecoder();
            m_maxCharsSize = encoding.GetMaxCharCount(MaxCharBytesSize);
            int minBufferSize = encoding.GetMaxByteCount(1);
            if (minBufferSize < 16)
                minBufferSize = 16;
            m_buffer = new byte[minBufferSize];
            m_2BytesPerChar = encoding is UnicodeEncoding;
            m_isMemoryStream = (m_stream.GetType() == typeof (MemoryStream));
            m_leaveOpen = leaveOpen;
            Contract.Assert(m_decoder != null, "[BinaryReader.ctor]m_decoder!=null");
        }

        public virtual Stream BaseStream
        {
            get
            {
                return m_stream;
            }
        }

        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream copyOfStream = m_stream;
                m_stream = null;
                if (copyOfStream != null && !m_leaveOpen)
                    copyOfStream.Close();
            }

            m_stream = null;
            m_buffer = null;
            m_decoder = null;
            m_charBytes = null;
            m_singleChar = null;
            m_charBuffer = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual int PeekChar()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            if (m_stream == null)
                __Error.FileNotOpen();
            if (!m_stream.CanSeek)
                return -1;
            long origPos = m_stream.Position;
            int ch = Read();
            m_stream.Position = origPos;
            return ch;
        }

        public virtual int Read()
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            if (m_stream == null)
            {
                __Error.FileNotOpen();
            }

            return InternalReadOneChar();
        }

        public virtual bool ReadBoolean()
        {
            FillBuffer(1);
            return (m_buffer[0] != 0);
        }

        public virtual byte ReadByte()
        {
            if (m_stream == null)
                __Error.FileNotOpen();
            int b = m_stream.ReadByte();
            if (b == -1)
                __Error.EndOfFile();
            return (byte)b;
        }

        public virtual sbyte ReadSByte()
        {
            FillBuffer(1);
            return (sbyte)(m_buffer[0]);
        }

        public virtual char ReadChar()
        {
            int value = Read();
            if (value == -1)
            {
                __Error.EndOfFile();
            }

            return (char)value;
        }

        public virtual short ReadInt16()
        {
            FillBuffer(2);
            return (short)(m_buffer[0] | m_buffer[1] << 8);
        }

        public virtual ushort ReadUInt16()
        {
            FillBuffer(2);
            return (ushort)(m_buffer[0] | m_buffer[1] << 8);
        }

        public virtual int ReadInt32()
        {
            if (m_isMemoryStream)
            {
                if (m_stream == null)
                    __Error.FileNotOpen();
                MemoryStream mStream = m_stream as MemoryStream;
                Contract.Assert(mStream != null, "m_stream as MemoryStream != null");
                return mStream.InternalReadInt32();
            }
            else
            {
                FillBuffer(4);
                return (int)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            }
        }

        public virtual uint ReadUInt32()
        {
            FillBuffer(4);
            return (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
        }

        public virtual long ReadInt64()
        {
            FillBuffer(8);
            uint lo = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            uint hi = (uint)(m_buffer[4] | m_buffer[5] << 8 | m_buffer[6] << 16 | m_buffer[7] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }

        public virtual ulong ReadUInt64()
        {
            FillBuffer(8);
            uint lo = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            uint hi = (uint)(m_buffer[4] | m_buffer[5] << 8 | m_buffer[6] << 16 | m_buffer[7] << 24);
            return ((ulong)hi) << 32 | lo;
        }

        public virtual unsafe float ReadSingle()
        {
            FillBuffer(4);
            uint tmpBuffer = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            return *((float *)&tmpBuffer);
        }

        public virtual unsafe double ReadDouble()
        {
            FillBuffer(8);
            uint lo = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            uint hi = (uint)(m_buffer[4] | m_buffer[5] << 8 | m_buffer[6] << 16 | m_buffer[7] << 24);
            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double *)&tmpBuffer);
        }

        public virtual decimal ReadDecimal()
        {
            FillBuffer(16);
            try
            {
                return Decimal.ToDecimal(m_buffer);
            }
            catch (ArgumentException e)
            {
                throw new IOException(Environment.GetResourceString("Arg_DecBitCtor"), e);
            }
        }

        public virtual String ReadString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            if (m_stream == null)
                __Error.FileNotOpen();
            int currPos = 0;
            int n;
            int stringLength;
            int readLength;
            int charsRead;
            stringLength = Read7BitEncodedInt();
            if (stringLength < 0)
            {
                throw new IOException(Environment.GetResourceString("IO.IO_InvalidStringLen_Len", stringLength));
            }

            if (stringLength == 0)
            {
                return String.Empty;
            }

            if (m_charBytes == null)
            {
                m_charBytes = new byte[MaxCharBytesSize];
            }

            if (m_charBuffer == null)
            {
                m_charBuffer = new char[m_maxCharsSize];
            }

            StringBuilder sb = null;
            do
            {
                readLength = ((stringLength - currPos) > MaxCharBytesSize) ? MaxCharBytesSize : (stringLength - currPos);
                n = m_stream.Read(m_charBytes, 0, readLength);
                if (n == 0)
                {
                    __Error.EndOfFile();
                }

                charsRead = m_decoder.GetChars(m_charBytes, 0, n, m_charBuffer, 0);
                if (currPos == 0 && n == stringLength)
                    return new String(m_charBuffer, 0, charsRead);
                if (sb == null)
                    sb = StringBuilderCache.Acquire(stringLength);
                sb.Append(m_charBuffer, 0, charsRead);
                currPos += n;
            }
            while (currPos < stringLength);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public virtual int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (buffer.Length - index < count)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            }

            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            Contract.EndContractBlock();
            if (m_stream == null)
                __Error.FileNotOpen();
            return InternalReadChars(buffer, index, count);
        }

        private int InternalReadChars(char[] buffer, int index, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(index >= 0 && count >= 0);
            Contract.Assert(m_stream != null);
            int numBytes = 0;
            int charsRemaining = count;
            if (m_charBytes == null)
            {
                m_charBytes = new byte[MaxCharBytesSize];
            }

            while (charsRemaining > 0)
            {
                int charsRead = 0;
                numBytes = charsRemaining;
                DecoderNLS decoder = m_decoder as DecoderNLS;
                if (decoder != null && decoder.HasState && numBytes > 1)
                {
                    numBytes -= 1;
                }

                if (m_2BytesPerChar)
                    numBytes <<= 1;
                if (numBytes > MaxCharBytesSize)
                    numBytes = MaxCharBytesSize;
                int position = 0;
                byte[] byteBuffer = null;
                if (m_isMemoryStream)
                {
                    MemoryStream mStream = m_stream as MemoryStream;
                    Contract.Assert(mStream != null, "m_stream as MemoryStream != null");
                    position = mStream.InternalGetPosition();
                    numBytes = mStream.InternalEmulateRead(numBytes);
                    byteBuffer = mStream.InternalGetBuffer();
                }
                else
                {
                    numBytes = m_stream.Read(m_charBytes, 0, numBytes);
                    byteBuffer = m_charBytes;
                }

                if (numBytes == 0)
                {
                    return (count - charsRemaining);
                }

                Contract.Assert(byteBuffer != null, "expected byteBuffer to be non-null");
                unsafe
                {
                    fixed (byte *pBytes = byteBuffer)
                        fixed (char *pChars = buffer)
                        {
                            charsRead = m_decoder.GetChars(pBytes + position, numBytes, pChars + index, charsRemaining, false);
                        }
                }

                charsRemaining -= charsRead;
                index += charsRead;
            }

            Contract.Assert(charsRemaining >= 0, "We read too many characters.");
            return (count - charsRemaining);
        }

        private int InternalReadOneChar()
        {
            int charsRead = 0;
            int numBytes = 0;
            long posSav = posSav = 0;
            if (m_stream.CanSeek)
                posSav = m_stream.Position;
            if (m_charBytes == null)
            {
                m_charBytes = new byte[MaxCharBytesSize];
            }

            if (m_singleChar == null)
            {
                m_singleChar = new char[1];
            }

            while (charsRead == 0)
            {
                numBytes = m_2BytesPerChar ? 2 : 1;
                int r = m_stream.ReadByte();
                m_charBytes[0] = (byte)r;
                if (r == -1)
                    numBytes = 0;
                if (numBytes == 2)
                {
                    r = m_stream.ReadByte();
                    m_charBytes[1] = (byte)r;
                    if (r == -1)
                        numBytes = 1;
                }

                if (numBytes == 0)
                {
                    return -1;
                }

                Contract.Assert(numBytes == 1 || numBytes == 2, "BinaryReader::InternalReadOneChar assumes it's reading one or 2 bytes only.");
                try
                {
                    charsRead = m_decoder.GetChars(m_charBytes, 0, numBytes, m_singleChar, 0);
                }
                catch
                {
                    if (m_stream.CanSeek)
                        m_stream.Seek((posSav - m_stream.Position), SeekOrigin.Current);
                    throw;
                }

                Contract.Assert(charsRead < 2, "InternalReadOneChar - assuming we only got 0 or 1 char, not 2!");
            }

            if (charsRead == 0)
                return -1;
            return m_singleChar[0];
        }

        public virtual char[] ReadChars(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.Ensures(Contract.Result<char[]>() != null);
            Contract.Ensures(Contract.Result<char[]>().Length <= count);
            Contract.EndContractBlock();
            if (m_stream == null)
            {
                __Error.FileNotOpen();
            }

            if (count == 0)
            {
                return EmptyArray<Char>.Value;
            }

            char[] chars = new char[count];
            int n = InternalReadChars(chars, 0, count);
            if (n != count)
            {
                char[] copy = new char[n];
                Buffer.InternalBlockCopy(chars, 0, copy, 0, 2 * n);
                chars = copy;
            }

            return chars;
        }

        public virtual int Read(byte[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetResourceString("ArgumentNull_Buffer"));
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= count);
            Contract.EndContractBlock();
            if (m_stream == null)
                __Error.FileNotOpen();
            return m_stream.Read(buffer, index, count);
        }

        public virtual byte[] ReadBytes(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length <= Contract.OldValue(count));
            Contract.EndContractBlock();
            if (m_stream == null)
                __Error.FileNotOpen();
            if (count == 0)
            {
                return EmptyArray<Byte>.Value;
            }

            byte[] result = new byte[count];
            int numRead = 0;
            do
            {
                int n = m_stream.Read(result, numRead, count);
                if (n == 0)
                    break;
                numRead += n;
                count -= n;
            }
            while (count > 0);
            if (numRead != result.Length)
            {
                byte[] copy = new byte[numRead];
                Buffer.InternalBlockCopy(result, 0, copy, 0, numRead);
                result = copy;
            }

            return result;
        }

        protected virtual void FillBuffer(int numBytes)
        {
            if (m_buffer != null && (numBytes < 0 || numBytes > m_buffer.Length))
            {
                throw new ArgumentOutOfRangeException("numBytes", Environment.GetResourceString("ArgumentOutOfRange_BinaryReaderFillBuffer"));
            }

            int bytesRead = 0;
            int n = 0;
            if (m_stream == null)
                __Error.FileNotOpen();
            if (numBytes == 1)
            {
                n = m_stream.ReadByte();
                if (n == -1)
                    __Error.EndOfFile();
                m_buffer[0] = (byte)n;
                return;
            }

            do
            {
                n = m_stream.Read(m_buffer, bytesRead, numBytes - bytesRead);
                if (n == 0)
                {
                    __Error.EndOfFile();
                }

                bytesRead += n;
            }
            while (bytesRead < numBytes);
        }

        internal protected int Read7BitEncodedInt()
        {
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                if (shift == 5 * 7)
                    throw new FormatException(Environment.GetResourceString("Format_Bad7BitInt32"));
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return count;
        }
    }
}