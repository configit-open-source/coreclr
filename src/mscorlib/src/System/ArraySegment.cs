using System.Collections;
using System.Collections.Generic;

namespace System
{
    public struct ArraySegment<T> : IList<T>, IReadOnlyList<T>
    {
        private T[] _array;
        private int _offset;
        private int _count;
        public ArraySegment(T[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
                        _array = array;
            _offset = 0;
            _count = array.Length;
        }

        public ArraySegment(T[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - offset < count)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
                        _array = array;
            _offset = offset;
            _count = count;
        }

        public T[] Array
        {
            get
            {
                                return _array;
            }
        }

        public int Offset
        {
            get
            {
                                                return _offset;
            }
        }

        public int Count
        {
            get
            {
                                                return _count;
            }
        }

        public override int GetHashCode()
        {
            return null == _array ? 0 : _array.GetHashCode() ^ _offset ^ _count;
        }

        public override bool Equals(Object obj)
        {
            if (obj is ArraySegment<T>)
                return Equals((ArraySegment<T>)obj);
            else
                return false;
        }

        public bool Equals(ArraySegment<T> obj)
        {
            return obj._array == _array && obj._offset == _offset && obj._count == _count;
        }

        public static bool operator ==(ArraySegment<T> a, ArraySegment<T> b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ArraySegment<T> a, ArraySegment<T> b)
        {
            return !(a == b);
        }

        T IList<T>.this[int index]
        {
            get
            {
                if (_array == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException("index");
                                return _array[_offset + index];
            }

            set
            {
                if (_array == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException("index");
                                _array[_offset + index] = value;
            }
        }

        int IList<T>.IndexOf(T item)
        {
            if (_array == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
                        int index = System.Array.IndexOf<T>(_array, item, _offset, _count);
                        return index >= 0 ? index - _offset : -1;
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        T IReadOnlyList<T>.this[int index]
        {
            get
            {
                if (_array == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException("index");
                                return _array[_offset + index];
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            if (_array == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
                        int index = System.Array.IndexOf<T>(_array, item, _offset, _count);
                        return index >= 0;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (_array == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
                        System.Array.Copy(_array, _offset, array, arrayIndex, _count);
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (_array == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
                        return new ArraySegmentEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_array == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NullArray"));
                        return new ArraySegmentEnumerator(this);
        }

        private sealed class ArraySegmentEnumerator : IEnumerator<T>
        {
            private T[] _array;
            private int _start;
            private int _end;
            private int _current;
            internal ArraySegmentEnumerator(ArraySegment<T> arraySegment)
            {
                                                                                _array = arraySegment._array;
                _start = arraySegment._offset;
                _end = _start + arraySegment._count;
                _current = _start - 1;
            }

            public bool MoveNext()
            {
                if (_current < _end)
                {
                    _current++;
                    return (_current < _end);
                }

                return false;
            }

            public T Current
            {
                get
                {
                    if (_current < _start)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (_current >= _end)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    return _array[_current];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _current = _start - 1;
            }

            public void Dispose()
            {
            }
        }
    }
}