namespace System.Runtime.InteropServices
{
    public sealed class CurrencyWrapper
    {
        public CurrencyWrapper(Decimal obj)
        {
            m_WrappedObject = obj;
        }

        public CurrencyWrapper(Object obj)
        {
            if (!(obj is Decimal))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDecimal"), "obj");
            m_WrappedObject = (Decimal)obj;
        }

        public Decimal WrappedObject
        {
            get
            {
                return m_WrappedObject;
            }
        }

        private Decimal m_WrappedObject;
    }
}