namespace System.Security.Cryptography
{
    public sealed class SHA1CryptoServiceProvider : SHA1
    {
        private SafeHashHandle _safeHashHandle = null;
        public SHA1CryptoServiceProvider()
        {
            _safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, Constants.CALG_SHA1);
        }

        protected override void Dispose(bool disposing)
        {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose();
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose();
            _safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, Constants.CALG_SHA1);
        }

        protected override void HashCore(byte[] rgb, int ibStart, int cbSize)
        {
            Utils.HashData(_safeHashHandle, rgb, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            return Utils.EndHash(_safeHashHandle);
        }
    }
}