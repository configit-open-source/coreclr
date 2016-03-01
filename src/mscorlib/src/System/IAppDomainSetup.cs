namespace System
{
    using System.Runtime.InteropServices;

    public interface IAppDomainSetup
    {
        String ApplicationBase
        {
            get;
            set;
        }

        String ApplicationName
        {
            get;
            set;
        }

        String CachePath
        {
            get;
            set;
        }

        String ConfigurationFile
        {
            get;
            set;
        }

        String DynamicBase
        {
            get;
            set;
        }

        String LicenseFile
        {
            get;
            set;
        }

        String PrivateBinPath
        {
            get;
            set;
        }

        String PrivateBinPathProbe
        {
            get;
            set;
        }

        String ShadowCopyDirectories
        {
            get;
            set;
        }

        String ShadowCopyFiles
        {
            get;
            set;
        }
    }
}