using System.Globalization;
using System.Runtime.Serialization;

namespace System.Security
{
    sealed public class XmlSyntaxException : SystemException
    {
        public XmlSyntaxException(): base (Environment.GetResourceString("XMLSyntax_InvalidSyntax"))
        {
            SetErrorCode(__HResults.CORSEC_E_XMLSYNTAX);
        }

        public XmlSyntaxException(String message): base (message)
        {
            SetErrorCode(__HResults.CORSEC_E_XMLSYNTAX);
        }

        public XmlSyntaxException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.CORSEC_E_XMLSYNTAX);
        }

        public XmlSyntaxException(int lineNumber): base (String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("XMLSyntax_SyntaxError"), lineNumber))
        {
            SetErrorCode(__HResults.CORSEC_E_XMLSYNTAX);
        }

        public XmlSyntaxException(int lineNumber, String message): base (String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("XMLSyntax_SyntaxErrorEx"), lineNumber, message))
        {
            SetErrorCode(__HResults.CORSEC_E_XMLSYNTAX);
        }
    }
}