using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IIterable<T> : IEnumerable<T>
    {
        IIterator<T> First();
    }

    internal interface IBindableIterable
    {
        IBindableIterator First();
    }
}