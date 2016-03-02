namespace System
{
    public interface IComparable
    {
        int CompareTo(Object obj);
    }

    public interface IComparable<in T>
    {
        int CompareTo(T other);
    }
}