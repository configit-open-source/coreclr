using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class VectorToCollectionAdapter
    {
        private VectorToCollectionAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal int Count<T>()
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            uint size = _this.Size;
            if (((uint)Int32.MaxValue) < size)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            return (int)size;
        }

        internal bool IsReadOnly<T>()
        {
            return false;
        }

        internal void Add<T>(T item)
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            _this.Append(item);
        }

        internal void Clear<T>()
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            _this.Clear();
        }

        internal bool Contains<T>(T item)
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            uint index;
            return _this.IndexOf(item, out index);
        }

        internal void CopyTo<T>(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (array.Length <= arrayIndex && Count<T>() > 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_IndexOutOfArrayBounds"));
            if (array.Length - arrayIndex < Count<T>())
                throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));
            Contract.EndContractBlock();
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            int count = Count<T>();
            for (int i = 0; i < count; i++)
            {
                array[i + arrayIndex] = VectorToListAdapter.GetAt<T>(_this, (uint)i);
            }
        }

        internal bool Remove<T>(T item)
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            uint index;
            bool exists = _this.IndexOf(item, out index);
            if (!exists)
                return false;
            if (((uint)Int32.MaxValue) < index)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            VectorToListAdapter.RemoveAtHelper<T>(_this, index);
            return true;
        }
    }
}