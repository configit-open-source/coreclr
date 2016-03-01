using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class IReadOnlyDictionaryToIMapViewAdapter
    {
        private IReadOnlyDictionaryToIMapViewAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal V Lookup<K, V>(K key)
        {
            IReadOnlyDictionary<K, V> _this = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
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
            IReadOnlyDictionary<K, V> _this = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
            return (uint)_this.Count;
        }

        internal bool HasKey<K, V>(K key)
        {
            IReadOnlyDictionary<K, V> _this = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
            return _this.ContainsKey(key);
        }

        internal void Split<K, V>(out IMapView<K, V> first, out IMapView<K, V> second)
        {
            IReadOnlyDictionary<K, V> _this = JitHelpers.UnsafeCast<IReadOnlyDictionary<K, V>>(this);
            if (_this.Count < 2)
            {
                first = null;
                second = null;
                return;
            }

            ConstantSplittableMap<K, V> splittableMap = _this as ConstantSplittableMap<K, V>;
            if (splittableMap == null)
                splittableMap = new ConstantSplittableMap<K, V>(_this);
            splittableMap.Split(out first, out second);
        }
    }
}