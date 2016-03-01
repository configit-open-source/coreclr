using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class VectorViewToReadOnlyCollectionAdapter
    {
        private VectorViewToReadOnlyCollectionAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal int Count<T>()
        {
            IVectorView<T> _this = JitHelpers.UnsafeCast<IVectorView<T>>(this);
            uint size = _this.Size;
            if (((uint)Int32.MaxValue) < size)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            return (int)size;
        }
    }
}