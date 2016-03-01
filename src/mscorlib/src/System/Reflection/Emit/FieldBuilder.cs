using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public sealed class FieldBuilder : FieldInfo, _FieldBuilder
    {
        private int m_fieldTok;
        private FieldToken m_tkField;
        private TypeBuilder m_typeBuilder;
        private String m_fieldName;
        private FieldAttributes m_Attributes;
        private Type m_fieldType;
        internal FieldBuilder(TypeBuilder typeBuilder, String fieldName, Type type, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers, FieldAttributes attributes)
        {
            if (fieldName == null)
                throw new ArgumentNullException("fieldName");
            if (fieldName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "fieldName");
            if (fieldName[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "fieldName");
            if (type == null)
                throw new ArgumentNullException("type");
            if (type == typeof (void))
                throw new ArgumentException(Environment.GetResourceString("Argument_BadFieldType"));
            Contract.EndContractBlock();
            m_fieldName = fieldName;
            m_typeBuilder = typeBuilder;
            m_fieldType = type;
            m_Attributes = attributes & ~FieldAttributes.ReservedMask;
            SignatureHelper sigHelp = SignatureHelper.GetFieldSigHelper(m_typeBuilder.Module);
            sigHelp.AddArgument(type, requiredCustomModifiers, optionalCustomModifiers);
            int sigLength;
            byte[] signature = sigHelp.InternalGetSignature(out sigLength);
            m_fieldTok = TypeBuilder.DefineField(m_typeBuilder.GetModuleBuilder().GetNativeHandle(), typeBuilder.TypeToken.Token, fieldName, signature, sigLength, m_Attributes);
            m_tkField = new FieldToken(m_fieldTok, type);
        }

        internal void SetData(byte[] data, int size)
        {
            ModuleBuilder.SetFieldRVAContent(m_typeBuilder.GetModuleBuilder().GetNativeHandle(), m_tkField.Token, data, size);
        }

        internal TypeBuilder GetTypeBuilder()
        {
            return m_typeBuilder;
        }

        internal int MetadataTokenInternal
        {
            get
            {
                return m_fieldTok;
            }
        }

        public override Module Module
        {
            get
            {
                return m_typeBuilder.Module;
            }
        }

        public override String Name
        {
            get
            {
                return m_fieldName;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                if (m_typeBuilder.m_isHiddenGlobalType == true)
                    return null;
                return m_typeBuilder;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                if (m_typeBuilder.m_isHiddenGlobalType == true)
                    return null;
                return m_typeBuilder;
            }
        }

        public override Type FieldType
        {
            get
            {
                return m_fieldType;
            }
        }

        public override Object GetValue(Object obj)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override void SetValue(Object obj, Object val, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
            }
        }

        public override FieldAttributes Attributes
        {
            get
            {
                return m_Attributes;
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

        public FieldToken GetToken()
        {
            return m_tkField;
        }

        public void SetOffset(int iOffset)
        {
            m_typeBuilder.ThrowIfCreated();
            TypeBuilder.SetFieldLayoutOffset(m_typeBuilder.GetModuleBuilder().GetNativeHandle(), GetToken().Token, iOffset);
        }

        public void SetMarshal(UnmanagedMarshal unmanagedMarshal)
        {
            if (unmanagedMarshal == null)
                throw new ArgumentNullException("unmanagedMarshal");
            Contract.EndContractBlock();
            m_typeBuilder.ThrowIfCreated();
            byte[] ubMarshal = unmanagedMarshal.InternalGetBytes();
            TypeBuilder.SetFieldMarshal(m_typeBuilder.GetModuleBuilder().GetNativeHandle(), GetToken().Token, ubMarshal, ubMarshal.Length);
        }

        public void SetConstant(Object defaultValue)
        {
            m_typeBuilder.ThrowIfCreated();
            TypeBuilder.SetConstantValue(m_typeBuilder.GetModuleBuilder(), GetToken().Token, m_fieldType, defaultValue);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();
            ModuleBuilder module = m_typeBuilder.Module as ModuleBuilder;
            m_typeBuilder.ThrowIfCreated();
            TypeBuilder.DefineCustomAttribute(module, m_tkField.Token, module.GetConstructorToken(con).Token, binaryAttribute, false, false);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException("customBuilder");
            Contract.EndContractBlock();
            m_typeBuilder.ThrowIfCreated();
            ModuleBuilder module = m_typeBuilder.Module as ModuleBuilder;
            customBuilder.CreateCustomAttribute(module, m_tkField.Token);
        }
    }
}