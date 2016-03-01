using System.Collections;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;

using Microsoft.Win32;

namespace System.Runtime.InteropServices
{
    [Flags]
    public enum RegistrationClassContext
    {
        InProcessServer = 0x1,
        InProcessHandler = 0x2,
        LocalServer = 0x4,
        InProcessServer16 = 0x8,
        RemoteServer = 0x10,
        InProcessHandler16 = 0x20,
        Reserved1 = 0x40,
        Reserved2 = 0x80,
        Reserved3 = 0x100,
        Reserved4 = 0x200,
        NoCodeDownload = 0x400,
        Reserved5 = 0x800,
        NoCustomMarshal = 0x1000,
        EnableCodeDownload = 0x2000,
        NoFailureLog = 0x4000,
        DisableActivateAsActivator = 0x8000,
        EnableActivateAsActivator = 0x10000,
        FromDefaultContext = 0x20000
    }

    [Flags]
    public enum RegistrationConnectionType
    {
        SingleUse = 0,
        MultipleUse = 1,
        MultiSeparate = 2,
        Suspended = 4,
        Surrogate = 8
    }

    public class RegistrationServices : IRegistrationServices
    {
        private const String strManagedCategoryGuid = "{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}";
        private const String strDocStringPrefix = "";
        private const String strManagedTypeThreadingModel = "Both";
        private const String strComponentCategorySubKey = "Component Categories";
        private const String strManagedCategoryDescription = ".NET Category";
        private const String strImplementedCategoriesSubKey = "Implemented Categories";
        private const String strMsCorEEFileName = "mscoree.dll";
        private const String strRecordRootName = "Record";
        private const String strClsIdRootName = "CLSID";
        private const String strTlbRootName = "TypeLib";
        private static Guid s_ManagedCategoryGuid = new Guid(strManagedCategoryGuid);
        public virtual bool RegisterAssembly(Assembly assembly, AssemblyRegistrationFlags flags)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            if (assembly.ReflectionOnly)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsmLoadedForReflectionOnly"));
            Contract.EndContractBlock();
            RuntimeAssembly rtAssembly = assembly as RuntimeAssembly;
            if (rtAssembly == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
            String strAsmName = assembly.FullName;
            if (strAsmName == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoAsmName"));
            String strAsmCodeBase = null;
            if ((flags & AssemblyRegistrationFlags.SetCodeBase) != 0)
            {
                strAsmCodeBase = rtAssembly.GetCodeBase(false);
                if (strAsmCodeBase == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoAsmCodeBase"));
            }

            Type[] aTypes = GetRegistrableTypesInAssembly(assembly);
            int NumTypes = aTypes.Length;
            String strAsmVersion = rtAssembly.GetVersion().ToString();
            String strRuntimeVersion = assembly.ImageRuntimeVersion;
            for (int cTypes = 0; cTypes < NumTypes; cTypes++)
            {
                if (IsRegisteredAsValueType(aTypes[cTypes]))
                    RegisterValueType(aTypes[cTypes], strAsmName, strAsmVersion, strAsmCodeBase, strRuntimeVersion);
                else if (TypeRepresentsComType(aTypes[cTypes]))
                    RegisterComImportedType(aTypes[cTypes], strAsmName, strAsmVersion, strAsmCodeBase, strRuntimeVersion);
                else
                    RegisterManagedType(aTypes[cTypes], strAsmName, strAsmVersion, strAsmCodeBase, strRuntimeVersion);
                CallUserDefinedRegistrationMethod(aTypes[cTypes], true);
            }

            Object[] aPIAAttrs = assembly.GetCustomAttributes(typeof (PrimaryInteropAssemblyAttribute), false);
            int NumPIAAttrs = aPIAAttrs.Length;
            for (int cPIAAttrs = 0; cPIAAttrs < NumPIAAttrs; cPIAAttrs++)
                RegisterPrimaryInteropAssembly(rtAssembly, strAsmCodeBase, (PrimaryInteropAssemblyAttribute)aPIAAttrs[cPIAAttrs]);
            if (aTypes.Length > 0 || NumPIAAttrs > 0)
                return true;
            else
                return false;
        }

        public virtual bool UnregisterAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            if (assembly.ReflectionOnly)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_AsmLoadedForReflectionOnly"));
            Contract.EndContractBlock();
            RuntimeAssembly rtAssembly = assembly as RuntimeAssembly;
            if (rtAssembly == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
            bool bAllVersionsGone = true;
            Type[] aTypes = GetRegistrableTypesInAssembly(assembly);
            int NumTypes = aTypes.Length;
            String strAsmVersion = rtAssembly.GetVersion().ToString();
            for (int cTypes = 0; cTypes < NumTypes; cTypes++)
            {
                CallUserDefinedRegistrationMethod(aTypes[cTypes], false);
                if (IsRegisteredAsValueType(aTypes[cTypes]))
                {
                    if (!UnregisterValueType(aTypes[cTypes], strAsmVersion))
                        bAllVersionsGone = false;
                }
                else if (TypeRepresentsComType(aTypes[cTypes]))
                {
                    if (!UnregisterComImportedType(aTypes[cTypes], strAsmVersion))
                        bAllVersionsGone = false;
                }
                else
                {
                    if (!UnregisterManagedType(aTypes[cTypes], strAsmVersion))
                        bAllVersionsGone = false;
                }
            }

            Object[] aPIAAttrs = assembly.GetCustomAttributes(typeof (PrimaryInteropAssemblyAttribute), false);
            int NumPIAAttrs = aPIAAttrs.Length;
            if (bAllVersionsGone)
            {
                for (int cPIAAttrs = 0; cPIAAttrs < NumPIAAttrs; cPIAAttrs++)
                    UnregisterPrimaryInteropAssembly(assembly, (PrimaryInteropAssemblyAttribute)aPIAAttrs[cPIAAttrs]);
            }

            if (aTypes.Length > 0 || NumPIAAttrs > 0)
                return true;
            else
                return false;
        }

        public virtual Type[] GetRegistrableTypesInAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            Contract.EndContractBlock();
            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"), "assembly");
            Type[] aTypes = assembly.GetExportedTypes();
            int NumTypes = aTypes.Length;
            ArrayList TypeList = new ArrayList();
            for (int cTypes = 0; cTypes < NumTypes; cTypes++)
            {
                Type CurrentType = aTypes[cTypes];
                if (TypeRequiresRegistration(CurrentType))
                    TypeList.Add(CurrentType);
            }

            Type[] RetArray = new Type[TypeList.Count];
            TypeList.CopyTo(RetArray);
            return RetArray;
        }

        public virtual String GetProgIdForType(Type type)
        {
            return Marshal.GenerateProgIdForType(type);
        }

        public virtual void RegisterTypeForComClients(Type type, ref Guid g)
        {
            throw new NotImplementedException("CoreCLR_REMOVED -- managed activation removed");
        }

        public virtual Guid GetManagedCategoryGuid()
        {
            return s_ManagedCategoryGuid;
        }

        public virtual bool TypeRequiresRegistration(Type type)
        {
            return TypeRequiresRegistrationHelper(type);
        }

        public virtual bool TypeRepresentsComType(Type type)
        {
            if (!type.IsCOMObject)
                return false;
            if (type.IsImport)
                return true;
            Type baseComImportType = GetBaseComImportType(type);
            Contract.Assert(baseComImportType != null, "baseComImportType != null");
            if (Marshal.GenerateGuidForType(type) == Marshal.GenerateGuidForType(baseComImportType))
                return true;
            return false;
        }

        public virtual int RegisterTypeForComClients(Type type, RegistrationClassContext classContext, RegistrationConnectionType flags)
        {
            throw new NotImplementedException("CoreCLR_REMOVED -- managed activation removed");
        }

        public virtual void UnregisterTypeForComClients(int cookie)
        {
            CoRevokeClassObject(cookie);
        }

        internal static bool TypeRequiresRegistrationHelper(Type type)
        {
            if (!type.IsClass && !type.IsValueType)
                return false;
            if (type.IsAbstract)
                return false;
            if (!type.IsValueType && type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Array.Empty<Type>(), null) == null)
                return false;
            return Marshal.IsTypeVisibleFromCom(type);
        }

