namespace System.Runtime.InteropServices
{
    public sealed class ExtensibleClassFactory
    {
        private ExtensibleClassFactory()
        {
        }

        public static extern void RegisterObjectCreationCallback(ObjectCreationDelegate callback);
    }
}