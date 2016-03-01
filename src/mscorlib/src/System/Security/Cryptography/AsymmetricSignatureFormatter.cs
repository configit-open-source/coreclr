namespace System.Security.Cryptography
{
    using System;
    using System.Diagnostics.Contracts;

    public abstract class AsymmetricSignatureFormatter
    {
        protected AsymmetricSignatureFormatter()
        {
        }

        abstract public void SetKey(AsymmetricAlgorithm key);
        abstract public void SetHashAlgorithm(String strName);
        public virtual byte[] CreateSignature(HashAlgorithm hash)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            Contract.EndContractBlock();
            SetHashAlgorithm(hash.ToString());
            return CreateSignature(hash.Hash);
        }

        abstract public byte[] CreateSignature(byte[] rgbHash);
    }
}