        private void RegisterValueType(Type type, String strAsmName, String strAsmVersion, String strAsmCodeBase, String strRuntimeVersion)
        {
            String strRecordId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            using (RegistryKey RecordRootKey = Registry.ClassesRoot.CreateSubKey(strRecordRootName))
            {
                using (RegistryKey RecordKey = RecordRootKey.CreateSubKey(strRecordId))
                {
                    using (RegistryKey RecordVersionKey = RecordKey.CreateSubKey(strAsmVersion))
                    {
                        RecordVersionKey.SetValue("Class", type.FullName);
                        RecordVersionKey.SetValue("Assembly", strAsmName);
                        RecordVersionKey.SetValue("RuntimeVersion", strRuntimeVersion);
                        if (strAsmCodeBase != null)
                            RecordVersionKey.SetValue("CodeBase", strAsmCodeBase);
                    }
                }
            }
        }

        private void RegisterManagedType(Type type, String strAsmName, String strAsmVersion, String strAsmCodeBase, String strRuntimeVersion)
        {
            String strDocString = strDocStringPrefix + type.FullName;
            String strClsId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strProgId = GetProgIdForType(type);
            if (strProgId != String.Empty)
            {
                using (RegistryKey TypeNameKey = Registry.ClassesRoot.CreateSubKey(strProgId))
                {
                    TypeNameKey.SetValue("", strDocString);
                    using (RegistryKey ProgIdClsIdKey = TypeNameKey.CreateSubKey("CLSID"))
                    {
                        ProgIdClsIdKey.SetValue("", strClsId);
                    }
                }
            }

            using (RegistryKey ClsIdRootKey = Registry.ClassesRoot.CreateSubKey(strClsIdRootName))
            {
                using (RegistryKey ClsIdKey = ClsIdRootKey.CreateSubKey(strClsId))
                {
                    ClsIdKey.SetValue("", strDocString);
                    using (RegistryKey InProcServerKey = ClsIdKey.CreateSubKey("InprocServer32"))
                    {
                        InProcServerKey.SetValue("", strMsCorEEFileName);
                        InProcServerKey.SetValue("ThreadingModel", strManagedTypeThreadingModel);
                        InProcServerKey.SetValue("Class", type.FullName);
                        InProcServerKey.SetValue("Assembly", strAsmName);
                        InProcServerKey.SetValue("RuntimeVersion", strRuntimeVersion);
                        if (strAsmCodeBase != null)
                            InProcServerKey.SetValue("CodeBase", strAsmCodeBase);
                        using (RegistryKey VersionSubKey = InProcServerKey.CreateSubKey(strAsmVersion))
                        {
                            VersionSubKey.SetValue("Class", type.FullName);
                            VersionSubKey.SetValue("Assembly", strAsmName);
                            VersionSubKey.SetValue("RuntimeVersion", strRuntimeVersion);
                            if (strAsmCodeBase != null)
                                VersionSubKey.SetValue("CodeBase", strAsmCodeBase);
                        }

                        if (strProgId != String.Empty)
                        {
                            using (RegistryKey ProgIdKey = ClsIdKey.CreateSubKey("ProgId"))
                            {
                                ProgIdKey.SetValue("", strProgId);
                            }
                        }
                    }

                    using (RegistryKey CategoryKey = ClsIdKey.CreateSubKey(strImplementedCategoriesSubKey))
                    {
                        using (RegistryKey ManagedCategoryKey = CategoryKey.CreateSubKey(strManagedCategoryGuid))
                        {
                        }
                    }
                }
            }

            EnsureManagedCategoryExists();
        }

