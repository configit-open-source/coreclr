
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;

namespace System.Security.Cryptography
{
    internal class RSACspObject
    {
        internal byte[] Exponent;
        internal byte[] Modulus;
        internal byte[] P;
        internal byte[] Q;
        internal byte[] DP;
        internal byte[] DQ;
        internal byte[] InverseQ;
        internal byte[] D;
    }

    public sealed class RSACryptoServiceProvider : RSA, ICspAsymmetricAlgorithm
    {
        private int _dwKeySize;
        private CspParameters _parameters;
        private bool _randomKeyContainer;
        private SafeProvHandle _safeProvHandle;
        private SafeKeyHandle _safeKeyHandle;
        private static volatile CspProviderFlags s_UseMachineKeyStore = 0;
        private static extern void DecryptKey(SafeKeyHandle pKeyContext, [MarshalAs(UnmanagedType.LPArray)] byte[] pbEncryptedKey, int cbEncryptedKey, [MarshalAs(UnmanagedType.Bool)] bool fOAEP, ObjectHandleOnStack ohRetDecryptedKey);
        private static extern void EncryptKey(SafeKeyHandle pKeyContext, [MarshalAs(UnmanagedType.LPArray)] byte[] pbKey, int cbKey, [MarshalAs(UnmanagedType.Bool)] bool fOAEP, ObjectHandleOnStack ohRetEncryptedKey);
        public RSACryptoServiceProvider(): this (0, new CspParameters(Utils.DefaultRsaProviderType, null, null, s_UseMachineKeyStore), true)
        {
        }

        public RSACryptoServiceProvider(int dwKeySize): this (dwKeySize, new CspParameters(Utils.DefaultRsaProviderType, null, null, s_UseMachineKeyStore), false)
        {
        }

        public RSACryptoServiceProvider(CspParameters parameters): this (0, parameters, true)
        {
        }

        public RSACryptoServiceProvider(int dwKeySize, CspParameters parameters): this (dwKeySize, parameters, false)
        {
        }

        private RSACryptoServiceProvider(int dwKeySize, CspParameters parameters, bool useDefaultKeySize)
        {
            if (dwKeySize < 0)
                throw new ArgumentOutOfRangeException("dwKeySize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                        _parameters = Utils.SaveCspParameters(CspAlgorithmType.Rsa, parameters, s_UseMachineKeyStore, ref _randomKeyContainer);
            LegalKeySizesValue = new KeySizes[]{new KeySizes(384, 16384, 8)};
            _dwKeySize = useDefaultKeySize ? 1024 : dwKeySize;
            if (!_randomKeyContainer)
                GetKeyPair();
        }

        private void GetKeyPair()
        {
            if (_safeKeyHandle == null)
            {
                lock (this)
                {
                    if (_safeKeyHandle == null)
                    {
                        Utils.GetKeyPairHelper(CspAlgorithmType.Rsa, _parameters, _randomKeyContainer, _dwKeySize, ref _safeProvHandle, ref _safeKeyHandle);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed)
                _safeKeyHandle.Dispose();
            if (_safeProvHandle != null && !_safeProvHandle.IsClosed)
                _safeProvHandle.Dispose();
        }

        public bool PublicOnly
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                GetKeyPair();
                byte[] publicKey = (byte[])Utils._GetKeyParameter(_safeKeyHandle, Constants.CLR_PUBLICKEYONLY);
                return (publicKey[0] == 1);
            }
        }

        public CspKeyContainerInfo CspKeyContainerInfo
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                GetKeyPair();
                return new CspKeyContainerInfo(_parameters, _randomKeyContainer);
            }
        }

