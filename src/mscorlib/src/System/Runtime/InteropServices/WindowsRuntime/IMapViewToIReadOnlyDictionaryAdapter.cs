using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class IMapViewToIReadOnlyDictionaryAdapter
    {
        private IMapViewToIReadOnlyDictionaryAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal V Indexer_Get<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            Contract.EndContractBlock();
            IMapView<K, V> _this = JitHelpers.UnsafeCast<IMapView<K, V>>(this);
            return Lookup(_this, key);
        }

        internal IEnumerable<K> Keys<K, V>()
        {
            IMapView<K, V> _this = JitHelpers.UnsafeCast<IMapView<K, V>>(this);
            IReadOnlyDictionary<K, V> roDictionary = (IReadOnlyDictionary<K, V>)_this;
            return new ReadOnlyDictionaryKeyCollection<K, V>(roDictionary);
        }

        internal IEnumerable<V> Values<K, V>()
        {
            IMapView<K, V> _this = JitHelpers.UnsafeCast<IMapView<K, V>>(this);
            IReadOnlyDictionary<K, V> roDictionary = (IReadOnlyDictionary<K, V>)_this;
            return new ReadOnlyDictionaryValueCollection<K, V>(roDictionary);
        }

        internal bool ContainsKey<K, V>(K key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            IMapView<K, V> _this = JitHelpers.UnsafeCast<IMapView<K, V>>(this);
            return _this.HasKey(key);
        }

        internal bool TryGetValue<K, V>(K key, out V value)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            IMapView<K, V> _this = JitHelpers.UnsafeCast<IMapView<K, V>>(this);
            if (!_this.HasKey(key))
            {
                value = default (V);
                return false;
            }

            try
            {
                value = _this.Lookup(key);
                return true;
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                {
                    value = default (V);
                    return false;
                }

                throw;
            }
        }

        private static V Lookup<K, V>(IMapView<K, V> _this, K key)
        {
            Contract.Requires(null != key);
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
    }

    internal sealed class ReadOnlyDictionaryKeyCollection<TKey, TValue> : IEnumerable<TKey>
    {
        private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
        public ReadOnlyDictionaryKeyCollection(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");
            this.dictionary = dictionary;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TKey>)this).GetEnumerator();
        }

        public IEnumerator<TKey> GetEnumerator()
        {
            return new ReadOnlyDictionaryKeyEnumerator<TKey, TValue>(dictionary);
        }
    }

    internal sealed class ReadOnlyDictionaryKeyEnumerator<TKey, TValue> : IEnumerator<TKey>
    {
        private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
        private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;
        public ReadOnlyDictionaryKeyEnumerator(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");
            this.dictionary = dictionary;
            this.enumeration = dictionary.GetEnumerator();
        }

        void IDisposable.Dispose()
        {
            enumeration.Dispose();
        }

        public bool MoveNext()
        {
            return enumeration.MoveNext();
        }

        Object IEnumerator.Current
        {
            get
            {
                return ((IEnumerator<TKey>)this).Current;
            }
        }

        public TKey Current
        {
            get
            {
                return enumeration.Current.Key;
            }
        }

        public void Reset()
        {
            enumeration = dictionary.GetEnumerator();
        }
    }

    internal sealed class ReadOnlyDictionaryValueCollection<TKey, TValue> : IEnumerable<TValue>
    {
        private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
        public ReadOnlyDictionaryValueCollection(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");
            this.dictionary = dictionary;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TValue>)this).GetEnumerator();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return new ReadOnlyDictionaryValueEnumerator<TKey, TValue>(dictionary);
        }
    }

    internal sealed class ReadOnlyDictionaryValueEnumerator<TKey, TValue> : IEnumerator<TValue>
    {
        private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
        private IEnumerator<KeyValuePair<TKey, TValue>> enumeration;
        public ReadOnlyDictionaryValueEnumerator(IReadOnlyDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");
            this.dictionary = dictionary;
            this.enumeration = dictionary.GetEnumerator();
        }

        void IDisposable.Dispose()
        {
            enumeration.Dispose();
        }

        public bool MoveNext()
        {
            return enumeration.MoveNext();
        }

        Object IEnumerator.Current
        {
            get
            {
                return ((IEnumerator<TValue>)this).Current;
            }
        }

        public TValue Current
        {
            get
            {
                return enumeration.Current.Value;
            }
        }

        public void Reset()
        {
            enumeration = dictionary.GetEnumerator();
        }
    }
}