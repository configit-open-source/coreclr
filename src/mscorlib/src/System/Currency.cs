namespace System
{
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;

    internal struct Currency
    {
        internal long m_value;
        public Currency(Decimal value)
        {
            m_value = Decimal.ToCurrency(value).m_value;
        }

        internal Currency(long value, int ignored)
        {
            m_value = value;
        }

        public static Currency FromOACurrency(long cy)
        {
            return new Currency(cy, 0);
        }

        public long ToOACurrency()
        {
            return m_value;
        }

        public static Decimal ToDecimal(Currency c)
        {
            Decimal result = new Decimal();
            FCallToDecimal(ref result, c);
            return result;
        }

        private static extern void FCallToDecimal(ref Decimal result, Currency c);
    }
}