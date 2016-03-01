namespace System.Security.Cryptography
{
    public abstract class KeyedHashAlgorithm : HashAlgorithm
    {
        protected byte[] KeyValue;
        protected KeyedHashAlgorithm()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (KeyValue != null)
                    Array.Clear(KeyValue, 0, KeyValue.Length);
                KeyValue = null;
            }

            base.Dispose(disposing);
        }

        public virtual byte[] Key
        {
            get
            {
                return (byte[])KeyValue.Clone();
            }

            set
            {
                if (State != 0)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_HashKeySet"));
                KeyValue = (byte[])value.Clone();
            }
        }

        new static public KeyedHashAlgorithm Create()
        {
            return Create("System.Security.Cryptography.KeyedHashAlgorithm");
        }

        new static public KeyedHashAlgorithm Create(String algName)
        {
            return (KeyedHashAlgorithm)CryptoConfig.CreateFromName(algName);
        }
    }
}