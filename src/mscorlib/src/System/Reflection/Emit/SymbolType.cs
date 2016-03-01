
using System.Globalization;

namespace System.Reflection.Emit
{
    internal enum TypeKind
    {
        IsArray = 1,
        IsPointer = 2,
        IsByRef = 3
    }

    internal sealed class SymbolType : TypeInfo
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo)
        {
            if (typeInfo == null)
                return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        internal static Type FormCompoundType(char[] bFormat, Type baseType, int curIndex)
        {
            SymbolType symbolType;
            int iLowerBound;
            int iUpperBound;
            if (bFormat == null || curIndex == bFormat.Length)
            {
                return baseType;
            }

            if (bFormat[curIndex] == '&')
            {
                symbolType = new SymbolType(TypeKind.IsByRef);
                symbolType.SetFormat(bFormat, curIndex, 1);
                curIndex++;
                if (curIndex != bFormat.Length)
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
                symbolType.SetElementType(baseType);
                return symbolType;
            }

            if (bFormat[curIndex] == '[')
            {
                symbolType = new SymbolType(TypeKind.IsArray);
                int startIndex = curIndex;
                curIndex++;
                iLowerBound = 0;
                iUpperBound = -1;
                while (bFormat[curIndex] != ']')
                {
                    if (bFormat[curIndex] == '*')
                    {
                        symbolType.m_isSzArray = false;
                        curIndex++;
                    }

                    if ((bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9') || bFormat[curIndex] == '-')
                    {
                        bool isNegative = false;
                        if (bFormat[curIndex] == '-')
                        {
                            isNegative = true;
                            curIndex++;
                        }

                        while (bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9')
                        {
                            iLowerBound = iLowerBound * 10;
                            iLowerBound += bFormat[curIndex] - '0';
                            curIndex++;
                        }

                        if (isNegative)
                        {
                            iLowerBound = 0 - iLowerBound;
                        }

                        iUpperBound = iLowerBound - 1;
                    }

                    if (bFormat[curIndex] == '.')
                    {
                        curIndex++;
                        if (bFormat[curIndex] != '.')
                        {
                            throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
                        }

                        curIndex++;
                        if ((bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9') || bFormat[curIndex] == '-')
                        {
                            bool isNegative = false;
                            iUpperBound = 0;
                            if (bFormat[curIndex] == '-')
                            {
                                isNegative = true;
                                curIndex++;
                            }

                            while (bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9')
                            {
                                iUpperBound = iUpperBound * 10;
                                iUpperBound += bFormat[curIndex] - '0';
                                curIndex++;
                            }

                            if (isNegative)
                            {
                                iUpperBound = 0 - iUpperBound;
                            }

                            if (iUpperBound < iLowerBound)
                            {
                                throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
                            }
                        }
                    }

                    if (bFormat[curIndex] == ',')
                    {
                        curIndex++;
                        symbolType.SetBounds(iLowerBound, iUpperBound);
                        iLowerBound = 0;
                        iUpperBound = -1;
                    }
                    else if (bFormat[curIndex] != ']')
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
                    }
                }

                symbolType.SetBounds(iLowerBound, iUpperBound);
                curIndex++;
                symbolType.SetFormat(bFormat, startIndex, curIndex - startIndex);
                symbolType.SetElementType(baseType);
                return FormCompoundType(bFormat, symbolType, curIndex);
            }
            else if (bFormat[curIndex] == '*')
            {
                symbolType = new SymbolType(TypeKind.IsPointer);
                symbolType.SetFormat(bFormat, curIndex, 1);
                curIndex++;
                symbolType.SetElementType(baseType);
                return FormCompoundType(bFormat, symbolType, curIndex);
            }

            return null;
        }

        internal TypeKind m_typeKind;
        internal Type m_baseType;
        internal int m_cRank;
        internal int[] m_iaLowerBound;
        internal int[] m_iaUpperBound;
        private char[] m_bFormat;
        private bool m_isSzArray = true;
        internal SymbolType(TypeKind typeKind)
        {
            m_typeKind = typeKind;
            m_iaLowerBound = new int[4];
            m_iaUpperBound = new int[4];
        }

        internal void SetElementType(Type baseType)
        {
            if (baseType == null)
                throw new ArgumentNullException("baseType");
                        m_baseType = baseType;
        }

        private void SetBounds(int lower, int upper)
        {
            if (lower != 0 || upper != -1)
                m_isSzArray = false;
            if (m_iaLowerBound.Length <= m_cRank)
            {
                int[] iaTemp = new int[m_cRank * 2];
                Array.Copy(m_iaLowerBound, iaTemp, m_cRank);
                m_iaLowerBound = iaTemp;
                Array.Copy(m_iaUpperBound, iaTemp, m_cRank);
                m_iaUpperBound = iaTemp;
            }

            m_iaLowerBound[m_cRank] = lower;
            m_iaUpperBound[m_cRank] = upper;
            m_cRank++;
        }

        internal void SetFormat(char[] bFormat, int curIndex, int length)
        {
            char[] bFormatTemp = new char[length];
            Array.Copy(bFormat, curIndex, bFormatTemp, 0, length);
            m_bFormat = bFormatTemp;
        }

        internal override bool IsSzArray
        {
            get
            {
                if (m_cRank > 1)
                    return false;
                return m_isSzArray;
            }
        }

        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType((new String(m_bFormat) + "*").ToCharArray(), m_baseType, 0);
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType((new String(m_bFormat) + "&").ToCharArray(), m_baseType, 0);
        }

        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType((new String(m_bFormat) + "[]").ToCharArray(), m_baseType, 0);
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
            SymbolType st = SymbolType.FormCompoundType((new String(m_bFormat) + s).ToCharArray(), m_baseType, 0) as SymbolType;
            return st;
        }

        public override int GetArrayRank()
        {
            if (!IsArray)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
                        return m_cRank;
        }

        public override Guid GUID
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
            }
        }

        public override Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Module Module
        {
            get
            {
                Type baseType;
                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType)
                    ;
                return baseType.Module;
            }
        }

        public override Assembly Assembly
        {
            get
            {
                Type baseType;
                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType)
                    ;
                return baseType.Assembly;
            }
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
            }
        }

        public override String Name
        {
            get
            {
                Type baseType;
                String sFormat = new String(m_bFormat);
                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType)
                    sFormat = new String(((SymbolType)baseType).m_bFormat) + sFormat;
                return baseType.Name + sFormat;
            }
        }

        public override String FullName
        {
            get
            {
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);
            }
        }

        public override String AssemblyQualifiedName
        {
            get
            {
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);
            }
        }

        public override String ToString()
        {
            return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
        }

        public override String Namespace
        {
            get
            {
                return m_baseType.Namespace;
            }
        }

        public override Type BaseType
        {
            get
            {
                return typeof (System.Array);
            }
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        protected override MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Type GetInterface(String name, bool ignoreCase)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Type[] GetInterfaces()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override EventInfo GetEvent(String name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override EventInfo[] GetEvents()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        protected override PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Type GetNestedType(String name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            Type baseType;
            for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType)
                ;
            return baseType.Attributes;
        }

        protected override bool IsArrayImpl()
        {
            return m_typeKind == TypeKind.IsArray;
        }

        protected override bool IsPointerImpl()
        {
            return m_typeKind == TypeKind.IsPointer;
        }

        protected override bool IsByRefImpl()
        {
            return m_typeKind == TypeKind.IsByRef;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsValueTypeImpl()
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
            return m_baseType;
        }

        protected override bool HasElementTypeImpl()
        {
            return m_baseType != null;
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                return this;
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));
        }
    }
}