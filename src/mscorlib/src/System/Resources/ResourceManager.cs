using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Resources
{
    internal class WindowsRuntimeResourceManagerBase
    {
        public virtual bool Initialize(string libpath, string reswFilename, out PRIExceptionInfo exceptionInfo)
        {
            exceptionInfo = null;
            return false;
        }

        public virtual String GetString(String stringName, String startingCulture, String neutralResourcesCulture)
        {
            return null;
        }

        public virtual CultureInfo GlobalResourceContextBestFitCultureInfo
        {
            [SecurityCritical]
            get
            {
                return null;
            }
        }

        public virtual bool SetGlobalResourceContextDefaultCulture(CultureInfo ci)
        {
            return false;
        }
    }

    internal class PRIExceptionInfo
    {
        public string _PackageSimpleName;
        public string _ResWFile;
    }

    public class ResourceManager
    {
        internal class CultureNameResourceSetPair
        {
            public String lastCultureName;
            public ResourceSet lastResourceSet;
        }

        protected String BaseNameField;
        protected Hashtable ResourceSets;
        private Dictionary<String, ResourceSet> _resourceSets;
        private String moduleDir;
        protected Assembly MainAssembly;
        private Type _locationInfo;
        private Type _userResourceSet;
        private CultureInfo _neutralResourcesCulture;
        private CultureNameResourceSetPair _lastUsedResourceCache;
        private bool _ignoreCase;
        private bool UseManifest;
        private bool UseSatelliteAssem;
        private static volatile Hashtable _installedSatelliteInfo;
        private static volatile bool _checkedConfigFile;
        private UltimateResourceFallbackLocation _fallbackLoc;
        private Version _satelliteContractVersion;
        private bool _lookedForSatelliteContractVersion;
        private Assembly _callingAssembly;
        private RuntimeAssembly m_callingAssembly;
        private IResourceGroveler resourceGroveler;
        public static readonly int MagicNumber = unchecked ((int)0xBEEFCACE);
        public static readonly int HeaderVersionNumber = 1;
        private static readonly Type _minResourceSet = typeof (ResourceSet);
        internal static readonly String ResReaderTypeName = typeof (ResourceReader).FullName;
        internal static readonly String ResSetTypeName = typeof (RuntimeResourceSet).FullName;
        internal static readonly String MscorlibName = typeof (ResourceReader).Assembly.FullName;
        internal const String ResFileExtension = ".resources";
        internal const int ResFileExtensionLength = 10;
        internal static readonly int DEBUG = 0;
        private static volatile bool s_IsAppXModel;
        private void Init()
        {
            m_callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
        }

        protected ResourceManager()
        {
            Init();
            _lastUsedResourceCache = new CultureNameResourceSetPair();
            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            resourceGroveler = new ManifestBasedResourceGroveler(mediator);
        }

        private ResourceManager(String baseName, String resourceDir, Type usingResourceSet)
        {
            if (null == baseName)
                throw new ArgumentNullException("baseName");
            if (null == resourceDir)
                throw new ArgumentNullException("resourceDir");
            Contract.EndContractBlock();
            BaseNameField = baseName;
            moduleDir = resourceDir;
            _userResourceSet = usingResourceSet;
            ResourceSets = new Hashtable();
            _resourceSets = new Dictionary<String, ResourceSet>();
            _lastUsedResourceCache = new CultureNameResourceSetPair();
            UseManifest = false;
            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            resourceGroveler = new FileBasedResourceGroveler(mediator);
        }

        public ResourceManager(String baseName, Assembly assembly)
        {
            if (null == baseName)
                throw new ArgumentNullException("baseName");
            if (null == assembly)
                throw new ArgumentNullException("assembly");
            Contract.EndContractBlock();
            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
            MainAssembly = assembly;
            BaseNameField = baseName;
            SetAppXConfiguration();
            CommonAssemblyInit();
            m_callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
            if (assembly == typeof (Object).Assembly && m_callingAssembly != assembly)
            {
                m_callingAssembly = null;
            }
        }

        public ResourceManager(String baseName, Assembly assembly, Type usingResourceSet)
        {
            if (null == baseName)
                throw new ArgumentNullException("baseName");
            if (null == assembly)
                throw new ArgumentNullException("assembly");
            Contract.EndContractBlock();
            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeAssembly"));
            MainAssembly = assembly;
            BaseNameField = baseName;
            if (usingResourceSet != null && (usingResourceSet != _minResourceSet) && !(usingResourceSet.IsSubclassOf(_minResourceSet)))
                throw new ArgumentException(Environment.GetResourceString("Arg_ResMgrNotResSet"), "usingResourceSet");
            _userResourceSet = usingResourceSet;
            CommonAssemblyInit();
            m_callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
            if (assembly == typeof (Object).Assembly && m_callingAssembly != assembly)
                m_callingAssembly = null;
        }

        public ResourceManager(Type resourceSource)
        {
            if (null == resourceSource)
                throw new ArgumentNullException("resourceSource");
            Contract.EndContractBlock();
            if (!(resourceSource is RuntimeType))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
            _locationInfo = resourceSource;
            MainAssembly = _locationInfo.Assembly;
            BaseNameField = resourceSource.Name;
            SetAppXConfiguration();
            CommonAssemblyInit();
            m_callingAssembly = (RuntimeAssembly)Assembly.GetCallingAssembly();
            if (MainAssembly == typeof (Object).Assembly && m_callingAssembly != MainAssembly)
            {
                m_callingAssembly = null;
            }
        }

        private void OnDeserializing(StreamingContext ctx)
        {
            this._resourceSets = null;
            this.resourceGroveler = null;
            this._lastUsedResourceCache = null;
        }

        private void OnDeserialized(StreamingContext ctx)
        {
            _resourceSets = new Dictionary<String, ResourceSet>();
            _lastUsedResourceCache = new CultureNameResourceSetPair();
            ResourceManagerMediator mediator = new ResourceManagerMediator(this);
            if (UseManifest)
            {
                resourceGroveler = new ManifestBasedResourceGroveler(mediator);
            }
            else
            {
                resourceGroveler = new FileBasedResourceGroveler(mediator);
            }

            if (this.m_callingAssembly == null)
            {
                this.m_callingAssembly = (RuntimeAssembly)_callingAssembly;
            }

            if (UseManifest && this._neutralResourcesCulture == null)
            {
                _neutralResourcesCulture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(MainAssembly, ref _fallbackLoc);
            }
        }

        private void OnSerializing(StreamingContext ctx)
        {
            _callingAssembly = m_callingAssembly;
            UseSatelliteAssem = UseManifest;
            ResourceSets = new Hashtable();
        }

        private void CommonAssemblyInit()
        {
            if (_bUsingModernResourceManagement == false)
            {
                UseManifest = true;
                _resourceSets = new Dictionary<String, ResourceSet>();
                _lastUsedResourceCache = new CultureNameResourceSetPair();
                _fallbackLoc = UltimateResourceFallbackLocation.MainAssembly;
                ResourceManagerMediator mediator = new ResourceManagerMediator(this);
                resourceGroveler = new ManifestBasedResourceGroveler(mediator);
            }

            _neutralResourcesCulture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(MainAssembly, ref _fallbackLoc);
        }

        public virtual String BaseName
        {
            get
            {
                return BaseNameField;
            }
        }

        public virtual bool IgnoreCase
        {
            get
            {
                return _ignoreCase;
            }

            set
            {
                _ignoreCase = value;
            }
        }

        public virtual Type ResourceSetType
        {
            get
            {
                return (_userResourceSet == null) ? typeof (RuntimeResourceSet) : _userResourceSet;
            }
        }

        protected UltimateResourceFallbackLocation FallbackLocation
        {
            get
            {
                return _fallbackLoc;
            }

            set
            {
                _fallbackLoc = value;
            }
        }

        public virtual void ReleaseAllResources()
        {
            Dictionary<String, ResourceSet> localResourceSets = _resourceSets;
            _resourceSets = new Dictionary<String, ResourceSet>();
            _lastUsedResourceCache = new CultureNameResourceSetPair();
            lock (localResourceSets)
            {
                IDictionaryEnumerator setEnum = localResourceSets.GetEnumerator();
                while (setEnum.MoveNext())
                {
                    ((ResourceSet)setEnum.Value).Close();
                }
            }
        }

        public static ResourceManager CreateFileBasedResourceManager(String baseName, String resourceDir, Type usingResourceSet)
        {
            return new ResourceManager(baseName, resourceDir, usingResourceSet);
        }

        protected virtual String GetResourceFileName(CultureInfo culture)
        {
            StringBuilder sb = new StringBuilder(255);
            sb.Append(BaseNameField);
            if (!culture.HasInvariantCultureName)
            {
                CultureInfo.VerifyCultureName(culture.Name, true);
                sb.Append('.');
                sb.Append(culture.Name);
            }

            sb.Append(ResFileExtension);
            return sb.ToString();
        }

        internal ResourceSet GetFirstResourceSet(CultureInfo culture)
        {
            if (_neutralResourcesCulture != null && culture.Name == _neutralResourcesCulture.Name)
            {
                culture = CultureInfo.InvariantCulture;
            }

            if (_lastUsedResourceCache != null)
            {
                lock (_lastUsedResourceCache)
                {
                    if (culture.Name == _lastUsedResourceCache.lastCultureName)
                        return _lastUsedResourceCache.lastResourceSet;
                }
            }

            Dictionary<String, ResourceSet> localResourceSets = _resourceSets;
            ResourceSet rs = null;
            if (localResourceSets != null)
            {
                lock (localResourceSets)
                {
                    localResourceSets.TryGetValue(culture.Name, out rs);
                }
            }

            if (rs != null)
            {
                if (_lastUsedResourceCache != null)
                {
                    lock (_lastUsedResourceCache)
                    {
                        _lastUsedResourceCache.lastCultureName = culture.Name;
                        _lastUsedResourceCache.lastResourceSet = rs;
                    }
                }

                return rs;
            }

            return null;
        }

        public virtual ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            if (null == culture)
                throw new ArgumentNullException("culture");
            Contract.EndContractBlock();
            Dictionary<String, ResourceSet> localResourceSets = _resourceSets;
            ResourceSet rs;
            if (localResourceSets != null)
            {
                lock (localResourceSets)
                {
                    if (localResourceSets.TryGetValue(culture.Name, out rs))
                        return rs;
                }
            }

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            if (UseManifest && culture.HasInvariantCultureName)
            {
                string fileName = GetResourceFileName(culture);
                RuntimeAssembly mainAssembly = (RuntimeAssembly)MainAssembly;
                Stream stream = mainAssembly.GetManifestResourceStream(_locationInfo, fileName, m_callingAssembly == MainAssembly, ref stackMark);
                if (createIfNotExists && stream != null)
                {
                    rs = ((ManifestBasedResourceGroveler)resourceGroveler).CreateResourceSet(stream, MainAssembly);
                    AddResourceSet(localResourceSets, culture.Name, ref rs);
                    return rs;
                }
            }

            return InternalGetResourceSet(culture, createIfNotExists, tryParents);
        }

        protected virtual ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            Contract.Assert(culture != null, "culture != null");
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetResourceSet(culture, createIfNotExists, tryParents, ref stackMark);
        }

        private ResourceSet InternalGetResourceSet(CultureInfo requestedCulture, bool createIfNotExists, bool tryParents, ref StackCrawlMark stackMark)
        {
            Dictionary<String, ResourceSet> localResourceSets = _resourceSets;
            ResourceSet rs = null;
            CultureInfo foundCulture = null;
            lock (localResourceSets)
            {
                if (localResourceSets.TryGetValue(requestedCulture.Name, out rs))
                {
                    return rs;
                }
            }

            ResourceFallbackManager mgr = new ResourceFallbackManager(requestedCulture, _neutralResourcesCulture, tryParents);
            foreach (CultureInfo currentCultureInfo in mgr)
            {
                lock (localResourceSets)
                {
                    if (localResourceSets.TryGetValue(currentCultureInfo.Name, out rs))
                    {
                        if (requestedCulture != currentCultureInfo)
                            foundCulture = currentCultureInfo;
                        break;
                    }
                }

                rs = resourceGroveler.GrovelForResourceSet(currentCultureInfo, localResourceSets, tryParents, createIfNotExists, ref stackMark);
                if (rs != null)
                {
                    foundCulture = currentCultureInfo;
                    break;
                }
            }

            if (rs != null && foundCulture != null)
            {
                foreach (CultureInfo updateCultureInfo in mgr)
                {
                    AddResourceSet(localResourceSets, updateCultureInfo.Name, ref rs);
                    if (updateCultureInfo == foundCulture)
                    {
                        break;
                    }
                }
            }

            return rs;
        }

        private static void AddResourceSet(Dictionary<String, ResourceSet> localResourceSets, String cultureName, ref ResourceSet rs)
        {
            lock (localResourceSets)
            {
                ResourceSet lostRace;
                if (localResourceSets.TryGetValue(cultureName, out lostRace))
                {
                    if (!Object.ReferenceEquals(lostRace, rs))
                    {
                        if (!localResourceSets.ContainsValue(rs))
                            rs.Dispose();
                        rs = lostRace;
                    }
                }
                else
                {
                    localResourceSets.Add(cultureName, rs);
                }
            }
        }

        protected static Version GetSatelliteContractVersion(Assembly a)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a", Environment.GetResourceString("ArgumentNull_Assembly"));
            }

            Contract.EndContractBlock();
            return null;
        }

        protected static CultureInfo GetNeutralResourcesLanguage(Assembly a)
        {
            UltimateResourceFallbackLocation ignoringUsefulData = UltimateResourceFallbackLocation.MainAssembly;
            CultureInfo culture = ManifestBasedResourceGroveler.GetNeutralResourcesLanguage(a, ref ignoringUsefulData);
            return culture;
        }

        internal static bool CompareNames(String asmTypeName1, String typeName2, AssemblyName asmName2)
        {
            Contract.Assert(asmTypeName1 != null, "asmTypeName1 was unexpectedly null");
            int comma = asmTypeName1.IndexOf(',');
            if (((comma == -1) ? asmTypeName1.Length : comma) != typeName2.Length)
                return false;
            if (String.Compare(asmTypeName1, 0, typeName2, 0, typeName2.Length, StringComparison.Ordinal) != 0)
                return false;
            if (comma == -1)
                return true;
            while (Char.IsWhiteSpace(asmTypeName1[++comma]))
                ;
            AssemblyName an1 = new AssemblyName(asmTypeName1.Substring(comma));
            if (String.Compare(an1.Name, asmName2.Name, StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            if (String.Compare(an1.Name, "mscorlib", StringComparison.OrdinalIgnoreCase) == 0)
                return true;
            if ((an1.CultureInfo != null) && (asmName2.CultureInfo != null) && (an1.CultureInfo.Name != asmName2.CultureInfo.Name))
                return false;
            byte[] pkt1 = an1.GetPublicKeyToken();
            byte[] pkt2 = asmName2.GetPublicKeyToken();
            if ((pkt1 != null) && (pkt2 != null))
            {
                if (pkt1.Length != pkt2.Length)
                    return false;
                for (int i = 0; i < pkt1.Length; i++)
                {
                    if (pkt1[i] != pkt2[i])
                        return false;
                }
            }

            return true;
        }

        private string GetStringFromPRI(String stringName, String startingCulture, String neutralResourcesCulture)
        {
            Contract.Assert(_bUsingModernResourceManagement);
            Contract.Assert(_WinRTResourceManager != null);
            Contract.Assert(_PRIonAppXInitialized);
            Contract.Assert(AppDomain.IsAppXModel());
            if (stringName.Length == 0)
                return null;
            string resourceString = null;
            resourceString = _WinRTResourceManager.GetString(stringName, String.IsNullOrEmpty(startingCulture) ? null : startingCulture, String.IsNullOrEmpty(neutralResourcesCulture) ? null : neutralResourcesCulture);
            return resourceString;
        }

        internal static WindowsRuntimeResourceManagerBase GetWinRTResourceManager()
        {
            Type WinRTResourceManagerType = Type.GetType("System.Resources.WindowsRuntimeResourceManager, " + AssemblyRef.SystemRuntimeWindowsRuntime, true);
            return (WindowsRuntimeResourceManagerBase)Activator.CreateInstance(WinRTResourceManagerType, true);
        }

        private bool _bUsingModernResourceManagement;
        private WindowsRuntimeResourceManagerBase _WinRTResourceManager;
        private bool _PRIonAppXInitialized;
        private PRIExceptionInfo _PRIExceptionInfo;
        private bool ShouldUseSatelliteAssemblyResourceLookupUnderAppX(RuntimeAssembly resourcesAssembly)
        {
            bool fUseSatelliteAssemblyResourceLookupUnderAppX = resourcesAssembly.IsFrameworkAssembly();
            if (!fUseSatelliteAssemblyResourceLookupUnderAppX)
            {
                String platformResourceRoots = (String)(AppDomain.CurrentDomain.GetData("PLATFORM_RESOURCE_ROOTS"));
                if ((platformResourceRoots != null) && (platformResourceRoots != String.Empty))
                {
                    string resourceAssemblyPath = resourcesAssembly.Location;
                    foreach (string pathPlatformResourceRoot in platformResourceRoots.Split(Path.PathSeparator))
                    {
                        if (resourceAssemblyPath.StartsWith(pathPlatformResourceRoot, StringComparison.CurrentCultureIgnoreCase))
                        {
                            fUseSatelliteAssemblyResourceLookupUnderAppX = true;
                            break;
                        }
                    }
                }
            }

            return fUseSatelliteAssemblyResourceLookupUnderAppX;
        }

        private void SetAppXConfiguration()
        {
            Contract.Assert(_bUsingModernResourceManagement == false);
            Contract.Assert(_WinRTResourceManager == null);
            Contract.Assert(_PRIonAppXInitialized == false);
            Contract.Assert(_PRIExceptionInfo == null);
            bool bUsingSatelliteAssembliesUnderAppX = false;
            RuntimeAssembly resourcesAssembly = (RuntimeAssembly)MainAssembly;
            if (resourcesAssembly == null)
                resourcesAssembly = m_callingAssembly;
            if (resourcesAssembly != null)
            {
                if (resourcesAssembly != typeof (Object).Assembly)
                {
                    if (AppDomain.IsAppXModel() && !AppDomain.IsAppXNGen)
                    {
                        s_IsAppXModel = true;
                        String reswFilename = _locationInfo == null ? BaseNameField : _locationInfo.FullName;
                        if (reswFilename == null)
                            reswFilename = String.Empty;
                        WindowsRuntimeResourceManagerBase WRRM = null;
                        bool bWRRM_Initialized = false;
                        if (AppDomain.IsAppXDesignMode())
                        {
                            WRRM = GetWinRTResourceManager();
                            try
                            {
                                PRIExceptionInfo exceptionInfo;
                                bWRRM_Initialized = WRRM.Initialize(resourcesAssembly.Location, reswFilename, out exceptionInfo);
                                bUsingSatelliteAssembliesUnderAppX = !bWRRM_Initialized;
                            }
                            catch (Exception e)
                            {
                                bUsingSatelliteAssembliesUnderAppX = true;
                                if (e.IsTransient)
                                    throw;
                            }
                        }

                        if (!bUsingSatelliteAssembliesUnderAppX)
                        {
                            _bUsingModernResourceManagement = !ShouldUseSatelliteAssemblyResourceLookupUnderAppX(resourcesAssembly);
                            if (_bUsingModernResourceManagement)
                            {
                                if (WRRM != null && bWRRM_Initialized)
                                {
                                    _WinRTResourceManager = WRRM;
                                    _PRIonAppXInitialized = true;
                                }
                                else
                                {
                                    _WinRTResourceManager = GetWinRTResourceManager();
                                    try
                                    {
                                        _PRIonAppXInitialized = _WinRTResourceManager.Initialize(resourcesAssembly.Location, reswFilename, out _PRIExceptionInfo);
                                    }
                                    catch (FileNotFoundException)
                                    {
                                    }
                                    catch (Exception e)
                                    {
                                        if (e.HResult != __HResults.ERROR_MRM_MAP_NOT_FOUND)
                                            throw;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public virtual String GetString(String name)
        {
            return GetString(name, (CultureInfo)null);
        }

        public virtual String GetString(String name, CultureInfo culture)
        {
            if (null == name)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            if (s_IsAppXModel)
            {
                if (Object.ReferenceEquals(culture, CultureInfo.CurrentUICulture))
                {
                    culture = null;
                }
            }

            if (_bUsingModernResourceManagement)
            {
                if (_PRIonAppXInitialized == false)
                {
                    if (_PRIExceptionInfo != null && _PRIExceptionInfo._PackageSimpleName != null && _PRIExceptionInfo._ResWFile != null)
                        throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_ResWFileNotLoaded", _PRIExceptionInfo._ResWFile, _PRIExceptionInfo._PackageSimpleName));
                    throw new MissingManifestResourceException(Environment.GetResourceString("MissingManifestResource_NoPRIresources"));
                }

                return GetStringFromPRI(name, culture == null ? null : culture.Name, _neutralResourcesCulture.Name);
            }
            else
            {
                if (null == culture)
                {
                    culture = Thread.CurrentThread.GetCurrentUICultureNoAppX();
                }

                ResourceSet last = GetFirstResourceSet(culture);
                if (last != null)
                {
                    String value = last.GetString(name, _ignoreCase);
                    if (value != null)
                        return value;
                }

                ResourceFallbackManager mgr = new ResourceFallbackManager(culture, _neutralResourcesCulture, true);
                foreach (CultureInfo currentCultureInfo in mgr)
                {
                    ResourceSet rs = InternalGetResourceSet(currentCultureInfo, true, true);
                    if (rs == null)
                        break;
                    if (rs != last)
                    {
                        String value = rs.GetString(name, _ignoreCase);
                        if (value != null)
                        {
                            if (_lastUsedResourceCache != null)
                            {
                                lock (_lastUsedResourceCache)
                                {
                                    _lastUsedResourceCache.lastCultureName = currentCultureInfo.Name;
                                    _lastUsedResourceCache.lastResourceSet = rs;
                                }
                            }

                            return value;
                        }

                        last = rs;
                    }
                }
            }

            return null;
        }

        public virtual Object GetObject(String name)
        {
            return GetObject(name, (CultureInfo)null, true);
        }

        public virtual Object GetObject(String name, CultureInfo culture)
        {
            return GetObject(name, culture, true);
        }

        private Object GetObject(String name, CultureInfo culture, bool wrapUnmanagedMemStream)
        {
            if (null == name)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            if (s_IsAppXModel)
            {
                if (Object.ReferenceEquals(culture, CultureInfo.CurrentUICulture))
                {
                    culture = null;
                }
            }

            if (null == culture)
            {
                culture = Thread.CurrentThread.GetCurrentUICultureNoAppX();
            }

            ResourceSet last = GetFirstResourceSet(culture);
            if (last != null)
            {
                Object value = last.GetObject(name, _ignoreCase);
                if (value != null)
                {
                    UnmanagedMemoryStream stream = value as UnmanagedMemoryStream;
                    if (stream != null && wrapUnmanagedMemStream)
                        return new UnmanagedMemoryStreamWrapper(stream);
                    else
                        return value;
                }
            }

            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, _neutralResourcesCulture, true);
            foreach (CultureInfo currentCultureInfo in mgr)
            {
                ResourceSet rs = InternalGetResourceSet(currentCultureInfo, true, true);
                if (rs == null)
                    break;
                if (rs != last)
                {
                    Object value = rs.GetObject(name, _ignoreCase);
                    if (value != null)
                    {
                        if (_lastUsedResourceCache != null)
                        {
                            lock (_lastUsedResourceCache)
                            {
                                _lastUsedResourceCache.lastCultureName = currentCultureInfo.Name;
                                _lastUsedResourceCache.lastResourceSet = rs;
                            }
                        }

                        UnmanagedMemoryStream stream = value as UnmanagedMemoryStream;
                        if (stream != null && wrapUnmanagedMemStream)
                            return new UnmanagedMemoryStreamWrapper(stream);
                        else
                            return value;
                    }

                    last = rs;
                }
            }

            return null;
        }

        public UnmanagedMemoryStream GetStream(String name)
        {
            return GetStream(name, (CultureInfo)null);
        }

        public UnmanagedMemoryStream GetStream(String name, CultureInfo culture)
        {
            Object obj = GetObject(name, culture, false);
            UnmanagedMemoryStream ums = obj as UnmanagedMemoryStream;
            if (ums == null && obj != null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotStream_Name", name));
            return ums;
        }

        private bool TryLookingForSatellite(CultureInfo lookForCulture)
        {
            if (!_checkedConfigFile)
            {
                lock (this)
                {
                    if (!_checkedConfigFile)
                    {
                        _checkedConfigFile = true;
                        _installedSatelliteInfo = GetSatelliteAssembliesFromConfig();
                    }
                }
            }

            if (_installedSatelliteInfo == null)
                return true;
            String[] installedSatellites = (String[])_installedSatelliteInfo[MainAssembly.FullName];
            if (installedSatellites == null)
                return true;
            int pos = Array.IndexOf(installedSatellites, lookForCulture.Name);
            return pos >= 0;
        }

        private Hashtable GetSatelliteAssembliesFromConfig()
        {
            return null;
        }

        internal class ResourceManagerMediator
        {
            private ResourceManager _rm;
            internal ResourceManagerMediator(ResourceManager rm)
            {
                if (rm == null)
                {
                    throw new ArgumentNullException("rm");
                }

                _rm = rm;
            }

            internal String ModuleDir
            {
                get
                {
                    return _rm.moduleDir;
                }
            }

            internal Type LocationInfo
            {
                get
                {
                    return _rm._locationInfo;
                }
            }

            internal Type UserResourceSet
            {
                get
                {
                    return _rm._userResourceSet;
                }
            }

            internal String BaseNameField
            {
                get
                {
                    return _rm.BaseNameField;
                }
            }

            internal CultureInfo NeutralResourcesCulture
            {
                get
                {
                    return _rm._neutralResourcesCulture;
                }

                set
                {
                    _rm._neutralResourcesCulture = value;
                }
            }

            internal String GetResourceFileName(CultureInfo culture)
            {
                return _rm.GetResourceFileName(culture);
            }

            internal bool LookedForSatelliteContractVersion
            {
                get
                {
                    return _rm._lookedForSatelliteContractVersion;
                }

                set
                {
                    _rm._lookedForSatelliteContractVersion = value;
                }
            }

            internal Version SatelliteContractVersion
            {
                get
                {
                    return _rm._satelliteContractVersion;
                }

                set
                {
                    _rm._satelliteContractVersion = value;
                }
            }

            internal Version ObtainSatelliteContractVersion(Assembly a)
            {
                return ResourceManager.GetSatelliteContractVersion(a);
            }

            internal UltimateResourceFallbackLocation FallbackLoc
            {
                get
                {
                    return _rm.FallbackLocation;
                }

                set
                {
                    _rm._fallbackLoc = value;
                }
            }

            internal RuntimeAssembly CallingAssembly
            {
                get
                {
                    return _rm.m_callingAssembly;
                }
            }

            internal RuntimeAssembly MainAssembly
            {
                get
                {
                    return (RuntimeAssembly)_rm.MainAssembly;
                }
            }

            internal String BaseName
            {
                get
                {
                    return _rm.BaseName;
                }
            }

            internal bool TryLookingForSatellite(CultureInfo lookForCulture)
            {
                return _rm.TryLookingForSatellite(lookForCulture);
            }
        }
    }
}