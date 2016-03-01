namespace System.Security.Cryptography.X509Certificates
{
    using Microsoft.Win32;
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Util;
    using System.Text;
    using System.Runtime.Versioning;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    public enum X509ContentType
    {
        Unknown = 0x00,
        Cert = 0x01,
        SerializedCert = 0x02,
        Pfx = 0x03,
        Pkcs12 = Pfx,
        SerializedStore = 0x04,
        Pkcs7 = 0x05,
        Authenticode = 0x06
    }

    [Flags]
    public enum X509KeyStorageFlags
    {
        DefaultKeySet = 0x00,
        UserKeySet = 0x01,
        MachineKeySet = 0x02,
        Exportable = 0x04,
        UserProtected = 0x08,
        PersistKeySet = 0x10
    }

    public class X509Certificate : IDisposable, IDeserializationCallback, ISerializable
    {
        private const string m_format = "X509";
        private string m_subjectName;
        private string m_issuerName;
        private byte[] m_serialNumber;
        private byte[] m_publicKeyParameters;
        private byte[] m_publicKeyValue;
        private string m_publicKeyOid;
        private byte[] m_rawData;
        private byte[] m_thumbprint;
        private DateTime m_notBefore;
        private DateTime m_notAfter;
        private SafeCertContextHandle m_safeCertContext;
        private bool m_certContextCloned = false;
        private void Init()
        {
            m_safeCertContext = SafeCertContextHandle.InvalidHandle;
        }

        public X509Certificate()
        {
            Init();
        }

        public X509Certificate(byte[] data): this ()
        {
            if ((data != null) && (data.Length != 0))
                LoadCertificateFromBlob(data, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate(byte[] rawData, string password): this ()
        {
            if ((rawData != null) && (rawData.Length != 0))
            {
                LoadCertificateFromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet);
            }
        }

        public X509Certificate(byte[] rawData, SecureString password): this ()
        {
            LoadCertificateFromBlob(rawData, password, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags): this ()
        {
            if ((rawData != null) && (rawData.Length != 0))
            {
                LoadCertificateFromBlob(rawData, password, keyStorageFlags);
            }
        }

        public X509Certificate(byte[] rawData, SecureString password, X509KeyStorageFlags keyStorageFlags): this ()
        {
            LoadCertificateFromBlob(rawData, password, keyStorageFlags);
        }

        public X509Certificate(string fileName): this ()
        {
            LoadCertificateFromFile(fileName, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate(string fileName, string password): this ()
        {
            LoadCertificateFromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate(string fileName, SecureString password): this ()
        {
            LoadCertificateFromFile(fileName, password, X509KeyStorageFlags.DefaultKeySet);
        }

        public X509Certificate(string fileName, string password, X509KeyStorageFlags keyStorageFlags): this ()
        {
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }

        public X509Certificate(string fileName, SecureString password, X509KeyStorageFlags keyStorageFlags): this ()
        {
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }

        public X509Certificate(IntPtr handle): this ()
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidHandle"), "handle");
            Contract.EndContractBlock();
            X509Utils._DuplicateCertContext(handle, ref m_safeCertContext);
        }

        public X509Certificate(X509Certificate cert): this ()
        {
            if (cert == null)
                throw new ArgumentNullException("cert");
            Contract.EndContractBlock();
            if (cert.m_safeCertContext.pCertContext != IntPtr.Zero)
            {
                m_safeCertContext = cert.GetCertContextForCloning();
                m_certContextCloned = true;
            }
        }

        public X509Certificate(SerializationInfo info, StreamingContext context): this ()
        {
            byte[] rawData = (byte[])info.GetValue("RawData", typeof (byte[]));
            if (rawData != null)
                LoadCertificateFromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public static X509Certificate CreateFromCertFile(string filename)
        {
            return new X509Certificate(filename);
        }

        public static X509Certificate CreateFromSignedFile(string filename)
        {
            return new X509Certificate(filename);
        }

        public IntPtr Handle
        {
            [System.Security.SecurityCritical]
            get
            {
                return m_safeCertContext.pCertContext;
            }
        }

        public virtual string GetName()
        {
            ThrowIfContextInvalid();
            return X509Utils._GetSubjectInfo(m_safeCertContext, X509Constants.CERT_NAME_RDN_TYPE, true);
        }

        public virtual string GetIssuerName()
        {
            ThrowIfContextInvalid();
            return X509Utils._GetIssuerName(m_safeCertContext, true);
        }

        public virtual byte[] GetSerialNumber()
        {
            ThrowIfContextInvalid();
            if (m_serialNumber == null)
                m_serialNumber = X509Utils._GetSerialNumber(m_safeCertContext);
            return (byte[])m_serialNumber.Clone();
        }

        public virtual string GetSerialNumberString()
        {
            return SerialNumber;
        }

        public virtual byte[] GetKeyAlgorithmParameters()
        {
            ThrowIfContextInvalid();
            if (m_publicKeyParameters == null)
                m_publicKeyParameters = X509Utils._GetPublicKeyParameters(m_safeCertContext);
            return (byte[])m_publicKeyParameters.Clone();
        }

        public virtual string GetKeyAlgorithmParametersString()
        {
            ThrowIfContextInvalid();
            return Hex.EncodeHexString(GetKeyAlgorithmParameters());
        }

        public virtual string GetKeyAlgorithm()
        {
            ThrowIfContextInvalid();
            if (m_publicKeyOid == null)
                m_publicKeyOid = X509Utils._GetPublicKeyOid(m_safeCertContext);
            return m_publicKeyOid;
        }

        public virtual byte[] GetPublicKey()
        {
            ThrowIfContextInvalid();
            if (m_publicKeyValue == null)
                m_publicKeyValue = X509Utils._GetPublicKeyValue(m_safeCertContext);
            return (byte[])m_publicKeyValue.Clone();
        }

        public virtual string GetPublicKeyString()
        {
            return Hex.EncodeHexString(GetPublicKey());
        }

        public virtual byte[] GetRawCertData()
        {
            return RawData;
        }

        public virtual string GetRawCertDataString()
        {
            return Hex.EncodeHexString(GetRawCertData());
        }

        public virtual byte[] GetCertHash()
        {
            SetThumbprint();
            return (byte[])m_thumbprint.Clone();
        }

        public virtual string GetCertHashString()
        {
            SetThumbprint();
            return Hex.EncodeHexString(m_thumbprint);
        }

        public virtual string GetEffectiveDateString()
        {
            return NotBefore.ToString();
        }

        public virtual string GetExpirationDateString()
        {
            return NotAfter.ToString();
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is X509Certificate))
                return false;
            X509Certificate other = (X509Certificate)obj;
            return this.Equals(other);
        }

        public virtual bool Equals(X509Certificate other)
        {
            if (other == null)
                return false;
            if (m_safeCertContext.IsInvalid)
                return other.m_safeCertContext.IsInvalid;
            if (!this.Issuer.Equals(other.Issuer))
                return false;
            if (!this.SerialNumber.Equals(other.SerialNumber))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            if (m_safeCertContext.IsInvalid)
                return 0;
            SetThumbprint();
            int value = 0;
            for (int i = 0; i < m_thumbprint.Length && i < 4; ++i)
            {
                value = value << 8 | m_thumbprint[i];
            }

            return value;
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public virtual string ToString(bool fVerbose)
        {
            if (fVerbose == false || m_safeCertContext.IsInvalid)
                return GetType().FullName;
            StringBuilder sb = new StringBuilder();
            sb.Append("[Subject]" + Environment.NewLine + "  ");
            sb.Append(this.Subject);
            sb.Append(Environment.NewLine + Environment.NewLine + "[Issuer]" + Environment.NewLine + "  ");
            sb.Append(this.Issuer);
            sb.Append(Environment.NewLine + Environment.NewLine + "[Serial Number]" + Environment.NewLine + "  ");
            sb.Append(this.SerialNumber);
            sb.Append(Environment.NewLine + Environment.NewLine + "[Not Before]" + Environment.NewLine + "  ");
            sb.Append(FormatDate(this.NotBefore));
            sb.Append(Environment.NewLine + Environment.NewLine + "[Not After]" + Environment.NewLine + "  ");
            sb.Append(FormatDate(this.NotAfter));
            sb.Append(Environment.NewLine + Environment.NewLine + "[Thumbprint]" + Environment.NewLine + "  ");
            sb.Append(this.GetCertHashString());
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        protected static string FormatDate(DateTime date)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            if (!culture.DateTimeFormat.Calendar.IsValidDay(date.Year, date.Month, date.Day, 0))
            {
                if (culture.DateTimeFormat.Calendar is UmAlQuraCalendar)
                {
                    culture = culture.Clone() as CultureInfo;
                    culture.DateTimeFormat.Calendar = new HijriCalendar();
                }
                else
                {
                    culture = CultureInfo.InvariantCulture;
                }
            }

            return date.ToString(culture);
        }

        public virtual string GetFormat()
        {
            return m_format;
        }

        public string Issuer
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                ThrowIfContextInvalid();
                if (m_issuerName == null)
                    m_issuerName = X509Utils._GetIssuerName(m_safeCertContext, false);
                return m_issuerName;
            }
        }

        public string Subject
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                ThrowIfContextInvalid();
                if (m_subjectName == null)
                    m_subjectName = X509Utils._GetSubjectInfo(m_safeCertContext, X509Constants.CERT_NAME_RDN_TYPE, false);
                return m_subjectName;
            }
        }

        public virtual void Import(byte[] rawData)
        {
            Reset();
            LoadCertificateFromBlob(rawData, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public virtual void Import(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            Reset();
            LoadCertificateFromBlob(rawData, password, keyStorageFlags);
        }

        public virtual void Import(byte[] rawData, SecureString password, X509KeyStorageFlags keyStorageFlags)
        {
            Reset();
            LoadCertificateFromBlob(rawData, password, keyStorageFlags);
        }

        public virtual void Import(string fileName)
        {
            Reset();
            LoadCertificateFromFile(fileName, null, X509KeyStorageFlags.DefaultKeySet);
        }

        public virtual void Import(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            Reset();
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }

        public virtual void Import(string fileName, SecureString password, X509KeyStorageFlags keyStorageFlags)
        {
            Reset();
            LoadCertificateFromFile(fileName, password, keyStorageFlags);
        }

        public virtual byte[] Export(X509ContentType contentType)
        {
            return ExportHelper(contentType, null);
        }

        public virtual byte[] Export(X509ContentType contentType, string password)
        {
            return ExportHelper(contentType, password);
        }

        public virtual byte[] Export(X509ContentType contentType, SecureString password)
        {
            return ExportHelper(contentType, password);
        }

        public virtual void Reset()
        {
            m_subjectName = null;
            m_issuerName = null;
            m_serialNumber = null;
            m_publicKeyParameters = null;
            m_publicKeyValue = null;
            m_publicKeyOid = null;
            m_rawData = null;
            m_thumbprint = null;
            m_notBefore = DateTime.MinValue;
            m_notAfter = DateTime.MinValue;
            if (!m_safeCertContext.IsInvalid)
            {
                if (!m_certContextCloned)
                {
                    m_safeCertContext.Dispose();
                }

                m_safeCertContext = SafeCertContextHandle.InvalidHandle;
            }

            m_certContextCloned = false;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reset();
            }
        }

        internal SafeCertContextHandle CertContext
        {
            [System.Security.SecurityCritical]
            get
            {
                return m_safeCertContext;
            }
        }

        internal SafeCertContextHandle GetCertContextForCloning()
        {
            m_certContextCloned = true;
            return m_safeCertContext;
        }

        private void ThrowIfContextInvalid()
        {
            if (m_safeCertContext.IsInvalid)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidHandle"), "m_safeCertContext");
        }

        private DateTime NotAfter
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                ThrowIfContextInvalid();
                if (m_notAfter == DateTime.MinValue)
                {
                    Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME();
                    X509Utils._GetDateNotAfter(m_safeCertContext, ref fileTime);
                    m_notAfter = DateTime.FromFileTime(fileTime.ToTicks());
                }

                return m_notAfter;
            }
        }

        private DateTime NotBefore
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                ThrowIfContextInvalid();
                if (m_notBefore == DateTime.MinValue)
                {
                    Win32Native.FILE_TIME fileTime = new Win32Native.FILE_TIME();
                    X509Utils._GetDateNotBefore(m_safeCertContext, ref fileTime);
                    m_notBefore = DateTime.FromFileTime(fileTime.ToTicks());
                }

                return m_notBefore;
            }
        }

        private byte[] RawData
        {
            [System.Security.SecurityCritical]
            get
            {
                ThrowIfContextInvalid();
                if (m_rawData == null)
                    m_rawData = X509Utils._GetCertRawData(m_safeCertContext);
                return (byte[])m_rawData.Clone();
            }
        }

        private string SerialNumber
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                ThrowIfContextInvalid();
                if (m_serialNumber == null)
                    m_serialNumber = X509Utils._GetSerialNumber(m_safeCertContext);
                return Hex.EncodeHexStringFromInt(m_serialNumber);
            }
        }

        private void SetThumbprint()
        {
            ThrowIfContextInvalid();
            if (m_thumbprint == null)
                m_thumbprint = X509Utils._GetThumbprint(m_safeCertContext);
        }

        private byte[] ExportHelper(X509ContentType contentType, object password)
        {
            switch (contentType)
            {
                case X509ContentType.Cert:
                    break;
                case (X509ContentType)0x02:
                case (X509ContentType)0x03:
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_InvalidContentType"), new NotSupportedException());
                default:
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_X509_InvalidContentType"));
            }

            return RawData;
        }

        private void LoadCertificateFromBlob(byte[] rawData, object password, X509KeyStorageFlags keyStorageFlags)
        {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyOrNullArray"), "rawData");
            Contract.EndContractBlock();
            X509ContentType contentType = X509Utils.MapContentType(X509Utils._QueryCertBlobType(rawData));
            uint dwFlags = X509Utils.MapKeyStorageFlags(keyStorageFlags);
            IntPtr szPassword = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                szPassword = X509Utils.PasswordToHGlobalUni(password);
                X509Utils._LoadCertFromBlob(rawData, szPassword, dwFlags, false, ref m_safeCertContext);
            }
            finally
            {
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(szPassword);
            }
        }

        private void LoadCertificateFromFile(string fileName, object password, X509KeyStorageFlags keyStorageFlags)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            Contract.EndContractBlock();
            string fullPath = Path.GetFullPathInternal(fileName);
            new FileIOPermission(FileIOPermissionAccess.Read, fullPath).Demand();
            X509ContentType contentType = X509Utils.MapContentType(X509Utils._QueryCertFileType(fileName));
            uint dwFlags = X509Utils.MapKeyStorageFlags(keyStorageFlags);
            IntPtr szPassword = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                szPassword = X509Utils.PasswordToHGlobalUni(password);
                X509Utils._LoadCertFromFile(fileName, szPassword, dwFlags, false, ref m_safeCertContext);
            }
            finally
            {
                if (szPassword != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(szPassword);
            }
        }

        protected internal String CreateHexString(byte[] sArray)
        {
            return Hex.EncodeHexString(sArray);
        }
    }
}