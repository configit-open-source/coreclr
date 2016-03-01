using System.Diagnostics.Contracts;

namespace System.Security.Cryptography
{
    public abstract class HMAC : KeyedHashAlgorithm
    {
        private int blockSizeValue = 64;
        protected int BlockSizeValue
        {
            get
            {
                return blockSizeValue;
            }

            set
            {
                blockSizeValue = value;
            }
        }

        internal string m_hashName;
        internal HashAlgorithm m_hash1;
        internal HashAlgorithm m_hash2;
        private byte[] m_inner;
        private byte[] m_outer;
        private bool m_hashing = false;
        private void UpdateIOPadBuffers()
        {
            if (m_inner == null)
                m_inner = new byte[BlockSizeValue];
            if (m_outer == null)
                m_outer = new byte[BlockSizeValue];
            int i;
            for (i = 0; i < BlockSizeValue; i++)
            {
                m_inner[i] = 0x36;
                m_outer[i] = 0x5C;
            }

            for (i = 0; i < KeyValue.Length; i++)
            {
                m_inner[i] ^= KeyValue[i];
                m_outer[i] ^= KeyValue[i];
            }
        }

        internal void InitializeKey(byte[] key)
        {
            m_inner = null;
            m_outer = null;
            if (key.Length > BlockSizeValue)
            {
                KeyValue = m_hash1.ComputeHash(key);
            }
            else
            {
                KeyValue = (byte[])key.Clone();
            }

            UpdateIOPadBuffers();
        }

        public override byte[] Key
        {
            get
            {
                return (byte[])KeyValue.Clone();
            }

            set
            {
                if (m_hashing)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_HashKeySet"));
                InitializeKey(value);
            }
        }

        public string HashName
        {
            get
            {
                return m_hashName;
            }

            set
            {
                if (m_hashing)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_HashNameSet"));
                m_hashName = value;
                m_hash1 = HashAlgorithm.Create(m_hashName);
                m_hash2 = HashAlgorithm.Create(m_hashName);
            }
        }

        new static public HMAC Create()
        {
            return Create("System.Security.Cryptography.HMAC");
        }

        new static public HMAC Create(string algorithmName)
        {
            return (HMAC)CryptoConfig.CreateFromName(algorithmName);
        }

        public override void Initialize()
        {
            m_hash1.Initialize();
            m_hash2.Initialize();
            m_hashing = false;
        }

        protected override void HashCore(byte[] rgb, int ib, int cb)
        {
            if (m_hashing == false)
            {
                m_hash1.TransformBlock(m_inner, 0, m_inner.Length, m_inner, 0);
                m_hashing = true;
            }

            m_hash1.TransformBlock(rgb, ib, cb, rgb, ib);
        }

        protected override byte[] HashFinal()
        {
            if (m_hashing == false)
            {
                m_hash1.TransformBlock(m_inner, 0, m_inner.Length, m_inner, 0);
                m_hashing = true;
            }

            m_hash1.TransformFinalBlock(EmptyArray<Byte>.Value, 0, 0);
            byte[] hashValue1 = m_hash1.HashValue;
            m_hash2.TransformBlock(m_outer, 0, m_outer.Length, m_outer, 0);
            m_hash2.TransformBlock(hashValue1, 0, hashValue1.Length, hashValue1, 0);
            m_hashing = false;
            m_hash2.TransformFinalBlock(EmptyArray<Byte>.Value, 0, 0);
            return m_hash2.HashValue;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_hash1 != null)
                    ((IDisposable)m_hash1).Dispose();
                if (m_hash2 != null)
                    ((IDisposable)m_hash2).Dispose();
                if (m_inner != null)
                    Array.Clear(m_inner, 0, m_inner.Length);
                if (m_outer != null)
                    Array.Clear(m_outer, 0, m_outer.Length);
            }

            base.Dispose(disposing);
        }

        internal static HashAlgorithm GetHashAlgorithmWithFipsFallback(Func<HashAlgorithm> createStandardHashAlgorithmCallback, Func<HashAlgorithm> createFipsHashAlgorithmCallback)
        {
            Contract.Requires(createStandardHashAlgorithmCallback != null);
            Contract.Requires(createFipsHashAlgorithmCallback != null);
            if (CryptoConfig.AllowOnlyFipsAlgorithms)
            {
                try
                {
                    return createFipsHashAlgorithmCallback();
                }
                catch (PlatformNotSupportedException e)
                {
                    throw new InvalidOperationException(e.Message, e);
                }
            }
            else
            {
                return createStandardHashAlgorithmCallback();
            }
        }
    }
}