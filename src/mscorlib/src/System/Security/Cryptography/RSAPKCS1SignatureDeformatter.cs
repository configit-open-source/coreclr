using System.Diagnostics.Contracts;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography
{
    public class RSAPKCS1SignatureDeformatter : AsymmetricSignatureDeformatter
    {
        private RSA _rsaKey;
        private String _strOID;
        public RSAPKCS1SignatureDeformatter()
        {
        }

        public RSAPKCS1SignatureDeformatter(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA)key;
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _rsaKey = (RSA)key;
        }

        public override void SetHashAlgorithm(String strName)
        {
            _strOID = CryptoConfig.MapNameToOID(strName, OidGroup.HashAlgorithm);
        }

        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
        {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null)
                throw new ArgumentNullException("rgbSignature");
            Contract.EndContractBlock();
            if (_strOID == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingOID"));
            if (_rsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            if (_rsaKey is RSACryptoServiceProvider)
            {
                int calgHash = X509Utils.GetAlgIdFromOid(_strOID, OidGroup.HashAlgorithm);
                return ((RSACryptoServiceProvider)_rsaKey).VerifyHash(rgbHash, calgHash, rgbSignature);
            }
            else
            {
                byte[] pad = Utils.RsaPkcs1Padding(_rsaKey, CryptoConfig.EncodeOID(_strOID), rgbHash);
                return Utils.CompareBigIntArrays(_rsaKey.EncryptValue(rgbSignature), pad);
            }
        }
    }
}