
using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed class MethodBuilderInstantiation : MethodInfo
    {
        internal static MethodInfo MakeGenericMethod(MethodInfo method, Type[] inst)
        {
            if (!method.IsGenericMethodDefinition)
                throw new InvalidOperationException();
                        return new MethodBuilderInstantiation(method, inst);
        }

        internal MethodInfo m_method;
        private Type[] m_inst;
        internal MethodBuilderInstantiation(MethodInfo method, Type[] inst)
        {
            m_method = method;
            m_inst = inst;
        }

        internal override Type[] GetParameterTypes()
        {
            return m_method.GetParameterTypes();
        }

        public override MemberTypes MemberType
        {
            get
            {
                return m_method.MemberType;
            }
        }

        public override String Name
        {
            get
            {
                return m_method.Name;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_method.DeclaringType;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_method.ReflectedType;
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return m_method.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_method.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_method.IsDefined(attributeType, inherit);
        }

        public override Module Module
        {
            get
            {
                return m_method.Module;
            }
        }

        public new Type GetType()
        {
            return base.GetType();
        }

        public override ParameterInfo[] GetParameters()
        {
            throw new NotSupportedException();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_method.GetMethodImplementationFlags();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
            }
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return m_method.Attributes;
            }
        }

        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                return m_method.CallingConvention;
            }
        }

        public override Type[] GetGenericArguments()
        {
            return m_inst;
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            return m_method;
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return false;
            }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                for (int i = 0; i < m_inst.Length; i++)
                {
                    if (m_inst[i].ContainsGenericParameters)
                        return true;
                }

                if (DeclaringType != null && DeclaringType.ContainsGenericParameters)
                    return true;
                return false;
            }
        }

        public override MethodInfo MakeGenericMethod(params Type[] arguments)
        {
            throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericMethodDefinition"));
        }

        public override bool IsGenericMethod
        {
            get
            {
                return true;
            }
        }

        public override Type ReturnType
        {
            get
            {
                return m_method.ReturnType;
            }
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            throw new NotSupportedException();
        }
    }
}