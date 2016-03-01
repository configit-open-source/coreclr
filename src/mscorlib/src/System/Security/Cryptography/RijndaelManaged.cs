using System.Diagnostics.Contracts;

namespace System.Security.Cryptography
{
    public sealed class RijndaelManaged : Rijndael
    {
        public RijndaelManaged()
        {
            if (CryptoConfig.AllowOnlyFipsAlgorithms)
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
            Contract.EndContractBlock();
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return NewEncryptor(rgbKey, ModeValue, rgbIV, FeedbackSizeValue, RijndaelManagedTransformMode.Encrypt);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            return NewEncryptor(rgbKey, ModeValue, rgbIV, FeedbackSizeValue, RijndaelManagedTransformMode.Decrypt);
        }

        public override void GenerateKey()
        {
            KeyValue = Utils.GenerateRandom(KeySizeValue / 8);
        }

        public override void GenerateIV()
        {
            IVValue = Utils.GenerateRandom(BlockSizeValue / 8);
        }

        private ICryptoTransform NewEncryptor(byte[] rgbKey, CipherMode mode, byte[] rgbIV, int feedbackSize, RijndaelManagedTransformMode encryptMode)
        {
            if (rgbKey == null)
            {
                rgbKey = Utils.GenerateRandom(KeySizeValue / 8);
            }

            if (rgbIV == null)
            {
                rgbIV = Utils.GenerateRandom(BlockSizeValue / 8);
            }

            return new RijndaelManagedTransform(rgbKey, mode, rgbIV, BlockSizeValue, feedbackSize, PaddingValue, encryptMode);
        }
    }
}