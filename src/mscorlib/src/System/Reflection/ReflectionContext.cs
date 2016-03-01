namespace System.Reflection
{
    using System;

    public abstract class ReflectionContext
    {
        protected ReflectionContext()
        {
        }

        public abstract Assembly MapAssembly(Assembly assembly);
        public abstract TypeInfo MapType(TypeInfo type);
        public virtual TypeInfo GetTypeForObject(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            return MapType(value.GetType().GetTypeInfo());
        }
    }
}