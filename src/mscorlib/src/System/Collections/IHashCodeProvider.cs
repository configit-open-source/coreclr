namespace System.Collections
{
    using System;

    public interface IHashCodeProvider
    {
        int GetHashCode(Object obj);
    }
}