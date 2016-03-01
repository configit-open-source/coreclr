namespace System.Security.Policy
{
    using System.Runtime.Remoting;
    using System;
    using System.Security.Util;

    public interface IIdentityPermissionFactory
    {
        IPermission CreateIdentityPermission(Evidence evidence);
    }
}