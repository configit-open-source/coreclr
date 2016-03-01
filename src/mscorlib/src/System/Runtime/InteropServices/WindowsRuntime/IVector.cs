using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IVector<T> : IIterable<T>
    {
        T GetAt(uint index);
        uint Size
        {
            get;
        }

        IReadOnlyList<T> GetView();
        bool IndexOf(T value, out uint index);
        void SetAt(uint index, T value);
        void InsertAt(uint index, T value);
        void RemoveAt(uint index);
        void Append(T value);
        void RemoveAtEnd();
        void Clear();
        uint GetMany(uint startIndex, [Out] T[] items);
        void ReplaceAll(T[] items);
    }

    internal interface IVector_Raw<T> : IIterable<T>
    {
        T GetAt(uint index);
        uint Size
        {
            get;
        }

        IVectorView<T> GetView();
        bool IndexOf(T value, out uint index);
        void SetAt(uint index, T value);
        void InsertAt(uint index, T value);
        void RemoveAt(uint index);
        void Append(T value);
        void RemoveAtEnd();
        void Clear();
        uint GetMany(uint startIndex, [Out] T[] items);
        void ReplaceAll(T[] items);
    }

    internal interface IVectorView<T> : IIterable<T>
    {
        T GetAt(uint index);
        uint Size
        {
            get;
        }

        bool IndexOf(T value, out uint index);
        uint GetMany(uint startIndex, [Out] T[] items);
    }

    internal interface IBindableVector : IBindableIterable
    {
        object GetAt(uint index);
        uint Size
        {
            get;
        }

        IBindableVectorView GetView();
        bool IndexOf(object value, out uint index);
        void SetAt(uint index, object value);
        void InsertAt(uint index, object value);
        void RemoveAt(uint index);
        void Append(object value);
        void RemoveAtEnd();
        void Clear();
    }

    internal interface IBindableVectorView : IBindableIterable
    {
        object GetAt(uint index);
        uint Size
        {
            get;
        }

        bool IndexOf(object value, out uint index);
    }
}