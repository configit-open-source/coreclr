using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal delegate T Indexer_Get_Delegate<out T>(int index);
    internal sealed class IVectorViewToIReadOnlyListAdapter
    {
        private IVectorViewToIReadOnlyListAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal T Indexer_Get<T>(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IVectorView<T> _this = JitHelpers.UnsafeCast<IVectorView<T>>(this);
            try
            {
                return _this.GetAt((uint)index);
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    throw new ArgumentOutOfRangeException("index");
                throw;
            }
        }

        internal T Indexer_Get_Variance<T>(int index)where T : class
        {
            bool fUseString;
            Delegate target = System.StubHelpers.StubHelpers.GetTargetForAmbiguousVariantCall(this, typeof (IReadOnlyList<T>).TypeHandle.Value, out fUseString);
            if (target != null)
            {
                return (JitHelpers.UnsafeCast<Indexer_Get_Delegate<T>>(target))(index);
            }

            if (fUseString)
            {
                return JitHelpers.UnsafeCast<T>(Indexer_Get<string>(index));
            }

            return Indexer_Get<T>(index);
        }
    }
}