using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class DictionaryToMapAdapter
    {
        private DictionaryToMapAdapter()
        {
                    }

        internal V Lookup<K, V>(K key)
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            V value;
            bool keyFound = _this.TryGetValue(key, out value);
            if (!keyFound)
            {
                Exception e = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }

            return value;
        }

        internal uint Size<K, V>()
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            return (uint)_this.Count;
        }

        internal bool HasKey<K, V>(K key)
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            return _this.ContainsKey(key);
        }

        internal IReadOnlyDictionary<K, V> GetView<K, V>()
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
                        IReadOnlyDictionary<K, V> roDictionary = _this as IReadOnlyDictionary<K, V>;
            if (roDictionary == null)
            {
                roDictionary = new ReadOnlyDictionary<K, V>(_this);
            }

            return roDictionary;
        }

        internal bool Insert<K, V>(K key, V value)
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            bool replacing = _this.ContainsKey(key);
            _this[key] = value;
            return replacing;
        }

        internal void Remove<K, V>(K key)
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            bool removed = _this.Remove(key);
            if (!removed)
            {
                Exception e = new KeyNotFoundException(Environment.GetResourceString("Arg_KeyNotFound"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }
        }

        internal void Clear<K, V>()
        {
            IDictionary<K, V> _this = JitHelpers.UnsafeCast<IDictionary<K, V>>(this);
            _this.Clear();
        }
    }
}