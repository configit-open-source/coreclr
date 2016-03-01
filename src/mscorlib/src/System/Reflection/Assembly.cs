using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;

namespace System.Reflection
{
    public delegate Module ModuleResolveEventHandler(Object sender, ResolveEventArgs e);
    public abstract class Assembly : _Assembly, IEvidenceFactory, ICustomAttributeProvider, ISerializable
    {
        protected Assembly()
        {
        }

        public static String CreateQualifiedName(String assemblyName, String typeName)
        {
            return typeName + ", " + assemblyName;
        }

        public static Assembly GetAssembly(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
                        Module m = type.Module;
            if (m == null)
                return null;
            else
                return m.Assembly;
        }

        public override bool Equals(object o)
        {
            return base.Equals(o);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static Assembly LoadFrom(String assemblyFile)
        {
                                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_WindowsPhone", "Assembly.LoadFrom"));
        }

        public static Assembly ReflectionOnlyLoadFrom(String assemblyFile)
        {
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadFrom(assemblyFile, null, null, AssemblyHashAlgorithm.None, true, false, ref stackMark);
        }

        public static Assembly LoadFrom(String assemblyFile, Evidence securityEvidence)
        {
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadFrom(assemblyFile, securityEvidence, null, AssemblyHashAlgorithm.None, false, false, ref stackMark);
        }

        public static Assembly LoadFrom(String assemblyFile, Evidence securityEvidence, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
        {
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadFrom(assemblyFile, securityEvidence, hashValue, hashAlgorithm, false, false, ref stackMark);
        }

        public static Assembly LoadFrom(String assemblyFile, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadFrom(assemblyFile, null, hashValue, hashAlgorithm, false, false, ref stackMark);
        }

        public static Assembly Load(String assemblyString)
        {
                                    StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackMark, false);
        }

        internal static Type GetType_Compat(String assemblyString, String typeName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            RuntimeAssembly assembly;
            AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(assemblyString, false, out assembly);
            if (assembly == null)
            {
                if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
                {
                    return Type.GetType(typeName + ", " + assemblyString, true, false);
                }

                assembly = RuntimeAssembly.InternalLoadAssemblyName(assemblyName, null, null, ref stackMark, true, false, false);
            }

            return assembly.GetType(typeName, true, false);
        }

        public static Assembly ReflectionOnlyLoad(String assemblyString)
        {
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackMark, true);
        }

        public static Assembly Load(String assemblyString, Evidence assemblySecurity)
        {
                                    StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, assemblySecurity, ref stackMark, false);
        }

