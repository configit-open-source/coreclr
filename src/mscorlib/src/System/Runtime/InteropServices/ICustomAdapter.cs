namespace System.Runtime.InteropServices
{
    using System;

    public interface ICustomAdapter
    {
        Object GetUnderlyingObject();
    }
}