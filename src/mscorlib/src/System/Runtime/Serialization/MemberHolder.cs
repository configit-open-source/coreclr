using System.Reflection;

namespace System.Runtime.Serialization
{
    internal class MemberHolder
    {
        internal MemberInfo[] members = null;
        internal Type memberType;
        internal StreamingContext context;
        internal MemberHolder(Type type, StreamingContext ctx)
        {
            memberType = type;
            context = ctx;
        }

        public override int GetHashCode()
        {
            return memberType.GetHashCode();
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is MemberHolder))
            {
                return false;
            }

            MemberHolder temp = (MemberHolder)obj;
            if (Object.ReferenceEquals(temp.memberType, memberType) && temp.context.State == context.State)
            {
                return true;
            }

            return false;
        }
    }
}