
using System.Runtime.Serialization;

namespace System
{
    public class ArgumentOutOfRangeException : ArgumentException, ISerializable
    {
        private static volatile String _rangeMessage;
        private Object m_actualValue;
        private static String RangeMessage
        {
            get
            {
                if (_rangeMessage == null)
                    _rangeMessage = Environment.GetResourceString("Arg_ArgumentOutOfRangeException");
                return _rangeMessage;
            }
        }

        public ArgumentOutOfRangeException(): base (RangeMessage)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENTOUTOFRANGE);
        }

        public ArgumentOutOfRangeException(String paramName): base (RangeMessage, paramName)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENTOUTOFRANGE);
        }

        public ArgumentOutOfRangeException(String paramName, String message): base (message, paramName)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENTOUTOFRANGE);
        }

        public ArgumentOutOfRangeException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENTOUTOFRANGE);
        }

        public ArgumentOutOfRangeException(String paramName, Object actualValue, String message): base (message, paramName)
        {
            m_actualValue = actualValue;
            SetErrorCode(__HResults.COR_E_ARGUMENTOUTOFRANGE);
        }

        public override String Message
        {
            get
            {
                String s = base.Message;
                if (m_actualValue != null)
                {
                    String valueMessage = Environment.GetResourceString("ArgumentOutOfRange_ActualValue", m_actualValue.ToString());
                    if (s == null)
                        return valueMessage;
                    return s + Environment.NewLine + valueMessage;
                }

                return s;
            }
        }

        public virtual Object ActualValue
        {
            get
            {
                return m_actualValue;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

                        base.GetObjectData(info, context);
            info.AddValue("ActualValue", m_actualValue, typeof (Object));
        }

        protected ArgumentOutOfRangeException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            m_actualValue = info.GetValue("ActualValue", typeof (Object));
        }
    }
}