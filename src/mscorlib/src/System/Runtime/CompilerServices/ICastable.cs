namespace System.Runtime.CompilerServices
{
    public interface ICastable
    {
        bool IsInstanceOfInterface(RuntimeTypeHandle interfaceType, out Exception castError);
        RuntimeTypeHandle GetImplType(RuntimeTypeHandle interfaceType);
    }
}