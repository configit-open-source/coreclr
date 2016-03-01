
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class X509Constants
    {
        internal const uint CRYPT_EXPORTABLE = 0x00000001;
        internal const uint CRYPT_USER_PROTECTED = 0x00000002;
        internal const uint CRYPT_MACHINE_KEYSET = 0x00000020;
        internal const uint CRYPT_USER_KEYSET = 0x00001000;
        internal const uint CERT_QUERY_CONTENT_CERT = 1;
        internal const uint CERT_QUERY_CONTENT_CTL = 2;
        internal const uint CERT_QUERY_CONTENT_CRL = 3;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_STORE = 4;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CERT = 5;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CTL = 6;
        internal const uint CERT_QUERY_CONTENT_SERIALIZED_CRL = 7;
        internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED = 8;
        internal const uint CERT_QUERY_CONTENT_PKCS7_UNSIGNED = 9;
        internal const uint CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10;
        internal const uint CERT_QUERY_CONTENT_PKCS10 = 11;
        internal const uint CERT_QUERY_CONTENT_PFX = 12;
        internal const uint CERT_QUERY_CONTENT_CERT_PAIR = 13;
        internal const uint CERT_STORE_PROV_MEMORY = 2;
        internal const uint CERT_STORE_PROV_SYSTEM = 10;
        internal const uint CERT_STORE_NO_CRYPT_RELEASE_FLAG = 0x00000001;
        internal const uint CERT_STORE_SET_LOCALIZED_NAME_FLAG = 0x00000002;
        internal const uint CERT_STORE_DEFER_CLOSE_UNTIL_LAST_FREE_FLAG = 0x00000004;
        internal const uint CERT_STORE_DELETE_FLAG = 0x00000010;
        internal const uint CERT_STORE_SHARE_STORE_FLAG = 0x00000040;
        internal const uint CERT_STORE_SHARE_CONTEXT_FLAG = 0x00000080;
        internal const uint CERT_STORE_MANIFOLD_FLAG = 0x00000100;
        internal const uint CERT_STORE_ENUM_ARCHIVED_FLAG = 0x00000200;
        internal const uint CERT_STORE_UPDATE_KEYID_FLAG = 0x00000400;
        internal const uint CERT_STORE_BACKUP_RESTORE_FLAG = 0x00000800;
        internal const uint CERT_STORE_READONLY_FLAG = 0x00008000;
        internal const uint CERT_STORE_OPEN_EXISTING_FLAG = 0x00004000;
        internal const uint CERT_STORE_CREATE_NEW_FLAG = 0x00002000;
        internal const uint CERT_STORE_MAXIMUM_ALLOWED_FLAG = 0x00001000;
        internal const uint CERT_NAME_EMAIL_TYPE = 1;
        internal const uint CERT_NAME_RDN_TYPE = 2;
        internal const uint CERT_NAME_SIMPLE_DISPLAY_TYPE = 4;
        internal const uint CERT_NAME_FRIENDLY_DISPLAY_TYPE = 5;
        internal const uint CERT_NAME_DNS_TYPE = 6;
        internal const uint CERT_NAME_URL_TYPE = 7;
        internal const uint CERT_NAME_UPN_TYPE = 8;
    }

    internal enum OidGroup
    {
        AllGroups = 0,
        HashAlgorithm = 1,
        EncryptionAlgorithm = 2,
        PublicKeyAlgorithm = 3,
        SignatureAlgorithm = 4,
        Attribute = 5,
        ExtensionOrAttribute = 6,
        EnhancedKeyUsage = 7,
        Policy = 8,
        Template = 9,
        KeyDerivationFunction = 10,
        DisableSearchDS = unchecked ((int)0x80000000)}

    internal enum OidKeyType
    {
        Oid = 1,
        Name = 2,
        AlgorithmID = 3,
        SignatureID = 4,
        CngAlgorithmID = 5,
        CngSignatureID = 6
    }

    internal struct CRYPT_OID_INFO
    {
        internal int cbSize;
        internal string pszOID;
        internal string pwszName;
        internal OidGroup dwGroupId;
        internal int AlgId;
        internal int cbData;
        internal IntPtr pbData;
    }

    internal static class X509Utils
    {
        private static bool OidGroupWillNotUseActiveDirectory(OidGroup group)
        {
            return group == OidGroup.HashAlgorithm || group == OidGroup.EncryptionAlgorithm || group == OidGroup.PublicKeyAlgorithm || group == OidGroup.SignatureAlgorithm || group == OidGroup.Attribute || group == OidGroup.ExtensionOrAttribute || group == OidGroup.KeyDerivationFunction;
        }

        private static CRYPT_OID_INFO FindOidInfo(OidKeyType keyType, string key, OidGroup group)
        {
                        IntPtr rawKey = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                if (keyType == OidKeyType.Oid)
                {
                    rawKey = Marshal.StringToCoTaskMemAnsi(key);
                }
                else
                {
                    rawKey = Marshal.StringToCoTaskMemUni(key);
                }

                if (!OidGroupWillNotUseActiveDirectory(group))
                {
                    OidGroup localGroup = group | OidGroup.DisableSearchDS;
                    IntPtr localOidInfo = CryptFindOIDInfo(keyType, rawKey, localGroup);
                    if (localOidInfo != IntPtr.Zero)
                    {
                        return (CRYPT_OID_INFO)Marshal.PtrToStructure(localOidInfo, typeof (CRYPT_OID_INFO));
                    }
                }

                IntPtr fullOidInfo = CryptFindOIDInfo(keyType, rawKey, group);
                if (fullOidInfo != IntPtr.Zero)
                {
                    return (CRYPT_OID_INFO)Marshal.PtrToStructure(fullOidInfo, typeof (CRYPT_OID_INFO));
                }

                if (group != OidGroup.AllGroups)
                {
                    IntPtr allGroupOidInfo = CryptFindOIDInfo(keyType, rawKey, OidGroup.AllGroups);
                    if (allGroupOidInfo != IntPtr.Zero)
                    {
                        return (CRYPT_OID_INFO)Marshal.PtrToStructure(fullOidInfo, typeof (CRYPT_OID_INFO));
                    }
                }

                return new CRYPT_OID_INFO();
            }
            finally
            {
                if (rawKey != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(rawKey);
                }
            }
        }

        internal static int GetAlgIdFromOid(string oid, OidGroup oidGroup)
        {
                        if (String.Equals(oid, Constants.OID_OIWSEC_SHA256, StringComparison.Ordinal))
            {
                return Constants.CALG_SHA_256;
            }
            else if (String.Equals(oid, Constants.OID_OIWSEC_SHA384, StringComparison.Ordinal))
            {
                return Constants.CALG_SHA_384;
            }
            else if (String.Equals(oid, Constants.OID_OIWSEC_SHA512, StringComparison.Ordinal))
            {
                return Constants.CALG_SHA_512;
            }
            else
            {
                return FindOidInfo(OidKeyType.Oid, oid, oidGroup).AlgId;
            }
        }

        internal static string GetFriendlyNameFromOid(string oid, OidGroup oidGroup)
        {
                        CRYPT_OID_INFO oidInfo = FindOidInfo(OidKeyType.Oid, oid, oidGroup);
            return oidInfo.pwszName;
        }

        internal static string GetOidFromFriendlyName(string friendlyName, OidGroup oidGroup)
        {
                        CRYPT_OID_INFO oidInfo = FindOidInfo(OidKeyType.Name, friendlyName, oidGroup);
            return oidInfo.pszOID;
        }

        internal static int NameOrOidToAlgId(string oid, OidGroup oidGroup)
        {
            if (oid == null)
                return Constants.CALG_SHA1;
            string oidValue = CryptoConfig.MapNameToOID(oid, oidGroup);
            if (oidValue == null)
                oidValue = oid;
            int algId = GetAlgIdFromOid(oidValue, oidGroup);
            if (algId == 0 || algId == -1)
            {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidOID"));
            }

            return algId;
        }

        internal static X509ContentType MapContentType(uint contentType)
        {
            switch (contentType)
            {
                case X509Constants.CERT_QUERY_CONTENT_CERT:
                    return X509ContentType.Cert;
                default:
                    return X509ContentType.Unknown;
            }
        }

        internal static uint MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                if (keyStorageFlags != X509KeyStorageFlags.DefaultKeySet)
                    throw new NotSupportedException(Environment.GetResourceString("Argument_InvalidFlag"), new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "keyStorageFlags"));
            }

            if ((keyStorageFlags & (X509KeyStorageFlags)~0x1F) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "keyStorageFlags");
            uint dwFlags = 0;
            if (keyStorageFlags != X509KeyStorageFlags.DefaultKeySet)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "keyStorageFlags", new NotSupportedException());
            }

            return dwFlags;
        }

        internal static IntPtr PasswordToHGlobalUni(object password)
        {
            if (password != null)
            {
                string pwd = password as string;
                if (pwd != null)
                    return Marshal.StringToHGlobalUni(pwd);
                SecureString securePwd = password as SecureString;
                if (securePwd != null)
                    return Marshal.SecureStringToGlobalAllocUnicode(securePwd);
            }

            return IntPtr.Zero;
        }

        private static extern IntPtr CryptFindOIDInfo(OidKeyType dwKeyType, IntPtr pvKey, OidGroup dwGroupId);
        internal static extern void _DuplicateCertContext(IntPtr handle, ref SafeCertContextHandle safeCertContext);
        internal static extern byte[] _GetCertRawData(SafeCertContextHandle safeCertContext);
        internal static extern void _GetDateNotAfter(SafeCertContextHandle safeCertContext, ref Win32Native.FILE_TIME fileTime);
        internal static extern void _GetDateNotBefore(SafeCertContextHandle safeCertContext, ref Win32Native.FILE_TIME fileTime);
        internal static extern string _GetIssuerName(SafeCertContextHandle safeCertContext, bool legacyV1Mode);
        internal static extern string _GetPublicKeyOid(SafeCertContextHandle safeCertContext);
        internal static extern byte[] _GetPublicKeyParameters(SafeCertContextHandle safeCertContext);
        internal static extern byte[] _GetPublicKeyValue(SafeCertContextHandle safeCertContext);
        internal static extern string _GetSubjectInfo(SafeCertContextHandle safeCertContext, uint displayType, bool legacyV1Mode);
        internal static extern byte[] _GetSerialNumber(SafeCertContextHandle safeCertContext);
        internal static extern byte[] _GetThumbprint(SafeCertContextHandle safeCertContext);
        internal static extern void _LoadCertFromBlob(byte[] rawData, IntPtr password, uint dwFlags, bool persistKeySet, ref SafeCertContextHandle pCertCtx);
        internal static extern void _LoadCertFromFile(string fileName, IntPtr password, uint dwFlags, bool persistKeySet, ref SafeCertContextHandle pCertCtx);
        internal static extern uint _QueryCertBlobType(byte[] rawData);
        internal static extern uint _QueryCertFileType(string fileName);
    }
}