
using System.Runtime.Serialization;

namespace System.Globalization
{
    public class CultureNotFoundException : ArgumentException, ISerializable
    {
        private string m_invalidCultureName;
        public CultureNotFoundException(): base (DefaultMessage)
        {
        }

        public CultureNotFoundException(String message): base (message)
        {
        }

        public CultureNotFoundException(String paramName, String message): base (message, paramName)
        {
        }

        public CultureNotFoundException(String message, Exception innerException): base (message, innerException)
        {
        }

        public CultureNotFoundException(String paramName, string invalidCultureName, String message): base (message, paramName)
        {
            m_invalidCultureName = invalidCultureName;
        }

        public CultureNotFoundException(String message, string invalidCultureName, Exception innerException): base (message, innerException)
        {
            m_invalidCultureName = invalidCultureName;
        }

        public virtual string InvalidCultureName
        {
            get
            {
                return m_invalidCultureName;
            }
        }

        private static String DefaultMessage
        {
            get
            {
                return Environment.GetResourceString("Argument_CultureNotSupported");
            }
        }

        private String FormatedInvalidCultureId
        {
            get
            {
                return InvalidCultureName;
            }
        }

        public override String Message
        {
            get
            {
                String s = base.Message;
                if (m_invalidCultureName != null)
                {
                    String valueMessage = Environment.GetResourceString("Argument_CultureInvalidIdentifier", FormatedInvalidCultureId);
                    if (s == null)
                        return valueMessage;
                    return s + Environment.NewLine + valueMessage;
                }

                return s;
            }
        }
    }
}