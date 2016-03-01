using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

using Microsoft.Win32;

namespace System.Security.Cryptography
{
    internal enum CspAlgorithmType
    {
        Rsa = 0,
        Dss = 1
    }

    internal static class Constants
    {
        internal const int S_OK = 0;
        internal const int NTE_FILENOTFOUND = unchecked ((int)0x80070002);
        internal const int NTE_NO_KEY = unchecked ((int)0x8009000D);
        internal const int NTE_BAD_KEYSET = unchecked ((int)0x80090016);
        internal const int NTE_KEYSET_NOT_DEF = unchecked ((int)0x80090019);
        internal const int KP_IV = 1;
        internal const int KP_MODE = 4;
        internal const int KP_MODE_BITS = 5;
        internal const int KP_EFFECTIVE_KEYLEN = 19;
        internal const int ALG_CLASS_SIGNATURE = (1 << 13);
        internal const int ALG_CLASS_DATA_ENCRYPT = (3 << 13);
        internal const int ALG_CLASS_HASH = (4 << 13);
        internal const int ALG_CLASS_KEY_EXCHANGE = (5 << 13);
        internal const int ALG_TYPE_DSS = (1 << 9);
        internal const int ALG_TYPE_RSA = (2 << 9);
        internal const int ALG_TYPE_BLOCK = (3 << 9);
        internal const int ALG_TYPE_STREAM = (4 << 9);
        internal const int ALG_TYPE_ANY = (0);
        internal const int CALG_MD5 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 3);
        internal const int CALG_SHA1 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 4);
        internal const int CALG_SHA_256 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 12);
        internal const int CALG_SHA_384 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 13);
        internal const int CALG_SHA_512 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 14);
        internal const int CALG_RSA_KEYX = (ALG_CLASS_KEY_EXCHANGE | ALG_TYPE_RSA | 0);
        internal const int CALG_RSA_SIGN = (ALG_CLASS_SIGNATURE | ALG_TYPE_RSA | 0);
        internal const int CALG_DSS_SIGN = (ALG_CLASS_SIGNATURE | ALG_TYPE_DSS | 0);
        internal const int CALG_DES = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 1);
        internal const int CALG_RC2 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 2);
        internal const int CALG_3DES = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 3);
        internal const int CALG_3DES_112 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 9);
        internal const int CALG_AES_128 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 14);
        internal const int CALG_AES_192 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 15);
        internal const int CALG_AES_256 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 16);
        internal const int CALG_RC4 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_STREAM | 1);
        internal const int PROV_RSA_FULL = 1;
        internal const int PROV_DSS_DH = 13;
        internal const int PROV_RSA_AES = 24;
        internal const int AT_KEYEXCHANGE = 1;
        internal const int AT_SIGNATURE = 2;
        internal const int PUBLICKEYBLOB = 0x6;
        internal const int PRIVATEKEYBLOB = 0x7;
        internal const int CRYPT_OAEP = 0x40;
        internal const uint CRYPT_VERIFYCONTEXT = 0xF0000000;
        internal const uint CRYPT_NEWKEYSET = 0x00000008;
        internal const uint CRYPT_DELETEKEYSET = 0x00000010;
        internal const uint CRYPT_MACHINE_KEYSET = 0x00000020;
        internal const uint CRYPT_SILENT = 0x00000040;
        internal const uint CRYPT_EXPORTABLE = 0x00000001;
        internal const uint CLR_KEYLEN = 1;
        internal const uint CLR_PUBLICKEYONLY = 2;
        internal const uint CLR_EXPORTABLE = 3;
        internal const uint CLR_REMOVABLE = 4;
        internal const uint CLR_HARDWARE = 5;
        internal const uint CLR_ACCESSIBLE = 6;
        internal const uint CLR_PROTECTED = 7;
        internal const uint CLR_UNIQUE_CONTAINER = 8;
        internal const uint CLR_ALGID = 9;
        internal const uint CLR_PP_CLIENT_HWND = 10;
        internal const uint CLR_PP_PIN = 11;
        internal const string OID_RSA_SMIMEalgCMS3DESwrap = "1.2.840.113549.1.9.16.3.6";
        internal const string OID_RSA_MD5 = "1.2.840.113549.2.5";
        internal const string OID_RSA_RC2CBC = "1.2.840.113549.3.2";
        internal const string OID_RSA_DES_EDE3_CBC = "1.2.840.113549.3.7";
        internal const string OID_OIWSEC_desCBC = "1.3.14.3.2.7";
        internal const string OID_OIWSEC_SHA1 = "1.3.14.3.2.26";
        internal const string OID_OIWSEC_SHA256 = "2.16.840.1.101.3.4.2.1";
        internal const string OID_OIWSEC_SHA384 = "2.16.840.1.101.3.4.2.2";
        internal const string OID_OIWSEC_SHA512 = "2.16.840.1.101.3.4.2.3";
        internal const string OID_OIWSEC_RIPEMD160 = "1.3.36.3.2.1";
    }

    internal static class Utils
    {
        static Utils()
        {
        }

        internal const int DefaultRsaProviderType = Constants.PROV_RSA_AES;
        private static Object s_InternalSyncObject = new Object();
        private static Object InternalSyncObject
        {
            get
            {
                return s_InternalSyncObject;
            }
        }

        private static volatile SafeProvHandle _safeProvHandle;
        internal static SafeProvHandle StaticProvHandle
        {
            [System.Security.SecurityCritical]
            get
            {
                if (_safeProvHandle == null)
                {
                    lock (InternalSyncObject)
                    {
                        if (_safeProvHandle == null)
                        {
                            _safeProvHandle = AcquireProvHandle(new CspParameters(DefaultRsaProviderType));
                        }
                    }
                }

                return _safeProvHandle;
            }
        }

        private static volatile SafeProvHandle _safeDssProvHandle;
        internal static SafeProvHandle StaticDssProvHandle
        {
            [System.Security.SecurityCritical]
            get
            {
                if (_safeDssProvHandle == null)
                {
                    lock (InternalSyncObject)
                    {
                        if (_safeDssProvHandle == null)
                        {
                            _safeDssProvHandle = CreateProvHandle(new CspParameters(Constants.PROV_DSS_DH), true);
                        }
                    }
                }

                return _safeDssProvHandle;
            }
        }

        internal static SafeProvHandle AcquireProvHandle(CspParameters parameters)
        {
            if (parameters == null)
                parameters = new CspParameters(DefaultRsaProviderType);
            SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
            Utils._AcquireCSP(parameters, ref safeProvHandle);
            return safeProvHandle;
        }

        internal static SafeProvHandle CreateProvHandle(CspParameters parameters, bool randomKeyContainer)
        {
            SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
            int hr = Utils._OpenCSP(parameters, 0, ref safeProvHandle);
            KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
            if (hr != Constants.S_OK)
            {
                if ((parameters.Flags & CspProviderFlags.UseExistingKey) != 0 || (hr != Constants.NTE_KEYSET_NOT_DEF && hr != Constants.NTE_BAD_KEYSET && hr != Constants.NTE_FILENOTFOUND))
                    throw new CryptographicException(hr);
                if (!randomKeyContainer)
                {
                    if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    {
                        KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Create);
                        kp.AccessEntries.Add(entry);
                        kp.Demand();
                    }
                }

                Utils._CreateCSP(parameters, randomKeyContainer, ref safeProvHandle);
            }
            else
            {
                if (!randomKeyContainer)
                {
                    if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    {
                        KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Open);
                        kp.AccessEntries.Add(entry);
                        kp.Demand();
                    }
                }
            }

            return safeProvHandle;
        }

        internal static byte[] ExportCspBlobHelper(bool includePrivateParameters, CspParameters parameters, SafeKeyHandle safeKeyHandle)
        {
            if (includePrivateParameters)
            {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Export);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }
            }

            byte[] blob = null;
            Utils.ExportCspBlob(safeKeyHandle, includePrivateParameters ? Constants.PRIVATEKEYBLOB : Constants.PUBLICKEYBLOB, JitHelpers.GetObjectHandleOnStack(ref blob));
            return blob;
        }

        internal static void GetKeyPairHelper(CspAlgorithmType keyType, CspParameters parameters, bool randomKeyContainer, int dwKeySize, ref SafeProvHandle safeProvHandle, ref SafeKeyHandle safeKeyHandle)
        {
            SafeProvHandle TempFetchedProvHandle = Utils.CreateProvHandle(parameters, randomKeyContainer);
            if (parameters.ParentWindowHandle != IntPtr.Zero)
                SetProviderParameter(TempFetchedProvHandle, parameters.KeyNumber, Constants.CLR_PP_CLIENT_HWND, parameters.ParentWindowHandle);
            else if (parameters.KeyPassword != null)
            {
                IntPtr szPassword = Marshal.SecureStringToCoTaskMemAnsi(parameters.KeyPassword);
                try
                {
                    SetProviderParameter(TempFetchedProvHandle, parameters.KeyNumber, Constants.CLR_PP_PIN, szPassword);
                }
                finally
                {
                    if (szPassword != IntPtr.Zero)
                        Marshal.ZeroFreeCoTaskMemAnsi(szPassword);
                }
            }

            safeProvHandle = TempFetchedProvHandle;
            SafeKeyHandle TempFetchedKeyHandle = SafeKeyHandle.InvalidHandle;
            int hr = Utils._GetUserKey(safeProvHandle, parameters.KeyNumber, ref TempFetchedKeyHandle);
            if (hr != Constants.S_OK)
            {
                if ((parameters.Flags & CspProviderFlags.UseExistingKey) != 0 || hr != Constants.NTE_NO_KEY)
                    throw new CryptographicException(hr);
                Utils._GenerateKey(safeProvHandle, parameters.KeyNumber, parameters.Flags, dwKeySize, ref TempFetchedKeyHandle);
            }

            byte[] algid = (byte[])Utils._GetKeyParameter(TempFetchedKeyHandle, Constants.CLR_ALGID);
            int dwAlgId = (algid[0] | (algid[1] << 8) | (algid[2] << 16) | (algid[3] << 24));
            if ((keyType == CspAlgorithmType.Rsa && dwAlgId != Constants.CALG_RSA_KEYX && dwAlgId != Constants.CALG_RSA_SIGN) || (keyType == CspAlgorithmType.Dss && dwAlgId != Constants.CALG_DSS_SIGN))
            {
                TempFetchedKeyHandle.Dispose();
                throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_WrongKeySpec"));
            }

            safeKeyHandle = TempFetchedKeyHandle;
        }

        internal static void ImportCspBlobHelper(CspAlgorithmType keyType, byte[] keyBlob, bool publicOnly, ref CspParameters parameters, bool randomKeyContainer, ref SafeProvHandle safeProvHandle, ref SafeKeyHandle safeKeyHandle)
        {
            if (safeKeyHandle != null && !safeKeyHandle.IsClosed)
                safeKeyHandle.Dispose();
            safeKeyHandle = SafeKeyHandle.InvalidHandle;
            if (publicOnly)
            {
                parameters.KeyNumber = Utils._ImportCspBlob(keyBlob, keyType == CspAlgorithmType.Dss ? Utils.StaticDssProvHandle : Utils.StaticProvHandle, (CspProviderFlags)0, ref safeKeyHandle);
            }
            else
            {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Import);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }

                if (safeProvHandle == null)
                    safeProvHandle = Utils.CreateProvHandle(parameters, randomKeyContainer);
                parameters.KeyNumber = Utils._ImportCspBlob(keyBlob, safeProvHandle, parameters.Flags, ref safeKeyHandle);
            }
        }

        internal static CspParameters SaveCspParameters(CspAlgorithmType keyType, CspParameters userParameters, CspProviderFlags defaultFlags, ref bool randomKeyContainer)
        {
            CspParameters parameters;
            if (userParameters == null)
            {
                parameters = new CspParameters(keyType == CspAlgorithmType.Dss ? Constants.PROV_DSS_DH : DefaultRsaProviderType, null, null, defaultFlags);
            }
            else
            {
                ValidateCspFlags(userParameters.Flags);
                parameters = new CspParameters(userParameters);
            }

            if (parameters.KeyNumber == -1)
                parameters.KeyNumber = keyType == CspAlgorithmType.Dss ? Constants.AT_SIGNATURE : Constants.AT_KEYEXCHANGE;
            else if (parameters.KeyNumber == Constants.CALG_DSS_SIGN || parameters.KeyNumber == Constants.CALG_RSA_SIGN)
                parameters.KeyNumber = Constants.AT_SIGNATURE;
            else if (parameters.KeyNumber == Constants.CALG_RSA_KEYX)
                parameters.KeyNumber = Constants.AT_KEYEXCHANGE;
            randomKeyContainer = (parameters.Flags & CspProviderFlags.CreateEphemeralKey) == CspProviderFlags.CreateEphemeralKey;
            if (parameters.KeyContainerName == null && (parameters.Flags & CspProviderFlags.UseDefaultKeyContainer) == 0)
            {
                parameters.Flags |= CspProviderFlags.CreateEphemeralKey;
                randomKeyContainer = true;
            }

            return parameters;
        }

        private static void ValidateCspFlags(CspProviderFlags flags)
        {
            if ((flags & CspProviderFlags.UseExistingKey) != 0)
            {
                CspProviderFlags keyFlags = (CspProviderFlags.UseNonExportableKey | CspProviderFlags.UseArchivableKey | CspProviderFlags.UseUserProtectedKey);
                if ((flags & keyFlags) != CspProviderFlags.NoFlags)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"));
            }

            if ((flags & CspProviderFlags.UseUserProtectedKey) != 0)
            {
                if (!System.Environment.UserInteractive)
                    throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NotInteractive"));
                UIPermission uiPermission = new UIPermission(UIPermissionWindow.SafeTopLevelWindows);
                uiPermission.Demand();
            }
        }

        private static volatile RNGCryptoServiceProvider _rng;
        internal static RNGCryptoServiceProvider StaticRandomNumberGenerator
        {
            get
            {
                if (_rng == null)
                    _rng = new RNGCryptoServiceProvider();
                return _rng;
            }
        }

        internal static byte[] GenerateRandom(int keySize)
        {
            byte[] key = new byte[keySize];
            StaticRandomNumberGenerator.GetBytes(key);
            return key;
        }

        internal static bool ReadLegacyFipsPolicy()
        {
            Contract.Assert(Environment.OSVersion.Version.Major < 6, "CryptGetFIPSAlgorithmMode should be used on Vista+");
            try
            {
                using (RegistryKey fipsAlgorithmPolicyKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Lsa", false))
                {
                    if (fipsAlgorithmPolicyKey == null)
                        return false;
                    object data = fipsAlgorithmPolicyKey.GetValue("FIPSAlgorithmPolicy");
                    if (data == null)
                    {
                        return false;
                    }
                    else if (fipsAlgorithmPolicyKey.GetValueKind("FIPSAlgorithmPolicy") != RegistryValueKind.DWord)
                    {
                        return true;
                    }
                    else
                    {
                        return ((int)data != 0);
                    }
                }
            }
            catch (SecurityException)
            {
                return true;
            }
        }

        internal static bool HasAlgorithm(int dwCalg, int dwKeySize)
        {
            bool r = false;
            lock (InternalSyncObject)
            {
                r = SearchForAlgorithm(StaticProvHandle, dwCalg, dwKeySize);
            }

            return r;
        }

        internal static int ObjToAlgId(object hashAlg, OidGroup group)
        {
            if (hashAlg == null)
                throw new ArgumentNullException("hashAlg");
            Contract.EndContractBlock();
            string oidValue = null;
            string hashAlgString = hashAlg as string;
            if (hashAlgString != null)
            {
                oidValue = CryptoConfig.MapNameToOID(hashAlgString, group);
                if (oidValue == null)
                    oidValue = hashAlgString;
            }
            else if (hashAlg is HashAlgorithm)
            {
                oidValue = CryptoConfig.MapNameToOID(hashAlg.GetType().ToString(), group);
            }
            else if (hashAlg is Type)
            {
                oidValue = CryptoConfig.MapNameToOID(hashAlg.ToString(), group);
            }

            if (oidValue == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            return X509Utils.GetAlgIdFromOid(oidValue, group);
        }

        internal static HashAlgorithm ObjToHashAlgorithm(Object hashAlg)
        {
            if (hashAlg == null)
                throw new ArgumentNullException("hashAlg");
            Contract.EndContractBlock();
            HashAlgorithm hash = null;
            if (hashAlg is String)
            {
                hash = (HashAlgorithm)CryptoConfig.CreateFromName((string)hashAlg);
                if (hash == null)
                {
                    string oidFriendlyName = X509Utils.GetFriendlyNameFromOid((string)hashAlg, OidGroup.HashAlgorithm);
                    if (oidFriendlyName != null)
                        hash = (HashAlgorithm)CryptoConfig.CreateFromName(oidFriendlyName);
                }
            }
            else if (hashAlg is HashAlgorithm)
            {
                hash = (HashAlgorithm)hashAlg;
            }
            else if (hashAlg is Type)
            {
                hash = (HashAlgorithm)CryptoConfig.CreateFromName(hashAlg.ToString());
            }

            if (hash == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            return hash;
        }

        internal static String DiscardWhiteSpaces(string inputBuffer)
        {
            return DiscardWhiteSpaces(inputBuffer, 0, inputBuffer.Length);
        }

        internal static String DiscardWhiteSpaces(string inputBuffer, int inputOffset, int inputCount)
        {
            int i, iCount = 0;
            for (i = 0; i < inputCount; i++)
                if (Char.IsWhiteSpace(inputBuffer[inputOffset + i]))
                    iCount++;
            char[] output = new char[inputCount - iCount];
            iCount = 0;
            for (i = 0; i < inputCount; i++)
            {
                if (!Char.IsWhiteSpace(inputBuffer[inputOffset + i]))
                    output[iCount++] = inputBuffer[inputOffset + i];
            }

            return new String(output);
        }

        internal static int ConvertByteArrayToInt(byte[] input)
        {
            int dwOutput = 0;
            for (int i = 0; i < input.Length; i++)
            {
                dwOutput *= 256;
                dwOutput += input[i];
            }

            return (dwOutput);
        }

        internal static byte[] ConvertIntToByteArray(int dwInput)
        {
            byte[] temp = new byte[8];
            int t1;
            int t2;
            int i = 0;
            if (dwInput == 0)
                return new byte[1];
            t1 = dwInput;
            while (t1 > 0)
            {
                Contract.Assert(i < 8, "Got too big an int here!");
                t2 = t1 % 256;
                temp[i] = (byte)t2;
                t1 = (t1 - t2) / 256;
                i++;
            }

            byte[] output = new byte[i];
            for (int j = 0; j < i; j++)
            {
                output[j] = temp[i - j - 1];
            }

            return output;
        }

        internal static void ConvertIntToByteArray(uint dwInput, ref byte[] counter)
        {
            uint t1 = dwInput;
            uint t2;
            int i = 0;
            Array.Clear(counter, 0, counter.Length);
            if (dwInput == 0)
                return;
            while (t1 > 0)
            {
                Contract.Assert(i < 4, "Got too big an int here!");
                t2 = t1 % 256;
                counter[3 - i] = (byte)t2;
                t1 = (t1 - t2) / 256;
                i++;
            }
        }

        internal static byte[] FixupKeyParity(byte[] key)
        {
            byte[] oddParityKey = new byte[key.Length];
            for (int index = 0; index < key.Length; index++)
            {
                oddParityKey[index] = (byte)(key[index] & 0xfe);
                byte tmp1 = (byte)((oddParityKey[index] & 0xF) ^ (oddParityKey[index] >> 4));
                byte tmp2 = (byte)((tmp1 & 0x3) ^ (tmp1 >> 2));
                byte sumBitsMod2 = (byte)((tmp2 & 0x1) ^ (tmp2 >> 1));
                if (sumBitsMod2 == 0)
                    oddParityKey[index] |= 1;
            }

            return oddParityKey;
        }

        internal unsafe static void DWORDFromLittleEndian(uint *x, int digits, byte *block)
        {
            int i;
            int j;
            for (i = 0, j = 0; i < digits; i++, j += 4)
                x[i] = (uint)(block[j] | (block[j + 1] << 8) | (block[j + 2] << 16) | (block[j + 3] << 24));
        }

        internal static void DWORDToLittleEndian(byte[] block, uint[] x, int digits)
        {
            int i;
            int j;
            for (i = 0, j = 0; i < digits; i++, j += 4)
            {
                block[j] = (byte)(x[i] & 0xff);
                block[j + 1] = (byte)((x[i] >> 8) & 0xff);
                block[j + 2] = (byte)((x[i] >> 16) & 0xff);
                block[j + 3] = (byte)((x[i] >> 24) & 0xff);
            }
        }

        internal unsafe static void DWORDFromBigEndian(uint *x, int digits, byte *block)
        {
            int i;
            int j;
            for (i = 0, j = 0; i < digits; i++, j += 4)
                x[i] = (uint)((block[j] << 24) | (block[j + 1] << 16) | (block[j + 2] << 8) | block[j + 3]);
        }

        internal static void DWORDToBigEndian(byte[] block, uint[] x, int digits)
        {
            int i;
            int j;
            for (i = 0, j = 0; i < digits; i++, j += 4)
            {
                block[j] = (byte)((x[i] >> 24) & 0xff);
                block[j + 1] = (byte)((x[i] >> 16) & 0xff);
                block[j + 2] = (byte)((x[i] >> 8) & 0xff);
                block[j + 3] = (byte)(x[i] & 0xff);
            }
        }

        internal unsafe static void QuadWordFromBigEndian(UInt64*x, int digits, byte *block)
        {
            int i;
            int j;
            for (i = 0, j = 0; i < digits; i++, j += 8)
                x[i] = ((((UInt64)block[j]) << 56) | (((UInt64)block[j + 1]) << 48) | (((UInt64)block[j + 2]) << 40) | (((UInt64)block[j + 3]) << 32) | (((UInt64)block[j + 4]) << 24) | (((UInt64)block[j + 5]) << 16) | (((UInt64)block[j + 6]) << 8) | ((UInt64)block[j + 7]));
        }

        internal static void QuadWordToBigEndian(byte[] block, UInt64[] x, int digits)
        {
            int i;
            int j;
            for (i = 0, j = 0; i < digits; i++, j += 8)
            {
                block[j] = (byte)((x[i] >> 56) & 0xff);
                block[j + 1] = (byte)((x[i] >> 48) & 0xff);
                block[j + 2] = (byte)((x[i] >> 40) & 0xff);
                block[j + 3] = (byte)((x[i] >> 32) & 0xff);
                block[j + 4] = (byte)((x[i] >> 24) & 0xff);
                block[j + 5] = (byte)((x[i] >> 16) & 0xff);
                block[j + 6] = (byte)((x[i] >> 8) & 0xff);
                block[j + 7] = (byte)(x[i] & 0xff);
            }
        }

        internal static byte[] Int(uint i)
        {
            return unchecked (new byte[]{(byte)(i >> 24), (byte)(i >> 16), (byte)(i >> 8), (byte)i});
        }

        internal static byte[] RsaOaepEncrypt(RSA rsa, HashAlgorithm hash, PKCS1MaskGenerationMethod mgf, RandomNumberGenerator rng, byte[] data)
        {
            int cb = rsa.KeySize / 8;
            int cbHash = hash.HashSize / 8;
            if ((data.Length + 2 + 2 * cbHash) > cb)
                throw new CryptographicException(String.Format(null, Environment.GetResourceString("Cryptography_Padding_EncDataTooBig"), cb - 2 - 2 * cbHash));
            hash.ComputeHash(EmptyArray<Byte>.Value);
            byte[] DB = new byte[cb - cbHash];
            Buffer.InternalBlockCopy(hash.Hash, 0, DB, 0, cbHash);
            DB[DB.Length - data.Length - 1] = 1;
            Buffer.InternalBlockCopy(data, 0, DB, DB.Length - data.Length, data.Length);
            byte[] seed = new byte[cbHash];
            rng.GetBytes(seed);
            byte[] mask = mgf.GenerateMask(seed, DB.Length);
            for (int i = 0; i < DB.Length; i++)
            {
                DB[i] = (byte)(DB[i] ^ mask[i]);
            }

            mask = mgf.GenerateMask(DB, cbHash);
            for (int i = 0; i < seed.Length; i++)
            {
                seed[i] ^= mask[i];
            }

            byte[] pad = new byte[cb];
            Buffer.InternalBlockCopy(seed, 0, pad, 0, seed.Length);
            Buffer.InternalBlockCopy(DB, 0, pad, seed.Length, DB.Length);
            return rsa.EncryptValue(pad);
        }

        internal static byte[] RsaOaepDecrypt(RSA rsa, HashAlgorithm hash, PKCS1MaskGenerationMethod mgf, byte[] encryptedData)
        {
            int cb = rsa.KeySize / 8;
            byte[] data = null;
            try
            {
                data = rsa.DecryptValue(encryptedData);
            }
            catch (CryptographicException)
            {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            }

            int cbHash = hash.HashSize / 8;
            int zeros = cb - data.Length;
            if (zeros < 0 || zeros >= cbHash)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            byte[] seed = new byte[cbHash];
            Buffer.InternalBlockCopy(data, 0, seed, zeros, seed.Length - zeros);
            byte[] DB = new byte[data.Length - seed.Length + zeros];
            Buffer.InternalBlockCopy(data, seed.Length - zeros, DB, 0, DB.Length);
            byte[] mask = mgf.GenerateMask(DB, seed.Length);
            int i = 0;
            for (i = 0; i < seed.Length; i++)
            {
                seed[i] ^= mask[i];
            }

            mask = mgf.GenerateMask(seed, DB.Length);
            for (i = 0; i < DB.Length; i++)
            {
                DB[i] = (byte)(DB[i] ^ mask[i]);
            }

            hash.ComputeHash(EmptyArray<Byte>.Value);
            byte[] hashValue = hash.Hash;
            for (i = 0; i < cbHash; i++)
            {
                if (DB[i] != hashValue[i])
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            }

            for (; i < DB.Length; i++)
            {
                if (DB[i] == 1)
                    break;
                else if (DB[i] != 0)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            }

            if (i == DB.Length)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_OAEPDecoding"));
            i++;
            byte[] output = new byte[DB.Length - i];
            Buffer.InternalBlockCopy(DB, i, output, 0, output.Length);
            return output;
        }

        internal static byte[] RsaPkcs1Padding(RSA rsa, byte[] oid, byte[] hash)
        {
            int cb = rsa.KeySize / 8;
            byte[] pad = new byte[cb];
            byte[] data = new byte[oid.Length + 8 + hash.Length];
            data[0] = 0x30;
            int tmp = data.Length - 2;
            data[1] = (byte)tmp;
            data[2] = 0x30;
            tmp = oid.Length + 2;
            data[3] = (byte)tmp;
            Buffer.InternalBlockCopy(oid, 0, data, 4, oid.Length);
            data[4 + oid.Length] = 0x05;
            data[4 + oid.Length + 1] = 0x00;
            data[4 + oid.Length + 2] = 0x04;
            data[4 + oid.Length + 3] = (byte)hash.Length;
            Buffer.InternalBlockCopy(hash, 0, data, oid.Length + 8, hash.Length);
            int cb1 = cb - data.Length;
            if (cb1 <= 2)
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOID"));
            pad[0] = 0;
            pad[1] = 1;
            for (int i = 2; i < cb1 - 1; i++)
            {
                pad[i] = 0xff;
            }

            pad[cb1 - 1] = 0;
            Buffer.InternalBlockCopy(data, 0, pad, cb1, data.Length);
            return pad;
        }

        internal static bool CompareBigIntArrays(byte[] lhs, byte[] rhs)
        {
            if (lhs == null)
                return (rhs == null);
            int i = 0, j = 0;
            while (i < lhs.Length && lhs[i] == 0)
                i++;
            while (j < rhs.Length && rhs[j] == 0)
                j++;
            int count = (lhs.Length - i);
            if ((rhs.Length - j) != count)
                return false;
            for (int k = 0; k < count; k++)
            {
                if (lhs[i + k] != rhs[j + k])
                    return false;
            }

            return true;
        }

        internal static extern SafeHashHandle CreateHash(SafeProvHandle hProv, int algid);
        private static extern void EndHash(SafeHashHandle hHash, ObjectHandleOnStack retHash);
        internal static byte[] EndHash(SafeHashHandle hHash)
        {
            byte[] hash = null;
            EndHash(hHash, JitHelpers.GetObjectHandleOnStack(ref hash));
            return hash;
        }

        private static extern void ExportCspBlob(SafeKeyHandle hKey, int blobType, ObjectHandleOnStack retBlob);
        internal static extern bool GetPersistKeyInCsp(SafeProvHandle hProv);
        private static extern void HashData(SafeHashHandle hHash, byte[] data, int cbData, int ibStart, int cbSize);
        internal static void HashData(SafeHashHandle hHash, byte[] data, int ibStart, int cbSize)
        {
            HashData(hHash, data, data.Length, ibStart, cbSize);
        }

        private static extern bool SearchForAlgorithm(SafeProvHandle hProv, int algID, int keyLength);
        internal static extern void SetKeyParamDw(SafeKeyHandle hKey, int param, int dwValue);
        internal static extern void SetKeyParamRgb(SafeKeyHandle hKey, int param, byte[] value, int cbValue);
        internal static extern void SetPersistKeyInCsp(SafeProvHandle hProv, bool fPersistKeyInCsp);
        internal static extern void SetProviderParameter(SafeProvHandle hProv, int keyNumber, uint paramID, IntPtr pbData);
        private static extern void SignValue(SafeKeyHandle hKey, int keyNumber, int calgKey, int calgHash, byte[] hash, int cbHash, ObjectHandleOnStack retSignature);
        internal static byte[] SignValue(SafeKeyHandle hKey, int keyNumber, int calgKey, int calgHash, byte[] hash)
        {
            byte[] signature = null;
            SignValue(hKey, keyNumber, calgKey, calgHash, hash, hash.Length, JitHelpers.GetObjectHandleOnStack(ref signature));
            return signature;
        }

        private static extern bool VerifySign(SafeKeyHandle hKey, int calgKey, int calgHash, byte[] hash, int cbHash, byte[] signature, int cbSignature);
        internal static bool VerifySign(SafeKeyHandle hKey, int calgKey, int calgHash, byte[] hash, byte[] signature)
        {
            return VerifySign(hKey, calgKey, calgHash, hash, hash.Length, signature, signature.Length);
        }

        internal static extern void _CreateCSP(CspParameters param, bool randomKeyContainer, ref SafeProvHandle hProv);
        internal static extern int _DecryptData(SafeKeyHandle hKey, byte[] data, int ib, int cb, ref byte[] outputBuffer, int outputOffset, PaddingMode PaddingMode, bool fDone);
        internal static extern int _EncryptData(SafeKeyHandle hKey, byte[] data, int ib, int cb, ref byte[] outputBuffer, int outputOffset, PaddingMode PaddingMode, bool fDone);
        internal static extern void _ExportKey(SafeKeyHandle hKey, int blobType, object cspObject);
        internal static extern void _GenerateKey(SafeProvHandle hProv, int algid, CspProviderFlags flags, int keySize, ref SafeKeyHandle hKey);
        internal static extern bool _GetEnforceFipsPolicySetting();
        internal static extern byte[] _GetKeyParameter(SafeKeyHandle hKey, uint paramID);
        internal static extern object _GetProviderParameter(SafeProvHandle hProv, int keyNumber, uint paramID);
        internal static extern int _GetUserKey(SafeProvHandle hProv, int keyNumber, ref SafeKeyHandle hKey);
        internal static extern void _ImportBulkKey(SafeProvHandle hProv, int algid, bool useSalt, byte[] key, ref SafeKeyHandle hKey);
        internal static extern int _ImportCspBlob(byte[] keyBlob, SafeProvHandle hProv, CspProviderFlags flags, ref SafeKeyHandle hKey);
        internal static extern void _ImportKey(SafeProvHandle hCSP, int keyNumber, CspProviderFlags flags, object cspObject, ref SafeKeyHandle hKey);
        internal static extern bool _ProduceLegacyHmacValues();
        internal static extern int _OpenCSP(CspParameters param, uint flags, ref SafeProvHandle hProv);
        internal static extern void _AcquireCSP(CspParameters param, ref SafeProvHandle hProv);
    }
}