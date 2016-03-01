namespace System.Security.Cryptography
{
    public class HMACSHA256 : HMAC
    {
        public HMACSHA256(): this (Utils.GenerateRandom(64))
        {
        }

        public HMACSHA256(byte[] key)
        {
            m_hashName = "SHA256";
            m_hash1 = GetHashAlgorithmWithFipsFallback(() => new SHA256Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA256CryptoServiceProvider"));
            m_hash2 = GetHashAlgorithmWithFipsFallback(() => new SHA256Managed(), () => HashAlgorithm.Create("System.Security.Cryptography.SHA256CryptoServiceProvider"));
            HashSizeValue = 256;
            base.InitializeKey(key);
        }
    }
}