

namespace System.Security.Cryptography
{
    public sealed class MD5CryptoServiceProvider : MD5
    {
        private SafeHashHandle _safeHashHandle = null;
        public MD5CryptoServiceProvider()
        {
            if (CryptoConfig.AllowOnlyFipsAlgorithms)
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
                        _safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, Constants.CALG_MD5);
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
            _safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, Constants.CALG_MD5);
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