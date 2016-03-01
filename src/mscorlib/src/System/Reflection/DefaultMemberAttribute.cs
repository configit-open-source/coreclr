namespace System.Reflection
{
    using System;

    public sealed class DefaultMemberAttribute : Attribute
    {
        private String m_memberName;
        public DefaultMemberAttribute(String memberName)
        {
            m_memberName = memberName;
        }

        public String MemberName
        {
            get
            {
                return m_memberName;
            }
        }
    }
}