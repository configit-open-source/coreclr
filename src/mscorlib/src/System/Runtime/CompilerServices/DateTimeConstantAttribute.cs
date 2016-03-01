
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public sealed class DateTimeConstantAttribute : CustomConstantAttribute
    {
        public DateTimeConstantAttribute(long ticks)
        {
            date = new System.DateTime(ticks);
        }

        public override Object Value
        {
            get
            {
                return date;
            }
        }

        internal static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
        {
                                    foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                {
                    return new DateTime((long)namedArgument.TypedValue.Value);
                }
            }

            return new DateTime((long)attr.ConstructorArguments[0].Value);
        }

        private System.DateTime date;
    }
}