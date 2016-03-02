namespace System.Security.Cryptography
{
    public abstract class SHA256 : HashAlgorithm
    {
        protected SHA256()
        {
            HashSizeValue = 256;
        }

        new static public SHA256 Create()
        {
            return Create("System.Security.Cryptography.SHA256");
        }

        new static public SHA256 Create(String hashName)
        {
            return (SHA256)CryptoConfig.CreateFromName(hashName);
        }
    }
}