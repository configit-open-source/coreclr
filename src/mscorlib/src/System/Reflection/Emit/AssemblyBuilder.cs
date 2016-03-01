namespace System.Reflection.Emit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.SymbolStore;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Threading;

    [Flags]
    internal enum DynamicAssemblyFlags
    {
        None = 0x00000000,
        AllCritical = 0x00000001,
        Aptca = 0x00000002,
        Critical = 0x00000004,
        Transparent = 0x00000008,
        TreatAsSafe = 0x00000010
    }

    internal sealed class InternalAssemblyBuilder : RuntimeAssembly
    {
        private InternalAssemblyBuilder()
        {
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is InternalAssemblyBuilder)
                return ((object)this == obj);
            return obj.Equals(this);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override String[] GetManifestResourceNames()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override FileStream GetFile(String name)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override FileStream[] GetFiles(bool getResourceModules)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override Stream GetManifestResourceStream(Type type, String name)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override Stream GetManifestResourceStream(String name)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override String Location
        {
            [SecurityCritical]
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
            }
        }

        public override String CodeBase
        {
            [SecurityCritical]
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
            }
        }

        public override Type[] GetExportedTypes()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicAssembly"));
        }

        public override String ImageRuntimeVersion
        {
            get
            {
                return RuntimeEnvironment.GetSystemVersion();
            }
        }
    }

    public sealed class AssemblyBuilder : Assembly, _AssemblyBuilder
    {
        private static extern RuntimeModule GetInMemoryAssemblyModule(RuntimeAssembly assembly);
        private Module nGetInMemoryAssemblyModule()
        {
            return AssemblyBuilder.GetInMemoryAssemblyModule(GetNativeHandle());
        }

        internal AssemblyBuilderData m_assemblyData;
        private InternalAssemblyBuilder m_internalAssemblyBuilder;
        private ModuleBuilder m_manifestModuleBuilder;
        private bool m_fManifestModuleUsedAsDefinedModule;
        internal const string MANIFEST_MODULE_NAME = "RefEmit_InMemoryManifestModule";
        private bool m_profileAPICheck;
        internal ModuleBuilder GetModuleBuilder(InternalModuleBuilder module)
        {
            Contract.Requires(module != null);
            Contract.Assert(this.InternalAssembly == module.Assembly);
            lock (SyncRoot)
            {
                if (m_manifestModuleBuilder.InternalModule == module)
                    return m_manifestModuleBuilder;
                throw new ArgumentException("module");
            }
        }

        internal object SyncRoot
        {
            get
            {
                return InternalAssembly.SyncRoot;
            }
        }

        internal InternalAssemblyBuilder InternalAssembly
        {
            get
            {
                return m_internalAssemblyBuilder;
            }
        }

        internal RuntimeAssembly GetNativeHandle()
        {
            return InternalAssembly.GetNativeHandle();
        }

        internal Version GetVersion()
        {
            return InternalAssembly.GetVersion();
        }

        internal bool ProfileAPICheck
        {
            get
            {
                return m_profileAPICheck;
            }
        }

        internal AssemblyBuilder(AppDomain domain, AssemblyName name, AssemblyBuilderAccess access, String dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, ref StackCrawlMark stackMark, IEnumerable<CustomAttributeBuilder> unsafeAssemblyAttributes, SecurityContextSource securityContextSource)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (access != AssemblyBuilderAccess.Run && access != AssemblyBuilderAccess.RunAndCollect)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)access), "access");
            }

            if (securityContextSource < SecurityContextSource.CurrentAppDomain || securityContextSource > SecurityContextSource.CurrentAssembly)
            {
                throw new ArgumentOutOfRangeException("securityContextSource");
            }

            name = (AssemblyName)name.Clone();
            if (evidence != null)
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
            List<CustomAttributeBuilder> assemblyAttributes = null;
            DynamicAssemblyFlags assemblyFlags = DynamicAssemblyFlags.None;
            byte[] securityRulesBlob = null;
            byte[] aptcaBlob = null;
            if (unsafeAssemblyAttributes != null)
            {
                assemblyAttributes = new List<CustomAttributeBuilder>(unsafeAssemblyAttributes);
                foreach (CustomAttributeBuilder attribute in assemblyAttributes)
                {
                    if (attribute.m_con.DeclaringType == typeof (SecurityTransparentAttribute))
                    {
                        assemblyFlags |= DynamicAssemblyFlags.Transparent;
                    }
                    else if (attribute.m_con.DeclaringType == typeof (SecurityCriticalAttribute))
                    {
                        {
                            assemblyFlags |= DynamicAssemblyFlags.AllCritical;
                        }
                    }
                }
            }

            m_internalAssemblyBuilder = (InternalAssemblyBuilder)nCreateDynamicAssembly(domain, name, evidence, ref stackMark, requiredPermissions, optionalPermissions, refusedPermissions, securityRulesBlob, aptcaBlob, access, assemblyFlags, securityContextSource);
            m_assemblyData = new AssemblyBuilderData(m_internalAssemblyBuilder, name.Name, access, dir);
            m_assemblyData.AddPermissionRequests(requiredPermissions, optionalPermissions, refusedPermissions);
            if (AppDomain.ProfileAPICheck)
            {
                RuntimeAssembly creator = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (creator != null && !creator.IsFrameworkAssembly())
                    m_profileAPICheck = true;
            }

            InitManifestModule();
            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
                    SetCustomAttribute(assemblyAttribute);
            }
        }

        private void InitManifestModule()
        {
            InternalModuleBuilder modBuilder = (InternalModuleBuilder)nGetInMemoryAssemblyModule();
            m_manifestModuleBuilder = new ModuleBuilder(this, modBuilder);
            m_manifestModuleBuilder.Init(AssemblyBuilder.MANIFEST_MODULE_NAME, null, 0);
            m_fManifestModuleUsedAsDefinedModule = false;
        }

        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, null, null, null, null, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, null, null, null, null, ref stackMark, assemblyAttributes, SecurityContextSource.CurrentAssembly);
        }

        private static extern Assembly nCreateDynamicAssembly(AppDomain domain, AssemblyName name, Evidence identity, ref StackCrawlMark stackMark, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, byte[] securityRulesBlob, byte[] aptcaBlob, AssemblyBuilderAccess access, DynamicAssemblyFlags flags, SecurityContextSource securityContextSource);
        private class AssemblyBuilderLock
        {
        }

        internal static AssemblyBuilder InternalDefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, ref StackCrawlMark stackMark, IEnumerable<CustomAttributeBuilder> unsafeAssemblyAttributes, SecurityContextSource securityContextSource)
        {
            lock (typeof (AssemblyBuilderLock))
            {
                return new AssemblyBuilder(AppDomain.CurrentDomain, name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, unsafeAssemblyAttributes, securityContextSource);
            }
        }

        public ModuleBuilder DefineDynamicModule(String name)
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal(name, false, ref stackMark);
        }

        public ModuleBuilder DefineDynamicModule(String name, bool emitSymbolInfo)
        {
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return DefineDynamicModuleInternal(name, emitSymbolInfo, ref stackMark);
        }

        private ModuleBuilder DefineDynamicModuleInternal(String name, bool emitSymbolInfo, ref StackCrawlMark stackMark)
        {
            lock (SyncRoot)
            {
                return DefineDynamicModuleInternalNoLock(name, emitSymbolInfo, ref stackMark);
            }
        }

        private ModuleBuilder DefineDynamicModuleInternalNoLock(String name, bool emitSymbolInfo, ref StackCrawlMark stackMark)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidName"), "name");
            Contract.Ensures(Contract.Result<ModuleBuilder>() != null);
            Contract.EndContractBlock();
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.DefineDynamicModule( " + name + " )");
            Contract.Assert(m_assemblyData != null, "m_assemblyData is null in DefineDynamicModuleInternal");
            ModuleBuilder dynModule;
            ISymbolWriter writer = null;
            IntPtr pInternalSymWriter = new IntPtr();
            if (m_fManifestModuleUsedAsDefinedModule == true)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoMultiModuleAssembly"));
            dynModule = m_manifestModuleBuilder;
            if (emitSymbolInfo)
            {
                writer = SymWrapperCore.SymWriter.CreateSymWriter();
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                String fileName = "Unused";
                pInternalSymWriter = ModuleBuilder.nCreateISymWriterForDynamicModule(dynModule.InternalModule, fileName);
                ((SymWrapperCore.SymWriter)writer).InternalSetUnderlyingWriter(pInternalSymWriter);
            }

            dynModule.SetSymWriter(writer);
            m_assemblyData.AddModule(dynModule);
            if (dynModule == m_manifestModuleBuilder)
            {
                m_fManifestModuleUsedAsDefinedModule = true;
            }

            return dynModule;
        }

        private Assembly LoadISymWrapper()
        {
            if (m_assemblyData.m_ISymWrapperAssembly != null)
                return m_assemblyData.m_ISymWrapperAssembly;
            Assembly assem = Assembly.Load("ISymWrapper, Version=" + ThisAssembly.Version + ", Culture=neutral, PublicKeyToken=" + AssemblyRef.MicrosoftPublicKeyToken);
            m_assemblyData.m_ISymWrapperAssembly = assem;
            return assem;
        }

        internal void CheckContext(params Type[][] typess)
        {
            if (typess == null)
                return;
            foreach (Type[] types in typess)
                if (types != null)
                    CheckContext(types);
        }

        internal void CheckContext(params Type[] types)
        {
            if (types == null)
                return;
            foreach (Type type in types)
            {
                if (type == null)
                    continue;
                if (type.Module == null || type.Module.Assembly == null)
                    throw new ArgumentException(Environment.GetResourceString("Argument_TypeNotValid"));
                if (type.Module.Assembly == typeof (object).Module.Assembly)
                    continue;
                if (type.Module.Assembly.ReflectionOnly && !ReflectionOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("Arugment_EmitMixedContext1", type.AssemblyQualifiedName));
                if (!type.Module.Assembly.ReflectionOnly && ReflectionOnly)
                    throw new InvalidOperationException(Environment.GetResourceString("Arugment_EmitMixedContext2", type.AssemblyQualifiedName));
            }
        }

        public void AddResourceFile(String name, String fileName)
        {
            AddResourceFile(name, fileName, ResourceAttributes.Public);
        }

        public void AddResourceFile(String name, String fileName, ResourceAttributes attribute)
        {
            lock (SyncRoot)
            {
                AddResourceFileNoLock(name, fileName, attribute);
            }
        }

        private void AddResourceFileNoLock(String name, String fileName, ResourceAttributes attribute)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), name);
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (fileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), fileName);
            if (!String.Equals(fileName, Path.GetFileName(fileName)))
                throw new ArgumentException(Environment.GetResourceString("Argument_NotSimpleFileName"), "fileName");
            Contract.EndContractBlock();
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.AddResourceFile( " + name + ", " + fileName + ")");
            m_assemblyData.CheckResNameConflict(name);
            m_assemblyData.CheckFileNameConflict(fileName);
            String fullFileName;
            if (m_assemblyData.m_strDir == null)
            {
                fullFileName = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            }
            else
            {
                fullFileName = Path.Combine(m_assemblyData.m_strDir, fileName);
            }

            fullFileName = Path.UnsafeGetFullPath(fullFileName);
            fileName = Path.GetFileName(fullFileName);
            if (File.UnsafeExists(fullFileName) == false)
                throw new FileNotFoundException(Environment.GetResourceString("IO.FileNotFound_FileName", fileName), fileName);
            m_assemblyData.AddResWriter(new ResWriterData(null, null, name, fileName, fullFileName, attribute));
        }

        public override bool Equals(object obj)
        {
            return InternalAssembly.Equals(obj);
        }

        public override int GetHashCode()
        {
            return InternalAssembly.GetHashCode();
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return InternalAssembly.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return InternalAssembly.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return InternalAssembly.IsDefined(attributeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return InternalAssembly.GetCustomAttributesData();
        }

        public override String[] GetManifestResourceNames()
        {
            return InternalAssembly.GetManifestResourceNames();
        }

        public override FileStream GetFile(String name)
        {
            return InternalAssembly.GetFile(name);
        }

        public override FileStream[] GetFiles(bool getResourceModules)
        {
            return InternalAssembly.GetFiles(getResourceModules);
        }

        public override Stream GetManifestResourceStream(Type type, String name)
        {
            return InternalAssembly.GetManifestResourceStream(type, name);
        }

        public override Stream GetManifestResourceStream(String name)
        {
            return InternalAssembly.GetManifestResourceStream(name);
        }

        public override ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            return InternalAssembly.GetManifestResourceInfo(resourceName);
        }

        public override String Location
        {
            [System.Security.SecurityCritical]
            get
            {
                return InternalAssembly.Location;
            }
        }

        public override String ImageRuntimeVersion
        {
            get
            {
                return InternalAssembly.ImageRuntimeVersion;
            }
        }

        public override String CodeBase
        {
            [System.Security.SecurityCritical]
            get
            {
                return InternalAssembly.CodeBase;
            }
        }

        public override MethodInfo EntryPoint
        {
            get
            {
                return m_assemblyData.m_entryPointMethod;
            }
        }

        public override Type[] GetExportedTypes()
        {
            return InternalAssembly.GetExportedTypes();
        }

        public override AssemblyName GetName(bool copiedName)
        {
            return InternalAssembly.GetName(copiedName);
        }

        public override String FullName
        {
            get
            {
                return InternalAssembly.FullName;
            }
        }

        public override Type GetType(String name, bool throwOnError, bool ignoreCase)
        {
            return InternalAssembly.GetType(name, throwOnError, ignoreCase);
        }

        public override Module ManifestModule
        {
            get
            {
                return m_manifestModuleBuilder.InternalModule;
            }
        }

        public override bool ReflectionOnly
        {
            get
            {
                return InternalAssembly.ReflectionOnly;
            }
        }

        public override Module GetModule(String name)
        {
            return InternalAssembly.GetModule(name);
        }

        public override AssemblyName[] GetReferencedAssemblies()
        {
            return InternalAssembly.GetReferencedAssemblies();
        }

        public override bool GlobalAssemblyCache
        {
            get
            {
                return InternalAssembly.GlobalAssemblyCache;
            }
        }

        public override Int64 HostContext
        {
            get
            {
                return InternalAssembly.HostContext;
            }
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            return InternalAssembly.GetModules(getResourceModules);
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return InternalAssembly.GetLoadedModules(getResourceModules);
        }

        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalAssembly.InternalGetSatelliteAssembly(culture, null, ref stackMark);
        }

        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalAssembly.InternalGetSatelliteAssembly(culture, version, ref stackMark);
        }

        public override bool IsDynamic
        {
            get
            {
                return true;
            }
        }

        public void DefineVersionInfoResource(String product, String productVersion, String company, String copyright, String trademark)
        {
            lock (SyncRoot)
            {
                DefineVersionInfoResourceNoLock(product, productVersion, company, copyright, trademark);
            }
        }

        private void DefineVersionInfoResourceNoLock(String product, String productVersion, String company, String copyright, String trademark)
        {
            if (m_assemblyData.m_strResourceFileName != null || m_assemblyData.m_resourceBytes != null || m_assemblyData.m_nativeVersion != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            m_assemblyData.m_nativeVersion = new NativeVersionInfo();
            m_assemblyData.m_nativeVersion.m_strCopyright = copyright;
            m_assemblyData.m_nativeVersion.m_strTrademark = trademark;
            m_assemblyData.m_nativeVersion.m_strCompany = company;
            m_assemblyData.m_nativeVersion.m_strProduct = product;
            m_assemblyData.m_nativeVersion.m_strProductVersion = productVersion;
            m_assemblyData.m_hasUnmanagedVersionInfo = true;
            m_assemblyData.m_OverrideUnmanagedVersionInfo = true;
        }

        public void DefineVersionInfoResource()
        {
            lock (SyncRoot)
            {
                DefineVersionInfoResourceNoLock();
            }
        }

        private void DefineVersionInfoResourceNoLock()
        {
            if (m_assemblyData.m_strResourceFileName != null || m_assemblyData.m_resourceBytes != null || m_assemblyData.m_nativeVersion != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            m_assemblyData.m_hasUnmanagedVersionInfo = true;
            m_assemblyData.m_nativeVersion = new NativeVersionInfo();
        }

        public void DefineUnmanagedResource(Byte[] resource)
        {
            if (resource == null)
                throw new ArgumentNullException("resource");
            Contract.EndContractBlock();
            lock (SyncRoot)
            {
                DefineUnmanagedResourceNoLock(resource);
            }
        }

        private void DefineUnmanagedResourceNoLock(Byte[] resource)
        {
            if (m_assemblyData.m_strResourceFileName != null || m_assemblyData.m_resourceBytes != null || m_assemblyData.m_nativeVersion != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            m_assemblyData.m_resourceBytes = new byte[resource.Length];
            System.Array.Copy(resource, m_assemblyData.m_resourceBytes, resource.Length);
        }

        public void DefineUnmanagedResource(String resourceFileName)
        {
            if (resourceFileName == null)
                throw new ArgumentNullException("resourceFileName");
            Contract.EndContractBlock();
            lock (SyncRoot)
            {
                DefineUnmanagedResourceNoLock(resourceFileName);
            }
        }

        private void DefineUnmanagedResourceNoLock(String resourceFileName)
        {
            if (m_assemblyData.m_strResourceFileName != null || m_assemblyData.m_resourceBytes != null || m_assemblyData.m_nativeVersion != null)
                throw new ArgumentException(Environment.GetResourceString("Argument_NativeResourceAlreadyDefined"));
            string strFullFileName;
            if (m_assemblyData.m_strDir == null)
            {
                strFullFileName = Path.Combine(Directory.GetCurrentDirectory(), resourceFileName);
            }
            else
            {
                strFullFileName = Path.Combine(m_assemblyData.m_strDir, resourceFileName);
            }

            strFullFileName = Path.GetFullPath(resourceFileName);
            new FileIOPermission(FileIOPermissionAccess.Read, strFullFileName).Demand();
            if (File.Exists(strFullFileName) == false)
                throw new FileNotFoundException(Environment.GetResourceString("IO.FileNotFound_FileName", resourceFileName), resourceFileName);
            m_assemblyData.m_strResourceFileName = strFullFileName;
        }

        public ModuleBuilder GetDynamicModule(String name)
        {
            lock (SyncRoot)
            {
                return GetDynamicModuleNoLock(name);
            }
        }

        private ModuleBuilder GetDynamicModuleNoLock(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            Contract.EndContractBlock();
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.GetDynamicModule( " + name + " )");
            int size = m_assemblyData.m_moduleBuilderList.Count;
            for (int i = 0; i < size; i++)
            {
                ModuleBuilder moduleBuilder = (ModuleBuilder)m_assemblyData.m_moduleBuilderList[i];
                if (moduleBuilder.m_moduleData.m_strModuleName.Equals(name))
                {
                    return moduleBuilder;
                }
            }

            return null;
        }

        public void SetEntryPoint(MethodInfo entryMethod)
        {
            SetEntryPoint(entryMethod, PEFileKinds.ConsoleApplication);
        }

        public void SetEntryPoint(MethodInfo entryMethod, PEFileKinds fileKind)
        {
            lock (SyncRoot)
            {
                SetEntryPointNoLock(entryMethod, fileKind);
            }
        }

        private void SetEntryPointNoLock(MethodInfo entryMethod, PEFileKinds fileKind)
        {
            if (entryMethod == null)
                throw new ArgumentNullException("entryMethod");
            Contract.EndContractBlock();
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: AssemblyBuilder.SetEntryPoint");
            Module tmpModule = entryMethod.Module;
            if (tmpModule == null || !InternalAssembly.Equals(tmpModule.Assembly))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_EntryMethodNotDefinedInAssembly"));
            m_assemblyData.m_entryPointMethod = entryMethod;
            m_assemblyData.m_peFileKind = fileKind;
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();
            lock (SyncRoot)
            {
                SetCustomAttributeNoLock(con, binaryAttribute);
            }
        }

        private void SetCustomAttributeNoLock(ConstructorInfo con, byte[] binaryAttribute)
        {
            TypeBuilder.DefineCustomAttribute(m_manifestModuleBuilder, AssemblyBuilderData.m_tkAssembly, m_manifestModuleBuilder.GetConstructorToken(con).Token, binaryAttribute, false, typeof (System.Diagnostics.DebuggableAttribute) == con.DeclaringType);
            if (m_assemblyData.m_access != AssemblyBuilderAccess.Run)
            {
                m_assemblyData.AddCustomAttribute(con, binaryAttribute);
            }
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException("customBuilder");
            }

            Contract.EndContractBlock();
            lock (SyncRoot)
            {
                SetCustomAttributeNoLock(customBuilder);
            }
        }

        private void SetCustomAttributeNoLock(CustomAttributeBuilder customBuilder)
        {
            customBuilder.CreateCustomAttribute(m_manifestModuleBuilder, AssemblyBuilderData.m_tkAssembly);
            if (m_assemblyData.m_access != AssemblyBuilderAccess.Run)
            {
                m_assemblyData.AddCustomAttribute(customBuilder);
            }
        }

        public void Save(String assemblyFileName)
        {
            Save(assemblyFileName, System.Reflection.PortableExecutableKinds.ILOnly, System.Reflection.ImageFileMachine.I386);
        }

        public void Save(String assemblyFileName, PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            lock (SyncRoot)
            {
                SaveNoLock(assemblyFileName, portableExecutableKind, imageFileMachine);
            }
        }

        private void SaveNoLock(String assemblyFileName, PortableExecutableKinds portableExecutableKind, ImageFileMachine imageFileMachine)
        {
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CantSaveTransientAssembly"));
        }

        internal bool IsPersistable()
        {
            {
                return false;
            }
        }

        private int DefineNestedComType(Type type, int tkResolutionScope, int tkTypeDef)
        {
            Type enclosingType = type.DeclaringType;
            if (enclosingType == null)
            {
                return AddExportedTypeOnDisk(GetNativeHandle(), type.FullName, tkResolutionScope, tkTypeDef, type.Attributes);
            }

            tkResolutionScope = DefineNestedComType(enclosingType, tkResolutionScope, tkTypeDef);
            return AddExportedTypeOnDisk(GetNativeHandle(), type.Name, tkResolutionScope, tkTypeDef, type.Attributes);
        }

        internal int DefineExportedTypeInMemory(Type type, int tkResolutionScope, int tkTypeDef)
        {
            Type enclosingType = type.DeclaringType;
            if (enclosingType == null)
            {
                return AddExportedTypeInMemory(GetNativeHandle(), type.FullName, tkResolutionScope, tkTypeDef, type.Attributes);
            }

            tkResolutionScope = DefineExportedTypeInMemory(enclosingType, tkResolutionScope, tkTypeDef);
            return AddExportedTypeInMemory(GetNativeHandle(), type.Name, tkResolutionScope, tkTypeDef, type.Attributes);
        }

        private AssemblyBuilder()
        {
        }

        static private extern void DefineDynamicModule(RuntimeAssembly containingAssembly, bool emitSymbolInfo, String name, String filename, StackCrawlMarkHandle stackMark, ref IntPtr pInternalSymWriter, ObjectHandleOnStack retModule, bool fIsTransient, out int tkFile);
        private static Module DefineDynamicModule(RuntimeAssembly containingAssembly, bool emitSymbolInfo, String name, String filename, ref StackCrawlMark stackMark, ref IntPtr pInternalSymWriter, bool fIsTransient, out int tkFile)
        {
            RuntimeModule retModule = null;
            DefineDynamicModule(containingAssembly.GetNativeHandle(), emitSymbolInfo, name, filename, JitHelpers.GetStackCrawlMarkHandle(ref stackMark), ref pInternalSymWriter, JitHelpers.GetObjectHandleOnStack(ref retModule), fIsTransient, out tkFile);
            return retModule;
        }

        static private extern void PrepareForSavingManifestToDisk(RuntimeAssembly assembly, RuntimeModule assemblyModule);
        static private extern void SaveManifestToDisk(RuntimeAssembly assembly, String strFileName, int entryPoint, int fileKind, int portableExecutableKind, int ImageFileMachine);
        static private extern int AddFile(RuntimeAssembly assembly, String strFileName);
        static private extern void SetFileHashValue(RuntimeAssembly assembly, int tkFile, String strFullFileName);
        static private extern int AddExportedTypeInMemory(RuntimeAssembly assembly, String strComTypeName, int tkAssemblyRef, int tkTypeDef, TypeAttributes flags);
        static private extern int AddExportedTypeOnDisk(RuntimeAssembly assembly, String strComTypeName, int tkAssemblyRef, int tkTypeDef, TypeAttributes flags);
        static private extern void AddStandAloneResource(RuntimeAssembly assembly, String strName, String strFileName, String strFullFileName, int attribute);
        static private extern void AddDeclarativeSecurity(RuntimeAssembly assembly, SecurityAction action, byte[] blob, int length);
        static private extern void CreateVersionInfoResource(String filename, String title, String iconFilename, String description, String copyright, String trademark, String company, String product, String productVersion, String fileVersion, int lcid, bool isDll, StringHandleOnStack retFileName);
    }
}