namespace System.Security
{
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    sealed internal class PermissionListSet
    {
        private PermissionSetTriple m_firstPermSetTriple;
        private ArrayList m_permSetTriples;
        internal PermissionListSet()
        {
        }

        private void EnsureTriplesListCreated()
        {
            if (m_permSetTriples == null)
            {
                m_permSetTriples = new ArrayList();
                if (m_firstPermSetTriple != null)
                {
                    m_permSetTriples.Add(m_firstPermSetTriple);
                    m_firstPermSetTriple = null;
                }
            }
        }

        private void Terminate(PermissionSetTriple currentTriple)
        {
            UpdateTripleListAndCreateNewTriple(currentTriple, null);
        }

        private void Terminate(PermissionSetTriple currentTriple, PermissionListSet pls)
        {
            this.UpdatePermissions(currentTriple, pls);
            this.UpdateTripleListAndCreateNewTriple(currentTriple, null);
        }

        private bool Update(PermissionSetTriple currentTriple, PermissionListSet pls)
        {
            return this.UpdatePermissions(currentTriple, pls);
        }

        private bool Update(PermissionSetTriple currentTriple, FrameSecurityDescriptor fsd)
        {
            bool fHalt = Update2(currentTriple, fsd, false);
            if (!fHalt)
            {
                fHalt = Update2(currentTriple, fsd, true);
            }

            return fHalt;
        }

        private bool Update2(PermissionSetTriple currentTriple, FrameSecurityDescriptor fsd, bool fDeclarative)
        {
            PermissionSet deniedPset = fsd.GetDenials(fDeclarative);
            if (deniedPset != null)
            {
                currentTriple.UpdateRefused(deniedPset);
            }

            PermissionSet permitOnlyPset = fsd.GetPermitOnly(fDeclarative);
            if (permitOnlyPset != null)
            {
                currentTriple.UpdateGrant(permitOnlyPset);
            }

            if (fsd.GetAssertAllPossible())
            {
                if (currentTriple.GrantSet == null)
                    currentTriple.GrantSet = PermissionSet.s_fullTrust;
                UpdateTripleListAndCreateNewTriple(currentTriple, m_permSetTriples);
                currentTriple.GrantSet = PermissionSet.s_fullTrust;
                currentTriple.UpdateAssert(fsd.GetAssertions(fDeclarative));
                return true;
            }

            PermissionSet assertPset = fsd.GetAssertions(fDeclarative);
            if (assertPset != null)
            {
                if (assertPset.IsUnrestricted())
                {
                    if (currentTriple.GrantSet == null)
                        currentTriple.GrantSet = PermissionSet.s_fullTrust;
                    UpdateTripleListAndCreateNewTriple(currentTriple, m_permSetTriples);
                    currentTriple.GrantSet = PermissionSet.s_fullTrust;
                    currentTriple.UpdateAssert(assertPset);
                    return true;
                }

                PermissionSetTriple retTriple = currentTriple.UpdateAssert(assertPset);
                if (retTriple != null)
                {
                    EnsureTriplesListCreated();
                    m_permSetTriples.Add(retTriple);
                }
            }

            return false;
        }

        private void Update(PermissionSetTriple currentTriple, PermissionSet in_g, PermissionSet in_r)
        {
            currentTriple.UpdateGrant(in_g);
            currentTriple.UpdateRefused(in_r);
        }

        private void Update(PermissionSet in_g)
        {
            if (m_firstPermSetTriple == null)
                m_firstPermSetTriple = new PermissionSetTriple();
            Update(m_firstPermSetTriple, in_g, null);
        }

        private bool UpdatePermissions(PermissionSetTriple currentTriple, PermissionListSet pls)
        {
            if (pls != null)
            {
                if (pls.m_permSetTriples != null)
                {
                    UpdateTripleListAndCreateNewTriple(currentTriple, pls.m_permSetTriples);
                }
                else
                {
                    PermissionSetTriple tmp_psTriple = pls.m_firstPermSetTriple;
                    PermissionSetTriple retTriple;
                    if (currentTriple.Update(tmp_psTriple, out retTriple))
                        return true;
                    if (retTriple != null)
                    {
                        EnsureTriplesListCreated();
                        m_permSetTriples.Add(retTriple);
                    }
                }
            }
            else
            {
                UpdateTripleListAndCreateNewTriple(currentTriple, null);
            }

            return false;
        }

        private void UpdateTripleListAndCreateNewTriple(PermissionSetTriple currentTriple, ArrayList tripleList)
        {
            if (!currentTriple.IsEmpty())
            {
                if (m_firstPermSetTriple == null && m_permSetTriples == null)
                {
                    m_firstPermSetTriple = new PermissionSetTriple(currentTriple);
                }
                else
                {
                    EnsureTriplesListCreated();
                    m_permSetTriples.Add(new PermissionSetTriple(currentTriple));
                }

                currentTriple.Reset();
            }

            if (tripleList != null)
            {
                EnsureTriplesListCreated();
                m_permSetTriples.AddRange(tripleList);
            }
        }

        private static void UpdateArrayList(ArrayList current, ArrayList newList)
        {
            if (newList == null)
                return;
            for (int i = 0; i < newList.Count; i++)
            {
                if (!current.Contains(newList[i]))
                    current.Add(newList[i]);
            }
        }

        internal bool CheckDemandNoThrow(CodeAccessPermission demand)
        {
            Contract.Assert(m_permSetTriples == null && m_firstPermSetTriple != null, "More than one PermissionSetTriple encountered in AD PermissionListSet");
            PermissionToken permToken = null;
            if (demand != null)
                permToken = PermissionToken.GetToken(demand);
            return m_firstPermSetTriple.CheckDemandNoThrow(demand, permToken);
        }

        internal bool CheckSetDemandNoThrow(PermissionSet pSet)
        {
            Contract.Assert(m_permSetTriples == null && m_firstPermSetTriple != null, "More than one PermissionSetTriple encountered in AD PermissionListSet");
            return m_firstPermSetTriple.CheckSetDemandNoThrow(pSet);
        }

        internal bool CheckDemand(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
        {
            bool bRet = SecurityRuntime.StackContinue;
            if (m_permSetTriples != null)
            {
                for (int i = 0; (i < m_permSetTriples.Count && bRet != SecurityRuntime.StackHalt); i++)
                {
                    PermissionSetTriple psTriple = (PermissionSetTriple)m_permSetTriples[i];
                    bRet = psTriple.CheckDemand(demand, permToken, rmh);
                }
            }
            else if (m_firstPermSetTriple != null)
            {
                bRet = m_firstPermSetTriple.CheckDemand(demand, permToken, rmh);
            }

            return bRet;
        }

        internal bool CheckSetDemand(PermissionSet pset, RuntimeMethodHandleInternal rmh)
        {
            PermissionSet unused;
            CheckSetDemandWithModification(pset, out unused, rmh);
            return SecurityRuntime.StackHalt;
        }

        internal bool CheckSetDemandWithModification(PermissionSet pset, out PermissionSet alteredDemandSet, RuntimeMethodHandleInternal rmh)
        {
            bool bRet = SecurityRuntime.StackContinue;
            PermissionSet demandSet = pset;
            alteredDemandSet = null;
            if (m_permSetTriples != null)
            {
                for (int i = 0; (i < m_permSetTriples.Count && bRet != SecurityRuntime.StackHalt); i++)
                {
                    PermissionSetTriple psTriple = (PermissionSetTriple)m_permSetTriples[i];
                    bRet = psTriple.CheckSetDemand(demandSet, out alteredDemandSet, rmh);
                    if (alteredDemandSet != null)
                        demandSet = alteredDemandSet;
                }
            }
            else if (m_firstPermSetTriple != null)
            {
                bRet = m_firstPermSetTriple.CheckSetDemand(demandSet, out alteredDemandSet, rmh);
            }

            return bRet;
        }

        private bool CheckFlags(int flags)
        {
            Contract.Assert(flags != 0, "Invalid permission flag demand");
            bool check = true;
            if (m_permSetTriples != null)
            {
                for (int i = 0; i < m_permSetTriples.Count && check && flags != 0; i++)
                {
                    check &= ((PermissionSetTriple)m_permSetTriples[i]).CheckFlags(ref flags);
                }
            }
            else if (m_firstPermSetTriple != null)
            {
                check = m_firstPermSetTriple.CheckFlags(ref flags);
            }

            return check;
        }

        internal void DemandFlagsOrGrantSet(int flags, PermissionSet grantSet)
        {
            if (CheckFlags(flags))
                return;
            CheckSetDemand(grantSet, RuntimeMethodHandleInternal.EmptyHandle);
        }
    }
}