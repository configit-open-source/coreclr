using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    public abstract class Type : MemberInfo, _Type, IReflect
    {
        public static readonly MemberFilter FilterAttribute = new MemberFilter(__Filters.Instance.FilterAttribute);
        public static readonly MemberFilter FilterName = new MemberFilter(__Filters.Instance.FilterName);
        public static readonly MemberFilter FilterNameIgnoreCase = new MemberFilter(__Filters.Instance.FilterIgnoreCase);
        public static readonly Object Missing = System.Reflection.Missing.Value;
        public static readonly char Delimiter = '.';
        public readonly static Type[] EmptyTypes = EmptyArray<Type>.Value;
        private static Binder defaultBinder;
        protected Type()
        {
        }

        public override MemberTypes MemberType
        {
            get
            {
                return System.Reflection.MemberTypes.TypeInfo;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return null;
            }
        }

        public virtual MethodBase DeclaringMethod
        {
            get
            {
                return null;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return null;
            }
        }

        public static Type GetType(String typeName, bool throwOnError, bool ignoreCase)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwOnError, ignoreCase, false, ref stackMark);
        }

        public static Type GetType(String typeName, bool throwOnError)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwOnError, false, false, ref stackMark);
        }

        public static Type GetType(String typeName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, false, false, false, ref stackMark);
        }

        public static Type ReflectionOnlyGetType(String typeName, bool throwIfNotFound, bool ignoreCase)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeType.GetType(typeName, throwIfNotFound, ignoreCase, true, ref stackMark);
        }

        public virtual Type MakePointerType()
        {
            throw new NotSupportedException();
        }

        public virtual StructLayoutAttribute StructLayoutAttribute
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual Type MakeByRefType()
        {
            throw new NotSupportedException();
        }

        public virtual Type MakeArrayType()
        {
            throw new NotSupportedException();
        }

        public virtual Type MakeArrayType(int rank)
        {
            throw new NotSupportedException();
        }

        public static Type GetTypeFromProgID(String progID)
        {
            return RuntimeType.GetTypeFromProgIDImpl(progID, null, false);
        }

        public static Type GetTypeFromProgID(String progID, bool throwOnError)
        {
            return RuntimeType.GetTypeFromProgIDImpl(progID, null, throwOnError);
        }

        public static Type GetTypeFromProgID(String progID, String server)
        {
            return RuntimeType.GetTypeFromProgIDImpl(progID, server, false);
        }

        public static Type GetTypeFromProgID(String progID, String server, bool throwOnError)
        {
            return RuntimeType.GetTypeFromProgIDImpl(progID, server, throwOnError);
        }

        public static Type GetTypeFromCLSID(Guid clsid)
        {
            return RuntimeType.GetTypeFromCLSIDImpl(clsid, null, false);
        }

        public static Type GetTypeFromCLSID(Guid clsid, bool throwOnError)
        {
            return RuntimeType.GetTypeFromCLSIDImpl(clsid, null, throwOnError);
        }

        public static Type GetTypeFromCLSID(Guid clsid, String server)
        {
            return RuntimeType.GetTypeFromCLSIDImpl(clsid, server, false);
        }

        public static Type GetTypeFromCLSID(Guid clsid, String server, bool throwOnError)
        {
            return RuntimeType.GetTypeFromCLSIDImpl(clsid, server, throwOnError);
        }

        public static TypeCode GetTypeCode(Type type)
        {
            if (type == null)
                return TypeCode.Empty;
            return type.GetTypeCodeImpl();
        }

        protected virtual TypeCode GetTypeCodeImpl()
        {
            if (this != UnderlyingSystemType && UnderlyingSystemType != null)
                return Type.GetTypeCode(UnderlyingSystemType);
            return TypeCode.Object;
        }

        public abstract Guid GUID
        {
            get;
        }

        static public Binder DefaultBinder
        {
            get
            {
                if (defaultBinder == null)
                    CreateBinder();
                return defaultBinder;
            }
        }

        static private void CreateBinder()
        {
            if (defaultBinder == null)
            {
                DefaultBinder binder = new DefaultBinder();
                Interlocked.CompareExchange<Binder>(ref defaultBinder, binder, null);
            }
        }

        abstract public Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters);
        public Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args, CultureInfo culture)
        {
            return InvokeMember(name, invokeAttr, binder, target, args, null, culture, null);
        }

        public Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, Object[] args)
        {
            return InvokeMember(name, invokeAttr, binder, target, args, null, null, null);
        }

        public new abstract Module Module
        {
            get;
        }

        public abstract Assembly Assembly
        {
            
            get;
        }

        public virtual RuntimeTypeHandle TypeHandle
        {
            
            get
            {
                throw new NotSupportedException();
            }
        }

        internal virtual RuntimeTypeHandle GetTypeHandleInternal()
        {
            return TypeHandle;
        }

        public static RuntimeTypeHandle GetTypeHandle(Object o)
        {
            if (o == null)
                throw new ArgumentNullException(null, Environment.GetResourceString("Arg_InvalidHandle"));
            return new RuntimeTypeHandle((RuntimeType)o.GetType());
        }

        internal static extern RuntimeType GetTypeFromHandleUnsafe(IntPtr handle);
        public static extern Type GetTypeFromHandle(RuntimeTypeHandle handle);
        public abstract String FullName
        {
            
            get;
        }

        public abstract String Namespace
        {
            
            get;
        }

        public abstract String AssemblyQualifiedName
        {
            
            get;
        }

        public virtual int GetArrayRank()
        {
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        public abstract Type BaseType
        {
            
            get;
        }

        public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (types == null)
                throw new ArgumentNullException("types");
                        for (int i = 0; i < types.Length; i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers);
        }

        public ConstructorInfo GetConstructor(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
        {
            if (types == null)
                throw new ArgumentNullException("types");
                        for (int i = 0; i < types.Length; i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetConstructorImpl(bindingAttr, binder, CallingConventions.Any, types, modifiers);
        }

        public ConstructorInfo GetConstructor(Type[] types)
        {
            return GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, types, null);
        }

        abstract protected ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);
        public ConstructorInfo[] GetConstructors()
        {
            return GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        }

        abstract public ConstructorInfo[] GetConstructors(BindingFlags bindingAttr);
        public ConstructorInfo TypeInitializer
        {
            get
            {
                return GetConstructorImpl(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.Any, Type.EmptyTypes, null);
            }
        }

        public MethodInfo GetMethod(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
                        for (int i = 0; i < types.Length; i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public MethodInfo GetMethod(String name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
                        for (int i = 0; i < types.Length; i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetMethodImpl(name, bindingAttr, binder, CallingConventions.Any, types, modifiers);
        }

        public MethodInfo GetMethod(String name, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
                        for (int i = 0; i < types.Length; i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetMethodImpl(name, Type.DefaultLookup, null, CallingConventions.Any, types, modifiers);
        }

        public MethodInfo GetMethod(String name, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
                        for (int i = 0; i < types.Length; i++)
                if (types[i] == null)
                    throw new ArgumentNullException("types");
            return GetMethodImpl(name, Type.DefaultLookup, null, CallingConventions.Any, types, null);
        }

        public MethodInfo GetMethod(String name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException("name");
                        return GetMethodImpl(name, bindingAttr, null, CallingConventions.Any, null, null);
        }

        public MethodInfo GetMethod(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
                        return GetMethodImpl(name, Type.DefaultLookup, null, CallingConventions.Any, null, null);
        }

        abstract protected MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers);
        public MethodInfo[] GetMethods()
        {
            return GetMethods(Type.DefaultLookup);
        }

        abstract public MethodInfo[] GetMethods(BindingFlags bindingAttr);
        abstract public FieldInfo GetField(String name, BindingFlags bindingAttr);
        public FieldInfo GetField(String name)
        {
            return GetField(name, Type.DefaultLookup);
        }

        public FieldInfo[] GetFields()
        {
            return GetFields(Type.DefaultLookup);
        }

        abstract public FieldInfo[] GetFields(BindingFlags bindingAttr);
        public Type GetInterface(String name)
        {
            return GetInterface(name, false);
        }

        abstract public Type GetInterface(String name, bool ignoreCase);
        abstract public Type[] GetInterfaces();
        public virtual Type[] FindInterfaces(TypeFilter filter, Object filterCriteria)
        {
            if (filter == null)
                throw new ArgumentNullException("filter");
                        Type[] c = GetInterfaces();
            int cnt = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (!filter(c[i], filterCriteria))
                    c[i] = null;
                else
                    cnt++;
            }

            if (cnt == c.Length)
                return c;
            Type[] ret = new Type[cnt];
            cnt = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] != null)
                    ret[cnt++] = c[i];
            }

            return ret;
        }

        public EventInfo GetEvent(String name)
        {
            return GetEvent(name, Type.DefaultLookup);
        }

        abstract public EventInfo GetEvent(String name, BindingFlags bindingAttr);
        virtual public EventInfo[] GetEvents()
        {
            return GetEvents(Type.DefaultLookup);
        }

        abstract public EventInfo[] GetEvents(BindingFlags bindingAttr);
        public PropertyInfo GetProperty(String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
                        return GetPropertyImpl(name, bindingAttr, binder, returnType, types, modifiers);
        }

        public PropertyInfo GetProperty(String name, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
                        return GetPropertyImpl(name, Type.DefaultLookup, null, returnType, types, modifiers);
        }

        public PropertyInfo GetProperty(String name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException("name");
                        return GetPropertyImpl(name, bindingAttr, null, null, null, null);
        }

        public PropertyInfo GetProperty(String name, Type returnType, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
                        return GetPropertyImpl(name, Type.DefaultLookup, null, returnType, types, null);
        }

        public PropertyInfo GetProperty(String name, Type[] types)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (types == null)
                throw new ArgumentNullException("types");
                        return GetPropertyImpl(name, Type.DefaultLookup, null, null, types, null);
        }

        public PropertyInfo GetProperty(String name, Type returnType)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (returnType == null)
                throw new ArgumentNullException("returnType");
                        return GetPropertyImpl(name, Type.DefaultLookup, null, returnType, null, null);
        }

        internal PropertyInfo GetProperty(String name, BindingFlags bindingAttr, Type returnType)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (returnType == null)
                throw new ArgumentNullException("returnType");
                        return GetPropertyImpl(name, bindingAttr, null, returnType, null, null);
        }

        public PropertyInfo GetProperty(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
                        return GetPropertyImpl(name, Type.DefaultLookup, null, null, null, null);
        }

        protected abstract PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers);
        abstract public PropertyInfo[] GetProperties(BindingFlags bindingAttr);
        public PropertyInfo[] GetProperties()
        {
            return GetProperties(Type.DefaultLookup);
        }

        public Type[] GetNestedTypes()
        {
            return GetNestedTypes(Type.DefaultLookup);
        }

        abstract public Type[] GetNestedTypes(BindingFlags bindingAttr);
        public Type GetNestedType(String name)
        {
            return GetNestedType(name, Type.DefaultLookup);
        }

        abstract public Type GetNestedType(String name, BindingFlags bindingAttr);
        public MemberInfo[] GetMember(String name)
        {
            return GetMember(name, Type.DefaultLookup);
        }

        virtual public MemberInfo[] GetMember(String name, BindingFlags bindingAttr)
        {
            return GetMember(name, MemberTypes.All, bindingAttr);
        }

        virtual public MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        public MemberInfo[] GetMembers()
        {
            return GetMembers(Type.DefaultLookup);
        }

        abstract public MemberInfo[] GetMembers(BindingFlags bindingAttr);
        public virtual MemberInfo[] GetDefaultMembers()
        {
            throw new NotImplementedException();
        }

        public virtual MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, Object filterCriteria)
        {
            MethodInfo[] m = null;
            ConstructorInfo[] c = null;
            FieldInfo[] f = null;
            PropertyInfo[] p = null;
            EventInfo[] e = null;
            Type[] t = null;
            int i = 0;
            int cnt = 0;
            if ((memberType & System.Reflection.MemberTypes.Method) != 0)
            {
                m = GetMethods(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < m.Length; i++)
                        if (!filter(m[i], filterCriteria))
                            m[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += m.Length;
                }
            }

            if ((memberType & System.Reflection.MemberTypes.Constructor) != 0)
            {
                c = GetConstructors(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < c.Length; i++)
                        if (!filter(c[i], filterCriteria))
                            c[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += c.Length;
                }
            }

            if ((memberType & System.Reflection.MemberTypes.Field) != 0)
            {
                f = GetFields(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < f.Length; i++)
                        if (!filter(f[i], filterCriteria))
                            f[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += f.Length;
                }
            }

            if ((memberType & System.Reflection.MemberTypes.Property) != 0)
            {
                p = GetProperties(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < p.Length; i++)
                        if (!filter(p[i], filterCriteria))
                            p[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += p.Length;
                }
            }

            if ((memberType & System.Reflection.MemberTypes.Event) != 0)
            {
                e = GetEvents(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < e.Length; i++)
                        if (!filter(e[i], filterCriteria))
                            e[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += e.Length;
                }
            }

            if ((memberType & System.Reflection.MemberTypes.NestedType) != 0)
            {
                t = GetNestedTypes(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < t.Length; i++)
                        if (!filter(t[i], filterCriteria))
                            t[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += t.Length;
                }
            }

            MemberInfo[] ret = new MemberInfo[cnt];
            cnt = 0;
            if (m != null)
            {
                for (i = 0; i < m.Length; i++)
                    if (m[i] != null)
                        ret[cnt++] = m[i];
            }

            if (c != null)
            {
                for (i = 0; i < c.Length; i++)
                    if (c[i] != null)
                        ret[cnt++] = c[i];
            }

            if (f != null)
            {
                for (i = 0; i < f.Length; i++)
                    if (f[i] != null)
                        ret[cnt++] = f[i];
            }

            if (p != null)
            {
                for (i = 0; i < p.Length; i++)
                    if (p[i] != null)
                        ret[cnt++] = p[i];
            }

            if (e != null)
            {
                for (i = 0; i < e.Length; i++)
                    if (e[i] != null)
                        ret[cnt++] = e[i];
            }

            if (t != null)
            {
                for (i = 0; i < t.Length; i++)
                    if (t[i] != null)
                        ret[cnt++] = t[i];
            }

            return ret;
        }

        public bool IsNested
        {
            
            get
            {
                return DeclaringType != null;
            }
        }

        public TypeAttributes Attributes
        {
            
            get
            {
                return GetAttributeFlagsImpl();
            }
        }

        public virtual GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public bool IsVisible
        {
            
            get
            {
                RuntimeType rt = this as RuntimeType;
                if (rt != null)
                    return RuntimeTypeHandle.IsVisible(rt);
                if (IsGenericParameter)
                    return true;
                if (HasElementType)
                    return GetElementType().IsVisible;
                Type type = this;
                while (type.IsNested)
                {
                    if (!type.IsNestedPublic)
                        return false;
                    type = type.DeclaringType;
                }

                if (!type.IsPublic)
                    return false;
                if (IsGenericType && !IsGenericTypeDefinition)
                {
                    foreach (Type t in GetGenericArguments())
                    {
                        if (!t.IsVisible)
                            return false;
                    }
                }

                return true;
            }
        }

        public bool IsNotPublic
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic);
            }
        }

        public bool IsPublic
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.Public);
            }
        }

        public bool IsNestedPublic
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic);
            }
        }

        public bool IsNestedPrivate
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate);
            }
        }

        public bool IsNestedFamily
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily);
            }
        }

        public bool IsNestedAssembly
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly);
            }
        }

        public bool IsNestedFamANDAssem
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem);
            }
        }

        public bool IsNestedFamORAssem
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem);
            }
        }

        public bool IsAutoLayout
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout);
            }
        }

        public bool IsLayoutSequential
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout);
            }
        }

        public bool IsExplicitLayout
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout);
            }
        }

        public bool IsClass
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class && !IsValueType);
            }
        }

        public bool IsInterface
        {
            
            [System.Security.SecuritySafeCritical]
            get
            {
                RuntimeType rt = this as RuntimeType;
                if (rt != null)
                    return RuntimeTypeHandle.IsInterface(rt);
                return ((GetAttributeFlagsImpl() & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface);
            }
        }

        public bool IsValueType
        {
            
            get
            {
                return IsValueTypeImpl();
            }
        }

        public bool IsAbstract
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.Abstract) != 0);
            }
        }

        public bool IsSealed
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.Sealed) != 0);
            }
        }

        public bool IsEnum
        {
            
            get
            {
                return IsSubclassOf(RuntimeType.EnumType);
            }
        }

        public bool IsSpecialName
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.SpecialName) != 0);
            }
        }

        public bool IsImport
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.Import) != 0);
            }
        }

        public virtual bool IsSerializable
        {
            
            get
            {
                if ((GetAttributeFlagsImpl() & TypeAttributes.Serializable) != 0)
                    return true;
                RuntimeType rt = this.UnderlyingSystemType as RuntimeType;
                if (rt != null)
                    return rt.IsSpecialSerializableType();
                return false;
            }
        }

        public bool IsAnsiClass
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass);
            }
        }

        public bool IsUnicodeClass
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass);
            }
        }

        public bool IsAutoClass
        {
            
            get
            {
                return ((GetAttributeFlagsImpl() & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass);
            }
        }

        public bool IsArray
        {
            
            get
            {
                return IsArrayImpl();
            }
        }

        internal virtual bool IsSzArray
        {
            
            get
            {
                return false;
            }
        }

        public virtual bool IsGenericType
        {
            
            get
            {
                return false;
            }
        }

        public virtual bool IsGenericTypeDefinition
        {
            
            get
            {
                return false;
            }
        }

        public virtual bool IsConstructedGenericType
        {
            
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool IsGenericParameter
        {
            
            get
            {
                return false;
            }
        }

        public virtual int GenericParameterPosition
        {
            
            get
            {
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
            }
        }

        public virtual bool ContainsGenericParameters
        {
            
            get
            {
                if (HasElementType)
                    return GetRootElementType().ContainsGenericParameters;
                if (IsGenericParameter)
                    return true;
                if (!IsGenericType)
                    return false;
                Type[] genericArguments = GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (genericArguments[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }

        public virtual Type[] GetGenericParameterConstraints()
        {
            if (!IsGenericParameter)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
                        throw new InvalidOperationException();
        }

        public bool IsByRef
        {
            
            get
            {
                return IsByRefImpl();
            }
        }

        public bool IsPointer
        {
            
            get
            {
                return IsPointerImpl();
            }
        }

        public bool IsPrimitive
        {
            
            get
            {
                return IsPrimitiveImpl();
            }
        }

        public bool IsCOMObject
        {
            
            get
            {
                return IsCOMObjectImpl();
            }
        }

        internal bool IsWindowsRuntimeObject
        {
            
            get
            {
                return IsWindowsRuntimeObjectImpl();
            }
        }

        internal bool IsExportedToWindowsRuntime
        {
            
            get
            {
                return IsExportedToWindowsRuntimeImpl();
            }
        }

        public bool HasElementType
        {
            
            get
            {
                return HasElementTypeImpl();
            }
        }

        public bool IsContextful
        {
            
            get
            {
                return IsContextfulImpl();
            }
        }

        public bool IsMarshalByRef
        {
            
            get
            {
                return IsMarshalByRefImpl();
            }
        }

        internal bool HasProxyAttribute
        {
            
            get
            {
                return HasProxyAttributeImpl();
            }
        }

        protected virtual bool IsValueTypeImpl()
        {
            return IsSubclassOf(RuntimeType.ValueType);
        }

        abstract protected TypeAttributes GetAttributeFlagsImpl();
        abstract protected bool IsArrayImpl();
        abstract protected bool IsByRefImpl();
        abstract protected bool IsPointerImpl();
        abstract protected bool IsPrimitiveImpl();
        abstract protected bool IsCOMObjectImpl();
        virtual internal bool IsWindowsRuntimeObjectImpl()
        {
            throw new NotImplementedException();
        }

        virtual internal bool IsExportedToWindowsRuntimeImpl()
        {
            throw new NotImplementedException();
        }

        public virtual Type MakeGenericType(params Type[] typeArguments)
        {
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        protected virtual bool IsContextfulImpl()
        {
            return typeof (ContextBoundObject).IsAssignableFrom(this);
        }

        protected virtual bool IsMarshalByRefImpl()
        {
            return typeof (MarshalByRefObject).IsAssignableFrom(this);
        }

        internal virtual bool HasProxyAttributeImpl()
        {
            return false;
        }

        abstract public Type GetElementType();
        public virtual Type[] GetGenericArguments()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        public virtual Type[] GenericTypeArguments
        {
            get
            {
                if (IsGenericType && !IsGenericTypeDefinition)
                {
                    return GetGenericArguments();
                }
                else
                {
                    return Type.EmptyTypes;
                }
            }
        }

        public virtual Type GetGenericTypeDefinition()
        {
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        abstract protected bool HasElementTypeImpl();
        internal Type GetRootElementType()
        {
            Type rootElementType = this;
            while (rootElementType.HasElementType)
                rootElementType = rootElementType.GetElementType();
            return rootElementType;
        }

        public virtual string[] GetEnumNames()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
                        string[] names;
            Array values;
            GetEnumData(out names, out values);
            return names;
        }

        public virtual Array GetEnumValues()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
                        throw new NotImplementedException();
        }

        private Array GetEnumRawConstantValues()
        {
            string[] names;
            Array values;
            GetEnumData(out names, out values);
            return values;
        }

        private void GetEnumData(out string[] enumNames, out Array enumValues)
        {
                                    FieldInfo[] flds = GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object[] values = new object[flds.Length];
            string[] names = new string[flds.Length];
            for (int i = 0; i < flds.Length; i++)
            {
                names[i] = flds[i].Name;
                values[i] = flds[i].GetRawConstantValue();
            }

            IComparer comparer = Comparer.Default;
            for (int i = 1; i < values.Length; i++)
            {
                int j = i;
                string tempStr = names[i];
                object val = values[i];
                bool exchanged = false;
                while (comparer.Compare(values[j - 1], val) > 0)
                {
                    names[j] = names[j - 1];
                    values[j] = values[j - 1];
                    j--;
                    exchanged = true;
                    if (j == 0)
                        break;
                }

                if (exchanged)
                {
                    names[j] = tempStr;
                    values[j] = val;
                }
            }

            enumNames = names;
            enumValues = values;
        }

        public virtual Type GetEnumUnderlyingType()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
                        FieldInfo[] fields = GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fields == null || fields.Length != 1)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidEnum"), "enumType");
            return fields[0].FieldType;
        }

        public virtual bool IsEnumDefined(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
                        Type valueType = value.GetType();
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(this))
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", valueType.ToString(), this.ToString()));
                valueType = valueType.GetEnumUnderlyingType();
            }

            if (valueType == typeof (string))
            {
                string[] names = GetEnumNames();
                if (Array.IndexOf(names, value) >= 0)
                    return true;
                else
                    return false;
            }

            if (Type.IsIntegerType(valueType))
            {
                Type underlyingType = GetEnumUnderlyingType();
                if (underlyingType.GetTypeCodeImpl() != valueType.GetTypeCodeImpl())
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType", valueType.ToString(), underlyingType.ToString()));
                Array values = GetEnumRawConstantValues();
                return (BinarySearch(values, value) >= 0);
            }
            else if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType", valueType.ToString(), GetEnumUnderlyingType()));
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
            }
        }

        public virtual string GetEnumName(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
                        Type valueType = value.GetType();
            if (!(valueType.IsEnum || Type.IsIntegerType(valueType)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");
            Array values = GetEnumRawConstantValues();
            int index = BinarySearch(values, value);
            if (index >= 0)
            {
                string[] names = GetEnumNames();
                return names[index];
            }

            return null;
        }

        private static int BinarySearch(Array array, object value)
        {
            ulong[] ulArray = new ulong[array.Length];
            for (int i = 0; i < array.Length; ++i)
                ulArray[i] = Enum.ToUInt64(array.GetValue(i));
            ulong ulValue = Enum.ToUInt64(value);
            return Array.BinarySearch(ulArray, ulValue);
        }

        internal static bool IsIntegerType(Type t)
        {
            return (t == typeof (int) || t == typeof (short) || t == typeof (ushort) || t == typeof (byte) || t == typeof (sbyte) || t == typeof (uint) || t == typeof (long) || t == typeof (ulong) || t == typeof (char) || t == typeof (bool));
        }

        public virtual bool IsSecurityCritical
        {
            
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool IsSecuritySafeCritical
        {
            
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool IsSecurityTransparent
        {
            
            get
            {
                throw new NotImplementedException();
            }
        }

        internal bool NeedsReflectionSecurityCheck
        {
            get
            {
                if (!IsVisible)
                {
                    return true;
                }
                else if (IsSecurityCritical && !IsSecuritySafeCritical)
                {
                    return true;
                }
                else if (IsGenericType)
                {
                    foreach (Type genericArgument in GetGenericArguments())
                    {
                        if (genericArgument.NeedsReflectionSecurityCheck)
                        {
                            return true;
                        }
                    }
                }
                else if (IsArray || IsPointer)
                {
                    return GetElementType().NeedsReflectionSecurityCheck;
                }

                return false;
            }
        }

        public abstract Type UnderlyingSystemType
        {
            get;
        }

        public virtual bool IsSubclassOf(Type c)
        {
            Type p = this;
            if (p == c)
                return false;
            while (p != null)
            {
                if (p == c)
                    return true;
                p = p.BaseType;
            }

            return false;
        }

        public virtual bool IsInstanceOfType(Object o)
        {
            if (o == null)
                return false;
            return IsAssignableFrom(o.GetType());
        }

        public virtual bool IsAssignableFrom(Type c)
        {
            if (c == null)
                return false;
            if (this == c)
                return true;
            RuntimeType toType = this.UnderlyingSystemType as RuntimeType;
            if (toType != null)
                return toType.IsAssignableFrom(c);
            if (c.IsSubclassOf(this))
                return true;
            if (this.IsInterface)
            {
                return c.ImplementInterface(this);
            }
            else if (IsGenericParameter)
            {
                Type[] constraints = GetGenericParameterConstraints();
                for (int i = 0; i < constraints.Length; i++)
                    if (!constraints[i].IsAssignableFrom(c))
                        return false;
                return true;
            }

            return false;
        }

        public virtual bool IsEquivalentTo(Type other)
        {
            return (this == other);
        }

        internal bool ImplementInterface(Type ifaceType)
        {
                                    Type t = this;
            while (t != null)
            {
                Type[] interfaces = t.GetInterfaces();
                if (interfaces != null)
                {
                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        if (interfaces[i] == ifaceType || (interfaces[i] != null && interfaces[i].ImplementInterface(ifaceType)))
                            return true;
                    }
                }

                t = t.BaseType;
            }

            return false;
        }

        internal string FormatTypeName()
        {
            return FormatTypeName(false);
        }

        internal virtual string FormatTypeName(bool serialization)
        {
            throw new NotImplementedException();
        }

        public override String ToString()
        {
            return "Type: " + Name;
        }

        public static Type[] GetTypeArray(Object[] args)
        {
            if (args == null)
                throw new ArgumentNullException("args");
                        Type[] cls = new Type[args.Length];
            for (int i = 0; i < cls.Length; i++)
            {
                if (args[i] == null)
                    throw new ArgumentNullException();
                cls[i] = args[i].GetType();
            }

            return cls;
        }

        public override bool Equals(Object o)
        {
            if (o == null)
                return false;
            return Equals(o as Type);
        }

        public bool Equals(Type o)
        {
            if ((object)o == null)
                return false;
            return (Object.ReferenceEquals(this.UnderlyingSystemType, o.UnderlyingSystemType));
        }

        public override int GetHashCode()
        {
            Type SystemType = UnderlyingSystemType;
            if (!Object.ReferenceEquals(SystemType, this))
                return SystemType.GetHashCode();
            return base.GetHashCode();
        }

        public virtual InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
        }

        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        internal const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    }
}