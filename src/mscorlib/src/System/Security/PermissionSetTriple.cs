using System.Diagnostics.Contracts;
using System.Security.Permissions;

namespace System.Security
{
    sealed internal class PermissionSetTriple
    {
        static private volatile PermissionToken s_zoneToken;
        static private volatile PermissionToken s_urlToken;
        internal PermissionSet AssertSet;
        internal PermissionSet GrantSet;
        internal PermissionSet RefusedSet;
        internal PermissionSetTriple()
        {
            Reset();
        }

        internal PermissionSetTriple(PermissionSetTriple triple)
        {
            this.AssertSet = triple.AssertSet;
            this.GrantSet = triple.GrantSet;
            this.RefusedSet = triple.RefusedSet;
        }

        internal void Reset()
        {
            AssertSet = null;
            GrantSet = null;
            RefusedSet = null;
        }

        internal bool IsEmpty()
        {
            return (AssertSet == null && GrantSet == null && RefusedSet == null);
        }

        private PermissionToken ZoneToken
        {
            [System.Security.SecurityCritical]
            get
            {
                if (s_zoneToken == null)
                    s_zoneToken = PermissionToken.GetToken(typeof (ZoneIdentityPermission));
                return s_zoneToken;
            }
        }

        private PermissionToken UrlToken
        {
            [System.Security.SecurityCritical]
            get
            {
                if (s_urlToken == null)
                    s_urlToken = PermissionToken.GetToken(typeof (UrlIdentityPermission));
                return s_urlToken;
            }
        }

        internal bool Update(PermissionSetTriple psTriple, out PermissionSetTriple retTriple)
        {
            retTriple = null;
            retTriple = UpdateAssert(psTriple.AssertSet);
            if (psTriple.AssertSet != null && psTriple.AssertSet.IsUnrestricted())
            {
                return true;
            }

            UpdateGrant(psTriple.GrantSet);
            UpdateRefused(psTriple.RefusedSet);
            return false;
        }

        internal PermissionSetTriple UpdateAssert(PermissionSet in_a)
        {
            PermissionSetTriple retTriple = null;
            if (in_a != null)
            {
                Contract.Assert((!in_a.IsUnrestricted() || RefusedSet == null), "Cannot be unrestricted or refused must be null");
                if (in_a.IsSubsetOf(AssertSet))
                    return null;
                PermissionSet retPs;
                if (GrantSet != null)
                    retPs = in_a.Intersect(GrantSet);
                else
                {
                    GrantSet = new PermissionSet(true);
                    retPs = in_a.Copy();
                }

                bool bFailedToCompress = false;
                if (RefusedSet != null)
                {
                    retPs = PermissionSet.RemoveRefusedPermissionSet(retPs, RefusedSet, out bFailedToCompress);
                }

                if (!bFailedToCompress)
                    bFailedToCompress = PermissionSet.IsIntersectingAssertedPermissions(retPs, AssertSet);
                if (bFailedToCompress)
                {
                    retTriple = new PermissionSetTriple(this);
                    this.Reset();
                    this.GrantSet = retTriple.GrantSet.Copy();
                }

                if (AssertSet == null)
                    AssertSet = retPs;
                else
                    AssertSet.InplaceUnion(retPs);
            }

            return retTriple;
        }

        internal void UpdateGrant(PermissionSet in_g, out ZoneIdentityPermission z, out UrlIdentityPermission u)
        {
            z = null;
            u = null;
            if (in_g != null)
            {
                if (GrantSet == null)
                    GrantSet = in_g.Copy();
                else
                    GrantSet.InplaceIntersect(in_g);
                z = (ZoneIdentityPermission)in_g.GetPermission(ZoneToken);
                u = (UrlIdentityPermission)in_g.GetPermission(UrlToken);
            }
        }

        internal void UpdateGrant(PermissionSet in_g)
        {
            if (in_g != null)
            {
                if (GrantSet == null)
                    GrantSet = in_g.Copy();
                else
                    GrantSet.InplaceIntersect(in_g);
            }
        }

        internal void UpdateRefused(PermissionSet in_r)
        {
            if (in_r != null)
            {
                if (RefusedSet == null)
                    RefusedSet = in_r.Copy();
                else
                    RefusedSet.InplaceUnion(in_r);
            }
        }

        static bool CheckAssert(PermissionSet pSet, CodeAccessPermission demand, PermissionToken permToken)
        {
            if (pSet != null)
            {
                pSet.CheckDecoded(demand, permToken);
                CodeAccessPermission perm = (CodeAccessPermission)pSet.GetPermission(demand);
                try
                {
                    if (pSet.IsUnrestricted() || demand.CheckAssert(perm))
                    {
                        return SecurityRuntime.StackHalt;
                    }
                }
                catch (ArgumentException)
                {
                }
            }

            return SecurityRuntime.StackContinue;
        }

        static bool CheckAssert(PermissionSet assertPset, PermissionSet demandSet, out PermissionSet newDemandSet)
        {
            newDemandSet = null;
            if (assertPset != null)
            {
                assertPset.CheckDecoded(demandSet);
                if (demandSet.CheckAssertion(assertPset))
                    return SecurityRuntime.StackHalt;
                PermissionSet.RemoveAssertedPermissionSet(demandSet, assertPset, out newDemandSet);
            }

            return SecurityRuntime.StackContinue;
        }

        internal bool CheckDemand(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
        {
            if (CheckAssert(AssertSet, demand, permToken) == SecurityRuntime.StackHalt)
                return SecurityRuntime.StackHalt;
            CodeAccessSecurityEngine.CheckHelper(GrantSet, RefusedSet, demand, permToken, rmh, null, SecurityAction.Demand, true);
            return SecurityRuntime.StackContinue;
        }

        internal bool CheckSetDemand(PermissionSet demandSet, out PermissionSet alteredDemandset, RuntimeMethodHandleInternal rmh)
        {
            alteredDemandset = null;
            if (CheckAssert(AssertSet, demandSet, out alteredDemandset) == SecurityRuntime.StackHalt)
                return SecurityRuntime.StackHalt;
            if (alteredDemandset != null)
                demandSet = alteredDemandset;
            CodeAccessSecurityEngine.CheckSetHelper(GrantSet, RefusedSet, demandSet, rmh, null, SecurityAction.Demand, true);
            return SecurityRuntime.StackContinue;
        }

        internal bool CheckDemandNoThrow(CodeAccessPermission demand, PermissionToken permToken)
        {
            Contract.Assert(AssertSet == null, "AssertSet not null");
            return CodeAccessSecurityEngine.CheckHelper(GrantSet, RefusedSet, demand, permToken, RuntimeMethodHandleInternal.EmptyHandle, null, SecurityAction.Demand, false);
        }

        internal bool CheckSetDemandNoThrow(PermissionSet demandSet)
        {
            Contract.Assert(AssertSet == null, "AssertSet not null");
            return CodeAccessSecurityEngine.CheckSetHelper(GrantSet, RefusedSet, demandSet, RuntimeMethodHandleInternal.EmptyHandle, null, SecurityAction.Demand, false);
        }

        internal bool CheckFlags(ref int flags)
        {
            if (AssertSet != null)
            {
                int assertFlags = SecurityManager.GetSpecialFlags(AssertSet, null);
                if ((flags & assertFlags) != 0)
                    flags = flags & ~assertFlags;
            }

            return (SecurityManager.GetSpecialFlags(GrantSet, RefusedSet) & flags) == flags;
        }
    }
}