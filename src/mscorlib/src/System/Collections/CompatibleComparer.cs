

namespace System.Collections
{
    internal class CompatibleComparer : IEqualityComparer
    {
        IComparer _comparer;
        IHashCodeProvider _hcp;
        internal CompatibleComparer(IComparer comparer, IHashCodeProvider hashCodeProvider)
        {
            _comparer = comparer;
            _hcp = hashCodeProvider;
        }

        public int Compare(Object a, Object b)
        {
            if (a == b)
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;
            if (_comparer != null)
                return _comparer.Compare(a, b);
            IComparable ia = a as IComparable;
            if (ia != null)
                return ia.CompareTo(b);
            throw new ArgumentException(Environment.GetResourceString("Argument_ImplementIComparable"));
        }

        public new bool Equals(Object a, Object b)
        {
            return Compare(a, b) == 0;
        }

        public int GetHashCode(Object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

                        if (_hcp != null)
                return _hcp.GetHashCode(obj);
            return obj.GetHashCode();
        }

        internal IComparer Comparer
        {
            get
            {
                return _comparer;
            }
        }

        internal IHashCodeProvider HashCodeProvider
        {
            get
            {
                return _hcp;
            }
        }
    }
}