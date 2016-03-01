using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class VectorToListAdapter
    {
        private VectorToListAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal T Indexer_Get<T>(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            return GetAt(_this, (uint)index);
        }

        internal void Indexer_Set<T>(int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            SetAt(_this, (uint)index, value);
        }

        internal int IndexOf<T>(T item)
        {
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            uint index;
            bool exists = _this.IndexOf(item, out index);
            if (!exists)
                return -1;
            if (((uint)Int32.MaxValue) < index)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            return (int)index;
        }

        internal void Insert<T>(int index, T item)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            InsertAtHelper<T>(_this, (uint)index, item);
        }

        internal void RemoveAt<T>(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IVector<T> _this = JitHelpers.UnsafeCast<IVector<T>>(this);
            RemoveAtHelper<T>(_this, (uint)index);
        }

        internal static T GetAt<T>(IVector<T> _this, uint index)
        {
            try
            {
                return _this.GetAt(index);
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    throw new ArgumentOutOfRangeException("index");
                throw;
            }
        }

        private static void SetAt<T>(IVector<T> _this, UInt32 index, T value)
        {
            try
            {
                _this.SetAt(index, value);
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    throw new ArgumentOutOfRangeException("index");
                throw;
            }
        }

        private static void InsertAtHelper<T>(IVector<T> _this, uint index, T item)
        {
            try
            {
                _this.InsertAt(index, item);
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    throw new ArgumentOutOfRangeException("index");
                throw;
            }
        }

        internal static void RemoveAtHelper<T>(IVector<T> _this, uint index)
        {
            try
            {
                _this.RemoveAt(index);
            }
            catch (Exception ex)
            {
                if (__HResults.E_BOUNDS == ex._HResult)
                    throw new ArgumentOutOfRangeException("index");
                throw;
            }
        }
    }
}