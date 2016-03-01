using System.Diagnostics.Contracts;
using System.Security.Util;
using System.Text;

namespace System
{
    public sealed class ApplicationId
    {
        private string m_name;
        private Version m_version;
        private string m_processorArchitecture;
        private string m_culture;
        internal byte[] m_publicKeyToken;
        internal ApplicationId()
        {
        }

        public ApplicationId(byte[] publicKeyToken, string name, Version version, string processorArchitecture, string culture)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyApplicationName"));
            if (version == null)
                throw new ArgumentNullException("version");
            if (publicKeyToken == null)
                throw new ArgumentNullException("publicKeyToken");
            Contract.EndContractBlock();
            m_publicKeyToken = new byte[publicKeyToken.Length];
            Array.Copy(publicKeyToken, 0, m_publicKeyToken, 0, publicKeyToken.Length);
            m_name = name;
            m_version = version;
            m_processorArchitecture = processorArchitecture;
            m_culture = culture;
        }

        public byte[] PublicKeyToken
        {
            get
            {
                byte[] publicKeyToken = new byte[m_publicKeyToken.Length];
                Array.Copy(m_publicKeyToken, 0, publicKeyToken, 0, m_publicKeyToken.Length);
                return publicKeyToken;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public Version Version
        {
            get
            {
                return m_version;
            }
        }

        public string ProcessorArchitecture
        {
            get
            {
                return m_processorArchitecture;
            }
        }

        public string Culture
        {
            get
            {
                return m_culture;
            }
        }

        public ApplicationId Copy()
        {
            return new ApplicationId(m_publicKeyToken, m_name, m_version, m_processorArchitecture, m_culture);
        }

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append(this.m_name);
            if (m_culture != null)
            {
                sb.Append(", culture=\"");
                sb.Append(m_culture);
                sb.Append("\"");
            }

            sb.Append(", version=\"");
            sb.Append(m_version.ToString());
            sb.Append("\"");
            if (m_publicKeyToken != null)
            {
                sb.Append(", publicKeyToken=\"");
                sb.Append(Hex.EncodeHexString(m_publicKeyToken));
                sb.Append("\"");
            }

            if (m_processorArchitecture != null)
            {
                sb.Append(", processorArchitecture =\"");
                sb.Append(this.m_processorArchitecture);
                sb.Append("\"");
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public override bool Equals(Object o)
        {
            ApplicationId other = (o as ApplicationId);
            if (other == null)
                return false;
            if (!(Equals(this.m_name, other.m_name) && Equals(this.m_version, other.m_version) && Equals(this.m_processorArchitecture, other.m_processorArchitecture) && Equals(this.m_culture, other.m_culture)))
                return false;
            if (this.m_publicKeyToken.Length != other.m_publicKeyToken.Length)
                return false;
            for (int i = 0; i < this.m_publicKeyToken.Length; ++i)
            {
                if (this.m_publicKeyToken[i] != other.m_publicKeyToken[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return m_name.GetHashCode() ^ m_version.GetHashCode();
        }
    }
}