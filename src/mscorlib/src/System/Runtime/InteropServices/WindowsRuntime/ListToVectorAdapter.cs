using System;
using System.Security;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class ListToVectorAdapter
    {
        private ListToVectorAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal T GetAt<T>(uint index)
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            EnsureIndexInt32(index, _this.Count);
            try
            {
                return _this[(Int32)index];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(__HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
            }
        }

        internal uint Size<T>()
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            return (uint)_this.Count;
        }

        internal IReadOnlyList<T> GetView<T>()
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            Contract.Assert(_this != null);
            IReadOnlyList<T> roList = _this as IReadOnlyList<T>;
            if (roList == null)
            {
                roList = new ReadOnlyCollection<T>(_this);
            }

            return roList;
        }

        internal bool IndexOf<T>(T value, out uint index)
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            int ind = _this.IndexOf(value);
            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }

        internal void SetAt<T>(uint index, T value)
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            EnsureIndexInt32(index, _this.Count);
            try
            {
                _this[(int)index] = value;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(__HResults.E_BOUNDS, ex, "ArgumentOutOfRange_IndexOutOfRange");
            }
        }

        internal void InsertAt<T>(uint index, T value)
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            EnsureIndexInt32(index, _this.Count + 1);
            try
            {
                _this.Insert((int)index, value);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ex.SetErrorCode(__HResults.E_BOUNDS);
                throw;
            }
        }

        internal void RemoveAt<T>(uint index)
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            EnsureIndexInt32(index, _this.Count);
            try
            {
                _this.RemoveAt((Int32)index);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ex.SetErrorCode(__HResults.E_BOUNDS);
                throw;
            }
        }

        internal void Append<T>(T value)
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            _this.Add(value);
        }

        internal void RemoveAtEnd<T>()
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            if (_this.Count == 0)
            {
                Exception e = new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRemoveLastFromEmptyCollection"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }

            uint size = (uint)_this.Count;
            RemoveAt<T>(size - 1);
        }

        internal void Clear<T>()
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            _this.Clear();
        }

        internal uint GetMany<T>(uint startIndex, T[] items)
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            return GetManyHelper<T>(_this, startIndex, items);
        }

        internal void ReplaceAll<T>(T[] items)
        {
            IList<T> _this = JitHelpers.UnsafeCast<IList<T>>(this);
            _this.Clear();
            if (items != null)
            {
                foreach (T item in items)
                {
                    _this.Add(item);
                }
            }
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

        private static uint GetManyHelper<T>(IList<T> sourceList, uint startIndex, T[] items)
        {
            if (startIndex == sourceList.Count)
            {
                return 0;
            }

            EnsureIndexInt32(startIndex, sourceList.Count);
            if (items == null)
            {
                return 0;
            }

            uint itemCount = Math.Min((uint)items.Length, (uint)sourceList.Count - startIndex);
            for (uint i = 0; i < itemCount; ++i)
            {
                items[i] = sourceList[(int)(i + startIndex)];
            }

            if (typeof (T) == typeof (string))
            {
                string[] stringItems = items as string[];
                for (uint i = itemCount; i < items.Length; ++i)
                    stringItems[i] = String.Empty;
            }

            return itemCount;
        }
    }
}