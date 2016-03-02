

namespace System.Security.Cryptography
{
    public sealed class RNGCryptoServiceProvider : RandomNumberGenerator
    {
        SafeCspHandle m_cspHandle;
        public RNGCryptoServiceProvider()
        {
            m_cspHandle = CapiNative.AcquireCsp(null, CapiNative.ProviderNames.MicrosoftEnhanced, CapiNative.ProviderType.RsaFull, CapiNative.CryptAcquireContextFlags.VerifyContext);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (m_cspHandle != null)
                    {
                        m_cspHandle.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override void GetBytes(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

                        if (data.Length > 0)
            {
                CapiNative.GenerateRandomBytes(m_cspHandle, data);
            }
        }

        public override void GetBytes(byte[] data, int offset, int count)
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
                CapiNative.GenerateRandomBytes(m_cspHandle, data, offset, count);
            }
        }

        private static extern void GetBytes(SafeProvHandle hProv, byte[] randomBytes, int count);
        private static extern void GetNonZeroBytes(SafeProvHandle hProv, byte[] randomBytes, int count);
    }
}