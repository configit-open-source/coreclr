
using System.Globalization;

using Microsoft.Win32.SafeHandles;

namespace System.Security
{
    internal class FrameSecurityDescriptor
    {
        private PermissionSet m_assertions;
        private PermissionSet m_denials;
        private PermissionSet m_restriction;
        private PermissionSet m_DeclarativeAssertions;
        private PermissionSet m_DeclarativeDenials;
        private PermissionSet m_DeclarativeRestrictions;
        private SafeAccessTokenHandle m_callerToken;
        private SafeAccessTokenHandle m_impToken;
        private bool m_AssertFT;
        private bool m_assertAllPossible;
        private bool m_declSecComputed;
        private static extern void IncrementOverridesCount();
        private static extern void DecrementOverridesCount();
        private static extern void IncrementAssertCount();
        private static extern void DecrementAssertCount();
        internal FrameSecurityDescriptor()
        {
        }

        private PermissionSet CreateSingletonSet(IPermission perm)
        {
            PermissionSet permSet = new PermissionSet(false);
            permSet.AddPermission(perm.Copy());
            return permSet;
        }

        internal bool HasImperativeAsserts()
        {
            return (m_assertions != null);
        }

        internal bool HasImperativeDenials()
        {
            return (m_denials != null);
        }

        internal bool HasImperativeRestrictions()
        {
            return (m_restriction != null);
        }

        internal void SetAssert(IPermission perm)
        {
            m_assertions = CreateSingletonSet(perm);
            IncrementAssertCount();
        }

        internal void SetAssert(PermissionSet permSet)
        {
            m_assertions = permSet.Copy();
            m_AssertFT = m_AssertFT || m_assertions.IsUnrestricted();
            IncrementAssertCount();
        }

        internal PermissionSet GetAssertions(bool fDeclarative)
        {
            return (fDeclarative) ? m_DeclarativeAssertions : m_assertions;
        }

        internal void SetAssertAllPossible()
        {
            m_assertAllPossible = true;
            IncrementAssertCount();
        }

        internal bool GetAssertAllPossible()
        {
            return m_assertAllPossible;
        }

        internal void SetDeny(IPermission perm)
        {
            m_denials = CreateSingletonSet(perm);
            IncrementOverridesCount();
        }

        internal void SetDeny(PermissionSet permSet)
        {
            m_denials = permSet.Copy();
            IncrementOverridesCount();
        }

        internal PermissionSet GetDenials(bool fDeclarative)
        {
            return (fDeclarative) ? m_DeclarativeDenials : m_denials;
        }

        internal void SetPermitOnly(IPermission perm)
        {
            m_restriction = CreateSingletonSet(perm);
            IncrementOverridesCount();
        }

        internal void SetPermitOnly(PermissionSet permSet)
        {
            m_restriction = permSet.Copy();
            IncrementOverridesCount();
        }

        internal PermissionSet GetPermitOnly(bool fDeclarative)
        {
            return (fDeclarative) ? m_DeclarativeRestrictions : m_restriction;
        }

        internal void SetTokenHandles(SafeAccessTokenHandle callerToken, SafeAccessTokenHandle impToken)
        {
            m_callerToken = callerToken;
            m_impToken = impToken;
        }

        internal void RevertAssert()
        {
            if (m_assertions != null)
            {
                m_assertions = null;
                DecrementAssertCount();
            }

            if (m_DeclarativeAssertions != null)
            {
                m_AssertFT = m_DeclarativeAssertions.IsUnrestricted();
            }
            else
            {
                m_AssertFT = false;
            }
        }

        internal void RevertAssertAllPossible()
        {
            if (m_assertAllPossible)
            {
                m_assertAllPossible = false;
                DecrementAssertCount();
            }
        }

        internal void RevertDeny()
        {
            if (HasImperativeDenials())
            {
                DecrementOverridesCount();
                m_denials = null;
            }
        }

        internal void RevertPermitOnly()
        {
            if (HasImperativeRestrictions())
            {
                DecrementOverridesCount();
                m_restriction = null;
                ;
            }
        }

        internal void RevertAll()
        {
            RevertAssert();
            RevertAssertAllPossible();
            RevertDeny();
            RevertPermitOnly();
        }

