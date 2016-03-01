using System;
using System.Runtime;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class BindableVectorToListAdapter
    {
        private BindableVectorToListAdapter()
        {
            Contract.Assert(false, "This class is never instantiated");
        }

        internal object Indexer_Get(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            return GetAt(_this, (uint)index);
        }

        internal void Indexer_Set(int index, object value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            SetAt(_this, (uint)index, value);
        }

        internal int Add(object value)
        {
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            _this.Append(value);
            uint size = _this.Size;
            if (((uint)Int32.MaxValue) < size)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
            }

            return (int)(size - 1);
        }

        internal bool Contains(object item)
        {
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            uint index;
            return _this.IndexOf(item, out index);
        }

        internal void Clear()
        {
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            _this.Clear();
        }

        internal bool IsFixedSize()
        {
            return false;
        }

        internal bool IsReadOnly()
        {
            return false;
        }

        internal int IndexOf(object item)
        {
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
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

        internal void Insert(int index, object item)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            InsertAtHelper(_this, (uint)index, item);
        }

        internal void Remove(object item)
        {
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            uint index;
            bool exists = _this.IndexOf(item, out index);
            if (exists)
            {
                if (((uint)Int32.MaxValue) < index)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CollectionBackingListTooLarge"));
                }

                RemoveAtHelper(_this, index);
            }
        }

        internal void RemoveAt(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            IBindableVector _this = JitHelpers.UnsafeCast<IBindableVector>(this);
            RemoveAtHelper(_this, (uint)index);
        }

        private static object GetAt(IBindableVector _this, uint index)
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

        private static void SetAt(IBindableVector _this, uint index, object value)
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

        private static void InsertAtHelper(IBindableVector _this, uint index, object item)
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

        private static void RemoveAtHelper(IBindableVector _this, uint index)
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