using System.Diagnostics.Contracts;

namespace System.Security.Cryptography
{
    public abstract class AsymmetricSignatureDeformatter
    {
        protected AsymmetricSignatureDeformatter()
        {
        }

        abstract public void SetKey(AsymmetricAlgorithm key);
        abstract public void SetHashAlgorithm(String strName);
        public virtual bool VerifySignature(HashAlgorithm hash, byte[] rgbSignature)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            Contract.EndContractBlock();
            SetHashAlgorithm(hash.ToString());
            return VerifySignature(hash.Hash, rgbSignature);
        }

        abstract public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);
    }
}