namespace System.Runtime.Serialization
{
    public class SerializationException : SystemException
    {
        private static String _nullMessage = Environment.GetResourceString("Arg_SerializationException");
        public SerializationException(): base (_nullMessage)
        {
            SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }

        public SerializationException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }

        public SerializationException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_SERIALIZATION);
        }

        protected SerializationException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}