namespace System
{
    using System;
    using System.Reflection;

    public sealed class SerializableAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeType type)
        {
            return (type.Attributes & TypeAttributes.Serializable) == TypeAttributes.Serializable ? new SerializableAttribute() : null;
        }

        internal static bool IsDefined(RuntimeType type)
        {
            return type.IsSerializable;
        }

        public SerializableAttribute()
        {
        }
    }
}