namespace System.Security.Cryptography
{
    public abstract class SHA384 : HashAlgorithm
    {
        protected SHA384()
        {
            HashSizeValue = 384;
        }

        new static public SHA384 Create()
        {
            return Create("System.Security.Cryptography.SHA384");
        }

        new static public SHA384 Create(String hashName)
        {
            return (SHA384)CryptoConfig.CreateFromName(hashName);
        }
    }
}