        private void RegisterComImportedType(Type type, String strAsmName, String strAsmVersion, String strAsmCodeBase, String strRuntimeVersion)
        {
            String strClsId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            using (RegistryKey ClsIdRootKey = Registry.ClassesRoot.CreateSubKey(strClsIdRootName))
            {
                using (RegistryKey ClsIdKey = ClsIdRootKey.CreateSubKey(strClsId))
                {
                    using (RegistryKey InProcServerKey = ClsIdKey.CreateSubKey("InprocServer32"))
                    {
                        InProcServerKey.SetValue("Class", type.FullName);
                        InProcServerKey.SetValue("Assembly", strAsmName);
                        InProcServerKey.SetValue("RuntimeVersion", strRuntimeVersion);
                        if (strAsmCodeBase != null)
                            InProcServerKey.SetValue("CodeBase", strAsmCodeBase);
                        using (RegistryKey VersionSubKey = InProcServerKey.CreateSubKey(strAsmVersion))
                        {
                            VersionSubKey.SetValue("Class", type.FullName);
                            VersionSubKey.SetValue("Assembly", strAsmName);
                            VersionSubKey.SetValue("RuntimeVersion", strRuntimeVersion);
                            if (strAsmCodeBase != null)
                                VersionSubKey.SetValue("CodeBase", strAsmCodeBase);
                        }
                    }
                }
            }
        }