        public override int KeySize
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                GetKeyPair();
                byte[] keySize = (byte[])Utils._GetKeyParameter(_safeKeyHandle, Constants.CLR_KEYLEN);
                _dwKeySize = (keySize[0] | (keySize[1] << 8) | (keySize[2] << 16) | (keySize[3] << 24));
                return _dwKeySize;
            }
        }

        public override string KeyExchangeAlgorithm
        {
            get
            {
                if (_parameters.KeyNumber == Constants.AT_KEYEXCHANGE)
                    return "RSA-PKCS1-KeyEx";
                return null;
            }
        }

        public override string SignatureAlgorithm
        {
            get
            {
                return "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
            }
        }

        public static bool UseMachineKeyStore
        {
            get
            {
                return (s_UseMachineKeyStore == CspProviderFlags.UseMachineKeyStore);
            }

            set
            {
                s_UseMachineKeyStore = (value ? CspProviderFlags.UseMachineKeyStore : 0);
            }
        }

        public bool PersistKeyInCsp
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (_safeProvHandle == null)
                {
                    lock (this)
                    {
                        if (_safeProvHandle == null)
                            _safeProvHandle = Utils.CreateProvHandle(_parameters, _randomKeyContainer);
                    }
                }

                return Utils.GetPersistKeyInCsp(_safeProvHandle);
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                bool oldPersistKeyInCsp = this.PersistKeyInCsp;
                if (value == oldPersistKeyInCsp)
                    return;
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    if (!value)
                    {
                        KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Delete);
                        kp.AccessEntries.Add(entry);
                    }
                    else
                    {
                        KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Create);
                        kp.AccessEntries.Add(entry);
                    }

                    kp.Demand();
                }

                Utils.SetPersistKeyInCsp(_safeProvHandle, value);
            }
        }

        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            GetKeyPair();
            if (includePrivateParameters)
            {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Export);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }
            }

            RSACspObject rsaCspObject = new RSACspObject();
            int blobType = includePrivateParameters ? Constants.PRIVATEKEYBLOB : Constants.PUBLICKEYBLOB;
            Utils._ExportKey(_safeKeyHandle, blobType, rsaCspObject);
            return RSAObjectToStruct(rsaCspObject);
        }

        public byte[] ExportCspBlob(bool includePrivateParameters)
        {
            GetKeyPair();
            return Utils.ExportCspBlobHelper(includePrivateParameters, _parameters, _safeKeyHandle);
        }

        public override void ImportParameters(RSAParameters parameters)
        {
            if (_safeKeyHandle != null && !_safeKeyHandle.IsClosed)
            {
                _safeKeyHandle.Dispose();
                _safeKeyHandle = null;
            }

            RSACspObject rsaCspObject = RSAStructToObject(parameters);
            _safeKeyHandle = SafeKeyHandle.InvalidHandle;
            if (IsPublic(parameters))
            {
                Utils._ImportKey(Utils.StaticProvHandle, Constants.CALG_RSA_KEYX, (CspProviderFlags)0, rsaCspObject, ref _safeKeyHandle);
            }
            else
            {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Import);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }

                if (_safeProvHandle == null)
                    _safeProvHandle = Utils.CreateProvHandle(_parameters, _randomKeyContainer);
                Utils._ImportKey(_safeProvHandle, Constants.CALG_RSA_KEYX, _parameters.Flags, rsaCspObject, ref _safeKeyHandle);
            }
        }

        public void ImportCspBlob(byte[] keyBlob)
        {
            Utils.ImportCspBlobHelper(CspAlgorithmType.Rsa, keyBlob, IsPublic(keyBlob), ref _parameters, _randomKeyContainer, ref _safeProvHandle, ref _safeKeyHandle);
        }

        public byte[] SignData(Stream inputStream, Object halg)
        {
            int calgHash = Utils.ObjToAlgId(halg, OidGroup.HashAlgorithm);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(inputStream);
            return SignHash(hashVal, calgHash);
        }

        public byte[] SignData(byte[] buffer, Object halg)
        {
            int calgHash = Utils.ObjToAlgId(halg, OidGroup.HashAlgorithm);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer);
            return SignHash(hashVal, calgHash);
        }

        public byte[] SignData(byte[] buffer, int offset, int count, Object halg)
        {
            int calgHash = Utils.ObjToAlgId(halg, OidGroup.HashAlgorithm);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer, offset, count);
            return SignHash(hashVal, calgHash);
        }

        public bool VerifyData(byte[] buffer, Object halg, byte[] signature)
        {
            int calgHash = Utils.ObjToAlgId(halg, OidGroup.HashAlgorithm);
            HashAlgorithm hash = Utils.ObjToHashAlgorithm(halg);
            byte[] hashVal = hash.ComputeHash(buffer);
            return VerifyHash(hashVal, calgHash, signature);
        }

        public byte[] SignHash(byte[] rgbHash, string str)
        {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
                        if (PublicOnly)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NoPrivateKey"));
            int calgHash = X509Utils.NameOrOidToAlgId(str, OidGroup.HashAlgorithm);
            return SignHash(rgbHash, calgHash);
        }

        internal byte[] SignHash(byte[] rgbHash, int calgHash)
        {
                        GetKeyPair();
            if (!CspKeyContainerInfo.RandomlyGenerated)
            {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Sign);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }
            }

            return Utils.SignValue(_safeKeyHandle, _parameters.KeyNumber, Constants.CALG_RSA_SIGN, calgHash, rgbHash);
        }

        public bool VerifyHash(byte[] rgbHash, string str, byte[] rgbSignature)
        {
            if (rgbHash == null)
                throw new ArgumentNullException("rgbHash");
            if (rgbSignature == null)
                throw new ArgumentNullException("rgbSignature");
                        int calgHash = X509Utils.NameOrOidToAlgId(str, OidGroup.HashAlgorithm);
            return VerifyHash(rgbHash, calgHash, rgbSignature);
        }

        internal bool VerifyHash(byte[] rgbHash, int calgHash, byte[] rgbSignature)
        {
                                    GetKeyPair();
            return Utils.VerifySign(_safeKeyHandle, Constants.CALG_RSA_SIGN, calgHash, rgbHash, rgbSignature);
        }

        public byte[] Encrypt(byte[] rgb, bool fOAEP)
        {
            if (rgb == null)
                throw new ArgumentNullException("rgb");
                        GetKeyPair();
            byte[] encryptedKey = null;
            EncryptKey(_safeKeyHandle, rgb, rgb.Length, fOAEP, JitHelpers.GetObjectHandleOnStack(ref encryptedKey));
            return encryptedKey;
        }

        public byte[] Decrypt(byte[] rgb, bool fOAEP)
        {
            if (rgb == null)
                throw new ArgumentNullException("rgb");
                        GetKeyPair();
            if (rgb.Length > (KeySize / 8))
                throw new CryptographicException(Environment.GetResourceString("Cryptography_Padding_DecDataTooBig", KeySize / 8));
            if (!CspKeyContainerInfo.RandomlyGenerated)
            {
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                    KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(_parameters, KeyContainerPermissionFlags.Decrypt);
                    kp.AccessEntries.Add(entry);
                    kp.Demand();
                }
            }

            byte[] decryptedKey = null;
            DecryptKey(_safeKeyHandle, rgb, rgb.Length, fOAEP, JitHelpers.GetObjectHandleOnStack(ref decryptedKey));
            return decryptedKey;
        }

        public override byte[] DecryptValue(byte[] rgb)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        }

        public override byte[] EncryptValue(byte[] rgb)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
        }

        private static RSAParameters RSAObjectToStruct(RSACspObject rsaCspObject)
        {
            RSAParameters rsaParams = new RSAParameters();
            rsaParams.Exponent = rsaCspObject.Exponent;
            rsaParams.Modulus = rsaCspObject.Modulus;
            rsaParams.P = rsaCspObject.P;
            rsaParams.Q = rsaCspObject.Q;
            rsaParams.DP = rsaCspObject.DP;
            rsaParams.DQ = rsaCspObject.DQ;
            rsaParams.InverseQ = rsaCspObject.InverseQ;
            rsaParams.D = rsaCspObject.D;
            return rsaParams;
        }

        private static RSACspObject RSAStructToObject(RSAParameters rsaParams)
        {
            RSACspObject rsaCspObject = new RSACspObject();
            rsaCspObject.Exponent = rsaParams.Exponent;
            rsaCspObject.Modulus = rsaParams.Modulus;
            rsaCspObject.P = rsaParams.P;
            rsaCspObject.Q = rsaParams.Q;
            rsaCspObject.DP = rsaParams.DP;
            rsaCspObject.DQ = rsaParams.DQ;
            rsaCspObject.InverseQ = rsaParams.InverseQ;
            rsaCspObject.D = rsaParams.D;
            return rsaCspObject;
        }

        private static bool IsPublic(byte[] keyBlob)
        {
            if (keyBlob == null)
                throw new ArgumentNullException("keyBlob");
                        if (keyBlob[0] != Constants.PUBLICKEYBLOB)
                return false;
            if (keyBlob[11] != 0x31 || keyBlob[10] != 0x41 || keyBlob[9] != 0x53 || keyBlob[8] != 0x52)
                return false;
            return true;
        }

        private static bool IsPublic(RSAParameters rsaParams)
        {
            return (rsaParams.P == null);
        }
    }
}