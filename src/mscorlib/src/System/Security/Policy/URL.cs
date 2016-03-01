namespace System.Security.Policy
{
    using System.IO;
    using System.Security.Util;
    using UrlIdentityPermission = System.Security.Permissions.UrlIdentityPermission;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    public sealed class Url : EvidenceBase, IIdentityPermissionFactory
    {
        private URLString m_url;
        internal Url(String name, bool parsed)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            m_url = new URLString(name, parsed);
        }

        public Url(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            m_url = new URLString(name);
        }

        private Url(Url url)
        {
            Contract.Assert(url != null);
            m_url = url.m_url;
        }

        public String Value
        {
            get
            {
                return m_url.ToString();
            }
        }

        internal URLString GetURLString()
        {
            return m_url;
        }

        public IPermission CreateIdentityPermission(Evidence evidence)
        {
            return new UrlIdentityPermission(m_url);
        }

        public override bool Equals(Object o)
        {
            Url other = o as Url;
            if (other == null)
            {
                return false;
            }

            return other.m_url.Equals(m_url);
        }

        public override int GetHashCode()
        {
            return this.m_url.GetHashCode();
        }

        public override EvidenceBase Clone()
        {
            return new Url(this);
        }

        public Object Copy()
        {
            return Clone();
        }

        internal Object Normalize()
        {
            return m_url.NormalizeUrl();
        }
    }
}