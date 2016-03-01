using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

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