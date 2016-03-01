namespace System
{
    using System;
    using System.Runtime.Serialization;

    public sealed class DataMisalignedException : SystemException
    {
        public DataMisalignedException(): base (Environment.GetResourceString("Arg_DataMisalignedException"))
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }

        public DataMisalignedException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }

        public DataMisalignedException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_DATAMISALIGNED);
        }

        internal DataMisalignedException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }
    }
}