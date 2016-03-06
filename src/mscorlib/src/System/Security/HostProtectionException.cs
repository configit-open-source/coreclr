
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace System.Security
{
    public class HostProtectionException : SystemException
    {
        private HostProtectionResource m_protected;
        private HostProtectionResource m_demanded;
        private const String ProtectedResourcesName = "ProtectedResources";
        private const String DemandedResourcesName = "DemandedResources";
        public HostProtectionException(): base ()
        {
            m_protected = HostProtectionResource.None;
            m_demanded = HostProtectionResource.None;
        }

        public HostProtectionException(string message): base (message)
        {
            m_protected = HostProtectionResource.None;
            m_demanded = HostProtectionResource.None;
        }

        public HostProtectionException(string message, Exception e): base (message, e)
        {
            m_protected = HostProtectionResource.None;
            m_demanded = HostProtectionResource.None;
        }

        public HostProtectionException(string message, HostProtectionResource protectedResources, HostProtectionResource demandedResources): base (message)
        {
            SetErrorCode(__HResults.COR_E_HOSTPROTECTION);
            m_protected = protectedResources;
            m_demanded = demandedResources;
        }

        private HostProtectionException(HostProtectionResource protectedResources, HostProtectionResource demandedResources): base (SecurityException.GetResString("HostProtection_HostProtection"))
        {
            SetErrorCode(__HResults.COR_E_HOSTPROTECTION);
            m_protected = protectedResources;
            m_demanded = demandedResources;
        }

        public HostProtectionResource ProtectedResources
        {
            get
            {
                return m_protected;
            }
        }

        public HostProtectionResource DemandedResources
        {
            get
            {
                return m_demanded;
            }
        }

        private String ToStringHelper(String resourceString, Object attr)
        {
            if (attr == null)
                return String.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append(Environment.GetResourceString(resourceString));
            sb.Append(Environment.NewLine);
            sb.Append(attr);
            return sb.ToString();
        }

        public override String ToString()
        {
            String protectedResStrValue = ToStringHelper("HostProtection_ProtectedResources", ProtectedResources);
            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(protectedResStrValue);
            sb.Append(ToStringHelper("HostProtection_DemandedResources", DemandedResources));
            return sb.ToString();
        }
    }
}