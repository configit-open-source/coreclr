namespace System.Security.Cryptography
{
    public abstract class AsymmetricAlgorithm : IDisposable
    {
        protected int KeySizeValue;
        protected KeySizes[] LegalKeySizesValue;
        protected AsymmetricAlgorithm()
        {
        }

        public void Dispose()
        {
            Clear();
        }

        public void Clear()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            return;
        }

        public virtual int KeySize
        {
            get
            {
                return KeySizeValue;
            }

            set
            {
                int i;
                int j;
                for (i = 0; i < LegalKeySizesValue.Length; i++)
                {
                    if (LegalKeySizesValue[i].SkipSize == 0)
                    {
                        if (LegalKeySizesValue[i].MinSize == value)
                        {
                            KeySizeValue = value;
                            return;
                        }
                    }
                    else
                    {
                        for (j = LegalKeySizesValue[i].MinSize; j <= LegalKeySizesValue[i].MaxSize; j += LegalKeySizesValue[i].SkipSize)
                        {
                            if (j == value)
                            {
                                KeySizeValue = value;
                                return;
                            }
                        }
                    }
                }

                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
            }
        }

        public virtual KeySizes[] LegalKeySizes
        {
            get
            {
                return (KeySizes[])LegalKeySizesValue.Clone();
            }
        }

        public virtual String SignatureAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual String KeyExchangeAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        static public AsymmetricAlgorithm Create()
        {
            return Create("System.Security.Cryptography.AsymmetricAlgorithm");
        }

        static public AsymmetricAlgorithm Create(String algName)
        {
            return (AsymmetricAlgorithm)CryptoConfig.CreateFromName(algName);
        }

        public virtual void FromXmlString(String xmlString)
        {
            throw new NotImplementedException();
        }

        public virtual String ToXmlString(bool includePrivateParameters)
        {
            throw new NotImplementedException();
        }
    }
}