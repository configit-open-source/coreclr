namespace System.Runtime.CompilerServices
{
    using System;

    public sealed class RuntimeCompatibilityAttribute : Attribute
    {
        private bool m_wrapNonExceptionThrows;
        public RuntimeCompatibilityAttribute()
        {
        }

        public bool WrapNonExceptionThrows
        {
            get
            {
                return m_wrapNonExceptionThrows;
            }

            set
            {
                m_wrapNonExceptionThrows = value;
            }
        }
    }
}