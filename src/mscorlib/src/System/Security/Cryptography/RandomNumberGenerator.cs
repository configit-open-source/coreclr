namespace System.Security.Cryptography
{
    public abstract class RandomNumberGenerator : IDisposable
    {
        protected RandomNumberGenerator()
        {
        }

        static public RandomNumberGenerator Create()
        {
            return Create("System.Security.Cryptography.RandomNumberGenerator");
        }

        static public RandomNumberGenerator Create(String rngName)
        {
            return (RandomNumberGenerator)CryptoConfig.CreateFromName(rngName);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            return;
        }

        public abstract void GetBytes(byte[] data);
        public virtual void GetBytes(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (offset + count > data.Length)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            if (count > 0)
            {
                byte[] tempData = new byte[count];
                GetBytes(tempData);
                Array.Copy(tempData, 0, data, offset, count);
            }
        }

        public virtual void GetNonZeroBytes(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}