        public static Assembly Load(AssemblyName assemblyRef)
        {
                                    if (assemblyRef != null && assemblyRef.CodeBase != null)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AssemblyLoadCodeBase"));
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackMark, true, false, false);
        }

        public static Assembly Load(AssemblyName assemblyRef, Evidence assemblySecurity)
        {
                                    StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, assemblySecurity, null, ref stackMark, true, false, false);
        }

        public static Assembly Load(byte[] rawAssembly)
        {
                                    AppDomain.CheckLoadByteArraySupported();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly, null, null, ref stackMark, false, SecurityContextSource.CurrentAssembly);
        }

        public static Assembly ReflectionOnlyLoad(byte[] rawAssembly)
        {
                        AppDomain.CheckReflectionOnlyLoadSupported();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly, null, null, ref stackMark, true, SecurityContextSource.CurrentAssembly);
        }

        public static Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore)
        {
                                    AppDomain.CheckLoadByteArraySupported();
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, null, ref stackMark, false, SecurityContextSource.CurrentAssembly);
        }

        public static Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore, SecurityContextSource securityContextSource)
        {
                                    AppDomain.CheckLoadByteArraySupported();
            if (securityContextSource < SecurityContextSource.CurrentAppDomain || securityContextSource > SecurityContextSource.CurrentAssembly)
            {
                throw new ArgumentOutOfRangeException("securityContextSource");
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, null, ref stackMark, false, securityContextSource);
        }

        public static Assembly LoadFile(String path)
        {
                                    AppDomain.CheckLoadFileSupported();
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, path).Demand();
            return RuntimeAssembly.nLoadFile(path, null);
        }

        public static Assembly Load(Stream assemblyStream, Stream pdbStream)
        {
                                    StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadFromStream(assemblyStream, pdbStream, ref stackMark);
        }

        public static Assembly Load(Stream assemblyStream)
        {
                                    StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadFromStream(assemblyStream, null, ref stackMark);
        }

        public static Assembly GetExecutingAssembly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.GetExecutingAssembly(ref stackMark);
        }

        public static Assembly GetCallingAssembly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
            return RuntimeAssembly.GetExecutingAssembly(ref stackMark);
        }

        public static Assembly GetEntryAssembly()
        {
            AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
            if (domainManager == null)
                domainManager = new AppDomainManager();
            return domainManager.EntryAssembly;
        }

        public virtual event ModuleResolveEventHandler ModuleResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                throw new NotImplementedException();
            }

            [System.Security.SecurityCritical]
            remove
            {
                throw new NotImplementedException();
            }
        }

        public virtual String CodeBase
        {
            [System.Security.SecurityCritical]
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual String EscapedCodeBase
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return AssemblyName.EscapeCodeBase(CodeBase);
            }
        }

        public virtual AssemblyName GetName()
        {
            return GetName(false);
        }

        public virtual AssemblyName GetName(bool copiedName)
        {
            throw new NotImplementedException();
        }

        public virtual String FullName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual MethodInfo EntryPoint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Type GetType(String name)
        {
            return GetType(name, false, false);
        }

        public virtual Type GetType(String name, bool throwOnError)
        {
            return GetType(name, throwOnError, false);
        }

        public virtual Type GetType(String name, bool throwOnError, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<Type> ExportedTypes
        {
            get
            {
                return GetExportedTypes();
            }
        }

        public virtual Type[] GetExportedTypes()
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<TypeInfo> DefinedTypes
        {
            get
            {
                Type[] types = GetTypes();
                TypeInfo[] typeinfos = new TypeInfo[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    TypeInfo typeinfo = types[i].GetTypeInfo();
                    if (typeinfo == null)
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoTypeInfo", types[i].FullName));
                    typeinfos[i] = typeinfo;
                }

                return typeinfos;
            }
        }

        public virtual Type[] GetTypes()
        {
            Module[] m = GetModules(false);
            int iNumModules = m.Length;
            int iFinalLength = 0;
            Type[][] ModuleTypes = new Type[iNumModules][];
            for (int i = 0; i < iNumModules; i++)
            {
                ModuleTypes[i] = m[i].GetTypes();
                iFinalLength += ModuleTypes[i].Length;
            }

            int iCurrent = 0;
            Type[] ret = new Type[iFinalLength];
            for (int i = 0; i < iNumModules; i++)
            {
                int iLength = ModuleTypes[i].Length;
                Array.Copy(ModuleTypes[i], 0, ret, iCurrent, iLength);
                iCurrent += iLength;
            }

            return ret;
        }

        public virtual Stream GetManifestResourceStream(Type type, String name)
        {
            throw new NotImplementedException();
        }

        public virtual Stream GetManifestResourceStream(String name)
        {
            throw new NotImplementedException();
        }

        public virtual Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public virtual Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            throw new NotImplementedException();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public virtual Module ManifestModule
        {
            get
            {
                RuntimeAssembly rtAssembly = this as RuntimeAssembly;
                if (rtAssembly != null)
                    return rtAssembly.ManifestModule;
                throw new NotImplementedException();
            }
        }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GetCustomAttributesData();
            }
        }

        public virtual Object[] GetCustomAttributes(bool inherit)
        {
                        throw new NotImplementedException();
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
                        throw new NotImplementedException();
        }

        public virtual bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        internal virtual bool IsProfileAssembly
        {
            [System.Security.SecurityCritical]
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public virtual bool ReflectionOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Object CreateInstance(String typeName)
        {
            return CreateInstance(typeName, false, BindingFlags.Public | BindingFlags.Instance, null, null, null, null);
        }

        public Object CreateInstance(String typeName, bool ignoreCase)
        {
            return CreateInstance(typeName, ignoreCase, BindingFlags.Public | BindingFlags.Instance, null, null, null, null);
        }

        public virtual Object CreateInstance(String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes)
        {
            Type t = GetType(typeName, false, ignoreCase);
            if (t == null)
                return null;
            return Activator.CreateInstance(t, bindingAttr, binder, args, culture, activationAttributes);
        }

        public virtual IEnumerable<Module> Modules
        {
            get
            {
                return GetLoadedModules(true);
            }
        }

        public Module[] GetLoadedModules()
        {
            return GetLoadedModules(false);
        }

        public virtual Module[] GetLoadedModules(bool getResourceModules)
        {
            throw new NotImplementedException();
        }

        public Module[] GetModules()
        {
            return GetModules(false);
        }

        public virtual Module[] GetModules(bool getResourceModules)
        {
            throw new NotImplementedException();
        }

        public virtual Module GetModule(String name)
        {
            throw new NotImplementedException();
        }

        public virtual FileStream GetFile(String name)
        {
            throw new NotImplementedException();
        }

        public virtual FileStream[] GetFiles()
        {
            return GetFiles(false);
        }

        public virtual FileStream[] GetFiles(bool getResourceModules)
        {
            throw new NotImplementedException();
        }

        public virtual String[] GetManifestResourceNames()
        {
            throw new NotImplementedException();
        }

        public virtual AssemblyName[] GetReferencedAssemblies()
        {
            throw new NotImplementedException();
        }

        public virtual ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            throw new NotImplementedException();
        }

        public override String ToString()
        {
            String displayName = FullName;
            if (displayName == null)
                return base.ToString();
            else
                return displayName;
        }

        public virtual String Location
        {
            [System.Security.SecurityCritical]
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual String ImageRuntimeVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool GlobalAssemblyCache
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Int64 HostContext
        {
            get
            {
                RuntimeAssembly rtAssembly = this as RuntimeAssembly;
                if (rtAssembly != null)
                    return rtAssembly.HostContext;
                throw new NotImplementedException();
            }
        }

        public virtual bool IsDynamic
        {
            get
            {
                return false;
            }
        }
    }

    internal enum LoadContext
    {
        DEFAULT,
        LOADFROM,
        UNKNOWN,
        HOSTED
    }

    internal class RuntimeAssembly : Assembly
    {
        private enum ASSEMBLY_FLAGS : uint
        {
            ASSEMBLY_FLAGS_UNKNOWN = 0x00000000,
            ASSEMBLY_FLAGS_INITIALIZED = 0x01000000,
            ASSEMBLY_FLAGS_FRAMEWORK = 0x02000000,
            ASSEMBLY_FLAGS_SAFE_REFLECTION = 0x04000000,
            ASSEMBLY_FLAGS_TOKEN_MASK = 0x00FFFFFF
        }

        private const uint COR_E_LOADING_REFERENCE_ASSEMBLY = 0x80131058U;
        internal RuntimeAssembly()
        {
            throw new NotSupportedException();
        }

        private event ModuleResolveEventHandler _ModuleResolve;
        private string m_fullname;
        private object m_syncRoot;
        private IntPtr m_assembly;
        private ASSEMBLY_FLAGS m_flags;
        internal int InvocableAttributeCtorToken
        {
            get
            {
                int token = (int)(Flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_TOKEN_MASK);
                return token | (int)MetadataTokenType.MethodDef;
            }
        }

        private ASSEMBLY_FLAGS Flags
        {
            [SecuritySafeCritical]
            get
            {
                if ((m_flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_INITIALIZED) == 0)
                {
                    ASSEMBLY_FLAGS flags = ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_UNKNOWN;
                    if (IsProfileAssembly)
                    {
                        flags |= ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_FRAMEWORK | ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION;
                        foreach (string name in s_unsafeFrameworkAssemblyNames)
                        {
                            if (String.Compare(GetSimpleName(), name, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                flags &= ~ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION;
                                break;
                            }
                        }

                        Type invocableAttribute = GetType("__DynamicallyInvokableAttribute", false);
                        if (invocableAttribute != null)
                        {
                                                        ConstructorInfo ctor = invocableAttribute.GetConstructor(Type.EmptyTypes);
                                                        int token = ctor.MetadataToken;
                                                        flags |= (ASSEMBLY_FLAGS)token & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_TOKEN_MASK;
                        }
                    }
                    else if (IsDesignerBindingContext())
                    {
                        flags = ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION;
                    }

                    m_flags = flags | ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_INITIALIZED;
                }

                return m_flags;
            }
        }

        internal object SyncRoot
        {
            get
            {
                if (m_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref m_syncRoot, new object (), null);
                }

                return m_syncRoot;
            }
        }

        public override event ModuleResolveEventHandler ModuleResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                _ModuleResolve += value;
            }

            [System.Security.SecurityCritical]
            remove
            {
                _ModuleResolve -= value;
            }
        }

        private const String s_localFilePrefix = "file:";
        private static extern void GetCodeBase(RuntimeAssembly assembly, bool copiedName, StringHandleOnStack retString);
        internal String GetCodeBase(bool copiedName)
        {
            String codeBase = null;
            GetCodeBase(GetNativeHandle(), copiedName, JitHelpers.GetStringHandleOnStack(ref codeBase));
            return codeBase;
        }

        public override String CodeBase
        {
            [System.Security.SecurityCritical]
            get
            {
                String codeBase = GetCodeBase(false);
                VerifyCodeBaseDiscovery(codeBase);
                return codeBase;
            }
        }

        internal RuntimeAssembly GetNativeHandle()
        {
            return this;
        }

        public override AssemblyName GetName(bool copiedName)
        {
            AssemblyName an = new AssemblyName();
            String codeBase = GetCodeBase(copiedName);
            VerifyCodeBaseDiscovery(codeBase);
            an.Init(GetSimpleName(), GetPublicKey(), null, GetVersion(), GetLocale(), GetHashAlgorithm(), AssemblyVersionCompatibility.SameMachine, codeBase, GetFlags() | AssemblyNameFlags.PublicKey, null);
            PortableExecutableKinds pek;
            ImageFileMachine ifm;
            Module manifestModule = ManifestModule;
            if (manifestModule != null)
            {
                if (manifestModule.MDStreamVersion > 0x10000)
                {
                    ManifestModule.GetPEKind(out pek, out ifm);
                    an.SetProcArchIndex(pek, ifm);
                }
            }

            return an;
        }

        private extern static void GetFullName(RuntimeAssembly assembly, StringHandleOnStack retString);
        public override String FullName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_fullname == null)
                {
                    string s = null;
                    GetFullName(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref s));
                    Interlocked.CompareExchange<string>(ref m_fullname, s, null);
                }

                return m_fullname;
            }
        }

        private static extern void GetEntryPoint(RuntimeAssembly assembly, ObjectHandleOnStack retMethod);
        public override MethodInfo EntryPoint
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                IRuntimeMethodInfo methodHandle = null;
                GetEntryPoint(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref methodHandle));
                if (methodHandle == null)
                    return null;
                return (MethodInfo)RuntimeType.GetMethodBase(methodHandle);
            }
        }

        private static extern void GetType(RuntimeAssembly assembly, String name, bool throwOnError, bool ignoreCase, ObjectHandleOnStack type);
        public override Type GetType(String name, bool throwOnError, bool ignoreCase)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            RuntimeType type = null;
            GetType(GetNativeHandle(), name, throwOnError, ignoreCase, JitHelpers.GetObjectHandleOnStack(ref type));
            return type;
        }

        internal extern static void GetForwardedTypes(RuntimeAssembly assembly, ObjectHandleOnStack retTypes);
        private extern static void GetExportedTypes(RuntimeAssembly assembly, ObjectHandleOnStack retTypes);
        public override Type[] GetExportedTypes()
        {
            Type[] types = null;
            GetExportedTypes(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref types));
            return types;
        }

        public override IEnumerable<TypeInfo> DefinedTypes
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                List<RuntimeType> rtTypes = new List<RuntimeType>();
                RuntimeModule[] modules = GetModulesInternal(true, false);
                for (int i = 0; i < modules.Length; i++)
                {
                    rtTypes.AddRange(modules[i].GetDefinedTypes());
                }

                return rtTypes.ToArray();
            }
        }

        public override Stream GetManifestResourceStream(Type type, String name)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetManifestResourceStream(type, name, false, ref stackMark);
        }

        public override Stream GetManifestResourceStream(String name)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return GetManifestResourceStream(name, ref stackMark, false);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
                        UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.AssemblyUnity, this.FullName, this);
        }

        public override Module ManifestModule
        {
            get
            {
                return RuntimeAssembly.GetManifestModule(GetNativeHandle());
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof (object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
                        RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
                        RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "caType");
            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }

        internal static RuntimeAssembly InternalLoadFrom(String assemblyFile, Evidence securityEvidence, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm, bool forIntrospection, bool suppressSecurityChecks, ref StackCrawlMark stackMark)
        {
            if (assemblyFile == null)
                throw new ArgumentNullException("assemblyFile");
                        AssemblyName an = new AssemblyName();
            an.CodeBase = assemblyFile;
            an.SetHashControl(hashValue, hashAlgorithm);
            return InternalLoadAssemblyName(an, securityEvidence, null, ref stackMark, true, forIntrospection, suppressSecurityChecks);
        }

        internal static RuntimeAssembly InternalLoad(String assemblyString, Evidence assemblySecurity, ref StackCrawlMark stackMark, bool forIntrospection)
        {
            return InternalLoad(assemblyString, assemblySecurity, ref stackMark, IntPtr.Zero, forIntrospection);
        }

        internal static RuntimeAssembly InternalLoad(String assemblyString, Evidence assemblySecurity, ref StackCrawlMark stackMark, IntPtr pPrivHostBinder, bool forIntrospection)
        {
            RuntimeAssembly assembly;
            AssemblyName an = CreateAssemblyName(assemblyString, forIntrospection, out assembly);
            if (assembly != null)
            {
                return assembly;
            }

            return InternalLoadAssemblyName(an, assemblySecurity, null, ref stackMark, pPrivHostBinder, true, forIntrospection, false);
        }

        internal static AssemblyName CreateAssemblyName(String assemblyString, bool forIntrospection, out RuntimeAssembly assemblyFromResolveEvent)
        {
            if (assemblyString == null)
                throw new ArgumentNullException("assemblyString");
                        if ((assemblyString.Length == 0) || (assemblyString[0] == '\0'))
                throw new ArgumentException(Environment.GetResourceString("Format_StringZeroLength"));
            if (forIntrospection)
                AppDomain.CheckReflectionOnlyLoadSupported();
            AssemblyName an = new AssemblyName();
            an.Name = assemblyString;
            an.nInit(out assemblyFromResolveEvent, forIntrospection, true);
            return an;
        }

        internal static RuntimeAssembly InternalLoadAssemblyName(AssemblyName assemblyRef, Evidence assemblySecurity, RuntimeAssembly reqAssembly, ref StackCrawlMark stackMark, bool throwOnFileNotFound, bool forIntrospection, bool suppressSecurityChecks)
        {
            return InternalLoadAssemblyName(assemblyRef, assemblySecurity, reqAssembly, ref stackMark, IntPtr.Zero, true, forIntrospection, suppressSecurityChecks);
        }

        internal static RuntimeAssembly InternalLoadAssemblyName(AssemblyName assemblyRef, Evidence assemblySecurity, RuntimeAssembly reqAssembly, ref StackCrawlMark stackMark, IntPtr pPrivHostBinder, bool throwOnFileNotFound, bool forIntrospection, bool suppressSecurityChecks)
        {
            if (assemblyRef == null)
                throw new ArgumentNullException("assemblyRef");
                        if (assemblyRef.CodeBase != null)
            {
                AppDomain.CheckLoadFromSupported();
            }

            assemblyRef = (AssemblyName)assemblyRef.Clone();
            if (!forIntrospection && (assemblyRef.ProcessorArchitecture != ProcessorArchitecture.None))
            {
                assemblyRef.ProcessorArchitecture = ProcessorArchitecture.None;
            }

            if (assemblySecurity != null)
            {
                if (!suppressSecurityChecks)
                {
                    new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
                }
            }

            String codeBase = VerifyCodeBase(assemblyRef.CodeBase);
            if (codeBase != null && !suppressSecurityChecks)
            {
                if (String.Compare(codeBase, 0, s_localFilePrefix, 0, 5, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileName"), "assemblyRef.CodeBase");
                }
                else
                {
                    System.Security.Util.URLString urlString = new System.Security.Util.URLString(codeBase, true);
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery | FileIOPermissionAccess.Read, urlString.GetFileName()).Demand();
                }
            }

            return nLoad(assemblyRef, codeBase, assemblySecurity, reqAssembly, ref stackMark, pPrivHostBinder, throwOnFileNotFound, forIntrospection, suppressSecurityChecks);
        }

        static string[] s_unsafeFrameworkAssemblyNames = new string[]{"System.Reflection.Context", "Microsoft.VisualBasic"};
        internal bool IsFrameworkAssembly()
        {
            ASSEMBLY_FLAGS flags = Flags;
            return (flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_FRAMEWORK) != 0;
        }

        internal bool IsSafeForReflection()
        {
            ASSEMBLY_FLAGS flags = Flags;
            return (flags & ASSEMBLY_FLAGS.ASSEMBLY_FLAGS_SAFE_REFLECTION) != 0;
        }

        private bool IsDesignerBindingContext()
        {
            return RuntimeAssembly.nIsDesignerBindingContext(this);
        }

        private extern static bool nIsDesignerBindingContext(RuntimeAssembly assembly);
        private static extern RuntimeAssembly _nLoad(AssemblyName fileName, String codeBase, Evidence assemblySecurity, RuntimeAssembly locationHint, ref StackCrawlMark stackMark, IntPtr pPrivHostBinder, bool throwOnFileNotFound, bool forIntrospection, bool suppressSecurityChecks);
        private static RuntimeAssembly nLoad(AssemblyName fileName, String codeBase, Evidence assemblySecurity, RuntimeAssembly locationHint, ref StackCrawlMark stackMark, IntPtr pPrivHostBinder, bool throwOnFileNotFound, bool forIntrospection, bool suppressSecurityChecks)
        {
            return _nLoad(fileName, codeBase, assemblySecurity, locationHint, ref stackMark, pPrivHostBinder, throwOnFileNotFound, forIntrospection, suppressSecurityChecks);
        }

        private static extern bool IsReflectionOnly(RuntimeAssembly assembly);
        public override bool ReflectionOnly
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return IsReflectionOnly(GetNativeHandle());
            }
        }

        internal static RuntimeAssembly InternalLoadFromStream(Stream assemblyStream, Stream pdbStream, ref StackCrawlMark stackMark)
        {
            if (assemblyStream == null)
                throw new ArgumentNullException("assemblyStream");
            if (assemblyStream.GetType() != typeof (UnmanagedMemoryStream))
                throw new NotSupportedException();
            if (pdbStream != null && pdbStream.GetType() != typeof (UnmanagedMemoryStream))
                throw new NotSupportedException();
            AppDomain.CheckLoadFromSupported();
            UnmanagedMemoryStream umAssemblyStream = (UnmanagedMemoryStream)assemblyStream;
            UnmanagedMemoryStream umPdbStream = (UnmanagedMemoryStream)pdbStream;
            unsafe
            {
                byte *umAssemblyStreamBuffer = umAssemblyStream.PositionPointer;
                byte *umPdbStreamBuffer = (umPdbStream != null) ? umPdbStream.PositionPointer : null;
                long assemblyDataLength = umAssemblyStream.Length - umAssemblyStream.Position;
                long pdbDataLength = (umPdbStream != null) ? (umPdbStream.Length - umPdbStream.Position) : 0;
                umAssemblyStream.Seek(assemblyDataLength, SeekOrigin.Current);
                if (umPdbStream != null)
                {
                    umPdbStream.Seek(pdbDataLength, SeekOrigin.Current);
                }

                BCLDebug.Assert(assemblyDataLength > 0L, "assemblyDataLength > 0L");
                RuntimeAssembly assembly = null;
                nLoadFromUnmanagedArray(false, umAssemblyStreamBuffer, (ulong)assemblyDataLength, umPdbStreamBuffer, (ulong)pdbDataLength, JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack(ref assembly));
                return assembly;
            }
        }

        private static extern void GetModule(RuntimeAssembly assembly, String name, ObjectHandleOnStack retModule);
        public override Module GetModule(String name)
        {
            Module retModule = null;
            GetModule(GetNativeHandle(), name, JitHelpers.GetObjectHandleOnStack(ref retModule));
            return retModule;
        }

        public override FileStream GetFile(String name)
        {
            RuntimeModule m = (RuntimeModule)GetModule(name);
            if (m == null)
                return null;
            return new FileStream(m.GetFullyQualifiedName(), FileMode.Open, FileAccess.Read, FileShare.Read, FileStream.DefaultBufferSize, false);
        }

        public override FileStream[] GetFiles(bool getResourceModules)
        {
            Module[] m = GetModules(getResourceModules);
            int iLength = m.Length;
            FileStream[] fs = new FileStream[iLength];
            for (int i = 0; i < iLength; i++)
                fs[i] = new FileStream(((RuntimeModule)m[i]).GetFullyQualifiedName(), FileMode.Open, FileAccess.Read, FileShare.Read, FileStream.DefaultBufferSize, false);
            return fs;
        }

        private static extern String[] GetManifestResourceNames(RuntimeAssembly assembly);
        public override String[] GetManifestResourceNames()
        {
            return GetManifestResourceNames(GetNativeHandle());
        }

        private extern static void GetExecutingAssembly(StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);
        internal static RuntimeAssembly GetExecutingAssembly(ref StackCrawlMark stackMark)
        {
            RuntimeAssembly retAssembly = null;
            GetExecutingAssembly(JitHelpers.GetStackCrawlMarkHandle(ref stackMark), JitHelpers.GetObjectHandleOnStack(ref retAssembly));
            return retAssembly;
        }

        private static extern AssemblyName[] GetReferencedAssemblies(RuntimeAssembly assembly);
        public override AssemblyName[] GetReferencedAssemblies()
        {
            return GetReferencedAssemblies(GetNativeHandle());
        }

        private static extern int GetManifestResourceInfo(RuntimeAssembly assembly, String resourceName, ObjectHandleOnStack assemblyRef, StringHandleOnStack retFileName, StackCrawlMarkHandle stackMark);
        public override ManifestResourceInfo GetManifestResourceInfo(String resourceName)
        {
            RuntimeAssembly retAssembly = null;
            String fileName = null;
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            int location = GetManifestResourceInfo(GetNativeHandle(), resourceName, JitHelpers.GetObjectHandleOnStack(ref retAssembly), JitHelpers.GetStringHandleOnStack(ref fileName), JitHelpers.GetStackCrawlMarkHandle(ref stackMark));
            if (location == -1)
                return null;
            return new ManifestResourceInfo(retAssembly, fileName, (ResourceLocation)location);
        }

        private static extern void GetLocation(RuntimeAssembly assembly, StringHandleOnStack retString);
        public override String Location
        {
            [System.Security.SecurityCritical]
            get
            {
                String location = null;
                GetLocation(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref location));
                if (location != null)
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, location).Demand();
                return location;
            }
        }

        private extern static void GetImageRuntimeVersion(RuntimeAssembly assembly, StringHandleOnStack retString);
        public override String ImageRuntimeVersion
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String s = null;
                GetImageRuntimeVersion(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref s));
                return s;
            }
        }

        private extern static bool IsGlobalAssemblyCache(RuntimeAssembly assembly);
        public override bool GlobalAssemblyCache
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return IsGlobalAssemblyCache(GetNativeHandle());
            }
        }

        private extern static Int64 GetHostContext(RuntimeAssembly assembly);
        public override Int64 HostContext
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return GetHostContext(GetNativeHandle());
            }
        }

        private static String VerifyCodeBase(String codebase)
        {
            if (codebase == null)
                return null;
            int len = codebase.Length;
            if (len == 0)
                return null;
            int j = codebase.IndexOf(':');
            if ((j != -1) && (j + 2 < len) && ((codebase[j + 1] == '/') || (codebase[j + 1] == '\\')) && ((codebase[j + 2] == '/') || (codebase[j + 2] == '\\')))
                return codebase;
            else if ((len > 2) && (codebase[0] == '\\') && (codebase[1] == '\\'))
                return "file://" + codebase;
            else
                return "file:///" + Path.GetFullPathInternal(codebase);
        }

        internal Stream GetManifestResourceStream(Type type, String name, bool skipSecurityCheck, ref StackCrawlMark stackMark)
        {
            StringBuilder sb = new StringBuilder();
            if (type == null)
            {
                if (name == null)
                    throw new ArgumentNullException("type");
            }
            else
            {
                String nameSpace = type.Namespace;
                if (nameSpace != null)
                {
                    sb.Append(nameSpace);
                    if (name != null)
                        sb.Append(Type.Delimiter);
                }
            }

            if (name != null)
                sb.Append(name);
            return GetManifestResourceStream(sb.ToString(), ref stackMark, skipSecurityCheck);
        }

        private static unsafe extern byte *GetResource(RuntimeAssembly assembly, String resourceName, out ulong length, StackCrawlMarkHandle stackMark, bool skipSecurityCheck);
        internal unsafe Stream GetManifestResourceStream(String name, ref StackCrawlMark stackMark, bool skipSecurityCheck)
        {
            ulong length = 0;
            byte *pbInMemoryResource = GetResource(GetNativeHandle(), name, out length, JitHelpers.GetStackCrawlMarkHandle(ref stackMark), skipSecurityCheck);
            if (pbInMemoryResource != null)
            {
                if (length > Int64.MaxValue)
                    throw new NotImplementedException(Environment.GetResourceString("NotImplemented_ResourcesLongerThan2^63"));
                return new UnmanagedMemoryStream(pbInMemoryResource, (long)length, (long)length, FileAccess.Read, true);
            }

            return null;
        }

        private static extern void GetVersion(RuntimeAssembly assembly, out int majVer, out int minVer, out int buildNum, out int revNum);
        internal Version GetVersion()
        {
            int majorVer, minorVer, build, revision;
            GetVersion(GetNativeHandle(), out majorVer, out minorVer, out build, out revision);
            return new Version(majorVer, minorVer, build, revision);
        }

        private static extern void GetLocale(RuntimeAssembly assembly, StringHandleOnStack retString);
        internal CultureInfo GetLocale()
        {
            String locale = null;
            GetLocale(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref locale));
            if (locale == null)
                return CultureInfo.InvariantCulture;
            return new CultureInfo(locale);
        }

        private static extern bool FCallIsDynamic(RuntimeAssembly assembly);
        public override bool IsDynamic
        {
            [SecuritySafeCritical]
            get
            {
                return FCallIsDynamic(GetNativeHandle());
            }
        }

        private void VerifyCodeBaseDiscovery(String codeBase)
        {
            if ((codeBase != null) && (String.Compare(codeBase, 0, s_localFilePrefix, 0, 5, StringComparison.OrdinalIgnoreCase) == 0))
            {
                System.Security.Util.URLString urlString = new System.Security.Util.URLString(codeBase, true);
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery, urlString.GetFileName()).Demand();
            }
        }

        private static extern void GetSimpleName(RuntimeAssembly assembly, StringHandleOnStack retSimpleName);
        internal String GetSimpleName()
        {
            string name = null;
            GetSimpleName(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref name));
            return name;
        }

        private extern static AssemblyHashAlgorithm GetHashAlgorithm(RuntimeAssembly assembly);
        private AssemblyHashAlgorithm GetHashAlgorithm()
        {
            return GetHashAlgorithm(GetNativeHandle());
        }

        private extern static AssemblyNameFlags GetFlags(RuntimeAssembly assembly);
        private AssemblyNameFlags GetFlags()
        {
            return GetFlags(GetNativeHandle());
        }

        private static extern void GetRawBytes(RuntimeAssembly assembly, ObjectHandleOnStack retRawBytes);
        internal byte[] GetRawBytes()
        {
            byte[] rawBytes = null;
            GetRawBytes(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref rawBytes));
            return rawBytes;
        }

        private static extern void GetPublicKey(RuntimeAssembly assembly, ObjectHandleOnStack retPublicKey);
        internal byte[] GetPublicKey()
        {
            byte[] publicKey = null;
            GetPublicKey(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref publicKey));
            return publicKey;
        }

        private extern static void GetGrantSet(RuntimeAssembly assembly, ObjectHandleOnStack granted, ObjectHandleOnStack denied);
        internal void GetGrantSet(out PermissionSet newGrant, out PermissionSet newDenied)
        {
            PermissionSet granted = null, denied = null;
            GetGrantSet(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref granted), JitHelpers.GetObjectHandleOnStack(ref denied));
            newGrant = granted;
            newDenied = denied;
        }

        internal static extern bool GetIsProfileAssembly(RuntimeAssembly assembly);
        internal override bool IsProfileAssembly
        {
            [System.Security.SecurityCritical]
            get
            {
                return GetIsProfileAssembly(GetNativeHandle());
            }
        }

        private extern static bool IsAllSecurityCritical(RuntimeAssembly assembly);
        internal bool IsAllSecurityCritical()
        {
            return IsAllSecurityCritical(GetNativeHandle());
        }

        private extern static bool IsAllSecuritySafeCritical(RuntimeAssembly assembly);
        internal bool IsAllSecuritySafeCritical()
        {
            return IsAllSecuritySafeCritical(GetNativeHandle());
        }

        private extern static bool IsAllPublicAreaSecuritySafeCritical(RuntimeAssembly assembly);
        internal bool IsAllPublicAreaSecuritySafeCritical()
        {
            return IsAllPublicAreaSecuritySafeCritical(GetNativeHandle());
        }

        private extern static bool IsAllSecurityTransparent(RuntimeAssembly assembly);
        internal bool IsAllSecurityTransparent()
        {
            return IsAllSecurityTransparent(GetNativeHandle());
        }

        private RuntimeModule OnModuleResolveEvent(String moduleName)
        {
            ModuleResolveEventHandler moduleResolve = _ModuleResolve;
            if (moduleResolve == null)
                return null;
            Delegate[] ds = moduleResolve.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++)
            {
                RuntimeModule ret = (RuntimeModule)((ModuleResolveEventHandler)ds[i])(this, new ResolveEventArgs(moduleName, this));
                if (ret != null)
                    return ret;
            }

            return null;
        }

        public override Assembly GetSatelliteAssembly(CultureInfo culture)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetSatelliteAssembly(culture, null, ref stackMark);
        }

        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version version)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetSatelliteAssembly(culture, version, ref stackMark);
        }

        internal Assembly InternalGetSatelliteAssembly(CultureInfo culture, Version version, ref StackCrawlMark stackMark)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");
                        String name = GetSimpleName() + ".resources";
            return InternalGetSatelliteAssembly(name, culture, version, true, ref stackMark);
        }

        internal RuntimeAssembly InternalGetSatelliteAssembly(String name, CultureInfo culture, Version version, bool throwOnFileNotFound, ref StackCrawlMark stackMark)
        {
            AssemblyName an = new AssemblyName();
            an.SetPublicKey(GetPublicKey());
            an.Flags = GetFlags() | AssemblyNameFlags.PublicKey;
            if (version == null)
                an.Version = GetVersion();
            else
                an.Version = version;
            an.CultureInfo = culture;
            an.Name = name;
            RuntimeAssembly retAssembly = null;
            {
                retAssembly = nLoad(an, null, null, this, ref stackMark, IntPtr.Zero, throwOnFileNotFound, false, false);
            }

            if (retAssembly == this || (retAssembly == null && throwOnFileNotFound))
            {
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    if (retAssembly == this)
                    {
                        if (throwOnFileNotFound)
                            throw new FileNotFoundException();
                        return null;
                    }
                }

                throw new FileNotFoundException(String.Format(culture, Environment.GetResourceString("IO.FileNotFound_FileName"), an.Name));
            }

            return retAssembly;
        }

        private RuntimeAssembly InternalProbeForSatelliteAssemblyNextToParentAssembly(AssemblyName an, String name, String codeBase, CultureInfo culture, bool throwOnFileNotFound, bool useLoadFile, ref StackCrawlMark stackMark)
        {
            RuntimeAssembly retAssembly = null;
            String location = null;
            if (useLoadFile)
                location = Location;
            FileNotFoundException dllNotFoundException = null;
            StringBuilder assemblyFile = new StringBuilder(useLoadFile ? location : codeBase, 0, useLoadFile ? location.LastIndexOf('\\') + 1 : codeBase.LastIndexOf('/') + 1, Path.MaxPath);
            assemblyFile.Append(an.CultureInfo.Name);
            assemblyFile.Append(useLoadFile ? '\\' : '/');
            assemblyFile.Append(name);
            assemblyFile.Append(".DLL");
            string fileNameOrCodeBase = assemblyFile.ToString();
            AssemblyName loadFromAsmName = null;
            if (useLoadFile == false)
            {
                loadFromAsmName = new AssemblyName();
                loadFromAsmName.CodeBase = fileNameOrCodeBase;
            }

            try
            {
                try
                {
                    retAssembly = useLoadFile ? nLoadFile(fileNameOrCodeBase, null) : nLoad(loadFromAsmName, fileNameOrCodeBase, null, this, ref stackMark, IntPtr.Zero, throwOnFileNotFound, false, false);
                }
                catch (FileNotFoundException)
                {
                    dllNotFoundException = new FileNotFoundException(String.Format(culture, Environment.GetResourceString("IO.FileNotFound_FileName"), fileNameOrCodeBase), fileNameOrCodeBase);
                    retAssembly = null;
                }

                if (retAssembly == null)
                {
                    BCLDebug.Assert((useLoadFile == false && throwOnFileNotFound == false) || dllNotFoundException != null, "(useLoadFile == false && throwOnFileNotFound == false) || dllNotFoundException != null");
                    assemblyFile.Remove(assemblyFile.Length - 4, 4);
                    assemblyFile.Append(".EXE");
                    fileNameOrCodeBase = assemblyFile.ToString();
                    if (useLoadFile == false)
                        loadFromAsmName.CodeBase = fileNameOrCodeBase;
                    try
                    {
                        retAssembly = useLoadFile ? nLoadFile(fileNameOrCodeBase, null) : nLoad(loadFromAsmName, fileNameOrCodeBase, null, this, ref stackMark, IntPtr.Zero, false, false, false);
                    }
                    catch (FileNotFoundException)
                    {
                        retAssembly = null;
                    }

                    if (retAssembly == null && throwOnFileNotFound)
                        throw dllNotFoundException;
                }
            }
            catch (DirectoryNotFoundException)
            {
                if (throwOnFileNotFound)
                    throw;
                retAssembly = null;
            }

            return retAssembly;
        }

        static internal extern RuntimeAssembly nLoadFile(String path, Evidence evidence);
        static internal extern RuntimeAssembly nLoadImage(byte[] rawAssembly, byte[] rawSymbolStore, Evidence evidence, ref StackCrawlMark stackMark, bool fIntrospection, SecurityContextSource securityContextSource);
        static internal extern unsafe void nLoadFromUnmanagedArray(bool fIntrospection, byte *assemblyContent, ulong assemblySize, byte *pdbContent, ulong pdbSize, StackCrawlMarkHandle stackMark, ObjectHandleOnStack retAssembly);
        private extern static void GetModules(RuntimeAssembly assembly, bool loadIfNotFound, bool getResourceModules, ObjectHandleOnStack retModuleHandles);
        private RuntimeModule[] GetModulesInternal(bool loadIfNotFound, bool getResourceModules)
        {
            RuntimeModule[] modules = null;
            GetModules(GetNativeHandle(), loadIfNotFound, getResourceModules, JitHelpers.GetObjectHandleOnStack(ref modules));
            return modules;
        }

        public override Module[] GetModules(bool getResourceModules)
        {
            return GetModulesInternal(true, getResourceModules);
        }

        public override Module[] GetLoadedModules(bool getResourceModules)
        {
            return GetModulesInternal(false, getResourceModules);
        }

        internal static extern RuntimeModule GetManifestModule(RuntimeAssembly assembly);
        internal static extern int GetToken(RuntimeAssembly assembly);
    }
}