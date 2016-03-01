namespace System.Security.Cryptography
{
    using System.IO;
    using System.Diagnostics.Contracts;

    public class MACTripleDES : KeyedHashAlgorithm
    {
        private ICryptoTransform m_encryptor;
        private CryptoStream _cs;
        private TailStream _ts;
        private const int m_bitsPerByte = 8;
        private int m_bytesPerBlock;
        private TripleDES des;
        public MACTripleDES()
        {
            KeyValue = new byte[24];
            Utils.StaticRandomNumberGenerator.GetBytes(KeyValue);
            des = TripleDES.Create();
            HashSizeValue = des.BlockSize;
            m_bytesPerBlock = des.BlockSize / m_bitsPerByte;
            des.IV = new byte[m_bytesPerBlock];
            des.Padding = PaddingMode.Zeros;
            m_encryptor = null;
        }

        public MACTripleDES(byte[] rgbKey): this ("System.Security.Cryptography.TripleDES", rgbKey)
        {
        }

        public MACTripleDES(String strTripleDES, byte[] rgbKey)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");
            Contract.EndContractBlock();
            if (strTripleDES == null)
            {
                des = TripleDES.Create();
            }
            else
            {
                des = TripleDES.Create(strTripleDES);
            }

            HashSizeValue = des.BlockSize;
            KeyValue = (byte[])rgbKey.Clone();
            m_bytesPerBlock = des.BlockSize / m_bitsPerByte;
            des.IV = new byte[m_bytesPerBlock];
            des.Padding = PaddingMode.Zeros;
            m_encryptor = null;
        }

        public override void Initialize()
        {
            m_encryptor = null;
        }

        public PaddingMode Padding
        {
            get
            {
                return des.Padding;
            }

            set
            {
                if ((value < PaddingMode.None) || (PaddingMode.ISO10126 < value))
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidPaddingMode"));
                des.Padding = value;
            }
        }

        protected override void HashCore(byte[] rgbData, int ibStart, int cbSize)
        {
            if (m_encryptor == null)
            {
                des.Key = this.Key;
                m_encryptor = des.CreateEncryptor();
                _ts = new TailStream(des.BlockSize / 8);
                _cs = new CryptoStream(_ts, m_encryptor, CryptoStreamMode.Write);
            }

            _cs.Write(rgbData, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            if (m_encryptor == null)
            {
                des.Key = this.Key;
                m_encryptor = des.CreateEncryptor();
                _ts = new TailStream(des.BlockSize / 8);
                _cs = new CryptoStream(_ts, m_encryptor, CryptoStreamMode.Write);
            }

            _cs.FlushFinalBlock();
            return _ts.Buffer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (des != null)
                    des.Clear();
                if (m_encryptor != null)
                    m_encryptor.Dispose();
                if (_cs != null)
                    _cs.Clear();
                if (_ts != null)
                    _ts.Clear();
            }

            base.Dispose(disposing);
        }
    }

    internal sealed class TailStream : Stream
    {
        private byte[] _Buffer;
        private int _BufferSize;
        private int _BufferIndex = 0;
        private bool _BufferFull = false;
        public TailStream(int bufferSize)
        {
            _Buffer = new byte[bufferSize];
            _BufferSize = bufferSize;
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
                    if (_Buffer != null)
                    {
                        Array.Clear(_Buffer, 0, _Buffer.Length);
                    }

                    _Buffer = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public byte[] Buffer
        {
            get
            {
                return (byte[])_Buffer.Clone();
            }
        }

        public override bool CanRead
        {
            [Pure]
            get
            {
                return false;
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
                return _Buffer != null;
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

        public override void Flush()
        {
            return;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnseekableStream"));
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_UnreadableStream"));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_Buffer == null)
                throw new ObjectDisposedException("TailStream");
            if (count == 0)
                return;
            if (_BufferFull)
            {
                if (count > _BufferSize)
                {
                    System.Buffer.InternalBlockCopy(buffer, offset + count - _BufferSize, _Buffer, 0, _BufferSize);
                    return;
                }
                else
                {
                    System.Buffer.InternalBlockCopy(_Buffer, _BufferSize - count, _Buffer, 0, _BufferSize - count);
                    System.Buffer.InternalBlockCopy(buffer, offset, _Buffer, _BufferSize - count, count);
                    return;
                }
            }
            else
            {
                if (count > _BufferSize)
                {
                    System.Buffer.InternalBlockCopy(buffer, offset + count - _BufferSize, _Buffer, 0, _BufferSize);
                    _BufferFull = true;
                    return;
                }
                else if (count + _BufferIndex >= _BufferSize)
                {
                    System.Buffer.InternalBlockCopy(_Buffer, _BufferIndex + count - _BufferSize, _Buffer, 0, _BufferSize - count);
                    System.Buffer.InternalBlockCopy(buffer, offset, _Buffer, _BufferIndex, count);
                    _BufferFull = true;
                    return;
                }
                else
                {
                    System.Buffer.InternalBlockCopy(buffer, offset, _Buffer, _BufferIndex, count);
                    _BufferIndex += count;
                    return;
                }
            }
        }
    }
}