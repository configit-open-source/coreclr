using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.StubHelpers;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;

[assembly: Guid("BED7F4EA-1A96-11d2-8F08-00A0C9A6186D")]
[assembly: System.Runtime.InteropServices.ComCompatibleVersion(1, 0, 3300, 0)]
[assembly: System.Runtime.InteropServices.TypeLibVersion(2, 4)]
[assembly: DefaultDependencyAttribute(LoadHint.Always)]
[assembly: System.Runtime.CompilerServices.StringFreezingAttribute()]
namespace System
{
    static class Internal
    {
        static void CommonlyUsedGenericInstantiations()
        {
            System.Array.Sort<double>(null);
            System.Array.Sort<int>(null);
            System.Array.Sort<IntPtr>(null);
            new ArraySegment<byte>(new byte[1], 0, 0);
            new Dictionary<Char, Object>();
            new Dictionary<Guid, Byte>();
            new Dictionary<Guid, Object>();
            new Dictionary<Guid, Guid>();
            new Dictionary<Int16, IntPtr>();
            new Dictionary<Int32, Byte>();
            new Dictionary<Int32, Int32>();
            new Dictionary<Int32, Object>();
            new Dictionary<IntPtr, Boolean>();
            new Dictionary<IntPtr, Int16>();
            new Dictionary<Object, Boolean>();
            new Dictionary<Object, Char>();
            new Dictionary<Object, Guid>();
            new Dictionary<Object, Int32>();
            new Dictionary<Object, Int64>();
            new Dictionary<uint, WeakReference>();
            new Dictionary<Object, UInt32>();
            new Dictionary<UInt32, Object>();
            new Dictionary<Int64, Object>();
            new Dictionary<Guid, Int32>();
            new Dictionary<System.Reflection.MemberTypes, Object>();
            new EnumEqualityComparer<System.Reflection.MemberTypes>();
            new Dictionary<Object, KeyValuePair<Object, Object>>();
            new Dictionary<KeyValuePair<Object, Object>, Object>();
            NullableHelper<Boolean>();
            NullableHelper<Byte>();
            NullableHelper<Char>();
            NullableHelper<DateTime>();
            NullableHelper<Decimal>();
            NullableHelper<Double>();
            NullableHelper<Guid>();
            NullableHelper<Int16>();
            NullableHelper<Int32>();
            NullableHelper<Int64>();
            NullableHelper<Single>();
            NullableHelper<TimeSpan>();
            NullableHelper<DateTimeOffset>();
            new List<Boolean>();
            new List<Byte>();
            new List<Char>();
            new List<DateTime>();
            new List<Decimal>();
            new List<Double>();
            new List<Guid>();
            new List<Int16>();
            new List<Int32>();
            new List<Int64>();
            new List<TimeSpan>();
            new List<SByte>();
            new List<Single>();
            new List<UInt16>();
            new List<UInt32>();
            new List<UInt64>();
            new List<IntPtr>();
            new List<KeyValuePair<Object, Object>>();
            new List<GCHandle>();
            new List<DateTimeOffset>();
            new KeyValuePair<Char, UInt16>('\0', UInt16.MinValue);
            new KeyValuePair<UInt16, Double>(UInt16.MinValue, Double.MinValue);
            new KeyValuePair<Object, Int32>(String.Empty, Int32.MinValue);
            new KeyValuePair<Int32, Int32>(Int32.MinValue, Int32.MinValue);
            SZArrayHelper<Boolean>(null);
            SZArrayHelper<Byte>(null);
            SZArrayHelper<DateTime>(null);
            SZArrayHelper<Decimal>(null);
            SZArrayHelper<Double>(null);
            SZArrayHelper<Guid>(null);
            SZArrayHelper<Int16>(null);
            SZArrayHelper<Int32>(null);
            SZArrayHelper<Int64>(null);
            SZArrayHelper<TimeSpan>(null);
            SZArrayHelper<SByte>(null);
            SZArrayHelper<Single>(null);
            SZArrayHelper<UInt16>(null);
            SZArrayHelper<UInt32>(null);
            SZArrayHelper<UInt64>(null);
            SZArrayHelper<DateTimeOffset>(null);
            SZArrayHelper<CustomAttributeTypedArgument>(null);
            SZArrayHelper<CustomAttributeNamedArgument>(null);
            AsyncHelper<int>();
            AsyncHelper2<int>();
            AsyncHelper3();
        }

