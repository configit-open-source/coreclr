using System.Diagnostics.Contracts;
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

        protected MissingMemberException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            ClassName = (String)info.GetString("MMClassName");
            MemberName = (String)info.GetString("MMMemberName");
            Signature = (byte[])info.GetValue("MMSignature", typeof (byte[]));
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

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("MMClassName", ClassName, typeof (String));
            info.AddValue("MMMemberName", MemberName, typeof (String));
            info.AddValue("MMSignature", Signature, typeof (byte[]));
        }

        protected String ClassName;
        protected String MemberName;
        protected byte[] Signature;
    }
}