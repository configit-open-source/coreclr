namespace System.Collections
{
    public interface IList : ICollection
    {
        Object this[int index]
        {
            get;
            set;
        }

        int Add(Object value);
        bool Contains(Object value);
        void Clear();
        bool IsReadOnly
        {
            get;
        }

        bool IsFixedSize
        {
            get;
        }

        int IndexOf(Object value);
        void Insert(int index, Object value);
        void Remove(Object value);
        void RemoveAt(int index);
    }
}