

namespace System.Security.Cryptography
{
    public class RSAPKCS1KeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
    {
        RSA _rsaKey;
        RandomNumberGenerator RngValue;
        public RSAPKCS1KeyExchangeDeformatter()
        {
        }

        public RSAPKCS1KeyExchangeDeformatter(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
                        _rsaKey = (RSA)key;
        }

        public RandomNumberGenerator RNG
        {
            get
            {
                return RngValue;
            }

            set
            {
                RngValue = value;
            }
        }

        public override String Parameters
        {
            get
            {
                return null;
            }

            set
            {
                ;
            }
        }

        public override byte[] DecryptKeyExchange(byte[] rgbIn)
        {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            byte[] rgbOut;
            if (_rsaKey is RSACryptoServiceProvider)
            {
                rgbOut = ((RSACryptoServiceProvider)_rsaKey).Decrypt(rgbIn, false);
            }
            else
            {
                int i;
                byte[] rgb;
                rgb = _rsaKey.DecryptValue(rgbIn);
                for (i = 2; i < rgb.Length; i++)
                {
                    if (rgb[i] == 0)
                    {
                        break;
                    }
                }

                if (i >= rgb.Length)
                    throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_PKCS1Decoding"));
                i++;
                rgbOut = new byte[rgb.Length - i];
                Buffer.InternalBlockCopy(rgb, i, rgbOut, 0, rgbOut.Length);
            }

            return rgbOut;
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
                        _rsaKey = (RSA)key;
        }
    }
}