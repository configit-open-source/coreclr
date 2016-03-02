using System.Collections;

namespace System.Security.Permissions
{
    sealed public class ZoneIdentityPermission : CodeAccessPermission, IBuiltInPermission
    {
        private const uint AllZones = 0x1f;
        private uint m_zones;
        public ZoneIdentityPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_zones = AllZones;
            }
            else if (state == PermissionState.None)
            {
                m_zones = 0;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        public ZoneIdentityPermission(SecurityZone zone)
        {
            this.SecurityZone = zone;
        }

        internal ZoneIdentityPermission(uint zones)
        {
            m_zones = (zones & AllZones);
        }

        internal void AppendZones(ArrayList zoneList)
        {
            int nEnum = 0;
            uint nFlag;
            for (nFlag = 1; nFlag < AllZones; nFlag <<= 1)
            {
                if ((m_zones & nFlag) != 0)
                {
                    zoneList.Add((SecurityZone)nEnum);
                }

                nEnum++;
            }
        }

        public SecurityZone SecurityZone
        {
            set
            {
                VerifyZone(value);
                if (value == SecurityZone.NoZone)
                    m_zones = 0;
                else
                    m_zones = (uint)1 << (int)value;
            }

            get
            {
                SecurityZone z = SecurityZone.NoZone;
                int nEnum = 0;
                uint nFlag;
                for (nFlag = 1; nFlag < AllZones; nFlag <<= 1)
                {
                    if ((m_zones & nFlag) != 0)
                    {
                        if (z == SecurityZone.NoZone)
                            z = (SecurityZone)nEnum;
                        else
                            return SecurityZone.NoZone;
                    }

                    nEnum++;
                }

                return z;
            }
        }

        private static void VerifyZone(SecurityZone zone)
        {
            if (zone < SecurityZone.NoZone || zone > SecurityZone.Untrusted)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalZone"));
            }

                    }

        public override IPermission Copy()
        {
            return new ZoneIdentityPermission(this.m_zones);
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
                return this.m_zones == 0;
            ZoneIdentityPermission that = target as ZoneIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return (this.m_zones & that.m_zones) == this.m_zones;
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            ZoneIdentityPermission that = target as ZoneIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            uint newZones = this.m_zones & that.m_zones;
            if (newZones == 0)
                return null;
            return new ZoneIdentityPermission(newZones);
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
                return this.m_zones != 0 ? this.Copy() : null;
            ZoneIdentityPermission that = target as ZoneIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return new ZoneIdentityPermission(this.m_zones | that.m_zones);
        }

        int IBuiltInPermission.GetTokenIndex()
        {
            return ZoneIdentityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.ZoneIdentityPermissionIndex;
        }
    }
}