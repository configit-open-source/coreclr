namespace System.Runtime.InteropServices.WindowsRuntime
{
    public sealed class DefaultInterfaceAttribute : Attribute
    {
        private Type m_defaultInterface;
        public DefaultInterfaceAttribute(Type defaultInterface)
        {
            m_defaultInterface = defaultInterface;
        }

        public Type DefaultInterface
        {
            get
            {
                return m_defaultInterface;
            }
        }
    }

    internal sealed class WindowsRuntimeImportAttribute : Attribute
    {
        public WindowsRuntimeImportAttribute()
        {
        }
    }

    public sealed class InterfaceImplementedInVersionAttribute : Attribute
    {
        public InterfaceImplementedInVersionAttribute(Type interfaceType, byte majorVersion, byte minorVersion, byte buildVersion, byte revisionVersion)
        {
            m_interfaceType = interfaceType;
            m_majorVersion = majorVersion;
            m_minorVersion = minorVersion;
            m_buildVersion = buildVersion;
            m_revisionVersion = revisionVersion;
        }

        public Type InterfaceType
        {
            get
            {
                return m_interfaceType;
            }
        }

        public byte MajorVersion
        {
            get
            {
                return m_majorVersion;
            }
        }

        public byte MinorVersion
        {
            get
            {
                return m_minorVersion;
            }
        }

        public byte BuildVersion
        {
            get
            {
                return m_buildVersion;
            }
        }

        public byte RevisionVersion
        {
            get
            {
                return m_revisionVersion;
            }
        }

        private Type m_interfaceType;
        private byte m_majorVersion;
        private byte m_minorVersion;
        private byte m_buildVersion;
        private byte m_revisionVersion;
    }

    public sealed class ReadOnlyArrayAttribute : Attribute
    {
        public ReadOnlyArrayAttribute()
        {
        }
    }

    public sealed class WriteOnlyArrayAttribute : Attribute
    {
        public WriteOnlyArrayAttribute()
        {
        }
    }

    public sealed class ReturnValueNameAttribute : Attribute
    {
        private string m_Name;
        public ReturnValueNameAttribute(string name)
        {
            m_Name = name;
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
        }
    }
}