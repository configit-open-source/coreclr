using System.Diagnostics.Contracts;

namespace System.Security.Cryptography
{
    public class RSAOAEPKeyExchangeFormatter : AsymmetricKeyExchangeFormatter
    {
        private byte[] ParameterValue;
        private RSA _rsaKey;
        private RandomNumberGenerator RngValue;
        public RSAOAEPKeyExchangeFormatter()
        {
        }

        public RSAOAEPKeyExchangeFormatter(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA)key;
        }

        public byte[] Parameter
        {
            get
            {
                if (ParameterValue != null)
                    return (byte[])ParameterValue.Clone();
                return null;
            }

            set
            {
                if (value != null)
                    ParameterValue = (byte[])value.Clone();
                else
                    ParameterValue = null;
            }
        }

        public override String Parameters
        {
            get
            {
                return null;
            }
        }

        public RandomNumberGenerator Rng
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

        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA)key;
        }

        public override byte[] CreateKeyExchange(byte[] rgbData)
        {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (_rsaKey is RSACryptoServiceProvider)
            {
                return ((RSACryptoServiceProvider)_rsaKey).Encrypt(rgbData, true);
            }
            else
            {
                return Utils.RsaOaepEncrypt(_rsaKey, SHA1.Create(), new PKCS1MaskGenerationMethod(), RandomNumberGenerator.Create(), rgbData);
            }
        }

        public override byte[] CreateKeyExchange(byte[] rgbData, Type symAlgType)
        {
            return CreateKeyExchange(rgbData);
        }
    }
}