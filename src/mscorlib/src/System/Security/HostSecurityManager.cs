using System.Reflection;
using System.Security.Policy;

namespace System.Security
{
    [Flags]
    public enum HostSecurityManagerOptions
    {
        None = 0x0000,
        HostAppDomainEvidence = 0x0001,
        HostPolicyLevel = 0x0002,
        HostAssemblyEvidence = 0x0004,
        HostDetermineApplicationTrust = 0x0008,
        HostResolvePolicy = 0x0010,
        AllFlags = 0x001F
    }

    public class HostSecurityManager
    {
        public HostSecurityManager()
        {
        }

        public virtual HostSecurityManagerOptions Flags
        {
            get
            {
                return HostSecurityManagerOptions.AllFlags;
            }
        }

        public virtual Evidence ProvideAppDomainEvidence(Evidence inputEvidence)
        {
            return inputEvidence;
        }

        public virtual Evidence ProvideAssemblyEvidence(Assembly loadedAssembly, Evidence inputEvidence)
        {
            return inputEvidence;
        }

        public virtual Type[] GetHostSuppliedAppDomainEvidenceTypes()
        {
            return null;
        }

        public virtual Type[] GetHostSuppliedAssemblyEvidenceTypes(Assembly assembly)
        {
            return null;
        }

        public virtual EvidenceBase GenerateAppDomainEvidence(Type evidenceType)
        {
            return null;
        }

        public virtual EvidenceBase GenerateAssemblyEvidence(Type evidenceType, Assembly assembly)
        {
            return null;
        }
    }
}