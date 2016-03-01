using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class IReadOnlyListToIVectorViewAdapter
    {
        private IReadOnlyListToIVectorViewAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal T GetAt<T>(uint index)
        {
            IReadOnlyList<T> _this = JitHelpers.UnsafeCast<IReadOnlyList<T>>(this);
            EnsureIndexInt32(index, _this.Count);
            try
            {
                return _this[(int)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ex.SetErrorCode(__HResults.E_BOUNDS);
                throw;
            }
        }

        internal uint Size<T>()
        {
            IReadOnlyList<T> _this = JitHelpers.UnsafeCast<IReadOnlyList<T>>(this);
            return (uint)_this.Count;
        }

        internal bool IndexOf<T>(T value, out uint index)
        {
            IReadOnlyList<T> _this = JitHelpers.UnsafeCast<IReadOnlyList<T>>(this);
            int ind = -1;
            int max = _this.Count;
            for (int i = 0; i < max; i++)
            {
                if (EqualityComparer<T>.Default.Equals(value, _this[i]))
                {
                    ind = i;
                    break;
                }
            }

            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }

        internal uint GetMany<T>(uint startIndex, T[] items)
        {
            IReadOnlyList<T> _this = JitHelpers.UnsafeCast<IReadOnlyList<T>>(this);
            if (startIndex == _this.Count)
                return 0;
            EnsureIndexInt32(startIndex, _this.Count);
            if (items == null)
            {
                return 0;
            }

            uint itemCount = Math.Min((uint)items.Length, (uint)_this.Count - startIndex);
            for (uint i = 0; i < itemCount; ++i)
            {
                items[i] = _this[(int)(i + startIndex)];
            }

            if (typeof (T) == typeof (string))
            {
                string[] stringItems = items as string[];
                for (uint i = itemCount; i < items.Length; ++i)
                    stringItems[i] = String.Empty;
            }

            return itemCount;
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
    }
}