using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public sealed class ConstructorBuilder : ConstructorInfo, _ConstructorBuilder
    {
        private readonly MethodBuilder m_methodBuilder;
        internal bool m_isDefaultConstructor;
        private ConstructorBuilder()
        {
        }

        internal ConstructorBuilder(String name, MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers, ModuleBuilder mod, TypeBuilder type)
        {
            int sigLength;
            byte[] sigBytes;
            MethodToken token;
            m_methodBuilder = new MethodBuilder(name, attributes, callingConvention, null, null, null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, mod, type, false);
            type.m_listMethods.Add(m_methodBuilder);
            sigBytes = m_methodBuilder.GetMethodSignature().InternalGetSignature(out sigLength);
            token = m_methodBuilder.GetToken();
        }

        internal ConstructorBuilder(String name, MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes, ModuleBuilder mod, TypeBuilder type): this (name, attributes, callingConvention, parameterTypes, null, null, mod, type)
        {
        }

        internal override Type[] GetParameterTypes()
        {
            return m_methodBuilder.GetParameterTypes();
        }

        private TypeBuilder GetTypeBuilder()
        {
            return m_methodBuilder.GetTypeBuilder();
        }

        internal ModuleBuilder GetModuleBuilder()
        {
            return GetTypeBuilder().GetModuleBuilder();
        }

        public override String ToString()
        {
            return m_methodBuilder.ToString();
        }

        internal int MetadataTokenInternal
        {
            get
            {
                return m_methodBuilder.MetadataTokenInternal;
            }
        }

        public override Module Module
        {
            get
            {
                return m_methodBuilder.Module;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_methodBuilder.ReflectedType;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_methodBuilder.DeclaringType;
            }
        }

        public override String Name
        {
            get
            {
                return m_methodBuilder.Name;
            }
        }

        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override ParameterInfo[] GetParameters()
        {
            ConstructorInfo rci = GetTypeBuilder().GetConstructor(m_methodBuilder.m_parameterTypes);
            return rci.GetParameters();
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return m_methodBuilder.Attributes;
            }
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_methodBuilder.GetMethodImplementationFlags();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                return m_methodBuilder.MethodHandle;
            }
        }

        public override Object Invoke(BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return m_methodBuilder.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_methodBuilder.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_methodBuilder.IsDefined(attributeType, inherit);
        }

        public MethodToken GetToken()
        {
            return m_methodBuilder.GetToken();
        }

        public ParameterBuilder DefineParameter(int iSequence, ParameterAttributes attributes, String strParamName)
        {
            attributes = attributes & ~ParameterAttributes.ReservedMask;
            return m_methodBuilder.DefineParameter(iSequence, attributes, strParamName);
        }

        public void SetSymCustomAttribute(String name, byte[] data)
        {
            m_methodBuilder.SetSymCustomAttribute(name, data);
        }

        public ILGenerator GetILGenerator()
        {
            if (m_isDefaultConstructor)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DefaultConstructorILGen"));
            return m_methodBuilder.GetILGenerator();
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            if (m_isDefaultConstructor)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DefaultConstructorILGen"));
            return m_methodBuilder.GetILGenerator(streamSize);
        }

        public void SetMethodBody(byte[] il, int maxStack, byte[] localSignature, IEnumerable<ExceptionHandler> exceptionHandlers, IEnumerable<int> tokenFixups)
        {
            if (m_isDefaultConstructor)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DefaultConstructorDefineBody"));
            }

            m_methodBuilder.SetMethodBody(il, maxStack, localSignature, exceptionHandlers, tokenFixups);
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                if (DeclaringType.IsGenericType)
                    return CallingConventions.HasThis;
                return CallingConventions.Standard;
            }
        }

        public Module GetModule()
        {
            return m_methodBuilder.GetModule();
        }

        public Type ReturnType
        {
            get
            {
                return GetReturnType();
            }
        }

        internal override Type GetReturnType()
        {
            return m_methodBuilder.ReturnType;
        }

        public String Signature
        {
            get
            {
                return m_methodBuilder.Signature;
            }
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            m_methodBuilder.SetCustomAttribute(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            m_methodBuilder.SetCustomAttribute(customBuilder);
        }

        public void SetImplementationFlags(MethodImplAttributes attributes)
        {
            m_methodBuilder.SetImplementationFlags(attributes);
        }

        public bool InitLocals
        {
            get
            {
                return m_methodBuilder.InitLocals;
            }

            set
            {
                m_methodBuilder.InitLocals = value;
            }
        }
    }
}