        static T NullableHelper<T>()where T : struct
        {
            Nullable.Compare<T>(null, null);
            Nullable.Equals<T>(null, null);
            Nullable<T> nullable = new Nullable<T>();
            return nullable.GetValueOrDefault();
        }

        static void SZArrayHelper<T>(SZArrayHelper oSZArrayHelper)
        {
            oSZArrayHelper.get_Count<T>();
            oSZArrayHelper.get_Item<T>(0);
            oSZArrayHelper.GetEnumerator<T>();
        }

        static async void AsyncHelper<T>()
        {
            await Task.Delay(1);
        }

        static async Task<String> AsyncHelper2<T>()
        {
            return await Task.FromResult<string>("");
        }

        static async Task AsyncHelper3()
        {
            await Task.FromResult<string>("");
        }

        static void CommonlyUsedWinRTRedirectedInterfaceStubs()
        {
            WinRT_IEnumerable<byte>(null, null, null);
            WinRT_IEnumerable<char>(null, null, null);
            WinRT_IEnumerable<short>(null, null, null);
            WinRT_IEnumerable<ushort>(null, null, null);
            WinRT_IEnumerable<int>(null, null, null);
            WinRT_IEnumerable<uint>(null, null, null);
            WinRT_IEnumerable<long>(null, null, null);
            WinRT_IEnumerable<ulong>(null, null, null);
            WinRT_IEnumerable<float>(null, null, null);
            WinRT_IEnumerable<double>(null, null, null);
            WinRT_IEnumerable<string>(null, null, null);
            typeof (IIterable<string>).ToString();
            typeof (IIterator<string>).ToString();
            WinRT_IEnumerable<object>(null, null, null);
            typeof (IIterable<object>).ToString();
            typeof (IIterator<object>).ToString();
            WinRT_IList<int>(null, null, null, null);
            WinRT_IList<string>(null, null, null, null);
            typeof (IVector<string>).ToString();
            WinRT_IList<object>(null, null, null, null);
            typeof (IVector<object>).ToString();
            WinRT_IReadOnlyList<int>(null, null, null);
            WinRT_IReadOnlyList<string>(null, null, null);
            typeof (IVectorView<string>).ToString();
            WinRT_IReadOnlyList<object>(null, null, null);
            typeof (IVectorView<object>).ToString();
            WinRT_IDictionary<string, int>(null, null, null, null);
            typeof (IMap<string, int>).ToString();
            WinRT_IDictionary<string, string>(null, null, null, null);
            typeof (IMap<string, string>).ToString();
            WinRT_IDictionary<string, object>(null, null, null, null);
            typeof (IMap<string, object>).ToString();
            WinRT_IDictionary<object, object>(null, null, null, null);
            typeof (IMap<object, object>).ToString();
            WinRT_IReadOnlyDictionary<string, int>(null, null, null, null);
            typeof (IMapView<string, int>).ToString();
            WinRT_IReadOnlyDictionary<string, string>(null, null, null, null);
            typeof (IMapView<string, string>).ToString();
            WinRT_IReadOnlyDictionary<string, object>(null, null, null, null);
            typeof (IMapView<string, object>).ToString();
            WinRT_IReadOnlyDictionary<object, object>(null, null, null, null);
            typeof (IMapView<object, object>).ToString();
            WinRT_Nullable<bool>();
            WinRT_Nullable<byte>();
            WinRT_Nullable<int>();
            WinRT_Nullable<uint>();
            WinRT_Nullable<long>();
            WinRT_Nullable<ulong>();
            WinRT_Nullable<float>();
            WinRT_Nullable<double>();
        }

        static void WinRT_IEnumerable<T>(IterableToEnumerableAdapter iterableToEnumerableAdapter, EnumerableToIterableAdapter enumerableToIterableAdapter, IIterable<T> iterable)
        {
            iterableToEnumerableAdapter.GetEnumerator_Stub<T>();
            enumerableToIterableAdapter.First_Stub<T>();
        }

