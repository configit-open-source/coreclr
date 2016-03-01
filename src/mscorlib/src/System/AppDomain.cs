using System.Collections;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;

namespace System
{
    public class ResolveEventArgs : EventArgs
    {
        private String _Name;
        private Assembly _RequestingAssembly;
        public String Name
        {
            get
            {
                return _Name;
            }
        }

        public Assembly RequestingAssembly
        {
            get
            {
                return _RequestingAssembly;
            }
        }

        public ResolveEventArgs(String name)
        {
            _Name = name;
        }

        public ResolveEventArgs(String name, Assembly requestingAssembly)
        {
            _Name = name;
            _RequestingAssembly = requestingAssembly;
        }
    }

    public class AssemblyLoadEventArgs : EventArgs
    {
        private Assembly _LoadedAssembly;
        public Assembly LoadedAssembly
        {
            get
            {
                return _LoadedAssembly;
            }
        }

        public AssemblyLoadEventArgs(Assembly loadedAssembly)
        {
            _LoadedAssembly = loadedAssembly;
        }
    }

    public delegate Assembly ResolveEventHandler(Object sender, ResolveEventArgs args);
    public delegate void AssemblyLoadEventHandler(Object sender, AssemblyLoadEventArgs args);
    public delegate void AppDomainInitializer(string[] args);
    internal class AppDomainInitializerInfo
    {
        internal class ItemInfo
        {
            public string TargetTypeAssembly;
            public string TargetTypeName;
            public string MethodName;
        }

        internal ItemInfo[] Info;
        internal AppDomainInitializerInfo(AppDomainInitializer init)
        {
            Info = null;
            if (init == null)
                return;
            List<ItemInfo> itemInfo = new List<ItemInfo>();
            List<AppDomainInitializer> nestedDelegates = new List<AppDomainInitializer>();
            nestedDelegates.Add(init);
            int idx = 0;
            while (nestedDelegates.Count > idx)
            {
                AppDomainInitializer curr = nestedDelegates[idx++];
                Delegate[] list = curr.GetInvocationList();
                for (int i = 0; i < list.Length; i++)
                {
                    if (!list[i].Method.IsStatic)
                    {
                        if (list[i].Target == null)
                            continue;
                        AppDomainInitializer nested = list[i].Target as AppDomainInitializer;
                        if (nested != null)
                            nestedDelegates.Add(nested);
                        else
                            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeStatic"), list[i].Method.ReflectedType.FullName + "::" + list[i].Method.Name);
                    }
                    else
                    {
                        ItemInfo info = new ItemInfo();
                        info.TargetTypeAssembly = list[i].Method.ReflectedType.Module.Assembly.FullName;
                        info.TargetTypeName = list[i].Method.ReflectedType.FullName;
                        info.MethodName = list[i].Method.Name;
                        itemInfo.Add(info);
                    }
                }
            }

            Info = itemInfo.ToArray();
        }

        internal AppDomainInitializer Unwrap()
        {
            if (Info == null)
                return null;
            AppDomainInitializer retVal = null;
            new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
            for (int i = 0; i < Info.Length; i++)
            {
                Assembly assembly = Assembly.Load(Info[i].TargetTypeAssembly);
                AppDomainInitializer newVal = (AppDomainInitializer)Delegate.CreateDelegate(typeof (AppDomainInitializer), assembly.GetType(Info[i].TargetTypeName), Info[i].MethodName);
                if (retVal == null)
                    retVal = newVal;
                else
                    retVal += newVal;
            }

            return retVal;
        }
    }

    public sealed class AppDomain : _AppDomain, IEvidenceFactory
    {
        private AppDomainManager _domainManager;
        private Dictionary<String, Object[]> _LocalStore;
        private AppDomainSetup _FusionStore;
        private Evidence _SecurityIdentity;
        private Object[] _Policies;
        public event AssemblyLoadEventHandler AssemblyLoad;
        private ResolveEventHandler _TypeResolve;
        public event ResolveEventHandler TypeResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                lock (this)
                {
                    _TypeResolve += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (this)
                {
                    _TypeResolve -= value;
                }
            }
        }

