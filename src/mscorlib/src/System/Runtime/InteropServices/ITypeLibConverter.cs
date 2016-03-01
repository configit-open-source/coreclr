namespace System.Runtime.InteropServices
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    public enum TypeLibImporterFlags
    {
        None = 0x00000000,
        PrimaryInteropAssembly = 0x00000001,
        UnsafeInterfaces = 0x00000002,
        SafeArrayAsSystemArray = 0x00000004,
        TransformDispRetVals = 0x00000008,
        PreventClassMembers = 0x00000010,
        SerializableValueClasses = 0x00000020,
        ImportAsX86 = 0x00000100,
        ImportAsX64 = 0x00000200,
        ImportAsItanium = 0x00000400,
        ImportAsAgnostic = 0x00000800,
        ReflectionOnlyLoading = 0x00001000,
        NoDefineVersionResource = 0x00002000,
        ImportAsArm = 0x00004000
    }

    public enum TypeLibExporterFlags
    {
        None = 0x00000000,
        OnlyReferenceRegistered = 0x00000001,
        CallerResolvedReferences = 0x00000002,
        OldNames = 0x00000004,
        ExportAs32Bit = 0x00000010,
        ExportAs64Bit = 0x00000020
    }

    public enum ImporterEventKind
    {
        NOTIF_TYPECONVERTED = 0,
        NOTIF_CONVERTWARNING = 1,
        ERROR_REFTOINVALIDTYPELIB = 2
    }

    public enum ExporterEventKind
    {
        NOTIF_TYPECONVERTED = 0,
        NOTIF_CONVERTWARNING = 1,
        ERROR_REFTOINVALIDASSEMBLY = 2
    }

    public interface ITypeLibImporterNotifySink
    {
        void ReportEvent(ImporterEventKind eventKind, int eventCode, String eventMsg);
        Assembly ResolveRef([MarshalAs(UnmanagedType.Interface)] Object typeLib);
    }

    public interface ITypeLibExporterNotifySink
    {
        void ReportEvent(ExporterEventKind eventKind, int eventCode, String eventMsg);
        Object ResolveRef(Assembly assembly);
    }

    public interface ITypeLibConverter
    {
        AssemblyBuilder ConvertTypeLibToAssembly([MarshalAs(UnmanagedType.Interface)] Object typeLib, String asmFileName, TypeLibImporterFlags flags, ITypeLibImporterNotifySink notifySink, byte[] publicKey, StrongNameKeyPair keyPair, String asmNamespace, Version asmVersion);
        Object ConvertAssemblyToTypeLib(Assembly assembly, String typeLibName, TypeLibExporterFlags flags, ITypeLibExporterNotifySink notifySink);
        bool GetPrimaryInteropAssembly(Guid g, Int32 major, Int32 minor, Int32 lcid, out String asmName, out String asmCodeBase);
        AssemblyBuilder ConvertTypeLibToAssembly([MarshalAs(UnmanagedType.Interface)] Object typeLib, String asmFileName, int flags, ITypeLibImporterNotifySink notifySink, byte[] publicKey, StrongNameKeyPair keyPair, bool unsafeInterfaces);
    }

    public interface ITypeLibExporterNameProvider
    {
        String[] GetNames();
    }
}