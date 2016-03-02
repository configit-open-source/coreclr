using System.Security;

namespace System.IO.IsolatedStorage
{
    public enum IsolatedStorageSecurityOptions
    {
        GetRootUserDirectory = 0,
        GetGroupAndIdForApplication = 1,
        GetGroupAndIdForSite = 2,
        IncreaseQuotaForGroup = 3,
        DefaultQuotaForGroup = 4,
        AvailableFreeSpace = 5,
        IsolatedStorageFolderName = 6
    }

    public class IsolatedStorageSecurityState : SecurityState
    {
        private Int64 m_UsedSize;
        private Int64 m_Quota;
        private string m_Id;
        private string m_Group;
        private string m_RootUserDirectory;
        private string m_IsolatedStorageFolderName;
        private Int64 m_AvailableFreeSpace;
        private bool m_AvailableFreeSpaceComputed;
        private IsolatedStorageSecurityOptions m_Options;
        internal static IsolatedStorageSecurityState CreateStateToGetRootUserDirectory()
        {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.GetRootUserDirectory;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateToGetGroupAndIdForApplication()
        {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.GetGroupAndIdForApplication;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateToGetGroupAndIdForSite()
        {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.GetGroupAndIdForSite;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateToIncreaseQuotaForGroup(String group, Int64 newQuota, Int64 usedSize)
        {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.IncreaseQuotaForGroup;
            state.m_Group = group;
            state.m_Quota = newQuota;
            state.m_UsedSize = usedSize;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateToGetAvailableFreeSpace()
        {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.AvailableFreeSpace;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateForIsolatedStorageFolderName()
        {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.IsolatedStorageFolderName;
            return state;
        }

        private IsolatedStorageSecurityState()
        {
        }

        public IsolatedStorageSecurityOptions Options
        {
            get
            {
                return m_Options;
            }
        }

        public String Group
        {
            get
            {
                return m_Group;
            }

            set
            {
                m_Group = value;
            }
        }

        public String Id
        {
            get
            {
                return m_Id;
            }

            set
            {
                m_Id = value;
            }
        }

        public String RootUserDirectory
        {
            get
            {
                return m_RootUserDirectory;
            }

            set
            {
                m_RootUserDirectory = value;
            }
        }

        public Int64 UsedSize
        {
            get
            {
                return m_UsedSize;
            }
        }

        public Int64 Quota
        {
            get
            {
                return m_Quota;
            }

            set
            {
                m_Quota = value;
            }
        }

        public Int64 AvailableFreeSpace
        {
            get
            {
                return m_AvailableFreeSpace;
            }

            set
            {
                m_AvailableFreeSpace = value;
                m_AvailableFreeSpaceComputed = true;
            }
        }

        public bool AvailableFreeSpaceComputed
        {
            get
            {
                return m_AvailableFreeSpaceComputed;
            }

            set
            {
                m_AvailableFreeSpaceComputed = value;
            }
        }

        public string IsolatedStorageFolderName
        {
            get
            {
                return m_IsolatedStorageFolderName;
            }

            set
            {
                m_IsolatedStorageFolderName = value;
            }
        }

        public override void EnsureState()
        {
            if (!IsStateAvailable())
            {
                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
            }
        }
    }
}