namespace System.Security.Cryptography
{
    public abstract class SHA1 : HashAlgorithm
    {
        protected SHA1()
        {
            HashSizeValue = 160;
        }

        new static public SHA1 Create()
        {
            return Create("System.Security.Cryptography.SHA1");
        }

        new static public SHA1 Create(String hashName)
        {
            return (SHA1)CryptoConfig.CreateFromName(hashName);
        }
    }
}