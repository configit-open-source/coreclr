namespace System.Runtime.Remoting
{
    using System;
    using System.Runtime.InteropServices;

    public interface IObjectHandle
    {
        Object Unwrap();
    }
}