using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public abstract class CustomConstantAttribute : Attribute
    {
        public abstract Object Value
        {
            get;
        }

        internal static object GetRawConstant(CustomAttributeData attr)
        {
            foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                    return namedArgument.TypedValue.Value;
            }

            return DBNull.Value;
        }
    }
}