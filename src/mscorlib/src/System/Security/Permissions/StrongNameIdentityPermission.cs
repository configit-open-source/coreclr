using System.Collections.Generic;

namespace System.Security.Permissions
{
    sealed internal class StrongName2
    {
        public StrongNamePublicKeyBlob m_publicKeyBlob;
        public String m_name;
        public Version m_version;
        public StrongName2(StrongNamePublicKeyBlob publicKeyBlob, String name, Version version)
        {
            m_publicKeyBlob = publicKeyBlob;
            m_name = name;
            m_version = version;
        }

        public StrongName2 Copy()
        {
            return new StrongName2(m_publicKeyBlob, m_name, m_version);
        }

        public bool IsSubsetOf(StrongName2 target)
        {
            if (this.m_publicKeyBlob == null)
                return true;
            if (!this.m_publicKeyBlob.Equals(target.m_publicKeyBlob))
                return false;
            if (this.m_name != null)
            {
                if (target.m_name == null || !System.Security.Policy.StrongName.CompareNames(target.m_name, this.m_name))
                    return false;
            }

            if ((Object)this.m_version != null)
            {
                if ((Object)target.m_version == null || target.m_version.CompareTo(this.m_version) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public StrongName2 Intersect(StrongName2 target)
        {
            if (target.IsSubsetOf(this))
                return target.Copy();
            else if (this.IsSubsetOf(target))
                return this.Copy();
            else
                return null;
        }

        public bool Equals(StrongName2 target)
        {
            if (!target.IsSubsetOf(this))
                return false;
            if (!this.IsSubsetOf(target))
                return false;
            return true;
        }
    }

    sealed public class StrongNameIdentityPermission : CodeAccessPermission, IBuiltInPermission
    {
        private bool m_unrestricted;
        private StrongName2[] m_strongNames;
        public StrongNameIdentityPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_unrestricted = true;
            }
            else if (state == PermissionState.None)
            {
                m_unrestricted = false;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        public StrongNameIdentityPermission(StrongNamePublicKeyBlob blob, String name, Version version)
        {
            if (blob == null)
                throw new ArgumentNullException("blob");
            if (name != null && name.Equals(""))
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyStrongName"));
                        m_unrestricted = false;
            m_strongNames = new StrongName2[1];
            m_strongNames[0] = new StrongName2(blob, name, version);
        }

        public StrongNamePublicKeyBlob PublicKey
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException("PublicKey");
                                m_unrestricted = false;
                if (m_strongNames != null && m_strongNames.Length == 1)
                    m_strongNames[0].m_publicKeyBlob = value;
                else
                {
                    m_strongNames = new StrongName2[1];
                    m_strongNames[0] = new StrongName2(value, "", new Version());
                }
            }

            get
            {
                if (m_strongNames == null || m_strongNames.Length == 0)
                    return null;
                if (m_strongNames.Length > 1)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AmbiguousIdentity"));
                return m_strongNames[0].m_publicKeyBlob;
            }
        }

        public String Name
        {
            set
            {
                if (value != null && value.Length == 0)
                    throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"));
                                m_unrestricted = false;
                if (m_strongNames != null && m_strongNames.Length == 1)
                    m_strongNames[0].m_name = value;
                else
                {
                    m_strongNames = new StrongName2[1];
                    m_strongNames[0] = new StrongName2(null, value, new Version());
                }
            }

            get
            {
                if (m_strongNames == null || m_strongNames.Length == 0)
                    return "";
                if (m_strongNames.Length > 1)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AmbiguousIdentity"));
                return m_strongNames[0].m_name;
            }
        }

        public Version Version
        {
            set
            {
                m_unrestricted = false;
                if (m_strongNames != null && m_strongNames.Length == 1)
                    m_strongNames[0].m_version = value;
                else
                {
                    m_strongNames = new StrongName2[1];
                    m_strongNames[0] = new StrongName2(null, "", value);
                }
            }

            get
            {
                if (m_strongNames == null || m_strongNames.Length == 0)
                    return new Version();
                if (m_strongNames.Length > 1)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_AmbiguousIdentity"));
                return m_strongNames[0].m_version;
            }
        }

        public override IPermission Copy()
        {
            StrongNameIdentityPermission perm = new StrongNameIdentityPermission(PermissionState.None);
            perm.m_unrestricted = this.m_unrestricted;
            if (this.m_strongNames != null)
            {
                perm.m_strongNames = new StrongName2[this.m_strongNames.Length];
                int n;
                for (n = 0; n < this.m_strongNames.Length; n++)
                    perm.m_strongNames[n] = this.m_strongNames[n].Copy();
            }

            return perm;
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                if (m_unrestricted)
                    return false;
                if (m_strongNames == null)
                    return true;
                if (m_strongNames.Length == 0)
                    return true;
                return false;
            }

            StrongNameIdentityPermission that = target as StrongNameIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if (that.m_unrestricted)
                return true;
            if (m_unrestricted)
                return false;
            if (this.m_strongNames != null)
            {
                foreach (StrongName2 snThis in m_strongNames)
                {
                    bool bOK = false;
                    if (that.m_strongNames != null)
                    {
                        foreach (StrongName2 snThat in that.m_strongNames)
                        {
                            if (snThis.IsSubsetOf(snThat))
                            {
                                bOK = true;
                                break;
                            }
                        }
                    }

                    if (!bOK)
                        return false;
                }
            }

            return true;
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            StrongNameIdentityPermission that = target as StrongNameIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if (this.m_unrestricted && that.m_unrestricted)
            {
                StrongNameIdentityPermission res = new StrongNameIdentityPermission(PermissionState.None);
                res.m_unrestricted = true;
                return res;
            }

            if (this.m_unrestricted)
                return that.Copy();
            if (that.m_unrestricted)
                return this.Copy();
            if (this.m_strongNames == null || that.m_strongNames == null || this.m_strongNames.Length == 0 || that.m_strongNames.Length == 0)
                return null;
            List<StrongName2> alStrongNames = new List<StrongName2>();
            foreach (StrongName2 snThis in this.m_strongNames)
            {
                foreach (StrongName2 snThat in that.m_strongNames)
                {
                    StrongName2 snInt = (StrongName2)snThis.Intersect(snThat);
                    if (snInt != null)
                        alStrongNames.Add(snInt);
                }
            }

            if (alStrongNames.Count == 0)
                return null;
            StrongNameIdentityPermission result = new StrongNameIdentityPermission(PermissionState.None);
            result.m_strongNames = alStrongNames.ToArray();
            return result;
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
            {
                if ((this.m_strongNames == null || this.m_strongNames.Length == 0) && !this.m_unrestricted)
                    return null;
                return this.Copy();
            }

            StrongNameIdentityPermission that = target as StrongNameIdentityPermission;
            if (that == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            if (this.m_unrestricted || that.m_unrestricted)
            {
                StrongNameIdentityPermission res = new StrongNameIdentityPermission(PermissionState.None);
                res.m_unrestricted = true;
                return res;
            }

            if (this.m_strongNames == null || this.m_strongNames.Length == 0)
            {
                if (that.m_strongNames == null || that.m_strongNames.Length == 0)
                    return null;
                return that.Copy();
            }

            if (that.m_strongNames == null || that.m_strongNames.Length == 0)
                return this.Copy();
            List<StrongName2> alStrongNames = new List<StrongName2>();
            foreach (StrongName2 snThis in this.m_strongNames)
                alStrongNames.Add(snThis);
            foreach (StrongName2 snThat in that.m_strongNames)
            {
                bool bDupe = false;
                foreach (StrongName2 sn in alStrongNames)
                {
                    if (snThat.Equals(sn))
                    {
                        bDupe = true;
                        break;
                    }
                }

                if (!bDupe)
                    alStrongNames.Add(snThat);
            }

            StrongNameIdentityPermission result = new StrongNameIdentityPermission(PermissionState.None);
            result.m_strongNames = alStrongNames.ToArray();
            return result;
        }

        int IBuiltInPermission.GetTokenIndex()
        {
            return StrongNameIdentityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.StrongNameIdentityPermissionIndex;
        }
    }
}