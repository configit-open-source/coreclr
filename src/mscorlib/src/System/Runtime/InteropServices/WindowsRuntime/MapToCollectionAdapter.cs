using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class MapToCollectionAdapter
    {
        private MapToCollectionAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal int Count<K, V>()
        {
            object _this = JitHelpers.UnsafeCast<object>(this);
            IMap<K, V> _this_map = _this as IMap<K, V>;
            if (_this_map != null)
            {
                uint size = _this_map.Size;
                if (((uint)Int32.MaxValue) < size)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingDictionaryTooLarge"));
                }

                return (int)size;
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
                uint size = _this_vector.Size;
                if (((uint)Int32.MaxValue) < size)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
                }

                return (int)size;
            }
        }

        internal bool IsReadOnly<K, V>()
        {
            return false;
        }

        internal void Add<K, V>(KeyValuePair<K, V> item)
        {
            object _this = JitHelpers.UnsafeCast<object>(this);
            IDictionary<K, V> _this_dictionary = _this as IDictionary<K, V>;
            if (_this_dictionary != null)
            {
                _this_dictionary.Add(item.Key, item.Value);
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
                _this_vector.Append(item);
            }
        }

        internal void Clear<K, V>()
        {
            object _this = JitHelpers.UnsafeCast<object>(this);
            IMap<K, V> _this_map = _this as IMap<K, V>;
            if (_this_map != null)
            {
                _this_map.Clear();
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
                _this_vector.Clear();
            }
        }

        internal bool Contains<K, V>(KeyValuePair<K, V> item)
        {
            object _this = JitHelpers.UnsafeCast<object>(this);
            IDictionary<K, V> _this_dictionary = _this as IDictionary<K, V>;
            if (_this_dictionary != null)
            {
                V value;
                bool hasKey = _this_dictionary.TryGetValue(item.Key, out value);
                if (!hasKey)
                    return false;
                return EqualityComparer<V>.Default.Equals(value, item.Value);
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
                uint index;
                return _this_vector.IndexOf(item, out index);
            }
        }

        internal void CopyTo<K, V>(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (array.Length <= arrayIndex && Count<K, V>() > 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_IndexOutOfArrayBounds"));
            if (array.Length - arrayIndex < Count<K, V>())
                throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));
            Contract.EndContractBlock();
            IIterable<KeyValuePair<K, V>> _this = JitHelpers.UnsafeCast<IIterable<KeyValuePair<K, V>>>(this);
            foreach (KeyValuePair<K, V> mapping in _this)
            {
                array[arrayIndex++] = mapping;
            }
        }

        internal bool Remove<K, V>(KeyValuePair<K, V> item)
        {
            object _this = JitHelpers.UnsafeCast<object>(this);
            IDictionary<K, V> _this_dictionary = _this as IDictionary<K, V>;
            if (_this_dictionary != null)
            {
                return _this_dictionary.Remove(item.Key);
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = JitHelpers.UnsafeCast<IVector<KeyValuePair<K, V>>>(this);
                uint index;
                bool exists = _this_vector.IndexOf(item, out index);
                if (!exists)
                    return false;
                if (((uint)Int32.MaxValue) < index)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
                }

                VectorToListAdapter.RemoveAtHelper<KeyValuePair<K, V>>(_this_vector, index);
                return true;
            }
        }
    }
}