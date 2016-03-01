using System;
using System.Security;
using System.Security.Permissions;

namespace System.Security
{
    public abstract class SecurityState
    {
        protected SecurityState()
        {
        }

        public bool IsStateAvailable()
        {
            AppDomainManager domainManager = AppDomainManager.CurrentAppDomainManager;
            return domainManager != null ? domainManager.CheckSecuritySettings(this) : true;
        }

        public abstract void EnsureState();
    }
}