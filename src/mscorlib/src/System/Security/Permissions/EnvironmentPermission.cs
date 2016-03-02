
using System.Security.Util;

namespace System.Security.Permissions
{
    [Flags]
    public enum EnvironmentPermissionAccess
    {
        NoAccess = 0x00,
        Read = 0x01,
        Write = 0x02,
        AllAccess = 0x03
    }

    internal class EnvironmentStringExpressionSet : StringExpressionSet
    {
        public EnvironmentStringExpressionSet(): base (true, null, false)
        {
        }

        public EnvironmentStringExpressionSet(String str): base (true, str, false)
        {
        }

        protected override StringExpressionSet CreateNewEmpty()
        {
            return new EnvironmentStringExpressionSet();
        }

        protected override bool StringSubsetString(String left, String right, bool ignoreCase)
        {
            return (ignoreCase ? (String.Compare(left, right, StringComparison.OrdinalIgnoreCase) == 0) : (String.Compare(left, right, StringComparison.Ordinal) == 0));
        }

        protected override String ProcessWholeString(String str)
        {
            return str;
        }

        protected override String ProcessSingleString(String str)
        {
            return str;
        }

        public override string ToString()
        {
            return base.UnsafeToString();
        }
    }

    sealed public class EnvironmentPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        private StringExpressionSet m_read;
        private StringExpressionSet m_write;
        private bool m_unrestricted;
        public EnvironmentPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
                m_unrestricted = true;
            else if (state == PermissionState.None)
                m_unrestricted = false;
            else
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
        }

        public EnvironmentPermission(EnvironmentPermissionAccess flag, String pathList)
        {
            SetPathList(flag, pathList);
        }

        public void SetPathList(EnvironmentPermissionAccess flag, String pathList)
        {
            VerifyFlag(flag);
            m_unrestricted = false;
            if ((flag & EnvironmentPermissionAccess.Read) != 0)
                m_read = null;
            if ((flag & EnvironmentPermissionAccess.Write) != 0)
                m_write = null;
            AddPathList(flag, pathList);
        }

        public void AddPathList(EnvironmentPermissionAccess flag, String pathList)
        {
            VerifyFlag(flag);
            if (FlagIsSet(flag, EnvironmentPermissionAccess.Read))
            {
                if (m_read == null)
                    m_read = new EnvironmentStringExpressionSet();
                m_read.AddExpressions(pathList);
            }

            if (FlagIsSet(flag, EnvironmentPermissionAccess.Write))
            {
                if (m_write == null)
                    m_write = new EnvironmentStringExpressionSet();
                m_write.AddExpressions(pathList);
            }
        }

        public String GetPathList(EnvironmentPermissionAccess flag)
        {
            VerifyFlag(flag);
            ExclusiveFlag(flag);
            if (FlagIsSet(flag, EnvironmentPermissionAccess.Read))
            {
                if (m_read == null)
                {
                    return "";
                }

                return m_read.ToString();
            }

            if (FlagIsSet(flag, EnvironmentPermissionAccess.Write))
            {
                if (m_write == null)
                {
                    return "";
                }

                return m_write.ToString();
            }

            return "";
        }

        private void VerifyFlag(EnvironmentPermissionAccess flag)
        {
            if ((flag & ~EnvironmentPermissionAccess.AllAccess) != 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)flag));
                    }

        private void ExclusiveFlag(EnvironmentPermissionAccess flag)
        {
            if (flag == EnvironmentPermissionAccess.NoAccess)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumNotSingleFlag"));
            }

            if (((int)flag & ((int)flag - 1)) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumNotSingleFlag"));
            }

                    }

        private bool FlagIsSet(EnvironmentPermissionAccess flag, EnvironmentPermissionAccess question)
        {
            return (flag & question) != 0;
        }

        private bool IsEmpty()
        {
            return (!m_unrestricted && (this.m_read == null || this.m_read.IsEmpty()) && (this.m_write == null || this.m_write.IsEmpty()));
        }

        public bool IsUnrestricted()
        {
            return m_unrestricted;
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return this.IsEmpty();
            }

            try
            {
                EnvironmentPermission operand = (EnvironmentPermission)target;
                if (operand.IsUnrestricted())
                    return true;
                else if (this.IsUnrestricted())
                    return false;
                else
                    return ((this.m_read == null || this.m_read.IsSubsetOf(operand.m_read)) && (this.m_write == null || this.m_write.IsSubsetOf(operand.m_write)));
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }
            else if (!VerifyType(target))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }
            else if (this.IsUnrestricted())
            {
                return target.Copy();
            }

            EnvironmentPermission operand = (EnvironmentPermission)target;
            if (operand.IsUnrestricted())
            {
                return this.Copy();
            }

            StringExpressionSet intersectRead = this.m_read == null ? null : this.m_read.Intersect(operand.m_read);
            StringExpressionSet intersectWrite = this.m_write == null ? null : this.m_write.Intersect(operand.m_write);
            if ((intersectRead == null || intersectRead.IsEmpty()) && (intersectWrite == null || intersectWrite.IsEmpty()))
            {
                return null;
            }

            EnvironmentPermission intersectPermission = new EnvironmentPermission(PermissionState.None);
            intersectPermission.m_unrestricted = false;
            intersectPermission.m_read = intersectRead;
            intersectPermission.m_write = intersectWrite;
            return intersectPermission;
        }

        public override IPermission Union(IPermission other)
        {
            if (other == null)
            {
                return this.Copy();
            }
            else if (!VerifyType(other))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }

            EnvironmentPermission operand = (EnvironmentPermission)other;
            if (this.IsUnrestricted() || operand.IsUnrestricted())
            {
                return new EnvironmentPermission(PermissionState.Unrestricted);
            }

            StringExpressionSet unionRead = this.m_read == null ? operand.m_read : this.m_read.Union(operand.m_read);
            StringExpressionSet unionWrite = this.m_write == null ? operand.m_write : this.m_write.Union(operand.m_write);
            if ((unionRead == null || unionRead.IsEmpty()) && (unionWrite == null || unionWrite.IsEmpty()))
            {
                return null;
            }

            EnvironmentPermission unionPermission = new EnvironmentPermission(PermissionState.None);
            unionPermission.m_unrestricted = false;
            unionPermission.m_read = unionRead;
            unionPermission.m_write = unionWrite;
            return unionPermission;
        }

        public override IPermission Copy()
        {
            EnvironmentPermission copy = new EnvironmentPermission(PermissionState.None);
            if (this.m_unrestricted)
            {
                copy.m_unrestricted = true;
            }
            else
            {
                copy.m_unrestricted = false;
                if (this.m_read != null)
                {
                    copy.m_read = this.m_read.Copy();
                }

                if (this.m_write != null)
                {
                    copy.m_write = this.m_write.Copy();
                }
            }

            return copy;
        }

        int IBuiltInPermission.GetTokenIndex()
        {
            return EnvironmentPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.EnvironmentPermissionIndex;
        }
    }
}