namespace System.Security.Cryptography
{
    using System.Security.AccessControl;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [Flags]
    public enum CspProviderFlags
    {
        NoFlags = 0x0000,
        UseMachineKeyStore = 0x0001,
        UseDefaultKeyContainer = 0x0002,
        UseNonExportableKey = 0x0004,
        UseExistingKey = 0x0008,
        UseArchivableKey = 0x0010,
        UseUserProtectedKey = 0x0020,
        NoPrompt = 0x0040,
        CreateEphemeralKey = 0x0080
    }

    public sealed class CspParameters
    {
        public int ProviderType;
        public string ProviderName;
        public string KeyContainerName;
        public int KeyNumber;
        private int m_flags;
        public CspProviderFlags Flags
        {
            get
            {
                return (CspProviderFlags)m_flags;
            }

            set
            {
                int allFlags = 0x00FF;
                Contract.Assert((CspProviderFlags.UseMachineKeyStore | CspProviderFlags.UseDefaultKeyContainer | CspProviderFlags.UseNonExportableKey | CspProviderFlags.UseExistingKey | CspProviderFlags.UseArchivableKey | CspProviderFlags.UseUserProtectedKey | CspProviderFlags.NoPrompt | CspProviderFlags.CreateEphemeralKey) == (CspProviderFlags)allFlags, "allFlags does not match all CspProviderFlags");
                int flags = (int)value;
                if ((flags & ~allFlags) != 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)value), "value");
                m_flags = flags;
            }
        }

        private SecureString m_keyPassword;
        public SecureString KeyPassword
        {
            get
            {
                return m_keyPassword;
            }

            set
            {
                m_keyPassword = value;
                m_parentWindowHandle = IntPtr.Zero;
            }
        }

        private IntPtr m_parentWindowHandle;
        public IntPtr ParentWindowHandle
        {
            get
            {
                return m_parentWindowHandle;
            }

            set
            {
                m_parentWindowHandle = value;
                m_keyPassword = null;
            }
        }

        public CspParameters(): this (Utils.DefaultRsaProviderType, null, null)
        {
        }

        public CspParameters(int dwTypeIn): this (dwTypeIn, null, null)
        {
        }

        public CspParameters(int dwTypeIn, string strProviderNameIn): this (dwTypeIn, strProviderNameIn, null)
        {
        }

        public CspParameters(int dwTypeIn, string strProviderNameIn, string strContainerNameIn): this (dwTypeIn, strProviderNameIn, strContainerNameIn, CspProviderFlags.NoFlags)
        {
        }

        internal CspParameters(int providerType, string providerName, string keyContainerName, CspProviderFlags flags)
        {
            ProviderType = providerType;
            ProviderName = providerName;
            KeyContainerName = keyContainerName;
            KeyNumber = -1;
            Flags = flags;
        }

        internal CspParameters(CspParameters parameters)
        {
            ProviderType = parameters.ProviderType;
            ProviderName = parameters.ProviderName;
            KeyContainerName = parameters.KeyContainerName;
            KeyNumber = parameters.KeyNumber;
            Flags = parameters.Flags;
            m_keyPassword = parameters.m_keyPassword;
            m_parentWindowHandle = parameters.m_parentWindowHandle;
        }
    }
}