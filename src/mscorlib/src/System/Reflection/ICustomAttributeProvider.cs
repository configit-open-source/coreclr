namespace System.Reflection
{
    using System;

    public interface ICustomAttributeProvider
    {
        Object[] GetCustomAttributes(Type attributeType, bool inherit);
        Object[] GetCustomAttributes(bool inherit);
        bool IsDefined(Type attributeType, bool inherit);
    }
}