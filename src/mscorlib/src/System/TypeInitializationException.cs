using System.Runtime.Serialization;

namespace System
{
    public sealed class TypeInitializationException : SystemException
    {
        private String _typeName;
        private TypeInitializationException(): base (Environment.GetResourceString("TypeInitialization_Default"))
        {
            SetErrorCode(__HResults.COR_E_TYPEINITIALIZATION);
        }

        private TypeInitializationException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TYPEINITIALIZATION);
        }

        public TypeInitializationException(String fullTypeName, Exception innerException): base (Environment.GetResourceString("TypeInitialization_Type", fullTypeName), innerException)
        {
            _typeName = fullTypeName;
            SetErrorCode(__HResults.COR_E_TYPEINITIALIZATION);
        }

        internal TypeInitializationException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            _typeName = info.GetString("TypeName");
        }

        public String TypeName
        {
            get
            {
                if (_typeName == null)
                {
                    return String.Empty;
                }

                return _typeName;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("TypeName", TypeName, typeof (String));
        }
    }
}