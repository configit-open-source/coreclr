namespace System
{
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Globalization;

    public class MissingMethodException : MissingMemberException, ISerializable
    {
        public MissingMethodException(): base (Environment.GetResourceString("Arg_MissingMethodException"))
        {
            SetErrorCode(__HResults.COR_E_MISSINGMETHOD);
        }

        public MissingMethodException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_MISSINGMETHOD);
        }

        public MissingMethodException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_MISSINGMETHOD);
        }

        protected MissingMethodException(SerializationInfo info, StreamingContext context): base (info, context)
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
                    return Environment.GetResourceString("MissingMethod_Name", ClassName + "." + MemberName + (Signature != null ? " " + FormatSignature(Signature) : ""));
                }
            }
        }

        private MissingMethodException(String className, String methodName, byte[] signature)
        {
            ClassName = className;
            MemberName = methodName;
            Signature = signature;
        }

        public MissingMethodException(String className, String methodName)
        {
            ClassName = className;
            MemberName = methodName;
        }
    }
}