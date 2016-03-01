using System.Threading;

namespace System.Security
{
    abstract public class CodeAccessPermission : IPermission, ISecurityEncodable, IStackWalk
    {
        public static void RevertAssert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertAssert(ref stackMark);
        }

        public static void RevertDeny()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertDeny(ref stackMark);
        }

        public static void RevertPermitOnly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertPermitOnly(ref stackMark);
        }

        public static void RevertAll()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertAll(ref stackMark);
        }

        public void Demand()
        {
            if (!this.CheckDemand(null))
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
                CodeAccessSecurityEngine.Check(this, ref stackMark);
            }
        }

        internal static void Demand(PermissionType permissionType)
        {
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
            CodeAccessSecurityEngine.SpecialDemand(permissionType, ref stackMark);
        }

        public void Assert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            CodeAccessSecurityEngine.Assert(this, ref stackMark);
        }

        static internal void Assert(bool allPossible)
        {
                        StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.AssertAllPossible(ref stackMark);
        }

        public void Deny()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            CodeAccessSecurityEngine.Deny(this, ref stackMark);
        }

        public void PermitOnly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            CodeAccessSecurityEngine.PermitOnly(this, ref stackMark);
        }

        public virtual IPermission Union(IPermission other)
        {
            if (other == null)
                return (this.Copy());
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SecurityPermissionUnion"));
        }

        internal bool VerifyType(IPermission perm)
        {
            if ((perm == null) || (perm.GetType() != this.GetType()))
            {
                return (false);
            }
            else
            {
                return (true);
            }
        }

        public abstract IPermission Copy();
        public abstract IPermission Intersect(IPermission target);
        public abstract bool IsSubsetOf(IPermission target);
        public override bool Equals(Object obj)
        {
            IPermission perm = obj as IPermission;
            if (obj != null && perm == null)
                return false;
            try
            {
                if (!this.IsSubsetOf(perm))
                    return false;
                if (perm != null && !perm.IsSubsetOf(this))
                    return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal bool CheckDemand(CodeAccessPermission grant)
        {
                        return IsSubsetOf(grant);
        }

        internal bool CheckPermitOnly(CodeAccessPermission permitted)
        {
                        return IsSubsetOf(permitted);
        }

        internal bool CheckDeny(CodeAccessPermission denied)
        {
                        IPermission intersectPerm = Intersect(denied);
            return (intersectPerm == null || intersectPerm.IsSubsetOf(null));
        }

        internal bool CheckAssert(CodeAccessPermission asserted)
        {
                        return IsSubsetOf(asserted);
        }
    }
}