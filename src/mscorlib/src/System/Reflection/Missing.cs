namespace System.Reflection
{
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    public sealed class Missing : ISerializable
    {
        public static readonly Missing Value = new Missing();
        private Missing()
        {
        }
    }
}