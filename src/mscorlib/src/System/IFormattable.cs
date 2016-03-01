namespace System
{
    using System;
    using System.Diagnostics.Contracts;

    public interface IFormattable
    {
        String ToString(String format, IFormatProvider formatProvider);
    }
}