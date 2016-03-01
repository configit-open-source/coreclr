namespace System.Runtime.Serialization
{
    public interface ISerializationSurrogate
    {
        void GetObjectData(Object obj, SerializationInfo info, StreamingContext context);
        Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector);
    }
}