        private bool UnregisterValueType(Type type, String strAsmVersion)
        {
            bool bAllVersionsGone = true;
            String strRecordId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            using (RegistryKey RecordRootKey = Registry.ClassesRoot.OpenSubKey(strRecordRootName, true))
            {
                if (RecordRootKey != null)
                {
                    using (RegistryKey RecordKey = RecordRootKey.OpenSubKey(strRecordId, true))
                    {
                        if (RecordKey != null)
                        {
                            using (RegistryKey VersionSubKey = RecordKey.OpenSubKey(strAsmVersion, true))
                            {
                                if (VersionSubKey != null)
                                {
                                    VersionSubKey.DeleteValue("Assembly", false);
                                    VersionSubKey.DeleteValue("Class", false);
                                    VersionSubKey.DeleteValue("CodeBase", false);
                                    VersionSubKey.DeleteValue("RuntimeVersion", false);
                                    if ((VersionSubKey.SubKeyCount == 0) && (VersionSubKey.ValueCount == 0))
                                        RecordKey.DeleteSubKey(strAsmVersion);
                                }
                            }

                            if (RecordKey.SubKeyCount != 0)
                                bAllVersionsGone = false;
                            if ((RecordKey.SubKeyCount == 0) && (RecordKey.ValueCount == 0))
                                RecordRootKey.DeleteSubKey(strRecordId);
                        }
                    }

                    if ((RecordRootKey.SubKeyCount == 0) && (RecordRootKey.ValueCount == 0))
                        Registry.ClassesRoot.DeleteSubKey(strRecordRootName);
                }
            }

            return bAllVersionsGone;
        }

