namespace System
{
    public interface IFormatProvider
    {
        Object GetFormat(Type formatType);
    }
}