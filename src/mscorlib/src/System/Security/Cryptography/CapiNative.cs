using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static class CapiNative
    {
        internal enum AlgorithmClass
        {
            Any = (0 << 13),
            Signature = (1 << 13),
            Hash = (4 << 13),
            KeyExchange = (5 << 13)}

        internal enum AlgorithmType
        {
            Any = (0 << 9),
            Rsa = (2 << 9)}

        internal enum AlgorithmSubId
        {
            Any = 0,
            RsaAny = 0,
            Sha1 = 4,
            Sha256 = 12,
            Sha384 = 13,
            Sha512 = 14
        }

        internal enum AlgorithmID
        {
            None = 0,
            RsaSign = (AlgorithmClass.Signature | AlgorithmType.Rsa | AlgorithmSubId.RsaAny),
            RsaKeyExchange = (AlgorithmClass.KeyExchange | AlgorithmType.Rsa | AlgorithmSubId.RsaAny),
            Sha1 = (AlgorithmClass.Hash | AlgorithmType.Any | AlgorithmSubId.Sha1),
            Sha256 = (AlgorithmClass.Hash | AlgorithmType.Any | AlgorithmSubId.Sha256),
            Sha384 = (AlgorithmClass.Hash | AlgorithmType.Any | AlgorithmSubId.Sha384),
            Sha512 = (AlgorithmClass.Hash | AlgorithmType.Any | AlgorithmSubId.Sha512)}

        [Flags]
        internal enum CryptAcquireContextFlags
        {
            None = 0x00000000,
            NewKeyset = 0x00000008,
            DeleteKeyset = 0x00000010,
            MachineKeyset = 0x00000020,
            Silent = 0x00000040,
            VerifyContext = unchecked ((int)0xF0000000)}

        internal enum ErrorCode
        {
            Ok = 0x00000000,
            MoreData = 0x000000ea,
            BadHash = unchecked ((int)0x80090002),
            BadData = unchecked ((int)0x80090005),
            BadSignature = unchecked ((int)0x80090006),
            NoKey = unchecked ((int)0x8009000d)}

        internal enum HashProperty
        {
            None = 0,
            HashValue = 0x0002,
            HashSize = 0x0004
        }

        [Flags]
        internal enum KeyGenerationFlags
        {
            None = 0x00000000,
            Exportable = 0x00000001,
            UserProtected = 0x00000002,
            Archivable = 0x00004000
        }

        internal enum KeyProperty
        {
            None = 0,
            AlgorithmID = 7,
            KeyLength = 9
        }

        internal enum KeySpec
        {
            KeyExchange = 1,
            Signature = 2
        }

        internal static class ProviderNames
        {
            internal const string MicrosoftEnhanced = "Microsoft Enhanced Cryptographic Provider v1.0";
        }

        internal enum ProviderType
        {
            RsaFull = 1
        }

        internal static class UnsafeNativeMethods
        {
            internal static extern bool CryptAcquireContext([Out] out SafeCspHandle phProv, string pszContainer, string pszProvider, ProviderType dwProvType, CryptAcquireContextFlags dwFlags);
            internal static extern bool CryptCreateHash(SafeCspHandle hProv, AlgorithmID Algid, IntPtr hKey, int dwFlags, [Out] out SafeCspHashHandle phHash);
            internal static extern bool CryptGenKey(SafeCspHandle hProv, int Algid, uint dwFlags, [Out] out SafeCspKeyHandle phKey);
            internal static extern bool CryptGenRandom(SafeCspHandle hProv, int dwLen, [In, Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbBuffer);
            internal static extern unsafe bool CryptGenRandom(SafeCspHandle hProv, int dwLen, byte *pbBuffer);
            internal static extern bool CryptGetHashParam(SafeCspHashHandle hHash, HashProperty dwParam, [In, Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbData, [In, Out] ref int pdwDataLen, int dwFlags);
            internal static extern bool CryptGetKeyParam(SafeCspKeyHandle hKey, KeyProperty dwParam, [In, Out, MarshalAs(UnmanagedType.LPArray)] byte[] pbData, [In, Out] ref int pdwDataLen, int dwFlags);
            internal static extern bool CryptImportKey(SafeCspHandle hProv, [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbData, int pdwDataLen, IntPtr hPubKey, KeyGenerationFlags dwFlags, [Out] out SafeCspKeyHandle phKey);
            internal static extern bool CryptSetHashParam(SafeCspHashHandle hHash, HashProperty dwParam, [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbData, int dwFlags);
            internal static extern bool CryptVerifySignature(SafeCspHashHandle hHash, [In, MarshalAs(UnmanagedType.LPArray)] byte[] pbSignature, int dwSigLen, SafeCspKeyHandle hPubKey, string sDescription, int dwFlags);
        }

        internal static SafeCspHandle AcquireCsp(string keyContainer, string providerName, ProviderType providerType, CryptAcquireContextFlags flags)
        {
            Contract.Assert(keyContainer == null, "Key containers are not supported");
            if (((flags & CryptAcquireContextFlags.VerifyContext) == CryptAcquireContextFlags.VerifyContext) && ((flags & CryptAcquireContextFlags.MachineKeyset) == CryptAcquireContextFlags.MachineKeyset))
            {
                flags &= ~CryptAcquireContextFlags.MachineKeyset;
            }

            SafeCspHandle cspHandle = null;
            if (!UnsafeNativeMethods.CryptAcquireContext(out cspHandle, keyContainer, providerName, providerType, flags))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return cspHandle;
        }

        internal static SafeCspHashHandle CreateHashAlgorithm(SafeCspHandle cspHandle, AlgorithmID algorithm)
        {
            Contract.Assert(cspHandle != null && !cspHandle.IsInvalid, "cspHandle != null && !cspHandle.IsInvalid");
            Contract.Assert(((AlgorithmClass)algorithm & AlgorithmClass.Hash) == AlgorithmClass.Hash, "Invalid hash algorithm");
            SafeCspHashHandle hashHandle = null;
            if (!UnsafeNativeMethods.CryptCreateHash(cspHandle, algorithm, IntPtr.Zero, 0, out hashHandle))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return hashHandle;
        }

        internal static void GenerateRandomBytes(SafeCspHandle cspHandle, byte[] buffer)
        {
            Contract.Assert(cspHandle != null && !cspHandle.IsInvalid, "cspHandle != null && !cspHandle.IsInvalid");
            Contract.Assert(buffer != null && buffer.Length > 0, "buffer != null && buffer.Length > 0");
            if (!UnsafeNativeMethods.CryptGenRandom(cspHandle, buffer.Length, buffer))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }
        }

        internal static unsafe void GenerateRandomBytes(SafeCspHandle cspHandle, byte[] buffer, int offset, int count)
        {
            Contract.Assert(cspHandle != null && !cspHandle.IsInvalid, "cspHandle != null && !cspHandle.IsInvalid");
            Contract.Assert(buffer != null && buffer.Length > 0, "buffer != null && buffer.Length > 0");
            Contract.Assert(offset >= 0 && count > 0, "offset >= 0 && count > 0");
            Contract.Assert(buffer.Length >= offset + count, "buffer.Length >= offset + count");
            fixed (byte *pBuffer = &buffer[offset])
            {
                if (!UnsafeNativeMethods.CryptGenRandom(cspHandle, count, pBuffer))
                {
                    throw new CryptographicException(Marshal.GetLastWin32Error());
                }
            }
        }

        internal static int GetHashPropertyInt32(SafeCspHashHandle hashHandle, HashProperty property)
        {
            byte[] rawProperty = GetHashProperty(hashHandle, property);
            Contract.Assert(rawProperty.Length == sizeof (int) || rawProperty.Length == 0, "Unexpected property size");
            return rawProperty.Length == sizeof (int) ? BitConverter.ToInt32(rawProperty, 0) : 0;
        }

        internal static byte[] GetHashProperty(SafeCspHashHandle hashHandle, HashProperty property)
        {
            Contract.Assert(hashHandle != null && !hashHandle.IsInvalid, "keyHandle != null && !keyHandle.IsInvalid");
            int bufferSize = 0;
            byte[] buffer = null;
            if (!UnsafeNativeMethods.CryptGetHashParam(hashHandle, property, buffer, ref bufferSize, 0))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != (int)ErrorCode.MoreData)
                {
                    throw new CryptographicException(errorCode);
                }
            }

            buffer = new byte[bufferSize];
            if (!UnsafeNativeMethods.CryptGetHashParam(hashHandle, property, buffer, ref bufferSize, 0))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return buffer;
        }

        internal static int GetKeyPropertyInt32(SafeCspKeyHandle keyHandle, KeyProperty property)
        {
            byte[] rawProperty = GetKeyProperty(keyHandle, property);
            Contract.Assert(rawProperty.Length == sizeof (int) || rawProperty.Length == 0, "Unexpected property size");
            return rawProperty.Length == sizeof (int) ? BitConverter.ToInt32(rawProperty, 0) : 0;
        }

        internal static byte[] GetKeyProperty(SafeCspKeyHandle keyHandle, KeyProperty property)
        {
            Contract.Assert(keyHandle != null && !keyHandle.IsInvalid, "keyHandle != null && !keyHandle.IsInvalid");
            int bufferSize = 0;
            byte[] buffer = null;
            if (!UnsafeNativeMethods.CryptGetKeyParam(keyHandle, property, buffer, ref bufferSize, 0))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode != (int)ErrorCode.MoreData)
                {
                    throw new CryptographicException(errorCode);
                }
            }

            buffer = new byte[bufferSize];
            if (!UnsafeNativeMethods.CryptGetKeyParam(keyHandle, property, buffer, ref bufferSize, 0))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }

            return buffer;
        }

        internal static void SetHashProperty(SafeCspHashHandle hashHandle, HashProperty property, byte[] value)
        {
            Contract.Assert(hashHandle != null && !hashHandle.IsInvalid, "hashHandle != null && !hashHandle.IsInvalid");
            if (!UnsafeNativeMethods.CryptSetHashParam(hashHandle, property, value, 0))
            {
                throw new CryptographicException(Marshal.GetLastWin32Error());
            }
        }

        internal static bool VerifySignature(SafeCspHandle cspHandle, SafeCspKeyHandle keyHandle, AlgorithmID signatureAlgorithm, AlgorithmID hashAlgorithm, byte[] hashValue, byte[] signature)
        {
            Contract.Assert(cspHandle != null && !cspHandle.IsInvalid, "cspHandle != null && !cspHandle.IsInvalid");
            Contract.Assert(keyHandle != null && !keyHandle.IsInvalid, "keyHandle != null && !keyHandle.IsInvalid");
            Contract.Assert(((AlgorithmClass)signatureAlgorithm & AlgorithmClass.Signature) == AlgorithmClass.Signature, "Invalid signature algorithm");
            Contract.Assert(((AlgorithmClass)hashAlgorithm & AlgorithmClass.Hash) == AlgorithmClass.Hash, "Invalid hash algorithm");
            Contract.Assert(hashValue != null, "hashValue != null");
            Contract.Assert(signature != null, "signature != null");
            byte[] signatureValue = new byte[signature.Length];
            Array.Copy(signature, signatureValue, signatureValue.Length);
            Array.Reverse(signatureValue);
            using (SafeCspHashHandle hashHandle = CreateHashAlgorithm(cspHandle, hashAlgorithm))
            {
                if (hashValue.Length != GetHashPropertyInt32(hashHandle, HashProperty.HashSize))
                {
                    throw new CryptographicException((int)ErrorCode.BadHash);
                }

                SetHashProperty(hashHandle, HashProperty.HashValue, hashValue);
                if (UnsafeNativeMethods.CryptVerifySignature(hashHandle, signatureValue, signatureValue.Length, keyHandle, null, 0))
                {
                    return true;
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error != (int)ErrorCode.BadSignature)
                    {
                        throw new CryptographicException(error);
                    }

                    return false;
                }
            }
        }
    }

    internal sealed class SafeCspHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCspHandle(): base (true)
        {
        }

        private extern static bool CryptReleaseContext(IntPtr hProv, int dwFlags);
        protected override bool ReleaseHandle()
        {
            return CryptReleaseContext(handle, 0);
        }
    }

    internal sealed class SafeCspHashHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeCspHashHandle(): base (true)
        {
        }

        private extern static bool CryptDestroyHash(IntPtr hKey);
        protected override bool ReleaseHandle()
        {
            return CryptDestroyHash(handle);
        }
    }

    internal sealed class SafeCspKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeCspKeyHandle(): base (true)
        {
        }

        private extern static bool CryptDestroyKey(IntPtr hKey);
        protected override bool ReleaseHandle()
        {
            return CryptDestroyKey(handle);
        }
    }
}