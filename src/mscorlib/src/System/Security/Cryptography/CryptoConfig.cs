using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Threading;

namespace System.Security.Cryptography
{
    public class CryptoConfig
    {
        private static volatile Dictionary<string, string> defaultOidHT = null;
        private static volatile Dictionary<string, object> defaultNameHT = null;
        private static volatile Dictionary<string, string> machineOidHT = null;
        private static volatile Dictionary<string, string> machineNameHT = null;
        private static volatile Dictionary<string, Type> appNameHT = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static volatile Dictionary<string, string> appOidHT = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private const string MachineConfigFilename = "machine.config";
        private static volatile string version = null;
        private static volatile bool s_fipsAlgorithmPolicy;
        private static volatile bool s_haveFipsAlgorithmPolicy;
        public static bool AllowOnlyFipsAlgorithms
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (!s_haveFipsAlgorithmPolicy)
                {
                    {
                        s_fipsAlgorithmPolicy = false;
                        s_haveFipsAlgorithmPolicy = true;
                    }
                }

                return s_fipsAlgorithmPolicy;
            }
        }

        private static string Version
        {
            [System.Security.SecurityCritical]
            get
            {
                if (version == null)
                    version = ((RuntimeType)typeof (CryptoConfig)).GetRuntimeAssembly().GetVersion().ToString();
                return version;
            }
        }

        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                }

                return s_InternalSyncObject;
            }
        }

        private static Dictionary<string, string> DefaultOidHT
        {
            get
            {
                if (defaultOidHT == null)
                {
                    Dictionary<string, string> ht = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    ht.Add("SHA", Constants.OID_OIWSEC_SHA1);
                    ht.Add("SHA1", Constants.OID_OIWSEC_SHA1);
                    ht.Add("System.Security.Cryptography.SHA1", Constants.OID_OIWSEC_SHA1);
                    ht.Add("System.Security.Cryptography.SHA1CryptoServiceProvider", Constants.OID_OIWSEC_SHA1);
                    ht.Add("System.Security.Cryptography.SHA1Managed", Constants.OID_OIWSEC_SHA1);
                    ht.Add("SHA256", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256CryptoServiceProvider", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256Cng", Constants.OID_OIWSEC_SHA256);
                    ht.Add("System.Security.Cryptography.SHA256Managed", Constants.OID_OIWSEC_SHA256);
                    ht.Add("SHA384", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384CryptoServiceProvider", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384Cng", Constants.OID_OIWSEC_SHA384);
                    ht.Add("System.Security.Cryptography.SHA384Managed", Constants.OID_OIWSEC_SHA384);
                    ht.Add("SHA512", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512CryptoServiceProvider", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512Cng", Constants.OID_OIWSEC_SHA512);
                    ht.Add("System.Security.Cryptography.SHA512Managed", Constants.OID_OIWSEC_SHA512);
                    ht.Add("RIPEMD160", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("System.Security.Cryptography.RIPEMD160", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("System.Security.Cryptography.RIPEMD160Managed", Constants.OID_OIWSEC_RIPEMD160);
                    ht.Add("MD5", Constants.OID_RSA_MD5);
                    ht.Add("System.Security.Cryptography.MD5", Constants.OID_RSA_MD5);
                    ht.Add("System.Security.Cryptography.MD5CryptoServiceProvider", Constants.OID_RSA_MD5);
                    ht.Add("System.Security.Cryptography.MD5Managed", Constants.OID_RSA_MD5);
                    ht.Add("TripleDESKeyWrap", Constants.OID_RSA_SMIMEalgCMS3DESwrap);
                    ht.Add("RC2", Constants.OID_RSA_RC2CBC);
                    ht.Add("System.Security.Cryptography.RC2CryptoServiceProvider", Constants.OID_RSA_RC2CBC);
                    ht.Add("DES", Constants.OID_OIWSEC_desCBC);
                    ht.Add("System.Security.Cryptography.DESCryptoServiceProvider", Constants.OID_OIWSEC_desCBC);
                    ht.Add("TripleDES", Constants.OID_RSA_DES_EDE3_CBC);
                    ht.Add("System.Security.Cryptography.TripleDESCryptoServiceProvider", Constants.OID_RSA_DES_EDE3_CBC);
                    defaultOidHT = ht;
                }

                return defaultOidHT;
            }
        }

        private static Dictionary<string, object> DefaultNameHT
        {
            get
            {
                if (defaultNameHT == null)
                {
                    Dictionary<string, object> ht = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    Type SHA1CryptoServiceProviderType = typeof (System.Security.Cryptography.SHA1CryptoServiceProvider);
                    Type MD5CryptoServiceProviderType = typeof (System.Security.Cryptography.MD5CryptoServiceProvider);
                    Type RIPEMD160ManagedType = typeof (System.Security.Cryptography.RIPEMD160Managed);
                    Type HMACMD5Type = typeof (System.Security.Cryptography.HMACMD5);
                    Type HMACRIPEMD160Type = typeof (System.Security.Cryptography.HMACRIPEMD160);
                    Type HMACSHA1Type = typeof (System.Security.Cryptography.HMACSHA1);
                    Type HMACSHA256Type = typeof (System.Security.Cryptography.HMACSHA256);
                    Type HMACSHA384Type = typeof (System.Security.Cryptography.HMACSHA384);
                    Type HMACSHA512Type = typeof (System.Security.Cryptography.HMACSHA512);
                    Type MAC3DESType = typeof (System.Security.Cryptography.MACTripleDES);
                    Type RSACryptoServiceProviderType = typeof (System.Security.Cryptography.RSACryptoServiceProvider);
                    Type RijndaelManagedType = typeof (System.Security.Cryptography.RijndaelManaged);
                    Type DSASignatureDescriptionType = typeof (System.Security.Cryptography.DSASignatureDescription);
                    Type RSAPKCS1SHA1SignatureDescriptionType = typeof (System.Security.Cryptography.RSAPKCS1SHA1SignatureDescription);
                    Type RNGCryptoServiceProviderType = typeof (System.Security.Cryptography.RNGCryptoServiceProvider);
                    string AesCryptoServiceProviderType = "System.Security.Cryptography.AesCryptoServiceProvider, " + AssemblyRef.SystemCore;
                    string AesManagedType = "System.Security.Cryptography.AesManaged, " + AssemblyRef.SystemCore;
                    string MD5CngType = "System.Security.Cryptography.MD5Cng, " + AssemblyRef.SystemCore;
                    string SHA1CngType = "System.Security.Cryptography.SHA1Cng, " + AssemblyRef.SystemCore;
                    string SHA256CngType = "System.Security.Cryptography.SHA256Cng, " + AssemblyRef.SystemCore;
                    string SHA256CryptoServiceProviderType = "System.Security.Cryptography.SHA256CryptoServiceProvider, " + AssemblyRef.SystemCore;
                    string SHA384CngType = "System.Security.Cryptography.SHA384Cng, " + AssemblyRef.SystemCore;
                    string SHA384CryptoSerivceProviderType = "System.Security.Cryptography.SHA384CryptoServiceProvider, " + AssemblyRef.SystemCore;
                    string SHA512CngType = "System.Security.Cryptography.SHA512Cng, " + AssemblyRef.SystemCore;
                    string SHA512CryptoServiceProviderType = "System.Security.Cryptography.SHA512CryptoServiceProvider, " + AssemblyRef.SystemCore;
                    bool fipsOnly = AllowOnlyFipsAlgorithms;
                    object SHA256DefaultType = typeof (SHA256Managed);
                    if (fipsOnly)
                    {
                        SHA256DefaultType = SHA256CngType;
                    }

                    object SHA384DefaultType = fipsOnly ? (object)SHA384CngType : (object)typeof (SHA384Managed);
                    object SHA512DefaultType = fipsOnly ? (object)SHA512CngType : (object)typeof (SHA512Managed);
                    string DpapiDataProtectorType = "System.Security.Cryptography.DpapiDataProtector, " + AssemblyRef.SystemSecurity;
                    ht.Add("RandomNumberGenerator", RNGCryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.RandomNumberGenerator", RNGCryptoServiceProviderType);
                    ht.Add("SHA", SHA1CryptoServiceProviderType);
                    ht.Add("SHA1", SHA1CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.SHA1", SHA1CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.SHA1Cng", SHA1CngType);
                    ht.Add("System.Security.Cryptography.HashAlgorithm", SHA1CryptoServiceProviderType);
                    ht.Add("MD5", MD5CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.MD5", MD5CryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.MD5Cng", MD5CngType);
                    ht.Add("SHA256", SHA256DefaultType);
                    ht.Add("SHA-256", SHA256DefaultType);
                    ht.Add("System.Security.Cryptography.SHA256", SHA256DefaultType);
                    ht.Add("System.Security.Cryptography.SHA256Cng", SHA256CngType);
                    ht.Add("System.Security.Cryptography.SHA256CryptoServiceProvider", SHA256CryptoServiceProviderType);
                    ht.Add("SHA384", SHA384DefaultType);
                    ht.Add("SHA-384", SHA384DefaultType);
                    ht.Add("System.Security.Cryptography.SHA384", SHA384DefaultType);
                    ht.Add("System.Security.Cryptography.SHA384Cng", SHA384CngType);
                    ht.Add("System.Security.Cryptography.SHA384CryptoServiceProvider", SHA384CryptoSerivceProviderType);
                    ht.Add("SHA512", SHA512DefaultType);
                    ht.Add("SHA-512", SHA512DefaultType);
                    ht.Add("System.Security.Cryptography.SHA512", SHA512DefaultType);
                    ht.Add("System.Security.Cryptography.SHA512Cng", SHA512CngType);
                    ht.Add("System.Security.Cryptography.SHA512CryptoServiceProvider", SHA512CryptoServiceProviderType);
                    ht.Add("RIPEMD160", RIPEMD160ManagedType);
                    ht.Add("RIPEMD-160", RIPEMD160ManagedType);
                    ht.Add("System.Security.Cryptography.RIPEMD160", RIPEMD160ManagedType);
                    ht.Add("System.Security.Cryptography.RIPEMD160Managed", RIPEMD160ManagedType);
                    ht.Add("System.Security.Cryptography.HMAC", HMACSHA1Type);
                    ht.Add("System.Security.Cryptography.KeyedHashAlgorithm", HMACSHA1Type);
                    ht.Add("HMACMD5", HMACMD5Type);
                    ht.Add("System.Security.Cryptography.HMACMD5", HMACMD5Type);
                    ht.Add("HMACRIPEMD160", HMACRIPEMD160Type);
                    ht.Add("System.Security.Cryptography.HMACRIPEMD160", HMACRIPEMD160Type);
                    ht.Add("HMACSHA1", HMACSHA1Type);
                    ht.Add("System.Security.Cryptography.HMACSHA1", HMACSHA1Type);
                    ht.Add("HMACSHA256", HMACSHA256Type);
                    ht.Add("System.Security.Cryptography.HMACSHA256", HMACSHA256Type);
                    ht.Add("HMACSHA384", HMACSHA384Type);
                    ht.Add("System.Security.Cryptography.HMACSHA384", HMACSHA384Type);
                    ht.Add("HMACSHA512", HMACSHA512Type);
                    ht.Add("System.Security.Cryptography.HMACSHA512", HMACSHA512Type);
                    ht.Add("MACTripleDES", MAC3DESType);
                    ht.Add("System.Security.Cryptography.MACTripleDES", MAC3DESType);
                    ht.Add("RSA", RSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.RSA", RSACryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.AsymmetricAlgorithm", RSACryptoServiceProviderType);
                    ht.Add("Rijndael", RijndaelManagedType);
                    ht.Add("System.Security.Cryptography.Rijndael", RijndaelManagedType);
                    ht.Add("System.Security.Cryptography.SymmetricAlgorithm", RijndaelManagedType);
                    ht.Add("AES", AesCryptoServiceProviderType);
                    ht.Add("AesCryptoServiceProvider", AesCryptoServiceProviderType);
                    ht.Add("System.Security.Cryptography.AesCryptoServiceProvider", AesCryptoServiceProviderType);
                    ht.Add("AesManaged", AesManagedType);
                    ht.Add("System.Security.Cryptography.AesManaged", AesManagedType);
                    ht.Add("DpapiDataProtector", DpapiDataProtectorType);
                    ht.Add("System.Security.Cryptography.DpapiDataProtector", DpapiDataProtectorType);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#dsa-sha1", DSASignatureDescriptionType);
                    ht.Add("System.Security.Cryptography.DSASignatureDescription", DSASignatureDescriptionType);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#rsa-sha1", RSAPKCS1SHA1SignatureDescriptionType);
                    ht.Add("System.Security.Cryptography.RSASignatureDescription", RSAPKCS1SHA1SignatureDescriptionType);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#sha1", SHA1CryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#sha256", SHA256DefaultType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes128-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes128", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes192-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes192", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#aes256-cbc", RijndaelManagedType);
                    ht.Add("http://www.w3.org/2001/04/xmlenc#kw-aes256", RijndaelManagedType);
                    ht.Add("http://www.w3.org/TR/2001/REC-xml-c14n-20010315", "System.Security.Cryptography.Xml.XmlDsigC14NTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/TR/2001/REC-xml-c14n-20010315#WithComments", "System.Security.Cryptography.Xml.XmlDsigC14NWithCommentsTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2001/10/xml-exc-c14n#", "System.Security.Cryptography.Xml.XmlDsigExcC14NTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2001/10/xml-exc-c14n#WithComments", "System.Security.Cryptography.Xml.XmlDsigExcC14NWithCommentsTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#base64", "System.Security.Cryptography.Xml.XmlDsigBase64Transform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/TR/1999/REC-xpath-19991116", "System.Security.Cryptography.Xml.XmlDsigXPathTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/TR/1999/REC-xslt-19991116", "System.Security.Cryptography.Xml.XmlDsigXsltTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#enveloped-signature", "System.Security.Cryptography.Xml.XmlDsigEnvelopedSignatureTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2002/07/decrypt#XML", "System.Security.Cryptography.Xml.XmlDecryptionTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("urn:mpeg:mpeg21:2003:01-REL-R-NS:licenseTransform", "System.Security.Cryptography.Xml.XmlLicenseTransform, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# X509Data", "System.Security.Cryptography.Xml.KeyInfoX509Data, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyName", "System.Security.Cryptography.Xml.KeyInfoName, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyValue/DSAKeyValue", "System.Security.Cryptography.Xml.DSAKeyValue, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# KeyValue/RSAKeyValue", "System.Security.Cryptography.Xml.RSAKeyValue, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig# RetrievalMethod", "System.Security.Cryptography.Xml.KeyInfoRetrievalMethod, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2001/04/xmlenc# EncryptedKey", "System.Security.Cryptography.Xml.KeyInfoEncryptedKey, " + AssemblyRef.SystemSecurity);
                    ht.Add("http://www.w3.org/2000/09/xmldsig#hmac-sha1", HMACSHA1Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#md5", MD5CryptoServiceProviderType);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#sha384", SHA384DefaultType);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-md5", HMACMD5Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160", HMACRIPEMD160Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha256", HMACSHA256Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha384", HMACSHA384Type);
                    ht.Add("http://www.w3.org/2001/04/xmldsig-more#hmac-sha512", HMACSHA512Type);
                    ht.Add("2.5.29.10", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, " + AssemblyRef.System);
                    ht.Add("2.5.29.19", "System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension, " + AssemblyRef.System);
                    ht.Add("2.5.29.14", "System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension, " + AssemblyRef.System);
                    ht.Add("2.5.29.15", "System.Security.Cryptography.X509Certificates.X509KeyUsageExtension, " + AssemblyRef.System);
                    ht.Add("2.5.29.37", "System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension, " + AssemblyRef.System);
                    ht.Add("X509Chain", "System.Security.Cryptography.X509Certificates.X509Chain, " + AssemblyRef.System);
                    ht.Add("1.2.840.113549.1.9.3", "System.Security.Cryptography.Pkcs.Pkcs9ContentType, " + AssemblyRef.SystemSecurity);
                    ht.Add("1.2.840.113549.1.9.4", "System.Security.Cryptography.Pkcs.Pkcs9MessageDigest, " + AssemblyRef.SystemSecurity);
                    ht.Add("1.2.840.113549.1.9.5", "System.Security.Cryptography.Pkcs.Pkcs9SigningTime, " + AssemblyRef.SystemSecurity);
                    ht.Add("1.3.6.1.4.1.311.88.2.1", "System.Security.Cryptography.Pkcs.Pkcs9DocumentName, " + AssemblyRef.SystemSecurity);
                    ht.Add("1.3.6.1.4.1.311.88.2.2", "System.Security.Cryptography.Pkcs.Pkcs9DocumentDescription, " + AssemblyRef.SystemSecurity);
                    defaultNameHT = ht;
                }

                return defaultNameHT;
            }
        }

        private static void InitializeConfigInfo()
        {
            if (machineNameHT == null)
                machineNameHT = new Dictionary<string, string>();
            if (machineOidHT == null)
                machineOidHT = new Dictionary<string, string>();
        }

        public static void AddAlgorithm(Type algorithm, params string[] names)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");
            if (!algorithm.IsVisible)
                throw new ArgumentException(Environment.GetResourceString("Cryptography_AlgorithmTypesMustBeVisible"), "algorithm");
            if (names == null)
                throw new ArgumentNullException("names");
                        string[] algorithmNames = new string[names.Length];
            Array.Copy(names, algorithmNames, algorithmNames.Length);
            foreach (string name in algorithmNames)
            {
                if (String.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(Environment.GetResourceString("Cryptography_AddNullOrEmptyName"));
                }
            }

            lock (InternalSyncObject)
            {
                foreach (string name in algorithmNames)
                {
                    appNameHT[name] = algorithm;
                }
            }
        }

        public static object CreateFromName(string name, params object[] args)
        {
            if (name == null)
                throw new ArgumentNullException("name");
                        Type retvalType = null;
            Object retval;
            InitializeConfigInfo();
            lock (InternalSyncObject)
            {
                retvalType = appNameHT.GetValueOrDefault(name);
            }

            if (retvalType == null)
            {
                BCLDebug.Assert(machineNameHT != null, "machineNameHT != null");
                String retvalTypeString = machineNameHT.GetValueOrDefault(name);
                if (retvalTypeString != null)
                {
                    retvalType = Type.GetType(retvalTypeString, false, false);
                    if (retvalType != null && !retvalType.IsVisible)
                        retvalType = null;
                }
            }

            if (retvalType == null)
            {
                Object retvalObj = DefaultNameHT.GetValueOrDefault(name);
                if (retvalObj != null)
                {
                    if (retvalObj is Type)
                    {
                        retvalType = (Type)retvalObj;
                    }
                    else if (retvalObj is String)
                    {
                        retvalType = Type.GetType((String)retvalObj, false, false);
                        if (retvalType != null && !retvalType.IsVisible)
                            retvalType = null;
                    }
                }
            }

            if (retvalType == null)
            {
                retvalType = Type.GetType(name, false, false);
                if (retvalType != null && !retvalType.IsVisible)
                    retvalType = null;
            }

            if (retvalType == null)
                return null;
            RuntimeType rtType = retvalType as RuntimeType;
            if (rtType == null)
                return null;
            if (args == null)
                args = new Object[]{};
            MethodBase[] cons = rtType.GetConstructors(Activator.ConstructorDefault);
            if (cons == null)
                return null;
            List<MethodBase> candidates = new List<MethodBase>();
            for (int i = 0; i < cons.Length; i++)
            {
                MethodBase con = cons[i];
                if (con.GetParameters().Length == args.Length)
                {
                    candidates.Add(con);
                }
            }

            if (candidates.Count == 0)
                return null;
            cons = candidates.ToArray();
            Object state;
            RuntimeConstructorInfo rci = Type.DefaultBinder.BindToMethod(Activator.ConstructorDefault, cons, ref args, null, null, null, out state) as RuntimeConstructorInfo;
            if (rci == null || typeof (Delegate).IsAssignableFrom(rci.DeclaringType))
                return null;
            retval = rci.Invoke(Activator.ConstructorDefault, Type.DefaultBinder, args, null);
            if (state != null)
                Type.DefaultBinder.ReorderArgumentArray(ref args, state);
            return retval;
        }

        public static object CreateFromName(string name)
        {
            return CreateFromName(name, null);
        }

        public static void AddOID(string oid, params string[] names)
        {
            if (oid == null)
                throw new ArgumentNullException("oid");
            if (names == null)
                throw new ArgumentNullException("names");
                        string[] oidNames = new string[names.Length];
            Array.Copy(names, oidNames, oidNames.Length);
            foreach (string name in oidNames)
            {
                if (String.IsNullOrEmpty(name))
                {
                    throw new ArgumentException(Environment.GetResourceString("Cryptography_AddNullOrEmptyName"));
                }
            }

            lock (InternalSyncObject)
            {
                foreach (string name in oidNames)
                {
                    appOidHT[name] = oid;
                }
            }
        }

        public static string MapNameToOID(string name)
        {
            return MapNameToOID(name, OidGroup.AllGroups);
        }

        internal static string MapNameToOID(string name, OidGroup oidGroup)
        {
            if (name == null)
                throw new ArgumentNullException("name");
                        InitializeConfigInfo();
            string oid = null;
            lock (InternalSyncObject)
            {
                oid = appOidHT.GetValueOrDefault(name);
            }

            if (oid == null)
                oid = machineOidHT.GetValueOrDefault(name);
            if (oid == null)
                oid = DefaultOidHT.GetValueOrDefault(name);
            if (oid == null)
                oid = X509Utils.GetOidFromFriendlyName(name, oidGroup);
            return oid;
        }

        static public byte[] EncodeOID(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }

                        char[] sepArray = {'.'};
            String[] oidString = str.Split(sepArray);
            uint[] oidNums = new uint[oidString.Length];
            for (int i = 0; i < oidString.Length; i++)
            {
                oidNums[i] = (uint)Int32.Parse(oidString[i], CultureInfo.InvariantCulture);
            }

            byte[] encodedOidNums = new byte[oidNums.Length * 5];
            int encodedOidNumsIndex = 0;
            if (oidNums.Length < 2)
            {
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_InvalidOID"));
            }

            uint firstTwoOidNums = (oidNums[0] * 40) + oidNums[1];
            byte[] retval = EncodeSingleOIDNum(firstTwoOidNums);
            Array.Copy(retval, 0, encodedOidNums, encodedOidNumsIndex, retval.Length);
            encodedOidNumsIndex += retval.Length;
            for (int i = 2; i < oidNums.Length; i++)
            {
                retval = EncodeSingleOIDNum(oidNums[i]);
                Buffer.InternalBlockCopy(retval, 0, encodedOidNums, encodedOidNumsIndex, retval.Length);
                encodedOidNumsIndex += retval.Length;
            }

            if (encodedOidNumsIndex > 0x7f)
            {
                throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_Config_EncodedOIDError"));
            }

            retval = new byte[encodedOidNumsIndex + 2];
            retval[0] = (byte)0x06;
            retval[1] = (byte)encodedOidNumsIndex;
            Buffer.InternalBlockCopy(encodedOidNums, 0, retval, 2, encodedOidNumsIndex);
            return retval;
        }

        static private byte[] EncodeSingleOIDNum(uint dwValue)
        {
            byte[] retval;
            if ((int)dwValue < 0x80)
            {
                retval = new byte[1];
                retval[0] = (byte)dwValue;
                return retval;
            }
            else if (dwValue < 0x4000)
            {
                retval = new byte[2];
                retval[0] = (byte)((dwValue >> 7) | 0x80);
                retval[1] = (byte)(dwValue & 0x7f);
                return retval;
            }
            else if (dwValue < 0x200000)
            {
                retval = new byte[3];
                retval[0] = (byte)((dwValue >> 14) | 0x80);
                retval[1] = (byte)((dwValue >> 7) | 0x80);
                retval[2] = (byte)(dwValue & 0x7f);
                return retval;
            }
            else if (dwValue < 0x10000000)
            {
                retval = new byte[4];
                retval[0] = (byte)((dwValue >> 21) | 0x80);
                retval[1] = (byte)((dwValue >> 14) | 0x80);
                retval[2] = (byte)((dwValue >> 7) | 0x80);
                retval[3] = (byte)(dwValue & 0x7f);
                return retval;
            }
            else
            {
                retval = new byte[5];
                retval[0] = (byte)((dwValue >> 28) | 0x80);
                retval[1] = (byte)((dwValue >> 21) | 0x80);
                retval[2] = (byte)((dwValue >> 14) | 0x80);
                retval[3] = (byte)((dwValue >> 7) | 0x80);
                retval[4] = (byte)(dwValue & 0x7f);
                return retval;
            }
        }

        private static Dictionary<string, string> InitializeNameMappings(ConfigNode nameMappingNode)
        {
                                    Dictionary<string, string> nameMappings = new Dictionary<string, string>();
            Dictionary<string, string> typeAliases = new Dictionary<string, string>();
            foreach (ConfigNode node in nameMappingNode.Children)
            {
                if (String.Compare(node.Name, "cryptoClasses", StringComparison.Ordinal) == 0)
                {
                    foreach (ConfigNode cryptoClass in node.Children)
                    {
                        if (String.Compare(cryptoClass.Name, "cryptoClass", StringComparison.Ordinal) == 0)
                        {
                            if (cryptoClass.Attributes.Count > 0)
                            {
                                DictionaryEntry attribute = (DictionaryEntry)cryptoClass.Attributes[0];
                                typeAliases.Add((string)attribute.Key, (string)attribute.Value);
                            }
                        }
                    }
                }
                else if (String.Compare(node.Name, "nameEntry", StringComparison.Ordinal) == 0)
                {
                    string friendlyName = null;
                    string className = null;
                    foreach (DictionaryEntry attribute in node.Attributes)
                    {
                        if (String.Compare((string)attribute.Key, "name", StringComparison.Ordinal) == 0)
                            friendlyName = (string)attribute.Value;
                        else if (String.Compare((string)attribute.Key, "class", StringComparison.Ordinal) == 0)
                            className = (string)attribute.Value;
                    }

                    if (friendlyName != null && className != null)
                    {
                        string typeName = typeAliases.GetValueOrDefault(className);
                        if (typeName != null)
                            nameMappings.Add(friendlyName, typeName);
                    }
                }
            }

            return nameMappings;
        }

        private static Dictionary<string, string> InitializeOidMappings(ConfigNode oidMappingNode)
        {
                                    Dictionary<string, string> oidMap = new Dictionary<string, string>();
            foreach (ConfigNode node in oidMappingNode.Children)
            {
                if (String.Compare(node.Name, "oidEntry", StringComparison.Ordinal) == 0)
                {
                    string oidString = null;
                    string friendlyName = null;
                    foreach (DictionaryEntry attribute in node.Attributes)
                    {
                        if (String.Compare((string)attribute.Key, "OID", StringComparison.Ordinal) == 0)
                            oidString = (string)attribute.Value;
                        else if (String.Compare((string)attribute.Key, "name", StringComparison.Ordinal) == 0)
                            friendlyName = (string)attribute.Value;
                    }

                    if ((friendlyName != null) && (oidString != null))
                        oidMap.Add(friendlyName, oidString);
                }
            }

            return oidMap;
        }

        private static ConfigNode OpenCryptoConfig()
        {
            string machineConfigFile = System.Security.Util.Config.MachineDirectory + MachineConfigFilename;
            new FileIOPermission(FileIOPermissionAccess.Read, machineConfigFile).Assert();
            if (!File.Exists(machineConfigFile))
                return null;
            CodeAccessPermission.RevertAssert();
            ConfigTreeParser parser = new ConfigTreeParser();
            ConfigNode rootNode = parser.Parse(machineConfigFile, "configuration", true);
            if (rootNode == null)
                return null;
            ConfigNode mscorlibNode = null;
            foreach (ConfigNode node in rootNode.Children)
            {
                bool versionSpecificMscorlib = false;
                if (String.Compare(node.Name, "mscorlib", StringComparison.Ordinal) == 0)
                {
                    foreach (DictionaryEntry attribute in node.Attributes)
                    {
                        if (String.Compare((string)attribute.Key, "version", StringComparison.Ordinal) == 0)
                        {
                            versionSpecificMscorlib = true;
                            if (String.Compare((string)attribute.Value, Version, StringComparison.Ordinal) == 0)
                            {
                                mscorlibNode = node;
                                break;
                            }
                        }
                    }

                    if (!versionSpecificMscorlib)
                        mscorlibNode = node;
                }

                if (mscorlibNode != null)
                    break;
            }

            if (mscorlibNode == null)
                return null;
            foreach (ConfigNode node in mscorlibNode.Children)
            {
                if (String.Compare(node.Name, "cryptographySettings", StringComparison.Ordinal) == 0)
                    return node;
            }

            return null;
        }
    }
}