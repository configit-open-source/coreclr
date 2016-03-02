using System.Text;

namespace System.Security.Policy
{
    [Flags]
    public enum PolicyStatementAttribute
    {
        Nothing = 0x0,
        Exclusive = 0x01,
        LevelFinal = 0x02,
        All = 0x03
    }

    sealed public class PolicyStatement : ISecurityPolicyEncodable, ISecurityEncodable
    {
        internal PermissionSet m_permSet;
        internal PolicyStatementAttribute m_attributes;
        internal PolicyStatement()
        {
            m_permSet = null;
            m_attributes = PolicyStatementAttribute.Nothing;
        }

        public PolicyStatement(PermissionSet permSet): this (permSet, PolicyStatementAttribute.Nothing)
        {
        }

        public PolicyStatement(PermissionSet permSet, PolicyStatementAttribute attributes)
        {
            if (permSet == null)
            {
                m_permSet = new PermissionSet(false);
            }
            else
            {
                m_permSet = permSet.Copy();
            }

            if (ValidProperties(attributes))
            {
                m_attributes = attributes;
            }
        }

        private PolicyStatement(PermissionSet permSet, PolicyStatementAttribute attributes, bool copy)
        {
            if (permSet != null)
            {
                if (copy)
                    m_permSet = permSet.Copy();
                else
                    m_permSet = permSet;
            }
            else
            {
                m_permSet = new PermissionSet(false);
            }

            m_attributes = attributes;
        }

        public PermissionSet PermissionSet
        {
            get
            {
                lock (this)
                {
                    return m_permSet.Copy();
                }
            }

            set
            {
                lock (this)
                {
                    if (value == null)
                    {
                        m_permSet = new PermissionSet(false);
                    }
                    else
                    {
                        m_permSet = value.Copy();
                    }
                }
            }
        }

        internal void SetPermissionSetNoCopy(PermissionSet permSet)
        {
            m_permSet = permSet;
        }

        internal PermissionSet GetPermissionSetNoCopy()
        {
            lock (this)
            {
                return m_permSet;
            }
        }

        public PolicyStatementAttribute Attributes
        {
            get
            {
                return m_attributes;
            }

            set
            {
                if (ValidProperties(value))
                {
                    m_attributes = value;
                }
            }
        }

        public PolicyStatement Copy()
        {
            PolicyStatement copy = new PolicyStatement(m_permSet, Attributes, true);
            return copy;
        }

        public String AttributeString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                bool first = true;
                if (GetFlag((int)PolicyStatementAttribute.Exclusive))
                {
                    sb.Append("Exclusive");
                    first = false;
                }

                if (GetFlag((int)PolicyStatementAttribute.LevelFinal))
                {
                    if (!first)
                        sb.Append(" ");
                    sb.Append("LevelFinal");
                }

                return sb.ToString();
            }
        }

        private static bool ValidProperties(PolicyStatementAttribute attributes)
        {
            if ((attributes & ~(PolicyStatementAttribute.All)) == 0)
            {
                return true;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"));
            }
        }

        private bool GetFlag(int flag)
        {
            return (flag & (int)m_attributes) != 0;
        }

        internal void InplaceUnion(PolicyStatement childPolicy)
        {
            BCLDebug.Assert(childPolicy != null, "childPolicy != null");
            if (((Attributes & childPolicy.Attributes) & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive)
            {
                throw new PolicyException(Environment.GetResourceString("Policy_MultipleExclusive"));
            }

            if ((childPolicy.Attributes & PolicyStatementAttribute.Exclusive) == PolicyStatementAttribute.Exclusive)
            {
                m_permSet = childPolicy.GetPermissionSetNoCopy();
                Attributes = childPolicy.Attributes;
            }
            else
            {
                m_permSet.InplaceUnion(childPolicy.GetPermissionSetNoCopy());
                Attributes = Attributes | childPolicy.Attributes;
            }
        }

        public override bool Equals(Object obj)
        {
            PolicyStatement other = obj as PolicyStatement;
            if (other == null)
                return false;
            if (this.m_attributes != other.m_attributes)
                return false;
            if (!Object.Equals(this.m_permSet, other.m_permSet))
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            int accumulator = (int)this.m_attributes;
            if (m_permSet != null)
                accumulator = accumulator ^ m_permSet.GetHashCode();
            return accumulator;
        }
    }
}