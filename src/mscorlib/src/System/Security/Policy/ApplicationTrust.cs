namespace System.Security.Policy
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.Permissions;
    using System.Security.Util;
    using System.Text;
    using System.Threading;
    using System.Diagnostics.Contracts;

    public enum ApplicationVersionMatch
    {
        MatchExactVersion,
        MatchAllVersions
    }

    public sealed class ApplicationTrust : EvidenceBase, ISecurityEncodable
    {
        private PolicyStatement m_psDefaultGrant;
        private IList<StrongName> m_fullTrustAssemblies;
        private int m_grantSetSpecialFlags;
        public ApplicationTrust(): this (new PermissionSet(PermissionState.None))
        {
        }

        internal ApplicationTrust(PermissionSet defaultGrantSet)
        {
            InitDefaultGrantSet(defaultGrantSet);
            m_fullTrustAssemblies = new List<StrongName>().AsReadOnly();
        }

        public ApplicationTrust(PermissionSet defaultGrantSet, IEnumerable<StrongName> fullTrustAssemblies)
        {
            if (fullTrustAssemblies == null)
            {
                throw new ArgumentNullException("fullTrustAssemblies");
            }

            InitDefaultGrantSet(defaultGrantSet);
            List<StrongName> fullTrustList = new List<StrongName>();
            foreach (StrongName strongName in fullTrustAssemblies)
            {
                if (strongName == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NullFullTrustAssembly"));
                }

                fullTrustList.Add(new StrongName(strongName.PublicKey, strongName.Name, strongName.Version));
            }

            m_fullTrustAssemblies = fullTrustList.AsReadOnly();
        }

        private void InitDefaultGrantSet(PermissionSet defaultGrantSet)
        {
            if (defaultGrantSet == null)
            {
                throw new ArgumentNullException("defaultGrantSet");
            }

            DefaultGrantSet = new PolicyStatement(defaultGrantSet);
        }

        public PolicyStatement DefaultGrantSet
        {
            get
            {
                if (m_psDefaultGrant == null)
                    return new PolicyStatement(new PermissionSet(PermissionState.None));
                return m_psDefaultGrant;
            }

            set
            {
                if (value == null)
                {
                    m_psDefaultGrant = null;
                    m_grantSetSpecialFlags = 0;
                }
                else
                {
                    m_psDefaultGrant = value;
                    m_grantSetSpecialFlags = SecurityManager.GetSpecialFlags(m_psDefaultGrant.PermissionSet, null);
                }
            }
        }

        public IList<StrongName> FullTrustAssemblies
        {
            get
            {
                return m_fullTrustAssemblies;
            }
        }

        public override EvidenceBase Clone()
        {
            return base.Clone();
        }
    }
}