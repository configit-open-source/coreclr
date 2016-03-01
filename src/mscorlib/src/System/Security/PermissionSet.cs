namespace System.Security
{
    using System;
    using System.Threading;
    using System.Security.Util;
    using System.Collections;
    using System.IO;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Security.Policy;
    using BindingFlags = System.Reflection.BindingFlags;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    internal enum SpecialPermissionSetFlag
    {
        Regular = 0,
        NoSet = 1,
        EmptySet = 2,
        SkipVerification = 3
    }

    public class PermissionSet : ISecurityEncodable, ICollection, IStackWalk, IDeserializationCallback
    {
        internal static readonly bool debug;
        private static void DEBUG_WRITE(String str)
        {
            if (debug)
                Console.WriteLine(str);
        }

        private static void DEBUG_COND_WRITE(bool exp, String str)
        {
            if (debug && (exp))
                Console.WriteLine(str);
        }

        private static void DEBUG_PRINTSTACK(Exception e)
        {
            if (debug)
                Console.Error.WriteLine((e).StackTrace);
        }

        private bool m_Unrestricted;
        private bool m_allPermissionsDecoded = false;
        internal TokenBasedSet m_permSet = null;
        private bool m_ignoreTypeLoadFailures = false;
        private String m_serializedPermissionSet;
        private bool m_CheckedForNonCas;
        private bool m_ContainsCas;
        private bool m_ContainsNonCas;
        private TokenBasedSet m_permSetSaved;
        private bool readableonly;
        private TokenBasedSet m_unrestrictedPermSet;
        private TokenBasedSet m_normalPermSet;
        private bool m_canUnrestrictedOverride;
        internal static readonly PermissionSet s_fullTrust = new PermissionSet(true);
        private void OnSerialized(StreamingContext context)
        {
            Contract.Assert(false, "PermissionSet does not support serialization on CoreCLR");
        }

        internal PermissionSet()
        {
            Reset();
            m_Unrestricted = true;
        }

        internal PermissionSet(bool fUnrestricted): this ()
        {
            SetUnrestricted(fUnrestricted);
        }

        public PermissionSet(PermissionState state): this ()
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

        public PermissionSet(PermissionSet permSet): this ()
        {
            if (permSet == null)
            {
                Reset();
                return;
            }

            m_Unrestricted = permSet.m_Unrestricted;
            m_CheckedForNonCas = permSet.m_CheckedForNonCas;
            m_ContainsCas = permSet.m_ContainsCas;
            m_ContainsNonCas = permSet.m_ContainsNonCas;
            m_ignoreTypeLoadFailures = permSet.m_ignoreTypeLoadFailures;
            if (permSet.m_permSet != null)
            {
                m_permSet = new TokenBasedSet(permSet.m_permSet);
                for (int i = m_permSet.GetStartingIndex(); i <= m_permSet.GetMaxUsedIndex(); i++)
                {
                    Object obj = m_permSet.GetItem(i);
                    IPermission perm = obj as IPermission;
                    if (perm != null)
                    {
                        m_permSet.SetItem(i, perm.Copy());
                    }
                }
            }
        }

        public virtual void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(this);
            while (enumerator.MoveNext())
            {
                array.SetValue(enumerator.Current, index++);
            }
        }

        private PermissionSet(Object trash, Object junk)
        {
            m_Unrestricted = false;
        }

        public virtual Object SyncRoot
        {
            get
            {
                return this;
            }
        }

        public virtual bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        internal void Reset()
        {
            m_Unrestricted = false;
            m_allPermissionsDecoded = true;
            m_permSet = null;
            m_ignoreTypeLoadFailures = false;
            m_CheckedForNonCas = false;
            m_ContainsCas = false;
            m_ContainsNonCas = false;
            m_permSetSaved = null;
        }

        internal void CheckSet()
        {
            if (this.m_permSet == null)
                this.m_permSet = new TokenBasedSet();
        }

        public bool IsEmpty()
        {
            if (m_Unrestricted)
                return false;
            if (m_permSet == null || m_permSet.FastIsEmpty())
                return true;
            PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(this);
            while (enumerator.MoveNext())
            {
                IPermission perm = (IPermission)enumerator.Current;
                if (!perm.IsSubsetOf(null))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool FastIsEmpty()
        {
            if (m_Unrestricted)
                return false;
            if (m_permSet == null || m_permSet.FastIsEmpty())
                return true;
            return false;
        }

        public virtual int Count
        {
            get
            {
                int count = 0;
                if (m_permSet != null)
                    count += m_permSet.GetCount();
                return count;
            }
        }

        internal IPermission GetPermission(int index)
        {
            if (m_permSet == null)
                return null;
            Object obj = m_permSet.GetItem(index);
            if (obj == null)
                return null;
            IPermission perm = obj as IPermission;
            if (perm != null)
                return perm;
            if (perm == null)
                return null;
            Contract.Assert(PermissionToken.IsTokenProperlyAssigned(perm, PermissionToken.GetToken(perm)), "PermissionToken was improperly assigned");
            Contract.Assert(PermissionToken.GetToken(perm).m_index == index, "Assigning permission to incorrect index in tokenbasedset");
            return perm;
        }

        internal IPermission GetPermission(PermissionToken permToken)
        {
            if (permToken == null)
                return null;
            return GetPermission(permToken.m_index);
        }

        internal IPermission GetPermission(IPermission perm)
        {
            if (perm == null)
                return null;
            return GetPermission(PermissionToken.GetToken(perm));
        }

        public IPermission SetPermission(IPermission perm)
        {
            return SetPermissionImpl(perm);
        }

        protected virtual IPermission SetPermissionImpl(IPermission perm)
        {
            if (perm == null)
                return null;
            PermissionToken permToken = PermissionToken.GetToken(perm);
            if ((permToken.m_type & PermissionTokenType.IUnrestricted) != 0)
            {
                m_Unrestricted = false;
            }

            CheckSet();
            IPermission currPerm = GetPermission(permToken.m_index);
            m_CheckedForNonCas = false;
            m_permSet.SetItem(permToken.m_index, perm);
            return perm;
        }

        public IPermission AddPermission(IPermission perm)
        {
            return AddPermissionImpl(perm);
        }

        protected virtual IPermission AddPermissionImpl(IPermission perm)
        {
            if (perm == null)
                return null;
            m_CheckedForNonCas = false;
            PermissionToken permToken = PermissionToken.GetToken(perm);
            if (this.IsUnrestricted() && ((permToken.m_type & PermissionTokenType.IUnrestricted) != 0))
            {
                Type perm_type = perm.GetType();
                Object[] objs = new Object[1];
                objs[0] = PermissionState.Unrestricted;
                return (IPermission)Activator.CreateInstance(perm_type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, objs, null);
            }

            CheckSet();
            IPermission currPerm = GetPermission(permToken.m_index);
            if (currPerm != null)
            {
                IPermission ip_union = currPerm.Union(perm);
                m_permSet.SetItem(permToken.m_index, ip_union);
                return ip_union;
            }
            else
            {
                m_permSet.SetItem(permToken.m_index, perm);
                return perm;
            }
        }

        private IPermission RemovePermission(int index)
        {
            IPermission perm = GetPermission(index);
            if (perm == null)
                return null;
            return (IPermission)m_permSet.RemoveItem(index);
        }

        internal void SetUnrestricted(bool unrestricted)
        {
            m_Unrestricted = unrestricted;
            if (unrestricted)
            {
                m_permSet = null;
            }
        }

        public bool IsUnrestricted()
        {
            return m_Unrestricted;
        }

        internal enum IsSubsetOfType
        {
            Normal,
            CheckDemand,
            CheckPermitOnly,
            CheckAssertion
        }

        internal bool IsSubsetOfHelper(PermissionSet target, IsSubsetOfType type, out IPermission firstPermThatFailed, bool ignoreNonCas)
        {
            if (debug)
                DEBUG_WRITE("IsSubsetOf\n" + "Other:\n" + (target == null ? "<null>" : target.ToString()) + "\nMe:\n" + ToString());
            firstPermThatFailed = null;
            if (target == null || target.FastIsEmpty())
            {
                if (this.IsEmpty())
                    return true;
                else
                {
                    firstPermThatFailed = GetFirstPerm();
                    return false;
                }
            }
            else if (this.IsUnrestricted() && !target.IsUnrestricted())
                return false;
            else if (this.m_permSet == null)
                return true;
            else
            {
                target.CheckSet();
                for (int i = m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); ++i)
                {
                    IPermission thisPerm = this.GetPermission(i);
                    if (thisPerm == null || thisPerm.IsSubsetOf(null))
                        continue;
                    IPermission targetPerm = target.GetPermission(i);
                    PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
                    Contract.Assert(targetPerm == null || (token.m_type & PermissionTokenType.DontKnow) == 0, "Token not properly initialized");
                    if (target.m_Unrestricted)
                        continue;
                    CodeAccessPermission cap = thisPerm as CodeAccessPermission;
                    if (cap == null)
                    {
                        if (!ignoreNonCas && !thisPerm.IsSubsetOf(targetPerm))
                        {
                            firstPermThatFailed = thisPerm;
                            return false;
                        }
                    }
                    else
                    {
                        firstPermThatFailed = thisPerm;
                        switch (type)
                        {
                            case IsSubsetOfType.Normal:
                                if (!thisPerm.IsSubsetOf(targetPerm))
                                    return false;
                                break;
                            case IsSubsetOfType.CheckDemand:
                                if (!cap.CheckDemand((CodeAccessPermission)targetPerm))
                                    return false;
                                break;
                            case IsSubsetOfType.CheckPermitOnly:
                                if (!cap.CheckPermitOnly((CodeAccessPermission)targetPerm))
                                    return false;
                                break;
                            case IsSubsetOfType.CheckAssertion:
                                if (!cap.CheckAssert((CodeAccessPermission)targetPerm))
                                    return false;
                                break;
                        }

                        firstPermThatFailed = null;
                    }
                }
            }

            return true;
        }

        public bool IsSubsetOf(PermissionSet target)
        {
            IPermission perm;
            return IsSubsetOfHelper(target, IsSubsetOfType.Normal, out perm, false);
        }

        internal bool CheckDemand(PermissionSet target, out IPermission firstPermThatFailed)
        {
            return IsSubsetOfHelper(target, IsSubsetOfType.CheckDemand, out firstPermThatFailed, true);
        }

        internal bool CheckPermitOnly(PermissionSet target, out IPermission firstPermThatFailed)
        {
            return IsSubsetOfHelper(target, IsSubsetOfType.CheckPermitOnly, out firstPermThatFailed, true);
        }

        internal bool CheckAssertion(PermissionSet target)
        {
            IPermission perm;
            return IsSubsetOfHelper(target, IsSubsetOfType.CheckAssertion, out perm, true);
        }

        internal bool CheckDeny(PermissionSet deniedSet, out IPermission firstPermThatFailed)
        {
            firstPermThatFailed = null;
            if (deniedSet == null || deniedSet.FastIsEmpty() || this.FastIsEmpty())
                return true;
            if (this.m_Unrestricted && deniedSet.m_Unrestricted)
                return false;
            CodeAccessPermission permThis, permThat;
            PermissionSetEnumeratorInternal enumThis = new PermissionSetEnumeratorInternal(this);
            while (enumThis.MoveNext())
            {
                permThis = enumThis.Current as CodeAccessPermission;
                if (permThis == null || permThis.IsSubsetOf(null))
                    continue;
                if (deniedSet.m_Unrestricted)
                {
                    firstPermThatFailed = permThis;
                    return false;
                }

                permThat = (CodeAccessPermission)deniedSet.GetPermission(enumThis.GetCurrentIndex());
                if (!permThis.CheckDeny(permThat))
                {
                    firstPermThatFailed = permThis;
                    return false;
                }
            }

            if (this.m_Unrestricted)
            {
                PermissionSetEnumeratorInternal enumThat = new PermissionSetEnumeratorInternal(deniedSet);
                while (enumThat.MoveNext())
                {
                    if (enumThat.Current is IPermission)
                        return false;
                }
            }

            return true;
        }

        internal void CheckDecoded(CodeAccessPermission demandedPerm, PermissionToken tokenDemandedPerm)
        {
            Contract.Assert(demandedPerm != null, "Expected non-null value");
            if (this.m_allPermissionsDecoded || this.m_permSet == null)
                return;
            if (tokenDemandedPerm == null)
                tokenDemandedPerm = PermissionToken.GetToken(demandedPerm);
            Contract.Assert(tokenDemandedPerm != null, "Unable to find token for demanded permission");
            CheckDecoded(tokenDemandedPerm.m_index);
        }

        internal void CheckDecoded(int index)
        {
            if (this.m_allPermissionsDecoded || this.m_permSet == null)
                return;
            GetPermission(index);
        }

        internal void CheckDecoded(PermissionSet demandedSet)
        {
            Contract.Assert(demandedSet != null, "Expected non-null value");
            if (this.m_allPermissionsDecoded || this.m_permSet == null)
                return;
            PermissionSetEnumeratorInternal enumerator = demandedSet.GetEnumeratorInternal();
            while (enumerator.MoveNext())
            {
                CheckDecoded(enumerator.GetCurrentIndex());
            }
        }

        internal void InplaceIntersect(PermissionSet other)
        {
            Exception savedException = null;
            m_CheckedForNonCas = false;
            if (this == other)
                return;
            if (other == null || other.FastIsEmpty())
            {
                Reset();
                return;
            }

            if (this.FastIsEmpty())
                return;
            int maxMax = this.m_permSet == null ? -1 : this.m_permSet.GetMaxUsedIndex();
            int otherMax = other.m_permSet == null ? -1 : other.m_permSet.GetMaxUsedIndex();
            if (this.IsUnrestricted() && maxMax < otherMax)
            {
                maxMax = otherMax;
                this.CheckSet();
            }

            if (other.IsUnrestricted())
            {
                other.CheckSet();
            }

            for (int i = 0; i <= maxMax; ++i)
            {
                Object thisObj = this.m_permSet.GetItem(i);
                IPermission thisPerm = thisObj as IPermission;
                Object otherObj = other.m_permSet.GetItem(i);
                IPermission otherPerm = otherObj as IPermission;
                if (thisObj == null && otherObj == null)
                    continue;
                if (thisObj == null)
                {
                    if (this.IsUnrestricted())
                    {
                        {
                            PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
                            if ((token.m_type & PermissionTokenType.IUnrestricted) != 0)
                            {
                                this.m_permSet.SetItem(i, otherPerm.Copy());
                                Contract.Assert(PermissionToken.s_tokenSet.GetItem(i) != null, "PermissionToken should already be assigned");
                            }
                        }
                    }
                }
                else if (otherObj == null)
                {
                    if (other.IsUnrestricted())
                    {
                        {
                            PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
                            if ((token.m_type & PermissionTokenType.IUnrestricted) == 0)
                                this.m_permSet.SetItem(i, null);
                        }
                    }
                    else
                    {
                        this.m_permSet.SetItem(i, null);
                    }
                }
                else
                {
                    try
                    {
                        IPermission intersectPerm;
                        if (thisPerm == null)
                            intersectPerm = otherPerm;
                        else if (otherPerm == null)
                            intersectPerm = thisPerm;
                        else
                            intersectPerm = thisPerm.Intersect(otherPerm);
                        this.m_permSet.SetItem(i, intersectPerm);
                    }
                    catch (Exception e)
                    {
                        if (savedException == null)
                            savedException = e;
                    }
                }
            }

            this.m_Unrestricted = this.m_Unrestricted && other.m_Unrestricted;
            if (savedException != null)
                throw savedException;
        }

        public PermissionSet Intersect(PermissionSet other)
        {
            if (other == null || other.FastIsEmpty() || this.FastIsEmpty())
            {
                return null;
            }

            int thisMax = this.m_permSet == null ? -1 : this.m_permSet.GetMaxUsedIndex();
            int otherMax = other.m_permSet == null ? -1 : other.m_permSet.GetMaxUsedIndex();
            int minMax = thisMax < otherMax ? thisMax : otherMax;
            if (this.IsUnrestricted() && minMax < otherMax)
            {
                minMax = otherMax;
                this.CheckSet();
            }

            if (other.IsUnrestricted() && minMax < thisMax)
            {
                minMax = thisMax;
                other.CheckSet();
            }

            PermissionSet pset = new PermissionSet(false);
            if (minMax > -1)
            {
                pset.m_permSet = new TokenBasedSet();
            }

            for (int i = 0; i <= minMax; ++i)
            {
                Object thisObj = this.m_permSet.GetItem(i);
                IPermission thisPerm = thisObj as IPermission;
                Object otherObj = other.m_permSet.GetItem(i);
                IPermission otherPerm = otherObj as IPermission;
                if (thisObj == null && otherObj == null)
                    continue;
                if (thisObj == null)
                {
                    if (this.m_Unrestricted)
                    {
                        if (otherPerm != null)
                        {
                            PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
                            if ((token.m_type & PermissionTokenType.IUnrestricted) != 0)
                            {
                                pset.m_permSet.SetItem(i, otherPerm.Copy());
                                Contract.Assert(PermissionToken.s_tokenSet.GetItem(i) != null, "PermissionToken should already be assigned");
                            }
                        }
                    }
                }
                else if (otherObj == null)
                {
                    if (other.m_Unrestricted)
                    {
                        if (thisPerm != null)
                        {
                            PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
                            if ((token.m_type & PermissionTokenType.IUnrestricted) != 0)
                            {
                                pset.m_permSet.SetItem(i, thisPerm.Copy());
                                Contract.Assert(PermissionToken.s_tokenSet.GetItem(i) != null, "PermissionToken should already be assigned");
                            }
                        }
                    }
                }
                else
                {
                    IPermission intersectPerm;
                    if (thisPerm == null)
                        intersectPerm = otherPerm;
                    else if (otherPerm == null)
                        intersectPerm = thisPerm;
                    else
                        intersectPerm = thisPerm.Intersect(otherPerm);
                    pset.m_permSet.SetItem(i, intersectPerm);
                    Contract.Assert(intersectPerm == null || PermissionToken.s_tokenSet.GetItem(i) != null, "PermissionToken should already be assigned");
                }
            }

            pset.m_Unrestricted = this.m_Unrestricted && other.m_Unrestricted;
            if (pset.FastIsEmpty())
                return null;
            else
                return pset;
        }

        internal void InplaceUnion(PermissionSet other)
        {
            if (this == other)
                return;
            if (other == null || other.FastIsEmpty())
                return;
            m_CheckedForNonCas = false;
            this.m_Unrestricted = this.m_Unrestricted || other.m_Unrestricted;
            if (this.m_Unrestricted)
            {
                this.m_permSet = null;
                return;
            }

            int maxMax = -1;
            if (other.m_permSet != null)
            {
                maxMax = other.m_permSet.GetMaxUsedIndex();
                this.CheckSet();
            }

            Exception savedException = null;
            for (int i = 0; i <= maxMax; ++i)
            {
                Object thisObj = this.m_permSet.GetItem(i);
                IPermission thisPerm = thisObj as IPermission;
                Object otherObj = other.m_permSet.GetItem(i);
                IPermission otherPerm = otherObj as IPermission;
                if (thisObj == null && otherObj == null)
                    continue;
                if (thisObj == null)
                {
                    if (otherPerm != null)
                    {
                        PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
                        if (((token.m_type & PermissionTokenType.IUnrestricted) == 0) || !this.m_Unrestricted)
                        {
                            this.m_permSet.SetItem(i, otherPerm.Copy());
                        }
                    }
                }
                else if (otherObj == null)
                {
                    continue;
                }
                else
                {
                    try
                    {
                        IPermission unionPerm;
                        if (thisPerm == null)
                            unionPerm = otherPerm;
                        else if (otherPerm == null)
                            unionPerm = thisPerm;
                        else
                            unionPerm = thisPerm.Union(otherPerm);
                        this.m_permSet.SetItem(i, unionPerm);
                    }
                    catch (Exception e)
                    {
                        if (savedException == null)
                            savedException = e;
                    }
                }
            }

            if (savedException != null)
                throw savedException;
        }

        public PermissionSet Union(PermissionSet other)
        {
            if (other == null || other.FastIsEmpty())
            {
                return this.Copy();
            }

            if (this.FastIsEmpty())
            {
                return other.Copy();
            }

            int maxMax = -1;
            PermissionSet pset = new PermissionSet();
            pset.m_Unrestricted = this.m_Unrestricted || other.m_Unrestricted;
            if (pset.m_Unrestricted)
            {
                return pset;
            }

            this.CheckSet();
            other.CheckSet();
            maxMax = this.m_permSet.GetMaxUsedIndex() > other.m_permSet.GetMaxUsedIndex() ? this.m_permSet.GetMaxUsedIndex() : other.m_permSet.GetMaxUsedIndex();
            pset.m_permSet = new TokenBasedSet();
            for (int i = 0; i <= maxMax; ++i)
            {
                Object thisObj = this.m_permSet.GetItem(i);
                IPermission thisPerm = thisObj as IPermission;
                Object otherObj = other.m_permSet.GetItem(i);
                IPermission otherPerm = otherObj as IPermission;
                if (thisObj == null && otherObj == null)
                    continue;
                if (thisObj == null)
                {
                    if (otherPerm != null)
                    {
                        PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
                        if (((token.m_type & PermissionTokenType.IUnrestricted) == 0) || !pset.m_Unrestricted)
                        {
                            pset.m_permSet.SetItem(i, otherPerm.Copy());
                            Contract.Assert(PermissionToken.s_tokenSet.GetItem(i) != null, "PermissionToken should already be assigned");
                        }
                    }
                }
                else if (otherObj == null)
                {
                    if (thisPerm != null)
                    {
                        PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem(i);
                        if (((token.m_type & PermissionTokenType.IUnrestricted) == 0) || !pset.m_Unrestricted)
                        {
                            pset.m_permSet.SetItem(i, thisPerm.Copy());
                            Contract.Assert(PermissionToken.s_tokenSet.GetItem(i) != null, "PermissionToken should already be assigned");
                        }
                    }
                }
                else
                {
                    IPermission unionPerm;
                    if (thisPerm == null)
                        unionPerm = otherPerm;
                    else if (otherPerm == null)
                        unionPerm = thisPerm;
                    else
                        unionPerm = thisPerm.Union(otherPerm);
                    pset.m_permSet.SetItem(i, unionPerm);
                    Contract.Assert(unionPerm == null || PermissionToken.s_tokenSet.GetItem(i) != null, "PermissionToken should already be assigned");
                }
            }

            return pset;
        }

        internal void MergeDeniedSet(PermissionSet denied)
        {
            if (denied == null || denied.FastIsEmpty() || this.FastIsEmpty())
                return;
            m_CheckedForNonCas = false;
            if (this.m_permSet == null || denied.m_permSet == null)
                return;
            int maxIndex = denied.m_permSet.GetMaxUsedIndex() > this.m_permSet.GetMaxUsedIndex() ? this.m_permSet.GetMaxUsedIndex() : denied.m_permSet.GetMaxUsedIndex();
            for (int i = 0; i <= maxIndex; ++i)
            {
                IPermission deniedPerm = denied.m_permSet.GetItem(i) as IPermission;
                if (deniedPerm == null)
                    continue;
                IPermission thisPerm = this.m_permSet.GetItem(i) as IPermission;
                if (thisPerm == null && !this.m_Unrestricted)
                {
                    denied.m_permSet.SetItem(i, null);
                    continue;
                }

                if (thisPerm != null && deniedPerm != null)
                {
                    if (thisPerm.IsSubsetOf(deniedPerm))
                    {
                        this.m_permSet.SetItem(i, null);
                        denied.m_permSet.SetItem(i, null);
                    }
                }
            }
        }

        internal bool Contains(IPermission perm)
        {
            if (perm == null)
                return true;
            if (m_Unrestricted)
                return true;
            if (FastIsEmpty())
                return false;
            PermissionToken token = PermissionToken.GetToken(perm);
            Object thisObj = this.m_permSet.GetItem(token.m_index);
            if (thisObj == null)
                return perm.IsSubsetOf(null);
            IPermission thisPerm = GetPermission(token.m_index);
            if (thisPerm != null)
                return perm.IsSubsetOf(thisPerm);
            else
                return perm.IsSubsetOf(null);
        }

        public override bool Equals(Object obj)
        {
            PermissionSet other = obj as PermissionSet;
            if (other == null)
                return false;
            if (this.m_Unrestricted != other.m_Unrestricted)
                return false;
            CheckSet();
            other.CheckSet();
            DecodeAllPermissions();
            other.DecodeAllPermissions();
            int maxIndex = Math.Max(this.m_permSet.GetMaxUsedIndex(), other.m_permSet.GetMaxUsedIndex());
            for (int i = 0; i <= maxIndex; ++i)
            {
                IPermission thisPerm = (IPermission)this.m_permSet.GetItem(i);
                IPermission otherPerm = (IPermission)other.m_permSet.GetItem(i);
                if (thisPerm == null && otherPerm == null)
                {
                    continue;
                }
                else if (thisPerm == null)
                {
                    if (!otherPerm.IsSubsetOf(null))
                        return false;
                }
                else if (otherPerm == null)
                {
                    if (!thisPerm.IsSubsetOf(null))
                        return false;
                }
                else
                {
                    if (!thisPerm.Equals(otherPerm))
                        return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int accumulator;
            accumulator = this.m_Unrestricted ? -1 : 0;
            if (this.m_permSet != null)
            {
                DecodeAllPermissions();
                int maxIndex = this.m_permSet.GetMaxUsedIndex();
                for (int i = m_permSet.GetStartingIndex(); i <= maxIndex; ++i)
                {
                    IPermission perm = (IPermission)this.m_permSet.GetItem(i);
                    if (perm != null)
                    {
                        accumulator = accumulator ^ perm.GetHashCode();
                    }
                }
            }

            return accumulator;
        }

        public void Demand()
        {
            if (this.FastIsEmpty())
                return;
            ContainsNonCodeAccessPermissions();
            if (m_ContainsCas)
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
                CodeAccessSecurityEngine.Check(GetCasOnlySet(), ref stackMark);
            }

            if (m_ContainsNonCas)
            {
                DemandNonCAS();
            }
        }

        internal void DemandNonCAS()
        {
            ContainsNonCodeAccessPermissions();
            if (m_ContainsNonCas)
            {
                if (this.m_permSet != null)
                {
                    CheckSet();
                    for (int i = m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); ++i)
                    {
                        IPermission currPerm = GetPermission(i);
                        if (currPerm != null && !(currPerm is CodeAccessPermission))
                            currPerm.Demand();
                    }
                }
            }
        }

        public void Assert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.Assert(this, ref stackMark);
        }

        public void Deny()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.Deny(this, ref stackMark);
        }

        public void PermitOnly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.PermitOnly(this, ref stackMark);
        }

        internal IPermission GetFirstPerm()
        {
            IEnumerator enumerator = GetEnumerator();
            if (!enumerator.MoveNext())
                return null;
            return enumerator.Current as IPermission;
        }

        public virtual PermissionSet Copy()
        {
            return new PermissionSet(this);
        }

        internal PermissionSet CopyWithNoIdentityPermissions()
        {
            PermissionSet copy = new PermissionSet(this);
            return copy;
        }

        public IEnumerator GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        protected virtual IEnumerator GetEnumeratorImpl()
        {
            return new PermissionSetEnumerator(this);
        }

        internal PermissionSetEnumeratorInternal GetEnumeratorInternal()
        {
            return new PermissionSetEnumeratorInternal(this);
        }

        private void NormalizePermissionSet()
        {
            PermissionSet permSetTemp = new PermissionSet(false);
            permSetTemp.m_Unrestricted = this.m_Unrestricted;
            if (this.m_permSet != null)
            {
                for (int i = m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); ++i)
                {
                    Object obj = this.m_permSet.GetItem(i);
                    IPermission perm = obj as IPermission;
                    if (perm != null)
                        permSetTemp.SetPermission(perm);
                }
            }

            this.m_permSet = permSetTemp.m_permSet;
        }

        private void DecodeAllPermissions()
        {
            if (m_permSet == null)
            {
                m_allPermissionsDecoded = true;
                return;
            }

            int maxIndex = m_permSet.GetMaxUsedIndex();
            for (int i = 0; i <= maxIndex; ++i)
            {
                GetPermission(i);
            }

            m_allPermissionsDecoded = true;
        }

        internal void FilterHostProtectionPermissions(HostProtectionResource fullTrustOnly, HostProtectionResource inaccessible)
        {
            HostProtectionPermission.protectedResources = fullTrustOnly;
            HostProtectionPermission hpp = (HostProtectionPermission)GetPermission(HostProtectionPermission.GetTokenIndex());
            if (hpp == null)
                return;
            HostProtectionPermission newHpp = (HostProtectionPermission)hpp.Intersect(new HostProtectionPermission(fullTrustOnly));
            if (newHpp == null)
            {
                RemovePermission(HostProtectionPermission.GetTokenIndex());
            }
            else if (newHpp.Resources != hpp.Resources)
            {
                SetPermission(newHpp);
            }
        }

        public bool ContainsNonCodeAccessPermissions()
        {
            if (m_CheckedForNonCas)
                return m_ContainsNonCas;
            lock (this)
            {
                if (m_CheckedForNonCas)
                    return m_ContainsNonCas;
                m_ContainsCas = false;
                m_ContainsNonCas = false;
                if (IsUnrestricted())
                    m_ContainsCas = true;
                if (this.m_permSet != null)
                {
                    PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(this);
                    while (enumerator.MoveNext() && (!m_ContainsCas || !m_ContainsNonCas))
                    {
                        IPermission perm = enumerator.Current as IPermission;
                        if (perm != null)
                        {
                            if (perm is CodeAccessPermission)
                                m_ContainsCas = true;
                            else
                                m_ContainsNonCas = true;
                        }
                    }
                }

                m_CheckedForNonCas = true;
            }

            return m_ContainsNonCas;
        }

        private PermissionSet GetCasOnlySet()
        {
            if (!m_ContainsNonCas)
                return this;
            if (IsUnrestricted())
                return this;
            PermissionSet pset = new PermissionSet(false);
            PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(this);
            while (enumerator.MoveNext())
            {
                IPermission perm = (IPermission)enumerator.Current;
                if (perm is CodeAccessPermission)
                    pset.AddPermission(perm);
            }

            pset.m_CheckedForNonCas = true;
            pset.m_ContainsCas = !pset.IsEmpty();
            pset.m_ContainsNonCas = false;
            return pset;
        }

        private static void MergePermission(IPermission perm, bool separateCasFromNonCas, ref PermissionSet casPset, ref PermissionSet nonCasPset)
        {
            Contract.Assert(casPset == null || !casPset.IsReadOnly);
            Contract.Assert(nonCasPset == null || !nonCasPset.IsReadOnly);
            if (perm == null)
                return;
            if (!separateCasFromNonCas || perm is CodeAccessPermission)
            {
                if (casPset == null)
                    casPset = new PermissionSet(false);
                IPermission oldPerm = casPset.GetPermission(perm);
                IPermission unionPerm = casPset.AddPermission(perm);
                if (oldPerm != null && !oldPerm.IsSubsetOf(unionPerm))
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_DeclarativeUnion"));
            }
            else
            {
                if (nonCasPset == null)
                    nonCasPset = new PermissionSet(false);
                IPermission oldPerm = nonCasPset.GetPermission(perm);
                IPermission unionPerm = nonCasPset.AddPermission(perm);
                if (oldPerm != null && !oldPerm.IsSubsetOf(unionPerm))
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_DeclarativeUnion"));
            }
        }

        private static byte[] CreateSerialized(Object[] attrs, bool serialize, ref byte[] nonCasBlob, out PermissionSet casPset, HostProtectionResource fullTrustOnlyResources, bool allowEmptyPermissionSets)
        {
            casPset = null;
            PermissionSet nonCasPset = null;
            for (int i = 0; i < attrs.Length; i++)
            {
                Contract.Assert(i == 0 || ((SecurityAttribute)attrs[i]).m_action == ((SecurityAttribute)attrs[i - 1]).m_action, "Mixed SecurityActions");
                if (attrs[i] is PermissionSetAttribute)
                {
                    PermissionSet pset = ((PermissionSetAttribute)attrs[i]).CreatePermissionSet();
                    if (pset == null)
                        throw new ArgumentException(Environment.GetResourceString("Argument_UnableToGeneratePermissionSet"));
                    PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(pset);
                    while (enumerator.MoveNext())
                    {
                        IPermission perm = (IPermission)enumerator.Current;
                        MergePermission(perm, serialize, ref casPset, ref nonCasPset);
                    }

                    if (casPset == null)
                        casPset = new PermissionSet(false);
                    if (pset.IsUnrestricted())
                        casPset.SetUnrestricted(true);
                }
                else
                {
                    IPermission perm = ((SecurityAttribute)attrs[i]).CreatePermission();
                    MergePermission(perm, serialize, ref casPset, ref nonCasPset);
                }
            }

            Contract.Assert(serialize || nonCasPset == null, "We shouldn't separate nonCAS permissions unless fSerialize is true");
            if (casPset != null)
            {
                casPset.FilterHostProtectionPermissions(fullTrustOnlyResources, HostProtectionResource.None);
                casPset.ContainsNonCodeAccessPermissions();
                if (allowEmptyPermissionSets && casPset.IsEmpty())
                    casPset = null;
            }

            if (nonCasPset != null)
            {
                nonCasPset.FilterHostProtectionPermissions(fullTrustOnlyResources, HostProtectionResource.None);
                nonCasPset.ContainsNonCodeAccessPermissions();
                if (allowEmptyPermissionSets && nonCasPset.IsEmpty())
                    nonCasPset = null;
            }

            byte[] casBlob = null;
            nonCasBlob = null;
            Contract.Assert(!serialize, "Cannot serialize permission sets on CoreCLR");
            return casBlob;
        }

        public static void RevertAssert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertAssert(ref stackMark);
        }

        internal static PermissionSet RemoveRefusedPermissionSet(PermissionSet assertSet, PermissionSet refusedSet, out bool bFailedToCompress)
        {
            Contract.Assert((assertSet == null || !assertSet.IsUnrestricted()), "Cannot be unrestricted here");
            PermissionSet retPs = null;
            bFailedToCompress = false;
            if (assertSet == null)
                return null;
            if (refusedSet != null)
            {
                if (refusedSet.IsUnrestricted())
                    return null;
                PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(refusedSet);
                while (enumerator.MoveNext())
                {
                    CodeAccessPermission refusedPerm = (CodeAccessPermission)enumerator.Current;
                    int i = enumerator.GetCurrentIndex();
                    if (refusedPerm != null)
                    {
                        CodeAccessPermission perm = (CodeAccessPermission)assertSet.GetPermission(i);
                        try
                        {
                            if (refusedPerm.Intersect(perm) != null)
                            {
                                if (refusedPerm.Equals(perm))
                                {
                                    if (retPs == null)
                                        retPs = assertSet.Copy();
                                    retPs.RemovePermission(i);
                                }
                                else
                                {
                                    bFailedToCompress = true;
                                    return assertSet;
                                }
                            }
                        }
                        catch (ArgumentException)
                        {
                            if (retPs == null)
                                retPs = assertSet.Copy();
                            retPs.RemovePermission(i);
                        }
                    }
                }
            }

            if (retPs != null)
                return retPs;
            return assertSet;
        }

        internal static void RemoveAssertedPermissionSet(PermissionSet demandSet, PermissionSet assertSet, out PermissionSet alteredDemandSet)
        {
            Contract.Assert(!assertSet.IsUnrestricted(), "Cannot call this function if assertSet is unrestricted");
            alteredDemandSet = null;
            PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(demandSet);
            while (enumerator.MoveNext())
            {
                CodeAccessPermission demandDerm = (CodeAccessPermission)enumerator.Current;
                int i = enumerator.GetCurrentIndex();
                if (demandDerm != null)
                {
                    CodeAccessPermission assertPerm = (CodeAccessPermission)assertSet.GetPermission(i);
                    try
                    {
                        if (demandDerm.CheckAssert(assertPerm))
                        {
                            if (alteredDemandSet == null)
                                alteredDemandSet = demandSet.Copy();
                            alteredDemandSet.RemovePermission(i);
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }

            return;
        }

        internal static bool IsIntersectingAssertedPermissions(PermissionSet assertSet1, PermissionSet assertSet2)
        {
            bool isIntersecting = false;
            if (assertSet1 != null && assertSet2 != null)
            {
                PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(assertSet2);
                while (enumerator.MoveNext())
                {
                    CodeAccessPermission perm2 = (CodeAccessPermission)enumerator.Current;
                    int i = enumerator.GetCurrentIndex();
                    if (perm2 != null)
                    {
                        CodeAccessPermission perm1 = (CodeAccessPermission)assertSet1.GetPermission(i);
                        try
                        {
                            if (perm1 != null && !perm1.Equals(perm2))
                            {
                                isIntersecting = true;
                            }
                        }
                        catch (ArgumentException)
                        {
                            isIntersecting = true;
                        }
                    }
                }
            }

            return isIntersecting;
        }

        internal bool IgnoreTypeLoadFailures
        {
            set
            {
                m_ignoreTypeLoadFailures = value;
            }
        }
    }
}