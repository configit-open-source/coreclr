namespace System.Security.Cryptography
{
    public abstract class SHA512 : HashAlgorithm
    {
        protected SHA512()
        {
            HashSizeValue = 512;
        }

        new static public SHA512 Create()
        {
            return Create("System.Security.Cryptography.SHA512");
        }

        new static public SHA512 Create(String hashName)
        {
            return (SHA512)CryptoConfig.CreateFromName(hashName);
        }
    }
}