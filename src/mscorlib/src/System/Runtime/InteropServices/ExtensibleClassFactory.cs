namespace System.Runtime.InteropServices
{
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System;

    public sealed class ExtensibleClassFactory
    {
        private ExtensibleClassFactory()
        {
        }

        public static extern void RegisterObjectCreationCallback(ObjectCreationDelegate callback);
    }
}