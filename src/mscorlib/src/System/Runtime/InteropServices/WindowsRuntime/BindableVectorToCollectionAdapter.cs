using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class BindableVectorToCollectionAdapter
    {
        private BindableVectorToCollectionAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal int Count()
        {
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            uint size = _this.Size;
            if (((uint)Int32.MaxValue) < size)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            return (int)size;
        }

        internal bool IsSynchronized()
        {
            return false;
        }

        internal object SyncRoot()
        {
            return this;
        }

        internal void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            int destLB = array.GetLowerBound(0);
            int srcLen = Count();
            int destLen = array.GetLength(0);
            if (arrayIndex < destLB)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (srcLen > (destLen - (arrayIndex - destLB)))
                throw new ArgumentException(Environment.GetResourceString("Argument_InsufficientSpaceToCopyCollection"));
            if (arrayIndex - destLB > destLen)
                throw new ArgumentException(Environment.GetResourceString("Argument_IndexOutOfArrayBounds"));
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            for (uint i = 0; i < srcLen; i++)
            {
                array.SetValue(_this.GetAt(i), i + arrayIndex);
            }
        }
    }
}