        static void WinRT_IList<T>(VectorToListAdapter vectorToListAdapter, VectorToCollectionAdapter vectorToCollectionAdapter, ListToVectorAdapter listToVectorAdapter, IVector<T> vector)
        {
            WinRT_IEnumerable<T>(null, null, null);
            vectorToListAdapter.Indexer_Get<T>(0);
            vectorToListAdapter.Indexer_Set<T>(0, default (T));
            vectorToListAdapter.Insert<T>(0, default (T));
            vectorToListAdapter.RemoveAt<T>(0);
            vectorToCollectionAdapter.Count<T>();
            vectorToCollectionAdapter.Add<T>(default (T));
            vectorToCollectionAdapter.Clear<T>();
            listToVectorAdapter.GetAt<T>(0);
            listToVectorAdapter.Size<T>();
            listToVectorAdapter.SetAt<T>(0, default (T));
            listToVectorAdapter.InsertAt<T>(0, default (T));
            listToVectorAdapter.RemoveAt<T>(0);
            listToVectorAdapter.Append<T>(default (T));
            listToVectorAdapter.RemoveAtEnd<T>();
            listToVectorAdapter.Clear<T>();
        }

        static void WinRT_IReadOnlyCollection<T>(VectorViewToReadOnlyCollectionAdapter vectorViewToReadOnlyCollectionAdapter)
        {
            WinRT_IEnumerable<T>(null, null, null);
            vectorViewToReadOnlyCollectionAdapter.Count<T>();
        }

        static void WinRT_IReadOnlyList<T>(IVectorViewToIReadOnlyListAdapter vectorToListAdapter, IReadOnlyListToIVectorViewAdapter listToVectorAdapter, IVectorView<T> vectorView)
        {
            WinRT_IEnumerable<T>(null, null, null);
            WinRT_IReadOnlyCollection<T>(null);
            vectorToListAdapter.Indexer_Get<T>(0);
            listToVectorAdapter.GetAt<T>(0);
            listToVectorAdapter.Size<T>();
        }

        static void WinRT_IDictionary<K, V>(MapToDictionaryAdapter mapToDictionaryAdapter, MapToCollectionAdapter mapToCollectionAdapter, DictionaryToMapAdapter dictionaryToMapAdapter, IMap<K, V> map)
        {
            WinRT_IEnumerable<KeyValuePair<K, V>>(null, null, null);
            V dummy;
            mapToDictionaryAdapter.Indexer_Get<K, V>(default (K));
            mapToDictionaryAdapter.Indexer_Set<K, V>(default (K), default (V));
            mapToDictionaryAdapter.ContainsKey<K, V>(default (K));
            mapToDictionaryAdapter.Add<K, V>(default (K), default (V));
            mapToDictionaryAdapter.Remove<K, V>(default (K));
            mapToDictionaryAdapter.TryGetValue<K, V>(default (K), out dummy);
            mapToCollectionAdapter.Count<K, V>();
            mapToCollectionAdapter.Add<K, V>(new KeyValuePair<K, V>(default (K), default (V)));
            mapToCollectionAdapter.Clear<K, V>();
            dictionaryToMapAdapter.Lookup<K, V>(default (K));
            dictionaryToMapAdapter.Size<K, V>();
            dictionaryToMapAdapter.HasKey<K, V>(default (K));
            dictionaryToMapAdapter.Insert<K, V>(default (K), default (V));
            dictionaryToMapAdapter.Remove<K, V>(default (K));
            dictionaryToMapAdapter.Clear<K, V>();
        }

        static void WinRT_IReadOnlyDictionary<K, V>(IMapViewToIReadOnlyDictionaryAdapter mapToDictionaryAdapter, IReadOnlyDictionaryToIMapViewAdapter dictionaryToMapAdapter, IMapView<K, V> mapView, MapViewToReadOnlyCollectionAdapter mapViewToReadOnlyCollectionAdapter)
        {
            WinRT_IEnumerable<KeyValuePair<K, V>>(null, null, null);
            WinRT_IReadOnlyCollection<KeyValuePair<K, V>>(null);
            V dummy;
            mapToDictionaryAdapter.Indexer_Get<K, V>(default (K));
            mapToDictionaryAdapter.ContainsKey<K, V>(default (K));
            mapToDictionaryAdapter.TryGetValue<K, V>(default (K), out dummy);
            mapViewToReadOnlyCollectionAdapter.Count<K, V>();
            dictionaryToMapAdapter.Lookup<K, V>(default (K));
            dictionaryToMapAdapter.Size<K, V>();
            dictionaryToMapAdapter.HasKey<K, V>(default (K));
        }

        static void WinRT_Nullable<T>()where T : struct
        {
            Nullable<T> nullable = new Nullable<T>();
            NullableMarshaler.ConvertToNative(ref nullable);
            NullableMarshaler.ConvertToManagedRetVoid(IntPtr.Zero, ref nullable);
        }
    }
}