namespace System
{
    public interface IServiceProvider
    {
        Object GetService(Type serviceType);
    }
}