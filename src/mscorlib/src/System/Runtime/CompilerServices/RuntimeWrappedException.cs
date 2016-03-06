
using System.Runtime.Serialization;

namespace System.Runtime.CompilerServices
{
    public sealed class RuntimeWrappedException : Exception
    {
        private RuntimeWrappedException(Object thrownObject): base (Environment.GetResourceString("RuntimeWrappedException"))
        {
            SetErrorCode(System.__HResults.COR_E_RUNTIMEWRAPPED);
            m_wrappedException = thrownObject;
        }

        public Object WrappedException
        {
            get
            {
                return m_wrappedException;
            }
        }

        private Object m_wrappedException;
    }
}