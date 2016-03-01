namespace System
{
    using System;
    using System.Runtime.InteropServices;

    public interface IServiceProvider
    {
        Object GetService(Type serviceType);
    }
}