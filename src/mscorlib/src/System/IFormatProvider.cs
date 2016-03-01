namespace System
{
    using System;

    public interface IFormatProvider
    {
        Object GetFormat(Type formatType);
    }
}