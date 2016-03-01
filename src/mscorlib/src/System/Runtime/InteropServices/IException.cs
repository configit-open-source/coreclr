namespace System.Runtime.InteropServices
{
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    public interface _Exception
    {
        Exception InnerException
        {
            get;
        }
    }
}