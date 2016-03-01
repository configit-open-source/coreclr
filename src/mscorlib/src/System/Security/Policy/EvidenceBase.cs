using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Security.Policy
{
    public abstract class EvidenceBase
    {
        protected EvidenceBase()
        {
        }

        public virtual EvidenceBase Clone()
        {
            throw new NotImplementedException();
        }
    }

    internal interface ILegacyEvidenceAdapter
    {
        object EvidenceObject
        {
            get;
        }

        Type EvidenceType
        {
            get;
        }
    }

    internal sealed class LegacyEvidenceWrapper : EvidenceBase, ILegacyEvidenceAdapter
    {
        private object m_legacyEvidence;
        internal LegacyEvidenceWrapper(object legacyEvidence)
        {
            Contract.Assert(legacyEvidence != null);
            Contract.Assert(legacyEvidence.GetType() != typeof (EvidenceBase), "Attempt to wrap an EvidenceBase in a LegacyEvidenceWrapper");
            Contract.Assert(legacyEvidence.GetType().IsSerializable, "legacyEvidence.GetType().IsSerializable");
            m_legacyEvidence = legacyEvidence;
        }

        public object EvidenceObject
        {
            get
            {
                return m_legacyEvidence;
            }
        }

        public Type EvidenceType
        {
            get
            {
                return m_legacyEvidence.GetType();
            }
        }

        public override bool Equals(object obj)
        {
            return m_legacyEvidence.Equals(obj);
        }

        public override int GetHashCode()
        {
            return m_legacyEvidence.GetHashCode();
        }

        public override EvidenceBase Clone()
        {
            return base.Clone();
        }
    }

    internal sealed class LegacyEvidenceList : EvidenceBase, IEnumerable<EvidenceBase>, ILegacyEvidenceAdapter
    {
        private List<EvidenceBase> m_legacyEvidenceList = new List<EvidenceBase>();
        public object EvidenceObject
        {
            get
            {
                return m_legacyEvidenceList.Count > 0 ? m_legacyEvidenceList[0] : null;
            }
        }

        public Type EvidenceType
        {
            get
            {
                Contract.Assert(m_legacyEvidenceList.Count > 0, "No items in LegacyEvidenceList, cannot tell what type they are");
                ILegacyEvidenceAdapter adapter = m_legacyEvidenceList[0] as ILegacyEvidenceAdapter;
                return adapter == null ? m_legacyEvidenceList[0].GetType() : adapter.EvidenceType;
            }
        }

        public void Add(EvidenceBase evidence)
        {
            Contract.Assert(evidence != null);
            Contract.Assert(m_legacyEvidenceList.Count == 0 || EvidenceType == evidence.GetType() || (evidence is LegacyEvidenceWrapper && (evidence as LegacyEvidenceWrapper).EvidenceType == EvidenceType), "LegacyEvidenceList must be homogeonous");
            Contract.Assert(evidence.GetType() != typeof (LegacyEvidenceList), "Attempt to add a legacy evidence list to another legacy evidence list");
            m_legacyEvidenceList.Add(evidence);
        }

        public IEnumerator<EvidenceBase> GetEnumerator()
        {
            return m_legacyEvidenceList.GetEnumerator();
        }

        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_legacyEvidenceList.GetEnumerator();
        }

        public override EvidenceBase Clone()
        {
            return base.Clone();
        }
    }
}