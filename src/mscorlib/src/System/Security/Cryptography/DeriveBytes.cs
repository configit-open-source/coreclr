namespace System.Security.Cryptography
{
    public abstract class DeriveBytes : IDisposable
    {
        public abstract byte[] GetBytes(int cb);
        public abstract void Reset();
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            return;
        }
    }
}