        private bool UnregisterManagedType(Type type, String strAsmVersion)
        {
            bool bAllVersionsGone = true;
            String strClsId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strProgId = GetProgIdForType(type);
            using (RegistryKey ClsIdRootKey = Registry.ClassesRoot.OpenSubKey(strClsIdRootName, true))
            {
                if (ClsIdRootKey != null)
                {
                    using (RegistryKey ClsIdKey = ClsIdRootKey.OpenSubKey(strClsId, true))
                    {
                        if (ClsIdKey != null)
                        {
                            using (RegistryKey InProcServerKey = ClsIdKey.OpenSubKey("InprocServer32", true))
                            {
                                if (InProcServerKey != null)
                                {
                                    using (RegistryKey VersionSubKey = InProcServerKey.OpenSubKey(strAsmVersion, true))
                                    {
                                        if (VersionSubKey != null)
                                        {
                                            VersionSubKey.DeleteValue("Assembly", false);
                                            VersionSubKey.DeleteValue("Class", false);
                                            VersionSubKey.DeleteValue("RuntimeVersion", false);
                                            VersionSubKey.DeleteValue("CodeBase", false);
                                            if ((VersionSubKey.SubKeyCount == 0) && (VersionSubKey.ValueCount == 0))
                                                InProcServerKey.DeleteSubKey(strAsmVersion);
                                        }
                                    }

                                    if (InProcServerKey.SubKeyCount != 0)
                                        bAllVersionsGone = false;
                                    if (bAllVersionsGone)
                                    {
                                        InProcServerKey.DeleteValue("", false);
                                        InProcServerKey.DeleteValue("ThreadingModel", false);
                                    }

                                    InProcServerKey.DeleteValue("Assembly", false);
                                    InProcServerKey.DeleteValue("Class", false);
                                    InProcServerKey.DeleteValue("RuntimeVersion", false);
                                    InProcServerKey.DeleteValue("CodeBase", false);
                                    if ((InProcServerKey.SubKeyCount == 0) && (InProcServerKey.ValueCount == 0))
                                        ClsIdKey.DeleteSubKey("InprocServer32");
                                }
                            }

                            if (bAllVersionsGone)
                            {
                                ClsIdKey.DeleteValue("", false);
                                if (strProgId != String.Empty)
                                {
                                    using (RegistryKey ProgIdKey = ClsIdKey.OpenSubKey("ProgId", true))
                                    {
                                        if (ProgIdKey != null)
                                        {
                                            ProgIdKey.DeleteValue("", false);
                                            if ((ProgIdKey.SubKeyCount == 0) && (ProgIdKey.ValueCount == 0))
                                                ClsIdKey.DeleteSubKey("ProgId");
                                        }
                                    }
                                }

                                using (RegistryKey CategoryKey = ClsIdKey.OpenSubKey(strImplementedCategoriesSubKey, true))
                                {
                                    if (CategoryKey != null)
                                    {
                                        using (RegistryKey ManagedCategoryKey = CategoryKey.OpenSubKey(strManagedCategoryGuid, true))
                                        {
                                            if (ManagedCategoryKey != null)
                                            {
                                                if ((ManagedCategoryKey.SubKeyCount == 0) && (ManagedCategoryKey.ValueCount == 0))
                                                    CategoryKey.DeleteSubKey(strManagedCategoryGuid);
                                            }
                                        }

                                        if ((CategoryKey.SubKeyCount == 0) && (CategoryKey.ValueCount == 0))
                                            ClsIdKey.DeleteSubKey(strImplementedCategoriesSubKey);
                                    }
                                }
                            }

                            if ((ClsIdKey.SubKeyCount == 0) && (ClsIdKey.ValueCount == 0))
                                ClsIdRootKey.DeleteSubKey(strClsId);
                        }
                    }

                    if ((ClsIdRootKey.SubKeyCount == 0) && (ClsIdRootKey.ValueCount == 0))
                        Registry.ClassesRoot.DeleteSubKey(strClsIdRootName);
                }

                if (bAllVersionsGone)
                {
                    if (strProgId != String.Empty)
                    {
                        using (RegistryKey TypeNameKey = Registry.ClassesRoot.OpenSubKey(strProgId, true))
                        {
                            if (TypeNameKey != null)
                            {
                                TypeNameKey.DeleteValue("", false);
                                using (RegistryKey ProgIdClsIdKey = TypeNameKey.OpenSubKey("CLSID", true))
                                {
                                    if (ProgIdClsIdKey != null)
                                    {
                                        ProgIdClsIdKey.DeleteValue("", false);
                                        if ((ProgIdClsIdKey.SubKeyCount == 0) && (ProgIdClsIdKey.ValueCount == 0))
                                            TypeNameKey.DeleteSubKey("CLSID");
                                    }
                                }

                                if ((TypeNameKey.SubKeyCount == 0) && (TypeNameKey.ValueCount == 0))
                                    Registry.ClassesRoot.DeleteSubKey(strProgId);
                            }
                        }
                    }
                }
            }

            return bAllVersionsGone;
        }

