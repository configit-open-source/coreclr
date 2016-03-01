using System.IO;

namespace System.Runtime.Serialization
{
    public interface IFormatter
    {
        Object Deserialize(Stream serializationStream);
        void Serialize(Stream serializationStream, Object graph);
        ISurrogateSelector SurrogateSelector
        {
            get;
            set;
        }

        SerializationBinder Binder
        {
            get;
            set;
        }

        StreamingContext Context
        {
            get;
            set;
        }
    }
}