using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography.X509Certificates;

namespace System.Security.Cryptography
{
    public class DSASignatureDeformatter : AsymmetricSignatureDeformatter
    {
        DSA _dsaKey;
        string _oid;
        public DSASignatureDeformatter()
        {
            _oid = CryptoConfig.MapNameToOID("SHA1", OidGroup.HashAlgorithm);
        }

        public DSASignatureDeformatter(AsymmetricAlgorithm key): this ()
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _dsaKey = (DSA)key;
        }

        public override void SetKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            _dsaKey = (DSA)key;
        }

        public override void SetHashAlgorithm(string strName)
        {
            if (CryptoConfig.MapNameToOID(strName, OidGroup.HashAlgorithm) != _oid)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOperation"));
        }

        public override bool VerifySignature(byte[] rgbHash, byte[] rgbSignature)
        {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null)
                throw new ArgumentNullException("rgbSignature");
            Contract.EndContractBlock();
            if (_dsaKey == null)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_MissingKey"));
            return _dsaKey.VerifySignature(rgbHash, rgbSignature);
        }
    }
}