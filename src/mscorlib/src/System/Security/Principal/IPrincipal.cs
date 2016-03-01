namespace System.Security.Principal
{
    using System.Runtime.Remoting;
    using System;
    using System.Security.Util;

    public interface IPrincipal
    {
        IIdentity Identity
        {
            get;
        }

        bool IsInRole(string role);
    }
}