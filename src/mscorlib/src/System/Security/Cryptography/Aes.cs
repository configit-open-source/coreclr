namespace System.Security.Cryptography
{
    public abstract class Aes : SymmetricAlgorithm
    {
        private static KeySizes[] s_legalBlockSizes = {new KeySizes(128, 128, 0)};
        private static KeySizes[] s_legalKeySizes = {new KeySizes(128, 256, 64)};
        protected Aes()
        {
            LegalBlockSizesValue = s_legalBlockSizes;
            LegalKeySizesValue = s_legalKeySizes;
            BlockSizeValue = 128;
            FeedbackSizeValue = 8;
            KeySizeValue = 256;
            ModeValue = CipherMode.CBC;
        }

        public static new Aes Create()
        {
            return Create("AES");
        }

        public static new Aes Create(string algorithmName)
        {
            if (algorithmName == null)
            {
                throw new ArgumentNullException("algorithmName");
            }

            return CryptoConfig.CreateFromName(algorithmName) as Aes;
        }
    }
}