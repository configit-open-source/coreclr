namespace System.Security.Principal
{
    using System.Runtime.Remoting;
    using System;
    using System.Security.Util;

    public interface IIdentity
    {
        string Name
        {
            get;
        }

        string AuthenticationType
        {
            get;
        }

        bool IsAuthenticated
        {
            get;
        }
    }
}