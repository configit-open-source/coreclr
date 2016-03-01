namespace System.Reflection
{
    public static class IntrospectionExtensions
    {
        public static TypeInfo GetTypeInfo(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var rcType = (IReflectableType)type;
            if (rcType == null)
            {
                return null;
            }
            else
            {
                return rcType.GetTypeInfo();
            }
        }
    }
}