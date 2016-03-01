namespace System.Security.Permissions
{
    sealed public class GacIdentityPermissionAttribute : CodeAccessSecurityAttribute
    {
        public GacIdentityPermissionAttribute(SecurityAction action): base (action)
        {
        }

        public override IPermission CreatePermission()
        {
            return new GacIdentityPermission();
        }
    }

    sealed public class GacIdentityPermission : CodeAccessPermission, IBuiltInPermission
    {
        public GacIdentityPermission(PermissionState state)
        {
            if (state != PermissionState.Unrestricted && state != PermissionState.None)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        public GacIdentityPermission()
        {
        }

        public override IPermission Copy()
        {
            return new GacIdentityPermission();
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
                return false;
            if (!(target is GacIdentityPermission))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return true;
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
                return null;
            if (!(target is GacIdentityPermission))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return this.Copy();
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
                return this.Copy();
            if (!(target is GacIdentityPermission))
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            return this.Copy();
        }

        int IBuiltInPermission.GetTokenIndex()
        {
            return GacIdentityPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.GacIdentityPermissionIndex;
        }
    }
}