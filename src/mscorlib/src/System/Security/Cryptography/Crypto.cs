using System.Globalization;

using Microsoft.Win32;

namespace System.Security.Cryptography
{
    public enum CipherMode
    {
        CBC = 1,
        ECB = 2,
        OFB = 3,
        CFB = 4,
        CTS = 5
    }

    public enum PaddingMode
    {
        None = 1,
        PKCS7 = 2,
        Zeros = 3,
        ANSIX923 = 4,
        ISO10126 = 5
    }

    public sealed class KeySizes
    {
        private int m_minSize;
        private int m_maxSize;
        private int m_skipSize;
        public int MinSize
        {
            get
            {
                return m_minSize;
            }
        }

        public int MaxSize
        {
            get
            {
                return m_maxSize;
            }
        }

        public int SkipSize
        {
            get
            {
                return m_skipSize;
            }
        }

        public KeySizes(int minSize, int maxSize, int skipSize)
        {
            m_minSize = minSize;
            m_maxSize = maxSize;
            m_skipSize = skipSize;
        }
    }

    public class CryptographicException : SystemException
    {
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
        public CryptographicException(): base (Environment.GetResourceString("Arg_CryptographyException"))
        {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO);
        }

        public CryptographicException(String message): base (message)
        {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO);
        }

        public CryptographicException(String format, String insert): base (String.Format(CultureInfo.CurrentCulture, format, insert))
        {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO);
        }

        public CryptographicException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO);
        }

        public CryptographicException(int hr): this (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 ? GetLegacyNetCFMessage(hr) : Win32Native.GetMessage(hr))
        {
            if ((hr & 0x80000000) != 0x80000000)
                hr = (hr & 0x0000FFFF) | unchecked ((int)0x80070000);
            SetErrorCode(hr);
        }

        private static void ThrowCryptographicException(int hr)
        {
            throw new CryptographicException(hr);
        }

        private static string GetLegacyNetCFMessage(int hr)
        {
            if ((uint)hr == 0x8000701a)
            {
                return Environment.GetResourceString("Cryptography_LegacyNetCF_CSP_CouldNotAcquire");
            }

            return Environment.GetResourceString("Cryptography_LegacyNetCF_UnknownError", hr.ToString("X", CultureInfo.InvariantCulture));
        }
    }

    public class CryptographicUnexpectedOperationException : CryptographicException
    {
        public CryptographicUnexpectedOperationException(): base ()
        {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO_UNEX_OPER);
        }

        public CryptographicUnexpectedOperationException(String message): base (message)
        {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO_UNEX_OPER);
        }

        public CryptographicUnexpectedOperationException(String format, String insert): base (String.Format(CultureInfo.CurrentCulture, format, insert))
        {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO_UNEX_OPER);
        }

        public CryptographicUnexpectedOperationException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.CORSEC_E_CRYPTO_UNEX_OPER);
        }
    }
}