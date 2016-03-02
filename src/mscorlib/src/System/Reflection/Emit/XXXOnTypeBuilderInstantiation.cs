
using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed class MethodOnTypeBuilderInstantiation : MethodInfo
    {
        internal static MethodInfo GetMethod(MethodInfo method, TypeBuilderInstantiation type)
        {
            return new MethodOnTypeBuilderInstantiation(method, type);
        }

        internal MethodInfo m_method;
        private TypeBuilderInstantiation m_type;
        internal MethodOnTypeBuilderInstantiation(MethodInfo method, TypeBuilderInstantiation type)
        {
                        m_method = method;
            m_type = type;
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
                return m_type;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_type;
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

        internal int MetadataTokenInternal
        {
            get
            {
                MethodBuilder mb = m_method as MethodBuilder;
                if (mb != null)
                    return mb.MetadataTokenInternal;
                else
                {
                                        return m_method.MetadataToken;
                }
            }
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
            return m_method.GetParameters();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_method.GetMethodImplementationFlags();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                return m_method.MethodHandle;
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
            return m_method.GetGenericArguments();
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            return m_method;
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return m_method.IsGenericMethodDefinition;
            }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                return m_method.ContainsGenericParameters;
            }
        }

        public override MethodInfo MakeGenericMethod(params Type[] typeArgs)
        {
            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericMethodDefinition"));
                        return MethodBuilderInstantiation.MakeGenericMethod(this, typeArgs);
        }

        public override bool IsGenericMethod
        {
            get
            {
                return m_method.IsGenericMethod;
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

    internal sealed class ConstructorOnTypeBuilderInstantiation : ConstructorInfo
    {
        internal static ConstructorInfo GetConstructor(ConstructorInfo Constructor, TypeBuilderInstantiation type)
        {
            return new ConstructorOnTypeBuilderInstantiation(Constructor, type);
        }

        internal ConstructorInfo m_ctor;
        private TypeBuilderInstantiation m_type;
        internal ConstructorOnTypeBuilderInstantiation(ConstructorInfo constructor, TypeBuilderInstantiation type)
        {
                        m_ctor = constructor;
            m_type = type;
        }

        internal override Type[] GetParameterTypes()
        {
            return m_ctor.GetParameterTypes();
        }

        internal override Type GetReturnType()
        {
            return DeclaringType;
        }

        public override MemberTypes MemberType
        {
            get
            {
                return m_ctor.MemberType;
            }
        }

        public override String Name
        {
            get
            {
                return m_ctor.Name;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_type;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_type;
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return m_ctor.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_ctor.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_ctor.IsDefined(attributeType, inherit);
        }

        internal int MetadataTokenInternal
        {
            get
            {
                ConstructorBuilder cb = m_ctor as ConstructorBuilder;
                if (cb != null)
                    return cb.MetadataTokenInternal;
                else
                {
                                        return m_ctor.MetadataToken;
                }
            }
        }

        public override Module Module
        {
            get
            {
                return m_ctor.Module;
            }
        }

        public new Type GetType()
        {
            return base.GetType();
        }

        public override ParameterInfo[] GetParameters()
        {
            return m_ctor.GetParameters();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_ctor.GetMethodImplementationFlags();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                return m_ctor.MethodHandle;
            }
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return m_ctor.Attributes;
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
                return m_ctor.CallingConvention;
            }
        }

        public override Type[] GetGenericArguments()
        {
            return m_ctor.GetGenericArguments();
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
                return false;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                return false;
            }
        }

        public override Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }
    }

    internal sealed class FieldOnTypeBuilderInstantiation : FieldInfo
    {
        internal static FieldInfo GetField(FieldInfo Field, TypeBuilderInstantiation type)
        {
            FieldInfo m = null;
            if (type.m_hashtable.Contains(Field))
            {
                m = type.m_hashtable[Field] as FieldInfo;
            }
            else
            {
                m = new FieldOnTypeBuilderInstantiation(Field, type);
                type.m_hashtable[Field] = m;
            }

            return m;
        }

        private FieldInfo m_field;
        private TypeBuilderInstantiation m_type;
        internal FieldOnTypeBuilderInstantiation(FieldInfo field, TypeBuilderInstantiation type)
        {
                        m_field = field;
            m_type = type;
        }

        internal FieldInfo FieldInfo
        {
            get
            {
                return m_field;
            }
        }

        public override MemberTypes MemberType
        {
            get
            {
                return System.Reflection.MemberTypes.Field;
            }
        }

        public override String Name
        {
            get
            {
                return m_field.Name;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_type;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_type;
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return m_field.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_field.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_field.IsDefined(attributeType, inherit);
        }

        internal int MetadataTokenInternal
        {
            get
            {
                FieldBuilder fb = m_field as FieldBuilder;
                if (fb != null)
                    return fb.MetadataTokenInternal;
                else
                {
                                        return m_field.MetadataToken;
                }
            }
        }

        public override Module Module
        {
            get
            {
                return m_field.Module;
            }
        }

        public new Type GetType()
        {
            return base.GetType();
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return m_field.GetRequiredCustomModifiers();
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return m_field.GetOptionalCustomModifiers();
        }

        public override void SetValueDirect(TypedReference obj, Object value)
        {
            throw new NotImplementedException();
        }

        public override Object GetValueDirect(TypedReference obj)
        {
            throw new NotImplementedException();
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Type FieldType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Object GetValue(Object obj)
        {
            throw new InvalidOperationException();
        }

        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }

        public override FieldAttributes Attributes
        {
            get
            {
                return m_field.Attributes;
            }
        }
    }
}