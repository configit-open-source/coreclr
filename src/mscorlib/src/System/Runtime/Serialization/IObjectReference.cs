namespace System.Runtime.Serialization
{
    public interface IObjectReference
    {
        Object GetRealObject(StreamingContext context);
    }
}