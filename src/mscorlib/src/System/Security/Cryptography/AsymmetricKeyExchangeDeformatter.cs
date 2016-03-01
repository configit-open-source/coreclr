namespace System.Security.Cryptography
{
    using System;

    public abstract class AsymmetricKeyExchangeDeformatter
    {
        protected AsymmetricKeyExchangeDeformatter()
        {
        }

        public abstract String Parameters
        {
            get;
            set;
        }

        abstract public void SetKey(AsymmetricAlgorithm key);
        abstract public byte[] DecryptKeyExchange(byte[] rgb);
    }
}