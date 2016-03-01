namespace System.Runtime.Serialization
{
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System;
    using System.Reflection;

    public interface ISerializationSurrogate
    {
        void GetObjectData(Object obj, SerializationInfo info, StreamingContext context);
        Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector);
    }
}