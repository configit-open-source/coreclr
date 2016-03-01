using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class MapToDictionaryAdapter
    {
        private MapToDictionaryAdapter()
        {
                    }

        internal V Indexer_Get<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
                        IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            return Lookup(_this, key);
        }

        internal void Indexer_Set<K, V>(K key, V value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
                        IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            Insert(_this, key, value);
        }

        internal ICollection<K> Keys<K, V>()
        {
            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            IDictionary<K, V> dictionary = (IDictionary<K, V>)_this;
            return new DictionaryKeyCollection<K, V>(dictionary);
        }

        internal ICollection<V> Values<K, V>()
        {
            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            IDictionary<K, V> dictionary = (IDictionary<K, V>)_this;
            return new DictionaryValueCollection<K, V>(dictionary);
        }

        internal bool ContainsKey<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            return _this.HasKey(key);
        }

        internal void Add<K, V>(K key, V value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (ContainsKey<K, V>(key))
                throw new ArgumentException(Environment.GetResourceString("Argument_AddingDuplicate"));
                        IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            Insert(_this, key, value);
        }

        internal bool Remove<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            if (!_this.HasKey(key))
                return false;
            try
            {
                _this.Remove(key);
                return true;
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    return false;
                throw;
            }
        }

        internal bool TryGetValue<K, V>(K key, out V value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            IMap<K, V> _this = JitHelpers.UnsafeCast<IMap<K, V>>(this);
            if (!_this.HasKey(key))
            {
                value = default (V);
                return false;
            }

            try
            {
                value = Lookup(_this, key);
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = default (V);
                return false;
            }
        }

        private static V Lookup<K, V>(IMap<K, V> _this, K key)
        {
                        try
            {
                return _this.Lookup(key);
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    throw new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
                throw;
            }
        }

        private static bool Insert<K, V>(IMap<K, V> _this, K key, V value)
        {
                        bool replaced = _this.Insert(key, value);
            return replaced;
        }
    }
}