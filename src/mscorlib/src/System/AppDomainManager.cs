using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace System
{
    public class AppDomainManager
    {
        public AppDomainManager()
        {
        }

        public virtual void InitializeNewDomain(AppDomainSetup appDomainInfo)
        {
        }

        private static extern void GetEntryAssembly(ObjectHandleOnStack retAssembly);
        private Assembly m_entryAssembly = null;
        public virtual Assembly EntryAssembly
        {
            [System.Security.SecurityCritical]
            get
            {
                if (m_entryAssembly == null)
                {
                    {
                        RuntimeAssembly entryAssembly = null;
                        GetEntryAssembly(JitHelpers.GetObjectHandleOnStack(ref entryAssembly));
                        m_entryAssembly = entryAssembly;
                    }
                }

                return m_entryAssembly;
            }
        }

        internal static AppDomainManager CurrentAppDomainManager
        {
            [System.Security.SecurityCritical]
            get
            {
                return AppDomain.CurrentDomain.DomainManager;
            }
        }

        public virtual bool CheckSecuritySettings(SecurityState state)
        {
            return false;
        }
    }
}