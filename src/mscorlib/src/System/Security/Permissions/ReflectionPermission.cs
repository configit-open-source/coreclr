

namespace System.Security.Permissions
{
    [Flags]
    public enum ReflectionPermissionFlag
    {
        NoFlags = 0x00,
        TypeInformation = 0x01,
        MemberAccess = 0x02,
        ReflectionEmit = 0x04,
        RestrictedMemberAccess = 0x08,
        AllFlags = 0x07
    }

    sealed public class ReflectionPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        internal const ReflectionPermissionFlag AllFlagsAndMore = ReflectionPermissionFlag.AllFlags | ReflectionPermissionFlag.RestrictedMemberAccess;
        private ReflectionPermissionFlag m_flags;
        public ReflectionPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                SetUnrestricted(true);
            }
            else if (state == PermissionState.None)
            {
                SetUnrestricted(false);
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        public ReflectionPermission(ReflectionPermissionFlag flag)
        {
            VerifyAccess(flag);
            SetUnrestricted(false);
            m_flags = flag;
        }

        private void SetUnrestricted(bool unrestricted)
        {
            if (unrestricted)
            {
                m_flags = ReflectionPermission.AllFlagsAndMore;
            }
            else
            {
                Reset();
            }
        }

        private void Reset()
        {
            m_flags = ReflectionPermissionFlag.NoFlags;
        }

        public ReflectionPermissionFlag Flags
        {
            set
            {
                VerifyAccess(value);
                m_flags = value;
            }

            get
            {
                return m_flags;
            }
        }

        public bool IsUnrestricted()
        {
            return m_flags == ReflectionPermission.AllFlagsAndMore;
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

            ReflectionPermission operand = (ReflectionPermission)other;
            if (this.IsUnrestricted() || operand.IsUnrestricted())
            {
                return new ReflectionPermission(PermissionState.Unrestricted);
            }
            else
            {
                ReflectionPermissionFlag flag_union = (ReflectionPermissionFlag)(m_flags | operand.m_flags);
                return (new ReflectionPermission(flag_union));
            }
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return m_flags == ReflectionPermissionFlag.NoFlags;
            }

            try
            {
                ReflectionPermission operand = (ReflectionPermission)target;
                if (operand.IsUnrestricted())
                    return true;
                else if (this.IsUnrestricted())
                    return false;
                else
                    return (((int)this.m_flags) & ~((int)operand.m_flags)) == 0;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            else if (!VerifyType(target))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }

            ReflectionPermission operand = (ReflectionPermission)target;
            ReflectionPermissionFlag newFlags = operand.m_flags & this.m_flags;
            if (newFlags == ReflectionPermissionFlag.NoFlags)
                return null;
            else
                return new ReflectionPermission(newFlags);
        }

        public override IPermission Copy()
        {
            if (this.IsUnrestricted())
            {
                return new ReflectionPermission(PermissionState.Unrestricted);
            }
            else
            {
                return new ReflectionPermission((ReflectionPermissionFlag)m_flags);
            }
        }

        private void VerifyAccess(ReflectionPermissionFlag type)
        {
            if ((type & ~ReflectionPermission.AllFlagsAndMore) != 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)type));
                    }

        int IBuiltInPermission.GetTokenIndex()
        {
            return ReflectionPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.ReflectionPermissionIndex;
        }
    }
}