        private bool UnregisterComImportedType(Type type, String strAsmVersion)
        {
            bool bAllVersionsGone = true;
            String strClsId = "{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            using (RegistryKey ClsIdRootKey = Registry.ClassesRoot.OpenSubKey(strClsIdRootName, true))
            {
                if (ClsIdRootKey != null)
                {
                    using (RegistryKey ClsIdKey = ClsIdRootKey.OpenSubKey(strClsId, true))
                    {
                        if (ClsIdKey != null)
                        {
                            using (RegistryKey InProcServerKey = ClsIdKey.OpenSubKey("InprocServer32", true))
                            {
                                if (InProcServerKey != null)
                                {
                                    InProcServerKey.DeleteValue("Assembly", false);
                                    InProcServerKey.DeleteValue("Class", false);
                                    InProcServerKey.DeleteValue("RuntimeVersion", false);
                                    InProcServerKey.DeleteValue("CodeBase", false);
                                    using (RegistryKey VersionSubKey = InProcServerKey.OpenSubKey(strAsmVersion, true))
                                    {
                                        if (VersionSubKey != null)
                                        {
                                            VersionSubKey.DeleteValue("Assembly", false);
                                            VersionSubKey.DeleteValue("Class", false);
                                            VersionSubKey.DeleteValue("RuntimeVersion", false);
                                            VersionSubKey.DeleteValue("CodeBase", false);
                                            if ((VersionSubKey.SubKeyCount == 0) && (VersionSubKey.ValueCount == 0))
                                                InProcServerKey.DeleteSubKey(strAsmVersion);
                                        }
                                    }

                                    if (InProcServerKey.SubKeyCount != 0)
                                        bAllVersionsGone = false;
                                    if ((InProcServerKey.SubKeyCount == 0) && (InProcServerKey.ValueCount == 0))
                                        ClsIdKey.DeleteSubKey("InprocServer32");
                                }
                            }

                            if ((ClsIdKey.SubKeyCount == 0) && (ClsIdKey.ValueCount == 0))
                                ClsIdRootKey.DeleteSubKey(strClsId);
                        }
                    }

                    if ((ClsIdRootKey.SubKeyCount == 0) && (ClsIdRootKey.ValueCount == 0))
                        Registry.ClassesRoot.DeleteSubKey(strClsIdRootName);
                }
            }

            return bAllVersionsGone;
        }

