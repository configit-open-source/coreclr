namespace System
{
    using System;

    public interface IEquatable<T>
    {
        bool Equals(T other);
    }
}