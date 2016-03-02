using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Policy;
using System.Text;

namespace System
{
    public sealed class AppDomainSetup : IAppDomainSetup
    {
        internal enum LoaderInformation
        {
            ApplicationBaseValue = 0,
            ConfigurationFileValue = 1,
            DynamicBaseValue = 2,
            DevPathValue = 3,
            ApplicationNameValue = 4,
            PrivateBinPathValue = 5,
            PrivateBinPathProbeValue = 6,
            ShadowCopyDirectoriesValue = 7,
            ShadowCopyFilesValue = 8,
            CachePathValue = 9,
            LicenseFileValue = 10,
            DisallowPublisherPolicyValue = 11,
            DisallowCodeDownloadValue = 12,
            DisallowBindingRedirectsValue = 13,
            DisallowAppBaseProbingValue = 14,
            ConfigurationBytesValue = 15,
            LoaderMaximum = 18
        }

        private const string LOADER_OPTIMIZATION = "LOADER_OPTIMIZATION";
        private const string CONFIGURATION_EXTENSION = ".config";
        private const string APPENV_RELATIVEPATH = "RELPATH";
        private const string MACHINE_CONFIGURATION_FILE = "config\\machine.config";
        private const string ACTAG_HOST_CONFIG_FILE = "HOST_CONFIG";
        private const string ACTAG_APP_CONFIG_FILE = "APP_CONFIG_FILE";
        private const string ACTAG_MACHINE_CONFIG = "MACHINE_CONFIG";
        private const string ACTAG_APP_BASE_URL = "APPBASE";
        private const string ACTAG_APP_NAME = "APP_NAME";
        private const string ACTAG_BINPATH_PROBE_ONLY = "BINPATH_PROBE_ONLY";
        private const string ACTAG_APP_CACHE_BASE = "CACHE_BASE";
        private const string ACTAG_DEV_PATH = "DEV_PATH";
        private const string ACTAG_APP_DYNAMIC_BASE = "DYNAMIC_BASE";
        private const string ACTAG_FORCE_CACHE_INSTALL = "FORCE_CACHE_INSTALL";
        private const string ACTAG_APP_PRIVATE_BINPATH = "PRIVATE_BINPATH";
        private const string ACTAG_APP_SHADOW_COPY_DIRS = "SHADOW_COPY_DIRS";
        private const string ACTAG_DISALLOW_APPLYPUBLISHERPOLICY = "DISALLOW_APP";
        private const string ACTAG_CODE_DOWNLOAD_DISABLED = "CODE_DOWNLOAD_DISABLED";
        private const string ACTAG_DISALLOW_APP_BINDING_REDIRECTS = "DISALLOW_APP_REDIRECTS";
        private const string ACTAG_DISALLOW_APP_BASE_PROBING = "DISALLOW_APP_BASE_PROBING";
        private const string ACTAG_APP_CONFIG_BLOB = "APP_CONFIG_BLOB";
        private string[] _Entries;
        private LoaderOptimization _LoaderOptimization;
        private String _AppBase;
        private AppDomainInitializer _AppDomainInitializer;
        private string[] _AppDomainInitializerArguments;
        private string _ApplicationTrust;
        private byte[] _ConfigurationBytes;
        private bool _DisableInterfaceCache = false;
        private string _AppDomainManagerAssembly;
        private string _AppDomainManagerType;
        private Dictionary<string, object> _CompatFlags;
        private String _TargetFrameworkName;
        private bool _CheckedForTargetFrameworkName;
        private bool _UseRandomizedStringHashing;
        internal AppDomainSetup(AppDomainSetup copy, bool copyDomainBoundData)
        {
            string[] mine = Value;
            if (copy != null)
            {
                string[] other = copy.Value;
                int mineSize = _Entries.Length;
                int otherSize = other.Length;
                int size = (otherSize < mineSize) ? otherSize : mineSize;
                for (int i = 0; i < size; i++)
                    mine[i] = other[i];
                if (size < mineSize)
                {
                    for (int i = size; i < mineSize; i++)
                        mine[i] = null;
                }

                _LoaderOptimization = copy._LoaderOptimization;
                _AppDomainInitializerArguments = copy.AppDomainInitializerArguments;
                _ApplicationTrust = copy._ApplicationTrust;
                if (copyDomainBoundData)
                    _AppDomainInitializer = copy.AppDomainInitializer;
                else
                    _AppDomainInitializer = null;
                _ConfigurationBytes = copy.GetConfigurationBytes();
                _DisableInterfaceCache = copy._DisableInterfaceCache;
                _AppDomainManagerAssembly = copy.AppDomainManagerAssembly;
                _AppDomainManagerType = copy.AppDomainManagerType;
                if (copy._CompatFlags != null)
                {
                    SetCompatibilitySwitches(copy._CompatFlags.Keys);
                }

                _TargetFrameworkName = copy._TargetFrameworkName;
                _UseRandomizedStringHashing = copy._UseRandomizedStringHashing;
            }
            else
                _LoaderOptimization = LoaderOptimization.NotSpecified;
        }

