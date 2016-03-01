namespace System.Runtime.Serialization
{
    using System;
    using System.Security.Permissions;

    public interface IObjectReference
    {
        Object GetRealObject(StreamingContext context);
    }
}