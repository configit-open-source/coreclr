namespace System.Security.Principal
{
    using System;
    using System.Diagnostics.Contracts;

    public class GenericIdentity : IIdentity
    {
        private string m_name;
        private string m_type;
        public GenericIdentity(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            m_name = name;
            m_type = "";
        }

        public GenericIdentity(string name, string type)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            m_name = name;
            m_type = type;
        }

        public virtual string Name
        {
            get
            {
                return m_name;
            }
        }

        public virtual string AuthenticationType
        {
            get
            {
                return m_type;
            }
        }

        public virtual bool IsAuthenticated
        {
            get
            {
                return !m_name.Equals("");
            }
        }
    }
}