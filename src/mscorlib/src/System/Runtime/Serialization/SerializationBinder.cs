namespace System.Runtime.Serialization
{
    public abstract class SerializationBinder
    {
        public virtual void BindToName(Type serializedType, out String assemblyName, out String typeName)
        {
            assemblyName = null;
            typeName = null;
        }

        public abstract Type BindToType(String assemblyName, String typeName);
    }
}