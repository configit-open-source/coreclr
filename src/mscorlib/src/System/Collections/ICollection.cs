namespace System.Collections
{
    using System;
    using System.Diagnostics.Contracts;

    public interface ICollection : IEnumerable
    {
        void CopyTo(Array array, int index);
        int Count
        {
            get;
        }

        Object SyncRoot
        {
            get;
        }

        bool IsSynchronized
        {
            get;
        }
    }
}