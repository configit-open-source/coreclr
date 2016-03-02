using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class MapViewToReadOnlyCollectionAdapter
    {
        private MapViewToReadOnlyCollectionAdapter()
        {
                    }

        internal int Count<K, V>()
        {
            object _this = JitHelpers.UnsafeCast<object>(this);
            IMapView<K, V> _this_map = _this as IMapView<K, V>;
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
                IVectorView<KeyValuePair<K, V>> _this_vector = JitHelpers.UnsafeCast<IVectorView<KeyValuePair<K, V>>>(this);
                uint size = _this_vector.Size;
                if (((uint)Int32.MaxValue) < size)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
                }

                return (int)size;
            }
        }
    }
}