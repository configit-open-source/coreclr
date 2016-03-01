namespace System.Collections.Generic
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    public interface ICollection<T> : IEnumerable<T>
    {
        int Count
        {
            get;
        }

        bool IsReadOnly
        {
            get;
        }

        void Add(T item);
        void Clear();
        bool Contains(T item);
        void CopyTo(T[] array, int arrayIndex);
        bool Remove(T item);
    }
}