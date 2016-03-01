namespace System
{
    using System;
    using System.Runtime.Serialization;

    public interface ICustomFormatter
    {
        String Format(String format, Object arg, IFormatProvider formatProvider);
    }
}