        private void RegisterPrimaryInteropAssembly(RuntimeAssembly assembly, String strAsmCodeBase, PrimaryInteropAssemblyAttribute attr)
        {
            if (assembly.GetPublicKey().Length == 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_PIAMustBeStrongNamed"));
            String strTlbId = "{" + Marshal.GetTypeLibGuidForAssembly(assembly).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strVersion = attr.MajorVersion.ToString("x", CultureInfo.InvariantCulture) + "." + attr.MinorVersion.ToString("x", CultureInfo.InvariantCulture);
            using (RegistryKey TypeLibRootKey = Registry.ClassesRoot.CreateSubKey(strTlbRootName))
            {
                using (RegistryKey TypeLibKey = TypeLibRootKey.CreateSubKey(strTlbId))
                {
                    using (RegistryKey VersionSubKey = TypeLibKey.CreateSubKey(strVersion))
                    {
                        VersionSubKey.SetValue("PrimaryInteropAssemblyName", assembly.FullName);
                        if (strAsmCodeBase != null)
                            VersionSubKey.SetValue("PrimaryInteropAssemblyCodeBase", strAsmCodeBase);
                    }
                }
            }
        }

        private void UnregisterPrimaryInteropAssembly(Assembly assembly, PrimaryInteropAssemblyAttribute attr)
        {
            String strTlbId = "{" + Marshal.GetTypeLibGuidForAssembly(assembly).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
            String strVersion = attr.MajorVersion.ToString("x", CultureInfo.InvariantCulture) + "." + attr.MinorVersion.ToString("x", CultureInfo.InvariantCulture);
            using (RegistryKey TypeLibRootKey = Registry.ClassesRoot.OpenSubKey(strTlbRootName, true))
            {
                if (TypeLibRootKey != null)
                {
                    using (RegistryKey TypeLibKey = TypeLibRootKey.OpenSubKey(strTlbId, true))
                    {
                        if (TypeLibKey != null)
                        {
                            using (RegistryKey VersionSubKey = TypeLibKey.OpenSubKey(strVersion, true))
                            {
                                if (VersionSubKey != null)
                                {
                                    VersionSubKey.DeleteValue("PrimaryInteropAssemblyName", false);
                                    VersionSubKey.DeleteValue("PrimaryInteropAssemblyCodeBase", false);
                                    if ((VersionSubKey.SubKeyCount == 0) && (VersionSubKey.ValueCount == 0))
                                        TypeLibKey.DeleteSubKey(strVersion);
                                }
                            }

                            if ((TypeLibKey.SubKeyCount == 0) && (TypeLibKey.ValueCount == 0))
                                TypeLibRootKey.DeleteSubKey(strTlbId);
                        }
                    }

                    if ((TypeLibRootKey.SubKeyCount == 0) && (TypeLibRootKey.ValueCount == 0))
                        Registry.ClassesRoot.DeleteSubKey(strTlbRootName);
                }
            }
        }

        private void EnsureManagedCategoryExists()
        {
            if (!ManagedCategoryExists())
            {
                using (RegistryKey ComponentCategoryKey = Registry.ClassesRoot.CreateSubKey(strComponentCategorySubKey))
                {
                    using (RegistryKey ManagedCategoryKey = ComponentCategoryKey.CreateSubKey(strManagedCategoryGuid))
                    {
                        ManagedCategoryKey.SetValue("0", strManagedCategoryDescription);
                    }
                }
            }
        }

        private static bool ManagedCategoryExists()
        {
            using (RegistryKey componentCategoryKey = Registry.ClassesRoot.OpenSubKey(strComponentCategorySubKey, false))
            {
                if (componentCategoryKey == null)
                    return false;
                using (RegistryKey managedCategoryKey = componentCategoryKey.OpenSubKey(strManagedCategoryGuid, false))
                {
                    if (managedCategoryKey == null)
                        return false;
                    object value = managedCategoryKey.GetValue("0");
                    if (value == null || value.GetType() != typeof (string))
                        return false;
                    string stringValue = (string)value;
                    if (stringValue != strManagedCategoryDescription)
                        return false;
                }
            }

            return true;
        }

        private void CallUserDefinedRegistrationMethod(Type type, bool bRegister)
        {
            bool bFunctionCalled = false;
            Type RegFuncAttrType = null;
            if (bRegister)
                RegFuncAttrType = typeof (ComRegisterFunctionAttribute);
            else
                RegFuncAttrType = typeof (ComUnregisterFunctionAttribute);
            for (Type currType = type; !bFunctionCalled && currType != null; currType = currType.BaseType)
            {
                MethodInfo[] aMethods = currType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                int NumMethods = aMethods.Length;
                for (int cMethods = 0; cMethods < NumMethods; cMethods++)
                {
                    MethodInfo CurrentMethod = aMethods[cMethods];
                    if (CurrentMethod.GetCustomAttributes(RegFuncAttrType, true).Length != 0)
                    {
                        if (!CurrentMethod.IsStatic)
                        {
                            if (bRegister)
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NonStaticComRegFunction", CurrentMethod.Name, currType.Name));
                            else
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NonStaticComUnRegFunction", CurrentMethod.Name, currType.Name));
                        }

                        ParameterInfo[] aParams = CurrentMethod.GetParameters();
                        if (CurrentMethod.ReturnType != typeof (void) || aParams == null || aParams.Length != 1 || (aParams[0].ParameterType != typeof (String) && aParams[0].ParameterType != typeof (Type)))
                        {
                            if (bRegister)
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InvalidComRegFunctionSig", CurrentMethod.Name, currType.Name));
                            else
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InvalidComUnRegFunctionSig", CurrentMethod.Name, currType.Name));
                        }

                        if (bFunctionCalled)
                        {
                            if (bRegister)
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MultipleComRegFunctions", currType.Name));
                            else
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MultipleComUnRegFunctions", currType.Name));
                        }

                        Object[] objs = new Object[1];
                        if (aParams[0].ParameterType == typeof (String))
                        {
                            objs[0] = "HKEY_CLASSES_ROOT\\CLSID\\{" + Marshal.GenerateGuidForType(type).ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
                        }
                        else
                        {
                            objs[0] = type;
                        }

                        CurrentMethod.Invoke(null, objs);
                        bFunctionCalled = true;
                    }
                }
            }
        }

        private Type GetBaseComImportType(Type type)
        {
            for (; type != null && !type.IsImport; type = type.BaseType)
                ;
            return type;
        }

        private bool IsRegisteredAsValueType(Type type)
        {
            if (!type.IsValueType)
                return false;
            return true;
        }

        private static extern void CoRevokeClassObject(int cookie);
    }
}