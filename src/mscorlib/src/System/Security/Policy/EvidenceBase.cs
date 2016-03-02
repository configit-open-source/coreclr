using System.Collections;
using System.Collections.Generic;

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
                                ILegacyEvidenceAdapter adapter = m_legacyEvidenceList[0] as ILegacyEvidenceAdapter;
                return adapter == null ? m_legacyEvidenceList[0].GetType() : adapter.EvidenceType;
            }
        }

        public void Add(EvidenceBase evidence)
        {
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