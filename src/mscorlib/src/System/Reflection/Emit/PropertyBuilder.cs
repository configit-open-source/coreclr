namespace System.Reflection.Emit
{
    using System;
    using System.Reflection;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

    public sealed class PropertyBuilder : PropertyInfo, _PropertyBuilder
    {
        private PropertyBuilder()
        {
        }

        internal PropertyBuilder(ModuleBuilder mod, String name, SignatureHelper sig, PropertyAttributes attr, Type returnType, PropertyToken prToken, TypeBuilder containingType)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "name");
            Contract.EndContractBlock();
            m_name = name;
            m_moduleBuilder = mod;
            m_signature = sig;
            m_attributes = attr;
            m_returnType = returnType;
            m_prToken = prToken;
            m_tkProperty = prToken.Token;
            m_containingType = containingType;
        }

        public void SetConstant(Object defaultValue)
        {
            m_containingType.ThrowIfCreated();
            TypeBuilder.SetConstantValue(m_moduleBuilder, m_prToken.Token, m_returnType, defaultValue);
        }

        public PropertyToken PropertyToken
        {
            get
            {
                return m_prToken;
            }
        }

        internal int MetadataTokenInternal
        {
            get
            {
                return m_tkProperty;
            }
        }

        public override Module Module
        {
            get
            {
                return m_containingType.Module;
            }
        }

        private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
        {
            if (mdBuilder == null)
            {
                throw new ArgumentNullException("mdBuilder");
            }

            m_containingType.ThrowIfCreated();
            TypeBuilder.DefineMethodSemantics(m_moduleBuilder.GetNativeHandle(), m_prToken.Token, semantics, mdBuilder.GetToken().Token);
        }

        public void SetGetMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Getter);
            m_getMethod = mdBuilder;
        }

        public void SetSetMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Setter);
            m_setMethod = mdBuilder;
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            m_containingType.ThrowIfCreated();
            TypeBuilder.DefineCustomAttribute(m_moduleBuilder, m_prToken.Token, m_moduleBuilder.GetConstructorToken(con).Token, binaryAttribute, false, false);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException("customBuilder");
            }

            m_containingType.ThrowIfCreated();
            customBuilder.CreateCustomAttribute(m_moduleBuilder, m_prToken.Token);
        }

        public override Object GetValue(Object obj, Object[] index)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override Object GetValue(Object obj, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override void SetValue(Object obj, Object value, Object[] index)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            if (nonPublic || m_getMethod == null)
                return m_getMethod;
            if ((m_getMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                return m_getMethod;
            return null;
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            if (nonPublic || m_setMethod == null)
                return m_setMethod;
            if ((m_setMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                return m_setMethod;
            return null;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override Type PropertyType
        {
            get
            {
                return m_returnType;
            }
        }

        public override PropertyAttributes Attributes
        {
            get
            {
                return m_attributes;
            }
        }

        public override bool CanRead
        {
            get
            {
                if (m_getMethod != null)
                    return true;
                else
                    return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (m_setMethod != null)
                    return true;
                else
                    return false;
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override String Name
        {
            get
            {
                return m_name;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_containingType;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_containingType;
            }
        }

        private String m_name;
        private PropertyToken m_prToken;
        private int m_tkProperty;
        private ModuleBuilder m_moduleBuilder;
        private SignatureHelper m_signature;
        private PropertyAttributes m_attributes;
        private Type m_returnType;
        private MethodInfo m_getMethod;
        private MethodInfo m_setMethod;
        private TypeBuilder m_containingType;
    }
}