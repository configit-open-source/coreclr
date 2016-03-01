using System;
using System.Diagnostics.Contracts;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IIterator<T>
    {
        T Current
        {
            get;
        }

        bool HasCurrent
        {
            get;
        }

        bool MoveNext();
        int GetMany([Out] T[] items);
    }

    internal interface IBindableIterator
    {
        object Current
        {
            get;
        }

        bool HasCurrent
        {
            get;
        }

        bool MoveNext();
    }
}