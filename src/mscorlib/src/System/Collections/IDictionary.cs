namespace System.Collections
{
    using System;
    using System.Diagnostics.Contracts;

    public interface IDictionary : ICollection
    {
        Object this[Object key]
        {
            get;
            set;
        }

        ICollection Keys
        {
            get;
        }

        ICollection Values
        {
            get;
        }

        bool Contains(Object key);
        void Add(Object key, Object value);
        void Clear();
        bool IsReadOnly
        {
            get;
        }

        bool IsFixedSize
        {
            get;
        }

        new IDictionaryEnumerator GetEnumerator();
        void Remove(Object key);
    }
}