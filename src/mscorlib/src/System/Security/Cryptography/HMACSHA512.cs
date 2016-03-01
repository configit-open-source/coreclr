namespace System.Security.Cryptography
{
    public class HMACSHA512 : HMAC
    {
        private bool m_useLegacyBlockSize = Utils._ProduceLegacyHmacValues();
        public HMACSHA512(): this (Utils.GenerateRandom(128))
        {
        }

        public HMACSHA512(byte[] key)
        {
            m_hashName = "SHA512";
            m_hash1 = GetHashAlgorithmWithFipsFallback(() => new SHA512Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA512CryptoServiceProvider"));
            m_hash2 = GetHashAlgorithmWithFipsFallback(() => new SHA512Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA512CryptoServiceProvider"));
            HashSizeValue = 512;
            BlockSizeValue = BlockSize;
            base.InitializeKey(key);
        }

        private int BlockSize
        {
            get
            {
                return m_useLegacyBlockSize ? 64 : 128;
            }
        }

        public bool ProduceLegacyHmacValues
        {
            get
            {
                return m_useLegacyBlockSize;
            }

            set
            {
                m_useLegacyBlockSize = value;
                BlockSizeValue = BlockSize;
                InitializeKey(KeyValue);
            }
        }
    }
}