        public AppDomainSetup()
        {
            _LoaderOptimization = LoaderOptimization.NotSpecified;
        }

        internal void SetupDefaults(string imageLocation, bool imageLocationAlreadyNormalized = false)
        {
            char[] sep = {'\\', '/'};
            int i = imageLocation.LastIndexOfAny(sep);
            if (i == -1)
            {
                ApplicationName = imageLocation;
            }
            else
            {
                ApplicationName = imageLocation.Substring(i + 1);
                string appBase = imageLocation.Substring(0, i + 1);
                if (imageLocationAlreadyNormalized)
                    Value[(int)LoaderInformation.ApplicationBaseValue] = appBase;
                else
                    ApplicationBase = appBase;
            }

            ConfigurationFile = ApplicationName + AppDomainSetup.ConfigurationExtension;
        }

        internal string[] Value
        {
            get
            {
                if (_Entries == null)
                    _Entries = new String[(int)LoaderInformation.LoaderMaximum];
                return _Entries;
            }
        }

        internal String GetUnsecureApplicationBase()
        {
            return Value[(int)LoaderInformation.ApplicationBaseValue];
        }

        public string AppDomainManagerAssembly
        {
            get
            {
                return _AppDomainManagerAssembly;
            }

            set
            {
                _AppDomainManagerAssembly = value;
            }
        }

        public string AppDomainManagerType
        {
            get
            {
                return _AppDomainManagerType;
            }

            set
            {
                _AppDomainManagerType = value;
            }
        }

        public String ApplicationBase
        {
            [System.Security.SecurityCritical]
            
            get
            {
                return VerifyDir(GetUnsecureApplicationBase(), false);
            }

            [System.Security.SecurityCritical]
            set
            {
                Value[(int)LoaderInformation.ApplicationBaseValue] = NormalizePath(value, false);
            }
        }

        private String NormalizePath(String path, bool useAppBase)
        {
            if (path == null)
                return null;
            if (!useAppBase)
                path = System.Security.Util.URLString.PreProcessForExtendedPathRemoval(path, false);
            int len = path.Length;
            if (len == 0)
                return null;
            bool UNCpath = false;
            if ((len > 7) && (String.Compare(path, 0, "file:", 0, 5, StringComparison.OrdinalIgnoreCase) == 0))
            {
                int trim;
                if (path[6] == '\\')
                {
                    if ((path[7] == '\\') || (path[7] == '/'))
                    {
                        if ((len > 8) && ((path[8] == '\\') || (path[8] == '/')))
                            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
                        else
                            trim = 8;
                    }
                    else
                    {
                        trim = 5;
                        UNCpath = true;
                    }
                }
                else if (path[7] == '/')
                    trim = 8;
                else
                {
                    if ((len > 8) && (path[7] == '\\') && (path[8] == '\\'))
                        trim = 7;
                    else
                    {
                        trim = 5;
                        System.Text.StringBuilder winPathBuilder = new System.Text.StringBuilder(len);
                        for (int i = 0; i < len; i++)
                        {
                            char c = path[i];
                            if (c == '/')
                                winPathBuilder.Append('\\');
                            else
                                winPathBuilder.Append(c);
                        }

                        path = winPathBuilder.ToString();
                    }

                    UNCpath = true;
                }

                path = path.Substring(trim);
                len -= trim;
            }

            bool localPath;
            if (UNCpath || ((len > 1) && ((path[0] == '/') || (path[0] == '\\')) && ((path[1] == '/') || (path[1] == '\\'))))
                localPath = false;
            else
            {
                int colon = path.IndexOf(':') + 1;
                if ((colon != 0) && (len > colon + 1) && ((path[colon] == '/') || (path[colon] == '\\')) && ((path[colon + 1] == '/') || (path[colon + 1] == '\\')))
                    localPath = false;
                else
                    localPath = true;
            }

            if (localPath)
            {
                if (useAppBase && ((len == 1) || (path[1] != ':')))
                {
                    String appBase = Value[(int)LoaderInformation.ApplicationBaseValue];
                    if ((appBase == null) || (appBase.Length == 0))
                        throw new MemberAccessException(Environment.GetResourceString("AppDomain_AppBaseNotSet"));
                    StringBuilder result = StringBuilderCache.Acquire();
                    bool slash = false;
                    if ((path[0] == '/') || (path[0] == '\\'))
                    {
                        String pathRoot = Path.GetPathRoot(appBase);
                        if (pathRoot.Length == 0)
                        {
                            int index = appBase.IndexOf(":/", StringComparison.Ordinal);
                            if (index == -1)
                                index = appBase.IndexOf(":\\", StringComparison.Ordinal);
                            int urlLen = appBase.Length;
                            for (index += 1; (index < urlLen) && ((appBase[index] == '/') || (appBase[index] == '\\')); index++)
                                ;
                            for (; (index < urlLen) && (appBase[index] != '/') && (appBase[index] != '\\'); index++)
                                ;
                            pathRoot = appBase.Substring(0, index);
                        }

                        result.Append(pathRoot);
                        slash = true;
                    }
                    else
                        result.Append(appBase);
                    int aLen = result.Length - 1;
                    if ((result[aLen] != '/') && (result[aLen] != '\\'))
                    {
                        if (!slash)
                        {
                            if (appBase.IndexOf(":/", StringComparison.Ordinal) == -1)
                                result.Append('\\');
                            else
                                result.Append('/');
                        }
                    }
                    else if (slash)
                        result.Remove(aLen, 1);
                    result.Append(path);
                    path = StringBuilderCache.GetStringAndRelease(result);
                }
                else
                    path = Path.GetFullPathInternal(path);
            }

            return path;
        }

