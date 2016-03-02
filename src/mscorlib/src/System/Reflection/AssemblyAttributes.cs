using System.Configuration.Assemblies;

namespace System.Reflection
{
    public sealed class AssemblyCopyrightAttribute : Attribute
    {
        private String m_copyright;
        public AssemblyCopyrightAttribute(String copyright)
        {
            m_copyright = copyright;
        }

        public String Copyright
        {
            get
            {
                return m_copyright;
            }
        }
    }

    public sealed class AssemblyTrademarkAttribute : Attribute
    {
        private String m_trademark;
        public AssemblyTrademarkAttribute(String trademark)
        {
            m_trademark = trademark;
        }

        public String Trademark
        {
            get
            {
                return m_trademark;
            }
        }
    }

    public sealed class AssemblyProductAttribute : Attribute
    {
        private String m_product;
        public AssemblyProductAttribute(String product)
        {
            m_product = product;
        }

        public String Product
        {
            get
            {
                return m_product;
            }
        }
    }

    public sealed class AssemblyCompanyAttribute : Attribute
    {
        private String m_company;
        public AssemblyCompanyAttribute(String company)
        {
            m_company = company;
        }

        public String Company
        {
            get
            {
                return m_company;
            }
        }
    }

    public sealed class AssemblyDescriptionAttribute : Attribute
    {
        private String m_description;
        public AssemblyDescriptionAttribute(String description)
        {
            m_description = description;
        }

        public String Description
        {
            get
            {
                return m_description;
            }
        }
    }

    public sealed class AssemblyTitleAttribute : Attribute
    {
        private String m_title;
        public AssemblyTitleAttribute(String title)
        {
            m_title = title;
        }

        public String Title
        {
            get
            {
                return m_title;
            }
        }
    }

    public sealed class AssemblyConfigurationAttribute : Attribute
    {
        private String m_configuration;
        public AssemblyConfigurationAttribute(String configuration)
        {
            m_configuration = configuration;
        }

        public String Configuration
        {
            get
            {
                return m_configuration;
            }
        }
    }

    public sealed class AssemblyDefaultAliasAttribute : Attribute
    {
        private String m_defaultAlias;
        public AssemblyDefaultAliasAttribute(String defaultAlias)
        {
            m_defaultAlias = defaultAlias;
        }

        public String DefaultAlias
        {
            get
            {
                return m_defaultAlias;
            }
        }
    }

    public sealed class AssemblyInformationalVersionAttribute : Attribute
    {
        private String m_informationalVersion;
        public AssemblyInformationalVersionAttribute(String informationalVersion)
        {
            m_informationalVersion = informationalVersion;
        }

        public String InformationalVersion
        {
            get
            {
                return m_informationalVersion;
            }
        }
    }

    public sealed class AssemblyFileVersionAttribute : Attribute
    {
        private String _version;
        public AssemblyFileVersionAttribute(String version)
        {
            if (version == null)
                throw new ArgumentNullException("version");
                        _version = version;
        }

        public String Version
        {
            get
            {
                return _version;
            }
        }
    }

    public unsafe sealed class AssemblyCultureAttribute : Attribute
    {
        private String m_culture;
        public AssemblyCultureAttribute(String culture)
        {
            m_culture = culture;
        }

        public String Culture
        {
            get
            {
                return m_culture;
            }
        }
    }

    public unsafe sealed class AssemblyVersionAttribute : Attribute
    {
        private String m_version;
        public AssemblyVersionAttribute(String version)
        {
            m_version = version;
        }

        public String Version
        {
            get
            {
                return m_version;
            }
        }
    }

    public sealed class AssemblyKeyFileAttribute : Attribute
    {
        private String m_keyFile;
        public AssemblyKeyFileAttribute(String keyFile)
        {
            m_keyFile = keyFile;
        }

        public String KeyFile
        {
            get
            {
                return m_keyFile;
            }
        }
    }

    public sealed class AssemblyDelaySignAttribute : Attribute
    {
        private bool m_delaySign;
        public AssemblyDelaySignAttribute(bool delaySign)
        {
            m_delaySign = delaySign;
        }

        public bool DelaySign
        {
            get
            {
                return m_delaySign;
            }
        }
    }

    public unsafe sealed class AssemblyAlgorithmIdAttribute : Attribute
    {
        private uint m_algId;
        public AssemblyAlgorithmIdAttribute(AssemblyHashAlgorithm algorithmId)
        {
            m_algId = (uint)algorithmId;
        }

        public AssemblyAlgorithmIdAttribute(uint algorithmId)
        {
            m_algId = algorithmId;
        }

        public uint AlgorithmId
        {
            get
            {
                return m_algId;
            }
        }
    }

    public unsafe sealed class AssemblyFlagsAttribute : Attribute
    {
        private AssemblyNameFlags m_flags;
        public AssemblyFlagsAttribute(uint flags)
        {
            m_flags = (AssemblyNameFlags)flags;
        }

        public uint Flags
        {
            get
            {
                return (uint)m_flags;
            }
        }

        public int AssemblyFlags
        {
            get
            {
                return (int)m_flags;
            }
        }

        public AssemblyFlagsAttribute(int assemblyFlags)
        {
            m_flags = (AssemblyNameFlags)assemblyFlags;
        }

        public AssemblyFlagsAttribute(AssemblyNameFlags assemblyFlags)
        {
            m_flags = assemblyFlags;
        }
    }

    public sealed class AssemblyMetadataAttribute : Attribute
    {
        private String m_key;
        private String m_value;
        public AssemblyMetadataAttribute(string key, string value)
        {
            m_key = key;
            m_value = value;
        }

        public string Key
        {
            get
            {
                return m_key;
            }
        }

        public string Value
        {
            get
            {
                return m_value;
            }
        }
    }

    public sealed class AssemblySignatureKeyAttribute : Attribute
    {
        private String _publicKey;
        private String _countersignature;
        public AssemblySignatureKeyAttribute(String publicKey, String countersignature)
        {
            _publicKey = publicKey;
            _countersignature = countersignature;
        }

        public String PublicKey
        {
            get
            {
                return _publicKey;
            }
        }

        public String Countersignature
        {
            get
            {
                return _countersignature;
            }
        }
    }

    public sealed class AssemblyKeyNameAttribute : Attribute
    {
        private String m_keyName;
        public AssemblyKeyNameAttribute(String keyName)
        {
            m_keyName = keyName;
        }

        public String KeyName
        {
            get
            {
                return m_keyName;
            }
        }
    }
}