namespace System.Security.Cryptography
{
    public class PKCS1MaskGenerationMethod : MaskGenerationMethod
    {
        private String HashNameValue;
        public PKCS1MaskGenerationMethod()
        {
            HashNameValue = "SHA1";
        }

        public String HashName
        {
            get
            {
                return HashNameValue;
            }

            set
            {
                HashNameValue = value;
                if (HashNameValue == null)
                {
                    HashNameValue = "SHA1";
                }
            }
        }

        public override byte[] GenerateMask(byte[] rgbSeed, int cbReturn)
        {
            HashAlgorithm hash = (HashAlgorithm)CryptoConfig.CreateFromName(HashNameValue);
            byte[] rgbCounter = new byte[4];
            byte[] rgbT = new byte[cbReturn];
            uint counter = 0;
            for (int ib = 0; ib < rgbT.Length;)
            {
                Utils.ConvertIntToByteArray(counter++, ref rgbCounter);
                hash.TransformBlock(rgbSeed, 0, rgbSeed.Length, rgbSeed, 0);
                hash.TransformFinalBlock(rgbCounter, 0, 4);
                byte[] _hash = hash.Hash;
                hash.Initialize();
                if (rgbT.Length - ib > _hash.Length)
                {
                    Buffer.BlockCopy(_hash, 0, rgbT, ib, _hash.Length);
                }
                else
                {
                    Buffer.BlockCopy(_hash, 0, rgbT, ib, rgbT.Length - ib);
                }

                ib += hash.Hash.Length;
            }

            return rgbT;
        }
    }
}