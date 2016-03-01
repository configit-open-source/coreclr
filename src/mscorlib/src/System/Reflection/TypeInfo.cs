namespace System.Reflection
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    public abstract class TypeInfo : Type, IReflectableType
    {
        internal TypeInfo()
        {
        }

        TypeInfo IReflectableType.GetTypeInfo()
        {
            return this;
        }

        public virtual Type AsType()
        {
            return (Type)this;
        }

        public virtual Type[] GenericTypeParameters
        {
            get
            {
                if (IsGenericTypeDefinition)
                {
                    return GetGenericArguments();
                }
                else
                {
                    return Type.EmptyTypes;
                }
            }
        }

        public virtual bool IsAssignableFrom(TypeInfo typeInfo)
        {
            if (typeInfo == null)
                return false;
            if (this == typeInfo)
                return true;
            if (typeInfo.IsSubclassOf(this))
                return true;
            if (this.IsInterface)
            {
                return typeInfo.ImplementInterface(this);
            }
            else if (IsGenericParameter)
            {
                Type[] constraints = GetGenericParameterConstraints();
                for (int i = 0; i < constraints.Length; i++)
                    if (!constraints[i].IsAssignableFrom(typeInfo))
                        return false;
                return true;
            }

            return false;
        }

        public virtual EventInfo GetDeclaredEvent(String name)
        {
            return GetEvent(name, Type.DeclaredOnlyLookup);
        }

        public virtual FieldInfo GetDeclaredField(String name)
        {
            return GetField(name, Type.DeclaredOnlyLookup);
        }

        public virtual MethodInfo GetDeclaredMethod(String name)
        {
            return GetMethod(name, Type.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<MethodInfo> GetDeclaredMethods(String name)
        {
            foreach (MethodInfo method in GetMethods(Type.DeclaredOnlyLookup))
            {
                if (method.Name == name)
                    yield return method;
            }
        }

        public virtual System.Reflection.TypeInfo GetDeclaredNestedType(String name)
        {
            var nt = GetNestedType(name, Type.DeclaredOnlyLookup);
            if (nt == null)
            {
                return null;
            }
            else
            {
                return nt.GetTypeInfo();
            }
        }

        public virtual PropertyInfo GetDeclaredProperty(String name)
        {
            return GetProperty(name, Type.DeclaredOnlyLookup);
        }

        public virtual IEnumerable<ConstructorInfo> DeclaredConstructors
        {
            get
            {
                return GetConstructors(Type.DeclaredOnlyLookup);
            }
        }

        public virtual IEnumerable<EventInfo> DeclaredEvents
        {
            get
            {
                return GetEvents(Type.DeclaredOnlyLookup);
            }
        }

        public virtual IEnumerable<FieldInfo> DeclaredFields
        {
            get
            {
                return GetFields(Type.DeclaredOnlyLookup);
            }
        }

        public virtual IEnumerable<MemberInfo> DeclaredMembers
        {
            get
            {
                return GetMembers(Type.DeclaredOnlyLookup);
            }
        }

        public virtual IEnumerable<MethodInfo> DeclaredMethods
        {
            get
            {
                return GetMethods(Type.DeclaredOnlyLookup);
            }
        }

        public virtual IEnumerable<System.Reflection.TypeInfo> DeclaredNestedTypes
        {
            get
            {
                foreach (var t in GetNestedTypes(Type.DeclaredOnlyLookup))
                {
                    yield return t.GetTypeInfo();
                }
            }
        }

        public virtual IEnumerable<PropertyInfo> DeclaredProperties
        {
            get
            {
                return GetProperties(Type.DeclaredOnlyLookup);
            }
        }

        public virtual IEnumerable<Type> ImplementedInterfaces
        {
            get
            {
                return GetInterfaces();
            }
        }
    }
}