namespace System.Security.Permissions
{
    using System;

    public interface IUnrestrictedPermission
    {
        bool IsUnrestricted();
    }
}