        private bool IsFilePath(String path)
        {
            return (path[1] == ':') || ((path[0] == '\\') && (path[1] == '\\'));
        }

        internal static String ApplicationBaseKey
        {
            get
            {
                return ACTAG_APP_BASE_URL;
            }
        }

        public String ConfigurationFile
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return VerifyDir(Value[(int)LoaderInformation.ConfigurationFileValue], true);
            }

            set
            {
                Value[(int)LoaderInformation.ConfigurationFileValue] = value;
            }
        }

        internal String ConfigurationFileInternal
        {
            get
            {
                return NormalizePath(Value[(int)LoaderInformation.ConfigurationFileValue], true);
            }
        }

        internal static String ConfigurationFileKey
        {
            get
            {
                return ACTAG_APP_CONFIG_FILE;
            }
        }

        public byte[] GetConfigurationBytes()
        {
            if (_ConfigurationBytes == null)
                return null;
            return (byte[])_ConfigurationBytes.Clone();
        }

        public void SetConfigurationBytes(byte[] value)
        {
            _ConfigurationBytes = value;
        }

        private static String ConfigurationBytesKey
        {
            get
            {
                return ACTAG_APP_CONFIG_BLOB;
            }
        }

        internal Dictionary<string, object> GetCompatibilityFlags()
        {
            return _CompatFlags;
        }

        public void SetCompatibilitySwitches(IEnumerable<String> switches)
        {
            _UseRandomizedStringHashing = false;
            if (switches != null)
            {
                _CompatFlags = new Dictionary<string, object>();
                foreach (String str in switches)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals("UseRandomizedStringHashAlgorithm", str))
                    {
                        _UseRandomizedStringHashing = true;
                    }

                    _CompatFlags.Add(str, null);
                }
            }
            else
            {
                _CompatFlags = null;
            }
        }

        public String TargetFrameworkName
        {
            get
            {
                return _TargetFrameworkName;
            }

            set
            {
                _TargetFrameworkName = value;
            }
        }

        internal bool CheckedForTargetFrameworkName
        {
            get
            {
                return _CheckedForTargetFrameworkName;
            }

            set
            {
                _CheckedForTargetFrameworkName = value;
            }
        }

        public String DynamicBase
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return VerifyDir(Value[(int)LoaderInformation.DynamicBaseValue], true);
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                if (value == null)
                    Value[(int)LoaderInformation.DynamicBaseValue] = null;
                else
                {
                    if (ApplicationName == null)
                        throw new MemberAccessException(Environment.GetResourceString("AppDomain_RequireApplicationName"));
                    StringBuilder s = new StringBuilder(NormalizePath(value, false));
                    s.Append('\\');
                    string h = ParseNumbers.IntToString(ApplicationName.GetLegacyNonRandomizedHashCode(), 16, 8, '0', ParseNumbers.PrintAsI4);
                    s.Append(h);
                    Value[(int)LoaderInformation.DynamicBaseValue] = s.ToString();
                }
            }
        }

        internal static String DynamicBaseKey
        {
            get
            {
                return ACTAG_APP_DYNAMIC_BASE;
            }
        }

        public bool DisallowPublisherPolicy
        {
            get
            {
                return (Value[(int)LoaderInformation.DisallowPublisherPolicyValue] != null);
            }

            set
            {
                if (value)
                    Value[(int)LoaderInformation.DisallowPublisherPolicyValue] = "true";
                else
                    Value[(int)LoaderInformation.DisallowPublisherPolicyValue] = null;
            }
        }

        public bool DisallowBindingRedirects
        {
            get
            {
                return (Value[(int)LoaderInformation.DisallowBindingRedirectsValue] != null);
            }

            set
            {
                if (value)
                    Value[(int)LoaderInformation.DisallowBindingRedirectsValue] = "true";
                else
                    Value[(int)LoaderInformation.DisallowBindingRedirectsValue] = null;
            }
        }

        public bool DisallowCodeDownload
        {
            get
            {
                return (Value[(int)LoaderInformation.DisallowCodeDownloadValue] != null);
            }

            set
            {
                if (value)
                    Value[(int)LoaderInformation.DisallowCodeDownloadValue] = "true";
                else
                    Value[(int)LoaderInformation.DisallowCodeDownloadValue] = null;
            }
        }

        public bool DisallowApplicationBaseProbing
        {
            get
            {
                return (Value[(int)LoaderInformation.DisallowAppBaseProbingValue] != null);
            }

            set
            {
                if (value)
                    Value[(int)LoaderInformation.DisallowAppBaseProbingValue] = "true";
                else
                    Value[(int)LoaderInformation.DisallowAppBaseProbingValue] = null;
            }
        }

        private String VerifyDir(String dir, bool normalize)
        {
            if (dir != null)
            {
                if (dir.Length == 0)
                    dir = null;
                else
                {
                    if (normalize)
                        dir = NormalizePath(dir, true);
                }
            }

            return dir;
        }

        private void VerifyDirList(String dirs)
        {
            if (dirs != null)
            {
                String[] dirArray = dirs.Split(';');
                int len = dirArray.Length;
                for (int i = 0; i < len; i++)
                    VerifyDir(dirArray[i], true);
            }
        }

        internal String DeveloperPath
        {
            [System.Security.SecurityCritical]
            get
            {
                String dirs = Value[(int)LoaderInformation.DevPathValue];
                VerifyDirList(dirs);
                return dirs;
            }

            set
            {
                if (value == null)
                    Value[(int)LoaderInformation.DevPathValue] = null;
                else
                {
                    String[] directories = value.Split(';');
                    int size = directories.Length;
                    StringBuilder newPath = StringBuilderCache.Acquire();
                    bool fDelimiter = false;
                    for (int i = 0; i < size; i++)
                    {
                        if (directories[i].Length != 0)
                        {
                            if (fDelimiter)
                                newPath.Append(";");
                            else
                                fDelimiter = true;
                            newPath.Append(Path.GetFullPathInternal(directories[i]));
                        }
                    }

                    String newString = StringBuilderCache.GetStringAndRelease(newPath);
                    if (newString.Length == 0)
                        Value[(int)LoaderInformation.DevPathValue] = null;
                    else
                        Value[(int)LoaderInformation.DevPathValue] = newString;
                }
            }
        }

        internal static String DisallowPublisherPolicyKey
        {
            get
            {
                return ACTAG_DISALLOW_APPLYPUBLISHERPOLICY;
            }
        }

        internal static String DisallowCodeDownloadKey
        {
            get
            {
                return ACTAG_CODE_DOWNLOAD_DISABLED;
            }
        }

        internal static String DisallowBindingRedirectsKey
        {
            get
            {
                return ACTAG_DISALLOW_APP_BINDING_REDIRECTS;
            }
        }

        internal static String DeveloperPathKey
        {
            get
            {
                return ACTAG_DEV_PATH;
            }
        }

        internal static String DisallowAppBaseProbingKey
        {
            get
            {
                return ACTAG_DISALLOW_APP_BASE_PROBING;
            }
        }

        public String ApplicationName
        {
            get
            {
                return Value[(int)LoaderInformation.ApplicationNameValue];
            }

            set
            {
                Value[(int)LoaderInformation.ApplicationNameValue] = value;
            }
        }

        internal static String ApplicationNameKey
        {
            get
            {
                return ACTAG_APP_NAME;
            }
        }

        public AppDomainInitializer AppDomainInitializer
        {
            get
            {
                return _AppDomainInitializer;
            }

            set
            {
                _AppDomainInitializer = value;
            }
        }

        public string[] AppDomainInitializerArguments
        {
            get
            {
                return _AppDomainInitializerArguments;
            }

            set
            {
                _AppDomainInitializerArguments = value;
            }
        }

        internal ApplicationTrust InternalGetApplicationTrust()
        {
            if (_ApplicationTrust == null)
                return null;
            ApplicationTrust grantSet = new ApplicationTrust(NamedPermissionSet.GetBuiltInSet(_ApplicationTrust));
            return grantSet;
        }

        internal void InternalSetApplicationTrust(String permissionSetName)
        {
            _ApplicationTrust = permissionSetName;
        }

        internal ApplicationTrust ApplicationTrust
        {
            get
            {
                return InternalGetApplicationTrust();
            }
        }

        public String PrivateBinPath
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String dirs = Value[(int)LoaderInformation.PrivateBinPathValue];
                VerifyDirList(dirs);
                return dirs;
            }

            set
            {
                Value[(int)LoaderInformation.PrivateBinPathValue] = value;
            }
        }

        internal static String PrivateBinPathKey
        {
            get
            {
                return ACTAG_APP_PRIVATE_BINPATH;
            }
        }

        public String PrivateBinPathProbe
        {
            get
            {
                return Value[(int)LoaderInformation.PrivateBinPathProbeValue];
            }

            set
            {
                Value[(int)LoaderInformation.PrivateBinPathProbeValue] = value;
            }
        }

        internal static String PrivateBinPathProbeKey
        {
            get
            {
                return ACTAG_BINPATH_PROBE_ONLY;
            }
        }

        public String ShadowCopyDirectories
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String dirs = Value[(int)LoaderInformation.ShadowCopyDirectoriesValue];
                VerifyDirList(dirs);
                return dirs;
            }

            set
            {
                Value[(int)LoaderInformation.ShadowCopyDirectoriesValue] = value;
            }
        }

        internal static String ShadowCopyDirectoriesKey
        {
            get
            {
                return ACTAG_APP_SHADOW_COPY_DIRS;
            }
        }

        public String ShadowCopyFiles
        {
            get
            {
                return Value[(int)LoaderInformation.ShadowCopyFilesValue];
            }

            set
            {
                if ((value != null) && (String.Compare(value, "true", StringComparison.OrdinalIgnoreCase) == 0))
                    Value[(int)LoaderInformation.ShadowCopyFilesValue] = value;
                else
                    Value[(int)LoaderInformation.ShadowCopyFilesValue] = null;
            }
        }

        internal static String ShadowCopyFilesKey
        {
            get
            {
                return ACTAG_FORCE_CACHE_INSTALL;
            }
        }

        public String CachePath
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return VerifyDir(Value[(int)LoaderInformation.CachePathValue], false);
            }

            set
            {
                Value[(int)LoaderInformation.CachePathValue] = NormalizePath(value, false);
            }
        }

        internal static String CachePathKey
        {
            get
            {
                return ACTAG_APP_CACHE_BASE;
            }
        }

        public String LicenseFile
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return VerifyDir(Value[(int)LoaderInformation.LicenseFileValue], true);
            }

            set
            {
                Value[(int)LoaderInformation.LicenseFileValue] = value;
            }
        }

        public LoaderOptimization LoaderOptimization
        {
            get
            {
                return _LoaderOptimization;
            }

            set
            {
                _LoaderOptimization = value;
            }
        }

        internal static string LoaderOptimizationKey
        {
            get
            {
                return LOADER_OPTIMIZATION;
            }
        }

        internal static string ConfigurationExtension
        {
            get
            {
                return CONFIGURATION_EXTENSION;
            }
        }

        internal static String PrivateBinPathEnvironmentVariable
        {
            get
            {
                return APPENV_RELATIVEPATH;
            }
        }

        internal static string RuntimeConfigurationFile
        {
            get
            {
                return MACHINE_CONFIGURATION_FILE;
            }
        }

        internal static string MachineConfigKey
        {
            get
            {
                return ACTAG_MACHINE_CONFIG;
            }
        }

        internal static string HostBindingKey
        {
            get
            {
                return ACTAG_HOST_CONFIG_FILE;
            }
        }

        static internal int Locate(String s)
        {
            if (String.IsNullOrEmpty(s))
                return -1;
                        if (s[0] == 'A' && s == ACTAG_APP_BASE_URL)
                return (int)LoaderInformation.ApplicationBaseValue;
            return -1;
        }

        public bool SandboxInterop
        {
            get
            {
                return _DisableInterfaceCache;
            }

            set
            {
                _DisableInterfaceCache = value;
            }
        }
    }
}