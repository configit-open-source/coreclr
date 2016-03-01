using System.Diagnostics.Contracts;
using System.Globalization;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security.Policy
{
    public sealed class Site : EvidenceBase, IIdentityPermissionFactory
    {
        private SiteString m_name;
        public Site(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            m_name = new SiteString(name);
        }

        private Site(SiteString name)
        {
            Contract.Assert(name != null);
            m_name = name;
        }

        public static Site CreateFromUrl(String url)
        {
            return new Site(ParseSiteFromUrl(url));
        }

        private static SiteString ParseSiteFromUrl(String name)
        {
            URLString urlString = new URLString(name);
            if (String.Compare(urlString.Scheme, "file", StringComparison.OrdinalIgnoreCase) == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
            return new SiteString(new URLString(name).Host);
        }

        public String Name
        {
            get
            {
                return m_name.ToString();
            }
        }

        internal SiteString GetSiteString()
        {
            return m_name;
        }

        public IPermission CreateIdentityPermission(Evidence evidence)
        {
            return new SiteIdentityPermission(Name);
        }

        public override bool Equals(Object o)
        {
            Site other = o as Site;
            if (other == null)
            {
                return false;
            }

            return String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override EvidenceBase Clone()
        {
            return new Site(m_name);
        }

        public Object Copy()
        {
            return Clone();
        }

        internal Object Normalize()
        {
            return m_name.ToString().ToUpper(CultureInfo.InvariantCulture);
        }
    }
}