
using System.IO;

namespace System.Security.Cryptography
{
    public abstract class HashAlgorithm : IDisposable, ICryptoTransform
    {
        protected int HashSizeValue;
        protected internal byte[] HashValue;
        protected int State = 0;
        private bool m_bDisposed = false;
        protected HashAlgorithm()
        {
        }

        public virtual int HashSize
        {
            get
            {
                return HashSizeValue;
            }
        }

        public virtual byte[] Hash
        {
            get
            {
                if (m_bDisposed)
                    throw new ObjectDisposedException(null);
                if (State != 0)
                    throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_HashNotYetFinalized"));
                return (byte[])HashValue.Clone();
            }
        }

        static public HashAlgorithm Create()
        {
            return Create("System.Security.Cryptography.HashAlgorithm");
        }

        static public HashAlgorithm Create(String hashName)
        {
            return (HashAlgorithm)CryptoConfig.CreateFromName(hashName);
        }

        public byte[] ComputeHash(Stream inputStream)
        {
            if (m_bDisposed)
                throw new ObjectDisposedException(null);
            byte[] buffer = new byte[4096];
            int bytesRead;
            do
            {
                bytesRead = inputStream.Read(buffer, 0, 4096);
                if (bytesRead > 0)
                {
                    HashCore(buffer, 0, bytesRead);
                }
            }
            while (bytesRead > 0);
            HashValue = HashFinal();
            byte[] Tmp = (byte[])HashValue.Clone();
            Initialize();
            return (Tmp);
        }

        public byte[] ComputeHash(byte[] buffer)
        {
            if (m_bDisposed)
                throw new ObjectDisposedException(null);
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            HashCore(buffer, 0, buffer.Length);
            HashValue = HashFinal();
            byte[] Tmp = (byte[])HashValue.Clone();
            Initialize();
            return (Tmp);
        }

        public byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0 || (count > buffer.Length))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((buffer.Length - count) < offset)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (m_bDisposed)
                throw new ObjectDisposedException(null);
            HashCore(buffer, offset, count);
            HashValue = HashFinal();
            byte[] Tmp = (byte[])HashValue.Clone();
            Initialize();
            return (Tmp);
        }

        public virtual int InputBlockSize
        {
            get
            {
                return (1);
            }
        }

        public virtual int OutputBlockSize
        {
            get
            {
                return (1);
            }
        }

        public virtual bool CanTransformMultipleBlocks
        {
            get
            {
                return (true);
            }
        }

        public virtual bool CanReuseTransform
        {
            get
            {
                return (true);
            }
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (inputCount < 0 || (inputCount > inputBuffer.Length))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (m_bDisposed)
                throw new ObjectDisposedException(null);
            State = 1;
            HashCore(inputBuffer, inputOffset, inputCount);
            if ((outputBuffer != null) && ((inputBuffer != outputBuffer) || (inputOffset != outputOffset)))
                Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (inputCount < 0 || (inputCount > inputBuffer.Length))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        if (m_bDisposed)
                throw new ObjectDisposedException(null);
            HashCore(inputBuffer, inputOffset, inputCount);
            HashValue = HashFinal();
            byte[] outputBytes;
            if (inputCount != 0)
            {
                outputBytes = new byte[inputCount];
                Buffer.InternalBlockCopy(inputBuffer, inputOffset, outputBytes, 0, inputCount);
            }
            else
            {
                outputBytes = EmptyArray<Byte>.Value;
            }

            State = 0;
            return outputBytes;
        }

        void IDisposable.Dispose()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            (this as IDisposable).Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (HashValue != null)
                    Array.Clear(HashValue, 0, HashValue.Length);
                HashValue = null;
                m_bDisposed = true;
            }
        }

        public abstract void Initialize();
        protected abstract void HashCore(byte[] array, int ibStart, int cbSize);
        protected abstract byte[] HashFinal();
    }
}