namespace System.Runtime.InteropServices
{
    using System;

    public interface ICustomFactory
    {
        MarshalByRefObject CreateInstance(Type serverType);
    }
}