        internal bool CheckDemand(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
        {
            bool fContinue = CheckDemand2(demand, permToken, rmh, false);
            if (fContinue == SecurityRuntime.StackContinue)
            {
                fContinue = CheckDemand2(demand, permToken, rmh, true);
            }

            return fContinue;
        }

        internal bool CheckDemand2(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh, bool fDeclarative)
        {
            PermissionSet permSet;
                        if (GetPermitOnly(fDeclarative) != null)
                GetPermitOnly(fDeclarative).CheckDecoded(demand, permToken);
            if (GetDenials(fDeclarative) != null)
                GetDenials(fDeclarative).CheckDecoded(demand, permToken);
            if (GetAssertions(fDeclarative) != null)
                GetAssertions(fDeclarative).CheckDecoded(demand, permToken);
            bool bThreadSecurity = SecurityManager._SetThreadSecurity(false);
            try
            {
                permSet = GetPermitOnly(fDeclarative);
                if (permSet != null)
                {
                    CodeAccessPermission perm = (CodeAccessPermission)permSet.GetPermission(demand);
                    if (perm == null)
                    {
                        if (!permSet.IsUnrestricted())
                            throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), null, permSet, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
                    }
                    else
                    {
                        bool bNeedToThrow = true;
                        try
                        {
                            bNeedToThrow = !demand.CheckPermitOnly(perm);
                        }
                        catch (ArgumentException)
                        {
                        }

                        if (bNeedToThrow)
                            throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), null, permSet, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
                    }
                }

                permSet = GetDenials(fDeclarative);
                if (permSet != null)
                {
                    CodeAccessPermission perm = (CodeAccessPermission)permSet.GetPermission(demand);
                    if (permSet.IsUnrestricted())
                        throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), permSet, null, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
                    bool bNeedToThrow = true;
                    try
                    {
                        bNeedToThrow = !demand.CheckDeny(perm);
                    }
                    catch (ArgumentException)
                    {
                    }

                    if (bNeedToThrow)
                        throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), permSet, null, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
                }

                if (GetAssertAllPossible())
                {
                    return SecurityRuntime.StackHalt;
                }

                permSet = GetAssertions(fDeclarative);
                if (permSet != null)
                {
                    CodeAccessPermission perm = (CodeAccessPermission)permSet.GetPermission(demand);
                    try
                    {
                        if (permSet.IsUnrestricted() || demand.CheckAssert(perm))
                        {
                            return SecurityRuntime.StackHalt;
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            finally
            {
                if (bThreadSecurity)
                    SecurityManager._SetThreadSecurity(true);
            }

            return SecurityRuntime.StackContinue;
        }

        internal bool CheckSetDemand(PermissionSet demandSet, out PermissionSet alteredDemandSet, RuntimeMethodHandleInternal rmh)
        {
            PermissionSet altPset1 = null, altPset2 = null;
            bool fContinue = CheckSetDemand2(demandSet, out altPset1, rmh, false);
            if (altPset1 != null)
            {
                demandSet = altPset1;
            }

            if (fContinue == SecurityRuntime.StackContinue)
            {
                fContinue = CheckSetDemand2(demandSet, out altPset2, rmh, true);
            }

            if (altPset2 != null)
                alteredDemandSet = altPset2;
            else if (altPset1 != null)
                alteredDemandSet = altPset1;
            else
                alteredDemandSet = null;
            return fContinue;
        }

        internal bool CheckSetDemand2(PermissionSet demandSet, out PermissionSet alteredDemandSet, RuntimeMethodHandleInternal rmh, bool fDeclarative)
        {
            PermissionSet permSet;
            alteredDemandSet = null;
            if (demandSet == null || demandSet.IsEmpty())
                return SecurityRuntime.StackHalt;
            if (GetPermitOnly(fDeclarative) != null)
                GetPermitOnly(fDeclarative).CheckDecoded(demandSet);
            if (GetDenials(fDeclarative) != null)
                GetDenials(fDeclarative).CheckDecoded(demandSet);
            if (GetAssertions(fDeclarative) != null)
                GetAssertions(fDeclarative).CheckDecoded(demandSet);
            bool bThreadSecurity = SecurityManager._SetThreadSecurity(false);
            try
            {
                permSet = GetPermitOnly(fDeclarative);
                if (permSet != null)
                {
                    IPermission permFailed = null;
                    bool bNeedToThrow = true;
                    try
                    {
                        bNeedToThrow = !demandSet.CheckPermitOnly(permSet, out permFailed);
                    }
                    catch (ArgumentException)
                    {
                    }

                    if (bNeedToThrow)
                        throw new SecurityException(Environment.GetResourceString("Security_GenericNoType"), null, permSet, SecurityRuntime.GetMethodInfo(rmh), demandSet, permFailed);
                }

                permSet = GetDenials(fDeclarative);
                if (permSet != null)
                {
                    IPermission permFailed = null;
                    bool bNeedToThrow = true;
                    try
                    {
                        bNeedToThrow = !demandSet.CheckDeny(permSet, out permFailed);
                    }
                    catch (ArgumentException)
                    {
                    }

                    if (bNeedToThrow)
                        throw new SecurityException(Environment.GetResourceString("Security_GenericNoType"), permSet, null, SecurityRuntime.GetMethodInfo(rmh), demandSet, permFailed);
                }

                if (GetAssertAllPossible())
                {
                    return SecurityRuntime.StackHalt;
                }

                permSet = GetAssertions(fDeclarative);
                if (permSet != null)
                {
                    if (demandSet.CheckAssertion(permSet))
                        return SecurityRuntime.StackHalt;
                    if (!permSet.IsUnrestricted())
                    {
                        PermissionSet.RemoveAssertedPermissionSet(demandSet, permSet, out alteredDemandSet);
                    }
                }
            }
            finally
            {
                if (bThreadSecurity)
                    SecurityManager._SetThreadSecurity(true);
            }

            return SecurityRuntime.StackContinue;
        }
    }
}