        private ResolveEventHandler _ResourceResolve;
        public event ResolveEventHandler ResourceResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                lock (this)
                {
                    _ResourceResolve += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (this)
                {
                    _ResourceResolve -= value;
                }
            }
        }

        private ResolveEventHandler _AssemblyResolve;
        public event ResolveEventHandler AssemblyResolve
        {
            [System.Security.SecurityCritical]
            add
            {
                lock (this)
                {
                    _AssemblyResolve += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (this)
                {
                    _AssemblyResolve -= value;
                }
            }
        }

        private ApplicationTrust _applicationTrust;
        private EventHandler _processExit;
        private EventHandler _domainUnload;
        private UnhandledExceptionEventHandler _unhandledException;
        private Dictionary<String, object> _compatFlags;
        private IntPtr _pDomain;
        private bool _HasSetPolicy;
        private bool _IsFastFullTrustDomain;
        private bool _compatFlagsInitialized;
        internal const String TargetFrameworkNameAppCompatSetting = "TargetFrameworkName";
        private static APPX_FLAGS s_flags;
        [Flags]
        private enum APPX_FLAGS
        {
            APPX_FLAGS_INITIALIZED = 0x01,
            APPX_FLAGS_APPX_MODEL = 0x02,
            APPX_FLAGS_APPX_DESIGN_MODE = 0x04,
            APPX_FLAGS_APPX_NGEN = 0x08,
            APPX_FLAGS_APPX_MASK = APPX_FLAGS_APPX_MODEL | APPX_FLAGS_APPX_DESIGN_MODE | APPX_FLAGS_APPX_NGEN,
            APPX_FLAGS_API_CHECK = 0x10
        }

        private static APPX_FLAGS Flags
        {
            [SecuritySafeCritical]
            get
            {
                if (s_flags == 0)
                    s_flags = nGetAppXFlags();
                Contract.Assert(s_flags != 0);
                return s_flags;
            }
        }

        internal static bool ProfileAPICheck
        {
            [SecuritySafeCritical]
            get
            {
                return (Flags & APPX_FLAGS.APPX_FLAGS_API_CHECK) != 0;
            }
        }

        internal static bool IsAppXNGen
        {
            [SecuritySafeCritical]
            get
            {
                return (Flags & APPX_FLAGS.APPX_FLAGS_APPX_NGEN) != 0;
            }
        }

        private static extern bool DisableFusionUpdatesFromADManager(AppDomainHandle domain);
        private static extern APPX_FLAGS nGetAppXFlags();
        private static extern void GetAppDomainManagerType(AppDomainHandle domain, StringHandleOnStack retAssembly, StringHandleOnStack retType);
        private static extern void SetAppDomainManagerType(AppDomainHandle domain, string assembly, string type);
        private static extern void nSetHostSecurityManagerFlags(HostSecurityManagerOptions flags);
        private static extern void SetSecurityHomogeneousFlag(AppDomainHandle domain, [MarshalAs(UnmanagedType.Bool)] bool runtimeSuppliedHomogenousGrantSet);
        internal AppDomainHandle GetNativeHandle()
        {
            if (_pDomain.IsNull())
            {
                throw new InvalidOperationException(Environment.GetResourceString("Argument_InvalidHandle"));
            }

            return new AppDomainHandle(_pDomain);
        }

        private void CreateAppDomainManager()
        {
            Contract.Assert(_domainManager == null, "_domainManager == null");
            AppDomainSetup adSetup = FusionStore;
            String trustedPlatformAssemblies = (String)(GetData("TRUSTED_PLATFORM_ASSEMBLIES"));
            if (trustedPlatformAssemblies != null)
            {
                String platformResourceRoots = (String)(GetData("PLATFORM_RESOURCE_ROOTS"));
                if (platformResourceRoots == null)
                {
                    platformResourceRoots = String.Empty;
                }

                String appPaths = (String)(GetData("APP_PATHS"));
                if (appPaths == null)
                {
                    appPaths = String.Empty;
                }

                String appNiPaths = (String)(GetData("APP_NI_PATHS"));
                if (appNiPaths == null)
                {
                    appNiPaths = String.Empty;
                }

                String appLocalWinMD = (String)(GetData("APP_LOCAL_WINMETADATA"));
                if (appLocalWinMD == null)
                {
                    appLocalWinMD = String.Empty;
                }

                SetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPaths, appNiPaths, appLocalWinMD);
            }

            string domainManagerAssembly;
            string domainManagerType;
            GetAppDomainManagerType(out domainManagerAssembly, out domainManagerType);
            if (domainManagerAssembly != null && domainManagerType != null)
            {
                try
                {
                    new PermissionSet(PermissionState.Unrestricted).Assert();
                    _domainManager = CreateInstanceAndUnwrap(domainManagerAssembly, domainManagerType) as AppDomainManager;
                    CodeAccessPermission.RevertAssert();
                }
                catch (FileNotFoundException e)
                {
                    throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), e);
                }
                catch (SecurityException e)
                {
                    throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), e);
                }
                catch (TypeLoadException e)
                {
                    throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"), e);
                }

                if (_domainManager == null)
                {
                    throw new TypeLoadException(Environment.GetResourceString("Argument_NoDomainManager"));
                }

                FusionStore.AppDomainManagerAssembly = domainManagerAssembly;
                FusionStore.AppDomainManagerType = domainManagerType;
                bool notifyFusion = _domainManager.GetType() != typeof (System.AppDomainManager) && !DisableFusionUpdatesFromADManager();
                AppDomainSetup FusionStoreOld = null;
                if (notifyFusion)
                    FusionStoreOld = new AppDomainSetup(FusionStore, true);
                _domainManager.InitializeNewDomain(FusionStore);
                if (notifyFusion)
                    SetupFusionStore(_FusionStore, FusionStoreOld);
            }

            InitializeCompatibilityFlags();
        }

        private void InitializeCompatibilityFlags()
        {
            AppDomainSetup adSetup = FusionStore;
            if (adSetup.GetCompatibilityFlags() != null)
            {
                _compatFlags = new Dictionary<String, object>(adSetup.GetCompatibilityFlags(), StringComparer.OrdinalIgnoreCase);
            }

            Contract.Assert(!_compatFlagsInitialized);
            _compatFlagsInitialized = true;
            CompatibilitySwitches.InitializeSwitches();
        }

        internal String GetTargetFrameworkName()
        {
            String targetFrameworkName = _FusionStore.TargetFrameworkName;
            if (targetFrameworkName == null && IsDefaultAppDomain() && !_FusionStore.CheckedForTargetFrameworkName)
            {
                Assembly assembly = Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    TargetFrameworkAttribute[] attrs = (TargetFrameworkAttribute[])assembly.GetCustomAttributes(typeof (TargetFrameworkAttribute));
                    if (attrs != null && attrs.Length > 0)
                    {
                        Contract.Assert(attrs.Length == 1);
                        targetFrameworkName = attrs[0].FrameworkName;
                        _FusionStore.TargetFrameworkName = targetFrameworkName;
                    }
                }

                _FusionStore.CheckedForTargetFrameworkName = true;
            }

            return targetFrameworkName;
        }

        internal bool DisableFusionUpdatesFromADManager()
        {
            return DisableFusionUpdatesFromADManager(GetNativeHandle());
        }

        internal static bool IsAppXModel()
        {
            return (Flags & APPX_FLAGS.APPX_FLAGS_APPX_MODEL) != 0;
        }

        internal static bool IsAppXDesignMode()
        {
            return (Flags & APPX_FLAGS.APPX_FLAGS_APPX_MASK) == (APPX_FLAGS.APPX_FLAGS_APPX_MODEL | APPX_FLAGS.APPX_FLAGS_APPX_DESIGN_MODE);
        }

        internal static void CheckLoadFromSupported()
        {
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.LoadFrom"));
        }

        internal static void CheckLoadFileSupported()
        {
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.LoadFile"));
        }

        internal static void CheckReflectionOnlyLoadSupported()
        {
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.ReflectionOnlyLoad"));
        }

        internal static void CheckLoadWithPartialNameSupported(StackCrawlMark stackMark)
        {
            if (IsAppXModel())
            {
                RuntimeAssembly callingAssembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                bool callerIsFxAssembly = callingAssembly != null && callingAssembly.IsFrameworkAssembly();
                if (!callerIsFxAssembly)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.LoadWithPartialName"));
                }
            }
        }

        internal static void CheckDefinePInvokeSupported()
        {
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "DefinePInvokeMethod"));
        }

        internal static void CheckLoadByteArraySupported()
        {
            if (IsAppXModel())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "Assembly.Load(byte[], ...)"));
        }

        internal static void CheckCreateDomainSupported()
        {
            if (IsAppXModel())
            {
                if (!IsAppXDesignMode())
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AppX", "AppDomain.CreateDomain"));
                }
            }
        }

        internal void GetAppDomainManagerType(out string assembly, out string type)
        {
            string localAssembly = null;
            string localType = null;
            GetAppDomainManagerType(GetNativeHandle(), JitHelpers.GetStringHandleOnStack(ref localAssembly), JitHelpers.GetStringHandleOnStack(ref localType));
            assembly = localAssembly;
            type = localType;
        }

        private void SetAppDomainManagerType(string assembly, string type)
        {
            Contract.Assert(assembly != null, "assembly != null");
            Contract.Assert(type != null, "type != null");
            SetAppDomainManagerType(GetNativeHandle(), assembly, type);
        }

        private void InitializeDomainSecurity(Evidence providedSecurityInfo, Evidence creatorsSecurityInfo, bool generateDefaultEvidence, IntPtr parentSecurityDescriptor, bool publishAppDomain)
        {
            AppDomainSetup adSetup = FusionStore;
            {
                bool runtimeSuppliedHomogenousGrant = false;
                ApplicationTrust appTrust = adSetup.ApplicationTrust;
                if (appTrust != null)
                {
                    SetupDomainSecurityForHomogeneousDomain(appTrust, runtimeSuppliedHomogenousGrant);
                }
                else if (_IsFastFullTrustDomain)
                {
                    SetSecurityHomogeneousFlag(GetNativeHandle(), runtimeSuppliedHomogenousGrant);
                }
            }

            Evidence newAppDomainEvidence = (providedSecurityInfo != null ? providedSecurityInfo : creatorsSecurityInfo);
            if (newAppDomainEvidence == null && generateDefaultEvidence)
            {
                newAppDomainEvidence = new Evidence();
            }

            _SecurityIdentity = newAppDomainEvidence;
            SetupDomainSecurity(newAppDomainEvidence, parentSecurityDescriptor, publishAppDomain);
        }

        private void SetupDomainSecurityForHomogeneousDomain(ApplicationTrust appTrust, bool runtimeSuppliedHomogenousGrantSet)
        {
            if (runtimeSuppliedHomogenousGrantSet)
            {
                BCLDebug.Assert(_FusionStore.ApplicationTrust != null, "Expected to find runtime supplied ApplicationTrust");
            }

            _applicationTrust = appTrust;
            SetSecurityHomogeneousFlag(GetNativeHandle(), runtimeSuppliedHomogenousGrantSet);
        }

        public AppDomainManager DomainManager
        {
            [System.Security.SecurityCritical]
            get
            {
                return _domainManager;
            }
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, null, null, null, null, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, null, null, null, null, ref stackMark, assemblyAttributes, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> assemblyAttributes, SecurityContextSource securityContextSource)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, null, null, null, null, ref stackMark, assemblyAttributes, securityContextSource);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, null, null, null, null, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, Evidence evidence)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, evidence, null, null, null, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, null, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir, Evidence evidence)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, evidence, null, null, null, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, null, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, null, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, bool isSynchronized)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, null, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, bool isSynchronized, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, assemblyAttributes, SecurityContextSource.CurrentAssembly);
        }

        public AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir, bool isSynchronized, IEnumerable<CustomAttributeBuilder> assemblyAttributes)
        {
            Contract.Ensures(Contract.Result<AssemblyBuilder>() != null);
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalDefineDynamicAssembly(name, access, dir, null, null, null, null, ref stackMark, assemblyAttributes, SecurityContextSource.CurrentAssembly);
        }

        private AssemblyBuilder InternalDefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access, String dir, Evidence evidence, PermissionSet requiredPermissions, PermissionSet optionalPermissions, PermissionSet refusedPermissions, ref StackCrawlMark stackMark, IEnumerable<CustomAttributeBuilder> assemblyAttributes, SecurityContextSource securityContextSource)
        {
            return AssemblyBuilder.InternalDefineDynamicAssembly(name, access, dir, evidence, requiredPermissions, optionalPermissions, refusedPermissions, ref stackMark, assemblyAttributes, securityContextSource);
        }

        private extern String nApplyPolicy(AssemblyName an);
        public String ApplyPolicy(String assemblyName)
        {
            AssemblyName asmName = new AssemblyName(assemblyName);
            byte[] pk = asmName.GetPublicKeyToken();
            if (pk == null)
                pk = asmName.GetPublicKey();
            if ((pk == null) || (pk.Length == 0))
                return assemblyName;
            else
                return nApplyPolicy(asmName);
        }

        public ObjectHandle CreateInstance(String assemblyName, String typeName)
        {
            if (this == null)
                throw new NullReferenceException();
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();
            return Activator.CreateInstance(assemblyName, typeName);
        }

        internal ObjectHandle InternalCreateInstanceWithNoSecurity(string assemblyName, string typeName)
        {
            PermissionSet.s_fullTrust.Assert();
            return CreateInstance(assemblyName, typeName);
        }

        public ObjectHandle CreateInstanceFrom(String assemblyFile, String typeName)
        {
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();
            return Activator.CreateInstanceFrom(assemblyFile, typeName);
        }

        internal ObjectHandle InternalCreateInstanceFromWithNoSecurity(string assemblyName, string typeName)
        {
            PermissionSet.s_fullTrust.Assert();
            return CreateInstanceFrom(assemblyName, typeName);
        }

        public ObjectHandle CreateComInstanceFrom(String assemblyName, String typeName)
        {
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();
            return Activator.CreateComInstanceFrom(assemblyName, typeName);
        }

        public ObjectHandle CreateComInstanceFrom(String assemblyFile, String typeName, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
        {
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();
            return Activator.CreateComInstanceFrom(assemblyFile, typeName, hashValue, hashAlgorithm);
        }

        public ObjectHandle CreateInstance(String assemblyName, String typeName, Object[] activationAttributes)
        {
            if (this == null)
                throw new NullReferenceException();
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();
            return Activator.CreateInstance(assemblyName, typeName, activationAttributes);
        }

        public ObjectHandle CreateInstanceFrom(String assemblyFile, String typeName, Object[] activationAttributes)
        {
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();
            return Activator.CreateInstanceFrom(assemblyFile, typeName, activationAttributes);
        }

        public ObjectHandle CreateInstance(String assemblyName, String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityAttributes)
        {
            if (this == null)
                throw new NullReferenceException();
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();
            return Activator.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        public ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            if (this == null)
                throw new NullReferenceException();
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName");
            Contract.EndContractBlock();
            return Activator.CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
        }

        internal ObjectHandle InternalCreateInstanceWithNoSecurity(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityAttributes)
        {
            PermissionSet.s_fullTrust.Assert();
            return CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        public ObjectHandle CreateInstanceFrom(String assemblyFile, String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityAttributes)
        {
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();
            return Activator.CreateInstanceFrom(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        public ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            if (this == null)
                throw new NullReferenceException();
            Contract.EndContractBlock();
            return Activator.CreateInstanceFrom(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
        }

        internal ObjectHandle InternalCreateInstanceFromWithNoSecurity(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityAttributes)
        {
            PermissionSet.s_fullTrust.Assert();
            return CreateInstanceFrom(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        public Assembly Load(AssemblyName assemblyRef)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, null, null, ref stackMark, true, false, false);
        }

        public Assembly Load(String assemblyString)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, null, ref stackMark, false);
        }

        public Assembly Load(byte[] rawAssembly)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly, null, null, ref stackMark, false, SecurityContextSource.CurrentAssembly);
        }

        public Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, null, ref stackMark, false, SecurityContextSource.CurrentAssembly);
        }

        public Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore, Evidence securityEvidence)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.nLoadImage(rawAssembly, rawSymbolStore, securityEvidence, ref stackMark, false, SecurityContextSource.CurrentAssembly);
        }

        public Assembly Load(AssemblyName assemblyRef, Evidence assemblySecurity)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoadAssemblyName(assemblyRef, assemblySecurity, null, ref stackMark, true, false, false);
        }

        public Assembly Load(String assemblyString, Evidence assemblySecurity)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeAssembly.InternalLoad(assemblyString, assemblySecurity, ref stackMark, false);
        }

        public int ExecuteAssembly(String assemblyFile)
        {
            return ExecuteAssembly(assemblyFile, (string[])null);
        }

        public int ExecuteAssembly(String assemblyFile, Evidence assemblySecurity)
        {
            return ExecuteAssembly(assemblyFile, assemblySecurity, null);
        }

        public int ExecuteAssembly(String assemblyFile, Evidence assemblySecurity, String[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile, assemblySecurity);
            if (args == null)
                args = new String[0];
            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssembly(string assemblyFile, string[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile);
            if (args == null)
                args = new String[0];
            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssembly(String assemblyFile, Evidence assemblySecurity, String[] args, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile, assemblySecurity, hashValue, hashAlgorithm);
            if (args == null)
                args = new String[0];
            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssembly(string assemblyFile, string[] args, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.LoadFrom(assemblyFile, hashValue, hashAlgorithm);
            if (args == null)
                args = new String[0];
            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssemblyByName(String assemblyName)
        {
            return ExecuteAssemblyByName(assemblyName, (string[])null);
        }

        public int ExecuteAssemblyByName(String assemblyName, Evidence assemblySecurity)
        {
            return ExecuteAssemblyByName(assemblyName, assemblySecurity, null);
        }

        public int ExecuteAssemblyByName(String assemblyName, Evidence assemblySecurity, params String[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName, assemblySecurity);
            if (args == null)
                args = new String[0];
            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssemblyByName(string assemblyName, params string[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName);
            if (args == null)
                args = new String[0];
            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssemblyByName(AssemblyName assemblyName, Evidence assemblySecurity, params String[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName, assemblySecurity);
            if (args == null)
                args = new String[0];
            return nExecuteAssembly(assembly, args);
        }

        public int ExecuteAssemblyByName(AssemblyName assemblyName, params string[] args)
        {
            RuntimeAssembly assembly = (RuntimeAssembly)Assembly.Load(assemblyName);
            if (args == null)
                args = new String[0];
            return nExecuteAssembly(assembly, args);
        }

        public static AppDomain CurrentDomain
        {
            get
            {
                Contract.Ensures(Contract.Result<AppDomain>() != null);
                return Thread.GetDomain();
            }
        }

        public String FriendlyName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return nGetFriendlyName();
            }
        }

        public String BaseDirectory
        {
            [System.Security.SecurityCritical]
            get
            {
                return FusionStore.ApplicationBase;
            }
        }

        public override String ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            String fn = nGetFriendlyName();
            if (fn != null)
            {
                sb.Append(Environment.GetResourceString("Loader_Name") + fn);
                sb.Append(Environment.NewLine);
            }

            if (_Policies == null || _Policies.Length == 0)
                sb.Append(Environment.GetResourceString("Loader_NoContextPolicies") + Environment.NewLine);
            else
            {
                sb.Append(Environment.GetResourceString("Loader_ContextPolicies") + Environment.NewLine);
                for (int i = 0; i < _Policies.Length; i++)
                {
                    sb.Append(_Policies[i]);
                    sb.Append(Environment.NewLine);
                }
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public Assembly[] GetAssemblies()
        {
            return nGetAssemblies(false);
        }

        public Assembly[] ReflectionOnlyGetAssemblies()
        {
            return nGetAssemblies(true);
        }

        private extern Assembly[] nGetAssemblies(bool forIntrospection);
        internal extern bool IsUnloadingForcedFinalize();
        public extern bool IsFinalizingForUnload();
        internal static extern void PublishAnonymouslyHostedDynamicMethodsAssembly(RuntimeAssembly assemblyHandle);
        public void SetData(string name, object data)
        {
            if (!name.Equals("LOCATION_URI"))
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData_OnlyLocationURI", name));
            }

            SetDataHelper(name, data, null);
        }

        public void SetData(string name, object data, IPermission permission)
        {
            if (!name.Equals("LOCATION_URI"))
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData_OnlyLocationURI", name));
            }

            SetDataHelper(name, data, permission);
        }

        private void SetDataHelper(string name, object data, IPermission permission)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            {
                object[] currentVal;
                lock (((ICollection)LocalStore).SyncRoot)
                {
                    LocalStore.TryGetValue(name, out currentVal);
                }

                if (currentVal != null && currentVal[0] != null)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SetData_OnlyOnce"));
                }
            }

            lock (((ICollection)LocalStore).SyncRoot)
            {
                LocalStore[name] = new object[]{data, permission};
            }
        }

        public Object GetData(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            int key = AppDomainSetup.Locate(name);
            if (key == -1)
            {
                if (name.Equals(AppDomainSetup.LoaderOptimizationKey))
                    return FusionStore.LoaderOptimization;
                else
                {
                    object[] data;
                    lock (((ICollection)LocalStore).SyncRoot)
                    {
                        LocalStore.TryGetValue(name, out data);
                    }

                    if (data == null)
                        return null;
                    if (data[1] != null)
                    {
                        IPermission permission = (IPermission)data[1];
                        permission.Demand();
                    }

                    return data[0];
                }
            }
            else
            {
                switch (key)
                {
                    case (int)AppDomainSetup.LoaderInformation.ApplicationBaseValue:
                        return FusionStore.ApplicationBase;
                    case (int)AppDomainSetup.LoaderInformation.ApplicationNameValue:
                        return FusionStore.ApplicationName;
                    default:
                        Contract.Assert(false, "Need to handle new LoaderInformation value in AppDomain.GetData()");
                        return null;
                }
            }
        }

        public Nullable<bool> IsCompatibilitySwitchSet(String value)
        {
            Nullable<bool> fReturn;
            if (_compatFlagsInitialized == false)
            {
                fReturn = new Nullable<bool>();
            }
            else
            {
                fReturn = new Nullable<bool>(_compatFlags != null && _compatFlags.ContainsKey(value));
            }

            return fReturn;
        }

        public static extern int GetCurrentThreadId();
        internal ApplicationTrust ApplicationTrust
        {
            get
            {
                if (_applicationTrust == null && _IsFastFullTrustDomain)
                {
                    _applicationTrust = new ApplicationTrust(new PermissionSet(PermissionState.Unrestricted));
                }

                return _applicationTrust;
            }
        }

        public String DynamicDirectory
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String dyndir = GetDynamicDir();
                if (dyndir != null)
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, dyndir).Demand();
                return dyndir;
            }
        }

        extern private String GetDynamicDir();
        public static IAppDomainPauseManager PauseManager
        {
            [System.Security.SecurityCritical]
            get
            {
                return AppDomainPauseManager.Instance;
            }
        }

        private AppDomain()
        {
            throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_Constructor));
        }

        private extern int _nExecuteAssembly(RuntimeAssembly assembly, String[] args);
        internal int nExecuteAssembly(RuntimeAssembly assembly, String[] args)
        {
            return _nExecuteAssembly(assembly, args);
        }

        internal extern void nCreateContext();
        private static extern void nSetupBindingPaths(String trustedPlatformAssemblies, String platformResourceRoots, String appPath, String appNiPaths, String appLocalWinMD);
        internal void SetupBindingPaths(String trustedPlatformAssemblies, String platformResourceRoots, String appPath, String appNiPaths, String appLocalWinMD)
        {
            nSetupBindingPaths(trustedPlatformAssemblies, platformResourceRoots, appPath, appNiPaths, appLocalWinMD);
        }

        private extern String nGetFriendlyName();
        private extern bool nIsDefaultAppDomainForEvidence();
        public event EventHandler ProcessExit
        {
            [System.Security.SecuritySafeCritical]
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _processExit += value;
                }
            }

            remove
            {
                lock (this)
                    _processExit -= value;
            }
        }

        public event EventHandler DomainUnload
        {
            [System.Security.SecuritySafeCritical]
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _domainUnload += value;
                }
            }

            [System.Security.SecuritySafeCritical]
            remove
            {
                lock (this)
                    _domainUnload -= value;
            }
        }

        public event UnhandledExceptionEventHandler UnhandledException
        {
            [System.Security.SecurityCritical]
            add
            {
                if (value != null)
                {
                    RuntimeHelpers.PrepareContractedDelegate(value);
                    lock (this)
                        _unhandledException += value;
                }
            }

            [System.Security.SecurityCritical]
            remove
            {
                lock (this)
                    _unhandledException -= value;
            }
        }

        private void OnAssemblyLoadEvent(RuntimeAssembly LoadedAssembly)
        {
            AssemblyLoadEventHandler eventHandler = AssemblyLoad;
            if (eventHandler != null)
            {
                AssemblyLoadEventArgs ea = new AssemblyLoadEventArgs(LoadedAssembly);
                eventHandler(this, ea);
            }
        }

        private RuntimeAssembly OnResourceResolveEvent(RuntimeAssembly assembly, String resourceName)
        {
            ResolveEventHandler eventHandler = _ResourceResolve;
            if (eventHandler == null)
                return null;
            Delegate[] ds = eventHandler.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++)
            {
                Assembly asm = ((ResolveEventHandler)ds[i])(this, new ResolveEventArgs(resourceName, assembly));
                RuntimeAssembly ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }

        private RuntimeAssembly OnTypeResolveEvent(RuntimeAssembly assembly, String typeName)
        {
            ResolveEventHandler eventHandler = _TypeResolve;
            if (eventHandler == null)
                return null;
            Delegate[] ds = eventHandler.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++)
            {
                Assembly asm = ((ResolveEventHandler)ds[i])(this, new ResolveEventArgs(typeName, assembly));
                RuntimeAssembly ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }

        private RuntimeAssembly OnAssemblyResolveEvent(RuntimeAssembly assembly, String assemblyFullName)
        {
            ResolveEventHandler eventHandler = _AssemblyResolve;
            if (eventHandler == null)
            {
                return null;
            }

            Delegate[] ds = eventHandler.GetInvocationList();
            int len = ds.Length;
            for (int i = 0; i < len; i++)
            {
                Assembly asm = ((ResolveEventHandler)ds[i])(this, new ResolveEventArgs(assemblyFullName, assembly));
                RuntimeAssembly ret = GetRuntimeAssembly(asm);
                if (ret != null)
                    return ret;
            }

            return null;
        }

        private string[] OnDesignerNamespaceResolveEvent(string namespaceName)
        {
            return System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMetadata.OnDesignerNamespaceResolveEvent(this, namespaceName);
        }

        internal AppDomainSetup FusionStore
        {
            get
            {
                Contract.Assert(_FusionStore != null, "Fusion store has not been correctly setup in this domain");
                return _FusionStore;
            }
        }

        internal static RuntimeAssembly GetRuntimeAssembly(Assembly asm)
        {
            if (asm == null)
                return null;
            RuntimeAssembly rtAssembly = asm as RuntimeAssembly;
            if (rtAssembly != null)
                return rtAssembly;
            AssemblyBuilder ab = asm as AssemblyBuilder;
            if (ab != null)
                return ab.InternalAssembly;
            return null;
        }

        private Dictionary<String, Object[]> LocalStore
        {
            get
            {
                if (_LocalStore != null)
                    return _LocalStore;
                else
                {
                    _LocalStore = new Dictionary<String, Object[]>();
                    return _LocalStore;
                }
            }
        }

        internal static extern bool IsDomainIdValid(Int32 id);
        private static extern void nSetNativeDllSearchDirectories(string paths);
        private void SetupFusionStore(AppDomainSetup info, AppDomainSetup oldInfo)
        {
            Contract.Requires(info != null);
            if (info.ApplicationBase == null)
            {
                info.SetupDefaults(RuntimeEnvironment.GetModuleFileName(), imageLocationAlreadyNormalized: true);
            }

            nCreateContext();
            if (info.LoaderOptimization != LoaderOptimization.NotSpecified || (oldInfo != null && info.LoaderOptimization != oldInfo.LoaderOptimization))
                UpdateLoaderOptimization(info.LoaderOptimization);
            _FusionStore = info;
        }

        private class EvidenceCollection
        {
            public Evidence ProvidedSecurityInfo;
            public Evidence CreatorsSecurityInfo;
        }

        private static void RunInitializer(AppDomainSetup setup)
        {
            if (setup.AppDomainInitializer != null)
            {
                string[] args = null;
                if (setup.AppDomainInitializerArguments != null)
                    args = (string[])setup.AppDomainInitializerArguments.Clone();
                setup.AppDomainInitializer(args);
            }
        }

        private static object PrepareDataForSetup(String friendlyName, AppDomainSetup setup, Evidence providedSecurityInfo, Evidence creatorsSecurityInfo, IntPtr parentSecurityDescriptor, string sandboxName, string[] propertyNames, string[] propertyValues)
        {
            byte[] serializedEvidence = null;
            bool generateDefaultEvidence = false;
            AppDomainInitializerInfo initializerInfo = null;
            if (setup != null && setup.AppDomainInitializer != null)
                initializerInfo = new AppDomainInitializerInfo(setup.AppDomainInitializer);
            AppDomainSetup newSetup = new AppDomainSetup(setup, false);
            List<String> compatList = new List<String>();
            if (propertyNames != null && propertyValues != null)
            {
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    if (String.Compare(propertyNames[i], "AppDomainCompatSwitch", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        compatList.Add(propertyValues[i]);
                        propertyNames[i] = null;
                        propertyValues[i] = null;
                    }
                }

                if (compatList.Count > 0)
                {
                    newSetup.SetCompatibilitySwitches(compatList);
                }
            }

            return new Object[]{friendlyName, newSetup, parentSecurityDescriptor, generateDefaultEvidence, serializedEvidence, initializerInfo, sandboxName, propertyNames, propertyValues};
        }

        private static Object Setup(Object arg)
        {
            Contract.Requires(arg != null && arg is Object[]);
            Contract.Requires(((Object[])arg).Length >= 8);
            Object[] args = (Object[])arg;
            String friendlyName = (String)args[0];
            AppDomainSetup setup = (AppDomainSetup)args[1];
            IntPtr parentSecurityDescriptor = (IntPtr)args[2];
            bool generateDefaultEvidence = (bool)args[3];
            byte[] serializedEvidence = (byte[])args[4];
            AppDomainInitializerInfo initializerInfo = (AppDomainInitializerInfo)args[5];
            string sandboxName = (string)args[6];
            string[] propertyNames = (string[])args[7];
            string[] propertyValues = (string[])args[8];
            Evidence providedSecurityInfo = null;
            Evidence creatorsSecurityInfo = null;
            AppDomain ad = AppDomain.CurrentDomain;
            AppDomainSetup newSetup = new AppDomainSetup(setup, false);
            if (propertyNames != null && propertyValues != null)
            {
                StringBuilder normalisedAppPathList = null;
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    if (propertyNames[i] == "APPBASE")
                    {
                        if (propertyValues[i] == null)
                            throw new ArgumentNullException("APPBASE");
                        if (Path.IsRelative(propertyValues[i]))
                            throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"));
                        newSetup.ApplicationBase = Path.NormalizePath(propertyValues[i], true);
                    }
                    else if (propertyNames[i] == "LOADER_OPTIMIZATION")
                    {
                        if (propertyValues[i] == null)
                            throw new ArgumentNullException("LOADER_OPTIMIZATION");
                        switch (propertyValues[i])
                        {
                            case "SingleDomain":
                                newSetup.LoaderOptimization = LoaderOptimization.SingleDomain;
                                break;
                            case "MultiDomain":
                                newSetup.LoaderOptimization = LoaderOptimization.MultiDomain;
                                break;
                            case "MultiDomainHost":
                                newSetup.LoaderOptimization = LoaderOptimization.MultiDomainHost;
                                break;
                            case "NotSpecified":
                                newSetup.LoaderOptimization = LoaderOptimization.NotSpecified;
                                break;
                            default:
                                throw new ArgumentException(Environment.GetResourceString("Argument_UnrecognizedLoaderOptimization"), "LOADER_OPTIMIZATION");
                        }
                    }
                    else if (propertyNames[i] == "NATIVE_DLL_SEARCH_DIRECTORIES")
                    {
                        if (propertyValues[i] == null)
                            throw new ArgumentNullException("NATIVE_DLL_SEARCH_DIRECTORIES");
                        ad.SetDataHelper(propertyNames[i], propertyValues[i], null);
                        string paths = (string)propertyValues[i];
                        if (paths.Length == 0)
                            continue;
                        nSetNativeDllSearchDirectories(paths);
                    }
                    else if (propertyNames[i] == "TRUSTED_PLATFORM_ASSEMBLIES" || propertyNames[i] == "PLATFORM_RESOURCE_ROOTS" || propertyNames[i] == "APP_PATHS" || propertyNames[i] == "APP_NI_PATHS")
                    {
                        string values = propertyValues[i];
                        if (values == null)
                            throw new ArgumentNullException(propertyNames[i]);
                        int estimatedLength = values.Length + 1;
                        if (normalisedAppPathList == null)
                        {
                            normalisedAppPathList = new StringBuilder(estimatedLength);
                        }
                        else
                        {
                            normalisedAppPathList.Clear();
                            if (normalisedAppPathList.Capacity < estimatedLength)
                                normalisedAppPathList.Capacity = estimatedLength;
                        }

                        for (int pos = 0; pos < values.Length; pos++)
                        {
                            string path;
                            int nextPos = values.IndexOf(Path.PathSeparator, pos);
                            if (nextPos == -1)
                            {
                                path = values.Substring(pos);
                                pos = values.Length - 1;
                            }
                            else
                            {
                                path = values.Substring(pos, nextPos - pos);
                                pos = nextPos;
                            }

                            if (path.Length == 0)
                                continue;
                            if (Path.IsRelative(path))
                                throw new ArgumentException(Environment.GetResourceString("Argument_AbsolutePathRequired"));
                            string appPath = Path.NormalizePath(path, true);
                            normalisedAppPathList.Append(appPath);
                            normalisedAppPathList.Append(Path.PathSeparator);
                        }

                        if (normalisedAppPathList.Length > 0)
                        {
                            normalisedAppPathList.Remove(normalisedAppPathList.Length - 1, 1);
                        }

                        ad.SetDataHelper(propertyNames[i], normalisedAppPathList.ToString(), null);
                    }
                    else if (propertyNames[i] != null)
                    {
                        ad.SetDataHelper(propertyNames[i], propertyValues[i], null);
                    }
                }
            }

            ad.SetupFusionStore(newSetup, null);
            AppDomainSetup adSetup = ad.FusionStore;
            adSetup.InternalSetApplicationTrust(sandboxName);
            ad.nSetupFriendlyName(friendlyName);
            if (setup != null && setup.SandboxInterop)
            {
                ad.nSetDisableInterfaceCache();
            }

            if (adSetup.AppDomainManagerAssembly != null && adSetup.AppDomainManagerType != null)
            {
                ad.SetAppDomainManagerType(adSetup.AppDomainManagerAssembly, adSetup.AppDomainManagerType);
            }

            ad.CreateAppDomainManager();
            ad.InitializeDomainSecurity(providedSecurityInfo, creatorsSecurityInfo, generateDefaultEvidence, parentSecurityDescriptor, true);
            if (initializerInfo != null)
                adSetup.AppDomainInitializer = initializerInfo.Unwrap();
            RunInitializer(adSetup);
            return null;
        }

        private void SetupDomain(bool allowRedirects, String path, String configFile, String[] propertyNames, String[] propertyValues)
        {
            lock (this)
            {
                if (_FusionStore == null)
                {
                    AppDomainSetup setup = new AppDomainSetup();
                    setup.InternalSetApplicationTrust("Internet");
                    SetupFusionStore(setup, null);
                }
            }
        }

        private void SetupLoaderOptimization(LoaderOptimization policy)
        {
            if (policy != LoaderOptimization.NotSpecified)
            {
                Contract.Assert(FusionStore.LoaderOptimization == LoaderOptimization.NotSpecified, "It is illegal to change the Loader optimization on a domain");
                FusionStore.LoaderOptimization = policy;
                UpdateLoaderOptimization(FusionStore.LoaderOptimization);
            }
        }

        internal extern IntPtr GetSecurityDescriptor();
        private void SetupDomainSecurity(Evidence appDomainEvidence, IntPtr creatorsSecurityDescriptor, bool publishAppDomain)
        {
            Evidence stackEvidence = appDomainEvidence;
            SetupDomainSecurity(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref stackEvidence), creatorsSecurityDescriptor, publishAppDomain);
        }

        private static extern void SetupDomainSecurity(AppDomainHandle appDomain, ObjectHandleOnStack appDomainEvidence, IntPtr creatorsSecurityDescriptor, [MarshalAs(UnmanagedType.Bool)] bool publishAppDomain);
        private extern void nSetupFriendlyName(string friendlyName);
        private extern void nSetDisableInterfaceCache();
        internal extern void UpdateLoaderOptimization(LoaderOptimization optimization);
        public AppDomainSetup SetupInformation
        {
            get
            {
                return new AppDomainSetup(FusionStore, true);
            }
        }

        internal extern String IsStringInterned(String str);
        internal extern String GetOrInternString(String str);
        private static extern void GetGrantSet(AppDomainHandle domain, ObjectHandleOnStack retGrantSet);
        public PermissionSet PermissionSet
        {
            [SecurityCritical]
            get
            {
                PermissionSet grantSet = null;
                GetGrantSet(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref grantSet));
                if (grantSet != null)
                {
                    return grantSet.Copy();
                }
                else
                {
                    return new PermissionSet(PermissionState.Unrestricted);
                }
            }
        }

        public bool IsFullyTrusted
        {
            [SecuritySafeCritical]
            get
            {
                PermissionSet grantSet = null;
                GetGrantSet(GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref grantSet));
                return grantSet == null || grantSet.IsUnrestricted();
            }
        }

        public bool IsHomogenous
        {
            get
            {
                return _IsFastFullTrustDomain || _applicationTrust != null;
            }
        }

        private extern void nChangeSecurityPolicy();
        internal static extern void nUnload(Int32 domainInternal);
        public Object CreateInstanceAndUnwrap(String assemblyName, String typeName)
        {
            ObjectHandle oh = CreateInstance(assemblyName, typeName);
            if (oh == null)
                return null;
            return oh.Unwrap();
        }

        public Object CreateInstanceAndUnwrap(String assemblyName, String typeName, Object[] activationAttributes)
        {
            ObjectHandle oh = CreateInstance(assemblyName, typeName, activationAttributes);
            if (oh == null)
                return null;
            return oh.Unwrap();
        }

        public Object CreateInstanceAndUnwrap(String assemblyName, String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityAttributes)
        {
            ObjectHandle oh = CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
            if (oh == null)
                return null;
            return oh.Unwrap();
        }

        public object CreateInstanceAndUnwrap(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            ObjectHandle oh = CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
            if (oh == null)
            {
                return null;
            }

            return oh.Unwrap();
        }

        public Object CreateInstanceFromAndUnwrap(String assemblyName, String typeName)
        {
            ObjectHandle oh = CreateInstanceFrom(assemblyName, typeName);
            if (oh == null)
                return null;
            return oh.Unwrap();
        }

        public Object CreateInstanceFromAndUnwrap(String assemblyName, String typeName, Object[] activationAttributes)
        {
            ObjectHandle oh = CreateInstanceFrom(assemblyName, typeName, activationAttributes);
            if (oh == null)
                return null;
            return oh.Unwrap();
        }

        public Object CreateInstanceFromAndUnwrap(String assemblyName, String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityAttributes)
        {
            ObjectHandle oh = CreateInstanceFrom(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
            if (oh == null)
                return null;
            return oh.Unwrap();
        }

        public object CreateInstanceFromAndUnwrap(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            ObjectHandle oh = CreateInstanceFrom(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
            if (oh == null)
            {
                return null;
            }

            return oh.Unwrap();
        }

        public Int32 Id
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            get
            {
                return GetId();
            }
        }

        internal extern Int32 GetId();
        internal const Int32 DefaultADID = 1;
        public bool IsDefaultAppDomain()
        {
            if (GetId() == DefaultADID)
                return true;
            return false;
        }

        private static extern void nEnableMonitoring();
        private static extern bool nMonitoringIsEnabled();
        private extern Int64 nGetTotalProcessorTime();
        private extern Int64 nGetTotalAllocatedMemorySize();
        private extern Int64 nGetLastSurvivedMemorySize();
        private static extern Int64 nGetLastSurvivedProcessMemorySize();
        public static bool MonitoringIsEnabled
        {
            [System.Security.SecurityCritical]
            get
            {
                return nMonitoringIsEnabled();
            }

            [System.Security.SecurityCritical]
            set
            {
                if (value == false)
                {
                    throw new ArgumentException(Environment.GetResourceString("Arg_MustBeTrue"));
                }
                else
                {
                    nEnableMonitoring();
                }
            }
        }

        public TimeSpan MonitoringTotalProcessorTime
        {
            [System.Security.SecurityCritical]
            get
            {
                Int64 i64ProcessorTime = nGetTotalProcessorTime();
                if (i64ProcessorTime == -1)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
                }

                return new TimeSpan(i64ProcessorTime);
            }
        }

        public Int64 MonitoringTotalAllocatedMemorySize
        {
            [System.Security.SecurityCritical]
            get
            {
                Int64 i64AllocatedMemory = nGetTotalAllocatedMemorySize();
                if (i64AllocatedMemory == -1)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
                }

                return i64AllocatedMemory;
            }
        }

        public Int64 MonitoringSurvivedMemorySize
        {
            [System.Security.SecurityCritical]
            get
            {
                Int64 i64LastSurvivedMemory = nGetLastSurvivedMemorySize();
                if (i64LastSurvivedMemory == -1)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
                }

                return i64LastSurvivedMemory;
            }
        }

        public static Int64 MonitoringSurvivedProcessMemorySize
        {
            [System.Security.SecurityCritical]
            get
            {
                Int64 i64LastSurvivedProcessMemory = nGetLastSurvivedProcessMemorySize();
                if (i64LastSurvivedProcessMemory == -1)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_WithoutARM"));
                }

                return i64LastSurvivedProcessMemory;
            }
        }
    }

    internal struct AppDomainHandle
    {
        private IntPtr m_appDomainHandle;
        internal AppDomainHandle(IntPtr domainHandle)
        {
            m_appDomainHandle = domainHandle;
        }
    }
}