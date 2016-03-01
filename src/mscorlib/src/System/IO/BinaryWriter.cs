using System;
using System.Runtime;
using System.Runtime.Serialization;
using System.Text;
using System.Diagnostics.Contracts;

namespace System.IO
{
    public class BinaryWriter : IDisposable
    {
        public static readonly BinaryWriter Null = new BinaryWriter();
        protected Stream OutStream;
        private byte[] _buffer;
        private Encoding _encoding;
        private Encoder _encoder;
        private bool _leaveOpen;
        private char[] _tmpOneCharBuffer;
        private byte[] _largeByteBuffer;
        private int _maxChars;
        private const int LargeByteBufferSize = 256;
        protected BinaryWriter()
        {
            OutStream = Stream.Null;
            _buffer = new byte[16];
            _encoding = new UTF8Encoding(false, true);
            _encoder = _encoding.GetEncoder();
        }

        public BinaryWriter(Stream output): this (output, new UTF8Encoding(false, true), false)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding): this (output, encoding, false)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
        {
            if (output == null)
                throw new ArgumentNullException("output");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (!output.CanWrite)
                throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotWritable"));
            Contract.EndContractBlock();
            OutStream = output;
            _buffer = new byte[16];
            _encoding = encoding;
            _encoder = _encoding.GetEncoder();
            _leaveOpen = leaveOpen;
        }

        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_leaveOpen)
                    OutStream.Flush();
                else
                    OutStream.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual Stream BaseStream
        {
            get
            {
                Flush();
                return OutStream;
            }
        }

        public virtual void Flush()
        {
            OutStream.Flush();
        }

        public virtual long Seek(int offset, SeekOrigin origin)
        {
            return OutStream.Seek(offset, origin);
        }

        public virtual void Write(bool value)
        {
            _buffer[0] = (byte)(value ? 1 : 0);
            OutStream.Write(_buffer, 0, 1);
        }

        public virtual void Write(byte value)
        {
            OutStream.WriteByte(value);
        }

        public virtual void Write(sbyte value)
        {
            OutStream.WriteByte((byte)value);
        }

        public virtual void Write(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            Contract.EndContractBlock();
            OutStream.Write(buffer, 0, buffer.Length);
        }

        public virtual void Write(byte[] buffer, int index, int count)
        {
            OutStream.Write(buffer, index, count);
        }

        public unsafe virtual void Write(char ch)
        {
            if (Char.IsSurrogate(ch))
                throw new ArgumentException(Environment.GetResourceString("Arg_SurrogatesNotAllowedAsSingleChar"));
            Contract.EndContractBlock();
            Contract.Assert(_encoding.GetMaxByteCount(1) <= 16, "_encoding.GetMaxByteCount(1) <= 16)");
            int numBytes = 0;
            fixed (byte *pBytes = _buffer)
            {
                numBytes = _encoder.GetBytes(&ch, 1, pBytes, 16, true);
            }

            OutStream.Write(_buffer, 0, numBytes);
        }

        public virtual void Write(char[] chars)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            Contract.EndContractBlock();
            byte[] bytes = _encoding.GetBytes(chars, 0, chars.Length);
            OutStream.Write(bytes, 0, bytes.Length);
        }

        public virtual void Write(char[] chars, int index, int count)
        {
            byte[] bytes = _encoding.GetBytes(chars, index, count);
            OutStream.Write(bytes, 0, bytes.Length);
        }

        public unsafe virtual void Write(double value)
        {
            ulong TmpValue = *(ulong *)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            _buffer[4] = (byte)(TmpValue >> 32);
            _buffer[5] = (byte)(TmpValue >> 40);
            _buffer[6] = (byte)(TmpValue >> 48);
            _buffer[7] = (byte)(TmpValue >> 56);
            OutStream.Write(_buffer, 0, 8);
        }

        public virtual void Write(decimal value)
        {
            Decimal.GetBytes(value, _buffer);
            OutStream.Write(_buffer, 0, 16);
        }

        public virtual void Write(short value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            OutStream.Write(_buffer, 0, 2);
        }

        public virtual void Write(ushort value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            OutStream.Write(_buffer, 0, 2);
        }

        public virtual void Write(int value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            OutStream.Write(_buffer, 0, 4);
        }

        public virtual void Write(uint value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            OutStream.Write(_buffer, 0, 4);
        }

        public virtual void Write(long value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);
            OutStream.Write(_buffer, 0, 8);
        }

        public virtual void Write(ulong value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);
            OutStream.Write(_buffer, 0, 8);
        }

        public unsafe virtual void Write(float value)
        {
            uint TmpValue = *(uint *)&value;
            _buffer[0] = (byte)TmpValue;
            _buffer[1] = (byte)(TmpValue >> 8);
            _buffer[2] = (byte)(TmpValue >> 16);
            _buffer[3] = (byte)(TmpValue >> 24);
            OutStream.Write(_buffer, 0, 4);
        }

        public unsafe virtual void Write(String value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            int len = _encoding.GetByteCount(value);
            Write7BitEncodedInt(len);
            if (_largeByteBuffer == null)
            {
                _largeByteBuffer = new byte[LargeByteBufferSize];
                _maxChars = LargeByteBufferSize / _encoding.GetMaxByteCount(1);
            }

            if (len <= LargeByteBufferSize)
            {
                _encoding.GetBytes(value, 0, value.Length, _largeByteBuffer, 0);
                OutStream.Write(_largeByteBuffer, 0, len);
            }
            else
            {
                int charStart = 0;
                int numLeft = value.Length;
                int totalBytes = 0;
                while (numLeft > 0)
                {
                    int charCount = (numLeft > _maxChars) ? _maxChars : numLeft;
                    int byteLen;
                    fixed (char *pChars = value)
                    {
                        fixed (byte *pBytes = _largeByteBuffer)
                        {
                            byteLen = _encoder.GetBytes(pChars + charStart, charCount, pBytes, LargeByteBufferSize, charCount == numLeft);
                        }
                    }

                    totalBytes += byteLen;
                    Contract.Assert(totalBytes <= len && byteLen <= LargeByteBufferSize, "BinaryWriter::Write(String) - More bytes encoded than expected!");
                    OutStream.Write(_largeByteBuffer, 0, byteLen);
                    charStart += charCount;
                    numLeft -= charCount;
                }

                Contract.Assert(totalBytes == len, "BinaryWriter::Write(String) - Didn't write out all the bytes!");
            }
        }

        protected void Write7BitEncodedInt(int value)
        {
            uint v = (uint)value;
            while (v >= 0x80)
            {
                Write((byte)(v | 0x80));
                v >>= 7;
            }

            Write((byte)v);
        }
    }
}