namespace System.Collections
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;

    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }
}