namespace System.Runtime.Serialization
{
    using System.Runtime.Remoting;
    using System.Security.Permissions;
    using System;

    public interface ISurrogateSelector
    {
        void ChainSelector(ISurrogateSelector selector);
        ISerializationSurrogate GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector);
        ISurrogateSelector GetNextSelector();
    }
}