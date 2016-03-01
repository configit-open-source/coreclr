namespace System.Security.Cryptography
{
    using System;

    public abstract class AsymmetricKeyExchangeFormatter
    {
        protected AsymmetricKeyExchangeFormatter()
        {
        }

        public abstract String Parameters
        {
            get;
        }

        abstract public void SetKey(AsymmetricAlgorithm key);
        abstract public byte[] CreateKeyExchange(byte[] data);
        abstract public byte[] CreateKeyExchange(byte[] data, Type symAlgType);
    }
}