using System.Collections.Generic;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IMap<K, V> : IIterable<IKeyValuePair<K, V>>
    {
        V Lookup(K key);
        uint Size
        {
            get;
        }

        bool HasKey(K key);
        IReadOnlyDictionary<K, V> GetView();
        bool Insert(K key, V value);
        void Remove(K key);
        void Clear();
    }

    internal interface IMapView<K, V> : IIterable<IKeyValuePair<K, V>>
    {
        V Lookup(K key);
        uint Size
        {
            get;
        }

        bool HasKey(K key);
        void Split(out IMapView<K, V> first, out IMapView<K, V> second);
    }

    internal interface IKeyValuePair<K, V>
    {
        K Key
        {
            get;
        }

        V Value
        {
            get;
        }
    }
}