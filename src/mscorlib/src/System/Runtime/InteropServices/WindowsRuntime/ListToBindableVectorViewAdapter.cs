using System.Collections;
using System.Diagnostics.Contracts;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class ListToBindableVectorViewAdapter : IBindableVectorView
    {
        private readonly IList list;
        internal ListToBindableVectorViewAdapter(IList list)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            Contract.EndContractBlock();
            this.list = list;
        }

        private static void EnsureIndexInt32(uint index, int listCapacity)
        {
            if (((uint)Int32.MaxValue) <= index || index >= (uint)listCapacity)
            {
                Exception e = new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_IndexLargerThanMaxValue"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }
        }

        public IBindableIterator First()
        {
            IEnumerator enumerator = list.GetEnumerator();
            return new EnumeratorToIteratorAdapter<object>(new EnumerableToBindableIterableAdapter.NonGenericToGenericEnumerator(enumerator));
        }

        public object GetAt(uint index)
        {
            EnsureIndexInt32(index, list.Count);
            try
            {
                return list[(int)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(__HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
            }
        }

        public uint Size
        {
            get
            {
                return (uint)list.Count;
            }
        }

        public bool IndexOf(object value, out uint index)
        {
            int ind = list.IndexOf(value);
            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }
    }
}