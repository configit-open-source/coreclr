namespace System
{
    using System.Runtime.Serialization;

    public class AppDomainUnloadedException : SystemException
    {
        public AppDomainUnloadedException(): base (Environment.GetResourceString("Arg_AppDomainUnloadedException"))
        {
            SetErrorCode(__HResults.COR_E_APPDOMAINUNLOADED);
        }

        public AppDomainUnloadedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_APPDOMAINUNLOADED);
        }

        public AppDomainUnloadedException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_APPDOMAINUNLOADED);
        }

        protected AppDomainUnloadedException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}