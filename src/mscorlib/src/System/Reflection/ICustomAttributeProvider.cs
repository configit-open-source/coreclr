namespace System.Reflection
{
    public interface ICustomAttributeProvider
    {
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
    }
}