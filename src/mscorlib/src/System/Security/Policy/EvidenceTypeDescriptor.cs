using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System.Security.Policy
{
    internal sealed class EvidenceTypeDescriptor
    {
        private bool m_hostCanGenerate;
        private bool m_generated;
        private EvidenceBase m_hostEvidence;
        private EvidenceBase m_assemblyEvidence;
        private Type m_evidenceType;
        public EvidenceTypeDescriptor()
        {
        }

        private EvidenceTypeDescriptor(EvidenceTypeDescriptor descriptor)
        {
            Contract.Assert(descriptor != null);
            m_hostCanGenerate = descriptor.m_hostCanGenerate;
            if (descriptor.m_assemblyEvidence != null)
            {
                m_assemblyEvidence = descriptor.m_assemblyEvidence.Clone() as EvidenceBase;
            }

            if (descriptor.m_hostEvidence != null)
            {
                m_hostEvidence = descriptor.m_hostEvidence.Clone() as EvidenceBase;
            }

            m_evidenceType = descriptor.m_evidenceType;
        }

        public EvidenceBase AssemblyEvidence
        {
            get
            {
                return m_assemblyEvidence;
            }

            set
            {
                Contract.Assert(value != null);
                Contract.Assert(CheckEvidenceType(value), "Incorrect type of AssemblyEvidence set");
                m_assemblyEvidence = value;
            }
        }

        public bool Generated
        {
            get
            {
                return m_generated;
            }

            set
            {
                Contract.Assert(value, "Attempt to clear the Generated flag");
                m_generated = value;
            }
        }

        public bool HostCanGenerate
        {
            get
            {
                return m_hostCanGenerate;
            }

            set
            {
                Contract.Assert(value, "Attempt to clear HostCanGenerate flag");
                m_hostCanGenerate = value;
            }
        }

        public EvidenceBase HostEvidence
        {
            get
            {
                return m_hostEvidence;
            }

            set
            {
                Contract.Assert(value != null);
                Contract.Assert(CheckEvidenceType(value), "Incorrect type of HostEvidence set");
                m_hostEvidence = value;
            }
        }

        private bool CheckEvidenceType(EvidenceBase evidence)
        {
            Contract.Assert(evidence != null);
            ILegacyEvidenceAdapter legacyAdapter = evidence as ILegacyEvidenceAdapter;
            Type storedType = legacyAdapter == null ? evidence.GetType() : legacyAdapter.EvidenceType;
            return m_evidenceType == null || m_evidenceType.IsAssignableFrom(storedType);
        }

        public EvidenceTypeDescriptor Clone()
        {
            return new EvidenceTypeDescriptor(this);
        }

        internal void SetEvidenceType(Type evidenceType)
        {
            Contract.Assert(evidenceType != null);
            Contract.Assert(m_evidenceType == null, "Attempt to reset evidence type");
            m_evidenceType = evidenceType;
        }
    }
}