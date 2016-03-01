using System.Reflection;

namespace System
{
    public sealed class NonSerializedAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeFieldInfo field)
        {
            if ((field.Attributes & FieldAttributes.NotSerialized) == 0)
                return null;
            return new NonSerializedAttribute();
        }

        internal static bool IsDefined(RuntimeFieldInfo field)
        {
            return (field.Attributes & FieldAttributes.NotSerialized) != 0;
        }

        public NonSerializedAttribute()
        {
        }
    }
}