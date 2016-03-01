namespace System.Diagnostics
{
    public sealed class ConditionalAttribute : Attribute
    {
        public ConditionalAttribute(String conditionString)
        {
            m_conditionString = conditionString;
        }

        public String ConditionString
        {
            get
            {
                return m_conditionString;
            }
        }

        private String m_conditionString;
    }
}