using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace System
{
    public class ArgumentException : SystemException, ISerializable
    {
        private String m_paramName;
        public ArgumentException(): base (Environment.GetResourceString("Arg_ArgumentException"))
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public ArgumentException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public ArgumentException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public ArgumentException(String message, String paramName, Exception innerException): base (message, innerException)
        {
            m_paramName = paramName;
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public ArgumentException(String message, String paramName): base (message)
        {
            m_paramName = paramName;
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        protected ArgumentException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            m_paramName = info.GetString("ParamName");
        }

        public override String Message
        {
            get
            {
                String s = base.Message;
                if (!String.IsNullOrEmpty(m_paramName))
                {
                    String resourceString = Environment.GetResourceString("Arg_ParamName_Name", m_paramName);
                    return s + Environment.NewLine + resourceString;
                }
                else
                    return s;
            }
        }

        public virtual String ParamName
        {
            get
            {
                return m_paramName;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("ParamName", m_paramName, typeof (String));
        }
    }
}