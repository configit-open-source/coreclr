namespace System
{
    public interface ICustomFormatter
    {
        String Format(String format, Object arg, IFormatProvider formatProvider);
    }
}