namespace System.Collections.Generic
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    public interface IList<T> : ICollection<T>
    {
        T this[int index]
        {
            get;
            set;
        }

        int IndexOf(T item);
        void Insert(int index, T item);
        void RemoveAt(int index);
    }
}