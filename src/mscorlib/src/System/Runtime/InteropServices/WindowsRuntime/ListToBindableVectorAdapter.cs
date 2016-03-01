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
    internal sealed class ListToBindableVectorAdapter
    {
        private ListToBindableVectorAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal object GetAt(uint index)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
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

        internal uint Size()
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            return (uint)_this.Count;
        }

        internal IBindableVectorView GetView()
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            return new ListToBindableVectorViewAdapter(_this);
        }

        internal bool IndexOf(object value, out uint index)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            int ind = _this.IndexOf(value);
            if (-1 == ind)
            {
                index = 0;
                return false;
            }

            index = (uint)ind;
            return true;
        }

        internal void SetAt(uint index, object value)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
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

        internal void InsertAt(uint index, object value)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
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

        internal void RemoveAt(uint index)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
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

        internal void Append(object value)
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            _this.Add(value);
        }

        internal void RemoveAtEnd()
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            if (_this.Count == 0)
            {
                Exception e = new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CannotRemoveLastFromEmptyCollection"));
                e.SetErrorCode(__HResults.E_BOUNDS);
                throw e;
            }

            uint size = (uint)_this.Count;
            RemoveAt(size - 1);
        }

        internal void Clear()
        {
            IList _this = JitHelpers.UnsafeCast<IList>(this);
            _this.Clear();
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