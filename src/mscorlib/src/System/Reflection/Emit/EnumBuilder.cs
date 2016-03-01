namespace System.Reflection.Emit
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Security.Permissions;

    sealed public class EnumBuilder : TypeInfo, _EnumBuilder
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo)
        {
            if (typeInfo == null)
                return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        public FieldBuilder DefineLiteral(String literalName, Object literalValue)
        {
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: EnumBuilder.DefineLiteral( " + literalName + " )");
            FieldBuilder fieldBuilder = m_typeBuilder.DefineField(literalName, this, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
            fieldBuilder.SetConstant(literalValue);
            return fieldBuilder;
        }

        public TypeInfo CreateTypeInfo()
        {
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: EnumBuilder.CreateType() ");
            return m_typeBuilder.CreateTypeInfo();
        }

        public Type CreateType()
        {
            BCLDebug.Log("DYNIL", "## DYNIL LOGGING: EnumBuilder.CreateType() ");
            return m_typeBuilder.CreateType();
        }

        public TypeToken TypeToken
        {
            get
            {
                return m_typeBuilder.TypeToken;
            }
        }

        public FieldBuilder UnderlyingField
        {
            get
            {
                return m_underlyingField;
            }
        }

        public override String Name
        {
            get
            {
                return m_typeBuilder.Name;
            }
        }

        public override Guid GUID
        {
            get
            {
                return m_typeBuilder.GUID;
            }
        }

        public override Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters)
        {
            return m_typeBuilder.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

        public override Module Module
        {
            get
            {
                return m_typeBuilder.Module;
            }
        }

        public override Assembly Assembly
        {
            get
            {
                return m_typeBuilder.Assembly;
            }
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                return m_typeBuilder.TypeHandle;
            }
        }

        public override String FullName
        {
            get
            {
                return m_typeBuilder.FullName;
            }
        }

        public override String AssemblyQualifiedName
        {
            get
            {
                return m_typeBuilder.AssemblyQualifiedName;
            }
        }

        public override String Namespace
        {
            get
            {
                return m_typeBuilder.Namespace;
            }
        }

        public override Type BaseType
        {
            get
            {
                return m_typeBuilder.BaseType;
            }
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            return m_typeBuilder.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetConstructors(bindingAttr);
        }

        protected override MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (types == null)
                return m_typeBuilder.GetMethod(name, bindingAttr);
            else
                return m_typeBuilder.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetMethods(bindingAttr);
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetField(name, bindingAttr);
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetFields(bindingAttr);
        }

        public override Type GetInterface(String name, bool ignoreCase)
        {
            return m_typeBuilder.GetInterface(name, ignoreCase);
        }

        public override Type[] GetInterfaces()
        {
            return m_typeBuilder.GetInterfaces();
        }

        public override EventInfo GetEvent(String name, BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetEvent(name, bindingAttr);
        }

        public override EventInfo[] GetEvents()
        {
            return m_typeBuilder.GetEvents();
        }

        protected override PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetProperties(bindingAttr);
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetNestedTypes(bindingAttr);
        }

        public override Type GetNestedType(String name, BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetNestedType(name, bindingAttr);
        }

        public override MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetMember(name, type, bindingAttr);
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetMembers(bindingAttr);
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            return m_typeBuilder.GetInterfaceMap(interfaceType);
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetEvents(bindingAttr);
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return m_typeBuilder.Attributes;
        }

        protected override bool IsArrayImpl()
        {
            return false;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsValueTypeImpl()
        {
            return true;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        public override bool IsConstructedGenericType
        {
            get
            {
                return false;
            }
        }

        public override Type GetElementType()
        {
            return m_typeBuilder.GetElementType();
        }

        protected override bool HasElementTypeImpl()
        {
            return m_typeBuilder.HasElementType;
        }

        public override Type GetEnumUnderlyingType()
        {
            return m_underlyingField.FieldType;
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                return GetEnumUnderlyingType();
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return m_typeBuilder.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_typeBuilder.GetCustomAttributes(attributeType, inherit);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            m_typeBuilder.SetCustomAttribute(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            m_typeBuilder.SetCustomAttribute(customBuilder);
        }

        public override Type DeclaringType
        {
            get
            {
                return m_typeBuilder.DeclaringType;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_typeBuilder.ReflectedType;
            }
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_typeBuilder.IsDefined(attributeType, inherit);
        }

        internal int MetadataTokenInternal
        {
            get
            {
                return m_typeBuilder.MetadataTokenInternal;
            }
        }

        private EnumBuilder()
        {
        }

        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType("*".ToCharArray(), this, 0);
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType("&".ToCharArray(), this, 0);
        }

        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]".ToCharArray(), this, 0);
        }

        public override Type MakeArrayType(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();
            string szrank = "";
            if (rank == 1)
            {
                szrank = "*";
            }
            else
            {
                for (int i = 1; i < rank; i++)
                    szrank += ",";
            }

            string s = String.Format(CultureInfo.InvariantCulture, "[{0}]", szrank);
            return SymbolType.FormCompoundType((s).ToCharArray(), this, 0);
        }

        internal EnumBuilder(String name, Type underlyingType, TypeAttributes visibility, ModuleBuilder module)
        {
            if ((visibility & ~TypeAttributes.VisibilityMask) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_ShouldOnlySetVisibilityFlags"), "name");
            m_typeBuilder = new TypeBuilder(name, visibility | TypeAttributes.Sealed, typeof (System.Enum), null, module, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize, null);
            m_underlyingField = m_typeBuilder.DefineField("value__", underlyingType, FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName);
        }

        internal TypeBuilder m_typeBuilder;
        private FieldBuilder m_underlyingField;
    }
}