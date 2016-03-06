
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    public class TypeLoadException : SystemException, ISerializable
    {
        public TypeLoadException(): base (Environment.GetResourceString("Arg_TypeLoadException"))
        {
            SetErrorCode(__HResults.COR_E_TYPELOAD);
        }

        public TypeLoadException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TYPELOAD);
        }

        public TypeLoadException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_TYPELOAD);
        }

        public override String Message
        {
            get
            {
                return _message;
            }
        }



        public String TypeName
        {
            get
            {
                if (ClassName == null)
                    return String.Empty;
                return ClassName;
            }
        }

        private String ClassName;
        private String AssemblyName;
        private String MessageArg;
        internal int ResourceId;
    }
}