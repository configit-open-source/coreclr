
using System.Security.Permissions;

namespace System.Security.Policy
{
    public sealed class Zone : EvidenceBase, IIdentityPermissionFactory
    {
        private SecurityZone m_zone;
        private static readonly String[] s_names = {"MyComputer", "Intranet", "Trusted", "Internet", "Untrusted", "NoZone"};
        public Zone(SecurityZone zone)
        {
            if (zone < SecurityZone.NoZone || zone > SecurityZone.Untrusted)
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalZone"));
                        m_zone = zone;
        }

        private Zone(Zone zone)
        {
                        m_zone = zone.m_zone;
        }

        public IPermission CreateIdentityPermission(Evidence evidence)
        {
            return new ZoneIdentityPermission(SecurityZone);
        }

        public SecurityZone SecurityZone
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return m_zone;
            }
        }

        public override bool Equals(Object o)
        {
            Zone other = o as Zone;
            if (other == null)
            {
                return false;
            }

            return SecurityZone == other.SecurityZone;
        }

        public override int GetHashCode()
        {
            return (int)SecurityZone;
        }

        public override EvidenceBase Clone()
        {
            return new Zone(this);
        }

        public Object Copy()
        {
            return Clone();
        }

        internal Object Normalize()
        {
            return s_names[(int)SecurityZone];
        }
    }
}