namespace System
{
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Globalization;

    public class MissingFieldException : MissingMemberException, ISerializable
    {
        public MissingFieldException(): base (Environment.GetResourceString("Arg_MissingFieldException"))
        {
            SetErrorCode(__HResults.COR_E_MISSINGFIELD);
        }

        public MissingFieldException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_MISSINGFIELD);
        }

        public MissingFieldException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_MISSINGFIELD);
        }

        protected MissingFieldException(SerializationInfo info, StreamingContext context): base (info, context)
        {
        }

        public override String Message
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (ClassName == null)
                {
                    return base.Message;
                }
                else
                {
                    return Environment.GetResourceString("MissingField_Name", (Signature != null ? FormatSignature(Signature) + " " : "") + ClassName + "." + MemberName);
                }
            }
        }

        private MissingFieldException(String className, String fieldName, byte[] signature)
        {
            ClassName = className;
            MemberName = fieldName;
            Signature = signature;
        }

        public MissingFieldException(String className, String fieldName)
        {
            ClassName = className;
            MemberName = fieldName;
        }
    }
}