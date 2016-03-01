namespace System.Security.Permissions
{
    using System;
    using System.IO;
    using System.Security.Util;
    using System.Text;
    using System.Threading;
    using System.Runtime.Remoting;
    using System.Security;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    [Flags]
    public enum HostProtectionResource
    {
        None = 0x0,
        Synchronization = 0x1,
        SharedState = 0x2,
        ExternalProcessMgmt = 0x4,
        SelfAffectingProcessMgmt = 0x8,
        ExternalThreading = 0x10,
        SelfAffectingThreading = 0x20,
        SecurityInfrastructure = 0x40,
        UI = 0x80,
        MayLeakOnAbort = 0x100,
        All = 0x1ff
    }

    sealed public class HostProtectionAttribute : CodeAccessSecurityAttribute
    {
        private HostProtectionResource m_resources = HostProtectionResource.None;
        public HostProtectionAttribute(): base (SecurityAction.LinkDemand)
        {
        }

        public HostProtectionAttribute(SecurityAction action): base (action)
        {
            if (action != SecurityAction.LinkDemand)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"));
            Contract.EndContractBlock();
        }

        public HostProtectionResource Resources
        {
            get
            {
                return m_resources;
            }

            set
            {
                m_resources = value;
            }
        }

        public bool Synchronization
        {
            get
            {
                return (m_resources & HostProtectionResource.Synchronization) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.Synchronization : m_resources & ~HostProtectionResource.Synchronization);
            }
        }

        public bool SharedState
        {
            get
            {
                return (m_resources & HostProtectionResource.SharedState) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.SharedState : m_resources & ~HostProtectionResource.SharedState);
            }
        }

        public bool ExternalProcessMgmt
        {
            get
            {
                return (m_resources & HostProtectionResource.ExternalProcessMgmt) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.ExternalProcessMgmt : m_resources & ~HostProtectionResource.ExternalProcessMgmt);
            }
        }

        public bool SelfAffectingProcessMgmt
        {
            get
            {
                return (m_resources & HostProtectionResource.SelfAffectingProcessMgmt) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.SelfAffectingProcessMgmt : m_resources & ~HostProtectionResource.SelfAffectingProcessMgmt);
            }
        }

        public bool ExternalThreading
        {
            get
            {
                return (m_resources & HostProtectionResource.ExternalThreading) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.ExternalThreading : m_resources & ~HostProtectionResource.ExternalThreading);
            }
        }

        public bool SelfAffectingThreading
        {
            get
            {
                return (m_resources & HostProtectionResource.SelfAffectingThreading) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.SelfAffectingThreading : m_resources & ~HostProtectionResource.SelfAffectingThreading);
            }
        }

        public bool SecurityInfrastructure
        {
            get
            {
                return (m_resources & HostProtectionResource.SecurityInfrastructure) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.SecurityInfrastructure : m_resources & ~HostProtectionResource.SecurityInfrastructure);
            }
        }

        public bool UI
        {
            get
            {
                return (m_resources & HostProtectionResource.UI) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.UI : m_resources & ~HostProtectionResource.UI);
            }
        }

        public bool MayLeakOnAbort
        {
            get
            {
                return (m_resources & HostProtectionResource.MayLeakOnAbort) != 0;
            }

            set
            {
                m_resources = (value ? m_resources | HostProtectionResource.MayLeakOnAbort : m_resources & ~HostProtectionResource.MayLeakOnAbort);
            }
        }

        public override IPermission CreatePermission()
        {
            if (m_unrestricted)
            {
                return new HostProtectionPermission(PermissionState.Unrestricted);
            }
            else
            {
                return new HostProtectionPermission(m_resources);
            }
        }
    }

    sealed internal class HostProtectionPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        internal static volatile HostProtectionResource protectedResources = HostProtectionResource.None;
        private HostProtectionResource m_resources;
        public HostProtectionPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
                Resources = HostProtectionResource.All;
            else if (state == PermissionState.None)
                Resources = HostProtectionResource.None;
            else
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
        }

        public HostProtectionPermission(HostProtectionResource resources)
        {
            Resources = resources;
        }

        public bool IsUnrestricted()
        {
            return Resources == HostProtectionResource.All;
        }

        public HostProtectionResource Resources
        {
            set
            {
                if (value < HostProtectionResource.None || value > HostProtectionResource.All)
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)value));
                Contract.EndContractBlock();
                m_resources = value;
            }

            get
            {
                return m_resources;
            }
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
                return m_resources == HostProtectionResource.None;
            if (this.GetType() != target.GetType())
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return ((uint)this.m_resources & (uint)((HostProtectionPermission)target).m_resources) == (uint)this.m_resources;
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
                return (this.Copy());
            if (this.GetType() != target.GetType())
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            HostProtectionResource newResources = (HostProtectionResource)((uint)this.m_resources | (uint)((HostProtectionPermission)target).m_resources);
            return new HostProtectionPermission(newResources);
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            if (this.GetType() != target.GetType())
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            HostProtectionResource newResources = (HostProtectionResource)((uint)this.m_resources & (uint)((HostProtectionPermission)target).m_resources);
            if (newResources == HostProtectionResource.None)
                return null;
            return new HostProtectionPermission(newResources);
        }

        public override IPermission Copy()
        {
            return new HostProtectionPermission(m_resources);
        }

        int IBuiltInPermission.GetTokenIndex()
        {
            return HostProtectionPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.HostProtectionPermissionIndex;
        }
    }
}