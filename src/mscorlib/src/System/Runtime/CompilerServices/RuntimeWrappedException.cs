
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
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

                        base.GetObjectData(info, context);
            info.AddValue("WrappedException", m_wrappedException, typeof (Object));
        }

        internal RuntimeWrappedException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            m_wrappedException = info.GetValue("WrappedException", typeof (Object));
        }
    }
}