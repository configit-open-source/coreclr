using System;
using System.Collections.Generic;
using System.Security.Permissions;

namespace System.IO.IsolatedStorage
{
    public class IsolatedStorageGroup
    {
        public string m_Group;
        public Int64 m_Quota;
        public Int64 m_UsedSize;
        private string m_GroupPath;
        private string m_ObfuscatedId;
        internal IsolatedStorageGroup(string group, long quota, long used, string groupPath)
        {
            m_Group = group;
            m_Quota = quota;
            m_UsedSize = used;
            m_ObfuscatedId = DirectoryInfo.UnsafeCreateDirectoryInfo(groupPath).Name;
            m_GroupPath = groupPath;
        }

        public string Group
        {
            get
            {
                return m_Group;
            }
        }

        public Int64 Quota
        {
            get
            {
                return m_Quota;
            }
        }

        public Int64 UsedSize
        {
            get
            {
                return m_UsedSize;
            }
        }

        public static Boolean Enabled
        {
            [System.Security.SecurityCritical]
            get
            {
                try
                {
                    return !File.UnsafeExists(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.c_DisabledFileName));
                }
                catch (IsolatedStorageException)
                {
                    return false;
                }
            }

            [System.Security.SecurityCritical]
            set
            {
                if (!value)
                {
                    IsolatedStorageFile.TouchFile(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.c_DisabledFileName));
                }
                else
                {
                    try
                    {
                        File.UnsafeDelete(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.c_DisabledFileName));
                    }
                    catch (IOException)
                    {
                    }
                }
            }
        }

        public void Remove()
        {
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    isf.Remove();
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        public static IEnumerable<IsolatedStorageGroup> GetGroups()
        {
            List<IsolatedStorageGroup> groups = new List<IsolatedStorageGroup>();
            try
            {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    groups.Add(new IsolatedStorageGroup(isf.GroupName, Int64.MaxValue, 0, IsolatedStorageFile.IsolatedStorageRoot));
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IsolatedStorageException)
            {
            }

            return groups;
        }

        public static void RemoveAll()
        {
            foreach (IsolatedStorageGroup g in GetGroups())
            {
                g.Remove();
            }
        }
    }
}