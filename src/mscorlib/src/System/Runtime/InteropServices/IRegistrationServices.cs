namespace System.Runtime.InteropServices
{
    using System;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;

    public enum AssemblyRegistrationFlags
    {
        None = 0x00000000,
        SetCodeBase = 0x00000001
    }

    public interface IRegistrationServices
    {
        bool RegisterAssembly(Assembly assembly, AssemblyRegistrationFlags flags);
        bool UnregisterAssembly(Assembly assembly);
        Type[] GetRegistrableTypesInAssembly(Assembly assembly);
        String GetProgIdForType(Type type);
        void RegisterTypeForComClients(Type type, ref Guid g);
        Guid GetManagedCategoryGuid();
        bool TypeRequiresRegistration(Type type);
        bool TypeRepresentsComType(Type type);
    }
}