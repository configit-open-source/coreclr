namespace System.Security.Cryptography
{
    public abstract class MD5 : HashAlgorithm
    {
        protected MD5()
        {
            HashSizeValue = 128;
        }

        new static public MD5 Create()
        {
            return Create("System.Security.Cryptography.MD5");
        }

        new static public MD5 Create(String algName)
        {
            return (MD5)CryptoConfig.CreateFromName(algName);
        }
    }
}