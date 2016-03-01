
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public sealed class DecimalConstantAttribute : Attribute
    {
        public DecimalConstantAttribute(byte scale, byte sign, uint hi, uint mid, uint low)
        {
            dec = new System.Decimal((int)low, (int)mid, (int)hi, (sign != 0), scale);
        }

        public DecimalConstantAttribute(byte scale, byte sign, int hi, int mid, int low)
        {
            dec = new System.Decimal(low, mid, hi, (sign != 0), scale);
        }

        public System.Decimal Value
        {
            get
            {
                return dec;
            }
        }

        internal static Decimal GetRawDecimalConstant(CustomAttributeData attr)
        {
                        foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                {
                                        return (Decimal)namedArgument.TypedValue.Value;
                }
            }

            ParameterInfo[] parameters = attr.Constructor.GetParameters();
                        System.Collections.Generic.IList<CustomAttributeTypedArgument> args = attr.ConstructorArguments;
                        if (parameters[2].ParameterType == typeof (uint))
            {
                int low = (int)(UInt32)args[4].Value;
                int mid = (int)(UInt32)args[3].Value;
                int hi = (int)(UInt32)args[2].Value;
                byte sign = (byte)args[1].Value;
                byte scale = (byte)args[0].Value;
                return new System.Decimal(low, mid, hi, (sign != 0), scale);
            }
            else
            {
                int low = (int)args[4].Value;
                int mid = (int)args[3].Value;
                int hi = (int)args[2].Value;
                byte sign = (byte)args[1].Value;
                byte scale = (byte)args[0].Value;
                return new System.Decimal(low, mid, hi, (sign != 0), scale);
            }
        }

        private System.Decimal dec;
    }
}