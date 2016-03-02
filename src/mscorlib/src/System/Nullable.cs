using System.Collections.Generic;

namespace System
{
    public struct Nullable<T>
        where T : struct
    {
        private bool hasValue;
        internal T value;
        public Nullable(T value)
        {
            this.value = value;
            this.hasValue = true;
        }

        public bool HasValue
        {
            [System.Runtime.Versioning.NonVersionable]
            get
            {
                return hasValue;
            }
        }

        public T Value
        {
            get
            {
                if (!hasValue)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NoValue);
                }

                return value;
            }
        }

        public T GetValueOrDefault()
        {
            return value;
        }

        public T GetValueOrDefault(T defaultValue)
        {
            return hasValue ? value : defaultValue;
        }

        public override bool Equals(object other)
        {
            if (!hasValue)
                return other == null;
            if (other == null)
                return false;
            return value.Equals(other);
        }

        public override int GetHashCode()
        {
            return hasValue ? value.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return hasValue ? value.ToString() : "";
        }

        public static implicit operator Nullable<T>(T value)
        {
            return new Nullable<T>(value);
        }

        public static explicit operator T(Nullable<T> value)
        {
            return value.Value;
        }
    }

    public static class Nullable
    {
        public static int Compare<T>(Nullable<T> n1, Nullable<T> n2)where T : struct
        {
            if (n1.HasValue)
            {
                if (n2.HasValue)
                    return Comparer<T>.Default.Compare(n1.value, n2.value);
                return 1;
            }

            if (n2.HasValue)
                return -1;
            return 0;
        }

        public static bool Equals<T>(Nullable<T> n1, Nullable<T> n2)where T : struct
        {
            if (n1.HasValue)
            {
                if (n2.HasValue)
                    return EqualityComparer<T>.Default.Equals(n1.value, n2.value);
                return false;
            }

            if (n2.HasValue)
                return false;
            return true;
        }

        public static Type GetUnderlyingType(Type nullableType)
        {
            if ((object)nullableType == null)
            {
                throw new ArgumentNullException("nullableType");
            }

                        Type result = null;
            if (nullableType.IsGenericType && !nullableType.IsGenericTypeDefinition)
            {
                Type genericType = nullableType.GetGenericTypeDefinition();
                if (Object.ReferenceEquals(genericType, typeof (Nullable<>)))
                {
                    result = nullableType.GetGenericArguments()[0];
                }
            }

            return result;
        }
    }
}