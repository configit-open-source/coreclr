namespace System.Security.Permissions
{
    using System;
    using System.IO;
    using System.Security;
    using System.Security.Util;
    using System.Globalization;

    public enum IsolatedStorageContainment
    {
        None = 0x00,
        DomainIsolationByUser = 0x10,
        ApplicationIsolationByUser = 0x15,
        AssemblyIsolationByUser = 0x20,
        DomainIsolationByMachine = 0x30,
        AssemblyIsolationByMachine = 0x40,
        ApplicationIsolationByMachine = 0x45,
        DomainIsolationByRoamingUser = 0x50,
        AssemblyIsolationByRoamingUser = 0x60,
        ApplicationIsolationByRoamingUser = 0x65,
        AdministerIsolatedStorageByUser = 0x70,
        UnrestrictedIsolatedStorage = 0xF0
    }

    ;
    abstract public class IsolatedStoragePermission : CodeAccessPermission, IUnrestrictedPermission
    {
        internal long m_userQuota;
        internal long m_machineQuota;
        internal long m_expirationDays;
        internal bool m_permanentData;
        internal IsolatedStorageContainment m_allowed;
        protected IsolatedStoragePermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_userQuota = Int64.MaxValue;
                m_machineQuota = Int64.MaxValue;
                m_expirationDays = Int64.MaxValue;
                m_permanentData = true;
                m_allowed = IsolatedStorageContainment.UnrestrictedIsolatedStorage;
            }
            else if (state == PermissionState.None)
            {
                m_userQuota = 0;
                m_machineQuota = 0;
                m_expirationDays = 0;
                m_permanentData = false;
                m_allowed = IsolatedStorageContainment.None;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        internal IsolatedStoragePermission(IsolatedStorageContainment UsageAllowed, long ExpirationDays, bool PermanentData)
        {
            m_userQuota = 0;
            m_machineQuota = 0;
            m_expirationDays = ExpirationDays;
            m_permanentData = PermanentData;
            m_allowed = UsageAllowed;
        }

        internal IsolatedStoragePermission(IsolatedStorageContainment UsageAllowed, long ExpirationDays, bool PermanentData, long UserQuota)
        {
            m_machineQuota = 0;
            m_userQuota = UserQuota;
            m_expirationDays = ExpirationDays;
            m_permanentData = PermanentData;
            m_allowed = UsageAllowed;
        }

        public long UserQuota
        {
            set
            {
                m_userQuota = value;
            }

            get
            {
                return m_userQuota;
            }
        }

        public IsolatedStorageContainment UsageAllowed
        {
            set
            {
                m_allowed = value;
            }

            get
            {
                return m_allowed;
            }
        }

        public bool IsUnrestricted()
        {
            return m_allowed == IsolatedStorageContainment.UnrestrictedIsolatedStorage;
        }

        internal static long min(long x, long y)
        {
            return x > y ? y : x;
        }

        internal static long max(long x, long y)
        {
            return x < y ? y : x;
        }
    }
}