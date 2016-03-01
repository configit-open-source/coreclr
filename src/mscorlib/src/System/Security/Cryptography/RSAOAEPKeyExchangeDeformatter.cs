using System.Diagnostics.Contracts;

namespace System.Security.Cryptography
{
    public class RSAOAEPKeyExchangeDeformatter : AsymmetricKeyExchangeDeformatter
    {
        private RSA _rsaKey;
        public RSAOAEPKeyExchangeDeformatter()
        {
        }

        public RSAOAEPKeyExchangeDeformatter(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA)key;
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

        public override byte[] DecryptKeyExchange(byte[] rgbData)
        {
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (_rsaKey is RSACryptoServiceProvider)
            {
                return ((RSACryptoServiceProvider)_rsaKey).Decrypt(rgbData, true);
            }
            else
            {
                return Utils.RsaOaepDecrypt(_rsaKey, SHA1.Create(), new PKCS1MaskGenerationMethod(), rgbData);
            }
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA)key;
        }
    }
}