
using System.Runtime.Serialization;

namespace System
{
    public class MissingMemberException : MemberAccessException, ISerializable
    {
        public MissingMemberException(): base (Environment.GetResourceString("Arg_MissingMemberException"))
        {
            SetErrorCode(__HResults.COR_E_MISSINGMEMBER);
        }

        public MissingMemberException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_MISSINGMEMBER);
        }

        public MissingMemberException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_MISSINGMEMBER);
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
                    return Environment.GetResourceString("MissingMember_Name", ClassName + "." + MemberName + (Signature != null ? " " + FormatSignature(Signature) : ""));
                }
            }
        }

        internal static extern String FormatSignature(byte[] signature);
        private MissingMemberException(String className, String memberName, byte[] signature)
        {
            ClassName = className;
            MemberName = memberName;
            Signature = signature;
        }

        public MissingMemberException(String className, String memberName)
        {
            ClassName = className;
            MemberName = memberName;
        }

        protected String ClassName;
        protected String MemberName;
        protected byte[] Signature;
    }
}