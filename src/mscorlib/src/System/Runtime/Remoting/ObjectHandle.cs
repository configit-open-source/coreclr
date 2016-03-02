namespace System.Runtime.Remoting
{
    public class ObjectHandle : IObjectHandle
    {
        private Object WrappedObject;
        private ObjectHandle()
        {
        }

        public ObjectHandle(Object o)
        {
            WrappedObject = o;
        }

        public Object Unwrap()
        {
            return WrappedObject;
        }
    }
}