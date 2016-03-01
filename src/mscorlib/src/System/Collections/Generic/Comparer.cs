

namespace System.Collections.Generic
{
    public abstract class Comparer<T> : IComparer, IComparer<T>
    {
        static volatile Comparer<T> defaultComparer;
        public static Comparer<T> Default
        {
            get
            {
                                Comparer<T> comparer = defaultComparer;
                if (comparer == null)
                {
                    comparer = CreateComparer();
                    defaultComparer = comparer;
                }

                return comparer;
            }
        }

        public static Comparer<T> Create(Comparison<T> comparison)
        {
                        if (comparison == null)
                throw new ArgumentNullException("comparison");
            return new ComparisonComparer<T>(comparison);
        }

        private static Comparer<T> CreateComparer()
        {
            RuntimeType t = (RuntimeType)typeof (T);
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                if (t.ImplementInterface(typeof (IComparable<T>)))
                {
                    return (Comparer<T>)RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof (GenericComparer<int>), t);
                }
            }
            else if (typeof (IComparable<T>).IsAssignableFrom(t))
            {
                return (Comparer<T>)RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof (GenericComparer<int>), t);
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof (Nullable<>))
            {
                RuntimeType u = (RuntimeType)t.GetGenericArguments()[0];
                if (typeof (IComparable<>).MakeGenericType(u).IsAssignableFrom(u))
                {
                    return (Comparer<T>)RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof (NullableComparer<int>), u);
                }
            }

            return new ObjectComparer<T>();
        }

        public abstract int Compare(T x, T y);
        int IComparer.Compare(object x, object y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            if (y == null)
                return 1;
            if (x is T && y is T)
                return Compare((T)x, (T)y);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return 0;
        }
    }

    internal class GenericComparer<T> : Comparer<T> where T : IComparable<T>
    {
        public override int Compare(T x, T y)
        {
            if (x != null)
            {
                if (y != null)
                    return x.CompareTo(y);
                return 1;
            }

            if (y != null)
                return -1;
            return 0;
        }

        public override bool Equals(Object obj)
        {
            GenericComparer<T> comparer = obj as GenericComparer<T>;
            return comparer != null;
        }

        public override int GetHashCode()
        {
            return this.GetType().Name.GetHashCode();
        }
    }

    internal class NullableComparer<T> : Comparer<Nullable<T>> where T : struct, IComparable<T>
    {
        public override int Compare(Nullable<T> x, Nullable<T> y)
        {
            if (x.HasValue)
            {
                if (y.HasValue)
                    return x.value.CompareTo(y.value);
                return 1;
            }

            if (y.HasValue)
                return -1;
            return 0;
        }

        public override bool Equals(Object obj)
        {
            NullableComparer<T> comparer = obj as NullableComparer<T>;
            return comparer != null;
        }

        public override int GetHashCode()
        {
            return this.GetType().Name.GetHashCode();
        }
    }

    internal class ObjectComparer<T> : Comparer<T>
    {
        public override int Compare(T x, T y)
        {
            return System.Collections.Comparer.Default.Compare(x, y);
        }

        public override bool Equals(Object obj)
        {
            ObjectComparer<T> comparer = obj as ObjectComparer<T>;
            return comparer != null;
        }

        public override int GetHashCode()
        {
            return this.GetType().Name.GetHashCode();
        }
    }

    internal class ComparisonComparer<T> : Comparer<T>
    {
        private readonly Comparison<T> _comparison;
        public ComparisonComparer(Comparison<T> comparison)
        {
            _comparison = comparison;
        }

        public override int Compare(T x, T y)
        {
            return _comparison(x, y);
        }
    }
}