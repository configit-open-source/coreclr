using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class CLRIKeyValuePairImpl<K, V> : IKeyValuePair<K, V>, IGetProxyTarget
    {
        private readonly KeyValuePair<K, V> _pair;
        public CLRIKeyValuePairImpl([In] ref KeyValuePair<K, V> pair)
        {
            _pair = pair;
        }

        public K Key
        {
            get
            {
                return _pair.Key;
            }
        }

        public V Value
        {
            get
            {
                return _pair.Value;
            }
        }

        internal static object BoxHelper(object pair)
        {
            Contract.Requires(pair != null);
            KeyValuePair<K, V> unboxedPair = (KeyValuePair<K, V>)pair;
            return new CLRIKeyValuePairImpl<K, V>(ref unboxedPair);
        }

        internal static object UnboxHelper(object wrapper)
        {
            Contract.Requires(wrapper != null);
            CLRIKeyValuePairImpl<K, V> reference = (CLRIKeyValuePairImpl<K, V>)wrapper;
            return reference._pair;
        }

        public override string ToString()
        {
            return _pair.ToString();
        }

        object IGetProxyTarget.GetTarget()
        {
            return _pair;
        }
    }
}