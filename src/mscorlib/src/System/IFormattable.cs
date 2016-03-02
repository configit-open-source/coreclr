namespace System
{
    public interface IFormattable
    {
        String ToString(String format, IFormatProvider formatProvider);
    }
}